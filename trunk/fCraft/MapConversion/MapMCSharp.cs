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

        static readonly byte[] Mapping = new byte[256];

        static MapMCSharp() {
            Mapping[100] = (byte)Block.Glass;       // op_glass
            Mapping[101] = (byte)Block.Obsidian;    // opsidian
            Mapping[102] = (byte)Block.Brick;       // op_brick
            Mapping[103] = (byte)Block.Stone;       // op_stone
            Mapping[104] = (byte)Block.Rocks;       // op_cobblestone
            // 105 = op_air
            Mapping[106] = (byte)Block.Water;       // op_water

            // 107-109 unused
            Mapping[110] = (byte)Block.Wood;        // wood_float
            Mapping[111] = (byte)Block.Log;         // door
            Mapping[112] = (byte)Block.Lava;        // lava_fast
            Mapping[113] = (byte)Block.Obsidian;    // door2
            Mapping[114] = (byte)Block.Glass;       // door3
            Mapping[115] = (byte)Block.Stone;       // door4
            Mapping[116] = (byte)Block.Leaves;      // door5
            Mapping[117] = (byte)Block.Sand;        // door6
            Mapping[118] = (byte)Block.Wood;        // door7
            Mapping[119] = (byte)Block.Green;       // door8
            Mapping[120] = (byte)Block.TNT;         // door9
            Mapping[121] = (byte)Block.Stair;       // door10

            Mapping[122] = (byte)Block.Log;         // tdoor
            Mapping[123] = (byte)Block.Obsidian;    // tdoor2
            Mapping[124] = (byte)Block.Glass;       // tdoor3
            Mapping[125] = (byte)Block.Stone;       // tdoor4
            Mapping[126] = (byte)Block.Leaves;      // tdoor5
            Mapping[127] = (byte)Block.Sand;        // tdoor6
            Mapping[128] = (byte)Block.Wood;        // tdoor7
            Mapping[129] = (byte)Block.Green;       // tdoor8

            Mapping[130] = (byte)Block.White;       // MsgWhite
            Mapping[131] = (byte)Block.Black;       // MsgBlack
            Mapping[132] = (byte)Block.Air;         // MsgAir
            Mapping[133] = (byte)Block.Water;       // MsgWater
            Mapping[134] = (byte)Block.Lava;        // MsgLava

            Mapping[135] = (byte)Block.TNT;         // tdoor9
            Mapping[136] = (byte)Block.Stair;       // tdoor10
            Mapping[137] = (byte)Block.Air;         // tdoor11
            Mapping[138] = (byte)Block.Water;       // tdoor12
            Mapping[139] = (byte)Block.Lava;        // tdoor13

            Mapping[140] = (byte)Block.Water;       // WaterDown
            Mapping[141] = (byte)Block.Lava;        // LavaDown
            Mapping[143] = (byte)Block.Aqua;        // WaterFaucet
            Mapping[144] = (byte)Block.Orange;      // LavaFaucet

            // 143 unused
            Mapping[145] = (byte)Block.Water;       // finiteWater
            Mapping[146] = (byte)Block.Lava;        // finiteLava
            Mapping[147] = (byte)Block.Cyan;        // finiteFaucet

            Mapping[148] = (byte)Block.Log;         // odoor1
            Mapping[149] = (byte)Block.Obsidian;    // odoor2
            Mapping[150] = (byte)Block.Glass;       // odoor3
            Mapping[151] = (byte)Block.Stone;       // odoor4
            Mapping[152] = (byte)Block.Leaves;      // odoor5
            Mapping[153] = (byte)Block.Sand;        // odoor6
            Mapping[154] = (byte)Block.Wood;        // odoor7
            Mapping[155] = (byte)Block.Green;       // odoor8
            Mapping[156] = (byte)Block.TNT;         // odoor9
            Mapping[157] = (byte)Block.Stair;       // odoor10
            Mapping[158] = (byte)Block.Lava;        // odoor11
            Mapping[159] = (byte)Block.Water;       // odoor12

            Mapping[160] = (byte)Block.Air;         // air_portal
            Mapping[161] = (byte)Block.Water;       // water_portal
            Mapping[162] = (byte)Block.Lava;        // lava_portal

            // 163 unused
            Mapping[164] = (byte)Block.Air;         // air_door
            Mapping[165] = (byte)Block.Air;         // air_switch
            Mapping[166] = (byte)Block.Water;       // water_door
            Mapping[167] = (byte)Block.Lava;        // lava_door

            // 168-174 = odoor*_air
            Mapping[175] = (byte)Block.Cyan;        // blue_portal
            Mapping[176] = (byte)Block.Orange;      // orange_portal
            // 177-181 = odoor*_air

            Mapping[182] = (byte)Block.TNT;         // smalltnt
            Mapping[183] = (byte)Block.TNT;         // bigtnt
            Mapping[184] = (byte)Block.Lava;        // tntexplosion
            Mapping[185] = (byte)Block.Lava;        // fire

            // 186 unused
            Mapping[187] = (byte)Block.Glass;       // rocketstart
            Mapping[188] = (byte)Block.Gold;        // rockethead
            Mapping[189] = (byte)Block.Steel;       // firework

            Mapping[190] = (byte)Block.Lava;        // deathlava
            Mapping[191] = (byte)Block.Water;       // deathwater
            Mapping[192] = (byte)Block.Air;         // deathair
            Mapping[193] = (byte)Block.Water;       // activedeathwater
            Mapping[194] = (byte)Block.Lava;        // activedeathlava

            Mapping[195] = (byte)Block.Lava;        // magma
            Mapping[196] = (byte)Block.Water;       // geyser

            // 197-210 = air
            Mapping[211] = (byte)Block.Red;         // door8_air
            Mapping[212] = (byte)Block.Lava;        // door9_air
            // 213-229 = air

            Mapping[230] = (byte)Block.Aqua;        // train
            Mapping[231] = (byte)Block.TNT;         // creeper
            Mapping[232] = (byte)Block.MossyRocks;  // zombiebody
            Mapping[233] = (byte)Block.Lime;        // zombiehead

            // 234 unused
            Mapping[235] = (byte)Block.White;       // birdwhite
            Mapping[236] = (byte)Block.Black;       // birdblack
            Mapping[237] = (byte)Block.Lava;        // birdlava
            Mapping[238] = (byte)Block.Red;         // birdred
            Mapping[239] = (byte)Block.Water;       // birdwater
            Mapping[240] = (byte)Block.Blue;        // birdblue
            Mapping[242] = (byte)Block.Lava;        // birdkill

            Mapping[245] = (byte)Block.Gold;        // fishgold
            Mapping[246] = (byte)Block.Sponge;      // fishsponge
            Mapping[247] = (byte)Block.Gray;        // fishshark
            Mapping[248] = (byte)Block.Red;         // fishsalmon
            Mapping[249] = (byte)Block.Blue;        // fishbetta
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
                    return LoadHeaderInternal( gs );
                }
            }
        }


        static Map LoadHeaderInternal( Stream stream ) {
            BinaryReader bs = new BinaryReader( stream );

            // Read in the magic number
            if( bs.ReadUInt16() != 0x752 ) {
                throw new MapFormatException();
            }

            // Read in the map dimesions
            int widthX = bs.ReadInt16();
            int widthY = bs.ReadInt16();
            int height = bs.ReadInt16();

            Map map = new Map( null, widthX, widthY, height, false );

            // Read in the spawn location
            map.Spawn.X = (short)(bs.ReadInt16() * 32);
            map.Spawn.H = (short)(bs.ReadInt16() * 32);
            map.Spawn.Y = (short)(bs.ReadInt16() * 32);

            // Skip over the VisitPermission and BuildPermission bytes
            bs.ReadByte();
            bs.ReadByte();

            return map;
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {

                    Map map = LoadHeaderInternal( gs );

                    if( !map.ValidateHeader() ) {
                        throw new MapFormatException( "One or more of the map dimensions are invalid." );
                    }

                    // Read in the map data
                    map.Blocks = new byte[map.WidthX * map.WidthY * map.Height];
                    mapStream.Read( map.Blocks, 0, map.Blocks.Length );

                    for( int i = 0; i < map.Blocks.Length; i++ ) {
                        if( map.Blocks[i] > 49 ) {
                            map.Blocks[i] = Mapping[map.Blocks[i]];
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
                    bs.Write( mapToSave.WidthY );
                    bs.Write( mapToSave.Height );

                    // Write the spawn location
                    bs.Write( mapToSave.Spawn.X / 32 );
                    bs.Write( mapToSave.Spawn.H / 32 );
                    bs.Write( mapToSave.Spawn.Y / 32 );

                    //Write the spawn orientation
                    bs.Write( mapToSave.Spawn.R );
                    bs.Write( mapToSave.Spawn.L );

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