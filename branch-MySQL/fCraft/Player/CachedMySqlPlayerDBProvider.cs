using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Devart.Data;
using Devart.Data.MySql;
using System.Xml.Linq;

namespace fCraft {
    class CachedMySqlPlayerDBProvider : IPlayerDBProvider {
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

        public PlayerInfo AddPlayer( string name, IPAddress lastIP, Rank startingRank, RankChangeType rankChangeType ) {
            throw new NotImplementedException();
        }

        public PlayerInfo AddUnrecognizedPlayer( string name, Rank startingRank, RankChangeType rankChangeType ) {
            throw new NotImplementedException();
        }

        public PlayerInfo AddSuperPlayer( ReservedPlayerID id, string name, Rank rank ) {
            throw new NotImplementedException();
        }

        public void Remove( PlayerInfo playerInfo ) {
            throw new NotImplementedException();
        }

        public PlayerInfo FindExact( string fullName ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByIP( IPAddress address, int limit ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByPartialName( string partialName, int limit ) {
            throw new NotImplementedException();
        }

        public bool FindOneByPartialName( string partialName, out PlayerInfo result ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByPattern( string pattern, int limit ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> Load() {
            connection = new MySqlConnection();
            LoadConfig( Config.ProviderConfig );
            connection.Host = Host;
            connection.Port = Port;
            connection.Database = Database;
            connection.UserId = UserId;
            connection.Password = Password;
            connection.Open();

            MySqlCommand cmd = new MySqlCommand( "SELECT * FROM players ORDER BY id;", connection );
            using( MySqlDataReader reader = cmd.ExecuteReader() ) {
                while( reader.Read() ) {
                    yield return LoadInfo( reader );
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

        public void Save() {
            throw new NotImplementedException();
        }

        public void MassRankChange( Player player, Rank from, Rank to, string reason ) {
            throw new NotImplementedException();
        }

        public void SwapInfo( PlayerInfo player1, PlayerInfo player2 ) {
            throw new NotImplementedException();
        }

        static DateTime ReadDate( MySqlDataReader reader, Field field ) {
            return DateTimeUtil.ToDateTime( reader.GetInt64( (int)field ) );
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


        PlayerInfo LoadInfo( MySqlDataReader reader ) {
            int id = reader.GetInt32( (int)Field.ID );
            PlayerInfo info = new PlayerInfo( id );


            info.Name = reader.GetString( (int)Field.Name );
            info.DisplayedName = reader.GetString( (int)Field.DisplayedName );
            info.LastSeen = ReadDate(reader, Field.LastSeen );

            // Rank
            info.Rank = ReadRank(reader, Field.Rank );
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
                info.LastFailedLoginIP = ReadIPAddress(reader, Field.LastFailedLoginIP );
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