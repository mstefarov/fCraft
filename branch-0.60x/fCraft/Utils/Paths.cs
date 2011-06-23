// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;

namespace fCraft {
    /// <summary> Contains fCraft path settings, and some filesystem-related utilities. </summary>
    public static class Paths {

        static readonly string[] ProtectedFiles;

        internal static readonly string[] DataFilesToBackup;

        static Paths() {
            WorkingPathDefault = Path.GetFullPath( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) );
            WorkingPath = WorkingPathDefault;
            MapPath = MapPathDefault;
            LogPath = LogPathDefault;
            ConfigFileName = ConfigFileNameDefault;

            ProtectedFiles = new[]{
                "AutoLauncher.exe",
                "ConfigTool.exe",
                "fCraft.dll",
                "fCraftConsole.exe",
                "fCraftUI.exe",
                "fCraftWinService.exe",
                UpdaterFile,
                ConfigFileNameDefault,
                PlayerDBFileName,
                IPBanListFileName,
                RulesFileName,
                AnnouncementsFileName,
                GreetingFileName,
                HeartbeatDataFileName,
                WorldListFileName,
                AutoRankFile
            };

            DataFilesToBackup = new[]{
                PlayerDBFileName,
                IPBanListFileName,
                WorldListFileName
            };
        }


        #region Paths & Properties

        public static bool IgnoreMapPathConfigKey { get; internal set; }

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



        public const string PlayerDBFileName = "PlayerDB.txt";

        public const string IPBanListFileName = "ipbans.txt";

        public const string GreetingFileName = "greeting.txt";

        public const string AnnouncementsFileName = "announcements.txt";

        public const string RulesFileName = "rules.txt";

        public const string HeartbeatDataFileName = "heartbeatdata.txt";

        public const string UpdaterFile = "fCraftUpdater.exe";

        public const string WorldListFileName = "worlds.xml";

        public const string AutoRankFile = "autorank.xml";

        public const string PluginDirectory = "plugins";

        #endregion


        #region Utility Methods

        public static void MoveOrReplace( string source, string destination ) {
            if( File.Exists( destination ) ) {
                File.Replace( source, destination, null, true );
            } else {
                File.Move( source, destination );
            }
        }

        /// <summary>
        /// Makes sure that the path format is valid, that it exists, that it is accessible and writeable.
        /// </summary>
        /// <param name="pathLabel">name of the path that's being tested (e.g. "map path").
        /// Used for logging.</param>
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
                    string randomFileName = Path.Combine( info.FullName, "fCraft_write_test_" + Guid.NewGuid() );
                    using( File.Create( randomFileName ) ) { }
                    File.Delete( randomFileName );
                }
                return true;

            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( "Paths.TestDirectory: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).", LogType.Error,
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( "Paths.TestDirectory: Cannot create or write to file/path for {0}, please check permissions ({1}: {2}).", LogType.Error,
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( "Paths.TestDirectory: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).", LogType.Error,
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( "Paths.TestDirectory: Specified directory for {0} is not readable/writable ({1}: {2}).", LogType.Error,
                                pathLabel, ex.GetType().Name, ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        public static bool TestFile( string fileLabel, string filename, bool createIfDoesNotExist, bool checkForReadAccess, bool checkForWriteAccess ) {
            try {
                new FileInfo( filename );
                if( File.Exists( filename ) ) {
                    if( checkForReadAccess ) {
                        using( File.OpenRead( filename ) ) { }
                    }
                    if( checkForWriteAccess ) {
                        using( File.OpenWrite( filename ) ) { }
                    }
                } else if( createIfDoesNotExist ) {
                    using( File.Create( filename ) ) { }
                }
                return true;

            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( "Paths.TestFile: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).", LogType.Error,
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( "Paths.TestFile: Cannot create or write to {0}, please check permissions ({1}: {2}).", LogType.Error,
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( "Paths.TestFile: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).", LogType.Error,
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( "Paths.TestFile: Specified file for {0} is not readable/writable ({1}: {2}).", LogType.Error,
                                fileLabel, ex.GetType().Name, ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        /// <summary> Path where map backups are stored </summary>
        public static string BackupPath {
            get {
                return Path.Combine( MapPath, "backups" );
            }
        }


        public static bool IsDefaultMapPath( string path ) {
            return String.IsNullOrEmpty( path ) || Compare( MapPathDefault, path );
        }


        /// <summary>Returns true if paths or filenames reference the same location (accounts for all the filesystem quirks).</summary>
        public static bool Compare( string p1, string p2 ) {
            return Compare( p1, p2, MonoCompat.IsCaseSensitive );
        }


        /// <summary>Returns true if paths or filenames reference the same location (accounts for all the filesystem quirks).</summary>
        public static bool Compare( string p1, string p2, bool caseSensitive ) {
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return String.Equals( Path.GetFullPath( p1 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  Path.GetFullPath( p2 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  sc );
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


        /// <summary> Checks whether childPath is inside parentPath </summary>
        /// <param name="parentPath">Path that is supposed to contain childPath</param>
        /// <param name="childPath">Path that is supposed to be contained within parentPath</param>
        /// <returns>true if childPath is contained within parentPath</returns>
        public static bool Contains( string parentPath, string childPath ) {
            return Contains( parentPath, childPath, MonoCompat.IsCaseSensitive );
        }


        /// <summary> Checks whether childPath is inside parentPath </summary>
        /// <param name="parentPath"> Path that is supposed to contain childPath </param>
        /// <param name="childPath"> Path that is supposed to be contained within parentPath </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> true if childPath is contained within parentPath </returns>
        public static bool Contains( string parentPath, string childPath, bool caseSensitive ) {
            string fullParentPath = Path.GetFullPath( parentPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            string fullChildPath = Path.GetFullPath( childPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return fullChildPath.StartsWith( fullParentPath, sc );
        }


        /// <summary> Checks whether the file exists in a specified way (case-sensitive or case-insensitive) </summary>
        /// <param name="fileName"> filename in question </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> true if file exists, otherwise false </returns>
        public static bool FileExists( string fileName, bool caseSensitive ) {
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return File.Exists( fileName );
            } else {
                return new FileInfo( fileName ).Exists( caseSensitive );
            }
        }


        /// <summary>Checks whether the file exists in a specified way (case-sensitive or case-insensitive)</summary>
        /// <param name="fileInfo">FileInfo object in question</param>
        /// <param name="caseSensitive">Whether check should be case-sensitive or case-insensitive.</param>
        /// <returns>true if file exists, otherwise false</returns>
        public static bool Exists( this FileInfo fileInfo, bool caseSensitive ) {
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return fileInfo.Exists;
            } else {
                DirectoryInfo parentDir = fileInfo.Directory;
                StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                foreach( FileInfo file in parentDir.GetFiles( "*", SearchOption.TopDirectoryOnly ) ) {
                    if( file.Name.Equals( fileInfo.Name, sc ) ) return true;
                }
                return false;
            }
        }


        /// <summary> Allows making changes to filename capitalization on case-insensitive filesystems. </summary>
        /// <param name="originalFullFileName"> Full path to the original filename </param>
        /// <param name="newFileName"> New file name (do not include the full path) </param>
        public static void ForceRename( string originalFullFileName, string newFileName ) {
            FileInfo originalFile = new FileInfo( originalFullFileName );
            if( originalFile.Name == newFileName ) return;
            FileInfo newFile = new FileInfo( Path.Combine( originalFile.DirectoryName, newFileName ) );
            string tempFileName = originalFile.FullName + Guid.NewGuid();
            MoveOrReplace( originalFile.FullName, tempFileName );
            MoveOrReplace( tempFileName, newFile.FullName );
        }


        /// <summary> Find files that match the name in a case-insensitive way. </summary>
        /// <param name="fullFileName"> Case-insensitive filename to look for. </param>
        /// <returns> Array of matches. Empty array if no files matches. </returns>
        public static FileInfo[] FindFiles( string fullFileName ) {
            FileInfo fi = new FileInfo( fullFileName );
            DirectoryInfo parentDir = fi.Directory;
            return parentDir.GetFiles( "*", SearchOption.TopDirectoryOnly )
                            .Where( file => file.Name.Equals( fi.Name, StringComparison.OrdinalIgnoreCase ) )
                            .ToArray();
        }


        public static bool IsProtectedFileName( string fileName ) {
            for( int i = 0; i < ProtectedFiles.Length; i++ ) {
                if( Compare( ProtectedFiles[i], fileName ) ) return true;
            }
            return false;
        }

        #endregion

    }
}