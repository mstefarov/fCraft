using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace fCraft {
    sealed class Session {
        // high-level layer
        public Player player;
        public State state;
        Thread inputThread, outputThread;

        public enum State {
            Connected,
            Loading,
            Ready
        }

        public Session( TcpClient _client ) {
            state = State.Connected;
            player = new Player();

            outputQueue = new Queue<Packet>();
            queueLock = new object();

            client = _client;
            reader = new BinaryReader( client.GetStream() );
            writer = new BinaryWriter( client.GetStream() );

            Logger.Log( "Session: New: " + this.ToString() );

            if( LoginSequence() ) {
                inputThread = new Thread( InputHandler );
                inputThread.Start();
                outputThread = new Thread( OutputHandler );
                outputThread.Start();
                state = State.Loading;
                World.RegisterSession( this );
            } else {
                Disconnect();
            }
        }

        // network layer
        TcpClient client;
        BinaryReader reader;
        BinaryWriter writer;

        public Queue<Packet> outputQueue;
        private object queueLock;

        private void InputHandler() {
            while( true ) {
                while( client.GetStream().DataAvailable ) {
                    HandleInput( new Packet( reader ) );
                }
                Thread.Sleep( 1 );
            }
        }

        private void OutputHandler() {
            Packet packet;
            while( true ) {
                while( outputQueue.Count > 0 ) {
                    lock( queueLock ) {
                        packet = outputQueue.Dequeue();
                    }
                    try {
                        writer.Write( packet.data );
                    } catch( Exception e ) {
                        Logger.LogError( "Session: Error while sending " + packet.ToString() +
                            " to " + this.ToString() +
                            ": " + e.Message );
                    }
                }
                Thread.Sleep( 1 );
            }
        }

        public void Send( Packet packet ) {
            lock( queueLock ) {
                outputQueue.Enqueue( packet );
            }
        }

        private void HandleInput( Packet packet ) { }

        private void Disconnect() {
            Logger.LogAlert( "Session.Disconnect: Disconnecting" );
            if( inputThread != null && inputThread.IsAlive ) inputThread.Abort();
            if( outputThread != null && outputThread.IsAlive ) outputThread.Abort();
            if( reader != null ) reader.Close();
            if( writer != null ) writer.Close();
            if( client != null ) {
                if( client.Connected ) client.Client.Close();
                client.Close();
            }
        }

        // login logic
        private bool LoginSequence() {
            PacketReader loginPacket = new PacketReader( reader );

            if( loginPacket.opcode != (uint)InputCodes.Handshake ) {
                Logger.LogAlert( "Session.LoginSequence: Unexpected opcode in the first packet: " + loginPacket.opcode );
                LoginFailure( "Unexpected handshake message - possible protocol mismatch!" );
                return false;
            }

            int clientProtocolVersion = (int)loginPacket.ReadByte();
            if( clientProtocolVersion != Config.ProtocolVersion ) {
                Logger.LogAlert( "Session.LoginSequence: Wrong protocol version: " + clientProtocolVersion );
                LoginFailure( "Incompatible protocol version!" );
                return false;
            }

            player.name = loginPacket.ReadString();
            if( !Player.IsValidName( player.name ) ) {
                Logger.LogAlert( "Session.LoginSequence: Unacceptible player name: " + player.name );
                LoginFailure( "Invalid characters in player name!" );
                return false;
            }

            Logger.LogAlert( "Session.LoginSequence: Success! " + player.name + " authenticated." );
            LoginSuccess();

            // string verificationKey = loginPacket.ReadString();
            // byte unused = loginPacket.ReadByte();
            return true;
        }

        private void LoginFailure( string message ) {
            Packet disconnectPacket = PacketWriter.MakeDisconnectPacket( message );
            writer.Write( disconnectPacket.data );
            writer.Flush();
        }

        private void LoginSuccess() {
            Packet handshakePacket = PacketWriter.MakeHandshakePacket( player );
            writer.Write( handshakePacket.data );
            writer.Flush();
        }

        public override string ToString() {
            string signature = "Session(";
            if( client != null && client.Connected ) {
                signature += " connected, from " + client.Client.LocalEndPoint.ToString() + " to " + client.Client.RemoteEndPoint.ToString();
            } else {
                signature += " not connected";
            }
            signature += " )";
            return signature;
        }
    }
}