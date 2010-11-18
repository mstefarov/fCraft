// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;


namespace fCraft {
    public static class Color {
        public const string Black   = "&0",
                            Navy    = "&1",
                            Green   = "&2",
                            Teal    = "&3",
                            Maroon  = "&4",
                            Purple  = "&5",
                            Olive   = "&6",
                            Silver  = "&7",
                            Gray    = "&8",
                            Blue    = "&9",
                            Lime    = "&a",
                            Aqua    = "&b",
                            Red     = "&c",
                            Magenta = "&d",
                            Yellow  = "&e",
                            White   = "&f";

        public static string Sys, Help, Say, Announcement, PM, IRC, Me, Warning;

        static SortedList<char, string> colors = new SortedList<char, string>(16);


        static Color() {
            colors.Add( '0', "black" );
            colors.Add( '1', "navy" );
            colors.Add( '2', "green" );
            colors.Add( '3', "teal" );
            colors.Add( '4', "maroon" );
            colors.Add( '5', "purple" );
            colors.Add( '6', "olive" );
            colors.Add( '7', "silver" );
            colors.Add( '8', "gray" );
            colors.Add( '9', "blue" );
            colors.Add( 'a', "lime" );
            colors.Add( 'b', "aqua" );
            colors.Add( 'c', "red" );
            colors.Add( 'd', "magenta" );
            colors.Add( 'e', "yellow" );
            colors.Add( 'f', "white" );
        }


        public static string GetName( char code ) {
            return colors[code];
        }

        public static string GetName( string color ) {
            if( color != null && color.Length > 0 && Parse( color ) != null ) {
                return GetName( Parse( color )[1] );
            } else {
                return "";
            }
        }

        public static string GetName( int index ) {
            return colors.Values[index];
        }

        public static string Parse( int index ) {
            if( index >= 0 && index <= 15 ) {
                return "&" + colors.Keys[index];
            } else {
                return White;
            }
        }

        public static string Parse( string color ) {
            color = color.ToLower();
            if( color.Length == 2 && color[0] == '&' && colors.ContainsKey( color[1] ) ) {
                return color;
            } else if( colors.ContainsValue( color ) ) {
                return "&" + colors.Keys[colors.IndexOfValue( color )];
            } else {
                return null;
            }
        }


        public static int ParseToIndex( string color ) {
            color = color.ToLower();
            if( color.Length == 2 && color[0] == '&' ) {
                if( colors.ContainsKey( color[1] ) ) {
                    return colors.IndexOfKey( color[1] );
                } else {
                    switch( color ) {
                        case "&s": return colors.IndexOfKey( Color.Sys[1] );
                        case "&y": return colors.IndexOfKey( Color.Say[1] );
                        case "&p": return colors.IndexOfKey( Color.PM[1] );
                        case "&r": return colors.IndexOfKey( Color.Announcement[1] );
                        case "&h": return colors.IndexOfKey( Color.Help[1] );
                        case "&w": return colors.IndexOfKey( Color.Warning[1] );
                        case "&m": return colors.IndexOfKey( Color.Me[1] );
                        case "&i": return colors.IndexOfKey( Color.IRC[1] );
                        default: return 15;
                    }
                }
            } else if( colors.ContainsValue( color ) ) {
                return colors.IndexOfValue( color );
            } else {
                return 15; // white
            }
        }


        public static bool IsValidColorCode( char code ) {
            return code >= '0' && code <= '9' || code >= 'a' && code <= 'f' || code >= 'A' && code <= 'F';
        }
    }
}
