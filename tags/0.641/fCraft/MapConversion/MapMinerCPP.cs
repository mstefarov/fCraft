// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
// Initial support contributed by Tyler Kennedy <tk@tkte.ch>
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> MinerCPP/LuaCraft map conversion implementation, for converting MinerCPP/LuaCraft map format into fCraft's default map format. </summary>
    public sealed class MapMinerCPP : IMapImporter, IMapExporter {

        public string ServerName {
            get { return "MinerCPP/LuaCraft"; }
        }

        public bool SupportsImport {
            get { return true; }
        }

        public bool SupportsExport {
            get { return true; }
        }

        public string FileExtension {
            get { return "dat"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.MinerCPP; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".dat", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                        BinaryReader bs = new BinaryReader( gs );
                        return ( bs.ReadByte() == 0xbe && bs.ReadByte() == 0xee && bs.ReadByte() == 0xef );
                    }
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                // Setup a GZipStream to decompress and read the map file
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                    return LoadHeaderInternal( gs );
                }
            }
        }


        static Map LoadHeaderInternal( [NotNull] Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryReader bs = new BinaryReader( stream );

            // Read in the magic number
            if( bs.ReadByte() != 0xbe || bs.ReadByte() != 0xee || bs.ReadByte() != 0xef ) {
                throw new MapFormatException( "MapMinerCPP: Map header is incorrect." );
            }

            // Read in the map dimensions
            // Saved in big endian for who-knows-what reason.
            // XYZ(?)
            int width = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            int height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            int length = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map( null, width, length, height, false );
            // ReSharper restore UseObjectOrCollectionInitializer

            // Read in the spawn location
            // XYZ(?)
            map.Spawn = new Position {
                X = IPAddress.NetworkToHostOrder( bs.ReadInt16() ),
                Z = IPAddress.NetworkToHostOrder( bs.ReadInt16() ),
                Y = IPAddress.NetworkToHostOrder( bs.ReadInt16() ),
                R = bs.ReadByte(),
                L = bs.ReadByte()
            };

            // Skip over the block count, totally useless
            bs.ReadInt32();

            return map;
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                // Setup a GZipStream to decompress and read the map file
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {

                    Map map = LoadHeaderInternal( gs );

                    // Read in the map data
                    map.Blocks = new byte[map.Volume];
                    mapStream.Read( map.Blocks, 0, map.Blocks.Length );

                    return map;
                }
            }
        }


        public void Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write out the magic number
                    bs.Write( new byte[] { 0xbe, 0xee, 0xef } );

                    // Save the map dimensions
                    // XYZ(?)
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.Width ) );
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.Height ) );
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.Length ) );

                    // Save the spawn location
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Spawn.X ) );
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Spawn.Z ) );
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Spawn.Y ) );

                    // Save the spawn orientation
                    bs.Write( mapToSave.Spawn.R );
                    bs.Write( mapToSave.Spawn.L );

                    // Write out the block count (which is totally useless, can't stress that enough.)
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Blocks.Length ) );

                    // Write out the map data
                    bs.Write( mapToSave.Blocks );
                }
            }
        }
    }
}