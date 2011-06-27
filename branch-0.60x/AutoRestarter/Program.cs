// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


namespace AutoRestarter {
    static class Program {
        const int Tick = 600000;
        const int Delay = 5000;
        const string BinaryName = "ServerCLI.exe";

        static void Main( string[] args ) {
            string argString = "--norestart --exitoncrash " + String.Join( " ", args );
            Console.Title = "fCraft AutoRestarter";

            if( !File.Exists( BinaryName ) ) {
                Console.WriteLine( "ERROR: {0} not found.", BinaryName );
                return;
            }

            Process p = new Process {
                StartInfo = {
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };

            switch( Environment.OSVersion.Platform ) {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    p.StartInfo.FileName = "mono-sgen";
                    p.StartInfo.Arguments = BinaryName + " " + argString;
                    break;
                default:
                    p.StartInfo.FileName = BinaryName;
                    p.StartInfo.Arguments = argString;
                    break;
            }

            Console.WriteLine( "{0} ==== STARTING ====", DateTime.Now ); // localized

            while( true ) {
                Thread.Sleep( Delay );
                p.Start();
                while( !p.HasExited ) {
                    p.WaitForExit( Tick );
                }
                Console.WriteLine( "{0} ==== SERVER SHUT DOWN, RESTARTING ====", DateTime.Now ); // localized
            }
        }
    }
}