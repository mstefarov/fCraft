using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Globalization;

namespace fCraft {

    static class IPAddressUtil {
        public static bool IsLAN( this IPAddress addr ) {
            if( addr == null ) throw new ArgumentNullException( "addr" );
            byte[] bytes = addr.GetAddressBytes();
            return (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 10);
        }
    }


    static class DateTimeUtil {

        static DateTimeUtil() {
            TicksToUnixEpoch = UnixEpoch.Ticks;
        }

        public static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );
        public static readonly long TicksToUnixEpoch;
        public const int TicksPerSecond = 10000;

        public static string ToCompactString( this DateTime date ) {
            return date.ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ssK" );
        }

        public static string ToTickString( this DateTime date ) {
            if( date == DateTime.MinValue ) {
                return "";
            } else {
                return ((date.Ticks - TicksToUnixEpoch) / TicksPerSecond).ToString();
            }
        }

        public static StringBuilder ToTickString( this DateTime date, StringBuilder sb ) {
            if( date != DateTime.MinValue ) {
                sb.Append( (date.Ticks - TicksToUnixEpoch) / TicksPerSecond );
            }
            return sb;
        }



        public static long ToTimestamp( this DateTime timestamp ) {
            return (long)(timestamp - UnixEpoch).TotalSeconds;
        }

        public static long ToUtcTimestamp( this DateTime timestamp ) {
            if( timestamp.Kind != DateTimeKind.Utc ) {
                timestamp = TimeZone.CurrentTimeZone.ToUniversalTime( timestamp );
            }
            return timestamp.ToTimestamp();
        }


        #region ToDateTime

        public static DateTime ToDateTime( this long timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }

        public static DateTime ToDateTime( this int timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }

        public static DateTime ToDateTime( this uint timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }

        public static bool ToDateTime( this string str, ref DateTime date ) {
            if( str.Length > 1 ) {
                date = new DateTime( Int64.Parse( str ) * TicksPerSecond + TicksToUnixEpoch, DateTimeKind.Utc );
                return true;
            } else {
                return false;
            }
        }

        #endregion


        public static bool ToTimeSpan( this string str, ref TimeSpan date ) {
            if( str.Length > 1 ) {
                date = new TimeSpan( Int64.Parse( str ) * TicksPerSecond );
                return true;
            } else {
                return false;
            }
        }


        public static string ToCompactString( this TimeSpan span ) {
            return String.Format( "{0}.{1:00}:{2:00}:{3:00}",
                span.Days, span.Hours, span.Minutes, span.Seconds );
        }


        #region Mini-string (very compact format)

        public static string ToTickString( this TimeSpan time ) {
            if( time == TimeSpan.Zero ) {
                return "";
            } else {
                return (time.Ticks / TicksPerSecond).ToString();
            }
        }

        public static StringBuilder ToTickString( this TimeSpan time, StringBuilder sb ) {
            if( time != TimeSpan.Zero ) {
                sb.Append( time.Ticks / TicksPerSecond );
            }
            return sb;
        }


        public static string ToMiniString( this TimeSpan span ) {
            if( span.TotalSeconds < 60 ) {
                return String.Format( "{0}s", span.Seconds );
            } else if( span.TotalMinutes < 60 ) {
                return String.Format( "{0:0}m{1}s", span.TotalMinutes, span.Seconds );
            } else if( span.TotalHours < 48 ) {
                return String.Format( "{0:0}h{1}m", span.TotalHours, span.Minutes );
            } else if( span.TotalDays < 14 ) {
                return String.Format( "{0:0}d{1}h", span.TotalDays, span.Hours );
            } else {
                return String.Format( "{0:0}w{1:0}d", span.TotalDays / 7, span.TotalDays % 7 );
            }
        }


        public static bool TryParseMiniTimespan( this string text, out TimeSpan result ) {
            try {
                result = ParseMiniTimespan( text );
                return true;
            } catch( ArgumentNullException ) {
            } catch( ArgumentException ) {
            } catch( FormatException ) { }
            result = TimeSpan.Zero;
            return false;
        }


        public static TimeSpan ParseMiniTimespan( this string text ) {
            if( text == null ) throw new ArgumentNullException( "text" );

            int secondCount;
            if( Int32.TryParse( text, out secondCount ) ) {
                return TimeSpan.FromSeconds( secondCount );
            }

            text = text.Trim();
            bool expectingDigit = true;
            TimeSpan result = TimeSpan.Zero;
            int digitOffset = 0;
            for( int i = 0; i < text.Length; i++ ) {
                if( expectingDigit ) {
                    if( text[i] < '0' || text[i] > '9' ) {
                        throw new FormatException();
                    }
                    expectingDigit = false;
                } else {
                    if( text[i] >= '0' && text[i] <= '9' ) {
                        continue;
                    } else {
                        string numberString = text.Substring( digitOffset, i - digitOffset );
                        digitOffset = i + 1;
                        int number = Int32.Parse( numberString );
                        switch( Char.ToLower( text[i] ) ) {
                            case 's':
                                result += TimeSpan.FromSeconds( number );
                                break;
                            case 'm':
                                result += TimeSpan.FromMinutes( number );
                                break;
                            case 'h':
                                result += TimeSpan.FromHours( number );
                                break;
                            case 'd':
                                result += TimeSpan.FromDays( number );
                                break;
                            case 'w':
                                result += TimeSpan.FromDays( number * 7 );
                                break;
                            default:
                                throw new FormatException();
                        }
                    }
                }
            }
            return result;
        }

        #endregion



        static CultureInfo cultureInfo = CultureInfo.CurrentCulture;

        /// <summary> Tries to parse a data in a culture-specific ways.
        /// This method is, unfortunately, necessary because in versions 0.520-0.522,
        /// fCraft saved dates in a culture-specific format. This means that if the
        /// server's culture settings were changed, or if the PlayerDB and IPBanList
        /// files were moved between machines, all dates became unparseable. </summary>
        /// <param name="dateString"> String to parse. </param>
        /// <param name="date"> Date to output. </param>
        /// <returns> True if date string could be parsed and was not empty/MinValue. </returns>
        public static bool TryParseLocalDate( string dateString, out DateTime date ) {
            if( dateString.Length <= 1 ) {
                date = DateTime.MinValue;
                return false;
            } else {
                if( !DateTime.TryParse( dateString, cultureInfo, DateTimeStyles.None, out date ) ) {
                    Logger.Log( "PlayerInfo.TryParseLocalDate: Unable to parse a date string \"{0}\". Trying to guess format...",
                                LogType.Warning, dateString );
                    CultureInfo[] cultureList = CultureInfo.GetCultures( CultureTypes.FrameworkCultures );
                    foreach( CultureInfo otherCultureInfo in cultureList ) {
                        cultureInfo = otherCultureInfo;
                        try {
                            if( DateTime.TryParse( dateString, cultureInfo, DateTimeStyles.None, out date ) ) {
                                Logger.Log( "PlayerInfo.TryParseLocalDate: Date string parsed succesfully using \"{0}\" format...", LogType.Warning,
                                            cultureInfo.EnglishName );
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
    }


    static class EnumerableUtil {

        public static string JoinToString<T>( this IEnumerable<T> items ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( ',' ).Append( ' ' );
                sb.Append( item );
                first = false;
            }
            return sb.ToString();
        }


        public static string JoinToString<T>( this IEnumerable<T> items, string separator ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( separator );
                sb.Append( item );
                first = false;
            }
            return sb.ToString();
        }


        public static string JoinToString<T>( this IEnumerable<T> items, string separator, Func<T, string> stringConversionFunction ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( separator );
                sb.Append( stringConversionFunction( item ) );
                first = false;
            }
            return sb.ToString();
        }


        // TODO: After switching to 4.0, mess with generics to allow IEnumerable<IClassy>
        public static string JoinToClassyString( this IEnumerable<IClassy> list ) {
            return list.JoinToString( "&S, ", p => p.GetClassyName() );
        }
    }
}