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

    /// <summary> Persistent database of player information. </summary>
    public static class PlayerDB {
        static readonly Trie<PlayerInfo> Trie = new Trie<PlayerInfo>();
        static List<PlayerInfo> list = new List<PlayerInfo>();

        public static string ProviderType { get; internal set; }

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
                              "BlocksBuilt,BlocksDeleted,TimesVisited,MessagesWritten,UNUSED,UNUSED," +
                              "PreviousRank,RankChangeReason,TimesKicked,TimesKickedOthers," +
                              "TimesBannedOthers,ID,RankChangeType,LastKickDate,LastSeen,BlocksDrawn," +
                              "LastKickBy,LastKickReason,BannedUntil,IsFrozen,FrozenBy,FrozenOn,MutedUntil,MutedBy," +
                              "Password,IsOnline,BandwidthUseMode,IsHidden,LastModified,DisplayedName";


        // used to ensure PlayerDB consistency when adding/removing PlayerDB entries
        static readonly object AddLocker = new object();

        // used to prevent concurrent access to the PlayerDB file
        static readonly object SaveLoadLocker = new object();


        public static bool IsLoaded { get; private set; }


        static void CheckIfLoaded() {
            if( !IsLoaded ) throw new InvalidOperationException( "PlayerDB is not loaded." );
        }

        [NotNull]
        public static PlayerInfo AddFakeEntry( [NotNull] string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();

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


        public static IPlayerDBProvider Provider;


        public static void Save() {
            Provider.Save();
        }

        #region Scheduled Saving

        static SchedulerTask saveTask;
        static TimeSpan saveInterval = TimeSpan.FromSeconds( 90 );
        public static TimeSpan SaveInterval {
            get { return saveInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "Save interval may not be negative" );
                saveInterval = value;
                if( saveTask != null ) saveTask.Interval = value;
            }
        }

        internal static void StartSaveTask() {
            saveTask = Scheduler.NewBackgroundTask( SaveTask )
                                .RunForever( SaveInterval, SaveInterval + TimeSpan.FromSeconds( 15 ) );
        }

        static void SaveTask( SchedulerTask task ) {
            Provider.Save();
        }

        #endregion


        #region Lookup

        [NotNull]
        public static PlayerInfo FindOrCreateInfoForPlayer( [NotNull] string name, [NotNull] IPAddress lastIP ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            CheckIfLoaded();
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


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            return FindPlayers( address, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( limit < 0 ) throw new ArgumentOutOfRangeException( "limit" );
            CheckIfLoaded();
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


        [NotNull]
        public static PlayerInfo[] FindPlayersCidr( [NotNull] IPAddress address, byte range ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            return FindPlayersCidr( address, range, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayersCidr( [NotNull] IPAddress address, byte range, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            if( limit < 0 ) throw new ArgumentOutOfRangeException( "limit" );
            CheckIfLoaded();
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            uint addressInt = address.AsUInt();
            uint netMask = IPAddressUtil.NetMask( range );
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( cache[i].LastIP.Match( addressInt, netMask ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) return result.ToArray();
                }
            }
            return result.ToArray();
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] Regex regex ) {
            if( regex == null ) throw new ArgumentNullException( "regex" );
            return FindPlayers( regex, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] Regex regex, int limit ) {
            if( regex == null ) throw new ArgumentNullException( "regex" );
            CheckIfLoaded();
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


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] string namePart ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            return FindPlayers( namePart, Int32.MaxValue );
        }


        [NotNull]
        public static PlayerInfo[] FindPlayers( [NotNull] string namePart, int limit ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            CheckIfLoaded();
            lock( AddLocker ) {
                //return Trie.ValuesStartingWith( namePart ).Take( limit ).ToArray(); // <- works, but is slightly slower
                return Trie.GetList( namePart, limit ).ToArray();
            }
        }


        /// <summary>Searches for player names starting with namePart, returning just one or none of the matches.</summary>
        /// <param name="namePart">Partial or full player name</param>
        /// <param name="info">PlayerInfo to output (will be set to null if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        internal static bool FindPlayerInfo( [NotNull] string namePart, out PlayerInfo info ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            CheckIfLoaded();
            lock( AddLocker ) {
                return Trie.GetOneMatch( namePart, out info );
            }
        }


        [CanBeNull]
        public static PlayerInfo FindPlayerInfoExact( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();
            lock( AddLocker ) {
                return Trie.Get( name );
            }
        }

        [CanBeNull]
        public static PlayerInfo FindPlayerInfoOrPrintMatches( [NotNull] Player player, [NotNull] string name ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return null;
                }
            }
            if( !Player.ContainsValidCharacters( name ) ) {
                player.MessageInvalidPlayerName( name );
                return null;
            }
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
            player.LastUsedPlayerName = target.Name;
            return target;
        }


        [NotNull]
        public static string FindExactClassyName( [CanBeNull] string name ) {
            if( string.IsNullOrEmpty( name ) ) return "?";
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
            CheckIfLoaded();
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
            CheckIfLoaded();
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
            if( player.BanStatus != BanStatus.NotBanned || player.UnbanDate != DateTime.MinValue ||
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


        internal static void SwapPlayerInfo( [NotNull] PlayerInfo p1, [NotNull] PlayerInfo p2 ) {
            if( p1 == null ) throw new ArgumentNullException( "p1" );
            if( p2 == null ) throw new ArgumentNullException( "p2" );
            lock( AddLocker ) {
                lock( SaveLoadLocker ) {
                    if( p1.IsOnline || p2.IsOnline ) {
                        throw new InvalidOperationException( "Both players must be offline to swap info." );
                    }

                    string tempString = p1.Name;
                    p1.Name = p2.Name;
                    p2.Name = tempString;

                    DateTime tempDate = p1.LastLoginDate;
                    p1.LastLoginDate = p2.LastLoginDate;
                    p2.LastLoginDate = tempDate;

                    tempDate = p1.LastSeen;
                    p1.LastSeen = p2.LastSeen;
                    p2.LastSeen = tempDate;

                    LeaveReason tempLeaveReason = p1.LeaveReason;
                    p1.LeaveReason = p2.LeaveReason;
                    p2.LeaveReason = tempLeaveReason;

                    IPAddress tempIP = p1.LastIP;
                    p1.LastIP = p2.LastIP;
                    p2.LastIP = tempIP;

                    bool tempBool = p1.IsHidden;
                    p1.IsHidden = p2.IsHidden;
                    p2.IsHidden = tempBool;
                }
            }
        }

        #endregion


        sealed class PlayerIDComparer : IComparer<PlayerInfo> {
            public static readonly PlayerIDComparer Instance = new PlayerIDComparer();
            private PlayerIDComparer() { }

            public int Compare( PlayerInfo x, PlayerInfo y ) {
                return x.ID - y.ID;
            }
        }


        public static StringBuilder AppendEscaped( [NotNull] this StringBuilder sb, [CanBeNull] string str ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
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