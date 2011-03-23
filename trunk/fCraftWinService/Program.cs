// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace fCraftWinService {
    static class Program {

        /// <summary>  
        /// Function that return .net framnework installation path.  
        /// </summary>  
        const int MaxPathLength = 256;

        [DllImport( "mscoree.dll", CharSet = CharSet.Unicode, ExactSpelling = true )]
        public static extern int GetCORSystemDirectory( StringBuilder buf, int cchBuf, ref int cchRequired );

        public static string GetNetFrameworkDirectory() {
            StringBuilder buf = new StringBuilder( MaxPathLength, MaxPathLength );
            int cch = MaxPathLength;
            int hr = GetCORSystemDirectory( buf, MaxPathLength, ref cch );
            if( hr < 0 ) Marshal.ThrowExceptionForHR( hr );
            return buf.ToString();
        }

        static void Main( string[] args ) {
            if( args.Length == 1 ) {
                Console.WriteLine( "Looking up .NET installation path..." );
                string installUtilPath = Path.Combine( GetNetFrameworkDirectory(), "installutil.exe" );
                if( !File.Exists( installUtilPath ) ) {
                    Console.WriteLine( "ERROR: Could not locate installutil.exe (part of Microsoft .NET)" );
                    return;
                }

                string currentProcessPath = Process.GetCurrentProcess().Modules[0].FileName;

                switch( args[0].ToLower() ) {
                    case "install":
                        Console.WriteLine( "Installing the service..." );
                        Process.Start( new ProcessStartInfo {
                            FileName = installUtilPath,
                            Arguments = currentProcessPath
                        } );
                        break;

                    case "uninstall":
                        Console.WriteLine( "Uninstalling the service..." );
                        Process.Start( new ProcessStartInfo {
                            FileName = installUtilPath,
                            Arguments = "-u " + currentProcessPath
                        } );
                        break;

                    case "start":
                        Console.WriteLine( "Starting the service..." );
                        Process.Start( new ProcessStartInfo {
                            FileName = "net",
                            Arguments = "start " + fCraftWinService.Name
                        } );
                        break;

                    case "stop":
                        Console.WriteLine( "Stopping the service..." );
                        Process.Start( new ProcessStartInfo {
                            FileName = "net",
                            Arguments = "stop " + fCraftWinService.Name
                        } );
                        break;
                }
            } else {
                ServiceBase.Run( new fCraftWinService() );
            }
        }
    }
}