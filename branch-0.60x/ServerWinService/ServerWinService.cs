// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.ServiceProcess;
using System.Text;
using fCraft.Events;

namespace fCraft.ServerWinService {
    sealed class ServerWinService : ServiceBase {
        public const string Name = "fCraftServerWinService";
        public const string Description = "Windows service frontend for fCraft Minecraft server.";


        internal ServerWinService() {
            ServiceName = Name;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }


        protected override void OnStart( string[] args ) {
            try {
                Server.InitLibrary( args );
                Heartbeat.UrlChanged += OnHeartbeatUrlChanged;
                Server.InitServer();
                Server.StartServer();
                Logger.Log( "ServerWinService.OnStart: Service started.", LogType.SystemActivity );
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "ServerWinService failed to initialize or start", "ServerWinService", ex, true );
            }
            base.OnStart( args );
        }


        protected override void OnStop() {
            Logger.Log( "ServerWinService.OnStop: Stopping.", LogType.SystemActivity );
            Server.Shutdown( new ShutdownParams( ShutdownReason.ProcessClosing, TimeSpan.Zero, false, false ), true );
            base.OnStop();
        }


        static void OnHeartbeatUrlChanged( object sender, UrlChangedEventArgs e ) {
            File.WriteAllText( "externalurl.txt", e.NewUrl, Encoding.ASCII );
            Console.WriteLine( "** " + e.NewUrl + " **" );
        }
    }
}