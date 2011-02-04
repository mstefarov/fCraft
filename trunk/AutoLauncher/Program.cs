// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


namespace AutoLauncher {
    class Program {
        const int Tick = 600000;
        const int Delay = 5000;
        const string BinaryName = "fCraftConsole.exe";

        static void Main() {
            Console.Title = "fCraftConsole AutoLauncher";

            if( !File.Exists( BinaryName ) ) {
                Console.WriteLine( "ERROR: {0} not found.", BinaryName );
                return;
            }

            Process p = new Process {
                StartInfo = {
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    FileName = BinaryName
                }
            };

            DateTime startTimer = DateTime.Now;
            Console.WriteLine( "{0} ==== STARTING ====", DateTime.Now );

            while( true ) {
                Thread.Sleep( Delay );
                p.Start();
                TimeSpan oldCPUTime = TimeSpan.Zero;
                while( !p.HasExited ) {
                    try {
                        TimeSpan newCPUTime = p.TotalProcessorTime;
                        Console.WriteLine( "{0} Server UP, uptime {1:0.0}h: {2}% avg CPU",
                                           DateTime.Now,
                                           DateTime.Now.Subtract( startTimer ).TotalHours,
                                           (newCPUTime - oldCPUTime).TotalMilliseconds / (Environment.ProcessorCount * Tick) );
                        oldCPUTime = newCPUTime;
                    } catch { }
                    p.WaitForExit( Tick );
                }
                Console.WriteLine( "{0} ==== SERVER SHUT DOWN, RESTARTING ====", DateTime.Now );
            }
        }
    }
}