using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace fCraft {
    sealed class Server {
        private TcpListener listener;
        private Thread listenerThread;
        private HeartBeat heartBeat;

        public void Run() {
            listener = new TcpListener( IPAddress.Any, Config.Port );
            listener.Start();
            listenerThread = new Thread( ListenerHandler );
            listenerThread.Start();
            Logger.Log( "Server.Run: now accepting connections at port "+Config.Port );
            heartBeat = new HeartBeat();
        }

        private void ListenerHandler() {
            while( true ) {
                if( listener.Pending() ) {
                    TcpClient client = listener.AcceptTcpClient();
                    Logger.Log( "Server.ListenerHandler: Incoming connection" );
                    World.RegisterSession( new Session( client ) );
                }
                Thread.Sleep( 1 );
            }
        }

        public void ShutDown() {
            Logger.LogAlert( "Server.ShutDown: Shutting down" );
            if( listenerThread != null && listenerThread.IsAlive ) listenerThread.Abort();
            if( listener != null ) listener.Stop();
        }
    }
}