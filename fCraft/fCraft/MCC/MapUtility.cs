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
using System.Text;
using System.Collections.Generic;

namespace mcc {
    
    /// <summary>
    /// Static utility class for working with maps.
    /// </summary>
    public static class MapUtility {
        
        /// <summary>
        /// A dictionary of classes implementing the IConverter interface, and thus can be used to convert maps to
        /// and from our intermediate Map class
        /// </summary>
        private static Dictionary<MapFormats, IConverter> AvailableConverters = new Dictionary<MapFormats, IConverter>(  );

#region Utilities
        /// <summary>
        /// Static construtor use to populate Converters with available map formats.
        /// </summary>
        static MapUtility() {
            AvailableConverters.Add( MapFormats.MCSharp, new MapMCSharp(  ) );
            AvailableConverters.Add( MapFormats.FCMv2, new MapFCMv2(  ) );
            AvailableConverters.Add( MapFormats.MinerCPP, new MapMinerCPP(  ) );
            AvailableConverters.Add( MapFormats.NBT, new MapNBT(  ) );
        }

        /// <summary>
        /// Attempts to identify the type of map passed in MapStream.
        /// </summary>
        /// <param name="MapStream">
        /// A <see cref="Stream"/>
        /// </param>
        /// <returns>
        /// A <see cref="MapFormats"/>
        /// </returns>
        public static MapFormats Identify( Stream MapStream ) {
            foreach( IConverter Converter in AvailableConverters.Values ) {
                if( Converter.Claims( MapStream ) )
                    return Converter.Format;
            }
            return MapFormats.Unknown;
        }

        /// <summary>
        /// Attemps to get the map format (most commonly) used by the server
        /// or application passed in Server.
        /// </summary>
        /// <param name="Server">
        /// A <see cref="System.String"/>
        /// </param>
        /// <returns>
        /// A <see cref="MapFormats"/>
        /// </returns>
        public static MapFormats UsedBy( string Server ) {
            foreach( IConverter Converter in AvailableConverters.Values ) {
                foreach( string tmp in Converter.UsedBy ) {
                    if( tmp.ToLower( ) == Server.ToLower( ) ) {
                        return Converter.Format;   
                    }
                }
            }
            return MapFormats.Unknown;
        }

        /// <summary>
        /// Attempts to load a map from MapStream, throwing an exception if 
        /// the format is unrecognized.
        /// </summary>
        /// <param name="MapStream">
        /// A <see cref="Stream"/>
        /// </param>
        /// <returns>
        /// A <see cref="Map"/>
        /// </returns>
        public static Map TryLoading( Stream MapStream ) {
            foreach( IConverter Converter in AvailableConverters.Values ) {
                if( Converter.Claims( MapStream ) ) {
                    return Converter.Load( MapStream );
                }
            }
            throw new System.FormatException(  );
        }

        /// <summary>
        /// Attempts to save a map from MapStream, throwing an exception if
        /// the format is unrecognized.
        /// </summary>
        /// <param name="MapToSave">
        /// A <see cref="Map"/>
        /// </param>
        /// <param name="MapStream">
        /// A <see cref="Stream"/>
        /// </param>
        /// <param name="Format">
        /// A <see cref="MapFormats"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public static bool TrySaving( Map MapToSave, Stream MapStream, MapFormats Format ) {
            if( AvailableConverters.ContainsKey( Format ) ) {
                return AvailableConverters[Format].Save( MapToSave, MapStream );
            }
            throw new System.FormatException(  );
        }
#endregion
    }
}
