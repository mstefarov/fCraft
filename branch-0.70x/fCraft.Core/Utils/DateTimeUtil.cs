// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides utility functions for working with DateTime and TimeSpan. </summary>
    public static class DateTimeUtil {
        static readonly NumberFormatInfo NumberFormatter = CultureInfo.InvariantCulture.NumberFormat;

        /// <summary> UTC Unix epoch (1970-01-01, 00:00:00). </summary>
        public static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        /// <summary> UTC Unix Epoch, in terms of DateTime ticks. </summary>
        public static readonly long TicksToUnixEpoch;

        static DateTimeUtil() {
            TicksToUnixEpoch = UnixEpoch.Ticks;
        }


        #region To Unix Time

        /// <summary> Converts a DateTime to UTC Unix Timestamp. </summary>
        public static long ToUnixTime( this DateTime date ) {
            if( date == DateTime.MinValue ) {
                return 0;
            } else {
                return (long)date.Subtract( UnixEpoch ).TotalSeconds;
            }
        }


        /// <summary> Converts a DateTime to a string containing the UTC Unix Timestamp.
        /// If the date equals DateTime.MinValue, returns an empty string. </summary>
        public static string ToUnixTimeString( this DateTime date ) {
            if( date == DateTime.MinValue ) {
                return "";
            } else {
                return date.ToUnixTime().ToString( NumberFormatter );
            }
        }


        /// <summary> Appends a Utc Unix Timestamp to the given StringBuilder.
        /// If the date equals DateTime.MinValue, nothing is appended. </summary>
        public static StringBuilder ToUnixTimeString( this DateTime date, StringBuilder sb ) {
            if( date != DateTime.MinValue ) {
                sb.Append( date.ToUnixTime() );
            }
            return sb;
        }

        #endregion


        #region To Date Time

        /// <summary> Creates a DateTime from a Utc Unix Timestamp. </summary>
        public static DateTime ToDateTime( this long timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }


        /// <summary> Tries to create a DateTime from a string containing a Utc Unix Timestamp.
        /// If the string was empty, returns false and does not affect result. </summary>
        public static bool ToDateTime( this string str, out DateTime result ) {
            long t;
            if( str.Length > 1 && Int64.TryParse( str, out t ) ) {
                result = UnixEpoch.AddSeconds( Int64.Parse( str ) );
                return true;
            } else {
                result = DateTime.MinValue;
            }
            return false;
        }

        #endregion


        public static long ToSeconds( this TimeSpan time ) {
            return (time.Ticks / TimeSpan.TicksPerSecond);
        }


        /// <summary> Converts a TimeSpan to a string containing the number of seconds.
        /// If the timestamp is zero seconds, returns an empty string. </summary>
        public static string ToSecondsString( this TimeSpan time ) {
            if( time == TimeSpan.Zero ) {
                return "";
            } else {
                return (time.Ticks / TimeSpan.TicksPerSecond).ToString( NumberFormatter );
            }
        }


        /// <summary> Tries to create a TimeSpan from a string containing the number of seconds.
        /// If the string was empty, returns false and sets result to TimeSpan.Zero </summary>
        public static bool ToTimeSpan( [NotNull] this string str, out TimeSpan result ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length == 0 ) {
                result = TimeSpan.Zero;
                return true;
            }
            long ticks;
            if( Int64.TryParse( str, out ticks ) ) {
                result = new TimeSpan( ticks * TimeSpan.TicksPerSecond );
                return true;
            } else {
                result = TimeSpan.Zero;
                return false;
            }
        }


        #region MiniString

        public static StringBuilder ToTickString( this TimeSpan time, StringBuilder sb ) {
            if( time != TimeSpan.Zero ) {
                sb.Append( time.Ticks / TimeSpan.TicksPerSecond );
            }
            return sb;
        }


        public static string ToMiniString( this TimeSpan span ) {
            if( span.TotalSeconds < 60 ) {
                return String.Format( "{0}s", span.Seconds );
            } else if( span.TotalMinutes < 60 ) {
                return String.Format( "{0}m{1}s", span.Minutes, span.Seconds );
            } else if( span.TotalHours < 48 ) {
                return String.Format( "{0}h{1}m", (int)Math.Floor( span.TotalHours ), span.Minutes );
            } else if( span.TotalDays < 15 ) {
                return String.Format( "{0}d{1}h", span.Days, span.Hours );
            } else {
                return String.Format( "{0:0}w{1:0}d", Math.Floor( span.TotalDays / 7 ), Math.Floor( span.TotalDays ) % 7 );
            }
        }


        public static bool TryParseMiniTimespan( this string text, out TimeSpan result ) {
            try {
                result = ParseMiniTimespan( text );
                return true;
            } catch( ArgumentException ) {
            } catch( OverflowException ) {
            } catch( FormatException ) { }
            result = TimeSpan.Zero;
            return false;
        }

        
        public static readonly TimeSpan MaxTimeSpan = TimeSpan.FromDays( 9999 );


        public static TimeSpan ParseMiniTimespan( [NotNull] this string text ) {
            if( text == null ) throw new ArgumentNullException( "text" );

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

                } else if( text[i] < '0' || text[i] > '9' ) {
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
            return result;
        }

        #endregion


        #region CompactString

        public static string ToCompactString( this DateTime date ) {
            return date.ToUniversalTime().ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ssK" );
        }


        public static string ToCompactString( this TimeSpan span ) {
            return String.Format( "{0}.{1:00}:{2:00}:{3:00}",
                                  span.Days, span.Hours, span.Minutes, span.Seconds );
        }

        #endregion
    }
}