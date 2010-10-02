// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace fCraft {

    /*
     * Config format-version changelog:
     * 100 - r1-r133
     *
     * 101 - r134 - Per-class draw limits and antigrief detection
     *              Removed AntigriefBlockCount and AntigriefBlockInterval keys
     * 
     * 102 - r171 - Added RequireBanReason, RequireClassChangeReason, AnnounceKickAndBanReasons, AnnounceClassChanges keys
     *              Removed AnnounceUnverifiedNames key
     *              
     * 103 - r190 - Added UseSpeedHack permission
     *              Added PrivateMessageColor and IRCColor keys
     * 
     * 104 - r198 - Added IRCBotAnnounceServerJoins and IRCBotAnnounceIRCJoins keys
     *              Removed IRCBotMsg key
     * 
     * 105 - r205 - Added SubmitCrashReports key
     *              Removed PolicyColorCodesInChat, PolicyIllegalCharacters, and RunOnStartup
     *              
     * 106 - r212 - Added IRCDelay key
     * 
     * 107 - r214 - Added ShowJoinedWorldMessages and ClassColorsInWorldNames keys
     *              Removed ChangeName permission
     *              
     * 108 - r224 - Added IP config key
     *              Capped MaxPlayers at 128
     *              
     * 109 - r226 - Added PatrolledClass config key
     *              Added Patrol permission
     *              
     * 110 - r227 - Added ShutdownServer and Mute permissions
     * 
     * 111 - r231 - Renamed config keys:
     *                  DefaultClass             -> DefaultRank
     *                  ClassColorsInChat        -> RankColorsInChat
     *                  ClassColorsInWorldNames  -> RankColorsInWorldNames
     *                  ClassPrefixesInChat      -> RankPrefixesInChat
     *                  ClassPrefixesInList      -> RankPrefixesInList
     *                  PatrolledClass           -> PatrolledRank
     *                  RequireClassChangeReason -> RequireRankChangeReason
     *                  AnnounceClassChanges     -> AnnounceRankChanges
     *              Renamed XML elements:
     *                  Classes     -> Ranks
     *                  PlayerClass -> Rank
     *              Removed "rank" from PlayerClass/Rank object
     *              Made the order of Rank elements determine the relative index
     * 
     */

    public static class Config {
        public const int ProtocolVersion = 7;
        public const int ConfigVersion = 110;
        public const int MaxPlayersSupported = 128;
        public const string ConfigRootName = "fCraftConfig",
                            ConfigFile = "config.xml";
        static Dictionary<ConfigKey, string> settings = new Dictionary<ConfigKey, string>();
        static Dictionary<string, ConfigKey> legacyConfigKeys = new Dictionary<string, ConfigKey>(); // LEGACY


        static Config() { // LEGACY
            legacyConfigKeys.Add( "DefaultClass".ToLower(), ConfigKey.DefaultRank );
            legacyConfigKeys.Add( "ClassColorsInChat".ToLower(), ConfigKey.RankColorsInChat );
            legacyConfigKeys.Add( "ClassColorsInWorldNames".ToLower(), ConfigKey.RankColorsInWorldNames );
            legacyConfigKeys.Add( "ClassPrefixesInChat".ToLower(), ConfigKey.RankPrefixesInChat );
            legacyConfigKeys.Add( "ClassPrefixesInList".ToLower(), ConfigKey.RankPrefixesInList );
            legacyConfigKeys.Add( "PatrolledClass".ToLower(), ConfigKey.PatrolledRank );
            legacyConfigKeys.Add( "RequireClassChangeReason".ToLower(), ConfigKey.RequireRankChangeReason );
            legacyConfigKeys.Add( "AnnounceClassChanges".ToLower(), ConfigKey.AnnounceRankChanges );
        }


        #region Defaults

        public static void LoadDefaults() {
            settings.Clear();
            LoadDefaultsGeneral();
            LoadDefaultsSecurity();
            LoadDefaultsSavingAndBackup();
            LoadDefaultsLogging();
            LoadDefaultsIRC();
            LoadDefaultsAdvanced();
        }

        public static void LoadDefaultsGeneral() {
            SetValue( ConfigKey.ServerName, "Minecraft custom server (fCraft)" );
            SetValue( ConfigKey.MOTD, "Welcome to the server!" );
            SetValue( ConfigKey.MaxPlayers, 20 );
            SetValue( ConfigKey.DefaultRank, "" ); // empty = lowest rank
            SetValue( ConfigKey.IsPublic, false );
            SetValue( ConfigKey.Port, 25565 );
            SetValue( ConfigKey.IP, IPAddress.Any );
            SetValue( ConfigKey.UploadBandwidth, 100 );

            SetValue( ConfigKey.ShowJoinedWorldMessages, true );
            SetValue( ConfigKey.RankColorsInWorldNames, true );
            SetValue( ConfigKey.RankColorsInChat, true );
            SetValue( ConfigKey.RankPrefixesInChat, false );
            SetValue( ConfigKey.RankPrefixesInList, false );
            SetValue( ConfigKey.SystemMessageColor, Color.GetName( Color.Yellow ) );
            SetValue( ConfigKey.HelpColor, Color.GetName( Color.Lime ) );
            SetValue( ConfigKey.SayColor, Color.GetName( Color.Green ) );
            SetValue( ConfigKey.AnnouncementColor, Color.GetName( Color.Green ) );
            SetValue( ConfigKey.PrivateMessageColor, Color.GetName( Color.Aqua ) );
            SetValue( ConfigKey.AnnouncementInterval, 5 );
        }

        public static void LoadDefaultsSecurity() {
            SetValue( ConfigKey.VerifyNames, "Balanced" ); // can be "Always," "Balanced," or "Never"
            SetValue( ConfigKey.LimitOneConnectionPerIP, false );

            SetValue( ConfigKey.PatrolledRank, "" ); // empty = lowest rank

            SetValue( ConfigKey.AntispamMessageCount, 4 );
            SetValue( ConfigKey.AntispamInterval, 5 );
            SetValue( ConfigKey.AntispamMuteDuration, 5 );
            SetValue( ConfigKey.AntispamMaxWarnings, 2 );

            SetValue( ConfigKey.RequireBanReason, false );
            SetValue( ConfigKey.RequireRankChangeReason, false );
            SetValue( ConfigKey.AnnounceKickAndBanReasons, true );
            SetValue( ConfigKey.AnnounceRankChanges, true );
        }

        public static void LoadDefaultsSavingAndBackup() {
            SetValue( ConfigKey.SaveOnShutdown, true );
            SetValue( ConfigKey.SaveInterval, 60 );   // 0 = no auto save
            SetValue( ConfigKey.BackupOnStartup, false );
            SetValue( ConfigKey.BackupOnJoin, false );
            SetValue( ConfigKey.BackupOnlyWhenChanged, true );
            SetValue( ConfigKey.BackupInterval, 20 );   // 0 = no auto backup
            SetValue( ConfigKey.MaxBackups, 100 );  // 0 = no backup file count limit
            SetValue( ConfigKey.MaxBackupSize, 0 );    // 0 = no backup file size count limit
        }

        public static void LoadDefaultsLogging() {
            SetValue( ConfigKey.LogMode, LogSplittingType.OneFile ); // can be: "OneFile", "SplitBySession", "SplitByDay"
            SetValue( ConfigKey.MaxLogs, 0 );
            for( int i = 0; i < Logger.consoleOptions.Length; i++ ) {
                Logger.consoleOptions[i] = true;
            }
            Logger.consoleOptions[(int)LogType.ConsoleInput] = false;
            //Logger.consoleOptions[(int)LogType.Debug] = false;
            for( int i = 0; i < Logger.logFileOptions.Length; i++ ) {
                Logger.logFileOptions[i] = true;
            }
        }

        public static void LoadDefaultsIRC() {
            SetValue( ConfigKey.IRCBot, false ); // Bot is disabled by default
            SetValue( ConfigKey.IRCBotNick, "fBot" );
            SetValue( ConfigKey.IRCBotQuitMsg, "I've been told to go offline now." );
            SetValue( ConfigKey.IRCBotNetwork, "irc.esper.net" );
            SetValue( ConfigKey.IRCBotPort, 6667 );
            SetValue( ConfigKey.IRCBotChannels, "#changeme" ); // CASE SENSITIVE!!!!!!!!!!!!!!!!!!!!! This can be multiple using csv
            SetValue( ConfigKey.IRCBotAnnounceIRCJoins, false );
            SetValue( ConfigKey.IRCBotAnnounceServerJoins, false );
            SetValue( ConfigKey.IRCBotForwardFromIRC, false ); // Disabled by default
            SetValue( ConfigKey.IRCBotForwardFromServer, false ); // Disabled by default
            SetValue( ConfigKey.IRCMessageColor, Color.Purple );
            SetValue( ConfigKey.IRCDelay, 750 );
        }

        public static void LoadDefaultsAdvanced() {
            SetValue( ConfigKey.SendRedundantBlockUpdates, false );
            SetValue( ConfigKey.PingInterval, 0 ); // 0 = ping disabled
            SetValue( ConfigKey.AutomaticUpdates, "Prompt" ); // can be "Disabled", "Notify", "Prompt", and "Auto"
            SetValue( ConfigKey.NoPartialPositionUpdates, false );
            SetValue( ConfigKey.ProcessPriority, "" );
            SetValue( ConfigKey.BlockUpdateThrottling, 2048 );
            SetValue( ConfigKey.TickInterval, 100 );
            SetValue( ConfigKey.LowLatencyMode, false );
            SetValue( ConfigKey.SubmitCrashReports, true );
        }

        #endregion


        #region Loading

        public static bool Load( bool skipClassList ) {
            // generate random salt
            LoadDefaults();
            bool fromFile = false;

            // try to load config file (XML)
            XDocument file;
            if( File.Exists( ConfigFile ) ) {
                try {
                    file = XDocument.Load( ConfigFile );
                    if( file.Root == null || file.Root.Name != ConfigRootName ) {
                        Log( "Config.Load: Malformed or incompatible config file {0}. Loading defaults.", LogType.Warning, ConfigFile );
                        file = new XDocument();
                        file.Add( new XElement( ConfigRootName ) );
                    } else {
                        Log( "Config.Load: Config file {0} loaded succesfully.", LogType.Debug, ConfigFile );
                        fromFile = true;
                    }
                } catch( Exception ex ) {
                    Log( "Config.Load: Fatal error while loading config file {0}: {1}", LogType.FatalError,
                                        ConfigFile, ex.Message );
                    return false;
                }
            } else {
                // create a new one (with defaults) if no file exists
                file = new XDocument();
                file.Add( new XElement( ConfigRootName ) );
            }

            XElement config = file.Root;

            XAttribute attr = config.Attribute( "version" );
            int version = 0;
            if( fromFile && (attr == null || !Int32.TryParse( attr.Value, out version ) || version != ConfigVersion) ) {
                Log( "Config.Load: Your config.xml was made for a different version of fCraft. " +
                     "Some obsolete settings might be ignored, and some recently-added settings will be set to their default values. " +
                     "It is recommended that you run ConfigTool to make sure everything is in order.", LogType.Warning );
            }

            if( !skipClassList ) {
                LoadClassList( config, version, fromFile );
            }

            XElement consoleOptions = config.Element( "ConsoleOptions" );
            if( consoleOptions != null ) {
                LoadLogOptions( consoleOptions, Logger.consoleOptions );
            } else {
                if( fromFile ) Log( "Config.Load: using default console options.", LogType.Warning );
                for( int i = 0; i < Logger.consoleOptions.Length; i++ ) {
                    Logger.consoleOptions[i] = true;
                }
                Logger.consoleOptions[(int)LogType.ConsoleInput] = false;
                Logger.consoleOptions[(int)LogType.Debug] = false;
            }

            XElement logFileOptions = config.Element( "LogFileOptions" );
            if( logFileOptions != null ) {
                LoadLogOptions( logFileOptions, Logger.logFileOptions );
            } else {
                if( fromFile ) Log( "Config.Load: using default log file options.", LogType.Warning );
                for( int i = 0; i < Logger.logFileOptions.Length; i++ ) {
                    Logger.logFileOptions[i] = true;
                }
            }

            // Load config
            string[] keyNames = Enum.GetNames( typeof( ConfigKey ) );
            foreach( XElement element in config.Elements() ) {
                if( keyNames.Contains<string>( element.Name.ToString() ) ) {
                    // known key
                    SetValue( (ConfigKey)Enum.Parse( typeof( ConfigKey ), element.Name.ToString(), true ), element.Value );

                } else if( legacyConfigKeys.ContainsKey( element.Name.ToString().ToLower() ) ) { // LEGACY
                    SetValue( legacyConfigKeys[element.Name.ToString().ToLower()], element.Value );

                } else if( element.Name.ToString() != "ConsoleOptions" &&
                           element.Name.ToString() != "LogFileOptions" &&
                           element.Name.ToString() != "Classes" && // LEGACY
                           element.Name.ToString() != "Ranks" &&
                           element.Name.ToString() != "LegacyRankMapping" ) {
                    // unknown key
                    Log( "Unrecognized entry ignored: {0} = {1}", LogType.Debug, element.Name, element.Value );
                }
            }
            return true;
        }

        static void LoadLogOptions( XElement el, bool[] list ) {
            for( int i = 0; i < 13; i++ ) {
                if( el.Element( ((LogType)i).ToString() ) != null ) {
                    list[i] = true;
                } else {
                    list[i] = false;
                }
            }
        }

        static void LoadClassList( XElement config, int version, bool fromFile ) {

            XElement legacyRankMappingTag = config.Element( "LegacyRankMapping" );
            if( legacyRankMappingTag != null ) {
                foreach( XElement rankPair in legacyRankMappingTag.Elements( "LegacyRankPair" ) ) {
                    XAttribute fromClassID = rankPair.Attribute( "from" );
                    XAttribute toClassID = rankPair.Attribute( "to" );
                    if( fromClassID == null || fromClassID.Value == null || fromClassID.Value == "" ||
                        toClassID == null || toClassID.Value == null || toClassID.Value == "" ) {
                        Log( "Config.Load: Could not parse a LegacyRankMapping entry: {0}", LogType.Error, rankPair.ToString() );
                    } else {
                        RankList.LegacyRankMapping.Add( fromClassID.Value, toClassID.Value );
                    }
                }
            }


            XElement rankList = config.Element( "Ranks" );
            if( rankList == null ) rankList = config.Element( "Classes" ); // LEGACY

            if( rankList != null ) {
                XElement[] rankDefinitionList = rankList.Elements( "Rank" ).ToArray();
                if( rankDefinitionList.Length == 0 ) rankDefinitionList = rankList.Elements( "PlayerClass" ).ToArray(); // LEGACY

                foreach( XElement rankDefinition in rankDefinitionList ) {
                    try {
                        RankList.AddRank( new Rank( rankDefinition ) );
                    } catch( Rank.RankDefinitionException ex ) {
                        Log( ex.Message, LogType.Error );
                    }
                }

                if( RankList.RanksByName.Count == 0 ) {
                    Log( "Config.Load: No ranks were defined, or none were defined correctly. Using default ranks (guest, regular, op, and owner).", LogType.Warning );
                    rankList.Remove();
                    config.Add( DefineDefaultRanks() );

                } else if( version < ConfigVersion ) { // start LEGACY code

                    if( version < 103 ) { // speedhack permission
                        bool foundClassWithSpeedHackPermission = false;
                        foreach( Rank pc in RankList.RanksByID.Values ) {
                            if( pc.Can( Permission.UseSpeedHack ) ) {
                                foundClassWithSpeedHackPermission = true;
                                break;
                            }
                        }
                        if( !foundClassWithSpeedHackPermission ) {
                            foreach( Rank pc in RankList.RanksByID.Values ) {
                                pc.Permissions[(int)Permission.UseSpeedHack] = true;
                            }
                            Log( "Config.Load: All ranks were granted UseSpeedHack permission (default). " +
                                 "Use ConfigTool to update config. If you are editing config.xml manually, " +
                                 "set version=\"{0}\" to prevent permissions from resetting in the future.", LogType.Warning, ConfigVersion );
                        }
                    }

                    if( version < 111 ) {
                        RankList.Ranks.OrderBy( rank => rank.legacyNumericRank );
                        RankList.RebuildIndex();
                    }

                } // end LEGACY code

            } else {
                if( fromFile ) Log( "Config.Load: using default player ranks.", LogType.Warning );
                config.Add( DefineDefaultRanks() );
            }

            // parse rank-limit permissions
            RankList.ParsePermissionLimits();
        }

        #endregion


        #region Saving
        public static bool Save() {
            XDocument file = new XDocument();

            XElement config = new XElement( ConfigRootName );
            config.Add( new XAttribute( "version", ConfigVersion ) );


            foreach( KeyValuePair<ConfigKey, string> pair in settings ) {
                config.Add( new XElement( pair.Key.ToString(), pair.Value ) );
            }


            XElement consoleOptions = new XElement( "ConsoleOptions" );
            for( int i = 0; i < Logger.consoleOptions.Length; i++ ) {
                if( Logger.consoleOptions[i] ) {
                    consoleOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( consoleOptions );


            XElement logFileOptions = new XElement( "LogFileOptions" );
            for( int i = 0; i < Logger.logFileOptions.Length; i++ ) {
                if( Logger.logFileOptions[i] ) {
                    logFileOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( logFileOptions );


            XElement ranksTag = new XElement( "Ranks" );
            foreach( Rank rank in RankList.RanksByName.Values ) {
                ranksTag.Add( rank.Serialize() );
            }
            config.Add( ranksTag );


            XElement legacyRankMappingTag = new XElement( "LegacyRankMapping" );
            foreach( KeyValuePair<string, string> pair in RankList.LegacyRankMapping ) {
                XElement rankPair = new XElement( "LegacyRankPair" );
                rankPair.Add( new XAttribute( "from", pair.Key ), new XAttribute( "to", pair.Value ) );
                legacyRankMappingTag.Add( rankPair );
            }
            config.Add( legacyRankMappingTag );


            file.Add( config );
            // save the settings
            try {
                file.Save( ConfigFile );
                return true;
            } catch( Exception ex ) {
                Log( "Config.Load: Fatal error while saving config file {0}: {1}", LogType.FatalError, ConfigFile, ex.Message );
                return false;
            }
        }
        #endregion


        #region Getters

        public static string GetString( ConfigKey key ) {
            return settings[key];
        }

        public static int GetInt( ConfigKey key ) {
            return Int32.Parse( settings[key] );
        }

        public static bool GetBool( ConfigKey key ) {
            return Boolean.Parse( settings[key] );
        }

        #endregion


        #region Setters

        public static bool SetValue( ConfigKey key, object _value ) {
            string value = _value.ToString();
            switch( key ) {
                case ConfigKey.ServerName:
                    return ValidateString( key, value, 1, 64 );
                case ConfigKey.MOTD:
                    return ValidateString( key, value, 0, 64 );
                case ConfigKey.MaxPlayers:
                    return ValidateInt( key, value, 1, MaxPlayersSupported );
                case ConfigKey.DefaultRank:
                    if( value.Length > 0 ) {
                        if( RankList.ParseRank( value ) != null ) {
                            settings[key] = RankList.ParseRank( value ).Name;
                            return true;
                        } else {
                            Log( "Config.SetValue: DefaultRank could not be parsed. " +
                                 "It should be either blank (indicating \"use lowest rank\") or set to a valid class name. " +
                                 "DefaultRank was reset to default (lowest rank).", LogType.Warning );
                            return false;
                        }
                    } else {
                        settings[key] = "";
                        return true;
                    }
                case ConfigKey.Port:
                case ConfigKey.IRCBotPort:
                    return ValidateInt( key, value, 1, 65535 );
                case ConfigKey.UploadBandwidth:
                    return ValidateInt( key, value, 1, 10000 );
                case ConfigKey.IP:
                    IPAddress tempIP;
                    if( IPAddress.TryParse( value, out tempIP ) && tempIP.ToString() != IPAddress.Broadcast.ToString() ) {
                        settings[key] = value;
                        return true;
                    } else {
                        return false;
                    }

                case ConfigKey.IRCBotNick:
                    return ValidateString( key, value, 1, 32 );
                //case "IRCBotNetwork":
                //case "IRCBotChannels": // don't bother validating network and channel list
                case ConfigKey.IRCDelay:
                    return ValidateInt( key, value, 100, 1000 );
                case ConfigKey.AnnouncementInterval:
                    return ValidateInt( key, value, 1, 60 );

                case ConfigKey.IsPublic:
                case ConfigKey.RankColorsInChat:
                case ConfigKey.RankPrefixesInChat:
                case ConfigKey.RankPrefixesInList:
                case ConfigKey.RankColorsInWorldNames:
                case ConfigKey.ShowJoinedWorldMessages:
                case ConfigKey.SaveOnShutdown:
                case ConfigKey.BackupOnStartup:
                case ConfigKey.BackupOnJoin:
                case ConfigKey.BackupOnlyWhenChanged:
                case ConfigKey.SendRedundantBlockUpdates:
                case ConfigKey.NoPartialPositionUpdates:
                case ConfigKey.IRCBot:
                case ConfigKey.IRCBotForwardFromIRC:
                case ConfigKey.IRCBotForwardFromServer:
                case ConfigKey.IRCBotAnnounceIRCJoins:
                case ConfigKey.IRCBotAnnounceServerJoins:
                case ConfigKey.RequireBanReason:
                case ConfigKey.RequireRankChangeReason:
                case ConfigKey.AnnounceKickAndBanReasons:
                case ConfigKey.AnnounceRankChanges:
                case ConfigKey.SubmitCrashReports:
                    return ValidateBool( key, value );

                case ConfigKey.SystemMessageColor:
                case ConfigKey.HelpColor:
                case ConfigKey.SayColor:
                case ConfigKey.AnnouncementColor:
                case ConfigKey.PrivateMessageColor:
                case ConfigKey.IRCMessageColor:
                    return ValidateColor( key, value );

                case ConfigKey.VerifyNames:
                    return ValidateEnum( key, value, "Always", "Balanced", "Never" );
                case ConfigKey.AntispamMessageCount:
                    return ValidateInt( key, value, 2, 50 );
                case ConfigKey.AntispamInterval:
                    return ValidateInt( key, value, 0, 60 );
                case ConfigKey.AntispamMuteDuration:
                    return ValidateInt( key, value, 0, 3600 );
                case ConfigKey.AntispamMaxWarnings:
                    return ValidateInt( key, value, 0, 50 );


                case ConfigKey.SaveInterval:
                    return ValidateInt( key, value, 1, 100000 );
                case ConfigKey.BackupInterval:
                    return ValidateInt( key, value, 0, 100000 );
                case ConfigKey.MaxBackups:
                    return ValidateInt( key, value, 0, 100000 );
                case ConfigKey.MaxBackupSize:
                    return ValidateInt( key, value, 0, 1000000 );

                case ConfigKey.LogMode:
                    return ValidateEnum( key, value, "OneFile", "SplitBySession", "SplitByDay" );
                case ConfigKey.MaxLogs:
                    return ValidateInt( key, value, 0, 100000 );

                case ConfigKey.ProcessPriority:
                    return ValidateEnum( key, value, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
                case ConfigKey.AutomaticUpdates:
                    return ValidateEnum( key, value, "Disabled", "Notify", "Prompt", "Auto" );
                case ConfigKey.BlockUpdateThrottling:
                    return ValidateInt( key, value, 10, 100000 );
                case ConfigKey.TickInterval:
                    return ValidateInt( key, value, 20, 1000 );
                default:
                    settings[key] = value;
                    return true;
            }
        }

        static bool ValidateInt( ConfigKey key, string value, int minRange, int maxRange ) {
            int temp;
            if( Int32.TryParse( value, out temp ) ) {
                if( temp >= minRange && temp <= maxRange ) {
                    settings[key] = temp.ToString();
                } else {
                    Log( "Config.ValidateInt: Specified value for {0} is not within valid range ({1}...{2}). Using default ({3}).", LogType.Warning,
                         key, minRange, maxRange, settings[key] );
                }
                return true;

            } else {
                Log( "Config.ValidateInt: Specified value for {0} could not be parsed. Using default ({1}).", LogType.Warning,
                     key, settings[key] );
                return false;
            }
        }

        static bool ValidateBool( ConfigKey key, string value ) {
            bool temp;
            if( Boolean.TryParse( value, out temp ) ) {
                settings[key] = temp.ToString();
                return true;

            } else {
                Log( "Config.ValidateBool: Specified value for {0} could not be parsed. Expected 'true' or 'false'. Using default ({1}).", LogType.Warning,
                     key, settings[key] );
                return false;
            }
        }

        static bool ValidateColor( ConfigKey key, string value ) {
            if( Color.Parse( value ) != null ) {
                settings[key] = value;
                return true;

            } else {
                Log( "Config.ValidateColor: Specified value for {0} could not be parsed. Using default ({1}).", LogType.Warning,
                     key, settings[key] );
                return false;
            }
        }

        static bool ValidateString( ConfigKey key, string value, int minLength, int maxLength ) {
            if( value.Length < minLength ) {
                Log( "Config.ValidateString: Specified value for {0} is too short (expected length: {1}...{2}). Using default ({3}).", LogType.Warning,
                     key, minLength, maxLength, settings[key] );
                return false;

            } else if( value.Length > maxLength ) {
                settings[key] = value.Substring( 0, maxLength );
                Log( "Config.ValidateString: Specified value for {0} is too long (expected length: {1}...{2}). The value has been truncated to \"{3}\".", LogType.Warning,
                     key, minLength, maxLength, settings[key] );
                return true;

            } else {
                settings[key] = value;
                return true;
            }
        }

        static bool ValidateEnum( ConfigKey key, string value, params string[] options ) {
            for( int i = 0; i < options.Length; i++ ) {
                if( value.ToLower() == options[i].ToLower() ) {
                    settings[key] = options[i];
                    return true;
                }
            }
            Log( "Config.SetValue: Invalid option specified for {0}. " +
                    "See documentation for the list of permitted options. Using default: {1}", LogType.Warning,
                    key, settings[key] );
            return false;
        }

        #endregion


        internal static void ApplyConfig() {
            Logger.split = (LogSplittingType)Enum.Parse( typeof( LogSplittingType ), settings[ConfigKey.LogMode] );
            Logger.MarkLogStart();

            // chat colors
            Color.Sys = Color.Parse( settings[ConfigKey.SystemMessageColor] );
            Color.Say = Color.Parse( settings[ConfigKey.SayColor] );
            Color.Help = Color.Parse( settings[ConfigKey.HelpColor] );
            Color.Announcement = Color.Parse( settings[ConfigKey.AnnouncementColor] );
            Color.PM = Color.Parse( settings[ConfigKey.PrivateMessageColor] );
            Color.IRC = Color.Parse( settings[ConfigKey.IRCMessageColor] );

            // default class
            if( settings[ConfigKey.DefaultRank] != "" ) {
                if( RankList.ParseRank( settings[ConfigKey.DefaultRank] ) != null ) {
                    RankList.DefaultRank = RankList.ParseRank( settings[ConfigKey.DefaultRank] );
                } else {
                    RankList.DefaultRank = RankList.LowestRank;
                    Log( "Config.ApplyConfig: Could not parse DefaultRank; assuming that the lowest rank ({0}) is the default.",
                         LogType.Warning, RankList.DefaultRank.Name );
                }
            } else {
                RankList.DefaultRank = RankList.LowestRank;
            }

            // antispam
            Player.spamChatCount = GetInt( ConfigKey.AntispamMessageCount );
            Player.spamChatTimer = GetInt( ConfigKey.AntispamInterval );
            Player.muteDuration = TimeSpan.FromSeconds( GetInt( ConfigKey.AntispamMuteDuration ) );

            // scheduler settings
            Server.maxUploadSpeed = GetInt( ConfigKey.UploadBandwidth );
            Server.packetsPerSecond = GetInt( ConfigKey.BlockUpdateThrottling );
            Server.ticksPerSecond = 1000 / (float)GetInt( ConfigKey.TickInterval );

            // class to patrol
            if( RankList.ParseRank( settings[ConfigKey.PatrolledRank] ) != null ) {
                World.classToPatrol = RankList.ParseRank( settings[ConfigKey.PatrolledRank] );
            } else {
                World.classToPatrol = RankList.LowestRank;
            }

            // IRC delay
            IRC.SendDelay = GetInt( ConfigKey.IRCDelay );
        }


        public static void ResetRanks() {
            RankList.RanksByName = new Dictionary<string, Rank>();
            RankList.Ranks = new List<Rank>();
            DefineDefaultRanks();
            // parse rank-limit permissions
            RankList.ParsePermissionLimits();
        }


        static XElement DefineDefaultRanks() {
            XElement temp;
            XElement permissions = new XElement( "Ranks" );


            XElement owner = new XElement( "Rank" );
            owner.Add( new XAttribute( "id", RankList.GenerateID() ) );
            owner.Add( new XAttribute( "name", "owner" ) );
            owner.Add( new XAttribute( "rank", 100 ) );
            owner.Add( new XAttribute( "color", "red" ) );
            owner.Add( new XAttribute( "prefix", "+" ) );
            owner.Add( new XAttribute( "drawLimit", 0 ) );
            owner.Add( new XAttribute( "antiGriefBlocks", 0 ) );
            owner.Add( new XAttribute( "antiGriefSeconds", 0 ) );
            owner.Add( new XAttribute( "idleKickAfter", 0 ) );

            owner.Add( new XElement( Permission.Chat.ToString() ) );
            owner.Add( new XElement( Permission.Build.ToString() ) );
            owner.Add( new XElement( Permission.Delete.ToString() ) );
            owner.Add( new XElement( Permission.UseSpeedHack.ToString() ) );

            owner.Add( new XElement( Permission.PlaceGrass.ToString() ) );
            owner.Add( new XElement( Permission.PlaceWater.ToString() ) );
            owner.Add( new XElement( Permission.PlaceLava.ToString() ) );
            owner.Add( new XElement( Permission.PlaceAdmincrete.ToString() ) );
            owner.Add( new XElement( Permission.DeleteAdmincrete.ToString() ) );

            owner.Add( new XElement( Permission.Say.ToString() ) );
            temp = new XElement( Permission.Kick.ToString() );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            temp = new XElement( Permission.Ban.ToString() );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            owner.Add( new XElement( Permission.BanIP.ToString() ) );
            owner.Add( new XElement( Permission.BanAll.ToString() ) );

            temp = new XElement( Permission.Promote.ToString() );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            temp = new XElement( Permission.Demote.ToString() );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            owner.Add( new XElement( Permission.Hide.ToString() ) );

            owner.Add( new XElement( Permission.ViewOthersInfo.ToString() ) );

            owner.Add( new XElement( Permission.Teleport.ToString() ) );
            owner.Add( new XElement( Permission.Bring.ToString() ) );
            owner.Add( new XElement( Permission.Patrol.ToString() ) );
            owner.Add( new XElement( Permission.Freeze.ToString() ) );
            owner.Add( new XElement( Permission.Mute.ToString() ) );
            owner.Add( new XElement( Permission.SetSpawn.ToString() ) );

            owner.Add( new XElement( Permission.Lock.ToString() ) );

            owner.Add( new XElement( Permission.ManageZones.ToString() ) );
            owner.Add( new XElement( Permission.ManageWorlds.ToString() ) );
            owner.Add( new XElement( Permission.Import.ToString() ) );
            owner.Add( new XElement( Permission.Draw.ToString() ) );
            owner.Add( new XElement( Permission.CopyAndPaste.ToString() ) );

            owner.Add( new XElement( Permission.ReloadConfig.ToString() ) );
            owner.Add( new XElement( Permission.ShutdownServer.ToString() ) );
            permissions.Add( owner );
            try {
                RankList.AddRank( new Rank( owner ) );
            } catch( Rank.RankDefinitionException ex ) {
                Log( ex.Message, LogType.Error );
            }


            XElement op = new XElement( "Rank" );
            op.Add( new XAttribute( "id", RankList.GenerateID() ) );
            op.Add( new XAttribute( "name", "op" ) );
            op.Add( new XAttribute( "rank", 80 ) );
            op.Add( new XAttribute( "color", "aqua" ) );
            op.Add( new XAttribute( "prefix", "-" ) );
            op.Add( new XAttribute( "drawLimit", 0 ) );
            op.Add( new XAttribute( "antiGriefBlocks", 0 ) );
            op.Add( new XAttribute( "antiGriefSeconds", 0 ) );
            op.Add( new XAttribute( "idleKickAfter", 0 ) );

            op.Add( new XElement( Permission.Chat.ToString() ) );
            op.Add( new XElement( Permission.Build.ToString() ) );
            op.Add( new XElement( Permission.Delete.ToString() ) );
            op.Add( new XElement( Permission.UseSpeedHack.ToString() ) );

            op.Add( new XElement( Permission.PlaceGrass.ToString() ) );
            op.Add( new XElement( Permission.PlaceWater.ToString() ) );
            op.Add( new XElement( Permission.PlaceLava.ToString() ) );
            op.Add( new XElement( Permission.PlaceAdmincrete.ToString() ) );
            op.Add( new XElement( Permission.DeleteAdmincrete.ToString() ) );

            op.Add( new XElement( Permission.Say.ToString() ) );
            temp = new XElement( Permission.Kick.ToString() );
            temp.Add( new XAttribute( "max", "op" ) );
            op.Add( temp );
            temp = new XElement( Permission.Ban.ToString() );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            op.Add( new XElement( Permission.BanIP.ToString() ) );

            temp = new XElement( Permission.Promote.ToString() );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            temp = new XElement( Permission.Demote.ToString() );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            op.Add( new XElement( Permission.Hide.ToString() ) );

            op.Add( new XElement( Permission.ViewOthersInfo.ToString() ) );

            op.Add( new XElement( Permission.Teleport.ToString() ) );
            op.Add( new XElement( Permission.Bring.ToString() ) );
            op.Add( new XElement( Permission.Patrol.ToString() ) );
            op.Add( new XElement( Permission.Freeze.ToString() ) );
            op.Add( new XElement( Permission.Mute.ToString() ) );
            op.Add( new XElement( Permission.SetSpawn.ToString() ) );

            op.Add( new XElement( Permission.ManageZones.ToString() ) );
            op.Add( new XElement( Permission.Lock.ToString() ) );
            op.Add( new XElement( Permission.Draw.ToString() ) );
            op.Add( new XElement( Permission.CopyAndPaste.ToString() ) );
            permissions.Add( op );
            try {
                RankList.AddRank( new Rank( op ) );
            } catch( Rank.RankDefinitionException ex ) {
                Log( ex.Message, LogType.Error );
            }


            XElement regular = new XElement( "Rank" );
            regular.Add( new XAttribute( "id", RankList.GenerateID() ) );
            regular.Add( new XAttribute( "name", "regular" ) );
            regular.Add( new XAttribute( "rank", 30 ) );
            regular.Add( new XAttribute( "color", "white" ) );
            regular.Add( new XAttribute( "prefix", "" ) );
            regular.Add( new XAttribute( "drawLimit", 4096 ) );
            regular.Add( new XAttribute( "antiGriefBlocks", 47 ) );
            regular.Add( new XAttribute( "antiGriefSeconds", 6 ) );
            regular.Add( new XAttribute( "idleKickAfter", 20 ) );

            regular.Add( new XElement( Permission.Chat.ToString() ) );
            regular.Add( new XElement( Permission.Build.ToString() ) );
            regular.Add( new XElement( Permission.Delete.ToString() ) );
            regular.Add( new XElement( Permission.UseSpeedHack.ToString() ) );

            regular.Add( new XElement( Permission.PlaceGrass.ToString() ) );
            regular.Add( new XElement( Permission.PlaceWater.ToString() ) );
            regular.Add( new XElement( Permission.PlaceLava.ToString() ) );
            regular.Add( new XElement( Permission.PlaceAdmincrete.ToString() ) );
            regular.Add( new XElement( Permission.DeleteAdmincrete.ToString() ) );

            temp = new XElement( Permission.Kick.ToString() );
            temp.Add( new XAttribute( "max", "regular" ) );
            regular.Add( temp );

            regular.Add( new XElement( Permission.ViewOthersInfo.ToString() ) );

            regular.Add( new XElement( Permission.Teleport.ToString() ) );

            regular.Add( new XElement( Permission.Draw.ToString() ) );
            permissions.Add( regular );
            try {
                RankList.AddRank( new Rank( regular ) );
            } catch( Rank.RankDefinitionException ex ) {
                Log( ex.Message, LogType.Error );
            }


            XElement guest = new XElement( "Rank" );
            guest.Add( new XAttribute( "id", RankList.GenerateID() ) );
            guest.Add( new XAttribute( "name", "guest" ) );
            guest.Add( new XAttribute( "rank", 0 ) );
            guest.Add( new XAttribute( "color", "silver" ) );
            guest.Add( new XAttribute( "prefix", "" ) );
            guest.Add( new XAttribute( "drawLimit", 512 ) );
            guest.Add( new XAttribute( "antiGriefBlocks", 37 ) );
            guest.Add( new XAttribute( "antiGriefSeconds", 5 ) );
            guest.Add( new XAttribute( "idleKickAfter", 20 ) );
            guest.Add( new XElement( Permission.Chat.ToString() ) );
            guest.Add( new XElement( Permission.Build.ToString() ) );
            guest.Add( new XElement( Permission.Delete.ToString() ) );
            guest.Add( new XElement( Permission.UseSpeedHack.ToString() ) );
            permissions.Add( guest );
            try {
                RankList.AddRank( new Rank( guest ) );
            } catch( Rank.RankDefinitionException ex ) {
                Log( ex.Message, LogType.Error );
            }

            return permissions;
        }


        public static ProcessPriorityClass GetProcessPriority() {
            switch( GetString( ConfigKey.ProcessPriority ) ) {
                case "High": return ProcessPriorityClass.High;
                case "AboveNormal": return ProcessPriorityClass.AboveNormal;
                case "BelowNormal": return ProcessPriorityClass.BelowNormal;
                case "Low": return ProcessPriorityClass.Idle;
                default: return ProcessPriorityClass.Normal;
            }
        }


        #region Logging

        public static string errors = ""; // for ConfigTool
        public static bool logToString;

        static void Log( string format, LogType type, params object[] args ) {
            Log( String.Format( format, args ), type );
        }

        static void Log( string message, LogType type ) {
            if( !logToString ) {
                if( type == LogType.Warning ) {
                    Logger.LogWarning( message, WarningLogSubtype.ConfigWarning );
                } else {
                    Logger.Log( message, type );
                }
            } else if( type != LogType.Debug ) {
                errors += message + Environment.NewLine;
            }
        }

        #endregion
    }
}