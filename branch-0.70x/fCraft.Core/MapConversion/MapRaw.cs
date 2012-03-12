// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace fCraft.MapConversion {
    /// <summary> D3 map conversion implementation, for converting D3 map format into fCraft's default map format. </summary>
    public sealed class MapRaw : IMapExporter {

        public string ServerName {
            get { return "Raw"; }
        }

        public bool SupportsImport {
            get { return false; }
        }

        public bool SupportsExport {
            get { return true; }
        }

        public string FileExtension {
            get { return "raw"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.Raw; }
        }

        public bool Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                mapStream.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );
            }
            return true;
        }
    }
}