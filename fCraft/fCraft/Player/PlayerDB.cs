// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;


namespace fCraft {
    public class PlayerDB {
        StringTree tree = new StringTree();
        List<PlayerInfo> list = new List<PlayerInfo>();

        public const string FileName = "PlayerDB.txt",
                            Header = "playerName,lastIP,playerClass,classChangeDate,classChangedBy,"+
                                     "banStatus,banDate,bannedBy,unbanDate,unbannedBy,"+
                                     "firstLoginDate,lastLoginDate,lastFailedLoginDate,"+
                                     "lastFailedLoginIP,failedLoginCount,totalTimeOnServer," +
                                     "blocksBuilt,blocksDeleted,timesVisited,"+
                                     "linesWritten,thanksReceived,warningsReceived";

        public ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        World world;

        internal PlayerDB( World _world ) {
            world = _world;
        }

        public void Load() {
            if( File.Exists( FileName ) ) {
                using( StreamReader reader = File.OpenText( FileName ) ) {
                    reader.ReadLine(); // header
                    while( !reader.EndOfStream ) {
                        string[] fields = reader.ReadLine().Split( ',' );
                        if( fields.Length == PlayerInfo.fieldCount ) {
                            try {
                                PlayerInfo info = new PlayerInfo(world, fields );
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


        public void Save() {
            Logger.Log( "PlayerDB.Save: Saving player database ({0} records).", LogType.Debug, tree.Count() );
            string tempFile = FileName + (new Random()).Next().ToString();
            locker.EnterReadLock();
            using( StreamWriter writer = File.CreateText( tempFile ) ) {
                writer.WriteLine( Header );
                foreach( PlayerInfo entry in list ) {
                    writer.WriteLine( entry.Serialize() );
                }
            }
            locker.ExitReadLock();
            File.Delete( FileName );
            File.Move( tempFile, FileName );
        }


        public PlayerInfo FindPlayerInfo( Player player ) {
            if( player == null ) return null;
            
            locker.EnterWriteLock();
            PlayerInfo info = tree.Get( player.name );
            if( info == null ) {
                info = new PlayerInfo( world, player );
                tree.Add( player.name, info );
                list.Add( info );
            }
            locker.ExitWriteLock();
            return info;
        }


        public List<PlayerInfo> FindPlayersByIP( IPAddress address ) {
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


        public bool FindPlayerInfo( string name, out PlayerInfo info ) {
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


        public PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) return null;

            locker.EnterWriteLock();
            PlayerInfo info = tree.Get( name );
            locker.ExitWriteLock();

            return info;
        }

        internal void ProcessLogout( Player player ) {
            if( player == null ) return;
            locker.EnterWriteLock();
            tree.Get( player.name ).ProcessLogout( player );
            locker.ExitWriteLock();
        }
    }
}