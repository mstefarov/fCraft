// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
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
    }
}