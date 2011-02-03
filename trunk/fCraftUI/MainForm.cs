// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using fCraft;

namespace fCraftUI {

    public sealed partial class MainForm : Form {
        bool shutdownPending, shutdownComplete;
        const int MaxLinesInLog = 2000;
        string[] args;

        public MainForm( string[] _args ) {
            args = _args;
            InitializeComponent();
            Shown += StartUp;
            FormClosing += HandleShutDown;
            console.OnCommand += console_Enter;
        }


        void StartUp( object sender, EventArgs a ) {
            Server.InitLibrary( args );

            Server.OnLog += Log;
            Server.OnURLChanged += SetURL;
            Server.OnPlayerListChanged += UpdatePlayerList;
            Server.OnShutdownEnd += OnServerShutdown;

#if !DEBUG
            try {
#endif
                if( Server.InitServer() ) {
                    Text = "fCraft " + Updater.GetVersionString() + " - " + Config.GetString( ConfigKey.ServerName );

                    Application.DoEvents();
                    UpdaterResult update = Updater.CheckForUpdates();
                    Application.DoEvents();

                    if( update.UpdateAvailable ) {
                        if( Config.GetString( ConfigKey.AutomaticUpdates ) == "Notify" ) {
                            Log( String.Format( Environment.NewLine +
                                                "*** A new version of fCraft is available: v{0}, released {1:0} day(s) ago. ***" +
                                                Environment.NewLine,
                                                update.GetVersionString(),
                                                DateTime.Now.Subtract( update.ReleaseDate ).TotalDays ), LogType.ConsoleOutput );
                            StartServer();
                        } else {
                            UpdateWindow updateWindow = new UpdateWindow( update, this, Config.GetString( ConfigKey.AutomaticUpdates ) == "Auto" );
                            updateWindow.StartPosition = FormStartPosition.CenterParent;
                            updateWindow.ShowDialog();
                        }
                    } else {
                        StartServer();
                    }
                } else {
                    Shutdown( "failed to initialize", false );
                }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in fCraftUI.StartUp", "fCraftUI", ex );
                Shutdown( "error at startup", false );
            }
#endif
        }


        public void StartServer() {
            try {
                if( Process.GetCurrentProcess().PriorityClass != Config.GetProcessPriority() ) {
                    Process.GetCurrentProcess().PriorityClass = Config.GetProcessPriority();
                }
            } catch( Exception ) {
                Logger.Log( "MainForm.StartServer: Could not set process priority, using defaults.", LogType.Warning );
            }
            if( Server.StartServer() ) {
                if( !Config.GetBool( ConfigKey.HeartbeatEnabled ) ) {
                    urlDisplay.Text = "Heartbeat disabled. See externalurl.txt";
                }
                console.Enabled = true;
            } else {
                Shutdown( "failed to start", false );
            }
        }

        void HandleShutDown( object sender, CancelEventArgs e ) {
            if( !shutdownComplete ) {
                e.Cancel = true;
                Shutdown( "quit", true );
            }
        }

        void Shutdown( string reason, bool quit ) {
            if( shutdownPending ) return;

            //Log( "Shutting down...", LogType.ConsoleOutput ); // write to console only

            shutdownPending = true;
            Logger.Log( "---- Shutting Down: {0} ----", LogType.SystemActivity, reason );
            Server.InitiateShutdown( reason, 0, quit, false );
            urlDisplay.Enabled = false;
            console.Enabled = false;
        }

        delegate void LogDelegate( string message, LogType type );
        delegate void SetURLDelegate( string URL );
        delegate void PlayerListUpdateDelegate( string[] items );

        public void Log( string message, LogType type ) {
            try {
                if( shutdownComplete ) return;
                if( logBox.InvokeRequired ) {
                    Invoke( (LogDelegate)Log, message, type );
                } else {
                    logBox.AppendText( message + Environment.NewLine );
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


        public void SetURL( string URL ) {
            try {
                if( shutdownPending ) return;
                if( urlDisplay.InvokeRequired ) {
                    Invoke( (SetURLDelegate)SetURL, URL );
                } else {
                    urlDisplay.Text = URL;
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


        void OnServerShutdown() {
            try {
                Invoke( (MethodInvoker)delegate {
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
                } else {
                    Player.Console.ParseMessage( line, true );
                }
#if !DEBUG
                } catch( Exception ex ) {
                    Logger.LogConsole( "Error occured while trying to execute last console command: " );
                    Logger.LogConsole( ex.ToString() + ": " + ex.Message );
                    Logger.LogAndReportCrash( "Exception executing command from console", "fCraftUI", ex );
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