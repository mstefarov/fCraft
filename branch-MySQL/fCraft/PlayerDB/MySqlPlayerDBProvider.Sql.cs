// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using Devart.Data.MySql;
using JetBrains.Annotations;

namespace fCraft {
    partial class MySqlPlayerDBProvider {

        const string MetadataTableName = "metadata";
        const string PlayersTableName = "players";
        const string RankMappingTableName = "rank_mapping";

        const string PreInsertQuery = "INSERT INTO " + PlayersTableName + "(id) VALUES(0);";
        const string LoadAllQuery = "SELECT * FROM " + PlayersTableName + " ORDER BY id;";
        const string FindExactQuery = "SELECT id FROM " + PlayersTableName + " WHERE name LIKE ? LIMIT 1;";
        const string FindByIPQuery = "SELECT id FROM " + PlayersTableName + " WHERE last_ip=? LIMIT ?;";
        const string FindPartialQuery = "SELECT id FROM " + PlayersTableName + " WHERE name LIKE ?;";
        const string DeleteCommandText = "DELETE FROM " + PlayersTableName + " WHERE id=? LIMIT 1;";

        const string LoadMetadataQuery = "SELECT * FROM " + MetadataTableName + ";";
        const string LoadRankMappingQuery = "SELECT * FROM " + RankMappingTableName + ";";
        
        const string UpdateQuery =
@"UPDATE " + PlayersTableName + @" SET
name=?,
displayed_name=?,
last_seen=?,
rank=?,
previous_rank=?,
rank_change_type=?,
rank_change_date=?,
rank_changed_by=?,
rank_change_reason=?,
ban_status=?,
ban_date=?,
banned_by=?,
ban_reason=?,
banned_until=?,
last_failed_login_date=?,
last_failed_login_ip=?,
unban_date=?,
unbanned_by=?,
unban_reason=?,
first_login_date=?,
last_login_date=?,
total_time=?,
blocks_built=?,
blocks_deleted=?,
blocks_drawn=?,
times_visited=?,
messages_written=?,
times_kicked_others=?,
times_banned_others=?,
times_kicked=?,
last_kick_date=?,
last_kick_by=?,
last_kick_reason=?,
is_frozen=?,
frozen_on=?,
frozen_by=?,
muted_until=?,
muted_by=?,
password=?,
last_modified=?,
is_online=?,
is_hidden=?,
last_ip=?,
leave_reason=?,
bandwidth_use_mode=?
WHERE id=? LIMIT 1;";

        const string MetadataTableSchema =
@"CREATE TABLE IF NOT EXISTS " + MetadataTableName + @" (
  format_version int(11) NOT NULL,
  server_version_string varchar(64) NOT NULL,
  last_modified int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=ascii COMMENT='Info about the origin and storage format.';";

        const string PlayersTableSchema =
@"CREATE TABLE IF NOT EXISTS " + PlayersTableName + @" (
  id int(11) NOT NULL AUTO_INCREMENT,
  name varchar(16) NOT NULL,
  displayed_name varchar(64) DEFAULT NULL,
  last_seen bigint(20) NOT NULL,
  rank smallint(6) NOT NULL,
  previous_rank smallint(6) NOT NULL,
  rank_change_type tinyint(4) NOT NULL,
  rank_change_date bigint(20) NOT NULL,
  rank_changed_by varchar(255) DEFAULT NULL,
  rank_change_reason varchar(1024) DEFAULT NULL,
  ban_status tinyint(4) NOT NULL,
  ban_date bigint(20) NOT NULL,
  banned_by varchar(255) DEFAULT NULL,
  ban_reason varchar(1024) DEFAULT NULL,
  banned_until bigint(20) NOT NULL,
  last_failed_login_date bigint(20) NOT NULL,
  last_failed_login_ip int(11) NOT NULL,
  unban_date bigint(20) NOT NULL,
  unbanned_by varchar(255) DEFAULT NULL,
  unban_reason varchar(1024) DEFAULT NULL,
  first_login_date bigint(20) NOT NULL,
  last_login_date bigint(20) NOT NULL,
  total_time int(11) NOT NULL,
  blocks_built int(11) NOT NULL,
  blocks_deleted int(11) NOT NULL,
  blocks_drawn bigint(20) NOT NULL,
  times_visited int(11) NOT NULL,
  messages_written int(11) NOT NULL,
  times_kicked_others int(11) NOT NULL,
  times_banned_others int(11) NOT NULL,
  times_kicked int(11) NOT NULL,
  last_kick_date int(11) NOT NULL,
  last_kick_by varchar(255) DEFAULT NULL,
  last_kick_reason varchar(1024) DEFAULT NULL,
  is_frozen tinyint(1) NOT NULL,
  frozen_on int(11) NOT NULL,
  frozen_by varchar(255) DEFAULT NULL,
  muted_until int(11) NOT NULL,
  muted_by varchar(255) DEFAULT NULL,
  password varchar(64) DEFAULT NULL,
  last_modified int(11) NOT NULL,
  is_online tinyint(1) NOT NULL,
  is_hidden tinyint(1) NOT NULL,
  last_ip int(11) NOT NULL,
  leave_reason tinyint(4) NOT NULL,
  bandwidth_use_mode tinyint(4) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY name (name),
  KEY last_ip (last_ip)
) ENGINE=InnoDB  DEFAULT CHARSET=ascii COMMENT='PlayerInfo records.' AUTO_INCREMENT=256;";

        const string RankMappingTableSchema =
@"CREATE TABLE IF NOT EXISTS `" + RankMappingTableName + @"` (
  `index` smallint(6) NOT NULL,
  `name` varchar(64) NOT NULL,
  PRIMARY KEY (`index`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=ascii COMMENT='Mapping of numeric rank indices to qualified rank names.';";

        const string InsertMetadataCommandText = "INSERT INTO " + MetadataTableName + " VALUES(?,?,?);";

        MySqlCommand findExactCommand,
                     findByIPCommand,
                     findPartialCommand,
                     deleteCommand,
                     preInsertCommand,
                     updateCommand;

        const int NameSize = 16,
                  DisplayedNameSize = 64,
                  ByFieldSize = 255,
                  ReasonFieldSize = 1024,
                  PasswordFieldSize = 64;

        const short NoRankIndex = -1;

        const MySqlType DateType = MySqlType.BigInt;


        void PrepareCommands() {
            findExactCommand = new MySqlCommand( FindExactQuery, connection );
            findExactCommand.Parameters.Add( "name", MySqlType.VarChar, 16 );
            findExactCommand.Prepare();

            findByIPCommand = new MySqlCommand( FindByIPQuery, connection );
            findByIPCommand.Parameters.Add( "lastIP", MySqlType.Int );
            findByIPCommand.Parameters.Add( "limit", MySqlType.Int );
            findByIPCommand.Prepare();

            findPartialCommand = new MySqlCommand( FindPartialQuery, connection );
            findPartialCommand.Parameters.Add( "partialName", MySqlType.VarChar, 16 );
            findPartialCommand.Prepare();

            deleteCommand = new MySqlCommand( DeleteCommandText, connection );
            deleteCommand.Parameters.Add( "id", MySqlType.Int );
            deleteCommand.Prepare();

            preInsertCommand = new MySqlCommand( PreInsertQuery, connection );
            preInsertCommand.Prepare();

            updateCommand = new MySqlCommand( UpdateQuery, connection );
            updateCommand.Parameters.Add( "Name", MySqlType.VarChar, NameSize );
            updateCommand.Parameters.Add( "DisplayedName", MySqlType.VarChar, DisplayedNameSize );
            updateCommand.Parameters.Add( "LastSeen", DateType );
            updateCommand.Parameters.Add( "Rank", MySqlType.SmallInt );
            updateCommand.Parameters.Add( "PreviousRank", MySqlType.SmallInt );
            updateCommand.Parameters.Add( "RankChangeType", MySqlType.TinyInt );
            updateCommand.Parameters.Add( "RankChangeDate", DateType );
            updateCommand.Parameters.Add( "RankChangedBy", MySqlType.VarChar, ByFieldSize );
            updateCommand.Parameters.Add( "RankChangeReason", MySqlType.VarChar, ReasonFieldSize );
            updateCommand.Parameters.Add( "BanStatus", MySqlType.TinyInt );
            updateCommand.Parameters.Add( "BanDate", DateType );
            updateCommand.Parameters.Add( "BannedBy", MySqlType.VarChar, ByFieldSize );
            updateCommand.Parameters.Add( "BanReason", MySqlType.VarChar, ReasonFieldSize );
            updateCommand.Parameters.Add( "BannedUntil", DateType );
            updateCommand.Parameters.Add( "LastFailedLoginDate", DateType );
            updateCommand.Parameters.Add( "LastFailedLoginIP", MySqlType.Int );
            updateCommand.Parameters.Add( "UnbanDate", DateType );
            updateCommand.Parameters.Add( "UnbannedBy", MySqlType.VarChar, ByFieldSize );
            updateCommand.Parameters.Add( "UnbanReason", MySqlType.VarChar, ReasonFieldSize );
            updateCommand.Parameters.Add( "FirstLoginDate", DateType );
            updateCommand.Parameters.Add( "LastLoginDate", DateType );
            updateCommand.Parameters.Add( "TotalTime", MySqlType.Int );
            updateCommand.Parameters.Add( "BlocksBuilt", MySqlType.Int );
            updateCommand.Parameters.Add( "BlocksDeleted", MySqlType.Int );
            updateCommand.Parameters.Add( "BlocksDrawn", MySqlType.BigInt );
            updateCommand.Parameters.Add( "TimesVisited", MySqlType.Int );
            updateCommand.Parameters.Add( "MessagesWritten", MySqlType.Int );
            updateCommand.Parameters.Add( "TimesKickedOthers", MySqlType.Int );
            updateCommand.Parameters.Add( "TimesBannedOthers", MySqlType.Int );
            updateCommand.Parameters.Add( "TimesKicked", MySqlType.Int );
            updateCommand.Parameters.Add( "LastKickDate", DateType );
            updateCommand.Parameters.Add( "LastKickBy", MySqlType.VarChar, ByFieldSize );
            updateCommand.Parameters.Add( "LastKickReason", MySqlType.VarChar, ReasonFieldSize );
            updateCommand.Parameters.Add( "IsFrozen", MySqlType.TinyInt, 1 );
            updateCommand.Parameters.Add( "FrozenOn", DateType );
            updateCommand.Parameters.Add( "FrozenBy", MySqlType.VarChar, ByFieldSize );
            updateCommand.Parameters.Add( "MutedUntil", DateType );
            updateCommand.Parameters.Add( "MutedBy", MySqlType.VarChar, ByFieldSize );
            updateCommand.Parameters.Add( "Password", MySqlType.VarChar, PasswordFieldSize );
            updateCommand.Parameters.Add( "LastModified", DateType );
            updateCommand.Parameters.Add( "IsOnline", MySqlType.TinyInt, 1 );
            updateCommand.Parameters.Add( "IsHidden", MySqlType.TinyInt, 1 );
            updateCommand.Parameters.Add( "LastIP", MySqlType.Int );
            updateCommand.Parameters.Add( "LeaveReason", MySqlType.TinyInt );
            updateCommand.Parameters.Add( "BandwidthUseMode", MySqlType.TinyInt );
            updateCommand.Parameters.Add( "ID", MySqlType.Int );
            updateCommand.Prepare();
        }

        const int FormatVersion = 0;

        void LoadSchema() {
            using( MySqlTransaction transaction = connection.BeginTransaction() ) {
                using( MySqlCommand createMetadataCmd = new MySqlCommand( MetadataTableSchema, connection, transaction ) ) {
                    createMetadataCmd.ExecuteNonQuery();
                }
                using( MySqlCommand loadMetadataCmd = new MySqlCommand( LoadMetadataQuery, connection, transaction ) ) {
                    using( MySqlDataReader reader = loadMetadataCmd.ExecuteReader() ) {
                        if( reader.Read() ) {
                            // read existing metadata
                            int workingFormatVersion = reader.GetInt32( 0 );
                            string workingServerVersionString = reader.GetString( 1 );
                            DateTime workingLastModified = reader.GetInt64( 2 ).ToDateTime();
                            Logger.Log( LogType.SystemActivity,
                                        "Loading PlayerDB (format {0}, generated by {1}, last modified {2})",
                                        workingFormatVersion,
                                        workingServerVersionString,
                                        workingLastModified.ToCompactString() );
                        } else {
                            // no metadata found - insert a row
                            Logger.Log( LogType.Warning,
                                        "No existing metadata record found in the specified database." );
                            GenerateSchema( transaction );
                        }
                    }
                }
                transaction.Commit();
            }
        }

        void GenerateSchema( MySqlTransaction transaction ) {
            using( MySqlCommand insertMetadataCmd = new MySqlCommand( InsertMetadataCommandText, connection, transaction ) ) {
                insertMetadataCmd.Parameters.Add( "format_version", MySqlType.Int );
                insertMetadataCmd.Parameters.Add( "server_version_string", MySqlType.VarChar, 255 );
                insertMetadataCmd.Parameters.Add( "last_modified", DateType );
                insertMetadataCmd.Parameters[0].Value = FormatVersion;
                insertMetadataCmd.Parameters[1].Value = Updater.CurrentRelease.VersionString;
                insertMetadataCmd.Parameters[2].Value = DateTime.UtcNow.ToUnixTime();
                insertMetadataCmd.ExecuteNonQuery();
            }
            using( MySqlCommand createPlayersCmd = new MySqlCommand( PlayersTableSchema, connection, transaction ) ) {
                createPlayersCmd.ExecuteNonQuery();
            }
            using( MySqlCommand createRankMappingCmd = new MySqlCommand( RankMappingTableSchema, connection, transaction ) ) {
                createRankMappingCmd.ExecuteNonQuery();
            }
        }


        [NotNull]
        MySqlCommand GetFindExactCommand( [NotNull] string fullName ) {
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            findExactCommand.Parameters[0].Value = fullName;
            return findExactCommand;
        }


        [NotNull]
        MySqlCommand GetFindByIPCommand( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            findByIPCommand.Parameters[0].Value = address.AsInt();
            findByIPCommand.Parameters[1].Value = limit;
            return findByIPCommand;
        }


        [NotNull]
        MySqlCommand GetFindPartialCommand( [NotNull] string partialName, int limit ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            findPartialCommand.Parameters[0].Value = partialName;
            findPartialCommand.Parameters[1].Value = limit;
            return findPartialCommand;
        }


        [NotNull]
        MySqlCommand GetDeleteCommand( int id ) {
            deleteCommand.Parameters[0].Value = id;
            return deleteCommand;
        }


        [NotNull]
        MySqlCommand GetUpdateCommand( PlayerInfo info ) {
            updateCommand.Parameters[(int)Field.Name - 1].Value = info.Name;
            updateCommand.Parameters[(int)Field.DisplayedName - 1].Value = info.DisplayedName;
            updateCommand.Parameters[(int)Field.LastSeen - 1].Value = info.LastSeen.ToUnixTime();

            updateCommand.Parameters[(int)Field.Rank - 1].Value = (short)info.Rank.Index;
            if( info.PreviousRank != null ) {
                updateCommand.Parameters[(int)Field.PreviousRank - 1].Value = (short)info.PreviousRank.Index;
            } else {
                updateCommand.Parameters[(int)Field.PreviousRank - 1].Value = NoRankIndex;
            }
            updateCommand.Parameters[(int)Field.RankChangeType - 1].Value = (sbyte)info.RankChangeType;
            updateCommand.Parameters[(int)Field.RankChangeDate - 1].Value = info.RankChangeDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.RankChangedBy - 1].Value = info.RankChangedBy;
            updateCommand.Parameters[(int)Field.RankChangeReason - 1].Value = info.RankChangeReason;

            updateCommand.Parameters[(int)Field.BanStatus - 1].Value = (sbyte)info.BanStatus;
            updateCommand.Parameters[(int)Field.BanDate - 1].Value = info.BanDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.BannedBy - 1].Value = info.BannedBy;
            updateCommand.Parameters[(int)Field.BanReason - 1].Value = info.BanReason;
            updateCommand.Parameters[(int)Field.BannedUntil - 1].Value = info.BannedUntil.ToUnixTime();
            updateCommand.Parameters[(int)Field.LastFailedLoginDate - 1].Value = info.LastFailedLoginDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.LastFailedLoginIP - 1].Value = info.LastFailedLoginIP.AsInt();
            updateCommand.Parameters[(int)Field.UnbanDate - 1].Value = info.UnbanDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.UnbannedBy - 1].Value = info.UnbannedBy;
            updateCommand.Parameters[(int)Field.UnbanReason - 1].Value = info.UnbanReason;

            updateCommand.Parameters[(int)Field.FirstLoginDate - 1].Value = info.FirstLoginDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.LastLoginDate - 1].Value = info.LastLoginDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.TotalTime - 1].Value = (int)info.TotalTime.ToSeconds();
            updateCommand.Parameters[(int)Field.BlocksBuilt - 1].Value = info.BlocksBuilt;
            updateCommand.Parameters[(int)Field.BlocksDeleted - 1].Value = info.BlocksDeleted;
            updateCommand.Parameters[(int)Field.BlocksDrawn - 1].Value = info.BlocksDrawn;
            updateCommand.Parameters[(int)Field.TimesVisited - 1].Value = info.TimesVisited;
            updateCommand.Parameters[(int)Field.MessagesWritten - 1].Value = info.MessagesWritten;
            updateCommand.Parameters[(int)Field.TimesKickedOthers - 1].Value = info.TimesKickedOthers;
            updateCommand.Parameters[(int)Field.TimesBannedOthers - 1].Value = info.TimesBannedOthers;

            updateCommand.Parameters[(int)Field.TimesKicked - 1].Value = info.TimesKicked;
            updateCommand.Parameters[(int)Field.LastKickDate - 1].Value = info.LastKickDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.LastKickBy - 1].Value = info.LastKickBy;
            updateCommand.Parameters[(int)Field.LastKickReason - 1].Value = info.LastKickReason;

            updateCommand.Parameters[(int)Field.IsFrozen - 1].Value = info.IsFrozen;
            updateCommand.Parameters[(int)Field.FrozenOn - 1 - 1].Value = info.FrozenOn.ToUnixTime();
            updateCommand.Parameters[(int)Field.FrozenBy - 1].Value = info.FrozenBy;
            updateCommand.Parameters[(int)Field.MutedUntil - 1].Value = info.MutedUntil.ToUnixTime();
            updateCommand.Parameters[(int)Field.MutedBy - 1].Value = info.MutedBy;

            updateCommand.Parameters[(int)Field.Password - 1].Value = info.Password;
            updateCommand.Parameters[(int)Field.LastModified - 1].Value = info.LastModified.ToUnixTime();
            updateCommand.Parameters[(int)Field.IsOnline - 1].Value = info.IsOnline;
            updateCommand.Parameters[(int)Field.IsHidden - 1].Value = info.IsHidden;
            updateCommand.Parameters[(int)Field.LastIP - 1].Value = info.LastIP.AsInt();
            updateCommand.Parameters[(int)Field.LeaveReason - 1].Value = (sbyte)info.LeaveReason;
            updateCommand.Parameters[(int)Field.BandwidthUseMode - 1].Value = (sbyte)info.BandwidthUseMode;

            // ID last
            updateCommand.Parameters[updateCommand.Parameters.Count - 1].Value = info.ID;
            return updateCommand;
        }


        enum Field {
            ID = 0,
            Name = 1,
            DisplayedName = 2,
            LastSeen = 3,
            Rank = 4,
            PreviousRank = 5,
            RankChangeType = 6,
            RankChangeDate = 7,
            RankChangedBy = 8,
            RankChangeReason = 9,
            BanStatus = 10,
            BanDate = 11,
            BannedBy = 12,
            BanReason = 13,
            BannedUntil = 14,
            LastFailedLoginDate = 15,
            LastFailedLoginIP = 16,
            UnbanDate = 17,
            UnbannedBy = 18,
            UnbanReason = 19,
            FirstLoginDate = 20,
            LastLoginDate = 21,
            TotalTime = 22,
            BlocksBuilt = 23,
            BlocksDeleted = 24,
            BlocksDrawn = 25,
            TimesVisited = 26,
            MessagesWritten = 27,
            TimesKickedOthers = 28,
            TimesBannedOthers = 29,
            TimesKicked = 30,
            LastKickDate = 31,
            LastKickBy = 32,
            LastKickReason = 33,
            IsFrozen = 34,
            FrozenOn = 35,
            FrozenBy = 36,
            MutedUntil = 37,
            MutedBy = 38,
            Password = 39,
            LastModified = 40,
            IsOnline = 41,
            IsHidden = 42,
            LastIP = 43,
            LeaveReason = 44,
            BandwidthUseMode = 45
        }
    }
}
