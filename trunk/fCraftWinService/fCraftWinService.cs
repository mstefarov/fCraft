// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
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

        static readonly AutoResetEvent ShutdownWaiter = new AutoResetEvent( false );


        internal fCraftWinService() {
            ServiceName = Name;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }


        protected override void OnStart( string[] args ) {
            Server.InitLibrary( args );
            Server.OnShutdownEnd += ShutdownEndHandler;
            Server.OnURLChanged += SetUrl;
            if( !Server.InitServer() || !Server.StartServer() ) {
                throw new Exception( "Could not start fCraft." );
            }
            Logger.Log( "fCraftWinService.OnStart: Service started.", LogType.SystemActivity );
            base.OnStart( args );
        }


        protected override void OnStop() {
            Logger.Log( "fCraftWinService.OnStop: Stopping.", LogType.SystemActivity );
            Server.InitiateShutdown( "Shutting down", 0, false, false );
            ShutdownWaiter.WaitOne();
            base.OnStop();
        }


        static void ShutdownEndHandler() {
            ShutdownWaiter.Set();
        }


        static void SetUrl( string url ) {
            File.WriteAllText( "externalurl.txt", url, Encoding.ASCII );
            Console.WriteLine( "** " + url + " **" );
        }
    }
}