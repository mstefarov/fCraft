using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;


namespace AutoLauncher {
    class Program {
        const int Tick = 600000;

        static void Main( string[] args ) {
            Console.Title = "fCraftConsole AutoLauncher";
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.FileName = "fCraftConsole.exe";

            DateTime startTimer = DateTime.Now;
            TimeSpan oldCPUTime = new TimeSpan( 0 );
            Console.WriteLine( "{0} ==== STARTING ====", DateTime.Now );
            while( true ) {
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