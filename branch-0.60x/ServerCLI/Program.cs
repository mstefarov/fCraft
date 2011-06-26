/*
 *  Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using fCraft;
using fCraft.Events;


namespace fCraft.ServerCLI {

    static class Program {
        static void Main( string[] args ) {
            Logger.Logged += OnLogged;
            Heartbeat.UrlChanged += OnHeartbeatUrlChanged;

#if !DEBUG
            try {
#endif
                Server.InitLibrary( args );

                Server.InitServer();

                UpdaterMode updaterMode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
                if( updaterMode != UpdaterMode.Disabled ) {
                    UpdaterResult update = Updater.CheckForUpdates();
                    if( update.UpdateAvailable ) {
                        Console.WriteLine( "** A new version of fCraft is available: {0}, released {1:0} day(s) ago. **",
                                           update.LatestRelease.VersionString,
                                           update.LatestRelease.Age.TotalDays );
                    }
                }

                if( !ConfigKey.ProcessPriority.IsBlank() ) {
                    try {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    } catch( Exception ) {
                        Logger.Log( "Program.Main: Could not set process priority, using defaults.", LogType.Warning );
                    }
                }

                if( Server.StartServer() ) {
                    Console.Title = "fCraft " + Updater.CurrentRelease.VersionString + " - " + ConfigKey.ServerName.GetString();
                    Console.WriteLine( "** Running fCraft version {0}. **", Updater.CurrentRelease.VersionString );
                    Console.WriteLine( "** Server is now ready. Type /shutdown to exit safely. **" );

                    while( !Server.IsShuttingDown ) {
                        string cmd = Console.ReadLine();
                        if( cmd.Equals( "/clear", StringComparison.OrdinalIgnoreCase ) ) {
                            Console.Clear();
                        } else {
                            try {
                                Player.Console.ParseMessage( cmd, true );
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "Error while executing a command from console", "ServerCLI", ex, false );
                            }
                        }
                    }

                } else {
                    ReportFailure( ShutdownReason.FailedToStart );
                }
#if !DEBUG
            } catch( Exception ex ) {
                ReportFailure( ShutdownReason.Crashed );
                Logger.LogAndReportCrash( "Unhandled exception in ServerCLI", "ServerCLI", ex, true );
            } finally {
                Console.ResetColor();
            }
#endif
        }


        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "fCraft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            Console.ResetColor();
            Server.Shutdown( new ShutdownParams( reason, 0, false, false ), true );
            if( !Server.HasArg( ArgKey.ExitOnCrash ) ) {
                Console.ReadLine();
            }
        }


        [DebuggerStepThrough]
        static void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            switch( e.MessageType ) {
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                case LogType.SeriousError:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                case LogType.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                default:
                    Console.WriteLine( e.Message );
                    return;
            }
        }


        static void OnHeartbeatUrlChanged( object sender, UrlChangedEventArgs e ) {
            File.WriteAllText( "externalurl.txt", e.NewUrl, Encoding.ASCII );
            Console.WriteLine( "** URL: {0} **", e.NewUrl );
            Console.WriteLine( "URL is also saved to file externalurl.txt" );
        }
    }
}