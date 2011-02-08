// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
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
        static StringTree tree = new StringTree();
        static List<PlayerInfo> list = new List<PlayerInfo>();
        public static PlayerInfo[] PlayerInfoList { get; private set; }
        public static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds( 60 );

        static int MaxID = 255;

        public const int NumberOfMatchesToPrint = 20;

        public const string DBFileName = "PlayerDB.txt",
                            TempDBFileName = DBFileName + ".temp",
                            BackupDBFileName = DBFileName + ".backup";

        const string Header = " fCraft PlayerDB | Row format: " +
                              "playerName,lastIP,rank,rankChangeDate,rankChangeBy," +
                              "banStatus,banDate,bannedBy,unbanDate,unbannedBy," +
                              "firstLoginDate,lastLoginDate,lastFailedLoginDate," +
                              "lastFailedLoginIP,failedLoginCount,totalTimeOnServer," +
                              "blocksBuilt,blocksDeleted,timesVisited," +
                              "linesWritten,UNUSED,UNUSED,previousRank,rankChangeReason," +
                              "timesKicked,timesKickedOthers,timesBannedOthers,UID,rankChangeType," +
                              "lastKickDate,LastSeen,BlocksDrawn,lastKickBy,lastKickReason," +
                              "bannedUntil,loggedOutFrozen,frozenBy,"+
                              "mutedUntil,mutedBy,IRCPassword,online,leaveReason";


        static readonly object locker = new object();
        public static bool IsLoaded { get; private set; }


        public static PlayerInfo AddFakeEntry( string name, RankChangeType rankChangeType ) {
            PlayerInfo info = new PlayerInfo( name, RankList.DefaultRank, false, rankChangeType );
            lock( locker ) {
                list.Add( info );
                tree.Add( info.name, info );
                UpdateCache();
            }
            return info;
        }


        #region Saving/Loading

        internal static void Load() {

            if( File.Exists( DBFileName ) ) {
                Stopwatch sw = Stopwatch.StartNew();
                using( StreamReader reader = File.OpenText( DBFileName ) ) {

                    string header = reader.ReadLine(); // header

                    lock( locker ) {
                        // first number of the header is MaxID
                        int maxIDField;
                        if( Int32.TryParse( header.Split( ' ' )[0], out maxIDField ) ) {
                            if( maxIDField >= 255 ) {// IDs start at 256
                                MaxID = maxIDField;
                            }
                        }

                        while( !reader.EndOfStream ) {
                            string[] fields = reader.ReadLine().Split( ',' );
                            if( fields.Length >= PlayerInfo.MinFieldCount ) {
                                try {
                                    PlayerInfo info = new PlayerInfo( fields );
                                    PlayerInfo dupe = tree.Get( info.name );
                                    if( dupe == null ) {
                                        tree.Add( info.name, info );
                                        list.Add( info );
                                    } else {
                                        Logger.Log( "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.", LogType.Error, info.name );
                                    }
                                } catch( FormatException ex ) {
                                    Logger.Log( "PlayerDB.Load: Could not parse a record: {0}.", LogType.Error, ex );
                                } catch( IOException ex ) {
                                    Logger.Log( "PlayerDB.Load: Error while trying to read from file: {0}.", LogType.Error, ex );
                                }
                            } else {
                                Logger.Log( "PlayerDB.Load: Unexpected field count ({0}), expecting at least {1} fields for a PlayerDB entry.", LogType.Error,
                                            fields.Length,
                                            PlayerInfo.MinFieldCount );
                            }
                        }
                    }
                }
                list.TrimExcess();
                sw.Stop();
                Logger.Log( "PlayerDB.Load: Done loading player DB ({0} records) in {1}ms.", LogType.Debug,
                            tree.Count, sw.ElapsedMilliseconds );
            } else {
                Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
            }
            UpdateCache();
            IsLoaded = true;

            /*
            Stopwatch sw2 = Stopwatch.StartNew();
            int pruneable = CountPrunedPlayers();
            sw2.Stop();
            Logger.Log( "Prune: {0} found in {1} ms", LogType.SystemActivity, pruneable, sw2.ElapsedMilliseconds );
             * */
        }


        internal static void SaveTask( Scheduler.Task task ) {
            Save();
        }


        internal static void Save() {
            Logger.Log( "PlayerDB.Save: Saving player database ({0} records).", LogType.Debug, tree.Count );


            PlayerInfo[] listCopy = GetPlayerListCopy();

            using( FileStream fs = new FileStream( TempDBFileName, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024 ) ) {
                using( StreamWriter writer = new StreamWriter( fs ) ) {
                    writer.WriteLine( MaxID + Header );
                    for( int i=0; i<listCopy.Length; i++){
                        writer.WriteLine( listCopy[i].Serialize() );
                    }
                }
            }
            try {
                if( File.Exists( DBFileName ) ) File.Replace( TempDBFileName, DBFileName, BackupDBFileName, true );
                else File.Move( TempDBFileName, DBFileName );
            } catch( Exception ex ) {
                Logger.Log( "PlayerDB.Save: An error occured while trying to save PlayerDB: " + ex, LogType.Error );
            }
        }

        #endregion


        #region Lookup

        public static PlayerInfo FindOrCreateInfoForPlayer( Player player ) {
            if( player == null ) return null;
            PlayerInfo info;

            lock( locker ) {
                info = tree.Get( player.name );
                if( info == null ) {
                    info = new PlayerInfo( player );
                    tree.Add( player.name, info );
                    list.Add( info );
                    UpdateCache();
                }
            }
            return info;
        }


        public static PlayerInfo[] FindPlayers( IPAddress address ) {
            return FindPlayers( address, Int32.MaxValue );
        }

        public static PlayerInfo[] FindPlayers( IPAddress address, int limit ) {
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            PlayerInfo[] cache = PlayerInfoList;
            foreach( PlayerInfo info in cache ) {
                if( info.lastIP.ToString() == address.ToString() ) {
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
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            PlayerInfo[] cache = PlayerInfoList;
            foreach( PlayerInfo info in cache ) {
                if( regex.IsMatch( info.name ) ) {
                    result.Add( info );
                    count++;
                    if( count >= limit ) return result.ToArray();
                }
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( string namePart ) {
            return FindPlayers( namePart, Int32.MaxValue );
        }

        public static PlayerInfo[] FindPlayers( string namePart, int limit ) {
            return tree.GetMultiple( namePart, limit ).ToArray();
        }

        /// <summary>Searches for player names starting with namePart, returning just one or none of the matches.</summary>
        /// <param name="name">Partial or full player name</param>
        /// <param name="info">PlayerInfo to output (will be set to null if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        public static bool FindPlayerInfo( string name, out PlayerInfo info ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( locker ) {
                return tree.Get( name, out info );
            }
        }


        public static PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( locker ) {
                return tree.Get( name );
            }
        }

        #endregion


        #region Stats

        public static int CountBannedPlayers() {
            PlayerInfo[] cache = PlayerInfoList;
            return cache.Count( t => t.banned );
        }


        public static int CountTotalPlayers() {
            return list.Count;
        }


        public static int CountPlayersByRank( Rank pc ) {
            return PlayerInfoList.Count( t => t.rank == pc );
        }

        #endregion


        public static int GetNextID() {
            return Interlocked.Increment( ref MaxID );
        }


        public static int MassRankChange( Player player, Rank from, Rank to, bool silent ) {
            int affected = 0;
            lock( locker ) {
                foreach( PlayerInfo info in list ) {
                    if( info.rank == from ) {
                        Player target = Server.FindPlayerExact( info.name );
                        AdminCommands.DoChangeRank( player, info, target, to, "~MassRank", silent, false );
                        affected++;
                    }
                }
                return affected;
            }
        }


        public static PlayerInfo[] GetPlayerListCopy() {
            return PlayerInfoList;
        }


        public static PlayerInfo[] GetPlayerListCopy( Rank pc ) {
            PlayerInfo[] cache = PlayerInfoList;
            return cache.Where( info => info.rank == pc ).ToArray();
        }


        static void UpdateCache() {
            lock( locker ) {
                PlayerInfoList = list.ToArray();
            }
        }


        public static int CountPrunedPlayers() {
            int i = 0;
            return PlayerInfoList.Count( delegate( PlayerInfo p ) {
                if( i % 100 == 0 ) {
                    System.Diagnostics.Trace.WriteLine( i );
                }
                i++;
                return PlayerIsInactive( p, false );
            } );
        }


        static bool PlayerIsInactive( PlayerInfo p, bool checkIP ) {
            if( p.banned || p.isFrozen || p.IsMuted() || p.timesKicked != 0 || p.rank != RankList.DefaultRank ) {
                return false;
            }
            if( p.totalTime.TotalMinutes > 60 || DateTime.Now.Subtract( p.lastSeen ).TotalDays < 30 ) {
                return false;
            }
            if( IPBanList.Get( p.lastIP ) != null ) {
                return false;
            }
            if(checkIP){
                foreach( PlayerInfo other in FindPlayers(p.lastIP)){
                    if( other != p && !PlayerIsInactive( other, false ) ) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}