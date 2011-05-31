// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace fCraft.MapConversion {
    /// <summary>
    /// fCraft map format converter, for format version #2 (2010)
    /// </summary>
    public sealed class MapFCMv2 : IMapConverter {
        public const uint Identifier = 0xfc000002;

        public string ServerName {
            get { return "fCraft"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.FCMv2; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    BinaryReader reader = new BinaryReader( mapStream );
                    return (reader.ReadUInt32() == Identifier);
                }
            } catch( Exception ) {
                return false;
            }

        }


        public Map LoadHeader( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                return LoadHeaderInternal( mapStream );
            }
        }


        static Map LoadHeaderInternal( Stream stream ) {
            BinaryReader reader = new BinaryReader( stream );

            // Read in the magic number
            if( reader.ReadUInt32() != Identifier ) {
                throw new MapFormatException();
            }

            // Read in the map dimesions
            int widthX = reader.ReadInt16();
            int widthY = reader.ReadInt16();
            int height = reader.ReadInt16();

            Map map = new Map( null, widthX, widthY, height, false );

            // Read in the spawn location
            map.Spawn.X = reader.ReadInt16();
            map.Spawn.Y = reader.ReadInt16();
            map.Spawn.H = reader.ReadInt16();

            // Read in the spawn orientation
            map.Spawn.R = reader.ReadByte();
            map.Spawn.L = reader.ReadByte();

            return map;
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {

                Map map = LoadHeaderInternal( mapStream );

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "One or more of the map dimensions are invalid." );
                }

                BinaryReader reader = new BinaryReader( mapStream );

                // Read the metadata
                int metaSize = reader.ReadUInt16();

                for( int i = 0; i < metaSize; i++ ) {
                    string key = ReadLengthPrefixedString( reader );
                    string value = ReadLengthPrefixedString( reader );
                    if( key.StartsWith( "@zone", StringComparison.OrdinalIgnoreCase ) ) {
                        try {
                            map.AddZone( new Zone( value, map.World ) );
                        } catch( Exception ex ) {
                            Logger.Log( "MapFCMv2.Load: Error importing zone definition: {0}", LogType.Error, ex );
                        }
                    } else {
                        map.SetMeta( key, value );
                    }
                }

                // Read in the map data
                map.Blocks = new Byte[map.GetBlockCount()];
                using( GZipStream decompressor = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    decompressor.Read( map.Blocks, 0, map.Blocks.Length );
                }

                map.RemoveUnknownBlocktypes( false );

                return map;
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            throw new NotImplementedException();
        }


        static string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = reader.ReadInt32();
            byte[] stringData = reader.ReadBytes( length );
            return Encoding.ASCII.GetString( stringData );
        }
    }
}