// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides utility functions for working with DateTime and TimeSpan. </summary>
    public static class DateTimeUtil {
        static readonly NumberFormatInfo NumberFormatter = CultureInfo.InvariantCulture.NumberFormat;
        static readonly long TicksToUnixEpoch;
        const long TicksPerMillisecond = 10000;

        /// <summary> UTC Unix epoch (1970-01-01, 00:00:00). </summary>
        public static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        static DateTimeUtil() {
            TicksToUnixEpoch = UnixEpoch.Ticks;
        }


        #region Conversion to/from Unix timestamps

        /// <summary> Converts a DateTime to UTC Unix Timestamp. </summary>
        public static long ToUnixTime( this DateTime date ) {
            return (long)date.Subtract( UnixEpoch ).TotalSeconds;
        }


        // Converts a DateTime to UTC Unix Timestamp, with millisecond precision. Used in FCMv3 saving.
        internal static long ToUnixTimeLegacy( this DateTime date ) {
            return (date.Ticks - TicksToUnixEpoch) / TicksPerMillisecond;
        }


        /// <summary> Converts a DateTime to a string containing the UTC Unix Timestamp.
        /// If the date equals DateTime.MinValue, returns an empty string. </summary>
        [NotNull]
        public static string ToUnixTimeString( this DateTime date ) {
            if( date == DateTime.MinValue ) {
                return "";
            } else {
                return date.ToUnixTime().ToString( NumberFormatter );
            }
        }


        /// <summary> Appends a UTC Unix Timestamp to the given StringBuilder.
        /// If the date equals DateTime.MinValue, nothing is appended. </summary>
        [NotNull]
        public static StringBuilder ToUnixTimeString( this DateTime date, [NotNull] StringBuilder sb ) {
            if( date != DateTime.MinValue ) {
                sb.Append( date.ToUnixTime() );
            }
            return sb;
        }

        #endregion


        #region To Date Time

        /// <summary> Creates a DateTime from a Utc Unix Timestamp. </summary>
        public static DateTime TryParseDateTime( long timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }


        /// <summary> Tries to create a DateTime from a string containing a Utc Unix Timestamp.
        /// If the string was empty, returns false and does not affect result. </summary>
        public static bool TryParseDateTime( [NotNull] string str, ref DateTime result ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            long t;
            if( str.Length > 1 && Int64.TryParse( str, out t ) ) {
                result = UnixEpoch.AddSeconds( t );
                return true;
            }
            return false;
        }


        // Tries to parse the given DateTime representation (stingified integer, number of milliseconds since Unix Epoch).
        // Used to load old versions of PlayerDB, and creation/modification dates in FCMv3
        internal static DateTime ToDateTimeLegacy( long timestamp ) {
            return new DateTime( timestamp * TicksPerMillisecond + TicksToUnixEpoch, DateTimeKind.Utc );
        }


        // Tries to parse the given DateTime representation (stingified integer, number of milliseconds since Unix Epoch).
        // Used to load old versions of PlayerDB, and creation/modification dates in FCMv3
        internal static bool ToDateTimeLegacy( [NotNull] this string str, ref DateTime result ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length <= 1 ) {
                return false;
            }
            result = ToDateTimeLegacy( Int64.Parse( str ) );
            return true;
        }

        #endregion


        /// <summary> Converts a TimeSpan to a string containing the number of seconds.
        /// If the timestamp is zero seconds, returns an empty string. </summary>
        [NotNull]
        public static string ToSecondsString( this TimeSpan time ) {
            if( time == TimeSpan.Zero ) {
                return "";
            } else {
                return (time.Ticks / TimeSpan.TicksPerSecond).ToString( NumberFormatter );
            }
        }


        /// <summary> Serializes the given TimeSpan to the given StringBuilder, as the number of seconds. </summary>
        /// <param name="time"> TimeSpan to serialize. </param>
        /// <param name="sb"> StringBuilder to which time will be saved. </param>
        /// <exception cref="ArgumentNullException"> sb is null. </exception>
        public static void ToSecondsString( this TimeSpan time, [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            if( time != TimeSpan.Zero ) {
                sb.Append( time.Ticks / TimeSpan.TicksPerSecond );
            }
        }


        /// <summary> Tries to create a TimeSpan from a string containing the number of seconds.
        /// If the string was empty, returns false and sets result to TimeSpan.Zero </summary>
        /// <exception cref="ArgumentNullException"> str is null </exception>
        public static bool TryParseTimeSpan( [NotNull] string str, out TimeSpan result ) {
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


        // Tries to convert the given TimeSpan representation (stingified integer, number of milliseconds).
        // Used to load old versions of PlayerDB.
        internal static bool ToTimeSpanLegacy( [NotNull] this string str, ref TimeSpan result ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > 1 ) {
                result = new TimeSpan( Int64.Parse( str ) * TicksPerMillisecond );
                return true;
            } else {
                return false;
            }
        }


        #region MiniString

        /// <summary> Converts given TimeSpan to compact string representation. </summary>
        /// <param name="span"> Time span to present. May not be negative. </param>
        /// <returns> A string representation of the given time span. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> span is negative. </exception>
        [NotNull]
        public static string ToMiniString( this TimeSpan span ) {
            if( span.Ticks < 0 ) {
                throw new ArgumentOutOfRangeException( "span", "ToMiniString cannot be used on negative time spans." );
            }else if( span.TotalSeconds < 60 ) {
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


        /// <summary> Attempts to parse the given string as a TimeSpan in compact representation. 
        /// No exception is thrown if parsing failed. </summary>
        /// <param name="text"> String to parse. </param>
        /// <param name="result"> Parsed TimeSpan. Set to TimeSpan.Zero if parsing failed. </param>
        /// <returns> True if parsing succeeded; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> text is null. </exception>
        public static bool TryParseMiniTimespan( [NotNull] this string text, out TimeSpan result ) {
            if( text == null ) throw new ArgumentNullException( "text" );
            try {
                result = ParseMiniTimespan( text );
                return true;
            } catch( ArgumentException ) {
            } catch( OverflowException ) {
            } catch( FormatException ) { }
            result = TimeSpan.Zero;
            return false;
        }


        /// <summary> Longest reasonable value that fCraft will allow to be entered for time spans (9999 days). </summary>
        public static readonly TimeSpan MaxTimeSpan = TimeSpan.FromDays( 9999 );


        /// <summary> Parses the given string as a TimeSpan in compact representation. Throws exceptions on failure. </summary>
        /// <param name="text"> String to parse. </param>
        /// <returns> Parsed TimeSpan. </returns>
        /// <exception cref="ArgumentNullException"> text is null. </exception>
        /// <exception cref="OverflowException"> The resulting TimeSpan is greater than TimeSpan.MaxValue. </exception>
        /// <exception cref="FormatException"> input has an invalid format. </exception>
        public static TimeSpan ParseMiniTimespan( [NotNull] this string text ) {
            if( text == null ) throw new ArgumentNullException( "text" );

            text = text.Trim();
            bool expectingDigit = true;
            TimeSpan result = TimeSpan.Zero;
            int digitOffset = 0;
            bool hadUnit = false;
            for( int i = 0; i < text.Length; i++ ) {
                if( expectingDigit ) {
                    if( text[i] < '0' || text[i] > '9' ) {
                        throw new FormatException();
                    }
                    expectingDigit = false;
                } else {
                    if( text[i] < '0' || text[i] > '9' ) {
                        hadUnit = true;
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
            if( !hadUnit ) {
                throw new FormatException();
            }
            return result;
        }

        #endregion


        #region CompactString

        [NotNull]
        public static string ToCompactString( this DateTime date ) {
            return date.ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ssK" );
        }


        [NotNull]
        public static string ToCompactString( this TimeSpan span ) {
            return String.Format( "{0}.{1:00}:{2:00}:{3:00}",
                                  span.Days, span.Hours, span.Minutes, span.Seconds );
        }

        #endregion


        static CultureInfo cultureInfo = CultureInfo.CurrentCulture;

        // Tries to parse a data in a culture-specific ways.
        // This method is, unfortunately, necessary because in versions 0.520-0.522,
        // fCraft saved dates in a culture-specific format. This means that if the
        // server's culture settings were changed, or if the PlayerDB and IPBanList
        // files were moved between machines, all dates became unparseable.
        // Returns true if date string could be parsed and was not empty/MinValue.
        internal static bool TryParseLocalDate( [NotNull] string dateString, out DateTime date ) {
            if( dateString == null ) throw new ArgumentNullException( "dateString" );
            if( dateString.Length <= 1 ) {
                date = DateTime.MinValue;
                return false;
            } else {
                if( !DateTime.TryParse( dateString, cultureInfo, DateTimeStyles.None, out date ) ) {
                    CultureInfo[] cultureList = CultureInfo.GetCultures( CultureTypes.FrameworkCultures );
                    foreach( CultureInfo otherCultureInfo in cultureList ) {
                        cultureInfo = otherCultureInfo;
                        try {
                            if( DateTime.TryParse( dateString, cultureInfo, DateTimeStyles.None, out date ) ) {
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
}