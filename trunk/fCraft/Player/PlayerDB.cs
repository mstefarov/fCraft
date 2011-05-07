// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Events;

namespace fCraft {
    public static class PlayerDB {
        static readonly Trie<PlayerInfo> Trie = new Trie<PlayerInfo>();
        static readonly List<PlayerInfo> List = new List<PlayerInfo>();
        public static PlayerInfo[] PlayerInfoList { get; private set; }

        static int maxID = 255;

        public const int NumberOfMatchesToPrint = 20;

        public const int FormatVersion = 1;

        const string Header = "fCraft PlayerDB | Row format: " +
                              "playerName,lastIP,rank,rankChangeDate,rankChangeBy," +
                              "banStatus,banDate,bannedBy,unbanDate,unbannedBy," +
                              "firstLoginDate,lastLoginDate,lastFailedLoginDate," +
                              "lastFailedLoginIP,failedLoginCount,totalTimeOnServer," +
                              "blocksBuilt,blocksDeleted,timesVisited," +
                              "linesWritten,UNUSED,UNUSED,previousRank,rankChangeReason," +
                              "timesKicked,timesKickedOthers,timesBannedOthers,UID,rankChangeType," +
                              "lastKickDate,LastSeen,BlocksDrawn,lastKickBy,lastKickReason," +
                              "bannedUntil,loggedOutFrozen,frozenBy," +
                              "mutedUntil,mutedBy,IRCPassword,online,leaveReason";


        static readonly object AddLocker = new object(),
                               SaveLoadLocker = new object();
        public static bool IsLoaded { get; private set; }


        public static PlayerInfo AddFakeEntry( string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            PlayerInfo info;
            lock( AddLocker ) {
                info = Trie.Get( name );
                if( info != null ) throw new ArgumentException( "A PlayerDB entry already exists for this name." );

                var e = new PlayerInfoCreatingEventArgs( name, IPAddress.None, RankManager.DefaultRank, true );
                Server.RaisePlayerInfoCreatingEvent( e );
                if( e.Cancel ) throw new OperationCanceledException( "Cancelled by a plugin." );

                info = new PlayerInfo( name, e.StartingRank, false, rankChangeType );

                List.Add( info );
                Trie.Add( info.Name, info );
                UpdateCache();
            }
            Server.RaisePlayerInfoCreatedEvent( info, false );
            return info;
        }


        #region Saving/Loading

        internal static void Load() {
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName ) ) {
                    Stopwatch sw = Stopwatch.StartNew();
                    using( StreamReader reader = File.OpenText( Paths.PlayerDBFileName ) ) {

                        string header = reader.ReadLine();

                        if( header == null ) return; // if PlayerDB is an empty file

                        lock( AddLocker ) {
                            int version = IdentifyFormatVersion( header );

                            while( !reader.EndOfStream ) {
                                string[] fields = reader.ReadLine().Split( ',' );
                                if( fields.Length >= PlayerInfo.MinFieldCount ) {
#if !DEBUG
                                    try {
#endif
                                        PlayerInfo info;
                                        if( version == 0 ) {
                                            info = PlayerInfo.LoadOldFormat( fields, true );
                                        } else {
                                            info = PlayerInfo.Load( fields );
                                        }
                                        if( Trie.ContainsKey( info.Name ) ) {
                                            Logger.Log( "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.", LogType.Error, info.Name );
                                        } else {
                                            Trie.Add( info.Name, info );
                                            List.Add( info );
                                        }
#if !DEBUG
                                    } catch( Exception ex ) {
                                        Logger.LogAndReportCrash( "Error while parsing PlayerInfo record", "fCraft", ex, false );
                                    }
#endif
                                } else {
                                    Logger.Log( "PlayerDB.Load: Unexpected field count ({0}), expecting at least {1} fields for a PlayerDB entry.", LogType.Error,
                                                fields.Length,
                                                PlayerInfo.MinFieldCount );
                                }
                            }
                        }
                    }
                    List.TrimExcess();
                    sw.Stop();
                    Logger.Log( "PlayerDB.Load: Done loading player DB ({0} records) in {1}ms.", LogType.Debug,
                                Trie.Count, sw.ElapsedMilliseconds );
                } else {
                    Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
                }
                UpdateCache();
                IsLoaded = true;
            }
        }


        static int IdentifyFormatVersion( string header ) {
            string[] headerParts = header.Split( ' ' );
            if( headerParts.Length < 2 ) throw new FormatException( "Invalid PlayerDB file format." );
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
                PlayerInfo[] listCopy = GetPlayerListCopy();
                Stopwatch sw = Stopwatch.StartNew();
                using( FileStream fs = new FileStream( tempFileName, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024 ) ) {
                    using( StreamWriter writer = new StreamWriter( fs, System.Text.Encoding.ASCII, 64 * 1024 ) ) {
                        writer.WriteLine( "{0} {1} {2}", maxID, FormatVersion, Header );

                        for( int i = 0; i < listCopy.Length; i++ ) {
                            // TODO: Reuse StringBuilder after switching to 4.0
                            writer.WriteLine( listCopy[i].Serialize() );
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
                                .RunForever( SaveInterval, TimeSpan.FromSeconds( 15 ) );
        }

        #endregion


        #region Lookup

        public static PlayerInfo FindOrCreateInfoForPlayer( string name, IPAddress lastIP ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            PlayerInfo info;

            // this flag is used to avoid executing PlayerInfoCreated event in the lock
            bool raiseCreatedEvent = false;

            lock( AddLocker ) {
                info = Trie.Get( name );
                if( info == null ) {
                    var e = new PlayerInfoCreatingEventArgs( name, lastIP, RankManager.DefaultRank, false );
                    Server.RaisePlayerInfoCreatingEvent( e );
                    if( e.Cancel ) throw new OperationCanceledException( "Cancelled by a plugin." );

                    info = new PlayerInfo( name, lastIP, e.StartingRank );
                    Trie.Add( name, info );
                    List.Add( info );
                    UpdateCache();

                    raiseCreatedEvent = true;
                }
            }

            if( raiseCreatedEvent ) {
                Server.RaisePlayerInfoCreatedEvent( info, false );
            }
            return info;
        }


        public static PlayerInfo[] FindPlayers( IPAddress address ) {
            return FindPlayers( address, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( IPAddress address, int limit ) {
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


        public static PlayerInfo[] FindPlayers( Regex regex ) {
            return FindPlayers( regex, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( Regex regex, int limit ) {
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


        public static PlayerInfo[] FindPlayers( string namePart ) {
            return FindPlayers( namePart, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( string namePart, int limit ) {
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
        public static bool FindPlayerInfo( string name, out PlayerInfo info ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( AddLocker ) {
                return Trie.GetOneMatch( name, out info );
            }
        }


        public static PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( AddLocker ) {
                return Trie.Get( name );
            }
        }

        #endregion


        #region Stats

        public static int CountBannedPlayers() {
            return PlayerInfoList.Count( t => t.Banned );
        }


        public static int CountTotalPlayers() {
            return Trie.Count;
        }


        public static int CountPlayersByRank( Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            return PlayerInfoList.Count( t => t.Rank == rank );
        }

        #endregion


        public static int GetNextID() {
            return Interlocked.Increment( ref maxID );
        }


        public static int MassRankChange( Player player, Rank from, Rank to, bool silent ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            int affected = 0;
            lock( AddLocker ) {
                foreach( PlayerInfo info in List ) {
                    if( info.Rank == from ) {
                        ModerationCommands.DoChangeRank( player, info, to, "~MassRank", silent, false );
                        affected++;
                    }
                }
                return affected;
            }
        }


        public static PlayerInfo[] GetPlayerListCopy() {
            return PlayerInfoList;
        }


        public static PlayerInfo[] GetPlayerListCopy( Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            PlayerInfo[] cache = PlayerInfoList;
            return cache.Where( info => info.Rank == rank ).ToArray();
        }


        static void UpdateCache() {
            lock( AddLocker ) {
                PlayerInfoList = List.ToArray();
            }
        }


        #region Experimental & Debug things

        // TODO: figure out a good way of making this persistent. ReaderWriterLockSlim maybe? (If Mono is fixed)
        static Dictionary<IPAddress, List<PlayerInfo>> playersByIP;


        internal static int CountInactivePlayers() {
            int count;
            lock( AddLocker ) {
                playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }
                count = playerInfoListCache.Count( p => PlayerIsInactive( p, true ) );
                playersByIP = null;
            }
            return count;
        }


        internal static int RemoveInactivePlayers() {
            int count = 0;
            lock( AddLocker ) {
                playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }
                foreach( PlayerInfo p in playerInfoListCache.Where( p => PlayerIsInactive( p, true ) ) ) {
                    Trie.Remove( p.Name );
                    List.Remove( p );
                    count++;
                }
                List.TrimExcess();
                UpdateCache();
                playersByIP = null;
            }
            return count;
        }


        static bool PlayerIsInactive( PlayerInfo player, bool checkIP ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.Banned || !String.IsNullOrEmpty( player.UnbannedBy ) || player.IsFrozen || player.IsMuted || player.TimesKicked != 0 || !String.IsNullOrEmpty( player.RankChangedBy ) ) {
                return false;
            }
            if( player.TotalTime.TotalMinutes > 30 || player.TimeSinceLastSeen.TotalDays < 30 ) {
                return false;
            }
            if( IPBanList.Get( player.LastIP ) != null ) {
                return false;
            }
            if( checkIP ) {
                return playersByIP[player.LastIP].All( other => (other == player) || PlayerIsInactive( other, false ) );
            }
            return true;
        }


        internal static void RecoverIPBans() {
            PlayerInfo[] playerInfoListCache = PlayerInfoList;
            for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                PlayerInfo p = playerInfoListCache[i];
                if( p.Banned && p.BanReason.EndsWith( "~BanAll", StringComparison.OrdinalIgnoreCase ) && IPBanList.Get( p.LastIP ) == null ) {
                    IPBanList.Add( new IPBanInfo( p.LastIP, p.Name, p.BannedBy, p.BanReason ) );
                    Logger.Log( "PlayerDB.RecoverIPBans: Banned {0} by association with {1}. Banned by {2}. Reason: {3}", LogType.SystemActivity,
                                p.LastIP, p.Name, p.BannedBy, p.BanReason );
                }
            }
        }

        #endregion
    }
}