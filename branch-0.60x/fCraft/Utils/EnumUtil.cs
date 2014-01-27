// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides TryParse method, for parsing enumerations. </summary>
    public static class EnumUtil {
        /// <summary> Tries to parse a given value as an enumeration.
        /// Even if value is numeric, this method still ensures that given number is among the enumerated constants.
        /// This differs in behavior from Enum.Parse, which accepts any valid numeric string (that fits into enumeration's base type). </summary>
        /// <typeparam name="TEnum"> Enumeration type. </typeparam>
        /// <param name="value"> Raw string value to parse. </param>
        /// <param name="output"> Parsed enumeration to output. Set to default(TEnum) on failure. </param>
        /// <param name="ignoreCase"> Whether parsing should be case-insensitive. </param>
        /// <returns> Whether parsing succeeded. </returns>
        /// <exception cref="ArgumentNullException"> value is null. </exception>
        public static bool TryParse<TEnum>( [NotNull] string value, out TEnum output, bool ignoreCase ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            try {
                output = (TEnum)Enum.Parse( typeof( TEnum ), value, ignoreCase );
                return Enum.IsDefined( typeof( TEnum ), output );
            } catch( ArgumentException ) {
            } catch( OverflowException ) {
            }
            output = default( TEnum );
            return false;
        }


        /// <summary> Tries to parse a given value as an enumeration, with partial-name completion.
        /// If there is an exact name match, exact numeric match, or a single partial name match, completion succeeds.
        /// If there are no matches, multiple partial matches, or if value was an empty string, completion fails.
        /// Even if value is numeric, this method still ensures that given number is among the enumerated constants.
        /// This differs in behavior from Enum.Parse, which accepts any valid numeric string (that fits into enumeration's base type). </summary>
        /// <typeparam name="TEnum"> Enumeration type. </typeparam>
        /// <param name="value"> Raw string value to parse. </param>
        /// <param name="output"> Parsed enumeration to output. Set to default(TEnum) on failure. </param>
        /// <param name="ignoreCase"> Whether parsing should be case-insensitive. </param>
        /// <returns> Whether parsing/completion succeeded. </returns>
        /// <exception cref="ArgumentNullException"> value is null. </exception>
        public static bool TryComplete<TEnum>( [NotNull] string value, out TEnum output, bool ignoreCase ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            output = default(TEnum);

            // first, try to find an exact match
            try {
                output = (TEnum)Enum.Parse( typeof( TEnum ), value, ignoreCase );
                if( Enum.IsDefined( typeof( TEnum ), output ) ) {
                    return true;
                }
            } catch( ArgumentException ) {
                // No exact match found. Proceed unless value was an empty string.
                if( value.Length == 0 ) return false;
            } catch( OverflowException ) {
                // Value was a numeric string, beyond enum's scope.
                return false;
            }

            // Try name completion
            bool matchFound = false;
            StringComparison comparison = (ignoreCase
                                               ? StringComparison.OrdinalIgnoreCase
                                               : StringComparison.Ordinal);
            foreach( string name in Enum.GetNames( typeof( TEnum ) ) ) {
                if( name.StartsWith( value, comparison ) ) {
                    if( matchFound ) {
                        // Multiple matches found. Fail.
                        output = default(TEnum);
                        return false;
                    } else {
                        // First (and hopefully only) partial match found.
                        output = (TEnum)Enum.Parse( typeof( TEnum ), name );
                        matchFound = true;
                    }
                }
            }

            // Either 0 or 1 match found.
            return matchFound;
        }
    }
}