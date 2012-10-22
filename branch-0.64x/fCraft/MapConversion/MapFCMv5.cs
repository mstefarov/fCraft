// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Runtime.Serialization;
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
            NbtFile file = new NbtFile( fileName, NbtCompression.AutoDetect, null );
            NbtCompound root = file.RootTag;
            Map map = LoadHeaderInternal( root );
            map.Blocks = root["MapData"]["Blocks"].ByteArrayValue;
            return map;
        }


        static Map LoadHeaderInternal( [NotNull] NbtCompound root ) {
            if( root.Name != RootTagName ) {
                throw new MapFormatException( "MapFCMv5: Incorrect root tag name" );
            }
            NbtCompound mapDataTag = root.Get<NbtCompound>( "MapData" );
            NbtCompound spawnTag = root.Get<NbtCompound>( "Spawn" );
            NbtList zonesTag = root.Get<NbtList>( "Zones" );

            if( mapDataTag == null || spawnTag == null || zonesTag == null ) {
                throw new MapFormatException( "MapFCMv5: Some of the required metadata is missing." );
            }

            Map map = new Map( null,
                               mapDataTag["Width"].ShortValue,
                               mapDataTag["Length"].ShortValue,
                               mapDataTag["Height"].ShortValue,
                               false );
            map.DateCreated = DateTimeUtil.TryParseDateTime( mapDataTag["DateCreated"].LongValue );
            map.DateModified = DateTimeUtil.TryParseDateTime( mapDataTag["DateModified"].LongValue );
            map.Guid = new Guid( mapDataTag["GUID"].ByteArrayValue );
            map.GeneratorName = mapDataTag["GeneratorName"].StringValue;
            map.GeneratorParams = mapDataTag["GeneratorParams"];

            map.Spawn = new Position( spawnTag );

            foreach( NbtCompound zoneTag in zonesTag ) {
                try {
                    map.Zones.Add( new Zone( zoneTag ) );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error, "MapFCMv5: Error parsing a zone definition: {0}", ex );
                }
            }
            return map;
        }


        static World LoadWorld( string worldName, string fileName, WorldDataCategory cats ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            NbtFile file = new NbtFile( fileName, NbtCompression.AutoDetect, null );
            NbtCompound root = file.RootTag;

            Map map = LoadHeaderInternal( root );
            if( ( cats & WorldDataCategory.MapData ) != 0 ) {
                map.Blocks = root["MapData"]["Blocks"].ByteArrayValue;
            }

            World world = new World( worldName ) { Map = map };

            NbtCompound backupSettingsTag = root.Get<NbtCompound>( "BackupSettings" );
            if( ( cats & WorldDataCategory.BackupSettings ) != 0 && backupSettingsTag != null ) {
                YesNoAuto backupMode;
                if( !Enum.TryParse( backupSettingsTag["EnabledState"].StringValue, out backupMode ) ) {
                    throw new MapFormatException( "Could not parse BackupEnabledState." );
                }
                world.BackupInterval = TimeSpan.FromSeconds( backupSettingsTag["Interval"].IntValue );
                world.BackupEnabledState = backupMode;
            }

            NbtCompound accessSecurityTag = root.Get<NbtCompound>( "AccessSecurity" );
            if( ( cats & WorldDataCategory.AccessPermissions ) != 0 && accessSecurityTag != null ) {
                world.AccessSecurity = new SecurityController( accessSecurityTag );
            }

            NbtCompound buildSecurityTag = root.Get<NbtCompound>( "BuildSecurity" );
            if( ( cats & WorldDataCategory.BuildPermissions ) != 0 && buildSecurityTag != null ) {
                world.BuildSecurity = new SecurityController( buildSecurityTag );
            }

            NbtCompound envTag = root.Get<NbtCompound>( "Environment" );
            if( ( cats & WorldDataCategory.Environment ) != 0 && envTag != null ) {
                world.CloudColor = envTag["CloudColor"].IntValue;
                world.FogColor = envTag["FogColor"].IntValue;
                world.SkyColor = envTag["SkyColor"].IntValue;
                world.EdgeLevel = envTag["EdgeLevel"].ShortValue;
                world.EdgeBlock = (Block)envTag["EdgeBlock"].ByteValue;
            }

            NbtCompound blockDBSettingsTag = root.Get<NbtCompound>( "BlockDBSettings" );
            if( ( cats & WorldDataCategory.BlockDBSettings ) != 0 && blockDBSettingsTag != null ) {
                world.BlockDB.LoadSettings( blockDBSettingsTag );
            }

            NbtCompound worldEventsTag = root.Get<NbtCompound>( "WorldEvents" );
            if( ( cats & WorldDataCategory.WorldEvents ) != 0 && worldEventsTag != null ) {
                world.LoadedBy = worldEventsTag["LoadedBy"].StringValue;
                world.LoadedOn = DateTimeUtil.TryParseDateTime( worldEventsTag["LoadedOn"].LongValue );
                // TODO: UnloadedBy/UnloadedOn
                world.MapChangedBy = worldEventsTag["MapChangedBy"].StringValue;
                world.MapChangedOn = DateTimeUtil.TryParseDateTime( worldEventsTag["MapChangedOn"].LongValue );
                world.LockedBy = worldEventsTag["LockedBy"].StringValue;
                world.LockedOn = DateTimeUtil.TryParseDateTime( worldEventsTag["LockedOn"].LongValue );
                world.UnlockedBy = worldEventsTag["UnlockedBy"].StringValue;
                world.UnlockedOn = DateTimeUtil.TryParseDateTime( worldEventsTag["UnlockedOn"].LongValue );
            }
            return world;
        }


        public void Save( Map mapToSave, string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
        }
    }
}