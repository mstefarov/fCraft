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
using System.Threading;
using System.IO;
using System.IO.Compression;
using fCraftUpdater.Properties;
using System.Reflection;
using System.Diagnostics;


namespace fCraftUpdater {
    static class Program {

        static void Main( string[] args ) {
            string restartTarget = null;

            string defaultPath = Path.GetFullPath( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) );
            Directory.SetCurrentDirectory( defaultPath );

            // parse args
            foreach( string arg in args ) {
                if( arg.StartsWith( "--path=" ) ) {
                    Directory.SetCurrentDirectory( arg.Substring( arg.IndexOf( '=' ) + 1 ) );
                } else if( arg.StartsWith( "--restart=" ) ) {
                    restartTarget = arg.Substring( arg.IndexOf( '=' ) + 1 );
                }
            }
            // TODO: parse all the paths, and pass them back to the restart callback



            using( MemoryStream ms = new MemoryStream( Resources.Payload ) ) {
                using( ZipStorer zs = ZipStorer.Open( ms, FileAccess.Read ) ) {

                    // ensure that fcraft files are writable
                    bool allPassed = false;
                    do {
                        allPassed = true;
                        foreach( var entry in zs.ReadCentralDir() ) {
                            try {
                                FileInfo fi = new FileInfo( entry.FilenameInZip );
                                if( !fi.Exists ) continue;
                                using( var testStream = fi.OpenWrite() ) { }
                            } catch( Exception ex ) {
                                if( ex is IOException ) {
                                    Console.WriteLine( "Waiting for fCraft-related applications to close..." );
                                } else {
                                    Console.WriteLine( "ERROR: could not write to {0}: {1} - {2}", entry.FilenameInZip, ex.GetType().Name, ex.Message );
                                    Console.WriteLine();
                                }
                                allPassed = false;
                                Thread.Sleep( 1000 );
                                break;
                            }
                        }
                    } while( !allPassed );

                    // extract files
                    foreach( var entry in zs.ReadCentralDir() ) {
                        Console.WriteLine( "Extracting {0}", entry.FilenameInZip );
                        try {
                            using( FileStream fs = File.Create( entry.FilenameInZip ) ) {
                                zs.ExtractFile( entry, fs );
                            }
                        } catch( Exception ex ) {
                            Console.WriteLine( "    ERROR: {0} {1}", ex.GetType().Name, ex.Message );
                        }
                    }
                }
            }

            Console.WriteLine( "Done." );
            Console.ReadLine();

            if( restartTarget != null ) {
                switch( Environment.OSVersion.Platform ) {
                    case PlatformID.MacOSX:
                    case PlatformID.Unix:
                        Process.Start( "mono " + restartTarget );
                        break;
                    default:
                        Process.Start( restartTarget );
                        break;
                }
            }
        }
    }
}