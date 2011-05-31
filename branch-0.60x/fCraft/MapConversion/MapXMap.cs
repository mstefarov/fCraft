// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.IO;

namespace fCraft.MapConversion {
    class MapXMap : IMapConverter {
        public const int FormatID = 88776580;                     // 88 77 45 80 - XMAP in ascii
        public const int FormatRevision = 20110319;               // This is based on the date the revision was finalized


        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        public string ServerName { get { return "(Universal)"; } }


        /// <summary> Returns the format type (file-based or directory-based). </summary>
        public MapFormatType FormatType { get { return MapFormatType.SingleFile; } }


        /// <summary> Returns the format name. </summary>
        public MapFormat Format { get { return MapFormat.XMap; } }


        /// <summary> Returns true if the filename (or directory name) matches this format's expectations. </summary>
        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".xmap", StringComparison.OrdinalIgnoreCase );
        }


        /// <summary> Allows validating the map format while using minimal resources. </summary>
        /// <returns> Returns true if specified file/directory is valid for this format. </returns>
        public bool Claims( string path ) {
            using( FileStream fs = File.OpenRead( path ) ) {
                BinaryReader reader = new BinaryReader( fs );
                return (reader.ReadInt32() == FormatID) && (reader.ReadInt32() == FormatRevision);
            }
        }


        /// <summary> Attempts to load map dimensions from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        public Map LoadHeader( string path ) {
            using( FileStream fs = File.OpenRead( path ) ) {
                return LoadInternal( fs, false );
            }
        }


        /// <summary> Fully loads map from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        public Map Load( string path ) {
            using( FileStream fs = File.OpenRead( path ) ) {
                return LoadInternal( fs, true );
            }
        }


        /// <summary> Saves given map at the given location. </summary>
        /// <returns> true if saving succeeded. </returns>
        public bool Save( Map mapToSave, string path ) {
            throw new NotImplementedException();
        }


        static Map LoadInternal( Stream stream, bool readLayers ) {
            BinaryReader bs = new BinaryReader( stream );

            // headers
            if( bs.ReadInt32() != FormatID ) {
                throw new MapFormatException( "Invalid XMap format ID." );
            }

            if( bs.ReadInt32() != FormatRevision ) {
                throw new MapFormatException( "Invalid XMap format revision." );
            }

            // map dimensions
            int widthX = bs.ReadInt32();
            int height = bs.ReadInt32();
            int widthY = bs.ReadInt32();

            Map map = new Map( null, widthX, widthY, height, false );

            // spawn
            Position spawn = new Position();
            spawn.X = (short)bs.ReadInt32();
            spawn.H = (short)bs.ReadInt32();
            spawn.Y = (short)bs.ReadInt32();
            spawn.R = bs.ReadByte();
            spawn.L = bs.ReadByte();

            map.SetSpawn( spawn );

            // creation/modification dates
            map.DateCreated = bs.ReadInt64().ToDateTime();
            map.DateModified = bs.ReadInt64().ToDateTime();


            int metaCount = bs.ReadInt32();
            int layerCount = bs.ReadInt32();

            // metadata
            for( int i = 0; i < metaCount; i++ ) {
                string groupName = ReadString( bs );
                int keyCount = bs.ReadInt32();
                for( int k = 0; k < keyCount; k++ ) {
                    string keyName = ReadString( bs );
                    string value = ReadString( bs );
                    // TODO: parse zones etc
                    map.SetMeta( groupName, keyName, value );
                }
            }

            // layers
            if( readLayers ) {
                for( int l = 0; l < layerCount; l++ ) {
                    string layerName = ReadString( bs );
                    int layerSize = bs.ReadInt32();
                    int layerFlags = bs.ReadInt32();



                    // TODO: map.SetLayer( layerType, layerSize, layerFlags, stream )
                }
            }

            return map;
        }


        internal static string ReadString( BinaryReader reader ) {
            int stringLength = reader.ReadInt32();
            return Encoding.ASCII.GetString( reader.ReadBytes( stringLength ) );
        }


        internal static void WriteString( BinaryWriter writer, string str ) {
            byte[] stringData = Encoding.ASCII.GetBytes( str );
            writer.Write( stringData.Length );
            writer.Write( stringData, 0, stringData.Length );
        }
    }


    public enum XMapLayerType {
        Unknown,

        /// <summary> Array of blocks that make up the world (1 byte per block). </summary>
        BlockArray,

        PlayerTable,        // Table of players that have been on the map
        BlockPhysicsCode,   // Definition of all the phyiscs. Blocks should reference these
        BlockUndo,          // Last change (per-block) ***Not Used by MCSharp***
        BlockProperties,    // Parallel array to block array, defining what physics code to run on specific blocks
        BlockAccessLevel,   // Parallel array of block access levels
        BlockOwner          // Parallel array of PlayerIDs
    }


    public class XDataLayer {
        public string Name { get; set; }
        public XMapLayerType LayerType { get; set; }
        public int Flags { get; set; }

        bool writeRaw = true;
        byte[] RawData;


        public XDataLayer( string name, int flags, int length, Stream stream ) {
            Name = name;
            Flags = flags;

            try {
                LayerType = (XMapLayerType)Enum.Parse( typeof( XMapLayerType ), name, true );
            } catch( ArgumentException ) {
                LayerType = XMapLayerType.Unknown;
            }

            if( LayerType == XMapLayerType.Unknown || !Enum.IsDefined( typeof( XMapLayerType ), LayerType ) ) {
                LayerType = XMapLayerType.Unknown;
                stream.Read( RawData, 0, length );
            }

            // check if layer type is known, and set writeRaw
        }


        public static XDataLayer LoadFromStream( Stream stream ) {
            BinaryReader br = new BinaryReader( stream );
            string layerName = MapXMap.ReadString( br );
            int layerSize = br.ReadInt32();
            int layerFlags = br.ReadInt32();
            return new XDataLayer( layerName, layerFlags, layerSize, stream );
        }


        public void SaveToStream( Stream stream ) {
            BinaryWriter writer = new BinaryWriter( stream );
            MapXMap.WriteString( writer, Name );
            if( writeRaw ) {
            } else {
            }
        }
    }
}
