﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using fCraft.Events;
using System.IO;
using System.Collections.Generic;

namespace fCraft {
    public unsafe sealed class BlockDB {

        public BlockDB( World world ) {
            World = world;
        }


        internal readonly object SyncRoot = new object();

        public World World { get; set; }


        bool enabled;
        public bool Enabled {
            get { return enabled; }
            set {
                if( value == enabled ) return;
                if( value && isPreloaded ) {
                    Preload();
                } else if( value == false ) {
                    Flush();
                    CacheClear();
                }
                Logger.Log( "BlockDB({0}): Enabled={1}", LogType.Debug, World.Name, value );
                enabled = value;
            }
        }


        public string FileName {
            get {
                return Path.Combine( Paths.BlockDBPath, World.Name + ".fbdb" );
            }
        }


        #region Cache

        const int BufferSize = 64 * 1024; // 64 KB
        readonly byte[] writeBuffer = new byte[BufferSize];

        BlockDBEntry[] cacheStore = new BlockDBEntry[MinCacheSize];
        internal int cacheSize;
        const int MinCacheSize = 2 * 1024, // 32 KB
                  CacheLinearResizeThreshold = 64 * 1024; // 1 MB

        void CacheAdd( BlockDBEntry item ) {
            if( cacheSize == cacheStore.Length ) {
                EnsureCapacity( cacheSize + 1 );
            }
            cacheStore[cacheSize++] = item;
        }


        void CacheClear() {
            cacheSize = 0;
            cacheStore = new BlockDBEntry[MinCacheSize];
            lastFlushedIndex = 0;
        }


        void EnsureCapacity( int min ) {
            if( cacheStore.Length < min ) {
                int num = cacheStore.Length;
                while( num < min ) {
                    if( num <= CacheLinearResizeThreshold ) {
                        num *= 2;
                    } else {
                        num += CacheLinearResizeThreshold;
                    }
                }
                CacheCapacity = num;
            }
        }


        void LimitCapacity( int max ) {
            if( cacheStore.Length > max ) {
                int num = max;
                if( num < MinCacheSize ) {
                    num = MinCacheSize;
                } else if( num < CacheLinearResizeThreshold ) {
                    num = 1 << (int)(1 + Math.Floor( Math.Log( num, 2 ) ));
                } else {
                    num = (num / CacheLinearResizeThreshold + 1) * CacheLinearResizeThreshold;
                }
                CacheCapacity = num;
                if( max < cacheSize ) {
                    Array.Copy( cacheStore, cacheSize - max, cacheStore, 0, max );
                    cacheSize = max;
                }
            }
        }


        internal int CacheCapacity {
            get {
                return cacheStore.Length;
            }
            set {
                if( value < MinCacheSize ) throw new ArgumentOutOfRangeException();
                if( value != cacheStore.Length ) {
                    BlockDBEntry[] destinationArray = new BlockDBEntry[value];
                    if( value < cacheSize ) {
                        Array.Copy( cacheStore, cacheSize - value, destinationArray, 0, value );
                    } else {
                        Array.Copy( cacheStore, 0, destinationArray, 0, Math.Min(cacheStore.Length,cacheSize) );
                    }
                    cacheStore = destinationArray;
                    Logger.Log( "BlockDB({0}): CacheCapacity={1}", LogType.Debug, World.Name, value );
                }
            }
        }

        #endregion


        #region Preload


        bool isPreloaded;
        public bool IsPreloaded {
            get {
                return isPreloaded;
            }
            set {
                lock( SyncRoot ) {
                    if( value == isPreloaded ) return;
                    Flush();
                    if( value && File.Exists( FileName ) ) {
                        Preload();
                    } else if( value == false ) {
                        CacheClear();
                    }
                    Logger.Log( "BlockDB({0}): Preloaded={1}", LogType.Debug, World.Name, value );
                    isPreloaded = value;
                }
            }
        }


        void Preload() {
            using( FileStream fs = OpenRead() ) {

                cacheSize = (int)(fs.Length / BlockDBEntrySize);
                EnsureCapacity( cacheSize );
                lastFlushedIndex = cacheSize;

                fixed( BlockDBEntry* pCache = cacheStore ) {
                    fixed( byte* pBuffer = writeBuffer ) {
                        while( fs.Position < fs.Length ) {
                            int bytesToRead = Math.Min( BufferSize, (int)(fs.Length - fs.Position) );
                            int bytesInBuffer = 0;
                            do {
                                int bytesRead = fs.Read( writeBuffer, bytesInBuffer, BufferSize - bytesInBuffer );
                                bytesInBuffer += bytesRead;
                            } while( bytesInBuffer < bytesToRead );
                            BufferUtil.MemCpy( pBuffer, (byte*)pCache, bytesInBuffer );
                        }
                    }
                }
            }
        }

        #endregion


        #region Limiting

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
                    while( true ) {
                        int bytesRead = source.Read( buffer, 0, buffer.Length );
                        if( bytesRead == 0 ) break;
                        destination.Write( buffer, 0, bytesRead );
                    }
                }
            }
            Paths.MoveOrReplace( tempFileName, FileName );
        }


        int CountNewerEntries( TimeSpan age ) {
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
                int entryCount = bytes.Length / BlockDBEntrySize;
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


        internal int lastFlushedIndex;


        int limit;
        public int Limit {
            get { return limit; }
            set {
                if( value < 0 ) throw new ArgumentOutOfRangeException();
                lock( SyncRoot ) {
                    int oldLimit = limit;
                    limit = value;
                    if( oldLimit == 0 && value != 0 ||
                        oldLimit != 0 && value < oldLimit ) {
                        EnforceLimit();
                    }
                    Logger.Log( "BlockDB({0}): Limit={1}", LogType.Debug, World.Name, value );
                }
            }
        }


        TimeSpan timeLimit;
        public TimeSpan TimeLimit {
            get { return timeLimit; }
            set {
                if( value < TimeSpan.Zero ) throw new ArgumentOutOfRangeException();
                lock( SyncRoot ) {
                    TimeSpan oldTimeLimit = timeLimit;
                    timeLimit = value;
                    if( oldTimeLimit == TimeSpan.Zero && value != TimeSpan.Zero ||
                        oldTimeLimit != TimeSpan.Zero && value < oldTimeLimit ) {
                        EnforceTimeLimit();
                    }
                    Logger.Log( "BlockDB({0}): TimeLimit={1}", LogType.Debug, World.Name, value );
                }
            }
        }


        void EnforceLimit() {
            if( limit != 0 ) {
                if( isPreloaded ) {
                    LimitCapacity( limit );
                }
                TrimFile( limit );
                lastLimit = DateTime.UtcNow;
            }
        }


        void EnforceTimeLimit() {
            if( timeLimit != TimeSpan.Zero ) {
                if( timeLimit < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( "newLimit" );
                int newCapacity = CountNewerEntries( timeLimit );
                if( isPreloaded ) {
                    LimitCapacity( newCapacity );
                }
                TrimFile( newCapacity );
                lastTimeLimit = DateTime.UtcNow;
            }
        }

        #endregion


        internal void AddEntry( BlockDBEntry newEntry ) {
            lock( SyncRoot ) {
                CacheAdd( newEntry );
            }
        }


        internal void Clear() {
            lock( SyncRoot ) {
                CacheClear();
                File.Delete( FileName );
            }
        }


        DateTime lastLimit, lastTimeLimit;
        static readonly TimeSpan minLimitDelay = TimeSpan.FromMinutes( 5 ),
                                 minTimeLimitDelay = TimeSpan.FromMinutes( 10 );
        internal void Flush() {
            lock( SyncRoot ) {
                if( lastFlushedIndex < cacheSize ) {
                    Logger.Log( "BlockDB({0}): Flushing. CC={1} CS={2} LFI={3}", LogType.Debug,
                                World.Name, CacheCapacity, cacheSize, lastFlushedIndex );
                    int count = 0;
                    using( FileStream stream = OpenAppend() ) {
                        BinaryWriter writer = new BinaryWriter( stream );
                        for( int i = lastFlushedIndex; i < cacheSize; i++ ) {
                            cacheStore[i].Serialize( writer );
                            count++;
                        }
                    }
                    if( !isPreloaded ) cacheSize = 0;
                    lastFlushedIndex = cacheSize;
                    Logger.Log( "BlockDB({0}): Flushed {1} entries. CC={2} CS={3} LFI={4}", LogType.Debug,
                                World.Name, count, CacheCapacity, cacheSize, lastFlushedIndex );


                    if( limit > 0 ) {
                        bool limitingAllowed = DateTime.UtcNow.Subtract( lastLimit ) > minLimitDelay ||
                                               (cacheSize - limit) > CacheLinearResizeThreshold;
                        if( cacheSize > limit * 1.1 && limitingAllowed ) {
                            EnforceLimit();
                            LimitCapacity( cacheSize );
                        }
                    }

                    if( timeLimit > TimeSpan.Zero ) {
                        if( DateTime.UtcNow.Subtract( lastTimeLimit ) > minTimeLimitDelay ) {
                            EnforceTimeLimit();
                            LimitCapacity( cacheSize );
                        }
                    }
                }
            }
        }


        FileStream OpenRead() {
            return new FileStream( FileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize );
        }


        FileStream OpenAppend() {
            return new FileStream( FileName, FileMode.Append, FileAccess.Write, FileShare.None, BufferSize );
        }


        internal byte[] Load() {
            lock( SyncRoot ) {
                Flush();
                if( File.Exists( FileName ) ) {
                    return File.ReadAllBytes( FileName );
                } else {
                    return new byte[0];
                }
            }
        }


        internal BlockDBEntry[] Lookup( short x, short y, short z ) {
            List<BlockDBEntry> results = new List<BlockDBEntry>();

            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = 0; i < cacheSize; i++ ) {
                            if( entries[i].X == x && entries[i].Y == y && entries[i].Z == z ) {
                                results.Add( entries[i] );
                            }
                        }
                    }
                }
            } else {
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntrySize;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = 0; i < entryCount; i++ ) {
                        if( entries[i].X == x && entries[i].Y == y && entries[i].Z == z ) {
                            results.Add( entries[i] );
                        }
                    }
                }
            }

            return results.ToArray();
        }


        internal BlockDBEntry[] Lookup( PlayerInfo info, int max ) {
            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();
            int count = 0;

            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = cacheSize - 1; i >= 0; i-- ) {
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
                }
            } else {
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntrySize;
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
            }
            return results.Values.ToArray();
        }


        internal BlockDBEntry[] Lookup( PlayerInfo info, TimeSpan span ) {
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();

            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = cacheSize - 1; i >= 0; i-- ) {
                            if( entries[i].Timestamp < ticks ) break;
                            if( entries[i].PlayerID == info.ID ) {
                                int index = World.Map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                                if( !results.ContainsKey( index ) ) {
                                    results[index] = entries[i];
                                }
                            }
                        }
                    }
                }
            } else {
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntrySize;
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