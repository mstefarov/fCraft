using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Net;


namespace fCraft {
    static class DB {
        static SQLiteConnection db;
        const string DatabaseFile = "fCraft.db";
        const int SchemaVersion = 1;
        static SQLiteCommand cmd_PlayerInfo_ProcessLogin,
                             cmd_PlayerInfo_ProcessLogout;

        internal static bool Init() {

            SQLiteConnectionStringBuilder connectionBuilder = new SQLiteConnectionStringBuilder();
            connectionBuilder.DataSource = DatabaseFile;

            db = new SQLiteConnection( connectionBuilder.ConnectionString );

            if( File.Exists( DatabaseFile ) ) {
                db.Open();
                using( SQLiteCommand cmd = db.CreateCommand() ) {
                    cmd.CommandText = "SELECT [Value] FROM [ServerData] WHERE [KeyGroup]='PlayerDB' AND [Key]='SchemaVersion'";
                    try {
                        using( SQLiteDataReader reader = cmd.ExecuteReader() ) {
                            if( reader.Read() ) {
                                int fileSchemaVersion = Int32.Parse( reader.GetString( 0 ) );
                                if( fileSchemaVersion < SchemaVersion ) {
                                    Logger.Log( "DB: Database schema is out of date.", LogType.Warning );
                                } else if( fileSchemaVersion > SchemaVersion ) {
                                    Logger.Log( "DB: Database schema was made for a newer version of fCraft. Please update.", LogType.FatalError );
                                    return false;
                                } else {
                                    Logger.Log( "DB: Database file loaded normally.", LogType.SystemActivity );
                                }
                            } else {
                                Logger.Log( "DB: Database schema version not found. Database may be corrupt.", LogType.FatalError );
                                return false;
                            }
                        }
                    } catch( SQLiteException ex ) {
                        Logger.Log( "DB: Could not read database version. Database may be corrupt. Error message: " + ex, LogType.FatalError );
                        return false;
                    }
                }
            } else {
                SQLiteConnection.CreateFile( DatabaseFile );
                db.Open();
                Logger.Log( "DB: Database file not found, creating new one.", LogType.Warning );
                DefineSchema();
                // TODO: import old data
            }

            try {
                cmd_PlayerInfo_ProcessLogin = db.CreateCommand();
                cmd_PlayerInfo_ProcessLogin.CommandText = @"
UPDATE [Players]
SET [LastIP] = @LastIP,
    [LastLoginDate] = @LastLoginDate,
    [LastSeen] = @LastSeen,
    [TimesVisited] = [TimesVisited]+1
WHERE [ID] = @ID;
";
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@LastIP", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@LastLoginDate", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@LastSeen", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@ID", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Prepare();

                cmd_PlayerInfo_ProcessLogout = db.CreateCommand();
                cmd_PlayerInfo_ProcessLogout.CommandText = @"
BEGIN;
UPDATE [Players] SET [LastSeen]=@LastSeen, [TotalTimeOnServer]=[TotalTimeOnServer]+@SessionDuration WHERE ID=@ID;
INSERT INTO [Sessions] VALUES( @ID, @Login, @LastSeen, @IP, @BlocksPlaced, @BlocksDeleted, @BlocksDrawn, @MessagesWritten, @LeaveReason, @GeoIP );
";
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@LastSeen", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@SessionDuration", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@ID", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@IP", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@BlocksPlaced", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@BlocksDeleted", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@BlocksDrawn", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@MessagesWritten", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@LeaveReason", DbType.Int32 ) );
                cmd_PlayerInfo_ProcessLogin.Parameters.Add( new SQLiteParameter( "@GeoIP", DbType.String ) );
                cmd_PlayerInfo_ProcessLogout.Prepare();

                return true;

            } catch( SQLiteException ex ) {
                Logger.Log( "DB: Could not prepare database queries: " + ex, LogType.FatalError );
                return false;
            }
        }

        // TODO: clean up, this is unsafe
        public static void QueuePlayerInfoUpdate( PlayerInfo2 info, string field, object value ) {
            ExecuteNonQuery( "UPDATE [Players] SET [" + field + "]=\"" + value.ToString() + "\" WHERE [ID]=" + info.ID );
        }

        internal static void ExecuteNonQuery( string command ) {
            using( SQLiteCommand cmd = db.CreateCommand() ) {
                cmd.CommandText = command;
                cmd.ExecuteNonQuery();
            }
        }

        static void DefineSchema() {
            using( SQLiteCommand cmd = db.CreateCommand() ) {
                cmd.CommandText = @"
BEGIN;

CREATE TABLE [Bans] (
[Active] BOOLEAN  NULL,
[Target] INTEGER  NULL,
[BanPlayer] INTEGER  NULL,
[BanTimestamp] TIMESTAMP  NULL,
[BanReason] VARCHAR(64)  NULL,
[BanMethod] INTEGER  NULL,
[UnbanPlayer] INTEGER  NULL,
[UnbanTimestamp] TIMESTAMP  NULL,
[UnbanReason] VARCHAR(64)  NULL,
[UnbanMethod] INTEGER  NULL
);

CREATE TABLE [IPBans] (
[Active] BOOLEAN  NULL,
[RangeStart] INTEGER  NULL,
[RangeEnd] INTEGER  NULL,
[BanPlayer] INTEGER  NULL,
[BanTimestamp] TIMESTAMP  NULL,
[BanReason] VARCHAR(64)  NULL,
[BanMethod] INTEGER  NULL,
[UnbanPlayer] INTEGER  NULL,
[UnbanTimestamp] TIMESTAMP  NULL,
[UnbanComment] VARCHAR(64)  NULL,
[UnbanMethod] INTEGER  NULL
);

CREATE TABLE [Kicks] (
[Player] INTEGER  NULL,
[Target] INTEGER  NULL,
[Timestamp] TIMESTAMP  NULL,
[Reason] VARCHAR(64)  NULL
);

CREATE TABLE [Log] (
[ID] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
[Type] INTEGER  NULL,
[Subtype] INTEGER  NULL,
[Source] INTEGER  NULL,
[Timestamp] TIMESTAMP  NULL,
[Message] TEXT  NULL
);

CREATE TABLE [PlayerData] (
[Player] INTEGER  NULL,
[KeyGroup] VARCHAR(32)  NULL,
[Key] VARCHAR(32)  NULL,
[Value] TEXT  NULL
);

CREATE TABLE [Players] (
[ID] INTEGER  PRIMARY KEY AUTOINCREMENT NOT NULL,
[Name] VARCHAR(16)  UNIQUE NULL,
[State] INTEGER  NULL,
[Rank] INTEGER  NULL,
[BlocksPlaced] INTEGER  NULL,
[BlocksDeleted] INTEGER  NULL,
[BlocksDrawn] INTEGER  NULL,
[FirstLogin] TIMESTAMP  NULL,
[LastLogin] TIMESTAMP  NULL,
[LastSeen] TIMESTAMP  NULL,
[TimeTotal] INTEGER  NULL,
[MessagesWritten] INTEGER  NULL
);

CREATE TABLE [RankChanges] (
[Target] INTEGER  NULL,
[Player] INTEGER  NULL,
[OldRank] INTEGER  NULL,
[NewRank] INTEGER  NULL,
[Type] INTEGER  NULL,
[Timestamp] TIMESTAMP  NULL,
[Comment] VARCHAR(64)  NULL
);

CREATE TABLE [ServerData] (
[KeyGroup] VARCHAR(32)  NULL,
[Key] VARCHAR(32)  NULL,
[Value] TEXT  NULL
);

CREATE TABLE [Sessions] (
[Player] INTEGER  NULL,
[Start] TIMESTAMP  NULL,
[End] TIMESTAMP  NULL,
[IP] INTEGER  NULL,
[BlocksPlaced] INTEGER  NULL,
[BlocksDeleted] INTEGER  NULL,
[BlocksDrawn] INTEGER  NULL,
[MessagesWritten] INTEGER  NULL,
[LeaveReason] INTEGER  NULL,
[GeoIP] VARCHAR(2)  NULL
);

CREATE TABLE [ClassMapping] (
[Index] INTEGER  NOT NULL PRIMARY KEY,
[ClassID] VARCHAR(33)  NULL
);

CREATE INDEX [iBans] ON [Bans] ( [BanPlayer] );

CREATE INDEX [iIPBans] ON [IPBans] ( [BanPlayer] );

CREATE INDEX [iKicks] ON [Kicks] ( [Player] );

CREATE INDEX [iLog] ON [Log](
[ID]  ASC
);

CREATE INDEX [iPlayerData] ON [PlayerData](
[Player]  ASC,
[Key]  ASC
);

CREATE INDEX [iPlayers_ID] ON [Players](
[ID]  ASC
);

CREATE INDEX [iPlayers_Name] ON [Players](
[Name]  ASC
);

CREATE INDEX [iRankChanges] ON [RankChanges] ( [Target] );

CREATE UNIQUE INDEX [iServerData] ON [ServerData](
[Key]  ASC
);

CREATE INDEX [iSessions] ON [Sessions] ( [Player] );

INSERT INTO [ServerData] VALUES ('PlayerDB','SchemaVersion'," + SchemaVersion + @");

COMMIT;
";
                cmd.ExecuteNonQuery();
            }
        }


        #region Utilities
        static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );

        public static int DateTimeToTimestamp( DateTime timestamp ) {
            return (int)(timestamp - UnixEpoch).TotalSeconds;
        }

        public static DateTime TimestampToDateTime( int timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }


        public static int IPAddressToInt32( IPAddress ipAddress ) {
            return BitConverter.ToInt32( ipAddress.GetAddressBytes().Reverse().ToArray(), 0 );
        }

        public static IPAddress Int32ToIPAddress( int ipAddress ) {
            return new IPAddress( BitConverter.GetBytes( ipAddress ).Reverse().ToArray() );
        }

        #endregion


        #region Parametrized Queries

        public static void ProcessLogin( PlayerInfo2 info ) {
            lock( cmd_PlayerInfo_ProcessLogin ) {
                cmd_PlayerInfo_ProcessLogin.Parameters["@LastIP"].Value = DB.IPAddressToInt32( info.LastIP );
                cmd_PlayerInfo_ProcessLogin.Parameters["@LastLoginDate"].Value = DB.DateTimeToTimestamp( info.LastLoginDate );
                cmd_PlayerInfo_ProcessLogin.Parameters["@LastSeen"].Value = DB.DateTimeToTimestamp( info.LastSeen );
                cmd_PlayerInfo_ProcessLogin.Parameters["@ID"].Value = info.ID;
                cmd_PlayerInfo_ProcessLogin.ExecuteNonQuery();
            }
        }

        public static void ProcessLogout( PlayerInfo2 info ) {
            lock( cmd_PlayerInfo_ProcessLogout ) {
                cmd_PlayerInfo_ProcessLogin.Parameters["@LastSeen"].Value = DB.DateTimeToTimestamp( info.LastSeen );
                cmd_PlayerInfo_ProcessLogin.Parameters["@SessionDuration"].Value = (int)info.LastSessionDuration.TotalSeconds;
                cmd_PlayerInfo_ProcessLogin.Parameters["@ID"].Value = info.ID;
                cmd_PlayerInfo_ProcessLogin.Parameters["@IP"].Value = DB.IPAddressToInt32( info.LastIP );
                cmd_PlayerInfo_ProcessLogin.Parameters["@BlocksPlaced"].Value = info.BlocksPlacedLastSession;
                cmd_PlayerInfo_ProcessLogin.Parameters["@BlocksDeleted"].Value = info.BlocksDeletedLastSession;
                cmd_PlayerInfo_ProcessLogin.Parameters["@BlocksDrawn"].Value = info.BlocksDrawnLastSession;
                cmd_PlayerInfo_ProcessLogin.Parameters["@MessagesWritten"].Value = info.MessagesWrittenLastSession;
                cmd_PlayerInfo_ProcessLogin.Parameters["@LeaveReason"].Value = info.LastLeaveReason.ToString();
                cmd_PlayerInfo_ProcessLogin.Parameters["@GeoIP"].Value = ""; // todo: geoip
                cmd_PlayerInfo_ProcessLogin.ExecuteNonQuery();
            }
        }

        #endregion
    }
}