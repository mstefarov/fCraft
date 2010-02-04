using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace fCraft {
    sealed class Server {
        private TcpListener listener;
        private Thread mainThread;


        // starts up the listener thread and heartbeat
        public void Run() {
            listener = new TcpListener( IPAddress.Any, Config.Port );
            listener.Start();
            mainThread = new Thread( MainHandler );
            mainThread.Start();
            Logger.Log( "Server.Run: now accepting connections at port " + Config.Port );
            HeartBeat.Begin();
        }


        // loops forever, waiting for incoming connections
        private void MainHandler() {
            while( true ) {
                if( listener.Pending() ) {
                    Logger.Log( "Server.ListenerHandler: Incoming connection" );
                    Session session = new Session( listener.AcceptTcpClient() );
                }
                World.Update();
            }
        }


        // shuts down the server and aborts threads
        // NOTE: heartbeat should stop automatically
        public void ShutDown() {
            Logger.LogAlert( "Server.ShutDown: Shutting down" );
            HeartBeat.ShutDown();
            World.ShutDown();
            if( mainThread != null && mainThread.IsAlive ) {
                mainThread.Abort();
                mainThread = null;
            }
            if( listener != null ) {
                listener.Stop();
                listener = null;
            }
        }
    }
}