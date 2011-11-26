// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Persistent database of player information. </summary>
    public static class PlayerDB {
        static readonly List<PlayerInfo> List = new List<PlayerInfo>();

        public static string ProviderType { get; internal set; }

        static readonly object AddLocker = new object();

        /// <summary> Cached list of all players in the database.
        /// May be quite long. Make sure to copy a reference to
        /// the list before accessing it in a loop, since this 
        /// array be frequently be replaced by an updated one. </summary>
        public static PlayerInfo[] PlayerInfoList { get; private set; }


        public static bool IsLoaded { get; private set; }


        static void CheckIfLoaded() {
            if( !IsLoaded ) throw new InvalidOperationException( "PlayerDB is not loaded." );
        }


        [NotNull]
        public static PlayerInfo AddSuper( ReservedPlayerIDs id, [NotNull] string name, Rank rank ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();
            return Provider.AddSuperPlayer( id, name, rank );
        }

        [NotNull]
        public static PlayerInfo AddUnrecognized( [NotNull] string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();

            PlayerInfo info;
            lock( Provider.SyncRoot ) {
                info = Provider.FindExact( name );
                if( info != null ) {
                    throw new ArgumentException( "A PlayerDB entry already exists for this name.", "name" );
                }

                var e = new PlayerInfoCreatingEventArgs( name, IPAddress.None, RankManager.DefaultRank, true );
                PlayerInfo.RaiseCreatingEvent( e );
                if( e.Cancel ) {
                    throw new OperationCanceledException( "Cancelled by a plugin." );
                }

                info = Provider.AddUnrecognizedPlayer( name, e.StartingRank, rankChangeType );

                List.Add( info );
                UpdateCache();
            }
            PlayerInfo.RaiseCreatedEvent( info, false );
            return info;
        }


        [NotNull]
        static IPlayerDBProvider Provider = new FlatfilePlayerDBProvider(); // TODO


        public static void Load() {
            if( IsLoaded ) throw new InvalidOperationException( "PlayerDB is already loaded." );
            Stopwatch sw = Stopwatch.StartNew();

            var playerList = Provider.Load();

            if( playerList != null ) {
                List.AddRange( playerList );
                sw.Stop();
                Logger.Log( LogType.Debug,
                            "PlayerDB.Load: Done loading ({0} records read) in {1}ms",
                            List.Count, sw.ElapsedMilliseconds);
            } else {
                Logger.Log( LogType.Debug,
                            "PlayerDB.Load: No records loaded." );
            }
            PostLoadCheck();
            UpdateCache();
            IsLoaded = true;
        }


        public static void Save() {
            CheckIfLoaded();
            Stopwatch sw = Stopwatch.StartNew();

            Provider.Save();

            sw.Stop();
            Logger.Log( LogType.Debug,
                        "PlayerDB.Save: Done saving ({0} records written) in {1}ms",
                        List.Count, sw.ElapsedMilliseconds );
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

            lock( Provider.SyncRoot ) {
                info = Provider.FindExact( name );
                if( info == null ) {
                    var e = new PlayerInfoCreatingEventArgs( name, lastIP, RankManager.DefaultRank, false );
                    PlayerInfo.RaiseCreatingEvent( e );
                    if( e.Cancel ) throw new OperationCanceledException( "Cancelled by a plugin." );

                    info = Provider.AddPlayer( name, lastIP, e.StartingRank, RankChangeType.Default );
                    PlayerInfo.RaiseCreatedEvent( info, false );
                }
            }

            return info;
        }


        [NotNull]
        public static IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            return FindByIP( address, Int32.MaxValue );
        }


        [NotNull]
        public static IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( limit < 0 ) throw new ArgumentOutOfRangeException( "limit" );
            CheckIfLoaded();
            return Provider.FindByIP( address, limit );
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
        public static IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern ) {
            if( pattern == null ) throw new ArgumentNullException( "pattern" );
            return FindByPattern( pattern, Int32.MaxValue );
        }


        [NotNull]
        public static IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern, int limit ) {
            if( pattern == null ) throw new ArgumentNullException( "pattern" );
            CheckIfLoaded();
            return Provider.FindByPattern( pattern, limit );
        }


        [NotNull]
        public static IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string namePart ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            return FindByPartialName( namePart, Int32.MaxValue );
        }


        [NotNull]
        public static IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string namePart, int limit ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            CheckIfLoaded();
            return Provider.FindByPartialName( namePart, limit );
        }


        /// <summary>Searches for player names starting with namePart, returning just one or none of the matches.</summary>
        /// <param name="partialName">Partial or full player name</param>
        /// <param name="result">PlayerInfo to output (will be set to null if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        internal static bool FindPlayerInfo( [NotNull] string partialName, out PlayerInfo result ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            CheckIfLoaded();
            return Provider.FindOneByPartialName( partialName, out result );
        }


        [CanBeNull]
        public static PlayerInfo FindPlayerInfoExact( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();
            return Provider.FindExact( name );
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
                PlayerInfo[] targets = FindByPartialName( name ).ToArray();
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
            get { return List.Count; }
        }

        #endregion


        static void PostLoadCheck() {
            Logger.Log( LogType.SystemActivity, "PlayerDB: Checking consistency of player records..." );
            List.Sort( PlayerInfo.Comparer );

            int unhid = 0, unfroze = 0, unmuted = 0;
            for( int i = 0; i < List.Count; i++ ) {
                if( List[i].IsBanned ) {
                    if( List[i].IsHidden ) {
                        unhid++;
                        List[i].IsHidden = false;
                    }

                    if( List[i].IsFrozen ) {
                        List[i].Unfreeze();
                        unfroze++;
                    }

                    if( List[i].IsMuted ) {
                        List[i].Unmute();
                        unmuted++;
                    }
                }
            }
            Logger.Log( LogType.SystemActivity,
                        "PlayerDB.PostLoadCheck: Unhid {0}, unfroze {1}, and unmuted {2} banned accounts.",
                        unhid, unfroze, unmuted );
        }


        /// <summary> Finds PlayerInfo by ID. Returns null of not found. </summary>
        [CanBeNull]
        public static PlayerInfo FindPlayerInfoByID( int id ) {
            CheckIfLoaded();
            PlayerInfo dummy = new PlayerInfo( id );
            lock( AddLocker ) {
                int index = List.BinarySearch( dummy, PlayerInfo.Comparer );
                if( index >= 0 ) {
                    return List[index];
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
                            List[i].ChangeRank( player, to, fullReason, true, true, false );
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
                PlayerInfoList = List.ToArray();
            }
        }


        #region Experimental & Debug things

        /*internal static int CountInactivePlayers() {
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
        }*/


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
                lock( Provider.SyncRoot ) {
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