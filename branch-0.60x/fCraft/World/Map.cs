// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using fCraft.MapConversion;

namespace fCraft {
    public unsafe sealed class Map {

        public World World;

        /// <summary> Map width, in blocks. Equivalent to Notch's X (horizontal)</summary>
        public readonly int WidthX;

        /// <summary> Map length, in blocks. Equivalent to Notch's Z (horizontal)</summary>
        public readonly int WidthY;

        /// <summary> Map height, in blocks. Equivalent to Notch's Y (vertical)</summary>
        public readonly int Height;

        public BoundingBox Bounds {
            get { return new BoundingBox( Position.Zero, WidthX, WidthY, Height ); }
        }

        public Position Spawn;

        readonly Dictionary<string, Dictionary<string, string>> metadata = new Dictionary<string, Dictionary<string, string>>();

        // Queue of block updates. Updates are applied by ProcessUpdates()
        readonly ConcurrentQueue<BlockUpdate> updates = new ConcurrentQueue<BlockUpdate>();

        readonly object metaLock = new object(),
                        backupLock = new object();

        // used to skip backups/saves if no changes were made
        public bool ChangedSinceSave { get; internal set; }
        public bool ChangedSinceBackup { get; internal set; }

        public short[,] Shadows;

        // FCMv3 additions
        public DateTime DateModified = DateTime.UtcNow;
        public DateTime DateCreated = DateTime.UtcNow;
        public Guid Guid = Guid.NewGuid();

        // block data
        public byte[] Blocks;


        internal Map() {
            UpdateZoneCache();
        }

        // creates an empty new world of specified dimensions
        public Map( World world, int widthX, int widthY, int height, bool initBlockArray )
            : this() {
            World = world;

            WidthX = widthX;
            WidthY = widthY;
            Height = height;

            if( initBlockArray ) {
                int blockCount = WidthX * WidthY * Height;
                Blocks = new byte[blockCount];
                Blocks.Initialize();
            }
        }


        #region Saving

        public bool Save( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            string tempFileName = fileName + ".temp";

            try {
                ChangedSinceSave = false;
                if( !MapUtility.TrySave( this, tempFileName, MapFormat.FCMv3 ) ) {
                    ChangedSinceSave = true;
                }

            } catch( IOException ex ) {
                ChangedSinceSave = true;
                Logger.Log( "Map.Save: Unable to open file \"{0}\" for writing: {1}", LogType.Error,
                               tempFileName, ex.Message );
                try { File.Delete( tempFileName ); } catch { }
                return false;
            }

            try {
                Paths.MoveOrReplace( tempFileName, fileName );
                Logger.Log( "Saved map successfully to {0}", LogType.SystemActivity,
                            fileName );
                ChangedSinceBackup = true;

            } catch( Exception ex ) {
                ChangedSinceSave = true;
                Logger.Log( "Error trying to replace file \"{0}\": {1}", LogType.Error,
                            fileName, ex );
                try { File.Delete( tempFileName ); } catch { }
                return false;
            }
            return true;
        }


        internal int WriteMetadataFCMv3( Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryWriter writer = new BinaryWriter( stream );
            int metaCount = 0;
            lock( metaLock ) {
                foreach( var group in metadata ) {
                    foreach( var key in group.Value ) {
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
                    MapFCMv3.WriteLengthPrefixedString( writer, zone.Name );
                    MapFCMv3.WriteLengthPrefixedString( writer, zone.SerializeFCMv2() );
                    metaCount++;
                }
            }
            World thisWorld = World;
            if( thisWorld != null ) {
                MapFCMv3.WriteLengthPrefixedString( writer, "security" );
                MapFCMv3.WriteLengthPrefixedString( writer, "access" );
                MapFCMv3.WriteLengthPrefixedString( writer, thisWorld.AccessSecurity.Serialize().ToString() );
                MapFCMv3.WriteLengthPrefixedString( writer, "security" );
                MapFCMv3.WriteLengthPrefixedString( writer, "build" );
                MapFCMv3.WriteLengthPrefixedString( writer, thisWorld.BuildSecurity.Serialize().ToString() );
                metaCount += 2;
            }
            return metaCount;
        }

        #endregion


        #region Loading

        internal bool ValidateHeader() {
            if( !IsValidDimension( WidthX ) ) {
                Logger.Log( "Map.ValidateHeader: Invalid dimension specified for widthX: {0}.", LogType.Error, WidthX );
                return false;
            }

            if( !IsValidDimension( WidthY ) ) {
                Logger.Log( "Map.ValidateHeader: Invalid dimension specified for widthY: {0}.", LogType.Error, WidthY );
                return false;
            }

            if( !IsValidDimension( Height ) ) {
                Logger.Log( "Map.ValidateHeader: Invalid dimension specified for height: {0}.", LogType.Error, Height );
                return false;
            }

            if( Spawn.X > WidthX * 32 || Spawn.Y > WidthY * 32 || Spawn.H > Height * 32 || Spawn.X < 0 || Spawn.Y < 0 || Spawn.H < 0 ) {
                Logger.Log( "Map.ValidateHeader: Spawn coordinates are outside the valid range! Using center of the map instead.",
                            LogType.Warning );
                ResetSpawn();
            }

            return true;
        }


        // Only multiples of 16 are allowed, between 16 and 2032
        public static bool IsValidDimension( int dimension ) {
            return dimension > 0 && dimension % 16 == 0 && dimension < 2048;
        }



        internal bool RemoveUnknownBlocktypes( bool returnOnErrors ) {
            bool foundUnknownTypes = false;
            fixed( byte* ptr = Blocks ) {
                for( int j = 0; j < Blocks.Length; j++ ) {
                    if( ptr[j] > 49 ) {
                        if( returnOnErrors ) return false;
                        ptr[j] = 0;
                        foundUnknownTypes = true;
                    }
                }
            }
            if( foundUnknownTypes ) ChangedSinceSave = true;
            return !foundUnknownTypes;
        }


        public bool ConvertBlockTypes( byte[] mapping ) {
            bool mapped = false;
            fixed( byte* ptr = Blocks ) {
                for( int j = 0; j < Blocks.Length; j++ ) {
                    if( ptr[j] > 49 ) {
                        ptr[j] = mapping[ptr[j]];
                        mapped = true;
                    }
                }
            }
            if( mapped ) ChangedSinceSave = true;
            return mapped;
        }


        #endregion


        #region Metadata

        public string GetMeta( string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return GetMeta( "", key );
        }


        public string GetMeta( string group, string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            try {
                lock( metaLock ) {
                    return metadata[group][key];
                }
            } catch( KeyNotFoundException ) {
                return null;
            }
        }


        public void SetMeta( string key, string value ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            SetMeta( "", key, value );
        }


        public void SetMeta( string group, string key, string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( metaLock ) {
                if( !metadata.ContainsKey( group ) ) {
                    metadata[group] = new Dictionary<string, string>();
                }
                metadata[group][key] = value;
            }
            ChangedSinceSave = true;
        }

        #endregion


        #region Utilities

        static readonly Dictionary<string, Block> BlockNames = new Dictionary<string, Block>();

        static Map() {
            foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                if( block != Block.Undefined ) {
                    BlockNames.Add( block.ToString().ToLower(), block );
                    BlockNames.Add( ((int)block).ToString(), block );
                }
            }

            // alternative names for some blocks
            BlockNames["none"] = Block.Air;
            BlockNames["aire"] = Block.Air; // common typo
            BlockNames["nothing"] = Block.Air;
            BlockNames["empty"] = Block.Air;
            BlockNames["delete"] = Block.Air;
            BlockNames["erase"] = Block.Air;
            BlockNames["blank"] = Block.Air;

            BlockNames["cement"] = Block.Stone;
            BlockNames["concrete"] = Block.Stone;

            BlockNames["gras"] = Block.Grass; // common typo

            BlockNames["soil"] = Block.Dirt;
            BlockNames["stones"] = Block.Rocks;
            BlockNames["cobblestone"] = Block.Rocks;
            BlockNames["plank"] = Block.Wood;
            BlockNames["planks"] = Block.Wood;
            BlockNames["board"] = Block.Wood;
            BlockNames["boards"] = Block.Wood;
            BlockNames["tree"] = Block.Plant;
            BlockNames["sappling"] = Block.Plant;
            BlockNames["adminium"] = Block.Admincrete;
            BlockNames["adminite"] = Block.Admincrete;
            BlockNames["opcrete"] = Block.Admincrete;
            BlockNames["hardrock"] = Block.Admincrete;
            BlockNames["solid"] = Block.Admincrete;
            BlockNames["bedrock"] = Block.Admincrete;
            BlockNames["gold_ore"] = Block.GoldOre;
            BlockNames["iron_ore"] = Block.IronOre;
            BlockNames["copper"] = Block.IronOre;
            BlockNames["copperore"] = Block.IronOre;
            BlockNames["copper_ore"] = Block.IronOre;
            BlockNames["ore"] = Block.IronOre;
            BlockNames["coals"] = Block.Coal;
            BlockNames["coalore"] = Block.Coal;
            BlockNames["blackore"] = Block.Coal;

            BlockNames["trunk"] = Block.Log;
            BlockNames["stump"] = Block.Log;
            BlockNames["treestump"] = Block.Log;
            BlockNames["treetrunk"] = Block.Log;

            BlockNames["leaf"] = Block.Leaves;
            BlockNames["foliage"] = Block.Leaves;

            BlockNames["cheese"] = Block.Sponge;

            BlockNames["redcloth"] = Block.Red;
            BlockNames["redwool"] = Block.Red;
            BlockNames["orangecloth"] = Block.Orange;
            BlockNames["orangewool"] = Block.Orange;
            BlockNames["yellowcloth"] = Block.Yellow;
            BlockNames["yellowwool"] = Block.Yellow;
            BlockNames["limecloth"] = Block.Lime;
            BlockNames["limewool"] = Block.Lime;
            BlockNames["greenyellow"] = Block.Lime;
            BlockNames["yellowgreen"] = Block.Lime;
            BlockNames["lightgreen"] = Block.Lime;
            BlockNames["lightgreencloth"] = Block.Lime;
            BlockNames["lightgreenwool"] = Block.Lime;
            BlockNames["greencloth"] = Block.Green;
            BlockNames["greenwool"] = Block.Green;
            BlockNames["springgreen"] = Block.Teal;
            BlockNames["emerald"] = Block.Teal;
            BlockNames["tealwool"] = Block.Teal;
            BlockNames["tealcloth"] = Block.Teal;
            BlockNames["aquawool"] = Block.Aqua;
            BlockNames["aquacloth"] = Block.Aqua;
            BlockNames["cyanwool"] = Block.Cyan;
            BlockNames["cyancloth"] = Block.Cyan;
            BlockNames["bluewool"] = Block.Blue;
            BlockNames["bluecloth"] = Block.Blue;
            BlockNames["indigowool"] = Block.Indigo;
            BlockNames["indigocloth"] = Block.Indigo;
            BlockNames["violetwool"] = Block.Violet;
            BlockNames["violetcloth"] = Block.Violet;
            BlockNames["lightpurple"] = Block.Violet;
            BlockNames["purple"] = Block.Violet;
            BlockNames["purplewool"] = Block.Violet;
            BlockNames["purplecloth"] = Block.Violet;
            BlockNames["fuchsia"] = Block.Magenta;
            BlockNames["magentawool"] = Block.Magenta;
            BlockNames["magentacloth"] = Block.Magenta;
            BlockNames["darkpink"] = Block.Pink;
            BlockNames["pinkwool"] = Block.Pink;
            BlockNames["pinkcloth"] = Block.Pink;
            BlockNames["cloth"] = Block.White;
            BlockNames["cotton"] = Block.White;
            BlockNames["grey"] = Block.Gray;
            BlockNames["lightgray"] = Block.Gray;
            BlockNames["lightgrey"] = Block.Gray;
            BlockNames["darkgray"] = Block.Black;
            BlockNames["darkgrey"] = Block.Black;

            BlockNames["yellow_flower"] = Block.YellowFlower;
            BlockNames["flower"] = Block.YellowFlower;
            BlockNames["rose"] = Block.RedFlower;
            BlockNames["redrose"] = Block.RedFlower;
            BlockNames["red_flower"] = Block.RedFlower;

            BlockNames["mushroom"] = Block.BrownMushroom;
            BlockNames["shroom"] = Block.BrownMushroom;
            BlockNames["brown_shroom"] = Block.BrownMushroom;
            BlockNames["red_shroom"] = Block.RedMushroom;

            BlockNames["goldblock"] = Block.Gold;
            BlockNames["goldsolid"] = Block.Gold;
            BlockNames["golden"] = Block.Gold;
            BlockNames["copper"] = Block.Gold;
            BlockNames["brass"] = Block.Gold;

            BlockNames["ironblock"] = Block.Steel;
            BlockNames["iron"] = Block.Steel;
            BlockNames["metal"] = Block.Steel;
            BlockNames["silver"] = Block.Steel;

            BlockNames["slab"] = Block.Stair;
            BlockNames["slabs"] = Block.DoubleStair;
            BlockNames["steps"] = Block.DoubleStair;
            BlockNames["stairs"] = Block.DoubleStair;
            BlockNames["doublestep"] = Block.DoubleStair;
            BlockNames["double_step"] = Block.DoubleStair;
            BlockNames["double_stair"] = Block.DoubleStair;
            BlockNames["staircasefull"] = Block.DoubleStair;
            BlockNames["step"] = Block.Stair;
            BlockNames["halfstep"] = Block.Stair;
            BlockNames["halfblock"] = Block.Stair;
            BlockNames["staircasestep"] = Block.Stair;

            BlockNames["bricks"] = Block.Brick;
            BlockNames["explosive"] = Block.TNT;
            BlockNames["dynamite"] = Block.TNT;

            BlockNames["book"] = Block.Books;
            BlockNames["shelf"] = Block.Books;
            BlockNames["shelves"] = Block.Books;
            BlockNames["bookcase"] = Block.Books;
            BlockNames["bookshelf"] = Block.Books;
            BlockNames["bookshelves"] = Block.Books;

            BlockNames["moss"] = Block.MossyRocks;
            BlockNames["mossy"] = Block.MossyRocks;
            BlockNames["stonevine"] = Block.MossyRocks;
            BlockNames["mossyrock"] = Block.MossyRocks;
            BlockNames["mossystone"] = Block.MossyRocks;
            BlockNames["mossystones"] = Block.MossyRocks;
            BlockNames["greencobblestone"] = Block.MossyRocks;
            BlockNames["mossycobblestone"] = Block.MossyRocks;
            BlockNames["mossy_cobblestone"] = Block.MossyRocks;
            BlockNames["blockthathasgreypixelsonitmostlybutsomeareactuallygreen"] = Block.MossyRocks;

            BlockNames["onyx"] = Block.Obsidian;
        }


        public void SetSpawn( Position newSpawn ) {
            Spawn = newSpawn;
            ChangedSinceSave = true;
        }


        public void ResetSpawn() {
            Spawn.X = (short)(WidthX * 16);
            Spawn.Y = (short)(WidthY * 16);
            Spawn.H = (short)(Height * 32);
            Spawn.R = 0;
            Spawn.L = 0;
            ChangedSinceSave = true;
        }


        public void CalculateShadows() {
            if( Shadows != null ) return;

            Shadows = new short[WidthX, WidthY];
            for( int x = 0; x < WidthX; x++ ) {
                for( int y = 0; y < WidthY; y++ ) {
                    for( short h = (short)(Height - 1); h >= 0; h-- ) {
                        switch( GetBlock( x, y, h ) ) {
                            case Block.Air:
                            case Block.Leaves:
                            case Block.Glass:
                            case Block.RedFlower:
                            case Block.RedMushroom:
                            case Block.YellowFlower:
                            case Block.BrownMushroom:
                                continue;
                            default:
                                Shadows[x, y] = h;
                                break;
                        }
                        break;
                    }
                }
            }
        }


        internal static Block GetBlockByName( string block ) {
            if( block == null ) throw new ArgumentNullException( "block" );
            block = block.ToLower();
            return BlockNames.ContainsKey( block ) ? BlockNames[block] : Block.Undefined;
        }




        /// <summary> Writes a copy of the current map to a specified stream, compressed with GZipStream. </summary>
        /// <param name="stream"> Stream to write the compressed data to. </param>
        /// <param name="prependBlockCount"> If true, prepends block data with signed, 32bit, big-endian block count. </param>
        public void GetCompressedCopy( Stream stream, bool prependBlockCount ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            using( GZipStream compressor = new GZipStream( stream, CompressionMode.Compress ) ) {
                if( prependBlockCount ) {
                    // convert block count to big-endian
                    int convertedBlockCount = IPAddress.HostToNetworkOrder( Blocks.Length );
                    // write block count to gzip stream
                    compressor.Write( BitConverter.GetBytes( convertedBlockCount ), 0, sizeof( int ) );
                }
                compressor.Write( Blocks, 0, Blocks.Length );
            }
        }


        public void MakeFloodBarrier() {
            for( int x = 0; x < WidthX; x++ ) {
                for( int y = 0; y < WidthY; y++ ) {
                    SetBlock( x, y, 0, Block.Admincrete );
                }
            }

            for( int x = 0; x < WidthX; x++ ) {
                for( int h = 0; h < Height / 2; h++ ) {
                    SetBlock( x, 0, h, Block.Admincrete );
                    SetBlock( x, WidthY - 1, h, Block.Admincrete );
                }
            }

            for( int y = 0; y < WidthY; y++ ) {
                for( int h = 0; h < Height / 2; h++ ) {
                    SetBlock( 0, y, h, Block.Admincrete );
                    SetBlock( WidthX - 1, y, h, Block.Admincrete );
                }
            }
        }


        /// <summary>
        /// Returns the block count (volume) of the map.
        /// </summary>
        public int GetBlockCount() {
            return WidthX * WidthY * Height;
        }

        #endregion


        #region Zones

        readonly object zoneLock = new object(); // zone list (only needed when using "zones" dictionary, not "zoneList" cached array)
        readonly Dictionary<string, Zone> zones = new Dictionary<string, Zone>();
        public Zone[] ZoneList { get; private set; }


        public bool AddZone( Zone zone ) {
            if( zone == null ) throw new ArgumentNullException( "zone" );
            lock( zoneLock ) {
                if( zones.ContainsKey( zone.Name.ToLower() ) ) return false;
                zones.Add( zone.Name.ToLower(), zone );
                ChangedSinceSave = true;
                UpdateZoneCache();
            }
            return true;
        }


        public bool RemoveZone( string zone ) {
            if( zone == null ) throw new ArgumentNullException( "zone" );
            lock( zoneLock ) {
                if( !zones.ContainsKey( zone.ToLower() ) ) return false;
                zones.Remove( zone.ToLower() );
                ChangedSinceSave = true;
                UpdateZoneCache();
            }
            return true;
        }


        public PermissionOverride CheckZones( int x, int y, int h, Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            PermissionOverride result = PermissionOverride.None;
            Zone[] zoneListCache = ZoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].Bounds.Contains( x, y, h ) ) {
                    if( zoneListCache[i].Controller.Check( player.Info ) ) {
                        result = PermissionOverride.Allow;
                    } else {
                        return PermissionOverride.Deny;
                    }
                }
            }
            return result;
        }


        public Zone FindDeniedZone( int x, int y, int h, Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Zone[] zoneListCache = ZoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].Bounds.Contains( x, y, h ) && !zoneListCache[i].Controller.Check( player.Info ) ) {
                    return zoneListCache[i];
                }
            }
            return null;
        }


        public bool TestZones( short x, short y, short h, Player player, out Zone[] allowedZones, out Zone[] deniedZones ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            List<Zone> allowed = new List<Zone>(), denied = new List<Zone>();
            bool found = false;

            Zone[] zoneListCache = ZoneList;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].Bounds.Contains( x, y, h ) ) {
                    found = true;
                    if( zoneListCache[i].Controller.Check( player.Info ) ) {
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
            if( name == null ) throw new ArgumentNullException( "name" );
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
                ZoneList = newZoneList;
            }
        }

        #endregion


        #region Block Updates & Simulation

        public int Index( int x, int y, int h ) {
            return (h * WidthY + y) * WidthX + x;
        }


        public void SetBlock( int x, int y, int h, Block type ) {
            if( x < WidthX && y < WidthY && h < Height && x >= 0 && y >= 0 && h >= 0 ) {
                Blocks[Index( x, y, h )] = (byte)type;
                ChangedSinceSave = true;
            }
        }

        public void SetBlock( int x, int y, int h, byte type ) {
            if( h < Height && x < WidthX && y < WidthY && x >= 0 && y >= 0 && h >= 0 && type < 50 ) {
                Blocks[Index( x, y, h )] = type;
                ChangedSinceSave = true;
            }
        }

        public void SetBlock( Vector3i vec, Block type ) {
            if( vec.X < WidthX && vec.Z < WidthY && vec.Y < Height && vec.X >= 0 && vec.Z >= 0 && vec.Y >= 0 && (byte)type < 50 ) {
                Blocks[Index( vec.X, vec.Z, vec.Y )] = (byte)type;
                ChangedSinceSave = true;
            }
        }

        public void SetBlock( Vector3i vec, byte type ) {
            if( vec.X < WidthX && vec.Z < WidthY && vec.Y < Height && vec.X >= 0 && vec.Z >= 0 && vec.Y >= 0 && type < 50 ) {
                Blocks[Index( vec.X, vec.Z, vec.Y )] = type;
                ChangedSinceSave = true;
            }
        }


        public byte GetBlockByte( int x, int y, int h ) {
            if( x < WidthX && y < WidthY && h < Height && x >= 0 && y >= 0 && h >= 0 )
                return Blocks[Index( x, y, h )];
            return 0;
        }

        public Block GetBlock( int x, int y, int h ) {
            if( x < WidthX && y < WidthY && h < Height && x >= 0 && y >= 0 && h >= 0 )
                return (Block)Blocks[Index( x, y, h )];
            return Block.Undefined;
        }

        public byte GetBlockByte( Vector3i vec ) {
            if( vec.X < WidthX && vec.Z < WidthY && vec.Y < Height && vec.X >= 0 && vec.Z >= 0 && vec.Y >= 0 )
                return Blocks[Index( vec.X, vec.Z, vec.Y )];
            return 0;
        }


        public bool InBounds( int x, int y, int h ) {
            return x < WidthX && y < WidthY && h < Height && x >= 0 && y >= 0 && h >= 0;
        }

        public bool InBounds( Vector3i vec ) {
            return vec.X < WidthX && vec.Z < WidthY && vec.Y < Height && vec.X >= 0 && vec.Z >= 0 && vec.Y >= 0;
        }


        public int SearchColumn( int x, int y, Block id ) {
            return SearchColumn( x, y, id, Height - 1 );
        }

        public int SearchColumn( int x, int y, Block id, int startH ) {
            for( int h = startH; h > 0; h-- ) {
                if( GetBlock( x, y, h ) == id ) {
                    return h;
                }
            }
            return -1; // -1 means 'not found'
        }


        public void QueueUpdate( BlockUpdate update ) {
            updates.Enqueue( update );
        }


        public void ClearUpdateQueue() {
            BlockUpdate temp = new BlockUpdate();
            while( updates.Dequeue( ref temp ) ) { }
        }


        public int UpdateQueueSize() {
            return updates.Length;
        }


        public void ProcessUpdates() {
            if( World.IsLocked ) {
                if( World.PendingUnload ) {
                    World.UnloadMap( true );
                }
                return;
            }

            int packetsSent = 0;
            int maxPacketsPerUpdate = Server.CalculateMaxPacketsPerUpdate( World );
            BlockUpdate update = new BlockUpdate();
            while( packetsSent < maxPacketsPerUpdate ) {
                if( !updates.Dequeue( ref update ) ) {
                    if( World.IsFlushing ) {
                        World.EndFlushMapBuffer();
                    }
                    break;
                }
                ChangedSinceSave = true;
                if( !InBounds( update.X, update.Y, update.H ) ) continue;
                int blockIndex = Index( update.X, update.Y, update.H );
                Blocks[blockIndex] = update.BlockType; // TODO: investigate IndexOutOfRangeException here

                if( !World.IsFlushing ) {
                    World.SendToAllDelayed( PacketWriter.MakeSetBlock( update.X, update.Y, update.H, update.BlockType ), update.Origin );
                }
                packetsSent++;
            }

            if( packetsSent == 0 && World.PendingUnload ) {
                World.UnloadMap( true );
            }
        }

        #endregion


        #region Backup

        public void SaveBackup( string sourceName, string targetName, bool onlyIfChanged ) {
            if( sourceName == null ) throw new ArgumentNullException( "sourceName" );
            if( targetName == null ) throw new ArgumentNullException( "targetName" );
            if( onlyIfChanged && !ChangedSinceBackup && ConfigKey.BackupOnlyWhenChanged.GetBool() ) return;

            lock( backupLock ) {
                DirectoryInfo d = new DirectoryInfo( Paths.BackupPath );

                if( !d.Exists ) {
                    try {
                        d.Create();
                    } catch( Exception ex ) {
                        Logger.Log( "Map.SaveBackup: Error occured while trying to create backup directory: {0}", LogType.Error,
                                    ex );
                        return;
                    }
                }

                try {
                    ChangedSinceBackup = false;
                    File.Copy( sourceName, targetName, true );
                } catch( Exception ex ) {
                    ChangedSinceBackup = true;
                    Logger.Log( "Map.SaveBackup: Error occured while trying to save backup to \"{0}\": {1}", LogType.Error,
                                targetName, ex );
                    return;
                }

                List<FileInfo> backupList = new List<FileInfo>( d.GetFiles( "*.fcm" ) );
                backupList.Sort( FileInfoComparer.Instance );

                if( ConfigKey.MaxBackups.GetInt() > 0 ) {
                    while( backupList.Count > ConfigKey.MaxBackups.GetInt() ) {
                        FileInfo info = backupList[backupList.Count - 1];
                        backupList.RemoveAt( backupList.Count - 1 );
                        try {
                            File.Delete( info.FullName );
                        } catch( Exception ex ) {
                            Logger.Log( "Map.SaveBackup: Error occured while trying delete old backup \"{0}\": {1}", LogType.Error,
                                        info.FullName, ex );
                            break;
                        }
                        Logger.Log( "Map.SaveBackup: Deleted old backup \"{0}\"", LogType.SystemActivity,
                                    info.Name );
                    }
                }


                if( ConfigKey.MaxBackupSize.GetInt() > 0 ) {
                    while( true ) {
                        FileInfo[] fis = d.GetFiles();
                        long size = fis.Sum( fi => fi.Length );

                        if( size / 1024 / 1024 > ConfigKey.MaxBackupSize.GetInt() ) {
                            FileInfo info = backupList[backupList.Count - 1];
                            backupList.RemoveAt( backupList.Count - 1 );
                            try {
                                File.Delete( info.FullName );
                            } catch( Exception ex ) {
                                Logger.Log( "Map.SaveBackup: Error occured while trying delete old backup \"{0}\": {1}", LogType.Error,
                                            info.Name, ex );
                                break;
                            }
                            Logger.Log( "Map.SaveBackup: Deleted old backup \"{0}\"", LogType.SystemActivity,
                                        info.Name );
                        } else {
                            break;
                        }
                    }
                }
            }

            Logger.Log( "AutoBackup: " + targetName, LogType.SystemActivity );
        }


        sealed class FileInfoComparer : IComparer<FileInfo> {
            public static readonly FileInfoComparer Instance = new FileInfoComparer();
            public int Compare( FileInfo x, FileInfo y ) {
                return -x.CreationTime.CompareTo( y.CreationTime );
            }
        }

        #endregion
    }
}