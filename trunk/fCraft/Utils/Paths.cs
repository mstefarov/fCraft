using System;
using System.IO;
using System.Security;
using System.Reflection;


namespace fCraft {
    public static class Paths {

        static Paths() {
            WorkingPathDefault = Path.GetFullPath( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) );
            WorkingPath = WorkingPathDefault;
            MapPath = MapPathDefault;
            LogPath = LogPathDefault;
        }

        /// <summary>
        /// Makes sure that the path format is valid, that it exists, that it is accessible and writeable.
        /// </summary>
        /// <param name="path">full or partial path</param>
        /// <param name="checkForWriteAccess"></param>
        /// <returns>full path of the directory (on success) or null (on failure)</returns>
        public static bool TestDirectory( string path, bool checkForWriteAccess ) {
            try {
                if( !Directory.Exists( path ) ) {
                    Directory.CreateDirectory( path );
                }
                DirectoryInfo info = new DirectoryInfo( path );
                if( checkForWriteAccess ) {
                    info.LastWriteTimeUtc = DateTime.UtcNow; // equivalent to "touch" - checking for write access
                }
                return true;

            } catch( ArgumentException ) {
                Logger.Log( "Specified path is invalid (incorrect format), path reset to default.", LogType.Warning );
            } catch( PathTooLongException ) {
                Logger.Log( "Specified path is invalid (too long), path reset to default.", LogType.Warning );
            } catch( SecurityException ) {
                Logger.Log( "Cannot create specified directory (SecurityException).", LogType.Warning );
            } catch( UnauthorizedAccessException ) {
                Logger.Log( "Cannot create specified directory (UnauthorizedAccessException).", LogType.Warning );
            } catch( IOException ) {
                Logger.Log( "Cannot write to specified directory (IOException).", LogType.Warning );
            }
            return false;
        }



        public const string MapPathDefault = "maps",
                            LogPathDefault = "logs",
                            ConfigFileNameDefault = "config.xml";

        public static readonly string WorkingPathDefault;

        /// <summary>
        /// Path to save maps to (default: .\maps)
        /// Can be overridden at startup via command-line argument "--mappath=",
        /// or via "MapPath" ConfigKey
        /// </summary>
        public static string MapPath { get; set; }

        /// <summary>
        /// Working path (default: whatever directory fCraft.dll is located in)
        /// Can be overridden at startup via command line argument "--path="
        /// </summary>
        public static string WorkingPath { get; set; }

        /// <summary>
        /// Path to save logs to (default: .\logs)
        /// Can be overridden at startup via command-line argument "--logpath="
        /// </summary>
        public static string LogPath { get; set; }

        /// <summary>
        /// Path to load/save config to/from (default: .\config.xml)
        /// Can be overridden at startup via command-line argument "--config="
        /// </summary>
        public static string ConfigFileName { get; set; }


        public static bool IsDefaultMapPath( string path ) {
            return String.IsNullOrEmpty( path ) || Server.ComparePaths( MapPathDefault, path );
        }

    }
}