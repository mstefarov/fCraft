// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;


namespace fCraft {

    /// <summary>
    /// Static class with definitions of Minecraft color codes, parsers/converters, and utilities.
    /// </summary>
    public static class Color {
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

        // User-defined color assignments. Set by Config.ApplyConfig.
        public static string Sys, Help, Say, Announcement, PM, IRC, Me, Warning;

        // Defaults for user-defined colors.
        public const string SysDefault = Yellow,
                            HelpDefault = Lime,
                            SayDefault = Green,
                            AnnouncementDefault = Green,
                            PMDefault = Aqua,
                            IRCDefault = Purple,
                            MeDefault = Purple,
                            WarningDefault = Red;

        static SortedList<char, string> colorNames = new SortedList<char, string>{
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
        /// <param name="code">Hexadecimal color code (between '0' and 'f')</param>
        /// <returns>Lowercase color name</returns>
        /// <exception cref="System.ArgumentException">Thrown when code is not hexadecimal</exception>
        public static string GetName( char code ) {
            code = Char.ToLower( code );
            if( IsValidColorCode( code ) ) {
                return colorNames[code];
            } else {
                return colorNames[Parse( code )[1]];
            }
        }


        public static string Parse( char code ) {
            code = Char.ToLower( code );
            if( IsValidColorCode( code ) ) {
                return "&" + code;
            } else {
                switch( code ) {
                    case 's': return Color.Sys;
                    case 'y': return Color.Say;
                    case 'p': return Color.PM;
                    case 'r': return Color.Announcement;
                    case 'h': return Color.Help;
                    case 'w': return Color.Warning;
                    case 'm': return Color.Me;
                    case 'i': return Color.IRC;
                    default:
                        throw new ArgumentException( "Expected a hexadecimal color code.", "code" );
                }
            }
        }


        /// <summary> Gets color name for a string representation of a color. </summary>
        /// <param name="color"> Any parsable string representation of a color. </param>
        /// <returns> Lowercase color name </returns>
        /// <exception cref="System.ArgumentException"> Thrown when color cannot be parsed </exception>
        public static string GetName( string color ) {
            if( color != null && color.Length > 0 ) {
                return GetName( Parse( color )[1] );
            } else {
                throw new ArgumentException( "Could not parse color.", "color" );
            }
        }


        /// <summary> Gets color name for a numeric color code. </summary>
        /// <param name="index"> Ordinal numeric color code (between 0 and 15) </param>
        /// <returns> Lowercase color name </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"> Thrown when color code is not between 0 and 15 </exception>
        public static string GetName( int index ) {
            if( index >= 0 && index <= 15 ) {
                return colorNames.Values[index];
            } else {
                throw new ArgumentOutOfRangeException( "index", "Expected an ordinal numeric color code, between 0 and 15" );
            }
        }

        /// <summary> Parses a numeric color code to a string readable by Minecraft clients </summary>
        /// <param name="index"> Ordinal numeric color code (between 0 and 15) </param>
        /// <returns> Two-character color string, readable by Minecraft client </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"> Thrown when color code is not between 0 and 15 </exception>
        public static string Parse( int index ) {
            if( index >= 0 && index <= 15 ) {
                return "&" + colorNames.Keys[index];
            } else {
                throw new ArgumentOutOfRangeException( "index", "Expected an ordinal numeric color code, between 0 and 15" );
            }
        }


        /// <summary> Parses a string to a format readable by Minecraft clients. Can accept color names, colorcodes </summary>
        /// <param name="index"> Ordinal numeric color code (between 0 and 15) </param>
        /// <returns> Two-character color string, readable by Minecraft client </returns>
        /// <exception cref="System.ArgumentException"> Thrown when color cannot be parsed </exception>
        public static string Parse( string color ) {
            color = color.ToLower();
            if( color.Length == 2 && color[0] == '&' && IsValidColorCode( color[1] ) ) {
                return color;
            } else if( colorNames.ContainsValue( color ) ) {
                return "&" + colorNames.Keys[colorNames.IndexOfValue( color )];
            } else if( color.Length == 1 ) {
                return Parse( color[0] );
            } else {
                throw new ArgumentException( "Could not parse color.", "color" );
            }
        }


        public static int ParseToIndex( string color ) {
            color = color.ToLower();
            if( color.Length == 2 && color[0] == '&' ) {
                if( colorNames.ContainsKey( color[1] ) ) {
                    return colorNames.IndexOfKey( color[1] );
                } else {
                    switch( color ) {
                        case "&s": return colorNames.IndexOfKey( Color.Sys[1] );
                        case "&y": return colorNames.IndexOfKey( Color.Say[1] );
                        case "&p": return colorNames.IndexOfKey( Color.PM[1] );
                        case "&r": return colorNames.IndexOfKey( Color.Announcement[1] );
                        case "&h": return colorNames.IndexOfKey( Color.Help[1] );
                        case "&w": return colorNames.IndexOfKey( Color.Warning[1] );
                        case "&m": return colorNames.IndexOfKey( Color.Me[1] );
                        case "&i": return colorNames.IndexOfKey( Color.IRC[1] );
                        default: return 15;
                    }
                }
            } else if( colorNames.ContainsValue( color ) ) {
                return colorNames.IndexOfValue( color );
            } else {
                return 15; // white
            }
        }


        /// <summary>
        /// Checks whether a color code is valid (checks if it's hexadecimal char).
        /// </summary>
        /// <returns>True is char is valid, otherwise false</returns>
        public static bool IsValidColorCode( char code ) {
            return (code >= '0' && code <= '9') || (code >= 'a' && code <= 'f') || (code >= 'A' && code <= 'F');
        }




        static Dictionary<string, IRCColor> MinecraftToIRCColors = new Dictionary<string, IRCColor> {
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


        public static string ToIRCColorCodes( string input ) {
            StringBuilder sb = new StringBuilder( input );

            sb.Replace("&s", Color.Sys);
            sb.Replace("&S", Color.Sys);
            
            sb.Replace("&y", Color.Say);
            sb.Replace("&Y", Color.Say);
            
            sb.Replace("&p", Color.PM);
            sb.Replace("&P", Color.PM);
            
            sb.Replace("&r", Color.Announcement);
            sb.Replace("&R", Color.Announcement);
            
            sb.Replace("&h", Color.Help);
            sb.Replace("&H", Color.Help);
            
            sb.Replace("&w", Color.Warning);
            sb.Replace("&W", Color.Warning);
            
            sb.Replace("&m", Color.Me);
            sb.Replace("&M", Color.Me);
            
            sb.Replace("&i", Color.IRC);
            sb.Replace("&I", Color.IRC);

            foreach( KeyValuePair<string, IRCColor> code in MinecraftToIRCColors ) {
                sb.Replace( code.Key, '\u0003' + ((int)code.Value).ToString().PadLeft( 2, '0' ) );
            }
            return sb.ToString();
        }

        public const string IRCReset = "\u0003\u000f";
        public const string IRCBold = "\u0002";
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