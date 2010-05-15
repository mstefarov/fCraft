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


        public static void Load() {
            if( File.Exists( DBFile ) ) {
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
                Logger.Log( "PlayerDB.Load: Done loading player DB ({0} records).", LogType.Debug, tree.Count() );
            } else {
                Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
            }
        }


        public static void Save() {
            Logger.Log( "PlayerDB.Save: Saving player database ({0} records).", LogType.Debug, tree.Count() );
            string tempFile = DBFile + (new Random()).Next().ToString();
            locker.EnterReadLock();
            using( StreamWriter writer = File.CreateText( tempFile ) ) {
                writer.WriteLine( Header );
                foreach( PlayerInfo entry in list ) {
                    writer.WriteLine( entry.Serialize() );
                }
            }
            locker.ExitReadLock();
            File.Delete( DBFile );
            File.Move( tempFile, DBFile );
        }


        public static PlayerInfo FindPlayerInfo( Player player ) {
            if( player == null ) return null;
            
            locker.EnterWriteLock();
            PlayerInfo info = tree.Get( player.name );
            if( info == null ) {
                info = new PlayerInfo( player );
                tree.Add( player.name, info );
                list.Add( info );
            }
            locker.ExitWriteLock();
            return info;
        }


        public static List<PlayerInfo> FindPlayersByIP( IPAddress address ) {
            List<PlayerInfo> result = new List<PlayerInfo>();
            lock( locker ) {
                foreach( PlayerInfo info in list ){
                    if( info.lastIP == address ) {
                        result.Add( info );
                    }
                }
            }
            return result;
        }


        public static bool FindPlayerInfo( string name, out PlayerInfo info ) {
            if( name == null ) {
                info = null;
                return false;
            }

            bool noDupe;
            locker.EnterWriteLock();
            noDupe = tree.Get( name, out info );
            locker.ExitWriteLock();

            return noDupe;
        }


        public static PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) return null;

            locker.EnterWriteLock();
            PlayerInfo info = tree.Get( name );
            locker.ExitWriteLock();

            return info;
        }

        internal static void ProcessLogout( Player player ) {
            if( player == null ) return;
            locker.EnterWriteLock();
            tree.Get( player.name ).ProcessLogout( player );
            locker.ExitWriteLock();
        }
    }
}