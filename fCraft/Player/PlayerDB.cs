using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;

namespace fCraft {
    static class PlayerDB {
        public static Dictionary<string, PlayerInfo> data = new Dictionary<string, PlayerInfo>();
        public const string DateFormat = "o",
                            FileName = "PlayerDB.txt",
                            Header = "playerName,lastIP,playerClass,classChangeDate,classChangedBy,"+
                                     "banStatus,banDate,bannedBy,firstLoginDate,lastLoginDate,"+
                                     "totalTimeOnServer,blocksBuilt,blocksDeleted,timesVisited,"+
                                     "linesWritten,thanksReceived,warningsReceived";

        public static ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        public static void Load() {
            if( File.Exists( FileName ) ) {
                using( StreamReader reader = File.OpenText( FileName ) ) {
                    reader.ReadLine(); // header
                    while( !reader.EndOfStream ) {
                        string[] fields = reader.ReadLine().Split( ',' );
                        if( fields.Length == 17 ) {
                            try {
                                PlayerInfo info = new PlayerInfo( fields );
                                data.Add( info.name, info );
                            } catch( FormatException ex ) {
                                Logger.LogError( "PlayerDB.Load: Could not parse a record: " + ex.Message );
                            } catch( IOException ex ) {
                                Logger.LogError( "PlayerDB.Load: Error while trying to read from file: " + ex.Message );
                            }
                        }
                    }
                }
                Logger.Log( "PlayerDB.Save: Done loading player DB. " + data.Count + " records loaded." );
            } else {
                Logger.LogAlert( "PlayerDB.Load: No player DB file found." );
            }
        }

        public static void Save() {
            Logger.Log( "PlayerDB.Save: Saving player database (" + data.Count + ")" );
            string tempFile = FileName + (new Random()).Next().ToString();
            locker.EnterReadLock();
            using( StreamWriter writer = File.CreateText( tempFile ) ) {
                writer.WriteLine( Header );
                foreach( PlayerInfo entry in data.Values ) {
                    writer.WriteLine( entry.Serialize() );
                }
            }
            locker.ExitReadLock();
            File.Delete( FileName );
            File.Move( tempFile, FileName );
        }

        public static PlayerInfo GetPlayerInfo( Player player ) {
            locker.EnterWriteLock();
            if( data.ContainsKey( player.name ) ) {
                data[player.name].ProcessLogin( player );
            } else {
                PlayerInfo info = new PlayerInfo( player );
                data.Add( player.name, info );
            }
            locker.ExitWriteLock();
            return data[player.name];
        }

        public static void ProcessLogout( Player player ) {
            locker.EnterWriteLock();
            data[player.name].ProcessLogout( player );
            locker.ExitWriteLock();
        }
    }
}