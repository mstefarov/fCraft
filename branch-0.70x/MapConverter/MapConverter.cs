// Part of fCraft | Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | MIT License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using fCraft.MapConversion;
using fCraft.Events;
using Mono.Options;

namespace fCraft.MapConverter {
    static class MapConverter {
        static IMapConverter[] allConverters;
        static string importerName,
                      exporterName,
                      inputPath,
                      outputDirName,
                      inputFilter;
        static IMapConverter importer;
        static IMapConverter exporter;
        static bool recursive, overwrite;


        static int Main( string[] args ) {
            Logger.Logged += OnLogged;

            allConverters = MapUtility.GetConverters();

            ParseOptions( args );

            // parse importer name
            if( importerName != null ) {
                MapFormat importFormat;
                if( !EnumUtil.TryParse( importerName, out importFormat, true ) ||
                    ( importer = MapUtility.GetConverter( importFormat ) ) == null ) {
                    Console.Error.WriteLine( "Unrecognized importer \"{0}\"", importerName );
                    PrintUsage();
                    return (int)ReturnCode.UnrecognizedImporter;
                }
            }

            // parse exporter format
            MapFormat exportFormat;
            if( !EnumUtil.TryParse( exporterName, out exportFormat, true ) ||
                ( exporter = MapUtility.GetConverter( exportFormat ) ) == null ) {
                Console.Error.WriteLine( "Unrecognized exporter \"{0}\"", exporterName );
                PrintUsage();
                return (int)ReturnCode.UnrecognizedExporter;
            }
            if( !exporter.SupportsExport ) {
                Console.Error.WriteLine( "fCraft does not support exporting to {0} ({1}) format.",
                                         exporter.Format, exporter.ServerName );
                Console.WriteLine( "See \"MapConverter --help\" for a list of supported formats." );
                return (int)ReturnCode.UnsupportedSaveFormat;
            }

            // check if input path exists, and if it's a file or directory
            bool directoryMode;
            try {
                if( File.Exists( inputPath ) ) {
                    directoryMode = false;
                    if( outputDirName == null ) outputDirName = Path.GetDirectoryName( Path.GetFullPath( inputPath ) );

                } else if( Directory.Exists( inputPath ) ) {
                    directoryMode = true;
                    if( outputDirName == null ) outputDirName = Path.GetFullPath( inputPath );

                } else {
                    Console.Error.WriteLine( "MapConverter: Cannot locate \"{0}\"", inputPath );
                    return (int)ReturnCode.InputDirNotFound;
                }

                if( !Directory.Exists( outputDirName ) ) {
                    Directory.CreateDirectory( outputDirName );
                }

            } catch( Exception ex ) {
                Console.Error.WriteLine( "MapConverter: {0}: {1}",
                                         ex.GetType().Name,
                                         ex.Message );
                return (int)ReturnCode.PathError;
            }

            // check recursive flag
            if( recursive && !directoryMode ) {
                Console.Error.WriteLine( "MapConverter: Recursive flag is given, but input is not a directory." );
            }

            // check recursive flag
            if( inputFilter != null && !directoryMode ) {
                Console.Error.WriteLine( "MapConverter: Filter param is given, but input is not a directory." );
            }


            if( importer != null && importer.StorageType == MapStorageType.Directory ) {
                ConvertOneMap( new DirectoryInfo( inputPath ) );
            } else if( !directoryMode ) {
                ConvertOneMap( new FileInfo( inputPath ) );
            } else {
                SearchOption recursiveOption = ( recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
                DirectoryInfo inputDirInfo = new DirectoryInfo( inputPath );
                if( inputFilter == null ) inputFilter = "*";
                foreach( FileSystemInfo dirInfo in inputDirInfo.EnumerateFileSystemInfos( inputFilter, recursiveOption ) ) {
                    ConvertOneMap( dirInfo );
                }
            }

            return (int)ReturnCode.Success;
        }


        static void ConvertOneMap( [NotNull] FileSystemInfo fileSystemInfo ) {
            if( fileSystemInfo == null ) throw new ArgumentNullException( "fileSystemInfo" );

            try {
                Map map;
                if( importer != null ) {
                    if( !importer.ClaimsName( fileSystemInfo.FullName ) ) return;
                    Console.Write( "Loading {0}... ", fileSystemInfo.Name );
                    map = importer.Load( fileSystemInfo.FullName );
                } else {
                    Console.Write( "Checking {0}... ", fileSystemInfo.Name );
                    map = MapUtility.Load( fileSystemInfo.FullName );
                }

                string targetFileName;
                if( ( fileSystemInfo.Attributes & FileAttributes.Directory ) == FileAttributes.Directory ) {
                    targetFileName = fileSystemInfo.Name + '.' + exporter.FileExtension;
                } else {
                    targetFileName = Path.GetFileNameWithoutExtension( fileSystemInfo.Name ) + '.' +
                                     exporter.FileExtension;
                }

                string targetPath = Path.Combine( outputDirName, targetFileName );
                if( !overwrite && File.Exists( targetPath ) ) {
                    Console.WriteLine();
                    if( !ShowYesNo( "File \"{0}\" already exists. Overwrite?" ) ) {
                        return;
                    }
                }
                Console.Write( "Saving {0}... ", Path.GetFileName( targetFileName ) );
                exporter.Save( map, targetPath );
                Console.WriteLine( "ok" );

            } catch( NoMapConverterFoundException ) {
                Console.WriteLine( "skip" );

            } catch( Exception ex ) {
                Console.WriteLine( "ERROR" );
                Console.Error.WriteLine( "{0}: {1}", ex.GetType().Name, ex.Message );
            }
        }


        static void OnLogged( object sender, LogEventArgs e ) {
            switch( e.MessageType ) {
                case LogType.Error:
                case LogType.SeriousError:
                case LogType.Warning:
                    Console.Error.WriteLine( e.MessageType );
                    return;
                default:
                    Console.WriteLine( e.Message );
                    return;
            }
        }


        #region Options and help

        static OptionSet opts;

        static void ParseOptions( [NotNull] string[] args ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            string importerList = allConverters.JoinToString( c => c.Format.ToString() );
            string exporterList = allConverters.Where( c => c.SupportsExport ).JoinToString( c => c.Format.ToString() );

            bool printHelp = false;

            opts = new OptionSet()
                .Add( "e=|exporter=",
                      "REQUIRED: Converter used for exporting/saving maps. " +
                      "Available exporters: " + exporterList,
                      o => exporterName = o )

                .Add( "i=|importer=",
                      "Optional: Converter used for importing/loading maps. " +
                      "Available importers: " + importerList,
                      o => importerName = o )

                .Add( "o=|output=",
                      "Optional: Path to save converted map files to. " +
                      "If not specified, converted maps will be saved to the original maps' directory.",
                      o => outputDirName = o )

                .Add( "f=|filter=",
                      "Optional: Pattern to filter input filenames, e.g. \"*.dat\" or \"builder*\". " +
                      "Applicable only when a directory name is given as input.",
                      o => inputFilter = o )

                .Add( "r|recursive",
                      "Optional: Look through all subdirectories, and convert map files there too. " +
                      "Applicable only when a directory name is given as input.",
                      o => recursive = ( o != null ) )

                .Add( "y|overwrite",
                      "Optional: Do not ask for confirmation to overwrite existing files.",
                      o => overwrite = ( o != null ) )

                .Add( "?|help|h",
                      "Prints out the options.",
                      o => printHelp = ( o != null ) );

            List<string> pathList = new List<string>();
            try {
                pathList = opts.Parse( args );
            } catch( OptionException ex ) {
                Console.Error.Write( "MapConverter: " );
                Console.Error.WriteLine( ex.Message );
                PrintHelp();
                Environment.Exit( (int)ReturnCode.ArgumentParsingError );
            }

            if( printHelp ) {
                PrintHelp();
                Environment.Exit( (int)ReturnCode.Success );
            }

            if( pathList.Count != 1 ) {
                Console.Error.WriteLine( "MapConverter: At least one file or directory name required." );
                PrintUsage();
                Environment.Exit( (int)ReturnCode.ArgumentParsingError );
            }
            inputPath = pathList[0];

            if( exporterName == null ) {
                Console.Error.WriteLine( "MapConverter: Export format required." );
                PrintUsage();
                Environment.Exit( (int)ReturnCode.ArgumentParsingError );
            }
        }


        static void PrintUsage() {
            Console.WriteLine( "Usage: MapConverter [options] -e=Exporter \"FileOrDirectory\"" );
            Console.WriteLine( "See \"MapConverter --help\" for more details." );
        }


        static void PrintHelp() {
            Console.WriteLine();
            Console.WriteLine( "Usage: MapConverter [options] -e=Exporter \"FileOrDirectory\"" );
            Console.WriteLine();
            opts.WriteOptionDescriptions( Console.Out );
        }


        [StringFormatMethod( "prompt" )]
        public static bool ShowYesNo( [NotNull] string prompt, params object[] formatArgs ) {
            if( prompt == null ) throw new ArgumentNullException( "prompt" );
            while( true ) {
                Console.Write( prompt + " (Y/N): ", formatArgs );
                string input = Console.ReadLine().ToLower();

                if( input.Equals( "yes", StringComparison.OrdinalIgnoreCase ) ||
                    input.Equals( "y", StringComparison.OrdinalIgnoreCase ) ) {
                    return true;
                } else if( input.Equals( "no", StringComparison.OrdinalIgnoreCase ) ||
                    input.Equals( "n", StringComparison.OrdinalIgnoreCase ) ) {
                    return false;
                }
            }
        }

        #endregion
    }
}
