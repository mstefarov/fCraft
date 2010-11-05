// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Net;
using Mcc;


namespace fCraft {
    public sealed class Map {

        internal World world;
        internal byte[] blocks;
        public int widthX, widthY, height;
        public Position spawn;
        public Dictionary<string, string> meta = new Dictionary<string, string>();
        ConcurrentQueue<BlockUpdate> updates = new ConcurrentQueue<BlockUpdate>();
        object metaLock = new object(), zoneLock = new object();
        public int changesSinceSave, changesSinceBackup;
        public short[,] shadows;

        public DateTime DateModified;
        public DateTime DateCreated;
        public Guid GUID;

        public Dictionary<DataLayerType, DataLayer> layers;

        // undo information
        public byte[] blockUndo;
        public ushort[] blockOwnership;
        public BlockChangeCause[] blockChangeCauses;
        public int[] blockTimestamps;

        internal Map() { }

        public Map( World _world ) {
            world = _world;
        }

        // creates an empty new world of specified dimensions
        public Map( World _world, int _widthX, int _widthY, int _height )
            : this( _world ) {
            widthX = _widthX;
            widthY = _widthY;
            height = _height;

            int blockCount = widthX * widthY * height;

            blocks = new byte[blockCount];
            blocks.Initialize();
        }


        #region Saving
        public bool Save( string fileName ) {
            string tempFileName = fileName + ".temp";

            try {
                using( FileStream fs = File.OpenWrite( tempFileName ) ) {
                    WriteHeader( fs );
                    WriteMetadata( new BinaryWriter( fs ) );
                    changesSinceSave = 0;
                    GetCompressedCopy( fs, false );
                }
            } catch( IOException ex ) {
                Logger.Log( "Map.Save: Unable to open file \"{0}\" for writing: {1}", LogType.Error,
                               tempFileName, ex.Message );
                try { File.Delete( tempFileName ); } catch { }
                return false;
            }

            try {
                if( File.Exists( fileName ) ) {
                    File.Replace( tempFileName, fileName, null, true );
                } else {
                    File.Move( tempFileName, fileName );
                }
                changesSinceBackup++;
                Logger.Log( "Saved map successfully to {0}", LogType.SystemActivity,
                            fileName );
            } catch( Exception ex ) {
                Logger.Log( "Error trying to replace file \"{0}\": {1}", LogType.Error,
                            fileName, ex );
                try { File.Delete( tempFileName ); } catch { }
                return false;
            }
            return true;
        }


        void WriteHeader( FileStream fs ) {
            BinaryWriter writer = new BinaryWriter( fs );
            writer.Write( MapFCMv2.Identifier );
            writer.Write( (ushort)widthX );
            writer.Write( (ushort)widthY );
            writer.Write( (ushort)height );
            writer.Write( (ushort)spawn.x );
            writer.Write( (ushort)spawn.y );
            writer.Write( (ushort)spawn.h );
            writer.Write( (byte)spawn.r );
            writer.Write( (byte)spawn.l );
            writer.Flush();
        }


        internal void WriteMetadata( BinaryWriter writer ) {
            lock( metaLock ) {
                writer.Write( (ushort)(meta.Count + zones.Count) );
                foreach( KeyValuePair<string, string> pair in meta ) {
                    WriteLengthPrefixedString( writer, pair.Key );
                    WriteLengthPrefixedString( writer, pair.Value );
                }
                int i = 0;
                lock( zoneLock ) {
                    foreach( Zone zone in zones.Values ) {
                        WriteLengthPrefixedString( writer, "@zone" + i );
                        WriteLengthPrefixedString( writer, zone.Serialize() );
                    }
                }
            }
            writer.Flush();
        }


        static void WriteLengthPrefixedString( BinaryWriter writer, string s ) {
            byte[] stringData = ASCIIEncoding.ASCII.GetBytes( s );
            writer.Write( stringData.Length );
            writer.Write( stringData );
        }
        #endregion

        #region Loading
        public static Map Load( World _world, string fileName ) {
            // locate the file
            if( !File.Exists( fileName ) && !Directory.Exists( fileName ) ) {
                // try to append ".fcm" and/or prepend "maps/"
                if( File.Exists( fileName + ".fcm" ) ) {
                    fileName += ".fcm";
                } else if( File.Exists( Path.Combine( "maps", fileName ) ) || Directory.Exists( Path.Combine( "maps", fileName ) ) ) {
                    fileName = Path.Combine( "maps", fileName );
                } else if( File.Exists( Path.Combine( "maps", fileName + ".fcm" ) ) ) {
                    fileName = Path.Combine( "maps", fileName + ".fcm" );
                } else {
                    Logger.Log( "Map.Load: Could not find the specified file: {0}", LogType.Error, fileName );
                    return null;
                }
            }

            // do the loading
            try {
                Map map = MapUtility.TryLoading( fileName );
                if( !map.ValidateBlockTypes( true ) ) {
                    Logger.Log( "Map.Load: Invalid block types detected in \"{0}\". File may be corrupt, or format unsupported.", LogType.Error,
                                fileName );
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

        public static Map LoadHeaderOnly( string fileName ) {
            try {
                if( !File.Exists( fileName ) ) {
                    if( File.Exists( fileName + ".fcm" ) ) {
                        fileName += ".fcm";
                    } else if( File.Exists( Path.Combine("maps", fileName) ) ) {
                        fileName = Path.Combine("maps", fileName);
                    } else if( File.Exists( Path.Combine("maps", fileName + ".fcm") ) ) {
                        fileName = Path.Combine("maps", fileName + ".fcm");
                    } else {
                        Logger.Log( "Map.LoadHeaderOnly: File \"{0}\" not found.", LogType.Error, fileName );
                        return null;
                    }
                }

                Map map = new Map();
                using( FileStream fs = File.OpenRead( fileName ) ) {
                    BinaryReader reader = new BinaryReader( fs );

                    // Read in the magic number
                    if( reader.ReadUInt32() != Mcc.MapFCMv2.Identifier ) {
                        throw new FormatException();
                    }

                    // Read in the map dimesions
                    map.widthX = reader.ReadInt16();
                    map.widthY = reader.ReadInt16();
                    map.height = reader.ReadInt16();
                }
                return map;
            } catch( Exception ex ) {
                Logger.Log( "Map.LoadHeaderOnly: Error occured while trying to parse header of " + fileName + ": " + ex, LogType.Error );
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
                Logger.LogWarning( "Map.ReadHeader: Spawn coordinates are outside the valid range! Using center of the map instead.",
                                   WarningLogSubtype.MapLoadWarning );
                ResetSpawn();
            }

            return true;
        }


        internal void ReadMetadata( BinaryReader reader ) {
            try {
                int metaSize = (int)reader.ReadUInt16();

                for( int i = 0; i < metaSize; i++ ) {
                    string key = ReadLengthPrefixedString( reader );
                    string value = ReadLengthPrefixedString( reader );
                    if( key.StartsWith( "@zone" ) ) {
                        try {
                            AddZone( new Zone( value, world ) );
                        } catch( Exception ex ) {
                            Logger.Log( "Map.ReadMetadata: cannot parse a zone: {0}", LogType.Error, ex.Message );
                        }
                    } else {
                        meta.Add( key, value );
                    }
                }
                UpdateZoneCache();

            } catch( FormatException ex ) {
                Logger.Log( "Map.ReadMetadata: Cannot parse one or more of the metadata entries: {0}", LogType.Error,
                            ex.Message );
            }
        }

        static string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = reader.ReadInt32();
            byte[] stringData = reader.ReadBytes( length );
            return ASCIIEncoding.ASCII.GetString( stringData );
        }


        // Only multiples of 16 are allowed, between 16 and 2032
        public static bool IsValidDimension( int dimension ) {
            return dimension > 0 && dimension % 16 == 0 && dimension < 2048;
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


        public void ResetSpawn() {
            spawn.Set( widthX * 16, widthY * 16, height * 32, 0, 0 );
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
        }


        internal bool ValidateBlockTypes( bool returnOnErrors ) {
            for( int i = 0; i < blocks.Length; i++ ) {
                if( (blocks[i]) > 49 ) {
                    if( returnOnErrors ) return false;
                    else blocks[i] = 0;
                }
            }
            return true;
        }

        // zips a copy of the block array
        public void GetCompressedCopy( Stream stream, bool prependBlockCount ) {
            using( ZLibStream compressor = ZLibStream.MakeCompressor( stream, ZLibStream.BufferSize ) ) {
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


        public int GetBlockCount() {
            return widthX * widthY * height;
        }

        #endregion

        #region Zones
        public Dictionary<string, Zone> zones = new Dictionary<string, Zone>();
        public Zone[] zoneList = new Zone[0];

        public bool AddZone( Zone z ) {
            lock( zoneLock ) {
                if( zones.ContainsKey( z.name.ToLower() ) ) return false;
                zones.Add( z.name.ToLower(), z );
                changesSinceSave++;
                UpdateZoneCache();
            }
            return true;
        }

        public bool RemoveZone( string z ) {
            lock( zoneLock ) {
                if( !zones.ContainsKey( z.ToLower() ) ) return false;
                zones.Remove( z.ToLower() );
                changesSinceSave++;
                UpdateZoneCache();
            }
            return true;
        }


        public ZoneOverride CheckZones( int x, int y, int h, Player player ) {
            ZoneOverride result = ZoneOverride.None;
            Zone[] zoneListCache = zoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].bounds.Contains( x, y, h ) ) {
                    if( zoneListCache[i].CanBuild( player ) ) {
                        result = ZoneOverride.Allow;
                    } else {
                        return ZoneOverride.Deny;
                    }
                }
            }
            return result;
        }


        public Zone FindDeniedZone( int x, int y, int h, Player player ) {
            Zone[] zoneListCache = zoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].bounds.Contains( x, y, h ) && !zoneListCache[i].CanBuild( player ) ) {
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
                    if( zoneListCache[i].CanBuild( player ) ) {
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
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 )
                blocks[Index( x, y, h )] = (byte)type;
        }

        public void SetBlock( int x, int y, int h, byte type ) {
            if( h < height && x < widthX && y < widthY && x >= 0 && y >= 0 && h >= 0 && type < 50 )
                blocks[Index( x, y, h )] = type;
        }

        public void SetBlock( Vector3i vec, Block type ) {
            if( vec.x < widthX && vec.z < widthY && vec.y < height && vec.x >= 0 && vec.z >= 0 && vec.y >= 0 && (byte)type < 50 )
                blocks[Index( vec.x, vec.z, vec.y )] = (byte)type;
        }

        public void SetBlock( Vector3i vec, byte type ) {
            if( vec.x < widthX && vec.z < widthY && vec.y < height && vec.x >= 0 && vec.z >= 0 && vec.y >= 0 && type < 50 )
                blocks[Index( vec.x, vec.z, vec.y )] = type;
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
                if( world.pendingUnload ) world.UnloadMap();
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
                changesSinceSave++;
                SetBlock( update.x, update.y, update.h, update.type );
                if( !world.isFlushing ) world.SendToAllDelayed( PacketWriter.MakeSetBlock( update.x, update.y, update.h, update.type ), update.origin );
                if( update.origin != null ) {
                    update.origin.info.ProcessBlockPlaced( update.type );
                }
                packetsSent++;
            }

            if( packetsSent == 0 && world.pendingUnload ) {
                world.UnloadMap();
            }
        }

        #endregion

        #region Backup
        public void SaveBackup( string sourceName, string targetName, bool onlyIfChanged ) {
            if( onlyIfChanged && changesSinceBackup == 0 && Config.GetBool( ConfigKey.BackupOnlyWhenChanged ) ) return;

            if( !Directory.Exists( "backups" ) ) {
                try {
                    Directory.CreateDirectory( "backups" );
                } catch( Exception ex ) {
                    Logger.Log( "Map.SaveBackup: Error occured while trying to create backup directory: {0}", LogType.Error,
                                ex );
                    return;
                }
            }

            changesSinceBackup = 0;

            try {
                File.Copy( sourceName, targetName, true );
            } catch( Exception ex ) {
                Logger.Log( "Map.SaveBackup: Error occured while trying to save backup to \"{0}\": {1}", LogType.Error,
                            targetName,ex );
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

        public struct DataLayer {
            public DataLayerType Type;         // see "DataLayerType" below
            public DataLayerCompressionType CompressionType;   // see "DataLayerCompressionType" below
            public uint GeneralPurposeField;   // 32 bits that can be used in implementation-specific ways
            public uint ElementSize;           // size of each data element (if elements are variable-size, set this to 1)
            public uint ElementCount;          // number of fixed-sized elements (if elements are variable-size, set this to total number of bytes)
            // uncompressed length = (element size * element count)
            public byte[] Data;
            public long Offset;
            public uint CompressedLength;
        }

        // type of block - allows storing multiple layers of information about blocks
        public enum DataLayerType : byte {
            Blocks = 0,   // block types
            BlockUndo = 1,   // previous block type (per-block)
            BlockOwnership = 2,   // cause of previous change (per-block)
            BlockDate = 3    // modification date/time (per-block)
            // 4-31 reserved
            // 32-255 custom
        }

        public enum DataLayerCompressionType : byte {
            None = 0,    // raw, uncompressed data - implementation OPTIONAL
            Deflate = 1,    // deflate with no header - implementation OPTIONAL
            DeflateGZip = 2,    // deflate with gzip header - implementation OPTIONAL
            LZO = 3,    // LZO (Lempel–Ziv–Oberhumer) compression - implementation OPTIONAL, for use with custom DataLayerTypes only
            LZMA = 4     // LZMA (7-Zip) compression - implementation OPTIONAL, for use with custom DataLayerTypes only
            // 5-31 reserved
            // 32-255 custom
        }
        #endregion
    }
}