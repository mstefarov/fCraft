using System;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace fCraftWinService {
    class Program {

        /// <summary>  
        /// Function that return .net framnework installation path.  
        /// </summary>  
        const int MAX_PATH = 256;

        [DllImport( "mscoree.dll", CharSet = CharSet.Unicode, ExactSpelling = true )]
        public static extern int GetCORSystemDirectory( StringBuilder buf, int cchBuf, ref int cchRequired );

        public static string GetNetFrameworkDirectory() {
            StringBuilder buf = new StringBuilder( MAX_PATH, MAX_PATH );
            int cch = MAX_PATH;
            int hr = GetCORSystemDirectory( buf, MAX_PATH, ref cch );
            if( hr < 0 ) Marshal.ThrowExceptionForHR( hr );
            return buf.ToString();
        }

        static void Main( string[] args ) {
            if( args.Length == 1 ) {
                Console.WriteLine( "Looking up .NET installation path..." );
                string InstallUtilPath = Path.Combine( GetNetFrameworkDirectory(), "installutil.exe" );
                if( !File.Exists( InstallUtilPath ) ) {
                    Console.WriteLine( "ERROR: Could not locate installutil.exe (part of Microsoft .NET)" );
                    return;
                }

                string CurrentProcessPath = Process.GetCurrentProcess().Modules[0].FileName;

                switch( args[0].ToLower() ) {
                    case "install":
                        Console.WriteLine( "Installing the service..." );
                        Process.Start( new ProcessStartInfo {
                            FileName = InstallUtilPath,
                            Arguments = CurrentProcessPath
                        } );
                        break;

                    case "uninstall":
                        Console.WriteLine( "Uninstalling the service..." );
                        Process.Start( new ProcessStartInfo {
                            FileName = InstallUtilPath,
                            Arguments = "-u " + CurrentProcessPath
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
