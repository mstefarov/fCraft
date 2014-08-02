// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.IO;
using System.IO.Compression;
using fNbt;

namespace fCraft.MapConversion {
    /// <summary> NBT map conversion implementation, for converting NBT map format into fCraft's default map format. </summary>
    internal sealed class MapIndev : IMapImporter {
        public string ServerName {
            get { return "Indev"; }
        }

        public string FileExtension {
            get { return "mclevel"; }
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
                    return (bs.ReadByte() == 10 && Swap( bs.ReadInt16() ) == 14);
                }
            } catch( Exception ) {
                return false;
            }
        }

        static short Swap( short v ) {
            return (short)((v >> 8) & 0x00FF |
                           (v << 8) & 0xFF00);
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

            NbtCompound mapTag = file.RootTag;

            Map map = new Map( null,
                               mapTag["Width"].ShortValue,
                               mapTag["Length"].ShortValue,
                               mapTag["Height"].ShortValue,
                               false );

            map.Spawn = new Position( mapTag["Spawn"][0].ShortValue,
                                      mapTag["Spawn"][2].ShortValue,
                                      mapTag["Spawn"][1].ShortValue );

            map.Blocks = mapTag["Blocks"].ByteArrayValue;
            map.RemoveUnknownBlockTypes();

            return map;
        }
    }
}
