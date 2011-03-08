// 
//  Authors:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010-2011, Tyler Kennedy & Matvei Stefarov
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
using System.Collections.Generic;
using System.IO;
using fCraft;

namespace Mcc {

    public sealed class MapFormatException : Exception {
        public MapFormatException() { }
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


        public static MapFormat Identify( string fileName ) {
            MapFormatType targetType = MapFormatType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapFormatType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();
            foreach( IMapConverter Converter in AvailableConverters.Values ) {
                try {
                    if( Converter.FormatType == targetType && Converter.ClaimsName( fileName ) ) {
                        if( Converter.Claims( fileName ) ) {
                            return Converter.Format;
                        }
                    } else {
                        fallbackConverters.Add( Converter );
                    }
                } catch( Exception ) { }
            }

            foreach( IMapConverter Converter in fallbackConverters ) {
                try {
                    if( Converter.Claims( fileName ) ) {
                        return Converter.Format;
                    }
                } catch( Exception ) { }
            }

            return MapFormat.Unknown;
        }



        public static Map TryLoading( string fileName ) {
            MapFormatType targetType = MapFormatType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapFormatType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();

            // first try all converters for the file extension
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                bool claims = false;
                try {
                    claims = (converter.FormatType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                } catch { }
                if( claims ) {
                    try {
                        return converter.Load( fileName );
                    } catch( Exception ex ) {
                        Logger.LogAndReportCrash( "Map failed to load", "Mcc", ex, false );
                        return null;
                    }
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            foreach( IMapConverter converter in fallbackConverters ) {
                try {
                    return converter.Load( fileName );
                } catch {}
            }

            return null;
        }


        public static bool TrySaving( Map mapToSave, string fileName, MapFormat format ) {
            if( AvailableConverters.ContainsKey( format ) ) {
                IMapConverter converter = AvailableConverters[format];
                try {
                    return converter.Save( mapToSave, fileName );
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Map failed to save", "Mcc", ex, false );
                    return false;
                }
            }
            throw new MapFormatException( "Unknown map format for saving." );
        }


        public static Map LoadHeader( string fileName ) {

            MapFormatType targetType = MapFormatType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapFormatType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();

            // first try all converters for the file extension
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                bool claims = false;
                try {
                    claims = (converter.FormatType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                } catch( Exception ) { }
                if( claims ) {
                    try {
                        return converter.LoadHeader( fileName );
                    } catch( NotImplementedException ) { }
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            foreach( IMapConverter converter in fallbackConverters ) {
                try {
                    return converter.LoadHeader( fileName );
                } catch( NotImplementedException ) { }
            }

            return null;
        }
    }
}