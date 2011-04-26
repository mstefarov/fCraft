// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
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
            throw new NotImplementedException();
        }


        /// <summary> Fully loads map from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        public Map Load( string path ) {
            throw new NotImplementedException();
        }


        /// <summary> Saves given map at the given location. </summary>
        /// <returns> true if saving succeeded. </returns>
        public bool Save( Map mapToSave, string path ) {
            throw new NotImplementedException();
        }

    }
}
