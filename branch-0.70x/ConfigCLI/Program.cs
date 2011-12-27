using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using fCraft;
using fCraft.Events;

namespace fCraft.ConfigCLI {
    static class Program {
        public static bool UseColor { get; private set; }


        static void Main( string[] args ) {
            Console.Title = "fCraft Configuration (" + Updater.CurrentRelease.VersionString + ")";
            try {
                Logger.Logged += OnLogged;
                Console.WriteLine( "Initializing fCraft..." );
                Server.InitLibrary( args );
                UseColor = !Server.HasArg( ArgKey.NoConsoleColor );

                if( !File.Exists( Paths.ConfigFileName ) ) {
                    Console.WriteLine( "Configuration ({0}) was not found. Using defaults.",
                                       Paths.ConfigFileName );
                }

                Config.Load();

                MakeSectionMenu();

                menuState = MenuState.SectionList;
                StateLoop();


            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ConfigCLI", "ConfigCLI", ex, true );
                ReportFailure( ShutdownReason.Crashed );
            }
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

        static void ShowSeparator() {
            if( UseColor) Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
            Console.WriteLine( Separator );
            if( UseColor ) Console.ResetColor();
        }


        static TextMenu sectionMenu;
        static TextOption optionSaveAndExit, optionQuit, optionResetEverything, optionReloadConfig;

        static TextMenu MakeSectionMenu() {
            sectionMenu = new TextMenu();
            ConfigSection[] sections = (ConfigSection[])Enum.GetValues( typeof( ConfigSection ) );
            for( int i = 0; i < sections.Length; i++ ) {
                sectionMenu.AddOption( (i+1).ToString(), sections[i].ToString(), sections[i] );
            }
            optionSaveAndExit = sectionMenu.AddOption( "S", "Save and exit" );
            optionQuit = sectionMenu.AddOption( "Q", "Quit without saving" );
            optionResetEverything = sectionMenu.AddOption( "D", "Reset everything to defaults" );
            optionReloadConfig = sectionMenu.AddOption( "R", "Reload config" );
            return sectionMenu;
        }


        static void WriteHeader( string text ) {
            if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine( text );
            if( UseColor ) Console.ResetColor();
        }


        static MenuState ShowSectionList() {
            ShowSeparator();
            WriteHeader( "Config sections:" );
            var choice = sectionMenu.Show( "Enter your selection: " );

                if( choice == optionSaveAndExit ) {
                    if( Config.Save() ) {
                        return MenuState.Done;
                    }

                } else if( choice == optionQuit ) {
                    if( TextMenu.ShowYesNo( "Exit without saving?" ) ) {
                        return MenuState.Done;
                    }

                } else if( choice == optionResetEverything ) {
                    if( TextMenu.ShowYesNo( "Reset everything?" ) ) {
                        Config.LoadDefaults();
                    }

                } else if( choice == optionReloadConfig ) {
                    Config.Reload();

                } else {
                    currentSection = (ConfigSection)choice.Tag;
                    return MenuState.KeyList;
                }

            return MenuState.SectionList;
        }


        static MenuState ShowKeyList() {
            ShowSeparator();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine( "Keys in section {0}:", currentSection );
            Console.ResetColor();
            Console.WriteLine( "   0. .." );

            ConfigKey[] keys = currentSection.GetKeys();

            int maxLen = keys.Select( key => key.ToString().Length ).Max();

            for( int i = 0; i < keys.Length; i++ ) {
                var meta = keys[i].GetMetadata();
                string formattedValue;
                if( meta.ValueType == typeof( int ) ||
                    meta.ValueType == typeof( bool ) ) {
                    formattedValue = keys[i].GetString();
                } else {
                    formattedValue = "\"" + keys[i].GetString() + "\"";
                }
                string str = String.Format( "  {0,2}. {1," + maxLen + "} = {2}",
                                            i + 1,
                                            keys[i],
                                            formattedValue );
                if( !keys[i].IsDefault() ) {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine( str );
                    Console.ResetColor();
                } else {
                    Console.WriteLine( str );
                }
            }

            string replyString;
            int reply;
            do {
                Console.Write( "Enter key number: " );
                replyString = Console.ReadLine();
            } while( !Int32.TryParse( replyString, out reply ) ||
                     reply < 0 || reply > keys.Length );

            if( reply == 0 ) {
                return MenuState.SectionList;
            } else {
                currentKey = keys[reply - 1];
                return MenuState.Key;
            }
        }


        static MenuState ShowKey() {
            ShowSeparator();
            var meta = currentKey.GetMetadata();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine( "Key {0} in section {1}", currentKey, currentSection );

            Console.Write( "  Description: " );
            Console.ResetColor();
            string[] newlineSeparator = new[] { "\r\n" };
            string[] descriptionLines = meta.Description.Split( newlineSeparator, StringSplitOptions.RemoveEmptyEntries );
            Console.WriteLine( descriptionLines[0] );
            for( int i = 1; i < descriptionLines.Length; i++ ) {
                Console.WriteLine( "    " + descriptionLines[i] );
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write( "  Type: " );
            Console.ResetColor();
            Console.WriteLine( meta.ValueType.Name );

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write( "  Default value: " );
            Console.ResetColor();
            Console.WriteLine( meta.DefaultValue );

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write( "  Current value: " );
            Console.ResetColor();
            Console.WriteLine( currentKey.GetString() );

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write( "  New value: " );
            Console.ResetColor();

            while( true ) {
                try {
                    currentKey.SetValue( Console.ReadLine() );
                    break;
                } catch( FormatException ex ) {
                    Console.WriteLine( ex.Message );
                }
            }

            return MenuState.KeyList;
        }


        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "fCraft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            if( UseColor ) Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if( UseColor ) Console.ResetColor();
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, false, false ), true );
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
    }

    enum MenuState {
        SectionList,
        KeyList,
        Key,
        Done
    }
}