// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using fCraft;


namespace fCraftUI {

    public sealed partial class MainForm : Form {
        bool shuttingDown;
        const int MaxLinesInLog = 2000;


        public MainForm() {
            InitializeComponent();
            Shown += StartUp;
            FormClosing += HandleShutDown;
            console.OnCommand += console_Enter;
        }


        void StartUp( object sender, EventArgs a ) {
            Server.OnLog += Log;
            Server.OnURLChanged += SetURL;
            Server.OnPlayerListChanged += UpdatePlayerList;
#if DEBUG
#else
            try {
#endif
                if( Server.Init() ) {
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
                    Server.Shutdown( "error on init" );
                    Logger.Log( "---- Could Not Initialize Server ----", LogType.Error );
                }
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.Log( "Fatal error at startup: " + ex, LogType.FatalError );
                Logger.UploadCrashReport( "Unhandled exception in fCraftUI.StartUp", "fCraftUI", ex );
                Server.CheckForCommonErrors( ex );
                Server.Shutdown( "error on init" );
            }
#endif
        }


        public void StartServer() {
            Process.GetCurrentProcess().PriorityClass = Config.GetProcessPriority();
            if( Server.Start() ) {
                console.Enabled = true;
            } else {
                Logger.Log( "---- Could Not Start The Server ----", LogType.Error );
            }
        }

        void HandleShutDown( object sender, CancelEventArgs e ) {
            shuttingDown = true;
            Server.Shutdown( "quit" );
        }


        delegate void LogDelegate( string message );
        delegate void PlayerListUpdateDelegate( string[] items );

        public void Log( string message, LogType type ) {
            try {
                if( shuttingDown ) return;
                if( logBox.InvokeRequired ) {
                    LogDelegate d = new LogDelegate( LogInternal );
                    try {
                        Invoke( d, new object[] { message } );
                    } catch { };
                } else {
                    LogInternal( message );
                }
            } catch( ObjectDisposedException ) { }
        }

        void LogInternal( string message ) {
            try {
                logBox.AppendText( message + Environment.NewLine );
                if( logBox.Lines.Length > MaxLinesInLog ) {
                    logBox.Text = "----- cut off, see fCraft.log for complete log -----" +
                        Environment.NewLine +
                        logBox.Text.Substring( logBox.GetFirstCharIndexFromLine( 50 ) );
                }
                logBox.SelectionStart = logBox.Text.Length;
                logBox.ScrollToCaret();
            } catch( ObjectDisposedException ) { }
        }


        public void SetURL( string URL ) {
            try {
                if( shuttingDown ) return;
                if( urlDisplay.InvokeRequired ) {
                    LogDelegate d = new LogDelegate( SetURLInternal );
                    Invoke( d, new object[] { URL } );
                } else {
                    SetURLInternal( URL );
                }
            } catch( ObjectDisposedException ) { }
        }

        void SetURLInternal( string URL ) {
            try {
                urlDisplay.Text = URL;
                urlDisplay.Enabled = true;
                urlDisplay.Select();
                bPlay.Enabled = true;
            } catch( ObjectDisposedException ) { }
        }


        public void UpdatePlayerList( string[] playerNames ) {
            try {
                if( shuttingDown ) return;
                if( playerList.InvokeRequired ) {
                    Invoke( (PlayerListUpdateDelegate)UpdatePlayerListInternal, new object[] { playerNames } );
                } else {
                    UpdatePlayerListInternal( playerNames );
                }
            } catch( ObjectDisposedException ) { }
        }

        void UpdatePlayerListInternal( string[] items ) {
            try {
                playerList.Items.Clear();
                Array.Sort( items );
                foreach( string item in items ) {
                    playerList.Items.Add( item );
                }
            } catch( ObjectDisposedException ) { }
        }


        private void console_Enter() {
            string[] separator = { Environment.NewLine };
            string[] lines = console.Text.Trim().Split( separator, StringSplitOptions.RemoveEmptyEntries );
            foreach( string line in lines ) {
                try {
                    Player.Console.ParseMessage( line, true );
                } catch( Exception ex ) {
                    Logger.LogConsole( "Error occured while trying to execute last console command: " );
                    Logger.LogConsole( ex.ToString() + ": " + ex.Message );
                }
            }
            console.Text = "";
        }

        private void bPlay_Click( object sender, EventArgs e ) {
            Process.Start( urlDisplay.Text );
        }
    }
}