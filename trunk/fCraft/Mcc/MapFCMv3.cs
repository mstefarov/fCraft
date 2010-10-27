using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using fCraft;


namespace Mcc {
    class MapFCMv3 : IConverter {
        public const int Identifier = 0x0FC2AF40;
        public const byte Revision = 8;

        public MapFormat Format {
            get { return MapFormat.FCMv3; }
        }

        public string FileExtension {
            get { return ".fcm"; }
        }

        public string ServerName {
            get { return "fCraft"; }
        }

        static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );

        public static long DateTimeToTimestamp( DateTime timestamp ) {
            return (long)(timestamp - UnixEpoch).TotalSeconds;
        }

        public static DateTime TimestampToDateTime( long timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }

        public Map Load( Stream mapStream, string fileName ) {
            BinaryReader reader = new BinaryReader( mapStream );
            if( (reader.ReadInt32() != Identifier) ||
                (reader.ReadByte() == Revision) ) {
                throw new FormatException();
            }

            Map map = new Map();
            // read dimensions
            map.widthX = reader.ReadInt16();
            map.height = reader.ReadInt16();
            map.widthY = reader.ReadInt16();

            // read spawn
            map.spawn.x = reader.ReadInt16();
            map.spawn.h = reader.ReadInt16();
            map.spawn.y = reader.ReadInt16();
            map.spawn.r = reader.ReadByte();
            map.spawn.l = reader.ReadByte();

            // read modification/creation times
            map.DateModified = TimestampToDateTime( reader.ReadInt64() );
            map.DateCreated = TimestampToDateTime( reader.ReadInt64() );

            // read UUID
            map.GUID = new Guid( reader.ReadBytes( 16 ) );

            // read data index
            int layerCount = reader.ReadByte();
            reader.ReadUInt32(); // DataLayerFlags
            map.layers = new Dictionary<Map.DataLayerType, Map.DataLayer>();
            for( int i = 0; i < 256; i++ ) {
                long offset = reader.ReadInt64();
                if( offset != 0 ) {
                    Map.DataLayer layer = new Map.DataLayer();
                    layer.Type = (Map.DataLayerType)i;
                    layer.Offset = offset;
                    layer.CompressedLength = reader.ReadUInt32();
                }
            }

            // read metadata
            map.ReadMetadata( reader );

            foreach( Map.DataLayer layer in map.layers.Values ) {
                Map.DataLayer activeLayer = layer;
                reader.BaseStream.Seek( layer.Offset, SeekOrigin.Begin );
                reader.ReadByte();
                activeLayer.CompressionType = (Map.DataLayerCompressionType)reader.ReadByte();
                activeLayer.GeneralPurposeField = reader.ReadUInt32();
                activeLayer.ElementSize = reader.ReadUInt32();
                activeLayer.ElementCount = reader.ReadUInt32();
                activeLayer.Data = new byte[activeLayer.ElementCount * activeLayer.ElementSize];
                
                switch( activeLayer.CompressionType ) {
                    case Map.DataLayerCompressionType.Deflate:
                    case Map.DataLayerCompressionType.DeflateGZip:
                        using( ZLibStream zs = ZLibStream.MakeDecompressor( reader.BaseStream, ZLibStream.BufferSize, true ) ) {
                            zs.Read( activeLayer.Data, 0, activeLayer.Data.Length );
                        }
                        break;
                    case Map.DataLayerCompressionType.None:
                        reader.Read( activeLayer.Data, 0, activeLayer.Data.Length );
                        break;
                }

                switch( activeLayer.Type ) {
                    case Map.DataLayerType.Blocks:
                        map.blocks = activeLayer.Data;
                        break;
                }
            }

            return map;
        }
        

        public bool Save( Map mapToSave, Stream mapStream ) {
            return false;
        }

        public bool Claims( Stream mapStream, string fileName ) {
            BinaryReader reader = new BinaryReader( mapStream );
            return ((reader.ReadInt32() == Identifier) &&
                    (reader.ReadByte() == Revision));
        }


    }
}
