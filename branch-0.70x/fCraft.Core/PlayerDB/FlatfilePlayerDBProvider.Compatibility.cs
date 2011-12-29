// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    partial class FlatfilePlayerDBProvider {
        public const int MinFieldCount = 24;
        const long TicksPerMillisecond = 10000;

        #region Loading

        int IdentifyFormatVersion( [NotNull] string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            if( header.StartsWith( "playerName" ) ) return 0;
            string[] headerParts = header.Split( ' ' );
            if( headerParts.Length < 2 ) {
                throw new SerializationException( "Invalid PlayerDB header format: " + header );
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


        internal PlayerInfo LoadFormat2( [NotNull] string[] fields ) {
            if( fields == null ) throw new ArgumentNullException( "fields" );
            int id = Int32.Parse( fields[29] );
            if( id < 256 ) id = GetNextId();

            PlayerInfo info = new PlayerInfo( id ) {
                Name = fields[0]
            };

            if( fields[1].Length > 0 ) {
                IPAddress lastIP;
                IPAddress.TryParse( fields[1], out lastIP );
                info.LastIP = lastIP;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTime tempDate;
            fields[3].ToDateTime( out tempDate );
            info.RankChangeDate = tempDate;
            if( fields[4].Length > 0 ) info.RankChangedBy = fields[4];

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
            if( fields[6].ToDateTime( out tempDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = Unescape( fields[10] );
            }
            info.BanDate = tempDate;

            // unban information
            if( fields[8].ToDateTime( out tempDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = Unescape( fields[11] );
            }
            info.UnbanDate = tempDate;

            // failed logins
            DateTime lastFailedLoginDate;
            fields[12].ToDateTime( out lastFailedLoginDate );
            info.LastFailedLoginDate = lastFailedLoginDate;

            if( fields[13].Length > 1 ) {
                IPAddress lastFailedLoginIP;
                IPAddress.TryParse( fields[13], out lastFailedLoginIP );
                info.LastFailedLoginIP = lastFailedLoginIP;
            }
            // skip 14

            // login/logout dates
            TimeSpan totalTime;
            fields[15].ToDateTime( out tempDate );
            info.FirstLoginDate = tempDate;
            fields[16].ToDateTime( out tempDate );
            info.LastLoginDate = tempDate;
            fields[17].ToTimeSpan( out totalTime );
            info.TotalTime = totalTime;

            // stats
            int tempInt;
            if( fields[18].Length > 0 ) {
                Int32.TryParse( fields[18], out tempInt );
                info.BlocksBuilt = tempInt;
            }
            if( fields[19].Length > 0 ) {
                Int32.TryParse( fields[19], out tempInt );
                info.BlocksDeleted = tempInt;
            }
            Int32.TryParse( fields[20], out tempInt );
            info.TimesVisited = tempInt;
            if( fields[20].Length > 0 ) {
                Int32.TryParse( fields[21], out tempInt );
                info.MessagesWritten = tempInt;
            }
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out tempInt );
            info.TimesKicked = tempInt;
            Int32.TryParse( fields[27], out tempInt );
            info.TimesKickedOthers = tempInt;
            Int32.TryParse( fields[28], out tempInt );
            info.TimesBannedOthers = tempInt;
            // fields[29] is ID, read above

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    GuessRankChangeType( info );
                }
            } else {
                GuessRankChangeType( info );
            }

            fields[31].ToDateTime( out tempDate );
            info.LastKickDate = tempDate;
            if( !fields[32].ToDateTime( out tempDate ) ) {
                tempDate = info.LastLoginDate;
            }
            info.LastSeen = tempDate;
            long blocksDrawn;
            Int64.TryParse( fields[33], out blocksDrawn );
            info.BlocksDrawn = blocksDrawn;

            if( fields[34].Length > 0 ) info.LastKickBy = Unescape( fields[34] );
            if( fields[35].Length > 0 ) info.LastKickReason = Unescape( fields[35] );

            fields[36].ToDateTime( out tempDate );
            info.BannedUntil = tempDate;
            info.IsFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.FrozenBy = Unescape( fields[38] );
            fields[39].ToDateTime( out tempDate );
            info.FrozenOn = tempDate;
            fields[40].ToDateTime( out tempDate );
            info.MutedUntil = tempDate;
            if( fields[41].Length > 0 ) info.MutedBy = Unescape( fields[41] );
            info.Password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            ParseBandwidthUseMode( info, fields[44] );

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }
            if( fields.Length > 46 ) {
                DateTime tempLastModified;
                fields[46].ToDateTime( out tempLastModified );
                info.LastModified = tempLastModified;
            }
            if( fields.Length > 47 && fields[47].Length > 0 ) {
                info.DisplayedName = Unescape( fields[47] );
            }

            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }
            if( info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }

            return info;
        }


        internal PlayerInfo LoadFormat1( [NotNull] string[] fields ) {
            if( fields == null ) throw new ArgumentNullException( "fields" );
            int id = Int32.Parse( fields[29] );
            if( id < 256 ) id = GetNextId();

            PlayerInfo info = new PlayerInfo( id ) { Name = fields[0] };

            if( fields[1].Length > 0 ) {
                IPAddress lastIP;
                IPAddress.TryParse( fields[1], out lastIP );
                info.LastIP = lastIP;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTime tempDate;
            ToDateTimeLegacy( fields[3], out tempDate );
            info.RankChangeDate = tempDate;
            if( fields[4].Length > 0 ) info.RankChangedBy = fields[4];

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
            if( ToDateTimeLegacy( fields[6], out tempDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = Unescape( fields[10] );
            }
            info.BanDate = tempDate;

            // unban information
            if( ToDateTimeLegacy( fields[8], out tempDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = Unescape( fields[11] );
            }
            info.UnbanDate = tempDate;

            // failed logins
            ToDateTimeLegacy( fields[12], out tempDate );
            info.LastFailedLoginDate = tempDate;

            if( fields[13].Length > 1 ) {
                IPAddress lastFailedLoginIP;
                IPAddress.TryParse( fields[13], out lastFailedLoginIP );
                info.LastFailedLoginIP = lastFailedLoginIP;
            }
            // skip 14

            // login/logout times
            ToDateTimeLegacy( fields[15], out tempDate );
            info.FirstLoginDate = tempDate;
            ToDateTimeLegacy( fields[16], out tempDate );
            info.LastLoginDate = tempDate;
            TimeSpan totalTime;
            ToTimeSpanLegacy( fields[17], out totalTime );
            info.TotalTime = totalTime;

            // stats
            int tempInt;
            if( fields[18].Length > 0 ) {
                Int32.TryParse( fields[18], out tempInt );
                info.BlocksBuilt = tempInt;
            }
            if( fields[19].Length > 0 ) {
                Int32.TryParse( fields[19], out tempInt );
                info.BlocksDeleted = tempInt;
            }
            Int32.TryParse( fields[20], out tempInt );
            info.TimesVisited = tempInt;
            if( fields[20].Length > 0 ) {
                Int32.TryParse( fields[21], out tempInt );
                info.MessagesWritten = tempInt;
            }
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out tempInt );
            info.TimesKicked = tempInt;
            Int32.TryParse( fields[27], out tempInt );
            info.TimesKickedOthers = tempInt;
            Int32.TryParse( fields[28], out tempInt );
            info.TimesBannedOthers = tempInt;
            // fields[29] is ID, read above

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    GuessRankChangeType( info );
                }
            } else {
                GuessRankChangeType( info );
            }

            ToDateTimeLegacy( fields[31], out tempDate );
            info.LastKickDate = tempDate;
            if( !ToDateTimeLegacy( fields[32], out tempDate ) ) {
                tempDate = info.LastLoginDate;
            }
            info.LastSeen = tempDate;
            long blocksDrawn;
            Int64.TryParse( fields[33], out blocksDrawn );
            info.BlocksDrawn = blocksDrawn;

            if( fields[34].Length > 0 ) info.LastKickBy = Unescape( fields[34] );
            if( fields[34].Length > 0 ) info.LastKickReason = Unescape( fields[35] );

            ToDateTimeLegacy( fields[36], out tempDate );
            info.BannedUntil = tempDate;
            info.IsFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.FrozenBy = Unescape( fields[38] );
            ToDateTimeLegacy( fields[39], out tempDate );
            info.FrozenOn = tempDate;
            ToDateTimeLegacy( fields[40], out tempDate );
            info.MutedUntil = tempDate;
            if( fields[41].Length > 0 ) info.MutedBy = Unescape( fields[41] );
            info.Password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            ParseBandwidthUseMode( info, fields[44] );

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }

            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }
            if( info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }

            return info;
        }


        internal PlayerInfo LoadFormat0( [NotNull] string[] fields ) {
            if( fields == null ) throw new ArgumentNullException( "fields" );
            DateTime tempDate;

            // get ID
            int id;
            if( fields.Length > 29 ) {
                if( !Int32.TryParse( fields[29], out id ) || id < 256 ) {
                    id = GetNextId();
                }
            } else {
                id = GetNextId();
            }

            PlayerInfo info = new PlayerInfo( id ) {
                Name = fields[0]
            };

            if( fields[1].Length > 1 ) {
                IPAddress lastIP;
                IPAddress.TryParse( fields[1], out lastIP );
                info.LastIP = lastIP;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            TryParseLocalDate( fields[3], out tempDate );
            info.RankChangeDate = tempDate;
            if( fields[4].Length > 0 ) {
                info.RankChangedBy = UnescapeOldFormat( fields[4] );
                if( info.RankChangedBy == "-" ) info.RankChangedBy = null;
            }

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
            if( TryParseLocalDate( fields[6], out tempDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = fields[7];
                if( fields[10].Length > 0 ) {
                    info.BanReason = UnescapeOldFormat( fields[10] );
                    if( info.BanReason == "-" ) info.BanReason = null;
                }
            }
            info.BanDate = tempDate;

            // unban information
            if( TryParseLocalDate( fields[8], out tempDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = fields[9];
                if( fields[11].Length > 0 ) {
                    info.UnbanReason = UnescapeOldFormat( fields[11] );
                    if( info.UnbanReason == "-" ) info.UnbanReason = null;
                }
            }
            info.UnbanDate = tempDate;

            // failed logins
            if( fields[12].Length > 1 ) {
                TryParseLocalDate( fields[12], out tempDate );
                info.LastFailedLoginDate = tempDate;
            }
            if( fields[13].Length > 1 ) {
                IPAddress lastFailedLoginIP;
                IPAddress.TryParse( fields[13], out lastFailedLoginIP );
                info.LastFailedLoginIP = lastFailedLoginIP;
            }
            // skip 14

            // login/logout times
            TryParseLocalDate( fields[15], out tempDate );
            info.FirstLoginDate = tempDate;
            TryParseLocalDate( fields[16], out tempDate );
            info.LastLoginDate = tempDate;
            TimeSpan totalTime;
            TimeSpan.TryParse( fields[17], out totalTime );
            info.TotalTime = totalTime;

            // stats
            int tempInt;
            if( fields[18].Length > 0 ) {
                Int32.TryParse( fields[18], out tempInt );
                info.BlocksBuilt = tempInt;
            }
            if( fields[19].Length > 0 ) {
                Int32.TryParse( fields[19], out tempInt );
                info.BlocksDeleted = tempInt;
            }
            Int32.TryParse( fields[20], out tempInt );
            info.TimesVisited = tempInt;
            if( fields[20].Length > 0 ) {
                Int32.TryParse( fields[21], out tempInt );
                info.MessagesWritten = tempInt;
            }
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
                if( fields[25].Length > 0 ) info.RankChangeReason = UnescapeOldFormat( fields[25] );
                Int32.TryParse( fields[26], out tempInt );
                info.TimesKicked = tempInt;
                Int32.TryParse( fields[27], out tempInt );
                info.TimesKickedOthers = tempInt;
                Int32.TryParse( fields[28], out tempInt );
                info.TimesBannedOthers = tempInt;
                // fields[29] (id) already read/assigned by this point
                if( fields.Length > 29 ) {
                    byte rankChangeTypeCode;
                    if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            GuessRankChangeType( info );
                        }
                    } else {
                        GuessRankChangeType( info );
                    }
                    TryParseLocalDate( fields[31], out tempDate );
                    info.LastKickDate = tempDate;
                    if( !TryParseLocalDate( fields[32], out tempDate ) ) {
                        tempDate = info.LastLoginDate;
                    }
                    info.LastSeen = tempDate;
                    long blocksDrawn;
                    Int64.TryParse( fields[33], out blocksDrawn );
                    info.BlocksDrawn = blocksDrawn;

                    if( fields[34].Length > 0 ) info.LastKickBy = UnescapeOldFormat( fields[34] );
                    if( fields[35].Length > 0 ) info.LastKickReason = UnescapeOldFormat( fields[35] );

                } else {
                    GuessRankChangeType( info );
                    info.LastSeen = info.LastLoginDate;
                }

                if( fields.Length > 36 ) {
                    TryParseLocalDate( fields[36], out tempDate );
                    info.BannedUntil = tempDate;
                    info.IsFrozen = (fields[37] == "f");
                    if( fields[38].Length > 0 ) info.FrozenBy = UnescapeOldFormat( fields[38] );
                    TryParseLocalDate( fields[39], out tempDate );
                    info.FrozenOn = tempDate;
                    TryParseLocalDate( fields[40], out tempDate );
                    info.MutedUntil = tempDate;
                    if( fields[41].Length > 0 ) info.MutedBy = UnescapeOldFormat( fields[41] );
                    info.Password = UnescapeOldFormat( fields[42] );
                    // fields[43] is "online", and is ignored
                }

                if( fields.Length > 44 ) {
                    ParseBandwidthUseMode( info, fields[44] );
                }
            }

            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }
            if( info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }

            return info;
        }


        static void ParseBandwidthUseMode( [NotNull] PlayerInfo info, [NotNull] string field ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            if( field == null ) throw new ArgumentNullException( "field" );
            byte bandwidthUseModeCode;
            if( field.Length > 0 && Byte.TryParse( field, out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }
        }


        static void GuessRankChangeType( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            if( info.PreviousRank != null ) {
                if( info.RankChangeReason == "~AutoRank" || info.RankChangeReason == "~AutoRankAll" || info.RankChangeReason == "~MassRank" ) {
                    if( info.PreviousRank > info.Rank ) {
                        info.RankChangeType = RankChangeType.AutoDemoted;
                    } else if( info.PreviousRank < info.Rank ) {
                        info.RankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( info.PreviousRank > info.Rank ) {
                        info.RankChangeType = RankChangeType.Demoted;
                    } else if( info.PreviousRank < info.Rank ) {
                        info.RankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                info.RankChangeType = RankChangeType.Default;
            }
        }


        static CultureInfo cultureInfo = CultureInfo.CurrentCulture;

        /// <summary> Tries to parse a data in a culture-specific ways.
        /// This method is, unfortunately, necessary because in versions 0.520-0.522,
        /// fCraft saved dates in a culture-specific format. This means that if the
        /// server's culture settings were changed, or if the PlayerDB and IPBanList
        /// files were moved between machines, all dates became unparseable. </summary>
        /// <param name="dateString"> String to parse. </param>
        /// <param name="date"> Date to output. </param>
        /// <returns> True if date string could be parsed and was not empty/MinValue. </returns>
        public static bool TryParseLocalDate( [NotNull] string dateString, out DateTime date ) {
            if( dateString == null ) throw new ArgumentNullException( "dateString" );
            if( dateString.Length <= 1 ) {
                date = DateTime.MinValue;
                return false;
            } else {
                if( !DateTime.TryParse( dateString, cultureInfo, DateTimeStyles.None, out date ) ) {
                    CultureInfo[] cultureList = CultureInfo.GetCultures( CultureTypes.AllCultures );
                    foreach( CultureInfo otherCultureInfo in cultureList ) {
                        cultureInfo = otherCultureInfo;
                        try {
                            if( DateTime.TryParse( dateString, cultureInfo, DateTimeStyles.None, out date ) ) {
                                date = date.ToUniversalTime();
                                return true;
                            }
                        } catch( NotSupportedException ) { }
                    }
                    throw new Exception( "Could not find a culture that would be able to parse date/time formats." );
                } else {
                    return true;
                }
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


        public static long ToUnixTimeLegacy( DateTime date ) {
            return (date.Ticks - DateTimeUtil.TicksToUnixEpoch) / TicksPerMillisecond;
        }


        public static bool ToTimeSpanLegacy( [NotNull] string str, out TimeSpan result ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > 1 ) {
                result = new TimeSpan( Int64.Parse( str ) * TicksPerMillisecond );
                return true;
            } else {
                result = TimeSpan.Zero;
                return false;
            }
        }

        #endregion


        #region Saving

        public static string Escape( [CanBeNull] string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            } else if( str.IndexOf( ',' ) > -1 ) {
                return str.Replace( ',', '\xFF' );
            } else {
                return str;
            }
        }


        internal void SaveFormat5( [NotNull] PlayerInfo info, [NotNull] StringBuilder sb ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            if( sb == null ) throw new ArgumentNullException( "sb" );
            sb.Append( info.Name ).Append( ',' ); // 0
            if( !info.LastIP.Equals( IPAddress.None ) ) sb.Append( info.LastIP ); // 1
            sb.Append( ',' );

            sb.Append( info.Rank.FullName ).Append( ',' ); // 2
            info.RankChangeDate.ToUnixTimeString( sb ).Append( ',' ); // 3

            sb.AppendEscaped( info.RankChangedBy ).Append( ',' ); // 4

            switch( info.BanStatus ) {
                case BanStatus.Banned:
                    sb.Append( 'b' );
                    break;
                case BanStatus.IPBanExempt:
                    sb.Append( 'x' );
                    break;
            }
            sb.Append( ',' ); // 5

            info.BanDate.ToUnixTimeString( sb ).Append( ',' ); // 6
            sb.AppendEscaped( info.BannedBy ).Append( ',' ); // 7
            info.UnbanDate.ToUnixTimeString( sb ).Append( ',' ); // 8
            sb.AppendEscaped( info.UnbannedBy ).Append( ',' ); // 9
            sb.AppendEscaped( info.BanReason ).Append( ',' ); // 10
            sb.AppendEscaped( info.UnbanReason ).Append( ',' ); // 11

            info.LastFailedLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 12

            if( !info.LastFailedLoginIP.Equals( IPAddress.None ) ) {
                sb.Append( info.LastFailedLoginIP.ToString() ); // 13
            }
            sb.Append( ',', 2 ); // skip 14

            info.FirstLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 15
            info.LastLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 16

            if( info.IsOnline ) {
                (info.TotalTime.Add( info.TimeSinceLastLogin )).ToTickString( sb ).Append( ',' ); // 17
            } else {
                info.TotalTime.ToTickString( sb ).Append( ',' ); // 17
            }

            if( info.BlocksBuilt > 0 ) sb.Digits( info.BlocksBuilt ); // 18
            sb.Append( ',' );

            if( info.BlocksDeleted > 0 ) sb.Digits( info.BlocksDeleted ); // 19
            sb.Append( ',' );

            sb.Append( info.TimesVisited ).Append( ',' ); // 20


            if( info.MessagesWritten > 0 ) sb.Digits( info.MessagesWritten ); // 21
            sb.Append( ',', 3 ); // 22-23 no longer in use

            if( info.PreviousRank != null ) sb.Append( info.PreviousRank.FullName ); // 24
            sb.Append( ',' );

            sb.AppendEscaped( info.RankChangeReason ).Append( ',' ); // 25


            if( info.TimesKicked > 0 ) sb.Digits( info.TimesKicked ); // 26
            sb.Append( ',' );

            if( info.TimesKickedOthers > 0 ) sb.Digits( info.TimesKickedOthers ); // 27
            sb.Append( ',' );

            if( info.TimesBannedOthers > 0 ) sb.Digits( info.TimesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Digits( info.ID ).Append( ',' ); // 29

            sb.Digits( (int)info.RankChangeType ).Append( ',' ); // 30


            info.LastKickDate.ToUnixTimeString( sb ).Append( ',' ); // 31

            if( info.IsOnline ) DateTime.UtcNow.ToUnixTimeString( sb ); // 32
            else info.LastSeen.ToUnixTimeString( sb );
            sb.Append( ',' );

            if( info.BlocksDrawn > 0 ) sb.Append( info.BlocksDrawn ); // 33
            sb.Append( ',' );

            sb.AppendEscaped( info.LastKickBy ).Append( ',' ); // 34
            sb.AppendEscaped( info.LastKickReason ).Append( ',' ); // 35

            info.BannedUntil.ToUnixTimeString( sb ); // 36

            if( info.IsFrozen ) {
                sb.Append( ',' ).Append( 'f' ).Append( ',' ); // 37
                sb.AppendEscaped( info.FrozenBy ).Append( ',' ); // 38
                info.FrozenOn.ToUnixTimeString( sb ).Append( ',' ); // 39
            } else {
                sb.Append( ',', 4 ); // 37-39
            }

            if( info.MutedUntil > DateTime.UtcNow ) {
                info.MutedUntil.ToUnixTimeString( sb ).Append( ',' ); // 40
                sb.AppendEscaped( info.MutedBy ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            sb.AppendEscaped( info.Password ).Append( ',' ); // 42

            if( info.IsOnline ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( info.BandwidthUseMode != BandwidthUseMode.Default ) {
                sb.Digits( (int)info.BandwidthUseMode ); // 44
            }
            sb.Append( ',' );

            if( info.IsHidden ) sb.Append( 'h' ); // 45

            sb.Append( ',' );
            info.LastModified.ToUnixTimeString( sb ); // 46

            sb.Append( ',' );
            sb.AppendEscaped( info.DisplayedName ); // 47
        }


        public static DateTime ToDateTimeLegacy( long timestamp ) {
            return new DateTime( timestamp * TicksPerMillisecond + DateTimeUtil.TicksToUnixEpoch, DateTimeKind.Utc );
        }


        public static bool ToDateTimeLegacy( [NotNull] string str, out DateTime result ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            long dateTime;
            if( str.Length > 0 && Int64.TryParse( str, out dateTime ) ) {
                result = ToDateTimeLegacy( Int64.Parse( str ) );
                return true;
            } else {
                result = DateTime.MinValue;
                return false;
            }
        }

        #endregion
    }
}