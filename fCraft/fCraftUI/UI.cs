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
    public partial class UI : Form {
        bool shuttingDown = false;
        string[] args;

        public UI( string[] _args ) {
            args = _args;
            InitializeComponent();
            Shown += StartUp;
            FormClosing += HandleShutDown;
        }

        
        void StartUp( object sender, EventArgs a ) {
            Server.OnLog += Log;
            Server.OnURLChanged += SetURL;
            Server.OnPlayerListChanged += UpdatePlayerList; //TODO


            if( Server.Init() ) {
                Text = "fCraft " + Updater.GetVersionString() + " - " + Config.GetString( "ServerName" );

                UpdaterResult update = Updater.CheckForUpdates();
                if( update.UpdateAvailable ) {
                    if( Config.GetString( "AutomaticUpdates" ) == "Notify" ) {
                        Log( String.Format( Environment.NewLine +
                                            "*** A new version of fCraft is available: v{0:0.000}, released {1:0} day(s) ago. ***"+
                                            Environment.NewLine,
                                            Decimal.Divide( update.NewVersionNumber, 1000 ),
                                            DateTime.Now.Subtract( update.ReleaseDate ).TotalDays ), LogType.ConsoleOutput );
                        StartServer();
                    } else {
                        UpdateWindow updateWindow = new UpdateWindow( update, this, Config.GetString( "AutomaticUpdates" ) == "Auto" );
                        updateWindow.StartPosition = FormStartPosition.CenterParent;
                        updateWindow.ShowDialog();
                    }
                } else {
                    StartServer();
                }
            } else {
                Logger.Log( "---- Could Not Initialize World ----", LogType.FatalError );
            }
        }


        public void StartServer() {
            Process.GetCurrentProcess().PriorityClass = Config.GetBasePriority();
            Server.Start();
        }

        void HandleShutDown( object sender, CancelEventArgs e ) {
            shuttingDown = true;
            Server.Shutdown();
        }


        delegate void LogDelegate( string message );
        delegate void PlayerListUpdateDelegate( string[] items );

        public void Log( string message, LogType type ) {
            if( shuttingDown ) return;
            if( logBox.InvokeRequired ) {
                LogDelegate d = new LogDelegate( LogInternal );
                Invoke( d, new object[] { message } );
            } else {
                LogInternal( message );
            }
        }

        void LogInternal( string message ) {
            logBox.AppendText( message + Environment.NewLine );
            if( logBox.Lines.Length > 1000 ) {
                logBox.Text = "----- cut off, see fCraft.log for complete log -----" +
                    Environment.NewLine +
                    logBox.Text.Substring( logBox.GetFirstCharIndexFromLine(50) );
            }
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
        }


        public void SetURL( string URL ) {
            if( urlDisplay.InvokeRequired ) {
                LogDelegate d = new LogDelegate( SetURLInternal );
                Invoke( d, new object[] { URL } );
            } else {
                SetURLInternal( URL );
            }
        }

        void SetURLInternal( string URL ) {
            urlDisplay.Text = URL;
            urlDisplay.Enabled = true;
            urlDisplay.SelectAll();
        }


        public void UpdatePlayerList( string[] playerNames ) {
            if( playerList.InvokeRequired ) {
                PlayerListUpdateDelegate d = new PlayerListUpdateDelegate( UpdatePlayerListInternal );
                Invoke( d, new object[] { playerNames } );
            } else {
                UpdatePlayerListInternal( playerNames );
            }
        }

        void UpdatePlayerListInternal( string[] items ) {
            playerList.Items.Clear();
            Array.Sort( items );
            foreach( string item in items ) {
                playerList.Items.Add( item );
            }
        }


        private void console_Enter( object sender, PreviewKeyDownEventArgs e ) {
            if( e.KeyValue == (char)13 ) {
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
        }
    }
}