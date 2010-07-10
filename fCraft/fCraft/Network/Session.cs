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
        public bool canReceive, canSend, canQueue, canDispose;
        public bool isBetweenWorlds = true;

        Thread ioThread;
        TcpClient client;
        BinaryReader reader;
        PacketWriter writer;
        public Queue<Packet> outputQueue, priorityOutputQueue;
        object queueLock, priorityQueueLock;
        internal World forcedWorldToJoin = null;

        int fullPositionUpdateCounter = 0;
        const int fullPositionUpdateInterval = 10;


        public Session( TcpClient _client ) {
            loginTime = DateTime.Now;

            canReceive = true;
            canQueue = true;
            canSend = false;
            canDispose = false;

            outputQueue = new Queue<Packet>();
            priorityOutputQueue = new Queue<Packet>();
            queueLock = new object();
            priorityQueueLock = new object();

            client = _client;
            client.SendTimeout = 10000;
            client.ReceiveTimeout = 10000;

            reader = new BinaryReader( client.GetStream() );
            writer = new PacketWriter( client.GetStream() );

            Logger.Log( "Session: {0}", LogType.Debug, ToString() );

            ioThread = new Thread( IoLoop );
            ioThread.IsBackground = true;
            ioThread.Start();
        }


        void IoLoop() {
            short x, y, h;
            byte mode, type, opcode;
            Packet packet;

            int pollInterval = 200;
            int pollCounter = 0;
            int packetsSent = 0;

            try {
                LoginSequence();
                canSend = true;

                while( !canDispose ) {
                    Thread.Sleep( 1 );

                    if( forcedWorldToJoin != null ) {
                        JoinWorld( forcedWorldToJoin, true );
                        forcedWorldToJoin = null;
                        continue;
                    }

                    packetsSent = 0;

                    // detect player disconnect
                    if( pollCounter > pollInterval ) {
                        if( !client.Connected ||
                            (client.Client.Poll( 1000, SelectMode.SelectRead ) && client.Client.Available == 0) ) {
                            if( player != null ) {
                                Logger.Log( "Session.IoLoop: Lost connection to player {0} ({1}).", LogType.Debug, player.GetLogName(), GetIP() );
                            } else {
                                Logger.Log( "Session.IoLoop: Lost connection to unidentified player {0}.", LogType.Debug, GetIP() );
                            }
                            return;
                        }
                        pollCounter = 0;
                    }
                    pollCounter++;

                    // send priority output to player
                    while( canSend && priorityOutputQueue.Count > 0 && packetsSent < Server.maxSessionPacketsPerTick ) {
                        lock( priorityQueueLock ) {
                            packet = priorityOutputQueue.Dequeue();
                        }
                        writer.Write( packet.data );
                        packetsSent++;
                        if( packet.data[0] == (byte)OutputCodes.Disconnect ) {
                            Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug, player.GetLogName() );
                            return;
                        }
                    }

                    // send output to player
                    while( canSend && outputQueue.Count > 0 && packetsSent < Server.maxSessionPacketsPerTick ) {
                        lock( queueLock ) {
                            packet = outputQueue.Dequeue();
                        }
                        writer.Write( packet.data );
                        packetsSent++;
                        if( packet.data[0] == (byte)OutputCodes.Disconnect ) {
                            writer.Flush();
                            Logger.Log( "Session.IoLoop: Kick packet delivered to {0}.", LogType.Debug, player.GetLogName() );
                            return;
                        }
                    }

                    // get input from player
                    while( canReceive && client.GetStream().DataAvailable ) {
                        opcode = reader.ReadByte();
                        switch( (InputCodes)opcode ) {

                            // Message
                            case InputCodes.Message:
                                player.ResetIdleTimer();
                                reader.ReadByte();
                                string message = ReadString();
                                if( Player.CheckForIllegalChars( message ) ) {
                                    Logger.Log( "Player.ParseMessage: {0} attempted to write illegal characters in chat and was kicked.",
                                                LogType.SuspiciousActivity,
                                                player.GetLogName() );
                                    KickNow( "Illegal characters in chat." );
                                    return;
                                } else {
                                    player.ParseMessage( message, false );
                                }
                                break;

                            // Player movement
                            case InputCodes.MoveRotate:

                                reader.ReadByte();
                                Position newPos = new Position();
                                newPos.x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                newPos.h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                newPos.y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                newPos.r = reader.ReadByte();
                                newPos.l = reader.ReadByte();

                                if( newPos.h < 0 /*|| newPos.x < -32 || newPos.x >= player.world.map.widthX * 32 + 32 || newPos.y < -32 || newPos.y > player.world.map.widthY * 32 + 32*/ ) {
                                    Logger.Log( player.GetLogName() + " was kicked for moving out of map boundaries.", LogType.SuspiciousActivity );
                                    KickNow( "Hacking detected: out of map boundaries." );
                                    return;
                                }

                                Position delta = new Position(), oldPos = player.pos;
                                bool posChanged, rotChanged;

                                delta.Set( newPos.x - oldPos.x, newPos.y - oldPos.y, newPos.h - oldPos.h, newPos.r, newPos.l );
                                posChanged = delta.x != 0 || delta.y != 0 || delta.h != 0;
                                rotChanged = newPos.r != oldPos.r || newPos.l != oldPos.l;

                                if( rotChanged ) player.ResetIdleTimer();

                                if( player.isFrozen ) {
                                    if( rotChanged ) {
                                        player.world.SendToAll( PacketWriter.MakeRotate( player.id, newPos ), player );
                                        player.pos.r = newPos.r;
                                        player.pos.l = newPos.l;
                                    }
                                    if( posChanged ) {
                                        SendNow( PacketWriter.MakeTeleport( 255, player.pos ) );
                                    }

                                } else {
                                    if( !player.isHidden ) {
                                        if( delta.FitsIntoByte() && fullPositionUpdateCounter < fullPositionUpdateInterval ) {
                                            if( posChanged && rotChanged ) {
                                                player.world.SendToAll( PacketWriter.MakeMoveRotate( player.id, delta ), player );
                                            } else if( posChanged ) {
                                                player.world.SendToAll( PacketWriter.MakeMove( player.id, delta ), player );
                                            } else if( rotChanged ) {
                                                player.world.SendToAll( PacketWriter.MakeRotate( player.id, newPos ), player );
                                            }
                                        } else if( !delta.IsZero() && !player.isFrozen ) {
                                            player.world.SendToAll( PacketWriter.MakeTeleport( player.id, newPos ), player );
                                        }
                                    }
                                    player.pos = newPos;
                                }

                                fullPositionUpdateCounter++;
                                if( fullPositionUpdateCounter >= fullPositionUpdateInterval ) fullPositionUpdateCounter = 0;
                                break;

                            // Set tile
                            case InputCodes.SetTile:
                                player.ResetIdleTimer();
                                x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                mode = reader.ReadByte();
                                type = reader.ReadByte();
                                if( isBetweenWorlds ) continue;
                                if( type > 49 || x < 0 || x > player.world.map.widthX || y < 0 || y > player.world.map.widthY || h < 0 || h > player.world.map.height ) {
                                    Logger.Log( player.GetLogName() + " was kicked for sending bad SetTile packets.", LogType.SuspiciousActivity );
                                    Server.SendToAll( player.GetLogName() + " was kicked for attempted hacking.", null );
                                    KickNow( "Hacking detected: illegal SetTile packet." );
                                    return;
                                } else {
                                    if( player.SetTile( x, y, h, mode == 1, (Block)type ) ) return;
                                }
                                break;
                        }
                    }
                }

            } catch( ThreadAbortException ) {
                Logger.Log( "Session.IoLoop: Thread aborted!", LogType.Error );

            } catch( IOException ex ) {
                Logger.Log( "Session.IoLoop: {0}.", LogType.Warning, ex.Message );

            } catch( SocketException ex ) {
                Logger.Log( "Session.IoLoop: {0}.", LogType.Warning, ex.Message );
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.Log( "Session.IoLoop: {0}: {1}.", LogType.Error, ex.ToString(), ex.Message );
#endif
            } finally {
                canQueue = false;
                canSend = false;
                canDispose = true;
            }
        }


        // login logic
        void LoginSequence() {
            byte opcode = reader.ReadByte();
            if( opcode != (byte)InputCodes.Handshake ) {
                Logger.Log( "Session.LoginSequence: Unexpected opcode in the first packet: {0}.", LogType.Error, opcode );
                KickNow( "Unexpected handshake message - possible protocol mismatch!" );
                return;
            }

            // check protocol version
            int clientProtocolVersion = reader.ReadByte();
            if( clientProtocolVersion != Config.ProtocolVersion ) {
                Logger.Log( "Session.LoginSequence: Wrong protocol version: {0}.", LogType.Error, clientProtocolVersion );
                KickNow( "Incompatible protocol version!" );
                return;
            }

            // check name for nonstandard characters
            string playerName = ReadString();
            string verificationCode = ReadString();
            reader.ReadByte(); // unused

            if( !Player.IsValidName( playerName ) ) {
                Logger.Log( "Session.LoginSequence: Unacceptible player name: {0} ({1})", LogType.SuspiciousActivity, playerName, GetIP().ToString() );
                KickNow( "Invalid characters in player name!" );
                return;
            }

            // check if player is banned
            player = new Player( Server.mainWorld, playerName, this, Server.mainWorld.map.spawn );
            if( player.info.banned ) {
                player.info.ProcessFailedLogin( player );
                Logger.Log( "Banned player {0} tried to log in.", LogType.SuspiciousActivity, player.name );
                Server.SendToAll( Color.Sys + "Banned player " + player.name + " tried to log in." );
                KickNow( "You were banned by " + player.info.bannedBy + " " + DateTime.Now.Subtract( player.info.banDate ).Days + " days ago." );
                return;
            }

            // check if player's IP is banned
            IPBanInfo IPBanInfo = IPBanList.Get( GetIP() );
            if( IPBanInfo != null ) {
                player.info.ProcessFailedLogin( player );
                IPBanInfo.ProcessAttempt( player );
                Logger.Log( "{0} tried to log in from a banned IP.", LogType.SuspiciousActivity, player.name );
                Server.SendToAll( Color.Sys + player.name + " tried to log in from a banned IP." );
                KickNow( "Your IP was banned by " + IPBanInfo.bannedBy + " " + DateTime.Now.Subtract( IPBanInfo.banDate ).Days + " days ago." );
                return;
            }

            // verify name
            if( !Server.VerifyName( player.name, verificationCode ) ) {
                string standardMessage = String.Format( "Session.LoginSequence: Could not verify player name for {0} ({1}).",
                                                        player.name, GetIP() );
                if( GetIP().ToString() == "127.0.0.1" &&
                    (Config.GetString( ConfigKey.VerifyNames ) == "Balanced" || Config.GetString( ConfigKey.VerifyNames ) == "Never") ) {
                    Logger.Log( "{0} Player was identified as connecting from localhost and allowed in.", LogType.SuspiciousActivity, standardMessage );
                }else if( player.info.timesVisited == 1 || player.info.lastIP.ToString() != GetIP().ToString() ) {
                    switch( Config.GetString( ConfigKey.VerifyNames ) ) {
                        case "Always":
                        case "Balanced":
                            player.info.ProcessFailedLogin( player );
                            Logger.Log( "{0} IP did not match. Player was kicked.", LogType.SuspiciousActivity, standardMessage );
                            KickNow( "Could not verify player name!" );
                            return;
                        case "Never":
                            Logger.Log( "{0} IP did not match. Player was allowed in anyway because VerifyNames is set to Never.",
                                        LogType.SuspiciousActivity,
                                        standardMessage );
                            player.Message( Color.Red, "Your name could not be verified." );
                            Server.SendToAll( Color.Red + "Name and IP of " + player.name + " could not be verified!", player );
                            break;
                    }
                } else {
                    switch( Config.GetString( ConfigKey.VerifyNames ) ) {
                        case "Always":
                            player.info.ProcessFailedLogin( player );
                            Logger.Log( "{0} IP matched previous records for that name. " +
                                                "Player was kicked anyway because VerifyNames is set to Always.", LogType.SuspiciousActivity,
                                                standardMessage );
                            KickNow( "Could not verify player name!" );
                            return;
                        case "Balanced":
                        case "Never":
                            Logger.Log( "{0} IP matched previous records for that name. Player was allowed in.", LogType.SuspiciousActivity,
                                        standardMessage );
                            player.Message( Color.Red, "Your name could not be verified." );
                            if( Config.GetBool( ConfigKey.AnnounceUnverifiedNames ) ) {
                                Server.SendToAll( Color.Red + "Name of " + player.name + " could not be verified, but IP matches.", player );
                            }
                            break;
                    }
                }
            }

            // check if another player with the same name is on
            Player potentialClone = Server.FindPlayer( player.name );
            if( potentialClone != null ) {
                player.info.ProcessFailedLogin( player );
                Logger.Log( "Session.LoginSequence: Player {0} tried to log in from two computers at once.", LogType.SuspiciousActivity, player.name );
                potentialClone.Message( "Warning: someone just attempted to log in using your name." );
                KickNow( "Already connected from elsewhere!" );
                return;
            }

            if( Config.GetBool( ConfigKey.LimitOneConnectionPerIP ) ) {
                potentialClone = Server.FindPlayer( GetIP() );
                if( potentialClone != null ) {
                    player.info.ProcessFailedLogin( player );
                    Logger.Log( "Session.LoginSequence: Player {0} tried to log in from same IP ({1}) as {2}.", LogType.SuspiciousActivity,
                        player.name, GetIP().ToString(), potentialClone.name );
                    potentialClone.Message( "Warning: someone just attempted to log in using your IP." );
                    KickNow( "Only one connection per IP allowed!" );
                    return;
                }
            }

            // Register player for future block updates
            if( !Server.RegisterPlayer( player ) ) {
                KickNow( "Sorry, server is full." );
                return;
            }

            player.info.ProcessLogin( player );
            Server.FirePlayerConnectedEvent( this );
            Server.FirePlayerListChangedEvent();

            // Player is now authenticated. Send server info.
            writer.Write( PacketWriter.MakeHandshake( player, Config.GetString( ConfigKey.ServerName ), Config.GetString( ConfigKey.MOTD ) ) );

            JoinWorld( player.world, false );

            // Welcome message
            if( player.info.timesVisited > 1 ) {
                player.Message( "Welcome back to " + Config.GetString( ConfigKey.ServerName ) );
            } else {
                player.Message( "Welcome to " + Config.GetString( ConfigKey.ServerName ) );
            }

            player.Message( String.Format( "Your player class is {0}{1}{2}. Type /help for details.",
                                           player.info.playerClass.color,
                                           player.info.playerClass.name,
                                           Color.Sys ) );
        }


        internal void ClearQueues() {
            lock ( queueLock ) {
                outputQueue.Clear();
            }
        }


        public bool JoinWorld( World newWorld, bool useHandshakePacket ) {

            if( newWorld.classAccess.rank > player.info.playerClass.rank ) {
                return false;
            }

            if( !newWorld.FirePlayerTriedToJoinEvent( player ) ) {
                return false;
            }

            isBetweenWorlds = true;
            if( player.world != null ) {
                player.world.ReleasePlayer( player );
            }
            ClearQueues();

            client.NoDelay = false;

            World oldWorld = player.world;
            newWorld.AcceptPlayer( player );
            player.world = newWorld;

            // Start sending over the level copy
            if( useHandshakePacket ) {
                writer.Write( PacketWriter.MakeHandshake( player, Config.GetString( ConfigKey.ServerName ), "Loading world \"" + player.world.name + "\"" ) );
            }
            writer.WriteLevelBegin();
            byte[] buffer = new byte[1024];
            int bytesSent = 0;

            // Fetch compressed map copy
            byte[] blockData;
            using( MemoryStream stream = new MemoryStream() ) {
                player.world.map.GetCompressedCopy( stream, true );
                blockData = stream.ToArray();
            }
            Logger.Log( "Session.LoginSequence: Sending compressed level copy ({0} bytes) to {1}.", LogType.Debug,
                           blockData.Length, player.name );

            while ( bytesSent < blockData.Length ) {
                int chunkSize = blockData.Length - bytesSent;
                if ( chunkSize > 1024 ) {
                    chunkSize = 1024;
                }
                Array.Copy( blockData, bytesSent, buffer, 0, chunkSize );
                byte progress = (byte)( 100 * bytesSent / blockData.Length );

                // write in chunks of 1024 bytes or less
                writer.WriteLevelChunk( buffer, chunkSize, progress );
                bytesSent += chunkSize;
            }

            writer.Write( PacketWriter.MakeHandshake( player, Config.GetString( ConfigKey.ServerName ), "Almost there..." ) );

            // Done sending over level copy
            writer.Write( PacketWriter.MakeLevelEnd( player.world.map ) );

            // Send new spawn
            player.pos = player.world.map.spawn;
            Thread.Sleep( 100 );
            writer.WriteAddEntity( 255, player, player.pos );
            writer.WriteTeleport( 255, player.pos );

            isBetweenWorlds = false;

            // Send player list
            player.world.SendPlayerList( player );

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
                    lock( priorityQueueLock ) {
                        priorityOutputQueue.Enqueue( packet );
                    }
                } else {
                    lock( queueLock ) {
                        outputQueue.Enqueue( packet );
                    }
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