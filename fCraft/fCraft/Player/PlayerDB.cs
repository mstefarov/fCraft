// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;


namespace fCraft {
    public static class PlayerDB {
        static StringTree tree = new StringTree();
        static List<PlayerInfo> list = new List<PlayerInfo>();

        public const string DBFile = "PlayerDB.txt",
                            Header = "playerName,lastIP,playerClass,classChangeDate,classChangedBy,"+
                                     "banStatus,banDate,bannedBy,unbanDate,unbannedBy,"+
                                     "firstLoginDate,lastLoginDate,lastFailedLoginDate,"+
                                     "lastFailedLoginIP,failedLoginCount,totalTimeOnServer," +
                                     "blocksBuilt,blocksDeleted,timesVisited,"+
                                     "linesWritten,thanksReceived,warningsReceived";

        public static ReaderWriterLockSlim locker = new ReaderWriterLockSlim();


        public static PlayerInfo AddFakeEntry( string name ) {
            PlayerInfo info = new PlayerInfo( name, ClassList.defaultClass );
            locker.EnterWriteLock();
            try {
                list.Add( info );
            } finally {
                locker.ExitWriteLock();
            }
            return info;
        }

        public static void Load() {
            if( File.Exists( DBFile ) ) {
                locker.EnterWriteLock();
                try {
                    using( StreamReader reader = File.OpenText( DBFile ) ) {
                        reader.ReadLine(); // header
                        while( !reader.EndOfStream ) {
                            string[] fields = reader.ReadLine().Split( ',' );
                            if( fields.Length == PlayerInfo.fieldCount ) {
                                try {
                                    PlayerInfo info = new PlayerInfo( fields );
                                    tree.Add( info.name, info );
                                    list.Add( info );
                                } catch( FormatException ex ) {
                                    Logger.Log( "PlayerDB.Load: Could not parse a record: {0}.", LogType.Error, ex.Message );
                                } catch( IOException ex ) {
                                    Logger.Log( "PlayerDB.Load: Error while trying to read from file: {0}.", LogType.Error, ex.Message );
                                }
                            }
                        }
                    }
                } finally {
                    locker.ExitWriteLock();
                }
                Logger.Log( "PlayerDB.Load: Done loading player DB ({0} records).", LogType.Debug, tree.Count() );
            } else {
                Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
            }
        }


        public static void Save() {
            Logger.Log( "PlayerDB.Save: Saving player database ({0} records).", LogType.Debug, tree.Count() );
            string tempFile = Path.GetTempFileName();
            locker.EnterReadLock();
            try {
                using( StreamWriter writer = File.CreateText( tempFile ) ) {
                    writer.WriteLine( Header );
                    foreach( PlayerInfo entry in list ) {
                        writer.WriteLine( entry.Serialize() );
                    }
                }
            } finally {
                locker.ExitReadLock();
            }
            try {
                File.Delete( DBFile );
                File.Move( tempFile, DBFile );
            } catch( Exception ex ) {
                Logger.Log( "PlayerDB.Save: An error occured while trying to save PlayerDB: " + ex, LogType.Error );
            }
        }


        public static PlayerInfo FindPlayerInfo( Player player ) {
            if( player == null ) return null;
            PlayerInfo info;
            locker.EnterUpgradeableReadLock();
            try {
                info = tree.Get( player.name );
                if( info == null ) {
                    info = new PlayerInfo( player );
                    locker.EnterWriteLock();
                    try {
                        tree.Add( player.name, info );
                        list.Add( info );
                    } finally {
                        locker.ExitWriteLock();
                    }
                }
            } finally {
                locker.ExitUpgradeableReadLock();
            }
            return info;
        }


        public static List<PlayerInfo> FindPlayersByIP( IPAddress address ) {
            List<PlayerInfo> result = new List<PlayerInfo>();
            locker.EnterReadLock();
            try {
                foreach( PlayerInfo info in list ) {
                    if( info.lastIP == address ) {
                        result.Add( info );
                    }
                }
            } finally {
                locker.ExitReadLock();
            }
            return result;
        }


        public static bool FindPlayerInfo( string name, out PlayerInfo info ) {
            if( name == null ) {
                info = null;
                return false;
            }

            bool noDupe;
            locker.EnterReadLock();
            try {
                noDupe = tree.Get( name, out info );
            } finally {
                locker.ExitReadLock();
            }

            return noDupe;
        }


        public static PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) return null;
            PlayerInfo info;
            locker.EnterReadLock();
            try {
                info = tree.Get( name );
            } finally {
                locker.ExitReadLock();
            }

            return info;
        }

        internal static void ProcessLogout( Player player ) {
            if( player == null ) return;
            locker.EnterWriteLock();
            try {
                tree.Get( player.name ).ProcessLogout( player );
            } finally {
                locker.ExitWriteLock();
            }
        }
    }
}