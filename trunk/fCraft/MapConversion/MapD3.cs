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
    public sealed class MapD3 : IMapConverter {
        const byte HeaderConstant1 = 232,
                   HeaderConstant2 = 3;

        static readonly byte[] Mapping = new byte[256];

        static MapD3() {
            // 0-49 default
            Mapping[50] = (byte)Block.TNT;          // Torch
            Mapping[51] = (byte)Block.StillLava;    // Fire
            Mapping[52] = (byte)Block.Blue;         // Water Source
            Mapping[53] = (byte)Block.Red;          // Lava Source
            Mapping[54] = (byte)Block.TNT;          // Chest
            Mapping[55] = (byte)Block.TNT;          // Gear
            Mapping[56] = (byte)Block.Glass;        // Diamond Ore
            Mapping[57] = (byte)Block.Glass;        // Diamond
            Mapping[58] = (byte)Block.TNT;          // Workbench
            Mapping[59] = (byte)Block.Leaves;       // Crops
            Mapping[60] = (byte)Block.Obsidian;     // Soil
            Mapping[61] = (byte)Block.Rocks;        // Furnace
            Mapping[62] = (byte)Block.StillLava;    // Burning Furnace
            // 63-199 unused
            Mapping[200] = (byte)Block.Lava;        // Kill Lava
            Mapping[201] = (byte)Block.Stone;       // Kill Lava
            // 202 unused
            Mapping[203] = (byte)Block.Stair;       // Still Stair
            // 204-205 unused
            Mapping[206] = (byte)Block.Water;       // Original Water
            Mapping[207] = (byte)Block.Lava;        // Original Lava
            // 208 Invisible
            Mapping[209] = (byte)Block.Water;       // Acid
            Mapping[210] = (byte)Block.Sand;        // Still Sand
            Mapping[211] = (byte)Block.Water;       // Still Acid
            Mapping[212] = (byte)Block.RedFlower;   // Kill Rose
            Mapping[213] = (byte)Block.Gravel;      // Still Gravel
            // 214 No Entry
            Mapping[215] = (byte)Block.White;       // Snow
            Mapping[216] = (byte)Block.Lava;        // Fast Lava
            Mapping[217] = (byte)Block.White;       // Kill Glass
            // 218 Invisible Sponge
            Mapping[219] = (byte)Block.Sponge;      // Drain Sponge
            Mapping[220] = (byte)Block.Sponge;      // Super Drain Sponge
            Mapping[221] = (byte)Block.Gold;        // Spark
            Mapping[222] = (byte)Block.TNT;         // Rocket
            Mapping[223] = (byte)Block.Gold;        // Short Spark
            Mapping[224] = (byte)Block.TNT;         // Mega Rocket
            Mapping[225] = (byte)Block.Lava;        // Red Spark
            Mapping[226] = (byte)Block.TNT;         // Fire Fountain
            Mapping[227] = (byte)Block.TNT;         // Admin TNT
            Mapping[228] = (byte)Block.Steel;       // Fan
            Mapping[229] = (byte)Block.Steel;       // Door
            Mapping[230] = (byte)Block.Lava;        // Campfire
            Mapping[231] = (byte)Block.Red;         // Laser
            Mapping[232] = (byte)Block.Black;       // Ash
            // 233-234 unused
            Mapping[235] = (byte)Block.Water;       // Sea
            Mapping[236] = (byte)Block.White;       // Flasher
            // 237-243 unused
            Mapping[244] = (byte)Block.Leaves;      // Vines
            Mapping[245] = (byte)Block.Lava;        // Flamethrower
            // 246 unused
            Mapping[247] = (byte)Block.Steel;       // Cannon
            Mapping[248] = (byte)Block.Obsidian;    // Blob
            // all others default to 0/air
        }


        public string ServerName {
            get { return "D3"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.D3; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".map", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                        BinaryReader bs = new BinaryReader( gs );
                        return (bs.ReadByte() == HeaderConstant1 && bs.ReadByte() == HeaderConstant2);
                    }
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                return LoadHeaderInternal( mapStream );
            }
        }


        Map LoadHeaderInternal( Stream stream ) {
            // Setup a GZipStream to decompress and read the map file
            using( GZipStream gs = new GZipStream( stream, CompressionMode.Decompress, true ) ) {
                BinaryReader bs = new BinaryReader( gs );

                int formatVersion = IPAddress.NetworkToHostOrder( bs.ReadInt32() );

                // Read in the map dimesions
                int widthX = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                int widthY = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                int height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                Map map = new Map( null, widthX, widthY, height, false );

                Position spawn = new Position();

                switch( formatVersion ) {
                    case 1000:
                    case 1010:
                        map.ResetSpawn();
                        break;
                    case 1020:
                        spawn.X = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.Y = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.H = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        map.SetSpawn( spawn );
                        break;
                    case 1030:
                    case 1040:
                    case 1050:
                    default:
                        spawn.X = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.Y = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.H = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.R = (byte)IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.L = (byte)IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        map.SetSpawn( spawn );
                        break;
                }

                return map;
            }
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {

                Map map = LoadHeaderInternal( mapStream );

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


        public bool Save( Map mapToSave, string fileName ) {
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write the magic number
                    bs.Write( HeaderConstant1 );
                    bs.Write( HeaderConstant2 );
                    bs.Write( (byte)0 );
                    bs.Write( (byte)0 );

                    // Write the map dimensions
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.WidthX ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.WidthY ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Height ) );

                    // Write the map data
                    bs.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );

                    bs.Close();
                    return true;
                }
            }
        }
    }
}