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
using System.IO.Compression;
using System.Text;
using fCraft;


namespace Mcc {
    public sealed class MapFCMv2 : IMapConverter {
        [CLSCompliant( false )]
        public const uint Identifier = 0xfc000002;

        public bool ClaimsFileName( string fileName ) {
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }

        public MapFormat Format {
            get { return MapFormat.FCMv2; }
        }

        public string ServerName {
            get { return "fCraft"; }
        }


        public Map Load( Stream mapStream, string fileName ) {
            // Reset the seeker to the front of the stream
            // This should probably be done differently.
            mapStream.Seek( 0, SeekOrigin.Begin );

            Map map = new Map();

            BinaryReader reader = new BinaryReader( mapStream );

            // Read in the magic number
            if( reader.ReadUInt32() != Identifier ) {
                throw new FormatException();
            }

            // Read in the map dimesions
            map.widthX = reader.ReadInt16();
            map.widthY = reader.ReadInt16();
            map.height = reader.ReadInt16();

            // Read in the spawn location
            map.spawn.x = reader.ReadInt16();
            map.spawn.y = reader.ReadInt16();
            map.spawn.h = reader.ReadInt16();

            // Read in the spawn orientation
            map.spawn.r = reader.ReadByte();
            map.spawn.l = reader.ReadByte();

            // Read the metadata
            int metaSize = (int)reader.ReadUInt16();

            for( int i = 0; i < metaSize; i++ ) {
                string key = ReadLengthPrefixedString( reader );
                string value = ReadLengthPrefixedString( reader );
                if( key.StartsWith( "@zone", StringComparison.OrdinalIgnoreCase ) ) {
                    try {
                        map.AddZone( new Zone( value, map.world ) );
                    } catch( Exception ex ) {
                        Logger.Log( "MapFCMv2.Load: Error importing zone definition: {0}", LogType.Error, ex );
                    }
                } else {
                    map.SetMeta( key, value );
                }
            }

            if( !map.ValidateHeader() ) {
                throw new MapFormatException( "MapFCMv2.Load: One or more of the map dimensions are invalid." );
            }

            // Read in the map data
            map.blocks = new Byte[map.GetBlockCount()];
            using( GZipStream decompressor = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                decompressor.Read( map.blocks, 0, map.blocks.Length );
            }

            return map;
        }


        static string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = reader.ReadInt32();
            byte[] stringData = reader.ReadBytes( length );
            return ASCIIEncoding.ASCII.GetString( stringData );
        }


        public bool Save( Map mapToSave, Stream mapStream ) {
            throw new NotImplementedException();
        }


        public bool Claims( Stream mapStream, string fileName ) {
            try {
                mapStream.Seek( 0, SeekOrigin.Begin );
                BinaryReader reader = new BinaryReader( mapStream );
                return reader.ReadUInt32() == Identifier;
            } catch( Exception ) {
                return false;
            }

        }
    }
}