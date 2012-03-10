// Part of fCraft | Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | MIT License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;
using fCraft.Events;
using Mono.Options;

namespace fCraft.MapConverter {
    static class Program {
        static IMapConverter[] allConverters;

        static string importerName,
                      exporterName,
                      inputPath,
                      outputDirName,
                      inputFilter;

        static IMapConverter importer;
        static IMapConverter exporter;

        static bool recursive;

        static OptionSet opts;


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
                Console.Error.WriteLine( "fCraft does not support exporting to {0} format.", exporter );
                return (int)ReturnCode.UnsupportedSaveFormat;
            }

            // check if input path exists, and if it's a file or directory
            bool directoryMode;
            string outputDirectory;
            try {
                if( File.Exists( inputPath ) ) {
                    directoryMode = false;
                    outputDirectory = Path.GetDirectoryName( Path.GetFullPath( inputPath ) );

                } else if( Directory.Exists( inputPath ) ) {
                    directoryMode = true;
                    outputDirectory = Path.GetFullPath( inputPath );

                } else {
                    Console.Error.WriteLine( "MapConverter: Cannot locate \"{0}\"", inputPath );
                    return (int)ReturnCode.InputDirNotFound;
                }

                if( outputDirName != null ) {
                    outputDirectory = Path.GetFullPath( outputDirName );
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
                ConvertOneMap( outputDirectory, new DirectoryInfo( inputPath ) );
            } else if( !directoryMode ) {
                ConvertOneMap( outputDirectory, new FileInfo( inputPath ) );
            } else {
                SearchOption recursiveOption = ( recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
                DirectoryInfo inputDirInfo = new DirectoryInfo( inputPath );
                if( inputFilter == null ) inputFilter = "*";
                foreach( FileSystemInfo dirInfo in inputDirInfo.EnumerateFileSystemInfos( inputFilter, recursiveOption )
                    ) {
                    ConvertOneMap( outputDirectory, dirInfo );
                }
            }

            return (int)ReturnCode.Success;
        }


        static void ConvertOneMap( string outputDir, FileSystemInfo fileSystemInfo ) {
            try {
                if( !importer.ClaimsName( fileSystemInfo.FullName ) ) {
                    Console.WriteLine( "Skipping {0}", fileSystemInfo.Name );
                    return;
                }

                Console.Write( "Loading {0}... ", fileSystemInfo.Name );
                Map map;
                if( importer != null ) {
                    map = importer.Load( fileSystemInfo.FullName );
                } else {
                    map = MapUtility.Load( fileSystemInfo.FullName );
                }

                string targetFileName;
                if( ( fileSystemInfo.Attributes & FileAttributes.Directory ) == FileAttributes.Directory ) {
                    targetFileName = fileSystemInfo.Name + '.' + exporter.FileExtension;
                } else {
                    targetFileName = Path.GetFileNameWithoutExtension( fileSystemInfo.Name ) + '.' + exporter.FileExtension;
                }
                Console.Write( "Saving {0}... ", Path.GetFileName( targetFileName ) );
                exporter.Save( map, Path.Combine( outputDir, targetFileName ) );
                Console.WriteLine( "ok" );

            } catch( Exception ex ) {
                Console.WriteLine( "ERROR" );
                Console.Error.WriteLine( "{0}: {1}", ex.GetType().Name, ex.Message );
            }
        }


        static void ParseOptions( string[] args ) {
            string importerList = allConverters.JoinToString( c => c.Format.ToString() );
            string exporterList = allConverters.Where( c => c.SupportsExport ).JoinToString( c => c.Format.ToString() );

            bool printHelp = false;

            opts = new OptionSet()
                .Add( "?|help|h",
                      "Prints out the options.",
                      o => printHelp = ( o != null ) )

                .Add( "i=|importer=",
                      "Optional: Converter used for importing/loading maps. " +
                      "Available importers: " + importerList,
                      o => importerName = o )

                .Add( "e=|exporter=",
                      "REQUIRED: Converter used for exporting/saving maps. " +
                      "Available exporters: " + exporterList,
                      o => exporterName = o )

                .Add( "o=|output=",
                      "Optional: Path to save converted map files to. " +
                      "If not specified, converted maps will be saved to the original maps' directory.",
                      o => outputDirName = o )

                .Add( "r|recursive",
                      "Optional: Look through all subdirectories, and convert map files there too.",
                      o => recursive = ( o != null ) )

                .Add( "f=|filter=",
                      "Optional: Pattern to filter input filenames, e.g. \"*.dat\" or \"builder*\"",
                      o => inputFilter = o );

            List<string> pathList = new List<string>();
            try {
                pathList = opts.Parse( args );
            } catch( OptionException ex ) {
                Console.Error.Write( "MapConverter: " );
                Console.Error.WriteLine( ex.Message );
                PrintUsage();
                Environment.Exit( (int)ReturnCode.ArgumentParsingError );
            }

            if( printHelp ) {
                PrintUsage();
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


        static void PrintUsage() {
            opts.WriteOptionDescriptions( Console.Out );
        }
    }

    enum ReturnCode {
        Success = 0,
        ArgumentParsingError = 1,
        UnrecognizedImporter = 2,
        UnrecognizedExporter = 3,
        InputDirNotFound = 4,
        PathError = 5,
        ErrorOpeningDirForSaving = 6,
        UnsupportedSaveFormat = 7
    }
}
