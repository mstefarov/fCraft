// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using fCraft.Events;
using fNbt;
using JetBrains.Annotations;

namespace fCraft.ServerCLI {
    internal static class Program {
        static bool useColor,
                    exitOnShutdown = true;


        static void Main( [NotNull] string[] args ) {
            // Check fCraft.dll version
            if( typeof( Server ).Assembly.GetName().Version != typeof( Program ).Assembly.GetName().Version ) {
                Console.Error.WriteLine( "fCraft.dll version does not match ServerCLI.exe version." );
                Environment.ExitCode = (int)ShutdownReason.FailedToInitialize;
                return;
            }

            NbtFile file = new NbtFile("ClassicWorldTest.cw");
            string derp = file.ToString();


            Console.Title = "fCraft " + Updater.CurrentRelease.VersionString + " - starting...";

            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;
            Server.ShutdownEnded += OnShutdownEnded;

#if !DEBUG
            try {
#endif
            Server.InitLibrary( args );
            useColor = !Server.HasArg( ArgKey.NoConsoleColor );

            Server.InitServer();

            CheckForUpdates();
            Console.Title = "fCraft " + Updater.CurrentRelease.VersionString + " - " +
                            ConfigKey.ServerName.GetString();

            if( !ConfigKey.ProcessPriority.IsBlank() ) {
                try {
                    Process.GetCurrentProcess().PriorityClass =
                        ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                } catch( Exception ) {
                    Logger.Log( LogType.Warning, "Program.Main: Could not set process priority, using defaults." );
                }
            }

            // don't hook up Ctrl+C handler until the server's about to start
            Console.CancelKeyPress += OnCancelKeyPress;

            if( Server.StartServer() ) {
                Console.WriteLine( "** Running fCraft version {0}. **", Updater.CurrentRelease.VersionString );
                Console.WriteLine( "** Server is now ready. Type /Shutdown to exit safely. **" );

                while( !Server.IsShuttingDown ) {
                    string cmd;
                    try {
                        cmd = Console.ReadLine();
                    } catch( ArgumentOutOfRangeException ) {
                        // workaround for TermInfoDriver bug under Mono
                        continue;
                    }
                    if( cmd == null ) {
                        Console.WriteLine(
                            "*** Received EOF from console. You will not be able to type anything in console any longer. ***" );
                        break;
                    }
                    if( cmd.Equals( "/Clear", StringComparison.OrdinalIgnoreCase ) ) {
                        Console.Clear();
                    } else {
#if !DEBUG
                            try {
                                Player.Console.ParseMessage( cmd, true );
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "Error while executing a command from console",
                                                          "ServerCLI",
                                                          ex, false );
                            }
#else
                        Player.Console.ParseMessage( cmd, true );
#endif
                    }
                }
            } else {
                ReportFailure( ShutdownReason.FailedToStart );
            }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerCLI", "ServerCLI", ex, true );
                ReportFailure( ShutdownReason.Crashed );
            } finally {
                Console.ResetColor();
            }
#endif
        }


        static void OnShutdownEnded( [CanBeNull] object sender, [NotNull] ShutdownEventArgs e ) {
            if( exitOnShutdown ) {
                Environment.Exit( Environment.ExitCode );
            }
        }


        static void OnCancelKeyPress( [CanBeNull] object sender, [NotNull] ConsoleCancelEventArgs e ) {
            switch( e.SpecialKey ) {
                case ConsoleSpecialKey.ControlBreak:
                    Console.WriteLine( "*** Shutting down (Ctrl-Break) ***" );
                    Thread t = new Thread( OnControlShutdown );
                    t.Start();
                    t.Join();
                    break;
                case ConsoleSpecialKey.ControlC:
                    e.Cancel = true;
                    Console.WriteLine( "*** Shutting down (Ctrl-C) ***" );
                    new Thread( OnControlShutdown ).Start();
                    break;
            }
        }


        static void OnControlShutdown() {
            Server.Shutdown( new ShutdownParams( ShutdownReason.ProcessClosing, TimeSpan.Zero, false ), true );
            Environment.Exit( (int)ShutdownReason.ProcessClosing );
        }


        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "fCraft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if( useColor ) Console.ResetColor();

            exitOnShutdown = Server.HasArg( ArgKey.ExitOnCrash );
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, false ), false );
            if( !exitOnShutdown ) {
                Console.ReadLine();
            }
        }


        [DebuggerStepThrough]
        static void OnLogged( [CanBeNull] object sender, [NotNull] LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            switch( e.MessageType ) {
                case LogType.Error:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.SeriousError:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.White;
                    if( useColor ) Console.BackgroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Warning:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Debug:
                case LogType.Trace:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                default:
                    Console.WriteLine( e.Message );
                    return;
            }
        }


        static void OnHeartbeatUriChanged( [CanBeNull] object sender, [NotNull] UrlChangedEventArgs e ) {
            File.WriteAllText( "externalurl.txt", e.NewUrl.ToString(), Encoding.ASCII );
            Console.WriteLine( "** URL: {0} **", e.NewUrl );
            Console.WriteLine( "URL is also saved to file externalurl.txt" );
        }

        #region Updates

        static readonly AutoResetEvent UpdateDownloadWaiter = new AutoResetEvent( false );
        static bool updateFailed;

        static readonly object ProgressReportLock = new object();


        static void OnUpdateDownloadProgress( [CanBeNull] object sender, [NotNull] DownloadProgressChangedEventArgs e ) {
            lock( ProgressReportLock ) {
                Console.CursorLeft = 0;
                int maxProgress = Console.WindowWidth - 9;
                int progress = (int)Math.Round( (e.ProgressPercentage/100f)*(maxProgress - 1) );
                Console.Write( "{0,3}% |", e.ProgressPercentage );
                Console.Write( new String( '=', progress ) );
                Console.Write( '>' );
                Console.Write( new String( ' ', maxProgress - progress ) );
                Console.Write( '|' );
            }
        }


        static void OnUpdateDownloadCompleted( [CanBeNull] object sender, [NotNull] AsyncCompletedEventArgs e ) {
            Console.WriteLine();
            if( e.Error != null ) {
                updateFailed = true;
                if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( "Downloading the updater failed: {0}", e.Error );
                if( useColor ) Console.ResetColor();
            } else {
                Console.WriteLine( "Update download finished." );
            }
            UpdateDownloadWaiter.Set();
        }


        static void CheckForUpdates() {
            UpdaterMode updaterMode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if( updaterMode == UpdaterMode.Disabled ) return;

            UpdaterResult update = Updater.CheckForUpdates();
            if( !update.UpdateAvailable ) return;

            Console.WriteLine( "** A new version of fCraft is available: {0}, released {1:0} day(s) ago. **",
                               update.LatestRelease.VersionString,
                               update.LatestRelease.Age.TotalDays );
            if( updaterMode != UpdaterMode.Notify ) {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += OnUpdateDownloadProgress;
                client.DownloadFileCompleted += OnUpdateDownloadCompleted;
                client.DownloadFileAsync( update.DownloadUri, Paths.UpdateInstallerFileName );
                UpdateDownloadWaiter.WaitOne();
                if( updateFailed ) return;

                if( updaterMode == UpdaterMode.Prompt ) {
                    Console.WriteLine( "Restart the server and update now? y/n" );
                    var key = Console.ReadKey();
                    if( key.KeyChar == 'y' ) {
                        RestartForUpdate();
                    } else {
                        Console.WriteLine( "You can update manually by shutting down the server and running " +
                                           Paths.UpdateInstallerFileName );
                    }
                } else {
                    RestartForUpdate();
                }
            }
        }


        static void RestartForUpdate() {
            if( Server.HasArg( ArgKey.NoUpdater ) ) return;
            string restartArgs = String.Format( "{0} --restart=\"{1}\"",
                                                Server.GetArgString(),
                                                MonoCompat.PrependMono( "ServerCLI.exe" ) );
            MonoCompat.StartDotNetProcess( Paths.UpdateInstallerFileName, restartArgs, true );
        }

        #endregion
    }
}
