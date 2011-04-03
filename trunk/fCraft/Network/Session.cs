// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace fCraft {
    public sealed class Session {
        public Player Player;
        public DateTime LoginTime = DateTime.Now;

        public bool CanReceive = true,
                    CanSend = true,
                    CanQueue = true,
                    HasRegistered;

        readonly object joinWorldLock = new object();
        public LeaveReason LeaveReason { get; set; }

        Thread ioThread;

        // networking
        TcpClient client;
        BinaryReader reader;
        PacketWriter writer;
        public ConcurrentQueue<Packet> OutputQueue,
                                       PriorityOutputQueue;

        // joining worlds
        World forcedWorldToJoin;
        Position? postJoinPosition;

        // movement optimization
        int fullPositionUpdateCounter;
        public const int FullPositionUpdateIntervalDefault = 20;
        public static int FullPositionUpdateInterval = FullPositionUpdateIntervalDefault;
        bool skippedLastMovementPacket;
        const int SkipMovementThreshold = 64,
                  SkipRotationThresholdSquared = 1500;

        // anti-speedhack vars
        int speedHackDetectionCounter;
        const int AntiSpeedMaxJumpDelta = 25, // 16 for normal client, 25 for WoM
                  AntiSpeedMaxDistanceSquared = 1024, // 32 * 32
                  AntiSpeedMaxPacketCount = 200,
                  AntiSpeedMaxPacketInterval = 5;

        const int SleepDelay = 10;


        const int SocketTimeout = 10000;
        readonly Queue<DateTime> antiSpeedPacketLog = new Queue<DateTime>();
        DateTime antiSpeedLastNotification = DateTime.UtcNow;


        public Session( TcpClient tcpClient ) {
            LeaveReason = LeaveReason.Unknown;
            OutputQueue = new ConcurrentQueue<Packet>();
            PriorityOutputQueue = new ConcurrentQueue<Packet>();

            client = tcpClient;
            client.SendTimeout = SocketTimeout;
            client.ReceiveTimeout = SocketTimeout;
        }


        public void Start() {
            try {
                if( Server.RaiseSessionConnectingEvent( GetIP() ) ) return;

                reader = new BinaryReader( client.GetStream() );
                writer = new PacketWriter( client.GetStream() );

                Logger.Log( "Session.Start: Incoming connection from {0}", LogType.Debug,
                            GetIP().ToString() );

                ioThread = new Thread( IoLoop ) { IsBackground = true };
                ioThread.Start();
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Session failed to start", "fCraft", ex, false );
                Disconnect();
            }
        }


        void IoLoop() {
            try {
                Server.RaiseSessionConnectedEvent( this );

                Packet packet = new Packet();

                const int pollInterval = 250;
                int pollCounter = 0;

                const int pingInterval = 5;
                int pingCounter = 0;

                // try to log the player in, otherwise die.
                if( !LoginSequence() ) return;

                Server.FirePlayerConnectedEvent( this );

                // main i/o loop
                while( CanSend ) {
                    Thread.Sleep( SleepDelay );

                    int packetsSent = 0;

                    // detect player disconnect
                    if( pollCounter > pollInterval ) {
                        if( !client.Connected ||
                            (client.Client.Poll( 1000, SelectMode.SelectRead ) && client.Client.Available == 0) ) {
                            if( Player != null ) {
                                Logger.Log( "Session.IoLoop: Lost connection to player {0} ({1}).", LogType.Debug, Player.Name, GetIP() );
                            } else {
                                Logger.Log( "Session.IoLoop: Lost connection to unidentified player at {0}.", LogType.Debug, GetIP() );
                            }
                            LeaveReason = LeaveReason.ClientQuit;
                            return;
                        }
                        if( pingCounter > pingInterval ) {
                            writer.WritePing();
                        }
                        pingCounter++;
                        pollCounter = 0;
                    }
                    pollCounter++;

                    // send output to player
                    while( CanSend && packetsSent < Server.MaxSessionPacketsPerTick ) {
                        if( !PriorityOutputQueue.Dequeue( ref packet ) )
                            if( !OutputQueue.Dequeue( ref packet ) ) break;

                        if( Player.IsDeaf && packet.OpCode == OutputCode.Message ) continue;

                        writer.Write( packet.Data );
                        packetsSent++;

                        if( packet.OpCode == OutputCode.Disconnect ) {
                            writer.Flush();
                            Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug,
                                        Player.Name );
                            if( LeaveReason == LeaveReason.Unknown ) LeaveReason = LeaveReason.Kick;
                            return;
                        }
                    }

                    // check if player needs to change worlds
                    if( CanSend ) {
                        lock( joinWorldLock ) {
                            if( forcedWorldToJoin != null ) {
                                while( PriorityOutputQueue.Dequeue( ref packet ) ) {
                                    writer.Write( packet.Data );
                                    packetsSent++;
                                    if( packet.Data[0] == (byte)OutputCode.Disconnect ) {
                                        writer.Flush();
                                        Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug,
                                                    Player.Name );
                                        if( LeaveReason == LeaveReason.Unknown ) LeaveReason = LeaveReason.Kick;
                                        return;
                                    }
                                }
                                JoinWorldNow( forcedWorldToJoin, false );
                                forcedWorldToJoin = null;
                            }
                        }
                    }

                    // get input from player
                    while( CanReceive && client.GetStream().DataAvailable ) {
                        byte opcode = reader.ReadByte();
                        switch( (InputCode)opcode ) {

                            // Message
                            case InputCode.Message:
                                Player.ResetIdleTimer();
                                reader.ReadByte();
                                string message = ReadString();
                                if( Player.CheckForIllegalChars( message ) ) {
                                    Logger.Log( "Player.ParseMessage: {0} attempted to write illegal characters in chat and was kicked.", LogType.SuspiciousActivity,
                                                Player.Name );
                                    Server.SendToAll( "{0}&W was kicked for attempted hacking (0x0d).", Player.GetClassyName() );
                                    KickNow( "Illegal characters in chat.", LeaveReason.InvalidMessageKick );
                                    return;
                                } else {
                                    try {
                                        Player.ParseMessage( message, false );
                                    } catch( IOException ) {
                                        throw;
                                    } catch( SocketException ) {
                                        throw;
                                    } catch( Exception ex ) {
                                        Logger.LogAndReportCrash( "Error while parsing player's message", "fCraft", ex, false );
                                        Player.MessageNow( "&WAn error occured while trying to process your message. " +
                                                           "Error details have been logged. " +
                                                           "It is recommended that you reconnect to the server." );
                                    }
                                }
                                break;

                            // Player movement
                            case InputCode.MoveRotate:
                                reader.ReadByte();
                                Position newPos = new Position {
                                    X = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                                    H = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                                    Y = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                                    R = reader.ReadByte(),
                                    L = reader.ReadByte()
                                };

                                Position oldPos = Player.Position;

                                // calculate difference between old and new positions
                                Position delta = new Position {
                                    X = (short)(newPos.X - oldPos.X),
                                    Y = (short)(newPos.Y - oldPos.Y),
                                    H = (short)(newPos.H - oldPos.H),
                                    R = (byte)Math.Abs( newPos.R - oldPos.R ),
                                    L = (byte)Math.Abs( newPos.L - oldPos.L )
                                };
                                // skip everything if player hasn't moved
                                if( delta.IsZero() ) continue;

                                bool posChanged = (delta.X != 0) || (delta.Y != 0) || (delta.H != 0);
                                bool rotChanged = (delta.R != 0) || (delta.L != 0);
                                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.H * delta.H;

                                // only reset the timer if player rotated
                                // if player is just pushed around, rotation does not change (and timer should not reset)
                                if( rotChanged ) Player.ResetIdleTimer();

                                if( Player.Info.IsFrozen ) {
                                    // special handling for frozen players
                                    if( delta.X * delta.X + delta.Y * delta.Y > AntiSpeedMaxDistanceSquared ||
                                        Math.Abs( delta.H ) > 40 ) {
                                        SendNow( PacketWriter.MakeSelfTeleport( Player.Position ) );
                                    }
                                    newPos.X = Player.Position.X;
                                    newPos.Y = Player.Position.Y;
                                    newPos.H = Player.Position.H;

                                    // recalculate deltas
                                    delta.X = 0;
                                    delta.Y = 0;
                                    delta.H = 0;
                                    posChanged = false;
                                    distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.H * delta.H;

                                } else if( !Player.Can( Permission.UseSpeedHack ) ) {
                                    // speedhack detection
                                    if( DetectMovementPacketSpam() ) {
                                        continue;

                                    } else if( (distSquared - delta.H * delta.H > AntiSpeedMaxDistanceSquared || delta.H > AntiSpeedMaxJumpDelta) &&
                                               speedHackDetectionCounter >= 0 ) {

                                        if( speedHackDetectionCounter == 0 ) {
                                            Player.LastValidPosition = Player.Position;
                                        } else if( speedHackDetectionCounter > 1 ) {
                                            DenyMovement();
                                            speedHackDetectionCounter = 0;
                                            continue;
                                        }
                                        speedHackDetectionCounter++;

                                    } else {
                                        speedHackDetectionCounter = 0;
                                    }
                                }

                                // movement optimization
                                if( distSquared < SkipMovementThreshold &&
                                    (delta.R * delta.R + delta.L * delta.L) < SkipRotationThresholdSquared &&
                                    !skippedLastMovementPacket ) {

                                    skippedLastMovementPacket = true;
                                    continue;
                                }
                                skippedLastMovementPacket = false;

                                if( Server.RaisePlayerMovingEvent( Player, newPos ) ) {
                                    DenyMovement();
                                    continue;
                                }

                                // create the movement packet
                                if( delta.FitsIntoByte() && fullPositionUpdateCounter < FullPositionUpdateInterval ) {
                                    if( posChanged && rotChanged ) {
                                        // incremental position + rotation update
                                        packet = PacketWriter.MakeMoveRotate( Player.ID, new Position {
                                            X = delta.X,
                                            Y = delta.Y,
                                            H = delta.H,
                                            R = newPos.R,
                                            L = newPos.L
                                        } );

                                    } else if( posChanged ) {
                                        // incremental position update
                                        packet = PacketWriter.MakeMove( Player.ID, delta );

                                    } else {
                                        // absolute rotation update
                                        packet = PacketWriter.MakeRotate( Player.ID, newPos );
                                    }

                                } else {
                                    // full (absolute position + rotation) update
                                    packet = PacketWriter.MakeTeleport( Player.ID, newPos );
                                }

                                fullPositionUpdateCounter++;
                                if( fullPositionUpdateCounter >= FullPositionUpdateInterval ) {
                                    fullPositionUpdateCounter = 0;
                                }

                                Player.Position = newPos;
                                Server.RaisePlayerMovedEvent( Player, oldPos );
                                Player.World.SendToSeeing( packet, Player );
                                break;

                            // Set tile
                            case InputCode.SetTile:
                                if( Player.World == null || Player.World.Map == null ) continue;
                                Player.ResetIdleTimer();
                                short x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                short h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                short y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                byte mode = reader.ReadByte();
                                byte type = reader.ReadByte();

                                if( type > 49 ) {
                                    Logger.Log( "{0} was kicked for sending bad SetTile packets.", LogType.SuspiciousActivity,
                                                Player.Name );
                                    Server.SendToAll( "{0}&W was kicked for attempted hacking (0x05).", Player.GetClassyName() );
                                    KickNow( "Hacking detected: illegal SetTile packet.", LeaveReason.InvalidSetTileKick );
                                    return;
                                } else if( !Player.World.Map.InBounds( x, y, h ) ) {
                                    continue;
                                } else {
                                    if( Player.PlaceBlock( x, y, h, mode == 1, (Block)type ) ) return;
                                }
                                break;

                            case InputCode.Ping:
                                continue;

                            default:
                                KickNow( "Unknown packet opcode.", LeaveReason.InvalidOpcodeKick );
                                return;
                        }
                    }
                }

            } catch( IOException ex ) {
                LeaveReason = LeaveReason.ClientQuit;
                Logger.Log( "Session.IoLoop: {0}", LogType.Debug, ex.Message );

            } catch( SocketException ex ) {
                LeaveReason = LeaveReason.ClientQuit;
                Logger.Log( "Session.IoLoop: {0}", LogType.Debug, ex.Message );
#if !DEBUG
            } catch( Exception ex ) {
                LeaveReason = LeaveReason.ServerError;
                Logger.LogAndReportCrash( "Error in Session.IoLoop", "fCraft", ex, false );
#endif
            } finally {
                CanQueue = false;
                CanSend = false;
                Disconnect();
            }
        }


        void DenyMovement() {
            SendNow( PacketWriter.MakeSelfTeleport( Player.LastValidPosition ) );
            if( DateTime.UtcNow.Subtract( antiSpeedLastNotification ).Seconds > 1 ) {
                Player.Message( "&WYou are not allowed to speedhack." );
                antiSpeedLastNotification = DateTime.UtcNow;
            }
        }


        public void Disconnect() {
            Server.UnregisterSession( this );
            Server.RaiseSessionDisconnectedEvent( this, LeaveReason );

            if( Player != null ) {
                Server.UnregisterPlayer( Player );
                Server.RaisePlayerDisconnectedEventArgs( Player, LeaveReason );
                Player = null;
            }

            if( reader != null ) {
                reader.Close();
                reader = null;
            }

            if( writer != null ) {
                writer.Close();
                writer = null;
            }

            if( client != null ) {
                client.Close();
                client = null;
            }

            ioThread = null;
        }


        const string NoSmpMessage = "This server is for Minecraft Classic only.";
        bool LoginSequence() {
            byte opcode = reader.ReadByte();
            if( opcode != (byte)InputCode.Handshake ) {
                if( opcode == 2 ) {
                    // This may be someone connecting with an SMP client
                    int strLen = IPAddress.NetworkToHostOrder( reader.ReadInt16() );

                    if( strLen >= 2 && strLen <= 16 ) {
                        string smpPlayerName = Encoding.UTF8.GetString( reader.ReadBytes( strLen ) );

                        Logger.Log( "Session.LoginSequence: Player \"{0}\" tried connecting with SMP/Beta client from {1}. " +
                                    "fCraft does not support SMP/Beta.", LogType.Warning,
                                    smpPlayerName, GetIP() );

                        // send SMP KICK packet
                        writer.Write( (byte)255 );
                        byte[] stringData = Encoding.UTF8.GetBytes( NoSmpMessage );
                        writer.Write( (short)stringData.Length );
                        writer.Write( stringData );
                        writer.Flush();

                    } else {
                        // Not SMP client (invalid player name length)
                        Logger.Log( "Session.LoginSequence: Unexpected opcode in the first packet from {0}: {1}.", LogType.Error,
                                    GetIP(), opcode );
                        KickNow( "Unexpected handshake message - possible protocol mismatch!", LeaveReason.ProtocolViolation );
                    }
                    return false;

                } else {
                    Logger.Log( "Session.LoginSequence: Unexpected opcode in the first packet from {0}: {1}.", LogType.Error,
                                GetIP(), opcode );
                    KickNow( "Unexpected handshake message - possible protocol mismatch!", LeaveReason.ProtocolViolation );
                    return false;
                }
            }

            // check protocol version
            int clientProtocolVersion = reader.ReadByte();
            if( clientProtocolVersion != Config.ProtocolVersion ) {
                Logger.Log( "Session.LoginSequence: Wrong protocol version: {0}.", LogType.Error,
                            clientProtocolVersion );
                KickNow( "Incompatible protocol version!", LeaveReason.ProtocolViolation );
                return false;
            }

            // check name for nonstandard characters
            string playerName = ReadString();
            string verificationCode = ReadString();
            reader.ReadByte(); // unused    

            if( !Player.IsValidName( playerName ) ) {
                Logger.Log( "Session.LoginSequence: Unacceptible player name: {0} ({1})", LogType.SuspiciousActivity,
                            playerName, GetIP() );
                KickNow( "Invalid characters in player name!", LeaveReason.ProtocolViolation );
                return false;
            }

            // check if player is banned
            Player = new Player( null, playerName, this, Server.MainWorld.Map.Spawn );
            if( Player.Info.Banned ) {
                Player.Info.ProcessFailedLogin( Player );
                Logger.Log( "Banned player {0} tried to log in.", LogType.SuspiciousActivity,
                            Player.Name );
                if( ConfigKey.ShowBannedConnectionMessages.GetBool() ) {
                    Server.SendToAll( "&SBanned player {0}&S tried to log in.", Player.GetClassyName() );
                }
                string bannedMessage = String.Format( "Banned {0} ago by {1}: {2}",
                                                      DateTime.Now.Subtract( Player.Info.BanDate ).ToMiniString(),
                                                      Player.Info.BannedBy,
                                                      Player.Info.BanReason );
                KickNow( bannedMessage, LeaveReason.LoginFailed );
                return false;
            }

            bool showVerifyNamesWarning = false;

            // verify name
            if( !Server.VerifyName( Player.Name, verificationCode, Server.Salt ) ) {
                NameVerificationMode nameVerificationMode = ConfigKey.VerifyNames.GetEnum<NameVerificationMode>();

                string standardMessage = String.Format( "Session.LoginSequence: Could not verify player name for {0} ({1}).",
                                                        Player.Name, GetIP() );
                if( GetIP().ToString() == "127.0.0.1" && nameVerificationMode == NameVerificationMode.Always ) {
                    Logger.Log( "{0} Player was identified as connecting from localhost and allowed in.", LogType.SuspiciousActivity,
                                standardMessage );

                } else if( GetIP().IsLAN() && ConfigKey.AllowUnverifiedLAN.GetBool() ) {
                    Logger.Log( "{0} Player was identified as connecting from LAN and allowed in.", LogType.SuspiciousActivity,
                                standardMessage );

                } else if( Player.Info.TimesVisited == 1 || Player.Info.LastIP.ToString() != GetIP().ToString() ) {
                    switch( nameVerificationMode ) {
                        case NameVerificationMode.Always:
                        case NameVerificationMode.Balanced:
                            Player.Info.ProcessFailedLogin( Player );
                            Logger.Log( "{0} IP did not match. Player was kicked.", LogType.SuspiciousActivity,
                                        standardMessage );
                            KickNow( "Could not verify player name!", LeaveReason.UnverifiedName );
                            return false;
                        case NameVerificationMode.Never:
                            Logger.Log( "{0} IP did not match. " +
                                        "Player was allowed in anyway because VerifyNames is set to Never.", LogType.SuspiciousActivity,
                                        standardMessage );
                            Player.Message( "&WYour name could not be verified." );
                            showVerifyNamesWarning = true;
                            break;
                    }

                } else {
                    switch( nameVerificationMode ) {
                        case NameVerificationMode.Always:
                            Player.Info.ProcessFailedLogin( Player );
                            Logger.Log( "{0} IP matched previous records for that name. " +
                                        "Player was kicked anyway because VerifyNames is set to Always.", LogType.SuspiciousActivity,
                                        standardMessage );
                            KickNow( "Could not verify player name!", LeaveReason.UnverifiedName );
                            return false;
                        case NameVerificationMode.Balanced:
                        case NameVerificationMode.Never:
                            Logger.Log( "{0} IP matched previous records for that name. Player was allowed in.", LogType.SuspiciousActivity,
                                        standardMessage );
                            break;
                    }
                }
            }

            // check if player's IP is banned
            IPBanInfo ipBanInfo = IPBanList.Get( GetIP() );
            if( ipBanInfo != null ) {
                Player.Info.ProcessFailedLogin( Player );
                ipBanInfo.ProcessAttempt( Player );
                if( ConfigKey.ShowBannedConnectionMessages.GetBool() ) {
                    Server.SendToAll( "{0}&S tried to log in from a banned IP.", Player.GetClassyName() );
                }
                Logger.Log( "{0} tried to log in from a banned IP.", LogType.SuspiciousActivity,
                            Player.Name );
                string bannedMessage = String.Format( "IP-banned {0} ago by {1}: {2}",
                                                      DateTime.Now.Subtract( ipBanInfo.BanDate ).ToMiniString(),
                                                      ipBanInfo.BannedBy,
                                                      ipBanInfo.BanReason );
                KickNow( bannedMessage, LeaveReason.LoginFailed );
                return false;
            }

            if( ConfigKey.PaidPlayersOnly.GetBool() ) {
                // write a "please wait" message while we validate player's paid status
                writer.Write( PacketWriter.MakeHandshake( Player,
                                                          ConfigKey.ServerName.GetString(),
                                                          "Please wait; Validating paid status..." ) );
                writer.Flush();

                if( !Player.CheckPaidStatus( Player.Name ) ) {
                    KickNow( "Paid players allowed only.", LeaveReason.LoginFailed );
                    return false;
                }
            }

            // Any additional security checks should be done right here
            if( Server.RaisePlayerConnectingEvent( Player ) ) return false;


            // ----==== beyond this point, player is considered connecting (allowed to join) ====----


            // check if another player with the same name is on
            Server.KickGhostsAndRegisterSession( this );

            if( ConfigKey.LimitOneConnectionPerIP.GetBool() ) {
                // note: FindPlayers only counts REGISTERED players
                Player[] potentialClones = Server.FindPlayers( GetIP() );
                if( potentialClones.Length > 0 ) {
                    Player.Info.ProcessFailedLogin( Player );
                    Logger.Log( "Session.LoginSequence: Player {0} tried to log in from same IP ({1}) as {2}.", LogType.SuspiciousActivity,
                                Player.Name, GetIP(), potentialClones[0].Name );
                    foreach( Player clone in potentialClones ) {
                        clone.Message( "Warning: someone just attempted to log in using your IP." );
                    }
                    KickNow( "Only one connection per IP allowed!", LeaveReason.LoginFailed );
                    return false;
                }
            }

            // Register player for future block updates
            if( !Server.RegisterPlayer( Player ) ) {
                KickNow( "Sorry, server is full (" + Server.PlayerList.Length + "/" + ConfigKey.MaxPlayers.GetInt() + ")", LeaveReason.ServerFull );
                return false;
            }
            Player.Info.ProcessLogin( Player );


            // ----==== Beyond this point, player is considered connected (authenticated and registered) ====----


            World startingWorld = Server.RaisePlayerConnectedEvent( Player, Server.MainWorld );

            // Send server information
            writer.Write( PacketWriter.MakeHandshake( Player, ConfigKey.ServerName.GetString(), ConfigKey.MOTD.GetString() ) );

            // AutoRank
            if( ConfigKey.AutoRankEnabled.GetBool() ) {
                Rank newRank = AutoRank.Check( Player.Info );
                if( newRank != null ) {
                    AdminCommands.DoChangeRank( Player.Console, Player.Info, newRank, "~AutoRank", false, true );
                }
            }

            bool firstTime = (Player.Info.TimesVisited == 1);
            if( !JoinWorldNow( startingWorld, true ) ) {
                Logger.Log( "Failed to load main world ({0}) for connecting player {1} (from {2})", LogType.Error,
                            startingWorld.Name, Player.Name, GetIP() );
                return false;
            }


            // ==== Beyond this point, player is considered ready (has a world) ====


            if( showVerifyNamesWarning ) {
                Server.SendToAllExcept( "&WName and IP of {0}&W are unverified!", Player,
                                        Player.GetClassyName() );
            }

            // Check if other banned players logged in from this IP
            PlayerInfo[] bannedPlayerNames = PlayerDB.FindPlayers( GetIP(), 25 ).Where( playerFromSameIP => playerFromSameIP.Banned ).ToArray();
            if( bannedPlayerNames.Length > 0 ) {
                string logString = String.Format( "&WPlayer {0}&W logged in from an IP previously used by banned players: {1}",
                                                  Player.GetClassyName(),
                                                  PlayerInfo.PlayerInfoArrayToString( bannedPlayerNames ) );
                Server.SendToAll( logString );
                Logger.Log( logString, LogType.SuspiciousActivity );
            }

            // Announce join
            if( ConfigKey.ShowConnectionMessages.GetBool() ) {
                Server.SendToAllExcept( Server.MakePlayerConnectedMessage( Player, firstTime, Player.World ), Player );
            }

            // check if player is still muted
            if( Player.Info.MutedUntil > DateTime.UtcNow ) {
                int secondsLeft = (int)Player.Info.MutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds;
                Player.Message( "&WYou were previously muted by {0}, {1} seconds left.",
                                Player.Info.MutedBy, secondsLeft );
                Server.SendToAllExcept( "&WPlayer {0}&W was previously muted by {1}&W, {2} seconds left.", Player,
                                        Player.GetClassyName(), Player.Info.MutedBy, secondsLeft );
            }

            // check if player is still frozen
            if( Player.Info.IsFrozen ) {
                if( Player.Info.FrozenOn != DateTime.MinValue ) {
                    Player.Message( "&WYou were previously frozen {0} ago by {1}",
                                    DateTime.Now.Subtract( Player.Info.FrozenOn ).ToMiniString(),
                                    Player.Info.FrozenBy );
                    Server.SendToAllExcept( "&WPlayer {0}&W was previously frozen {1} ago by {2}.", Player,
                                            Player.GetClassyName(),
                                            DateTime.Now.Subtract( Player.Info.FrozenOn ).ToMiniString(),
                                            Player.Info.FrozenBy );
                } else {
                    Player.Message( "&WYou were previously frozen by {0}",
                                    Player.Info.FrozenBy );
                    Server.SendToAllExcept( "&WPlayer {0}&W was previously frozen by {1}.", Player,
                                            Player.GetClassyName(),
                                            Player.Info.FrozenBy );
                }
            }

            // Welcome message
            if( File.Exists( Paths.GreetingFileName ) ) {
                string[] greetingText = File.ReadAllLines( Paths.GreetingFileName );
                foreach( string greetingLine in greetingText ) {
                    StringBuilder sb = new StringBuilder( greetingLine );
                    sb.Replace( "{SERVER_NAME}", ConfigKey.ServerName.GetString() );
                    sb.Replace( "{RANK}", Player.Info.Rank.GetClassyName() );
                    sb.Replace( "{PLAYER_NAME}", Player.GetClassyName() );
                    sb.Replace( "{TIME}", DateTime.Now.ToShortTimeString() );
                    Player.Message( sb.ToString() );
                }
            } else {
                if( firstTime ) {
                    Player.Message( "Welcome to {0}", ConfigKey.ServerName.GetString() );
                } else {
                    Player.Message( "Welcome back to {0}", ConfigKey.ServerName.GetString() );
                }

                Player.Message( "Your rank is {0}&S. Type &H/help&S for help.",
                                Player.Info.Rank.GetClassyName() );
            }

            // A reminder for first-time users
            if( PlayerDB.CountTotalPlayers() == 1 && Player.Info.Rank != RankList.HighestRank ) {
                Player.Message( "Type &H/rank {0} {1}&S in console to promote yourself",
                                Player.Name, RankList.HighestRank.Name );
            }

            Server.RaisePlayerReadyEvent( Player );

            return true;
        }


        public void JoinWorld( World newWorld, Position? position ) {
            lock( joinWorldLock ) {
                postJoinPosition = position;
                forcedWorldToJoin = newWorld;
            }
        }


        internal bool JoinWorldNow( World newWorld, bool firstTime ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );

            if( !Player.CanJoin( newWorld ) ) {
                Logger.Log( "Session.JoinWorldNow: Access limits prevented {0} from joining {1}.", LogType.Error,
                            Player.Name, newWorld.Name );
                return false;
            }

            if( !newWorld.FirePlayerTriedToJoinEvent( Player ) ) {
                Logger.Log( "Session.JoinWorldNow: FirePlayerTriedToJoinEvent prevented {0} from joining {1}", LogType.Warning,
                            Player.Name, newWorld.Name );
                return false;
            }

            World oldWorld = Player.World;

            // remove player from the old world
            if( oldWorld != null && oldWorld != newWorld ) {
                if( !oldWorld.ReleasePlayer( Player ) ) {
                    Logger.Log( "Session.JoinWorldNow: Player asked to be released from its world, " +
                                "but the world did not contain the player.", LogType.Error );
                }
                Player[] oldWorldPlayerList = oldWorld.PlayerList;
                foreach( Player otherPlayer in oldWorldPlayerList ) {
                    SendNow( PacketWriter.MakeRemoveEntity( otherPlayer.ID ) );
                }
            }

            ClearBlockUpdateQueue();

            // try to join the new world
            if( oldWorld != newWorld ) {
                if( !newWorld.AcceptPlayer( Player, !firstTime ) )
                    return false;
            }
            Player.World = newWorld;

            // Set spawn point
            Position spawn;
            if( postJoinPosition != null ) {
                spawn = (Position)postJoinPosition;
                postJoinPosition = null;
            } else {
                spawn = newWorld.Map.Spawn;
            }
            Player.Position = spawn;

            // Start sending over the level copy
            if( !firstTime ) {
                writer.Write( PacketWriter.MakeHandshake( Player,
                                                          ConfigKey.ServerName.GetString(),
                                                          "Loading world " + newWorld.GetClassyName() ) );
            }

            writer.WriteLevelBegin();

            // enable Nagle's algorithm (in case it was turned off by LowLatencyMode)
            // to avoid wasting bandwidth for map transfer
            client.NoDelay = false;

            // Fetch compressed map copy
            byte[] buffer = new byte[1024];
            int bytesSent = 0;
            byte[] blockData;
            using( MemoryStream stream = new MemoryStream() ) {
                newWorld.Map.GetCompressedCopy( stream, true );
                blockData = stream.ToArray();
            }
            Logger.Log( "Session.JoinWorldNow: Sending compressed level copy ({0} bytes) to {1}.", LogType.Debug,
                        blockData.Length, Player.Name );

            // Transfer the map copy
            while( bytesSent < blockData.Length ) {
                int chunkSize = blockData.Length - bytesSent;
                if( chunkSize > 1024 ) {
                    chunkSize = 1024;
                } else {
                    // CRC fix for ManicDigger
                    for( int i = 0; i < buffer.Length; i++ ) {
                        buffer[i] = 0;
                    }
                }
                Array.Copy( blockData, bytesSent, buffer, 0, chunkSize );
                byte progress = (byte)(100 * bytesSent / blockData.Length);

                // write in chunks of 1024 bytes or less
                writer.WriteLevelChunk( buffer, chunkSize, progress );
                bytesSent += chunkSize;
            }

            // Done sending over level copy
            writer.Write( PacketWriter.MakeLevelEnd( newWorld.Map ) );

            // Send spawn point
            writer.WriteAddEntity( 255, Player, newWorld.Map.Spawn );
            writer.WriteTeleport( 255, spawn );

            // Send player list
            newWorld.SendPlayerList( Player );

            Player.Message( "Joined world {0}", newWorld.GetClassyName() );

            // Turn off Nagel's algorithm again for LowLatencyMode
            if( ConfigKey.LowLatencyMode.GetBool() ) {
                client.NoDelay = true;
            }

            Server.FireWorldChangedEvent( Player, oldWorld, newWorld );

            // Done.
            Server.RequestGC();

            return true;
        }


        public IPAddress GetIP() {
            return ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;
        }


        /// <summary>
        /// Send packet to player (synchronous). Sends the packet off immediately.
        /// Should not be used from any thread other than this session's IoThread.
        /// Not thread-safe (for performance reason).
        /// </summary>
        public void SendNow( Packet packet ) {
            writer.Write( packet.Data );
        }


        /// <summary>
        /// Send packet (asynchronous, priority queue).
        /// This is used for most packets (movement, chat, etc).
        /// </summary>
        public void Send( Packet packet ) {
            if( CanQueue ) PriorityOutputQueue.Enqueue( packet );
        }


        /// <summary>
        /// Send packet (asynchronous, delayed queue).
        /// This is currently only used for block updates.
        /// </summary>
        public void SendDelayed( Packet packet ) {
            if( CanQueue ) OutputQueue.Enqueue( packet );
        }


        string ReadString() {
            return Encoding.ASCII.GetString( reader.ReadBytes( 64 ) ).Trim();
        }


        public void ClearBlockUpdateQueue() {
            Packet temp = new Packet();
            while( OutputQueue.Dequeue( ref temp ) ) { }
        }


        public void ClearPriorityOutputQueue() {
            Packet tempPacket = new Packet();
            while( PriorityOutputQueue.Dequeue( ref tempPacket ) ) { }
        }


        bool DetectMovementPacketSpam() {
            if( antiSpeedPacketLog.Count >= AntiSpeedMaxPacketCount ) {
                DateTime oldestTime = antiSpeedPacketLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < AntiSpeedMaxPacketInterval ) {
                    DenyMovement();
                    return true;
                }
            }
            antiSpeedPacketLog.Enqueue( DateTime.UtcNow );
            return false;
        }


        #region Kicking

        /// <summary>
        /// Kick (asynchronous). Immediately blocks all client input, but waits
        /// until client thread sends the kick packet.
        /// </summary>
        public void Kick( string message, LeaveReason leaveReason ) {
            LeaveReason = leaveReason;

            CanReceive = false;
            CanQueue = false;

            // clear all pending output to be written to client (it won't matter after the kick)
            ClearBlockUpdateQueue();
            ClearPriorityOutputQueue();

            // bypassing Send() because canQueue is false
            PriorityOutputQueue.Enqueue( PacketWriter.MakeDisconnect( message ) );
        }


        /// <summary>
        /// Kick (synchronous). Immediately sends the kick packet.
        /// Can only be used from IoThread (this is not thread-safe).
        /// </summary>
        public void KickNow( string message, LeaveReason leaveReason ) {
            LeaveReason = leaveReason;

            CanQueue = false;
            CanReceive = false;
            CanSend = false;
            SendNow( PacketWriter.MakeDisconnect( message ) );
            writer.Flush();
        }


        /// <summary>
        /// Blocks the calling thread until this session disconnects.
        /// </summary>
        public void WaitForDisconnect() {
            if( ioThread != null && ioThread.IsAlive ) {
                try {
                    ioThread.Join();
                } catch( NullReferenceException ) {
                } catch( ThreadStateException ) { }
            }
        }

        #endregion


        public override string ToString() {
            if( Player != null ) {
                return String.Format( "Session({0}@{1})", Player, GetIP() );
            } else {
                return String.Format( "Session({0})", GetIP() );
            }
        }
    }
}


#region EventArgs
namespace fCraft.Events {

    public sealed class SessionConnectingEventArgs : EventArgs {
        public SessionConnectingEventArgs( IPAddress ip ) {
            IP = ip;
        }
        public bool Cancel { get; set; }
        public IPAddress IP { get; private set; }
    }


    public sealed class SessionConnectedEventArgs : EventArgs {
        public SessionConnectedEventArgs( Session session ) {
            Session = session;
        }
        public Session Session { get; private set; }
    }


    public sealed class SessionDisconnectedEventArgs : EventArgs {
        public SessionDisconnectedEventArgs( Session session, LeaveReason leaveReason ) {
            Session = session;
            LeaveReason = leaveReason;
        }
        public Session Session { get; private set; }
        public LeaveReason LeaveReason { get; private set; }
    }

}
#endregion