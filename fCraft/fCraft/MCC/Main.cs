// 
//  Author:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010, Tyler Kennedy & Matvei Stefarov
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.IO;
using System.Collections.Generic;

// TODO: Pring usage information
// -in:<file>           -- Opens <file> for reading
// -out:<file>          -- Opens <file> for saving
// -force               -- Force overwriting of -out if the file exists
// -convert:<format>    -- Saves -in to -out as <format>
// -dump                -- Dumps input file information

namespace mcc {
    class MainClass {
        public static void Main( string[] args ) {
            // Field for final parsed command line arguments
            Dictionary<string, string> Options = new Dictionary<string, string>(  );
            // Holders for the -in and -out arguments
            FileStream fout = null, fin = null;
            // Currently working copy of the map
            Map WorkingMap = null;
            
            // Iterate over each argument passed on the
            // command line.
            foreach( string Option in args ) {
                // Make sure its an option by checking for a
                // '-' character at the start.
                if( Option[0] != '-' ) {
                    // Inform the user that this isn't a valid argument.
                    // TODO: Print usage information.
                    Console.WriteLine( "({0}) isn't a valid parameter.", Option );
                    // Leave the application
                    return;
                }
                
                // Find the first occurence of ':' in the argument
                // (or lack there-of)
                int index = Option.IndexOf( ':' );
                
                // ':' never occurs in the argument
                if( index == -1 ) {
                    Options.Add( Option.Substring( 1, Option.Length - 1 ), "" );
                } else {
                    // There's at least one argument
                    Options.Add( Option.Substring( 1, index - 1 ), Option.Substring( index + 1 ) );
                }
            }
#region Base Arguments
            // Check to see if the user specified an (in)put map
            if( Options.ContainsKey( "in" ) ) {
                // Attempt to open the path specified
                try {
                    fin = new FileStream( Options["in"], FileMode.Open );
                } catch( FileNotFoundException Ex ) {
                    // The file didn't exist, so notify the user and exit
                    Console.WriteLine( "The file - {0} - doesn't exist, unable to open.", Options["in"] );
                    return;
                }
            }
            
            // Check to see if the user specified an (out)put map
            if( Options.ContainsKey( "out" ) ) {
                try {
                    if( !Options.ContainsKey( "force" ) ) {
                        fout = new FileStream( Options["out"], FileMode.CreateNew );
                    } else {
                        fout = new FileStream( Options["out"], FileMode.Create );
                    }
                } catch( IOException Ex ) {
                    // The file already existed and the user didn't specify -force
                    Console.WriteLine( "The file - {0} - already exists, use -force to overwrite it.", Options["out"] );
                    return;
                }
            }
#endregion
            
#region Extended Arguments
            // Check to see if the user wants to (convert) a map
            if( Options.ContainsKey( "convert" ) ) {
                // Check to make sure we have an in and an out target
                if( fin == null || fout == null ) {
                    // Nope.
                    Console.WriteLine( "-convert requires both an (-in)put map and an (-out)put path." );
                    return;
                }
                
                // Try to load in the source file
                try {
                    WorkingMap = MapUtility.TryLoading( fin );
                } catch( FormatException Ex ) {
                    // Thrown if we couldn't identify what kind of map this is
                    Console.WriteLine( "We don't support this type of file." );
                    return;
                }
                
                // Try saving to disk
                MapUtility.TrySaving( WorkingMap, fout, MapUtility.UsedBy( Options["convert"] ) );
            }
#endregion
            
            // Check to see if a map object exists
            if( WorkingMap != null ) {
                // Iterate over each log entry and output it
                foreach( string LogEntry in WorkingMap.ProcessLog ) {
                    Console.WriteLine( LogEntry );
                }
            }
        }
    }
}
