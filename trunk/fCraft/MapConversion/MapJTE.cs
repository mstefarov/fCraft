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
using System.IO;
using System.IO.Compression;
using System.Net;

namespace fCraft.MapConversion {
    public sealed class MapJTE : IMapConverter {

        static readonly byte[] Mapping = new byte[256];

        static MapJTE() {
            Mapping[255] = (byte)Block.Sponge;      // lava sponge
            Mapping[254] = (byte)Block.TNT;         // dynamite
            Mapping[253] = (byte)Block.Sponge;      // supersponge
            Mapping[252] = (byte)Block.Water;       // watervator
            Mapping[251] = (byte)Block.White;       // soccer
            Mapping[250] = (byte)Block.Red;         // fire
            Mapping[249] = (byte)Block.Red;         // badfire
            Mapping[248] = (byte)Block.Red;         // hellfire
            Mapping[247] = (byte)Block.Black;       // ashes
            Mapping[246] = (byte)Block.Orange;      // torch
            Mapping[245] = (byte)Block.Orange;      // safetorch
            Mapping[244] = (byte)Block.Orange;      // helltorch
            Mapping[243] = (byte)Block.Red;         // uberfire
            Mapping[242] = (byte)Block.Red;         // godfire
            Mapping[241] = (byte)Block.TNT;         // nuke
            Mapping[240] = (byte)Block.Lava;        // lavavator
            Mapping[239] = (byte)Block.Admincrete;  // instawall
            Mapping[238] = (byte)Block.Admincrete;  // spleef
            Mapping[237] = (byte)Block.Green;       // resetspleef
            Mapping[236] = (byte)Block.Red;         // deletespleef
            Mapping[235] = (byte)Block.Sponge;      // godsponge
            // all others default to 0/air
        }


        public string ServerName {
            get { return "JTE's"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.JTE; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".gz", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    mapStream.Seek( 0, SeekOrigin.Begin );
                    GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress );
                    BinaryReader bs = new BinaryReader( gs );
                    byte version = bs.ReadByte();
                    return (version == 1 || version == 2);
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                // Setup a GZipStream to decompress and read the map file
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress );
                BinaryReader bs = new BinaryReader( gs );

                Map map = new Map();

                byte version = bs.ReadByte();
                if( version != 1 && version != 2 ) throw new MapFormatException();

                // Read in the spawn orientation
                mapStream.Seek( 8, SeekOrigin.Current );

                // Read in the map dimesions
                map.WidthX = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                map.WidthY = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                map.Height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                return map;
            }
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                // Setup a GZipStream to decompress and read the map file
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress );
                BinaryReader bs = new BinaryReader( gs );

                Map map = new Map();

                byte version = bs.ReadByte();
                if( version != 1 && version != 2 ) throw new MapFormatException();

                // Read in the spawn location
                map.Spawn.X = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() ) * 32);
                map.Spawn.H = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() ) * 32);
                map.Spawn.Y = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() ) * 32);

                // Read in the spawn orientation
                map.Spawn.R = bs.ReadByte();
                map.Spawn.L = bs.ReadByte();

                // Read in the map dimesions
                map.WidthX = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                map.WidthY = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                map.Height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "MapFCMv3.Load: One or more of the map dimensions are invalid." );
                }

                // Read in the map data
                map.Blocks = bs.ReadBytes( map.GetBlockCount() );

                for( int i = 0; i < map.Blocks.Length; i++ ) {
                    if( map.Blocks[i] > 49 ) {
                        map.Blocks[i] = Mapping[map.Blocks[i]];
                    }
                }

                return map;
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write the magic number
                    bs.Write( (byte)0x01 );

                    // Write the spawn location
                    bs.Write( IPAddress.NetworkToHostOrder( (short)(mapToSave.Spawn.X / 32) ) );
                    bs.Write( IPAddress.NetworkToHostOrder( (short)(mapToSave.Spawn.H / 32) ) );
                    bs.Write( IPAddress.NetworkToHostOrder( (short)(mapToSave.Spawn.Y / 32) ) );

                    //Write the spawn orientation
                    bs.Write( mapToSave.Spawn.R );
                    bs.Write( mapToSave.Spawn.L );

                    // Write the map dimensions
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.WidthX ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.WidthY ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Height ) );

                    // Write the map data
                    bs.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );
                }
                return true;
            }
        }
    }
}