// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    public sealed partial class PlayerInfo : IClassy {

        public const int MinFieldCount = 24;

        public string Name;

        public IPAddress LastIP;

        public Rank Rank;
        public DateTime RankChangeDate;

        public string RankChangedBy = "";

        public BanStatus BanStatus;
        public bool IsBanned {
            get { return BanStatus == BanStatus.Banned; }
        }

        public DateTime BanDate;
        public string BannedBy = "";
        public DateTime UnbanDate;
        public string UnbannedBy = "";
        public string BanReason = "";
        public string UnbanReason = "";

        public DateTime LastFailedLoginDate;
        public IPAddress LastFailedLoginIP;
        public int FailedLoginCount;
        public DateTime FirstLoginDate;
        public DateTime LastLoginDate;

        public TimeSpan TotalTime;
        public int BlocksBuilt;
        public int BlocksDeleted;
        public int TimesVisited;
        public int MessagesWritten;

        public Rank PreviousRank;
        public string RankChangeReason = "";
        public int TimesKicked;
        public int TimesKickedOthers;
        public int TimesBannedOthers;

        public int ID;
        public RankChangeType RankChangeType;
        public DateTime LastKickDate;
        public DateTime LastSeen;
        public long BlocksDrawn;

        public string LastKickBy = "";
        public string LastKickReason = "";

        // TODO: start tracking
        public DateTime BannedUntil;
        public bool IsFrozen;
        public string FrozenBy = "";
        public DateTime FrozenOn;
        public DateTime MutedUntil;
        public string MutedBy = "";

        public string Password = ""; // TODO

        public bool IsOnline;
        public Player PlayerObject;
        public LeaveReason LeaveReason;

        public BandwidthUseMode BandwidthUseMode;

        public bool IsHidden;

        public DateTime LastModified;

        public string DisplayedName = "";


        #region Constructors and Serialization

        internal PlayerInfo( int id ) {
            ID = id;
        }

        PlayerInfo() {
            // reset everything to defaults
            LastIP = IPAddress.None;
            RankChangeDate = DateTime.MinValue;
            BanDate = DateTime.MinValue;
            UnbanDate = DateTime.MinValue;
            LastFailedLoginDate = DateTime.MinValue;
            LastFailedLoginIP = IPAddress.None;
            FirstLoginDate = DateTime.MinValue;
            LastLoginDate = DateTime.MinValue;
            TotalTime = TimeSpan.Zero;
            RankChangeType = RankChangeType.Default;
            LastKickDate = DateTime.MinValue;
            LastSeen = DateTime.MinValue;
            BannedUntil = DateTime.MinValue;
            FrozenOn = DateTime.MinValue;
            MutedUntil = DateTime.MinValue;
            BandwidthUseMode = BandwidthUseMode.Default;
            LastModified = DateTime.UtcNow;
        }

        // fabricate info for an unrecognized player
        public PlayerInfo( string name, Rank rank, bool setLoginDate, RankChangeType rankChangeType )
            : this() {
            Name = name;
            Rank = rank;
            if( setLoginDate ) {
                FirstLoginDate = DateTime.UtcNow;
                LastLoginDate = FirstLoginDate;
                LastSeen = FirstLoginDate;
                TimesVisited = 1;
            }
            RankChangeType = rankChangeType;
        }


        // generate blank info for a new player
        public PlayerInfo( string name, IPAddress lastIP, Rank startingRank )
            : this() {
            FirstLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            LastLoginDate = DateTime.UtcNow;
            Rank = startingRank;
            Name = name;
            ID = PlayerDB.GetNextID();
            LastIP = lastIP;
        }

        #endregion


        #region Loading

        internal static PlayerInfo LoadFormat2( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTime( ref info.RankChangeDate );
            info.RankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTime( ref info.BanDate ) ) {
                info.BannedBy = Unescape( fields[7] );
                info.BanReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTime( ref info.UnbanDate ) ) {
                info.UnbannedBy = Unescape( fields[9] );
                info.UnbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTime( ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            if( fields[14].Length > 0 ) info.FailedLoginCount = Int32.Parse( fields[14] );
            fields[15].ToDateTime( ref info.FirstLoginDate );

            // login/logout times
            fields[16].ToDateTime( ref info.LastLoginDate );
            fields[17].ToTimeSpan( out info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.TimesKicked );
            Int32.TryParse( fields[27], out info.TimesKickedOthers );
            Int32.TryParse( fields[28], out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            int rankChangeTypeCode;
            if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTime( ref info.LastKickDate );
            if( !fields[32].ToDateTime( ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );

            info.LastKickBy = Unescape( fields[34] );
            info.LastKickReason = Unescape( fields[35] );

            fields[36].ToDateTime( ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            info.FrozenBy = Unescape( fields[38] );
            fields[39].ToDateTime( ref info.FrozenOn );
            fields[40].ToDateTime( ref info.MutedUntil );
            info.MutedBy = Unescape( fields[41] );
            info.Password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            int bandwidthUseModeCode;
            if( Int32.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }
            if( fields.Length > 46 ) {
                fields[46].ToDateTime( ref info.LastModified );
            }
            if( fields.Length > 47 ) {
                info.DisplayedName = Unescape( fields[47] );
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat1( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTimeLegacy( ref info.RankChangeDate );
            info.RankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTimeLegacy( ref info.BanDate ) ) {
                info.BannedBy = Unescape( fields[7] );
                info.BanReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTimeLegacy( ref info.UnbanDate ) ) {
                info.UnbannedBy = Unescape( fields[9] );
                info.UnbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTimeLegacy( ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            if( fields[14].Length > 0 ) info.FailedLoginCount = Int32.Parse( fields[14] );
            fields[15].ToDateTimeLegacy( ref info.FirstLoginDate );

            // login/logout times
            fields[16].ToDateTimeLegacy( ref info.LastLoginDate );
            fields[17].ToTimeSpanLegacy( ref info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.TimesKicked );
            Int32.TryParse( fields[27], out info.TimesKickedOthers );
            Int32.TryParse( fields[28], out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            int rankChangeTypeCode;
            if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTimeLegacy( ref info.LastKickDate );
            if( !fields[32].ToDateTimeLegacy( ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );

            info.LastKickBy = Unescape( fields[34] );
            info.LastKickReason = Unescape( fields[35] );

            fields[36].ToDateTimeLegacy( ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            info.FrozenBy = Unescape( fields[38] );
            fields[39].ToDateTimeLegacy( ref info.FrozenOn );
            fields[40].ToDateTimeLegacy( ref info.MutedUntil );
            info.MutedBy = Unescape( fields[41] );
            info.Password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            int bandwidthUseModeCode;
            if( Int32.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat0( string[] fields, bool convertDatesToUtc ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTimeUtil.TryParseLocalDate( fields[3], out info.RankChangeDate );
            info.RankChangedBy = fields[4];
            if( info.RankChangedBy == "-" ) info.RankChangedBy = "";

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( DateTimeUtil.TryParseLocalDate( fields[6], out info.BanDate ) ) {
                info.BannedBy = fields[7];
                info.BanReason = UnescapeOldFormat( fields[10] );
                if( info.BanReason == "-" ) info.BanReason = "";
            }

            // unban information
            if( DateTimeUtil.TryParseLocalDate( fields[8], out info.UnbanDate ) ) {
                info.UnbannedBy = fields[9];
                info.UnbanReason = UnescapeOldFormat( fields[11] );
                if( info.UnbanReason == "-" ) info.UnbanReason = "";
            }

            // failed logins
            if( fields[12].Length > 1 ) {
                DateTimeUtil.TryParseLocalDate( fields[12], out info.LastFailedLoginDate );
            }
            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            if( fields[14].Length > 0 ) info.FailedLoginCount = Int32.Parse( fields[14] );

            // login/logout times
            DateTimeUtil.TryParseLocalDate( fields[15], out info.FirstLoginDate );
            DateTimeUtil.TryParseLocalDate( fields[16], out info.LastLoginDate );
            TimeSpan.TryParse( fields[17], out info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
                if( fields[25].Length > 0 ) info.RankChangeReason = UnescapeOldFormat( fields[25] );
                Int32.TryParse( fields[26], out info.TimesKicked );
                Int32.TryParse( fields[27], out info.TimesKickedOthers );
                Int32.TryParse( fields[28], out info.TimesBannedOthers );
                if( fields.Length > 29 ) {
                    info.ID = Int32.Parse( fields[29] );
                    if( info.ID < 256 )
                        info.ID = PlayerDB.GetNextID();
                    int rankChangeTypeCode;
                    if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            info.GuessRankChangeType();
                        }
                    } else {
                        info.GuessRankChangeType();
                    }
                    DateTimeUtil.TryParseLocalDate( fields[31], out info.LastKickDate );
                    if( !DateTimeUtil.TryParseLocalDate( fields[32], out info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                        info.LastSeen = info.LastLoginDate;
                    }
                    Int64.TryParse( fields[33], out info.BlocksDrawn );

                    info.LastKickBy = fields[34];
                    info.LastKickReason = UnescapeOldFormat( fields[35] );

                } else {
                    info.ID = PlayerDB.GetNextID();
                    info.GuessRankChangeType();
                    info.LastSeen = info.LastLoginDate;
                }

                if( fields.Length > 36 ) {
                    DateTimeUtil.TryParseLocalDate( fields[36], out info.BannedUntil );
                    info.IsFrozen = (fields[37] == "f");
                    info.FrozenBy = UnescapeOldFormat( fields[38] );
                    DateTimeUtil.TryParseLocalDate( fields[39], out info.FrozenOn );
                    DateTimeUtil.TryParseLocalDate( fields[40], out info.MutedUntil );
                    info.MutedBy = UnescapeOldFormat( fields[41] );
                    info.Password = UnescapeOldFormat( fields[42] );
                    // fields[43] is "online", and is ignored
                }

                if( fields.Length > 44 ) {
                    if( fields[44].Length != 0 ) {
                        info.BandwidthUseMode = (BandwidthUseMode)Int32.Parse( fields[44] );
                    }
                }
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            if( convertDatesToUtc ) {
                if( info.RankChangeDate != DateTime.MinValue ) info.RankChangeDate = info.RankChangeDate.ToUniversalTime();
                if( info.BanDate != DateTime.MinValue ) info.BanDate = info.BanDate.ToUniversalTime();
                if( info.UnbanDate != DateTime.MinValue ) info.UnbanDate = info.UnbanDate.ToUniversalTime();
                if( info.LastFailedLoginDate != DateTime.MinValue ) info.LastFailedLoginDate = info.LastFailedLoginDate.ToUniversalTime();
                if( info.FirstLoginDate != DateTime.MinValue ) info.FirstLoginDate = info.FirstLoginDate.ToUniversalTime();
                if( info.LastLoginDate != DateTime.MinValue ) info.LastLoginDate = info.LastLoginDate.ToUniversalTime();
                if( info.LastKickDate != DateTime.MinValue ) info.LastKickDate = info.LastKickDate.ToUniversalTime();
                if( info.LastSeen != DateTime.MinValue ) info.LastSeen = info.LastSeen.ToUniversalTime();
                if( info.BannedUntil != DateTime.MinValue ) info.BannedUntil = info.BannedUntil.ToUniversalTime();
                if( info.FrozenOn != DateTime.MinValue ) info.FrozenOn = info.FrozenOn.ToUniversalTime();
                if( info.MutedUntil != DateTime.MinValue ) info.MutedUntil = info.MutedUntil.ToUniversalTime();
            }

            return info;
        }


        void GuessRankChangeType() {
            if( PreviousRank != null ) {
                if( RankChangeReason == "~AutoRank" || RankChangeReason == "~AutoRankAll" || RankChangeReason == "~MassRank" ) {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.AutoDemoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.Demoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                RankChangeType = RankChangeType.Default;
            }
        }

        #endregion


        #region Saving

        internal void Serialize( StringBuilder sb ) {
            sb.Append( Name ).Append( ',' ); // 0
            if( !LastIP.Equals( IPAddress.None ) ) sb.Append( LastIP ); // 1
            sb.Append( ',' );

            sb.Append( Rank.FullName ).Append( ',' ); // 2
            RankChangeDate.ToUnixTimeString( sb ).Append( ',' ); // 3

            sb.AppendEscaped( RankChangedBy ).Append( ',' ); // 4

            switch( BanStatus ) {
                case BanStatus.Banned:
                    sb.Append( 'b' );
                    break;
                case BanStatus.IPBanExempt:
                    sb.Append( 'x' );
                    break;
            }
            sb.Append( ',' ); // 5

            BanDate.ToUnixTimeString( sb ).Append( ',' ); // 6
            sb.AppendEscaped( BannedBy ).Append( ',' ); // 7
            UnbanDate.ToUnixTimeString( sb ).Append( ',' ); // 8
            sb.AppendEscaped( UnbannedBy ).Append( ',' ); // 9
            sb.AppendEscaped( BanReason ).Append( ',' ); // 10
            sb.AppendEscaped( UnbanReason ).Append( ',' ); // 11

            LastFailedLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 12

            if( !LastFailedLoginIP.Equals( IPAddress.None ) ) sb.Append( LastFailedLoginIP.ToString() ); // 13
            sb.Append( ',' );

            if( FailedLoginCount > 0 ) sb.Digits( FailedLoginCount ); // 14
            sb.Append( ',' );

            FirstLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 15
            LastLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 16

            Player pObject = PlayerObject;
            if( pObject != null ) {
                (TotalTime.Add( TimeSinceLastLogin )).ToTickString( sb ).Append( ',' ); // 17
            } else {
                TotalTime.ToTickString( sb ).Append( ',' ); // 17
            }

            if( BlocksBuilt > 0 ) sb.Digits( BlocksBuilt ); // 18
            sb.Append( ',' );

            if( BlocksDeleted > 0 ) sb.Digits( BlocksDeleted ); // 19
            sb.Append( ',' );

            sb.Append( TimesVisited ).Append( ',' ); // 20


            if( MessagesWritten > 0 ) sb.Digits( MessagesWritten ); // 21
            sb.Append( ',', 3 ); // 22-23 no longer in use

            if( PreviousRank != null ) sb.Append( PreviousRank.FullName ); // 24
            sb.Append( ',' );

            sb.AppendEscaped( RankChangeReason ).Append( ',' ); // 25


            if( TimesKicked > 0 ) sb.Digits( TimesKicked ); // 26
            sb.Append( ',' );

            if( TimesKickedOthers > 0 ) sb.Digits( TimesKickedOthers ); // 27
            sb.Append( ',' );

            if( TimesBannedOthers > 0 ) sb.Digits( TimesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Append( ID ).Append( ',' ); // 29

            sb.Append( (int)RankChangeType ).Append( ',' ); // 30


            LastKickDate.ToUnixTimeString( sb ).Append( ',' ); // 31

            if( IsOnline ) DateTime.UtcNow.ToUnixTimeString( sb ); // 32
            else LastSeen.ToUnixTimeString( sb );
            sb.Append( ',' );

            if( BlocksDrawn > 0 ) sb.Append( BlocksDrawn ); // 33
            sb.Append( ',' );

            sb.AppendEscaped( LastKickBy ).Append( ',' ); // 34

            sb.AppendEscaped( LastKickReason ).Append( ',' ); // 35

            BannedUntil.ToUnixTimeString( sb ); // 36

            if( IsFrozen ) {
                sb.Append( ',' ).Append( 'f' ).Append( ',' ); // 37
                sb.AppendEscaped( FrozenBy ).Append( ',' ); // 38
                FrozenOn.ToUnixTimeString( sb ).Append( ',' ); // 39
            } else {
                sb.Append( ',', 4 ); // 37-39
            }

            if( MutedUntil > DateTime.UtcNow ) {
                MutedUntil.ToUnixTimeString( sb ).Append( ',' ); // 40
                sb.AppendEscaped( MutedBy ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            sb.AppendEscaped( Password ).Append( ',' ); // 42

            if( IsOnline ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( BandwidthUseMode != BandwidthUseMode.Default ) sb.Append( (int)BandwidthUseMode ); // 44
            sb.Append( ',' );

            if( IsHidden ) sb.Append( 'h' ); // 45

            sb.Append( ',' );
            LastModified.ToUnixTimeString( sb ); // 46

            sb.Append( ',' );
            sb.AppendEscaped( DisplayedName ); // 47
        }

        #endregion


        #region Update Handlers

        public void ProcessMessageWritten() {
            Interlocked.Increment( ref MessagesWritten );
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogin( Player player ) {
            LastIP = player.IP;
            LastLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            Interlocked.Increment( ref TimesVisited );
            IsOnline = true;
            PlayerObject = player;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessFailedLogin( Player player ) {
            LastFailedLoginDate = DateTime.UtcNow;
            LastFailedLoginIP = player.IP;
            Interlocked.Increment( ref FailedLoginCount );
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogout( Player player ) {
            TotalTime += player.LastActiveTime.Subtract( player.LoginTime );
            LastSeen = DateTime.UtcNow;
            IsOnline = false;
            PlayerObject = null;
            LeaveReason = player.LeaveReason;
            LastModified = DateTime.UtcNow;
        }


        public bool ProcessBan( Player bannedBy, string banReason ) {
            if( !IsBanned ) {
                BanStatus = BanStatus.Banned;
                BannedBy = bannedBy.Name;
                BanDate = DateTime.UtcNow;
                BanReason = banReason;
                Interlocked.Increment( ref bannedBy.Info.TimesBannedOthers );
                Unmute();
                Unfreeze();
                IsHidden = false;
                LastModified = DateTime.UtcNow;
                return true;
            } else {
                return false;
            }
        }


        public bool ProcessUnban( string unbannedBy, string unbanReason ) {
            if( IsBanned ) {
                BanStatus = BanStatus.NotBanned;
                UnbannedBy = unbannedBy;
                UnbanDate = DateTime.UtcNow;
                UnbanReason = unbanReason;
                LastModified = DateTime.UtcNow;
                return true;
            } else {
                return false;
            }
        }


        public void ProcessRankChange( [NotNull] Rank newRank, string changer, string reason, RankChangeType type ) {
            if( newRank == null ) throw new ArgumentNullException( "newRank" );
            PreviousRank = Rank;
            Rank = newRank;
            RankChangeDate = DateTime.UtcNow;

            RankChangedBy = changer ?? "?";
            RankChangeReason = reason;
            RankChangeType = type;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessBlockPlaced( byte type ) {
            if( type == 0 ) { // air
                Interlocked.Increment( ref BlocksDeleted );
            } else {
                Interlocked.Increment( ref BlocksBuilt );
            }
            LastModified = DateTime.UtcNow;
        }


        public void ProcessDrawCommand( int blocksDrawn ) {
            Interlocked.Add( ref BlocksDrawn, blocksDrawn );
            LastModified = DateTime.UtcNow;
        }


        public void ProcessKick( Player kickedBy, string reason ) {
            Interlocked.Increment( ref TimesKicked );
            Interlocked.Increment( ref kickedBy.Info.TimesKickedOthers );
            LastKickDate = DateTime.UtcNow;
            LastKickBy = kickedBy.Name;
            LastKickReason = reason ?? "";
            Unfreeze();
            LastModified = DateTime.UtcNow;
        }

        #endregion


        #region Utilities

        public static string Escape( string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            } else if( str.IndexOf( ',' ) > -1 ) {
                return str.Replace( ',', '\xFF' );
            } else {
                return str;
            }
        }


        public static string UnescapeOldFormat( string str ) {
            return str.Replace( '\xFF', ',' ).Replace( "\'", "'" ).Replace( @"\\", @"\" );
        }


        public static string Unescape( string str ) {
            if( str.IndexOf( '\xFF' ) > -1 ) {
                return str.Replace( '\xFF', ',' );
            } else {
                return str;
            }
        }


        // implements IClassy interface
        public string ClassyName {
            get {
                StringBuilder sb = new StringBuilder();
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    sb.Append( Rank.Color );
                }
                if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                    sb.Append( Rank.Prefix );
                }
                if( DisplayedName.Length > 0 ) {
                    sb.Append( DisplayedName );
                } else {
                    sb.Append( Name );
                }
                if( IsBanned ) {
                    sb.Append( Color.Warning ).Append( '*' );
                }
                if( IsFrozen ) {
                    sb.Append( Color.Blue ).Append( '*' );
                }
                return sb.ToString();
            }
        }

        #endregion


        #region TimeSince_____ shortcuts

        public TimeSpan TimeSinceRankChange {
            get { return DateTime.UtcNow.Subtract( RankChangeDate ); }
        }

        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }

        public TimeSpan TimeSinceUnban {
            get { return DateTime.UtcNow.Subtract( UnbanDate ); }
        }

        public TimeSpan TimeSinceFirstLogin {
            get { return DateTime.UtcNow.Subtract( FirstLoginDate ); }
        }

        public TimeSpan TimeSinceLastLogin {
            get { return DateTime.UtcNow.Subtract( LastLoginDate ); }
        }

        public TimeSpan TimeSinceLastKick {
            get { return DateTime.UtcNow.Subtract( LastKickDate ); }
        }

        public TimeSpan TimeSinceLastSeen {
            get { return DateTime.UtcNow.Subtract( LastSeen ); }
        }

        public TimeSpan TimeSinceFrozen {
            get { return DateTime.UtcNow.Subtract( FrozenOn ); }
        }

        #endregion


        #region Actions

        public bool Mute( [NotNull] string by, TimeSpan timespan ) {
            if( by == null ) throw new ArgumentNullException( "by" );
            if( timespan <= TimeSpan.Zero ) {
                throw new ArgumentException( "Mute duration must be longer than 0", "timespan" );
            }
            DateTime newMutedUntil = DateTime.UtcNow.Add( timespan );
            if( newMutedUntil > MutedUntil ) {
                MutedUntil = newMutedUntil;
                MutedBy = by;
                LastModified = DateTime.UtcNow;
                return true;
            } else {
                return false;
            }
        }


        public bool Unmute() {
            if( IsMuted ) {
                MutedUntil = DateTime.MinValue;
                LastModified = DateTime.UtcNow;
                return true;
            } else {
                return false;
            }
        }


        public bool IsMuted {
            get {
                return DateTime.UtcNow < MutedUntil;
            }
        }


        public bool Freeze( [NotNull] string by ) {
            if( by == null ) throw new ArgumentNullException( "by" );
            if( !IsFrozen ) {
                IsFrozen = true;
                FrozenOn = DateTime.UtcNow;
                FrozenBy = by;
                LastModified = DateTime.UtcNow;
                return true;
            } else {
                return false;
            }
        }


        public bool Unfreeze() {
            if( IsFrozen ) {
                IsFrozen = false;
                return true;
            } else {
                return false;
            }
        }

        #endregion


        public override string ToString() {
            return String.Format( "PlayerInfo({0},{1})", Name, Rank.Name );
        }
    }


    sealed class PlayerInfoComparer : IComparer<PlayerInfo> {
        readonly Player observer;

        public PlayerInfoComparer( Player observer ) {
            this.observer = observer;
        }

        public int Compare( PlayerInfo x, PlayerInfo y ) {
            bool xIsOnline = x.IsOnline && observer.CanSee( x.PlayerObject );
            bool yIsOnline = y.IsOnline && observer.CanSee( y.PlayerObject );
            if( !xIsOnline && yIsOnline ) {
                return 1;
            } else if( xIsOnline && !yIsOnline ) {
                return -1;
            }

            if( x.Rank == y.Rank ) {
                return Math.Sign( y.LastSeen.Ticks - x.LastSeen.Ticks );
            } else {
                return x.Rank.Index - y.Rank.Index;
            }
        }
    }
}