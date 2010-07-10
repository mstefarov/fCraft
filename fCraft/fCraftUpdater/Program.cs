/*
 *  Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
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


namespace fCraftUpdater {
    static class Program {
        static string ExtractorFile = "UpdateExtractor.exe";
        static string[] fileList = { "fCraft.dll", "fCraftUI.exe", "ConfigTool.exe" };

        static void Main( string[] args ) {
            int pid;
            // extract updater
            Console.WriteLine( "Preparing to extract..." );
            File.WriteAllBytes( ExtractorFile, fCraftUpdater.Properties.Resources.Extractor );

            // wait for fCraft to close, if needed
            if( args.Length == 1 && Int32.TryParse( args[0], out pid ) ) {
                Console.WriteLine( "Waiting for fCraft to close..." );
                try {
                    Process.GetProcessById( pid ).WaitForExit();
                } catch( Exception ex ) {
                    Console.WriteLine( "Cound not locate fCraft process: " + ex.Message );
                }
            }

            // ensure that fcraft files are writable
            foreach( string file in fileList ) {
                try {
                    File.SetLastAccessTime( file, File.GetLastAccessTime( file ) );
                } catch( Exception ex ) {
                    Console.WriteLine( "Cound not write to " + file + ": " + ex.Message );
                }
            }

            // extract files
            Console.WriteLine( "Extracting..." );
            Process extractionProcess = Process.Start( ExtractorFile );
            extractionProcess.WaitForExit();

            // clean up
            File.Delete( ExtractorFile );
            Console.WriteLine( "Done." );

            Process.Start( "fCraftUI.exe" );
        }
    }
}
