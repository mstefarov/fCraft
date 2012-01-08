// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.ConfigCLI {
    enum Column {
        Left,
        Right
    }


    sealed class TextMenu {
        readonly Dictionary<string, TextOption> options = new Dictionary<string, TextOption>();
        readonly List<TextOption> lines = new List<TextOption>();
        public Column Column { get; set; }


        [NotNull]
        public TextOption AddOption( [NotNull] TextOption newOption ) {
            if( newOption == null ) throw new ArgumentNullException( "newOption" );
            if( newOption.Label != null ) {
                options.Add( newOption.Label.ToLower(), newOption );
            }
            lines.Add( newOption );
            return newOption;
        }


        [NotNull]
        public TextOption AddOption( int label, [NotNull] string text ) {
            if( text == null ) throw new ArgumentNullException( "text" );
            return AddOption( new TextOption( label.ToString( CultureInfo.InvariantCulture ), text, Column ) );
        }


        [NotNull]
        public TextOption AddOption( [CanBeNull] string label, [NotNull] string text ) {
            if( text == null ) throw new ArgumentNullException( "text" );
            return AddOption( new TextOption( label, text, Column ) );
        }


        [NotNull]
        public TextOption AddOption( int label, [NotNull] string text, [CanBeNull] object tag ) {
            TextOption newOption = new TextOption( label.ToString( CultureInfo.InvariantCulture ), text, Column ) {
                Tag = tag
            };
            return AddOption( newOption );
        }


        [NotNull]
        public TextOption AddOption( [CanBeNull] string label, [NotNull] string text, [CanBeNull] object tag ) {
            TextOption newOption = new TextOption( label, text, Column ) {
                Tag = tag
            };
            return AddOption( newOption );
        }


        public void AddSpacer() {
            if( Column == Column.Left ) {
                lines.Add( TextOption.SpacerLeft );
            } else {
                lines.Add( TextOption.SpacerRight );
            }
        }


        const int ColumnSize = 40;

        void PrintOptions() {
            bool hasRightSide = lines.Any( line => line.Column == Column.Right );

            if( hasRightSide ) {
                var listLeft = lines.Where( line => line.Column == Column.Left ).ToArray();
                var listRight = lines.Where( line => line.Column == Column.Right ).ToArray();
                int maxLeftOptionLength = listLeft.Where( line => line.Label != null )
                                                  .Max( line => line.Label.Length );
                int maxRightOptionLength = listRight.Where( line => line.Label != null )
                                                    .Max( line => line.Label.Length );
                int maxSize = Math.Max( listLeft.Length, listRight.Length );

                for( int i = 0; i < maxSize; i++ ) {
                    if( i >= listLeft.Length ) {
                        TextOption option = listRight[i];
                        if( option.Label == null ) {
                            SetColor( option );
                            Console.WriteLine( option.Text.PadLeft( ColumnSize + maxRightOptionLength + 2 ) );
                        } else {
                            Console.Write( "{0}. ", option.Label.PadLeft( ColumnSize + maxRightOptionLength ) );
                            SetColor( option );
                            Console.WriteLine( option.Text );
                        }
                        ResetColor();

                    } else if( i >= listRight.Length ) {
                        TextOption option = listLeft[i];
                        if( option.Label == null ) {
                            SetColor( option );
                            Console.WriteLine( option.Text.PadLeft( maxLeftOptionLength + 2 ) );
                        } else {
                            Console.Write( "{0}. ", option.Label.PadLeft( maxLeftOptionLength ));
                            SetColor(option);
                            Console.WriteLine( option.Text );
                        }
                        ResetColor();

                    } else {
                        TextOption option1 = listLeft[i];
                        TextOption option2 = listRight[i];

                        if( option1.Label == null ) {
                            SetColor( option1 );
                            Console.Write( option1.Text.PadRight( ColumnSize ).Substring( 0, ColumnSize ) );
                        } else {
                            Console.Write( "{0}. ", option1.Label.PadLeft( maxLeftOptionLength ) );
                            SetColor( option1 );
                            Console.Write( option1.Text.PadRight( ColumnSize ).Substring( 0, ColumnSize ) );
                        }
                        ResetColor();

                        if( option2.Label == null ) {
                            SetColor( option2 );
                            Console.WriteLine( option2.Text.PadLeft( maxRightOptionLength + 2 ) );
                        } else {
                            Console.Write( "{0}. ", option2.Label.PadLeft( maxRightOptionLength ) );
                            SetColor( option2 );
                            Console.WriteLine( option2.Text );
                        }
                        ResetColor();
                    }
                }

            } else {
                int maxOptionLength = lines.Where( line => line.Label != null ).Max( line => line.Label.Length );
                foreach( TextOption option in lines ) {
                    if( option.Label == null ) {
                        SetColor( option );
                        Console.WriteLine( option.Text.PadLeft( maxOptionLength + 2 ) );
                    } else {
                        Console.Write( "{0}. ", option.Label.PadLeft( maxOptionLength ) );
                        SetColor( option );
                        Console.WriteLine( option.Text );
                    }
                    ResetColor();
                }
            }

            if( Program.UseColor ) Console.ResetColor();
        }


        static void SetColor( [NotNull] TextOption option ) {
            if( option == null ) throw new ArgumentNullException( "option" );
            if( !Program.UseColor ) return;
            Console.BackgroundColor = option.BackColor;
            Console.ForegroundColor = option.ForeColor;
        }


        static void ResetColor() {
            if( Program.UseColor ) Console.ResetColor();
        }


        [NotNull]
        public TextOption Show() {
            return Show( "Enter your selection: " );
        }


        [NotNull]
        public TextOption Show( [NotNull] string prompt ) {
            if( prompt == null ) throw new ArgumentNullException( "prompt" );
            PrintOptions();
            Console.WriteLine();
            while( true ) {
                Console.Write( prompt );
                string input = Console.ReadLine().ToLower();
                if( String.IsNullOrWhiteSpace( input ) ) continue;
                TextOption result;
                if( options.TryGetValue( input, out result ) ) {
                    return result;
                } else {
                    Console.WriteLine( "\"{0}\" is not a recognized option. Try again.", input );
                }
            }
        }


        [StringFormatMethod("prompt")]
        public static bool ShowYesNo( [NotNull] string prompt, params object[] formatArgs ) {
            if( prompt == null ) throw new ArgumentNullException( "prompt" );
            while( true ) {
                if( Program.UseColor ) Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write( prompt + " (Y/N): ", formatArgs );
                string input = Console.ReadLine().ToLower();
                if( Program.UseColor ) Console.ResetColor();

                if( input.Equals( "yes", StringComparison.OrdinalIgnoreCase ) ||
                    input.Equals( "y", StringComparison.OrdinalIgnoreCase ) ) {
                    return true;
                } else if( input.Equals( "no", StringComparison.OrdinalIgnoreCase ) ||
                    input.Equals( "n", StringComparison.OrdinalIgnoreCase ) ) {
                    return false;
                }
            }
        }


        public static int ShowNumber( [NotNull] string prompt, int min, int max ) {
            if( prompt == null ) throw new ArgumentNullException( "prompt" );
            while( true ) {
                Console.Write( "{0} ({1}-{2}, or press enter to cancel): ", prompt, min, max );
                string input = Console.ReadLine();
                int choice;
                if( input.Length == 0 ) {
                    return -1;
                }else if( Int32.TryParse( input, out choice ) ) {
                    if( choice >= min && choice <= max ) {
                        return choice;
                    }
                }
            }
        }
    }
}