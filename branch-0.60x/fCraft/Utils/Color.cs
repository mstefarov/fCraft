// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Static class with definitions of Minecraft color codes,
    /// parsers, converters, and utilities. </summary>
    public static class Color {
#pragma warning disable 1591
        public const string Black = "&0",
                            Navy = "&1",
                            Green = "&2",
                            Teal = "&3",
                            Maroon = "&4",
                            Purple = "&5",
                            Olive = "&6",
                            Silver = "&7",
                            Gray = "&8",
                            Blue = "&9",
                            Lime = "&a",
                            Aqua = "&b",
                            Red = "&c",
                            Magenta = "&d",
                            Yellow = "&e",
                            White = "&f";
#pragma warning restore 1591

        // User-defined color assignments. Set by Config.ApplyConfig.
        /// <summary> Color of system messages, nickserv, chanserv. </summary>
        public static string Sys;

        /// <summary> Color of help messages, /help. </summary>
        public static string Help;

        /// <summary> Color of say messages (/say) and timer announcements. </summary>
        public static string Say;

        /// <summary> Color of announcements, server announcements. </summary>
        public static string Announcement;

        /// <summary> Color of personal messages. </summary>
        public static string PM;

        /// <summary> Color of IRC chat. </summary>
        public static string IRC;

        /// <summary> Color of /me command. </summary>
        public static string Me;

        /// <summary> Color of warning messages. </summary>
        public static string Warning;

        // Defaults for user-defined colors.
        /// <summary> Default color of system messages, nickserv, chanserv. </summary>
        public const string SysDefault = Yellow;

        /// <summary> Default color of help messages, /help.</summary>
        public const string HelpDefault = Lime;

        /// <summary> Default color of say messages (/say) and timer announcements. </summary>
        public const string SayDefault = Green;

        /// <summary> Default color of announcements, server announcements.</summary>
        public const string AnnouncementDefault = Green;

        /// <summary> Default color of personal messages.</summary>
        public const string PMDefault = Aqua;

        /// <summary> Default color of IRC chat. </summary>
        public const string IRCDefault = Purple;

        /// <summary> Default color of /me command.</summary>
        public const string MeDefault = Purple;

        /// <summary> Default color of warning messages.</summary>
        public const string WarningDefault = Red;

        /// <summary> List of color names indexed by their id. </summary>
        public static readonly SortedList<char, string> ColorNames = new SortedList<char, string> {
            { '0', "black" },
            { '1', "navy" },
            { '2', "green" },
            { '3', "teal" },
            { '4', "maroon" },
            { '5', "purple" },
            { '6', "olive" },
            { '7', "silver" },
            { '8', "gray" },
            { '9', "blue" },
            { 'a', "lime" },
            { 'b', "aqua" },
            { 'c', "red" },
            { 'd', "magenta" },
            { 'e', "yellow" },
            { 'f', "white" }
        };


        /// <summary> Gets color name for hex color code. </summary>
        /// <param name="code"> Hexadecimal color code (between '0' and 'f'). </param>
        /// <returns> Lowercase color name. </returns>
        [CanBeNull]
        public static string GetName( char code ) {
            code = Char.ToLower( code );
            if( IsValidColorCode( code ) ) {
                return ColorNames[code];
            }
            string color = Parse( code );
            if( color == null ) {
                return null;
            }
            return ColorNames[color[1]];
        }


        /// <summary> Gets color name for a numeric color code. </summary>
        /// <param name="index"> Ordinal numeric color code (between 0 and 15). </param>
        /// <returns> Lowercase color name. If input is out of range, returns null. </returns>
        [CanBeNull]
        public static string GetName( int index ) {
            if( index >= 0 && index <= 15 ) {
                return ColorNames.Values[index];
            } else {
                return null;
            }
        }


        /// <summary> Gets color name for a string representation of a color. </summary>
        /// <param name="color"> Any parsable string representation of a color. </param>
        /// <returns> Lowercase color name.
        /// If input is an empty string, returns empty string.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string GetName( string color ) {
            if( color == null ) {
                return null;
            } else if( color.Length == 0 ) {
                return "";
            } else {
                string parsedColor = Parse( color );
                if( parsedColor == null ) {
                    return null;
                } else {
                    return GetName( parsedColor[1] );
                }
            }
        }


        /// <summary> Parses a string to a format readable by Minecraft clients. 
        /// an accept color names and color codes (with or without the ampersand). </summary>
        /// <param name="code"> Color code character. </param>
        /// <returns> Two-character color string, readable by Minecraft client.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string Parse( char code ) {
            code = Char.ToLower( code );
            if( IsValidColorCode( code ) ) {
                return "&" + code;
            } else {
                switch( code ) {
                    case 's':
                        return Sys;
                    case 'y':
                        return Say;
                    case 'p':
                        return PM;
                    case 'r':
                        return Announcement;
                    case 'h':
                        return Help;
                    case 'w':
                        return Warning;
                    case 'm':
                        return Me;
                    case 'i':
                        return IRC;
                    default:
                        return null;
                }
            }
        }


        /// <summary> Parses a string to a format readable by Minecraft clients. 
        /// an accept color names and color codes (with or without the ampersand). </summary>
        /// <param name="color"> Ordinal numeric color code (between 0 and 15). </param>
        /// <returns> Two-character color string, readable by Minecraft client.
        /// If input is an empty string, returns empty string.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string Parse( string color ) {
            if( color == null ) {
                return null;
            }
            color = color.ToLower();
            switch( color.Length ) {
                case 2:
                    if( color[0] == '&' && IsValidColorCode( color[1] ) ) {
                        return color;
                    }
                    break;

                case 1:
                    return Parse( color[0] );

                case 0:
                    return "";
            }
            if( ColorNames.ContainsValue( color ) ) {
                return "&" + ColorNames.Keys[ColorNames.IndexOfValue( color )];
            } else {
                return null;
            }
        }


        /// <summary> Checks whether a color code is valid (checks if it's hexadecimal char). </summary>
        /// <returns>True is char is valid, otherwise false</returns>
        public static bool IsValidColorCode( char code ) {
            return ( code >= '0' && code <= '9' ) || ( code >= 'a' && code <= 'f' ) || ( code >= 'A' && code <= 'F' );
        }


        /// <summary> Substitutes percent color codes with equivalent ampersand color codes. </summary>
        public static void ReplacePercentCodes( [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            sb.Replace( "%0", "&0" );
            sb.Replace( "%1", "&1" );
            sb.Replace( "%2", "&2" );
            sb.Replace( "%3", "&3" );
            sb.Replace( "%4", "&4" );
            sb.Replace( "%5", "&5" );
            sb.Replace( "%6", "&6" );
            sb.Replace( "%7", "&7" );
            sb.Replace( "%8", "&8" );
            sb.Replace( "%9", "&9" );
            sb.Replace( "%a", "&a" );
            sb.Replace( "%b", "&b" );
            sb.Replace( "%c", "&c" );
            sb.Replace( "%d", "&d" );
            sb.Replace( "%e", "&e" );
            sb.Replace( "%f", "&f" );
            sb.Replace( "%A", "&a" );
            sb.Replace( "%B", "&b" );
            sb.Replace( "%C", "&c" );
            sb.Replace( "%D", "&d" );
            sb.Replace( "%E", "&e" );
            sb.Replace( "%F", "&f" );
            sb.Replace( "%n", "\n" );
            sb.Replace( "%N", "\n" );
        }


        /// <summary> Substitutes percent color codes with equivalent ampersand color codes. </summary>
        [NotNull]
        public static string ReplacePercentCodes( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            StringBuilder sb = new StringBuilder( message );
            ReplacePercentCodes( sb );
            return sb.ToString();
        }


        /// <summary> Substitutes all special ampersand color codes (like Color.Sys)
        /// with the assigned Minecraft colors (like Color.Yellow). </summary>
        public static void SubstituteSpecialColors( [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            for( int i = sb.Length - 1; i > 0; i-- ) {
                if( sb[i - 1] == '&' ) {
                    switch( Char.ToLower( sb[i] ) ) {
                        case 's':
                            sb[i] = Sys[1];
                            break;
                        case 'y':
                            sb[i] = Say[1];
                            break;
                        case 'p':
                            sb[i] = PM[1];
                            break;
                        case 'r':
                            sb[i] = Announcement[1];
                            break;
                        case 'h':
                            sb[i] = Help[1];
                            break;
                        case 'w':
                            sb[i] = Warning[1];
                            break;
                        case 'm':
                            sb[i] = Me[1];
                            break;
                        case 'i':
                            sb[i] = IRC[1];
                            break;
                        default:
                            if( !IsValidColorCode( sb[i] ) ) {
                                sb.Remove( i - 1, 1 );
                            }
                            break;
                    }
                }
            }
        }


        /// <summary> Substitutes all special ampersand color codes (like Color.Sys)
        /// with the assigned Minecraft colors (like Color.Yellow). </summary>
        [NotNull]
        public static string SubstituteSpecialColors( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            SubstituteSpecialColors( sb );
            return sb.ToString();
        }


        /// <summary> Strips all ampersand color codes and doubled-up ampersands. </summary>
        public static string StripColors( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( input.IndexOf( '&' ) == -1 ) {
                return input;
            } else {
                StringBuilder output = new StringBuilder( input.Length );
                for( int i = 0; i < input.Length; i++ ) {
                    if( input[i] == '&' ) {
                        if( i == input.Length - 1 ) {
                            break;
                        }
                        i++;
                        if( input[i] == 'n' || input[i] == 'N' ) {
                            output.Append( '\n' );
                        }
                    } else {
                        output.Append( input[i] );
                    }
                }
                return output.ToString();
            }
        }


        #region IRC Colors

        /// <summary> String that resets formatting for following part of an IRC message. </summary>
        public const string IRCReset = "\u0003\u000f";

        /// <summary> String that toggles bold text on/off in IRC messages. </summary>
        public const string IRCBold = "\u0002";

        static readonly Dictionary<string, IRCColor> MinecraftToIRCColors = new Dictionary<string, IRCColor> {
            { White, IRCColor.White },
            { Black, IRCColor.Black },
            { Navy, IRCColor.Navy },
            { Green, IRCColor.Green },
            { Red, IRCColor.Red },
            { Maroon, IRCColor.Maroon },
            { Purple, IRCColor.Purple },
            { Olive, IRCColor.Olive },
            { Yellow, IRCColor.Yellow },
            { Lime, IRCColor.Lime },
            { Teal, IRCColor.Teal },
            { Aqua, IRCColor.Aqua },
            { Blue, IRCColor.Blue },
            { Magenta, IRCColor.Magenta },
            { Gray, IRCColor.Gray },
            { Silver, IRCColor.Silver },
        };


        public static void ToIRCColorCodes( [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            SubstituteSpecialColors( sb );
            foreach( KeyValuePair<string, IRCColor> code in MinecraftToIRCColors ) {
                string replacement = '\u0003' + ( (int)code.Value ).ToStringInvariant().PadLeft( 2, '0' );
                sb.Replace( code.Key, replacement );
            }
        }


        public static string ToIRCColorCodes( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            ToIRCColorCodes( sb );
            return sb.ToString();
        }

        #endregion
    }


    enum IRCColor {
        White = 0,
        Black,
        Navy,
        Green,
        Red,
        Maroon,
        Purple,
        Olive,
        Yellow,
        Lime,
        Teal,
        Aqua,
        Blue,
        Magenta,
        Gray,
        Silver
    }
}