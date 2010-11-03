// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;


namespace fCraft {
    public static class PlayerDB {
        static StringTree tree = new StringTree();
        static List<PlayerInfo> list = new List<PlayerInfo>();
        public const int SaveInterval = 60000; // 60s

        static int MaxID = 255;

        public static string ToCompactString( this TimeSpan span ) {
            return String.Format( "{0}.{1:00}:{2:00}:{3:00}",
                span.Days, span.Hours, span.Minutes, span.Seconds );
        }

        public static string ToCompactString( this DateTime date ) {
            return date.ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ssK" );
        }


        public const string DBFile = "PlayerDB.txt",
                            Header = " fCraft PlayerDB | Row format: " +
                                     "playerName,lastIP,rank,rankChangeDate,rankChangeBy," +
                                     "banStatus,banDate,bannedBy,unbanDate,unbannedBy," +
                                     "firstLoginDate,lastLoginDate,lastFailedLoginDate," +
                                     "lastFailedLoginIP,failedLoginCount,totalTimeOnServer," +
                                     "blocksBuilt,blocksDeleted,timesVisited," +
                                     "linesWritten,UNUSED,UNUSED,previousRank,rankChangeReason," +
                                     "timesKicked,timesKickedOthers,timesBannedOthers,UID," +
                                     "rankChangeType,lastKickDate,LastSeen,BlocksDrawn,lastKickBy,lastKickReason";

        static object locker = new object();
        public static bool isLoaded;


        public static PlayerInfo AddFakeEntry( string name, RankChangeType _rankChangeType ) {
            PlayerInfo info = new PlayerInfo( name, RankList.DefaultRank, false, _rankChangeType );
            lock( locker ) {
                list.Add( info );
                tree.Add( info.name, info );
            }
            return info;
        }


        #region Saving/Loading

        public static void Load() {
            if( File.Exists( DBFile ) ) {
                using( StreamReader reader = File.OpenText( DBFile ) ) {

                    string header = reader.ReadLine();// header
                    int maxIDField;

                    lock( locker ) {
                        // first number of the header is MaxID
                        if( Int32.TryParse( header.Split( ' ' )[0], out maxIDField ) ) {
                            if( maxIDField >= 255 ) {// IDs start at 256
                                MaxID = maxIDField;
                            }
                        }

                        while( !reader.EndOfStream ) {
                            string[] fields = reader.ReadLine().Split( ',' );
                            if( fields.Length >= PlayerInfo.MinFieldCount && fields.Length <= PlayerInfo.MaxFieldCount ) {
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
                                Logger.Log( "PlayerDB.Load: Unexpected field count ({0}), expecting between {1} and {2} fields for a PlayerDB entry.", LogType.Error,
                                            fields.Length,
                                            PlayerInfo.MinFieldCount,
                                            PlayerInfo.MaxFieldCount );
                            }
                        }
                    }
                }
                Logger.Log( "PlayerDB.Load: Done loading player DB ({0} records).", LogType.Debug, tree.Count() );
                list.TrimExcess();
            } else {
                Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
            }
            isLoaded = true;
        }


        public static void Save() {
            Logger.Log( "PlayerDB.Save: Saving player database ({0} records).", LogType.Debug, tree.Count() );
            string tempFile = DBFile + ".temp";

            using( StreamWriter writer = File.CreateText( tempFile ) ) {
                lock( locker ) {
                    writer.WriteLine( MaxID + Header );
                    foreach( PlayerInfo entry in list ) {
                        writer.WriteLine( entry.Serialize() );
                    }
                }
            }
            try {
                if( File.Exists( DBFile ) ) {
                    File.Replace( tempFile, DBFile, null, true );
                } else {
                    File.Move( tempFile, DBFile );
                }
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
                }
            }
            return info;
        }


        public static PlayerInfo[] FindPlayers( IPAddress address, int limit ) {
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            lock( locker ) {
                foreach( PlayerInfo info in list ) {
                    if( info.lastIP.ToString() == address.ToString() ) {
                        result.Add( info );
                        count++;
                        if( count >= limit ) return result.ToArray();
                    }
                }
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( Regex regex, int limit ) {
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            lock( locker ) {
                foreach( PlayerInfo info in list ) {
                    if( regex.IsMatch(info.name) ) {
                        result.Add( info );
                        count++;
                        if( count >= limit ) return result.ToArray();
                    }
                }
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( string namePart, int limit ) {
            return tree.GetMultiple( namePart, limit ).ToArray();
        }


        public static bool FindPlayerInfo( string name, out PlayerInfo info ) {
            if( name == null ) {
                info = null;
                return false;
            }

            lock( locker ) {
                return tree.Get( name, out info );
            }
        }


        public static PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) return null;
            lock( locker ) {
                return tree.Get( name );
            }
        }

        #endregion


        #region Stats

        public static int CountBannedPlayers() {
            int banned = 0;
            lock( locker ) {
                foreach( PlayerInfo info in list ) {
                    if( info.banned ) banned++;
                }
                return banned;
            }
        }


        public static int CountTotalPlayers() {
            return list.Count;
        }


        public static int CountPlayersByRank( Rank pc ) {
            int count = 0;
            lock( locker ) {
                foreach( PlayerInfo info in list ) {
                    if( info.rank == pc ) count++;
                }
                return count;
            }
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
            lock( locker ) {
                return list.ToArray();
            }
        }


        public static PlayerInfo[] GetPlayerListCopy( Rank pc ) {
            lock( locker ) {
                List<PlayerInfo> tempList = new List<PlayerInfo>();
                foreach( PlayerInfo info in list ) {
                    if( info.rank == pc ) {
                        tempList.Add( info );
                    }
                }
                return tempList.ToArray();
            }
        }
    }
}