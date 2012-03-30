using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.UpgradeTool {

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
 * 110 - r227 - Added ShutdownServer and Mute permissions.
 *              NOTE: This build does not support loading config.xml of this or earlier versions.
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
 *              Config.xml files of earlier versions than 111 can NOT be loaded by this copy of fCraft.
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
 *              CONFIGS SAVED WITH THIS VERSION ARE NOT LOADABLE. It is obsolete.
 *              
 * 139 - r579 - Fixed XML structure messed up by 138. Sections are now saved into <Section> elements.
 * 
 * 140 - r616 - Added Spectate permission.
 * 
 * 141 - r622 - Added RestartInterval key.
 * 
 * 142 - r638 - Added BackupDataOnStartup key.
 * 
 * 143 - r676 - Added LoadPlugins key (currently unused).
 * 
 * 144 - r787 - Added DrawAdvanced permission.
 * 
 * 145 - r794 - Added UndoOthersActions permission.
 * 
 * 146 - r910 - Renamed BackupInterval to DefaultBackupInterval
 * 
 * 147 - r926 - Renamed EnableBlockDB to BlockDBEnabled
 * 
 * 148 - r1014 - Added BlockDBAutoEnable and BlockDBAutoEnableRank keys
 *               Moved BlockDBEnabled to Security ConfigSection
 *               
 * 149 - r1061 - Added HeartbeatToWoMDirect, WoMDirectDescription, and WoMEnableEnvExtensions keys
 * 
 * 150 - r1066 - Removed WoMDirectDescription key
 * 
 * 151 - r1169 - Added MaxUndoStates key
 *               Added fillLimit rank attribute.
 *               Changed defaults for some keys:
 *                  BlockDBEnabled to "true"
 *                  WomEnableEnvExtensions to "false"
 *                  IRCBotAnnounceServerEvents to "true"
 *                  
 * 152 - r1243 - Changed the way fCraft stores config keys.
 *               Before: <fCraftConfig><Section name="blah"><KeyName>Value</KeyName></Section></fCraftConfig>
 *               After: <fCraftConfig><Settings><ConfigKey key="KeyName" value="Value" default="DefaultValue" /></Settings></fCraftConfig>
 */

    class XmlConfig {
        const int HighestSupportedVersion = 156,
                  LowestSupportedVersion = 111,
                  FirstVersionWithMaxPlayersKey = 134,
                  FirstVersionWithSectionTags = 139,
                  FirstVersionWithSettingsTag = 152;

        const string XmlRootName = "fCraftConfig",
                     ConfigFileNameDefault = "config.xml";

        // List of renamed/remapped keys.
        static readonly Dictionary<string, ConfigKey> LegacyConfigKeys = new Dictionary<string, ConfigKey>(); // LEGACY

        // List of renamed/remapped key values.
        static readonly Dictionary<ConfigKey, KeyValuePair<string, string>> LegacyConfigValues =
                    new Dictionary<ConfigKey, KeyValuePair<string, string>>(); // LEGACY


        static XmlConfig() {
            // These keys were renamed at some point. LEGACY
            LegacyConfigKeys.Add( "SendRedundantBlockUpdates".ToLower(), ConfigKey.RelayAllBlockUpdates );
            LegacyConfigKeys.Add( "AutomaticUpdates".ToLower(), ConfigKey.UpdaterMode );
            LegacyConfigKeys.Add( "IRCBot".ToLower(), ConfigKey.IRCBotEnabled );
            LegacyConfigKeys.Add( "UpdateMode".ToLower(), ConfigKey.UpdaterMode );
            LegacyConfigKeys.Add( "BackupInterval".ToLower(), ConfigKey.DefaultBackupInterval );
            LegacyConfigKeys.Add( "EnableBlockDB".ToLower(), ConfigKey.BlockDBEnabled );

            // These values have been renamed at some point. LEGACY
            LegacyConfigValues.Add( ConfigKey.ProcessPriority,
                                    new KeyValuePair<string, string>( "Low", ProcessPriorityClass.Idle.ToString() ) );
        }



        static void Load() {
            bool fromFile = false;

            // try to load config file (XML)
            XElement config;
            if( File.Exists( Paths.ConfigFileName ) ) {
                try {
                    XDocument file = XDocument.Load( Paths.ConfigFileName );
                    config = file.Root;
                    if( config == null || config.Name != XmlRootName ) {
                        Logger.Log( LogType.Warning,
                                    "Config.Load: Malformed or incompatible config file {0}. Loading defaults.",
                                    Paths.ConfigFileName );
                        config = new XElement( XmlRootName );
                    } else {
                        fromFile = true;
                    }
                } catch( XmlException ex ) {
                    string errorMsg = "Config.Load: config.xml is not properly formatted: " + ex.Message;
                    throw new MisconfigurationException( errorMsg, ex );
                }
            } else {
                // create a new one (with defaults) if no file exists
                config = new XElement( XmlRootName );
            }

            int version = 0;
            if( fromFile ) {
                XAttribute attr = config.Attribute( "version" );
                if( attr != null && Int32.TryParse( attr.Value, out version ) ) {
                    if( version < LowestSupportedVersion ) {
                        Logger.Log( LogType.Warning,
                                    "Config.Load: Your copy of config.xml is too old to be loaded properly. " +
                                    "Some settings will be lost or replaced with defaults. " +
                                    "Please run ConfigGUI to make sure that everything is in order." );
                    } else if( version != HighestSupportedVersion ) {
                        Logger.Log( LogType.Warning,
                                    "Config.Load: Your config.xml was made for a different version of fCraft. " +
                                    "Some obsolete settings might be ignored, and some recently-added settings will be set to defaults. " +
                                    "It is recommended that you run ConfigGUI to make sure that everything is in order." );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Config.Load: Unknown version of config.xml found. It might be corrupted. " +
                                "Please run ConfigGUI to make sure that everything is in order." );
                }
            }

            // read rank definitions
            //TODO: RankManager.Reset();
            LoadRankList( config, fromFile );

            Config.ResetLogOptions();

            // read log options for console
            XElement consoleOptions = config.Element( "ConsoleOptions" );
            if( consoleOptions != null ) {
                LoadLogOptions( consoleOptions, Logger.ConsoleOptions );
            } else if( fromFile ) {
                Logger.Log( LogType.Warning, "Config.Load: using default console options." );
            }

            // read log options for logfiles
            XElement logFileOptions = config.Element( "LogFileOptions" );
            if( logFileOptions != null ) {
                LoadLogOptions( logFileOptions, Logger.LogFileOptions );
            } else if( fromFile ) {
                Logger.Log( LogType.Warning, "Config.Load: using default log file options." );
            }


            // read the rest of the keys
            if( version < FirstVersionWithSectionTags ) {
                foreach( XElement element in config.Elements() ) {
                    ParseKeyElementLegacy( element );
                }
            } else if( version < FirstVersionWithSettingsTag ) {
                foreach( XElement section in config.Elements( "Section" ) ) {
                    foreach( XElement keyElement in section.Elements() ) {
                        ParseKeyElementLegacy( keyElement );
                    }
                }
            } else {
                XElement settings = config.Element( "Settings" );
                if( settings != null ) {
                    foreach( XElement pair in settings.Elements( "ConfigKey" ) ) {
                        ParseKeyElement( pair );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Config.Load: No <Settings> tag present. Using default for everything." );
                }
            }

            RankManager.DefaultRank = Rank.Parse( ConfigKey.DefaultRank.GetString() );
            RankManager.DefaultBuildRank = Rank.Parse( ConfigKey.DefaultBuildRank.GetString() );
            RankManager.PatrolledRank = Rank.Parse( ConfigKey.PatrolledRank.GetString() );
            RankManager.BlockDBAutoEnableRank = Rank.Parse( ConfigKey.BlockDBAutoEnableRank.GetString() );

            // key relation validation
            if( version < FirstVersionWithMaxPlayersKey ) {
                ConfigKey.MaxPlayersPerWorld.TrySetValue( ConfigKey.MaxPlayers.GetInt() );
            }
            if( ConfigKey.MaxPlayersPerWorld.GetInt() > ConfigKey.MaxPlayers.GetInt() ) {
                Logger.Log( LogType.Warning,
                            "Value of MaxPlayersPerWorld ({0}) was lowered to match MaxPlayers ({1}).",
                            ConfigKey.MaxPlayersPerWorld.GetInt(),
                            ConfigKey.MaxPlayers.GetInt() );
                ConfigKey.MaxPlayersPerWorld.TrySetValue( ConfigKey.MaxPlayers.GetInt() );
            }
        }


        static void LoadLogOptions( [NotNull] XContainer el, [NotNull] IList<bool> list ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            if( list == null ) throw new ArgumentNullException( "list" );

            for( int i = 0; i < list.Count; i++ ) {
                if( el.Element( ( (LogType)i ).ToString() ) != null ) {
                    list[i] = true;
                } else {
                    list[i] = false;
                }
            }
        }


        // LEGACY loader (for compatibility with config.xml versions prior to 152)
        static void ParseKeyElementLegacy( [NotNull] XElement element ) {
            if( element == null ) throw new ArgumentNullException( "element" );

            string keyName = element.Name.ToString().ToLower();
            ConfigKey key;
            if( Enum.TryParse( keyName, true, out key ) ) {
                // known key
                key.TrySetValue( element.Value );

            } else if( LegacyConfigKeys.ContainsKey( keyName ) ) {
                // LEGACY
                // renamed/legacy key
                LegacyConfigKeys[keyName].TrySetValue( element.Value );

            } else if( keyName == "limitoneconnectionperip" ) {
                // LEGACY
                Logger.Log( LogType.Warning,
                            "Config: LimitOneConnectionPerIP (bool) was replaced by MaxConnectionsPerIP (int). " +
                            "Adjust your configuration accordingly." );
                ConfigKey.MaxConnectionsPerIP.TrySetValue( 1 );

            } else if( keyName != "consoleoptions" &&
                       keyName != "logfileoptions" &&
                       keyName != "ranks" &&
                       keyName != "legacyrankmapping" ) {
                // unknown key
                Logger.Log( LogType.Warning,
                            "Config: Unrecognized entry ignored: {0} = {1}",
                            element.Name, element.Value );
            }
        }


        static void ParseKeyElement( [NotNull] XElement element ) {
            if( element == null ) throw new ArgumentNullException( "element" );

            XAttribute keyAttr = element.Attribute( "key" );
            XAttribute valueAttr = element.Attribute( "value" );
            if( keyAttr == null || valueAttr == null ) {
                Logger.Log( LogType.Error,
                            "Malformed ConfigKey element: {0}",
                            element );
                return;
            }
            XAttribute defaultAttr = element.Attribute( "default" );
            string keyName = keyAttr.Value;
            string value = valueAttr.Value;

            ConfigKey key;
            if( !Enum.TryParse( keyName, true, out key ) ) {
                if( LegacyConfigKeys.ContainsKey( keyName ) ) {
                    key = LegacyConfigKeys[keyName];
                } else {
                    // unknown key
                    Logger.Log( LogType.Warning,
                                "Config: Unrecognized key ignored: {0} = {1}",
                                element.Name, element.Value );
                    return;
                }
            }

            // see if setting is on its default value, and whether defaults have changed
            if( defaultAttr != null ) {
                string oldDefault = defaultAttr.Value;
                if( key.GetString() == key.GetString( value ) && !key.IsDefault( oldDefault ) ) {
                    Logger.Log( LogType.Warning,
                                "Config: Default value for {0} has been changed from {1} (\"{2}\") to {3} (\"{4}\"). " +
                                "You may want to adjust your settings accordingly.",
                                key,
                                key.GetPresentationString( oldDefault ),
                                oldDefault,
                                key.GetPresentationString( key.GetDefault() ),
                                key.GetDefault() );
                }
            }

            // known key
            key.TrySetValue( value );
        }


        static void LoadRankList( [NotNull] XContainer el, bool fromFile ) {
            if( el == null ) throw new ArgumentNullException( "el" );

            XElement legacyRankMappingTag = el.Element( "LegacyRankMapping" );
            if( legacyRankMappingTag != null ) {
                foreach( XElement rankPair in legacyRankMappingTag.Elements( "LegacyRankPair" ) ) {
                    XAttribute fromRankID = rankPair.Attribute( "from" );
                    XAttribute toRankID = rankPair.Attribute( "to" );
                    if( fromRankID == null || String.IsNullOrEmpty( fromRankID.Value ) ||
                        toRankID == null || String.IsNullOrEmpty( toRankID.Value ) ) {
                        Logger.Log( LogType.Error,
                                    "Config.Load: Could not parse a LegacyRankMapping entry: {0}", rankPair );
                    } else {
                        //TODO: RankManager.LegacyRankMapping.Add( fromRankID.Value, toRankID.Value );
                    }
                }
            }

            XElement rankList = el.Element( "Ranks" );

            if( rankList != null ) {
                XElement[] rankDefinitionList = rankList.Elements( "Rank" ).ToArray();

                foreach( XElement rankDefinition in rankDefinitionList ) {
                    try {
                        RankManager.AddRank( new Rank( rankDefinition ) );
                    } catch( RankDefinitionException ex ) {
                        Logger.Log( LogType.Error, ex.Message );
                    }
                }

                if( RankManager.RanksByName.Count == 0 ) {
                    Logger.Log( LogType.Warning,
                                "Config.Load: No ranks were defined, or none were defined correctly. " +
                                "Using default ranks (guest, builder, op, and owner)." );
                    rankList.Remove();
                    //TODO: el.Add( RankManager.DefineDefaultRanks() );
                }

            } else {
                if( fromFile ) Logger.Log( LogType.Warning, "Config.Load: using default player ranks." );
                //TODO: el.Add( RankManager.DefineDefaultRanks() );
            }

            // parse rank-limit permissions
            //TODO: RankManager.ParsePermissionLimits();
        }
    }
}
