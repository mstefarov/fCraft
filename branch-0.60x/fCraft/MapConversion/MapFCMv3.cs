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

                        string oldValue;
                        if( map.Metadata.TryGetValue( key, group, out oldValue ) && oldValue != newValue ) {
                            Logger.Log( "MapFCMv3.LoadHeader: Duplicate metadata entry found for [{0}].[{1}]. " +
                                        "Old value (overwritten): \"{2}\". New value: \"{3}\"", LogType.Warning,
                                        group, key, oldValue, newValue );
                        }
                        if( group == "zones" ) {
                            try {
                                map.AddZone( new Zone( newValue, map.World ) );
                            } catch( Exception ex ) {
                                Logger.Log( "MapFCMv3.LoadHeader: Error importing zone definition: {0}", LogType.Error, ex );
                            }
                        } else {
                            map.Metadata[group, key] = newValue;
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

                        string oldValue;
                        if( map.Metadata.TryGetValue( key, group, out oldValue ) && oldValue != newValue ) {
                            Logger.Log( "MapFCMv3.LoadHeader: Duplicate metadata entry found for [{0}].[{1}]. " +
                                        "Old value (overwritten): \"{2}\". New value: \"{3}\"", LogType.Warning,
                                        group, key, oldValue, newValue );
                        }
                        if( group == "zones" ) {
                            try {
                                map.AddZone( new Zone( newValue, map.World ) );
                            } catch( Exception ex ) {
                                Logger.Log( "MapFCMv3.LoadHeader: Error importing zone definition: {0}", LogType.Error, ex );
                            }
                        } else {
                            map.Metadata[group, key] = newValue;
                        }
                    }
                    map.Blocks = new byte[map.Volume];
                    ds.Read( map.Blocks, 0, map.Blocks.Length );
                    map.RemoveUnknownBlocktypes();

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
            map.Spawn = new Position {
                X = (short)reader.ReadInt32(),
                H = (short)reader.ReadInt32(),
                Y = (short)reader.ReadInt32(),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };


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
                writer.Write( (uint)mapToSave.DateModified.ToUnixTime() );
                writer.Write( (uint)mapToSave.DateCreated.ToUnixTime() );

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
                        metaCount = WriteMetadata( ds, mapToSave );
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


        public static void WriteLengthPrefixedString( BinaryWriter writer, string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > ushort.MaxValue ) throw new ArgumentException( "String is too long.", "str" );
            byte[] stringData = Encoding.ASCII.GetBytes( str );
            writer.Write( (ushort)stringData.Length );
            writer.Write( stringData );
        }


        static int WriteMetadata( Stream stream, Map map ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryWriter writer = new BinaryWriter( stream );
            int metaCount = 0;
            lock( map.Metadata.SyncRoot ) {
                foreach( MetadataEntry entry in map.Metadata ) {
                    WriteLengthPrefixedString( writer, entry.Group );
                    WriteLengthPrefixedString( writer, entry.Key );
                    WriteLengthPrefixedString( writer, entry.Value );
                    metaCount++;
                }
            }

            Zone[] zoneList = map.ZoneList;
            foreach( Zone zone in zoneList ) {
                WriteLengthPrefixedString( writer, "zones" );
                WriteLengthPrefixedString( writer, zone.Name );
                WriteLengthPrefixedString( writer, SerializeZone(zone) );
                metaCount++;
            }

            World world = map.World;
            if( world != null ) {
                WriteLengthPrefixedString( writer, "security" );
                WriteLengthPrefixedString( writer, "access" );
                WriteLengthPrefixedString( writer, world.AccessSecurity.Serialize().ToString() );
                WriteLengthPrefixedString( writer, "security" );
                WriteLengthPrefixedString( writer, "build" );
                WriteLengthPrefixedString( writer, world.BuildSecurity.Serialize().ToString() );
                metaCount += 2;
            }
            return metaCount;
        }


        static string SerializeZone( Zone zone ) {
            string xheader;
            if( zone.CreatedBy != null ) {
                xheader = zone.CreatedBy.Name + " " + zone.CreatedDate.ToCompactString() + " ";
            } else {
                xheader = "- - ";
            }

            if( zone.EditedBy != null ) {
                xheader += zone.EditedBy.Name + " " + zone.EditedDate.ToCompactString();
            } else {
                xheader += "- -";
            }

            var zoneExceptions = zone.Controller.ExceptionList;

            return String.Format( "{0},{1},{2},{3}",
                                  String.Format( "{0} {1} {2} {3} {4} {5} {6} {7}",
                                                 zone.Name,
                                                 zone.Bounds.XMin, zone.Bounds.YMin, zone.Bounds.HMin,
                                                 zone.Bounds.XMax, zone.Bounds.YMax, zone.Bounds.HMax,
                                                 zone.Controller.MinRank.GetFullName() ),
                                  zoneExceptions.Included.JoinToString( " ", p => p.Name ),
                                  zoneExceptions.Excluded.JoinToString( " ", p => p.Name ),
                                  xheader );
        }
    }
}