// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Linq;

namespace fCraft {


    public enum ConfigKey : byte {
        ServerName,
        MOTD,
        MaxPlayers,
        DefaultClass,
        IsPublic,
        Port,
        UploadBandwidth,

        ClassColorsInChat,
        ClassPrefixesInChat,
        ClassPrefixesInList,
        SystemMessageColor,
        HelpColor,
        SayColor,

        VerifyNames,
        AnnounceUnverifiedNames,

        LimitOneConnectionPerIP,

        AntispamMessageCount,
        AntispamInterval,
        AntispamMuteDuration,
        AntispamMaxWarnings,

        AntigriefBlockCount,
        AntigriefInterval,

        SaveOnShutdown,
        SaveInterval,

        BackupOnStartup,
        BackupOnJoin,
        BackupOnlyWhenChanged,
        BackupInterval,
        MaxBackups,
        MaxBackupSize,

        LogMode,
        MaxLogs,

        IRCBot,
        IRCMsgs,
        IRCBotNick,
        IRCBotQuitMsg,
        IRCBotNetwork,
        IRCBotPort,
        IRCBotChannels,
        IRCBotForwardFromServer,
        IRCBotForwardFromIRC,

        PolicyColorCodesInChat,
        PolicyIllegalCharacters,
        SendRedundantBlockUpdates,
        PingInterval,
        AutomaticUpdates,
        NoPartialPositionUpdates,
        ProcessPriority,
        RunOnStartup,
        BlockUpdateThrottling,
        TickInterval,
        LowLatencyMode
    }


    public static class Config {
        public static string ServerURL;
        public const int HeartbeatDelay = 50000;

        public const int ProtocolVersion = 7;
        public const int ConfigVersion = 100;
        public const int MaxPlayersSupported = 256;
        public const string ConfigRootName = "fCraftConfig",
                            ConfigFile = "config.xml";
        static Dictionary<ConfigKey, string> settings = new Dictionary<ConfigKey, string>();

        public static string errors = ""; // for ConfigTool
        public static bool logToString = false;

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

        public static void LoadDefaultsGeneral() {
            settings[ConfigKey.ServerName] = "Minecraft custom server (fCraft)";
            settings[ConfigKey.MOTD] = "Welcome to the server!";
            settings[ConfigKey.MaxPlayers] = "16";
            settings[ConfigKey.DefaultClass] = ""; // empty = lowest rank
            settings[ConfigKey.IsPublic] = "false";
            settings[ConfigKey.Port] = "25565";
            settings[ConfigKey.UploadBandwidth] = "100";

            settings[ConfigKey.ClassColorsInChat] = "true";
            settings[ConfigKey.ClassPrefixesInChat] = "false";
            settings[ConfigKey.ClassPrefixesInList] = "false";
            settings[ConfigKey.SystemMessageColor] = "yellow";
            settings[ConfigKey.HelpColor] = "purple";
            settings[ConfigKey.SayColor] = "yellow";
        }


        public static void LoadDefaultsSecurity() {
            settings[ConfigKey.VerifyNames] = "Balanced"; // can be "Always," "Balanced," or "Never"
            settings[ConfigKey.AnnounceUnverifiedNames] = "True";
            settings[ConfigKey.LimitOneConnectionPerIP] = "False";

            settings[ConfigKey.AntispamMessageCount] = "4";
            settings[ConfigKey.AntispamInterval] = "5";
            settings[ConfigKey.AntispamMuteDuration] = "5";
            settings[ConfigKey.AntispamMaxWarnings] = "2";

            settings[ConfigKey.AntigriefBlockCount] = "35";
            settings[ConfigKey.AntigriefInterval] = "5";
        }


        public static void LoadDefaultsSavingAndBackup() {
            settings[ConfigKey.SaveOnShutdown] = "true";
            settings[ConfigKey.SaveInterval] = "60"; // 0 = no auto save

            settings[ConfigKey.BackupOnStartup] = "false";
            settings[ConfigKey.BackupOnJoin] = "false";
            settings[ConfigKey.BackupOnlyWhenChanged] = "true";
            settings[ConfigKey.BackupInterval] = "20"; // 0 = no auto backup
            settings[ConfigKey.MaxBackups] = "100"; // 0 = no backup file count limit
            settings[ConfigKey.MaxBackupSize] = "0"; // 0 = no backup file size count limit
        }

        public static void LoadDefaultsLogging() {
            settings[ConfigKey.LogMode] = "OneFile"; // can be: "None", "OneFile", "SplitBySession", "SplitByDay"
            settings[ConfigKey.MaxLogs] = "0";
            for( int i = 0; i < Logger.consoleOptions.Length; i++ ) {
                Logger.consoleOptions[i] = true;
            }
            Logger.consoleOptions[(int)LogType.ConsoleInput] = false;
            Logger.consoleOptions[(int)LogType.Debug] = false;
            for( int i = 0; i < Logger.logFileOptions.Length; i++ ) {
                Logger.logFileOptions[i] = true;
            }
        }


        public static void LoadDefaultsIRC() {
            settings[ConfigKey.IRCBot] = "false"; // Bot is disabled by default
            settings[ConfigKey.IRCMsgs] = "false"; // Join/quit messages disabled by default
            settings[ConfigKey.IRCBotNick] = "fBot";
            settings[ConfigKey.IRCBotQuitMsg] = "I've been told to go offline now.";
            settings[ConfigKey.IRCBotNetwork] = "irc.esper.net";
            settings[ConfigKey.IRCBotPort] = "6667";
            settings[ConfigKey.IRCBotChannels] = "#changeme"; // CASE SENSITIVE!!!!!!!!!!!!!!!!!!!!! This can be multiple using csv
            settings[ConfigKey.IRCBotForwardFromServer] = "false"; // Disabled by default
            settings[ConfigKey.IRCBotForwardFromIRC] = "false"; // Disabled by default
        }


        public static void LoadDefaultsAdvanced() {
            settings[ConfigKey.PolicyColorCodesInChat] = "ConsoleOnly"; // can be: "Allow", "ConsoleOnly", "Disallow"
            settings[ConfigKey.PolicyIllegalCharacters] = "Disallow"; // can be: "Allow", "ConsoleOnly", "Disallow"
            settings[ConfigKey.SendRedundantBlockUpdates] = "false";
            settings[ConfigKey.PingInterval] = "0"; // 0 = ping disabled
            settings[ConfigKey.AutomaticUpdates] = "Prompt"; // can be "Disabled", "Notify", "Prompt", and "Auto"
            settings[ConfigKey.NoPartialPositionUpdates] = "false";
            settings[ConfigKey.ProcessPriority] = "";
            settings[ConfigKey.RunOnStartup] = "Never"; // can be "Always", "OnUnexpectedShutdown", or "Never"
            settings[ConfigKey.BlockUpdateThrottling] = "2500";
            settings[ConfigKey.TickInterval] = "100";
            settings[ConfigKey.LowLatencyMode] = "false";
        }


        public static void LoadDefaults() {
            //locker.EnterWriteLock();
            settings.Clear();
            LoadDefaultsGeneral();
            LoadDefaultsSecurity();
            LoadDefaultsSavingAndBackup();
            LoadDefaultsLogging();
            LoadDefaultsIRC();
            LoadDefaultsAdvanced();
            //locker.ExitWriteLock();
        }


        public static bool Load() {
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
            int version;
            if( fromFile && (attr == null || !Int32.TryParse( attr.Value, out version ) || version != ConfigVersion) ) {
                Log( "Config.Load: Your config.xml was made for a different version of fCraft. " +
                    "Some obsolete settings might be ignored, and some recently-added settings will be set to their default values. " +
                    "It is recommended that you run ConfigTool to make sure everything is in order.", LogType.Warning );
            }


            XElement classList = config.Element( "Classes" );
            if( classList != null ) {
                foreach( XElement playerClass in classList.Elements( "PlayerClass" ) ) {
                    if( !DefineClass( playerClass ) ) {
                        Log( "Config.Load: Could not parse one of the class definitions.", LogType.Warning );
                    }
                }
                if( ClassList.classes.Count == 0 ) {
                    Log( "Config.Load: No classes were defined, or none were defined correctly. Using default player classes.", LogType.Warning );
                    config.Add( DefineDefaultClasses() );
                }
            } else {
                if( fromFile ) Log( "Config.Load: using default player classes.", LogType.Warning );
                config.Add( DefineDefaultClasses() );
            }

            // parse rank-limit permissions
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                if( !ClassList.ParseClassLimits( pc ) ) {
                    Log( "Could not parse one of the rank-limits for kick, ban, promote, and/or demote permissions for {0}. " +
                         "Any unrecognized limits were reset to default (own class).", LogType.Warning, pc.name );
                }
            }

            XElement consoleOptions = config.Element( "ConsoleOptions" );
            if( consoleOptions != null ) {
                ParseLogOptions( consoleOptions, ref Logger.consoleOptions );
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
                ParseLogOptions( logFileOptions, ref Logger.logFileOptions );
            } else {
                if( fromFile ) Log( "Config.Load: using default log file options.", LogType.Warning );
                for( int i = 0; i < Logger.logFileOptions.Length; i++ ) {
                    Logger.logFileOptions[i] = true;
                }
            }

            // Load config
            string[] keyNames = Enum.GetNames(typeof(ConfigKey));
            foreach( XElement element in config.Elements() ) {
                if( keyNames.Contains<string>( element.Name.ToString() ) ) {
                    // known key
                    SetValue( (ConfigKey)Enum.Parse(typeof(ConfigKey), element.Name.ToString()), element.Value );
                } else if( element.Name.ToString() != "ConsoleOptions" &&
                    element.Name.ToString() != "LogFileOptions" &&
                    element.Name.ToString() != "Classes" ) {
                    // unknown key
                    Log( "Unrecognized entry ignored: {0} = {1}", LogType.Debug, element.Name, element.Value );
                    //TODO: custom settings store
                    //settings.Add( element.Name.ToString(), element.Value );
                }
            }
            return true;
        }


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

            XElement classesTag = new XElement( "Classes" );
            foreach( PlayerClass playerClass in ClassList.classes.Values ) {
                XElement classTag = new XElement( "PlayerClass" );
                classTag.Add( new XAttribute( "name", playerClass.name ) );
                classTag.Add( new XAttribute( "rank", playerClass.rank ) );
                classTag.Add( new XAttribute( "color", Color.GetName( playerClass.color ) ) );
                if( playerClass.prefix.Length > 0 ) classTag.Add( new XAttribute( "prefix", playerClass.prefix ) );
                if( playerClass.spamKickThreshold > 0 ) classTag.Add( new XAttribute( "spamKickAt", playerClass.spamKickThreshold ) );
                if( playerClass.spamBanThreshold > 0 ) classTag.Add( new XAttribute( "spamBanAt", playerClass.spamBanThreshold ) );
                if( playerClass.idleKickTimer > 0 ) classTag.Add( new XAttribute( "idleKickAfter", playerClass.idleKickTimer ) );
                if( playerClass.reservedSlot ) classTag.Add( new XAttribute( "reserveSlot", playerClass.reservedSlot ) );
                XElement temp;
                for( int i = 0; i < Enum.GetValues( typeof( Permissions ) ).Length; i++ ) {
                    if( playerClass.permissions[i] ) {
                        temp = new XElement( ((Permissions)i).ToString() );
                        if( i == (int)Permissions.Ban && playerClass.maxBan != null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxBan.name ) );
                        } else if( i == (int)Permissions.Kick && playerClass.maxKick != null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxKick.name ) );
                        } else if( i == (int)Permissions.Promote && playerClass.maxPromote != null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxPromote.name ) );
                        } else if( i == (int)Permissions.Demote && playerClass.maxDemote != null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxDemote.name ) );
                        }
                        classTag.Add( temp );
                    }
                }
                classesTag.Add( classTag );
            }
            config.Add( classesTag );


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


        static void ParseLogOptions( XElement el, ref bool[] list ) {
            for( int i = 0; i < 13; i++ ) {
                if( el.Element( ((LogType)i).ToString() ) != null ) {
                    list[i] = true;
                } else {
                    list[i] = false;
                }
            }
        }


        internal static void ApplyConfig() {
            // TODO: logging settings
            //Logger.Threshold = (LogLevel)Enum.Parse( typeof( LogLevel ), settings[ConfigKey.LogThreshold"] );

            // chat colors
            Color.Sys = Color.Parse( settings[ConfigKey.SystemMessageColor] );
            Color.Say = Color.Parse( settings[ConfigKey.SayColor] );
            Color.Help = Color.Parse( settings[ConfigKey.HelpColor] );

            // default class
            if( ClassList.ParseClass( settings[ConfigKey.DefaultClass] ) != null ) {
                ClassList.defaultClass = ClassList.ParseClass( settings[ConfigKey.DefaultClass] );
            } else {
                ClassList.defaultClass = ClassList.lowestClass;
                Log( "Config.ParseConfig: No default player class defined; assuming that the lowest rank ({0}) is the default.",
                            LogType.Warning, ClassList.defaultClass.name );
            }

            Player.spamChatCount = GetInt( ConfigKey.AntispamMessageCount );
            Player.spamChatTimer = GetInt( ConfigKey.AntispamInterval );
            Player.spamBlockCount = GetInt( ConfigKey.AntigriefBlockCount );
            Player.spamBlockTimer = GetInt( ConfigKey.AntigriefInterval );
            Player.muteDuration = TimeSpan.FromSeconds( GetInt( ConfigKey.AntispamMuteDuration ) );

            Server.maxUploadSpeed = GetInt( ConfigKey.UploadBandwidth );
            Server.packetsPerSecond = GetInt( ConfigKey.BlockUpdateThrottling );
            Server.ticksPerSecond = 1000 / (float)GetInt( ConfigKey.TickInterval );
        }


        public static bool SetValue( ConfigKey key, string value ) {
            switch( key ) {
                case ConfigKey.ServerName:
                    return ValidateString( key, value, 1, 64 );
                case ConfigKey.MOTD:
                    return ValidateString( key, value, 0, 64 );
                case ConfigKey.MaxPlayers:
                    return ValidateInt( key, value, 1, MaxPlayersSupported );
                case ConfigKey.DefaultClass:
                    if( value != "" ) {
                        if( ClassList.ParseClass( value ) != null ) {
                            settings[key] = ClassList.ParseClass( value ).name;
                            return true;
                        } else {
                            Log( "DefaultClass could not be parsed. It should be either blank (indicating \"use lowest class\") or a valid class name", LogType.Warning );
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

                case ConfigKey.IRCBotNick:
                    return ValidateString( key, value, 1, 32 );
                //case "IRCBotNetwork":
                //case "IRCBotChannels": // don't bother validating network and channel list

                case ConfigKey.IsPublic:
                case ConfigKey.ClassColorsInChat:
                case ConfigKey.ClassPrefixesInChat:
                case ConfigKey.ClassPrefixesInList:
                case ConfigKey.SaveOnShutdown:
                case ConfigKey.BackupOnStartup:
                case ConfigKey.BackupOnJoin:
                case ConfigKey.BackupOnlyWhenChanged:
                case ConfigKey.SendRedundantBlockUpdates:
                case ConfigKey.NoPartialPositionUpdates:
                case ConfigKey.IRCBot:
                case ConfigKey.IRCBotForwardFromServer:
                case ConfigKey.IRCBotForwardFromIRC:
                    return ValidateBool( key, value );

                case ConfigKey.SystemMessageColor:
                case ConfigKey.HelpColor:
                case ConfigKey.SayColor:
                    return ValidateColor( key, value );


                case ConfigKey.VerifyNames:
                    return ValidateEnum( key, value, "Always", "Balanced", "Never" );
                case ConfigKey.AntispamMessageCount:
                    return ValidateInt( key, value, 2, 50 );
                case ConfigKey.AntispamInterval:
                    return ValidateInt( key, value, 0, 60 );
                case ConfigKey.AntigriefBlockCount:
                    return ValidateInt( key, value, 2, 500 );
                case ConfigKey.AntigriefInterval:
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
                    return ValidateEnum( key, value, "None", "OneFile", "SplitBySession", "SplitByDay" );
                case ConfigKey.MaxLogs:
                    return ValidateInt( key, value, 0, 100000 );

                case ConfigKey.PolicyColorCodesInChat:
                case ConfigKey.PolicyIllegalCharacters:
                    return ValidateEnum( key, value, "Allow", "ConsoleOnly", "Disallow" );
                case ConfigKey.ProcessPriority:
                    return ValidateEnum( key, value, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
                case ConfigKey.RunOnStartup:
                    return ValidateEnum( key, value, "Always", "OnUnexpectedShutdown", "Never" );
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
                    Log( "Config.SetValue: Specified value for {0} is not within valid range ({1}...{2}). Using default ({3}).", LogType.Warning,
                                        key, minRange, maxRange, settings[key] );
                }
                return true;
            } else {
                Log( "Config.SetValue: Specified value for {0} could not be parsed. Using default ({1}).", LogType.Warning,
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
                Log( "Config.SetValue: Specified value for {0} could not be parsed. Expected 'true' or 'false'. Using default ({1}).", LogType.Warning,
                                    key, settings[key] );
                return false;
            }
        }

        static bool ValidateColor( ConfigKey key, string value ) {
            if( Color.Parse( value ) != null ) {
                settings[key] = value;
                return true;
            } else {
                Log( "Config.SetValue: Specified value for {0} could not be parsed. Using default ({1}).", LogType.Warning,
                                    key, settings[key] );
                return false;
            }
        }

        static bool ValidateString( ConfigKey key, string value, int minLength, int maxLength ) {
            if( value.Length < minLength ) {
                Log( "Config.SetValue: Specified value for {0} is too short (expected length: {1}...{2}). Using default ({3}).", LogType.Warning,
                    key, minLength, maxLength, settings[key] );
                return false;
            } else if( value.Length > maxLength ) {
                settings[key] = value.Substring( 0, maxLength );
                Log( "Config.SetValue: Specified value for {0} is too long (expected length: {1}...{2}). The value has been truncated to \"{3}\".", LogType.Warning,
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


        public static string GetString( ConfigKey key ) {
            return settings[key];
        }

        public static int GetInt( ConfigKey key ) {
            return Int32.Parse( settings[key] );
        }

        public static bool GetBool( ConfigKey key ) {
            return Boolean.Parse( settings[key] );
        }

        public static void ResetClasses() {
            ClassList.classes = new Dictionary<string, PlayerClass>();
            ClassList.classesByIndex = new List<PlayerClass>();
            XElement classList = DefineDefaultClasses();
            foreach( XElement pc in classList.Elements() ) {
                DefineClass( pc );
            }
            // parse rank-limit permissions
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                ClassList.ParseClassLimits( pc );
            }
        }


        static XElement DefineDefaultClasses() {
            XElement temp;
            XElement permissions = new XElement( "Classes" );

            XElement guest = new XElement( "PlayerClass" );
            guest.Add( new XAttribute( "name", "guest" ) );
            guest.Add( new XAttribute( "rank", 0 ) );
            guest.Add( new XAttribute( "color", "silver" ) );
            guest.Add( new XAttribute( "prefix", "" ) );
            guest.Add( new XAttribute( "spamKickAt", 0 ) );
            guest.Add( new XAttribute( "spamBanAt", 0 ) );
            guest.Add( new XAttribute( "idleKickAfter", 20 ) );
            guest.Add( new XElement( "Chat" ) );
            guest.Add( new XElement( "Build" ) );
            guest.Add( new XElement( "Delete" ) );
            permissions.Add( guest );
            DefineClass( guest );


            XElement regular = new XElement( "PlayerClass" );
            regular.Add( new XAttribute( "name", "regular" ) );
            regular.Add( new XAttribute( "rank", 30 ) );
            regular.Add( new XAttribute( "color", "white" ) );
            regular.Add( new XAttribute( "prefix", "" ) );
            regular.Add( new XAttribute( "spamKickAt", 0 ) );
            regular.Add( new XAttribute( "spamBanAt", 0 ) );
            regular.Add( new XAttribute( "idleKickAfter", 20 ) );

            regular.Add( new XElement( "Chat" ) );
            regular.Add( new XElement( "Build" ) );
            regular.Add( new XElement( "Delete" ) );

            regular.Add( new XElement( "PlaceGrass" ) );
            regular.Add( new XElement( "PlaceWater" ) );
            regular.Add( new XElement( "PlaceLava" ) );
            regular.Add( new XElement( "PlaceAdmincrete" ) );
            regular.Add( new XElement( "DeleteAdmincrete" ) );

            temp = new XElement( "Kick" );
            temp.Add( new XAttribute( "max", "regular" ) );
            regular.Add( temp );

            regular.Add( new XElement( "ViewOthersInfo" ) );

            regular.Add( new XElement( "Teleport" ) );
            permissions.Add( regular );
            DefineClass( regular );


            XElement op = new XElement( "PlayerClass" );
            op.Add( new XAttribute( "name", "op" ) );
            op.Add( new XAttribute( "rank", 80 ) );
            op.Add( new XAttribute( "color", "aqua" ) );
            op.Add( new XAttribute( "prefix", "-" ) );
            op.Add( new XAttribute( "spamKickAt", 0 ) );
            op.Add( new XAttribute( "spamBanAt", 0 ) );
            op.Add( new XAttribute( "idleKickAfter", 0 ) );

            op.Add( new XElement( "Chat" ) );
            op.Add( new XElement( "Build" ) );
            op.Add( new XElement( "Delete" ) );

            op.Add( new XElement( "PlaceGrass" ) );
            op.Add( new XElement( "PlaceWater" ) );
            op.Add( new XElement( "PlaceLava" ) );
            op.Add( new XElement( "PlaceAdmincrete" ) );
            op.Add( new XElement( "DeleteAdmincrete" ) );
            op.Add( new XElement( "PlaceHardenedBlocks" ) );

            op.Add( new XElement( "Say" ) );
            temp = new XElement( "Kick" );
            temp.Add( new XAttribute( "max", "op" ) );
            op.Add( temp );
            temp = new XElement( "Ban" );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            op.Add( new XElement( "BanIP" ) );

            temp = new XElement( "Promote" );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            temp = new XElement( "Demote" );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            op.Add( new XElement( "Hide" ) );
            op.Add( new XElement( "ChangeName" ) );

            op.Add( new XElement( "ViewOthersInfo" ) );

            op.Add( new XElement( "Teleport" ) );
            op.Add( new XElement( "Bring" ) );
            op.Add( new XElement( "Freeze" ) );
            op.Add( new XElement( "SetSpawn" ) );

            op.Add( new XElement( "Draw" ) );
            permissions.Add( op );
            DefineClass( op );


            XElement owner = new XElement( "PlayerClass" );
            owner.Add( new XAttribute( "name", "owner" ) );
            owner.Add( new XAttribute( "rank", 100 ) );
            owner.Add( new XAttribute( "color", "red" ) );
            owner.Add( new XAttribute( "prefix", "+" ) );
            owner.Add( new XAttribute( "spamKickAt", 0 ) );
            owner.Add( new XAttribute( "spamBanAt", 0 ) );
            owner.Add( new XAttribute( "idleKickAfter", 0 ) );

            owner.Add( new XElement( "Chat" ) );
            owner.Add( new XElement( "Build" ) );
            owner.Add( new XElement( "Delete" ) );

            owner.Add( new XElement( "PlaceGrass" ) );
            owner.Add( new XElement( "PlaceWater" ) );
            owner.Add( new XElement( "PlaceLava" ) );
            owner.Add( new XElement( "PlaceAdmincrete" ) );
            owner.Add( new XElement( "DeleteAdmincrete" ) );
            owner.Add( new XElement( "PlaceHardenedBlocks" ) );

            owner.Add( new XElement( "Say" ) );
            temp = new XElement( "Kick" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            temp = new XElement( "Ban" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            owner.Add( new XElement( "BanIP" ) );
            owner.Add( new XElement( "BanAll" ) );

            temp = new XElement( "Promote" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            temp = new XElement( "Demote" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            owner.Add( new XElement( "Hide" ) );
            owner.Add( new XElement( "ChangeName" ) );

            owner.Add( new XElement( "ViewOthersInfo" ) );

            owner.Add( new XElement( "Teleport" ) );
            owner.Add( new XElement( "Bring" ) );
            owner.Add( new XElement( "Freeze" ) );
            owner.Add( new XElement( "SetSpawn" ) );

            owner.Add( new XElement( "Lock" ) );
            owner.Add( new XElement( "ControlPhysics" ) );
            owner.Add( new XElement( "AddLandmarks" ) );

            owner.Add( new XElement( "ManageZones" ) );
            owner.Add( new XElement( "ManageWorlds" ) );

            owner.Add( new XElement( "Draw" ) );
            permissions.Add( owner );
            DefineClass( owner );


            return permissions;
        }


        static bool DefineClass( XElement el ) {
            PlayerClass playerClass = new PlayerClass();

            // read required attributes
            XAttribute attr = el.Attribute( "name" );
            if( attr == null ) {
                Log( "Config.DefineClass: Class definition with no name was ignored.", LogType.Warning );
                return false;
            }
            if( !PlayerClass.IsValidClassName( attr.Value.Trim() ) ) {
                Log( "Config.DefineClass: Invalid name specified for class \"{0}\". Class name can only contain letters, digits, and underscores.",
                     LogType.Warning, playerClass.name );
                return false;
            }
            playerClass.name = attr.Value.Trim();

            if( ClassList.classes.ContainsKey( playerClass.name ) ) {
                Log( "Config.DefineClass: Duplicate class definition for \"{0}\" was ignored.", LogType.Warning, playerClass.name );
                return true;
            }


            if( (attr = el.Attribute( "rank" )) == null ) {
                Log( "Config.DefineClass: No rank specified for {0}. Class definition was ignored.", LogType.Warning, playerClass.name );
                return false;
            }
            if( !Byte.TryParse( attr.Value, out playerClass.rank ) ) {
                Log( "Config.DefineClass: Cannot parse rank for {0}. Class definition was ignored.", LogType.Warning, playerClass.name );
                return false;
            }

            attr = el.Attribute( "color" );
            if( attr == null || Color.Parse( attr.Value ) == null ) {
                playerClass.color = "";
            } else {
                playerClass.color = Color.Parse( attr.Value );
            }

            // read optional attributes
            if( (attr = el.Attribute( "prefix" )) != null ) {
                if( PlayerClass.IsValidPrefix( attr.Value ) ) {
                    playerClass.prefix = attr.Value;
                } else {
                    Log( "Config.DefineClass: Invalid prefix specified for {0}.", LogType.Warning, playerClass.name );
                    playerClass.prefix = "";
                }
            }

            if( (attr = el.Attribute( "spamKickAt" )) != null ) {
                if( !Int32.TryParse( attr.Value, out playerClass.spamKickThreshold ) ) {
                    Log( "Config.DefineClass: Could not parse the value for spamKickAt for {0}. Assuming 0 (never).", LogType.Warning, playerClass.name );
                    playerClass.spamKickThreshold = 0;
                }
            } else {
                playerClass.spamKickThreshold = 0;
            }

            if( (attr = el.Attribute( "spamBanAt" )) != null ) {
                if( !Int32.TryParse( attr.Value, out playerClass.spamBanThreshold ) ) {
                    Log( "Config.DefineClass: Could not parse the value for spamBanAt for {0}. Assuming 0 (never).", LogType.Warning, playerClass.name );
                    playerClass.spamBanThreshold = 0;
                }
            } else {
                playerClass.spamBanThreshold = 0;
            }

            if( (attr = el.Attribute( "idleKickAfter" )) != null ) {
                if( !Int32.TryParse( attr.Value, out playerClass.idleKickTimer ) ) {
                    Log( "Config.DefineClass: Could not parse the value for idleKickAfter for {0}. Assuming 0 (never).", LogType.Warning, playerClass.name );
                    playerClass.idleKickTimer = 0;
                }
            } else {
                playerClass.idleKickTimer = 0;
            }

            if( (attr = el.Attribute( "reserveSlot" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out playerClass.reservedSlot ) ) {
                    Log( "Config.DefineClass: Could not parse the value for reserveSlot for {0}. Assuming \"false\".", LogType.Warning, playerClass.name );
                    playerClass.reservedSlot = false;
                }
            } else {
                playerClass.reservedSlot = false;
            }

            // read permissions
            XElement temp;
            for( int i = 0; i < Enum.GetValues( typeof( Permissions ) ).Length; i++ ) {
                string permission = ((Permissions)i).ToString();
                if( (temp = el.Element( permission )) != null ) {
                    playerClass.permissions[i] = true;
                    if( i == (int)Permissions.Promote ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxPromoteVal = attr.Value;
                        } else {
                            playerClass.maxPromoteVal = "";
                        }
                    } else if( i == (int)Permissions.Demote ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxDemoteVal = attr.Value;
                        } else {
                            playerClass.maxDemoteVal = "";
                        }
                    } else if( i == (int)Permissions.Kick ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxKickVal = attr.Value;
                        } else {
                            playerClass.maxKickVal = "";
                        }
                    } else if( i == (int)Permissions.Ban ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxBanVal = attr.Value;
                        } else {
                            playerClass.maxBanVal = "";
                        }
                    }
                }
            }

            // check for consistency in ban permissions
            if( !playerClass.Can( Permissions.Ban ) &&
                (playerClass.Can( Permissions.BanAll ) || playerClass.Can( Permissions.BanIP )) ) {
                Log( "Class \"{0}\" is allowed to BanIP and/or BanAll but not allowed to Ban.\n" +
                    "Assuming that all ban permissions were ment to be off.", LogType.Warning, playerClass.name );
                playerClass.permissions[(int)Permissions.BanIP] = false;
                playerClass.permissions[(int)Permissions.BanAll] = false;
            }

            ClassList.AddClass( playerClass );
            return true;
        }


        public static ProcessPriorityClass GetBasePriority() {
            switch( GetString( ConfigKey.ProcessPriority ) ) {
                case "High": return System.Diagnostics.ProcessPriorityClass.High;
                case "AboveNormal": return System.Diagnostics.ProcessPriorityClass.AboveNormal;
                case "BelowNormal": return System.Diagnostics.ProcessPriorityClass.BelowNormal;
                case "Low": return System.Diagnostics.ProcessPriorityClass.Idle;
                default: return System.Diagnostics.ProcessPriorityClass.Normal;
            }
        }
    }
}