// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Events;
using fCraft.GUI;
using fCraft.MapConversion;
using JetBrains.Annotations;
using Mono.Options;

namespace fCraft.MapRenderer {
    static class MapRenderer {
        static readonly BlockingQueue<RenderTask> ResultQueue = new BlockingQueue<RenderTask>();
        static readonly BlockingQueue<RenderTask> WorkQueue = new BlockingQueue<RenderTask>();
        static readonly Queue<RenderTask> InputPaths = new Queue<RenderTask>();

        static readonly DateTime StartTime = DateTime.UtcNow;

        static readonly MapRendererParams p = new MapRendererParams();
        static string importerName;


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
                    Console.Error.WriteLine( "Unsupported importer \"{0}\"", importerName );
                    PrintUsage();
                    return (int)ReturnCode.UnrecognizedImporter;
                }
                p.MapImporter = MapUtility.GetImporter( importFormat );
                if( p.MapImporter == null ) {
                    Console.Error.WriteLine( "Loading from \"{0}\" is not supported", importFormat );
                    PrintUsage();
                    return (int)ReturnCode.UnsupportedLoadFormat;
                }
            }

            // check input paths
            bool hadFile = false,
                 hadDir = false;
            foreach( string inputPath in p.InputPathList ) {
                if( hadDir ) {
                    Console.Error.WriteLine( "MapRenderer: Only one directory may be specified at a time." );
                    return (int)ReturnCode.ArgumentError;
                }
                // check if input path exists, and if it's a file or directory
                try {
                    if( File.Exists( inputPath ) ) {
                        hadFile = true;
                    } else if( Directory.Exists( inputPath ) ) {
                        hadDir = true;
                        if( hadFile ) {
                            Console.Error.WriteLine( "MapRenderer: Cannot mix directories and files in input." );
                            return (int)ReturnCode.ArgumentError;
                        }
                        p.DirectoryMode = true;
                        if( !p.OutputDirGiven ) {
                            p.OutputDirName = inputPath;
                        }
                    } else {
                        Console.Error.WriteLine( "MapRenderer: Cannot locate \"{0}\"", inputPath );
                        return (int)ReturnCode.InputPathNotFound;
                    }
                } catch( Exception ex ) {
                    Console.Error.WriteLine( "MapRenderer: {0}: {1}",
                                             ex.GetType().Name,
                                             ex.Message );
                    return (int)ReturnCode.PathError;
                }
            }

            // initialize image encoder
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            p.ImageEncoder = codecs.FirstOrDefault( codec => codec.FormatID == p.ExportFormat.Guid );
            if( p.ImageEncoder == null ) {
                Console.Error.WriteLine( "MapRenderer: Specified image encoder is not supported." );
                return (int)ReturnCode.UnsupportedSaveFormat;
            }

            // check recursive flag
            if( p.Recursive && !p.DirectoryMode ) {
                Console.Error.WriteLine( "MapRenderer: Recursive flag is given, but input is not a directory." );
                return (int)ReturnCode.ArgumentError;
            }

            // check input filter
            if( p.InputFilter != null && !p.DirectoryMode ) {
                Console.Error.WriteLine( "MapRenderer: Filter param is given, but input is not a directory." );
                return (int)ReturnCode.ArgumentError;
            }

            // check regex filter
            if( p.UseRegex ) {
                try {
                    p.FilterRegex = new Regex( p.InputFilter );
                } catch( ArgumentException ex ) {
                    Console.Error.WriteLine( "MapRenderer: Cannot parse filter regex: {0}",
                                             ex.Message );
                    return (int)ReturnCode.ArgumentError;
                }
            }

            // check if output dir exists; create it if needed
            if( p.OutputDirName != null ) {
                try {
                    if( !Directory.Exists( p.OutputDirName ) ) {
                        Directory.CreateDirectory( p.OutputDirName );
                    }
                } catch( Exception ex ) {
                    Console.Error.WriteLine( "MapRenderer: Error checking output directory: {0}: {1}",
                                             ex.GetType().Name, ex.Message );
                }
            }

            Console.Write( "Counting files... " );

            // process inputs, one path at a time
            foreach( string inputPath in p.InputPathList ) {
                ProcessInputPath( inputPath );
            }
            int totalFiles = InputPaths.Count;
            Console.WriteLine( totalFiles );

            if( totalFiles > 0 ) {
                int actualThreadCount = Math.Min( p.ThreadCount, InputPaths.Count );
                RenderWorker[] workers = new RenderWorker[actualThreadCount];
                for( int i = 0; i < workers.Length; i++ ) {
                    workers[i] = new RenderWorker( WorkQueue, ResultQueue, p );
                    workers[i].Start();
                }

                int inputsProcessed = 0;
                int resultsProcessed = 0;
                while( resultsProcessed < totalFiles ) {
                    if( inputsProcessed < totalFiles ) {
                        // load and enqueue another map for rendering
                        if( WorkQueue.Count < actualThreadCount ) {
                            RenderTask newTask = InputPaths.Dequeue();
                            if( LoadMap( newTask ) ) {
                                WorkQueue.Enqueue( newTask );
                            } else {
                                resultsProcessed++;
                            }
                            inputsProcessed++;
                        } else {
                            Thread.Sleep( 1 );
                        }

                        // try dequeue a rendered image for saving
                        RenderTask resultTask;
                        if( ResultQueue.TryDequeue( out resultTask ) ) {
                            int percent = (resultsProcessed*100+100)/totalFiles;
                            SaveImage( percent, resultTask );
                            resultsProcessed++;
                        }

                    } else {
                        // no more maps to load -- just wait for results
                        int percent = (resultsProcessed * 100 + 100) / totalFiles;
                        SaveImage( percent, ResultQueue.WaitDequeue() );
                        resultsProcessed++;
                    }
                }
            }

            Console.WriteLine( "Processed {0} files in {1:0.00} seconds",
                               totalFiles,
                               DateTime.UtcNow.Subtract( StartTime ).TotalSeconds );
            return (int)ReturnCode.Success;
        }


        static bool LoadMap( RenderTask task ) {
            try {
                Map map;
                if( p.MapImporter != null ) {
                    map = p.MapImporter.Load( task.MapPath );

                } else {
                    map = MapUtility.Load( task.MapPath, p.TryHard );
                }
                task.Map = map;
                return true;

            } catch( NoMapConverterFoundException ) {
                Console.WriteLine( "{0}: skipped", task.RelativeName );
                return false;

            } catch( Exception ex ) {
                Console.WriteLine( "Error loading {0}", task.RelativeName );
                Console.Error.WriteLine( ex );
                return false;
            }
        }


        static void ProcessInputPath( [NotNull] string inputPath ) {
            if( inputPath == null ) throw new ArgumentNullException( "inputPath" );
            if( !p.Recursive && p.MapImporter != null && p.MapImporter.StorageType == MapStorageType.Directory ) {
                // single directory-based map (e.g. Myne)
                if( !p.OutputDirGiven ) {
                    string parentDir = Directory.GetParent( inputPath ).FullName;
                    p.OutputDirName = Paths.GetDirectoryNameOrRoot( parentDir );
                }
                QueueOneMap( new DirectoryInfo( inputPath ), Path.GetDirectoryName( inputPath ) );

            } else if( !p.DirectoryMode ) {
                // single file-based map
                if( !p.OutputDirGiven ) {
                    p.OutputDirName = Paths.GetDirectoryNameOrRoot( inputPath );
                }
                QueueOneMap( new FileInfo( inputPath ), Path.GetFileName( inputPath ) );

            } else {
                // go through all files inside the given directory
                SearchOption recursiveOption = (p.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                DirectoryInfo inputDirInfo = new DirectoryInfo( inputPath );
                string inputDirNormalizedName = Paths.NormalizeDirName( inputDirInfo.FullName );
                if( p.InputFilter == null || p.UseRegex ) p.InputFilter = "*";
                foreach( var file in inputDirInfo.GetFiles( p.InputFilter, recursiveOption ) ) {
                    string relativePath = Paths.MakeRelativePath( inputDirNormalizedName, file.FullName );
                    if( !p.UseRegex || p.FilterRegex.IsMatch( relativePath ) ) {
                        QueueOneMap( file, relativePath );
                    }
                }
                // try to go through all directories as well, for loading directory-based maps
                bool tryLoadDirs = (p.MapImporter == null || p.MapImporter.StorageType == MapStorageType.Directory);
                if( tryLoadDirs ) {
                    foreach( var dir in inputDirInfo.GetDirectories( p.InputFilter, recursiveOption ) ) {
                        string relativePath = Paths.MakeRelativePath( inputDirNormalizedName, Paths.NormalizeDirName( dir.FullName ) );
                        if( !p.UseRegex || p.FilterRegex.IsMatch( relativePath ) ) {
                            QueueOneMap( dir, relativePath );
                        }
                    }
                }
            }
        }


        static void QueueOneMap( [NotNull] FileSystemInfo fileSystemInfo, [NotNull] string relativeName ) {
            if( fileSystemInfo == null ) throw new ArgumentNullException( "fileSystemInfo" );
            if( relativeName == null ) throw new ArgumentNullException( "relativeName" );

            string mapPath = fileSystemInfo.FullName;

            if( p.MapImporter != null && !p.MapImporter.ClaimsName( mapPath ) ) {
                return;
            }

            // if output directory was not given, save to same directory as the mapfile
            if( !p.OutputDirGiven ) {
                p.OutputDirName = Paths.GetDirectoryNameOrRoot( fileSystemInfo.FullName );
            }

            // select target image file name
            string targetFileName;
            if( (fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                targetFileName = fileSystemInfo.Name + p.ImageFileExtension;
            } else {
                targetFileName = Path.GetFileNameWithoutExtension( fileSystemInfo.Name ) + p.ImageFileExtension;
            }

            // get full target image file name, check if it already exists
            string targetPath = Path.Combine( p.OutputDirName, targetFileName );

            InputPaths.Enqueue( new RenderTask( mapPath, targetPath, relativeName ) );
        }


        static void SaveImage( int percentage, RenderTask task ) {
            if( task.Exception != null ) {
                Console.WriteLine( "{0}: Error rendering image", task.RelativeName );
                Console.Error.WriteLine( "{0}: {1}", task.Exception.GetType().Name, task.Exception );
            } else {
                if( !p.AlwaysOverwrite && File.Exists( task.TargetPath ) ) {
                    Console.WriteLine();
                    if( !ShowYesNo( "File \"{0}\" already exists. Overwrite?", Path.GetFileName( task.TargetPath ) ) ) {
                        return;
                    }
                }
                using( FileStream fs = File.OpenWrite( task.TargetPath ) ) {
                    fs.Write( task.Result, 0, task.Result.Length );
                }
                Console.WriteLine( "[{0}%] {1}: ok",
                                   percentage.ToString( CultureInfo.InvariantCulture ).PadLeft( 3 ),
                                   task.RelativeName );
            }
        }


        static void OnLogged( object sender, LogEventArgs e ) {
            switch( e.MessageType ) {
                case LogType.Error:
                case LogType.SeriousError:
                case LogType.Warning:
                    Console.Error.WriteLine( e.Message );
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
            string jpegQualityString = null,
                   imageFormatName = null,
                   angleString = null,
                   isoCatModeName = null,
                   regionString = null,
                   threadCountString = null;

            string importerList = MapUtility.GetImporters().JoinToString( c => c.Format.ToString() );

            bool printHelp = false;

            opts = new OptionSet()
                .Add( "a=|angle=",
                      "Angle (orientation) from which the map is drawn. May be -90, 0, 90, 180, or 270. Default is 0.",
                      o => angleString = o )

                .Add( "d|tryhard",
                      "Try ALL the map converters on map files that cannot be loaded normally.",
                      o => p.TryHard = (o != null) )

                .Add( "e=|export=",
                      "Image format to use for exporting. " +
                      "Supported formats: PNG (default), BMP, GIF, JPEG, TIFF.",
                      o => imageFormatName = o )

                .Add( "f=|filter=",
                      "Pattern to filter input filenames, e.g. \"*.dat\" or \"builder*\". " +
                      "Applicable only when a directory name is given as input.",
                      o => p.InputFilter = o )

                .Add( "g|nogradient",
                      "Disables altitude-based gradient/shading on terrain.",
                      o => p.NoGradient = (o != null) )

                .Add( "i=|importer=",
                      "Optional: Converter used for importing/loading maps. " +
                      "Available importers: Auto (default), " + importerList,
                      o => importerName = o )

                .Add( "l|seethroughlava",
                      "Makes all lava partially see-through, instead of opaque.",
                      o => p.SeeThroughLava = (o != null) )

                .Add( "m=|mode=",
                      "Rendering mode. May be \"normal\" (default), \"cut\" (cuts out a quarter of the map, revealing inside), " +
                      "\"peeled\" (strips the outer-most layer of blocks), \"chunk\" (renders only a specified region of the map).",
                      o => isoCatModeName = o )

                .Add( "o=|output=",
                      "Path to save images to. " +
                      "If not specified, images will be saved to the maps' directories.",
                      o => p.OutputDirName = o )

                .Add( "q=|quality=",
                      "Sets JPEG compression quality. Between 0 and 100. Default is 80. " +
                      "Applicable only when exporting images to .jpg or .jpeg.",
                      o => jpegQualityString = o )

                .Add( "r|recursive",
                      "Look through all subdirectories for map files. " +
                      "Applicable only when a directory name is given as input.",
                      o => p.Recursive = (o != null) )

                .Add( "t=|threads=",
                      "Number of threads to use, to render multiple files in parallel.",
                      o => threadCountString = o )

                .Add( "region=",
                      "Region of the map to render. Should be given in following format: \"region=x1,y1,z1,x2,y2,z2\" " +
                      "Applicable only when rendering mode is set to \"chunk\".",
                      o => regionString = o )

                .Add( "s|noshadows",
                      "Disables rendering of shadows.",
                      o => p.NoShadows = (o != null) )

                .Add( "u|uncropped",
                      "Does not crop the finished map image, leaving some empty space around the edges.",
                      o => p.Uncropped = (o != null) )

                .Add( "w|seethroughwater",
                      "Makes all water see-through, instead of mostly opaque.",
                      o => p.SeeThroughWater = (o != null) )

                .Add( "x|regex",
                      "Enable regular expressions in \"filter\".",
                      o => p.UseRegex = (o != null) )

                .Add( "y|overwrite",
                      "Do not ask for confirmation to overwrite existing files.",
                      o => p.AlwaysOverwrite = (o != null) )

                .Add( "?|h|help",
                      "Prints out the options.",
                      o => printHelp = (o != null) );

            List<string> pathList;
            try {
                pathList = opts.Parse( args );
            } catch( OptionException ex ) {
                Console.Error.Write( "MapRenderer: " );
                Console.Error.WriteLine( ex.Message );
                PrintHelp();
                return ReturnCode.ArgumentError;
            }

            if( printHelp ) {
                PrintHelp();
                Environment.Exit( (int)ReturnCode.Success );
            }

            if( pathList.Count == 0 ) {
                Console.Error.WriteLine( "MapRenderer: At least one file or directory name required." );
                PrintUsage();
                return ReturnCode.ArgumentError;
            }
            p.InputPathList = pathList.ToArray();

            // Parse angle
            int angle = 0;
            if( angleString != null && (!Int32.TryParse( angleString, out angle ) ||
                                         angle != -90 && angle != 0 && angle != 180 && angle != 270) ) {
                Console.Error.WriteLine( "MapRenderer: Angle must be a number: -90, 0, 90, 180, or 270" );
                return ReturnCode.ArgumentError;
            }
            p.Angle = angle;

            // Parse mode
            IsoCatMode mode = IsoCatMode.Normal;
            if( isoCatModeName != null && !EnumUtil.TryParse( isoCatModeName, out mode, true ) ) {
                Console.Error.WriteLine(
                    "MapRenderer: Rendering mode should be: \"normal\", \"cut\", \"peeled\", or \"chunk\"." );
                return ReturnCode.ArgumentError;
            }
            p.Mode = mode;

            // Parse region (if in chunk mode)
            if( mode == IsoCatMode.Chunk ) {
                if( regionString == null ) {
                    Console.Error.WriteLine( "MapRenderer: Region parameter is required when mode is set to \"chunk\"" );
                    return ReturnCode.ArgumentError;
                }
                try {
                    string[] regionParts = regionString.Split( ',' );
                    p.Region = new BoundingBox( Int32.Parse( regionParts[0] ), Int32.Parse( regionParts[1] ),
                                                Int32.Parse( regionParts[2] ),
                                                Int32.Parse( regionParts[3] ), Int32.Parse( regionParts[4] ),
                                                Int32.Parse( regionParts[5] ) );
                } catch {
                    Console.Error.WriteLine(
                        "MapRenderer: Region should be specified in the following format: \"--region=x1,y1,z1,x2,y2,z2\"" );
                }
            } else if( regionString != null ) {
                Console.Error.WriteLine(
                    "MapRenderer: Region parameter is given, but rendering mode was not set to \"chunk\"" );
            }

            // Parse given image format
            if( imageFormatName != null ) {
                if( imageFormatName.Equals( "BMP", StringComparison.OrdinalIgnoreCase ) ) {
                    p.ExportFormat = ImageFormat.Bmp;
                    p.ImageFileExtension = ".bmp";
                } else if( imageFormatName.Equals( "GIF", StringComparison.OrdinalIgnoreCase ) ) {
                    p.ExportFormat = ImageFormat.Gif;
                    p.ImageFileExtension = ".gif";
                } else if( imageFormatName.Equals( "JPEG", StringComparison.OrdinalIgnoreCase ) ||
                           imageFormatName.Equals( "JPG", StringComparison.OrdinalIgnoreCase ) ) {
                    p.ExportFormat = ImageFormat.Jpeg;
                    p.ImageFileExtension = ".jpg";
                } else if( imageFormatName.Equals( "PNG", StringComparison.OrdinalIgnoreCase ) ) {
                    p.ExportFormat = ImageFormat.Png;
                    p.ImageFileExtension = ".png";
                } else if( imageFormatName.Equals( "TIFF", StringComparison.OrdinalIgnoreCase ) ||
                           imageFormatName.Equals( "TIF", StringComparison.OrdinalIgnoreCase ) ) {
                    p.ExportFormat = ImageFormat.Tiff;
                    p.ImageFileExtension = ".tif";
                } else {
                    Console.Error.WriteLine(
                        "MapRenderer: Image file format should be: BMP, GIF, JPEG, PNG, or TIFF" );
                    return ReturnCode.ArgumentError;
                }
            }

            // Parse JPEG quality
            if( jpegQualityString != null ) {
                if( p.ExportFormat.Guid == ImageFormat.Jpeg.Guid ) {
                    int jpegQuality;
                    if( !Int32.TryParse( jpegQualityString, out jpegQuality ) || jpegQuality < 0 || jpegQuality > 100 ) {
                        Console.Error.WriteLine(
                            "MapRenderer: JpegQuality parameter should be a number between 0 and 100" );
                        return ReturnCode.ArgumentError;
                    }
                    p.JpegQuality = jpegQuality;
                } else {
                    Console.Error.WriteLine(
                        "MapRenderer: JpegQuality parameter given, but image export format was not set to \"JPEG\"." );
                }
            }

            if( p.MapImporter != null && p.TryHard ) {
                Console.Error.WriteLine( "MapRenderer: --tryhard flag can only be used when importer is \"auto\"." );
                return ReturnCode.ArgumentError;
            }

            if( p.InputFilter == null && p.UseRegex ) {
                Console.Error.WriteLine( "MapRenderer: --regex flag can only be used when --filter is specified." );
                return ReturnCode.ArgumentError;
            }

            byte tempThreadCount = 2;
            if( threadCountString != null &&
                (!Byte.TryParse( threadCountString, out tempThreadCount ) || tempThreadCount < 1) ) {
                Console.Error.WriteLine( "MapRenderer: --threads flag must be a number between 1 and 255" );
                return ReturnCode.ArgumentError;
            }
            p.ThreadCount = tempThreadCount;

            p.OutputDirGiven = (p.OutputDirName != null);

            return ReturnCode.Success;
        }


        static void PrintUsage() {
            Console.WriteLine( "Usage: MapRenderer [options] \"MapFileOrDirectory\"" );
            Console.WriteLine( "See \"MapRenderer --help\" for more details." );
        }


        static void PrintHelp() {
            Console.WriteLine();
            Console.WriteLine( "Usage: MapRenderer [options] \"MapFileOrDirectory\"" );
            Console.WriteLine();
            opts.WriteOptionDescriptions( Console.Out );
        }


        [StringFormatMethod( "prompt" )]
        static bool ShowYesNo( [NotNull] string prompt, params object[] formatArgs ) {
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