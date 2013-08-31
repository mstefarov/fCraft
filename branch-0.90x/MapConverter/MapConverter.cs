// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.IO;
using fCraft.Events;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.Text.RegularExpressions;
using Mono.Options;

namespace fCraft.MapConverter {
    static class MapConverter {
        static string importerName,
                      exporterName,
                      outputDirName,
                      inputFilter;

        static Regex filterRegex;
        static IMapImporter importer;
        static IMapExporter exporter;

        static bool recursive,
                    overwrite,
                    useRegex,
                    directoryMode,
                    outputDirGiven,
                    tryHard;

        static string[] inputPathList;


        static int Main( string[] args ) {
            Logger.Logged += OnLogged;
            Logger.DisableFileLogging();

            ReturnCode optionParsingResult = ParseOptions( args );
            if( optionParsingResult != ReturnCode.Success ) {
                return (int)optionParsingResult;
            }

            // parse importer name
            if( importerName != null && !importerName.Equals( "auto", StringComparison.OrdinalIgnoreCase ) ) {
                MapFormat importFormat;
                if( !EnumUtil.TryParse( importerName, out importFormat, true ) ) {
                    Console.Error.WriteLine( "MapConverter: Unsupported importer \"{0}\"", importerName );
                    PrintUsage();
                    return (int)ReturnCode.UnrecognizedImporter;
                }
                importer = MapUtility.GetImporter( importFormat );
                if( importer == null ) {
                    Console.Error.WriteLine( "MapConverter: Loading from \"{0}\" is not supported", importFormat );
                    PrintUsage();
                    return (int)ReturnCode.UnsupportedLoadFormat;
                }
            }

            // parse exporter format
            MapFormat exportFormat;
            if( !EnumUtil.TryParse( exporterName, out exportFormat, true ) ) {
                Console.Error.WriteLine( "MapConverter: Unrecognized exporter \"{0}\"", exporterName );
                PrintUsage();
                return (int)ReturnCode.UnrecognizedExporter;
            }
            exporter = MapUtility.GetExporter( exportFormat );
            if( exporter == null ) {
                Console.Error.WriteLine( "MapConverter: Saving to \"{0}\" is not supported", exportFormat );
                PrintUsage();
                return (int)ReturnCode.UnsupportedSaveFormat;
            }

            // check input paths
            bool hadFile = false,
                 hadDir = false;
            foreach( string inputPath in inputPathList ) {
                if( hadDir ) {
                    Console.Error.WriteLine( "MapConverter: Only one directory may be specified at a time." );
                    return (int)ReturnCode.ArgumentError;
                }
                // check if input path exists, and if it's a file or directory
                try {
                    if( File.Exists( inputPath ) ) {
                        hadFile = true;
                    } else if( Directory.Exists( inputPath ) ) {
                        hadDir = true;
                        if( hadFile ) {
                            Console.Error.WriteLine( "MapConverter: Cannot mix directories and files in input." );
                            return (int)ReturnCode.ArgumentError;
                        }
                        directoryMode = true;
                        if( !outputDirGiven ) {
                            outputDirName = inputPath;
                        }
                    } else {
                        Console.Error.WriteLine( "MapConverter: Cannot locate \"{0}\"", inputPath );
                        return (int)ReturnCode.InputPathNotFound;
                    }
                } catch( Exception ex ) {
                    Console.Error.WriteLine( "MapConverter: {0}: {1}",
                                             ex.GetType().Name,
                                             ex.Message );
                    return (int)ReturnCode.PathError;
                }
            }

            // check recursive flag
            if( recursive && !directoryMode ) {
                Console.Error.WriteLine( "MapConverter: Recursive flag is given, but input is not a directory." );
                return (int)ReturnCode.ArgumentError;
            }

            // check input filter
            if( inputFilter != null && !directoryMode ) {
                Console.Error.WriteLine( "MapConverter: Filter param is given, but input is not a directory." );
                return (int)ReturnCode.ArgumentError;
            }

            // check regex filter
            if( useRegex ) {
                try {
                    filterRegex = new Regex( inputFilter );
                } catch( ArgumentException ex ) {
                    Console.Error.WriteLine( "MapConverter: Cannot parse filter regex: {0}",
                                             ex.Message );
                    return (int)ReturnCode.ArgumentError;
                }
            }

            // check if output dir exists; create it if needed
            if( outputDirName != null ) {
                try {
                    if( !Directory.Exists( outputDirName ) ) {
                        Directory.CreateDirectory( outputDirName );
                    }
                } catch( Exception ex ) {
                    Console.Error.WriteLine( "MapRenderer: Error checking output directory: {0}: {1}",
                                             ex.GetType().Name,
                                             ex.Message );
                }
            }

            // process inputs, one path at a time
            foreach( string inputPath in inputPathList ) {
                ReturnCode code = ProcessInputPath( inputPath );
                if( code != ReturnCode.Success ) {
                    return (int)code;
                }
            }
            return (int)ReturnCode.Success;
        }


        static ReturnCode ProcessInputPath( [NotNull] string inputPath ) {
            if( inputPath == null ) throw new ArgumentNullException( "inputPath" );
            if( !recursive && importer != null && importer.StorageType == MapStorageType.Directory ) {
                // single directory-based map (e.g. Myne)
                if( !outputDirGiven ) {
                    string parentDir = Directory.GetParent( inputPath ).FullName;
                    outputDirName = Paths.GetDirectoryNameOrRoot( parentDir );
                }
                ConvertOneMap( new DirectoryInfo( inputPath ), Path.GetDirectoryName( inputPath ) );

            } else if( !directoryMode ) {
                // single file-based map
                if( !outputDirGiven ) {
                    outputDirName = Paths.GetDirectoryNameOrRoot( inputPath );
                }
                ConvertOneMap( new FileInfo( inputPath ), Path.GetFileName( inputPath ) );

            } else {
                // go through all files inside the given directory
                SearchOption recursiveOption = (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                DirectoryInfo inputDirInfo = new DirectoryInfo( inputPath );
                string inputDirNormalizedName = Paths.NormalizeDirName( inputDirInfo.FullName );
                if( inputFilter == null || useRegex ) inputFilter = "*";
                foreach( var file in inputDirInfo.GetFiles( inputFilter, recursiveOption ) ) {
                    string relativePath = Paths.MakeRelativePath( inputDirNormalizedName, file.FullName );
                    if( !useRegex || filterRegex.IsMatch( relativePath ) ) {
                        ConvertOneMap( file, relativePath );
                    }
                }
                // try to go through all directories as well, for loading directory-based maps
                bool tryLoadDirs = (importer == null || importer.StorageType == MapStorageType.Directory);
                if( tryLoadDirs ) {
                    foreach( var dir in inputDirInfo.GetDirectories( inputFilter, recursiveOption ) ) {
                        string relativePath = Paths.MakeRelativePath( inputDirNormalizedName,
                                                                      Paths.NormalizeDirName( dir.FullName ) );
                        if( !useRegex || filterRegex.IsMatch( relativePath ) ) {
                            ConvertOneMap( dir, relativePath );
                        }
                    }
                }
            }
            return ReturnCode.Success;
        }


        static bool ConvertOneMap( [NotNull] FileSystemInfo fileSystemInfo, [NotNull] string relativeName ) {
            if( fileSystemInfo == null ) throw new ArgumentNullException( "fileSystemInfo" );
            if( relativeName == null ) throw new ArgumentNullException( "relativeName" );

            try {
                // if output directory was not given, save to same directory as the map file
                if( !outputDirGiven ) {
                    outputDirName = Paths.GetDirectoryNameOrRoot( fileSystemInfo.FullName );
                }

                // load the map file
                Map map;
                if( importer != null ) {
                    if( !importer.ClaimsName( fileSystemInfo.FullName ) ) {
                        return false;
                    }
                    Console.Write( "Loading {0}... ", relativeName );
                    map = importer.Load( fileSystemInfo.FullName );

                } else {
                    Console.Write( "Checking {0}... ", relativeName );
                    map = MapUtility.Load( fileSystemInfo.FullName, tryHard );
                }

                // select target map file name
                string targetFileName;
                if( (fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                    targetFileName = fileSystemInfo.Name + '.' + exporter.FileExtension;
                } else {
                    targetFileName = Path.GetFileNameWithoutExtension( fileSystemInfo.Name ) + '.' +
                                     exporter.FileExtension;
                }

                // get full target map file name, check if it already exists
                string targetPath = Path.Combine( outputDirName, targetFileName );
                if( !overwrite && File.Exists( targetPath ) ) {
                    Console.WriteLine();
                    if( !ShowYesNo( "File \"{0}\" already exists. Overwrite?", targetFileName ) ) {
                        return false;
                    }
                }

                // save
                Console.Write( "Saving {0}... ", Path.GetFileName( targetFileName ) );
                exporter.Save( map, targetPath );
                Console.WriteLine( "ok" );
                return true;

            } catch( NoMapConverterFoundException ) {
                Console.WriteLine( "skip" );
                return false;

            } catch( Exception ex ) {
                Console.WriteLine( "ERROR" );
                Console.Error.WriteLine( "{0}: {1}", ex.GetType().Name, ex.Message );
                return false;
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

        static ReturnCode ParseOptions( [NotNull] string[] args ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            string importerList = MapUtility.GetImporters().JoinToString( c => c.Format.ToString() );
            string exporterList = MapUtility.GetExporters().JoinToString( c => c.Format.ToString() );

            bool printHelp = false;

            opts = new OptionSet()
                .Add( "e=|exporter=",
                      "REQUIRED: Converter used for exporting/saving maps. " +
                      "Available exporters: " + exporterList,
                      o => exporterName = o )

                .Add( "f=|filter=",
                      "Optional: Pattern to filter input filenames, e.g. \"*.dat\" or \"builder*\". " +
                      "Applicable only when a directory name is given as input.",
                      o => inputFilter = o )

                .Add( "i=|importer=",
                      "Optional: Converter used for importing/loading maps. " +
                      "Available importers: Auto (default), " + importerList,
                      o => importerName = o )

                .Add( "o=|output=",
                      "Optional: Path to save converted map files to. " +
                      "If not specified, converted maps will be saved to the original maps' directory.",
                      o => outputDirName = o )

                .Add( "r|recursive",
                      "Optional: Look through all subdirectories, and convert map files there too. " +
                      "Applicable only when a directory name is given as input.",
                      o => recursive = (o != null) )

                .Add( "t|tryhard",
                      "Try ALL the map converters on map files that cannot be loaded normally.",
                      o => tryHard = (o != null) )

                .Add( "x|regex",
                      "Enable regular expessions in \"filter\".",
                      o => useRegex = (o != null) )

                .Add( "y|overwrite",
                      "Optional: Do not ask for confirmation to overwrite existing files.",
                      o => overwrite = (o != null) )

                .Add( "?|help|h",
                      "Prints usage information and a list of options.",
                      o => printHelp = (o != null) );

            List<string> pathList;
            try {
                pathList = opts.Parse( args );
            } catch( OptionException ex ) {
                Console.Error.Write( "MapConverter: " );
                Console.Error.WriteLine( ex.Message );
                PrintHelp();
                return ReturnCode.ArgumentError;
            }

            // Print help and break out
            if( printHelp ) {
                PrintHelp();
                Environment.Exit( (int)ReturnCode.Success );
            }

            if( pathList.Count != 1 ) {
                Console.Error.WriteLine( "MapConverter: At least one file or directory name required." );
                PrintUsage();
                return ReturnCode.ArgumentError;
            }
            inputPathList = pathList.ToArray();

            if( exporterName == null ) {
                Console.Error.WriteLine( "MapConverter: Export format required." );
                PrintUsage();
                return ReturnCode.ArgumentError;
            }

            outputDirGiven = (outputDirName != null);

            return ReturnCode.Success;
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
                string input = Console.ReadLine();

                if( input == null ||
                    input.Equals( "no", StringComparison.OrdinalIgnoreCase ) ||
                    input.Equals( "n", StringComparison.OrdinalIgnoreCase ) ) {
                    return false;
                } else if( input.Equals( "yes", StringComparison.OrdinalIgnoreCase ) ||
                           input.Equals( "y", StringComparison.OrdinalIgnoreCase ) ) {
                    return true;
                }
            }
        }

        #endregion
    }
}