// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using fCraft;


namespace Mcc {
    /// <summary>
    /// fCraft map format converter, for format version #3 (2011)
    /// </summary>
    class MapFCMv3 : IMapConverter {
        public const int Identifier = 0x0FC2AF40;
        public const byte Revision = 13;

        public string ServerName {
            get { return "fCraft"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.FCMv3; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                try {
                    BinaryReader reader = new BinaryReader( mapStream );
                    int id = reader.ReadInt32();
                    int rev = reader.ReadByte();
                    return (id == Identifier && rev == Revision);
                } catch( Exception ) {
                    return false;
                }
            }
        }


        public Map LoadHeader( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                BinaryReader reader = new BinaryReader( mapStream );
                if( reader.ReadInt32() != Identifier || reader.ReadByte() != Revision ) {
                    throw new FormatException();
                }

                Map map = new Map();

                // read dimensions
                map.widthX = reader.ReadInt16();
                map.height = reader.ReadInt16();
                map.widthY = reader.ReadInt16();

                return map;
            }
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                BinaryReader reader = new BinaryReader( mapStream );
                if( reader.ReadInt32() != Identifier || reader.ReadByte() != Revision ) {
                    throw new FormatException();
                }

                Map map = new Map();
                // read dimensions
                map.widthX = reader.ReadInt16();
                map.height = reader.ReadInt16();
                map.widthY = reader.ReadInt16();

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "MapFCMv3.Load: One or more of the map dimensions are invalid." );
                }

                // read spawn
                map.spawn.x = (short)reader.ReadInt32();
                map.spawn.h = (short)reader.ReadInt32();
                map.spawn.y = (short)reader.ReadInt32();
                map.spawn.r = reader.ReadByte();
                map.spawn.l = reader.ReadByte();

                // read modification/creation times
                map.DateModified = TimestampToDateTime( reader.ReadUInt32() );
                map.DateCreated = TimestampToDateTime( reader.ReadUInt32() );

                // read UUID
                map.GUID = new Guid( reader.ReadBytes( 16 ) );


                // read the index
                int layerCount = reader.ReadByte();
                List<Map.DataLayer> layers = new List<Map.DataLayer>( layerCount );
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
                int metaSize = reader.ReadInt32();

                using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Decompress ) ) {
                    BinaryReader br = new BinaryReader( ds );
                    for( int i = 0; i < metaSize; i++ ) {
                        string group = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string key = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string newValue = ReadLengthPrefixedString( br );

                        string oldValue = map.GetMeta( group, key );

                        if( oldValue != null && oldValue != newValue ) {
                            Logger.Log( "MapFCMv3.Load: Duplicate metadata entry found for [{0}].[{1}]. " +
                                        "Old value (overwritten): \"{2}\". New value: \"{3}\"", LogType.Warning,
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
        }


        public bool Save( Map mapToSave, string fileName ) {
            using( FileStream mapStream = File.Create( fileName ) ) {
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

                List<Map.DataLayer> layers = mapToSave.PrepareLayers();
                writer.Write( (byte)layers.Count );

                // skip over index and metacount
                long indexOffset = mapStream.Position;
                writer.Seek( 25 * layers.Count + 4, SeekOrigin.Current );

                int metaCount;
                using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Compress, true ) ) {
                    using( BufferedStream bs = new BufferedStream( ds ) ) {
                        // write metadata
                        metaCount = mapToSave.WriteMetadata( ds );

                        for( int i = 0; i < layers.Count; i++ ) {
                            Map.DataLayer layer = layers[i];
                            layer.Offset = mapStream.Position;
                            Map.WriteLayer( layer, bs );
                            bs.Flush();
                            ds.Flush();
                            layer.CompressedLength = (int)(mapStream.Position - layer.Offset);
                        }
                    }
                }

                // come back to write the index
                writer.BaseStream.Seek( indexOffset, SeekOrigin.Begin );
                for( int i = 0; i < layers.Count; i++ ) {
                    writer.Write( (byte)layers[i].Type );
                    writer.Write( (long)layers[i].Offset ); // written later
                    writer.Write( (int)layers[i].CompressedLength );  // written later
                    writer.Write( (int)layers[i].GeneralPurposeField );
                    writer.Write( (int)layers[i].ElementSize );  // -1 for PlayerIDs
                    writer.Write( (int)layers[i].ElementCount ); // to be written later for PlayerIDs
                }
                writer.Write( metaCount );

                return true;
            }
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


        static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );

        public static uint DateTimeToTimestamp( DateTime timestamp ) {
            return (uint)(timestamp - UnixEpoch).TotalSeconds;
        }

        public static DateTime TimestampToDateTime( uint timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }
    }
}