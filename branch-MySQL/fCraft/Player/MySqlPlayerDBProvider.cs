using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
using Devart.Data.MySql;
using System.Xml.Linq;
using JetBrains.Annotations;
using System.Linq;

namespace fCraft {
    class MySqlPlayerDBProvider : IPlayerDBProvider {
        MySqlConnection connection;

        readonly object syncRoot = new object();
        public object SyncRoot {
            get { return syncRoot; }
        }

        public string Host { get; private set; }
        public int Port { get; private set; }
        public string Database { get; private set; }
        public string UserId { get; private set; }
        public string Password { get; private set; }


        #region SQL

        const string PreInsertQuery = "INSERT INTO players(id) VALUES(0);";
        const string LoadAllQuery = "SELECT * FROM players ORDER BY id;";
        const string FindExactQuery = "SELECT id FROM players WHERE name LIKE ? LIMIT 1;";
        const string FindByIPQuery = "SELECT id FROM players WHERE lastIP=? LIMIT ?;";
        const string FindPartialQuery = "SELECT id FROM players WHERE name LIKE ?;";
        const string DeleteCommandText = "DELETE FROM players WHERE id=? LIMIT 1;";

        const string UpdateQuery = "UPDATE players SET " +
                                    "name=?,displayedName=?,lastSeen=?," +
                                   "rank=?,previousRank=?,rankChangeType=?,rankChangeDate=?,rankChangedBy=?,rankChangeReason=?," +
                                   "banStatus=?,banDate=?,bannedBy=?,banReason=?,bannedUntil=?,lastFailedLoginDate=?,lastFailedLoginIP=?," +
                                   "unbanDate=?,unbannedBy=?,unbanReason=?," +
                                   "firstLoginDate=?,lastLoginDate=?,totalTime=?,blocksBuilt=?,blocksDeleted=?,blocksDrawn=?," +
                                   "timesVisited=?,messagesWritten=?,timesKickedOthers=?,timesBannedOthers=?," +
                                   "timesKicked=?,lastKickDate=?,lastKickBy=?,lastKickReason=?," +
                                   "isFrozen=?,frozenOn=?,frozenBy=?,mutedUntil=?,mutedBy=?," +
                                   "password=?,lastModified=?,isOnline=?,isHidden=?,lastIP=?,leaveReason=?,bandwidthUseMode=? " +
                                   "WHERE id=? LIMIT 1;";


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

        const int NoRankIndex = 1;

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
            AddParamsForAllFieldsExceptID( updateCommand );
            updateCommand.Parameters.Add( "ID", MySqlType.Int );
            updateCommand.Prepare();
        }


        void AddParamsForAllFieldsExceptID( [NotNull] MySqlCommand cmd ) {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            cmd.Parameters.Add( "Name", MySqlType.VarChar, NameSize );
            cmd.Parameters.Add( "DisplayedName", MySqlType.VarChar, DisplayedNameSize );
            cmd.Parameters.Add( "LastSeen", DateType );
            cmd.Parameters.Add( "Rank", MySqlType.SmallInt );
            cmd.Parameters.Add( "PreviousRank", MySqlType.SmallInt );
            cmd.Parameters.Add( "RankChangeType", MySqlType.TinyInt );
            cmd.Parameters.Add( "RankChangeDate", DateType );
            cmd.Parameters.Add( "RankChangedBy", MySqlType.VarChar, ByFieldSize );
            cmd.Parameters.Add( "RankChangeReason", MySqlType.VarChar, ReasonFieldSize );
            cmd.Parameters.Add( "BanStatus", MySqlType.TinyInt );
            cmd.Parameters.Add( "BanDate", DateType );
            cmd.Parameters.Add( "BannedBy", MySqlType.VarChar, ByFieldSize );
            cmd.Parameters.Add( "BanReason", MySqlType.VarChar, ReasonFieldSize );
            cmd.Parameters.Add( "BannedUntil", DateType );
            cmd.Parameters.Add( "LastFailedLoginDate", DateType );
            cmd.Parameters.Add( "LastFailedLoginIP", MySqlType.Int );
            cmd.Parameters.Add( "UnbanDate", DateType );
            cmd.Parameters.Add( "UnbannedBy", MySqlType.VarChar, ByFieldSize );
            cmd.Parameters.Add( "UnbanReason", MySqlType.VarChar, ByFieldSize );
            cmd.Parameters.Add( "FirstLoginDate", DateType );
            cmd.Parameters.Add( "LastLoginDate", DateType );
            cmd.Parameters.Add( "TotalTime", MySqlType.Int );
            cmd.Parameters.Add( "BlocksBuilt", MySqlType.Int );
            cmd.Parameters.Add( "BlocksDeleted", MySqlType.Int );
            cmd.Parameters.Add( "BlocksDrawn", MySqlType.BigInt );
            cmd.Parameters.Add( "TimesVisited", MySqlType.Int );
            cmd.Parameters.Add( "MessagesWritten", MySqlType.Int );
            cmd.Parameters.Add( "TimesKickedOthers", MySqlType.Int );
            cmd.Parameters.Add( "TimesBannedOthers", MySqlType.Int );
            cmd.Parameters.Add( "TimesKicked", MySqlType.Int );
            cmd.Parameters.Add( "LastKickDate", DateType );
            cmd.Parameters.Add( "LastKickBy", MySqlType.VarChar, ByFieldSize );
            cmd.Parameters.Add( "LastKickReason", MySqlType.VarChar, ReasonFieldSize );
            cmd.Parameters.Add( "IsFrozen", MySqlType.TinyInt, 1 );
            cmd.Parameters.Add( "FrozenOn", DateType );
            cmd.Parameters.Add( "FrozenBy", MySqlType.VarChar, ByFieldSize );
            cmd.Parameters.Add( "MutedUntil", DateType );
            cmd.Parameters.Add( "MutedBy", MySqlType.VarChar, ByFieldSize );
            cmd.Parameters.Add( "Password", MySqlType.VarChar, PasswordFieldSize );
            cmd.Parameters.Add( "LastModified", DateType );
            cmd.Parameters.Add( "IsOnline", MySqlType.TinyInt, 1 );
            cmd.Parameters.Add( "IsHidden", MySqlType.TinyInt, 1 );
            cmd.Parameters.Add( "LastIP", MySqlType.Int );
            cmd.Parameters.Add( "LeaveReason", MySqlType.TinyInt );
            cmd.Parameters.Add( "BandwidthUseMode", MySqlType.TinyInt );
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

            updateCommand.Parameters[(int)Field.Rank - 1].Value = info.Rank.Index;
            if( info.PreviousRank != null ) {
                updateCommand.Parameters[(int)Field.PreviousRank - 1].Value = info.PreviousRank.Index;
            } else {
                updateCommand.Parameters[(int)Field.PreviousRank - 1].Value = NoRankIndex;
            }
            updateCommand.Parameters[(int)Field.RankChangeType - 1].Value = (byte)info.RankChangeType;
            updateCommand.Parameters[(int)Field.RankChangeDate - 1].Value = info.RankChangeDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.RankChangedBy - 1].Value = info.RankChangedBy;
            updateCommand.Parameters[(int)Field.RankChangeReason - 1].Value = info.RankChangeReason;

            updateCommand.Parameters[(int)Field.BanStatus - 1].Value = (byte)info.BanStatus;
            updateCommand.Parameters[(int)Field.BanDate - 1].Value = info.BanDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.BannedBy - 1].Value = info.BannedBy;
            updateCommand.Parameters[(int)Field.BanReason - 1].Value = info.BanReason;
            updateCommand.Parameters[(int)Field.BannedUntil - 1].Value = info.BannedUntil;
            updateCommand.Parameters[(int)Field.LastFailedLoginDate - 1].Value = info.LastFailedLoginDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.LastFailedLoginIP - 1].Value = info.LastFailedLoginIP.AsInt();
            updateCommand.Parameters[(int)Field.UnbanDate - 1].Value = info.UnbanDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.UnbannedBy - 1].Value = info.UnbannedBy;
            updateCommand.Parameters[(int)Field.UnbanReason - 1].Value = info.UnbanReason;

            updateCommand.Parameters[(int)Field.FirstLoginDate - 1].Value = info.FirstLoginDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.LastLoginDate - 1].Value = info.LastLoginDate.ToUnixTime();
            updateCommand.Parameters[(int)Field.TotalTime - 1].Value = info.TotalTime.ToSeconds();
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
            updateCommand.Parameters[(int)Field.LeaveReason - 1].Value = (byte)info.LeaveReason;
            updateCommand.Parameters[(int)Field.BandwidthUseMode - 1].Value = (byte)info.BandwidthUseMode;

            // ID last
            updateCommand.Parameters[(int)Field.BandwidthUseMode].Value = 0;
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

        #endregion


        [NotNull]
        public PlayerInfo AddPlayer( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            lock( syncRoot ) {
                using( MySqlTransaction transaction = connection.BeginTransaction() ) {
                    preInsertCommand.Transaction = transaction;
                    preInsertCommand.ExecuteNonQuery();
                    int id = (int)preInsertCommand.InsertId;

                    PlayerInfo info = new PlayerInfo( id, name, lastIP, startingRank, rankChangeType );

                    MySqlCommand updateCmd = GetUpdateCommand( info );
                    updateCmd.Transaction = transaction;
                    updateCmd.ExecuteNonQuery();

                    preInsertCommand.Transaction = null;
                    updateCmd.Transaction = null;
                    return info;
                }
            }
        }


        [NotNull]
        public PlayerInfo AddUnrecognizedPlayer( [NotNull] string name, [NotNull] Rank startingRank, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            lock( syncRoot ) {
                using( MySqlTransaction transaction = connection.BeginTransaction() ) {
                    preInsertCommand.Transaction = transaction;
                    preInsertCommand.ExecuteNonQuery();
                    int id = (int)preInsertCommand.InsertId;

                    PlayerInfo info = new PlayerInfo( id, name, IPAddress.None, startingRank, rankChangeType );

                    MySqlCommand updateCmd = GetUpdateCommand( info );
                    updateCmd.Transaction = transaction;
                    updateCmd.ExecuteNonQuery();

                    preInsertCommand.Transaction = null;
                    updateCmd.Transaction = null;
                    return info;
                }
            }
        }


        public bool Remove( [NotNull] PlayerInfo playerInfo ) {
            if( playerInfo == null ) throw new ArgumentNullException( "playerInfo" );
            lock( syncRoot ) {
                MySqlCommand cmd = GetDeleteCommand( playerInfo.ID );
                int rowsAffected = cmd.ExecuteNonQuery();
                return (rowsAffected > 0);
            }
        }


        [CanBeNull]
        public PlayerInfo FindExact( [NotNull] string fullName ) {
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            lock( syncRoot ) {
                MySqlCommand cmd = GetFindExactCommand( fullName );
                object playerIdOrNull = cmd.ExecuteScalar();
                if( playerIdOrNull == null ) {
                    return null;
                } else {
                    int id = (int)playerIdOrNull;
                    return GetPlayerInfoFromID( id );
                }
            }
        }


        [NotNull]
        public IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            lock( syncRoot ) {
                MySqlCommand cmd = GetFindByIPCommand( address, limit );
                List<PlayerInfo> results = new List<PlayerInfo>();
                using( MySqlDataReader reader = cmd.ExecuteReader() ) {
                    while( reader.Read() ) {
                        int id = reader.GetInt32( 0 );
                        results.Add( GetPlayerInfoFromID( id ) );
                    }
                }
                return results;
            }
        }


        [NotNull]
        public IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string partialName, int limit ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );

            lock( syncRoot ) {
                MySqlCommand cmdExact = GetFindExactCommand( partialName );
                object playerIdOrNull = cmdExact.ExecuteScalar();

                if( playerIdOrNull != null ) {
                    // An exact match was found, return it
                    int id = (int)playerIdOrNull;
                    return new[] {
                        GetPlayerInfoFromID( id )
                    };
                }

                MySqlCommand cmdPartial = GetFindPartialCommand( partialName + "%", limit );
                using( MySqlDataReader reader = cmdPartial.ExecuteReader() ) {
                    List<PlayerInfo> results = new List<PlayerInfo>();
                    while( reader.Read() ) {
                        // If multiple matches were found, they'll be added to the list
                        int id = reader.GetInt32( 0 );
                        results.Add( GetPlayerInfoFromID( id ) );
                    }
                    // If no matches were found, the list will be empty
                    return results;
                }
            }
        }


        public bool FindOneByPartialName( [NotNull] string partialName, [CanBeNull] out PlayerInfo result ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );

            lock( syncRoot ) {
                MySqlCommand cmdExact = GetFindExactCommand( partialName );
                object playerIdOrNull = cmdExact.ExecuteScalar();

                if( playerIdOrNull != null ) {
                    // An exact match was found, return it
                    int id = (int)playerIdOrNull;
                    result = GetPlayerInfoFromID( id );
                    return true;
                }

                MySqlCommand cmdPartial = GetFindPartialCommand( partialName + "%", 2 );
                using( MySqlDataReader reader = cmdPartial.ExecuteReader() ) {
                    if( !reader.Read() ) {
                        // zero matches found
                        result = null;
                        return true;
                    }
                    int id = reader.GetInt32( 0 );
                    if( !reader.Read() ) {
                        // one partial match found
                        result = GetPlayerInfoFromID( id );
                        return true;
                    }
                    // multiple partial matches found
                    result = null;
                    return false;
                }
            }
        }


        [NotNull]
        public IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern, int limit ) {
            if( pattern == null ) throw new ArgumentNullException( "pattern" );
            string processedPattern = pattern.Replace( "_", "\\_" ) // escape underscores
                                             .Replace( '*', '%' ) // zero-or-more-characters wildcard
                                             .Replace( '?', '_' ); // single-character wildcard

            lock( syncRoot ) {
                MySqlCommand cmdPartial = GetFindPartialCommand( processedPattern, limit );
                using( MySqlDataReader reader = cmdPartial.ExecuteReader() ) {
                    List<PlayerInfo> results = new List<PlayerInfo>();
                    while( reader.Read() ) {
                        int id = reader.GetInt32( 0 );
                        results.Add( GetPlayerInfoFromID( id ) );
                    }
                    return results;
                }
            }
        }


        public void Save() {
            lock( syncRoot ) {
                var playersToUpdate = PlayerDB.PlayerInfoList.Where( p => p.Changed );
                using( MySqlTransaction transaction = connection.BeginTransaction() ) {
                    MySqlCommand cmd = null;
                    foreach( PlayerInfo info in playersToUpdate ) {
                        cmd = GetUpdateCommand( info );
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                    }
                    if( cmd != null ) {
                        cmd.Transaction = null;
                    }
                }
            }
        }


        public void MassRankChange( Player player, Rank from, Rank to, string reason ) {
            throw new NotImplementedException();
        }


        public void SwapInfo( PlayerInfo player1, PlayerInfo player2 ) {
            throw new NotImplementedException();
        }


        #region Loading

        public IEnumerable<PlayerInfo> Load() {
            connection = new MySqlConnection();
            LoadConfig( Config.ProviderConfig );
            connection.Host = Host;
            connection.Port = Port;
            connection.Database = Database;
            connection.UserId = UserId;
            connection.Password = Password;
            connection.Open();

            PrepareCommands();

            rankMapping = new Dictionary<int, Rank>();

            using( MySqlCommand cmd = new MySqlCommand( LoadAllQuery, connection ) ) {
                using( MySqlDataReader reader = cmd.ExecuteReader() ) {
                    while( reader.Read() ) {
                        yield return LoadInfo( reader );
                    }
                }
            }
        }


        void LoadConfig( XElement el ) {
            Host = el.Element( "Host" ).Value;
            Port = Int32.Parse( el.Element( "Port" ).Value );
            Database = el.Element( "Database" ).Value;
            UserId = el.Element( "UserId" ).Value;
            Password = el.Element( "Password" ).Value;
        }


        static PlayerInfo LoadInfo( MySqlDataReader reader ) {
            int id = reader.GetInt32( (int)Field.ID );
            // ReSharper disable UseObjectOrCollectionInitializer
            PlayerInfo info = new PlayerInfo( id );
            // ReSharper restore UseObjectOrCollectionInitializer

            info.Name = reader.GetString( (int)Field.Name );
            info.DisplayedName = reader.GetString( (int)Field.DisplayedName );
            info.LastSeen = ReadDate( reader, Field.LastSeen );

            // Rank
            info.Rank = ReadRank( reader, Field.Rank );
            info.PreviousRank = ReadRank( reader, Field.PreviousRank );

            info.RankChangeType = (RankChangeType)reader.GetByte( (int)Field.RankChangeType );
            if( info.RankChangeType != RankChangeType.Default ) {
                info.RankChangeDate = ReadDate( reader, Field.RankChangeDate );
                info.RankChangedBy = reader.GetString( (int)Field.RankChangedBy );
                info.RankChangeReason = reader.GetString( (int)Field.RankChangeReason );
            }

            // Bans
            info.BanStatus = (BanStatus)reader.GetByte( (int)Field.BanStatus );
            info.BanDate = ReadDate( reader, Field.BanDate );
            info.BannedBy = reader.GetString( (int)Field.BannedBy );
            info.BanReason = reader.GetString( (int)Field.BanReason );
            if( info.BanStatus == BanStatus.Banned ) {
                info.BannedUntil = ReadDate( reader, Field.BannedUntil );
                info.LastFailedLoginDate = ReadDate( reader, Field.LastFailedLoginDate );
                info.LastFailedLoginIP = ReadIPAddress( reader, Field.LastFailedLoginIP );
            } else {
                info.UnbanDate = ReadDate( reader, Field.UnbanDate );
                info.UnbannedBy = reader.GetString( (int)Field.UnbannedBy );
                info.UnbanReason = reader.GetString( (int)Field.UnbanReason );
            }

            // Stats
            info.FirstLoginDate = ReadDate( reader, Field.FirstLoginDate );
            info.LastLoginDate = ReadDate( reader, Field.LastLoginDate );
            info.TotalTime = ReadTimeSpan( reader, Field.TotalTime );
            info.BlocksBuilt = reader.GetInt32( (int)Field.BlocksBuilt );
            info.BlocksDeleted = reader.GetInt32( (int)Field.BlocksDeleted );
            info.BlocksDrawn = reader.GetInt64( (int)Field.BlocksDrawn );
            info.TimesVisited = reader.GetInt32( (int)Field.TimesVisited );
            info.MessagesWritten = reader.GetInt32( (int)Field.MessagesWritten );
            info.TimesKickedOthers = reader.GetInt32( (int)Field.TimesKickedOthers );
            info.TimesBannedOthers = reader.GetInt32( (int)Field.TimesBannedOthers );

            // Kicks
            info.TimesKicked = reader.GetInt32( (int)Field.TimesKicked );
            if( info.TimesKicked > 0 ) {
                info.LastKickDate = ReadDate( reader, Field.LastKickDate );
                info.LastKickBy = reader.GetString( (int)Field.LastKickBy );
                info.LastKickReason = reader.GetString( (int)Field.LastKickReason );
            }

            // Freeze/Mute
            info.IsFrozen = reader.GetBoolean( (int)Field.IsFrozen );
            if( info.IsFrozen ) {
                info.FrozenOn = ReadDate( reader, Field.FrozenOn );
                info.FrozenBy = reader.GetString( (int)Field.FrozenBy );
            }
            info.MutedUntil = ReadDate( reader, Field.MutedUntil );
            if( info.MutedUntil != DateTime.MinValue ) {
                info.MutedBy = reader.GetString( (int)Field.MutedBy );
            }

            // Misc
            info.Password = reader.GetString( (int)Field.Password );
            info.LastModified = ReadDate( reader, Field.LastModified );
            // skip Field.IsOnline
            info.IsHidden = reader.GetBoolean( (int)Field.IsHidden );
            info.LastIP = ReadIPAddress( reader, Field.LastIP );
            info.LeaveReason = (LeaveReason)reader.GetByte( (int)Field.LeaveReason );
            info.BandwidthUseMode = (BandwidthUseMode)reader.GetByte( (int)Field.BandwidthUseMode );
            return info;
        }


        static DateTime ReadDate( MySqlDataReader reader, Field field ) {
            return reader.GetInt64( (int)field ).ToDateTime();
        }


        static Rank ReadRank( MySqlDataReader reader, Field field ) {
            return Rank.Parse( reader.GetString( (int)field ) );
        }


        static IPAddress ReadIPAddress( MySqlDataReader reader, Field field ) {
            return IPAddress.Parse( reader.GetString( (int)field ) );
        }


        static TimeSpan ReadTimeSpan( MySqlDataReader reader, Field field ) {
            return new TimeSpan( reader.GetInt32( (int)field ) * TimeSpan.TicksPerSecond );
        }

        #endregion


        static PlayerInfo GetPlayerInfoFromID( int id ) {
            PlayerInfo result = PlayerDB.FindPlayerInfoByID( id );
            if( result == null ) {
                throw new DataException( "Player id " + id + " was found, but no corresponding PlayerInfo exists." );
            }
            return result;
        }

        
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
    }
}