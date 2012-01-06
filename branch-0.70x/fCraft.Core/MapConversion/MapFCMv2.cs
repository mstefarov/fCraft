// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> fCraft map format converter, for obsolete format version #2 (2010). </summary>
    public sealed class MapFCMv2 : IMapConverter {
        /// <summary> ID used to differentiate this format from past and future formats </summary>
        private const uint Identifier = 0xfc000002;

        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        public string ServerName {
            get { return "fCraft"; }
        }


        /// <summary> Returns the map storage type (file-based or directory-based). </summary>
        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        /// <summary> Returns the format name. </summary>
        public MapFormat Format {
            get { return MapFormat.FCMv2; }
        }


        /// <summary> Returns true if the filename (or directory name) matches this format's expectations. </summary>
        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        /// <summary> Allows validating the map format while using minimal resources. </summary>
        /// <returns> Returns true if specified file/directory is valid for this format. </returns>
        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    BinaryReader reader = new BinaryReader( mapStream );
                    return (reader.ReadUInt32() == Identifier);
                }
            } catch( Exception ) {
                return false;
            }

        }


        /// <summary> Attempts to load map dimensions from specified location.
        /// Throws MapFormatException on failure. </summary>
        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                return LoadHeaderInternal( mapStream );
            }
        }


        static Map LoadHeaderInternal( [NotNull] Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryReader reader = new BinaryReader( stream );

            // Read in the magic number
            if( reader.ReadUInt32() != Identifier ) {
                throw new MapFormatException();
            }

            // Read in the map dimesions
            int width = reader.ReadInt16();
            int length = reader.ReadInt16();
            int height = reader.ReadInt16();

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map( null, width, length, height, false );
            // ReSharper restore UseObjectOrCollectionInitializer

            // Read in the spawn location
            map.Spawn = new Position {
                X = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                Z = reader.ReadInt16(),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };

            return map;
        }


        /// <summary> Fully loads map from specified location.
        /// Throws MapFormatException on failure. </summary>
        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
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
                            map.Zones.Add( new Zone( value, map.World ) );
                        } catch( Exception ex ) {
                            Logger.Log( LogType.Error,
                                        "MapFCMv2.Load: Error importing zone definition: {0}", ex );
                        }
                    } else {
                        Logger.Log( LogType.Warning,
                                    "MapFCMv2.Load: Metadata discarded: \"{0}\"=\"{1}\"",
                                    key, value );
                    }
                }

                // Read in the map data
                map.Blocks = new Byte[map.Volume];
                using( GZipStream decompressor = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    decompressor.Read( map.Blocks, 0, map.Blocks.Length );
                }

                map.RemoveUnknownBlocktypes();

                return map;
            }
        }


        /// <summary> Saves given map at the given location. </summary>
        /// <returns> True if saving succeeded; otherwise false. </returns>
        public bool Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            throw new NotImplementedException();
        }


        static string ReadLengthPrefixedString( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            int length = reader.ReadInt32();
            byte[] stringData = reader.ReadBytes( length );
            return Encoding.ASCII.GetString( stringData );
        }
    }
}