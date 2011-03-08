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
        public Player player;
        public DateTime loginTime;

        public bool canReceive = true,
                    canSend = true,
                    canQueue = true,
                    hasRegistered;

        readonly object joinWorldLock = new object();
        LeaveReason leaveReason = LeaveReason.Unknown;

        Thread ioThread;

        // networking
        TcpClient client;
        BinaryReader reader;
        PacketWriter writer;
        public ConcurrentQueue<Packet> outputQueue,
                                       priorityOutputQueue;

        // joining worlds
        World forcedWorldToJoin;
        Position? postJoinPosition;

        // movement optimization
        int fullPositionUpdateCounter;
        public const int FullPositionUpdateIntervalDefault = 20;
        public static int fullPositionUpdateInterval = FullPositionUpdateIntervalDefault;
        bool skippedLastMovementPacket;
        const int SkipMovementThreshold = 64,
                  SkipRotationThresholdSquared = 1500;

        // anti-speedhack vars
        int speedHackDetectionCounter;
        const int AntiSpeedMaxJumpDelta = 25, // 16 for normal client, 25 for WoM
                  AntiSpeedMaxDistanceSquared = 1024, // 32 * 32
                  AntiSpeedMaxPacketCount = 200,
                  AntiSpeedMaxPacketInterval = 5;


        const int SocketTimeout = 10000;
        Queue<DateTime> antiSpeedPacketLog = new Queue<DateTime>();
        DateTime antiSpeedLastNotification = DateTime.UtcNow;

        public const string GreetingFileName = "greeting.txt";


        public Session( TcpClient _client ) {
            loginTime = DateTime.Now;

            outputQueue = new ConcurrentQueue<Packet>();
            priorityOutputQueue = new ConcurrentQueue<Packet>();

            client = _client;
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
                RaiseConnectedEvent();
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
                while( canSend ) {
                    Thread.Sleep( 1 );

                    int packetsSent = 0;

                    // detect player disconnect
                    if( pollCounter > pollInterval ) {
                        if( !client.Connected ||
                            (client.Client.Poll( 1000, SelectMode.SelectRead ) && client.Client.Available == 0) ) {
                            if( player != null ) {
                                Logger.Log( "Session.IoLoop: Lost connection to player {0} ({1}).", LogType.Debug, player.name, GetIP() );
                            } else {
                                Logger.Log( "Session.IoLoop: Lost connection to unidentified player at {0}.", LogType.Debug, GetIP() );
                            }
                            leaveReason = LeaveReason.ClientQuit;
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
                    while( canSend && packetsSent < Server.MaxSessionPacketsPerTick ) {
                        if( !priorityOutputQueue.Dequeue( ref packet ) )
                            if( !outputQueue.Dequeue( ref packet ) ) break;

                        if( player.isDeaf && packet.OpCode == OutputCode.Message ) continue;

                        writer.Write( packet.data );
                        packetsSent++;

                        if( packet.OpCode == OutputCode.Disconnect ) {
                            writer.Flush();
                            Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug,
                                        player.name );
                            if( leaveReason == LeaveReason.Unknown ) leaveReason = LeaveReason.Kick;
                            return;
                        }
                    }

                    // check if player needs to change worlds
                    if( canSend ) {
                        lock( joinWorldLock ) {
                            if( forcedWorldToJoin != null ) {
                                while( priorityOutputQueue.Dequeue( ref packet ) ) {
                                    writer.Write( packet.data );
                                    packetsSent++;
                                    if( packet.data[0] == (byte)OutputCode.Disconnect ) {
                                        writer.Flush();
                                        Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug,
                                                    player.name );
                                        if( leaveReason == LeaveReason.Unknown ) leaveReason = LeaveReason.Kick;
                                        return;
                                    }
                                }
                                JoinWorldNow( forcedWorldToJoin, false );
                                forcedWorldToJoin = null;
                            }
                        }
                    }

                    // get input from player
                    while( canReceive && client.GetStream().DataAvailable ) {
                        byte opcode = reader.ReadByte();
                        switch( (InputCode)opcode ) {

                            // Message
                            case InputCode.Message:
                                player.ResetIdleTimer();
                                reader.ReadByte();
                                string message = ReadString();
                                if( Player.CheckForIllegalChars( message ) ) {
                                    Logger.Log( "Player.ParseMessage: {0} attempted to write illegal characters in chat and was kicked.", LogType.SuspiciousActivity,
                                                player.name );
                                    Server.SendToAll( "{0}&W was kicked for attempted hacking (0x0d).", player.GetClassyName() );
                                    KickNow( "Illegal characters in chat.", LeaveReason.InvalidMessageKick );
                                    return;
                                } else {
                                    try {
                                        player.ParseMessage( message, false );
                                    } catch( Exception ex ) {
                                        Logger.LogAndReportCrash( "Error while parsing player's message", "fCraft", ex, false );
                                        player.MessageNow( "&WAn error occured while trying to process your message. " +
                                                           "Error details have been logged. " +
                                                           "It is recommended that you reconnect to the server." );
                                    }
                                }
                                break;

                            // Player movement
                            case InputCode.MoveRotate:
                                reader.ReadByte();
                                Position newPos = new Position {
                                    x = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                                    h = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                                    y = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                                    r = reader.ReadByte(),
                                    l = reader.ReadByte()
                                };

                                Position oldPos = player.pos;

                                // calculate difference between old and new positions
                                Position delta = new Position {
                                    x = (short)(newPos.x - oldPos.x),
                                    y = (short)(newPos.y - oldPos.y),
                                    h = (short)(newPos.h - oldPos.h),
                                    r = (byte)Math.Abs( newPos.r - oldPos.r ),
                                    l = (byte)Math.Abs( newPos.l - oldPos.l )
                                };
                                bool posChanged = (delta.x != 0) || (delta.y != 0) || (delta.h != 0);
                                bool rotChanged = (delta.r != 0) || (delta.l != 0);
                                int distSquared = delta.x * delta.x + delta.y * delta.y + delta.h * delta.h;

                                // skip everything if player hasn't moved
                                if( delta.IsZero() ) continue;

                                // only reset the timer if player rotated
                                // if player is pushed around, or /bring is used, rotation does not change (and timer should not reset)
                                if( rotChanged ) player.ResetIdleTimer();

                                if( player.info.isFrozen ) {
                                    // special handling for frozen players
                                    if( delta.x * delta.x + delta.y * delta.y > AntiSpeedMaxDistanceSquared ||
                                        Math.Abs( delta.h ) > 40 ) {
                                        SendNow( PacketWriter.MakeSelfTeleport( player.pos ) );
                                    }
                                    newPos.x = player.pos.x;
                                    newPos.y = player.pos.y;
                                    newPos.h = player.pos.h;

                                    // recalculate deltas
                                    delta.x = 0;
                                    delta.y = 0;
                                    delta.h = 0;
                                    posChanged = false;
                                    distSquared = delta.x * delta.x + delta.y * delta.y + delta.h * delta.h;

                                } else if( !player.Can( Permission.UseSpeedHack ) ) {
                                    // speedhack detection
                                    if( DetectMovementPacketSpam() ) {
                                        continue;

                                    } else if( (distSquared - delta.h * delta.h > AntiSpeedMaxDistanceSquared || delta.h > AntiSpeedMaxJumpDelta) &&
                                               speedHackDetectionCounter >= 0 ) {

                                        if( speedHackDetectionCounter == 0 ) {
                                            player.lastValidPosition = player.pos;
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
                                    (delta.r * delta.r + delta.l * delta.l) < SkipRotationThresholdSquared &&
                                    !skippedLastMovementPacket ) {

                                    skippedLastMovementPacket = true;
                                    continue;
                                }
                                skippedLastMovementPacket = false;

                                // create the movement packet
                                if( delta.FitsIntoByte() && fullPositionUpdateCounter < fullPositionUpdateInterval ) {
                                    if( posChanged && rotChanged ) {
                                        // incremental position + rotation update
                                        packet = PacketWriter.MakeMoveRotate( player.id, new Position {
                                            x = delta.x,
                                            y = delta.y,
                                            h = delta.h,
                                            r = newPos.r,
                                            l = newPos.l
                                        } );

                                    } else if( posChanged ) {
                                        // incremental position update
                                        packet = PacketWriter.MakeMove( player.id, delta );

                                    } else {
                                        // absolute rotation update
                                        packet = PacketWriter.MakeRotate( player.id, newPos );
                                    }

                                } else {
                                    // full (absolute position + rotation) update
                                    packet = PacketWriter.MakeTeleport( player.id, newPos );
                                }

                                fullPositionUpdateCounter++;
                                if( fullPositionUpdateCounter >= fullPositionUpdateInterval ) {
                                    fullPositionUpdateCounter = 0;
                                }

                                player.pos = newPos;
                                player.world.SendToSeeing( packet, player );
                                break;

                            // Set tile
                            case InputCode.SetTile:
                                if( player.world == null || player.world.map == null ) continue;
                                player.ResetIdleTimer();
                                short x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                short h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                short y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                byte mode = reader.ReadByte();
                                byte type = reader.ReadByte();

                                if( type > 49 ) {
                                    Logger.Log( "{0} was kicked for sending bad SetTile packets.", LogType.SuspiciousActivity,
                                                player.name );
                                    Server.SendToAll( "{0}&W was kicked for attempted hacking (0x05).", player.GetClassyName() );
                                    KickNow( "Hacking detected: illegal SetTile packet.", LeaveReason.InvalidSetTileKick );
                                    return;
                                } else if( !player.world.map.InBounds( x, y, h ) ) {
                                    continue;
                                } else {
                                    if( player.PlaceBlock( x, y, h, mode == 1, (Block)type ) ) return;
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
                leaveReason = LeaveReason.ClientQuit;
                Logger.Log( "Session.IoLoop: {0}", LogType.Debug, ex.Message );

            } catch( SocketException ex ) {
                leaveReason = LeaveReason.ClientQuit;
                Logger.Log( "Session.IoLoop: {0}", LogType.Debug, ex.Message );
#if !DEBUG
            } catch( Exception ex ) {
                leaveReason = LeaveReason.ServerError;
                Logger.LogAndReportCrash( "Error in Session.IoLoop", "fCraft", ex, false );
#endif
            } finally {
                canQueue = false;
                canSend = false;
                Disconnect();
            }
        }


        void DenyMovement() {
            SendNow( PacketWriter.MakeSelfTeleport( player.lastValidPosition ) );
            if( DateTime.UtcNow.Subtract( antiSpeedLastNotification ).Seconds > 1 ) {
                player.Message( "&WYou are not allowed to speedhack." );
                antiSpeedLastNotification = DateTime.UtcNow;
            }
        }


        public void Disconnect() {
            Server.UnregisterSession( this );
            Server.RaiseSessionDisconnectedEvent( this, leaveReason );

            if( player != null ) {
                Server.UnregisterPlayer( player );
                player = null;
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


        const string noSMPMessage = "This server is for Minecraft Classic only.";
        bool LoginSequence() {
            byte opcode = reader.ReadByte();
            if( opcode != (byte)InputCode.Handshake ) {
                if( opcode == 2 ) {
                    // This may be someone connecting with an SMP client
                    int strLen = IPAddress.NetworkToHostOrder( reader.ReadInt16() );

                    if( strLen >= 2 && strLen <= 16 ) {
                        string SMPPlayerName = Encoding.UTF8.GetString( reader.ReadBytes( strLen ) );

                        Logger.Log( "Session.LoginSequence: Player \"{0}\" tried connecting with SMP/Beta client from {1}. " +
                                    "fCraft does not support SMP/Beta.", LogType.Warning,
                                    SMPPlayerName, GetIP() );

                        // send SMP KICK packet
                        writer.Write( (byte)255 );
                        byte[] stringData = Encoding.UTF8.GetBytes( noSMPMessage );
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
            player = new Player( null, playerName, this, Server.MainWorld.map.spawn );
            if( player.info.banned ) {
                player.info.ProcessFailedLogin( player );
                Logger.Log( "Banned player {0} tried to log in.", LogType.SuspiciousActivity,
                            player.name );
                if( Config.GetBool( ConfigKey.ShowBannedConnectionMessages ) ) {
                    Server.SendToAll( "&SBanned player {0}&S tried to log in.", player.GetClassyName() );
                }
                string bannedMessage = String.Format( "Banned {0} ago by {1}: {2}",
                                                      DateTime.Now.Subtract( player.info.banDate ).ToMiniString(),
                                                      player.info.bannedBy,
                                                      player.info.banReason );
                KickNow( bannedMessage, LeaveReason.LoginFailed );
                return false;
            }

            bool showVerifyNamesWarning = false;

            // verify name
            if( !Server.VerifyName( player.name, verificationCode, Server.Salt ) ) {
                NameVerificationMode nameVerificationMode = ConfigKey.VerifyNames.GetEnum<NameVerificationMode>();

                string standardMessage = String.Format( "Session.LoginSequence: Could not verify player name for {0} ({1}).",
                                                        player.name, GetIP() );
                if( GetIP().ToString() == "127.0.0.1" && nameVerificationMode == NameVerificationMode.Always ) {
                    Logger.Log( "{0} Player was identified as connecting from localhost and allowed in.", LogType.SuspiciousActivity,
                                standardMessage );

                } else if( GetIP().IsLAN() && Config.GetBool( ConfigKey.AllowUnverifiedLAN ) ) {
                    Logger.Log( "{0} Player was identified as connecting from LAN and allowed in.", LogType.SuspiciousActivity,
                                standardMessage );

                } else if( player.info.timesVisited == 1 || player.info.lastIP.ToString() != GetIP().ToString() ) {
                    switch( nameVerificationMode ) {
                        case NameVerificationMode.Always:
                        case NameVerificationMode.Balanced:
                            player.info.ProcessFailedLogin( player );
                            Logger.Log( "{0} IP did not match. Player was kicked.", LogType.SuspiciousActivity,
                                        standardMessage );
                            KickNow( "Could not verify player name!", LeaveReason.UnverifiedName );
                            return false;
                        case NameVerificationMode.Never:
                            Logger.Log( "{0} IP did not match. " +
                                        "Player was allowed in anyway because VerifyNames is set to Never.", LogType.SuspiciousActivity,
                                        standardMessage );
                            player.Message( "&WYour name could not be verified." );
                            showVerifyNamesWarning = true;
                            break;
                    }

                } else {
                    switch( nameVerificationMode ) {
                        case NameVerificationMode.Always:
                            player.info.ProcessFailedLogin( player );
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
            IPBanInfo IPBanInfo = IPBanList.Get( GetIP() );
            if( IPBanInfo != null ) {
                player.info.ProcessFailedLogin( player );
                IPBanInfo.ProcessAttempt( player );
                if( Config.GetBool( ConfigKey.ShowBannedConnectionMessages ) ) {
                    Server.SendToAll( "{0}&S tried to log in from a banned IP.", player.GetClassyName() );
                }
                Logger.Log( "{0} tried to log in from a banned IP.", LogType.SuspiciousActivity,
                            player.name );
                string bannedMessage = String.Format( "IP-banned {0} ago by {1}: {2}",
                                                      DateTime.Now.Subtract( IPBanInfo.banDate ).ToMiniString(),
                                                      IPBanInfo.bannedBy,
                                                      IPBanInfo.banReason );
                KickNow( bannedMessage, LeaveReason.LoginFailed );
                return false;
            }

            if( Config.GetBool( ConfigKey.PaidPlayersOnly ) ) {
                // write a "please wait" message while we validate player's paid status
                writer.Write( PacketWriter.MakeHandshake( player,
                                                          Config.GetString( ConfigKey.ServerName ),
                                                          "Please wait; Validating paid status..." ) );
                writer.Flush();

                if( !Player.CheckPaidStatus( player.name ) ) {
                    KickNow( "Paid players allowed only.", LeaveReason.LoginFailed );
                    return false;
                }
            }

            // Any additional security checks should be done right here
            if( Server.RaisePlayerConnectingEvent( player ) ) return false;


            // ----==== beyond this point, player is considered connecting (allowed to join) ====----


            // check if another player with the same name is on
            Server.KickGhostsAndRegisterSession( this );

            if( Config.GetBool( ConfigKey.LimitOneConnectionPerIP ) ) {
                // note: FindPlayers only counts REGISTERED players
                Player[] potentialClones = Server.FindPlayers( GetIP() );
                if( potentialClones.Length > 0 ) {
                    player.info.ProcessFailedLogin( player );
                    Logger.Log( "Session.LoginSequence: Player {0} tried to log in from same IP ({1}) as {2}.", LogType.SuspiciousActivity,
                                player.name, GetIP(), potentialClones[0].name );
                    foreach( Player clone in potentialClones ) {
                        clone.Message( "Warning: someone just attempted to log in using your IP." );
                    }
                    KickNow( "Only one connection per IP allowed!", LeaveReason.LoginFailed );
                    return false;
                }
            }

            // Register player for future block updates
            if( !Server.RegisterPlayer( player ) ) {
                KickNow( "Sorry, server is full (" + Server.PlayerList.Length + "/" + Config.GetInt( ConfigKey.MaxPlayers ) + ")", LeaveReason.ServerFull );
                return false;
            }
            player.info.ProcessLogin( player );


            // ----==== Beyond this point, player is considered connected (authenticated and registered) ====----


            World startingWorld = Server.RaisePlayerConnectedEvent( player, Server.MainWorld );

            // Send server information
            writer.Write( PacketWriter.MakeHandshake( player, Config.GetString( ConfigKey.ServerName ), Config.GetString( ConfigKey.MOTD ) ) );

            // AutoRank
            if( Config.GetBool( ConfigKey.AutoRankEnabled ) ) {
                Rank newRank = AutoRank.Check( player.info );
                if( newRank != null ) {
                    AdminCommands.DoChangeRank( Player.Console, player.info, player, newRank, "~AutoRank", false, true );
                }
            }

            bool firstTime = (player.info.timesVisited == 1);
            if( !JoinWorldNow( startingWorld, true ) ) {
                Logger.Log( "Failed to load main world ({0}) for connecting player {1} (from {2})", LogType.Error,
                            startingWorld.name, player.name, GetIP() );
                return false;
            }


            // ==== Beyond this point, player is considered ready (has a world) ====


            if( showVerifyNamesWarning ) {
                Server.SendToAllExcept( "&WName and IP of {0}&W are unverified!", player,
                                        player.GetClassyName() );
            }

            // Check if other banned players logged in from this IP
            PlayerInfo[] bannedPlayerNames = PlayerDB.FindPlayers( GetIP(), 25 ).Where( playerFromSameIP => playerFromSameIP.banned ).ToArray();
            if( bannedPlayerNames.Length > 0 ) {
                string logString = String.Format( "&WPlayer {0}&W logged in from an IP previously used by banned players: {1}",
                                                  player.GetClassyName(),
                                                  PlayerInfo.PlayerInfoArrayToString( bannedPlayerNames ) );
                Server.SendToAll( logString );
                Logger.Log( logString, LogType.SuspiciousActivity );
            }

            // Announce join
            if( Config.GetBool( ConfigKey.ShowConnectionMessages ) ) {
                Server.SendToAllExcept( Server.MakePlayerConnectedMessage( player, firstTime, player.world ), player );
            }

            // check if player is still muted
            if( player.info.mutedUntil > DateTime.UtcNow ) {
                int secondsLeft = (int)player.info.mutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds;
                player.Message( "&WYou were previously muted by {0}, {1} seconds left.",
                                player.info.mutedBy, secondsLeft );
                Server.SendToAllExcept( "&WPlayer {0}&W was previously muted by {1}&W, {2} seconds left.", player,
                                        player.GetClassyName(), player.info.mutedBy, secondsLeft );
            }

            // check if player is still frozen
            if( player.info.isFrozen ) {
                if( player.info.frozenOn != DateTime.MinValue ) {
                    player.Message( "&WYou were previously frozen {0} ago by {1}",
                                    DateTime.Now.Subtract( player.info.frozenOn ).ToMiniString(),
                                    player.info.frozenBy );
                    Server.SendToAllExcept( "&WPlayer {0}&W was previously frozen {1} ago by {2}.", player,
                                            player.GetClassyName(),
                                            DateTime.Now.Subtract( player.info.frozenOn ).ToMiniString(),
                                            player.info.frozenBy );
                } else {
                    player.Message( "&WYou were previously frozen by {0}",
                                    player.info.frozenBy );
                    Server.SendToAllExcept( "&WPlayer {0}&W was previously frozen by {1}.", player,
                                            player.GetClassyName(),
                                            player.info.frozenBy );
                }
            }

            // Welcome message
            if( File.Exists( GreetingFileName ) ) {
                string[] greetingText = File.ReadAllLines( GreetingFileName );
                foreach( string greetingLine in greetingText ) {
                    player.Message( greetingLine
                                    .Replace( "{SERVER_NAME}", Config.GetString( ConfigKey.ServerName ) )
                                    .Replace( "{RANK}", player.info.rank.GetClassyName() ) );
                }
            } else {
                if( firstTime ) {
                    player.Message( "Welcome to {0}", Config.GetString( ConfigKey.ServerName ) );
                } else {
                    player.Message( "Welcome back to {0}", Config.GetString( ConfigKey.ServerName ) );
                }

                player.Message( "Your rank is {0}&S. Type &H/help&S for help.",
                                player.info.rank.GetClassyName() );
            }

            // A reminder for first-time users
            if( PlayerDB.CountTotalPlayers() == 1 && player.info.rank != RankList.HighestRank ) {
                player.Message( "Type &H/rank {0} {1}&S in console to promote yourself",
                                player.name, RankList.HighestRank.Name );
            }

            Server.RaisePlayerReadyEvent( player );

            return true;
        }


        public void JoinWorld( World newWorld, Position? position ) {
            lock( joinWorldLock ) {
                postJoinPosition = position;
                forcedWorldToJoin = newWorld;
            }
        }


        internal bool JoinWorldNow( World newWorld, bool firstTime ) {
            if( newWorld == null ) {
                Logger.Log( "Session.JoinWorldNow: Requested to join a non-existing (null) world.", LogType.Error );
                return false;
            }

            if( !player.CanJoin( newWorld ) ) {
                Logger.Log( "Session.JoinWorldNow: Access limits prevented {0} from joining {1}.", LogType.Error,
                            player.name, newWorld.name );
                return false;
            }

            if( !newWorld.FirePlayerTriedToJoinEvent( player ) ) {
                Logger.Log( "Session.JoinWorldNow: FirePlayerTriedToJoinEvent prevented {0} from joining {1}", LogType.Warning,
                            player.name, newWorld.name );
                return false;
            }

            World oldWorld = player.world;

            // remove player from the old world
            if( oldWorld != null && oldWorld != newWorld ) {
                if( !oldWorld.ReleasePlayer( player ) ) {
                    Logger.Log( "Session.JoinWorldNow: Player asked to be released from its world, " +
                                "but the world did not contain the player.", LogType.Error );
                }
                Player[] oldWorldPlayerList = oldWorld.playerList;
                foreach( Player otherPlayer in oldWorldPlayerList ) {
                    SendNow( PacketWriter.MakeRemoveEntity( otherPlayer.id ) );
                }
            }

            ClearBlockUpdateQueue();

            // try to join the new world
            if( oldWorld != newWorld ) {
                if( !newWorld.AcceptPlayer( player, !firstTime ) )
                    return false;
            }
            player.world = newWorld;

            // Set spawn point
            Position spawn;
            if( postJoinPosition != null ) {
                spawn = (Position)postJoinPosition;
                postJoinPosition = null;
            } else {
                spawn = newWorld.map.spawn;
            }
            player.pos = spawn;

            // Start sending over the level copy
            if( !firstTime ) {
                writer.Write( PacketWriter.MakeHandshake( player,
                                                          Config.GetString( ConfigKey.ServerName ),
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
                newWorld.map.GetCompressedCopy( stream, true );
                blockData = stream.ToArray();
            }
            Logger.Log( "Session.JoinWorldNow: Sending compressed level copy ({0} bytes) to {1}.", LogType.Debug,
                        blockData.Length, player.name );

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
            writer.Write( PacketWriter.MakeLevelEnd( newWorld.map ) );

            // Send spawn point
            writer.WriteAddEntity( 255, player, newWorld.map.spawn );
            writer.WriteTeleport( 255, spawn );

            // Send player list
            newWorld.SendPlayerList( player );

            player.Message( "Joined world {0}", newWorld.GetClassyName() );

            // Turn off Nagel's algorithm again for LowLatencyMode
            if( Config.GetBool( ConfigKey.LowLatencyMode ) ) {
                client.NoDelay = true;
            }

            Server.FireWorldChangedEvent( player, oldWorld, newWorld );

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
            writer.Write( packet.data );
        }


        /// <summary>
        /// Send packet (asynchronous, priority queue).
        /// This is used for most packets (movement, chat, etc).
        /// </summary>
        public void Send( Packet packet ) {
            if( canQueue ) priorityOutputQueue.Enqueue( packet );
        }


        /// <summary>
        /// Send packet (asynchronous, delayed queue).
        /// This is currently only used for block updates.
        /// </summary>
        public void SendDelayed( Packet packet ) {
            if( canQueue ) outputQueue.Enqueue( packet );
        }


        string ReadString() {
            return Encoding.ASCII.GetString( reader.ReadBytes( 64 ) ).Trim();
        }


        public void ClearBlockUpdateQueue() {
            Packet temp = new Packet();
            while( outputQueue.Dequeue( ref temp ) ) { }
        }


        public void ClearPriorityOutputQueue() {
            Packet tempPacket = new Packet();
            while( priorityOutputQueue.Dequeue( ref tempPacket ) ) ;
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
        public void Kick( string message, LeaveReason _leaveReason ) {
            leaveReason = _leaveReason;

            canReceive = false;
            canQueue = false;

            // clear all pending output to be written to client (it won't matter after the kick)
            ClearBlockUpdateQueue();
            ClearPriorityOutputQueue();

            // bypassing Send() because canQueue is false
            priorityOutputQueue.Enqueue( PacketWriter.MakeDisconnect( message ) );
        }


        /// <summary>
        /// Kick (synchronous). Immediately sends the kick packet.
        /// Can only be used from IoThread (this is not thread-safe).
        /// </summary>
        public void KickNow( string message, LeaveReason _leaveReason ) {
            leaveReason = _leaveReason;

            canQueue = false;
            canReceive = false;
            canSend = false;
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


        #region Events

        public event EventHandler<SessionConnectedEventArgs> Connected;

        public event EventHandler<SessionDisconnectedEventArgs> Disconnected;

        void RaiseConnectedEvent() {
            var h = Connected;
            if( h != null ) h( this, new SessionConnectedEventArgs( this ) );
        }

        void RaiseDisconnectedEvent() {
            var h = Disconnected;
            if( h != null ) h( this, new SessionDisconnectedEventArgs( this, leaveReason ) );
        }

        #endregion

        public override string ToString() {
            if( player != null ) {
                return String.Format( "Session({0}@{1})", player, GetIP() );
            } else {
                return String.Format( "Session({0})", GetIP() );
            }
        }
    }


    #region EventHandlers

    public class SessionConnectingEventArgs : EventArgs {
        public SessionConnectingEventArgs( IPAddress _IP ) {
            IP = _IP;
        }
        public bool Cancel { get; set; }
        public IPAddress IP { get; private set; }
    }

    public class SessionConnectedEventArgs : EventArgs {
        public SessionConnectedEventArgs( Session _session ) {
            Session = _session;
        }
        public Session Session { get; private set; }
    }

    public class SessionDisconnectedEventArgs : EventArgs {
        public SessionDisconnectedEventArgs( Session _session, LeaveReason _leaveReason ) {
            Session = _session;
            LeaveReason = _leaveReason;
        }
        public Session Session { get; private set; }
        public LeaveReason LeaveReason { get; private set; }
    }

    #endregion
}