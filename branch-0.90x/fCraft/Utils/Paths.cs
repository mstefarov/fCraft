// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Contains fCraft path settings, and some filesystem-related utilities. </summary>
    public static class Paths {
        static readonly string[] ProtectedFiles;

        internal static readonly string[] DataFilesToBackup;


        static Paths() {
            WorkingPathDefault = GetDirectoryNameOrRoot( Assembly.GetExecutingAssembly().Location );

            WorkingPath = WorkingPathDefault;
            MapPath = MapPathDefault;
            LogPath = LogPathDefault;
            ConfigFileName = ConfigFileNameDefault;

            ProtectedFiles = new[] {
                "ConfigGUI.exe",
                "ConfigCLI.exe",
                "fCraft.dll",
                "fCraftGUI.dll",
                "ServerCLI.exe",
                "ServerGUI.exe",
                "ServerWinService.exe",
                "MapConverter.exe",
                "MapRenderer.exe",
                "HeartbeatSaver.exe",
                UpdateInstallerFileName,
                ConfigFileNameDefault,
                PlayerDBFileName,
                IPBanListFileName,
                RulesFileName,
                AnnouncementsFileName,
                GreetingFileName,
                HeartbeatDataFileName,
                WorldListFileName,
                "CHANGELOG.txt",
                "README.txt",
                "LICENSE.txt"
            };

            DataFilesToBackup = new[] {
                PlayerDBFileName,
                IPBanListFileName,
                WorldListFileName,
                ConfigFileName
            };
        }

        #region Paths & Properties

        /// <summary> Whether fCraft should parse ConfigKey.MapPath when loading config.xml
        /// This is set to "false" by Server if --mappath command-line argument was given. </summary>
        public static bool IgnoreMapPathConfigKey { get; internal set; }

        public const string MapPathDefault = "maps",
                            LogPathDefault = "logs",
                            ConfigFileNameDefault = "config.xml";

        public static readonly string WorkingPathDefault;

        /// <summary> Path to save maps to (default: .\maps)
        /// Can be overridden at startup via command-line argument "--mappath=",
        /// or via "MapPath" ConfigKey </summary>
        [NotNull]
        public static string MapPath { get; set; }

        /// <summary> Working path (default: whatever directory fCraft.dll is located in)
        /// Can be overridden at startup via command line argument "--path=" </summary>
        [NotNull]
        public static string WorkingPath { get; set; }

        /// <summary> Path to save logs to (default: .\logs)
        /// Can be overridden at startup via command-line argument "--logpath=" </summary>
        [NotNull]
        public static string LogPath { get; set; }

        /// <summary> Path to load/save config to/from (default: .\config.xml)
        /// Can be overridden at startup via command-line argument "--config=" </summary>
        [NotNull]
        public static string ConfigFileName { get; set; }


        public const string PlayerDBFileName = "PlayerDB.txt";

        public const string IPBanListFileName = "ipbans.txt";

        public const string GreetingFileName = "greeting.txt";

        public const string AnnouncementsFileName = "announcements.txt";

        public const string RulesFileName = "rules.txt";

        public const string RulesDirectory = "rules";

        public const string HeartbeatDataFileName = "heartbeatdata.txt";

        public const string UpdateInstallerFileName = "UpdateInstaller.exe";

        public const string WorldListFileName = "worlds.xml";

        public const string BlockDBDirectory = "blockdb";


        /// <summary> Directory where block database files (.fbdb) are stored. </summary>
        [NotNull]
        public static string BlockDBPath {
            get { return Path.Combine( WorkingPath, BlockDBDirectory ); }
        }

        /// <summary> Directory where rule sections are stored. </summary>
        [NotNull]
        public static string RulesPath {
            get { return Path.Combine( WorkingPath, RulesDirectory ); }
        }

        /// <summary> Path where map backups are stored. </summary>
        [NotNull]
        public static string BackupPath {
            get { return Path.Combine( MapPath, "backups" ); }
        }


        public const string DataBackupDirectory = "databackups";
        public const string DataBackupFileNameFormat = "fCraftData_{0:yyyyMMdd'_'HH'-'mm'-'ss}.zip";

        #endregion

        #region Utility Methods

        /// <summary> Normalizes a directory name: makes all separator characters match,
        /// adds an end separator, expands full path. </summary>
        /// <param name="path"> Path to normalize. </param>
        /// <returns> Normalized path. </returns>
        /// <exception cref="ArgumentNullException"> path is null. </exception>
        [NotNull]
        public static string NormalizeDirName( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            path = Path.GetFullPath( path );
            path = path.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
            if( !path.EndsWith( Path.DirectorySeparatorChar.ToString( CultureInfo.InvariantCulture ) ) ) {
                path += Path.DirectorySeparatorChar;
            }
            return new Uri( path ).LocalPath;
        }


        /// <summary> Creates a relative path from one file or folder to another. </summary>
        /// <param name="fromPath"> Contains the directory that defines the start of the relative path. </param>
        /// <param name="toPath"> Contains the path that defines the endpoint of the relative path. </param>
        /// <returns> The relative path from the start directory to the end path. </returns>
        /// <exception cref="ArgumentNullException"> fromPath or toPath is null. </exception>
        [NotNull]
        public static string MakeRelativePath( [NotNull] string fromPath, [NotNull] string toPath ) {
            if( fromPath == null ) throw new ArgumentNullException( "fromPath" );
            if( toPath == null ) throw new ArgumentNullException( "toPath" );

            Uri fromUri = new Uri( fromPath );
            Uri toUri = new Uri( toPath );

            Uri relativeUri = fromUri.MakeRelativeUri( toUri );
            String relativePath = Uri.UnescapeDataString( relativeUri.ToString() );

            return relativePath.Replace( '/', Path.DirectorySeparatorChar );
        }


        /// <summary> Moves file from source to destination, overwriting destination if it exists, as safely as possible. 
        /// File.Replace, File.Copy/File.Delete, or File.Move is used depending on circumstances. </summary>
        /// <param name="source"> File to be moved. </param>
        /// <param name="destination"> Destination file name. If this file exists, it will be replaced. </param>
        /// <exception cref="ArgumentNullException"> source or destination is used. </exception>
        public static void MoveOrReplaceFile( [NotNull] string source, [NotNull] string destination ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            if( File.Exists( destination ) ) {
                if( Path.GetPathRoot( source ) == Path.GetPathRoot( destination ) ) {
                    string backupFileName = destination + ".bak";
                    File.Replace( source, destination, backupFileName, true );
                    File.Delete( backupFileName );
                } else {
                    File.Copy( source, destination, true );
                    File.Delete( source );
                }
            } else {
                File.Move( source, destination );
            }
        }


        /// <summary> Makes sure that the path format is valid, that it exists, that it is accessible and writable. </summary>
        /// <param name="pathLabel"> Name of the path that's being tested (e.g. "map path"). Used for logging. </param>
        /// <param name="path"> Full or partial path. </param>
        /// <param name="checkForWriteAccess"> If set, tries to write to the given directory. </param>
        /// <returns> Full path of the directory (on success) or null (on failure). </returns>
        /// <exception cref="ArgumentNullException"> pathLabel or path is null. </exception>
        public static bool TestDirectory( [NotNull] string pathLabel, [NotNull] string path, bool checkForWriteAccess ) {
            if( pathLabel == null ) throw new ArgumentNullException( "pathLabel" );
            if( path == null ) throw new ArgumentNullException( "path" );
            try {
                if( !Directory.Exists( path ) ) {
                    Directory.CreateDirectory( path );
                }
                DirectoryInfo info = new DirectoryInfo( path );
                if( checkForWriteAccess ) {
                    string randomFileName = Path.Combine( info.FullName, "fCraft_write_test_" + Guid.NewGuid() );
                    using( File.Create( randomFileName ) ) {}
                    File.Delete( randomFileName );
                }
                return true;
            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                pathLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Cannot create or write to file/path for {0}, please check permissions ({1}: {2}).",
                                pathLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                pathLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Specified directory for {0} is not readable/writable ({1}: {2}).",
                                pathLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        /// <summary> Makes sure that the path format is valid, and optionally whether it is readable/writable. </summary>
        /// <param name="fileLabel"> Name of the path that's being tested (e.g. "map path"). Used for logging. </param>
        /// <param name="fileName"> Full or partial path of the file. </param>
        /// <param name="createIfDoesNotExist"> If target file is missing and this option is OFF, TestFile returns true.
        /// If target file is missing and this option is ON, TestFile tries to create
        /// a file and returns whether it succeeded. </param>
        /// <param name="neededAccess"> If file is present, type of access to test. </param>
        /// <returns> Whether target file passed all tests. </returns>
        /// <exception cref="ArgumentNullException"> fileLabel or fileName is null. </exception>
        public static bool TestFile( [NotNull] string fileLabel, [NotNull] string fileName,
                                     bool createIfDoesNotExist, FileAccess neededAccess ) {
            if( fileLabel == null ) throw new ArgumentNullException( "fileLabel" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                FileInfo fi = new FileInfo( fileName );
                if( fi.Exists ) {
                    if( (neededAccess & FileAccess.Read) == FileAccess.Read ) {
                        using( fi.OpenRead() ) {}
                    }
                    if( (neededAccess & FileAccess.Write) == FileAccess.Write ) {
                        using( fi.OpenWrite() ) {}
                    }
                } else if( createIfDoesNotExist ) {
                    using( fi.Create() ) {}
                }
                return true;
            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                fileLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Cannot create or write to {0}, please check permissions ({1}: {2}).",
                                fileLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                fileLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Specified file for {0} is not readable/writable ({1}: {2}).",
                                fileLabel,
                                ex.GetType().Name,
                                ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        /// <summary> Checks whether two paths/file names reference the exact same filesystem location. Accounts for filesystem quirks. </summary>
        /// <returns> True if given paths are referencing the same file. False if they're not. </returns>
        /// <exception cref="ArgumentNullException"> path1 or path2 is null. </exception>
        public static bool Compare( [NotNull] string path1, [NotNull] string path2 ) {
            if( path1 == null ) throw new ArgumentNullException( "path1" );
            if( path2 == null ) throw new ArgumentNullException( "path2" );
            return Compare( path1, path2, MonoCompat.IsCaseSensitive );
        }


        /// <summary> Checks whether two paths/file names reference the exact same filesystem location.
        /// Allows specifying whether comparison should be case-sensitive or not. </summary>
        /// <returns> True if given paths are referencing the same file. False if they're not. </returns>
        /// <exception cref="ArgumentNullException"> path1 or path2 is null. </exception>
        public static bool Compare( [NotNull] string path1, [NotNull] string path2, bool caseSensitive ) {
            if( path1 == null ) throw new ArgumentNullException( "path1" );
            if( path2 == null ) throw new ArgumentNullException( "path2" );
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return
                String.Equals(
                    Path.GetFullPath( path1 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                    Path.GetFullPath( path2 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                    sc );
        }


        /// <summary> Checks whether the given path is valid.
        /// Does NOT check for existence of the file/directory at the given path. </summary>
        /// <param name="path"> Path to check. </param>
        /// <returns> Returns false if path is of incorrect format, too long, unsupported, or blocked.
        /// Otherwise true. </returns>
        public static bool IsValidPath( [NotNull] string path ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            try {
                // ReSharper disable ObjectCreationAsStatement
                new FileInfo( path );
                // ReSharper restore ObjectCreationAsStatement
                return true;
            } catch( ArgumentException ) {} catch( PathTooLongException ) {} catch( NotSupportedException ) {} catch( UnauthorizedAccessException ) {}
            return false;
        }


        /// <summary> Checks whether childPath is inside parentPath. Accounts for filesystem quirks. </summary>
        /// <param name="parentPath"> Path that is supposed to contain childPath. </param>
        /// <param name="childPath"> Path that is supposed to be contained within parentPath. </param>
        /// <returns> True if childPath is contained within parentPath. </returns>
        public static bool Contains( [NotNull] string parentPath, [NotNull] string childPath ) {
            if( parentPath == null ) throw new ArgumentNullException( "parentPath" );
            if( childPath == null ) throw new ArgumentNullException( "childPath" );
            return Contains( parentPath, childPath, MonoCompat.IsCaseSensitive );
        }


        /// <summary> Checks whether childPath is inside parentPath. </summary>
        /// <param name="parentPath"> Path that is supposed to contain childPath. </param>
        /// <param name="childPath"> Path that is supposed to be contained within parentPath. </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> True if childPath is contained within parentPath. </returns>
        public static bool Contains( [NotNull] string parentPath, [NotNull] string childPath, bool caseSensitive ) {
            if( parentPath == null ) throw new ArgumentNullException( "parentPath" );
            if( childPath == null ) throw new ArgumentNullException( "childPath" );
            string fullParentPath = Path.GetFullPath( parentPath )
                                        .TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            string fullChildPath = Path.GetFullPath( childPath )
                                       .TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return fullChildPath.StartsWith( fullParentPath, sc );
        }


        /// <summary> Checks whether the file exists in a specified way (case-sensitive or case-insensitive) </summary>
        /// <param name="fileName"> File name/path to check. </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> True if file exists, otherwise false. </returns>
        public static bool FileExists( [NotNull] string fileName, bool caseSensitive ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return File.Exists( fileName );
            } else {
                return new FileInfo( fileName ).Exists( caseSensitive );
            }
        }


        /// <summary> Checks whether the file exists in a specified way (case-sensitive or case-insensitive). </summary>
        /// <param name="fileInfo"> FileInfo object in question. </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> True if file exists, otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> fileInfo is null. </exception>
        public static bool Exists( [NotNull] this FileInfo fileInfo, bool caseSensitive ) {
            if( fileInfo == null ) throw new ArgumentNullException( "fileInfo" );
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return fileInfo.Exists;
            } else {
                string parentDir = GetDirectoryNameOrRoot( fileInfo.FullName );
                string[] files = Directory.GetFiles( parentDir, "*", SearchOption.TopDirectoryOnly );
                StringComparison comparison = (caseSensitive
                                                   ? StringComparison.Ordinal
                                                   : StringComparison.OrdinalIgnoreCase);
                return files.Select( Path.GetFileName )
                            .Any( fileName => fileName.Equals( fileInfo.Name, comparison ) );
            }
        }


        /// <summary> Gets full directory name for a given path, or path root for directory-less paths. 
        /// Uses either Path.GetDirectoryName or Path.GetPathRoot to get the result. </summary>
        /// <exception cref="ArgumentNullException"> fileOrDirName is null. </exception>
        [NotNull]
        public static string GetDirectoryNameOrRoot( [NotNull] string fileOrDirName ) {
            if( fileOrDirName == null ) throw new ArgumentNullException( "fileOrDirName" );
            string fullPath = Path.GetFullPath( fileOrDirName );
            if( Directory.Exists( fullPath ) ) {
                return fullPath;
            }
            return Path.GetDirectoryName( fullPath ) ?? Path.GetPathRoot( fullPath );
        }


        /// <summary> Allows making changes to file name capitalization on case-insensitive filesystems. </summary>
        /// <param name="originalFullFileName"> Full path to the original file name </param>
        /// <param name="newFileName"> New file name (do not include the full path) </param>
        /// <exception cref="ArgumentNullException"> originalFullFileName or newFileName is null. </exception>
        public static void ForceRename( [NotNull] string originalFullFileName, [NotNull] string newFileName ) {
            if( originalFullFileName == null ) throw new ArgumentNullException( "originalFullFileName" );
            if( newFileName == null ) throw new ArgumentNullException( "newFileName" );
            FileInfo originalFile = new FileInfo( originalFullFileName );
            if( originalFile.Name == newFileName ) return;
            FileInfo newFile = new FileInfo( Path.Combine( GetDirectoryNameOrRoot( originalFullFileName ), newFileName ) );
            string tempFileName = originalFile.FullName + Guid.NewGuid();
            MoveOrReplaceFile( originalFile.FullName, tempFileName );
            MoveOrReplaceFile( tempFileName, newFile.FullName );
        }


        /// <summary> Find files that match the name in a case-insensitive way. </summary>
        /// <param name="fullFileName"> Case-insensitive file name to look for. </param>
        /// <returns> Array of matches. Empty array if no files matches. </returns>
        /// <exception cref="ArgumentNullException"> fullFileName is null. </exception>
        [NotNull]
        public static FileInfo[] FindFiles( [NotNull] string fullFileName ) {
            if( fullFileName == null ) throw new ArgumentNullException( "fullFileName" );
            string fileName = Path.GetFileName( fullFileName );
            DirectoryInfo directory = new DirectoryInfo( GetDirectoryNameOrRoot( fullFileName ) );
            return directory.GetFiles( "*", SearchOption.TopDirectoryOnly )
                            .Where( file => file.Name.Equals( fileName, StringComparison.OrdinalIgnoreCase ) )
                            .ToArray();
        }


        /// <summary> Checks whether the given file is on a list of protected file names.
        /// Protected file names include all fCraft binaries, configuration files, and data files. </summary>
        /// <param name="fileName"> File name/path to check. </param>
        /// <returns> True if given file name is considered protected; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> fileName is null. </exception>
        public static bool IsProtectedFileName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return ProtectedFiles.Any( t => Compare( t, fileName ) );
        }

        #endregion
    }
}
