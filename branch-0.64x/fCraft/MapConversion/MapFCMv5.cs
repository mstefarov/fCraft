// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;
using LibNbt;

namespace fCraft.MapConversion {
    /// <summary> fCraft map format converter, for format version #5 (0.640+). </summary>
    public sealed class MapFCMv5 : IMapImporter, IMapExporter {
        const string RootTagName = "fCraftMap";

        public string ServerName {
            get { return "fCraft"; }
        }

        public bool SupportsImport {
            get { return true; }
        }

        public bool SupportsExport {
            get { return true; }
        }

        public string FileExtension {
            get { return "fcm"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.FCMv3; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return ( NbtFile.ReadRootTagName( fileName ) == "fCraftMap" );
        }


        static bool HeaderTagSelector( NbtTag tag ) {
            return tag.Parent == null || tag.Parent.Name != "MapData" || tag.Name != "BlockData";
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            NbtFile file = new NbtFile( fileName, NbtCompression.AutoDetect, HeaderTagSelector );
            NbtTag root = file.RootTag;
            return LoadHeaderInternal( root );
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            NbtFile file = new NbtFile( fileName, NbtCompression.AutoDetect, HeaderTagSelector );
            NbtTag root = file.RootTag;
            return LoadHeaderInternal( root );
        }


        static Map LoadHeaderInternal( [NotNull] NbtTag root ) {
            return new Map( null, 16, 16, 16, true );
        }


        public void Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
        }
    }
}