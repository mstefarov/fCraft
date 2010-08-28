using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SQLite;


namespace fCraft {
    static class PlayerDBv2 {
        static SQLiteConnection db;
        const string DatabaseFile = "fCraft.db";
        const int SchemaVersion = 1;

        internal static bool Init() {

            SQLiteConnectionStringBuilder connectionBuilder = new SQLiteConnectionStringBuilder();
            connectionBuilder.DataSource = DatabaseFile;

            db = new SQLiteConnection( connectionBuilder.ConnectionString );

            if( File.Exists( DatabaseFile ) ) {
                db.Open();
                using( SQLiteCommand cmd = db.CreateCommand() ) {
                    cmd.CommandText = "SELECT value FROM serverdata WHERE keygroup='PlayerDB' AND key='SchemaVersion'";
                    try {
                        using( SQLiteDataReader reader = cmd.ExecuteReader() ) {
                            if( reader.Read() ) {
                                int fileSchemaVersion = Int32.Parse( reader.GetString( 0 ) );
                                if( fileSchemaVersion < SchemaVersion ) {
                                    Logger.Log( "PlayerDB: Database schema is out of date.", LogType.Warning );
                                } else if( fileSchemaVersion > SchemaVersion ) {
                                    Logger.Log( "PlayerDB: Database schema was made for a newer version of fCraft. Please update.", LogType.FatalError );
                                    return false;
                                } else {
                                    Logger.Log( "PlayerDB: Database file loaded normally.", LogType.SystemActivity );
                                }
                            } else {
                                Logger.Log( "PlayerDB: Database schema version not found. Database may be corrupt.", LogType.FatalError );
                                return false;
                            }
                        }
                    } catch( SQLiteException ex ) {
                        Logger.Log( "PlayerDB: Could not read database version. Database may be corrupt. Error message: " + ex, LogType.FatalError );
                        return false;
                    }
                }
            } else {
                SQLiteConnection.CreateFile( DatabaseFile );
                db.Open();
                Logger.Log( "PlayerDB: Database file not found, creating new one.", LogType.Warning );
                DefineSchema();
                // TODO: import old data
            }
            return true;
        }


        static void DefineSchema() {
            using( SQLiteCommand cmd = db.CreateCommand() ) {
                using( SQLiteTransaction transaction = db.BeginTransaction() ) {
                    cmd.Transaction = transaction;
                    cmd.CommandText =
@"CREATE TABLE [bans] (
[active] BOOLEAN  NULL,
[target] INTEGER  NULL,
[banPlayer] INTEGER  NULL,
[banTimestamp] TIMESTAMP  NULL,
[banComment] VARCHAR(64)  NULL,
[banMethod] INTEGER  NULL,
[unbanPlayer] INTEGER  NULL,
[unbanTimestamp] TIMESTAMP  NULL,
[unbanComment] VARCHAR(64)  NULL,
[unbanMethod] INTEGER  NULL
);

CREATE TABLE [ipbans] (
[active] BOOLEAN  NULL,
[rangeStart] INTEGER  NULL,
[rangeEnd] INTEGER  NULL,
[banPlayer] INTEGER  NULL,
[banTimestamp] TIMESTAMP  NULL,
[banComment] VARCHAR(64)  NULL,
[banMethod] INTEGER  NULL,
[unbanPlayer] INTEGER  NULL,
[unbanTimestamp] TIMESTAMP  NULL,
[unbanComment] VARCHAR(64)  NULL,
[unbanMethod] INTEGER  NULL
);

CREATE TABLE [kicks] (
[player] INTEGER  NULL,
[target] INTEGER  NULL,
[timestamp] TIMESTAMP  NULL,
[comment] VARCHAR(64)  NULL
);

CREATE TABLE [log] (
[id] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
[type] INTEGER  NULL,
[subtype] INTEGER  NULL,
[source] INTEGER  NULL,
[timestamp] TIMESTAMP  NULL,
[message] TEXT  NULL
);

CREATE TABLE [playerdata] (
[player] INTEGER  NULL,
[keygroup] VARCHAR(32)  NULL,
[key] VARCHAR(32)  NULL,
[value] TEXT  NULL
);

CREATE TABLE [players] (
[id] INTEGER  PRIMARY KEY AUTOINCREMENT NOT NULL,
[name] VARCHAR(16)  UNIQUE NULL,
[state] INTEGER  NULL,
[rank] INTEGER  NULL,
[blocksPlaced] INTEGER  NULL,
[blocksDeleted] INTEGER  NULL,
[blocksDrawn] INTEGER  NULL,
[firstLogin] TIMESTAMP  NULL,
[lastLogin] TIMESTAMP  NULL,
[lastLogoff] TIMESTAMP  NULL,
[timeTotal] INTEGER  NULL,
[messagesWritten] INTEGER  NULL
);

CREATE TABLE [rankchanges] (
[target] INTEGER  NULL,
[player] INTEGER  NULL,
[oldRank] INTEGER  NULL,
[newRank] INTEGER  NULL,
[type] INTEGER  NULL,
[timestamp] TIMESTAMP  NULL,
[comment] VARCHAR(64)  NULL
);

CREATE TABLE [serverdata] (
[keygroup] VARCHAR(32)  NULL,
[key] VARCHAR(32)  NULL,
[value] TEXT  NULL
);

CREATE TABLE [sessions] (
[player] INTEGER  NULL,
[login] TIMESTAMP  NULL,
[logoff] TIMESTAMP  NULL,
[ip] INTEGER  NULL,
[blocksPlaced] INTEGER  NULL,
[blocksErased] INTEGER  NULL,
[messagesWritten] INTEGER  NULL,
[leaveReason] INTEGER  NULL,
[leaveEventId] INTEGER  NULL,
[geoip] VARCHAR(2)  NULL
);

CREATE INDEX idx_bans ON bans ( banPlayer );

CREATE INDEX idx_ipbans ON ipbans ( banPlayer );

CREATE INDEX idx_kicks ON kicks ( player );

CREATE INDEX [idx_log] ON [log](
[id]  ASC
);

CREATE INDEX [idx_playerdata] ON [playerdata](
[player]  ASC,
[key]  ASC
);

CREATE INDEX [idx_players_id] ON [players](
[id]  ASC
);

CREATE INDEX [idx_players_name] ON [players](
[name]  ASC
);

CREATE INDEX idx_rankchanges ON rankchanges ( target );

CREATE UNIQUE INDEX [idx_serverdata] ON [serverdata](
[key]  ASC
);

CREATE INDEX idx_sessions ON sessions ( player );

INSERT INTO serverdata VALUES ('PlayerDB','SchemaVersion'," + SchemaVersion + @");
";
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                }
            }
        }
    }
}