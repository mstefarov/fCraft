// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides utility methods for working with byte arrays and pointers. </summary>
    public static unsafe class BufferUtil {
        /// <summary> Fills the entire given byte array with a specified byte value, as efficiently as possible. </summary>
        /// <param name="array"> Array to work with. </param>
        /// <param name="value"> Value to assign to each byte of the array. </param>
        /// <exception cref="ArgumentNullException"> array is null. </exception>
        public static void MemSet( [NotNull] this byte[] array, byte value ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            byte[] rawValue = { value, value, value, value, value, value, value, value };
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


        /// <summary> Fills a section of the given byte array with a specified byte value, as efficiently as possible. </summary>
        /// <param name="array"> Array to work with. </param>
        /// <param name="value"> Value to assign to each byte of the array. </param>
        /// <param name="startIndex"> Index of the first byte that should be set. </param>
        /// <param name="length"> Number of bytes of the array to set. </param>
        /// <exception cref="ArgumentNullException"> array is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> length is negative; startIndex is negative;
        /// or if (length+startIndex) is greater than array length. </exception>
        public static void MemSet( [NotNull] this byte[] array, byte value, int startIndex, int length ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            if( length < 0 || length > array.Length ) {
                throw new ArgumentOutOfRangeException( "length" );
            }
            if( startIndex < 0 || startIndex + length > array.Length ) {
                throw new ArgumentOutOfRangeException( "startIndex" );
            }

            byte[] rawValue = { value, value, value, value, value, value, value, value };
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


        /// <summary> Copies contents of src buffer to dest buffer, as efficiently as possible. </summary>
        /// <param name="src"> Source array/pointer. </param>
        /// <param name="dest"> Destination array/pointer. </param>
        /// <param name="len"> Number of bytes to copy. </param>
        public static void MemCpy( [NotNull] byte* src, [NotNull] byte* dest, int len ) {
            if( src == null ) throw new ArgumentNullException( "src" );
            if( dest == null ) throw new ArgumentNullException( "dest" );
            if( len >= 0x10 ) {
                do {
                    *( (int*)dest ) = *( (int*)src );
                    *( (int*)( dest + 4 ) ) = *( (int*)( src + 4 ) );
                    *( (int*)( dest + 8 ) ) = *( (int*)( src + 8 ) );
                    *( (int*)( dest + 12 ) ) = *( (int*)( src + 12 ) );
                    dest += 0x10;
                    src += 0x10;
                } while( ( len -= 0x10 ) >= 0x10 );
            }
            if( len > 0 ) {
                if( ( len & 8 ) != 0 ) {
                    *( (int*)dest ) = *( (int*)src );
                    *( (int*)( dest + 4 ) ) = *( (int*)( src + 4 ) );
                    dest += 8;
                    src += 8;
                }
                if( ( len & 4 ) != 0 ) {
                    *( (int*)dest ) = *( (int*)src );
                    dest += 4;
                    src += 4;
                }
                if( ( len & 2 ) != 0 ) {
                    *( (short*)dest ) = *( (short*)src );
                    dest += 2;
                    src += 2;
                }
                if( ( len & 1 ) != 0 ) {
                    dest++;
                    src++;
                    dest[0] = src[0];
                }
            }
        }


        /// <summary> Checks whether the sequence of bytes in data at the given offset matches the sequence of ASCII characters in value.
        /// Basically, checks whether the byte array contains the string at this offset. </summary>
        /// <param name="data"> Byte array to search. </param>
        /// <param name="offset"> Offset, in bytes, from the start of data. </param>
        /// <param name="value"> Value to search for. Must contain only ASCII characters. </param>
        /// <returns> Whether the pattern was found. </returns>
        /// <exception cref="ArgumentNullException"> data or value is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> offset is less than 0 or greater than data.Length. </exception>
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


        /// <summary> Reads a number of bytes from source that matches the length of destination byte array. </summary>
        /// <param name="source"> Stream to read from. </param>
        /// <param name="destination"> Byte array to write to. Length of this array is used. </param>
        /// <exception cref="ArgumentNullException"> source or destination is null. </exception>
        /// <exception cref="EndOfStreamException"> The end of stream is reached before destination array was filled. </exception>
        public static void ReadAll( [NotNull] Stream source, [NotNull] byte[] destination ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            int bytesRead = 0;
            int bytesLeft = destination.Length;
            while( bytesLeft > 0 ) {
                int readPass = source.Read( destination, bytesRead, bytesLeft );
                if( readPass == 0 ) throw new EndOfStreamException();
                bytesRead += readPass;
                bytesLeft -= readPass;
            }
        }
    }
}