using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Devart.Data.MySql;
using JetBrains.Annotations;

namespace fCraft {
    internal sealed partial class MySqlPlayerDBProvider : IPlayerDBProvider {
        MySqlConnection connection;

        public const string Name = "MySql";

        readonly object syncRoot = new object();
        public object SyncRoot {
            get { return syncRoot; }
        }

        public string Host { get; private set; }
        public int Port { get; private set; }
        public string Database { get; private set; }
        public string UserId { get; private set; }
        public string Password { get; private set; }


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

                    transaction.Commit();

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

                    transaction.Commit();

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



        public void MassRankChange( Player player, Rank from, Rank to, string reason ) {
            throw new NotImplementedException();
        }


        public void SwapInfo( PlayerInfo player1, PlayerInfo player2 ) {
            throw new NotImplementedException();
        }


        #region Loading

        public IEnumerable<PlayerInfo> Load() {
            connection = new MySqlConnection();
            LoadConfig( Config.PlayerDBProviderConfig );
            connection.Host = Host;
            connection.Port = Port;
            connection.Database = Database;
            connection.UserId = UserId;
            connection.Password = Password;
            connection.Open();

            LoadSchema();

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


        void LoadConfig( XContainer el ) {
            if( el == null ) {
                throw new Exception( "MySqlPlayerDBProvider: No configuration specified in config.xml" );
            }
            XElement hostEl = el.Element( "Host" );
            if( hostEl == null || hostEl.Value == null ) {
                throw new Exception( "MySqlPlayerDBProvider: No host specified in config.xml" );
            }
            Host = hostEl.Value;

            XElement portEl = el.Element( "Port" );
            if( portEl == null || portEl.Value == null ) {
                throw new Exception( "MySqlPlayerDBProvider: No port specified in config.xml" );
            }
            Port = Int32.Parse( portEl.Value );

            XElement databaseEl = el.Element( "Database" );
            if( databaseEl == null || databaseEl.Value == null ) {
                throw new Exception( "MySqlPlayerDBProvider: No database specified in config.xml" );
            }
            Database = databaseEl.Value;

            XElement userIdEl = el.Element( "UserId" );
            if( userIdEl == null || userIdEl.Value == null ) {
                throw new Exception( "MySqlPlayerDBProvider: No user id specified in config.xml" );
            }
            UserId = userIdEl.Value;

            XElement passwordEl = el.Element( "Password" );
            if( passwordEl == null || passwordEl.Value == null ) {
                throw new Exception( "MySqlPlayerDBProvider: No password specified in config.xml" );
            }
            Password = passwordEl.Value;
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


        public void Save() {
            lock( syncRoot ) {
                var playersToUpdate = PlayerDB.PlayerInfoList.Where( p => p.Changed );
                using( MySqlTransaction transaction = connection.BeginTransaction() ) {
                    MySqlCommand cmd = null;
                    foreach( PlayerInfo info in playersToUpdate ) {
                        lock( info.SyncRoot ) {
                            info.Changed = false;
                            cmd = GetUpdateCommand( info );
                        }
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                    }
                    if( cmd != null ) {
                        transaction.Commit();
                        cmd.Transaction = null;
                    }
                }
            }
        }


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