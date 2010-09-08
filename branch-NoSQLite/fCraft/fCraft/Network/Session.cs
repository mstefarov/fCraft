// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;


namespace fCraft {
    public sealed class Session {
        public Player player;
        public DateTime loginTime;
        public bool canReceive,
                    canSend,
                    canQueue,
                    canDispose;
        public bool isBetweenWorlds = true;
        object joinWorldLock = new object();

        Thread ioThread;
        TcpClient client;
        BinaryReader reader;
        PacketWriter writer;
        public ConcurrentQueue<Packet> outputQueue, priorityOutputQueue;
        World forcedWorldToJoin;
        Position? postJoinPosition;

        int fullPositionUpdateCounter;
        const int fullPositionUpdateInterval = 20;
        internal bool hasRegistered = false;

        // anti-speedhack vars
        int speedHackDetectionCounter;
        const int antiSpeedMaxJumpDelta = 25; // 16 for normal client, 25 for WoM
        const int antiSpeedMaxDistanceSquared = 144; // 12 * 12
        const int antiSpeedMaxPacketCount = 150;
        const int antiSpeedMaxPacketInterval = 5;
        const int socketTimeout = 12000;
        Queue<DateTime> antiSpeedPacketLog = new Queue<DateTime>();
        DateTime antiSpeedLastNotification = DateTime.UtcNow;
        bool skippedLastMovementPacket = false;

        public int PacketsReceived, PacketsSent, PacketsSkippedZero, PacketsSkippedOptimized;

        public Session( TcpClient _client ) {
            loginTime = DateTime.Now;

            canReceive = true;
            canQueue = true;
            //canSend = false;
            //canDispose = false;

            outputQueue = new ConcurrentQueue<Packet>();
            priorityOutputQueue = new ConcurrentQueue<Packet>();

            client = _client;
            client.SendTimeout = socketTimeout;
            client.ReceiveTimeout = socketTimeout;

            reader = new BinaryReader( client.GetStream() );
            writer = new PacketWriter( client.GetStream() );

            Logger.Log( "Server.CheckConnections: Incoming connection from " + GetIP().ToString(), LogType.Debug );

            ioThread = new Thread( IoLoop );
            ioThread.IsBackground = true;
            ioThread.Start();
        }


        void IoLoop() {
            short x, y, h;
            byte mode, type, opcode;
            Packet packet = new Packet();

            int pollInterval = 200;
            int pollCounter = 0;
            int packetsSent = 0;

            try {
                LoginSequence();
                if( player == null ) return;

                canSend = true;

                while( !canDispose ) {
                    Thread.Sleep( 1 );

                    lock( joinWorldLock ) {
                        if( forcedWorldToJoin != null ) {
                            JoinWorldNow( forcedWorldToJoin, false );
                            forcedWorldToJoin = null;
                        }
                    }

                    packetsSent = 0;

                    // detect player disconnect
                    if( pollCounter > pollInterval ) {
                        if( !client.Connected ||
                            (client.Client.Poll( 1000, SelectMode.SelectRead ) && client.Client.Available == 0) ) {
                            if( player != null ) {
                                Logger.Log( "Session.IoLoop: Lost connection to player {0} ({1}).", LogType.Debug, player.name, GetIP() );
                            } else {
                                Logger.Log( "Session.IoLoop: Lost connection to unidentified player at {0}.", LogType.Debug, GetIP() );
                            }
                            return;
                        }
                        pollCounter = 0;
                    }
                    pollCounter++;

                    // send priority output to player
                    while( canSend && packetsSent < Server.MaxSessionPacketsPerTick ) {
                        if( !priorityOutputQueue.Dequeue( ref packet ) ) break;
                        writer.Write( packet.data );
                        packetsSent++;
                        if( packet.data[0] == (byte)OutputCode.Disconnect ) {
                            Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug,
                                        player.name );
                            return;
                        }
                    }

                    // send output to player
                    while( canSend && packetsSent < Server.MaxSessionPacketsPerTick ) {
                        if( !outputQueue.Dequeue( ref packet ) ) break;
                        writer.Write( packet.data );
                        packetsSent++;
                        if( packet.data[0] == (byte)OutputCode.Disconnect ) {
                            writer.Flush();
                            Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug,
                                        player.name );
                            return;
                        }
                    }

                    // get input from player
                    while( canReceive && client.GetStream().DataAvailable ) {
                        opcode = reader.ReadByte();
                        switch( (InputCode)opcode ) {

                            // Message
                            case InputCode.Message:
                                player.ResetIdleTimer();
                                reader.ReadByte();
                                string message = ReadString();
                                if( Player.CheckForIllegalChars( message ) ) {
                                    Logger.Log( "Player.ParseMessage: {0} attempted to write illegal characters in chat and was kicked.", LogType.SuspiciousActivity,
                                                player.name );
                                    Server.SendToAll( player.GetClassyName() + Color.Red + " was kicked for attempted hacking (0x0d)." );
                                    KickNow( "Illegal characters in chat." );
                                    return;
                                } else {
                                    player.ParseMessage( message, false );
                                }
                                break;

                            // Player movement
                            case InputCode.MoveRotate:
                                PacketsReceived++;
                                reader.ReadByte();
                                Position newPos = new Position();
                                newPos.x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                newPos.h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                newPos.y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                newPos.r = reader.ReadByte();
                                newPos.l = reader.ReadByte();

                                /*if( newPos.h < 0 ) {
                                    Logger.Log( player.GetLogName() + " was kicked for moving out of map boundaries.", LogType.SuspiciousActivity );
                                    KickNow( "Hacking detected: out of map boundaries." );
                                    Server.SendToAll( Color.Red + player.GetLogName() + " was kicked for leaving the map." );
                                    return;
                                }*/

                                Position delta = new Position();
                                Position oldPos = player.pos;
                                bool posChanged, rotChanged;

                                delta.Set( newPos.x - oldPos.x, newPos.y - oldPos.y, newPos.h - oldPos.h, newPos.r, newPos.l );
                                posChanged = delta.x != 0 || delta.y != 0 || delta.h != 0;
                                rotChanged = newPos.r != oldPos.r || newPos.l != oldPos.l;

                                int distSquared = delta.x * delta.x + delta.y * delta.y + delta.h * delta.h;

                                // only reset the timer if player rotated
                                // if player is pushed around, or /bring is used, rotation does not change (and timer should not reset)
                                if( rotChanged ) player.ResetIdleTimer();

                                if( player.isFrozen ) {

                                    if( rotChanged ) {
                                        player.world.SendToAll( PacketWriter.MakeRotate( player.id, newPos ), player );
                                        player.pos.r = newPos.r;
                                        player.pos.l = newPos.l;
                                    }
                                    if( distSquared > antiSpeedMaxDistanceSquared ) {
                                        SendNow( PacketWriter.MakeTeleport( 255, player.pos ) );
                                    }

                                } else {
                                    // speedhack detection
                                    if( !player.Can( Permission.UseSpeedHack ) ) {
                                        if( DetectMovementPacketSpam() ) return;
                                        if( (distSquared - delta.h * delta.h > antiSpeedMaxDistanceSquared || delta.h > antiSpeedMaxJumpDelta) && speedHackDetectionCounter >= 0 ) {
                                            if( speedHackDetectionCounter == 0 ) {
                                                player.lastNonHackingPosition = player.pos;
                                            } else if( speedHackDetectionCounter > 1 ) {
                                                Position avgPosition = new Position();
                                                avgPosition.Set( (player.lastNonHackingPosition.x * 3 + newPos.x) / 4 + 1,
                                                                 (player.lastNonHackingPosition.y * 3 + newPos.y) / 4 + 1,
                                                                 (player.lastNonHackingPosition.h * 3 + newPos.h) / 4 + 1,
                                                                 player.lastNonHackingPosition.r,
                                                                 player.lastNonHackingPosition.l );

                                                SendNow( PacketWriter.MakeTeleport( 255, avgPosition ) );
                                                if( DateTime.UtcNow.Subtract( antiSpeedLastNotification ).Seconds > 1 ) {
                                                    player.Message( Color.Red + "You are not allowed to speedhack." );
                                                    antiSpeedLastNotification = DateTime.UtcNow;
                                                }
                                                speedHackDetectionCounter = 0;
                                                continue;
                                            }
                                            speedHackDetectionCounter++;
                                        } else {
                                            speedHackDetectionCounter = 0;
                                        }
                                    }

                                    if( !player.isHidden ) {
                                        if( distSquared < 64 && delta.r * delta.r + delta.l + delta.l < 4096 && !skippedLastMovementPacket ) {
                                            skippedLastMovementPacket = true;
                                            PacketsSkippedOptimized++;
                                            continue;
                                        }
                                        skippedLastMovementPacket = false;
                                        if( delta.FitsIntoByte() && fullPositionUpdateCounter < fullPositionUpdateInterval ) {
                                            if( posChanged && rotChanged ) {
                                                player.world.SendToAll( PacketWriter.MakeMoveRotate( player.id, delta ), player );
                                                PacketsSent++;
                                            } else if( posChanged ) {
                                                player.world.SendToAll( PacketWriter.MakeMove( player.id, delta ), player );
                                                PacketsSent++;
                                            } else if( rotChanged ) {
                                                player.world.SendToAll( PacketWriter.MakeRotate( player.id, newPos ), player );
                                                PacketsSent++;
                                            } else {
                                                PacketsSkippedZero++;
                                            }
                                        } else if( !delta.IsZero() && !player.isFrozen ) {
                                            player.world.SendToAll( PacketWriter.MakeTeleport( player.id, newPos ), player );
                                            PacketsSent++;
                                        } else if( delta.IsZero() ) {
                                            PacketsSkippedZero++;
                                        }
                                    }
                                    player.pos = newPos;
                                }

                                fullPositionUpdateCounter++;
                                if( fullPositionUpdateCounter >= fullPositionUpdateInterval ) fullPositionUpdateCounter = 0;
                                break;

                            // Set tile
                            case InputCode.SetTile:
                                player.ResetIdleTimer();
                                x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                mode = reader.ReadByte();
                                type = reader.ReadByte();

                                // ignore settile packets while player is changing world
                                if( isBetweenWorlds ) continue;

                                if( type > 49 || x < 0 || x > player.world.map.widthX || y < 0 || y > player.world.map.widthY || h < 0 || h > player.world.map.height ) {
                                    Logger.Log( "{0} was kicked for sending bad SetTile packets.", LogType.SuspiciousActivity,
                                                player.name );
                                    Server.SendToAll( player.GetClassyName() + Color.Red + " was kicked for attempted hacking (0x05)." );
                                    KickNow( "Hacking detected: illegal SetTile packet." );
                                    return;
                                } else {
                                    if( player.PlaceBlock( x, y, h, mode == 1, (Block)type ) ) return;
                                }
                                break;
                        }
                    }
                }

            } catch( ThreadAbortException ex ) {
                Logger.Log( "Session.IoLoop: Thread aborted: {0}", LogType.Error, ex );

            } catch( IOException ex ) {
                Logger.Log( "Session.IoLoop: {0}", LogType.Debug, ex.Message );

            } catch( SocketException ex ) {
                Logger.Log( "Session.IoLoop: {0}", LogType.Debug, ex.Message );
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.Log( "Session.IoLoop: {0}", LogType.Error, ex );
                Logger.UploadCrashReport( "Unhandled exception in Session.IoLoop", "fCraft", ex );
#endif
            } finally {
                canQueue = false;
                canSend = false;
                canDispose = true;
            }
        }


        void LoginSequence() {
            byte opcode = reader.ReadByte();
            if( opcode != (byte)InputCode.Handshake ) {
                if( opcode == 2 ) {
                    Logger.Log( "Session.LoginSequence: Someone tried connecting with SMP/Alpha client from {0}", LogType.Warning,
                                GetIP() );
                    KickNow( "This server is for Minecraft Classic only." );
                    return;
                } else {
                    Logger.Log( "Session.LoginSequence: Unexpected opcode in the first packet from {0}: {1}.", LogType.Error,
                                GetIP(), opcode );
                    KickNow( "Unexpected handshake message - possible protocol mismatch!" );
                    return;
                }
            }

            // check protocol version
            int clientProtocolVersion = reader.ReadByte();
            if( clientProtocolVersion != Config.ProtocolVersion ) {
                Logger.Log( "Session.LoginSequence: Wrong protocol version: {0}.", LogType.Error,
                            clientProtocolVersion );
                KickNow( "Incompatible protocol version!" );
                return;
            }

            // check name for nonstandard characters
            string playerName = ReadString();
            string verificationCode = ReadString();
            reader.ReadByte(); // unused    

            if( !Player.IsValidName( playerName ) ) {
                Logger.Log( "Session.LoginSequence: Unacceptible player name: {0} ({1})", LogType.SuspiciousActivity,
                            playerName,
                            GetIP() );
                KickNow( "Invalid characters in player name!" );
                return;
            }

            // check if player is banned
            player = new Player( null, playerName, this, Server.mainWorld.map.spawn );
            if( player.info.banned ) {
                player.info.ProcessFailedLogin( player );
                Logger.Log( "Banned player {0} tried to log in.", LogType.SuspiciousActivity,
                            player.name );
                Server.SendToAll( "&SBanned player " + player.GetClassyName() + "&S tried to log in." );
                KickNow( "You were banned by " + player.info.bannedBy + " " + DateTime.Now.Subtract( player.info.banDate ).Days + " days ago." );
                return;
            }

            // check if player's IP is banned
            IPBanInfo IPBanInfo = IPBanList.Get( GetIP() );
            if( IPBanInfo != null ) {
                player.info.ProcessFailedLogin( player );
                IPBanInfo.ProcessAttempt( player );
                Logger.Log( "{0} tried to log in from a banned IP.", LogType.SuspiciousActivity,
                            player.name );
                Server.SendToAll( player.GetClassyName() + "&S tried to log in from a banned IP." );
                KickNow( "Your IP was banned by " + IPBanInfo.bannedBy + " " + DateTime.Now.Subtract( IPBanInfo.banDate ).Days + " days ago." );
                return;
            }

            // check if other banned players logged in from this IP
            List<string> bannedPlayerNames = new List<string>();
            foreach( PlayerInfo playerFromSameIP in PlayerDB.FindPlayersByIP( GetIP() ) ) {
                if( playerFromSameIP.banned ) {
                    bannedPlayerNames.Add( playerFromSameIP.name );
                }
            }
            if( bannedPlayerNames.Count > 0 ) {
                string logString = String.Format( Color.Red + "Player {0} logged in from an IP previously used by banned players: {1}",
                                                  player.GetClassyName(),
                                                  String.Join( ", ", bannedPlayerNames.ToArray() ) );
                Server.SendToAll( logString );
                Logger.Log( logString, LogType.SuspiciousActivity );
            }

            // verify name
            if( !Server.VerifyName( player.name, verificationCode ) ) {
                string standardMessage = String.Format( "Session.LoginSequence: Could not verify player name for {0} ({1}).",
                                                        player.name,
                                                        GetIP() );
                if( GetIP().ToString() == "127.0.0.1" &&
                    (Config.GetString( ConfigKey.VerifyNames ) == "Balanced" || Config.GetString( ConfigKey.VerifyNames ) == "Never") ) {
                    Logger.Log( "{0} Player was identified as connecting from localhost and allowed in.", LogType.SuspiciousActivity,
                                standardMessage );
                } else if( player.info.timesVisited == 1 || player.info.lastIP.ToString() != GetIP().ToString() ) {
                    switch( Config.GetString( ConfigKey.VerifyNames ) ) {
                        case "Always":
                        case "Balanced":
                            player.info.ProcessFailedLogin( player );
                            Logger.Log( "{0} IP did not match. Player was kicked.", LogType.SuspiciousActivity,
                                        standardMessage );
                            KickNow( "Could not verify player name!" );
                            return;
                        case "Never":
                            Logger.Log( "{0} IP did not match. Player was allowed in anyway because VerifyNames is set to Never.", LogType.SuspiciousActivity,
                                        standardMessage );
                            player.Message( Color.Red + "Your name could not be verified." );
                            Server.SendToAll( Color.Red + "Name and IP of " + player.GetClassyName() + Color.Red + " are unverified!", player );
                            break;
                    }
                } else {
                    switch( Config.GetString( ConfigKey.VerifyNames ) ) {
                        case "Always":
                            player.info.ProcessFailedLogin( player );
                            Logger.Log( "{0} IP matched previous records for that name. Player was kicked anyway because VerifyNames is set to Always.", LogType.SuspiciousActivity,
                                                standardMessage );
                            KickNow( "Could not verify player name!" );
                            return;
                        case "Balanced":
                        case "Never":
                            Logger.Log( "{0} IP matched previous records for that name. Player was allowed in.", LogType.SuspiciousActivity,
                                        standardMessage );
                            break;
                    }
                }
            }

            // check if another player with the same name is on
            Player potentialClone = Server.FindPlayerExact( player.name );
            if( potentialClone != null ) {
                player.info.ProcessFailedLogin( player );
                Logger.Log( "Session.LoginSequence: Player {0} tried to log in from two computers at once.", LogType.SuspiciousActivity,
                            player.name );
                potentialClone.Message( "Warning: someone just attempted to log in using your name." );
                KickNow( "Already connected from elsewhere!" );
                return;
            }

            if( Config.GetBool( ConfigKey.LimitOneConnectionPerIP ) ) {
                List<Player> potentialClones = Server.FindPlayers( GetIP() );
                if( potentialClones.Count > 0 ) {
                    player.info.ProcessFailedLogin( player );
                    Logger.Log( "Session.LoginSequence: Player {0} tried to log in from same IP ({1}) as {2}.", LogType.SuspiciousActivity,
                                player.name, GetIP(), potentialClones[0].name );
                    foreach( Player clone in potentialClones ) {
                        clone.Message( "Warning: someone just attempted to log in using your IP." );
                    }
                    KickNow( "Only one connection per IP allowed!" );
                    return;
                }
            }

            // Register player for future block updates
            if( !Server.RegisterPlayer( player ) ) {
                KickNow( "Sorry, server is full (" + Server.playerList.Length + "/" + Config.GetInt( ConfigKey.MaxPlayers ) + ")" );
                return;
            }
            hasRegistered = true;

            player.info.ProcessLogin( player );
            Server.FirePlayerConnectedEvent( this );
            Server.FirePlayerListChangedEvent();

            // Player is now authenticated. Send server info.
            writer.Write( PacketWriter.MakeHandshake( player, Config.GetString( ConfigKey.ServerName ), Config.GetString( ConfigKey.MOTD ) ) );

            bool firstTime = (player.info.timesVisited == 1);
            Server.ShowPlayerConnectedMessage( player, firstTime, Server.mainWorld );
            JoinWorldNow( Server.mainWorld, true );

            // Welcome message
            if( firstTime ) {
                player.Message( "Welcome back to {0}", Config.GetString( ConfigKey.ServerName ) );
            } else {
                player.Message( "Welcome to {0}", Config.GetString( ConfigKey.ServerName ) );
            }

            player.Message( String.Format( "Your player class is {0}&S. Type &H/help&S for help.",
                                           player.info.playerClass.GetClassyName() ) );
        }


        internal void ClearBlockUpdateQueue() {
            Packet temp = new Packet();
            while( outputQueue.Dequeue( ref temp ) ) { }
        }

        public void JoinWorld( World newWorld, Position? position ) {
            lock( joinWorldLock ) {
                forcedWorldToJoin = newWorld;
                postJoinPosition = position;
            }
        }

        internal bool JoinWorldNow( World newWorld, bool firstTime ) {
            if( newWorld == null ) {
                Logger.Log( "Session.JoinWorld: Requested to join a non-existing (null) world.", LogType.Error );
                return false;
            }

            if( newWorld.classAccess.rank > player.info.playerClass.rank ) {
                Logger.Log( "Session.JoinWorld: Access limits prevented {0} from joining {1}.", LogType.Error,
                            player.name, newWorld.name );
                return false;
            }

            if( !newWorld.FirePlayerTriedToJoinEvent( player ) ) {
                Logger.LogWarning( "Session.JoinWorld: FirePlayerTriedToJoinEvent prevented {0} from joining {1}", WarningLogSubtype.EventWarning,
                                   player.name, newWorld.name );
                return false;
            }

            // prevents accepting block updates from player while he's switching worlds
            isBetweenWorlds = true;

            World oldWorld = player.world;

            // remove player from the old world
            if( oldWorld != null ) {
                if( !oldWorld.ReleasePlayer( player ) ) {
                    Logger.Log( "Session.JoinWorld: Player asked to be released from its world, but the world did not contain the player.", LogType.Error );
                }
                Player[] oldWorldPlayerList = oldWorld.playerList;
                foreach( Player otherPlayer in oldWorldPlayerList ) {
                    SendNow( PacketWriter.MakeRemoveEntity( otherPlayer.id ) );
                }
            }

            ClearBlockUpdateQueue();

            // try to join the new world
            if( !newWorld.AcceptPlayer( player, !firstTime ) ) return false;
            player.world = newWorld;

            // Start sending over the level copy
            if( !firstTime ) {
                writer.Write( PacketWriter.MakeHandshake( player, Config.GetString( ConfigKey.ServerName ), "Loading world " + newWorld.GetClassyName() ) );
            }

            writer.WriteLevelBegin();
            byte[] buffer = new byte[1024];
            int bytesSent = 0;

            // Fetch compressed map copy
            byte[] blockData;
            using( MemoryStream stream = new MemoryStream() ) {
                newWorld.map.GetCompressedCopy( stream, true );
                blockData = stream.ToArray();
            }
            Logger.Log( "Session.JoinWorld: Sending compressed level copy ({0} bytes) to {1}.", LogType.Debug,
                           blockData.Length, player.name );

            // disable low-latency-mode to avoid wasting bandwidth for map transfer
            client.NoDelay = false;

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

            writer.Write( PacketWriter.MakeHandshake( player,
                                                      Config.GetString( ConfigKey.ServerName ),
                                                      "Loading world " + newWorld.GetClassyName() + Color.White + " (almost there...)" ) );

            // Done sending over level copy
            writer.Write( PacketWriter.MakeLevelEnd( newWorld.map ) );

            // Begin accepting block changes again
            isBetweenWorlds = false;

            // Send new spawn
            Position spawn;
            if( postJoinPosition != null ) {
                spawn = (Position)postJoinPosition;
                postJoinPosition = null;
            } else {
                spawn = newWorld.map.spawn;
            }
            player.pos = spawn;
            writer.WriteAddEntity( 255, player, spawn );
            writer.WriteTeleport( 255, spawn );

            // Send player list
            newWorld.SendPlayerList( player );

            player.Message( "Joined world {0}", newWorld.GetClassyName() );

            if( Config.GetBool( ConfigKey.LowLatencyMode ) ) {
                client.NoDelay = true;
            }

            Server.FireWorldChangedEvent( player, oldWorld, newWorld );

            // Done.
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );

            return true;
        }


        public IPAddress GetIP() {
            return ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;
        }


        public void Send( Packet packet ) {
            Send( packet, true );
        }

        public void Send( Packet packet, bool isHighPriority ) {
            if( canQueue ) {
                if( isHighPriority ) {
                    priorityOutputQueue.Enqueue( packet );
                } else {
                    outputQueue.Enqueue( packet );
                }
            }
        }


        // warning: not thread safe. should ONLY be called from IoThread
        public void SendNow( Packet packet ) {
            writer.Write( packet.data );
        }


        string ReadString() {
            return ASCIIEncoding.ASCII.GetString( reader.ReadBytes( 64 ) ).Trim();
        }


        public void Kick( string message ) {
            Send( PacketWriter.MakeDisconnect( message ) );
            canReceive = false;
            canQueue = false;
        }


        public void KickNow( string message ) {
            SendNow( PacketWriter.MakeDisconnect( message ) );
            writer.Flush();
            canReceive = false;
            canSend = false;
            canQueue = false;
        }


        bool DetectMovementPacketSpam() {
            if( antiSpeedPacketLog.Count >= antiSpeedMaxPacketCount ) {
                DateTime oldestTime = antiSpeedPacketLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < antiSpeedMaxPacketInterval ) {
                    Logger.Log( "{0} was kicked for spamming movement packets ({1} packets in {2} seconds)", LogType.SuspiciousActivity,
                                player.name,
                                antiSpeedPacketLog.Count,
                                spamTimer );
                    Server.SendToAll( player.GetClassyName() + Color.Red + " was kicked for hacking (packet spam)." );
                    KickNow( "Packet spamming detected." );
                    return true;
                    /*Server.SendToAll( "DEBUG: " + player.name + " moved " + antiSpeedPacketLog.Count + " times in " + spamTimer.ToString("0.000") );
                    antiSpeedPacketLog.Clear();*/
                }
            }
            antiSpeedPacketLog.Enqueue( DateTime.UtcNow );
            return false;
        }


        public void Disconnect() {
            if( player != null ) {
                Server.UnregisterPlayer( player );
                player = null;
            }

            if( ioThread != null ) {
                if( ioThread.IsAlive ) {
                    ioThread.Abort();
                }
                ioThread = null;
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
        }
    }
}