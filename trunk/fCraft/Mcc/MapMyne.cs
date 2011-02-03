// 
//  Authors:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010-2011, Tyler Kennedy & Matvei Stefarov
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this
//       list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of MCC nor the names of its contributors may be
//       used to endorse or promote products derived from this software without
//       specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using fCraft;

namespace Mcc {
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
            Map map = new Map();
            using( Stream metaStream = File.OpenRead( fullMetaDataFileName ) ) {
                LoadMeta( map, metaStream );
            }
            return map;
        }


        public Map Load( string dirName ) {
            string fullBlockStoreFileName = Path.Combine( dirName, BlockStoreFileName );
            string fullMetaDataFileName = Path.Combine( dirName, MetaDataFileName );

            if( !File.Exists( fullBlockStoreFileName ) || !File.Exists( fullMetaDataFileName ) ) {
                throw new FileNotFoundException( "When loading myne maps, both .gz and .meta files are required." );
            }

            Map map = new Map();
            using( Stream metaStream = File.OpenRead( fullMetaDataFileName ) ) {
                LoadMeta( map, metaStream );
            }
            using( Stream dataStream = File.OpenRead( fullBlockStoreFileName ) ) {
                LoadBlocks( map, dataStream );
            }

            return map;
        }


        void LoadBlocks( Map map, Stream mapStream ) {
            mapStream.Seek( 0, SeekOrigin.Begin );

            // Setup a GZipStream to decompress and read the map file
            GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );

            int blockCount = IPAddress.HostToNetworkOrder( bs.ReadInt32() );
            if( blockCount != map.widthY * map.widthX * map.height ) {
                throw new Exception( "Map dimensions in the metadata do not match dimensions of the block array." );
            }

            map.blocks = new byte[blockCount];
            bs.Read( map.blocks, 0, map.blocks.Length );
        }


        void LoadMeta( Map map, Stream stream ) {
            INIFile metaFile = new INIFile( stream );
            if( metaFile.IsEmpty() ) {
                throw new Exception( "Metadata file is empty or incorrectly formatted." );
            }
            if( !metaFile.Contains( "size", "x", "y", "z" ) ) {
                throw new Exception( "Metadata file is missing map dimensions." );
            }

            map.widthX = Int32.Parse( metaFile["size", "x"] );
            map.widthY = Int32.Parse( metaFile["size", "z"] );
            map.height = Int32.Parse( metaFile["size", "y"] );

            if( !map.ValidateHeader() ) {
                throw new MapFormatException( "MapFCMv3.Load: One or more of the map dimensions are invalid." );
            }

            if( metaFile.Contains( "spawn", "x", "y", "z", "h" ) ) {
                map.spawn.Set( Int16.Parse( metaFile["spawn", "x"] ) * 32 + 16,
                               Int16.Parse( metaFile["spawn", "z"] ) * 32 + 16,
                               Int16.Parse( metaFile["spawn", "y"] ) * 32 + 16,
                               Byte.Parse( metaFile["spawn", "h"] ),
                               0 );
            } else {
                map.ResetSpawn();
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            throw new NotImplementedException();
        }
    }


    class INIFile {
        public string separator = "=";
        Dictionary<string, Dictionary<string, string>> contents = new Dictionary<string, Dictionary<string, string>>();

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
                } else if( line.Contains( separator ) && section != null ) {
                    string keyName = line.Substring( 0, line.IndexOf( separator ) ).TrimEnd().ToLower();
                    string valueName = line.Substring( line.IndexOf( separator ) + 1 ).TrimStart();
                    section.Add( keyName, valueName );
                }
            }
        }

        public bool ContainsSection( string section ) {
            return contents.ContainsKey( section.ToLower() );
        }

        public bool Contains( string section, params string[] keys ) {
            if( contents.ContainsKey( section.ToLower() ) ) {
                foreach( string key in keys ) {
                    if( !contents[section.ToLower()].ContainsKey( key.ToLower() ) ) return false;
                }
                return true;
            } else {
                return false;
            }
        }

        public bool IsEmpty() {
            return (contents.Count == 0);
        }
    }
}