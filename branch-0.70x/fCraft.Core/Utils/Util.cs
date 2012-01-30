// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

// Miscallaneous utilities
namespace fCraft {
    public static class EnumerableUtil {
        /// <summary> Joins all items in a collection into one string separated with commas and spaces (", "). </summary>
        /// <param name="items"> Sequence of items to join. ToString() is called on each item. </param>
        /// <typeparam name="T"> Type of items. </typeparam>
        /// <returns> A string containing all the items, or an empty string if items was empty. </returns>
        /// <exception cref="ArgumentNullException"> If items is null. </exception>
        [NotNull]
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


        /// <summary> Joins all items in a collection into one string separated with the given separator. </summary>
        /// <param name="items"> Sequence of items to join. ToString() is called on each item. </param>
        /// <param name="separator"> Separator/delimeter (string to insert between items). </param>
        /// <typeparam name="T"> Type of items. </typeparam>
        /// <returns> A string containing all the items, or an empty string if items was empty. </returns>
        /// <exception cref="ArgumentNullException"> If items or separator is null. </exception>
        [NotNull]
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


        /// <summary> Joins all items in a collection into one string separated with commas and spaces (", ").
        /// A specified string conversion function is called on each item before contactenation. </summary>
        /// <param name="items"> Sequence of items to join. </param>
        /// <param name="stringConversionFunction"> Function that converts each item to a string representation. </param>
        /// <typeparam name="T"> Type of items. </typeparam>
        /// <returns> A string containing all the items, or an empty string if items was empty. </returns>
        /// <exception cref="ArgumentNullException"> If items or stringConversionFunction is null. </exception>
        [NotNull]
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
        /// <param name="items"> Sequence of items to join. </param>
        /// <param name="separator"> Separator/delimeter (string to insert between items). </param>
        /// <param name="stringConversionFunction"> Function that converts each item to a string representation. </param>
        /// <typeparam name="T"> Type of items. </typeparam>
        /// <returns> A string containing all the items, or an empty string if items was empty. </returns>
        /// <exception cref="ArgumentNullException"> If any of the parameters are null. </exception>
        [NotNull]
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

        
        /// <summary> Joins formatted names of all IClassy objects in a collection into one string,
        /// with items separated by two spaces. </summary>
        /// <param name="items"> Sequence of items to join. ClassyName property of each object is used. </param>
        /// <returns> A string containing all the items, or an empty string if items was empty. </returns>
        /// <exception cref="ArgumentNullException"> If items is null. </exception>
        [NotNull]
        public static string JoinToClassyString( [NotNull] this IEnumerable<IClassy> items ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            return items.JoinToString( "  ", p => p.ClassyName );
        }


        /// <summary> Joins formatted names of all IClassy objects in a collection into one string with a custom separator. </summary>
        /// <param name="items"> Sequence of items to join. ClassyName property of each object is used. </param>
        /// <param name="separator"> Separator/delimeter (string to insert between items). </param>
        /// <returns> A string containing all the items, or an empty string if items was empty. </returns>
        /// <exception cref="ArgumentNullException"> If items or separator is null. </exception>
        [NotNull]
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