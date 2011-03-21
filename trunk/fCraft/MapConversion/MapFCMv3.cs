// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace fCraft.MapConversion {
    /// <summary>
    /// fCraft map format converter, for format version #3 (2011)
    /// </summary>
    sealed class MapFCMv3 : IMapConverter {
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
                    throw new MapFormatException();
                }

                Map map = new Map {
                                      WidthX = reader.ReadInt16(),
                                      Height = reader.ReadInt16(),
                                      WidthY = reader.ReadInt16()
                                  };

                // read dimensions

                return map;
            }
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                BinaryReader reader = new BinaryReader( mapStream );
                if( reader.ReadInt32() != Identifier || reader.ReadByte() != Revision ) {
                    throw new MapFormatException();
                }

                Map map = new Map {
                                      WidthX = reader.ReadInt16(),
                                      Height = reader.ReadInt16(),
                                      WidthY = reader.ReadInt16()
                                  };
                // read dimensions

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "MapFCMv3.Load: One or more of the map dimensions are invalid." );
                }

                // read spawn
                map.Spawn.X = (short)reader.ReadInt32();
                map.Spawn.H = (short)reader.ReadInt32();
                map.Spawn.Y = (short)reader.ReadInt32();
                map.Spawn.R = reader.ReadByte();
                map.Spawn.L = reader.ReadByte();

                // read modification/creation times
                map.DateModified = Server.TimestampToDateTime( reader.ReadUInt32() );
                map.DateCreated = Server.TimestampToDateTime( reader.ReadUInt32() );

                // read UUID
                map.Guid = new Guid( reader.ReadBytes( 16 ) );


                // read the index
                int layerCount = reader.ReadByte();
                List<Map.DataLayer> layers = new List<Map.DataLayer>( layerCount );
                for( int i = 0; i < layerCount; i++ ) {
                    Map.DataLayer layer = new Map.DataLayer {
                        Type = (Map.DataLayerType)reader.ReadByte(),
                        Offset = reader.ReadInt64(),
                        CompressedLength = reader.ReadInt32(),
                        GeneralPurposeField = reader.ReadInt32(),
                        ElementSize = reader.ReadInt32(),
                        ElementCount = reader.ReadInt32()
                    };
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
                                map.AddZone( new Zone( newValue, map.World ) );
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
                map.ChangedSinceSave = false;
                return map;
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            using( FileStream mapStream = File.Create( fileName ) ) {
                BinaryWriter writer = new BinaryWriter( mapStream );

                writer.Write( Identifier );
                writer.Write( Revision );

                writer.Write( (short)mapToSave.WidthX );
                writer.Write( (short)mapToSave.Height );
                writer.Write( (short)mapToSave.WidthY );

                writer.Write( (int)mapToSave.Spawn.X );
                writer.Write( (int)mapToSave.Spawn.H );
                writer.Write( (int)mapToSave.Spawn.Y );

                writer.Write( mapToSave.Spawn.R );
                writer.Write( mapToSave.Spawn.L );

                mapToSave.DateModified = DateTime.UtcNow;
                writer.Write( (uint)Server.DateTimeToTimestamp( mapToSave.DateModified ) );
                writer.Write( (uint)Server.DateTimeToTimestamp( mapToSave.DateCreated ) );

                writer.Write( mapToSave.Guid.ToByteArray() );

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
                    writer.Write( layers[i].Offset ); // written later
                    writer.Write( layers[i].CompressedLength );  // written later
                    writer.Write( layers[i].GeneralPurposeField );
                    writer.Write( layers[i].ElementSize );  // -1 for PlayerIDs
                    writer.Write( layers[i].ElementCount ); // to be written later for PlayerIDs
                }
                writer.Write( metaCount );

                return true;
            }
        }


        public static string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = reader.ReadUInt16();
            byte[] stringData = reader.ReadBytes( length );
            return Encoding.ASCII.GetString( stringData );
        }


        public static void WriteLengthPrefixedString( BinaryWriter writer, string s ) {
            byte[] stringData = Encoding.ASCII.GetBytes( s );
            writer.Write( (ushort)stringData.Length );
            writer.Write( stringData );
        }

    }
}