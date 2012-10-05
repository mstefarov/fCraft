// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.IO.Compression;

namespace fCraft.MapConversion {
    /// <summary> NBT map conversion implementation, for converting NBT map format into fCraft's default map format. </summary>
    public sealed class MapIndev : IMapImporter {
        public string ServerName {
            get { return "Indev"; }
        }

        public string FileExtension {
            get { return "mclevel"; }
        }

        public bool SupportsImport {
            get { return true; }
        }

        public bool SupportsExport {
            get { return false; }
        }


        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.Indev; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".mclevel", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                    BinaryReader bs = new BinaryReader( gs );
                    return ( bs.ReadByte() == 10 && NBTag.ReadString( bs ) == "MinecraftLevel" );
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            Map map = Load( fileName );
            map.Blocks = null;
            return map;
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                NBTag tag = NBTag.ReadStream( gs );

                NBTag mapTag = tag["Map"];
                // ReSharper disable UseObjectOrCollectionInitializer
                Map map = new Map( null,
                                   mapTag["Width"].GetShort(),
                                   mapTag["Length"].GetShort(),
                                   mapTag["Height"].GetShort(),
                                   false );
                // ReSharper restore UseObjectOrCollectionInitializer
                map.Spawn = new Position {
                    X = mapTag["Spawn"][0].GetShort(),
                    Z = mapTag["Spawn"][1].GetShort(),
                    Y = mapTag["Spawn"][2].GetShort(),
                    R = 0,
                    L = 0
                };

                map.Blocks = mapTag["Blocks"].GetBytes();
                map.RemoveUnknownBlocktypes();

                return map;
            }
        }
    }
}