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


                // read the index
                int layerCount = reader.ReadByte();
                List<DataLayer> layers = new List<DataLayer>( layerCount );
                for( int i = 0; i < layerCount; i++ ) {
                    DataLayer layer = new DataLayer {
                        Type = (DataLayerType)reader.ReadByte(),
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

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "One or more of the map dimensions are invalid." );
                }

                // read modification/creation times
                map.DateModified = reader.ReadUInt32().ToDateTime();
                map.DateCreated = reader.ReadUInt32().ToDateTime();

                // read UUID
                map.Guid = new Guid( reader.ReadBytes( 16 ) );


                // read the index
                int layerCount = reader.ReadByte();
                List<DataLayer> layers = new List<DataLayer>( layerCount );
                for( int i = 0; i < layerCount; i++ ) {
                    DataLayer layer = new DataLayer {
                        Type = (DataLayerType)reader.ReadByte(),
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
                        ReadLayer( layers[i], ds, map );
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

                writer.Write( (short)mapToSave.WidthX );
                writer.Write( (short)mapToSave.Height );
                writer.Write( (short)mapToSave.WidthY );

                writer.Write( (int)mapToSave.Spawn.X );
                writer.Write( (int)mapToSave.Spawn.H );
                writer.Write( (int)mapToSave.Spawn.Y );

                writer.Write( mapToSave.Spawn.R );
                writer.Write( mapToSave.Spawn.L );

                mapToSave.DateModified = DateTime.UtcNow;
                writer.Write( (uint)mapToSave.DateModified.ToTimestamp() ); // extension methods
                writer.Write( (uint)mapToSave.DateCreated.ToTimestamp() );

                writer.Write( mapToSave.Guid.ToByteArray() );

                writer.Write( (byte)1 );

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
                        offset = mapStream.Position;
                        bs.Write( blocksCache, 0, blocksCache.Length );
                        bs.Flush();
                        ds.Flush();
                        compressedLength = (int)(mapStream.Position - offset);
                    }
                }

                // come back to write the index
                writer.BaseStream.Seek( indexOffset, SeekOrigin.Begin );
                writer.Write( (byte)0 );
                writer.Write( offset ); // written later
                writer.Write( compressedLength );  // written later
                writer.Write( 0);
                writer.Write( 1 );
                writer.Write( blocksCache.Length ); // to be written later for PlayerIDs
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


        static void ReadLayer( DataLayer layer, DeflateStream stream, Map map ) {
            if( layer == null ) throw new ArgumentNullException( "layer" );
            if( stream == null ) throw new ArgumentNullException( "stream" );
            switch( layer.Type ) {
                case DataLayerType.Blocks:
                    map.Blocks = new byte[layer.ElementCount];
                    stream.Read( map.Blocks, 0, map.Blocks.Length );
                    map.RemoveUnknownBlocktypes( false );
                    break;

                default:
                    Logger.Log( "Map.ReadLayer: Skipping unknown layer ({0})", LogType.Warning, layer.Type );
                    stream.BaseStream.Seek( layer.CompressedLength, SeekOrigin.Current );
                    break;
            }
        }


        sealed class DataLayer {
            public DataLayerType Type;        // see "DataLayerType" below
            public int GeneralPurposeField;   // 32 bits that can be used in implementation-specific ways
            public int ElementSize;           // size of each data element (if elements are variable-size, set this to 1)
            public int ElementCount;          // number of fixed-sized elements (if elements are variable-size, set this to total number of bytes)
            public long Offset;
            public int CompressedLength;
        }


        // type of block - allows storing multiple layers of information about blocks
        enum DataLayerType : byte {
            Blocks = 0, // Block types (El.Size=1)

            BlockUndo = 1, // Previous block type (per-block) (El.Size=1)

            BlockOwnership = 2, // IDs of block changers (per-block) (El.Size=2)

            BlockTimestamps = 3, // Modification date/time (per-block) (El.Size=4)

            BlockChangeFlags = 4, // Type of action that resulted in the block change
            // See BlockChangeFlags flags (El.Size=1)

            PlayerIDs = 5  // mapping of player names to ID numbers (El.Size=2)

            // 4-31 reserved
            // 32-255 custom

        } // 1 byte
    }
}