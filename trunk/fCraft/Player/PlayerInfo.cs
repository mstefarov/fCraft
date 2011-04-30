// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace fCraft {
    public sealed class PlayerInfo : IClassy {

        public const int MinFieldCount = 24,
                         ExpectedFieldCount = 45;

        public string Name { get; private set; }

        public IPAddress LastIP;

        public Rank Rank;
        public DateTime RankChangeDate;
        public string RankChangedBy = "";

        public bool Banned;
        public DateTime BanDate = DateTime.MinValue;
        public string BannedBy = "";
        public DateTime UnbanDate = DateTime.MinValue;
        public string UnbannedBy = "";
        public string BanReason = "";
        public string UnbanReason = "";

        public DateTime LastFailedLoginDate = DateTime.MinValue;
        public IPAddress LastFailedLoginIP = IPAddress.None;
        public int FailedLoginCount;
        public DateTime FirstLoginDate = DateTime.MinValue;
        public DateTime LastLoginDate = DateTime.MinValue;

        public TimeSpan TotalTime = TimeSpan.Zero;
        public int BlocksBuilt;
        public int BlocksDeleted;
        public int TimesVisited;
        public int LinesWritten;

        public Rank PreviousRank;
        public string RankChangeReason = "";
        public int TimesKicked;
        public int TimesKickedOthers;
        public int TimesBannedOthers;

        public int ID;
        public RankChangeType RankChangeType = RankChangeType.Default;
        public DateTime LastKickDate = DateTime.MinValue;
        public DateTime LastSeen = DateTime.MinValue;
        public long BlocksDrawn;

        public string LastKickBy = "";
        public string LastKickReason = "";

        // TODO: start tracking
        public DateTime BannedUntil = DateTime.MinValue;
        public bool IsFrozen;
        public string FrozenBy = "";
        public DateTime FrozenOn = DateTime.MinValue;
        public DateTime MutedUntil = DateTime.MinValue;
        public string MutedBy = "";

        public string IRCPassword = ""; // TODO

        public bool Online;
        public Player PlayerObject;
        public LeaveReason LeaveReason;
        public bool BanExempt;

        public BandwidthUseMode BandwidthUseMode = BandwidthUseMode.Default;


        #region Constructors and Serialization


        PlayerInfo() {
            LastIP = IPAddress.None;
            RankChangeDate = DateTime.MinValue;
        }

        // fabricate info for an unrecognized player
        public PlayerInfo( string name, Rank rank, bool setLoginDate, RankChangeType rankChangeType )
            : this() {
            Name = name;
            Rank = rank;
            if( setLoginDate ) {
                FirstLoginDate = DateTime.Now;
                LastLoginDate = FirstLoginDate;
                LastSeen = FirstLoginDate;
                TimesVisited = 1;
            }
            RankChangeType = rankChangeType;
        }


        // generate blank info for a new player
        public PlayerInfo( string name, IPAddress lastIP )
            : this() {
            Name = name;
            Rank = RankManager.DefaultRank;
            FirstLoginDate = DateTime.Now;
            LastSeen = DateTime.Now;
            LastLoginDate = DateTime.Now;
            ID = PlayerDB.GetNextID();
            LastIP = lastIP;
        }


        // load info from file
        internal PlayerInfo( string[] fields )
            : this() {
            Name = fields[0];
            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out LastIP ) ) { // LEGACY
                LastIP = IPAddress.None;
            }

            Rank = RankManager.ParseRank( fields[2] ) ?? RankManager.DefaultRank;
            if( fields[3].Length > 1 ) {
                RankChangeDate = DateTime.Parse( fields[3] );
            }
            RankChangedBy = fields[4];
            if( RankChangedBy == "-" ) RankChangedBy = "";

            Banned = (fields[5] == "b");

            // ban information
            if( fields[6].Length > 1 && DateTime.TryParse( fields[6], out BanDate ) ) {
                BannedBy = fields[7];
                BanReason = Unescape( fields[10] );
                if( BanReason == "-" ) BanReason = "";
            }

            // unban information
            if( fields[8].Length > 1 && DateTime.TryParse( fields[8], out UnbanDate ) ) {
                UnbannedBy = fields[9];
                UnbanReason = Unescape( fields[11] );
                if( UnbanReason == "-" ) UnbanReason = "";
            }

            // failed logins
            if( fields[12].Length > 1 ) LastFailedLoginDate = DateTime.Parse( fields[12] ); // LEGACY
            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out LastFailedLoginIP ) ) { // LEGACY
                LastFailedLoginIP = IPAddress.None;
            }
            if( fields[14].Length > 0 ) FailedLoginCount = Int32.Parse( fields[14] );
            DateTime.TryParse( fields[15], out FirstLoginDate );

            // login/logout times
            DateTime.TryParse( fields[16], out LastLoginDate );
            TimeSpan.TryParse( fields[17], out TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out BlocksDeleted );
            Int32.TryParse( fields[20], out TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out LinesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) PreviousRank = RankManager.ParseRank( fields[24] );
                if( fields[25].Length > 0 ) RankChangeReason = Unescape( fields[25] );
                Int32.TryParse( fields[26], out TimesKicked );
                Int32.TryParse( fields[27], out TimesKickedOthers );
                Int32.TryParse( fields[28], out TimesBannedOthers );
                if( fields.Length > 29 ) {
                    ID = Int32.Parse( fields[29] );
                    if( ID < 256 )
                        ID = PlayerDB.GetNextID();
                    int rankChangeTypeCode;
                    if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        RankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            GuessRankChangeType();
                        }
                    } else {
                        GuessRankChangeType();
                    }
                    DateTime.TryParse( fields[31], out LastKickDate );
                    if( !DateTime.TryParse( fields[32], out LastSeen ) || LastSeen < LastLoginDate ) {
                        LastSeen = LastLoginDate;
                    }
                    Int64.TryParse( fields[33], out BlocksDrawn );

                    LastKickBy = fields[34];
                    LastKickReason = Unescape( fields[35] );

                } else {
                    ID = PlayerDB.GetNextID();
                    GuessRankChangeType();
                    LastSeen = LastLoginDate;
                }

                if( fields.Length > 36 ) {
                    DateTime.TryParse( fields[36], out BannedUntil );
                    IsFrozen = (fields[37] == "f");
                    FrozenBy = Unescape( fields[38] );
                    DateTime.TryParse( fields[39], out FrozenOn );
                    DateTime.TryParse( fields[40], out MutedUntil );
                    MutedBy = Unescape( fields[41] );
                    IRCPassword = Unescape( fields[42] );
                    // fields[43] is "online", and is ignored
                }

                if( fields.Length > 44 ) {
                    if( fields[44].Length != 0 ) {
                        BandwidthUseMode = (BandwidthUseMode)Int32.Parse( fields[44] );
                    }
                }
            }

            if( LastSeen < FirstLoginDate ) {
                LastSeen = FirstLoginDate;
            }
            if( LastLoginDate < FirstLoginDate ) {
                LastLoginDate = FirstLoginDate;
            }

            if( RankChangeDate != DateTime.MinValue ) RankChangeDate = RankChangeDate.ToUniversalTime();
            if( BanDate != DateTime.MinValue ) BanDate = BanDate.ToUniversalTime();
            if( UnbanDate != DateTime.MinValue ) UnbanDate = UnbanDate.ToUniversalTime();
            if( LastFailedLoginDate != DateTime.MinValue ) LastFailedLoginDate = LastFailedLoginDate.ToUniversalTime();
            if( FirstLoginDate != DateTime.MinValue ) FirstLoginDate = FirstLoginDate.ToUniversalTime();
            if( LastLoginDate != DateTime.MinValue ) LastLoginDate = LastLoginDate.ToUniversalTime();
            if( LastKickDate != DateTime.MinValue ) LastKickDate = LastKickDate.ToUniversalTime();
            if( LastSeen != DateTime.MinValue ) LastSeen = LastSeen.ToUniversalTime();
            if( BannedUntil != DateTime.MinValue ) BannedUntil = BannedUntil.ToUniversalTime();
            if( FrozenOn != DateTime.MinValue ) FrozenOn = FrozenOn.ToUniversalTime();
            if( MutedUntil != DateTime.MinValue ) MutedUntil = MutedUntil.ToUniversalTime();
        }


        internal static PlayerInfo LoadOldFormat( string[] fields ) {
            return new PlayerInfo( fields );
        }


        internal static PlayerInfo LoadNewFormat( string[] fields ) {
            PlayerInfo info = new PlayerInfo();

            info.Name = fields[0];
            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) { // LEGACY
                info.LastIP = IPAddress.None;
            }

            info.Rank = RankManager.ParseRank( fields[2] ) ?? RankManager.DefaultRank;
            DateFromString( fields[3], ref info.RankChangeDate );
            info.RankChangedBy = fields[4];

            info.Banned = (fields[5] == "b");

            // ban information
            if( DateFromString( fields[6], ref info.BanDate ) ) {
                info.BannedBy = Unescape(fields[7]);
                info.BanReason = Unescape( fields[10] );
            }

            // unban information
            if( DateFromString( fields[8], ref info.UnbanDate ) ) {
                info.UnbannedBy = Unescape(fields[9]);
                info.UnbanReason = Unescape( fields[11] );
            }

            // failed logins
            DateFromString( fields[12], ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            if( fields[14].Length > 0 ) info.FailedLoginCount = Int32.Parse( fields[14] );
            DateFromString( fields[15], ref info.FirstLoginDate );

            // login/logout times
            DateFromString( fields[16], ref info.LastLoginDate );
            TimeFromString( fields[17], ref info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.LinesWritten );
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

            DateFromString( fields[31], ref info.LastKickDate );
            if(!DateFromString( fields[32], ref info.LastSeen) || info.LastSeen < info.LastLoginDate ){
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );

            info.LastKickBy = Unescape( fields[34]);
            info.LastKickReason = Unescape( fields[35] );

            DateFromString( fields[36], ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            info.FrozenBy = Unescape( fields[38] );
            DateFromString( fields[39], ref info.FrozenOn );
            DateFromString( fields[40], ref info.MutedUntil );
            info.MutedBy = Unescape( fields[41] );
            info.IRCPassword = Unescape( fields[42] );
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


        // save to file
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

            fields[2] = Rank.ToString();
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

            if( BanReason.Length > 0 ) fields[10] = Escape( BanReason );
            else fields[10] = "";

            if( UnbanReason.Length > 0 ) fields[11] = Escape( UnbanReason );
            else fields[11] = "";

            if( LastFailedLoginDate == DateTime.MinValue ) fields[12] = "";
            else fields[12] = LastFailedLoginDate.ToString();

            if( LastFailedLoginIP == IPAddress.None ) fields[13] = "";
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

            if( LinesWritten > 0 ) fields[21] = LinesWritten.ToString();
            else fields[21] = "";

            // fields 22-23 are no longer in use
            fields[22] = "";
            fields[23] = "";

            if( PreviousRank != null ) fields[24] = PreviousRank.ToString();
            else fields[24] = "";

            if( RankChangeReason.Length > 0 ) fields[25] = Escape( RankChangeReason );
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
            else if( Online ) fields[32] = DateTime.Now.ToString();
            else fields[32] = LastSeen.ToString();

            if( BlocksDrawn > 0 ) fields[33] = BlocksDrawn.ToString();
            fields[33] = "";

            fields[34] = LastKickBy;
            if( LastKickReason.Length == 0 ) fields[35] = "";
            else fields[35] = Escape( LastKickReason );

            if( BannedUntil == DateTime.MinValue ) fields[36] = "";
            else fields[36] = BannedUntil.ToString();

            if( IsFrozen ) {
                fields[37] = "f";
                fields[38] = Escape( FrozenBy );
                fields[39] = FrozenOn.ToString();
            } else {
                fields[37] = "";
                fields[38] = "";
                fields[39] = "";
            }

            if( MutedUntil != DateTime.MinValue ) {
                fields[40] = MutedUntil.ToString();
                fields[41] = Escape( MutedBy );
            } else {
                fields[40] = "";
                fields[41] = "";
            }

            if( !String.IsNullOrEmpty( IRCPassword ) ) fields[42] = Escape( IRCPassword );
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


        internal void SerializeNewFormat( string[] fields ) {
#if DEBUG
            string testGuid = Guid.NewGuid().ToString();
            for( int i = 0; i < fields.Length; i++ ) fields[i] = testGuid;
#endif

            fields[0] = Name;
            if( LastIP.Equals( IPAddress.None ) ) fields[1] = "";
            else fields[1] = LastIP.ToString();

            fields[2] = Rank.ToString();
            fields[3] = DateToString( RankChangeDate );
            fields[4] = Escape(RankChangedBy);

            fields[5] = (Banned ? "b" : "");
            fields[6] = DateToString( BanDate );
            fields[7] = Escape(BannedBy);
            fields[8] = DateToString( UnbanDate );
            fields[9] = Escape(UnbannedBy);

            if( BanReason.Length > 0 ) fields[10] = Escape( BanReason );
            else fields[10] = "";

            if( UnbanReason.Length > 0 ) fields[11] = Escape( UnbanReason );
            else fields[11] = "";

            fields[12] = DateToString( LastFailedLoginDate );

            if( LastFailedLoginIP.Equals(IPAddress.None) ) fields[13] = "";
            else fields[13] = LastFailedLoginIP.ToString();

            if( FailedLoginCount > 0 ) fields[14] = FailedLoginCount.ToString();
            else fields[14] = "";

            fields[15] = DateToString( FirstLoginDate );
            fields[16] = DateToString( LastLoginDate );
            fields[17] = TimeToString( TotalTime );

            if( BlocksBuilt > 0 ) fields[18] = BlocksBuilt.ToString();
            else fields[18] = "";

            if( BlocksDeleted > 0 ) fields[19] = BlocksDeleted.ToString();
            else fields[19] = "";

            fields[20] = TimesVisited.ToString();

            if( LinesWritten > 0 ) fields[21] = LinesWritten.ToString();
            else fields[21] = "";

            // fields 22-23 are no longer in use
            fields[22] = "";
            fields[23] = "";

            if( PreviousRank != null ) fields[24] = PreviousRank.ToString();
            else fields[24] = "";

            if( RankChangeReason.Length > 0 ) fields[25] = Escape( RankChangeReason );
            else fields[25] = "";

            if( TimesKicked > 0 ) fields[26] = TimesKicked.ToString();
            else fields[26] = "";
            if( TimesKickedOthers > 0 ) fields[27] = TimesKickedOthers.ToString();
            else fields[27] = "";
            if( TimesBannedOthers > 0 ) fields[28] = TimesBannedOthers.ToString();
            else fields[28] = "";
            fields[29] = ID.ToString();
            fields[30] = ((int)RankChangeType).ToString();

            fields[31] = DateToString( LastKickDate );

            if( Online ) fields[32] = DateToString( DateTime.UtcNow );
            else fields[32] = DateToString( LastSeen );

            if( BlocksDrawn > 0 ) fields[33] = BlocksDrawn.ToString();
            fields[33] = "";

            fields[34] = Escape(LastKickBy);
            fields[35] = Escape( LastKickReason );

            fields[36] = DateToString( BannedUntil );

            if( IsFrozen ) {
                fields[37] = "f";
                fields[38] = Escape( FrozenBy );
                fields[39] = DateToString( FrozenOn );
            } else {
                fields[37] = "";
                fields[38] = "";
                fields[39] = "";
            }

            if( MutedUntil != DateTime.MinValue ) {
                fields[40] = DateToString( MutedUntil );
                fields[41] = Escape( MutedBy );
            } else {
                fields[40] = "";
                fields[41] = "";
            }

            if( !String.IsNullOrEmpty( IRCPassword ) ) fields[42] = Escape( IRCPassword );
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

        public void ProcessLogin( Player player ) {
            LastIP = player.Session.IP;
            LastLoginDate = DateTime.Now;
            LastSeen = DateTime.Now;
            Interlocked.Increment( ref TimesVisited );
            Online = true;
            PlayerObject = player;
        }


        public void ProcessFailedLogin( Session session ) {
            LastFailedLoginDate = DateTime.Now;
            LastFailedLoginIP = session.IP;
            Interlocked.Increment( ref FailedLoginCount );
        }


        public void ProcessLogout( Session session ) {
            TotalTime += DateTime.Now.Subtract( session.LoginTime );
            LastSeen = DateTime.Now;
            Online = false;
            PlayerObject = null;
        }


        public bool ProcessBan( Player bannedBy, string banReason ) {
            if( !Banned ) {
                Banned = true;
                BannedBy = bannedBy.Name;
                BanDate = DateTime.Now;
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
                UnbanDate = DateTime.Now;
                UnbanReason = unbanReason;
                return true;
            } else {
                return false;
            }
        }


        public void ProcessRankChange( Rank newRank, Player changer, string reason, RankChangeType type ) {
            PreviousRank = Rank;
            Rank = newRank;
            RankChangeDate = DateTime.Now;
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
            LastKickDate = DateTime.Now;
            LastKickBy = kickedBy.Name;
            LastKickReason = reason ?? "";
            Unfreeze();
        }

        #endregion


        #region Utilities

        const int TicksToSeconds = 10000;


        static bool DateFromString( string str, ref DateTime date ) {
            if( str.Length > 1 ) {
                date = new DateTime( Int64.Parse( str ) * TicksToSeconds, DateTimeKind.Utc );
                return true;
            } else {
                return false;
            }
        }


        static bool TimeFromString( string str, ref TimeSpan date ) {
            if( str.Length > 1 ) {
                date = new TimeSpan( Int64.Parse( str ) * TicksToSeconds );
                return true;
            } else {
                return false;
            }
        }


        static string DateToString( DateTime date ) {
            if( date == DateTime.MinValue ) {
                return "";
            } else {
                return (date.Ticks / TicksToSeconds).ToString();
            }
        }


        static string TimeToString( TimeSpan time ) {
            if( time == TimeSpan.Zero ) {
                return "";
            } else {
                return (time.Ticks / TicksToSeconds).ToString();
            }
        }


        public static string Escape( string str ) {
            return str.Replace( @"\", @"\\" ).Replace( "'", @"\'" ).Replace( ',', '\xFF' );
        }


        public static string Unescape( string str ) {
            return str.Replace( '\xFF', ',' ).Replace( @"\'", "'" ).Replace( @"\\", @"\" );
        }


        // implements IClassy interface
        public string GetClassyName() {
            StringBuilder sb = new StringBuilder();
            if( ConfigKey.RankColorsInChat.GetBool() ) {
                if( Name == "fragmer" ) return "&4f&cr&ea&ag&bm&9e&5r";
                if( Name == "Kirshi" ) return "&bKir&dshi";
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


        public static string PlayerInfoArrayToString( PlayerInfo[] list ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            for( int i = 0; i < list.Length; i++ ) {
                if( !first ) sb.Append( "&S, " );
                sb.Append( list[i].GetClassyName() );
                first = false;
            }
            return sb.ToString();
        }

        #endregion


        #region Actions

        public void Mute( string by, int seconds ) {
            if( by == null ) throw new ArgumentNullException( "by" );
            MutedUntil = DateTime.UtcNow.AddSeconds( seconds );
            MutedBy = by;
        }

        public void Unmute() {
            MutedUntil = DateTime.UtcNow;
        }

        public bool IsMuted() {
            return DateTime.UtcNow < MutedUntil;
        }

        public bool Freeze( string by ) {
            if( by == null ) throw new ArgumentNullException( "by" );
            if( !IsFrozen ) {
                IsFrozen = true;
                FrozenOn = DateTime.Now;
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
            return String.Format( "PlayerInfo({0},{1})", Rank.Name, Name );
        }
    }
}