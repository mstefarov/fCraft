// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides utility methods for working with IP addresses and ranges. </summary>
    public static class IPAddressUtil {

        /// <summary> Checks whether an IP address may belong to LAN or localhost (192.168.0.0/16, 10.0.0.0/24, or 127.0.0.0/24). </summary>
        /// <param name="addr"> IPv4 address to check. </param>
        /// <returns> True if given IP is local; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> If addr is null. </exception>
        public static bool IsLocal( [NotNull] this IPAddress addr ) {
            if( addr == null ) throw new ArgumentNullException( "addr" );
            byte[] bytes = addr.GetAddressBytes();
            return (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 10) ||
                   (bytes[0] == 127);
        }


        /// <summary> Represents an IPv4 address as an integer. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        public static int AsInt( [NotNull] this IPAddress thisAddr ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            return IPAddress.HostToNetworkOrder( BitConverter.ToInt32( thisAddr.GetAddressBytes(), 0 ) );
        }


        /// <summary> Represents an IPv4 address as an unsigned integer. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        public static uint AsUInt( [NotNull] this IPAddress thisAddr ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            return (uint)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( thisAddr.GetAddressBytes(), 0 ) );
        }


        /// <summary> Checks whether two IP addresses are in the same mask-defined range. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr or otherAddr is null. </exception>
        public static bool Match( [NotNull] this IPAddress thisAddr, [NotNull] IPAddress otherAddr, uint mask ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( otherAddr == null ) throw new ArgumentNullException( "otherAddr" );
            uint thisAsUInt = thisAddr.AsUInt();
            uint otherAsUInt = otherAddr.AsUInt();
            return (thisAsUInt & mask) == (otherAsUInt & mask);
        }

        /// <summary> Checks whether two IP addresses are in the same mask-defined range. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        internal static bool Match( [NotNull] this IPAddress thisAddr, uint otherAddr, uint mask ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            uint thisAsUInt = thisAddr.AsUInt();
            return (thisAsUInt & mask) == (otherAddr & mask);
        }



        /// <summary> Finds the first IPv4 address in the given range. </summary>
        /// <param name="thisAddr"> Base address for the range. </param>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If range byte is not in valid range. </exception>
        public static IPAddress FirstIAddressInRange( [NotNull] this IPAddress thisAddr, byte range ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)NetMask( range );
            return new IPAddress( IPAddress.HostToNetworkOrder( thisAsInt & mask ) );
        }


        /// <summary> Finds the last IP address in the given range. </summary>
        /// <param name="thisAddr"> Base address for the range. </param>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If range byte is not in valid range. </exception>
        public static IPAddress LastAddressInRange( [NotNull] this IPAddress thisAddr, byte range ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)~NetMask( range );
            return new IPAddress( (uint)IPAddress.HostToNetworkOrder( thisAsInt | mask ) );
        }


        /// <summary> Creates a mask for given range. </summary>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <exception cref="ArgumentOutOfRangeException"> If range byte is not in valid range. </exception>
        public static uint NetMask( byte range ) {
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            if( range == 0 ) {
                return 0;
            } else {
                return 0xffffffff << (32 - range);
            }
        }
    }


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


    public static class EnumerableUtil {
        /// <summary> Joins all items in a collection into one comma-separated string.
        /// If the items are not strings, .ToString() is called on them. </summary>
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( ',' ).Append( ' ' );
                sb.Append( item );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// If the items are not strings, .ToString() is called on them. </summary>
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items, [NotNull] string separator ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            if( separator == null ) throw new ArgumentNullException( "separator" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( separator );
                sb.Append( item );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// A specified string conversion function is called on each item before contactenation. </summary>
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items, [NotNull] Func<T, string> stringConversionFunction ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            if( stringConversionFunction == null ) throw new ArgumentNullException( "stringConversionFunction" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( ',' ).Append( ' ' );
                sb.Append( stringConversionFunction( item ) );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// A specified string conversion function is called on each item before contactenation. </summary>
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items, [NotNull] string separator, [NotNull] Func<T, string> stringConversionFunction ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            if( separator == null ) throw new ArgumentNullException( "separator" );
            if( stringConversionFunction == null ) throw new ArgumentNullException( "stringConversionFunction" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( separator );
                sb.Append( stringConversionFunction( item ) );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins formatted names of all IClassy objects in a collection into one string separated by spaces. </summary>
        public static string JoinToClassyString( [NotNull] this IEnumerable<IClassy> items ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            return items.JoinToString( "  ", p => p.ClassyName );
        }

        /// <summary> Joins formatted names of all IClassy objects in a collection into one string with a custom separator. </summary>
        public static string JoinToClassyString( [NotNull] this IEnumerable<IClassy> items, [NotNull] string separator ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            if( separator == null ) throw new ArgumentNullException( "separator" );
            return items.JoinToString( separator, p => p.ClassyName );
        }
    }


    unsafe static class FormatUtil {
        // Quicker StringBuilder.Append(int) by Sam Allen of http://www.dotnetperls.com
        public static StringBuilder Digits( [NotNull] this StringBuilder builder, int number ) {
            if( builder == null ) throw new ArgumentNullException( "builder" );
            if( number >= 100000000 || number < 0 ) {
                // Use system ToString.
                builder.Append( number );
            }
            int copy;
            int digit;
            if( number >= 10000000 ) {
                // 8.
                copy = number % 100000000;
                digit = copy / 10000000;
                builder.Append( (char)(digit + 48) );
            }
            if( number >= 1000000 ) {
                // 7.
                copy = number % 10000000;
                digit = copy / 1000000;
                builder.Append( (char)(digit + 48) );
            }
            if( number >= 100000 ) {
                // 6.
                copy = number % 1000000;
                digit = copy / 100000;
                builder.Append( (char)(digit + 48) );
            }
            if( number >= 10000 ) {
                // 5.
                copy = number % 100000;
                digit = copy / 10000;
                builder.Append( (char)(digit + 48) );
            }
            if( number >= 1000 ) {
                // 4.
                copy = number % 10000;
                digit = copy / 1000;
                builder.Append( (char)(digit + 48) );
            }
            if( number >= 100 ) {
                // 3.
                copy = number % 1000;
                digit = copy / 100;
                builder.Append( (char)(digit + 48) );
            }
            if( number >= 10 ) {
                // 2.
                copy = number % 100;
                digit = copy / 10;
                builder.Append( (char)(digit + 48) );
            }
            if( number >= 0 ) {
                // 1.
                copy = number % 10;
                builder.Append( (char)(copy + 48) );
            }
            return builder;
        }

        // Quicker Int32.Parse(string) by Karl Seguin
        public static int Parse( [NotNull] string stringToConvert ) {
            if( stringToConvert == null ) throw new ArgumentNullException( "stringToConvert" );
            int value = 0;
            int length = stringToConvert.Length;
            fixed( char* characters = stringToConvert ) {
                for( int i = 0; i < length; ++i ) {
                    value = 10 * value + (characters[i] - 48);
                }
            }
            return value;
        }

        // UppercaseFirst by Sam Allen of http://www.dotnetperls.com
        public static string UppercaseFirst( this string s ) {
            if( string.IsNullOrEmpty( s ) ) {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper( a[0] );
            return new string( a );
        }
    }


    /// <summary> Provides utility methods for working with byte arrays and pointers. </summary>
    public unsafe static class BufferUtil {

        /// <summary> Efficiently sets all bytes in the given array to a specified value. </summary>
        /// <param name="array"> Byte array to fill. </param>
        /// <param name="value"> Value to assign to every byte in the array. </param>
        /// <exception cref="ArgumentNullException"> If array is null. </exception>
        [PublicAPI]
        public static void MemSet( [NotNull] this byte[] array, byte value ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            byte[] rawValue = new[] { value, value, value, value, value, value, value, value };
            Int64 fillValue = BitConverter.ToInt64( rawValue, 0 );

            fixed( byte* ptr = array ) {
                Int64* dest = (Int64*)ptr;
                int length = array.Length;
                while( length >= 8 ) {
                    *dest = fillValue;
                    dest++;
                    length -= 8;
                }
                byte* bDest = (byte*)dest;
                for( byte i = 0; i < length; i++ ) {
                    *bDest = value;
                    bDest++;
                }
            }
        }



        /// <summary> Efficiently sets all bytes in a segment of the given byte array to a specified value. </summary>
        /// <param name="array"> Byte array to fill. </param>
        /// <param name="value"> Value to assign to bytes in the array segment. </param>
        /// <param name="startIndex"> Index at which to start filling the array. </param>
        /// <param name="length"> Number of bytes to set, starting with startIndex. </param>
        /// <exception cref="ArgumentNullException"> If array is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If length/startIndex are less than 0 or greater than array capacity. </exception>
        public static void MemSet( [NotNull] this byte[] array, byte value, int startIndex, int length ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            if( length < 0 || length > array.Length ) {
                throw new ArgumentOutOfRangeException( "length" );
            }
            if( startIndex < 0 || startIndex + length > array.Length ) {
                throw new ArgumentOutOfRangeException( "startIndex" );
            }

            byte[] rawValue = new[] { value, value, value, value, value, value, value, value };
            Int64 fillValue = BitConverter.ToInt64( rawValue, 0 );

            fixed( byte* ptr = &array[startIndex] ) {
                Int64* dest = (Int64*)ptr;
                while( length >= 8 ) {
                    *dest = fillValue;
                    dest++;
                    length -= 8;
                }
                byte* bDest = (byte*)dest;
                for( byte i = 0; i < length; i++ ) {
                    *bDest = value;
                    bDest++;
                }
            }
        }




        /// <summary> Efficiently copies raw memory contents from source byte pointer to destination byte pointer. </summary>
        /// <param name="source"> Source array pointer. </param>
        /// <param name="destination"> Destination array pointer. </param>
        /// <param name="bytesToCopy"> Number of bytes to copy. </param>
        /// <exception cref="ArgumentNullException"> If source or destination is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If bytesToCopy is less than 0. </exception>
        public static void MemCpy( [NotNull] byte* source, [NotNull] byte* destination, int bytesToCopy ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            if( bytesToCopy < 0 ) throw new ArgumentOutOfRangeException( "bytesToCopy" );
            if( bytesToCopy >= 0x10 ) {
                do {
                    *((int*)destination) = *((int*)source);
                    *((int*)(destination + 4)) = *((int*)(source + 4));
                    *((int*)(destination + 8)) = *((int*)(source + 8));
                    *((int*)(destination + 12)) = *((int*)(source + 12));
                    destination += 0x10;
                    source += 0x10;
                }
                while( (bytesToCopy -= 0x10) >= 0x10 );
            }
            if( bytesToCopy > 0 ) {
                if( (bytesToCopy & 8) != 0 ) {
                    *((int*)destination) = *((int*)source);
                    *((int*)(destination + 4)) = *((int*)(source + 4));
                    destination += 8;
                    source += 8;
                }
                if( (bytesToCopy & 4) != 0 ) {
                    *((int*)destination) = *((int*)source);
                    destination += 4;
                    source += 4;
                }
                if( (bytesToCopy & 2) != 0 ) {
                    *((short*)destination) = *((short*)source);
                    destination += 2;
                    source += 2;
                }
                if( (bytesToCopy & 1) != 0 ) {
                    destination++;
                    source++;
                    destination[0] = source[0];
                }
            }
        }



        /// <summary> Checks whether the sequence of bytes in data at the given offset matches the sequence of ASCII characters in value.
        /// Basically, checks whether the byte array contains the string at this offset. </summary>
        /// <param name="data"> Byte array to search. </param>
        /// <param name="offset"> Offset, in bytes, from the start of data. </param>
        /// <param name="value"> Value to search for. Must contain only ASCII characters. </param>
        /// <returns> Whether the pattern was found. </returns>
        /// <exception cref="ArgumentNullException">If data or value is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"> If offset is less than 0 or greater than data.Length. </exception>
        [Pure]
        public static bool MemCmp( [NotNull] byte[] data, int offset, [NotNull] string value ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            if( value == null ) throw new ArgumentNullException( "value" );
            if( offset < 0 || offset > data.Length ) throw new ArgumentOutOfRangeException( "offset" );
            for( int i = 0; i < value.Length; i++ ) {
                if( offset + i >= data.Length || data[offset + i] != value[i] ) return false;
            }
            return true;
        }
    }


    /// <summary> Provides utility methods for working with enumerations. </summary>
    public static class EnumUtil {
        /// <summary> Attempts to parse an enumeration </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <param name="output"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool TryParse<TEnum>( [NotNull] string value, out TEnum output, bool ignoreCase ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            try {
                output = (TEnum)Enum.Parse( typeof( TEnum ), value, ignoreCase );
                return Enum.IsDefined( typeof( TEnum ), output );
            } catch( ArgumentException ) {
                output = default( TEnum );
                return false;
            }
        }
    }
}