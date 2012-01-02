// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;

namespace fCraft.MapConversion {
    /// <summary> NBT map conversion implementation, for converting NBT map format into fCraft's default map format. </summary>
    public sealed class MapNBT : IMapConverter {
        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        public string ServerName {
            get { return "InDev"; }
        }


        /// <summary> Returns the map storage type (file-based or directory-based). </summary>
        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        /// <summary> Returns the format name. </summary>
        public MapFormat Format {
            get { return MapFormat.NBT; }
        }

        /// <summary> Whether or not the specified file is used by this format, determined by file extension. </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".mclevel", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary> Whether or not the specified file is used by this format, determined by Identifier in file. </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                    BinaryReader bs = new BinaryReader( gs );
                    return (bs.ReadByte() == 10 && NBTag.ReadString( bs ) == "MinecraftLevel");
                }
            } catch( Exception ) {
                return false;
            }
        }

        /// <summary> Loads the specified file, and creates a map with the specified dimension. </summary>
        /// <param name="fileName"> File to load. </param>
        /// <returns> Map instance from the specified file. </returns>
        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            Map map = Load( fileName );
            map.Blocks = null;
            return map;
        }

        /// <summary> Loads the specified file, and creates a map with the specified dimensions, and spawn point. </summary>
        /// <param name="fileName"> File to load. </param>
        /// <returns> Map instance from the specified file. </returns>
        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                NBTag tag = NBTag.ReadStream( gs );

                NBTag mapTag = tag["Map"];
                // ReSharper disable UseObjectOrCollectionInitializer
                Map map = new Map( null,
                                   mapTag["Width"].GetShort(),
                                   mapTag["Length"].GetShort(),
                                   mapTag["Height"].GetShort(),
                                   false );
                // ReSharper restore UseObjectOrCollectionInitializer
                map.Spawn = new Position {
                    X = mapTag["Spawn"][0].GetShort(),
                    Z = mapTag["Spawn"][1].GetShort(),
                    Y = mapTag["Spawn"][2].GetShort(),
                    R = 0,
                    L = 0
                };

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "One or more of the map dimensions are invalid." );
                }

                map.Blocks = mapTag["Blocks"].GetBytes();
                map.RemoveUnknownBlocktypes();

                return map;
            }
        }

        /// <summary> Saves the specified map, with the specified file name. </summary>
        /// <param name="mapToSave"> Map to save. </param>
        /// <param name="fileName"> File name to save the map under. </param>
        /// <returns> Whether the operation completed successfully. </returns>
        public bool Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            throw new NotImplementedException();
        }
    }
}