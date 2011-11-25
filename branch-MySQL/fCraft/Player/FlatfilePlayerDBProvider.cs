using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using JetBrains.Annotations;

namespace fCraft {
    public class FlatfilePlayerDBProvider : IPlayerDBProvider {

        public IEnumerable<PlayerInfo> Load() {
            throw new NotImplementedException();
        }

        public void Add( PlayerInfo playerInfo ) {
            throw new NotImplementedException();
        }

        public void Remove( PlayerInfo playerInfo ) {
            throw new NotImplementedException();
        }

        public void PullChanges( params PlayerInfo[] playerInfo ) {
            throw new NotImplementedException();
        }

        public void PushChanges( params PlayerInfo[] playerInfo ) {
            throw new NotImplementedException();
        }

        public PlayerInfo FindExact( string fullName ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByIP( System.Net.IPAddress address, int limit ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByPartialName( string partialName, int limit ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByPattern( string pattern, int limit ) {
            throw new NotImplementedException();
        }

        public void MassRankChange( Player player, Rank from, Rank to, string reason ) {
            throw new NotImplementedException();
        }

        public void SwapInfo( PlayerInfo player1, PlayerInfo player2 ) {
            throw new NotImplementedException();
        }


        #region Loading

        internal static PlayerInfo LoadFormat2( string[] fields ) {
            int id = Int32.Parse( fields[29] );
            if( id < 256 ) id = PlayerDB.GetNextID();

            PlayerInfo info = new PlayerInfo( id ) { name = fields[0] };

            if( fields[1].Length > 0 ) {
                IPAddress.TryParse( fields[1], out info.lastIP );
            }

            info.rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTime( out info.rankChangeDate );
            if( fields[4].Length > 0 ) info.rankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.banStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.banStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.banStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTime( out info.banDate ) ) {
                if( fields[7].Length > 0 ) info.bannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.banReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTime( out info.unbanDate ) ) {
                if( fields[9].Length > 0 ) info.unbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.unbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTime( out info.lastFailedLoginDate );

            if( fields[13].Length > 1 ) {
                IPAddress.TryParse( fields[13], out info.lastFailedLoginIP );
            }
            // skip 14

            // login/logout dates
            fields[15].ToDateTime( out info.firstLoginDate );
            fields[16].ToDateTime( out info.lastLoginDate );
            fields[17].ToTimeSpan( out info.totalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.blocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.blocksDeleted );
            Int32.TryParse( fields[20], out info.timesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.messagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.previousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.rankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.timesKicked );
            Int32.TryParse( fields[27], out info.timesKickedOthers );
            Int32.TryParse( fields[28], out info.timesBannedOthers );
            // fields[29] is ID, read above

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.rankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTime( out info.lastKickDate );
            if( !fields[32].ToDateTime( out info.lastSeen ) || info.lastSeen < info.lastLoginDate ) {
                info.lastSeen = info.lastLoginDate;
            }
            Int64.TryParse( fields[33], out info.blocksDrawn );

            if( fields[34].Length > 0 ) info.lastKickBy = Unescape( fields[34] );
            if( fields[35].Length > 0 ) info.lastKickReason = Unescape( fields[35] );

            fields[36].ToDateTime( out info.bannedUntil );
            info.isFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.frozenBy = Unescape( fields[38] );
            fields[39].ToDateTime( out info.frozenOn );
            fields[40].ToDateTime( out info.mutedUntil );
            if( fields[41].Length > 0 ) info.mutedBy = Unescape( fields[41] );
            info.password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.bandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.bandwidthUseMode = BandwidthUseMode.Default;
                }
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.isHidden = false;
                } else {
                    info.isHidden = info.Rank.Can( Permission.Hide );
                }
            }
            if( fields.Length > 46 ) {
                DateTime tempLastModified;
                fields[46].ToDateTime( out tempLastModified );
                info.LastModified = tempLastModified;
            }
            if( fields.Length > 47 && fields[47].Length > 0 ) {
                info.displayedName = Unescape( fields[47] );
            }

            if( info.lastSeen < info.firstLoginDate ) {
                info.lastSeen = info.firstLoginDate;
            }
            if( info.lastLoginDate < info.firstLoginDate ) {
                info.lastLoginDate = info.firstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat1( string[] fields ) {
            int id = Int32.Parse( fields[29] );
            if( id < 256 ) id = PlayerDB.GetNextID();

            PlayerInfo info = new PlayerInfo( id ) { name = fields[0] };

            if( fields[1].Length > 0 ) {
                IPAddress.TryParse( fields[1], out info.lastIP );
            }

            info.rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTimeLegacy( out info.rankChangeDate );
            if( fields[4].Length > 0 ) info.rankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.banStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.banStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.banStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTimeLegacy( out info.banDate ) ) {
                if( fields[7].Length > 0 ) info.bannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.banReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTimeLegacy( out info.unbanDate ) ) {
                if( fields[9].Length > 0 ) info.unbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.unbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTimeLegacy( out info.lastFailedLoginDate );

            if( fields[13].Length > 1 ) {
                IPAddress.TryParse( fields[13], out info.lastFailedLoginIP );
            }
            // skip 14

            // login/logout times
            fields[15].ToDateTimeLegacy( out info.firstLoginDate );
            fields[16].ToDateTimeLegacy( out info.lastLoginDate );
            fields[17].ToTimeSpanLegacy( ref info.totalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.blocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.blocksDeleted );
            Int32.TryParse( fields[20], out info.timesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.messagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.previousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.rankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.timesKicked );
            Int32.TryParse( fields[27], out info.timesKickedOthers );
            Int32.TryParse( fields[28], out info.timesBannedOthers );
            // fields[29] is ID, read above

            int rankChangeTypeCode;
            if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.rankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTimeLegacy( out info.lastKickDate );
            if( !fields[32].ToDateTimeLegacy( out info.lastSeen ) || info.lastSeen < info.lastLoginDate ) {
                info.lastSeen = info.lastLoginDate;
            }
            Int64.TryParse( fields[33], out info.blocksDrawn );

            if( fields[34].Length > 0 ) info.lastKickBy = Unescape( fields[34] );
            if( fields[34].Length > 0 ) info.lastKickReason = Unescape( fields[35] );

            fields[36].ToDateTimeLegacy( out info.bannedUntil );
            info.isFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.frozenBy = Unescape( fields[38] );
            fields[39].ToDateTimeLegacy( out info.frozenOn );
            fields[40].ToDateTimeLegacy( out info.mutedUntil );
            if( fields[41].Length > 0 ) info.mutedBy = Unescape( fields[41] );
            info.password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            int bandwidthUseModeCode;
            if( Int32.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.bandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.bandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.bandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.isHidden = false;
                } else {
                    info.isHidden = info.Rank.Can( Permission.Hide );
                }
            }

            if( info.lastSeen < info.firstLoginDate ) {
                info.lastSeen = info.firstLoginDate;
            }
            if( info.lastLoginDate < info.firstLoginDate ) {
                info.lastLoginDate = info.firstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat0( string[] fields ) {
            DateTime tempDateTime;

            // get ID
            int id;
            if( fields.Length > 29 ) {
                if( !Int32.TryParse( fields[29], out id ) || id < 256 ) {
                    id = PlayerDB.GetNextID();
                }
            } else {
                id = PlayerDB.GetNextID();
            }

            PlayerInfo info = new PlayerInfo( id ) { name = fields[0] };

            if( fields[1].Length > 1 ) {
                IPAddress.TryParse( fields[1], out info.lastIP );
            }

            info.rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTimeUtil.TryParseLocalDate( fields[3], out info.rankChangeDate );
            if( fields[4].Length > 0 ) {
                info.rankChangedBy = fields[4];
                if( info.rankChangedBy == "-" ) info.rankChangedBy = null;
            }

            switch( fields[5] ) {
                case "b":
                    info.banStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.banStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.banStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( DateTimeUtil.TryParseLocalDate( fields[6], out info.banDate ) ) {
                if( fields[7].Length > 0 ) info.bannedBy = fields[7];
                if( fields[10].Length > 0 ) {
                    info.banReason = UnescapeOldFormat( fields[10] );
                    if( info.banReason == "-" ) info.banReason = null;
                }
            }

            // unban information
            if( DateTimeUtil.TryParseLocalDate( fields[8], out info.unbanDate ) ) {
                if( fields[9].Length > 0 ) info.unbannedBy = fields[9];
                if( fields[11].Length > 0 ) {
                    info.unbanReason = UnescapeOldFormat( fields[11] );
                    if( info.unbanReason == "-" ) info.unbanReason = null;
                }
            }

            // failed logins
            if( fields[12].Length > 1 ) {
                DateTimeUtil.TryParseLocalDate( fields[12], out info.lastFailedLoginDate );
            }
            if( fields[13].Length > 1 ) {
                IPAddress.TryParse( fields[13], out info.lastFailedLoginIP );
            }
            // skip 14

            // login/logout times
            DateTimeUtil.TryParseLocalDate( fields[15], out tempDateTime );
            info.firstLoginDate = tempDateTime;
            DateTimeUtil.TryParseLocalDate( fields[16], out tempDateTime );
            info.lastLoginDate = tempDateTime;
            TimeSpan.TryParse( fields[17], out info.totalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.blocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.blocksDeleted );
            Int32.TryParse( fields[20], out info.timesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.messagesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) info.previousRank = Rank.Parse( fields[24] );
                if( fields[25].Length > 0 ) info.rankChangeReason = UnescapeOldFormat( fields[25] );
                Int32.TryParse( fields[26], out info.timesKicked );
                Int32.TryParse( fields[27], out info.timesKickedOthers );
                Int32.TryParse( fields[28], out info.timesBannedOthers );
                // fields[29] (id) already read/assigned by this point
                if( fields.Length > 29 ) {
                    int rankChangeTypeCode;
                    if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        info.rankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            info.GuessRankChangeType();
                        }
                    } else {
                        info.GuessRankChangeType();
                    }
                    DateTimeUtil.TryParseLocalDate( fields[31], out info.lastKickDate );
                    if( !DateTimeUtil.TryParseLocalDate( fields[32], out info.lastSeen ) || info.lastSeen < info.lastLoginDate ) {
                        info.lastSeen = info.lastLoginDate;
                    }
                    Int64.TryParse( fields[33], out info.blocksDrawn );

                    if( fields[34].Length > 0 ) info.lastKickBy = UnescapeOldFormat( fields[34] );
                    if( fields[35].Length > 0 ) info.lastKickReason = UnescapeOldFormat( fields[35] );

                } else {
                    info.GuessRankChangeType();
                    info.lastSeen = info.lastLoginDate;
                }

                if( fields.Length > 36 ) {
                    DateTimeUtil.TryParseLocalDate( fields[36], out info.bannedUntil );
                    info.isFrozen = (fields[37] == "f");
                    if( fields[38].Length > 0 ) info.frozenBy = UnescapeOldFormat( fields[38] );
                    DateTimeUtil.TryParseLocalDate( fields[39], out info.frozenOn );
                    DateTimeUtil.TryParseLocalDate( fields[40], out info.mutedUntil );
                    if( fields[41].Length > 0 ) info.mutedBy = UnescapeOldFormat( fields[41] );
                    info.password = UnescapeOldFormat( fields[42] );
                    // fields[43] is "online", and is ignored
                }

                if( fields.Length > 44 ) {
                    if( fields[44].Length != 0 ) {
                        info.bandwidthUseMode = (BandwidthUseMode)Int32.Parse( fields[44] );
                    }
                }
            }

            if( info.lastSeen < info.firstLoginDate ) {
                info.lastSeen = info.firstLoginDate;
            }
            if( info.lastLoginDate < info.firstLoginDate ) {
                info.lastLoginDate = info.firstLoginDate;
            }

            if( info.rankChangeDate != DateTime.MinValue ) info.rankChangeDate = info.rankChangeDate.ToUniversalTime();
            if( info.banDate != DateTime.MinValue ) info.banDate = info.banDate.ToUniversalTime();
            if( info.unbanDate != DateTime.MinValue ) info.unbanDate = info.unbanDate.ToUniversalTime();
            if( info.lastFailedLoginDate != DateTime.MinValue ) info.lastFailedLoginDate = info.lastFailedLoginDate.ToUniversalTime();
            if( info.firstLoginDate != DateTime.MinValue ) info.firstLoginDate = info.firstLoginDate.ToUniversalTime();
            if( info.lastLoginDate != DateTime.MinValue ) info.lastLoginDate = info.lastLoginDate.ToUniversalTime();
            if( info.lastKickDate != DateTime.MinValue ) info.lastKickDate = info.lastKickDate.ToUniversalTime();
            if( info.lastSeen != DateTime.MinValue ) info.lastSeen = info.lastSeen.ToUniversalTime();
            if( info.bannedUntil != DateTime.MinValue ) info.bannedUntil = info.bannedUntil.ToUniversalTime();
            if( info.frozenOn != DateTime.MinValue ) info.frozenOn = info.frozenOn.ToUniversalTime();
            if( info.mutedUntil != DateTime.MinValue ) info.mutedUntil = info.mutedUntil.ToUniversalTime();

            return info;
        }


        void GuessRankChangeType() {
            if( previousRank != null ) {
                if( rankChangeReason == "~AutoRank" || rankChangeReason == "~AutoRankAll" || rankChangeReason == "~MassRank" ) {
                    if( previousRank > rank ) {
                        rankChangeType = RankChangeType.AutoDemoted;
                    } else if( previousRank < rank ) {
                        rankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( previousRank > rank ) {
                        rankChangeType = RankChangeType.Demoted;
                    } else if( previousRank < rank ) {
                        rankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                rankChangeType = RankChangeType.Default;
            }
        }


        internal static PlayerInfo LoadBinaryFormat0( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            int id = Read7BitEncodedInt( reader );
            // ReSharper disable UseObjectOrCollectionInitializer
            PlayerInfo info = new PlayerInfo( id );
            // ReSharper restore UseObjectOrCollectionInitializer

            // General
            info.name = reader.ReadString();
            info.displayedName = ReadString( reader );
            info.lastSeen = DateTimeUtil.ToDateTime( reader.ReadUInt32() );

            // Rank
            int rankIndex = Read7BitEncodedInt( reader );
            info.rank = PlayerDB.GetRankByIndex( rankIndex );
            {
                bool hasPrevRank = reader.ReadBoolean();
                if( hasPrevRank ) {
                    int prevRankIndex = Read7BitEncodedInt( reader );
                    info.rank = PlayerDB.GetRankByIndex( prevRankIndex );
                }
            }
            info.rankChangeType = (RankChangeType)reader.ReadByte();
            if( info.rankChangeType != RankChangeType.Default ) {
                info.rankChangeDate = ReadDate( reader );
                info.rankChangedBy = ReadString( reader );
                info.rankChangeReason = ReadString( reader );
            }

            // Bans
            info.banStatus = (BanStatus)reader.ReadByte();
            info.banDate = ReadDate( reader );
            info.bannedBy = ReadString( reader );
            info.banReason = ReadString( reader );
            if( info.banStatus == BanStatus.Banned ) {
                info.bannedUntil = ReadDate( reader );
                info.lastFailedLoginDate = ReadDate( reader );
                info.lastFailedLoginIP = new IPAddress( reader.ReadBytes( 4 ) );
            } else {
                info.unbanDate = ReadDate( reader );
                info.unbannedBy = ReadString( reader );
                info.unbanReason = ReadString( reader );
            }

            // Stats
            info.firstLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.lastLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.totalTime = new TimeSpan( reader.ReadUInt32() * TimeSpan.TicksPerSecond );
            info.blocksBuilt = Read7BitEncodedInt( reader );
            info.blocksDeleted = Read7BitEncodedInt( reader );
            if( reader.ReadBoolean() ) {
                info.blocksDrawn = reader.ReadInt64();
            }
            info.timesVisited = Read7BitEncodedInt( reader );
            info.messagesWritten = Read7BitEncodedInt( reader );
            info.timesKickedOthers = Read7BitEncodedInt( reader );
            info.timesBannedOthers = Read7BitEncodedInt( reader );

            // Kicks
            info.timesKicked = Read7BitEncodedInt( reader );
            if( info.timesKicked > 0 ) {
                info.lastKickDate = ReadDate( reader );
                info.lastKickBy = ReadString( reader );
                info.lastKickReason = ReadString( reader );
            }

            // Freeze/Mute
            info.isFrozen = reader.ReadBoolean();
            if( info.isFrozen ) {
                info.frozenOn = ReadDate( reader );
                info.frozenBy = ReadString( reader );
            }
            info.mutedUntil = ReadDate( reader );
            if( info.mutedUntil != DateTime.MinValue ) {
                info.mutedBy = ReadString( reader );
            }

            // Misc
            info.password = ReadString( reader );
            info.LastModified = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.isOnline = reader.ReadBoolean();
            info.isHidden = reader.ReadBoolean();
            info.lastIP = new IPAddress( reader.ReadBytes( 4 ) );
            info.leaveReason = (LeaveReason)reader.ReadByte();
            info.bandwidthUseMode = (BandwidthUseMode)reader.ReadByte();

            return info;
        }


        static DateTime ReadDate( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            } else {
                return DateTime.MinValue;
            }
        }


        static string ReadString( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return reader.ReadString();
            } else {
                return null;
            }
        }


        static int Read7BitEncodedInt( [NotNull] BinaryReader reader ) {
            byte num3;
            int num = 0;
            int num2 = 0;
            do {
                if( num2 == 0x23 ) {
                    throw new FormatException( "Invalid 7bit encoded integer." );
                }
                num3 = reader.ReadByte();
                num |= (num3 & 0x7f) << num2;
                num2 += 7;
            }
            while( (num3 & 0x80) != 0 );
            return num;
        }

        #endregion


        public static string Escape( [CanBeNull] string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            } else if( str.IndexOf( ',' ) > -1 ) {
                return str.Replace( ',', '\xFF' );
            } else {
                return str;
            }
        }


        public static string UnescapeOldFormat( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            return str.Replace( '\xFF', ',' ).Replace( "\'", "'" ).Replace( @"\\", @"\" );
        }


        public static string Unescape( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.IndexOf( '\xFF' ) > -1 ) {
                return str.Replace( '\xFF', ',' );
            } else {
                return str;
            }
        }



        #region Saving

        internal void Serialize( StringBuilder sb ) {
            sb.Append( name ).Append( ',' ); // 0
            if( !lastIP.Equals( IPAddress.None ) ) sb.Append( lastIP ); // 1
            sb.Append( ',' );

            sb.Append( rank.FullName ).Append( ',' ); // 2
            rankChangeDate.ToUnixTimeString( sb ).Append( ',' ); // 3

            sb.AppendEscaped( rankChangedBy ).Append( ',' ); // 4

            switch( banStatus ) {
                case BanStatus.Banned:
                    sb.Append( 'b' );
                    break;
                case BanStatus.IPBanExempt:
                    sb.Append( 'x' );
                    break;
            }
            sb.Append( ',' ); // 5

            banDate.ToUnixTimeString( sb ).Append( ',' ); // 6
            sb.AppendEscaped( bannedBy ).Append( ',' ); // 7
            unbanDate.ToUnixTimeString( sb ).Append( ',' ); // 8
            sb.AppendEscaped( unbannedBy ).Append( ',' ); // 9
            sb.AppendEscaped( banReason ).Append( ',' ); // 10
            sb.AppendEscaped( unbanReason ).Append( ',' ); // 11

            lastFailedLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 12

            if( !lastFailedLoginIP.Equals( IPAddress.None ) ) sb.Append( lastFailedLoginIP.ToString() ); // 13
            sb.Append( ',', 2 ); // skip 14

            firstLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 15
            lastLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 16

            if( isOnline ) {
                (totalTime.Add( TimeSinceLastLogin )).ToTickString( sb ).Append( ',' ); // 17
            } else {
                totalTime.ToTickString( sb ).Append( ',' ); // 17
            }

            if( blocksBuilt > 0 ) sb.Digits( blocksBuilt ); // 18
            sb.Append( ',' );

            if( blocksDeleted > 0 ) sb.Digits( blocksDeleted ); // 19
            sb.Append( ',' );

            sb.Append( timesVisited ).Append( ',' ); // 20


            if( messagesWritten > 0 ) sb.Digits( messagesWritten ); // 21
            sb.Append( ',', 3 ); // 22-23 no longer in use

            if( previousRank != null ) sb.Append( previousRank.FullName ); // 24
            sb.Append( ',' );

            sb.AppendEscaped( rankChangeReason ).Append( ',' ); // 25


            if( timesKicked > 0 ) sb.Digits( timesKicked ); // 26
            sb.Append( ',' );

            if( timesKickedOthers > 0 ) sb.Digits( timesKickedOthers ); // 27
            sb.Append( ',' );

            if( timesBannedOthers > 0 ) sb.Digits( timesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Digits( ID ).Append( ',' ); // 29

            sb.Digits( (int)rankChangeType ).Append( ',' ); // 30


            lastKickDate.ToUnixTimeString( sb ).Append( ',' ); // 31

            if( isOnline ) DateTime.UtcNow.ToUnixTimeString( sb ); // 32
            else lastSeen.ToUnixTimeString( sb );
            sb.Append( ',' );

            if( blocksDrawn > 0 ) sb.Append( blocksDrawn ); // 33
            sb.Append( ',' );

            sb.AppendEscaped( lastKickBy ).Append( ',' ); // 34
            sb.AppendEscaped( lastKickReason ).Append( ',' ); // 35

            bannedUntil.ToUnixTimeString( sb ); // 36

            if( isFrozen ) {
                sb.Append( ',' ).Append( 'f' ).Append( ',' ); // 37
                sb.AppendEscaped( frozenBy ).Append( ',' ); // 38
                frozenOn.ToUnixTimeString( sb ).Append( ',' ); // 39
            } else {
                sb.Append( ',', 4 ); // 37-39
            }

            if( mutedUntil > DateTime.UtcNow ) {
                mutedUntil.ToUnixTimeString( sb ).Append( ',' ); // 40
                sb.AppendEscaped( mutedBy ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            sb.AppendEscaped( password ).Append( ',' ); // 42

            if( isOnline ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( bandwidthUseMode != BandwidthUseMode.Default ) sb.Append( (int)bandwidthUseMode ); // 44
            sb.Append( ',' );

            if( isHidden ) sb.Append( 'h' ); // 45

            sb.Append( ',' );
            LastModified.ToUnixTimeString( sb ); // 46

            sb.Append( ',' );
            sb.AppendEscaped( displayedName ); // 47
        }


        internal void SaveBinaryFormat0( [NotNull] BinaryWriter writer ) {
            if( writer == null ) throw new ArgumentNullException( "writer" );
            // General
            writer.Write( name ); // 0
            WriteString( writer, displayedName ); // 1
            Write7BitEncodedInt( writer, ID ); // 2
            if( isOnline ) {
                writer.Write( (uint)DateTime.UtcNow.ToUnixTime() ); // 5
            } else {
                writer.Write( (uint)lastSeen.ToUnixTime() ); // 5
            }

            // Rank
            Write7BitEncodedInt( writer, rank.Index ); // 7
            {
                bool hasPrevRank = (previousRank != null); // 8 prefix
                writer.Write( hasPrevRank );
                if( hasPrevRank ) {
                    Write7BitEncodedInt( writer, previousRank.Index ); // 8
                }
            }
            writer.Write( (byte)rankChangeType ); // 12
            if( rankChangeType != RankChangeType.Default ) {
                WriteDate( writer, rankChangeDate ); // 9
                WriteString( writer, rankChangedBy ); // 10
                WriteString( writer, rankChangeReason ); // 11
            }

            // Bans
            writer.Write( (byte)banStatus ); // 13
            WriteDate( writer, banDate ); // 14
            WriteString( writer, bannedBy ); // 15
            WriteString( writer, banReason ); // 16
            if( banStatus == BanStatus.Banned ) {
                WriteDate( writer, bannedUntil ); // 14
                WriteDate( writer, lastFailedLoginDate ); // 20
                writer.Write( lastFailedLoginIP.GetAddressBytes() ); // 21
            } else {
                WriteDate( writer, unbanDate ); // 17
                WriteString( writer, unbannedBy ); // 18
                WriteString( writer, unbanReason ); // 18
            }

            // Stats
            writer.Write( (uint)firstLoginDate.ToUnixTime() ); // 3
            writer.Write( (uint)lastLoginDate.ToUnixTime() ); // 4
            if( isOnline ) {
                writer.Write( (uint)totalTime.Add( TimeSinceLastLogin ).ToSeconds() ); // 22
            } else {
                writer.Write( (uint)totalTime.ToSeconds() ); // 22
            }
            Write7BitEncodedInt( writer, blocksBuilt ); // 23
            Write7BitEncodedInt( writer, blocksDeleted ); // 24
            {
                bool hasBlocksDrawn = (blocksDrawn > 0); // 25 prefix
                writer.Write( hasBlocksDrawn );
                if( hasBlocksDrawn ) {
                    writer.Write( blocksDrawn ); // 25
                }
            }
            Write7BitEncodedInt( writer, timesVisited ); // 26
            Write7BitEncodedInt( writer, messagesWritten ); // 27
            Write7BitEncodedInt( writer, timesKickedOthers ); // 28
            Write7BitEncodedInt( writer, timesBannedOthers ); // 29

            // Kicks
            Write7BitEncodedInt( writer, timesKicked ); // 30
            if( TimesKicked > 0 ) {
                WriteDate( writer, lastKickDate ); // 31
                WriteString( writer, lastKickBy ); // 32
                WriteString( writer, lastKickReason ); // 33
            }

            // Freeze/Mute
            writer.Write( isFrozen ); // 34
            if( isFrozen ) {
                WriteDate( writer, frozenOn ); // 35
                WriteString( writer, frozenBy ); // 36
            }
            WriteDate( writer, mutedUntil ); // 37
            if( mutedUntil != DateTime.MinValue ) {
                WriteString( writer, mutedBy ); // 38
            }

            // Misc
            WriteString( writer, password ); // 39
            writer.Write( (uint)LastModified.ToUnixTime() ); // 40
            writer.Write( isOnline ); // 41
            writer.Write( isHidden ); // 42
            writer.Write( lastIP.GetAddressBytes() ); // 43
            writer.Write( (byte)leaveReason ); // 44
            writer.Write( (byte)bandwidthUseMode ); // 45
        }


        static void WriteDate( [NotNull] BinaryWriter writer, DateTime dateTime ) {
            bool hasDate = (dateTime != DateTime.MinValue);
            writer.Write( hasDate );
            if( hasDate ) {
                writer.Write( (uint)dateTime.ToUnixTime() );
            }
        }


        static void WriteString( [NotNull] BinaryWriter writer, [CanBeNull] string str ) {
            bool hasString = (str != null);
            writer.Write( hasString );
            if( hasString ) {
                writer.Write( str );
            }
        }


        static void Write7BitEncodedInt( [NotNull] BinaryWriter writer, int value ) {
            uint num = (uint)value;
            while( num >= 0x80 ) {
                writer.Write( (byte)(num | 0x80) );
                num = num >> 7;
            }
            writer.Write( (byte)num );
        }

        #endregion


        #region Saving/Loading

        internal static void Load() {
            //LoadBinary();
            //return;
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName ) ) {
                    Stopwatch sw = Stopwatch.StartNew();
                    using( FileStream fs = OpenRead( Paths.PlayerDBFileName ) ) {
                        using( StreamReader reader = new StreamReader( fs, Encoding.UTF8, true, BufferSize ) ) {

                            string header = reader.ReadLine();

                            if( header == null ) return; // if PlayerDB is an empty file

                            lock( AddLocker ) {
                                int version = IdentifyFormatVersion( header );
                                if( version > FormatVersion ) {
                                    Logger.Log( LogType.Warning,
                                                "PlayerDB.Load: Attempting to load unsupported PlayerDB format ({0}). Errors may occur.",
                                                version );
                                } else if( version < FormatVersion ) {
                                    Logger.Log( LogType.Warning,
                                                "PlayerDB.Load: Converting PlayerDB to a newer format (version {0} to {1}).",
                                                version, FormatVersion );
                                }

                                int emptyRecords = 0;
                                while( true ) {
                                    string line = reader.ReadLine();
                                    if( line == null ) break;
                                    string[] fields = line.Split( ',' );
                                    if( fields.Length >= PlayerInfo.MinFieldCount ) {
#if !DEBUG
                                        try {
#endif
                                        PlayerInfo info;
                                        switch( version ) {
                                            case 0:
                                                info = PlayerInfo.LoadFormat0( fields );
                                                break;
                                            case 1:
                                                info = PlayerInfo.LoadFormat1( fields );
                                                break;
                                            default:
                                                // Versions 2-5 differ in semantics only, not in actual serialization format.
                                                info = PlayerInfo.LoadFormat2( fields );
                                                break;
                                        }

                                        if( info.ID > maxID ) {
                                            maxID = info.ID;
                                            Logger.Log( LogType.Warning, "PlayerDB.Load: Adjusting wrongly saved MaxID ({0} to {1})." );
                                        }

                                        // A record is considered "empty" if the player has never logged in.
                                        // Empty records may be created by /Import, /Ban, and /Rank commands on typos.
                                        // Deleting such records should have no negative impact on DB completeness.
                                        if( (info.LastIP.Equals( IPAddress.None ) || info.LastIP.Equals( IPAddress.Any ) || info.TimesVisited == 0) &&
                                            !info.IsBanned && info.Rank == RankManager.DefaultRank ) {

                                            Logger.Log( LogType.SystemActivity,
                                                        "PlayerDB.Load: Skipping an empty record for player \"{0}\"",
                                                        info.Name );
                                            emptyRecords++;
                                            continue;
                                        }

                                        // Check for duplicates. Unless PlayerDB.txt was altered externally, this does not happen.
                                        if( Trie.ContainsKey( info.Name ) ) {
                                            Logger.Log( LogType.Error,
                                                        "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.",
                                                        info.Name );
                                        } else {
                                            Trie.Add( info.Name, info );
                                            list.Add( info );
                                        }
#if !DEBUG
                                        } catch( Exception ex ) {
                                            Logger.LogAndReportCrash( "Error while parsing PlayerInfo record",
                                                                      "fCraft",
                                                                      ex,
                                                                      false );
                                        }
#endif
                                    } else {
                                        Logger.Log( LogType.Error,
                                                    "PlayerDB.Load: Unexpected field count ({0}), expecting at least {1} fields for a PlayerDB entry.",
                                                    fields.Length, PlayerInfo.MinFieldCount );
                                    }
                                }

                                if( emptyRecords > 0 ) {
                                    Logger.Log( LogType.Warning,
                                                "PlayerDB.Load: Skipped {0} empty records.", emptyRecords );
                                }

                                RunCompatibilityChecks( version );
                            }
                        }
                    }
                    sw.Stop();
                    Logger.Log( LogType.Debug,
                                "PlayerDB.Load: Done loading player DB ({0} records) in {1}ms. MaxID={2}",
                                Trie.Count, sw.ElapsedMilliseconds, maxID );
                } else {
                    Logger.Log( LogType.Warning, "PlayerDB.Load: No player DB file found." );
                }
                UpdateCache();
                IsLoaded = true;
            }
        }

        static Dictionary<int, Rank> rankMapping;

        internal static Rank GetRankByIndex( int index ) {
            Rank rank;
            if( rankMapping.TryGetValue( index, out rank ) ) {
                return rank;
            } else {
                Logger.Log( LogType.Error,
                            "Unknown rank index ({0}). Assigning rank {1} instead.",
                            index, RankManager.DefaultRank );
                return RankManager.DefaultRank;
            }
        }

        internal static void LoadBinary() {
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName + ".bin" ) ) {
                    Stopwatch sw = Stopwatch.StartNew();
                    using( FileStream fs = OpenRead( Paths.PlayerDBFileName + ".bin" ) ) {
                        BinaryReader reader = new BinaryReader( fs );
                        int version = reader.ReadInt32();

                        if( version > FormatVersion ) {
                            Logger.Log( LogType.Warning,
                                        "PlayerDB.LoadBinary: Attempting to load unsupported PlayerDB format ({0}). Errors may occur.",
                                        version );
                        } else if( version < FormatVersion ) {
                            Logger.Log( LogType.Warning,
                                        "PlayerDB.LoadBinary: Converting PlayerDB to a newer format (version {0} to {1}).",
                                        version, FormatVersion );
                        }

                        maxID = reader.ReadInt32();

                        lock( AddLocker ) {
                            int rankCount = reader.ReadInt32();
                            rankMapping = new Dictionary<int, Rank>( rankCount );
                            for( int i = 0; i < rankCount; i++ ) {
                                byte rankIndex = reader.ReadByte();
                                string rankName = reader.ReadString();
                                Rank rank = Rank.Parse( rankName );
                                if( rank == null ) {
                                    Logger.Log( LogType.Error,
                                                "PlayerDB.LoadBinary: Could not parse rank: \"{0}\". Assigning rank {1} instead.",
                                                rankName, RankManager.DefaultRank );
                                    rank = RankManager.DefaultRank;
                                }
                                rankMapping.Add( rankIndex, rank );
                            }
                            int records = reader.ReadInt32();

                            int emptyRecords = 0;
                            for( int i = 0; i < records; i++ ) {
#if !DEBUG
                                try {
#endif
                                PlayerInfo info;
                                switch( version ) {
                                    default:
                                        // Versions 2-5 differ in semantics only, not in actual serialization format.
                                        info = PlayerInfo.LoadBinaryFormat0( reader );
                                        break;
                                }

                                if( info.ID > maxID ) {
                                    maxID = info.ID;
                                    Logger.Log( LogType.Warning, "PlayerDB.LoadBinary: Adjusting wrongly saved MaxID ({0} to {1})." );
                                }

                                // A record is considered "empty" if the player has never logged in.
                                // Empty records may be created by /Import, /Ban, and /Rank commands on typos.
                                // Deleting such records should have no negative impact on DB completeness.
                                if( (info.LastIP.Equals( IPAddress.None ) || info.LastIP.Equals( IPAddress.Any ) || info.TimesVisited == 0) &&
                                    !info.IsBanned && info.Rank == RankManager.DefaultRank ) {

                                    Logger.Log( LogType.SystemActivity,
                                                "PlayerDB.LoadBinary: Skipping an empty record for player \"{0}\"",
                                                info.Name );
                                    emptyRecords++;
                                    continue;
                                }

                                // Check for duplicates. Unless PlayerDB.txt was altered externally, this does not happen.
                                if( Trie.ContainsKey( info.Name ) ) {
                                    Logger.Log( LogType.Error,
                                                "PlayerDB.LoadBinary: Duplicate record for player \"{0}\" skipped.",
                                                info.Name );
                                } else {
                                    Trie.Add( info.Name, info );
                                    list.Add( info );
                                }
#if !DEBUG
                                } catch( Exception ex ) {
                                    Logger.LogAndReportCrash( "Error while parsing PlayerInfo record",
                                                              "fCraft",
                                                              ex,
                                                              false );
                                }
#endif
                            }

                            if( emptyRecords > 0 ) {
                                Logger.Log( LogType.Warning,
                                            "PlayerDB.LoadBinary: Skipped {0} empty records.", emptyRecords );
                            }

                            RunCompatibilityChecks( version );
                        }
                    }
                    sw.Stop();
                    Logger.Log( LogType.Debug,
                                "PlayerDB.LoadBinary: Done loading player DB ({0} records) in {1}ms. MaxID={2}",
                                Trie.Count, sw.ElapsedMilliseconds, maxID );
                } else {
                    Logger.Log( LogType.Warning, "PlayerDB.Load: No player DB file found." );
                }
                UpdateCache();
                IsLoaded = true;
            }
        }

        static void RunCompatibilityChecks( int loadedVersion ) {
            // Sorting the list allows finding players by ID using binary search.
            list.Sort( PlayerIDComparer.Instance );

            if( loadedVersion < 4 ) {
                int unhid = 0, unfroze = 0, unmuted = 0;
                Logger.Log( LogType.SystemActivity, "PlayerDB: Checking consistency of banned player records..." );
                for( int i = 0; i < list.Count; i++ ) {
                    if( list[i].IsBanned ) {
                        if( list[i].IsHidden ) {
                            unhid++;
                            list[i].IsHidden = false;
                        }

                        if( list[i].IsFrozen ) {
                            list[i].Unfreeze();
                            unfroze++;
                        }

                        if( list[i].IsMuted ) {
                            list[i].Unmute();
                            unmuted++;
                        }
                    }
                }
                Logger.Log( LogType.SystemActivity,
                            "PlayerDB: Unhid {0}, unfroze {1}, and unmuted {2} banned accounts.",
                            unhid, unfroze, unmuted );
            }
        }


        static int IdentifyFormatVersion( [NotNull] string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            string[] headerParts = header.Split( ' ' );
            if( headerParts.Length < 2 ) {
                throw new FormatException( "Invalid PlayerDB header format: " + header );
            }
            int maxIDField;
            if( Int32.TryParse( headerParts[0], out maxIDField ) ) {
                if( maxIDField >= 255 ) {// IDs start at 256
                    maxID = maxIDField;
                }
            }
            int version;
            if( Int32.TryParse( headerParts[1], out version ) ) {
                return version;
            } else {
                return 0;
            }
        }


        public static void Save() {
            CheckIfLoaded();
            const string tempFileName = Paths.PlayerDBFileName + ".bin.temp";

            lock( SaveLoadLocker ) {
                PlayerInfo[] listCopy = PlayerInfoList;
                Stopwatch sw = Stopwatch.StartNew();
                using( FileStream fs = OpenWrite( tempFileName ) ) {
                    BinaryWriter writer = new BinaryWriter( fs );
                    writer.Write( FormatVersion );
                    writer.Write( maxID );
                    writer.Write( RankManager.Ranks.Count );
                    foreach( Rank rank in RankManager.Ranks ) {
                        writer.Write( (byte)rank.Index );
                        writer.Write( rank.FullName );
                    }
                    writer.Write( listCopy.Length );
                    for( int i = 0; i < listCopy.Length; i++ ) {
                        listCopy[i].SaveBinaryFormat0( writer );
                    }
                }
                sw.Stop();
                Logger.Log( LogType.Debug,
                            "PlayerDB.SaveBinary: Saved player database ({0} records) in {1}ms",
                            Trie.Count, sw.ElapsedMilliseconds );

                try {
                    Paths.MoveOrReplace( tempFileName, Paths.PlayerDBFileName + ".bin" );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "PlayerDB.SaveBinary: An error occured while trying to save PlayerDB: {0}", ex );
                }
            }
        }


        static FileStream OpenRead( string fileName ) {
            return new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan );
        }


        static FileStream OpenWrite( string fileName ) {
            return new FileStream( fileName, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize );
        }

        #endregion
    }
}
