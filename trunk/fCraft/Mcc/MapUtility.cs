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
using System.Linq;
using fCraft;


namespace Mcc {

    public sealed class MapFormatException : Exception {
        public MapFormatException() : base() { }
        public MapFormatException( string message ) : base( message ) { }
    }

    public static class MapUtility {

        static Dictionary<MapFormat, IMapConverter> AvailableConverters = new Dictionary<MapFormat, IMapConverter>();

        static MapUtility() {
            AvailableConverters.Add( MapFormat.MCSharp, new MapMCSharp() );
            AvailableConverters.Add( MapFormat.FCMv2, new MapFCMv2() );
            AvailableConverters.Add( MapFormat.FCMv3, new MapFCMv3() );
            AvailableConverters.Add( MapFormat.MinerCPP, new MapMinerCPP() );
            AvailableConverters.Add( MapFormat.NBT, new MapNBT() );
            AvailableConverters.Add( MapFormat.Creative, new MapDAT() );
            AvailableConverters.Add( MapFormat.JTE, new MapJTE() );
            AvailableConverters.Add( MapFormat.D3, new MapD3() );
            AvailableConverters.Add( MapFormat.Myne, new MapMyne() );
        }


        public static MapFormat Identify( Stream mapStream, string fileName ) {
            foreach( IMapConverter Converter in AvailableConverters.Values ) {
                if( Converter.Claims( mapStream, fileName ) )
                    return Converter.Format;
                mapStream.Seek( 0, SeekOrigin.Begin );
            }
            return MapFormat.Unknown;
        }



        public static Map TryLoading( string fileName ) {
            if( File.Exists( fileName ) ) {
                using( Stream mapStream = File.OpenRead( fileName ) ) {
                    string shortFileName = new FileInfo( fileName ).Name;
                    // first try all converters for the file extension
                    foreach( IMapConverter Converter in AvailableConverters.Values ) {
                        if( Converter.ClaimsFileName( shortFileName ) && Converter.Claims( mapStream, fileName ) ) {
                            mapStream.Seek( 0, SeekOrigin.Begin );
                            return Converter.Load( mapStream, fileName );
                        }
                        mapStream.Seek( 0, SeekOrigin.Begin );
                    }
                    // then try the rest
                    foreach( IMapConverter Converter in AvailableConverters.Values ) {
                        if( !Converter.ClaimsFileName( shortFileName ) && Converter.Claims( mapStream, fileName ) ) {
                            mapStream.Seek( 0, SeekOrigin.Begin );
                            return Converter.Load( mapStream, fileName );
                        }
                        mapStream.Seek( 0, SeekOrigin.Begin );
                    }
                }
                // if all else fails
                throw new MapFormatException( "Unknown map format for loading." );

            } else if( Directory.Exists( fileName ) ) {
                return AvailableConverters[MapFormat.Myne].Load( null, fileName );

            } else {
                throw new FileNotFoundException();
            }
        }


        public static bool TrySaving( Map mapToSave, Stream mapStream, MapFormat format ) {
            if( AvailableConverters.ContainsKey( format ) ) {
                return AvailableConverters[format].Save( mapToSave, mapStream );
            }
            throw new MapFormatException( "Unknown map format for saving." );
        }
    }
}