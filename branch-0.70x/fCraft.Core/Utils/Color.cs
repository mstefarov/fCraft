// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using System.Linq;

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
        /// <summary> Color of say messages, /say. </summary>
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
        /// <summary> Default color of say messages, /say. </summary>
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
        public static readonly SortedList<char, string> ColorNames = new SortedList<char, string>{
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
        public static string GetName( [CanBeNull] string color ) {
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
                    case 's': return Sys;
                    case 'y': return Say;
                    case 'p': return PM;
                    case 'r': return Announcement;
                    case 'h': return Help;
                    case 'w': return Warning;
                    case 'm': return Me;
                    case 'i': return IRC;
                    default:
                        return null;
                }
            }
        }


        /// <summary> Parses a numeric color code to a string readable by Minecraft clients </summary>
        /// <param name="index"> Ordinal numeric color code (between 0 and 15). </param>
        /// <returns> Two-character color string, readable by Minecraft client.
        /// If input cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string Parse( int index ) {
            if( index >= 0 && index <= 15 ) {
                return "&" + ColorNames.Keys[index];
            } else {
                return null;
            }
        }


        /// <summary> Parses a string to a format readable by Minecraft clients. 
        /// an accept color names and color codes (with or without the ampersand). </summary>
        /// <param name="color"> Ordinal numeric color code (between 0 and 15). </param>
        /// <returns> Two-character color string, readable by Minecraft client.
        /// If input is an empty string, returns empty string.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string Parse( [CanBeNull] string color ) {
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

        /// <summary> Gets the index of the specified color. </summary>
        /// <param name="color"> Color to parse. </param>
        /// <returns> Index of the specified color. </returns>
        public static int ParseToIndex( [NotNull] string color ) {
            if( color == null ) throw new ArgumentNullException( "color" );
            color = color.ToLower();
            if( color.Length == 2 && color[0] == '&' ) {
                if( ColorNames.ContainsKey( color[1] ) ) {
                    return ColorNames.IndexOfKey( color[1] );
                } else {
                    switch( color ) {
                        case "&s": return ColorNames.IndexOfKey( Sys[1] );
                        case "&y": return ColorNames.IndexOfKey( Say[1] );
                        case "&p": return ColorNames.IndexOfKey( PM[1] );
                        case "&r": return ColorNames.IndexOfKey( Announcement[1] );
                        case "&h": return ColorNames.IndexOfKey( Help[1] );
                        case "&w": return ColorNames.IndexOfKey( Warning[1] );
                        case "&m": return ColorNames.IndexOfKey( Me[1] );
                        case "&i": return ColorNames.IndexOfKey( IRC[1] );
                        default: return 15;
                    }
                }
            } else if( ColorNames.ContainsValue( color ) ) {
                return ColorNames.IndexOfValue( color );
            } else {
                return 15; // white
            }
        }


        /// <summary> Checks whether a color code is valid (checks if it's hexadecimal char). </summary>
        public static bool IsValidColorCode( char code ) {
            return (code >= '0' && code <= '9') || (code >= 'a' && code <= 'f') || (code >= 'A' && code <= 'F');
        }


        /// <summary> Substitutes percent color codes with equivalent ampersand color codes. </summary>
        public static void ReplacePercentCodes( [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            sb.Replace( "%%", "%" );
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
        }

        /// <summary> Substitutes percent color codes with equivalent ampersand color codes. </summary>
        [NotNull]
        public static string ReplacePercentCodes( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( input.IndexOf( '%' ) == -1 ) {
                return input;
            } else {
                StringBuilder sb = new StringBuilder( input );
                ReplacePercentCodes( sb );
                return sb.ToString();
            }
        }


        /// <summary> Substitutes all special ampersand color codes (like Color.Sys)
        /// with the assigned Minecraft colors (like Color.Yellow). </summary>
        public static void SubstituteSpecialColors( [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            for( int i = sb.Length - 1; i > 0; i-- ) {
                if( sb[i - 1] == '&' ) {
                    switch( Char.ToLower( sb[i] ) ) {
                        case 's': sb[i] = Sys[1]; break;
                        case 'y': sb[i] = Say[1]; break;
                        case 'p': sb[i] = PM[1]; break;
                        case 'r': sb[i] = Announcement[1]; break;
                        case 'h': sb[i] = Help[1]; break;
                        case 'w': sb[i] = Warning[1]; break;
                        case 'm': sb[i] = Me[1]; break;
                        case 'i': sb[i] = IRC[1]; break;
                        default:
                            if( IsValidColorCode( sb[i] ) ) {
                                continue;
                            } else {
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


        /// <summary> Escapes (doubles up) all ampersands in a string. </summary>
        public static void EscapeAmpersands( [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            sb.Replace( "&", "&&" );
        }


        /// <summary> Escapes (doubles up) all ampersands in a string. </summary>
        [NotNull]
        public static string EscapeAmpersands( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( input.IndexOf( '&' ) == -1 ) {
                return input;
            } else {
                return input.Replace( "&", "&&" );
            }
        }


        /// <summary> Strips all ampersand color codes, and unescapes doubled-up ampersands. </summary>
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
                        } else if( input[i + 1] == '&' ) {
                            output.Append( '&' );
                        }
                        i++;
                    } else {
                        output.Append( input[i] );
                    }
                }
                return output.ToString();
            }
        }


        #region IRC Colors
        /// <summary> String that indicates formatting should be reset./// </summary>
        public const string IRCReset = "\u0003\u000f";
        /// <summary> String that indicates the following text will be bold. </summary>
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
                string replacement = '\u0003' + ((int)code.Value).ToString( CultureInfo.InvariantCulture ).PadLeft( 2, '0' );
                sb.Replace( code.Key, replacement );
            }
        }


        public static string ToIRCColorCodes( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            ToIRCColorCodes( sb );
            return sb.ToString();
        }

        /*
        static IRCColor ToIRCColor( string colorCode ) {
            string parsedColor = Parse( colorCode );
            if( String.IsNullOrEmpty( parsedColor ) ) {
                throw new FormatException( "Could not parse color." );
            }
            return MinecraftToIRCColors[parsedColor];
        }

        static string Parse( IRCColor ircColor ) {
            return MinecraftToIRCColors.First( pair => pair.Value == ircColor ).Key;
        }
        */

        #endregion


        #region Console Colors

        static readonly Dictionary<string, ConsoleColor> MinecraftToConsoleColors = new Dictionary<string, ConsoleColor> {
            { White, ConsoleColor.White },
            { Black, ConsoleColor.Black },
            { Navy, ConsoleColor.DarkBlue },
            { Green, ConsoleColor.DarkGreen },
            { Red, ConsoleColor.Red },
            { Maroon, ConsoleColor.DarkRed },
            { Purple, ConsoleColor.DarkMagenta },
            { Olive, ConsoleColor.DarkYellow },
            { Yellow, ConsoleColor.Yellow },
            { Lime, ConsoleColor.Green },
            { Teal, ConsoleColor.DarkCyan },
            { Aqua, ConsoleColor.Cyan },
            { Blue, ConsoleColor.Blue },
            { Magenta, ConsoleColor.Magenta },
            { Gray, ConsoleColor.DarkGray },
            { Silver, ConsoleColor.Gray },
        };

        public static ConsoleColor ToConsoleColor( string colorCode ) {
            string parsedColor = Parse( colorCode );
            if( String.IsNullOrEmpty( parsedColor ) ) {
                throw new FormatException( "Could not parse color." );
            }
            return MinecraftToConsoleColors[parsedColor];
        }

        public static string Parse( ConsoleColor consoleColor ) {
            return MinecraftToConsoleColors.First( pair => pair.Value == consoleColor ).Key;
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