// 
//  Authors:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010, Tyler Kennedy & Matvei Stefarov
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this
//       list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of MCC nor the names of its contributors may be
//       used to endorse or promote products derived from this software without
//       specific prior written permission.
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
using System.Text;
using System.Collections.Generic;
using fCraft;


namespace Mcc {

    public static class MapUtility {

        private static Dictionary<MapFormat, IConverter> AvailableConverters = new Dictionary<MapFormat, IConverter>();


        static MapUtility() {
            AvailableConverters.Add( MapFormat.MCSharp, new MapMCSharp() );
            AvailableConverters.Add( MapFormat.FCMv2, new MapFCMv2() );
            AvailableConverters.Add( MapFormat.MinerCPP, new MapMinerCPP() );
            AvailableConverters.Add( MapFormat.NBT, new MapNBT() );
            AvailableConverters.Add( MapFormat.Creative, new MapDAT() );
            AvailableConverters.Add( MapFormat.JTE, new MapJTE() );
        }


        public static MapFormat Identify( Stream mapStream ) {
            foreach ( IConverter Converter in AvailableConverters.Values ) {
                if ( Converter.Claims( mapStream ) )
                    return Converter.Format;
            }
            return MapFormat.Unknown;
        }



        public static Map TryLoading( string fileName ) {
            Stream MapStream = File.OpenRead( fileName );
            string ext = new FileInfo( fileName ).Extension;
            // first try all converters for the file extension
            foreach ( IConverter Converter in AvailableConverters.Values ) {
                if( Converter.FileExtension == ext  && Converter.Claims( MapStream ) ) {
                    return Converter.Load( MapStream );
                }
            }
            // then try the rest
            foreach( IConverter Converter in AvailableConverters.Values ) {
                if( Converter.FileExtension != ext && Converter.Claims( MapStream ) ) {
                    return Converter.Load( MapStream );
                }
            }
            // if all else fails
            throw new FormatException();
        }



        public static string GetFileExtension( MapFormat format ) {
            return AvailableConverters[format].FileExtension;
        }

        public static bool TrySaving( Map mapToSave, Stream mapStream, MapFormat format ) {
            if ( AvailableConverters.ContainsKey( format ) ) {
                return AvailableConverters[format].Save( mapToSave, mapStream );
            }
            throw new FormatException();
        }
    }
}