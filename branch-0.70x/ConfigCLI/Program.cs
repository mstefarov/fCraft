using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using fCraft.Events;

namespace fCraft.ConfigCLI {
    static class Program {
        public static bool UseColor { get; private set; }


        static void Main( string[] args ) {
            Console.Title = "fCraft Configuration (" + Updater.CurrentRelease.VersionString + ")";
#if !DEBUG
            try {
#endif
            Logger.Logged += OnLogged;
            Console.WriteLine( "Initializing fCraft..." );
            Server.InitLibrary( args );
            UseColor = !Server.HasArg( ArgKey.NoConsoleColor );

            if( !File.Exists( Paths.ConfigFileName ) ) {
                Console.WriteLine( "Configuration ({0}) was not found. Using defaults.",
                                   Paths.ConfigFileName );
            }

            Config.Load();

            menuState = MenuState.SectionList;
            StateLoop();

#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ConfigCLI", "ConfigCLI", ex, true );
                ReportFailure( ShutdownReason.Crashed );
            }
#endif
        }


        static MenuState menuState;
        static ConfigSection currentSection;
        static ConfigKey currentKey;

        static void StateLoop() {
            while( menuState != MenuState.Done ) {
                switch( menuState ) {
                    case MenuState.SectionList:
                        menuState = ShowSectionList();
                        break;

                    case MenuState.KeyList:
                        menuState = ShowKeyList();
                        break;

                    case MenuState.Key:
                        menuState = ShowKey();
                        break;
                }
            }
        }


        const string Separator = "===============================================================================";


        static void Refresh() {
            Console.Clear();
            if( UseColor ) Console.ForegroundColor = ConsoleColor.DarkGray;
            switch( menuState ) {
                case MenuState.KeyList:
                    WriteHeader( "Section {0}", currentSection );
                    break;
                case MenuState.Key:
                    WriteHeader( "Section {0} > Key {1}", currentSection, currentKey );
                    break;
            }
            Console.WriteLine( Separator );
            if( UseColor ) Console.ResetColor();
        }


        static MenuState ShowSectionList() {
            Refresh();

            TextMenu sectionMenu = new TextMenu();
            TextOption optionSaveAndExit, optionQuit, optionResetEverything, optionReloadConfig;

            ConfigSection[] sections = (ConfigSection[])Enum.GetValues( typeof( ConfigSection ) );
            for( int i = 0; i < sections.Length; i++ ) {
                sectionMenu.AddOption( ( i + 1 ).ToString(), sections[i].ToString(), sections[i] );
            }

            sectionMenu.Column = Column.Right;
            optionSaveAndExit = sectionMenu.AddOption( "S", "Save and exit" );
            optionQuit = sectionMenu.AddOption( "Q", "Quit without saving" );
            optionResetEverything = sectionMenu.AddOption( "D", "Use defaults" );
            optionReloadConfig = sectionMenu.AddOption( "R", "Reload config" );

            var choice = sectionMenu.Show( "Enter your selection: " );

            if( choice == optionSaveAndExit ) {
                if( TextMenu.ShowYesNo( "Save and exit?" ) && Config.Save() ) {
                    return MenuState.Done;
                }

            } else if( choice == optionQuit ) {
                if( TextMenu.ShowYesNo( "Exit without saving?" ) ) {
                    return MenuState.Done;
                }

            } else if( choice == optionResetEverything ) {
                if( TextMenu.ShowYesNo( "Reset everything to defaults?" ) ) {
                    Config.LoadDefaults();
                }

            } else if( choice == optionReloadConfig ) {
                if( File.Exists( Paths.ConfigFileName ) ) {
                    if( TextMenu.ShowYesNo( "Reload configuration from \"" + Paths.ConfigFileName + "\"?" ) ) {
                        Config.Reload();
                        Console.WriteLine( "Configuration file \"{0}\" reloaded.", Paths.ConfigFileName );
                    }
                } else {
                    Console.WriteLine( "Configuration file \"{0}\" does not exist.", Paths.ConfigFileName );
                }

            } else {
                currentSection = (ConfigSection)choice.Tag;
                return MenuState.KeyList;
            }

            return MenuState.SectionList;
        }


        static MenuState ShowKeyList() {
            Refresh();

            TextMenu menu = new TextMenu();
            TextOption optionBack = menu.AddOption( "0", "Back to sections" );
            TextOption optionDefaults = menu.AddOption( "D", "Use defaults" );
            menu.AddSpacer( Column.Left );

            ConfigKey[] keys = currentSection.GetKeys();
            int maxLen = keys.Select( key => key.ToString().Length ).Max();

            for( int i = 0; i < keys.Length; i++ ) {
                string str = String.Format( "{0} = {1}",
                                            keys[i].ToString().PadLeft( maxLen ),
                                            keys[i].GetPresentationString() );
                TextOption option = new TextOption( ( i + 1 ).ToString(), str, Column.Left );
                if( !keys[i].IsDefault() ) {
                    option.ForeColor = ConsoleColor.White;
                }
                option.Tag = keys[i];
                menu.AddOption( option );
            }

            TextOption choice = menu.Show( "Enter key number: " );

            if( choice == optionBack ) {
                return MenuState.SectionList;

            } else if( choice == optionDefaults ) {
                if( TextMenu.ShowYesNo( "Reset everything in section " + currentSection + " to defaults?" ) ) {
                    Config.LoadDefaults( currentSection );
                }

            } else {
                currentKey = (ConfigKey)choice.Tag;
                return MenuState.Key;
            }

            return MenuState.KeyList;
        }


        static MenuState ShowKey() {
            Refresh();
            Type valueType = currentKey.GetValueType();

            if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
            Console.Write( "    Value Type: " );
            if( UseColor ) Console.ResetColor();
            if( valueType.IsEnum ) {
                Console.WriteLine( "{0} (enumeration)", valueType.Name );
            } else if( valueType == typeof( int ) ) {
                Console.WriteLine( "Integer" );
            } else if( valueType == typeof( bool ) ) {
                Console.WriteLine( "{0} (true/false)", valueType.Name );
            } else {
                Console.WriteLine( valueType.Name );
            }

            if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine( "   Description:" );
            if( UseColor ) Console.ResetColor();
            string[] newlineSeparator = new[] { "\r\n" };
            string[] descriptionLines = currentKey.GetDescription().Split( newlineSeparator, StringSplitOptions.RemoveEmptyEntries );
            foreach( string line in descriptionLines ) {
                Console.WriteLine( "    " + line );
            }

            if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
            Console.Write( " Default value: " );
            PrintKeyValue( currentKey.GetDefault().ToString() );

            if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
            Console.Write( " Current value: " );
            PrintKeyValue( currentKey.GetRawString() );

            if( valueType.IsEnum ) {
                if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
                Console.Write( "       Choices: " );
                if( UseColor ) Console.ResetColor();
                Console.WriteLine( Enum.GetNames( valueType ).JoinToString() );
            } else if( currentKey.IsColor() ) {
                PrintColorList();
            }

            Console.WriteLine();
            TextMenu menu = new TextMenu();
            TextOption optionBack = menu.AddOption( 0, "Back to " + currentSection );
            TextOption optionChange = menu.AddOption( 1, "Change value" );
            TextOption optionDefaults = menu.AddOption( 2, "Use default" );

            TextOption choice = menu.Show();
            if( choice == optionBack ) {
                return MenuState.KeyList;
            } else if( choice == optionChange ) {
                while( true ) {
                    try {
                        Console.Write( "Enter new value for {0}: ", currentKey );
                        currentKey.SetValue( Console.ReadLine() );
                        break;
                    } catch( FormatException ex ) {
                        Console.WriteLine( ex.Message );
                    }
                }
            } else if( choice == optionDefaults ) {
                currentKey.SetValue( currentKey.GetDefault() );
            }
            return MenuState.Key;
        }

        static void PrintColorList() {
            if( UseColor ) {
                Console.ForegroundColor = ConsoleColor.White;

                Console.Write( "       Choices: " );
                PrintColor( Color.Black );
                PrintColor( Color.Navy );
                PrintColor( Color.Green );
                PrintColor( Color.Teal );
                PrintColor( Color.Maroon );
                Console.WriteLine();

                Console.Write( "                " );
                PrintColor( Color.Purple );
                PrintColor( Color.Olive );
                PrintColor( Color.Silver );
                PrintColor( Color.Gray );
                PrintColor( Color.Blue );
                Console.WriteLine();

                Console.Write( "                " );
                PrintColor( Color.Lime );
                PrintColor( Color.Aqua );
                PrintColor( Color.Red );
                PrintColor( Color.Magenta );
                PrintColor( Color.Yellow );
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine( "                White (&f)" );
                Console.ResetColor();

            } else {
                Console.WriteLine(
@"       Choices: Black (&0), Navy (&1), Green (&2), Teal (&3), Maroon (&4),
                Purple (&5), Olive (&6), Silver (&7), Gray (&8), Blue (&9),
                Lime (&a), Aqua (&b), Red (&c), Magenta (&d), Yellow (&e),
                White (&f)" );
            }
        }

        static void PrintColor( string color ) {
            ConsoleColor parsedColor = Color.ToConsoleColor( color );
            if( parsedColor == ConsoleColor.Black ) {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            } else {
                Console.ForegroundColor = parsedColor;
            }
            Console.Write( "{0} ({1})", Color.GetName( color ), Color.Parse( color ) );
            Console.ResetColor();
            Console.Write( ", " );
        }

        static void PrintKeyValue( string value ) {
            if( UseColor ) {
                if( currentKey.IsColor() ) {
                    Console.ForegroundColor = Color.ToConsoleColor( value );
                    Console.Write( currentKey.GetPresentationString( value ) );
                    if( currentKey.IsDefault( value ) ) {
                        Console.ResetColor();
                        Console.Write( " (default)" );
                    }
                    Console.WriteLine();
                } else {
                    if( currentKey.IsDefault( value ) ) Console.ResetColor();
                    Console.WriteLine( currentKey.GetPresentationString( value ) );
                }
            } else {
                Console.WriteLine( currentKey.GetPresentationString( value ) );
            }
        }


        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "fCraft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            if( UseColor ) Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if( UseColor ) Console.ResetColor();
            if( !Server.HasArg( ArgKey.ExitOnCrash ) ) {
                Console.ReadLine();
            }
        }


        [DebuggerStepThrough]
        static void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            switch( e.MessageType ) {
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                case LogType.SeriousError:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                case LogType.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine( e.Message );
                    Console.ResetColor();
                    return;

                default:
                    Console.WriteLine( e.Message );
                    return;
            }
        }


        static void WriteHeader( string text, params object[] args ) {
            if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine( text, args );
            if( UseColor ) Console.ResetColor();
        }
    }

    enum MenuState {
        SectionList,
        KeyList,
        Key,
        Done
    }
}