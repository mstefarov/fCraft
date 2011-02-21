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

namespace fCraftConsole {

    static class Program {

        static void Main( string[] args ) {
            Server.InitLibrary( args );

            Server.OnLog += Log;
            Server.OnURLChanged += SetUrl;

#if !DEBUG
            try {
#endif
                if( Server.InitServer() ) {

                    UpdaterResult update = Updater.CheckForUpdates();
                    if( update.UpdateAvailable ) {
                        Console.WriteLine( "** A new version of fCraft is available: {0}, released {1:0} day(s) ago. **",
                                           update.GetVersionString(),
                                           DateTime.Now.Subtract( update.ReleaseDate ).TotalDays );
                    }

                    try {
                        Process.GetCurrentProcess().PriorityClass = Config.GetProcessPriority();
                    } catch( Exception ) {
                        Logger.Log( "Program.Main: Could not set process priority, using defaults.", LogType.Warning );
                    }

                    if( Server.StartServer() ) {
                        Console.Title = "fCraft " + Updater.GetVersionString() + " - " + Config.GetString( ConfigKey.ServerName );
                        Console.WriteLine( "** Running fCraft version {0}. **", Updater.GetVersionString() );
                        Console.WriteLine( "** Server is now ready. Type /shutdown to exit safely. **" );

                        while( true ) {
                            string cmd = Console.ReadLine();
                            if( cmd.Equals( "/clear", StringComparison.OrdinalIgnoreCase ) ) {
                                Console.Clear();
                            } else {
                                Player.Console.ParseMessage( cmd, true );
                            }
                        }

                    } else {
                        ReportFailure( "failed to start" );
                    }
                } else {
                    ReportFailure( "failed to initialize" );
                }
#if !DEBUG
            } catch( Exception ex ) {
                ReportFailure( "CRASHED" );
                Logger.LogAndReportCrash( "Unhandled exception in fCraftConsole", "fCraftConsole", ex );
            }
            Console.ReadLine();
            Console.ResetColor();
#endif
        }


        static void ReportFailure( string failureReason ) {
            Console.Title = String.Format( "fCraft {0} {1}", Updater.GetVersionString(), failureReason );
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( "** {0} **", failureReason );
            Server.ShutdownNow( new ShutdownParams {
                Reason = failureReason
            } );
            Console.ReadLine();
            Console.ResetColor();
        }


        static void Log( string message, LogType type ) {
            switch( type ) {
                case LogType.Error:
                case LogType.FatalError:
                case LogType.Warning:
                    Console.Error.WriteLine( message );
                    return;
                default:
                    Console.WriteLine( message );
                    return;
            }
        }

        static void SetUrl( string newUrl ) {
            File.WriteAllText( "externalurl.txt", newUrl, Encoding.ASCII );
            Console.WriteLine( "** URL: {0} **", newUrl );
            Console.WriteLine( "URL is also saved to file externalurl.txt" );
        }
    }
}