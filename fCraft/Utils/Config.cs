using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;

namespace fCraft {
    static class Config {
        static Dictionary<string, string> settings;

        static void LoadDefaults() {
            settings = new Dictionary<string, string>();
            settings.Add( "ServerName", "Minecraft custom server (fCraft)" );
            settings.Add( "MOTD", "Welcome to the server!" );
            settings.Add( "MaxPlayers", "16" );
            settings.Add( "IsPublic", "false" );
            settings.Add( "VerifyNames", "false" );
            settings.Add( "LogThreshold", LogLevel.Info.ToString() );
            settings.Add( "OwnerName", "fragmer" );
            settings.Add( "DefaultRank", "(lowest)" );
        }

        public static long Salt;
        public const int Port = 25565;
        public const string HeartBeatURL = "http://www.minecraft.net/heartbeat.jsp";
        public static string ServerURL;
        public const int HeartBeatDelay = 45000;

        public const int ProtocolVersion = 7;
        public const uint LevelFormatID = 0xFC000001;
        public const int MaxPlayersSupported = 256;
        const string ConfigRootName = "fCraftConfig";


        public static bool Init( string configFileName ) {
            // generate random salt
            Random random = new Random();
            Salt = (long)random.Next() * (long)random.Next();
            Logger.Log( "Config: Salt = " + Salt );

            LoadDefaults();

            // try to load config file (XML)
            XDocument file;
            if( File.Exists( configFileName ) ) {
                try {
                    file = XDocument.Load( configFileName );
                    if( file.Root == null || file.Root.Name != ConfigRootName ) {
                        Logger.LogAlert( "Config.Init: Malformed or incompatible config file " + configFileName + ". Loading defaults." );
                        file = new XDocument();
                        file.Add( new XElement( ConfigRootName ) );
                    } else {
                        Logger.Log( "Config.Init: Config file " + configFileName + " loaded succesfully." );
                    }
                } catch( Exception ex ) {
                    Logger.LogError( "Config.Init: Fatal error while loading config file " + configFileName + ": " + ex.Message );
                    return false;
                }
            } else {
                // create a new one (with defaults) if no file exists
                file = new XDocument();
                file.Add( new XElement( ConfigRootName ) );
            }

            XElement config = file.Root;

            bool definesClasses = false;

            // Load config
            foreach( XElement element in config.Elements() ) {
                if( settings.ContainsKey( element.Name.ToString() ) ) {
                    if( SetValue( element.Name.ToString(), element.Value ) ) {
                        Logger.Log( "| " + element.Name + " = " + element.Value );
                    } else {
                        Logger.LogAlert( "Config.Init: Ignoring invalid value for " + element.Name + ", using default instead." );
                    }
                } else if( element.Name.ToString() == "Classes" ) {
                    foreach( XElement playerClass in element.Elements( "PlayerClass" ) ) {
                        if( DefineClass( playerClass ) )
                            definesClasses = true;
                    }
                    if( PlayerClass.classes.Count < 1 )
                        definesClasses = false;
                } else {
                    settings.Add( element.Name.ToString(), element.Value );
                }
            }

            // If the config did not define one of the standard keys, add it and save it
            foreach( string key in settings.Keys ) {
                if( config.Element( key ) == null ) {
                    config.Add( new XElement( key, settings[key] ) );
                }
            }

            Logger.Threshold = (LogLevel)Enum.Parse( typeof( LogLevel ), settings["LogThreshold"] );

            // If player did not define any custom classes, load & save default ones
            if( !definesClasses ) {
                Logger.LogAlert( "Config.Init: using default player classes" );
                config.Add( DefineDefaultClasses() );
            }

            // save the settings
            try {
                file.Save( configFileName );
            } catch( Exception ex ) {
                Logger.LogError( "Config.Init: Fatal error while saving config file " + configFileName + ": " + ex.Message );
                return false;
            }
            return true;
        }

        static bool SetValue( string key, string value ) {
            if( key == "MaxPlayers" ) {
                return ValidateInt( key, value, 1, MaxPlayersSupported );
            } else if( key == "IsPublic" ) {
                return ValidateBool( key, value );
            } else if( key == "ValidateNames" ) {
                return ValidateBool( key, value );
            } else if( key == "LogThreshold" ) {
                LogLevel temp;
                try {
                    temp = (LogLevel)Enum.Parse( typeof( LogLevel ), value, true );
                    settings[key] = temp.ToString();
                    return true;
                } catch( ArgumentException ) {
                    Logger.LogAlert( "Config.SetValue: Specified value for LogThreshold cannot be parsed. Using default." );
                    return false;
                }
            } else {
                settings[key] = value;
                return true;
            }
        }

        static bool ValidateInt( string key, string value, int minRange, int maxRange ) {
            int temp;
            if( Int32.TryParse( value, out temp ) ) {
                if( temp >= minRange && temp <= maxRange ) {
                    settings[key] = temp.ToString();
                } else {
                    Logger.LogAlert( "Config.SetValue: Specified value for " + key + " is not within valid range (" + minRange + "..." + maxRange + "). Using default." );
                }
                return true;
            } else {
                Logger.LogAlert( "Config.SetValue: Specified value for " + key + " could not be parsed. Using default." );
                return false;
            }
        }

        static bool ValidateBool( string key, string value ) {
            bool temp;
            if( Boolean.TryParse( value, out temp ) ) {
                settings[key] = temp.ToString();
                return true;
            } else {
                Logger.LogAlert( "Config.SetValue: Specified value for " + key + " could not be parsed. Using default." );
                return false;
            }
        }

        public static string GetString( string key ) {
            return settings[key];
        }

        public static int GetInt( string key ) {
            return Int32.Parse( settings[key] );
        }

        public static bool GetBool( string key ) {
            return Boolean.Parse( settings[key] );
        }

        static XElement DefineDefaultClasses() {
            XElement permissions = new XElement( "Classes" );

            XElement visitor = new XElement( "PlayerClass" );
            visitor.Add( new XAttribute( "name", "visitor" ) );
            visitor.Add( new XAttribute( "rank", 0 ) );
            visitor.Add( new XAttribute( "color", "silver" ) );
            visitor.Add( new XElement( "CanBuild" ) );
            visitor.Add( new XElement( "CanDelete" ) );
            permissions.Add( visitor );
            DefineClass( visitor );

            XElement regular = new XElement( "PlayerClass" );
            regular.Add( new XAttribute( "name", "regular" ) );
            regular.Add( new XAttribute( "rank", 20 ) );
            regular.Add( new XAttribute( "color", "white" ) );
            regular.Add( new XElement( "CanBuild" ) );
            regular.Add( new XElement( "CanDelete" ) );
            regular.Add( new XElement( "CanBuildAdmin" ) );
            regular.Add( new XElement( "CanDeleteAdmin" ) );
            regular.Add( new XElement( "CanKick" ) );
            permissions.Add( regular );
            DefineClass( regular );

            XElement op = new XElement( "PlayerClass" );
            op.Add( new XAttribute( "name", "op" ) );
            op.Add( new XAttribute( "rank", 90 ) );
            op.Add( new XAttribute( "color", "yellow" ) );
            op.Add( new XElement( "CanBuild" ) );
            op.Add( new XElement( "CanDelete" ) );
            op.Add( new XElement( "CanBuildAdmin" ) );
            op.Add( new XElement( "CanDeleteAdmin" ) );
            op.Add( new XElement( "CanAnnounce" ) );
            op.Add( new XElement( "CanKick" ) );
            op.Add( new XElement( "CanBan" ) );
            op.Add( new XElement( "CanIPBan" ) );
            op.Add( new XElement( "CanPromote" ) );
            op.Add( new XElement( "CanHide" ) );
            op.Add( new XElement( "CanAlias" ) );
            permissions.Add( op );
            DefineClass( op );

            XElement owner = new XElement( "PlayerClass" );
            owner.Add( new XAttribute( "name", "owner" ) );
            owner.Add( new XAttribute( "rank", 100 ) );
            owner.Add( new XAttribute( "color", "red" ) );
            owner.Add( new XElement( "CanBuild" ) );
            owner.Add( new XElement( "CanDelete" ) );
            owner.Add( new XElement( "CanBuildAdmin" ) );
            owner.Add( new XElement( "CanDeleteAdmin" ) );
            owner.Add( new XElement( "CanAnnounce" ) );
            owner.Add( new XElement( "CanKick" ) );
            owner.Add( new XElement( "CanBan" ) );
            owner.Add( new XElement( "CanIPBan" ) );
            owner.Add( new XElement( "CanPromote" ) );
            owner.Add( new XElement( "CanDemote" ) );
            owner.Add( new XElement( "CanHide" ) );
            owner.Add( new XElement( "CanAlias" ) );
            owner.Add( new XElement( "CanBackup" ) );
            owner.Add( new XElement( "CanControlWater" ) );
            owner.Add( new XElement( "CanControlLava" ) );
            permissions.Add( owner );
            DefineClass( owner );

            return permissions;
        }

        static bool DefineClass( XElement classElement ) {
            PlayerClass playerClass = new PlayerClass();
            XAttribute nameAttr = classElement.Attribute( "name" );
            if( nameAttr == null ) return false;
            playerClass.name = nameAttr.Value;

            XAttribute rankAttr = classElement.Attribute( "rank" );
            if( rankAttr == null ) return false;
            playerClass.rank = Byte.Parse( rankAttr.Value );

            XAttribute colorAttr = classElement.Attribute( "color" );
            if( colorAttr == null ) return false;
            playerClass.color = Color.GetChar( colorAttr.Value.ToLower() );

            foreach( string permisson in Enum.GetNames( typeof( Permissions ) ) ) {
                if( classElement.Element( "Can" + permisson ) != null )
                    playerClass.permissions[(int)Enum.Parse( typeof( Permissions ), permisson, true )] = true;
            }

            PlayerClass.AddClass( playerClass );
            return true;
        }
    }
}