// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> JTE map conversion implementation, for converting JTE map format into fCraft's default map format. </summary>
    public sealed class MapJTE : IMapImporter, IMapExporter {

        public string ServerName {
            get { return "JTE's"; }
        }

        public bool SupportsImport {
            get { return true; }
        }

        public bool SupportsExport {
            get { return true; }
        }

        public string FileExtension {
            get { return "gz"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.JTE; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".gz", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                        BinaryReader bs = new BinaryReader( gs );
                        byte version = bs.ReadByte();
                        return ( version == 1 || version == 2 );
                    }
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    return LoadHeaderInternal( gs );
                }
            }
        }


        static Map LoadHeaderInternal( [NotNull] Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryReader bs = new BinaryReader( stream );

            byte version = bs.ReadByte();
            if( version != 1 && version != 2 ) throw new MapFormatException();

            // read spawn location and orientation
            short x = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() )*32);
            short z = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() )*32);
            short y = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() )*32);
            Position spawn = new Position( x, y, z, bs.ReadByte(), bs.ReadByte() );

            // Read in the map dimensions
            int width = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            int length = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            int height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

            return new Map( null, width, length, height, false ) { Spawn = spawn };
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                // Setup a GZipStream to decompress and read the map file
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress );

                Map map = LoadHeaderInternal( gs );

                // Read in the map data
                map.Blocks = new byte[map.Volume];
                mapStream.Read( map.Blocks, 0, map.Blocks.Length );

                map.ConvertBlockTypes( Mapping );

                return map;
            }
        }


        public void Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write the magic number
                    bs.Write( (byte)0x01 );

                    // Write the spawn location
                    bs.Write( IPAddress.NetworkToHostOrder( (short)( mapToSave.Spawn.X / 32 ) ) );
                    bs.Write( IPAddress.NetworkToHostOrder( (short)( mapToSave.Spawn.Z / 32 ) ) );
                    bs.Write( IPAddress.NetworkToHostOrder( (short)( mapToSave.Spawn.Y / 32 ) ) );

                    //Write the spawn orientation
                    bs.Write( mapToSave.Spawn.R );
                    bs.Write( mapToSave.Spawn.L );

                    // Write the map dimensions
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Width ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Length ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Height ) );

                    // Write the map data
                    bs.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );
                }
            }
        }


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
    }
}