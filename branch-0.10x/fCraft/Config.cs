using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;

namespace fCraft {
    static class Config {
        public static string ServerName;
        public static string MOTD;
        public static int MaxPlayers;
        public static bool IsPublic;
        public static bool VerifyNames;

        private const string DefaultServerName = "Minecraft custom server (fCraft)";
        private const string DefaultMOTD = "Welcome to the server!";
        private const int DefaultMaxPlayers = 16;
        private const bool DefaultIsPublic = false;
        private const bool DefaultVerifyNames = false;

        public static long Salt;

        public const LogLevel LogThreshold = LogLevel.Info;
        public const int Port = 25565;
        public const int ProtocolVersion = 7;
        public const uint LevelFormatID = 0xFC000001;

        public const string HeartBeatURL = "http://www.minecraft.net/heartbeat.jsp";
        public static string ServerURL;
        public const int HeartBeatDelay = 45000;


        private const string ConfigRootName = "fCraftConfig";

        public static bool Init( string configFileName ) {
            // generate random salt
            Random random = new Random();
            Salt = (long)random.Next() * (long)random.Next();
            Logger.Log( "Config: Salt = " + Salt );            

            // try to load config file (XML)
            XDocument file;
            if( File.Exists( configFileName ) ) {
                try {
                    file = XDocument.Load( configFileName );
                    if( file.Root == null || file.Root.Name != ConfigRootName ) {
                        Logger.LogAlert( "Config.Init: Malformed or incompatible config file " + configFileName+". Loading defaults." );
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

            // load settings
            ServerName = ReadString( config, "ServerName", DefaultServerName );
            MOTD = ReadString( config, "MOTD", DefaultMOTD );
            MaxPlayers = ReadInt( config, "MaxPlayers", DefaultMaxPlayers );
            IsPublic = ReadBool( config, "IsPublic", DefaultIsPublic );
            VerifyNames = ReadBool( config, "VerifyNames", DefaultVerifyNames );

            // save the settings
            try {
                file.Save( configFileName );
            } catch( Exception ex ) {
                Logger.LogError( "Config.Init: Fatal error while saving config file " + configFileName + ": " + ex.Message );
                return false;
            }
            return true;
        }

        private static string ReadString( XElement doc, string fieldName, string _default ) {
            XElement element = doc.Element( fieldName );
            if( element != null ) {
                Logger.Log( "Config.ReadString: " + fieldName + " = " + element.Value );
                return element.Value;
            } else {
                Logger.Log( "Config.ReadString: " + fieldName + " = " + _default + " (default)" );
                doc.Add( new XElement( fieldName, _default ) );
                return _default;
            }
        }

        private static int ReadInt( XElement doc, string fieldName, int _default ) {
            XElement element = doc.Element( fieldName );
            if( element != null ) {
                Logger.Log( "Config.ReadInt: " + fieldName + " = " + element.Value );
                return Int32.Parse( element.Value );
            } else {
                Logger.Log( "Config.ReadInt: " + fieldName + " = " + _default + " (default)" );
                doc.Add( new XElement( fieldName, _default ) );
                return _default;
            }
        }

        private static bool ReadBool( XElement doc, string fieldName, bool _default ) {
            XElement element = doc.Element( fieldName );
            if( element != null ) {
                Logger.Log( "Config.ReadBool: " + fieldName + " = " + element.Value );
                return Boolean.Parse( element.Value );
            } else {
                Logger.Log( "Config.ReadBool: " + fieldName + " = " + _default + " (default)" );
                doc.Add( new XElement( fieldName, _default ) );
                return _default;
            }
        }
    }
}