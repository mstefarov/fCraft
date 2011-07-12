// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;

namespace fCraft.MapConversion {
    public sealed class MapNBT : IMapConverter {

        public string ServerName {
            get { return "InDev"; }
        }


        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.NBT; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".mclevel", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
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


        public Map LoadHeader( string fileName ) {
            Map map = Load( fileName );
            map.Blocks = null;
            return map;
        }


        public Map Load( string fileName ) {
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
                map.Spawn = new Position {
                    X = mapTag["Spawn"][0].GetShort(),
                    H = mapTag["Spawn"][1].GetShort(),
                    Y = mapTag["Spawn"][2].GetShort(),
                    R = 0,
                    L = 0
                };
                // ReSharper restore UseObjectOrCollectionInitializer

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "One or more of the map dimensions are invalid." );
                }

                map.Blocks = mapTag["Blocks"].GetBytes();
                map.RemoveUnknownBlocktypes();

                return map;
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            throw new NotImplementedException();
        }
    }
}