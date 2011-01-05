// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using fCraft;


namespace fCraftWinService {
    class fCraftWinService : ServiceBase {
        public const string Name = "fCraftWinService";
        public const string Description = "fCraft Minecraft Server";

        static AutoResetEvent shutdownWaiter = new AutoResetEvent( false );

        internal fCraftWinService() {
            ServiceName = Name;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart( string[] args ) {
            Server.InitLibrary( args );
            Server.OnShutdownEnd += ShutdownEndHandler;
            Server.OnURLChanged += SetURL;
            if( !Server.InitServer() || !Server.StartServer() ) {
                throw new Exception( "Could not start fCraft." );
            }
            Logger.Log( "fCraftWinService.OnStart: Service started.", LogType.SystemActivity );
            base.OnStart( args );
        }

        protected override void OnStop() {
            Logger.Log( "fCraftWinService.OnStop: Stopping.", LogType.SystemActivity );
            Server.InitiateShutdown( "Shutting down", 0, false, false );
            shutdownWaiter.WaitOne();
            base.OnStop();
        }

        static void ShutdownEndHandler() {
            shutdownWaiter.Set();
        }

        static void SetURL( string URL ) {
            File.WriteAllText( "externalurl.txt", URL, ASCIIEncoding.ASCII );
            Console.WriteLine( "** " + URL + " **" );
        }
    }
}