using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using fCraft;


namespace Mcc {
    class MapFCMv3 : IMapConverter {
        public const int Identifier = 0x0FC2AF40;
        public const byte Revision = 12;

        public bool ClaimsFileName( string fileName ) {
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }

        public MapFormat Format {
            get { return MapFormat.FCMv3; }
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
            map.spawn.x = (short)reader.ReadInt32();
            map.spawn.h = (short)reader.ReadInt32();
            map.spawn.y = (short)reader.ReadInt32();
            map.spawn.r = reader.ReadByte();
            map.spawn.l = reader.ReadByte();

            // read modification/creation times
            map.DateModified = TimestampToDateTime( reader.ReadInt64() );
            map.DateCreated = TimestampToDateTime( reader.ReadInt64() );

            // read UUID
            map.GUID = new Guid( reader.ReadBytes( 16 ) );

            // read data layer index
            int layerCount = reader.ReadByte();

            // read the index
            List<Map.DataLayer> layers = new List<Map.DataLayer>();
            for( int i = 0; i < layerCount; i++ ) {
                Map.DataLayer layer = new Map.DataLayer();
                layer.Type = (Map.DataLayerType)reader.ReadByte();
                layer.Offset = reader.ReadInt64();
                layer.CompressedLength = reader.ReadInt32();
                layer.GeneralPurposeField = reader.ReadInt32();
                layer.ElementSize = reader.ReadInt32();
                layer.ElementCount = reader.ReadInt32();
                layers.Add( layer );
            }


            // read metadata
            Dictionary<string, Dictionary<string, string>> metadata = new Dictionary<string, Dictionary<string, string>>();
            int metaSize = reader.ReadInt32();

            using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Decompress ) ) {
                BinaryReader br = new BinaryReader( ds );
                for( int i = 0; i < metaSize; i++ ) {
                    string group = ReadLengthPrefixedString( br ).ToLowerInvariant();
                    string key = ReadLengthPrefixedString( br ).ToLowerInvariant();
                    string newValue = ReadLengthPrefixedString( br );

                    string oldValue = map.GetMeta( group, key );

                    if( oldValue != null && oldValue != newValue ) {
                        Logger.Log( "MapFCMv3.Load: Duplicate metadata entry found for [{0}].[{1}]. Old value (overwritten): \"{2}\". New value: \"{3}\"", LogType.Warning,
                                    group, key, map.GetMeta( group, key ), newValue );
                    }
                    if( group == "zones" ) {
                        try {
                            map.AddZone( new Zone( newValue, map.world ) );
                        } catch( Exception ex ) {
                            Logger.Log( "MapFCMv3.Load: Error importing zone definition: {0}", LogType.Error, ex );
                        }
                    } else {
                        map.SetMeta( group, key, newValue );
                    }
                }

                for( int i = 0; i < layerCount; i++ ) {
                    map.ReadLayer( layers[i], ds );
                }
            }
            return map;
        }

        public static string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = reader.ReadUInt16();
            byte[] stringData = reader.ReadBytes( length );
            return ASCIIEncoding.ASCII.GetString( stringData );
        }

        public static void WriteLengthPrefixedString( BinaryWriter writer, string s ) {
            byte[] stringData = ASCIIEncoding.ASCII.GetBytes( s );
            writer.Write( (ushort)stringData.Length );
            writer.Write( stringData );
        }

        public bool Save( Map mapToSave, Stream mapStream ) {
            Logger.Log( "Map saving is disabled in this release", LogType.Error );
            return false;
            BinaryWriter writer = new BinaryWriter( mapStream );

            writer.Write( Identifier );
            writer.Write( Revision );

            writer.Write( (short)mapToSave.widthX );
            writer.Write( (short)mapToSave.height );
            writer.Write( (short)mapToSave.widthY );

            writer.Write( (int)mapToSave.spawn.x );
            writer.Write( (int)mapToSave.spawn.h );
            writer.Write( (int)mapToSave.spawn.y );

            writer.Write( mapToSave.spawn.r );
            writer.Write( mapToSave.spawn.l );

            mapToSave.DateModified = DateTime.UtcNow;
            writer.Write( DateTimeToTimestamp( mapToSave.DateModified ) );
            writer.Write( DateTimeToTimestamp( mapToSave.DateCreated ) );

            writer.Write( mapToSave.GUID.ToByteArray() );

            // skip over the index (to be written later)
            long indexOffset = writer.BaseStream.Position;
            writer.BaseStream.Seek( 3127, SeekOrigin.Begin );

            // write metadata
            int metaCount = mapToSave.WriteMetadata( writer );

            // write layers
            int layerCount = 0;
            int layerFlags = 0;


            var layers = mapToSave.WriteLayers();

            foreach( Map.DataLayer layer in layers.Values ) {
                Map.DataLayer activeLayer = layer;
                activeLayer.Offset = writer.BaseStream.Position;
                activeLayer.CompressionType = Map.DataLayerCompressionType.Deflate;
                writer.Write( (byte)activeLayer.Type );
                writer.Write( (byte)activeLayer.CompressionType );
                writer.Write( (int)0 );
                writer.Write( (int)activeLayer.ElementSize );
                writer.Write( (int)activeLayer.ElementCount );
                using( DeflateStream ds = new DeflateStream( writer.BaseStream, CompressionMode.Compress, true ) ) {
                    //ds.Write( activeLayer.Data, 0, activeLayer.Data.Length );
                }
                activeLayer.CompressedLength = (int)(writer.BaseStream.Position - activeLayer.Offset);
                layerCount++;
                if((byte)activeLayer.Type < 32){
                    layerFlags |= (1<<(int)activeLayer.Type);
                }
            }

            // come back to write the index
            writer.BaseStream.Seek( indexOffset, SeekOrigin.Begin );
            writer.Write( layerCount );
            writer.Write( layerFlags );
            foreach( Map.DataLayer layer in layers.Values ) {
                writer.Write( layer.Offset );
                writer.Write( layer.CompressedLength );
            }
            writer.Write( metaCount );

            return true;
        }

        public bool Claims( Stream mapStream, string fileName ) {
            BinaryReader reader = new BinaryReader( mapStream );
            return ((reader.ReadInt32() == Identifier) &&
                    (reader.ReadByte() == Revision));
        }


    }
}