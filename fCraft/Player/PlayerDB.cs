﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace fCraft {
    public static class PlayerDB {
        static readonly StringTree<PlayerInfo> Tree = new StringTree<PlayerInfo>();
        static readonly List<PlayerInfo> List = new List<PlayerInfo>();
        public static PlayerInfo[] PlayerInfoList { get; private set; }
        public static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds( 60 );

        static int maxID = 255;

        public const int NumberOfMatchesToPrint = 20;

        const string Header = " fCraft PlayerDB | Row format: " +
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


        static readonly object Locker = new object();
        public static bool IsLoaded { get; private set; }


        public static PlayerInfo AddFakeEntry( string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            PlayerInfo info = new PlayerInfo( name, RankList.DefaultRank, false, rankChangeType );
            lock( Locker ) {
                List.Add( info );
                Tree.Add( info.Name, info );
                UpdateCache();
            }
            return info;
        }


        #region Saving/Loading

        internal static void Load() {

            if( File.Exists( Paths.PlayerDBFileName ) ) {
                Stopwatch sw = Stopwatch.StartNew();
                using( StreamReader reader = File.OpenText( Paths.PlayerDBFileName ) ) {

                    string header = reader.ReadLine(); // header

                    if( header == null ) return; // PlayerDB is an empty file

                    lock( Locker ) {
                        // first number of the header is MaxID
                        int maxIDField;
                        if( Int32.TryParse( header.Split( ' ' )[0], out maxIDField ) ) {
                            if( maxIDField >= 255 ) {// IDs start at 256
                                maxID = maxIDField;
                            }
                        }

                        while( !reader.EndOfStream ) {
                            string[] fields = reader.ReadLine().Split( ',' );
                            if( fields.Length >= PlayerInfo.MinFieldCount ) {
                                try {
                                    PlayerInfo info = new PlayerInfo( fields );
                                    PlayerInfo dupe = Tree.Get( info.Name );
                                    if( dupe == null ) {
                                        Tree.Add( info.Name, info );
                                        List.Add( info );
                                    } else {
                                        Logger.Log( "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.", LogType.Error, info.Name );
                                    }
                                } catch( Exception ex ) {
                                    Logger.LogAndReportCrash( "Error while parsing PlayerInfo record", "fCraft", ex, false );
                                }
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
                            Tree.Count, sw.ElapsedMilliseconds );
            } else {
                Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
            }
            UpdateCache();
            IsLoaded = true;
        }


        internal static void SaveTask( Scheduler.Task task ) {
            Save();
        }


        internal static void Save() {
            Logger.Log( "PlayerDB.Save: Saving player database ({0} records).", LogType.Debug, Tree.Count );

            const string tempFileName = Paths.PlayerDBFileName + ".temp";
            PlayerInfo[] listCopy = GetPlayerListCopy();

            using( FileStream fs = new FileStream( tempFileName, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024 ) ) {
                using( StreamWriter writer = new StreamWriter( fs ) ) {
                    writer.WriteLine( maxID + Header );
                    for( int i = 0; i < listCopy.Length; i++ ) {
                        writer.WriteLine( listCopy[i].Serialize() );
                    }
                }
            }
            try {
                Paths.MoveOrReplace( tempFileName, Paths.PlayerDBFileName );
            } catch( Exception ex ) {
                Logger.Log( "PlayerDB.Save: An error occured while trying to save PlayerDB: " + ex, LogType.Error );
            }
        }

        #endregion


        #region Lookup

        public static PlayerInfo FindOrCreateInfoForPlayer( string name, IPAddress lastIP ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            PlayerInfo info;

            lock( Locker ) {
                info = Tree.Get( name );
                if( info == null ) {
                    info = new PlayerInfo( name, lastIP );
                    Tree.Add( name, info );
                    List.Add( info );
                    UpdateCache();
                }
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
            foreach( PlayerInfo info in cache ) {
                if( info.LastIP.ToString() == address.ToString() ) {
                    result.Add( info );
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
            foreach( PlayerInfo info in cache.Where( info => regex.IsMatch( info.Name ) ) ) {
                result.Add( info );
                count++;
                if( count >= limit ) break;
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( string namePart ) {
            return FindPlayers( namePart, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( string namePart, int limit ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            return Tree.GetMultiple( namePart, limit ).ToArray();
        }

        /// <summary>Searches for player names starting with namePart, returning just one or none of the matches.</summary>
        /// <param name="name">Partial or full player name</param>
        /// <param name="info">PlayerInfo to output (will be set to null if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        public static bool FindPlayerInfo( string name, out PlayerInfo info ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( Locker ) {
                return Tree.Get( name, out info );
            }
        }


        public static PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( Locker ) {
                return Tree.Get( name );
            }
        }

        #endregion


        #region Stats

        public static int CountBannedPlayers() {
            PlayerInfo[] cache = PlayerInfoList;
            return cache.Count( t => t.Banned );
        }


        public static int CountTotalPlayers() {
            return List.Count;
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
            lock( Locker ) {
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
            lock( Locker ) {
                PlayerInfoList = List.ToArray();
            }
        }


        #region Experimental & Debug things

        // TODO: figure out a good way of making this persistent. ReaderWriterLockSlim maybe? (If Mono is fixed)
        static Dictionary<IPAddress, List<PlayerInfo>> playersByIP;


        internal static int CountInactivePlayers() {
            int count;
            lock( Locker ) {
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
            lock( Locker ) {
                playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }
                foreach( PlayerInfo p in playerInfoListCache.Where( p => PlayerIsInactive( p, true ) ) ) {
                    Tree.Remove( p.Name );
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
            if( player.Banned || !String.IsNullOrEmpty( player.UnbannedBy ) || player.IsFrozen || player.IsMuted() || player.TimesKicked != 0 || !String.IsNullOrEmpty( player.RankChangedBy ) ) {
                return false;
            }
            if( player.TotalTime.TotalMinutes > 30 || DateTime.Now.Subtract( player.LastSeen ).TotalDays < 30 ) {
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