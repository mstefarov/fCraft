// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace fCraft.MapConversion {
    public sealed class MapMyne : IMapConverter {

        const string BlockStoreFileName = "blocks.gz";
        const string MetaDataFileName = "world.meta";


        public string ServerName {
            get { return "Myne/MyneCraft/HyveBuild/iCraft"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.Directory; }
        }


        public MapFormat Format {
            get { return MapFormat.Myne; }
        }


        public bool ClaimsName( string dirName ) {
            return Directory.Exists( dirName ) &&
                   File.Exists( Path.Combine( dirName, BlockStoreFileName ) ) &&
                   File.Exists( Path.Combine( dirName, MetaDataFileName ) );
        }


        public bool Claims( string dirName ) {
            return ClaimsName( dirName );
        }


        public Map LoadHeader( string dirName ) {
            string fullMetaDataFileName = Path.Combine( dirName, MetaDataFileName );
            Map map;
            using( Stream metaStream = File.OpenRead( fullMetaDataFileName ) ) {
                map = LoadMeta( metaStream );
            }
            return map;
        }


        public Map Load( string dirName ) {
            string fullBlockStoreFileName = Path.Combine( dirName, BlockStoreFileName );
            string fullMetaDataFileName = Path.Combine( dirName, MetaDataFileName );

            if( !File.Exists( fullBlockStoreFileName ) || !File.Exists( fullMetaDataFileName ) ) {
                throw new FileNotFoundException( "When loading myne maps, both .gz and .meta files are required." );
            }

            Map map;
            using( Stream metaStream = File.OpenRead( fullMetaDataFileName ) ) {
                map = LoadMeta( metaStream );
            }
            using( Stream dataStream = File.OpenRead( fullBlockStoreFileName ) ) {
                LoadBlocks( map, dataStream );
            }

            return map;
        }


        static void LoadBlocks( Map map, Stream mapStream ) {
            mapStream.Seek( 0, SeekOrigin.Begin );

            // Setup a GZipStream to decompress and read the map file
            GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );

            int blockCount = IPAddress.HostToNetworkOrder( bs.ReadInt32() );
            if( blockCount != map.WidthY * map.WidthX * map.Height ) {
                throw new Exception( "Map dimensions in the metadata do not match dimensions of the block array." );
            }

            map.Blocks = new byte[blockCount];
            bs.Read( map.Blocks, 0, map.Blocks.Length );
            map.RemoveUnknownBlocktypes( false );
        }


        static Map LoadMeta( Stream stream ) {
            INIFile metaFile = new INIFile( stream );
            if( metaFile.IsEmpty() ) {
                throw new Exception( "Metadata file is empty or incorrectly formatted." );
            }
            if( !metaFile.Contains( "size", "x", "y", "z" ) ) {
                throw new Exception( "Metadata file is missing map dimensions." );
            }

            int widthX = Int32.Parse( metaFile["size", "x"] );
            int widthY = Int32.Parse( metaFile["size", "z"] );
            int height = Int32.Parse( metaFile["size", "y"] );

            Map map = new Map( null, widthX, widthY, height, false );

            if( !map.ValidateHeader() ) {
                throw new MapFormatException( "One or more of the map dimensions are invalid." );
            }

            if( metaFile.Contains( "spawn", "x", "y", "z", "h" ) ) {
                Position spawn = new Position {
                    X = (short)(Int16.Parse( metaFile["spawn", "x"] ) * 32 + 16),
                    Y = (short)(Int16.Parse( metaFile["spawn", "z"] ) * 32 + 16),
                    H = (short)(Int16.Parse( metaFile["spawn", "y"] ) * 32 + 16),
                    R = Byte.Parse( metaFile["spawn", "h"] ),
                    L = 0
                };
                map.SetSpawn( spawn );
            } else {
                map.ResetSpawn();
            }
            return map;
        }


        public bool Save( Map mapToSave, string fileName ) {
            throw new NotImplementedException();
        }
    }


    sealed class INIFile {
        const string Separator = "=";
        readonly Dictionary<string, Dictionary<string, string>> contents = new Dictionary<string, Dictionary<string, string>>();

        public string this[string section, string key] {
            get {
                return contents[section][key];
            }
            set {
                if( !contents.ContainsKey( section ) ) {
                    contents[section] = new Dictionary<string, string>();
                }
                contents[section][key] = value;
            }
        }

        public INIFile( Stream fileStream ) {
            StreamReader reader = new StreamReader( fileStream );
            Dictionary<string, string> section = null;
            while( !reader.EndOfStream ) {
                string line = reader.ReadLine().Trim();
                if( line.StartsWith( "#" ) ) continue;
                if( line.StartsWith( "[" ) ) {
                    string sectionName = line.Substring( 1, line.IndexOf( ']' ) - 1 ).Trim().ToLower();
                    section = new Dictionary<string, string>();
                    contents.Add( sectionName, section );
                } else if( line.Contains( Separator ) && section != null ) {
                    string keyName = line.Substring( 0, line.IndexOf( Separator ) ).TrimEnd().ToLower();
                    string valueName = line.Substring( line.IndexOf( Separator ) + 1 ).TrimStart();
                    section.Add( keyName, valueName );
                }
            }
        }

        public bool ContainsSection( string section ) {
            return contents.ContainsKey( section.ToLower() );
        }

        public bool Contains( string section, params string[] keys ) {
            if( contents.ContainsKey( section.ToLower() ) ) {
                return keys.All( key => contents[section.ToLower()].ContainsKey( key.ToLower() ) );
            } else {
                return false;
            }
        }

        public bool IsEmpty() {
            return (contents.Count == 0);
        }
    }
}