// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using Mcc;


namespace fCraft {
    public sealed class Map {

        internal World world;
        public int widthX, widthY, height;
        public Position spawn;

        Dictionary<string, Dictionary<string, string>> metadata = new Dictionary<string, Dictionary<string, string>>();

        // Queue of block updates. Updates are applied by ProcessUpdates()
        ConcurrentQueue<BlockUpdate> updates = new ConcurrentQueue<BlockUpdate>();

        object metaLock = new object();

        // used to skip backups/saves if no changes were made
        public bool changedSinceSave,
                    changedSinceBackup;
        public short[,] shadows;

        // FCMv3 additions
        public DateTime DateModified = DateTime.UtcNow;
        public DateTime DateCreated = DateTime.UtcNow;
        public Guid GUID = Guid.NewGuid();

        // block data
        internal byte[] blocks;
        internal byte[] blockUndo;

        // block ownership
        object playerIDLock = new object();
        ushort MaxPlayerID = 256;
        Dictionary<string, ushort> PlayerIDs;
        Dictionary<ushort, string> PlayerNames;
        internal ushort[] blockOwnership;


        // temporarily hardcoded to be on at all times
        [Obsolete( "Will be removed in 0.500 final" )]
        public void EnableOwnershipTracking( ReservedPlayerID initialState ) {
            if( blockOwnership == null ) {
                blockOwnership = new ushort[blocks.Length];
                if( initialState != ReservedPlayerID.None ) {
                    for( int i = 0; i < blockOwnership.Length; i++ ) {
                        blockOwnership[i] = (ushort)initialState;
                    }
                }
            }
            if( PlayerIDs == null ) {
                PlayerIDs = new Dictionary<string, ushort>();
            }
            if( PlayerNames == null ) {
                PlayerNames = new Dictionary<ushort, string>();
            }
            changedSinceSave = true;
        }


        [CLSCompliant( false )]
        public ushort FindPlayerID( string name ) {
            lock( playerIDLock ) {
                if( PlayerIDs.ContainsKey( name ) ) {
                    return PlayerIDs[name];
                }
            }
            return (ushort)ReservedPlayerID.None;
        }


        [CLSCompliant( false )]
        public ushort AssignPlayerID( string name ) {
            ushort id;
            lock( playerIDLock ) {
                id = MaxPlayerID++;
                PlayerIDs[name] = id;
                PlayerNames[id] = name;
                changedSinceSave = true;
            }
            return id;
        }


        [CLSCompliant( false )]
        public string FindPlayerName( ushort id ) {
            if( id < 256 ) {
                return ((ReservedPlayerID)id).ToString();
            } else {
                lock( playerIDLock ) {
                    if( PlayerNames.ContainsKey( id ) ) {
                        return PlayerNames[id];
                    }
                }
                return ReservedPlayerID.Unknown.ToString();
            }
        }


        // more block metadata
        internal byte[] blockChangeFlags;
        internal uint[] blockTimestamps;


        internal Map() { }


        // creates an empty new world of specified dimensions
        public Map( World _world, int _widthX, int _widthY, int _height ) {
            world = _world;

            widthX = _widthX;
            widthY = _widthY;
            height = _height;

            int blockCount = widthX * widthY * height;

            blocks = new byte[blockCount];
            blocks.Initialize();
            EnableOwnershipTracking( ReservedPlayerID.None );//TEMP
        }


        #region Saving

        public bool Save( string fileName ) {
            string tempFileName = fileName + ".temp";

            try {
                changedSinceSave = false;
                if( !MapUtility.TrySaving( this, tempFileName, MapFormat.FCMv3 ) ) {
                    changedSinceSave = true;
                }

            } catch( IOException ex ) {
                changedSinceSave = true;
                Logger.Log( "Map.Save: Unable to open file \"{0}\" for writing: {1}", LogType.Error,
                               tempFileName, ex.Message );
                try { File.Delete( tempFileName ); } catch { }
                return false;
            }

            try {
                if( File.Exists( fileName ) ) File.Replace( tempFileName, fileName, null, true );
                else File.Move( tempFileName, fileName );
                Logger.Log( "Saved map successfully to {0}", LogType.SystemActivity,
                            fileName );
                changedSinceBackup = true;

            } catch( Exception ex ) {
                changedSinceSave = true;
                Logger.Log( "Error trying to replace file \"{0}\": {1}", LogType.Error,
                            fileName, ex );
                try { File.Delete( tempFileName ); } catch { }
                return false;
            }
            return true;
        }


        internal int WriteMetadata( Stream stream ) {
            BinaryWriter writer = new BinaryWriter( stream );
            int metaCount = 0;
            lock( metaLock ) {
                foreach( KeyValuePair<string, Dictionary<string, string>> group in metadata ) {
                    foreach( KeyValuePair<string, string> key in group.Value ) {
                        MapFCMv3.WriteLengthPrefixedString( writer, group.Key );
                        MapFCMv3.WriteLengthPrefixedString( writer, key.Key );
                        MapFCMv3.WriteLengthPrefixedString( writer, key.Value );
                        metaCount++;
                    }
                }
            }
            lock( zoneLock ) {
                foreach( Zone zone in zones.Values ) {
                    MapFCMv3.WriteLengthPrefixedString( writer, "zones" );
                    MapFCMv3.WriteLengthPrefixedString( writer, zone.name );
                    MapFCMv3.WriteLengthPrefixedString( writer, zone.SerializeFCMv2() );
                    metaCount++;
                }
            }
            return metaCount;
        }

        #endregion


        #region Loading

        public static Map Load( World _world, string fileName ) {
            // locate the file
            if( !File.Exists( fileName ) && !Directory.Exists( fileName ) ) {
                // try to append ".fcm" and/or prepend "maps/"
                if( File.Exists( fileName + ".fcm" ) ) {
                    fileName += ".fcm";
                }else{
                    Logger.Log( "Map.Load: Could not find the specified file: {0}", LogType.Error, fileName );
                    return null;
                }
            }

            // do the loading
            try {
                Map map = MapUtility.TryLoading( fileName );

                if( !map.ValidateBlockTypes( false ) ) {
                    Logger.Log( "MapDAT.Load: Some unknown block types were replaced with air.", LogType.Warning );
                }

                map.world = _world;
                return map;

            } catch( EndOfStreamException ex ) {
                Logger.Log( "Map.Load: Unexpected end of file \"{0}\". File may be corrupt, or format unsupported: {1}", LogType.Error,
                            fileName,
                            ex );
                return null;

            } catch( Exception ex ) {
                Logger.Log( "Map.Load: Error trying to read from \"{0}\": {1}", LogType.Error,
                            fileName,
                            ex );
                return null;
            }
        }


        /// <summary>
        /// Loads map dimensions from specified file.
        /// </summary>
        /// <param name="fileName">FULL file name, including path and extension.</param>
        /// <returns>Map object on success, or null on failure.</returns>
        public static Map LoadHeaderOnly( string fileName ) {
            try {
                return MapUtility.LoadHeader( fileName );
            } catch( Exception ex ) {
                Logger.Log( "Map.LoadHeaderOnly: Error occured while trying to parse header of {0}: {1}", LogType.Error,
                            fileName, ex );
                return null;
            }
        }


        internal bool ValidateHeader() {
            if( !IsValidDimension( height ) ) {
                Logger.Log( "Map.ReadHeader: Invalid dimension specified for widthX: {0}.", LogType.Error, widthX );
                return false;
            }

            if( !IsValidDimension( widthY ) ) {
                Logger.Log( "Map.ReadHeader: Invalid dimension specified for widthY: {0}.", LogType.Error, widthY );
                return false;
            }

            if( !IsValidDimension( height ) ) {
                Logger.Log( "Map.ReadHeader: Invalid dimension specified for height: {0}.", LogType.Error, height );
                return false;
            }

            if( spawn.x > widthX * 32 || spawn.y > widthY * 32 || spawn.h > height * 32 || spawn.x < 0 || spawn.y < 0 || spawn.h < 0 ) {
                Logger.Log( "Map.ReadHeader: Spawn coordinates are outside the valid range! Using center of the map instead.",
                            LogType.Warning );
                ResetSpawn();
            }

            return true;
        }


        // Only multiples of 16 are allowed, between 16 and 2032
        public static bool IsValidDimension( int dimension ) {
            return dimension > 0 && dimension % 16 == 0 && dimension < 2048;
        }

        #endregion


        #region Metadata

        public string GetMeta( string key ) {
            return GetMeta( "", key );
        }


        public string GetMeta( string group, string key ) {
            try {
                lock( metaLock ) {
                    return metadata[group][key];
                }
            } catch( KeyNotFoundException ) {
                return null;
            }
        }


        public void SetMeta( string key, string value ) {
            SetMeta( "", key, value );
        }


        public void SetMeta( string group, string key, string value ) {
            lock( metaLock ) {
                if( !metadata.ContainsKey( group ) ) {
                    metadata[group] = new Dictionary<string, string>();
                }
                metadata[group][key] = value;
            }
            changedSinceSave = true;
        }

        #endregion


        #region Utilities

        static Dictionary<string, Block> blockNames = new Dictionary<string, Block>();

        static Map() {
            foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                if( block != Block.Undefined ) {
                    blockNames.Add( block.ToString().ToLower(), block );
                }
            }

            // alternative names for some blocks
            blockNames["none"] = Block.Air;
            blockNames["aire"] = Block.Air; // common typo
            blockNames["nothing"] = Block.Air;
            blockNames["empty"] = Block.Air;
            blockNames["delete"] = Block.Air;
            blockNames["erase"] = Block.Air;

            blockNames["cement"] = Block.Stone;
            blockNames["concrete"] = Block.Stone;

            blockNames["gras"] = Block.Grass; // common typo

            blockNames["soil"] = Block.Dirt;
            blockNames["stones"] = Block.Rocks;
            blockNames["cobblestone"] = Block.Rocks;
            blockNames["plank"] = Block.Wood;
            blockNames["planks"] = Block.Wood;
            blockNames["board"] = Block.Wood;
            blockNames["boards"] = Block.Wood;
            blockNames["tree"] = Block.Plant;
            blockNames["sappling"] = Block.Plant;
            blockNames["adminium"] = Block.Admincrete;
            blockNames["opcrete"] = Block.Admincrete;
            blockNames["solid"] = Block.Admincrete;
            blockNames["bedrock"] = Block.Admincrete;
            blockNames["gold_ore"] = Block.GoldOre;
            blockNames["iron_ore"] = Block.IronOre;
            blockNames["ore"] = Block.IronOre;
            blockNames["coals"] = Block.Coal;
            blockNames["coalore"] = Block.Coal;
            blockNames["blackore"] = Block.Coal;

            blockNames["trunk"] = Block.Log;
            blockNames["stump"] = Block.Log;
            blockNames["treestump"] = Block.Log;
            blockNames["treetrunk"] = Block.Log;

            blockNames["leaf"] = Block.Leaves;
            blockNames["foliage"] = Block.Leaves;

            blockNames["greenyellow"] = Block.Lime;
            blockNames["yellowgreen"] = Block.Lime;
            blockNames["springgreen"] = Block.Teal;
            blockNames["emerald"] = Block.Teal;
            blockNames["purple"] = Block.Violet;
            blockNames["fuchsia"] = Block.Magenta;
            blockNames["cloth"] = Block.White;
            blockNames["cotton"] = Block.White;
            blockNames["grey"] = Block.Gray;
            blockNames["lightgray"] = Block.Gray;
            blockNames["lightgrey"] = Block.Gray;
            blockNames["darkgray"] = Block.Black;
            blockNames["darkgrey"] = Block.Black;

            blockNames["yellow_flower"] = Block.YellowFlower;
            blockNames["flower"] = Block.YellowFlower;
            blockNames["red_flower"] = Block.RedFlower;

            blockNames["mushroom"] = Block.BrownMushroom;
            blockNames["shroom"] = Block.BrownMushroom;
            blockNames["brown_shroom"] = Block.BrownMushroom;
            blockNames["red_shroom"] = Block.RedMushroom;

            blockNames["golden"] = Block.Gold;
            blockNames["copper"] = Block.Gold;
            blockNames["brass"] = Block.Gold;

            blockNames["iron"] = Block.Steel;
            blockNames["metal"] = Block.Steel;
            blockNames["silver"] = Block.Steel;

            blockNames["halfstep"] = Block.Stair;
            blockNames["halfblock"] = Block.Stair;
            blockNames["step"] = Block.Stair;
            blockNames["doublestep"] = Block.DoubleStair;
            blockNames["slab"] = Block.Stair;
            blockNames["slabs"] = Block.DoubleStair;
            blockNames["stairs"] = Block.DoubleStair;
            blockNames["steps"] = Block.DoubleStair;
            blockNames["double_stair"] = Block.DoubleStair;

            blockNames["bricks"] = Block.Brick;
            blockNames["explosive"] = Block.TNT;
            blockNames["dynamite"] = Block.TNT;

            blockNames["bookcase"] = Block.Books;
            blockNames["bookshelf"] = Block.Books;
            blockNames["bookshelves"] = Block.Books;
            blockNames["shelf"] = Block.Books;
            blockNames["shelves"] = Block.Books;
            blockNames["book"] = Block.Books;

            blockNames["moss"] = Block.MossyRocks;
            blockNames["mossy"] = Block.MossyRocks;
            blockNames["mossyrock"] = Block.MossyRocks;
            blockNames["mossystone"] = Block.MossyRocks;
            blockNames["mossystones"] = Block.MossyRocks;
            blockNames["mossycobblestone"] = Block.MossyRocks;
            blockNames["mossy_cobblestone"] = Block.MossyRocks;

            blockNames["onyx"] = Block.Obsidian;
        }


        public void SetSpawn( Position newSpawn ) {
            spawn = newSpawn;
            changedSinceSave = true;
        }


        public void ResetSpawn() {
            spawn.Set( widthX * 16, widthY * 16, height * 32, 0, 0 );
            changedSinceSave = true;
        }


        public void CalculateShadows() {
            if( shadows != null ) return;

            shadows = new short[widthX, widthY];
            for( int x = 0; x < widthX; x++ ) {
                for( int y = 0; y < widthY; y++ ) {
                    for( short h = (short)(height - 1); h >= 0; h-- ) {
                        switch( GetBlock( x, y, h ) ) {
                            case (byte)Block.Air:
                            case (byte)Block.Leaves:
                            case (byte)Block.Glass:
                            case (byte)Block.RedFlower:
                            case (byte)Block.RedMushroom:
                            case (byte)Block.YellowFlower:
                            case (byte)Block.BrownMushroom:
                                continue;
                            default:
                                shadows[x, y] = h;
                                break;
                        }
                        break;
                    }
                }
            }
        }


        internal static Block GetBlockByName( string block ) {
            block = block.ToLower();
            if( blockNames.ContainsKey( block ) ) {
                return blockNames[block];
            } else {
                return Block.Undefined;
            }
        }


        internal void CopyBlocks( byte[] source, int offset ) {
            blocks = new byte[widthX * widthY * height];
            Array.Copy( source, offset, blocks, 0, blocks.Length );
            changedSinceSave = true;
        }


        internal bool ValidateBlockTypes( bool returnOnErrors ) {
            bool foundUnknownTypes = false;
            for( int i = 0; i < blocks.Length; i++ ) {
                if( (blocks[i]) > 49 ) {
                    if( returnOnErrors ) return false;
                    blocks[i] = 0;
                    foundUnknownTypes = true;
                }
            }
            if( foundUnknownTypes ) changedSinceSave = true;
            return !foundUnknownTypes;
        }


        /// <summary>
        /// Writes a copy of the current map to a specified stream, compressed with GZipStream
        /// </summary>
        /// <param name="stream">Stream to write the compressed data to.</param>
        /// <param name="prependBlockCount">If true, prepends block data with signed, 32bit, big-endian block count.</param>
        public void GetCompressedCopy( Stream stream, bool prependBlockCount ) {
            using( GZipStream compressor = new GZipStream( stream, CompressionMode.Compress ) ) {
                if( prependBlockCount ) {
                    // convert block count to big-endian
                    int convertedBlockCount = IPAddress.HostToNetworkOrder( blocks.Length );
                    // write block count to gzip stream
                    compressor.Write( BitConverter.GetBytes( convertedBlockCount ), 0, sizeof( int ) );
                }
                compressor.Write( blocks, 0, blocks.Length );
            }
        }


        public void MakeFloodBarrier() {
            for( int x = 0; x < widthX; x++ ) {
                for( int y = 0; y < widthY; y++ ) {
                    SetBlock( x, y, 0, Block.Admincrete );
                }
            }

            for( int x = 0; x < widthX; x++ ) {
                for( int h = 0; h < height / 2; h++ ) {
                    SetBlock( x, 0, h, Block.Admincrete );
                    SetBlock( x, widthY - 1, h, Block.Admincrete );
                }
            }

            for( int y = 0; y < widthY; y++ ) {
                for( int h = 0; h < height / 2; h++ ) {
                    SetBlock( 0, y, h, Block.Admincrete );
                    SetBlock( widthX - 1, y, h, Block.Admincrete );
                }
            }
        }


        /// <summary>
        /// Returns the block count (volume) of the map.
        /// </summary>
        public int GetBlockCount() {
            return widthX * widthY * height;
        }

        #endregion


        #region Zones

        object zoneLock = new object(); // zone list (only needed when using "zones" dictionary, not "zoneList" cached array)
        Dictionary<string, Zone> zones = new Dictionary<string, Zone>();
        public Zone[] zoneList = new Zone[0];


        public bool AddZone( Zone z ) {
            lock( zoneLock ) {
                if( zones.ContainsKey( z.name.ToLower() ) ) return false;
                zones.Add( z.name.ToLower(), z );
                changedSinceSave = true;
                UpdateZoneCache();
            }
            return true;
        }


        public bool RemoveZone( string z ) {
            lock( zoneLock ) {
                if( !zones.ContainsKey( z.ToLower() ) ) return false;
                zones.Remove( z.ToLower() );
                changedSinceSave = true;
                UpdateZoneCache();
            }
            return true;
        }


        public PermissionOverride CheckZones( int x, int y, int h, Player player ) {
            PermissionOverride result = PermissionOverride.None;
            Zone[] zoneListCache = zoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].bounds.Contains( x, y, h ) ) {
                    if( zoneListCache[i].controller.Check( player.info ) ) {
                        result = PermissionOverride.Allow;
                    } else {
                        return PermissionOverride.Deny;
                    }
                }
            }
            return result;
        }


        public Zone FindDeniedZone( int x, int y, int h, Player player ) {
            Zone[] zoneListCache = zoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].bounds.Contains( x, y, h ) && !zoneListCache[i].controller.Check( player.info ) ) {
                    return zoneListCache[i];
                }
            }
            return null;
        }


        public bool TestZones( short x, short y, short h, Player player, out Zone[] allowedZones, out Zone[] deniedZones ) {
            List<Zone> allowed = new List<Zone>(), denied = new List<Zone>();
            bool found = false;

            Zone[] zoneListCache = zoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].bounds.Contains( x, y, h ) ) {
                    found = true;
                    if( zoneListCache[i].controller.Check( player.info ) ) {
                        allowed.Add( zoneListCache[i] );
                    } else {
                        denied.Add( zoneListCache[i] );
                    }
                }
            }
            allowedZones = allowed.ToArray();
            deniedZones = denied.ToArray();
            return found;
        }


        public Zone FindZone( string name ) {
            lock( zoneLock ) {
                if( zones.ContainsKey( name.ToLower() ) ) {
                    return zones[name.ToLower()];
                }
            }
            return null;
        }


        void UpdateZoneCache() {
            lock( zoneLock ) {
                Zone[] newZoneList = new Zone[zones.Count];
                int i = 0;
                foreach( Zone zone in zones.Values ) {
                    newZoneList[i++] = zone;
                }
                zoneList = newZoneList;
            }
        }

        #endregion


        #region Block Updates & Simulation

        public int Index( int x, int y, int h ) {
            return (h * widthY + y) * widthX + x;
        }


        public void SetBlock( int x, int y, int h, Block type ) {
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 ) {
                blocks[Index( x, y, h )] = (byte)type;
                changedSinceSave = true;
            }
        }

        public void SetBlock( int x, int y, int h, byte type ) {
            if( h < height && x < widthX && y < widthY && x >= 0 && y >= 0 && h >= 0 && type < 50 ) {
                blocks[Index( x, y, h )] = type;
                changedSinceSave = true;
            }
        }

        public void SetBlock( Vector3i vec, Block type ) {
            if( vec.x < widthX && vec.z < widthY && vec.y < height && vec.x >= 0 && vec.z >= 0 && vec.y >= 0 && (byte)type < 50 ) {
                blocks[Index( vec.x, vec.z, vec.y )] = (byte)type;
                changedSinceSave = true;
            }
        }

        public void SetBlock( Vector3i vec, byte type ) {
            if( vec.x < widthX && vec.z < widthY && vec.y < height && vec.x >= 0 && vec.z >= 0 && vec.y >= 0 && type < 50 ) {
                blocks[Index( vec.x, vec.z, vec.y )] = type;
                changedSinceSave = true;
            }
        }


        public byte GetBlock( int x, int y, int h ) {
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 )
                return blocks[Index( x, y, h )];
            return 0;
        }

        public byte GetBlock( Vector3i vec ) {
            if( vec.x < widthX && vec.z < widthY && vec.y < height && vec.x >= 0 && vec.z >= 0 && vec.y >= 0 )
                return blocks[Index( vec.x, vec.z, vec.y )];
            return 0;
        }


        public bool InBounds( int x, int y, int h ) {
            return x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0;
        }

        public bool InBounds( Vector3i vec ) {
            return vec.x < widthX && vec.z < widthY && vec.y < height && vec.x >= 0 && vec.z >= 0 && vec.y >= 0;
        }


        public int SearchColumn( int x, int y, Block id ) {
            return SearchColumn( x, y, id, height - 1 );
        }

        public int SearchColumn( int x, int y, Block id, int startH ) {
            for( int h = startH; h > 0; h-- ) {
                if( GetBlock( x, y, h ) == (byte)id ) {
                    return h;
                }
            }
            return -1; // -1 means 'not found'
        }


        internal void QueueUpdate( BlockUpdate update ) {
            updates.Enqueue( update );
        }


        internal void ClearUpdateQueue() {
            BlockUpdate temp = new BlockUpdate();
            while( updates.Dequeue( ref temp ) ) ;
        }


        public int UpdateQueueSize() {
            return updates.Length;
        }


        public void ProcessUpdates() {
            if( world.isLocked ) {
                if( world.pendingUnload ) {
                    world.UnloadMap( true );
                }
                return;
            }

            int packetsSent = 0;
            int maxPacketsPerUpdate = Server.CalculateMaxPacketsPerUpdate( world );
            BlockUpdate update = new BlockUpdate();
            while( packetsSent < maxPacketsPerUpdate ) {
                if( !updates.Dequeue( ref update ) ) {
                    if( world.isFlushing ) {
                        world.EndFlushMapBuffer();
                    }
                    break;
                }
                changedSinceSave = true;
                if( !InBounds( update.x, update.y, update.h ) ) continue;
                int blockIndex = Index( update.x, update.y, update.h );
                blocks[blockIndex] = update.type; // TODO: investigate IndexOutOfRangeException here

                if( !world.isFlushing ) world.SendToAllDelayed( PacketWriter.MakeSetBlock( update.x, update.y, update.h, update.type ), update.origin );
                if( update.origin != null && blockOwnership != null ) {
                    // TODO: ensure safety in case player leaves the world (and localPlayerID changes) before everything is processed
                    if( update.origin.localPlayerID == (ushort)ReservedPlayerID.None ) {
                        update.origin.localPlayerID = AssignPlayerID( update.origin.name );
                    }
                    blockOwnership[blockIndex] = update.origin.localPlayerID;
                }
                packetsSent++;
            }

            if( packetsSent == 0 && world.pendingUnload ) {
                world.UnloadMap( true );
            }
        }

        #endregion


        #region Backup

        public void SaveBackup( string sourceName, string targetName, bool onlyIfChanged ) {
            if( onlyIfChanged && !changedSinceBackup && Config.GetBool( ConfigKey.BackupOnlyWhenChanged ) ) return;

            if( !Directory.Exists( "backups" ) ) {
                try {
                    Directory.CreateDirectory( "backups" );
                } catch( Exception ex ) {
                    Logger.Log( "Map.SaveBackup: Error occured while trying to create backup directory: {0}", LogType.Error,
                                ex );
                    return;
                }
            }

            try {
                changedSinceBackup = false;
                File.Copy( sourceName, targetName, true );
            } catch( Exception ex ) {
                changedSinceBackup = true;
                Logger.Log( "Map.SaveBackup: Error occured while trying to save backup to \"{0}\": {1}", LogType.Error,
                            targetName, ex );
                return;
            }

            DirectoryInfo d = new DirectoryInfo( "backups" );
            List<FileInfo> backupList = new List<FileInfo>( d.GetFiles( "*.fcm" ) );
            backupList.Sort( FileInfoComparer.instance );

            if( Config.GetInt( ConfigKey.MaxBackups ) > 0 ) {
                while( backupList.Count > Config.GetInt( ConfigKey.MaxBackups ) ) {
                    FileInfo info = backupList[backupList.Count - 1];
                    backupList.RemoveAt( backupList.Count - 1 );
                    try {
                        File.Delete( info.FullName );
                        Logger.Log( "Map.SaveBackup: Deleted old backup \"{0}\"", LogType.SystemActivity,
                                    info.Name );
                    } catch( Exception ex ) {
                        Logger.Log( "Map.SaveBackup: Error occured while trying delete old backup \"{0}\": {1}", LogType.Error,
                                    info.Name, ex );
                        break;
                    }
                }
            }


            if( Config.GetInt( ConfigKey.MaxBackupSize ) > 0 ) {
                while( true ) {
                    long Size = 0;
                    FileInfo[] fis = d.GetFiles();
                    foreach( FileInfo fi in fis ) {
                        Size += fi.Length;
                    }

                    if( Size / 1024 / 1024 > Config.GetInt( ConfigKey.MaxBackupSize ) ) {
                        FileInfo info = backupList[backupList.Count - 1];
                        backupList.RemoveAt( backupList.Count - 1 );
                        try {
                            File.Delete( info.FullName );
                            Logger.Log( "Map.SaveBackup: Deleted old backup \"{0}\"", LogType.SystemActivity,
                                        info.Name );
                        } catch( Exception ex ) {
                            Logger.Log( "Map.SaveBackup: Error occured while trying delete old backup \"{0}\": {1}", LogType.Error,
                                        info.Name, ex );
                            break;
                        }
                    } else {
                        break;
                    }
                }
            }

            Logger.Log( "AutoBackup: " + targetName, LogType.SystemActivity );
        }


        sealed class FileInfoComparer : IComparer<FileInfo> {
            public static FileInfoComparer instance = new FileInfoComparer();
            public int Compare( FileInfo x, FileInfo y ) {
                return -x.CreationTime.CompareTo( y.CreationTime );
            }
        }

        #endregion


        #region FCMv3

        // todo: layerLock object of some sort
        internal List<Map.DataLayer> PrepareLayers() {
            List<Map.DataLayer> layers = new List<DataLayer>();

            byte[] blocksCache = blocks;
            if( blocksCache != null ) {
                layers.Add( new DataLayer {
                    Type = DataLayerType.Blocks,
                    Data = blocksCache,
                    ElementSize = 1,
                    ElementCount = blocksCache.Length
                } );
            }

            byte[] blockUndoCache = blockUndo;
            if( blockUndoCache != null ) {
                layers.Add( new DataLayer {
                    Type = DataLayerType.BlockUndo,
                    Data = blockUndoCache,
                    ElementSize = 1,
                    ElementCount = blockUndoCache.Length
                } );
            }

            ushort[] blockOwnershipCache = blockOwnership;
            if( blockOwnershipCache != null ) {
                layers.Add( new DataLayer {
                    Type = DataLayerType.BlockOwnership,
                    Data = blockOwnershipCache,
                    ElementSize = 2,
                    ElementCount = blockOwnershipCache.Length
                } );
            }

            uint[] blockTimestampsCache = blockTimestamps;
            if( blockTimestampsCache != null ) {
                layers.Add( new DataLayer {
                    Type = DataLayerType.BlockTimestamps,
                    Data = blockTimestampsCache,
                    ElementSize = 4,
                    ElementCount = blockTimestampsCache.Length
                } );
            }

            byte[] blockChangeFlagsCache = blockChangeFlags;
            if( blockChangeFlagsCache != null ) {
                layers.Add( new DataLayer {
                    Type = DataLayerType.BlockChangeFlags,
                    Data = blockChangeFlagsCache,
                    ElementSize = 1,
                    ElementCount = blockChangeFlagsCache.Length
                } );
            }

            Dictionary<string, ushort> PlayerIDsCache = PlayerIDs;
            if( PlayerIDsCache != null && PlayerNames != null ) {
                lock( playerIDLock ) {
                    layers.Add( new DataLayer {
                        Type = DataLayerType.PlayerIDs,
                        Data = new Dictionary<string, ushort>( PlayerIDsCache ), // locked copy is needed to avoid threading issues
                        ElementSize = -1, // variable
                        ElementCount = PlayerIDsCache.Count
                    } );
                }
            }
            return layers;
        }

        internal void ReadLayer( DataLayer layer, Stream stream ) {

            switch( layer.Type ) {
                case DataLayerType.Blocks:
                    blocks = new byte[layer.ElementCount];
                    stream.Read( blocks, 0, blocks.Length );
                    break;

                case DataLayerType.BlockUndo:
                    blockUndo = new byte[layer.ElementCount];
                    stream.Read( blockUndo, 0, blockUndo.Length );
                    break;

                case DataLayerType.BlockOwnership: {
                        blockOwnership = new ushort[layer.ElementCount];
                        BinaryReader reader = new BinaryReader( stream );
                        for( int i = 0; i < layer.ElementCount; i++ ) {
                            blockOwnership[i] = reader.ReadUInt16();
                        }
                    } break;

                case DataLayerType.BlockTimestamps: {
                        blockTimestamps = new uint[layer.ElementCount];
                        BinaryReader reader = new BinaryReader( stream );
                        for( int i = 0; i < layer.ElementCount; i++ ) {
                            blockTimestamps[i] = reader.ReadUInt32();
                        }
                    } break;

                case DataLayerType.BlockChangeFlags:
                    blockChangeFlags = new byte[layer.ElementCount];
                    stream.Read( blockChangeFlags, 0, blockChangeFlags.Length );
                    break;

                case DataLayerType.PlayerIDs: {
                        PlayerIDs = new Dictionary<string, ushort>();
                        PlayerNames = new Dictionary<ushort, string>();
                        BinaryReader reader = new BinaryReader( stream );
                        MaxPlayerID = 256;
                        for( int i = 0; i < layer.ElementCount; i++ ) {
                            int length = reader.ReadByte();
                            byte[] stringData = reader.ReadBytes( length );
                            string name = ASCIIEncoding.ASCII.GetString( stringData );
                            PlayerNames[MaxPlayerID] = name;
                            PlayerIDs[name] = MaxPlayerID;
                            MaxPlayerID++;
                        }
                    } break;

                default:
                    // skip
                    Logger.Log( "Map.ReadLayer: Skipping unknown layer ({0})", LogType.Warning, layer.Type );
                    stream.Seek( layer.CompressedLength, SeekOrigin.Current );
                    break;
            }
        }

        internal static void WriteLayer( DataLayer layer, Stream stream ) {
            switch( layer.Type ) {
                case DataLayerType.Blocks:
                case DataLayerType.BlockUndo:
                case DataLayerType.BlockChangeFlags:
                    stream.Write( (byte[])layer.Data, 0, layer.ElementCount );
                    break;

                case DataLayerType.BlockOwnership: {
                        BinaryWriter bw = new BinaryWriter( stream );
                        ushort[] data = (ushort[])layer.Data;
                        for( int i = 0; i < layer.ElementCount; i++ ) {
                            bw.Write( data[i] );
                        }
                    }
                    break;

                case DataLayerType.BlockTimestamps: {
                        BinaryWriter bw = new BinaryWriter( stream );
                        uint[] data = (uint[])layer.Data;
                        for( int i = 0; i < layer.ElementCount; i++ ) {
                            bw.Write( data[i] );
                        }
                    }
                    break;

                case DataLayerType.PlayerIDs: {
                        BinaryWriter bw = new BinaryWriter( stream );
                        Dictionary<string, ushort> IDs = (Dictionary<string, ushort>)layer.Data;
                        foreach( string name in IDs.Keys ) {//todo: thread safety
                            byte[] stringData = ASCIIEncoding.ASCII.GetBytes( name );
                            bw.Write( (byte)stringData.Length );
                            bw.Write( stringData );
                        }
                    }
                    break;

                default: {
                        Type type = layer.GetType();
                        if( type == typeof( byte[] ) ) {
                            stream.Write( (byte[])layer.Data, 0, layer.ElementCount );
                        } else if( type == typeof( ushort[] ) ) {
                            BinaryWriter bw = new BinaryWriter( stream );
                            ushort[] data = (ushort[])layer.Data;
                            for( int i = 0; i < layer.ElementCount; i++ ) {
                                bw.Write( data[i] );
                            }
                        } else if( type == typeof( uint[] ) ) {
                            BinaryWriter bw = new BinaryWriter( stream );
                            uint[] data = (uint[])layer.Data;
                            for( int i = 0; i < layer.ElementCount; i++ ) {
                                bw.Write( data[i] );
                            }
                        } else {
                            Logger.Log( "Map.WriteLayer: Unknown layer type ({0})", LogType.Error,
                                        layer.Type );
                        }
                    }
                    break;
            }
        }

        public class DataLayer {
            public DataLayerType Type;         // see "DataLayerType" below
            public int GeneralPurposeField;   // 32 bits that can be used in implementation-specific ways
            public int ElementSize;           // size of each data element (if elements are variable-size, set this to 1)
            public int ElementCount;          // number of fixed-sized elements (if elements are variable-size, set this to total number of bytes)
            // uncompressed length = (element size * element count)
            public object Data;
            public long Offset;
            public int CompressedLength;
        }


        // type of block - allows storing multiple layers of information about blocks
        public enum DataLayerType : byte {
            Blocks = 0, // Block types (El.Size=1)

            BlockUndo = 1, // Previous block type (per-block) (El.Size=1)

            BlockOwnership = 2, // IDs of block changers (per-block) (El.Size=2)

            BlockTimestamps = 3, // Modification date/time (per-block) (El.Size=4)

            BlockChangeFlags = 4, // Type of action that resulted in the block change
            // See BlockChangeFlags flags (El.Size=1)

            PlayerIDs = 5  // mapping of player names to ID numbers (El.Size=2)

            // 4-31 reserved
            // 32-255 custom

        } // 1 byte

        #endregion
    }
}