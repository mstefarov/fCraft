// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.IO.Compression;
using fNbt;

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
                    return ( bs.ReadByte() == (byte)NbtTagType.Compound );
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
            NbtFile file = new NbtFile( fileName );
            if( file.RootTag == null ) throw new MapFormatException( "No root tag" );

            NbtCompound mapTag = file.RootTag.Get<NbtCompound>( "Map" );
            if( mapTag == null ) throw new MapFormatException( "No Map tag" );

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map( null,
                               mapTag["Width"].ShortValue,
                               mapTag["Length"].ShortValue,
                               mapTag["Height"].ShortValue,
                               false );
            map.Spawn = new Position {
                X = mapTag["Spawn"][0].ShortValue,
                Z = mapTag["Spawn"][1].ShortValue,
                Y = mapTag["Spawn"][2].ShortValue
            };
            // ReSharper restore UseObjectOrCollectionInitializer

            map.Blocks = mapTag["Blocks"].ByteArrayValue;
            map.RemoveUnknownBlocktypes();

            return map;
        }
    }
}