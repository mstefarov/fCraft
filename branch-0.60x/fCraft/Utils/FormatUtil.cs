// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    // Helper methods for working with strings and for serialization/parsing
    static unsafe class FormatUtil {
        // Quicker StringBuilder.Append(int) by Sam Allen of http://www.dotnetperls.com
        [NotNull]
        public static StringBuilder Digits( [NotNull] this StringBuilder builder, int number ) {
            if( builder == null ) throw new ArgumentNullException( "builder" );
            if( number >= 100000000 || number < 0 ) {
                // Use system ToString.
                builder.Append( number );
                return builder;
            }
            int copy;
            int digit;
            if( number >= 10000000 ) {
                // 8.
                copy = number % 100000000;
                digit = copy / 10000000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 1000000 ) {
                // 7.
                copy = number % 10000000;
                digit = copy / 1000000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 100000 ) {
                // 6.
                copy = number % 1000000;
                digit = copy / 100000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 10000 ) {
                // 5.
                copy = number % 100000;
                digit = copy / 10000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 1000 ) {
                // 4.
                copy = number % 10000;
                digit = copy / 1000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 100 ) {
                // 3.
                copy = number % 1000;
                digit = copy / 100;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 10 ) {
                // 2.
                copy = number % 100;
                digit = copy / 10;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 0 ) {
                // 1.
                copy = number % 10;
                builder.Append( (char)( copy + 48 ) );
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
                    value = 10 * value + ( characters[i] - 48 );
                }
            }
            return value;
        }


        // UppercaseFirst by Sam Allen of http://www.dotnetperls.com
        [NotNull]
        public static string UppercaseFirst( this string s ) {
            if( string.IsNullOrEmpty( s ) ) {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper( a[0] );
            return new string( a );
        }


        [NotNull]
        public static string ToStringInvariant( this int i ) {
            return i.ToString( CultureInfo.InvariantCulture );
        }


        public static int IndexOfOrdinal( [NotNull] this string haystack, [NotNull] string needle ) {
            return haystack.IndexOf( needle, StringComparison.Ordinal );
        }
    }
}