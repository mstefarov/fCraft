// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Reflection;
using System.Security;

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
        public static bool TestDirectory( string pathLabel, string path, bool checkForWriteAccess ) {
            try {
                if( !Directory.Exists( path ) ) {
                    Directory.CreateDirectory( path );
                }
                DirectoryInfo info = new DirectoryInfo( path );
                if( checkForWriteAccess ) {
                    string randomFileName = Path.Combine( info.FullName, "fCraft_write_test_" + DateTime.UtcNow.Ticks );
                    using( File.Create( randomFileName ) ) { }
                    File.Delete( randomFileName );
                }
                return true;

            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( "Paths.TestDirectory: Specified file/path for {0} is invalid or incorrectly formatted ({1}: {2}).", LogType.Error,
                                pathLabel, ex.GetType().ToString(), ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( "Paths.TestDirectory: Cannot create or write to file/path for {0}, please check permissions ({1}: {2}).", LogType.Error,
                                pathLabel, ex.GetType().ToString(), ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( "Paths.TestDirectory: Drive/volume for {0} does not exist or is not mounted ({1}).", LogType.Error,
                                pathLabel, ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( "Paths.TestDirectory: Specified file/path for {0} is not readable or writable ({1}: {2}).", LogType.Error,
                                pathLabel, ex.GetType().ToString(), ex.Message );
                } else {
                    throw;
                }
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

        /// <summary> Path where map backups are stored </summary>
        public static string BackupPath {
            get {
                return Path.Combine( MapPath, "backups" );
            }
        }

        internal static bool IgnoreMapPathConfigKey;

        public static bool IsDefaultMapPath( string path ) {
            return String.IsNullOrEmpty( path ) || Compare( MapPathDefault, path );
        }


        /// <summary>
        /// Returns true if paths or filenames reference the same location (accounts for all the filesystem quirks).
        /// </summary>
        public static bool Compare( string p1, string p2 ) {
            return String.Equals( Path.GetFullPath( p1 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  Path.GetFullPath( p2 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  StringComparison.Ordinal );
        }

        public static bool IsValidPath( string path ) {
            try {
                new FileInfo( path );
                return true;
            } catch( ArgumentException ) {
            } catch( PathTooLongException ) {
            } catch( NotSupportedException ) {
            }
            return false;
        }

        public static bool Contains( string parentPath, string childPath ) {
            string fullParentPath = Path.GetFullPath( parentPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            string fullChildPath = Path.GetFullPath( childPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            return fullChildPath.StartsWith( fullParentPath, StringComparison.Ordinal );
        }
    }
}