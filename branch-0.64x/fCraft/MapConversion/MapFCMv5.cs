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
            get { return MapFormat.FCMv5; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return ( NbtFile.ReadRootTagName( fileName ) == RootTagName );
        }


        static bool HeaderTagSelector( NbtTag tag ) {
            return tag.Parent == null || tag.Parent.Name != "MapData" || tag.Name != "BlockData";
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            NbtFile file = new NbtFile( fileName, NbtCompression.AutoDetect, HeaderTagSelector );
            NbtCompound root = file.RootTag;
            return LoadHeaderInternal( root );
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            NbtFile file = new NbtFile( fileName, NbtCompression.AutoDetect, HeaderTagSelector );
            NbtCompound root = file.RootTag;
            return LoadHeaderInternal( root );
        }


        static Map LoadHeaderInternal( [NotNull] NbtCompound root ) {
            if( root.Name != RootTagName ) {
                throw new MapFormatException( "Incorrect root tag name" );
            }
            NbtCompound mapData = root.Get<NbtCompound>( "MapData" );
            NbtCompound spawn = root.Get<NbtCompound>( "Spawn" );
            NbtCompound backupSettings = root.Get<NbtCompound>( "BackupSettings" );
            NbtCompound accessPerms = root.Get<NbtCompound>( "AccessPermissions" );
            NbtCompound buildPerms = root.Get<NbtCompound>( "BuildPermissions" );
            NbtCompound environment = root.Get<NbtCompound>( "Environment" );
            NbtCompound blockDBSettings = root.Get<NbtCompound>( "BlockDBSettings" );
            NbtList zones = root.Get<NbtList>( "Zones" );
            NbtCompound mapCustomData = root.Get<NbtCompound>( "MapCustomData" );
            NbtCompound worldCustomData = root.Get<NbtCompound>( "WorldCustomData" );
            NbtCompound events = root.Get<NbtCompound>( "Events" );

            if( mapData == null || spawn == null || backupSettings == null ||
                accessPerms == null || buildPerms == null || environment == null ||
                blockDBSettings == null || zones == null || mapCustomData == null ||
                worldCustomData == null || events == null ) {
                throw new MapFormatException( "Some of the required metadata is missing." );
            }

            Map map = new Map( null,
                               mapData["Width"].ShortValue,
                               mapData["Length"].ShortValue,
                               mapData["Height"].ShortValue,
                               false );
            map.Spawn = new Position( spawn["X"].ShortValue, spawn["Y"].ShortValue, spawn["Z"].ShortValue,
                                      spawn["R"].ByteValue, spawn["L"].ByteValue );
            // TODO: BackupSettings
            // TODO: AccessPerms
            // TODO: BuildPerms
            // TODO: Environment
            // TODO: BlockDBSettings

            foreach( NbtCompound zoneTag in zones ) {
                try {
                    map.Zones.Add( new Zone( zoneTag ) );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error, "Error parsing a zone: {0}", ex );
                }
            }

            // TODO: MapCustomData
            // TODO: WorldCustomData
            // TODO: Events
            return map;
        }


        public void Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
        }
    }
}