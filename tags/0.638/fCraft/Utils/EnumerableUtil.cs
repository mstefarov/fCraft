// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides methods JoinToString/JoinToClassyString methods
    /// for merging lists and enumerations into strings. </summary>
    public static class EnumerableUtil {
        /// <summary> Joins all items in a collection into one comma-separated string.
        /// If the items are not strings, .ToString() is called on them. </summary>
        [NotNull, Pure]
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
        [NotNull, Pure]
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
        [NotNull, Pure]
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items,
                                              [NotNull] Func<T, string> stringConversionFunction ) {
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
        [NotNull, Pure]
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items, [NotNull] string separator,
                                              [NotNull] Func<T, string> stringConversionFunction ) {
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


        /// <summary> Joins formatted names of all IClassy objects in a collection into one comma-separated string. </summary>
        [NotNull, Pure]
        public static string JoinToClassyString( [NotNull] this IEnumerable<IClassy> items ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            return items.JoinToString( "  ", p => p.ClassyName );
        }
    }
}