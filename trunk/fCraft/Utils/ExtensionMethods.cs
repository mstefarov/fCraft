using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

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

        static DateTimeUtil(){
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
                date = new TimeSpan( Int64.Parse( str ) * TicksPerSecond + TicksToUnixEpoch );
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
    }
}