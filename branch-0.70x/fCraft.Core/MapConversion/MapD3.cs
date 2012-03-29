// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace fCraft.MapConversion {
    /// <summary> D3 map conversion implementation, for converting D3 map format into fCraft's default map format. </summary>
    public sealed class MapD3 : IMapImporter, IMapExporter {

        public string ServerName {
            get { return "D3"; }
        }

        public bool SupportsImport {
            get { return true; }
        }

        public bool SupportsExport {
            get { return true; }
        }

        public string FileExtension {
            get { return "map"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.D3; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".map", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                        BinaryReader bs = new BinaryReader( gs );
                        int formatVersion = bs.ReadInt32();
                        return (formatVersion == 1000 || formatVersion == 1010 || formatVersion == 1020 ||
                                formatVersion == 1030 || formatVersion == 1040 || formatVersion == 1050);
                    }
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream fs = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( fs, CompressionMode.Decompress ) ) {
                    return LoadHeaderInternal( gs );
                }
            }
        }


        static Map LoadHeaderInternal( GZipStream gs ) {
            if( gs == null ) throw new ArgumentNullException( "gs" );
            // Setup a GZipStream to decompress and read the map file
                BinaryReader bs = new BinaryReader( gs );

                int formatVersion = bs.ReadInt32();

                // Read in the map dimesions
                int width = bs.ReadInt16();
                int length = bs.ReadInt16();
                int height = bs.ReadInt16();

                Map map = new Map( null, width, length, height, false );

                Position spawn = new Position();

                switch( formatVersion ) {
                    case 1000:
                    case 1010:
                        break;
                    case 1020:
                        spawn.X = (short)( bs.ReadInt16() * 32 );
                        spawn.Y = (short)( bs.ReadInt16() * 32 );
                        spawn.Z = (short)( bs.ReadInt16() * 32 );
                        map.Spawn = spawn;
                        break;
                    //case 1030:
                    //case 1040:
                    //case 1050:
                    default:
                        spawn.X = (short)(bs.ReadInt16() * 32);
                        spawn.Y = (short)(bs.ReadInt16() * 32);
                        spawn.Z = (short)(bs.ReadInt16() * 32);
                        spawn.R = (byte)bs.ReadInt16();
                        spawn.L = (byte)bs.ReadInt16();
                        map.Spawn = spawn;
                        break;
                }

                return map;
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream fs = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( fs, CompressionMode.Decompress ) ) {
                    Map map = LoadHeaderInternal( gs );

                    if( !map.ValidateHeader() ) {
                        throw new MapFormatException( "MapD3: One or more of the map dimensions are invalid." );
                    }

                    // Read in the map data
                    byte[] buffer = new byte[4];
                    map.Blocks = new byte[map.Volume];
                    for( int i = 0; i < map.Volume; i++ ) {
                        gs.Read( buffer, 0, 4 );
                        map.Blocks[i] = buffer[0];
                    }
                    map.ConvertBlockTypes( Mapping );

                    return map;
                }
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    using( BufferedStream bs = new BufferedStream( gs ) ) {
                        BinaryWriter bw = new BinaryWriter( bs );

                        // Write the magic number
                        bw.Write( 1050 );

                        // Write the map dimensions
                        bw.Write( mapToSave.Width );
                        bw.Write( mapToSave.Length );
                        bw.Write( mapToSave.Height );

                        Vector3I spawn = mapToSave.Spawn.ToBlockCoords();
                        bw.Write( (short)spawn.X );
                        bw.Write( (short)spawn.Y );
                        bw.Write( (short)spawn.Z );
                        bw.Write( (short)mapToSave.Spawn.R );
                        bw.Write( (short)mapToSave.Spawn.L );

                        // Write the map data
                        for( int i = 0; i < mapToSave.Volume; i++ ) {
                            bw.Write( (int)mapToSave.Blocks[i] );
                        }
                        return true;
                    }
                }
            }
        }


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
            Mapping[61] = (byte)Block.Cobblestone;  // Furnace
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
            Mapping[228] = (byte)Block.Iron;        // Fan
            Mapping[229] = (byte)Block.Iron;        // Door
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
            Mapping[247] = (byte)Block.Iron;        // Cannon
            Mapping[248] = (byte)Block.Obsidian;    // Blob
            // all others default to 0/air
        }
    }
}