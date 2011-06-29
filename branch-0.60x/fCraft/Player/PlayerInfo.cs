// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace fCraft {
    public sealed class PlayerInfo : IClassy {

        public const int MinFieldCount = 24;

        public string Name { get; internal set; }

        public IPAddress LastIP;

        public Rank Rank;
        public DateTime RankChangeDate;

        public string RankChangedBy = "";

        public bool Banned;
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

        public int ID { get; private set; }
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

        public bool Online { get; private set; }
        public Player PlayerObject { get; private set; }
        public LeaveReason LeaveReason;
        public bool BanExempt;

        public BandwidthUseMode BandwidthUseMode { get; set; }


        #region Constructors and Serialization

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

        internal static PlayerInfo Load( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) { // LEGACY
                info.LastIP = IPAddress.None;
            }

            info.Rank = RankManager.ParseRank( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTime( ref info.RankChangeDate );
            info.RankChangedBy = fields[4];

            info.Banned = (fields[5] == "b");

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
            fields[17].ToTimeSpan( ref info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = RankManager.ParseRank( fields[24] );
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

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadOldFormat( string[] fields, bool convertDatesToUtc ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) { // LEGACY
                info.LastIP = IPAddress.None;
            }

            info.Rank = RankManager.ParseRank( fields[2] ) ?? RankManager.DefaultRank;
            DateTimeUtil.TryParseLocalDate( fields[3], out info.RankChangeDate );
            info.RankChangedBy = fields[4];
            if( info.RankChangedBy == "-" ) info.RankChangedBy = "";

            info.Banned = (fields[5] == "b");

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
                if( fields[24].Length > 0 ) info.PreviousRank = RankManager.ParseRank( fields[24] );
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
            if( !LastIP.Equals( IPAddress.None ) ) sb.Append( LastIP.ToString() ); // 1
            sb.Append( ',' );

            sb.Append( Rank.FullName ).Append( ',' ); // 2
            RankChangeDate.ToTickString( sb ).Append( ',' ); // 3

            Escape( RankChangedBy, sb ).Append( ',' ); // 4

            if( Banned ) sb.Append( 'b' ); // 5
            sb.Append( ',' );

            BanDate.ToTickString( sb ).Append( ',' ); // 6
            Escape( BannedBy, sb ).Append( ',' ); // 7
            UnbanDate.ToTickString( sb ).Append( ',' ); // 8
            Escape( UnbannedBy, sb ).Append( ',' ); // 9
            Escape( BanReason, sb ).Append( ',' ); // 10
            Escape( UnbanReason, sb ).Append( ',' ); // 11

            LastFailedLoginDate.ToTickString( sb ).Append( ',' ); // 12

            if( !LastFailedLoginIP.Equals( IPAddress.None ) ) sb.Append( LastFailedLoginIP.ToString() ); // 13
            sb.Append( ',' );

            if( FailedLoginCount > 0 ) sb.Append( FailedLoginCount ); // 14
            sb.Append( ',' );

            FirstLoginDate.ToTickString( sb ).Append( ',' ); // 15
            LastLoginDate.ToTickString( sb ).Append( ',' ); // 16
            TotalTime.ToTickString( sb ).Append( ',' ); // 17


            if( BlocksBuilt > 0 ) sb.Append( BlocksBuilt ); // 18
            sb.Append( ',' );

            if( BlocksDeleted > 0 ) sb.Append( BlocksDeleted ); // 19
            sb.Append( ',' );

            sb.Append( TimesVisited ).Append( ',' ); // 20


            if( MessagesWritten > 0 ) sb.Append( MessagesWritten ); // 21
            sb.Append( ',', 3 ); // 22-23 no longer in use

            if( PreviousRank != null ) sb.Append( PreviousRank.FullName ); // 24
            sb.Append( ',' );

            Escape( RankChangeReason, sb ).Append( ',' ); // 25


            if( TimesKicked > 0 ) sb.Append( TimesKicked ); // 26
            sb.Append( ',' );

            if( TimesKickedOthers > 0 ) sb.Append( TimesKickedOthers ); // 27
            sb.Append( ',' );

            if( TimesBannedOthers > 0 ) sb.Append( TimesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Append( ID ).Append( ',' ); // 29

            sb.Append( (int)RankChangeType ).Append( ',' ); // 30


            LastKickDate.ToTickString( sb ).Append( ',' ); // 31

            if( Online ) DateTime.UtcNow.ToTickString( sb ); // 32
            else LastSeen.ToTickString( sb );
            sb.Append( ',' );

            if( BlocksDrawn > 0 ) sb.Append( BlocksDrawn ); // 33
            sb.Append( ',' );

            Escape( LastKickBy, sb ).Append( ',' ); // 34

            Escape( LastKickReason, sb ).Append( ',' ); // 35

            BannedUntil.ToTickString( sb ); // 36

            if( IsFrozen ) {
                sb.Append( ',' ).Append( 'f' ).Append( ',' ); // 37
                Escape( FrozenBy, sb ).Append( ',' ); // 38
                FrozenOn.ToTickString( sb ).Append( ',' ); // 39
            } else {
                sb.Append( ',', 4 ); // 37-39
            }

            if( MutedUntil > DateTime.UtcNow ) {
                MutedUntil.ToTickString( sb ).Append( ',' ); // 40
                Escape( MutedBy, sb ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            Escape( Password, sb ).Append( ',' ); // 42

            if( Online ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( BandwidthUseMode != BandwidthUseMode.Default ) sb.Append( (int)BandwidthUseMode ); // 44
        }


        internal void SerializeOldFormat( string[] fields ) {
#if DEBUG
            string testGuid = Guid.NewGuid().ToString();
            for( int i = 0; i < fields.Length; i++ ) fields[i] = testGuid;
#endif

            fields[0] = Name;
            if( LastIP.ToString() != IPAddress.None.ToString() ) {
                fields[1] = LastIP.ToString();
            } else {
                fields[1] = "";
            }

            fields[2] = Rank.FullName;
            if( RankChangeDate == DateTime.MinValue ) fields[3] = "";
            else fields[3] = RankChangeDate.ToString();
            fields[4] = RankChangedBy;

            if( Banned ) fields[5] = "b";
            else fields[5] = "";

            if( BanDate == DateTime.MinValue ) fields[6] = "";
            else fields[6] = BanDate.ToString();

            fields[7] = BannedBy;
            if( UnbanDate == DateTime.MinValue ) fields[8] = "";

            else fields[8] = UnbanDate.ToString();
            fields[9] = UnbannedBy;

            if( BanReason.Length > 0 ) fields[10] = EscapeOldFormat( BanReason );
            else fields[10] = "";

            if( UnbanReason.Length > 0 ) fields[11] = EscapeOldFormat( UnbanReason );
            else fields[11] = "";

            if( LastFailedLoginDate == DateTime.MinValue ) fields[12] = "";
            else fields[12] = LastFailedLoginDate.ToString();

            if( LastFailedLoginIP.Equals( IPAddress.None ) ) fields[13] = "";
            else fields[13] = LastFailedLoginIP.ToString();

            if( FailedLoginCount > 0 ) fields[14] = FailedLoginCount.ToString();
            else fields[14] = "";

            if( FirstLoginDate == DateTime.MinValue ) fields[15] = "";
            else fields[15] = FirstLoginDate.ToString();

            if( LastLoginDate == DateTime.MinValue ) fields[16] = "";
            else fields[16] = LastLoginDate.ToString();

            if( TotalTime == TimeSpan.Zero ) fields[17] = "";
            else fields[17] = TotalTime.ToString();

            if( BlocksBuilt > 0 ) fields[18] = BlocksBuilt.ToString();
            else fields[18] = "";

            if( BlocksDeleted > 0 ) fields[19] = BlocksDeleted.ToString();
            else fields[19] = "";

            fields[20] = TimesVisited.ToString();

            if( MessagesWritten > 0 ) fields[21] = MessagesWritten.ToString();
            else fields[21] = "";

            // fields 22-23 are no longer in use
            fields[22] = "";
            fields[23] = "";

            if( PreviousRank != null ) fields[24] = PreviousRank.FullName;
            else fields[24] = "";

            if( RankChangeReason.Length > 0 ) fields[25] = EscapeOldFormat( RankChangeReason );
            else fields[25] = "";

            if( TimesKicked > 0 ) fields[26] = TimesKicked.ToString();
            else fields[26] = "";
            if( TimesKickedOthers > 0 ) fields[27] = TimesKickedOthers.ToString();
            else fields[27] = "";
            if( TimesBannedOthers > 0 ) fields[28] = TimesBannedOthers.ToString();
            else fields[28] = "";
            fields[29] = ID.ToString();
            fields[30] = ((int)RankChangeType).ToString();

            if( LastKickDate == DateTime.MinValue ) fields[31] = "";
            else fields[31] = LastKickDate.ToString();

            if( LastSeen == DateTime.MinValue ) fields[32] = "";
            else if( Online ) fields[32] = DateTime.Now.ToString(); // localized
            else fields[32] = LastSeen.ToString();

            if( BlocksDrawn > 0 ) fields[33] = BlocksDrawn.ToString();
            fields[33] = "";

            fields[34] = LastKickBy;
            if( LastKickReason.Length == 0 ) fields[35] = "";
            else fields[35] = EscapeOldFormat( LastKickReason );

            if( BannedUntil == DateTime.MinValue ) fields[36] = "";
            else fields[36] = BannedUntil.ToString();

            if( IsFrozen ) {
                fields[37] = "f";
                fields[38] = EscapeOldFormat( FrozenBy );
                fields[39] = FrozenOn.ToString();
            } else {
                fields[37] = "";
                fields[38] = "";
                fields[39] = "";
            }

            if( MutedUntil != DateTime.MinValue ) {
                fields[40] = MutedUntil.ToString();
                fields[41] = EscapeOldFormat( MutedBy );
            } else {
                fields[40] = "";
                fields[41] = "";
            }

            if( !String.IsNullOrEmpty( Password ) ) fields[42] = EscapeOldFormat( Password );
            else fields[42] = "";

            fields[43] = (Online ? "o" : "");

            fields[44] = (BandwidthUseMode == BandwidthUseMode.Default ? "" : ((int)BandwidthUseMode).ToString());

#if DEBUG
            for( int i = 0; i < fields.Length; i++ ) {
                if( fields[i] == null || fields[i] == testGuid ) {
                    throw new Exception( "PlayerInfo did not save one of the fields properly." );
                }
            }
#endif
        }

        #endregion


        #region Update Handlers

        public void ProcessMessageWritten() {
            Interlocked.Increment( ref MessagesWritten );
        }

        public void ProcessLogin( Player player ) {
            LastIP = player.Session.IP;
            LastLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            Interlocked.Increment( ref TimesVisited );
            Online = true;
            PlayerObject = player;
        }


        public void ProcessFailedLogin( Session session ) {
            LastFailedLoginDate = DateTime.UtcNow;
            LastFailedLoginIP = session.IP;
            Interlocked.Increment( ref FailedLoginCount );
        }


        public void ProcessLogout( Session session ) {
            TotalTime += DateTime.UtcNow.Subtract( session.LoginTime );
            LastSeen = DateTime.UtcNow;
            Online = false;
            PlayerObject = null;
            LeaveReason = session.LeaveReason;
        }


        public bool ProcessBan( Player bannedBy, string banReason ) {
            if( !Banned ) {
                Banned = true;
                BannedBy = bannedBy.Name;
                BanDate = DateTime.UtcNow;
                BanReason = banReason;
                Interlocked.Increment( ref bannedBy.Info.TimesBannedOthers );
                return true;
            } else {
                return false;
            }
        }


        public bool ProcessUnban( string unbannedBy, string unbanReason ) {
            if( Banned ) {
                Banned = false;
                UnbannedBy = unbannedBy;
                UnbanDate = DateTime.UtcNow;
                UnbanReason = unbanReason;
                return true;
            } else {
                return false;
            }
        }


        public void ProcessRankChange( Rank newRank, Player changer, string reason, RankChangeType type ) {
            PreviousRank = Rank;
            Rank = newRank;
            RankChangeDate = DateTime.UtcNow;
            RankChangedBy = changer.Name;
            RankChangeReason = reason;
            RankChangeType = type;
        }


        public void ProcessBlockPlaced( byte type ) {
            if( type == 0 ) { // air
                Interlocked.Increment( ref BlocksDeleted );
            } else {
                Interlocked.Increment( ref BlocksBuilt );
            }
        }


        public void ProcessDrawCommand( int blocksDrawn ) {
            Interlocked.Add( ref BlocksDrawn, blocksDrawn );
        }


        public void ProcessKick( Player kickedBy, string reason ) {
            Interlocked.Increment( ref TimesKicked );
            Interlocked.Increment( ref kickedBy.Info.TimesKickedOthers );
            LastKickDate = DateTime.UtcNow;
            LastKickBy = kickedBy.Name;
            LastKickReason = reason ?? "";
            Unfreeze();
        }

        #endregion


        #region Utilities


        public static string EscapeOldFormat( string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            } else {
                return str.Replace( @"\", @"\\" ).Replace( "'", @"\'" ).Replace( ',', '\xFF' );
            }
        }


        public static string Escape( string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            }else if( str.IndexOf( ',' ) > -1 ) {
                return str.Replace( ',', '\xFF' );
            } else {
                return str;
            }
        }


        public static StringBuilder Escape( string str, StringBuilder sb ) {
            if( !String.IsNullOrEmpty( str ) ) {
                if( str.IndexOf( ',' ) > -1 ) {
                    int startIndex = sb.Length;
                    sb.Append( str );
                    sb.Replace( ',', '\xFF', startIndex, str.Length );
                } else {
                    sb.Append( str );
                }
            }
            return sb;
        }


        public static string UnescapeOldFormat( string str ) {
            return str.Replace( '\xFF', ',' ).Replace( @"\'", "'" ).Replace( @"\\", @"\" );
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
                if( ConfigKey.RankColorsInChat.GetBool() ) {
                    sb.Append( Rank.Color );
                }
                if( ConfigKey.RankPrefixesInChat.GetBool() ) {
                    sb.Append( Rank.Prefix );
                }
                sb.Append( Name );
                if( Banned ) {
                    sb.Append( Color.Warning ).Append( "*" );
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

        public TimeSpan TimeSinceLastFailedLogin {
            get { return DateTime.UtcNow.Subtract( LastFailedLoginDate ); }
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

        public bool Mute( string by, TimeSpan timespan ) {
            if( by == null ) throw new ArgumentNullException( "by" );
            DateTime newMutedUntil = DateTime.UtcNow.Add( timespan );
            if( newMutedUntil > MutedUntil ) {
                MutedUntil = newMutedUntil;
                MutedBy = by;
                return true;
            } else {
                return false;
            }
        }


        public bool Unmute() {
            if( IsMuted ) {
                MutedUntil = DateTime.MinValue;
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


        public bool Freeze( string by ) {
            if( by == null ) throw new ArgumentNullException( "by" );
            if( !IsFrozen ) {
                IsFrozen = true;
                FrozenOn = DateTime.UtcNow;
                FrozenBy = by;
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
}


namespace fCraft.Events {

    public class PlayerInfoEventArgs : EventArgs {
        public PlayerInfoEventArgs( PlayerInfo playerInfo ) {
            PlayerInfo = playerInfo;
        }
        public PlayerInfo PlayerInfo { get; protected set; }
    }


    public sealed class PlayerInfoCreatingEventArgs : EventArgs, ICancellableEvent {
        public PlayerInfoCreatingEventArgs( string name, IPAddress ip, Rank startingRank, bool isUnrecognized ) {
            Name = name;
            StartingRank = startingRank;
            IP = ip;
            IsUnrecognized = isUnrecognized;
        }
        public string Name { get; private set; }
        public Rank StartingRank { get; set; }
        public IPAddress IP { get; private set; }
        public bool IsUnrecognized { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoCreatedEventArgs : PlayerInfoEventArgs {
        public PlayerInfoCreatedEventArgs( PlayerInfo playerInfo, bool isUnrecognized )
            : base( playerInfo ) {
            IsUnrecognized = isUnrecognized;
        }
        public bool IsUnrecognized { get; private set; }
    }


    public class PlayerInfoRankChangedEventArgs : PlayerInfoEventArgs {
        public PlayerInfoRankChangedEventArgs( PlayerInfo playerInfo, Player rankChanger, Rank oldRank, string reason, RankChangeType rankChangeType )
            : base( playerInfo ) {
            RankChanger = rankChanger;
            OldRank = oldRank;
            NewRank = playerInfo.Rank;
            Reason = reason;
            RankChangeType = rankChangeType;
        }

        public Player RankChanger { get; private set; }
        public Rank OldRank { get; protected set; }
        public Rank NewRank { get; protected set; }
        public string Reason { get; private set; }
        public RankChangeType RankChangeType { get; private set; }
    }


    public sealed class PlayerInfoRankChangingEventArgs : PlayerInfoRankChangedEventArgs, ICancellableEvent {
        public PlayerInfoRankChangingEventArgs( PlayerInfo playerInfo, Player rankChanger, Rank newRank, string reason, RankChangeType rankChangeType )
            : base( playerInfo, rankChanger, playerInfo.Rank, reason, rankChangeType ) {
            NewRank = newRank;
        }
        public bool Cancel { get; set; }
    }
}