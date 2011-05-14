// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using fCraft.Events;
using fCraft.AutoRank;

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
     * 
     * 131 - r460 - Changed default for IRCNick from "fBot" to "MinecraftBot"
     *              Relaxed range limits on many integer keys.
     *              Renamed ProcessPriority value "Low" to "Idle", to match WinAPI 
     *              
     * 132 - r477 - Added BackupBeforeUpdate, RunBeforeUpdate, and RunAfterUpdate config keys
     *              Renamed UpdateMode to UpdaterMode
     *              
     * 133 - r517 - Added UseColorCodes permission
     * 
     * 134 - r520 - Removed LimitOneConnectionPerIP key
     *              Added MaxConnectionsPerIP key
     *              
     * 135 - r526 - Added RequireKickReason and AnnounceRankChangeReasons keys
     *              Added ViewPlayerIPs permission
     *              
     * 136 - r528 - Added BringAll permission.
     * 
     * 137 - r556 - Added BandwidthUseMode key.
     * 
     * 138 - r578 - Removed SaveOnShutdown key.
     *              Tweaked range checks on some keys.
     *              Grouped key tags into section tags.
     *              When saving, keys with default values are now commented out.
     *              CONFIGS SAVED WITH THIS VERSION ARE NOT LOADABLE
     *              
     * 139 - r579 - Fixed XML structure messed up by 138. Sections are now saved into <Section> elements.
     * 
     */

    /// <summary> Static class that handles loading/saving configuration, contains config defaults,
    /// and various configuration-related utilities. </summary>
    public static class Config {
        public const int ProtocolVersion = 7;
        public const int ConfigVersion = 139;
        public const string ConfigXmlRootName = "fCraftConfig";

        static readonly Dictionary<ConfigKey, string> Settings = new Dictionary<ConfigKey, string>();
        static readonly Dictionary<ConfigKey, ConfigKeyAttribute> KeyMetadata = new Dictionary<ConfigKey, ConfigKeyAttribute>();
        static readonly Dictionary<ConfigSection, ConfigKey[]> KeySections = new Dictionary<ConfigSection, ConfigKey[]>();

        static readonly Dictionary<string, ConfigKey> LegacyConfigKeys = new Dictionary<string, ConfigKey>(); // LEGACY
        static readonly Dictionary<ConfigKey, KeyValuePair<string, string>> LegacyConfigValues = new Dictionary<ConfigKey, KeyValuePair<string, string>>();


        static Config() {
            foreach( var keyField in typeof( ConfigKey ).GetFields() ) {
                foreach( var attribute in (ConfigKeyAttribute[])keyField.GetCustomAttributes( typeof( ConfigKeyAttribute ), false ) ) {
                    ConfigKey key = (ConfigKey)keyField.GetValue( null );
                    attribute.Key = key;
                    KeyMetadata.Add( key, attribute );
                }
            }

            foreach( ConfigSection section in Enum.GetValues( typeof( ConfigSection ) ) ) {
                ConfigSection sec = section;
                KeySections.Add( section, KeyMetadata.Values.Where( att => (att.Section == sec) )
                                                            .Select( att => att.Key )
                                                            .ToArray() );
            }

            LoadDefaults();

            // These keys were renamed at some point. LEGACY
            LegacyConfigKeys.Add( "DefaultClass".ToLower(), ConfigKey.DefaultRank );
            LegacyConfigKeys.Add( "ClassColorsInChat".ToLower(), ConfigKey.RankColorsInChat );
            LegacyConfigKeys.Add( "ClassColorsInWorldNames".ToLower(), ConfigKey.RankColorsInWorldNames );
            LegacyConfigKeys.Add( "ClassPrefixesInChat".ToLower(), ConfigKey.RankPrefixesInChat );
            LegacyConfigKeys.Add( "ClassPrefixesInList".ToLower(), ConfigKey.RankPrefixesInList );
            LegacyConfigKeys.Add( "PatrolledClass".ToLower(), ConfigKey.PatrolledRank );
            LegacyConfigKeys.Add( "RequireClassChangeReason".ToLower(), ConfigKey.RequireRankChangeReason );
            LegacyConfigKeys.Add( "AnnounceClassChanges".ToLower(), ConfigKey.AnnounceRankChanges );
            LegacyConfigKeys.Add( "SendRedundantBlockUpdates".ToLower(), ConfigKey.RelayAllBlockUpdates );
            LegacyConfigKeys.Add( "AutomaticUpdates".ToLower(), ConfigKey.UpdaterMode );
            LegacyConfigKeys.Add( "IRCBot".ToLower(), ConfigKey.IRCBotEnabled );
            LegacyConfigKeys.Add( "UpdateMode".ToLower(), ConfigKey.UpdaterMode );

            // These values have been renamed at some point. LEGACY
            LegacyConfigValues.Add( ConfigKey.ProcessPriority, new KeyValuePair<string, string>( "Low", ProcessPriorityClass.Idle.ToString() ) );
        }


        internal static void RunSelfTest() {
            // TESTS - ensure that all defaults are initialized
            foreach( ConfigKey key in Enum.GetValues( typeof( ConfigKey ) ) ) {
                if( !Settings.ContainsKey( key ) ) {
                    throw new Exception( "One of the ConfigKey keys is missing a default: " + key );
                }
                if( Settings[key] == null ) {
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
            foreach( var pair in KeyMetadata ) {
                SetValue( pair.Key, pair.Value.DefaultValue );
            }
        }


        public static void LoadDefaults( ConfigSection section ) {
            foreach( var key in KeySections[section] ) {
                SetValue( key, KeyMetadata[key].DefaultValue );
            }
        }


        public static bool IsDefault( this ConfigKey key ) {
            return (KeyMetadata[key].DefaultValue.ToString() == Settings[key]);
        }


        public static bool IsDefault( this ConfigKey key, object value ) {
            return (KeyMetadata[key].DefaultValue.ToString() == value.ToString());
        }


        public static object GetDefault( this ConfigKey key ) {
            return KeyMetadata[key].DefaultValue;
        }

        #endregion


        #region Loading

        /// <summary>
        /// Loads config from file.
        /// </summary>
        /// <param name="skipRankList">If true, skips over rank definitions.</param>
        /// <param name="raiseReloadedEvent">Whether ConfigReloaded event should be raised.</param>
        /// <returns>True if loading succeeded.</returns>
        public static bool Load( bool skipRankList, bool raiseReloadedEvent ) {
            bool fromFile = false;

            // try to load config file (XML)
            XDocument file;
            if( File.Exists( Paths.ConfigFileName ) ) {
                try {
                    file = XDocument.Load( Paths.ConfigFileName );
                    if( file.Root == null || file.Root.Name != ConfigXmlRootName ) {
                        Logger.Log( "Config.Load: Malformed or incompatible config file {0}. Loading defaults.", LogType.Warning, Paths.ConfigFileName );
                        file = new XDocument();
                        file.Add( new XElement( ConfigXmlRootName ) );
                    } else {
                        Logger.Log( "Config.Load: Config file {0} loaded succesfully.", LogType.Debug, Paths.ConfigFileName );
                        fromFile = true;
                    }
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Config failed to load", "fCraft", ex, true );
                    return false;
                }
            } else {
                // create a new one (with defaults) if no file exists
                file = new XDocument();
                file.Add( new XElement( ConfigXmlRootName ) );
            }

            XElement config = file.Root;

            XAttribute attr = config.Attribute( "version" );
            int version = 0;
            if( fromFile && (attr == null || !Int32.TryParse( attr.Value, out version ) || version != ConfigVersion) ) {
                Logger.Log( "Config.Load: Your config.xml was made for a different version of fCraft. " +
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
                LoadLogOptions( consoleOptions, Logger.ConsoleOptions );
            } else {
                if( fromFile ) Logger.Log( "Config.Load: using default console options.", LogType.Warning );
                for( int i = 0; i < Logger.ConsoleOptions.Length; i++ ) {
                    Logger.ConsoleOptions[i] = true;
                }
                Logger.ConsoleOptions[(int)LogType.ConsoleInput] = false;
                Logger.ConsoleOptions[(int)LogType.Debug] = false;
            }

            // read log options for logfile
            XElement logFileOptions = config.Element( "LogFileOptions" );
            if( logFileOptions != null ) {
                LoadLogOptions( logFileOptions, Logger.LogFileOptions );
            } else {
                if( fromFile ) Logger.Log( "Config.Load: using default log file options.", LogType.Warning );
                for( int i = 0; i < Logger.LogFileOptions.Length; i++ ) {
                    Logger.LogFileOptions[i] = true;
                }
            }

            // read the rest of the keys
            string[] keyNames = Enum.GetNames( typeof( ConfigKey ) );
            if( version < 139 ) {
                foreach( XElement element in config.Elements() ) {
                    ParseKeyElement( element, keyNames );
                }
            } else {
                foreach( XElement section in config.Elements( "Section" ) ) {
                    foreach( XElement keyElement in section.Elements() ) {
                        ParseKeyElement( keyElement, keyNames );
                    }
                }
            }

            // key relation validation
            if( version < 134 ) {
                ConfigKey.MaxPlayersPerWorld.TrySetValue( ConfigKey.MaxPlayers.GetInt() );
            }
            if( ConfigKey.MaxPlayersPerWorld.GetInt() > ConfigKey.MaxPlayers.GetInt() ) {
                Logger.Log( "Value of MaxPlayersPerWorld ({0}) was lowered to match MaxPlayers ({1}).", LogType.Warning,
                     ConfigKey.MaxPlayersPerWorld.GetInt(),
                     ConfigKey.MaxPlayers.GetInt() );
                ConfigKey.MaxPlayersPerWorld.TrySetValue( ConfigKey.MaxPlayers.GetInt() );
            }

            if( raiseReloadedEvent ) RaiseReloadedEvent();

            return true;
        }


        static void ParseKeyElement( XElement element, string[] keyNames ) {
            if( element == null ) throw new ArgumentNullException( "element" );
            if( keyNames == null ) throw new ArgumentNullException( "keyNames" );

            string key = element.Name.ToString().ToLower();
            if( keyNames.Contains( key, StringComparer.OrdinalIgnoreCase ) ) {
                // known key
                TrySetValue( (ConfigKey)Enum.Parse( typeof( ConfigKey ), key, true ), element.Value );

            } else if( LegacyConfigKeys.ContainsKey( key ) ) { // LEGACY
                // renamed/legacy key
                TrySetValue( LegacyConfigKeys[key], element.Value );

            } else if( key.Equals( "LimitOneConnectionPerIP", StringComparison.OrdinalIgnoreCase ) ) {
                Logger.Log( "Config.Load: LimitOneConnectionPerIP (bool) was replaced by MaxConnectionsPerIP (int). Adjust your configuration accordingly.",
                            LogType.Warning );
                ConfigKey.MaxConnectionsPerIP.TrySetValue( 1 );

            } else if( key != "consoleoptions" &&
                       key != "logfileoptions" &&
                       key != "classes" && // LEGACY
                       key != "ranks" &&
                       key != "legacyrankmapping" ) {
                // unknown key
                Logger.Log( "Unrecognized entry ignored: {0} = {1}", LogType.Debug, element.Name, element.Value );
            }
        }


        static void LoadLogOptions( XElement el, bool[] list ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            if( list == null ) throw new ArgumentNullException( "list" );

            for( int i = 0; i < list.Length; i++ ) {
                if( el.Element( ((LogType)i).ToString() ) != null ) {
                    list[i] = true;
                } else {
                    list[i] = false;
                }
            }
        }


        static void LoadRankList( XElement el, int version, bool fromFile ) {
            if( el == null ) throw new ArgumentNullException( "el" );

            XElement legacyRankMappingTag = el.Element( "LegacyRankMapping" );
            if( legacyRankMappingTag != null ) {
                foreach( XElement rankPair in legacyRankMappingTag.Elements( "LegacyRankPair" ) ) {
                    XAttribute fromRankID = rankPair.Attribute( "from" );
                    XAttribute toRankID = rankPair.Attribute( "to" );
                    if( fromRankID == null || String.IsNullOrEmpty( fromRankID.Value ) ||
                        toRankID == null || String.IsNullOrEmpty( toRankID.Value ) ) {
                        Logger.Log( "Config.Load: Could not parse a LegacyRankMapping entry: {0}", LogType.Error, rankPair.ToString() );
                    } else {
                        RankManager.LegacyRankMapping.Add( fromRankID.Value, toRankID.Value );
                    }
                }
            }

            XElement rankList = el.Element( "Ranks" ) ?? el.Element( "Classes" );

            if( rankList != null ) {
                XElement[] rankDefinitionList = rankList.Elements( "Rank" ).ToArray();
                if( rankDefinitionList.Length == 0 )
                    rankDefinitionList = rankList.Elements( "PlayerClass" ).ToArray(); // LEGACY

                foreach( XElement rankDefinition in rankDefinitionList ) {
                    try {
                        RankManager.AddRank( new Rank( rankDefinition ) );
                    } catch( RankDefinitionException ex ) {
                        Logger.Log( ex.Message, LogType.Error );
                    }
                }

                if( RankManager.RanksByName.Count == 0 ) {
                    Logger.Log( "Config.Load: No ranks were defined, or none were defined correctly. Using default ranks (guest, regular, op, and owner).", LogType.Warning );
                    rankList.Remove();
                    el.Add( DefineDefaultRanks() );

                } else if( version < ConfigVersion ) { // start LEGACY code

                    if( version < 103 ) { // speedhack permission
                        if( !RankManager.RanksByID.Values.Any( rank => rank.Can( Permission.UseSpeedHack ) ) ) {
                            foreach( Rank rank in RankManager.RanksByID.Values ) {
                                rank.Permissions[(int)Permission.UseSpeedHack] = true;
                            }
                            Logger.Log( "Config.Load: All ranks were granted UseSpeedHack permission (default). " +
                                 "Use ConfigTool to update config. If you are editing config.xml manually, " +
                                 "set version=\"{0}\" to prevent permissions from resetting in the future.", LogType.Warning, ConfigVersion );
                        }
                    }

                    if( version < 111 ) {
                        RankManager.SortRanksByLegacyNumericRank();
                    }

                } // end LEGACY code

            } else {
                if( fromFile ) Logger.Log( "Config.Load: using default player ranks.", LogType.Warning );
                el.Add( DefineDefaultRanks() );
            }

            // parse rank-limit permissions
            RankManager.ParsePermissionLimits();
        }


        internal static void ApplyConfig() {
            Logger.SplittingType = (LogSplittingType)Enum.Parse( typeof( LogSplittingType ), Settings[ConfigKey.LogMode], true );
            Logger.MarkLogStart();

            Player.RelayAllUpdates = GetBool( ConfigKey.RelayAllBlockUpdates );
            if( GetBool( ConfigKey.NoPartialPositionUpdates ) ) {
                Session.FullPositionUpdateInterval = 0;
            } else {
                Session.FullPositionUpdateInterval = Session.FullPositionUpdateIntervalDefault;
            }

            // chat colors
            Color.Sys = Color.Parse( Settings[ConfigKey.SystemMessageColor] );
            Color.Say = Color.Parse( Settings[ConfigKey.SayColor] );
            Color.Help = Color.Parse( Settings[ConfigKey.HelpColor] );
            Color.Announcement = Color.Parse( Settings[ConfigKey.AnnouncementColor] );
            Color.PM = Color.Parse( Settings[ConfigKey.PrivateMessageColor] );
            Color.IRC = Color.Parse( Settings[ConfigKey.IRCMessageColor] );
            Color.Me = Color.Parse( Settings[ConfigKey.MeColor] );
            Color.Warning = Color.Parse( Settings[ConfigKey.WarningColor] );

            // default class
            if( !ConfigKey.DefaultRank.IsBlank() ) {
                if( RankManager.ParseRank( Settings[ConfigKey.DefaultRank] ) != null ) {
                    RankManager.DefaultRank = RankManager.ParseRank( Settings[ConfigKey.DefaultRank] );
                } else {
                    RankManager.DefaultRank = RankManager.LowestRank;
                    Logger.Log( "Config.ApplyConfig: Could not parse DefaultRank; assuming that the lowest rank ({0}) is the default.",
                         LogType.Warning, RankManager.DefaultRank.Name );
                }
            } else {
                RankManager.DefaultRank = RankManager.LowestRank;
            }

            // antispam
            Player.SpamChatCount = GetInt( ConfigKey.AntispamMessageCount );
            Player.SpamChatTimer = GetInt( ConfigKey.AntispamInterval );
            Player.AutoMuteDuration = TimeSpan.FromSeconds( GetInt( ConfigKey.AntispamMuteDuration ) );

            // scheduler settings
            Server.MaxUploadSpeed = GetInt( ConfigKey.UploadBandwidth );
            Server.PacketsPerSecond = GetInt( ConfigKey.BlockUpdateThrottling );
            Server.TicksPerSecond = 1000 / (float)GetInt( ConfigKey.TickInterval );

            // rank to patrol
            World.RankToPatrol = RankManager.ParseRank( ConfigKey.PatrolledRank.GetString() );

            // IRC delay
            IRC.SendDelay = GetInt( ConfigKey.IRCDelay );

            BuildingCommands.MaxUndoCount = GetInt( ConfigKey.MaxUndo );

            if( !Paths.IgnoreMapPathConfigKey && GetString( ConfigKey.MapPath ).Length > 0 ) {
                if( Paths.TestDirectory( "MapPath", GetString( ConfigKey.MapPath ), true ) ) {
                    Paths.MapPath = Path.GetFullPath( GetString( ConfigKey.MapPath ) );
                    Logger.Log( "Maps are stored at: {0}", LogType.SystemActivity, Paths.MapPath );
                }
            }

            AutoRankManager.CheckAutoRankSetting();
        }

        #endregion


        #region Saving

        public static bool Save( bool saveSalt ) {
            XDocument file = new XDocument();

            XElement config = new XElement( ConfigXmlRootName );
            config.Add( new XAttribute( "version", ConfigVersion ) );
            if( saveSalt ) {
                config.Add( new XAttribute( "salt", Server.Salt ) );
            }

            // save general settings
            foreach( ConfigSection section in Enum.GetValues( typeof( ConfigSection ) ) ) {
                XElement sectionEl = new XElement( "Section" );
                sectionEl.Add( new XAttribute( "name", section ) );
                foreach( ConfigKey key in KeyMetadata.Values.Where( a => a.Section == section ).Select( a => a.Key ) ) {
                    if( IsDefault( key ) ) {
                        sectionEl.Add( new XComment( new XElement( key.ToString(), Settings[key] ).ToString() ) );
                    } else {
                        sectionEl.Add( new XElement( key.ToString(), Settings[key] ) );
                    }
                }
                config.Add( sectionEl );
            }

            // save console options
            XElement consoleOptions = new XElement( "ConsoleOptions" );
            for( int i = 0; i < Logger.ConsoleOptions.Length; i++ ) {
                if( Logger.ConsoleOptions[i] ) {
                    consoleOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( consoleOptions );

            // save logfile options
            XElement logFileOptions = new XElement( "LogFileOptions" );
            for( int i = 0; i < Logger.LogFileOptions.Length; i++ ) {
                if( Logger.LogFileOptions[i] ) {
                    logFileOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( logFileOptions );

            // save ranks
            XElement ranksTag = new XElement( "Ranks" );
            foreach( Rank rank in RankManager.Ranks ) {
                ranksTag.Add( rank.Serialize() );
            }
            config.Add( ranksTag );

            // save legacy rank mapping
            XElement legacyRankMappingTag = new XElement( "LegacyRankMapping" );
            foreach( KeyValuePair<string, string> pair in RankManager.LegacyRankMapping ) {
                XElement rankPair = new XElement( "LegacyRankPair" );
                rankPair.Add( new XAttribute( "from", pair.Key ), new XAttribute( "to", pair.Value ) );
                legacyRankMappingTag.Add( rankPair );
            }
            config.Add( legacyRankMappingTag );


            file.Add( config );
            try {
                // write out the changes
                string tempFileName = Paths.ConfigFileName + ".temp";
                file.Save( tempFileName );
                Paths.MoveOrReplace( tempFileName, Paths.ConfigFileName );
                return true;
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Config failed to save", "fCraft", ex, true );
                return false;
            }
        }

        #endregion


        #region Getters

        public static bool IsBlank( this ConfigKey key ) {
            return !Settings.ContainsKey( key ) || String.IsNullOrEmpty( Settings[key] );
        }

        public static string GetString( this ConfigKey key ) {
            return KeyMetadata[key].Process( Settings[key] );
        }

        public static int GetInt( this ConfigKey key ) {
            return Int32.Parse( GetString( key ) );
        }

        public static TEnum GetEnum<TEnum>( this ConfigKey key ) where TEnum : struct {
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required", "TEnum" );
            return (TEnum)Enum.Parse( typeof( TEnum ), GetString( key ), true );
        }

        public static bool GetBool( this ConfigKey key ) {
            return Boolean.Parse( GetString( key ) );
        }

        public static Type GetValueType( this ConfigKey key ) {
            return KeyMetadata[key].ValueType;
        }

        public static ConfigKeyAttribute GetMetadata( this ConfigKey key ) {
            return KeyMetadata[key];
        }

        public static ConfigSection GetSection( this ConfigKey key ) {
            return KeyMetadata[key].Section;
        }

        public static string GetDescription( this ConfigKey key ) {
            return KeyMetadata[key].Description;
        }

        #endregion


        #region Setters

        /// <summary> Resets key value to its default setting. </summary>
        /// <param name="key"> Config key to reset. </param>
        /// <returns> True if value was reset. False if resetting was cancelled by an event handler/plugin. </returns>
        public static bool ResetValue( this ConfigKey key ) {
            return key.TrySetValue( key.GetDefault() );
        }


        /// <summary> Sets value of a specified config key.
        /// Note that this method may throw exceptions if the given value is not acceptible.
        /// Use Config.TrySetValue() if you'd like to suppress exceptions in favor of a boolean return value. </summary>
        /// <param name="key"> Config key to set. </param>
        /// <param name="rawValue"> Value to assign to the key. If passed object is not a string, rawValue.ToString() is used. </param>
        /// <exception cref="T:System.ArgumentNullException" />
        /// <exception cref="T:System.FormatException" />
        /// <returns> True if value is valid and has been assigned.
        /// False if value is valid, but assignment was cancelled by an event handler/plugin. </returns>
        public static bool SetValue( this ConfigKey key, object rawValue ) {
            if( rawValue == null ) {
                throw new ArgumentNullException( "rawValue", key + ": ConfigKey values cannot be null. Use an empty string to indicate unset value." );
            }

            string value = (rawValue as string ?? rawValue.ToString());

            if( value == null ) {
                throw new NullReferenceException( key + ": rawValue.ToString() returned null." );
            }

            // LEGACY
            if( LegacyConfigValues.ContainsKey( key ) ) {
                foreach( var pair in LegacyConfigValues.Values ) {
                    if( pair.Key.Equals( value, StringComparison.OrdinalIgnoreCase ) ) {
                        value = pair.Value;
                        break;
                    }
                }
            }

            // throws various exceptions (most commonly FormatException) if invalid
            KeyMetadata[key].Validate( value );

            return DoSetValue( key, value );
        }

        public static bool TrySetValue( this ConfigKey key, object rawValue ) {
            try {
                SetValue( key, rawValue );
                return true;
            } catch( FormatException ex ) {
                Logger.Log( "{0}.TrySetValue: {1}", LogType.Error, key, ex.Message );
                return false;
            }
        }


        static bool DoSetValue( ConfigKey key, string newValue ) {
            if( !Settings.ContainsKey( key ) ) {
                Settings[key] = newValue;
            } else {
                string oldValue = Settings[key];
                if( oldValue != newValue ) {
                    if( RaiseKeyChangingEvent( key, oldValue, ref newValue ) ) return false;
                    Settings[key] = newValue;
                    RaiseKeyChangedEvent( key, oldValue, newValue );
                }
            }
            return true;
        }

        #endregion


        #region Ranks

        /// <summary> Resets the list of ranks to defaults (guest/regular/op/owner).
        /// Warning: This method is not thread-safe. </summary>
        public static void ResetRanks() {
            RankManager.Reset();
            DefineDefaultRanks();
            RankManager.ParsePermissionLimits();
        }


        static XElement DefineDefaultRanks() {
            XElement permissions = new XElement( "Ranks" );

            XElement owner = new XElement( "Rank" );
            owner.Add( new XAttribute( "id", RankManager.GenerateID() ) );
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
            owner.Add( new XElement( Permission.UseColorCodes.ToString() ) );

            owner.Add( new XElement( Permission.PlaceGrass.ToString() ) );
            owner.Add( new XElement( Permission.PlaceWater.ToString() ) );
            owner.Add( new XElement( Permission.PlaceLava.ToString() ) );
            owner.Add( new XElement( Permission.PlaceAdmincrete.ToString() ) );
            owner.Add( new XElement( Permission.DeleteAdmincrete.ToString() ) );

            owner.Add( new XElement( Permission.Say.ToString() ) );
            owner.Add( new XElement( Permission.ReadStaffChat.ToString() ) );
            XElement temp = new XElement( Permission.Kick.ToString() );
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
            owner.Add( new XElement( Permission.ViewPlayerIPs.ToString() ) );
            owner.Add( new XElement( Permission.EditPlayerDB.ToString() ) );

            owner.Add( new XElement( Permission.Teleport.ToString() ) );
            owner.Add( new XElement( Permission.Bring.ToString() ) );
            owner.Add( new XElement( Permission.BringAll.ToString() ) );
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
                RankManager.AddRank( new Rank( owner ) );
            } catch( RankDefinitionException ex ) {
                Logger.Log( ex.Message, LogType.Error );
            }


            XElement op = new XElement( "Rank" );
            op.Add( new XAttribute( "id", RankManager.GenerateID() ) );
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
            op.Add( new XElement( Permission.UseColorCodes.ToString() ) );

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
            op.Add( new XElement( Permission.ViewPlayerIPs.ToString() ) );

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
                RankManager.AddRank( new Rank( op ) );
            } catch( RankDefinitionException ex ) {
                Logger.Log( ex.Message, LogType.Error );
            }


            XElement regular = new XElement( "Rank" );
            regular.Add( new XAttribute( "id", RankManager.GenerateID() ) );
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
                RankManager.AddRank( new Rank( regular ) );
            } catch( RankDefinitionException ex ) {
                Logger.Log( ex.Message, LogType.Error );
            }


            XElement guest = new XElement( "Rank" );
            guest.Add( new XAttribute( "id", RankManager.GenerateID() ) );
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
                RankManager.AddRank( new Rank( guest ) );
            } catch( RankDefinitionException ex ) {
                Logger.Log( ex.Message, LogType.Error );
            }

            return permissions;
        }

        #endregion


        #region Events

        /// <summary> Occurs after the entire configuration has been reloaded from file. </summary>
        public static event EventHandler Reloaded;


        /// <summary> Occurs when a config key is about to be changed (cancellable).
        /// The new value may be replaced by the callback. </summary>
        public static event EventHandler<ConfigKeyChangingEventArgs> KeyChanging;


        /// <summary> Occurs after a config key has been changed. </summary>
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
}


#region EventArgs
namespace fCraft.Events {

    public sealed class ConfigKeyChangingEventArgs : EventArgs {
        public ConfigKey Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; set; }
        public bool Cancel { get; set; }

        public ConfigKeyChangingEventArgs( ConfigKey key, string oldValue, string newValue ) {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            Cancel = false;
        }
    }


    public sealed class ConfigKeyChangedEventArgs : EventArgs {
        public ConfigKey Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }

        public ConfigKeyChangedEventArgs( ConfigKey key, string oldValue, string newValue ) {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

}
#endregion