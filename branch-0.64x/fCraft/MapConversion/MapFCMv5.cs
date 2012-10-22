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
            NbtCompound mapDataTag = root.Get<NbtCompound>( "MapData" );
            NbtCompound spawnTag = root.Get<NbtCompound>( "Spawn" );
            NbtCompound backupSettingsTag = root.Get<NbtCompound>( "BackupSettings" );
            NbtCompound accessPermissionsTag = root.Get<NbtCompound>( "AccessPermissions" );
            NbtCompound buildPermissionsTag = root.Get<NbtCompound>( "BuildPermissions" );
            NbtCompound environmentTag = root.Get<NbtCompound>( "Environment" );
            NbtCompound blockDBSettingsTag = root.Get<NbtCompound>( "BlockDBSettings" );
            NbtList zonesTag = root.Get<NbtList>( "Zones" );
            NbtCompound mapCustomDataTag = root.Get<NbtCompound>( "MapCustomData" );
            NbtCompound worldCustomDataTag = root.Get<NbtCompound>( "WorldCustomData" );
            NbtCompound eventsTag = root.Get<NbtCompound>( "Events" );

            if( mapDataTag == null || spawnTag == null || backupSettingsTag == null ||
                accessPermissionsTag == null || buildPermissionsTag == null || environmentTag == null ||
                blockDBSettingsTag == null || zonesTag == null || mapCustomDataTag == null ||
                worldCustomDataTag == null || eventsTag == null ) {
                throw new MapFormatException( "Some of the required metadata is missing." );
            }

            Map map = new Map( null,
                               mapDataTag["Width"].ShortValue,
                               mapDataTag["Length"].ShortValue,
                               mapDataTag["Height"].ShortValue,
                               false );
            map.Spawn = new Position( spawnTag );
            // TODO: BackupSettings
            // TODO: AccessPerms
            // TODO: BuildPerms
            // TODO: Environment
            // TODO: BlockDBSettings

            foreach( NbtCompound zoneTag in zonesTag ) {
                try {
                    map.Zones.Add( new Zone( zoneTag ) );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error, "Error parsing a zone definition: {0}", ex );
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