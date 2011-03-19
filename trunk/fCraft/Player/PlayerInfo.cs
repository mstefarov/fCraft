// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace fCraft {
    public sealed class PlayerInfo : IClassy {

        public const int MinFieldCount = 24,
                         ExpectedFieldCount = 44;

        public string Name { get; private set; }

        public IPAddress LastIP = IPAddress.None;

        public Rank Rank;
        public DateTime RankChangeDate = DateTime.MinValue;
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
        public LeaveReason LeaveReason; // TODO
        public bool BanExempt;


        #region Constructors and Serialization

        // fabricate info for an unrecognized player
        public PlayerInfo( string name, Rank rank, bool setLoginDate, RankChangeType rankChangeType ) {
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
        public PlayerInfo( Player player ) {
            Name = player.Name;
            LastIP = player.Session.GetIP();
            Rank = RankList.DefaultRank;
            FirstLoginDate = DateTime.Now;
            LastSeen = DateTime.Now;
            LastLoginDate = DateTime.Now;
            ID = PlayerDB.GetNextID();
        }


        // load info from file
        public PlayerInfo( string[] fields ) {
            Name = fields[0];
            if( String.IsNullOrEmpty( fields[1] ) || !IPAddress.TryParse( fields[1], out LastIP ) ) { // LEGACY
                LastIP = IPAddress.None;
            }

            Rank = RankList.ParseRank( fields[2] ) ?? RankList.DefaultRank;
            if( fields[3] != "-" && !String.IsNullOrEmpty( fields[3] ) ) RankChangeDate = DateTime.Parse( fields[3] ); // LEGACY
            RankChangedBy = fields[4];
            if( RankChangedBy == "-" ) RankChangedBy = "";

            Banned = (fields[5] == "b");

            // ban information
            if( fields[6] != "-" && !String.IsNullOrEmpty( fields[6] ) && DateTime.TryParse( fields[6], out BanDate ) ) {
                BannedBy = fields[7];
                BanReason = Unescape( fields[10] );
                if( BanReason == "-" ) BanReason = "";
            }

            // unban information
            if( fields[8] != "-" && !String.IsNullOrEmpty( fields[8] ) && DateTime.TryParse( fields[8], out UnbanDate ) ) {
                UnbannedBy = fields[9];
                UnbanReason = Unescape( fields[11] );
                if( UnbanReason == "-" ) UnbanReason = "";
            }

            // failed logins
            if( fields[12] != "-" && !String.IsNullOrEmpty( fields[12] ) ) LastFailedLoginDate = DateTime.Parse( fields[12] ); // LEGACY
            if( fields[13] == "-" || String.IsNullOrEmpty( fields[13] ) || !IPAddress.TryParse( fields[13], out LastFailedLoginIP ) ) { // LEGACY
                LastFailedLoginIP = IPAddress.None;
            }
            FailedLoginCount = Int32.Parse( fields[14] );
            DateTime.TryParse( fields[15], out FirstLoginDate );

            // login/logout times
            DateTime.TryParse( fields[16], out LastLoginDate );
            TimeSpan.TryParse( fields[17], out TotalTime );

            // stats
            Int32.TryParse( fields[18], out BlocksBuilt );
            Int32.TryParse( fields[19], out BlocksDeleted );
            Int32.TryParse( fields[20], out TimesVisited );
            Int32.TryParse( fields[21], out LinesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) PreviousRank = RankList.ParseRank( fields[24] );
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
            }

            if( LastSeen < FirstLoginDate ) {
                LastSeen = FirstLoginDate;
            }
            if( LastLoginDate < FirstLoginDate ){
                LastLoginDate = FirstLoginDate;
            }
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
        public string Serialize() {
            string[] fields = new string[ExpectedFieldCount];

            fields[0] = Name;
            if( LastIP.ToString() != IPAddress.None.ToString() ) {
                fields[1] = LastIP.ToString();
            } else {
                fields[1] = "";
            }

            fields[2] = Rank.ToString();
            if( RankChangeDate == DateTime.MinValue ) fields[3] = "";
            else fields[3] = RankChangeDate.ToCompactString();
            fields[4] = RankChangedBy;

            if( Banned ) fields[5] = "b";
            else fields[5] = "";
            if( BanDate == DateTime.MinValue ) fields[6] = "";
            else fields[6] = BanDate.ToCompactString();
            fields[7] = BannedBy;
            if( UnbanDate == DateTime.MinValue ) fields[8] = "";
            else fields[8] = UnbanDate.ToCompactString();
            fields[9] = UnbannedBy;
            fields[10] = Escape( BanReason );
            fields[11] = Escape( UnbanReason );

            if( LastFailedLoginDate == DateTime.MinValue ) fields[12] = "";
            else fields[12] = LastFailedLoginDate.ToCompactString();
            if( LastFailedLoginIP == IPAddress.None ) fields[13] = "";
            else fields[13] = LastFailedLoginIP.ToString();
            fields[14] = FailedLoginCount.ToString();

            if( FirstLoginDate == DateTime.MinValue ) fields[15] = "";
            else fields[15] = FirstLoginDate.ToCompactString();
            if( LastLoginDate == DateTime.MinValue ) fields[16] = "";
            else fields[16] = LastLoginDate.ToCompactString();

            fields[17] = TotalTime.ToCompactString();

            fields[18] = BlocksBuilt.ToString();
            fields[19] = BlocksDeleted.ToString();
            fields[20] = TimesVisited.ToString();
            fields[21] = LinesWritten.ToString();

            // fields 22-23 are no longer in use
            fields[22] = "";
            fields[23] = "";

            if( PreviousRank != null ) fields[24] = PreviousRank.ToString();
            else fields[24] = "";

            fields[25] = Escape( RankChangeReason );
            fields[26] = TimesKicked.ToString();
            fields[27] = TimesKickedOthers.ToString();
            fields[28] = TimesBannedOthers.ToString();
            fields[29] = ID.ToString();
            fields[30] = ((int)RankChangeType).ToString();

            if( LastKickDate == DateTime.MinValue ) fields[31] = "";
            else fields[31] = LastKickDate.ToCompactString();

            if( LastSeen == DateTime.MinValue ) fields[32] = "";
            else if( Online ) fields[32] = DateTime.Now.ToCompactString();
            else fields[32] = LastSeen.ToCompactString();

            fields[33] = BlocksDrawn.ToString();

            fields[34] = LastKickBy;
            fields[35] = Escape( LastKickReason );

            if( BannedUntil == DateTime.MinValue ) fields[36] = "";
            else fields[36] = BannedUntil.ToCompactString();

            fields[37] = (IsFrozen ? "f" : "");

            fields[38] = Escape( FrozenBy );

            if( FrozenOn == DateTime.MinValue ) fields[39] = "";
            else fields[39] = FrozenOn.ToCompactString();

            if( MutedUntil == DateTime.MinValue ) fields[40] = "";
            else fields[40] = MutedUntil.ToCompactString();

            fields[41] = Escape( MutedBy );
            fields[42] = Escape( IRCPassword );
            fields[43] = (Online ? "o" : "");

            return String.Join( ",", fields );
        }

        #endregion


        #region Update Handlers

        public void ProcessLogin( Player player ) {
            LastIP = player.Session.GetIP();
            LastLoginDate = DateTime.Now;
            LastSeen = DateTime.Now;
            Interlocked.Increment( ref TimesVisited );
            Online = true;
        }


        public void ProcessFailedLogin( Player player ) {
            LastFailedLoginDate = DateTime.Now;
            LastFailedLoginIP = player.Session.GetIP();
            Interlocked.Increment( ref FailedLoginCount );
        }


        public void ProcessLogout( Player player ) {
            TotalTime += DateTime.Now.Subtract( player.Session.LoginTime );
            LastSeen = DateTime.Now;
            Online = false;
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

        public static string Escape( string str ) {
            return str.Replace( "\\", "\\\\" ).Replace( "'", "\\'" ).Replace( ',', '\xFF' );
        }


        public static string Unescape( string str ) {
            return str.Replace( '\xFF', ',' ).Replace( "\\'", "'" ).Replace( "\\\\", "\\" );
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