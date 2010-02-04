using System;
using System.Collections.Generic;

namespace fCraft {
    sealed class Color {
        static SortedList<string, char> colors = new SortedList<string, char>(16);

        public static void Init() {
            colors.Add( "black", '0' );
            colors.Add( "navy", '1' );
            colors.Add( "green", '2' );
            colors.Add( "teal", '3' );
            colors.Add( "maroon", '4' );
            colors.Add( "purple", '5' );
            colors.Add( "olive", '6' );
            colors.Add( "silver", '7' );
            colors.Add( "gray", '8' );
            colors.Add( "blue", '9' );
            colors.Add( "lime", 'a' );
            colors.Add( "aqua", 'b' );
            colors.Add( "red", 'c' );
            colors.Add( "magenta", 'b' );
            colors.Add( "yellow", 'e' );
            colors.Add( "white", 'f' );
        }

        public static char GetChar( string name ) {
            return colors[name];
        }

        public static string GetCode( string name ) {
            return "&"+colors[name];
        }

        public static string GetName( char code ) {
            return colors.Keys[colors.IndexOfValue( code )];
        }

        public static string Reset() {
            return "&f";
        }
    }
}
