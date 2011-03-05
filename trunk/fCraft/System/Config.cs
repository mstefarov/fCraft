// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

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
     * 112 - r235 - Removed PingInterval config key
     *              Removed inactive ControlPhysics and AddLandmarks permissions
     *
     * 113 - r243 - Removed IRCBotQuitMsg config key
     * 
     * 114 - r244 - Added IRCRegisteredNick, IRCNickServ, and IRCNickServMessage keys
     * 
     * 115 - r265 - Added IRCThreads keys
     * 
     * 116 - r272 - Added AutoRankEnabled keys
     * 
     * 117 - r280 - Added MaxUndo keys
     * 
     * 118 - r318 - Added MeColor and WarningColor keys
     * 
     * 119 - r331 - Added LogPath and MapPath keys
     * 
     * 120 - r332 - Added DataPath key
     * 
     * 121 - r335 - Renamed SendRedundantBlockUpdates key to RelayAllBlockUpdates
     * 
     * 122 - r341 - Added IRCUseColor key
     * 
     * 123 - r346 - Added IRCBotAnnounceServerEvents
     * 
     * 124 - r354 - Added HeartbeatEnabled
     * 
     * 125 - r356 - Removed LogPath and DataPath keys
     * 
     * 126 - r366 - Added PreventSecurityCircumvention key
     * 
     * 127 - r368 - Removed PreventSecurityCircumvention key
     *              Added per-rank AllowSecurityCircumvention setting instead
     *              
     * 128 - r379 - Added ConsoleName
     * 
     * 129 - r405 - Added ShowConnectedMessages
     * 
     * 130 - r413 - Added ShowBannedConnectionMessages
     */

    /// <summary> Static class that handles loading/saving configuration, contains config defaults,
    /// and various configuration-related utilities. </summary>
    public static class Config {
        public const int ProtocolVersion = 7;
        public const int ConfigVersion = 129;
        public const int MaxPlayersSupported = 128;
        public const string ConfigRootName = "fCraftConfig";
        static Dictionary<ConfigKey, string> settings = new Dictionary<ConfigKey, string>();
        static Dictionary<ConfigKey, string> defaults;
        static Dictionary<string, ConfigKey> legacyConfigKeys = new Dictionary<string, ConfigKey>(); // LEGACY
        static Dictionary<ConfigKey, KeyValuePair<string, string>> legacyConfigValues = new Dictionary<ConfigKey, KeyValuePair<string, string>>();

        static Config() {
            LoadDefaults();
            defaults = new Dictionary<ConfigKey, string>( settings );

            // These keys were renamed at some point. LEGACY
            legacyConfigKeys.Add( "DefaultClass".ToLower(), ConfigKey.DefaultRank );
            legacyConfigKeys.Add( "ClassColorsInChat".ToLower(), ConfigKey.RankColorsInChat );
            legacyConfigKeys.Add( "ClassColorsInWorldNames".ToLower(), ConfigKey.RankColorsInWorldNames );
            legacyConfigKeys.Add( "ClassPrefixesInChat".ToLower(), ConfigKey.RankPrefixesInChat );
            legacyConfigKeys.Add( "ClassPrefixesInList".ToLower(), ConfigKey.RankPrefixesInList );
            legacyConfigKeys.Add( "PatrolledClass".ToLower(), ConfigKey.PatrolledRank );
            legacyConfigKeys.Add( "RequireClassChangeReason".ToLower(), ConfigKey.RequireRankChangeReason );
            legacyConfigKeys.Add( "AnnounceClassChanges".ToLower(), ConfigKey.AnnounceRankChanges );
            legacyConfigKeys.Add( "SendRedundantBlockUpdates".ToLower(), ConfigKey.RelayAllBlockUpdates );
            legacyConfigKeys.Add( "AutomaticUpdates".ToLower(), ConfigKey.UpdateMode );
            legacyConfigKeys.Add( "IRCBot".ToLower(), ConfigKey.IRCBotEnabled );

            // These values have been renamed at some point. LEGACY
            legacyConfigValues.Add( ConfigKey.ProcessPriority, new KeyValuePair<string, string>( "Low", ProcessPriorityClass.Idle.ToString() ) );
        }

        internal static void RunSelfTest() {
            // TESTS - ensure that all defaults are initialized
            foreach( ConfigKey key in Enum.GetValues( typeof( ConfigKey ) ) ) {
                if( !settings.ContainsKey( key ) ) {
                    throw new Exception( "One of the ConfigKey keys is missing a default: " + key );
                }
                if( settings[key] == null ) {
                    throw new Exception( "One of the ConfigKey kets is null: " + key );
                }
                GetValueType( key );
            }
        }

        #region Defaults

        /// <summary>
        /// Overwrites current settings with defaults
        /// </summary>
        public static void LoadDefaults() {
            LoadDefaultsGeneral();
            LoadDefaultsChat();
            LoadDefaultsWorlds();
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
            SetValue( ConfigKey.AnnouncementInterval, 0 );
        }

        public static void LoadDefaultsChat() {
            SetValue( ConfigKey.SystemMessageColor, Color.GetName( Color.SysDefault ) );
            SetValue( ConfigKey.HelpColor, Color.GetName( Color.HelpDefault ) );
            SetValue( ConfigKey.SayColor, Color.GetName( Color.SayDefault ) );
            SetValue( ConfigKey.AnnouncementColor, Color.GetName( Color.AnnouncementDefault ) );
            SetValue( ConfigKey.PrivateMessageColor, Color.GetName( Color.PMDefault ) );
            SetValue( ConfigKey.MeColor, Color.GetName( Color.MeDefault ) );
            SetValue( ConfigKey.WarningColor, Color.GetName( Color.WarningDefault ) );
            SetValue( ConfigKey.ShowJoinedWorldMessages, true );
            SetValue( ConfigKey.ShowConnectionMessages, true );
            SetValue( ConfigKey.ShowBannedConnectionMessages, true );
            SetValue( ConfigKey.RankColorsInWorldNames, true );
            SetValue( ConfigKey.RankColorsInChat, true );
            SetValue( ConfigKey.RankPrefixesInChat, false );
            SetValue( ConfigKey.RankPrefixesInList, false );
        }

        public static void LoadDefaultsWorlds() {
            SetValue( ConfigKey.DefaultBuildRank, "" );
            SetValue( ConfigKey.MapPath, "./maps" );
        }

        public static void LoadDefaultsSecurity() {
            SetValue( ConfigKey.VerifyNames, NameVerificationMode.Balanced ); // can be "Always," "Balanced," or "Never"
            SetValue( ConfigKey.LimitOneConnectionPerIP, false );
            SetValue( ConfigKey.AllowUnverifiedLAN, false );

            SetValue( ConfigKey.PatrolledRank, "" ); // empty = lowest rank

            SetValue( ConfigKey.AntispamMessageCount, 4 );
            SetValue( ConfigKey.AntispamInterval, 5 );
            SetValue( ConfigKey.AntispamMuteDuration, 5 );
            SetValue( ConfigKey.AntispamMaxWarnings, 2 );

            SetValue( ConfigKey.RequireBanReason, false );
            SetValue( ConfigKey.RequireRankChangeReason, false );
            SetValue( ConfigKey.AnnounceKickAndBanReasons, true );
            SetValue( ConfigKey.AnnounceRankChanges, true );

            SetValue( ConfigKey.PaidPlayersOnly, false );
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
            SetValue( ConfigKey.IRCBotEnabled, false ); // Bot is disabled by default
            SetValue( ConfigKey.IRCBotNick, "fBot" );
            SetValue( ConfigKey.IRCBotNetwork, "irc.esper.net" );
            SetValue( ConfigKey.IRCBotPort, 6667 );
            SetValue( ConfigKey.IRCBotChannels, "#changeme" ); // This can be multiple using csv
            SetValue( ConfigKey.IRCBotAnnounceServerEvents, true );
            SetValue( ConfigKey.IRCBotAnnounceIRCJoins, false );
            SetValue( ConfigKey.IRCBotAnnounceServerJoins, false );
            SetValue( ConfigKey.IRCBotForwardFromIRC, false ); // Disabled by default
            SetValue( ConfigKey.IRCBotForwardFromServer, false ); // Disabled by default
            SetValue( ConfigKey.IRCMessageColor, Color.GetName( Color.IRCDefault ) );
            SetValue( ConfigKey.IRCDelay, 750 );
            SetValue( ConfigKey.IRCRegisteredNick, false );
            SetValue( ConfigKey.IRCNickServ, "NickServ" );
            SetValue( ConfigKey.IRCNickServMessage, "IDENTIFY password" );
            SetValue( ConfigKey.IRCThreads, 1 );
            SetValue( ConfigKey.IRCUseColor, true );
        }

        public static void LoadDefaultsAdvanced() {
            SetValue( ConfigKey.RelayAllBlockUpdates, false );
            SetValue( ConfigKey.UpdateMode, AutoUpdaterMode.Prompt );
            SetValue( ConfigKey.NoPartialPositionUpdates, false );
            SetValue( ConfigKey.ProcessPriority, "" );
            SetValue( ConfigKey.BlockUpdateThrottling, 2048 );
            SetValue( ConfigKey.TickInterval, 100 );
            SetValue( ConfigKey.LowLatencyMode, false );
            SetValue( ConfigKey.SubmitCrashReports, true );

            SetValue( ConfigKey.MaxUndo, 2000000 );
            SetValue( ConfigKey.AutoRankEnabled, false );
            SetValue( ConfigKey.HeartbeatEnabled, true );

            SetValue( ConfigKey.ConsoleName, "(console)" );
        }

        public static bool IsDefault( this ConfigKey key ) {
            return (settings[key] == defaults[key]);
        }

        public static bool IsDefault( this ConfigKey key, string value ) {
            return (value == defaults[key]);
        }

        public static string GetDefault( this ConfigKey key ) {
            return defaults[key];
        }

        #endregion


        #region Loading

        /// <summary>
        /// Loads config from file.
        /// </summary>
        /// <param name="skipRankList">If true, skips over rank definitions.</param>
        /// <returns>True if loading succeeded.</returns>
        public static bool Load( bool skipRankList, bool raiseReloadedEvent ) {
            // generate random salt
            bool fromFile = false;

            // try to load config file (XML)
            XDocument file;
            if( File.Exists( Paths.ConfigFileName ) ) {
                try {
                    file = XDocument.Load( Paths.ConfigFileName );
                    if( file.Root == null || file.Root.Name != ConfigRootName ) {
                        Log( "Config.Load: Malformed or incompatible config file {0}. Loading defaults.", LogType.Warning, Paths.ConfigFileName );
                        file = new XDocument();
                        file.Add( new XElement( ConfigRootName ) );
                    } else {
                        Log( "Config.Load: Config file {0} loaded succesfully.", LogType.Debug, Paths.ConfigFileName );
                        fromFile = true;
                    }
                } catch( Exception ex ) {
                    Log( "Config.Load: Fatal error while loading config file {0}: {1}", LogType.FatalError,
                         Paths.ConfigFileName, ex );
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

            // read rank definitions
            if( !skipRankList ) {
                LoadRankList( config, version, fromFile );
            }

            // read log options for console
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

            // read log options for logfile
            XElement logFileOptions = config.Element( "LogFileOptions" );
            if( logFileOptions != null ) {
                LoadLogOptions( logFileOptions, Logger.logFileOptions );
            } else {
                if( fromFile ) Log( "Config.Load: using default log file options.", LogType.Warning );
                for( int i = 0; i < Logger.logFileOptions.Length; i++ ) {
                    Logger.logFileOptions[i] = true;
                }
            }

            // read the rest of the keys
            string[] keyNames = Enum.GetNames( typeof( ConfigKey ) );
            foreach( XElement element in config.Elements() ) {
                string key = element.Name.ToString().ToLower();
                if( keyNames.Contains( key, StringComparer.OrdinalIgnoreCase ) ) {
                    // known key
                    SetValue( (ConfigKey)Enum.Parse( typeof( ConfigKey ), key, true ), element.Value );

                } else if( legacyConfigKeys.ContainsKey( key ) ) { // LEGACY
                    // renamed/legacy key
                    SetValue( legacyConfigKeys[key], element.Value );

                } else if( key != "consoleoptions" &&
                           key != "logfileoptions" &&
                           key != "classes" && // LEGACY
                           key != "ranks" &&
                           key != "legacyrankmapping" ) {
                    // unknown key
                    Log( "Unrecognized entry ignored: {0} = {1}", LogType.Debug, element.Name, element.Value );
                }
            }

            if( raiseReloadedEvent ) RaiseReloadedEvent();

            return true;
        }


        static void LoadLogOptions( XElement el, bool[] list ) {
            for( int i = 0; i < list.Length; i++ ) {
                if( el.Element( ((LogType)i).ToString() ) != null ) {
                    list[i] = true;
                } else {
                    list[i] = false;
                }
            }
        }


        static void LoadRankList( XElement config, int version, bool fromFile ) {
            XElement legacyRankMappingTag = config.Element( "LegacyRankMapping" );
            if( legacyRankMappingTag != null ) {
                foreach( XElement rankPair in legacyRankMappingTag.Elements( "LegacyRankPair" ) ) {
                    XAttribute fromRankID = rankPair.Attribute( "from" );
                    XAttribute toRankID = rankPair.Attribute( "to" );
                    if( fromRankID == null || String.IsNullOrEmpty( fromRankID.Value ) ||
                        toRankID == null || String.IsNullOrEmpty( toRankID.Value ) ) {
                        Log( "Config.Load: Could not parse a LegacyRankMapping entry: {0}", LogType.Error, rankPair.ToString() );
                    } else {
                        RankList.LegacyRankMapping.Add( fromRankID.Value, toRankID.Value );
                    }
                }
            }

            XElement rankList = config.Element( "Ranks" ) ?? config.Element( "Classes" );

            if( rankList != null ) {
                XElement[] rankDefinitionList = rankList.Elements( "Rank" ).ToArray();
                if( rankDefinitionList.Length == 0 )
                    rankDefinitionList = rankList.Elements( "PlayerClass" ).ToArray(); // LEGACY

                foreach( XElement rankDefinition in rankDefinitionList ) {
                    try {
                        RankList.AddRank( new Rank( rankDefinition ) );
                    } catch( RankDefinitionException ex ) {
                        Log( ex.Message, LogType.Error );
                    }
                }

                if( RankList.RanksByName.Count == 0 ) {
                    Log( "Config.Load: No ranks were defined, or none were defined correctly. Using default ranks (guest, regular, op, and owner).", LogType.Warning );
                    rankList.Remove();
                    config.Add( DefineDefaultRanks() );

                } else if( version < ConfigVersion ) { // start LEGACY code

                    if( version < 103 ) { // speedhack permission
                        if( !RankList.RanksByID.Values.Any( rank => rank.Can( Permission.UseSpeedHack ) ) ) {
                            foreach( Rank rank in RankList.RanksByID.Values ) {
                                rank.Permissions[(int)Permission.UseSpeedHack] = true;
                            }
                            Log( "Config.Load: All ranks were granted UseSpeedHack permission (default). " +
                                 "Use ConfigTool to update config. If you are editing config.xml manually, " +
                                 "set version=\"{0}\" to prevent permissions from resetting in the future.", LogType.Warning, ConfigVersion );
                        }
                    }

                    if( version < 111 ) {
                        RankList.SortRanksByLegacyNumericRank();
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

        public static bool Save( bool saveSalt ) {
            XDocument file = new XDocument();

            XElement config = new XElement( ConfigRootName );
            config.Add( new XAttribute( "version", ConfigVersion ) );
            if( saveSalt ) {
                config.Add( new XAttribute( "salt", Server.Salt ) );
            }

            // save general settings
            foreach( KeyValuePair<ConfigKey, string> pair in settings ) {
                config.Add( new XElement( pair.Key.ToString(), pair.Value ) );
            }

            // save console options
            XElement consoleOptions = new XElement( "ConsoleOptions" );
            for( int i = 0; i < Logger.consoleOptions.Length; i++ ) {
                if( Logger.consoleOptions[i] ) {
                    consoleOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( consoleOptions );

            // save logfile options
            XElement logFileOptions = new XElement( "LogFileOptions" );
            for( int i = 0; i < Logger.logFileOptions.Length; i++ ) {
                if( Logger.logFileOptions[i] ) {
                    logFileOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( logFileOptions );

            // save ranks
            XElement ranksTag = new XElement( "Ranks" );
            foreach( Rank rank in RankList.Ranks ) {
                ranksTag.Add( rank.Serialize() );
            }
            config.Add( ranksTag );

            // save legacy rank mapping
            XElement legacyRankMappingTag = new XElement( "LegacyRankMapping" );
            foreach( KeyValuePair<string, string> pair in RankList.LegacyRankMapping ) {
                XElement rankPair = new XElement( "LegacyRankPair" );
                rankPair.Add( new XAttribute( "from", pair.Key ), new XAttribute( "to", pair.Value ) );
                legacyRankMappingTag.Add( rankPair );
            }
            config.Add( legacyRankMappingTag );


            file.Add( config );
            try {
                // write out the changes
                string configTempFileName = Paths.ConfigFileName + ".temp";
                file.Save( configTempFileName );

                if( File.Exists( Paths.ConfigFileName ) ) {
                    File.Replace( configTempFileName, Paths.ConfigFileName, null, true );
                } else {
                    File.Move( configTempFileName, Paths.ConfigFileName );
                }
                return true;
            } catch( Exception ex ) {
                Log( "Config.Load: Fatal error while saving config file {0}: {1}", LogType.FatalError,
                     Paths.ConfigFileName, ex );
                return false;
            }
        }

        #endregion


        #region Getters

        public static bool IsEmpty( this ConfigKey key ) {
            return !settings.ContainsKey( key ) || String.IsNullOrEmpty( settings[key] );
        }

        public static string GetString( this ConfigKey key ) {
            return settings[key];
        }

        public static int GetInt( this ConfigKey key ) {
            return Int32.Parse( settings[key] );
        }

        public static TEnum GetEnum<TEnum>( this ConfigKey key ) where TEnum : struct {
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required", "T" );
            return (TEnum)Enum.Parse( typeof( TEnum ), settings[key], true );
        }

        public static bool GetBool( this ConfigKey key ) {
            return Boolean.Parse( settings[key] );
        }

        public static Type GetValueType( this ConfigKey key ) {
            switch( key ) {
                case ConfigKey.AllowUnverifiedLAN:
                case ConfigKey.AnnounceKickAndBanReasons:
                case ConfigKey.AnnounceRankChanges:
                case ConfigKey.AutoRankEnabled:
                case ConfigKey.BackupOnJoin:
                case ConfigKey.BackupOnlyWhenChanged:
                case ConfigKey.BackupOnStartup:
                case ConfigKey.HeartbeatEnabled:
                case ConfigKey.IRCBotEnabled:
                case ConfigKey.IRCBotAnnounceIRCJoins:
                case ConfigKey.IRCBotAnnounceServerEvents:
                case ConfigKey.IRCBotAnnounceServerJoins:
                case ConfigKey.IRCBotForwardFromIRC:
                case ConfigKey.IRCBotForwardFromServer:
                case ConfigKey.IRCRegisteredNick:
                case ConfigKey.IRCUseColor:
                case ConfigKey.IsPublic:
                case ConfigKey.LimitOneConnectionPerIP:
                case ConfigKey.LowLatencyMode:
                case ConfigKey.NoPartialPositionUpdates:
                case ConfigKey.PaidPlayersOnly:
                case ConfigKey.RankColorsInChat:
                case ConfigKey.RankColorsInWorldNames:
                case ConfigKey.RankPrefixesInChat:
                case ConfigKey.RankPrefixesInList:
                case ConfigKey.RelayAllBlockUpdates:
                case ConfigKey.RequireBanReason:
                case ConfigKey.RequireRankChangeReason:
                case ConfigKey.SaveOnShutdown:
                case ConfigKey.ShowBannedConnectionMessages:
                case ConfigKey.ShowConnectionMessages:
                case ConfigKey.ShowJoinedWorldMessages:
                case ConfigKey.SubmitCrashReports:
                    return typeof(bool);

                case ConfigKey.AnnouncementColor:
                case ConfigKey.HelpColor:
                case ConfigKey.IRCMessageColor:
                case ConfigKey.MeColor:
                case ConfigKey.PrivateMessageColor:
                case ConfigKey.SayColor:
                case ConfigKey.SystemMessageColor:
                case ConfigKey.WarningColor:
                    return typeof( Color );

                case ConfigKey.AnnouncementInterval:
                case ConfigKey.AntispamInterval:
                case ConfigKey.AntispamMaxWarnings:
                case ConfigKey.AntispamMessageCount:
                case ConfigKey.AntispamMuteDuration:
                case ConfigKey.BackupInterval:
                case ConfigKey.BlockUpdateThrottling:
                case ConfigKey.IRCBotPort:
                case ConfigKey.IRCDelay:
                case ConfigKey.IRCThreads:
                case ConfigKey.MaxBackups:
                case ConfigKey.MaxBackupSize:
                case ConfigKey.MaxLogs:
                case ConfigKey.MaxPlayers:
                case ConfigKey.MaxUndo:
                case ConfigKey.Port:
                case ConfigKey.SaveInterval:
                case ConfigKey.TickInterval:
                case ConfigKey.UploadBandwidth:
                    return typeof( int );

                case ConfigKey.ConsoleName:
                case ConfigKey.IRCBotChannels:
                case ConfigKey.IRCBotNetwork:
                case ConfigKey.IRCBotNick:
                case ConfigKey.IRCNickServ:
                case ConfigKey.IRCNickServMessage:
                case ConfigKey.MapPath:
                case ConfigKey.MOTD:
                case ConfigKey.ServerName:
                    return typeof( string );

                case ConfigKey.DefaultBuildRank:
                case ConfigKey.DefaultRank:
                case ConfigKey.PatrolledRank:
                    return typeof( Rank );

                case ConfigKey.IP:
                    return typeof( IPAddress );

                case ConfigKey.LogMode:
                    return typeof( LogSplittingType );

                case ConfigKey.UpdateMode:
                    return typeof( AutoUpdaterMode );

                case ConfigKey.ProcessPriority:
                    return typeof( ProcessPriorityClass );

                case ConfigKey.VerifyNames:
                    return typeof( NameVerificationMode );

                default:
                    throw new ArgumentOutOfRangeException( "key", key, "Unknown key" );
            }
        }

        #endregion


        #region Setters

        public static bool ResetValue( this ConfigKey key ) {
            return key.SetValue( key.GetDefault() );
        }

        public static bool SetValue( this ConfigKey key, object _value ) {
            if( _value == null ) {
                throw new ArgumentNullException( "_value", "ConfigKey values cannot be null. Use an empty string to indicate unset value." );
            }

            string value = _value.ToString();

            // LEGACY
            if( legacyConfigValues.ContainsKey( key ) ) {
                foreach( var pair in legacyConfigValues.Values ) {
                    if( pair.Key.Equals( value, StringComparison.OrdinalIgnoreCase ) ) {
                        value = pair.Value;
                        break;
                    }
                }
            }

            switch( key ) {
                case ConfigKey.ServerName:
                    return ValidateString( key, value, 1, 64 );
                case ConfigKey.MOTD:
                    return ValidateString( key, value, 0, 64 );
                case ConfigKey.MaxPlayers:
                    return ValidateInt( key, value, 1, MaxPlayersSupported );

                case ConfigKey.DefaultRank:
                case ConfigKey.DefaultBuildRank:
                case ConfigKey.PatrolledRank:
                    if( value.Length > 0 ) {
                        if( RankList.ParseRank( value ) != null ) {
                            settings[key] = RankList.ParseRank( value ).Name;
                            return true;
                        } else if( settings.ContainsKey( key ) && !String.IsNullOrEmpty( settings[key] ) ) {
                            Log( "Config.SetValue: {0} could not be parsed. " +
                                 "It should be either blank (indicating \"use lowest rank\") or set to a valid rank name. " +
                                 "Using default ({1}).", LogType.Warning,
                                 key, RankList.ParseRank( settings[key] ).Name );
                            return false;
                        } else {
                            Log( "Config.SetValue: {0} could not be parsed. " +
                                "It should be either blank (indicating \"use lowest rank\") or set to a valid rank name. " +
                                "Using default ({1}).", LogType.Warning,
                                key, RankList.LowestRank.Name );
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
                    return ValidateInt( key, value, 1, 100000 );
                case ConfigKey.IP:
                    IPAddress tempIP;
                    if( IPAddress.TryParse( value, out tempIP ) && tempIP.ToString() != IPAddress.Broadcast.ToString() ) {
                        settings[key] = value;
                        return true;
                    } else {
                        return false;
                    }

                case ConfigKey.IRCBotNick:
                case ConfigKey.IRCNickServ:
                    return ValidateString( key, value, 1, 32 );
                case ConfigKey.IRCDelay:
                    return ValidateInt( key, value, 100, 2000 );
                case ConfigKey.IRCThreads:
                    return ValidateInt( key, value, 1, 4 );
                case ConfigKey.AnnouncementInterval:
                    return ValidateInt( key, value, 0, 600 );

                case ConfigKey.AllowUnverifiedLAN:
                case ConfigKey.AnnounceKickAndBanReasons:
                case ConfigKey.AnnounceRankChanges:
                case ConfigKey.AutoRankEnabled:
                case ConfigKey.BackupOnJoin:
                case ConfigKey.BackupOnlyWhenChanged:
                case ConfigKey.BackupOnStartup:
                case ConfigKey.HeartbeatEnabled:
                case ConfigKey.IRCBotEnabled:
                case ConfigKey.IRCBotAnnounceIRCJoins:
                case ConfigKey.IRCBotAnnounceServerEvents:
                case ConfigKey.IRCBotAnnounceServerJoins:
                case ConfigKey.IRCBotForwardFromIRC:
                case ConfigKey.IRCBotForwardFromServer:
                case ConfigKey.IRCRegisteredNick:
                case ConfigKey.IRCUseColor:
                case ConfigKey.IsPublic:
                case ConfigKey.LimitOneConnectionPerIP:
                case ConfigKey.LowLatencyMode:
                case ConfigKey.NoPartialPositionUpdates:
                case ConfigKey.PaidPlayersOnly:
                case ConfigKey.RankColorsInChat:
                case ConfigKey.RankColorsInWorldNames:
                case ConfigKey.RankPrefixesInChat:
                case ConfigKey.RankPrefixesInList:
                case ConfigKey.RelayAllBlockUpdates:
                case ConfigKey.RequireBanReason:
                case ConfigKey.RequireRankChangeReason:
                case ConfigKey.SaveOnShutdown:
                case ConfigKey.ShowBannedConnectionMessages:
                case ConfigKey.ShowConnectionMessages:
                case ConfigKey.ShowJoinedWorldMessages:
                case ConfigKey.SubmitCrashReports:
                    return ValidateBool( key, value );

                case ConfigKey.SystemMessageColor:
                case ConfigKey.HelpColor:
                case ConfigKey.SayColor:
                case ConfigKey.AnnouncementColor:
                case ConfigKey.PrivateMessageColor:
                case ConfigKey.IRCMessageColor:
                case ConfigKey.MeColor:
                case ConfigKey.WarningColor:
                    return ValidateColor( key, value );

                case ConfigKey.VerifyNames:
                    return ValidateEnum<NameVerificationMode>( key, value );
                case ConfigKey.AntispamMessageCount:
                    return ValidateInt( key, value, 2, 50 );
                case ConfigKey.AntispamInterval:
                    return ValidateInt( key, value, 0, 60 );
                case ConfigKey.AntispamMuteDuration:
                    return ValidateInt( key, value, 0, 3600 );
                case ConfigKey.AntispamMaxWarnings:
                    return ValidateInt( key, value, 0, 50 );


                case ConfigKey.SaveInterval:
                    return ValidateInt( key, value, 0, 100000 );
                case ConfigKey.BackupInterval:
                    return ValidateInt( key, value, 0, 100000 );
                case ConfigKey.MaxBackups:
                    return ValidateInt( key, value, 0, 100000 );
                case ConfigKey.MaxBackupSize:
                    return ValidateInt( key, value, 0, 1000000 );

                case ConfigKey.LogMode:
                    return ValidateEnum<LogSplittingType>( key, value );
                case ConfigKey.MaxLogs:
                    return ValidateInt( key, value, 0, 100000 );

                case ConfigKey.ProcessPriority:
                    return ValidateEnum( key, value, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
                case ConfigKey.UpdateMode:
                    return ValidateEnum<AutoUpdaterMode>( key, value );
                case ConfigKey.BlockUpdateThrottling:
                    return ValidateInt( key, value, 10, 100000 );
                case ConfigKey.TickInterval:
                    return ValidateInt( key, value, 20, 1000 );

                case ConfigKey.MaxUndo:
                    return ValidateInt( key, value, 0, Int32.MaxValue );

                case ConfigKey.ConsoleName:
                case ConfigKey.IRCBotChannels:
                case ConfigKey.IRCBotNetwork:
                case ConfigKey.IRCNickServMessage:
                case ConfigKey.MapPath:
                    return DoSetValue( key, value );

                default:
                    throw new Exception( "No validation defined for this key: " + key );
            }
        }

        static bool ValidateInt( ConfigKey key, string value, int minRange, int maxRange ) {
            int temp;
            if( Int32.TryParse( value, out temp ) ) {
                if( temp >= minRange && temp <= maxRange ) {
                    return DoSetValue( key, temp.ToString() );
                } else {
                    Log( "Config.ValidateInt: Specified value for {0} is not within valid range ({1}...{2}). Using default ({3}).", LogType.Warning,
                         key, minRange, maxRange, settings[key] );
                    return false;
                }

            } else {
                Log( "Config.ValidateInt: Specified value for {0} could not be parsed. Using default ({1}).", LogType.Warning,
                     key, settings[key] );
                return false;
            }
        }

        static bool ValidateBool( ConfigKey key, string value ) {
            bool temp;
            if( Boolean.TryParse( value, out temp ) ) {
                return DoSetValue( key, temp.ToString() );

            } else {
                Log( "Config.ValidateBool: Specified value for {0} could not be parsed. Expected 'true' or 'false'. Using default ({1}).", LogType.Warning,
                     key, settings[key] );
                return false;
            }
        }

        static bool ValidateColor( ConfigKey key, string value ) {
            if( Color.Parse( value ) != null ) {
                return DoSetValue( key, value );

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
                return DoSetValue( key, value );
            }
        }

        static bool ValidateEnum( ConfigKey key, string value, params string[] options ) {
            for( int i = 0; i < options.Length; i++ ) {
                if( value.Equals( options[i], StringComparison.OrdinalIgnoreCase ) ) {
                    return DoSetValue( key, options[i] );
                }
            }
            Log( "Config.ValidateEnum: Invalid option specified for {0}. " +
                 "See documentation for the list of permitted options. Using default: {1}", LogType.Warning,
                 key, settings[key] );
            return false;
        }

        static bool ValidateEnum<TEnum>( ConfigKey key, string value ) where TEnum : struct {
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required", "TEnum" );
            try {
                TEnum val = (TEnum)Enum.Parse( typeof( TEnum ), value, true );
                DoSetValue( key, val.ToString() );
                return true;
            } catch( ArgumentException ) {
                Log( "Config.ValidateEnum: Invalid option specified for {0}. " +
                     "See documentation for the list of permitted options. Using default: {1}", LogType.Warning,
                     key, settings[key] );
                return false;
            }
        }

        static bool DoSetValue( ConfigKey key, string newValue ) {
            if( !settings.ContainsKey( key ) ) {
                settings[key] = newValue;
            } else {
                string oldValue = settings[key];
                if( oldValue != newValue ) {
                    if( RaiseKeyChangingEvent( key, oldValue, ref newValue ) ) return false;
                    settings[key] = newValue;
                    RaiseKeyChangedEvent( key, oldValue, newValue );
                }
            }
            return true;
        }

        #endregion


        internal static void ApplyConfig() {
            Logger.split = (LogSplittingType)Enum.Parse( typeof( LogSplittingType ), settings[ConfigKey.LogMode], true );
            Logger.MarkLogStart();

            Player.relayAllUpdates = GetBool( ConfigKey.RelayAllBlockUpdates );
            if( GetBool( ConfigKey.NoPartialPositionUpdates ) ) {
                Session.fullPositionUpdateInterval = 0;
            } else {
                Session.fullPositionUpdateInterval = Session.FullPositionUpdateIntervalDefault;
            }

            // chat colors
            Color.Sys = Color.Parse( settings[ConfigKey.SystemMessageColor] );
            Color.Say = Color.Parse( settings[ConfigKey.SayColor] );
            Color.Help = Color.Parse( settings[ConfigKey.HelpColor] );
            Color.Announcement = Color.Parse( settings[ConfigKey.AnnouncementColor] );
            Color.PM = Color.Parse( settings[ConfigKey.PrivateMessageColor] );
            Color.IRC = Color.Parse( settings[ConfigKey.IRCMessageColor] );
            Color.Me = Color.Parse( settings[ConfigKey.MeColor] );
            Color.Warning = Color.Parse( settings[ConfigKey.WarningColor] );

            // default class
            if( !String.IsNullOrEmpty( settings[ConfigKey.DefaultRank] ) ) {
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
            Player.autoMuteDuration = TimeSpan.FromSeconds( GetInt( ConfigKey.AntispamMuteDuration ) );

            // scheduler settings
            Server.maxUploadSpeed = GetInt( ConfigKey.UploadBandwidth );
            Server.packetsPerSecond = GetInt( ConfigKey.BlockUpdateThrottling );
            Server.ticksPerSecond = 1000 / (float)GetInt( ConfigKey.TickInterval );

            // rank to patrol
            World.rankToPatrol = RankList.ParseRank( settings[ConfigKey.PatrolledRank] ) ?? RankList.LowestRank;

            // IRC delay
            IRC.SendDelay = GetInt( ConfigKey.IRCDelay );

            DrawCommands.MaxUndoCount = GetInt( ConfigKey.MaxUndo );

            if( !Paths.IgnoreMapPathConfigKey && GetString( ConfigKey.MapPath ).Length > 0 ) {
                if( Paths.TestDirectory( GetString( ConfigKey.MapPath ), true ) ) {
                    Paths.MapPath = Path.GetFullPath( GetString( ConfigKey.MapPath ) );
                    Logger.Log( "Maps are stored at: {0}", LogType.SystemActivity, Paths.MapPath );
                }
            }

            AutoRank.CheckAutoRankSetting();
        }


        public static void ResetRanks() {
            RankList.Reset();
            DefineDefaultRanks();
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
            owner.Add( new XAttribute( "reserveSlot", true ) );
            owner.Add( new XAttribute( "allowSecurityCircumvention", true ) );

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
            owner.Add( new XElement( Permission.ReadStaffChat.ToString() ) );
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
            owner.Add( new XElement( Permission.EditPlayerDB.ToString() ) );

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
            } catch( RankDefinitionException ex ) {
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
            op.Add( new XElement( Permission.ReadStaffChat.ToString() ) );
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
            } catch( RankDefinitionException ex ) {
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
            } catch( RankDefinitionException ex ) {
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
            } catch( RankDefinitionException ex ) {
                Log( ex.Message, LogType.Error );
            }

            return permissions;
        }


        #region Logging

        public static string errors = ""; // for ConfigTool
        public static bool logToString;

        static void Log( string format, LogType type, params object[] args ) {
            Log( String.Format( format, args ), type );
        }

        static void Log( string message, LogType type ) {
            if( !logToString ) {
                Logger.Log( message, type );
            } else if( type != LogType.Debug ) {
                errors += message + Environment.NewLine;
            }
        }

        #endregion


        #region Events

        public static event EventHandler Reloaded;
        public static event EventHandler<ConfigKeyChangingEventArgs> KeyChanging;
        public static event EventHandler<ConfigKeyChangedEventArgs> KeyChanged;

        static void RaiseReloadedEvent() {
            var h = Reloaded;
            if( h != null ) h( null, EventArgs.Empty );
        }

        static bool RaiseKeyChangingEvent( ConfigKey key, string oldValue, ref string newValue ) {
            var h = KeyChanging;
            if( h == null ) return false;
            var e = new ConfigKeyChangingEventArgs( key, oldValue, newValue );
            h( null, e );
            newValue = e.NewValue;
            return e.Cancel;
        }

        static void RaiseKeyChangedEvent( ConfigKey key, string oldValue, string newValue ) {
            var h = KeyChanged;
            if( h != null ) h( null, new ConfigKeyChangedEventArgs( key, oldValue, newValue ) );
        }

        #endregion
    }


    #region EventArgs

    public class ConfigKeyChangingEventArgs : EventArgs {
        public ConfigKey Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; set; }
        public bool Cancel { get; set; }

        public ConfigKeyChangingEventArgs( ConfigKey _key, string _oldValue, string _newValue ) {
            Key = _key;
            OldValue = _oldValue;
            NewValue = _newValue;
            Cancel = false;
        }
    }


    public class ConfigKeyChangedEventArgs : EventArgs {
        public ConfigKey Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }

        public ConfigKeyChangedEventArgs( ConfigKey _key, string _oldValue, string _newValue ) {
            Key = _key;
            OldValue = _oldValue;
            NewValue = _newValue;
        }
    }

    #endregion
}