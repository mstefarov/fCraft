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
using System.Net;
using fCraft;


namespace Mcc {
    public sealed class MapD3 : IConverter {

        const byte HeaderConstant1 = 232, HeaderConstant2 = 3;

        public MapFormat Format {
            get { return MapFormat.D3; }
        }

        public string FileExtension {
            get { return ".map"; }
        }

        public string ServerName {
            get { return "D3"; }
        }

        static byte[] mapping = new byte[256];
        static MapD3() {
            // 0-49 default
            mapping[50] = (byte)Block.TNT;          // Torch
            mapping[51] = (byte)Block.StillLava;    // Fire
            mapping[52] = (byte)Block.Blue;         // Water Source
            mapping[53] = (byte)Block.Red;          // Lava Source
            mapping[54] = (byte)Block.TNT;          // Chest
            mapping[55] = (byte)Block.TNT;          // Gear
            mapping[56] = (byte)Block.Glass;        // Diamond Ore
            mapping[57] = (byte)Block.Glass;        // Diamond
            mapping[58] = (byte)Block.TNT;          // Workbench
            mapping[59] = (byte)Block.Leaves;       // Crops
            mapping[60] = (byte)Block.Obsidian;     // Soil
            mapping[61] = (byte)Block.Rocks;        // Furnace
            mapping[62] = (byte)Block.StillLava;    // Burning Furnace
            // 63-199 unused
            mapping[200] = (byte)Block.Lava;        // Kill Lava
            mapping[201] = (byte)Block.Stone;       // Kill Lava
            // 202 unused
            mapping[203] = (byte)Block.Stair;       // Still Stair
            // 204-205 unused
            mapping[206] = (byte)Block.Water;       // Original Water
            mapping[207] = (byte)Block.Lava;        // Original Lava
            // 208 Invisible
            mapping[209] = (byte)Block.Water;       // Acid
            mapping[210] = (byte)Block.Sand;        // Still Sand
            mapping[211] = (byte)Block.Water;       // Still Acid
            mapping[212] = (byte)Block.RedFlower;   // Kill Rose
            mapping[213] = (byte)Block.Gravel;      // Still Gravel
            // 214 No Entry
            mapping[215] = (byte)Block.White;       // Snow
            mapping[216] = (byte)Block.Lava;        // Fast Lava
            mapping[217] = (byte)Block.White;       // Kill Glass
            // 218 Invisible Sponge
            mapping[219] = (byte)Block.Sponge;      // Drain Sponge
            mapping[220] = (byte)Block.Sponge;      // Super Drain Sponge
            mapping[221] = (byte)Block.Gold;        // Spark
            mapping[222] = (byte)Block.TNT;         // Rocket
            mapping[223] = (byte)Block.Gold;        // Short Spark
            mapping[224] = (byte)Block.TNT;         // Mega Rocket
            mapping[225] = (byte)Block.Lava;        // Red Spark
            mapping[226] = (byte)Block.TNT;         // Fire Fountain
            mapping[227] = (byte)Block.TNT;         // Admin TNT
            mapping[228] = (byte)Block.Steel;       // Fan
            mapping[229] = (byte)Block.Steel;       // Door
            mapping[230] = (byte)Block.Lava;        // Campfire
            mapping[231] = (byte)Block.Red;         // Laser
            mapping[232] = (byte)Block.Black;       // Ash
            // 233-234 unused
            mapping[235] = (byte)Block.Water;       // Sea
            mapping[236] = (byte)Block.White;       // Flasher
            // 237-243 unused
            mapping[244] = (byte)Block.Leaves;      // Vines
            mapping[245] = (byte)Block.Lava;        // Flamethrower
            // 246 unused
            mapping[247] = (byte)Block.Steel;       // Cannon
            mapping[248] = (byte)Block.Obsidian;    // Blob
            // all others default to 0/air
        }


        public Map Load( Stream mapStream ) {
            // Reset the seeker to the front of the stream
            // This should probably be done differently.
            mapStream.Seek( 0, SeekOrigin.Begin );

            // Setup a GZipStream to decompress and read the map file
            GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );

            Map map = new Map();

            if( bs.ReadByte() != HeaderConstant1 ) {
                throw new Exception( "Incorrect D3 map header." );
            }
            if( bs.ReadByte() != HeaderConstant2 ) {
                throw new Exception( "Incorrect D3 map header." );
            }

            bs.ReadBytes( 2 );

            // Read in the map dimesions
            map.widthX = IPAddress.NetworkToHostOrder(bs.ReadInt16());
            map.widthY = IPAddress.NetworkToHostOrder(bs.ReadInt16());
            map.height = IPAddress.NetworkToHostOrder(bs.ReadInt16());

            // D3 doesn't save spawnpoint in the map... for SOME reason
            map.ResetSpawn();

            if( !map.ValidateHeader() ) {
                throw new Exception( "One or more of the map dimensions are invalid." );
            }

            // Read in the map data
            map.blocks = bs.ReadBytes( map.GetBlockCount() );

            for( int i = 0; i < map.blocks.Length; i++ ) {
                if( map.blocks[i] > 49 ) {
                    map.blocks[i] = mapping[map.blocks[i]];
                }
            }

            return map;
        }


        public bool Save( Map mapToSave, Stream mapStream ) {
            using ( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress, true ) ) {
                BinaryWriter bs = new BinaryWriter( gs );

                // Write the magic number
                bs.Write( (byte)HeaderConstant1 );
                bs.Write( (byte)HeaderConstant2 );
                bs.Write( (byte)0 );
                bs.Write( (byte)0 );

                // Write the map dimensions
                bs.Write( IPAddress.NetworkToHostOrder( mapToSave.widthX ) );
                bs.Write( IPAddress.NetworkToHostOrder( mapToSave.widthY ) );
                bs.Write( IPAddress.NetworkToHostOrder( mapToSave.height ) );

                // Write the map data
                bs.Write( mapToSave.blocks, 0, mapToSave.blocks.Length );

                bs.Close();
            }
            return true;
        }


        public bool Claims( Stream mapStream ) {
            mapStream.Seek( 0, SeekOrigin.Begin );
            try {
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                BinaryReader bs = new BinaryReader( gs );
                return (bs.ReadByte() == HeaderConstant1 && bs.ReadByte() == HeaderConstant2);
            } catch( Exception ) {
                return false;
            }
        }

    }
}