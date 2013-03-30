// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Devart.Data.MySql;
using JetBrains.Annotations;

namespace fCraft.MySql {
    // MySql-specific implementation details
    partial class MySqlPlayerDBProvider {

        #region Queries

        const string PreInsertQuery = "INSERT INTO `players`(`id`) VALUES(0);";
        const string LoadAllQuery = "SELECT * FROM `players` ORDER BY `id`;";
        const string FindExactQuery = "SELECT `id` FROM `players` WHERE `name` LIKE ? LIMIT 1;";
        const string FindByIPQuery = "SELECT `id` FROM `players` WHERE `last_ip`=? LIMIT ?;";
        const string FindPartialQuery = "SELECT `id` FROM `players` WHERE `name` LIKE ?;";
        const string DeleteCommandText = "DELETE FROM `players` WHERE `id`=? LIMIT 1;";
        const string ImportCommandText = "INSERT INTO `players` VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?);";

        const string LoadMetadataQuery = "SELECT * FROM `metadata`;";
        const string InsertMetadataCommandText = "INSERT INTO `metadata` VALUES(?,?,?);";

        const string LoadRankMappingQuery = "SELECT * FROM `rank_mapping`;";
        const string ListRanksQuery = "SELECT DISTINCT `rank` FROM `players`;";
        const string ListPreviousRanksQuery = "SELECT DISTINCT `previous_rank` FROM `players` WHERE `previous_rank`!=-1;";
        const string UpdateRankIndexCommandText = "UPDATE `players` SET `rank`=? WHERE `rank`=?;";
        const string UpdatePreviousRankIndexCommandText = "UPDATE `players` SET `previous_rank`=? WHERE `previous_rank`=?;";
        const string PermRankIncidesCommandText = "UPDATE `players` SET `rank`=-(`rank`+2);";
        const string PermPreviousRankIndicesCommandText = "UPDATE `players` SET `previous_rank`=-(`previous_rank`+2) WHERE `previous_rank`!=-1;";
        const string TruncateRankMappingCommandText = "TRUNCATE TABLE `rank_mapping`;";
        const string AddRankMappingCommandText = "INSERT INTO `rank_mapping`(`index`,`name`) VALUES(?,?);";
        
        const string UpdateCommandText =
@"UPDATE `players` SET
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
@"CREATE TABLE IF NOT EXISTS `metadata` (
  `format_version` int(11) NOT NULL,
  `server_version_string` varchar(64) NOT NULL,
  `last_modified` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=ascii COMMENT='Info about the origin and storage format.';";

        const string PlayersTableSchema =
@"CREATE TABLE IF NOT EXISTS `players` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(16) NOT NULL,
  `displayed_name` varchar(64) DEFAULT NULL,
  `last_seen` bigint(20) NOT NULL,
  `rank` smallint(6) NOT NULL,
  `previous_rank` smallint(6) NOT NULL,
  `rank_change_type` tinyint(4) NOT NULL,
  `rank_change_date` bigint(20) NOT NULL,
  `rank_changed_by` varchar(255) DEFAULT NULL,
  `rank_change_reason` varchar(1024) DEFAULT NULL,
  `ban_status` tinyint(4) NOT NULL,
  `ban_date` bigint(20) NOT NULL,
  `banned_by` varchar(255) DEFAULT NULL,
  `ban_reason` varchar(1024) DEFAULT NULL,
  `banned_until` bigint(20) NOT NULL,
  `last_failed_login_date` bigint(20) NOT NULL,
  `last_failed_login_ip` int(11) NOT NULL,
  `unban_date` bigint(20) NOT NULL,
  `unbanned_by` varchar(255) DEFAULT NULL,
  `unban_reason` varchar(1024) DEFAULT NULL,
  `first_login_date` bigint(20) NOT NULL,
  `last_login_date` bigint(20) NOT NULL,
  `total_time` int(11) NOT NULL,
  `blocks_built` int(11) NOT NULL,
  `blocks_deleted` int(11) NOT NULL,
  `blocks_drawn` bigint(20) NOT NULL,
  `times_visited` int(11) NOT NULL,
  `messages_written` int(11) NOT NULL,
  `times_kicked_others` int(11) NOT NULL,
  `times_banned_others` int(11) NOT NULL,
  `times_kicked` int(11) NOT NULL,
  `last_kick_date` int(11) NOT NULL,
  `last_kick_by` varchar(255) DEFAULT NULL,
  `last_kick_reason` varchar(1024) DEFAULT NULL,
  `is_frozen` tinyint(1) NOT NULL,
  `frozen_on` int(11) NOT NULL,
  `frozen_by` varchar(255) DEFAULT NULL,
  `muted_until` int(11) NOT NULL,
  `muted_by` varchar(255) DEFAULT NULL,
  `password` varchar(64) DEFAULT NULL,
  `last_modified` int(11) NOT NULL,
  `is_online` tinyint(1) NOT NULL,
  `is_hidden` tinyint(1) NOT NULL,
  `last_ip` int(11) NOT NULL,
  `leave_reason` tinyint(4) NOT NULL,
  `bandwidth_use_mode` tinyint(4) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`),
  KEY `last_ip` (`last_ip`)
) ENGINE=InnoDB DEFAULT CHARSET=ascii COMMENT='PlayerInfo records.' AUTO_INCREMENT=256;";

        const string RankMappingTableSchema =
@"CREATE TABLE IF NOT EXISTS `rank_mapping` (
  `index` smallint(6) NOT NULL,
  `name` varchar(64) NOT NULL,
  PRIMARY KEY (`index`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=ascii COMMENT='Mapping of numeric rank indices to qualified rank names.';";

        #endregion


        const int NameSize = 16,
                  DisplayedNameSize = 64,
                  ByFieldSize = 255,
                  ReasonFieldSize = 1024,
                  PasswordFieldSize = 64;
        const MySqlType DateType = MySqlType.BigInt;
        const int FormatVersion = 0;


        void CheckSchema() {
            using( MySqlTransaction transaction = connection.BeginTransaction() ) {
                // Create metadata table. Does noting if table already exists.
                transaction.ExecuteNonQuery( MetadataTableSchema );

                // load metadata
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

                // load the rank mapping
                var databaseRankMapping = new Dictionary<int, Rank>();
                using( MySqlCommand loadRankMappingCmd = new MySqlCommand( LoadRankMappingQuery, connection, transaction ) ) {
                    using( MySqlDataReader reader = loadRankMappingCmd.ExecuteReader() ) {
                        while( reader.Read() ) {
                            int index = reader.GetInt32( 0 );
                            string rankName = reader.GetString( 1 );
                            Rank rank = Rank.Parse( rankName );
                            if( rank == null ) {
                                rank = RankManager.DefaultRank;
                                Logger.Log( LogType.Warning,
                                            "MySqlPlayerDBProvider: Unrecognized rank \"{0}\". Any reference to this rank will be replaced with \"{1}\".",
                                            rankName, rank.Name );
                            }
                            databaseRankMapping.Add( index, rank );
                        }
                    }
                }

                // check if multiple indices refer to the same rank
                var indicesGroupedByRank = databaseRankMapping.GroupBy( pair => pair.Value );
                foreach( var indexGroup in indicesGroupedByRank.Where( group => group.Count() > 1 ) ) {
                    Logger.Log( LogType.Warning,
                                "MySqlPlayerDBProvider: Multiple incides ({0}) refer to the same rank ({1}) and will be merged.",
                                indexGroup.Select( pair => pair.Key ).JoinToString(),
                                indexGroup.Key.Name );
                }

                // enumerate all rank IDs in the database
                var allRankIDs = new HashSet<int>();
                using( MySqlCommand listRanksCmd = new MySqlCommand( ListRanksQuery, connection, transaction ) ) {
                    using( MySqlDataReader reader = listRanksCmd.ExecuteReader() ) {
                        while( reader.Read() ) {
                            allRankIDs.Add( reader.GetInt32( 0 ) );
                        }
                    }
                }
                using( MySqlCommand listPreviousRanksCmd = new MySqlCommand( ListPreviousRanksQuery, connection, transaction ) ) {
                    using( MySqlDataReader reader = listPreviousRanksCmd.ExecuteReader() ) {
                        while( reader.Read() ) {
                            allRankIDs.Add( reader.GetInt32( 0 ) );
                        }
                    }
                }

                // Replace any unknown rank indices with the default rank.
                var unknownRanks = allRankIDs.Except( databaseRankMapping.Keys );
                if( unknownRanks.Count() > 0 ) {
                    Logger.Log( LogType.Warning,
                                "MySqlPlayerDBProvider: Following unrecognized rank indices will be replaced with the default rank ({0}): {1}",
                                RankManager.DefaultRank.Name,
                                unknownRanks.JoinToString() );
                    foreach( int unknownIndex in unknownRanks ) {
                        databaseRankMapping.Add( unknownIndex, RankManager.DefaultRank );
                    }
                }

                // check if rank mappings are in sync
                rankMapping = RankManager.Ranks
                                         .OrderBy( rank => rank.Index )
                                         .ToDictionary( rank => rank.Index );

                if( !rankMapping.SequenceEqual( databaseRankMapping.OrderBy( pair => pair.Key ) ) ) {
                    Logger.Log( LogType.Warning,
                                "MySqlPlayerDBProvider: Updating database rank mapping..." );
                    RebuildRankMapping( databaseRankMapping, transaction );
                }

                // done loading (phew)
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
            transaction.ExecuteNonQuery( PlayersTableSchema );
            transaction.ExecuteNonQuery( RankMappingTableSchema );
        }


        #region Rank Mapping

        const short NoRankIndex = -1;
        Dictionary<int, Rank> rankMapping;


        [NotNull]
        Rank GetRankByIndex( int index ) {
            Rank rank;
            if( rankMapping.TryGetValue( index, out rank ) ) {
                return rank;
            } else {
                Logger.Log( LogType.Error,
                            "MySqlPlayerDBProvider.GetRankByIndex: Unknown rank index ({0}). Assigning rank {1} instead.",
                            index, RankManager.DefaultRank );
                return RankManager.DefaultRank;
            }
        }


        static int GetRankTempIndex( Rank rank ) {
            return -(rank.Index + 2);
        }


        void RebuildRankMapping( Dictionary<int, Rank> databaseRankMapping, MySqlTransaction transaction ) {
            // change all rank indices to temporary values
            using( MySqlCommand updateRankIndexCmd = new MySqlCommand( UpdateRankIndexCommandText, connection, transaction ) ) {
                updateRankIndexCmd.Parameters.Add( "newRank", MySqlType.SmallInt );
                updateRankIndexCmd.Parameters.Add( "oldRank", MySqlType.SmallInt );
                foreach( var pair in databaseRankMapping ) {
                    updateRankIndexCmd.Parameters[0].Value = GetRankTempIndex( pair.Value );
                    updateRankIndexCmd.Parameters[1].Value = pair.Key;
                    updateRankIndexCmd.ExecuteNonQuery();
                }
            }

            // change all rank indices to new permanent values
            transaction.ExecuteNonQuery( PermRankIncidesCommandText );

            // change all previous_rank indices to temporary values
            using( MySqlCommand updatePreviousRankIndexCmd = new MySqlCommand( UpdatePreviousRankIndexCommandText, connection, transaction ) ) {
                updatePreviousRankIndexCmd.Parameters.Add( "newRank", MySqlType.SmallInt );
                updatePreviousRankIndexCmd.Parameters.Add( "oldRank", MySqlType.SmallInt );
                foreach( var pair in databaseRankMapping ) {
                    updatePreviousRankIndexCmd.Parameters[0].Value = GetRankTempIndex( pair.Value );
                    updatePreviousRankIndexCmd.Parameters[1].Value = pair.Key;
                    updatePreviousRankIndexCmd.ExecuteNonQuery();
                }
            }

            // change all previous_rank indices to new permanent values
            transaction.ExecuteNonQuery( PermPreviousRankIndicesCommandText );

            // recreate the rank_mapping table
            transaction.ExecuteNonQuery( TruncateRankMappingCommandText );

            using( MySqlCommand addRankMappingCmd = new MySqlCommand( AddRankMappingCommandText, connection, transaction ) ) {
                addRankMappingCmd.Parameters.Add( "index", MySqlType.SmallInt );
                addRankMappingCmd.Parameters.Add( "name", MySqlType.VarChar, 64 );
                foreach( var pair in rankMapping ) {
                    addRankMappingCmd.Parameters[0].Value = pair.Key;
                    addRankMappingCmd.Parameters[1].Value = pair.Value.FullName;
                    addRankMappingCmd.ExecuteNonQuery();
                }
            }
        }

        #endregion


        #region Prepared Commands

        MySqlCommand findExactCommand,
                     findByIPCommand,
                     findPartialCommand,
                     deleteCommand,
                     preInsertCommand,
                     updateCommand,
                     importCommand;


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

            updateCommand = new MySqlCommand( UpdateCommandText, connection );
            AddInsertOrUpdateParams( updateCommand.Parameters );
            updateCommand.Parameters.Add( "ID", MySqlType.Int );
            updateCommand.Prepare();

            importCommand = new MySqlCommand( ImportCommandText, connection );
            importCommand.Parameters.Add( "ID", MySqlType.Int );
            AddInsertOrUpdateParams( importCommand.Parameters );
            importCommand.Prepare();
        }


        void AddInsertOrUpdateParams( MySqlParameterCollection paramCollection ) {
            paramCollection.Add( "Name", MySqlType.VarChar, NameSize );
            paramCollection.Add( "DisplayedName", MySqlType.VarChar, DisplayedNameSize );
            paramCollection.Add( "LastSeen", DateType );
            paramCollection.Add( "Rank", MySqlType.SmallInt );
            paramCollection.Add( "PreviousRank", MySqlType.SmallInt );
            paramCollection.Add( "RankChangeType", MySqlType.TinyInt );
            paramCollection.Add( "RankChangeDate", DateType );
            paramCollection.Add( "RankChangedBy", MySqlType.VarChar, ByFieldSize );
            paramCollection.Add( "RankChangeReason", MySqlType.VarChar, ReasonFieldSize );
            paramCollection.Add( "BanStatus", MySqlType.TinyInt );
            paramCollection.Add( "BanDate", DateType );
            paramCollection.Add( "BannedBy", MySqlType.VarChar, ByFieldSize );
            paramCollection.Add( "BanReason", MySqlType.VarChar, ReasonFieldSize );
            paramCollection.Add( "BannedUntil", DateType );
            paramCollection.Add( "LastFailedLoginDate", DateType );
            paramCollection.Add( "LastFailedLoginIP", MySqlType.Int );
            paramCollection.Add( "UnbanDate", DateType );
            paramCollection.Add( "UnbannedBy", MySqlType.VarChar, ByFieldSize );
            paramCollection.Add( "UnbanReason", MySqlType.VarChar, ReasonFieldSize );
            paramCollection.Add( "FirstLoginDate", DateType );
            paramCollection.Add( "LastLoginDate", DateType );
            paramCollection.Add( "TotalTime", MySqlType.Int );
            paramCollection.Add( "BlocksBuilt", MySqlType.Int );
            paramCollection.Add( "BlocksDeleted", MySqlType.Int );
            paramCollection.Add( "BlocksDrawn", MySqlType.BigInt );
            paramCollection.Add( "TimesVisited", MySqlType.Int );
            paramCollection.Add( "MessagesWritten", MySqlType.Int );
            paramCollection.Add( "TimesKickedOthers", MySqlType.Int );
            paramCollection.Add( "TimesBannedOthers", MySqlType.Int );
            paramCollection.Add( "TimesKicked", MySqlType.Int );
            paramCollection.Add( "LastKickDate", DateType );
            paramCollection.Add( "LastKickBy", MySqlType.VarChar, ByFieldSize );
            paramCollection.Add( "LastKickReason", MySqlType.VarChar, ReasonFieldSize );
            paramCollection.Add( "IsFrozen", MySqlType.TinyInt, 1 );
            paramCollection.Add( "FrozenOn", DateType );
            paramCollection.Add( "FrozenBy", MySqlType.VarChar, ByFieldSize );
            paramCollection.Add( "MutedUntil", DateType );
            paramCollection.Add( "MutedBy", MySqlType.VarChar, ByFieldSize );
            paramCollection.Add( "Password", MySqlType.VarChar, PasswordFieldSize );
            paramCollection.Add( "LastModified", DateType );
            paramCollection.Add( "IsOnline", MySqlType.TinyInt, 1 );
            paramCollection.Add( "IsHidden", MySqlType.TinyInt, 1 );
            paramCollection.Add( "LastIP", MySqlType.Int );
            paramCollection.Add( "LeaveReason", MySqlType.TinyInt );
            paramCollection.Add( "BandwidthUseMode", MySqlType.TinyInt );
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
        MySqlCommand GetUpdateCommand( [NotNull] PlayerInfo info ) {
            FillInsertOrUpdateParams( updateCommand.Parameters, info, -1 );
            // ID last
            updateCommand.Parameters[updateCommand.Parameters.Count - 1].Value = info.ID;
            return updateCommand;
        }


        [NotNull]
        MySqlCommand GetImportCommand( [NotNull] PlayerInfo info ) {
            // ID first
            importCommand.Parameters[(int)Field.ID].Value = info.ID;
            FillInsertOrUpdateParams( importCommand.Parameters, info, 0 );
            return importCommand;
        }


        void FillInsertOrUpdateParams( [NotNull] MySqlParameterCollection paramCollection, [NotNull] PlayerInfo info, int offset ) {
            paramCollection[(int)Field.Name + offset].Value = info.Name;
            paramCollection[(int)Field.DisplayedName + offset].Value = info.DisplayedName;
            paramCollection[(int)Field.LastSeen + offset].Value = info.LastSeen.ToUnixTime();

            paramCollection[(int)Field.Rank + offset].Value = (short)info.Rank.Index;
            if( info.PreviousRank != null ) {
                paramCollection[(int)Field.PreviousRank + offset].Value = (short)info.PreviousRank.Index;
            } else {
                paramCollection[(int)Field.PreviousRank + offset].Value = NoRankIndex;
            }
            paramCollection[(int)Field.RankChangeType + offset].Value = (sbyte)info.RankChangeType;
            paramCollection[(int)Field.RankChangeDate + offset].Value = info.RankChangeDate.ToUnixTime();
            paramCollection[(int)Field.RankChangedBy + offset].Value = info.RankChangedBy;
            paramCollection[(int)Field.RankChangeReason + offset].Value = info.RankChangeReason;

            paramCollection[(int)Field.BanStatus + offset].Value = (sbyte)info.BanStatus;
            paramCollection[(int)Field.BanDate + offset].Value = info.BanDate.ToUnixTime();
            paramCollection[(int)Field.BannedBy + offset].Value = info.BannedBy;
            paramCollection[(int)Field.BanReason + offset].Value = info.BanReason;
            paramCollection[(int)Field.BannedUntil + offset].Value = info.BannedUntil.ToUnixTime();
            paramCollection[(int)Field.LastFailedLoginDate + offset].Value = info.LastFailedLoginDate.ToUnixTime();
            paramCollection[(int)Field.LastFailedLoginIP + offset].Value = info.LastFailedLoginIP.AsInt();
            paramCollection[(int)Field.UnbanDate + offset].Value = info.UnbanDate.ToUnixTime();
            paramCollection[(int)Field.UnbannedBy + offset].Value = info.UnbannedBy;
            paramCollection[(int)Field.UnbanReason + offset].Value = info.UnbanReason;

            paramCollection[(int)Field.FirstLoginDate + offset].Value = info.FirstLoginDate.ToUnixTime();
            paramCollection[(int)Field.LastLoginDate + offset].Value = info.LastLoginDate.ToUnixTime();
            paramCollection[(int)Field.TotalTime + offset].Value = (int)info.TotalTime.ToSeconds();
            paramCollection[(int)Field.BlocksBuilt + offset].Value = info.BlocksBuilt;
            paramCollection[(int)Field.BlocksDeleted + offset].Value = info.BlocksDeleted;
            paramCollection[(int)Field.BlocksDrawn + offset].Value = info.BlocksDrawn;
            paramCollection[(int)Field.TimesVisited + offset].Value = info.TimesVisited;
            paramCollection[(int)Field.MessagesWritten + offset].Value = info.MessagesWritten;
            paramCollection[(int)Field.TimesKickedOthers + offset].Value = info.TimesKickedOthers;
            paramCollection[(int)Field.TimesBannedOthers + offset].Value = info.TimesBannedOthers;

            paramCollection[(int)Field.TimesKicked + offset].Value = info.TimesKicked;
            paramCollection[(int)Field.LastKickDate + offset].Value = info.LastKickDate.ToUnixTime();
            paramCollection[(int)Field.LastKickBy + offset].Value = info.LastKickBy;
            paramCollection[(int)Field.LastKickReason + offset].Value = info.LastKickReason;

            paramCollection[(int)Field.IsFrozen + offset].Value = info.IsFrozen;
            paramCollection[(int)Field.FrozenOn + offset + offset].Value = info.FrozenOn.ToUnixTime();
            paramCollection[(int)Field.FrozenBy + offset].Value = info.FrozenBy;
            paramCollection[(int)Field.MutedUntil + offset].Value = info.MutedUntil.ToUnixTime();
            paramCollection[(int)Field.MutedBy + offset].Value = info.MutedBy;

            paramCollection[(int)Field.Password + offset].Value = info.Password;
            paramCollection[(int)Field.LastModified + offset].Value = info.LastModified.ToUnixTime();
            paramCollection[(int)Field.IsOnline + offset].Value = info.IsOnline;
            paramCollection[(int)Field.IsHidden + offset].Value = info.IsHidden;
            paramCollection[(int)Field.LastIP + offset].Value = info.LastIP.AsInt();
            paramCollection[(int)Field.LeaveReason + offset].Value = (sbyte)info.LeaveReason;
            paramCollection[(int)Field.BandwidthUseMode + offset].Value = (sbyte)info.BandwidthUseMode;
        }

        #endregion


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

    static class MySqlUtils {
        public static void ExecuteNonQuery( this MySqlTransaction transaction, string commandText ) {
            using( MySqlCommand cmd = new MySqlCommand( commandText, transaction.Connection, transaction ) ) {
                cmd.ExecuteNonQuery();
            }
        }
    }
}