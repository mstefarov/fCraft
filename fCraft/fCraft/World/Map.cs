// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;


namespace fCraft {
    public sealed class Map {
        static Dictionary<string, Block> blockNames = new Dictionary<string, Block>();
        World world;

        public static void Init() {
            foreach( string block in Enum.GetNames( typeof( Block ) ) ) {
                blockNames.Add( block.ToLower(), (Block)Enum.Parse( typeof( Block ), block ) );
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
            blockNames["grey"] = Block.Gray;
            blockNames["flower"] = Block.YellowFlower;

            blockNames["mushroom"] = Block.BrownMushroom;
            blockNames["shroom"] = Block.BrownMushroom;

            blockNames["iron"] = Block.Steel;
            blockNames["metal"] = Block.Steel;
            blockNames["silver"] = Block.Steel;

            blockNames["slab"] = Block.DoubleStair;
            blockNames["slabs"] = Block.DoubleStair;
            blockNames["stairs"] = Block.DoubleStair;

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
        }

        internal static Block GetBlockByName( string block ) {
            return blockNames[block];
        }


        internal void CopyBlocks( byte[] source, int offset ) {
            blocks = new byte[widthX * widthY * height];
            Array.Copy( source, offset, blocks, 0, blocks.Length );
        }


        internal bool ValidateBlockTypes() {
            for( int i = 0; i < blocks.Length; i++ ) {
                if( (blocks[i]) > 49 ) {
                    return false;
                }
            }
            return true;
        }


        // ==== Members =======================================================

        byte[] blocks;
        public int widthX, widthY, height;
        public Position spawn;
        public Dictionary<string, string> meta = new Dictionary<string, string>();
        public Dictionary<string, Zone> zones = new Dictionary<string, Zone>();

        public bool AddZone( Zone z ) {
            lock( zoneLock ) {
                if( zones.ContainsKey( z.name.ToLowerInvariant() ) ) return false;
                zones.Add( z.name.ToLowerInvariant(), z );
            }
            return true;
        }

        public bool RemoveZone( string z ) {
            lock( zoneLock ) {
                if( !zones.ContainsKey( z.ToLowerInvariant() ) ) return false;
                zones.Remove( z.ToLowerInvariant() );
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

        public bool CheckZones( short x, short y, short h, Player player, ref bool zoneOverride, ref string zoneName ) {
            bool found = false;
            lock( zoneLock ) {
                foreach( Zone zone in world.map.zones.Values ) {
                    zoneName = zone.name;
                    if( zone.Contains( x, y, h ) ) {
                        if( zone.CanBuild( player ) ) {
                            zoneOverride = true;
                            return true;
                        } else {
                            found = true;
                        }
                    }
                }
            }
            return found;
        }

        Queue<BlockUpdate> updates = new Queue<BlockUpdate>();
        object queueLock = new object(), metaLock = new object(), zoneLock = new object();
        public int changesSinceSave, changesSinceBackup;


        public Map( World _world) {
            world = _world;
        }

        public Map( World _world, int _widthX, int _widthY, int _height ) : this(_world) {
            widthX = _widthX;
            widthY = _widthY;
            height = _height;

            int blockCount = widthX * widthY * height;

            blocks = new byte[blockCount];
            for( int i = 0; i < blocks.Length; i++ ) {
                blocks[i] = 0;
            }
        }


        // ==== Saving ========================================================


        public bool Save( string fileName ) {
            string tempFileName = fileName + "." + (new Random().Next().ToString());

            using( FileStream fs = File.Create( tempFileName ) ) {
                try {
                    WriteHeader( fs );
                    WriteMetadata( fs );
                    changesSinceSave = 0;
                    GetCompressedCopy( fs, false );
                } catch( IOException ex ) {
                    Logger.Log( "Map.Save: Unable to open file \"{0}\" for writing: {1}", LogType.Error,
                                   tempFileName, ex.Message );
                    if( File.Exists( tempFileName ) ) {
                        File.Delete( tempFileName );
                    }
                    return false;
                }
            }
            if( File.Exists( fileName ) ) {
                File.Delete( fileName );
            }
            File.Move( tempFileName, fileName );
            changesSinceBackup++;
            Logger.Log( "Saved map succesfully to {0}", LogType.SystemActivity, fileName );
            return true;
        }


        void WriteHeader( FileStream fs ) {
            BinaryWriter writer = new BinaryWriter( fs );
            writer.Write( Config.LevelFormatID );
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


        void WriteMetadata( FileStream fs ) {
            BinaryWriter writer = new BinaryWriter( fs );
            lock( metaLock ) {
                writer.Write( (ushort)(meta.Count+zones.Count) );
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


        void WriteLengthPrefixedString( BinaryWriter writer, string s ) {
            byte[] stringData = ASCIIEncoding.ASCII.GetBytes( s );
            writer.Write( stringData.Length );
            writer.Write( stringData );
        }


        // ==== Loading =======================================================


        public static Map Load( World _world, string fileName ) {
            if( !File.Exists( fileName ) ) {
                Logger.Log( "Map.Load: Specified file does not exist: {0}", LogType.Warning, fileName );
                return null;
            }

            switch( Path.GetExtension( fileName ).ToLowerInvariant() ) {
                case ".fcm":
                    return LoadFCM( _world, fileName );
                case ".dat":
                    return MapLoaderDAT.Load( _world, fileName );
                default:
                    throw new Exception( "Unknown map file format." );
            }
        }

        static Map LoadFCM( World _world, string fileName ) {
            FileStream fs = null;
            Map map = new Map( _world );
            try {
                fs = File.OpenRead( fileName );
                if( map.ReadHeader( fs ) ) {
                    map.ReadMetadata( fs );
                    map.ReadBlocks( fs );
                    if( !map.ValidateBlockTypes() ) {
                        throw new Exception( "Invalid block types detected. File is possibly corrupt." );
                    }
                    Logger.Log( "Loaded map succesfully from {0}", LogType.SystemActivity, fileName );
                    return map;
                } else {
                    return null;
                }
            } catch( EndOfStreamException ) {
                Logger.Log( "Map.Load: Unexpected end of file - possible corruption!", LogType.Error );
                return null;
            } catch( Exception ex ) {
                Logger.Log( "Map.Load: Error trying to read from \"{0}\": {1}", LogType.Error, fileName, ex.Message );
                return null;
            } finally {
                if( fs != null ) {
                    fs.Close();
                }
            }
        }


        // Parse the level header
        bool ReadHeader( FileStream fs ) {
            BinaryReader reader = new BinaryReader( fs );
            try {
                // TODO: reevaluate whether i need these restrictions or not
                if( reader.ReadUInt32() != Config.LevelFormatID ) {
                    Logger.Log( "Map.ReadHeader: Incorrect level format id (expected: {0}).", LogType.Error, Config.LevelFormatID );
                    return false;
                }

                widthX = reader.ReadUInt16();
                if( !IsValidDimension( widthX ) ) {
                    Logger.Log( "Map.ReadHeader: Invalid dimension specified for widthX: {0}.", LogType.Error, widthX );
                    return false;
                }

                widthY = reader.ReadUInt16();
                if( !IsValidDimension( widthY ) ) {
                    Logger.Log( "Map.ReadHeader: Invalid dimension specified for widthY: {0}.", LogType.Error, widthY );
                    return false;
                }

                height = reader.ReadUInt16();
                if( !IsValidDimension( height ) ) {
                    Logger.Log( "Map.ReadHeader: Invalid dimension specified for height: {0}.", LogType.Error, height );
                    return false;
                }

                spawn.x = reader.ReadInt16();
                spawn.y = reader.ReadInt16();
                spawn.h = reader.ReadInt16();
                spawn.r = reader.ReadByte();
                spawn.l = reader.ReadByte();
                if( spawn.x > widthX * 32 || spawn.y > widthY * 32 || spawn.h > height * 32 ||
                    spawn.x < 0 || spawn.y < 0 || spawn.h < 0 ) {
                    Logger.Log( "Map.ReadHeader: Spawn coordinates are outside the valid range! Using center of the map instead.", LogType.Warning );
                    spawn.Set( widthX / 2 * 32, widthY / 2 * 32, height / 2 * 32, 0, 0 );
                }

            } catch( FormatException ex ) {
                Logger.Log( "Map.ReadHeader: Cannot parse one or more of the header entries: {0}", LogType.Error, ex.Message );
                return false;
            }
            return true;

        }


        void ReadBlocks( FileStream fs ) {
            int blockCount = widthX * widthY * height;
            blocks = new byte[blockCount];

            GZipStream decompressor = new GZipStream( fs, CompressionMode.Decompress );
            decompressor.Read( blocks, 0, blockCount );
            decompressor.Flush();
        }


        void ReadMetadata( FileStream fs ) {
            BinaryReader reader = new BinaryReader( fs );
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

            } catch( FormatException ex ) {
                Logger.Log( "Map.ReadMetadata: Cannot parse one or more of the metadata entries: {0}", LogType.Error, ex.Message );
            }
        }

        string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = reader.ReadInt32();
            byte[] stringData = reader.ReadBytes( length );
            return ASCIIEncoding.ASCII.GetString( stringData );
        }


        // Only power-of-2 dimensions are allowed
        public static bool IsValidDimension( int dimension ) {
            return dimension > 0 && dimension % 16 == 0 && dimension < 2048;
        }


        // zips a copy of the block array
        public void GetCompressedCopy( Stream stream, bool prependBlockCount ) {
            using( GZipStream compressor = new GZipStream( stream, CompressionMode.Compress ) ) {
                if( prependBlockCount ) {
                    // convert block count to big-endian
                    int convertedBlockCount = Server.htons( blocks.Length );
                    // write block count to gzip stream
                    compressor.Write( BitConverter.GetBytes( convertedBlockCount ), 0, sizeof( int ) );
                }
                compressor.Write( blocks, 0, blocks.Length );
            }
        }


        // ==== Simulation ====================================================

        public int Index( int x, int y, int h ) {
            return (h * widthY + y) * widthX + x;
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
            lock( queueLock ) {
                updates.Enqueue( update );
            }
        }


        internal void ClearUpdateQueue() {
            lock( queueLock ) {
                updates.Clear();
            }
        }


        public void ProcessUpdates() {
            int packetsSent = 0;
            int maxPacketsPerUpdate = Server.CalculateMaxPacketsPerUpdate(world);
            BlockUpdate update;

            while( updates.Count > 0 && packetsSent < maxPacketsPerUpdate ) {
                lock( queueLock ) {
                    update = updates.Dequeue();
                }
                changesSinceSave++;
                SetBlock( update.x, update.y, update.h, update.type );
                world.SendToAllDelayed( PacketWriter.MakeSetBlock( update.x, update.y, update.h, update.type ), update.origin);
                if( update.origin != null ) {
                    update.origin.info.ProcessBlockBuild( update.type );
                }
                packetsSent++;
            }

            /*if( world.loadSendingInProgress ) { //TODO: streamload
                if( packetsSent < maxPacketsPerUpdate ) {
                    GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                    GC.WaitForPendingFinalizers();
                    world.SendToAll( PacketWriter.MakeMessage( Color.Red+"Map load complete." ), null );
                    Logger.Log( "Load command finished succesfully.", LogType.SystemActivity );
                    world.loadSendingInProgress = false;
                    //world.EndLockDown();
                } else {
                    if( !world.loadProgressReported && world.completedBlockUpdates / (float)world.totalBlockUpdates > 0.5f ) {
                        world.SendToAll( PacketWriter.MakeMessage( Color.Red + "Map loading: 50%" ), null );
                        world.loadProgressReported = true;
                    }
                    world.completedBlockUpdates += packetsSent;
                }
            }*/
        }


        public int CompareAndUpdate( Map other ) {
            int totalBlockUpdates = 0;
            int step = 8;
            for( int x = 0; x < widthX; x += step ) {
                for( int y = 0; y < widthY; y += step ) {
                    for( int h = 0; h < height; h += step ) {

                        for( int h2 = 0; h2 < step; h2++ ) {
                            for( int x2 = 0; x2 < step; x2++ ) {
                                for( int y2 = 0; y2 < step; y2++ ) {
                                    int index = Index( x + x2, y + y2, h + h2 );
                                    if( blocks[index] != other.blocks[index] ) {
                                        QueueUpdate( new BlockUpdate( null, x + x2, y + y2, h + h2, other.blocks[index] ) );
                                        totalBlockUpdates++;
                                    }
                                }
                            }
                        }

                    }
                }
            }
            return totalBlockUpdates;
        }


        public void SaveBackup( string sourceName, string targetName ) {
            if( changesSinceBackup == 0 && Config.GetBool( "BackupOnlyWhenChanged" ) ) return;
            if( !Directory.Exists( "backups" ) ) {
                Directory.CreateDirectory( "backups" );
            }
            changesSinceBackup = 0;
            File.Copy( sourceName, targetName, true );
            FileInfo[] info = new DirectoryInfo( "backups" ).GetFiles();
            Array.Sort<FileInfo>( info, FileInfoComparer.instance );
            Queue<string> files = new Queue<string>();
            for( int i = 0; i < info.Length; i++ ) {
                if( info[i].Extension == ".fcm" ) {
                    files.Enqueue( info[i].Name );
                }
            }
            if( Config.GetInt( "MaxBackups" ) > 0 ) {
                while( files.Count > Config.GetInt( "MaxBackups" ) ) {
                    File.Delete( "backups/" + files.Dequeue() );
                }
            }
            Logger.Log( "AutoBackup: " + targetName, LogType.SystemActivity );
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


        class FileInfoComparer : IComparer<FileInfo> {
            public static FileInfoComparer instance = new FileInfoComparer();
            public int Compare( FileInfo x, FileInfo y ) {
                return x.CreationTime.CompareTo( y.CreationTime );
            }
        }
    }
}