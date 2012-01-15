// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Persistent database of player information. </summary>
    public static class PlayerDB {
        static readonly List<PlayerInfo> List = new List<PlayerInfo>();
        static IPlayerDBProvider provider;
        static readonly object AddLocker = new object();

        /// <summary> Cached list of all players in the database.
        /// May be quite long. Make sure to copy a reference to
        /// the list before accessing it in a loop, since this 
        /// array be frequently be replaced by an updated one. </summary>
        public static PlayerInfo[] PlayerInfoList { get; private set; }


        /// <summary> True if PlayerDB is loaded and ready; otherwise false. </summary>
        public static bool IsLoaded { get; private set; }


        static void CheckIfLoaded() {
            if( !IsLoaded ) throw new InvalidOperationException( "PlayerDB is not loaded." );
        }


        /// <summary> Current PlayerDBProvider type. May only be changed BEFORE PlayerDB is loaded. </summary>
        /// <exception cref="InvalidOperationException"> If PlayerDB is already loaded. </exception>
        static PlayerDBProviderType providerType;
        public static PlayerDBProviderType ProviderType {
            get { return providerType; }
            set {
                if( IsLoaded ) throw new InvalidOperationException( "PlayerDB is already loaded." );
                providerType = value;
            }
        }


        [NotNull]
        internal static PlayerInfo AddSuperPlayer( ReservedPlayerID id, [NotNull] string name, [NotNull] Rank rank ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();
            PlayerInfo newInfo = new PlayerInfo( (int)id, name, rank, RankChangeType.AutoPromoted, true ) {
                RaisePropertyChangedEvents = true
            };
            return newInfo;
        }


        /// <summary> Adds a new PlayerInfo entry for a player who has never been online, by name. </summary>
        /// <returns> A newly-created PlayerInfo entry. </returns>
        /// <exception cref="InvalidOperationException"> If PlayerDB is not loaded. </exception>
        [NotNull]
        public static PlayerInfo AddUnrecognizedPlayer( [NotNull] string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            CheckIfLoaded();

            PlayerInfo newInfo;
            lock( provider.SyncRoot ) {
                newInfo = provider.FindExact( name );
                if( newInfo != null ) {
                    throw new ArgumentException( "A PlayerDB entry already exists for this name.", "name" );
                }

                var e = new PlayerInfoBeingCreatedEventArgs( name, IPAddress.None, RankManager.DefaultRank, true );
                PlayerInfo.RaiseBeingCreatedEvent( e );
                if( e.Cancel ) {
                    throw new OperationCanceledException( "Cancelled by a plugin." );
                }

                newInfo = provider.AddUnrecognizedPlayer( name, e.StartingRank, rankChangeType );
                newInfo.RaisePropertyChangedEvents = true;

                List.Add( newInfo );
                UpdateCache();
            }
            PlayerInfo.RaiseCreatedEvent( newInfo, false );
            return newInfo;
        }


        const string MySqlPlayerDBProviderType = "fCraft.MySql.MySqlPlayerDBProvider";
        
        /// <summary> Loads contents of PlayerDB. </summary>
        /// <exception cref="InvalidOperationException"> If PlayerDB is akready loaded. </exception>
        /// <exception cref="MisconfigurationException"> If an unknown PlayerDBProviderType is specified. </exception>
        /// <exception cref="TypeLoadException"> If MySqlPlayerDBProvider could not be found. </exception>
        public static void Load() {
            if( IsLoaded ) throw new InvalidOperationException( "PlayerDB is already loaded." );
            Stopwatch sw = Stopwatch.StartNew();

            switch( ProviderType ) {
                case PlayerDBProviderType.Flatfile:
                    provider = new FlatfilePlayerDBProvider();
                    break;
                case PlayerDBProviderType.MySql:
                    Assembly mySqlAsm =
                        Assembly.LoadFile( Path.Combine( Paths.WorkingPath, Paths.MySqlPlayerDBProviderModule ) );
                    provider = (IPlayerDBProvider)mySqlAsm.CreateInstance( MySqlPlayerDBProviderType );
                    if( provider == null ) {
                        throw new TypeLoadException( "PlayerDB.Load: Could not find MySqlPlayerDBProvider." );
                    }
                    break;
                default:
                    throw new MisconfigurationException( "PlayerDB.Load: Unknown ProviderType: " + ProviderType );
            }

            var playerList = provider.Load();

            if( playerList != null ) {
                List.AddRange( playerList );
                sw.Stop();
                Logger.Log( LogType.Debug,
                            "PlayerDB.Load: Done loading ({0} records read) in {1}ms",
                            List.Count, sw.ElapsedMilliseconds );
            } else {
                Logger.Log( LogType.Debug,
                            "PlayerDB.Load: No records loaded." );
            }

            Logger.Log( LogType.SystemActivity, "PlayerDB: Checking consistency of player records..." );
            List.Sort( PlayerInfo.ComparerByID );

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
                List[i].RaisePropertyChangedEvents = true;
            }
            if( unhid != 0 || unfroze != 0 || unmuted != 0 ) {
                Logger.Log( LogType.SystemActivity,
                            "PlayerDB: Unhid {0}, unfroze {1}, and unmuted {2} banned accounts.",
                            unhid, unfroze, unmuted );
            }

            UpdateCache();
            IsLoaded = true;

            // Import everything from flatfile
            //provider.Import( new FlatfilePlayerDBProvider().Load() );
        }


        #region Saving

        /// <summary> Saves contents of PlayerDB. </summary>
        public static void Save() {
            CheckIfLoaded();
            Stopwatch sw = Stopwatch.StartNew();

            provider.Save();

            sw.Stop();
            Logger.Log( LogType.Debug,
                        "PlayerDB.Save: Done saving ({0} records written) in {1}ms",
                        List.Count, sw.ElapsedMilliseconds );
        }


        static SchedulerTask saveTask;

        static TimeSpan saveInterval = TimeSpan.FromSeconds( 90 );
        /// <summary> Amount of time to wait between saving to DB. </summary>
        public static TimeSpan SaveInterval {
            get { return saveInterval; }
            set {
                if( value.Ticks < 1 ) throw new ArgumentException( "Save interval may not be zero or negative" );
                saveInterval = value;
                if( saveTask != null ) saveTask.Interval = value;
            }
        }

        internal static void StartSaveTask() {
            saveTask = Scheduler.NewBackgroundTask( SaveTask )
                                .RunForever( SaveInterval, SaveInterval + TimeSpan.FromSeconds( 15 ) );
        }

        static void SaveTask( SchedulerTask task ) {
            Save();
        }

        #endregion


        #region Lookup

        /// <summary> Looks for player with the exact given name. Creates a new PlayerInfo if no records exists. </summary>
        /// <param name="name"> Exact player name, case-insensitive. </param>
        /// <param name="lastIP"> IP address currently used by the player. </param>
        /// <returns> Either an existing or a new PlayerInfo object for the player. </returns>
        [NotNull]
        public static PlayerInfo FindOrCreateInfoForPlayer( [NotNull] string name, [NotNull] IPAddress lastIP ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            CheckIfLoaded();
            PlayerInfo info;

            lock( provider.SyncRoot ) {
                info = provider.FindExact( name );
                if( info == null ) {
                    var e = new PlayerInfoBeingCreatedEventArgs( name, lastIP, RankManager.DefaultRank, false );
                    PlayerInfo.RaiseBeingCreatedEvent( e );
                    if( e.Cancel ) throw new OperationCanceledException( "Cancelled by a plugin." );

                    info = provider.AddPlayer( name, e.StartingRank, RankChangeType.Default, lastIP );
                    info.RaisePropertyChangedEvents = true;
                    List.Add( info );

                    PlayerInfo.RaiseCreatedEvent( info, false );
                }
            }

            return info;
        }


        /// <summary> Finds players by IP address. </summary>
        /// <param name="address"> Player's IP address. </param>
        /// <returns> A sequence of zero or more PlayerInfos who have logged in from given IP. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            return FindByIP( address, Int32.MaxValue );
        }


        /// <summary> Finds players by IP address. </summary>
        /// <param name="address"> Player's IP address. </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos who have logged in from given IP. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( limit < 0 ) throw new ArgumentOutOfRangeException( "limit" );
            CheckIfLoaded();
            return provider.FindByIP( address, limit );
        }


        /// <summary> Finds players in the given IPv4 address range. </summary>
        /// <param name="address"> Player's IP address. </param>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <returns> A sequence of zero or more PlayerInfos who have logged in from given IP. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindPlayersCidr( [NotNull] IPAddress address, byte range ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            return FindPlayersCidr( address, range, Int32.MaxValue );
        }


        /// <summary> Finds players in the given IPv4 address range. </summary>
        /// <param name="address"> Player's IP address. </param>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos who have logged in from given IP. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindPlayersCidr( [NotNull] IPAddress address, byte range, int limit ) {
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
            return result;
        }


        /// <summary> Finds player by name pattern. </summary>
        /// <param name="pattern"> Pattern to search for.
        /// Asterisk (*) matches zero or more characters.
        /// Question mark (?) matches exactly one character. </param>
        /// <returns> A sequence of zero or more PlayerInfos whose names match the pattern. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern ) {
            if( pattern == null ) throw new ArgumentNullException( "pattern" );
            return FindByPattern( pattern, Int32.MaxValue );
        }


        /// <summary> Finds player by name pattern. </summary>
        /// <param name="pattern"> Pattern to search for.
        /// Asterisk (*) matches zero or more characters.
        /// Question mark (?) matches exactly one character. </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos whose names match the pattern. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern, int limit ) {
            if( pattern == null ) throw new ArgumentNullException( "pattern" );
            CheckIfLoaded();
            return provider.FindByPattern( pattern, limit );
        }


        /// <summary> Finds players by partial name (prefix). </summary>
        /// <param name="partialName"> Full or partial name of the player. </param>
        /// <returns> A sequence of zero or more PlayerInfos whose names start with partialName. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string partialName ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            return FindByPartialName( partialName, Int32.MaxValue );
        }


        /// <summary> Finds players by partial name (prefix). </summary>
        /// <param name="partialName"> Full or partial name of the player. </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos whose names start with partialName. </returns>
        [NotNull]
        public static IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string partialName, int limit ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            CheckIfLoaded();
            return provider.FindByPartialName( partialName, limit );
        }


        /// <summary> Searches for player names starting with namePart, returning just one or none of the matches. </summary>
        /// <param name="partialName"> Partial or full player name. </param>
        /// <param name="result"> PlayerInfo to output (will be set to null if no single match was found). </param>
        /// <returns> true if one or zero matches were found, false if multiple matches were found. </returns>
        internal static bool FindOneByPartialName( [NotNull] string partialName, [CanBeNull] out PlayerInfo result ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            CheckIfLoaded();
            return provider.FindOneByPartialName( partialName, out result );
        }


        /// <summary> Finds player by exact name. </summary>
        /// <param name="fullName"> Full, case-insensitive name of the player. </param>
        /// <returns> PlayerInfo object if the player was found. Null if not found. </returns>
        [CanBeNull]
        public static PlayerInfo FindExact( [NotNull] string fullName ) {
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            CheckIfLoaded();
            return provider.FindExact( fullName );
        }


        /// <summary> Searches for player names starting with namePart.
        /// If exactly one player matched, returns the corresponding PlayerInfo object.
        /// If name format is incorrect, or if no matches were found, an appropriate message is printed to the player.
        /// If multiple players were found matching the partialName, first 25 matches are printed. </summary>
        /// <param name="player"> Player to print feedback to. </param>
        /// <param name="partialName"> Partial or full player name. </param>
        /// <returns> PlayerInfo object if one player was found. Null if no or multiple matches were found. </returns>
        [CanBeNull]
        public static PlayerInfo FindByPartialNameOrPrintMatches( [NotNull] Player player, [NotNull] string partialName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            CheckIfLoaded();
            if( partialName == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    partialName = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return null;
                }
            }
            if( !Player.ContainsValidCharacters( partialName ) ) {
                player.MessageInvalidPlayerName( partialName );
                return null;
            }
            PlayerInfo target = FindExact( partialName );
            if( target == null ) {
                PlayerInfo[] targets = FindByPartialName( partialName ).ToArray();
                if( targets.Length == 0 ) {
                    player.MessageNoPlayer( partialName );
                    return null;

                } else if( targets.Length > 1 ) {
                    Array.Sort( targets, new PlayerInfoComparer( player ) );
                    player.MessageManyMatches( "player", targets );
                    return null;
                }
                target = targets[0];
            }
            player.LastUsedPlayerName = target.Name;
            return target;
        }


        /// <summary> Finds player by exact name, and returns formatted name (ClassyName) if found. </summary>
        /// <param name="fullName"> Full, case-insensitive name of the player. </param>
        /// <returns> Player's formatted name, if found. "?" if fullName is null or empty. "fullName(?)" if player was not found. </returns>
        [NotNull]
        public static string FindExactClassyName( [CanBeNull] string fullName ) {
            if( string.IsNullOrEmpty( fullName ) ) return "?";
            if( !IsLoaded ) return fullName + "(?)";
            PlayerInfo info = FindExact( fullName );
            if( info == null ) return fullName + "(?)";
            else return info.ClassyName;
        }


        /// <summary> Finds PlayerInfo by ID. </summary>
        /// <returns> PlayerInfo object if found; null if not found. </returns>
        [CanBeNull]
        public static PlayerInfo FindByID( int id ) {
            if( id < 256 ) throw new ArgumentException( "Valid player IDs start at 256." );
            CheckIfLoaded();
            PlayerInfo dummy = new PlayerInfo( id );
            lock( AddLocker ) {
                int index = List.BinarySearch( dummy, PlayerInfo.ComparerByID );
                if( index >= 0 ) {
                    return List[index];
                } else {
                    return null;
                }
            }
        }

        #endregion


        #region Stats

        /// <summary> Number of banned PlayerInfo records. Does not include IP-banned records. </summary>
        public static int BannedCount {
            get {
                return PlayerInfoList.Count( t => t.IsBanned );
            }
        }


        /// <summary> Percentage of players who are banned. </summary>
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


        /// <summary> Number of PlayerInfo records in the database. </summary>
        public static int Size {
            get { return List.Count; }
        }

        #endregion


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
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
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
        }*/


        internal static void SwapPlayerInfo( [NotNull] PlayerInfo p1, [NotNull] PlayerInfo p2 ) {
            if( p1 == null ) throw new ArgumentNullException( "p1" );
            if( p2 == null ) throw new ArgumentNullException( "p2" );
            lock( AddLocker ) {
                lock( provider.SyncRoot ) {
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


        internal static StringBuilder AppendEscaped( [NotNull] this StringBuilder sb, [CanBeNull] string str ) {
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