// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using fCraft;
using fCraft.Events;

namespace fCraftWinService {
    sealed class fCraftWinService : ServiceBase {
        public const string Name = "fCraftWinService";
        public const string Description = "fCraft Minecraft Server";


        internal fCraftWinService() {
            ServiceName = Name;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }


        protected override void OnStart( string[] args ) {
            try {
                Server.InitLibrary( args );
                Heartbeat.UrlChanged += OnHeartbeatUrlChanged;
                if( !Server.InitServer() || !Server.StartServer() ) {
                    throw new Exception( "Could not start fCraft." );
                }
                Logger.Log( "fCraftWinService.OnStart: Service started.", LogType.SystemActivity );
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "fCraftWinService failed to initialize or start", "fCraftWinService", ex, true );
            }
            base.OnStart( args );
        }


        protected override void OnStop() {
            Logger.Log( "fCraftWinService.OnStop: Stopping.", LogType.SystemActivity );
            Server.Shutdown( new ShutdownParams( ShutdownReason.ProcessClosing, 0, false, false ), true );
            base.OnStop();
        }


        static void OnHeartbeatUrlChanged( object sender, UrlChangedEventArgs e ) {
            File.WriteAllText( "externalurl.txt", e.NewUrl, Encoding.ASCII );
            Console.WriteLine( "** " + e.NewUrl + " **" );
        }
    }
}