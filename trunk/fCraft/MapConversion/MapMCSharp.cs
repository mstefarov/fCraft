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

namespace fCraft.MapConversion {
    public sealed class MapMCSharp : IMapConverter {

        static byte[] mapping = new byte[256];

        static MapMCSharp() {
            mapping[100] = (byte)Block.Glass;       // op_glass
            mapping[101] = (byte)Block.Obsidian;    // opsidian
            mapping[102] = (byte)Block.Brick;       // op_brick
            mapping[103] = (byte)Block.Stone;       // op_stone
            mapping[104] = (byte)Block.Rocks;       // op_cobblestone
            // 105 = op_air
            mapping[106] = (byte)Block.Water;       // op_water

            // 107-109 unused
            mapping[110] = (byte)Block.Wood;        // wood_float
            mapping[111] = (byte)Block.Log;         // door
            mapping[112] = (byte)Block.Lava;        // lava_fast
            mapping[113] = (byte)Block.Obsidian;    // door2
            mapping[114] = (byte)Block.Glass;       // door3
            mapping[115] = (byte)Block.Stone;       // door4
            mapping[116] = (byte)Block.Leaves;      // door5
            mapping[117] = (byte)Block.Sand;        // door6
            mapping[118] = (byte)Block.Wood;        // door7
            mapping[119] = (byte)Block.Green;       // door8
            mapping[120] = (byte)Block.TNT;         // door9
            mapping[121] = (byte)Block.Stair;       // door10

            mapping[122] = (byte)Block.Log;         // tdoor
            mapping[123] = (byte)Block.Obsidian;    // tdoor2
            mapping[124] = (byte)Block.Glass;       // tdoor3
            mapping[125] = (byte)Block.Stone;       // tdoor4
            mapping[126] = (byte)Block.Leaves;      // tdoor5
            mapping[127] = (byte)Block.Sand;        // tdoor6
            mapping[128] = (byte)Block.Wood;        // tdoor7
            mapping[129] = (byte)Block.Green;       // tdoor8

            mapping[130] = (byte)Block.White;       // MsgWhite
            mapping[131] = (byte)Block.Black;       // MsgBlack
            mapping[132] = (byte)Block.Air;         // MsgAir
            mapping[133] = (byte)Block.Water;       // MsgWater
            mapping[134] = (byte)Block.Lava;        // MsgLava

            mapping[135] = (byte)Block.TNT;         // tdoor9
            mapping[136] = (byte)Block.Stair;       // tdoor10
            mapping[137] = (byte)Block.Air;         // tdoor11
            mapping[138] = (byte)Block.Water;       // tdoor12
            mapping[139] = (byte)Block.Lava;        // tdoor13

            mapping[140] = (byte)Block.Water;       // WaterDown
            mapping[141] = (byte)Block.Lava;        // LavaDown
            mapping[143] = (byte)Block.Aqua;        // WaterFaucet
            mapping[144] = (byte)Block.Orange;      // LavaFaucet

            // 143 unused
            mapping[145] = (byte)Block.Water;       // finiteWater
            mapping[146] = (byte)Block.Lava;        // finiteLava
            mapping[147] = (byte)Block.Cyan;        // finiteFaucet

            mapping[148] = (byte)Block.Log;         // odoor1
            mapping[149] = (byte)Block.Obsidian;    // odoor2
            mapping[150] = (byte)Block.Glass;       // odoor3
            mapping[151] = (byte)Block.Stone;       // odoor4
            mapping[152] = (byte)Block.Leaves;      // odoor5
            mapping[153] = (byte)Block.Sand;        // odoor6
            mapping[154] = (byte)Block.Wood;        // odoor7
            mapping[155] = (byte)Block.Green;       // odoor8
            mapping[156] = (byte)Block.TNT;         // odoor9
            mapping[157] = (byte)Block.Stair;       // odoor10
            mapping[158] = (byte)Block.Lava;        // odoor11
            mapping[159] = (byte)Block.Water;       // odoor12

            mapping[160] = (byte)Block.Air;         // air_portal
            mapping[161] = (byte)Block.Water;       // water_portal
            mapping[162] = (byte)Block.Lava;        // lava_portal

            // 163 unused
            mapping[164] = (byte)Block.Air;         // air_door
            mapping[165] = (byte)Block.Air;         // air_switch
            mapping[166] = (byte)Block.Water;       // water_door
            mapping[167] = (byte)Block.Lava;        // lava_door

            // 168-174 = odoor*_air
            mapping[175] = (byte)Block.Cyan;        // blue_portal
            mapping[176] = (byte)Block.Orange;      // orange_portal
            // 177-181 = odoor*_air

            mapping[182] = (byte)Block.TNT;         // smalltnt
            mapping[183] = (byte)Block.TNT;         // bigtnt
            mapping[184] = (byte)Block.Lava;        // tntexplosion
            mapping[185] = (byte)Block.Lava;        // fire

            // 186 unused
            mapping[187] = (byte)Block.Glass;       // rocketstart
            mapping[188] = (byte)Block.Gold;        // rockethead
            mapping[189] = (byte)Block.Steel;       // firework

            mapping[190] = (byte)Block.Lava;        // deathlava
            mapping[191] = (byte)Block.Water;       // deathwater
            mapping[192] = (byte)Block.Air;         // deathair
            mapping[193] = (byte)Block.Water;       // activedeathwater
            mapping[194] = (byte)Block.Lava;        // activedeathlava

            mapping[195] = (byte)Block.Lava;        // magma
            mapping[196] = (byte)Block.Water;       // geyser

            // 197-210 = air
            mapping[211] = (byte)Block.Red;         // door8_air
            mapping[212] = (byte)Block.Lava;        // door9_air
            // 213-229 = air

            mapping[230] = (byte)Block.Aqua;        // train
            mapping[231] = (byte)Block.TNT;         // creeper
            mapping[232] = (byte)Block.MossyRocks;  // zombiebody
            mapping[233] = (byte)Block.Lime;        // zombiehead

            // 234 unused
            mapping[235] = (byte)Block.White;       // birdwhite
            mapping[236] = (byte)Block.Black;       // birdblack
            mapping[237] = (byte)Block.Lava;        // birdlava
            mapping[238] = (byte)Block.Red;         // birdred
            mapping[239] = (byte)Block.Water;       // birdwater
            mapping[240] = (byte)Block.Blue;        // birdblue
            mapping[242] = (byte)Block.Lava;        // birdkill

            mapping[245] = (byte)Block.Gold;        // fishgold
            mapping[246] = (byte)Block.Sponge;      // fishsponge
            mapping[247] = (byte)Block.Gray;        // fishshark
            mapping[248] = (byte)Block.Red;         // fishsalmon
            mapping[249] = (byte)Block.Blue;        // fishbetta
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
                    map.WidthX = bs.ReadInt16();
                    map.WidthY = bs.ReadInt16();
                    map.Height = bs.ReadInt16();

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
                    map.WidthX = bs.ReadInt16();
                    map.WidthY = bs.ReadInt16();
                    map.Height = bs.ReadInt16();

                    if( !map.ValidateHeader() ) {
                        throw new MapFormatException( "MapFCMv3.Load: One or more of the map dimensions are invalid." );
                    }

                    // Read in the spawn location
                    map.Spawn.x = (short)(bs.ReadInt16() * 32);
                    map.Spawn.h = (short)(bs.ReadInt16() * 32);
                    map.Spawn.y = (short)(bs.ReadInt16() * 32);

                    // Read in the spawn orientation
                    map.Spawn.r = bs.ReadByte();
                    map.Spawn.l = bs.ReadByte();

                    // Skip over the VisitPermission and BuildPermission bytes
                    bs.ReadByte();
                    bs.ReadByte();

                    // Read in the map data
                    map.Blocks = bs.ReadBytes( map.GetBlockCount() );

                    for( int i = 0; i < map.Blocks.Length; i++ ) {
                        if( map.Blocks[i] > 49 ) {
                            map.Blocks[i] = mapping[map.Blocks[i]];
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
                    bs.Write( mapToSave.WidthX );
                    bs.Write( mapToSave.Height );
                    bs.Write( mapToSave.WidthY );

                    // Write the spawn location
                    bs.Write( mapToSave.Spawn.x / 32 );
                    bs.Write( mapToSave.Spawn.h / 32 );
                    bs.Write( mapToSave.Spawn.y / 32 );

                    //Write the spawn orientation
                    bs.Write( mapToSave.Spawn.r );
                    bs.Write( mapToSave.Spawn.l );

                    // Write the VistPermission and BuildPermission bytes
                    bs.Write( (byte)0 );
                    bs.Write( (byte)0 );

                    // Write the map data
                    bs.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );

                    bs.Close();
                }
                return true;
            }
        }
    }
}