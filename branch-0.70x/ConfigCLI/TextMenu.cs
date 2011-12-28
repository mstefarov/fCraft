using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft.ConfigCLI {
    enum Column {
        Left,
        Right
    }

    class TextMenu {
        public readonly Dictionary<string, TextOption> Options = new Dictionary<string, TextOption>();
        List<TextOption> LinesLeft = new List<TextOption>();
        public Column Column { get; set; }

        public TextOption AddOption( TextOption newOption ) {
            Options.Add( newOption.Label.ToLower(), newOption );
            LinesLeft.Add( newOption );
            return newOption;
        }


        public TextOption AddOption( int label, string text ) {
            return AddOption( new TextOption( label.ToString(), text, Column ) );
        }


        public TextOption AddOption( string label, string text ) {
            return AddOption( new TextOption( label, text, Column ) );
        }


        public TextOption AddOption( string label, string text, object tag ) {
            TextOption newOption = new TextOption( label, text, Column ) {
                Tag = tag
            };
            return AddOption( newOption );
        }


        public TextOption AddOption( string label, string description, ConsoleColor foreColor, ConsoleColor backColor ) {
            TextOption newOption = new TextOption( label, description, Column ) {
                ForeColor = foreColor,
                BackColor = backColor
            };
            return AddOption( newOption );
        }


        public void AddSpacer( Column column ) {
            if( column == Column.Left ) {
                LinesLeft.Add( TextOption.SpacerLeft );
            } else {
                LinesLeft.Add( TextOption.SpacerRight );
            }
        }


        public void PrintOptions() {
            bool hasRightSide = LinesLeft.Any( line => line.Column == Column.Right );

            if( hasRightSide ) {
                var listLeft = LinesLeft.Where( line => line.Column == Column.Left ).ToArray();
                var listRight = LinesLeft.Where( line => line.Column == Column.Right ).ToArray();
                int maxLeftOptionLength = listLeft.Where( line => line.Label != null ).Max( line => line.Label.Length );
                int maxRightOptionLength = listRight.Where( line => line.Label != null ).Max( line => line.Label.Length );
                int maxSize = Math.Max( listLeft.Length, listRight.Length );
                for( int i = 0; i < maxSize; i++ ) {
                    if( i >= listLeft.Length ) {
                        TextOption option = listRight[i];
                        if( Program.UseColor ) {
                            Console.BackgroundColor = option.BackColor;
                            Console.ForegroundColor = option.ForeColor;
                        }
                        if( option.Label == null ) {
                            Console.WriteLine( "{0}", option.Text.PadLeft( 40 + maxRightOptionLength + 2 ) );
                        } else {
                            Console.WriteLine( "{0}. {1}", option.Label.PadLeft( 40 + maxRightOptionLength ), option.Text );
                        }

                    } else if( i >= listRight.Length ) {
                        TextOption option = listLeft[i];
                        if( Program.UseColor ) {
                            Console.BackgroundColor = option.BackColor;
                            Console.ForegroundColor = option.ForeColor;
                        }
                        if( option.Label == null ) {
                            Console.WriteLine( "{0}", option.Text.PadLeft( maxLeftOptionLength + 2 ) );
                        } else {
                            Console.WriteLine( "{0}. {1}", option.Label.PadLeft( maxLeftOptionLength ), option.Text );
                        }

                    } else {
                        TextOption option1 = listLeft[i];
                        TextOption option2 = listRight[i];
                        if( Program.UseColor ) {
                            Console.BackgroundColor = option1.BackColor;
                            Console.ForegroundColor = option1.ForeColor;
                        }
                        if( option1.Label == null ) {
                            string text = option1.Text;
                            Console.Write( text.PadRight( 40 ).Substring( 0, 40 ) );
                        } else {
                            string text = String.Format( "{0}. {1}", option1.Label.PadLeft( maxLeftOptionLength ), option1.Text );
                            Console.Write( text.PadRight( 40 ).Substring( 0, 40 ) );
                        }
                        if( Program.UseColor ) {
                            Console.BackgroundColor = option2.BackColor;
                            Console.ForegroundColor = option2.ForeColor;
                        }
                        if( option2.Label == null ) {
                            Console.WriteLine( "{0}", option2.Text.PadLeft( maxRightOptionLength + 2 ) );
                        } else {
                            Console.WriteLine( "{0}. {1}", option2.Label.PadLeft( maxRightOptionLength ), option2.Text );
                        }
                    }
                }

            } else {
                int maxOptionLength = LinesLeft.Where( line => line.Label != null ).Max( line => line.Label.Length );
                foreach( TextOption option in LinesLeft ) {
                    if( Program.UseColor ) {
                        Console.BackgroundColor = option.BackColor;
                        Console.ForegroundColor = option.ForeColor;
                    }
                    if( option.Label == null ) {
                        Console.WriteLine( "{0}", option.Text.PadLeft( maxOptionLength + 2 ) );
                    } else {
                        Console.WriteLine( "{0}. {1}", option.Label.PadLeft( maxOptionLength ), option.Text );
                    }
                }
            }

            if( Program.UseColor ) Console.ResetColor();
        }


        public TextOption Show() {
            return Show( "Select an option: " );
        }


        public TextOption Show( string prompt ) {
            PrintOptions();
            Console.WriteLine();
            while( true ) {
                Console.Write( prompt );
                string input = Console.ReadLine().ToLower();
                if( String.IsNullOrWhiteSpace( input ) ) continue;
                TextOption result;
                if( Options.TryGetValue( input, out result ) ) {
                    return result;
                } else {
                    Console.WriteLine( "\"{0}\" is not a recognized option. Try again.", input );
                }
            }
        }


        public static bool ShowYesNo( string prompt ) {
            while( true ) {
                Console.Write( prompt + " (Y/N): " );
                string input = Console.ReadLine().ToLower();
                if( input.Equals( "yes", StringComparison.OrdinalIgnoreCase ) || input.Equals( "y", StringComparison.OrdinalIgnoreCase ) ) {
                    return true;
                } else if( input.Equals( "no", StringComparison.OrdinalIgnoreCase ) || input.Equals( "n", StringComparison.OrdinalIgnoreCase ) ) {
                    return false;
                }
            }
        }
    }
}