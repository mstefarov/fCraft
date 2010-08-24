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

        internal Map() {}

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
            string tempFileName = Path.GetTempFileName();

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
                File.Delete( fileName );
                File.Move( tempFileName, fileName );
                changesSinceBackup++;
                Logger.Log( "Saved map succesfully to {0}", LogType.SystemActivity,
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
                writer.Write( (ushort)( meta.Count + zones.Count ) );
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
                } else if( File.Exists( "maps/" + fileName ) || Directory.Exists("maps/" + fileName) ) {
                    fileName = "maps/" + fileName;
                } else if( File.Exists( "maps/" + fileName + ".fcm" ) ) {
                    fileName = "maps/" + fileName + ".fcm";
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
                    } else if( File.Exists( "maps/" + fileName ) ) {
                        fileName = "maps/" + fileName;
                    } else if( File.Exists( "maps/" + fileName + ".fcm" ) ) {
                        fileName = "maps/" + fileName + ".fcm";
                    } else {
                        Logger.Log( "Map.LoadHeaderOnly: File \"" + fileName + "\" not found.", LogType.Error );
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
                Logger.Log( "Map.ReadHeader: Spawn coordinates are outside the valid range! Using center of the map instead.", LogType.Warning );
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
                            AddZone( new Zone( value ) );
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
            blockNames["nothing"] = Block.Air;
            blockNames["empty"] = Block.Air;

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
            blockNames["gold_ore"] = Block.GoldOre;
            blockNames["iron_ore"] = Block.IronOre;
            blockNames["ore"] = Block.IronOre;
            blockNames["coals"] = Block.Coal;
            blockNames["coalore"] = Block.Coal;
            blockNames["blackore"] = Block.Coal;

            blockNames["tree"] = Block.Log;
            blockNames["trunk"] = Block.Log;
            blockNames["stump"] = Block.Log;
            blockNames["treestump"] = Block.Log;
            blockNames["treetrunk"] = Block.Log;

            blockNames["leaf"] = Block.Leaves;
            blockNames["foliage"] = Block.Leaves;

            blockNames["greenyellow"] = Block.Lime;
            blockNames["yellowgreen"] = Block.Lime;
            blockNames["springgreen"] = Block.Teal;
            blockNames["purple"] = Block.Violet;
            blockNames["grey"] = Block.Gray;

            blockNames["yellow_flower"] = Block.YellowFlower;
            blockNames["flower"] = Block.YellowFlower;
            blockNames["red_flower"] = Block.RedFlower;

            blockNames["mushroom"] = Block.BrownMushroom;
            blockNames["shroom"] = Block.BrownMushroom;
            blockNames["brown_shroom"] = Block.BrownMushroom;
            blockNames["red_shroom"] = Block.BrownMushroom;

            blockNames["iron"] = Block.Steel;
            blockNames["metal"] = Block.Steel;
            blockNames["silver"] = Block.Steel;

            blockNames["step"] = Block.Stair;
            blockNames["doublestep"] = Block.DoubleStair;
            blockNames["slab"] = Block.Stair;
            blockNames["slabs"] = Block.DoubleStair;
            blockNames["stairs"] = Block.DoubleStair;
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
        }


        public void ResetSpawn() {
            spawn.Set( widthX * 16, widthY * 16, height * 32, 0, 0 );
        }

        public void CalculateShadows() {
            if( shadows != null ) return;
            else shadows = new short[widthX, widthY];
            for( int x = 0; x < widthX; x++ ) {
                for( int y = 0; y < widthY; y++ ) {
                    for( int h = height; h >= 0; h-- ) {
                        if( GetBlock( x, y, h ) > 0 ) {
                            shadows[x, y] = (short)h;
                            break;
                        }
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
                if( ( blocks[i] ) > 49 ) {
                    if( returnOnErrors ) return false;
                    else blocks[i] = 0;
                }
            }
            return true;
        }

        // zips a copy of the block array
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

        public Zone[] ListZones() {
            Zone[] output;
            int i = 0;
            lock( zoneLock ) {
                output = new Zone[zones.Count];
                foreach( Zone zone in zones.Values ) {
                    output[i++] = zone;
                }
            }
            return output;
        }


        public ZoneOverride CheckZones( int x, int y, int h, Player player ) {
            ZoneOverride result = ZoneOverride.None;
            Zone[] zoneListCache = zoneList;
            for( int i=0; i<zoneListCache.Length;i++){
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
            return ( h * widthY + y ) * widthX + x;
        }

        public void SetBlock( int x, int y, int h, Block type ) {
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 )
                blocks[Index( x, y, h )] = (byte)type;
        }

        public void SetBlock( int x, int y, int h, byte type ) {
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 && type < 50 )
                blocks[Index( x, y, h )] = type;
        }

        public byte GetBlock( int x, int y, int h ) {
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 )
                return blocks[Index( x, y, h )];
            return 0;
        }


        internal void QueueUpdate( BlockUpdate update ) {
            updates.Enqueue( update );
        }


        internal void ClearUpdateQueue() {
            BlockUpdate temp = new BlockUpdate();
            while( updates.Dequeue( ref temp ) ) ;
        }


        public void ProcessUpdates() {
            if( world.isLocked ) {
                if( world.isReadyForUnload ) world.UnloadMap();
                return;
            }

            int packetsSent = 0;
            int maxPacketsPerUpdate = Server.CalculateMaxPacketsPerUpdate( world );
            BlockUpdate update = new BlockUpdate();
            while( packetsSent < maxPacketsPerUpdate ) {
                if( !updates.Dequeue( ref update ) ) break;
                changesSinceSave++;
                SetBlock( update.x, update.y, update.h, update.type );
                world.SendToAllDelayed( PacketWriter.MakeSetBlock( update.x, update.y, update.h, update.type ), update.origin );
                if( update.origin != null ) {
                    update.origin.info.ProcessBlockPlaced( update.type );
                }
                packetsSent++;
            }

            if( packetsSent == 0 && world.isReadyForUnload ) {
                world.UnloadMap();
            }
        }
        #endregion

        #region Backup
        public void SaveBackup( string sourceName, string targetName ) {
            if( changesSinceBackup == 0 && Config.GetBool( ConfigKey.BackupOnlyWhenChanged ) ) return;

            if( !Directory.Exists( "backups" ) ) {
                try {
                    Directory.CreateDirectory( "backups" );
                } catch( Exception ex ) {
                    Logger.Log( "Map.SaveBackup: Error occured while trying to create backup directory: "+ex, LogType.Error );
                    return;
                }
            }
            
            changesSinceBackup = 0;

            try {
                File.Copy( sourceName, targetName, true );
            }catch(Exception ex){
                Logger.Log( "Map.SaveBackup: Error occured while trying to save backup to \""+targetName+"\": " + ex, LogType.Error );
                return;
            }

            FileInfo[] backupList = new DirectoryInfo( "backups" ).GetFiles( "*.fcm" );
            Array.Sort<FileInfo>( backupList, FileInfoComparer.instance );

            if( Config.GetInt( ConfigKey.MaxBackups ) > 0 ) {
                for( int i = backupList.Length - 1; i > Config.GetInt( ConfigKey.MaxBackups ); i-- ) {
                    try {
                        File.Delete( backupList[i].FullName );
                        Logger.Log( "Map.SaveBackup: Deleted old backup \"{0}\"", LogType.SystemActivity,
                                    backupList[i].Name );
                    } catch( Exception ex ) {
                        Logger.Log( "Map.SaveBackup: Error occured while trying delete old backup \"{0}\": " + ex, LogType.Error,
                                    backupList[i].Name );
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
    }
}