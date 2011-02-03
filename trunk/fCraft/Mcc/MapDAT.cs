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
using fCraft;

namespace Mcc {
    public sealed class MapDAT : IMapConverter {

        static byte[] mapping = new byte[256];

        static MapDAT() {
            mapping[50] = (byte)Block.Air;      // torch
            mapping[51] = (byte)Block.Lava;     // fire
            mapping[52] = (byte)Block.Glass;    // spawner
            mapping[53] = (byte)Block.Stair;    // wood stairs
            mapping[54] = (byte)Block.Wood;     // chest
            mapping[55] = (byte)Block.Air;      // redstone wire
            mapping[56] = (byte)Block.IronOre;  // diamond ore
            mapping[57] = (byte)Block.Aqua;     // diamond block
            mapping[58] = (byte)Block.Log;      // workbench
            mapping[59] = (byte)Block.Leaves;   // crops
            mapping[60] = (byte)Block.Dirt;     // soil
            mapping[61] = (byte)Block.Stone;    // furnace
            mapping[62] = (byte)Block.Stone;    // burning furnance
            mapping[63] = (byte)Block.Air;      // sign post
            mapping[64] = (byte)Block.Air;      // wooden door
            mapping[65] = (byte)Block.Air;      // ladder
            mapping[66] = (byte)Block.Air;      // rails
            mapping[67] = (byte)Block.Stair;    // cobblestone stairs
            mapping[68] = (byte)Block.Air;      // wall sign
            mapping[69] = (byte)Block.Air;      // lever
            mapping[70] = (byte)Block.Air;      // pressure plate
            mapping[71] = (byte)Block.Air;      // iron door
            mapping[72] = (byte)Block.Air;      // wooden pressure plate
            mapping[73] = (byte)Block.IronOre;  // redstone ore
            mapping[74] = (byte)Block.IronOre;  // glowing redstone ore
            mapping[75] = (byte)Block.Air;      // redstone torch (off)
            mapping[76] = (byte)Block.Air;      // redstone torch (on)
            mapping[77] = (byte)Block.Air;      // stone button
            mapping[78] = (byte)Block.Air;      // snow
            mapping[79] = (byte)Block.Glass;    // ice
            mapping[80] = (byte)Block.White;    // snow block
            mapping[81] = (byte)Block.Leaves;   // cactus
            mapping[82] = (byte)Block.Gray;     // clay
            mapping[83] = (byte)Block.Leaves;   // reed
            mapping[84] = (byte)Block.Log;      // jukebox
            mapping[85] = (byte)Block.Wood;     // fence
            mapping[86] = (byte)Block.Orange;   // pumpkin
            mapping[87] = (byte)Block.Dirt;     // netherstone
            mapping[88] = (byte)Block.Gravel;   // slow sand
            mapping[89] = (byte)Block.Sand;     // lightstone
            mapping[90] = (byte)Block.Violet;   // portal
            mapping[91] = (byte)Block.Orange;   // jack-o-lantern
            // all others default to 0/air
        }


        public string ServerName {
            get { return "Creative/Vanilla"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.Creative; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".dat", StringComparison.OrdinalIgnoreCase ) ||
                   fileName.EndsWith( ".mine", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    byte[] temp = new byte[8];
                    byte[] data;
                    int length;
                    mapStream.Seek( -4, SeekOrigin.End );
                    mapStream.Read( temp, 0, sizeof( int ) );
                    mapStream.Seek( 0, SeekOrigin.Begin );
                    length = BitConverter.ToInt32( temp, 0 );
                    data = new byte[length];
                    using( GZipStream reader = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                        reader.Read( data, 0, length );
                    }

                    for( int i = 0; i < length - 1; i++ ) {
                        if( data[i] == 0xAC && data[i + 1] == 0xED ) {
                            return true;
                        }
                    }
                    return false;
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            throw new NotImplementedException();
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                byte[] temp = new byte[8];
                Map map = new Map();
                byte[] data;
                int length;

                try {
                    mapStream.Seek( -4, SeekOrigin.End );
                    mapStream.Read( temp, 0, sizeof( int ) );
                    mapStream.Seek( 0, SeekOrigin.Begin );
                    length = BitConverter.ToInt32( temp, 0 );
                    data = new byte[length];
                    using( GZipStream reader = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                        reader.Read( data, 0, length );
                    }

                    for( int i = 0; i < length - 1; i++ ) {
                        if( data[i] == 0xAC && data[i + 1] == 0xED ) {

                            // bypassing the header crap
                            int pointer = i + 6;
                            Array.Copy( data, pointer, temp, 0, sizeof( short ) );
                            pointer += IPAddress.HostToNetworkOrder( BitConverter.ToInt16( temp, 0 ) );
                            pointer += 13;

                            int headerEnd = 0;
                            // find the end of serialization listing
                            for( headerEnd = pointer; headerEnd < data.Length - 1; headerEnd++ ) {
                                if( data[headerEnd] == 0x78 && data[headerEnd + 1] == 0x70 ) {
                                    headerEnd += 2;
                                    break;
                                }
                            }

                            // start parsing serialization listing
                            int offset = 0;
                            while( pointer < headerEnd ) {
                                if( data[pointer] == 'Z' ) offset++;
                                else if( data[pointer] == 'I' || data[pointer] == 'F' ) offset += 4;
                                else if( data[pointer] == 'J' ) offset += 8;

                                pointer += 1;
                                Array.Copy( data, pointer, temp, 0, sizeof( short ) );
                                short skip = IPAddress.HostToNetworkOrder( BitConverter.ToInt16( temp, 0 ) );
                                pointer += 2;

                                // look for relevant variables
                                Array.Copy( data, headerEnd + offset - 4, temp, 0, sizeof( int ) );
                                if( MemCmp( data, pointer, "width" ) ) {
                                    map.widthX = (ushort)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) );
                                } else if( MemCmp( data, pointer, "depth" ) ) {
                                    map.height = (ushort)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) );
                                } else if( MemCmp( data, pointer, "height" ) ) {
                                    map.widthY = (ushort)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) );
                                } else if( MemCmp( data, pointer, "xSpawn" ) ) {
                                    map.spawn.x = (short)(IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
                                } else if( MemCmp( data, pointer, "ySpawn" ) ) {
                                    map.spawn.h = (short)(IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
                                } else if( MemCmp( data, pointer, "zSpawn" ) ) {
                                    map.spawn.y = (short)(IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
                                }

                                pointer += skip;
                            }

                            if( !map.ValidateHeader() ) {
                                throw new MapFormatException( "MapDAT.Load: One or more of the map dimensions are invalid." );
                            }

                            // find the start of the block array
                            bool foundBlockArray = false;
                            offset = Array.IndexOf<byte>( data, 0x00, headerEnd );
                            while( offset != -1 && offset < data.Length - 2 ) {
                                if( data[offset] == 0x00 && data[offset + 1] == 0x78 && data[offset + 2] == 0x70 ) {
                                    foundBlockArray = true;
                                    pointer = offset + 7;
                                }
                                offset = Array.IndexOf<byte>( data, 0x00, offset + 1 );
                            }

                            // copy the block array... or fail
                            if( foundBlockArray ) {
                                map.CopyBlocks( data, pointer );
                                for( int j = 0; j < map.blocks.Length; j++ ) {
                                    if( map.blocks[j] > 49 ) {
                                        map.blocks[j] = mapping[map.blocks[j]];
                                    }
                                }
                            } else {
                                throw new MapFormatException( "Could not locate block array." );
                            }
                            break;
                        }
                    }
                    return map;

                } catch( Exception ex ) {
                    Logger.Log( "Conversion failed: {0}", LogType.Error, ex );
                    return null;
                }
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            throw new NotImplementedException();
        }


        static bool MemCmp( byte[] data, int offset, string value ) {
            for( int i = 0; i < value.Length; i++ ) {
                if( offset + i >= data.Length || data[offset + i] != value[i] ) return false;
            }
            return true;
        }
    }
}