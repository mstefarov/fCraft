// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
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

                Map map = LoadHeaderInternal( reader );

                // skip the index
                int layerCount = reader.ReadByte();
                mapStream.Seek( 25 * layerCount, SeekOrigin.Current );

                // read metadata
                int metaCount = reader.ReadInt32();

                using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Decompress ) ) {
                    BinaryReader br = new BinaryReader( ds );
                    for( int i = 0; i < metaCount; i++ ) {
                        string group = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string key = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string newValue = ReadLengthPrefixedString( br );

                        string oldValue = map.GetMeta( group, key );

                        if( oldValue != null && oldValue != newValue ) {
                            Logger.Log( "MapFCMv3.LoadHeader: Duplicate metadata entry found for [{0}].[{1}]. " +
                                        "Old value (overwritten): \"{2}\". New value: \"{3}\"", LogType.Warning,
                                        group, key, map.GetMeta( group, key ), newValue );
                        }
                        if( group == "zones" ) {
                            try {
                                map.AddZone( new Zone( newValue, map.World ) );
                            } catch( Exception ex ) {
                                Logger.Log( "MapFCMv3.LoadHeader: Error importing zone definition: {0}", LogType.Error, ex );
                            }
                        } else {
                            map.SetMeta( group, key, newValue );
                        }
                    }
                }

                return map;
            }
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                BinaryReader reader = new BinaryReader( mapStream );

                Map map = LoadHeaderInternal( reader );

                // read the layer index
                if( reader.ReadByte() != 1 ) {
                    throw new MapFormatException( "Multiple layers are no longer supported in FCMv3" );
                }
                mapStream.Seek( 25, SeekOrigin.Current );

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
                    map.Blocks = new byte[map.WidthX * map.WidthY * map.Height];
                    ds.Read( map.Blocks, 0, map.Blocks.Length );
                    map.RemoveUnknownBlocktypes( false );

                }
                return map;
            }
        }


        static Map LoadHeaderInternal( BinaryReader reader ) {
            if( reader.ReadInt32() != Identifier || reader.ReadByte() != Revision ) {
                throw new MapFormatException();
            }

            // read dimensions
            int widthX = reader.ReadInt16();
            int height = reader.ReadInt16();
            int widthY = reader.ReadInt16();

            Map map = new Map( null, widthX, widthY, height, false );

            // read spawn
            map.Spawn.X = (short)reader.ReadInt32();
            map.Spawn.H = (short)reader.ReadInt32();
            map.Spawn.Y = (short)reader.ReadInt32();
            map.Spawn.R = reader.ReadByte();
            map.Spawn.L = reader.ReadByte();


            // read modification/creation times
            map.DateModified = reader.ReadUInt32().ToDateTime();
            map.DateCreated = reader.ReadUInt32().ToDateTime();

            // read UUID
            map.Guid = new Guid( reader.ReadBytes( 16 ) );
            return map;
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
                writer.Write( (uint)mapToSave.DateModified.ToTimestamp() );
                writer.Write( (uint)mapToSave.DateCreated.ToTimestamp() );

                writer.Write( mapToSave.Guid.ToByteArray() );

                writer.Write( (byte)1 ); // layer count

                // skip over index and metacount
                long indexOffset = mapStream.Position;
                writer.Seek( 29, SeekOrigin.Current );

                byte[] blocksCache = mapToSave.Blocks;
                int metaCount, compressedLength;
                long offset;
                using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Compress, true ) ) {
                    using( BufferedStream bs = new BufferedStream( ds ) ) {
                        // write metadata
                        metaCount = mapToSave.WriteMetadataFCMv3( ds );
                        bs.Flush();
                        ds.Flush();
                        offset = mapStream.Position;
                        bs.Write( blocksCache, 0, blocksCache.Length );
                        bs.Flush();
                        ds.Flush();
                        compressedLength = (int)(mapStream.Position - offset);
                    }
                }

                // come back to write the index
                writer.BaseStream.Seek( indexOffset, SeekOrigin.Begin );

                writer.Write( (byte)0 );            // data layer type (Blocks)
                writer.Write( offset );             // offset, in bytes, from start of stream
                writer.Write( compressedLength );   // compressed length, in bytes
                writer.Write( 0 );                  // general purpose field
                writer.Write( 1 );                  // element size
                writer.Write( blocksCache.Length ); // element count

                writer.Write( metaCount );
                return true;
            }
        }


        static string ReadLengthPrefixedString( BinaryReader reader ) {
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