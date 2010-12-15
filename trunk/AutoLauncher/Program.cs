// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


namespace AutoLauncher {
    class Program {
        const int Tick = 600000;
        const int Delay = 5000;
        const string BinaryName = "fCraftConsole.exe";

        static void Main( string[] args ) {
            Console.Title = "fCraftConsole AutoLauncher";

            if( !File.Exists( BinaryName ) ) {
                Console.WriteLine( "ERROR: {0} not found.", BinaryName );
                Console.ReadLine();
            }

            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.FileName = BinaryName;

            DateTime startTimer = DateTime.Now;
            TimeSpan oldCPUTime = new TimeSpan( 0 );
            Console.WriteLine( "{0} ==== STARTING ====", DateTime.Now );

            while( true ) {
                Thread.Sleep( Delay );
                p.Start();
                oldCPUTime = new TimeSpan( 0 );
                while( !p.HasExited ) {
                    try {
                        TimeSpan newCPUTime = p.TotalProcessorTime;
                        Console.WriteLine( "{0} Server UP, uptime {1:0.0}h: {2}% avg CPU",
                                            DateTime.Now,
                                            DateTime.Now.Subtract( startTimer ).TotalHours,
                                            (newCPUTime - oldCPUTime).TotalMilliseconds / (System.Environment.ProcessorCount * Tick) );
                        oldCPUTime = newCPUTime;
                    } catch( Exception ) { }
                    p.WaitForExit( Tick );
                }
                Console.WriteLine( "{0} ==== SERVER SHUT DOWN, RESTARTING ====", DateTime.Now );
            }
        }
    }
}