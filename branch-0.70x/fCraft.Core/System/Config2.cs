using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using JetBrains.Annotations;
using fCraft.Events;

namespace fCraft {
    public static class Config2 {
        /// <summary> Latest (current) version of the configuration file.
        /// Versions before 200 were XML, versions 200+ are JSON. </summary>
        public const int CurrentVersion = 200;
        public const int LowestSupportedVersion = 200;

        // Mapping of keys to their values.
        static readonly string[] Values;

        // Cache of boolean keys
        static readonly bool[] SettingsEnabledCache; // cached .Enabled() calls
        static readonly bool[] SettingsUseEnabledCache; // cached .Enabled() calls

        // Mapping of keys to their metadata containers.
        static readonly ConfigKeyAttribute[] KeyMetadata;

        // Keys organized by sections
        static readonly Dictionary<ConfigSection, ConfigKey[]> KeySections = new Dictionary<ConfigSection, ConfigKey[]>();


        static Config2() {
            int keyCount = Enum.GetValues( typeof( ConfigKey ) ).Length;
            Values = new string[keyCount];
            SettingsEnabledCache = new bool[keyCount];
            SettingsUseEnabledCache = new bool[keyCount];
            KeyMetadata = new ConfigKeyAttribute[keyCount];

            // gather metadata for ConfigKeys
            foreach( var keyField in typeof( ConfigKey ).GetFields() ) {
                foreach( var attribute in (ConfigKeyAttribute[])keyField.GetCustomAttributes( typeof( ConfigKeyAttribute ), false ) ) {
                    ConfigKey key = (ConfigKey)keyField.GetValue( null );
                    attribute.Key = key;
                    KeyMetadata[(int)key] = attribute;
                }
            }

            // organize ConfigKeys into categories, based on metadata
            foreach( ConfigSection section in Enum.GetValues( typeof( ConfigSection ) ) ) {
                ConfigSection sec = section;
                KeySections.Add( section, KeyMetadata.Where( meta => (meta.Section == sec) )
                                                     .Select( meta => meta.Key )
                                                     .ToArray() );
            }

            LoadDefaults();
        }


#if DEBUG
        // Makes sure that defaults and metadata containers are set.
        // This is invoked by Server.InitServer() if built with DEBUG flag.
        internal static void RunSelfTest() {
            foreach( ConfigKey key in Enum.GetValues( typeof( ConfigKey ) ) ) {
                if( Values[(int)key] == null ) {
                    throw new Exception( "One of the ConfigKey keys is null: " + key );
                }

                if( KeyMetadata[(int)key] == null ) {
                    throw new Exception( "One of the ConfigKey keys does not have metadata set: " + key );
                }
            }
        }
#endif


        #region Defaults

        /// <summary> Overwrites current settings with defaults. </summary>
        public static void LoadDefaults() {
            for( int i = 0; i < KeyMetadata.Length; i++ ) {
                ( (ConfigKey)i ).SetValue( KeyMetadata[i].DefaultValue );
            }
        }


        /// <summary> Loads defaults for keys in a given ConfigSection. </summary>
        public static void LoadDefaults( ConfigSection section ) {
            foreach( var key in KeySections[section] ) {
                key.SetValue( KeyMetadata[(int)key].DefaultValue );
            }
        }


        /// <summary> Checks whether given ConfigKey still has its default value. </summary>
        public static bool IsDefault( this ConfigKey key ) {
            return KeyMetadata[(int)key].IsDefault( Values[(int)key] );
        }


        /// <summary> Checks whether given ConfigKey still has its default value. </summary>
        public static bool IsDefault( this ConfigKey key, string value ) {
            return KeyMetadata[(int)key].IsDefault( value );
        }


        /// <summary> Provides the default value for a given ConfigKey. </summary>
        public static string GetDefault( this ConfigKey key ) {
            return KeyMetadata[(int)key].DefaultValue;
        }

        public static void ResetLogOptions() {
            for( int i = 0; i < Logger.ConsoleOptions.Length; i++ ) {
                Logger.ConsoleOptions[i] = true;
                Logger.LogFileOptions[i] = true;
            }
            Logger.ConsoleOptions[(int)LogType.ConsoleInput] = false;
            Logger.ConsoleOptions[(int)LogType.Debug] = false;
        }

        #endregion


        #region Loading

        /// <summary> Whether Config has been loaded. If true, calling Config.Load() again will fail. </summary>
        public static bool IsLoaded { get; private set; }


        /// <summary> Loads configuration from file. </summary>
        public static void Load() {
            if( IsLoaded ) {
                throw new InvalidOperationException( "Config is already loaded. Use Config.Reload instead." );
            }
            Load( false, true );
            IsLoaded = true;
        }


        /// <summary> Reloads configuration from file. Raises ConfigReloaded event. </summary>
        public static void Reload( bool loadRankList ) {
            Load( true, loadRankList );
        }


        static void Load( bool reloading, bool loadRankList ) {
            JsonObject root;
            bool fromFile;

            // load file
            if( File.Exists( Paths.ConfigFileName ) ) {
                try {
                    string raw = File.ReadAllText( Paths.ConfigFileName );
                    root = new JsonObject( raw );
                    fromFile = true;
                } catch( SerializationException ex ) {
                    string errorMsg = "Config.Load: Config file is not properly formatted: " + ex.Message;
                    throw new MisconfigurationException( errorMsg, ex );
                }
            } else {
                Logger.Log( LogType.Warning,
                            "Config.Load: Config file not found; using defaults." );
                root = new JsonObject();
                fromFile = false;
            }

            // detect version number
            if( root.HasInt( "Version" ) ) {
                int version = root.GetInt( "Version" );
                if( version < LowestSupportedVersion ) {
                    Logger.Log( LogType.Warning,
                                "Config.Load: Your config file is too old to be loaded properly. " +
                                "Some settings will be lost or replaced with defaults. " +
                                "Please run ConfigGUI/ConfigCLI to make sure that everything is in order." );
                } else if( version != CurrentVersion ) {
                    Logger.Log( LogType.Warning,
                                "Config.Load: Your config.xml was made for a different version of fCraft. " +
                                "Some obsolete settings might be ignored, and some recently-added settings will be set to defaults. " +
                                "It is recommended that you run ConfigGUI/ConfigCLI to make sure that everything is in order." );
                }
            } else if( fromFile ) {
                Logger.Log( LogType.Warning,
                            "Config.Load: Version number missing from config file. It might be corrupted. " +
                            "Please run ConfigGUI/ConfigCLI to make sure that everything is in order." );
            }

            // read rank definitions
            if( loadRankList ) {
                RankManager.Reset();
                // TODO: LoadRankList( config, fromFile );
            }

            // load log options
            ResetLogOptions();
            if( root.HasArray( "ConsoleOptions" ) ) {
                ReadLogOptions( root, "ConsoleOptions", Logger.ConsoleOptions );
            } else if( fromFile ) {
                Logger.Log( LogType.Warning, "Config.Load: Using default console options." );
            }
            if( root.HasArray( "LogFileOptions" ) ) {
                ReadLogOptions( root, "LogFileOptions", Logger.LogFileOptions );
            } else if( fromFile ) {
                Logger.Log( LogType.Warning, "Config.Load: Using default log file options." );
            }

            // load normal config keys
            JsonObject settings;
            if( root.TryGetObject( "Settings", out settings ) ) {
                foreach( var kvp in settings ) {
                    ReadSetting( kvp.Key, kvp.Value );
                }
            }

            // apply rank-related settings
            if( !reloading ) {
                RankManager.DefaultRank = Rank.Parse( ConfigKey.DefaultRank.GetString() );
                RankManager.DefaultBuildRank = Rank.Parse( ConfigKey.DefaultBuildRank.GetString() );
                RankManager.PatrolledRank = Rank.Parse( ConfigKey.PatrolledRank.GetString() );
                RankManager.BlockDBAutoEnableRank = Rank.Parse( ConfigKey.BlockDBAutoEnableRank.GetString() );
            }

            // read PlayerDB settings
            JsonObject playerDBSettings;
            if( !reloading && root.TryGetObject( "PlayerDB", out playerDBSettings ) ) {
                ReadPlayerDBSettings( playerDBSettings );
            }

            // key relation validation
            if( ConfigKey.MaxPlayersPerWorld.GetInt() > ConfigKey.MaxPlayers.GetInt() ) {
                Logger.Log( LogType.Warning,
                            "Value of MaxPlayersPerWorld ({0}) was lowered to match MaxPlayers ({1}).",
                            ConfigKey.MaxPlayersPerWorld.GetInt(),
                            ConfigKey.MaxPlayers.GetInt() );
                ConfigKey.MaxPlayersPerWorld.TrySetValue( ConfigKey.MaxPlayers.GetInt() );
            }

            // raise Config.Reloaded, if applicable
            if( reloading ) RaiseReloadedEvent();
        }


        static void ReadLogOptions( JsonObject root, string propertyName, bool[] destination ) {
            try {
                string[] optionNames = root.GetArray<string>( propertyName );
                for( int i = 0; i < destination.Length; i++ ) {
                    destination[i] = optionNames.Contains( ( (LogType)i ).ToString() );
                }
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "Config.LoadLogOptions: Could not load {0}: {1}",
                            propertyName, ex );
            }
        }


        static void ReadSetting( string keyName, object rawObj ) {
            try {
                JsonObject obj = (JsonObject)rawObj;
                string value = obj.GetString( "Value" );

                ConfigKey key;
                if( !Enum.TryParse( keyName, true, out key ) ) {
                    // unknown key
                    Logger.Log( LogType.Warning,
                                "Config: Unrecognized key ignored: {0} = {1}",
                                keyName, value );
                }

                string oldDefault;
                if( obj.TryGetString( "Default", out oldDefault ) ) {
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

                key.TrySetValue( value );

            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "Config.LoadLogOptions: Could not load \"{0}\" setting: {1}",
                            keyName, ex );
            }
        }


        static void ReadPlayerDBSettings( JsonObject obj ) {
            PlayerDBProviderType providerType = PlayerDBProviderType.Flatfile;
            if( !obj.Has( "Engine" ) ) {
                Logger.Log( LogType.Warning,
                            "Config.Load: No PlayerDBProvider specified in config. Assuming default (flatfile)." );
            } else if( !obj.TryGetEnum( "Engine", true, out providerType ) ) {
                providerType = PlayerDBProviderType.Flatfile;
                Logger.Log( LogType.Error,
                            "Config.Load: Unknown PlayerDB engine: {0}. Assuming default (Flatfile).",
                            obj.Get( "Engine" ) );
            }
            PlayerDB.ProviderType = providerType;
            JsonObject settings = null;
            if( obj.Has( "Settings" ) && !obj.TryGetObject( "Settings", out settings ) ) {
                Logger.Log( LogType.Error,
                            "Config.Load: Could not parse PlayerDB engine settings." );
            }
            PlayerDB.ProviderSettings = settings;
        }

        #endregion


        #region Getters

        /// <summary> Checks whether any value has been set for a given key. </summary>
        public static bool IsBlank( this ConfigKey key ) {
            return ( Values[(int)key].Length == 0 );
        }


        /// <summary> Returns normalized string value for the given key. </summary>
        public static string GetString( this ConfigKey key ) {
            return KeyMetadata[(int)key].GetUsableString( Values[(int)key] );
        }

        /// <summary> Returns normalized string value for the given key. </summary>
        public static string GetString( this ConfigKey key, string value ) {
            return KeyMetadata[(int)key].GetUsableString( value );
        }


        /// <summary> Returns nicely formatted string (but not necessarily parsable) value for the given key. </summary>
        public static string GetPresentationString( this ConfigKey key ) {
            return KeyMetadata[(int)key].GetPresentationString( Values[(int)key] );
        }

        /// <summary> Returns nicely formatted string (but not necessarily parsable) value for the given key. </summary>
        public static string GetPresentationString( this ConfigKey key, string value ) {
            return KeyMetadata[(int)key].GetPresentationString( value );
        }


        /// <summary> Returns raw string value for the given key (the value straight from config.xml) </summary>
        public static string GetRawString( this ConfigKey key ) {
            return Values[(int)key];
        }


        /// <summary> Attempts to parse given key's value as an integer. </summary>
        /// <exception cref="T:System.FormatException" />
        public static int GetInt( this ConfigKey key ) {
            return Int32.Parse( GetString( key ) );
        }


        /// <summary> Attempts to parse a given key's value as an integer. </summary>
        /// <param name="key"> ConfigKey to get value from. </param>
        /// <param name="result"> Will be set to the value on success, or to 0 on failure. </param>
        /// <returns> Whether parsing succeeded. </returns>
        public static bool TryGetInt( this ConfigKey key, out int result ) {
            return Int32.TryParse( GetString( key ), out result );
        }


        /// <summary> Attempts to parse a given key's value as an enumeration.
        /// An ArgumentException is thrown if value could not be parsed.
        /// Note the parsing is done in a case-insensitive way. </summary>
        /// <typeparam name="TEnum"> Enum to use for parsing.
        /// An ArgumentException will be thrown if this is not an enum. </typeparam>
        public static TEnum GetEnum<TEnum>( this ConfigKey key ) where TEnum : struct {
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required" );
            return (TEnum)Enum.Parse( typeof( TEnum ), GetString( key ), true );
        }


        /// <summary> Attempts to parse given key's value as a boolean. </summary>
        /// <exception cref="T:System.FormatException" />
        public static bool Enabled( this ConfigKey key ) {
            if( SettingsUseEnabledCache[(int)key] ) {
                return SettingsEnabledCache[(int)key];
            } else {
                return Boolean.Parse( GetString( key ) );
            }
        }


        /// <summary> Attempts to parse a given key's value as a boolean. </summary>
        /// <param name="key"> ConfigKey to get value from. </param>
        /// <param name="result"> Will be set to the value on success, or to false on failure. </param>
        /// <returns> Whether parsing succeeded. </returns>
        public static bool TryGetBool( this ConfigKey key, out bool result ) {
            if( SettingsUseEnabledCache[(int)key] ) {
                result = SettingsEnabledCache[(int)key];
                return true;
            } else {
                result = false;
                return false;
            }
        }


        /// <summary> Returns the expected Type of the key's value, as specified in key metadata. </summary>
        public static Type GetValueType( this ConfigKey key ) {
            return KeyMetadata[(int)key].ValueType;
        }


        /// <summary> Returns the ConfigSection that a given key is associated with. </summary>
        public static ConfigSection GetSection( this ConfigKey key ) {
            return KeyMetadata[(int)key].Section;
        }


        /// <summary> Returns the description text for a given config key. </summary>
        public static string GetDescription( this ConfigKey key ) {
            return KeyMetadata[(int)key].Description;
        }

        /// <summary> Returns whether given ConfigKey contains a Minecraft color. </summary>
        public static bool IsColor( this ConfigKey key ) {
            return KeyMetadata[(int)key].IsColor;
        }

        #endregion


        #region Setters

        /// <summary> Resets key value to its default setting. </summary>
        /// <param name="key"> Config key to reset. </param>
        /// <returns> True if value was reset. False if resetting was canceled by an event handler/plugin. </returns>
        public static bool ResetValue( this ConfigKey key ) {
            return key.TrySetValue( key.GetDefault() );
        }


        /// <summary> Sets value of a given config key.
        /// Note that this method may throw exceptions if the given value is not acceptable.
        /// Use Config.TrySetValue() if you'd like to suppress exceptions in favor of a boolean return value. </summary>
        /// <param name="key"> Config key to set. </param>
        /// <param name="rawValue"> Value to assign to the key. If passed object is not a string, rawValue.ToString() is used. </param>
        /// <returns> True if value is valid and has been assigned.
        /// False if value is valid, but assignment was canceled by an event handler/plugin. </returns>
        /// <exception cref="T:System.ArgumentNullException" />
        /// <exception cref="T:System.FormatException" />
        public static bool SetValue( this ConfigKey key, object rawValue ) {
            if( rawValue == null ) {
                throw new ArgumentNullException( "rawValue", key + ": ConfigKey values cannot be null. Use an empty string to indicate unset value." );
            }

            string value = ( rawValue as string ?? rawValue.ToString() );

            // throws various exceptions (most commonly FormatException) if invalid
            KeyMetadata[(int)key].Validate( value );

            return DoSetValue( key, value );
        }


        /// <summary> Attempts to set the value of a given config key.
        /// Check the return value to make sure that the given value was acceptable. </summary>
        /// <param name="key"> Config key to set. </param>
        /// <param name="rawValue"> Value to assign to the key. If passed object is not a string, rawValue.ToString() is used. </param>
        /// <exception cref="T:System.ArgumentNullException" />
        /// <returns> True if value is valid and has been assigned.
        /// False if value was invalid, or if assignment was canceled by an event callback. </returns>
        public static bool TrySetValue( this ConfigKey key, object rawValue ) {
            try {
                return SetValue( key, rawValue );
            } catch( FormatException ex ) {
                Logger.Log( LogType.Error,
                            "{0}.TrySetValue: {1}",
                            key, ex.Message );
                return false;
            }
        }


        static bool DoSetValue( ConfigKey key, string newValue ) {
            string oldValue = Values[(int)key];
            if( oldValue != newValue ) {
                if( !RaiseKeyChangingEvent( key, oldValue, ref newValue ) ) return false;
                Values[(int)key] = newValue;

                bool enabledCache;
                if( Boolean.TryParse( newValue, out enabledCache ) ) {
                    SettingsUseEnabledCache[(int)key] = true;
                    SettingsEnabledCache[(int)key] = enabledCache;
                } else {
                    SettingsUseEnabledCache[(int)key] = false;
                    SettingsEnabledCache[(int)key] = false;
                }

                ApplyKeyChange( key );
                RaiseKeyChangedEvent( key, oldValue, newValue );
            }
            return true;
        }


        static void ApplyKeyChange( ConfigKey key ) {
            switch( key ) {
                case ConfigKey.AnnouncementColor:
                    Color.Announcement = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.AntispamInterval:
                    Player.AntispamInterval = key.GetInt();
                    break;

                case ConfigKey.AntispamMessageCount:
                    Player.AntispamMessageCount = key.GetInt();
                    break;

                case ConfigKey.DefaultBuildRank:
                    RankManager.DefaultBuildRank = Rank.Parse( key.GetString() );
                    break;

                case ConfigKey.DefaultRank:
                    RankManager.DefaultRank = Rank.Parse( key.GetString() );
                    break;

                case ConfigKey.BandwidthUseMode:
                    Player[] playerListCache = Server.Players;
                    if( playerListCache != null ) {
                        foreach( Player p in playerListCache ) {
                            if( p.BandwidthUseMode == BandwidthUseMode.Default ) {
                                // resets the use tweaks
                                p.BandwidthUseMode = BandwidthUseMode.Default;
                            }
                        }
                    }
                    break;

                case ConfigKey.BlockDBAutoEnableRank:
                    RankManager.BlockDBAutoEnableRank = Rank.Parse( key.GetString() );
                    if( BlockDB.IsEnabledGlobally ) {
                        World[] worldListCache = WorldManager.Worlds;
                        foreach( World world in worldListCache ) {
                            if( world.BlockDB.AutoToggleIfNeeded() ) {
                                if( world.BlockDB.IsEnabled ) {
                                    Logger.Log( LogType.SystemActivity,
                                                "BlockDB is now auto-enabled on world {0}", world.Name );
                                } else {
                                    Logger.Log( LogType.SystemActivity,
                                                "BlockDB is now auto-disabled on world {0}", world.Name );
                                }
                            }
                        }
                    }
                    break;

                case ConfigKey.BlockUpdateThrottling:
                    Server.BlockUpdateThrottling = key.GetInt();
                    break;

                case ConfigKey.ConsoleName:
                    if( Player.Console != null ) {
                        Player.Console.Info.Name = key.GetString();
                    }
                    break;

                case ConfigKey.HelpColor:
                    Color.Help = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.IRCDelay:
                    IRC.SendDelay = key.GetInt();
                    break;

                case ConfigKey.IRCMessageColor:
                    Color.IRC = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.LogMode:
                    Logger.SplittingType = key.GetEnum<LogSplittingType>();
                    break;

                case ConfigKey.MapPath:
                    if( !Paths.IgnoreMapPathConfigKey && GetString( ConfigKey.MapPath ).Length > 0 ) {
                        if( Paths.TestDirectory( "MapPath", GetString( ConfigKey.MapPath ), true ) ) {
                            Paths.MapPath = Path.GetFullPath( GetString( ConfigKey.MapPath ) );
                        }
                    }
                    break;

                case ConfigKey.MaxUndo:
                    BuildingCommands.MaxUndoCount = key.GetInt();
                    break;

                case ConfigKey.MeColor:
                    Color.Me = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.NoPartialPositionUpdates:
                    if( key.Enabled() ) {
                        Player.FullPositionUpdateInterval = 0;
                    } else {
                        Player.FullPositionUpdateInterval = Player.FullPositionUpdateIntervalDefault;
                    }
                    break;

                case ConfigKey.PatrolledRank:
                    RankManager.PatrolledRank = Rank.Parse( key.GetString() );
                    break;

                case ConfigKey.PrivateMessageColor:
                    Color.PM = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.RelayAllBlockUpdates:
                    Player.RelayAllUpdates = key.Enabled();
                    break;

                case ConfigKey.SayColor:
                    Color.Say = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.SystemMessageColor:
                    Color.Sys = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.TickInterval:
                    Server.TicksPerSecond = 1000 / (float)key.GetInt();
                    break;

                case ConfigKey.UploadBandwidth:
                    Server.MaxUploadSpeed = key.GetInt();
                    break;

                case ConfigKey.WarningColor:
                    Color.Warning = Color.Parse( key.GetString() );
                    break;
            }
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
            var handler = Reloaded;
            if( handler != null ) handler( null, EventArgs.Empty );
        }


        static bool RaiseKeyChangingEvent( ConfigKey key, string oldValue, ref string newValue ) {
            var handler = KeyChanging;
            if( handler == null ) return true;
            var e = new ConfigKeyChangingEventArgs( key, oldValue, newValue );
            handler( null, e );
            newValue = e.NewValue;
            return !e.Cancel;
        }


        static void RaiseKeyChangedEvent( ConfigKey key, string oldValue, string newValue ) {
            var handler = KeyChanged;
            var args = new ConfigKeyChangedEventArgs( key, oldValue, newValue );
            if( handler != null ) handler( null, args );
        }

        #endregion


        /// <summary> Returns a list of all keys in a section. </summary>
        public static ConfigKey[] GetKeys( this ConfigSection section ) {
            return KeySections[section];
        }
    }
}


namespace fCraft.Events {
    public sealed class ConfigKeyChangingEventArgs : EventArgs, ICancelableEvent {
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