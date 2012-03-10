// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using fCraft.Events;

namespace fCraft.ConfigCLI {
    static class ConfigCLI {
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

        const string Separator = "===============================================================================";


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

                    case MenuState.Ranks:
                        menuState = ShowRanks();
                        break;

                    case MenuState.RankDetails:
                        menuState = ShowRankDetails();
                        break;

                    case MenuState.Permissions:
                        menuState = ShowPermissions();
                        break;

                    case MenuState.PermissionLimits:
                        menuState = ShowPermissionLimits();
                        break;

                    case MenuState.PermissionLimitDetails:
                        menuState = ShowPermissionLimitDetails();
                        break;

                    case MenuState.RankAdd:
                        menuState = ShowAddRank();
                        break;
                }
            }
        }


        static void Refresh( [NotNull] string heading, [NotNull] params object[] formatArgs ) {
            if( heading == null ) throw new ArgumentNullException( "heading" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );

            Console.WriteLine();
            Console.WriteLine();

            if( UseColor ) Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( Separator );

            WriteHeader( heading, formatArgs );

            if( UseColor ) Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( Separator );
            if( UseColor ) Console.ResetColor();
        }


        [StringFormatMethod( "text" )]
        static void WriteHeader( string text, params object[] args ) {
            if( UseColor ) Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine( text, args );
            if( UseColor ) Console.ResetColor();
        }


        [StringFormatMethod( "text" )]
        static void WriteWarning( string text, params object[] args ) {
            if( UseColor ) Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine( text, args );
            if( UseColor ) Console.ResetColor();
        }


        static MenuState ShowSectionList() {
            Refresh( "Editing {0}", Paths.ConfigFileName );

            TextMenu menu = new TextMenu();

            ConfigSection[] sections = (ConfigSection[])Enum.GetValues( typeof( ConfigSection ) );
            for( int i = 0; i < sections.Length; i++ ) {
                menu.AddOption( i + 1,
                                sections[i].ToString(),
                                sections[i] );
            }
            TextOption optionRanks = menu.AddOption( sections.Length + 1, "Ranks" );

            menu.Column = Column.Right;
            TextOption optionSaveAndExit = menu.AddOption( "S", "Save and exit" );
            TextOption optionQuit = menu.AddOption( "Q", "Quit without saving" );
            TextOption optionResetEverything = menu.AddOption( "D", "Use defaults" );
            TextOption optionReloadConfig = menu.AddOption( "R", "Reload config" );

            var choice = menu.Show();

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
                    RankManager.ResetToDefaults();
                    Config.ResetLogOptions();
                }

            } else if( choice == optionReloadConfig ) {
                if( File.Exists( Paths.ConfigFileName ) ) {
                    if( TextMenu.ShowYesNo( "Reload configuration from \"{0}\"?",
                                            Paths.ConfigFileName ) ) {
                        Config.Reload( true );
                        Console.WriteLine( "Configuration file \"{0}\" reloaded.", Paths.ConfigFileName );
                    }
                } else {
                    Console.WriteLine( "Configuration file \"{0}\" does not exist.", Paths.ConfigFileName );
                }

            } else if( choice == optionRanks ) {
                return MenuState.Ranks;

            } else {
                currentSection = (ConfigSection)choice.Tag;
                return MenuState.KeyList;
            }

            return MenuState.SectionList;
        }


        #region Keys

        static MenuState ShowKeyList() {
            Refresh( "Section {0}", currentSection );

            TextMenu menu = new TextMenu();
            TextOption optionBack = menu.AddOption( "B", "Back to sections" );
            TextOption optionDefaults = menu.AddOption( "D", "Use defaults" );
            menu.AddSpacer();

            ConfigKey[] keys = currentSection.GetKeys();
            int maxLen = keys.Select( key => key.ToString().Length ).Max();

            for( int i = 0; i < keys.Length; i++ ) {
                string str = String.Format( "{0} = {1}",
                                            keys[i].ToString().PadLeft( maxLen, '.' ),
                                            keys[i].GetPresentationString() );
                TextOption option = new TextOption( ( i + 1 ).ToString( CultureInfo.InvariantCulture ),
                                                    str,
                                                    Column.Left );
                if( !keys[i].IsDefault() ) {
                    option.ForeColor = ConsoleColor.White;
                }
                option.Tag = keys[i];
                menu.AddOption( option );
            }

            TextOption choice = menu.Show();

            if( choice == optionBack ) {
                return MenuState.SectionList;

            } else if( choice == optionDefaults ) {
                if( TextMenu.ShowYesNo( "Reset everything in section {0} to defaults?",
                                        currentSection ) ) {
                    Config.LoadDefaults( currentSection );
                }

            } else {
                currentKey = (ConfigKey)choice.Tag;
                return MenuState.Key;
            }

            return MenuState.KeyList;
        }


        static MenuState ShowKey() {
            Refresh("Section {0} > Key {1}", currentSection, currentKey );
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
            string[] descriptionLines = currentKey.GetDescription().Split( newlineSeparator,
                                                                           StringSplitOptions.RemoveEmptyEntries );
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
            TextOption optionBack = menu.AddOption( "B", "Back to " + currentSection );
            TextOption optionChange = menu.AddOption( "C", "Change value" );
            TextOption optionDefaults = menu.AddOption( "D", "Use default" );

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
                        WriteWarning( ex.Message );
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

        #endregion


        #region Ranks

        static Rank currentRank;
        static Permission currentPermission;

        static MenuState ShowRanks() {
            Refresh( "Rank list" );

            TextMenu menu = new TextMenu();

            for( int i = 0; i < RankManager.Ranks.Count; i++ ) {
                Rank rank = RankManager.Ranks[i];
                TextOption derp = menu.AddOption( i + 1,
                                                  rank.Name,
                                                  rank );
                derp.ForeColor = Color.ToConsoleColor( rank.Color );
                if( derp.ForeColor == ConsoleColor.Black ) {
                    derp.BackColor = ConsoleColor.Gray;
                }
            }

            TextOption optionErase = null, optionRaise = null, optionLower = null;

            menu.Column = Column.Right;
            TextOption optionBack = menu.AddOption( "B", "Back to sections" );

            menu.AddSpacer();
            TextOption optionAdd = menu.AddOption( "A", "Add rank (blank)" );
            TextOption optionCopy = menu.AddOption( "C", "Copy existing rank" );
            if( RankManager.Ranks.Count > 1 ) {
                optionErase = menu.AddOption( "E", "Erase rank" );
            }

            if( RankManager.Ranks.Count > 1 ) {
                menu.AddSpacer();
                optionRaise = menu.AddOption( "R", "Raise rank in hierarchy" );
                optionLower = menu.AddOption( "L", "Lower rank in hierarchy" );
            }

            menu.AddSpacer();
            TextOption optionDefaults = menu.AddOption( "D", "Use defaults" );

            TextOption choice = menu.Show();

            if( choice == optionBack ) {
                return MenuState.SectionList;

            } else if( choice == optionAdd ) {
                Console.Write( "Enter new rank name: " );
                while( true ) {
                    string rankName = Console.ReadLine();
                    if( Rank.IsValidRankName( rankName ) ) {
                        if( RankManager.FindRank( rankName ) != null ) {
                            WriteWarning( "A rank with this name already exists." );
                        } else {
                            Rank newRank = new Rank( rankName, RankManager.GenerateID() );
                            AddRank( newRank );
                            break;
                        }
                    } else {
                        WriteWarning( "Rank names must be between 1 and 16 characters long, " +
                                      "and must contain only letters, digits, and underscores." );
                    }
                }

            } else if( choice == optionCopy ) {
                int rankToCopyIndex = TextMenu.ShowNumber( "Which rank to copy?",
                                                       1, RankManager.Ranks.Count );
                if( rankToCopyIndex != -1 ) {
                    Console.WriteLine();
                    Rank rankToCopy = RankManager.Ranks[rankToCopyIndex - 1];
                    Console.Write( "Enter new rank name: " );
                    while( true ) {
                        string rankName = Console.ReadLine();
                        if( Rank.IsValidRankName( rankName ) ) {
                            if( RankManager.FindRank( rankName ) != null ) {
                                WriteWarning( "A rank with this name already exists." );
                            } else {
                                Rank newRank = new Rank( rankName, RankManager.GenerateID(), rankToCopy );
                                AddRank( newRank );
                                break;
                            }
                        } else {
                            WriteWarning( "Rank names must be between 1 and 16 characters long, " +
                                          "and must contain only letters, digits, and underscores." );
                        }
                    }
                }

            } else if( choice == optionErase ) {
                EraseRank();

            } else if( choice == optionRaise ) {
                int rankToRaise = TextMenu.ShowNumber( "Which rank to raise?",
                                                       2, RankManager.Ranks.Count );
                if( rankToRaise != -1 ) {
                    RankManager.RaiseRank( RankManager.Ranks[rankToRaise - 1] );
                }

            } else if( choice == optionLower ) {
                int rankToLower = TextMenu.ShowNumber( "Which rank to lower?",
                                                       1, RankManager.Ranks.Count - 1 );
                if( rankToLower != -1 ) {
                    RankManager.LowerRank( RankManager.Ranks[rankToLower - 1] );
                }

            } else if( choice == optionDefaults ) {
                if( TextMenu.ShowYesNo( "Reset all ranks to defaults?" ) ) {
                    RankManager.ResetToDefaults();
                }

            } else {
                currentRank = (Rank)choice.Tag;
                return MenuState.RankDetails;
            }

            return MenuState.Ranks;
        }


        const string RankEraseWarning =
@"To preserve compatibility with older files (player database, world and zone
permissions, etc) you need to specify a substitute rank. This will allow fCraft
to handle references to the deleted ""{0}"" rank.";

        static void EraseRank() {
            int rankIndexToErase = TextMenu.ShowNumber( "Which rank to delete?",
                                                        1, RankManager.Ranks.Count );
            if( rankIndexToErase == -1 ) return;

            Rank rankToErase = RankManager.Ranks[rankIndexToErase - 1];
            Rank subRank;
            if( RankManager.Ranks.Count > 2 ) {
                Console.WriteLine();
                WriteWarning( RankEraseWarning, rankToErase.Name );

                while( true ) {
                    int substitute = TextMenu.ShowNumber( "Substitute rank",
                                                          1, RankManager.Ranks.Count );
                    if( substitute == rankIndexToErase ) {
                        Console.WriteLine( "Cannot substitute rank with itself; pick a different rank." );
                    } else {
                        subRank = RankManager.Ranks[substitute - 1];
                        break;
                    }
                }
            } else {
                subRank = rankToErase.NextRankDown ?? rankToErase.NextRankUp;
            }

            if( TextMenu.ShowYesNo( "Delete rank {0}, and substitute with {1}?",
                                     rankToErase.Name, subRank.Name ) ) {
                RankManager.DeleteRank( rankToErase, subRank );
            }
        }


        static MenuState ShowRankDetails() {
            Refresh( "Rank List > Rank {0} ({1} of {2})",
                     currentRank.Name, currentRank.Index + 1, RankManager.Ranks.Count );

            TextMenu menu = new TextMenu();

            TextOption optionName = menu.AddOption( 1, "Name: \"" + currentRank.Name + "\"" );

            TextOption optionColor = menu.AddOption( 2, "Color: " + Color.GetName( currentRank.Color ) );
            optionColor.ForeColor = Color.ToConsoleColor( currentRank.Color );

            TextOption optionPrefix = menu.AddOption( 3, "Prefix: \"" + currentRank.Prefix + "\"" );

            TextOption optionHasReservedSlot = menu.AddOption( 4, "HasReservedSlot: " + currentRank.HasReservedSlot );

            TextOption optionAllowSecurityCircumvention = menu.AddOption( 5,
                                                                          "AllowSecurityCircumvention: " +
                                                                          currentRank.AllowSecurityCircumvention );

            TextOption optionIdleKickTimer = menu.AddOption( 6, "IdleKickTimer: " + currentRank.IdleKickTimer );

            TextOption optionDrawLimit = menu.AddOption( 7, "DrawLimit: " + currentRank.DrawLimit );
            TextOption optionFillLimit = menu.AddOption( 8, "FillLimit: " + currentRank.FillLimit );
            TextOption optionCopySlots = menu.AddOption( 9, "CopySlots: " + currentRank.CopySlots );
            TextOption optionAntiGriefBlocks = menu.AddOption( 10, "AntiGriefBlocks: " + currentRank.AntiGriefBlocks );
            TextOption optionAntiGriefSeconds = menu.AddOption( 11, "AntiGriefSeconds: " + currentRank.AntiGriefSeconds );

            menu.Column = Column.Right;

            TextOption optionBack = menu.AddOption( "B", "Back to rank list" );

            menu.AddSpacer();
            TextOption optionPermissions = menu.AddOption( "P", "Permissions" );
            TextOption optionPermissionLimits = null;

            if( LimitedPermissions.Any( perm => currentRank.Can( perm ) ) ) {
                optionPermissionLimits = menu.AddOption( "L", "Permission limits" );
            }

            menu.AddSpacer();
            TextOption optionNextUp = null, optionNextDown = null;
            if( currentRank.NextRankUp != null ) {
                optionNextUp = menu.AddOption( "U", "Go to next rank up", currentRank.NextRankUp );
            }
            if( currentRank.NextRankDown != null ) {
                optionNextDown = menu.AddOption( "D", "Go to next rank down", currentRank.NextRankDown );
            }

            TextOption choice = menu.Show();
            if( choice == optionBack ) {
                return MenuState.Ranks;

            } else if( choice == optionPermissions ) {
                return MenuState.Permissions;

            } else if( choice == optionPermissionLimits ) {
                return MenuState.PermissionLimits;

            } else if( choice == optionNextDown || choice == optionNextUp ) {
                currentRank = (Rank)choice.Tag;
            }

            return MenuState.RankDetails;
        }


        static MenuState ShowPermissions() {
            Refresh( "Rank List > Rank {0} > Permissions",
                     currentRank.Name );

            TextMenu menu = new TextMenu();
            Permission[] permissions = (Permission[])Enum.GetValues( typeof( Permission ) );

            TextOption optionBack = menu.AddOption( "B", "Back to rank " + currentRank.Name );
            TextOption optionInvert = menu.AddOption( "I", "Invert" );
            menu.AddSpacer();

            menu.Column = Column.Right;
            TextOption optionAll = menu.AddOption( "A", "All" );
            TextOption optionNone = menu.AddOption( "N", "None" );
            menu.AddSpacer();

            for( int i = 0; i < permissions.Length; i++ ) {
                menu.Column = ( i > permissions.Length / 2 ? Column.Right : Column.Left );
                if( currentRank.Permissions[i] ) {
                    TextOption option = menu.AddOption( i + 1,
                                                        "[X] " + permissions[i],
                                                        permissions[i] );
                    option.ForeColor = ConsoleColor.White;
                } else {
                    menu.AddOption( i + 1,
                                    "[ ] " + permissions[i],
                                    permissions[i] );
                }
            }

            TextOption choice = menu.Show();
            if( choice == optionBack ) {
                return MenuState.RankDetails;

            }else if(choice ==optionAll){
                if( TextMenu.ShowYesNo( "Grant all permissions to rank {0}?",
                                        currentRank.Name ) ) {
                    for( int i = 0; i < permissions.Length; i++ ) {
                        currentRank.Permissions[i] = true;
                    }
                }

            } else if( choice == optionNone ) {
                if( TextMenu.ShowYesNo( "Revoke all permissions from rank {0}?",
                                        currentRank.Name ) ) {
                    for( int i = 0; i < permissions.Length; i++ ) {
                        currentRank.Permissions[i] = false;
                    }
                }

            } else if( choice == optionInvert) {
                for( int i = 0; i < permissions.Length; i++ ) {
                    currentRank.Permissions[i] = !currentRank.Permissions[i];
                }

            } else {
                int permissionIndex = (int)choice.Tag;
                currentRank.Permissions[permissionIndex] = !currentRank.Permissions[permissionIndex];
            }

            return MenuState.Permissions;
        }


        static MenuState ShowPermissionLimits() {
            Refresh( "Rank List > Rank {0} ({1} of {2}) > Permission Limits",
                     currentRank.Name, currentRank.Index + 1, RankManager.Ranks.Count );

            TextMenu menu = new TextMenu();
            int i = 1;

            Permission[] limits = LimitedPermissions.Where( perm => currentRank.Can( perm ) ).ToArray();

            int maxPermLength = limits.Max( perm => perm.ToString().Length );

            foreach( Permission perm in limits ) {
                string text;
                string permName = perm.ToString().PadLeft( maxPermLength, '.' );
                if( currentRank.HasLimitSet( perm ) ) {
                    Rank limit = currentRank.GetLimit( perm );
                    text = String.Format( "{0} - {1}", permName, limit.Name );
                } else {
                    text = String.Format( "{0} - (own rank)", permName );
                }
                menu.AddOption( i, text, perm );
                i++;
            }
            menu.Column = Column.Right;
            TextOption optionBack = menu.AddOption( "B", "Back to rank " + currentRank.Name );
            TextOption optionReset = menu.AddOption( "R", "Reset limits." );

            menu.AddSpacer();
            TextOption optionNextUp = null, optionNextDown = null;
            if( currentRank.NextRankUp != null ) {
                optionNextUp = menu.AddOption( "U", "Go to next rank up", currentRank.NextRankUp );
            }
            if( currentRank.NextRankDown != null ) {
                optionNextDown = menu.AddOption( "D", "Go to next rank down", currentRank.NextRankDown );
            }

            TextOption choice = menu.Show();
            if( choice == optionBack ) {
                return MenuState.RankDetails;

            } else if( choice == optionReset ) {
                if( TextMenu.ShowYesNo( "Reset all permission limits for rank {0} to \"own rank\"?",
                                        currentRank.Name ) ) {
                    foreach( Permission perm in LimitedPermissions ) {
                        currentRank.ResetLimit( perm );
                    }
                }

            } else if( choice == optionNextDown || choice == optionNextUp ) {
                currentRank = (Rank)choice.Tag;

            } else {
                currentPermission = (Permission)choice.Tag;
                return MenuState.PermissionLimitDetails;
            }
            return MenuState.PermissionLimits;
        }


        static MenuState ShowPermissionLimitDetails() {
            Refresh( "Rank List > Rank {0} > {1} Permission Limit",
                     currentRank.Name, currentPermission );
            TextMenu menu = new TextMenu();

            TextOption optionOwnRank = menu.AddOption( "0", "(own rank)" );

            for( int i = 0; i < RankManager.Ranks.Count; i++ ) {
                Rank rank = RankManager.Ranks[i];
                TextOption derp = menu.AddOption( i + 1,
                                                  rank.Name,
                                                  rank );
                derp.ForeColor = Color.ToConsoleColor( rank.Color );
                if( derp.ForeColor == ConsoleColor.Black ) {
                    derp.BackColor = ConsoleColor.Gray;
                }
            }

            menu.AddSpacer();
            TextOption optionCancel = menu.AddOption( "C", "Cancel" );

            TextOption choice = menu.Show();
            if( choice == optionOwnRank ) {
                currentRank.ResetLimit( currentPermission );
            } else if( choice != optionCancel ) {
                currentRank.SetLimit( currentPermission, (Rank)choice.Tag );
            }

            return MenuState.PermissionLimits;
        }


        static void AddRank( Rank newRank ) {
            Console.WriteLine();
            Console.WriteLine( "Where to position the new rank?" );
            TextMenu menu = new TextMenu();
            TextOption optionTop = menu.AddOption( "T", "Top of the hierarchy" );
            TextOption optionBottom = menu.AddOption( "B", "Bottom of the hierarchy" );
            TextOption optionCancel = menu.AddOption( "C", "Cancel" );
            TextOption optionAbove = null, optionUnder = null;
            if( RankManager.Ranks.Count > 1 ) {
                menu.Column = Column.Right;
                optionAbove = menu.AddOption( "O", "Over a specific rank" );
                optionUnder = menu.AddOption( "U", "Under a specific rank" );
            }

            TextOption choice = menu.Show();
            if( choice == optionCancel ) return;

            if( choice == optionTop ) {
                RankManager.AddRank( newRank, 0 );

            } else if( choice == optionBottom ) {
                RankManager.AddRank( newRank );

            } else if( choice == optionAbove ) {
                int otherRankIndex = TextMenu.ShowNumber( "Above which rank?",
                                                          1, RankManager.Ranks.Count );
                if( otherRankIndex == -1 ) return;
                RankManager.AddRank( newRank, otherRankIndex - 1 );

            } else if( choice == optionUnder ) {
                int otherRankIndex = TextMenu.ShowNumber( "Under which rank?",
                                                          1, RankManager.Ranks.Count );
                if( otherRankIndex == -1 ) return;
                RankManager.AddRank( newRank, otherRankIndex );
            }
        }


        static MenuState ShowAddRank() {
            Refresh( "Adding a new rank..." );
            return MenuState.Ranks;
        }


        static readonly Permission[] LimitedPermissions = new[] {
            Permission.Kick,
            Permission.Ban,
            Permission.Promote,
            Permission.Demote,
            Permission.Hide,
            Permission.Freeze,
            Permission.Mute,
            Permission.Bring,
            Permission.Spectate,
            Permission.UndoOthersActions
        };

        #endregion


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


        public static string PadLeftSub( this string text, int leftPad, int length ) {
            string result = text.PadLeft( leftPad );
            if( result.Length > length ) result = result.Substring( 0, length );
            return result;
        }

        public static string PadRightSub( this string text, int length ) {
            string result = text.PadRight( length );
            if( result.Length > length ) result = result.Substring( 0, length );
            return result;
        }

#if !DEBUG
        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "fCraft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            if( UseColor ) Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if( UseColor ) Console.ResetColor();
            if( !Server.HasArg( ArgKey.ExitOnCrash ) ) {
                Console.ReadLine();
            }
        }
#endif
    }


    enum MenuState {
        SectionList,
        KeyList,
        Key,

        Ranks,
        RankAdd,
        RankDetails,
        Permissions,
        PermissionLimits,
        PermissionLimitDetails,

        Done
    }
}