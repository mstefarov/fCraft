using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.ConfigCLI {
    class TextMenu {
        public readonly Dictionary<string, TextOption> Options = new Dictionary<string, TextOption>();

        public TextOption AddOption( TextOption newOption ) {
            Options.Add( newOption.Label.ToLower(), newOption );
            return newOption;
        }


        public TextOption AddOption( int label, string text ) {
            TextOption newOption = new TextOption( label.ToString(), text );
            Options.Add( newOption.Label, newOption );
            return newOption;
        }


        public TextOption AddOption( string label, string text ) {
            TextOption newOption = new TextOption( label, text );
            Options.Add( label.ToLower(), newOption );
            return newOption;
        }


        public TextOption AddOption( string label, string text, object tag ) {
            TextOption newOption = new TextOption( label, text ) {
                Tag = tag
            };
            Options.Add( label.ToLower(), newOption );
            return newOption;
        }


        public TextOption AddOption( string label, string description, ConsoleColor foreColor, ConsoleColor backColor ) {
            TextOption newOption = new TextOption( label, description ) {
                ForeColor = foreColor,
                BackColor = backColor
            };
            Options.Add( label.ToLower(), newOption );
            return newOption;
        }


        public void PrintOptions() {
            int maxOptionLength = Options.Keys.Max( key => key.Length );
            foreach( TextOption option in Options.Values ) {
                if( Program.UseColor ) {
                        Console.BackgroundColor = option.BackColor;
                        Console.ForegroundColor = option.ForeColor;
                }
                Console.WriteLine( "{0}. {1}", option.Label.PadLeft( maxOptionLength ), option.Text );
            }
            if( Program.UseColor ) Console.ResetColor();
        }

        public TextOption Show() {
            return Show( "Select an option: " );
        }

        public TextOption Show( string prompt ) {
            PrintOptions();
            while( true ) {
                Console.Write( prompt );
                string input = Console.ReadLine().ToLower();
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