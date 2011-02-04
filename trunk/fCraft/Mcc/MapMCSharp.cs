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
using fCraft;

namespace Mcc {
    public sealed class MapMCSharp : IMapConverter {

        static byte[] mapping = new byte[256];

        static MapMCSharp() {
            mapping[100] = (byte)Block.Glass;
            mapping[101] = (byte)Block.Obsidian;
            mapping[102] = (byte)Block.Brick;
            mapping[103] = (byte)Block.Stone;
            mapping[104] = (byte)Block.Rocks;
            mapping[106] = (byte)Block.Water;

            mapping[110] = (byte)Block.Wood;
            mapping[111] = (byte)Block.Log;
            mapping[112] = (byte)Block.Lava;
            mapping[113] = (byte)Block.Obsidian;
            mapping[114] = (byte)Block.Glass;
            // all others default to 0/air
        }


        public string ServerName {
            get { return "MCSharp/MCZall/MCLawl"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.MCSharp; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".lvl", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    mapStream.Seek( 0, SeekOrigin.Begin );
                    GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                    BinaryReader bs = new BinaryReader( gs );
                    return (bs.ReadUInt16() == 0x752);
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    BinaryReader bs = new BinaryReader( gs );

                    Map map = new Map();

                    // Read in the magic number
                    if( bs.ReadUInt16() != 0x752 ) {
                        throw new MapFormatException();
                    }

                    // Read in the map dimesions
                    map.widthX = bs.ReadInt16();
                    map.widthY = bs.ReadInt16();
                    map.height = bs.ReadInt16();

                    return map;
                }
            }
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    BinaryReader bs = new BinaryReader( gs );

                    Map map = new Map();

                    // Read in the magic number
                    if( bs.ReadUInt16() != 0x752 ) {
                        throw new MapFormatException();
                    }

                    // Read in the map dimesions
                    map.widthX = bs.ReadInt16();
                    map.widthY = bs.ReadInt16();
                    map.height = bs.ReadInt16();

                    if( !map.ValidateHeader() ) {
                        throw new MapFormatException( "MapFCMv3.Load: One or more of the map dimensions are invalid." );
                    }

                    // Read in the spawn location
                    map.spawn.x = (short)(bs.ReadInt16() * 32);
                    map.spawn.h = (short)(bs.ReadInt16() * 32);
                    map.spawn.y = (short)(bs.ReadInt16() * 32);

                    // Read in the spawn orientation
                    map.spawn.r = bs.ReadByte();
                    map.spawn.l = bs.ReadByte();

                    // Skip over the VisitPermission and BuildPermission bytes
                    bs.ReadByte();
                    bs.ReadByte();

                    // Read in the map data
                    map.blocks = bs.ReadBytes( map.GetBlockCount() );

                    for( int i = 0; i < map.blocks.Length; i++ ) {
                        if( map.blocks[i] > 49 ) {
                            map.blocks[i] = mapping[map.blocks[i]];
                        }
                    }

                    return map;
                }
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write the magic number
                    bs.Write( (ushort)0x752 );

                    // Write the map dimensions
                    bs.Write( mapToSave.widthX );
                    bs.Write( mapToSave.height );
                    bs.Write( mapToSave.widthY );

                    // Write the spawn location
                    bs.Write( mapToSave.spawn.x / 32 );
                    bs.Write( mapToSave.spawn.h / 32 );
                    bs.Write( mapToSave.spawn.y / 32 );

                    //Write the spawn orientation
                    bs.Write( mapToSave.spawn.r );
                    bs.Write( mapToSave.spawn.l );

                    // Write the VistPermission and BuildPermission bytes
                    bs.Write( (byte)0 );
                    bs.Write( (byte)0 );

                    // Write the map data
                    bs.Write( mapToSave.blocks, 0, mapToSave.blocks.Length );

                    bs.Close();
                }
                return true;
            }
        }
    }
}