using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;

namespace fCraft {
    sealed class Session {
        // high-level layer
        public Player player;
        public DateTime loginTime;
        bool keepGoing;
        Thread ioThread;

        public Session( TcpClient _client ) {
            loginTime = DateTime.Now;

            outputQueue = new Queue<Packet>();
            queueLock = new object();

            client = _client;
            reader = new BinaryReader( client.GetStream() );
            writer = new PacketWriter( new BinaryWriter( client.GetStream() ) );

            Logger.Log( "Session: New: " + this.ToString() );

            ioThread = new Thread( IoLoop );
            ioThread.Start();
        }

        // network layer
        TcpClient client;
        BinaryReader reader;
        PacketWriter writer;

        public Queue<Packet> outputQueue;
        private object queueLock;

        private void IoLoop() {
            short x, y, h;
            byte rotation, look, mode, type, opcode;
            Packet packet;
            
            int pollInterval = 500;
            int pollCounter = 0;

            keepGoing = LoginSequence(); // if login fails, abort

            while( keepGoing ) {
                Thread.Sleep( 1 );
                try {

                    // detect player disconnect
                    if( pollCounter > pollInterval ) {
                        if( !client.Connected || client.Client.Poll( 10, System.Net.Sockets.SelectMode.SelectRead ) && (client.Client.Available == 0) ) {
                            keepGoing = false;
                            break;
                        }
                        pollCounter = 0;
                    }
                    pollCounter++;

                    // send output to player
                    while( keepGoing && outputQueue.Count > 0 ) {
                        lock( queueLock ) {
                            packet = outputQueue.Dequeue();
                        }
                        try {
                            writer.Write( packet.data );
                        } catch( IOException e ) {
                            Logger.LogError( "Session: Error while sending " + packet.ToString() +
                                " to " + this.ToString() +
                                ": " + e.Message );
                        }
                        if( packet.data[0] == (byte)OutputCodes.Disconnect ) {
                            keepGoing = false;
                        }
                    }

                    // get input from player
                    while( client.GetStream().DataAvailable ) {
                        opcode = reader.ReadByte();
                        switch( opcode ) {

                            case (byte)InputCodes.Message:
                                reader.ReadByte();
                                string message = ReadString();
                                if( message[0] == '/' ) {
                                    Commands.Parse( player, message );
                                } else if( message[0] == '@' ) {
                                    int end = message.IndexOf( ' ' );
                                    if( end == -1 ) end = message.Length;
                                    string otherName = message.Substring( 1, end - 1 );
                                    if( otherName[0] == '@' ) {
                                        // class message
                                        PlayerClass playerClass = PlayerClass.Parse( otherName.Substring( 1 ) );
                                        if( playerClass != null ) {
                                            if( message.Length > end ) {
                                                Packet classMsg = PacketWriter.MakeMessage( player.id, Color.GetCode( "gray" ) + "[" + playerClass.name + "] " + player.name + ": " + message.Substring( end + 1 ) );
                                                World.SendToClass( classMsg, playerClass );
                                                Send( classMsg );
                                            }
                                        } else {
                                            player.SysMessage( "No such class: \"" + otherName.Substring( 1 ) + "\"" );
                                        }
                                    } else {
                                        Player otherPlayer = World.FindPlayer( otherName );
                                        if( otherPlayer != null ) {
                                            otherPlayer.Message( player, Color.GetCode( "gray" ) + player.name + ": " + message.Substring( end + 1 ) );
                                            writer.WriteMessage( 0, Color.GetCode( "gray" ) + player.name + ": " + message.Substring( end + 1 ) );
                                        } else {
                                            World.NoPlayerMessage( player, otherName );
                                        }
                                    }
                                } else {
                                    player.info.linesWritten++;
                                    World.SendToAll( PacketWriter.MakeMessage( player.id, player.coloredName + ": " + message ), player );
                                    writer.WriteMessage( 0, player.coloredName + ": " + message );
                                    Logger.Log( player.name + ": " + message );
                                }
                                break;

                            case (byte)InputCodes.MoveRotate:
                                reader.ReadByte();
                                x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                rotation = reader.ReadByte();
                                look = reader.ReadByte();
                                player.Move( x, y, h, rotation, look );
                                break;

                            case (byte)InputCodes.SetTile:
                                x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                h = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
                                mode = reader.ReadByte();
                                type = reader.ReadByte();
                                player.SetTile( x, y, h, mode, type );
                                break;

                            default:
                                Logger.LogAlert( "Session.IoLoop: Unexpected or unknown input opcode: " + opcode );
                                break;
                        }
                    }

                } catch( ThreadAbortException ) {
                    Logger.LogAlert( "Session.IoLoop: Thread aborted" );
                } catch( IOException ex ) {
                    Logger.LogError( "Session.IoLoop: Network error: " + ex.Message );
                } catch( InvalidOperationException ex ) {
                    Logger.LogError( "Session.IoLoop: InvalidOperationException: " + ex.Message );
                }
            }
            Disconnect();
        }

        public IPAddress GetIP() {
            return ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;
        }

        public void Send( Packet packet ) {
            if( keepGoing ) {
                lock( queueLock ) {
                    outputQueue.Enqueue( packet );
                }
            }
        }

        ~Session() {
            Disconnect();
        }

        public void Kick( string message ) {
            Send( PacketWriter.MakeDisconnect( message ) );
            keepGoing = false;
        }

        public void Disconnect() {
            Logger.LogAlert( "Session.Disconnect: Disconnecting player "+player.name );
            World.UnregisterPlayer( this.player );

            if( reader != null ) {
                reader.Close();
                reader = null;
            }

            if( writer != null ){
                writer.Close();
                writer = null;
            }

            if( client != null ){
                client.Close();
                client = null;
            }

            if( ioThread != null && ioThread.IsAlive ) {
                ioThread.Abort();
                ioThread = null;
            }
        }

        // login logic
        private bool LoginSequence() {
            byte opcode = reader.ReadByte();
            if( opcode != (byte)InputCodes.Handshake ) {
                Logger.LogAlert( "Session.LoginSequence: Unexpected opcode in the first packet: " + opcode );
                writer.WriteDisconnect( "Unexpected handshake message - possible protocol mismatch!" );
                return false;
            }

            int clientProtocolVersion = reader.ReadByte();
            if( clientProtocolVersion != Config.ProtocolVersion ) {
                Logger.LogAlert( "Session.LoginSequence: Wrong protocol version: " + clientProtocolVersion );
                writer.WriteDisconnect( "Incompatible protocol version!" );
                return false;
            }

            // TODO: name validation
            string playerName = ReadString();
            if( !Player.IsValidName( playerName ) ) {
                Logger.LogAlert( "Session.LoginSequence: Unacceptible player name: " + playerName );
                writer.WriteDisconnect( "Invalid characters in player name!" );
                return false;
            }

            player = new Player( playerName, this );
            if( player.info.banned ) {
                writer.WriteDisconnect( "You were banned by " + player.info.bannedBy + " " + DateTime.Now.Subtract( player.info.banDate ).Days + " day(s) ago." );
                return false;
            }

            // string verificationCode = loginPacket.ReadString();
            // byte unused = loginPacket.ReadByte();
            reader.ReadBytes( 65 );

            Logger.LogAlert( "Session.LoginSequence: " + player.name + " is now authenticated." );

            writer.WriteHandshake( player );

            byte[] blockData = World.level.GetCompressedCopy();
            Logger.Log( "Session.LoginSequence: " + player.name + ": Sending compressed level copy (" + blockData.Length + " bytes)..." );

            // start sending over the level copy
            writer.WriteLevelBegin();

            byte[] buffer = new byte[1024];
            int bytesSent = 0;

            while( bytesSent < blockData.Length ) {
                int chunkSize = blockData.Length - bytesSent;
                if( chunkSize > 1024 ) {
                    chunkSize = 1024;
                }
                Array.Copy( blockData, bytesSent, buffer, 0, chunkSize );
                byte progress = (byte)(100 * bytesSent / blockData.Length);

                // write in chunks of 1024 bytes or less
                writer.WriteLevelChunk( buffer, chunkSize, progress );
                bytesSent += chunkSize;
            }

            // done sending over level copy
            writer.WriteLevelEnd();
            writer.Flush();

            // add player himself
            player.pos = World.level.spawn;
            writer.WriteAddEntity( 255, player.name, player.pos );
            writer.WriteTeleport( 255, player.pos );
            World.RegisterPlayer( this.player );
            World.SendPlayerList( this.player );

            // welcome message
            if( player.info.timesVisited > 1 ) {
                Send( PacketWriter.MakeSysMessage( "Welcome back to " + Config.GetString( "ServerName" ) ));
            } else {
                Send( PacketWriter.MakeSysMessage( "Welcome to " + Config.GetString( "ServerName" ) ));
            }

            Send( PacketWriter.MakeSysMessage( "Your player class is: \"" + player.info.playerClass.name + "\". Type /help for details." ) );

            // done
            Logger.LogAlert( "Session.LoginSequence: " + player.name + " is now ready." );
            return true;
        }

        public override string ToString() {
            string signature = "Session(";
            if( client != null && client.Connected ) {
                signature += "connected, from " + client.Client.LocalEndPoint.ToString() + " to " + client.Client.RemoteEndPoint.ToString();
            } else {
                signature += "not connected";
            }
            signature += ")";
            return signature;
        }

        public string ReadString() {
            return ASCIIEncoding.ASCII.GetString( reader.ReadBytes( 64 ) ).Trim();
        }
    }
}