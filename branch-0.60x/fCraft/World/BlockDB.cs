// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Runtime.InteropServices;
using fCraft.Events;
using System.IO;
using System.Collections.Generic;

namespace fCraft {
    public class BlockDB {

        public BlockDB( World world ) {
            World = world;
        }


        public World World { get; set; }

        public bool Enabled { get; set; }

        internal readonly object SyncRoot = new object();


        #region Preload

        BlockDBEntry[] cacheStore;
        int cacheSize;
        const int CacheSizeIncrement = 1024;

        public void Add( BlockDBEntry item ) {
            if( cacheSize == cacheStore.Length ) {
                EnsureCapacity( cacheSize + 1 );
            }
            cacheStore[cacheSize++] = item;
        }


        void EnsureCapacity( int min ) {
            if( cacheStore.Length < min ) {
                int num = cacheStore.Length + CacheSizeIncrement;
                if( num < min ) {
                    num = min;
                }
                CacheCapacity = num;
            }
        }


        int CacheCapacity {
            get {
                return cacheStore.Length;
            }
            set {
                if( value != cacheStore.Length ) {
                    BlockDBEntry[] destinationArray = new BlockDBEntry[value];
                    if( value < CacheCapacity ) {
                        Array.Copy( cacheStore, cacheSize - value, destinationArray, 0, value );
                    } else {
                        Array.Copy( cacheStore, 0, destinationArray, 0, cacheSize );
                    }
                    cacheStore = destinationArray;
                }
            }
        }


        bool isPreloaded;
        public unsafe bool IsPreloaded {
            get {
                return isPreloaded;
            }
            set {
                lock( SyncRoot ) {
                    if( value == isPreloaded ) return;
                    if( value == true ) {
                        Flush();
                        byte[] bytes = Load();
                        int entryCount = bytes.Length / BlockDB.BlockDBEntrySize;
                        EnsureCapacity( entryCount );
                        fixed( byte* parr = bytes ) {
                            BlockDBEntry* entries = (BlockDBEntry*)parr;
                            Buffer.BlockCopy( (Array)entries, 0, cacheStore, 0, entryCount );
                            for( int i = 0; i < entryCount; i++ ) {
                                cache.Add( entries[i] );
                            }
                        }
                        pendingChanges = new List<BlockDBEntry>();
                    } else {
                        Flush();
                    }
                    isPreloaded = value;
                }
            }
        }

        #endregion


        void TrimFile( int maxCapacity ) {
            if( maxCapacity == 0 ) {
                using( File.Create( FileName ) ) { }
                return;
            }
            string tempFileName = FileName + ".tmp";
            using( FileStream source = File.OpenRead( FileName ) ) {
                int entries = (int)(source.Length / BlockDBEntrySize);
                if( entries <= maxCapacity ) return;
                source.Seek( (entries - maxCapacity) * BlockDBEntrySize, SeekOrigin.Begin );
                byte[] buffer = new byte[16 * 16 * 16];
                using( FileStream destination = File.Create( tempFileName ) ) {
                    int bytesRead;
                    while( true ) {
                        bytesRead = source.Read( buffer, 0, buffer.Length );
                        if( bytesRead == 0 ) break;
                        destination.Write( buffer, 0, bytesRead );
                    }
                }
                Paths.MoveOrReplace( tempFileName, FileName );
            }
        }


        unsafe int CountNewerEntries( TimeSpan age ) {
            int minTimestamp = (int)DateTime.UtcNow.Subtract( age ).ToUnixTime();

            if( isPreloaded ) {
                fixed( BlockDBEntry* ptr = cacheStore ) {
                    for( int i = 0; i < cacheSize; i++ ) {
                        if( ptr[i].Timestamp < minTimestamp ) {
                            return cacheSize - i;
                        }
                    }
                }
                return cacheSize;

            } else {
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDB.BlockDBEntrySize;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = entryCount - 1; i >= 0; i-- ) {
                        if( entries[i].Timestamp < minTimestamp ) {
                            return entryCount - i;
                        }
                    }
                }
                return entryCount;
            }
        }


        List<BlockDBEntry> pendingChanges = new List<BlockDBEntry>();

        int lastFlushedIndex = 0;


        int limit;
        public int Limit {
            get { return limit; }
            set {
                if( value < 0 ) throw new ArgumentOutOfRangeException();
                lock( SyncRoot ) {
                    if( value != 0 && value < limit ) {
                        if( isPreloaded ) {
                            CacheCapacity = Math.Min( CacheCapacity, value );
                        }
                        TrimFile( value );
                    }
                    limit = value;
                }
            }
        }


        TimeSpan timeLimit;
        public TimeSpan TimeLimit {
            get { return timeLimit; }
            set {
                if( value < TimeSpan.Zero ) throw new ArgumentOutOfRangeException();
                lock( SyncRoot ) {
                    if( value != TimeSpan.Zero && value < timeLimit ) {
                        int newCapacity = CountNewerEntries( value );
                        if( isPreloaded ) {
                            CacheCapacity = Math.Min( CacheCapacity, newCapacity );
                        }
                        TrimFile( newCapacity );
                    }
                    timeLimit = value;
                }
            }
        }


        public string FileName {
            get {
                return Path.Combine( Paths.BlockDBPath, World.Name + ".fbdb" );
            }
        }


        internal void AddEntry( BlockDBEntry newEntry ) {
            lock( SyncRoot ) {
                if( IsPreloaded ) {
                    cache.Add( newEntry );
                } else {
                    pendingChanges.Add( newEntry );
                }
            }
        }


        internal void Clear() {
            lock( SyncRoot ) {
                if( IsPreloaded ) {
                    cache.Clear();
                } else {
                    pendingChanges.Clear();
                }
                File.Delete( FileName );
            }
        }


        internal void Flush() {
            lock( SyncRoot ) {
                if( IsPreloaded && pendingChanges.Count > 0 ) {
                    using( var stream = File.Open( FileName, FileMode.Append, FileAccess.Write ) ) {
                        BinaryWriter writer = new BinaryWriter( stream );
                        for( int i = 0; i < pendingChanges.Count; i++ ) {
                            pendingChanges[i].Serialize( writer );
                        }
                    }
                    pendingChanges.Clear();

                } else if( !IsPreloaded && lastFlushedIndex < cache.Count ) {
                    using( var stream = File.Open( FileName, FileMode.Append, FileAccess.Write ) ) {
                        BinaryWriter writer = new BinaryWriter( stream );
                        for( int i = lastFlushedIndex; i < cache.Count; i++ ) {
                            pendingChanges[i].Serialize( writer );
                        }
                    }
                    lastFlushedIndex = cache.Count;
                }
            }
        }


        unsafe internal byte[] Load() {
            lock( SyncRoot ) {
                Flush();
                if( File.Exists( FileName ) ) {
                    return File.ReadAllBytes( FileName );
                } else {
                    return new byte[0];
                }
            }
        }


        unsafe internal BlockDBEntry[] Lookup( short x, short y, short z ) {
            byte[] bytes = Load();

            List<BlockDBEntry> results = new List<BlockDBEntry>();
            int entryCount = bytes.Length / BlockDB.BlockDBEntrySize;
            fixed( byte* parr = bytes ) {
                BlockDBEntry* entries = (BlockDBEntry*)parr;
                for( int i = 0; i < entryCount; i++ ) {
                    if( entries[i].X == x && entries[i].Y == y && entries[i].Z == z ) {
                        results.Add( entries[i] );
                    }
                }
            }
            return results.ToArray();
        }


        unsafe internal BlockDBEntry[] Lookup( PlayerInfo info, int max ) {
            byte[] bytes = Load();

            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();
            int count = 0;
            int entryCount = bytes.Length / BlockDB.BlockDBEntrySize;
            fixed( byte* parr = bytes ) {
                BlockDBEntry* entries = (BlockDBEntry*)parr;
                for( int i = entryCount - 1; i >= 0; i-- ) {
                    if( entries[i].PlayerID == info.ID ) {
                        int index = World.Map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                        if( !results.ContainsKey( index ) ) {
                            results[index] = entries[i];
                            count++;
                            if( count >= max ) break;
                        }
                    }
                }
            }
            return results.Values.ToArray();
        }


        unsafe internal BlockDBEntry[] Lookup( PlayerInfo info, TimeSpan span ) {
            byte[] bytes = Load();

            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();

            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();

            int entryCount = bytes.Length / BlockDB.BlockDBEntrySize;
            fixed( byte* parr = bytes ) {
                BlockDBEntry* entries = (BlockDBEntry*)parr;
                for( int i = entryCount - 1; i >= 0; i-- ) {
                    if( entries[i].Timestamp < ticks ) break;
                    if( entries[i].PlayerID == info.ID ) {
                        int index = World.Map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                        if( !results.ContainsKey( index ) ) {
                            results[index] = entries[i];
                        }
                    }
                }
            }
            return results.Values.ToArray();
        }


        #region Static

        public const int BlockDBEntrySize = 16;
        public static bool IsEnabled { get; private set; }
        static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds( 90 );

        internal static void Init() {
            Paths.TestDirectory( "BlockDB", Paths.BlockDBPath, true );
            Server.PlayerPlacedBlock += OnPlayerPlacedBlock;
            Scheduler.NewBackgroundTask( FlushAll ).RunForever( FlushInterval, FlushInterval );
            IsEnabled = true;
        }


        static void OnPlayerPlacedBlock( object sender, PlayerPlacedBlockEventArgs e ) {
            World world = e.Player.World;
            if( world.BlockDB.Enabled ) {
                BlockDBEntry newEntry = new BlockDBEntry( (int)DateTime.UtcNow.ToUnixTime(),
                                                          e.Player.Info.ID,
                                                          e.X, e.Y, e.Z,
                                                          e.OldBlock,
                                                          e.NewBlock );
                world.BlockDB.AddEntry( newEntry );
            }
        }


        static void FlushAll( SchedulerTask task ) {
            lock( WorldManager.WorldListLock ) {
                foreach( World w in WorldManager.WorldList.Where( w => w.BlockDB.Enabled ) ) {
                    w.BlockDB.Flush();
                }
            }
        }

        #endregion
    }
}