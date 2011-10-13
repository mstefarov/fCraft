// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    public static class PlayerDB {
        static readonly Trie<PlayerInfo> Trie = new Trie<PlayerInfo>();
        static List<PlayerInfo> list = new List<PlayerInfo>();

        /// <summary> Cached list of all players in the database.
        /// May be quite long. Make sure to copy a reference to
        /// the list before accessing it in a loop, since this 
        /// array be frequently be replaced by an updated one. </summary>
        public static PlayerInfo[] PlayerInfoList { get; private set; }

        static int maxID = 255;
        const int BufferSize = 64 * 1024;

        /* 
         * Version 0 - before 0.530 - all dates/times are local
         * Version 1 - 0.530-0.536 - all dates and times are stored as UTC unix timestamps (milliseconds)
         * Version 2 - 0.600 dev - all dates and times are stored as UTC unix timestamps (seconds)
         * Version 3 - 0.600 dev - same as v2, but sorting by ID is enforced
         * Version 4 - 0.600 dev - added LastModified column, forced banned players to be unfrozen/unmuted/unhidden.
         * Version 5 - 0.600+ - removed FailedLoginCount column
         */
        public const int FormatVersion = 5;

        const string Header = "fCraft PlayerDB | Row format: " +
                              "Name,IPAddress,Rank,RankChangeDate,RankChangedBy,Banned,BanDate,BannedBy," +
                              "UnbanDate,UnbannedBy,BanReason,UnbanReason,LastFailedLoginDate," +
                              "LastFailedLoginIP,UNUSED,FirstLoginDate,LastLoginDate,TotalTime," +
                              "BlocksBuilt,BlocksDeleted,TimesVisited,LinesWritten,UNUSED,UNUSED," +
                              "PreviousRank,RankChangeReason,TimesKicked,TimesKickedOthers," +
                              "TimesBannedOthers,ID,RankChangeType,LastKickDate,LastSeen,BlocksDrawn," +
                              "LastKickBy,LastKickReason,IsFrozen,FrozenBy,FrozenOn, MutedUntil,MutedBy," +
                              "Password,Online,BandwidthUseMode,LastModified";


        // used to ensure PlayerDB consistency when adding/removing PlayerDB entries
        static readonly object AddLocker = new object();

        // used to prevent concurrent access to the PlayerDB file
        static readonly object SaveLoadLocker = new object();


        public static bool IsLoaded { get; private set; }


        public static PlayerInfo AddFakeEntry( [NotNull] string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            PlayerInfo info;
            lock( AddLocker ) {
                info = Trie.Get( name );
                if( info != null ) {
                    throw new ArgumentException( "A PlayerDB entry already exists for this name.", "name" );
                }

                var e = new PlayerInfoCreatingEventArgs( name, IPAddress.None, RankManager.DefaultRank, true );
                PlayerInfo.RaiseCreatingEvent( e );
                if( e.Cancel ) {
                    throw new OperationCanceledException( "Cancelled by a plugin." );
                }

                info = new PlayerInfo( name, e.StartingRank, false, rankChangeType );

                list.Add( info );
                Trie.Add( info.Name, info );
                UpdateCache();
            }
            PlayerInfo.RaiseCreatedEvent( info, false );
            return info;
        }


        #region Saving/Loading

        internal static void Load() {
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName ) ) {
                    Stopwatch sw = Stopwatch.StartNew();
                    using( FileStream fs = new FileStream( Paths.PlayerDBFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize ) ) {
                        using( StreamReader reader = new StreamReader( fs, Encoding.UTF8, true, BufferSize ) ) {

                            string header = reader.ReadLine();

                            if( header == null ) return; // if PlayerDB is an empty file

                            lock( AddLocker ) {
                                int version = IdentifyFormatVersion( header );
                                if( version > FormatVersion ) {
                                    Logger.Log( "PlayerDB.Load: Attempting to load unsupported PlayerDB format ({0}). Errors may occur.",
                                                LogType.Warning,
                                                version );
                                } else if( version < FormatVersion ) {
                                    Logger.Log( "PlayerDB.Load: Converting PlayerDB to a newer format (version {0} to {1}).",
                                                LogType.Warning,
                                                version, FormatVersion );
                                }

                                int emptyRecords = 0;
                                while( true ) {
                                    string line = reader.ReadLine();
                                    if( line == null ) break;
                                    string[] fields = line.Split( ',' );
                                    if( fields.Length >= PlayerInfo.MinFieldCount ) {
#if !DEBUG
                                        try {
#endif
                                            PlayerInfo info;
                                            switch( version ) {
                                                case 0:
                                                    info = PlayerInfo.LoadFormat0( fields, true );
                                                    break;
                                                case 1:
                                                    info = PlayerInfo.LoadFormat1( fields );
                                                    break;
                                                default:
                                                    // Versions 2-5 differ in semantics only, not in actual serialization format.
                                                    info = PlayerInfo.LoadFormat2( fields );
                                                    break;
                                            }

                                            if( info.ID > maxID ) {
                                                maxID = info.ID;
                                                Logger.Log( "PlayerDB.Load: Adjusting wrongly saved MaxID ({0} to {1}).", LogType.Warning );
                                            }

                                            // A record is considered "empty" if the player has never logged in.
                                            // Empty records may be created by /import, /ban, and /rank commands on typos.
                                            // Deleting such records should have no negative impact on DB completeness.
                                            if( (info.LastIP.Equals( IPAddress.None ) || info.TimesVisited == 0) &&
                                                !info.IsBanned && info.Rank == RankManager.DefaultRank ) {

                                                Logger.Log( "PlayerDB.Load: Skipping an empty record for player \"{0}\"", LogType.SystemActivity,
                                                            info.Name );
                                                emptyRecords++;
                                                continue;
                                            }

                                            // Check for duplicates. Unless PlayerDB.txt was altered externally, this does not happen.
                                            if( Trie.ContainsKey( info.Name ) ) {
                                                Logger.Log( "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.", LogType.Error,
                                                            info.Name );
                                            } else {
                                                Trie.Add( info.Name, info );
                                                list.Add( info );
                                            }
#if !DEBUG
                                        } catch( Exception ex ) {
                                            Logger.LogAndReportCrash( "Error while parsing PlayerInfo record",
                                                                      "fCraft",
                                                                      ex,
                                                                      false );
                                        }
#endif
                                    } else {
                                        Logger.Log( "PlayerDB.Load: Unexpected field count ({0}), expecting at least {1} fields for a PlayerDB entry.",
                                                    LogType.Error,
                                                    fields.Length, PlayerInfo.MinFieldCount );
                                    }
                                }

                                if( emptyRecords > 0 ) {
                                    Logger.Log( "PlayerDB.Load: Skipped {0} empty records.", LogType.Warning, emptyRecords );
                                }

                                RunCompatibilityChecks( version );
                            }
                        }
                    }
                    sw.Stop();
                    Logger.Log( "PlayerDB.Load: Done loading player DB ({0} records) in {1}ms. MaxID={2}", LogType.Debug,
                                Trie.Count, sw.ElapsedMilliseconds, maxID );
                } else {
                    Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
                }
                UpdateCache();
                IsLoaded = true;
            }
        }


        static void RunCompatibilityChecks( int loadedVersion ) {
            // Sorting the list allows finding players by ID using binary search.
            // Normally this only needs to be done when 
            Logger.Log( "Sorting PlayerDB by ID...", LogType.SystemActivity );
            list.Sort( PlayerIDComparer.Instance );

            if( loadedVersion < 4 ) {
                int unhid = 0, unfroze = 0, unmuted = 0;
                Logger.Log( "PlayerDB: Checking consistency of banned player records...", LogType.SystemActivity );
                for( int i = 0; i < list.Count; i++ ) {
                    if( list[i].IsBanned ) {
                        if( list[i].IsHidden ) {
                            unhid++;
                            list[i].IsHidden = false;
                        }

                        if( list[i].IsFrozen ) {
                            list[i].Unfreeze();
                            unfroze++;
                        }

                        if( list[i].IsMuted ) {
                            list[i].Unmute();
                            unmuted++;
                        }
                    }
                }
                Logger.Log( "PlayerDB: Unhid {0}, unfroze {1}, and unmuted {2} banned accounts.", LogType.SystemActivity,
                            unhid, unfroze, unmuted );
            }
        }


        static int IdentifyFormatVersion( [NotNull] string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            string[] headerParts = header.Split( ' ' );
            if( headerParts.Length < 2 ) {
                throw new FormatException( "Invalid PlayerDB header format: " + header );
            }
            int maxIDField;
            if( Int32.TryParse( headerParts[0], out maxIDField ) ) {
                if( maxIDField >= 255 ) {// IDs start at 256
                    maxID = maxIDField;
                }
            }
            int version;
            if( Int32.TryParse( headerParts[1], out version ) ) {
                return version;
            } else {
                return 0;
            }
        }


        public static void Save() {
            const string tempFileName = Paths.PlayerDBFileName + ".temp";

            lock( SaveLoadLocker ) {
                PlayerInfo[] listCopy = PlayerInfoList;
                Stopwatch sw = Stopwatch.StartNew();
                using( FileStream fs = new FileStream( tempFileName, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize ) ) {
                    using( StreamWriter writer = new StreamWriter( fs, Encoding.UTF8, BufferSize ) ) {
                        writer.WriteLine( "{0} {1} {2}", maxID, FormatVersion, Header );

                        StringBuilder sb = new StringBuilder();
                        for( int i = 0; i < listCopy.Length; i++ ) {
                            listCopy[i].Serialize( sb );
                            writer.WriteLine( sb.ToString() );
                            sb.Length = 0;
                        }
                    }
                }
                sw.Stop();
                Logger.Log( "PlayerDB.Save: Saved player database ({0} records) in {1}ms", LogType.Debug,
                            Trie.Count, sw.ElapsedMilliseconds );

                try {
                    Paths.MoveOrReplace( tempFileName, Paths.PlayerDBFileName );
                } catch( Exception ex ) {
                    Logger.Log( "PlayerDB.Save: An error occured while trying to save PlayerDB: " + ex, LogType.Error );
                }
            }
        }

        #endregion


        #region Scheduled Saving

        static SchedulerTask saveTask;
        static TimeSpan saveInterval = TimeSpan.FromSeconds( 90 );
        public static TimeSpan SaveInterval {
            get { return saveInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException();
                saveInterval = value;
                if( saveTask != null ) saveTask.Interval = value;
            }
        }

        internal static void StartSaveTask() {
            saveTask = Scheduler.NewBackgroundTask( delegate { Save(); } )
                                .RunForever( SaveInterval, SaveInterval + TimeSpan.FromSeconds( 15 ) );
        }

        #endregion


        #region Lookup

        public static PlayerInfo FindOrCreateInfoForPlayer( [NotNull] string name, [NotNull] IPAddress lastIP ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            PlayerInfo info;

            // this flag is used to avoid executing PlayerInfoCreated event in the lock
            bool raiseCreatedEvent = false;

            lock( AddLocker ) {
                info = Trie.Get( name );
                if( info == null ) {
                    var e = new PlayerInfoCreatingEventArgs( name, lastIP, RankManager.DefaultRank, false );
                    PlayerInfo.RaiseCreatingEvent( e );
                    if( e.Cancel ) throw new OperationCanceledException( "Cancelled by a plugin." );

                    info = new PlayerInfo( name, lastIP, e.StartingRank );
                    Trie.Add( name, info );
                    list.Add( info );
                    UpdateCache();

                    raiseCreatedEvent = true;
                }
            }

            if( raiseCreatedEvent ) {
                PlayerInfo.RaiseCreatedEvent( info, false );
            }
            return info;
        }


        public static PlayerInfo[] FindPlayers( [NotNull] IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            return FindPlayers( address, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( cache[i].LastIP.Equals( address ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) return result.ToArray();
                }
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( [NotNull] Regex regex ) {
            if( regex == null ) throw new ArgumentNullException( "regex" );
            return FindPlayers( regex, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( [NotNull] Regex regex, int limit ) {
            if( regex == null ) throw new ArgumentNullException( "regex" );
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( regex.IsMatch( cache[i].Name ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) break;
                }
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( [NotNull] string namePart ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            return FindPlayers( namePart, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( [NotNull] string namePart, int limit ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            lock( AddLocker ) {
                //return Trie.ValuesStartingWith( namePart ).Take( limit ).ToArray(); // <- works, but is slightly slower
                return Trie.GetList( namePart, limit ).ToArray();
            }
        }


        /// <summary>Searches for player names starting with namePart, returning just one or none of the matches.</summary>
        /// <param name="name">Partial or full player name</param>
        /// <param name="info">PlayerInfo to output (will be set to null if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        public static bool FindPlayerInfo( [NotNull] string name, out PlayerInfo info ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( AddLocker ) {
                return Trie.GetOneMatch( name, out info );
            }
        }


        public static PlayerInfo FindPlayerInfoExact( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( AddLocker ) {
                return Trie.Get( name );
            }
        }

        [CanBeNull]
        public static PlayerInfo FindPlayerInfoOrPrintMatches( [NotNull] Player player, [NotNull] string name ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( name == null ) throw new ArgumentNullException( "name" );
            PlayerInfo target = FindPlayerInfoExact( name );
            if( target == null ) {
                PlayerInfo[] targets = FindPlayers( name );
                if( targets.Length == 0 ) {
                    player.MessageNoPlayer( name );
                    return null;

                } else if( targets.Length > 1 ) {
                    Array.Sort( targets, new PlayerInfoComparer( player ) );
                    player.MessageManyMatches( "player", targets.Take( 25 ).ToArray() );
                    return null;
                }
                target = targets[0];
            }
            return target;
        }

        public static string FindExactClassyName( [NotNull] string name ) {
            PlayerInfo info = FindPlayerInfoExact( name );
            if( info == null ) return name;
            else return info.ClassyName;
        }

        #endregion


        #region Stats

        public static int BannedCount {
            get {
                return PlayerInfoList.Count( t => t.IsBanned );
            }
        }


        public static float BannedPercentage {
            get {
                var listCache = PlayerInfoList;
                if( listCache.Length == 0 ) {
                    return 0;
                } else {
                    return listCache.Count( t => t.IsBanned ) * 100f / listCache.Length;
                }
            }
        }


        public static int Size {
            get {
                return Trie.Count;
            }
        }

        #endregion


        public static int GetNextID() {
            return Interlocked.Increment( ref maxID );
        }


        /// <summary> Finds PlayerInfo by ID. Returns null of not found. </summary>
        [CanBeNull]
        public static PlayerInfo FindPlayerInfoByID( int id ) {
            PlayerInfo dummy = new PlayerInfo( id );
            lock( AddLocker ) {
                int index = list.BinarySearch( dummy, PlayerIDComparer.Instance );
                if( index >= 0 ) {
                    return list[index];
                } else {
                    return null;
                }
            }
        }


        public static int MassRankChange( [NotNull] Player player, [NotNull] Rank from, [NotNull] Rank to, [NotNull] string reason ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            if( reason == null ) throw new ArgumentNullException( "reason" );
            int affected = 0;
            string fullReason = reason + "~MassRank";
            lock( AddLocker ) {
                for( int i = 0; i < PlayerInfoList.Length; i++ ) {
                    if( PlayerInfoList[i].Rank == from ) {
                        try {
                            list[i].ChangeRank( player, to, fullReason, true, true, false );
                        } catch( PlayerOpException ex ) {
                            player.Message( ex.MessageColored );
                        }
                        affected++;
                    }
                }
                return affected;
            }
        }


        static void UpdateCache() {
            lock( AddLocker ) {
                PlayerInfoList = list.ToArray();
            }
        }


        #region Experimental & Debug things

        internal static int CountInactivePlayers() {
            lock( AddLocker ) {
                Dictionary<IPAddress, List<PlayerInfo>> playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }

                int count = 0;
                // ReSharper disable LoopCanBeConvertedToQuery
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    // ReSharper restore LoopCanBeConvertedToQuery
                    if( PlayerIsInactive( playersByIP, playerInfoListCache[i], true ) ) count++;
                }
                return count;
            }
        }


        internal static int RemoveInactivePlayers() {
            int count = 0;
            lock( AddLocker ) {
                Dictionary<IPAddress, List<PlayerInfo>> playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }
                List<PlayerInfo> newList = new List<PlayerInfo>();
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    PlayerInfo p = playerInfoListCache[i];
                    if( PlayerIsInactive( playersByIP, p, true ) ) {
                        count++;
                    } else {
                        newList.Add( p );
                    }
                }

                list = newList;
                Trie.Clear();
                foreach( PlayerInfo p in list ) {
                    Trie.Add( p.Name, p );
                }

                list.TrimExcess();
                UpdateCache();
            }
            return count;
        }


        static bool PlayerIsInactive( [NotNull] IDictionary<IPAddress, List<PlayerInfo>> playersByIP, [NotNull] PlayerInfo player, bool checkIP ) {
            if( playersByIP == null ) throw new ArgumentNullException( "playersByIP" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.IsBanned || !String.IsNullOrEmpty( player.UnbannedBy ) ||
                player.IsFrozen || player.IsMuted || player.TimesKicked != 0 ||
                player.Rank != RankManager.DefaultRank || player.PreviousRank != null ) {
                return false;
            }
            if( player.TotalTime.TotalMinutes > 30 || player.TimeSinceLastSeen.TotalDays < 30 ) {
                return false;
            }
            if( IPBanList.Get( player.LastIP ) != null ) {
                return false;
            }
            if( checkIP ) {
                return playersByIP[player.LastIP].All( other => (other == player) || PlayerIsInactive( playersByIP, other, false ) );
            }
            return true;
        }


        internal static void SwapPlayerInfo( PlayerInfo p1, PlayerInfo p2 ) {
            lock( AddLocker ) {
                lock( SaveLoadLocker ) {
                    if( p1.IsOnline || p2.IsOnline ) {
                        throw new Exception( "Both players must be offline to swap info." );
                    }
                    Swap( ref p1.BanDate, ref p2.BanDate );
                    Swap( ref p1.BandwidthUseMode, ref p2.BandwidthUseMode );
                    Swap( ref p1.BanStatus, ref p2.BanStatus );
                    Swap( ref p1.BannedBy, ref p2.BannedBy );
                    Swap( ref p1.BannedUntil, ref p2.BannedUntil );
                    Swap( ref p1.BanReason, ref p2.BanReason );
                    Swap( ref p1.BlocksBuilt, ref p2.BlocksBuilt );
                    Swap( ref p1.BlocksDeleted, ref p2.BlocksDeleted );
                    Swap( ref p1.BlocksDrawn, ref p2.BlocksDrawn );
                    Swap( ref p1.DisplayedName, ref p2.DisplayedName );
                    Swap( ref p1.FirstLoginDate, ref p2.FirstLoginDate );
                    Swap( ref p1.FrozenBy, ref p2.FrozenBy );
                    Swap( ref p1.FrozenOn, ref p2.FrozenOn );
                    Swap( ref p1.ID, ref p2.ID );
                    Swap( ref p1.IsFrozen, ref p2.IsFrozen );
                    //Swap( ref p1.IsHidden, ref p2.IsHidden );
                    Swap( ref p1.LastFailedLoginDate, ref p2.LastFailedLoginDate );
                    Swap( ref p1.LastFailedLoginIP, ref p2.LastFailedLoginIP );
                    //Swap( ref p1.LastIP, ref p2.LastIP );
                    Swap( ref p1.LastKickBy, ref p2.LastKickBy );
                    Swap( ref p1.LastKickDate, ref p2.LastKickDate );
                    Swap( ref p1.LastKickReason, ref p2.LastKickReason );
                    //Swap( ref p1.LastLoginDate, ref p2.LastLoginDate );
                    //Swap( ref p1.LastSeen, ref p2.LastSeen );
                    //Swap( ref p1.LeaveReason, ref p2.LeaveReason );
                    Swap( ref p1.MessagesWritten, ref p2.MessagesWritten );
                    Swap( ref p1.MutedBy, ref p2.MutedBy );
                    Swap( ref p1.MutedUntil, ref p2.MutedUntil );
                    //Swap( ref p1.Name, ref p2.Name );
                    //Swap( ref p1.Online, ref p2.Online );
                    Swap( ref p1.Password, ref p2.Password );
                    //Swap( ref p1.PlayerObject, ref p2.PlayerObject );
                    Swap( ref p1.PreviousRank, ref p2.PreviousRank );

                    Rank p1Rank = p1.Rank;
                    p1.Rank = p2.Rank;
                    p2.Rank = p1Rank;

                    Swap( ref p1.RankChangeDate, ref p2.RankChangeDate );
                    Swap( ref p1.RankChangedBy, ref p2.RankChangedBy );
                    Swap( ref p1.RankChangeReason, ref p2.RankChangeReason );
                    Swap( ref p1.RankChangeType, ref p2.RankChangeType );
                    Swap( ref p1.TimesBannedOthers, ref p2.TimesBannedOthers );
                    Swap( ref p1.TimesKicked, ref p2.TimesKicked );
                    Swap( ref p1.TimesKickedOthers, ref p2.TimesKickedOthers );
                    Swap( ref p1.TimesVisited, ref p2.TimesVisited );
                    Swap( ref p1.TotalTime, ref p2.TotalTime );
                    Swap( ref p1.UnbanDate, ref p2.UnbanDate );
                    Swap( ref p1.UnbannedBy, ref p2.UnbannedBy );
                    Swap( ref p1.UnbanReason, ref p2.UnbanReason );

                    list.Sort( PlayerIDComparer.Instance );
                }
            }
        }


        static void Swap<T>( ref T t1, ref T t2 ) {
            var temp = t2;
            t2 = t1;
            t1 = temp;
        }

        #endregion


        sealed class PlayerIDComparer : IComparer<PlayerInfo> {
            public static readonly PlayerIDComparer Instance = new PlayerIDComparer();
            private PlayerIDComparer() { }

            public int Compare( PlayerInfo x, PlayerInfo y ) {
                return x.ID - y.ID;
            }
        }


        public static StringBuilder AppendEscaped( this StringBuilder sb, string str ) {
            if( !String.IsNullOrEmpty( str ) ) {
                if( str.IndexOf( ',' ) > -1 ) {
                    int startIndex = sb.Length;
                    sb.Append( str );
                    sb.Replace( ',', '\xFF', startIndex, str.Length );
                } else {
                    sb.Append( str );
                }
            }
            return sb;
        }
    }
}