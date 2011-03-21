// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using fCraft;
using fCraft.Events;

namespace fCraftUI {

    public sealed partial class MainForm : Form {
        bool shutdownPending, shutdownComplete;
        const int MaxLinesInLog = 2000;
        readonly string[] args;

        public MainForm( string[] _args ) {
            args = _args;
            InitializeComponent();
            Shown += StartUp;
            FormClosing += HandleShutDown;
            console.OnCommand += console_Enter;
        }


        void StartUp( object sender, EventArgs a ) {
            Logger.Logged += OnLogged;
            Heartbeat.UrlChanged += OnHeartbeatUrlChanged;
            Server.OnPlayerListChanged += UpdatePlayerList;
            Server.ShutdownEnded += OnServerShutdownEnded;

            Server.InitLibrary( args );

            //new UpdateWindow( new UpdaterResult { ChangeLog = "changelog goes here", DownloadLink = "www.derp.com", NewVersionNumber = Updater.Version + 1, ReleaseDate = DateTime.Now.AddDays( -1337 ), UpdateAvailable = true }, this, false ).ShowDialog();


#if !DEBUG
            try {
#endif
                if( Server.InitServer() ) {
                    Text = "fCraft " + Updater.GetVersionString() + " - " + ConfigKey.ServerName.GetString();
                    Application.DoEvents();
                    StartServer();
                    //Application.DoEvents();
                    //UpdaterResult update = Updater.CheckForUpdates();
                    //Application.DoEvents();

                    /*if( update.UpdateAvailable ) {
                        if( ConfigKey.UpdateMode.GetEnum<AutoUpdaterMode>() == AutoUpdaterMode.Notify ) {
                            Log( String.Format( Environment.NewLine +
                                                "*** A new version of fCraft is available: v{0}, released {1:0} day(s) ago. ***" +
                                                Environment.NewLine,
                                                update.GetVersionString(),
                                                DateTime.Now.Subtract( update.ReleaseDate ).TotalDays ), LogType.ConsoleOutput );
                            StartServer();
                        } else {
                            bool auto = (ConfigKey.UpdateMode.GetEnum<AutoUpdaterMode>() == AutoUpdaterMode.Auto);
                            UpdateWindow updateWindow = new UpdateWindow( update, this, auto );
                            updateWindow.ShowDialog();
                        }
                    } else {
                        StartServer();
                    }*/
                } else {
                    Shutdown( "failed to initialize", false );
                }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in fCraftUI.StartUp", "fCraftUI", ex, true );
                Shutdown( "error at startup", false );
            }
#endif
        }


        public void StartServer() {
            if( !ConfigKey.ProcessPriority.IsEmpty() ) {
                try {
                    Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                } catch( Exception ) {
                    Logger.Log( "MainForm.StartServer: Could not set process priority, using defaults.", LogType.Warning );
                }
            }
            if( Server.StartServer() ) {
                if( !ConfigKey.HeartbeatEnabled.GetBool() ) {
                    urlDisplay.Text = "Heartbeat disabled. See externalurl.txt";
                }
                console.Enabled = true;
            } else {
                Shutdown( "failed to start", false );
            }
        }

        void HandleShutDown( object sender, CancelEventArgs e ) {
            if( shutdownComplete ) return;
            e.Cancel = true;
            Shutdown( "quit", true );
        }

        void Shutdown( string reason, bool quit ) {
            if( shutdownPending ) return;

            //Log( "Shutting down...", LogType.ConsoleOutput ); // write to console only

            shutdownPending = true;
            Logger.Log( "---- Shutting Down: {0} ----", LogType.SystemActivity, reason );
            Server.Shutdown( reason, 0, quit, false, false );
            urlDisplay.Enabled = false;
            console.Enabled = false;
        }

        delegate void PlayerListUpdateDelegate( string[] items );

        public void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            try {
                if( shutdownComplete ) return;
                if( logBox.InvokeRequired ) {
                    Invoke( (EventHandler<LogEventArgs>)OnLogged,
                            sender, e );
                } else {
                    logBox.AppendText( e.Message + Environment.NewLine );
                    if( logBox.Lines.Length > MaxLinesInLog ) {
                        logBox.Text = "----- cut off, see fCraft.log for complete log -----" +
                            Environment.NewLine +
                            logBox.Text.Substring( logBox.GetFirstCharIndexFromLine( 50 ) );
                    }
                    logBox.SelectionStart = logBox.Text.Length;
                    logBox.ScrollToCaret();
                }
            } catch( ObjectDisposedException ) { }
        }


        public void OnHeartbeatUrlChanged( object sender, UrlChangedEventArgs e ) {
            try {
                if( shutdownPending ) return;
                if( urlDisplay.InvokeRequired ) {
                    Invoke( (EventHandler<UrlChangedEventArgs>)OnHeartbeatUrlChanged,
                            sender, e );
                } else {
                    urlDisplay.Text = e.NewUrl;
                    urlDisplay.Enabled = true;
                    bPlay.Enabled = true;
                }
            } catch( ObjectDisposedException ) { }
        }


        public void UpdatePlayerList( string[] playerNames ) {
            try {
                if( shutdownPending ) return;
                if( playerList.InvokeRequired ) {
                    Invoke( (PlayerListUpdateDelegate)UpdatePlayerList, new object[] { playerNames } );
                } else {
                    playerList.Items.Clear();
                    Array.Sort( playerNames );
                    foreach( string item in playerNames ) {
                        playerList.Items.Add( item );
                    }
                }
            } catch( ObjectDisposedException ) { }
        }


        void OnServerShutdownEnded(object sender, ShutdownEventArgs e) {
            try {
                Invoke( (Action)delegate {
                    shutdownComplete = true;
                    Application.Exit();
                } );
            } catch( ObjectDisposedException ) { }
        }


        private void console_Enter() {
            string[] separator = { Environment.NewLine };
            string[] lines = console.Text.Trim().Split( separator, StringSplitOptions.RemoveEmptyEntries );
            foreach( string line in lines ) {
#if !DEBUG
                try {
#endif
                    if( line.Equals( "/clear", StringComparison.OrdinalIgnoreCase ) ) {
                        logBox.Clear();
                    } else if(line.Equals("/credits", StringComparison.OrdinalIgnoreCase)){
                        new AboutWindow().Show();
                    }else{
                        Player.Console.ParseMessage( line, true );
                    }
#if !DEBUG
                } catch( Exception ex ) {
                    Logger.LogConsole( "Error occured while trying to execute last console command: " );
                    Logger.LogConsole( ex.GetType().Name + ": " + ex.Message );
                    Logger.LogAndReportCrash( "Exception executing command from console", "fCraftUI", ex, false );
                }
#endif
            }
            console.Text = "";
        }

        private void bPlay_Click( object sender, EventArgs e ) {
            Process.Start( urlDisplay.Text );
        }
    }
}