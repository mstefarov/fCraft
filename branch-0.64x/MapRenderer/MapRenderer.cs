// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using fCraft.Events;
using fCraft.GUI;
using fCraft.MapConversion;
using System.Text.RegularExpressions;
using ImageManipulation;
using JetBrains.Annotations;
using Mono.Options;

namespace fCraft.MapRenderer {
    static class MapRenderer {
        static int angle;
        static IsoCatMode mode = IsoCatMode.Normal;
        static ImageFormat exportFormat = ImageFormat.Png;
        static string imageFileExtension = ".png";
        static BoundingBox region = BoundingBox.Empty;
        static int jpegQuality = 80;
        static IMapImporter mapImporter;
        static IsoCat renderer;
        static ImageCodecInfo imageEncoder;
        static Regex filterRegex;

        static string[] inputPathList;

        static bool noGradient,
                    noShadows,
                    seeThroughWater,
                    seeThroughLava,
                    recursive,
                    overwrite,
                    uncropped,
                    useRegex;

        static string outputDirName,
                      inputFilter;


        static int Main( string[] args ) {
            Logger.Logged += OnLogged;
            Logger.DisableFileLogging();

            ReturnCode optionParsingResult = ParseOptions( args );
            if( optionParsingResult != ReturnCode.Success ) {
                return (int)optionParsingResult;
            }

            if( useRegex ) {
                try {
                    filterRegex = new Regex( inputFilter );
                } catch( ArgumentException ex) {
                    Console.WriteLine( "MapRenderer: Cannot parse filter regex: {0}",
                                       ex.Message );
                    return (int)ReturnCode.ArgumentError;
                }
            }

            // create and configure the renderer
            renderer = new IsoCat {
                SeeThroughLava = seeThroughLava,
                SeeThroughWater = seeThroughWater,
                Mode = mode,
                Gradient = !noGradient,
                DrawShadows = !noShadows
            };
            if( mode == IsoCatMode.Chunk ) {
                renderer.ChunkCoords[0] = region.XMin;
                renderer.ChunkCoords[1] = region.YMin;
                renderer.ChunkCoords[2] = region.ZMin;
                renderer.ChunkCoords[3] = region.XMax;
                renderer.ChunkCoords[4] = region.YMax;
                renderer.ChunkCoords[5] = region.ZMax;
            }
            switch( angle ) {
                case 90:
                    renderer.Rotation = 1;
                    break;
                case 180:
                    renderer.Rotation = 2;
                    break;
                case 270:
                case -90:
                    renderer.Rotation = 3;
                    break;
            }

            // initialize image encoder
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            imageEncoder = codecs.FirstOrDefault( codec => codec.FormatID == exportFormat.Guid );
            if( imageEncoder == null ) {
                Console.Error.WriteLine( "MapRenderer: Specified image encoder is not supported." );
                return (int)ReturnCode.UnsupportedSaveFormat;
            }

            foreach( string inputPath in inputPathList ) {
                ReturnCode code = ProcessInputPath( inputPath );
                if( code != ReturnCode.Success ) {
                    return (int)code;
                }
            }
            return (int)ReturnCode.Success;
        }


        static ReturnCode ProcessInputPath( string inputPath ) {
            // check if input path exists, and if it's a file or directory
            bool directoryMode;
            try {
                if( File.Exists( inputPath ) ) {
                    directoryMode = false;
                    if( outputDirName == null ) {
                        outputDirName = Paths.GetDirectoryNameOrRoot( inputPath );
                    }
                } else if( Directory.Exists( inputPath ) ) {
                    directoryMode = true;
                    if( outputDirName == null ) {
                        outputDirName = Paths.GetDirectoryNameOrRoot( inputPath );
                    }
                } else {
                    Console.Error.WriteLine( "MapRenderer: Cannot locate \"{0}\"", inputPath );
                    return ReturnCode.InputDirNotFound;
                }

                if( !Directory.Exists( outputDirName ) ) {
                    Directory.CreateDirectory( outputDirName );
                }
            } catch( Exception ex ) {
                Console.Error.WriteLine( "MapRenderer: {0}: {1}",
                                         ex.GetType().Name,
                                         ex.Message );
                return ReturnCode.PathError;
            }

            // check recursive flag
            if( recursive && !directoryMode ) {
                Console.Error.WriteLine( "MapRenderer: Recursive flag is given, but input is not a directory." );
            }

            // check input filter
            if( inputFilter != null && !directoryMode ) {
                Console.Error.WriteLine( "MapRenderer: Filter param is given, but input is not a directory." );
            }

            if( !recursive && mapImporter != null && mapImporter.StorageType == MapStorageType.Directory ) {
                // single-directory map
                RenderOneMap( new DirectoryInfo( inputPath ) );
            } else if( !directoryMode ) {
                // single-file map
                RenderOneMap( new FileInfo( inputPath ) );
            } else {
                // possible single-directory conversion
                if( !recursive && RenderOneMap( new DirectoryInfo( inputPath ) ) ) {
                    return ReturnCode.Success;
                }

                // go through all files inside the given directory
                SearchOption recursiveOption = ( recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
                DirectoryInfo inputDirInfo = new DirectoryInfo( inputPath );
                if( inputFilter == null || useRegex ) inputFilter = "*";
                foreach( var dir in inputDirInfo.GetDirectories( inputFilter, recursiveOption ) ) {
                    if( !useRegex || filterRegex.IsMatch( dir.Name ) ) {
                        RenderOneMap( dir );
                    }
                }
                foreach( var file in inputDirInfo.GetFiles( inputFilter, recursiveOption ) ) {
                    if( !useRegex || filterRegex.IsMatch( file.Name ) ) {
                        RenderOneMap( file );
                    }
                }
            }

            return ReturnCode.Success;
        }


        static bool RenderOneMap( [NotNull] FileSystemInfo fileSystemInfo ) {
            if( fileSystemInfo == null ) throw new ArgumentNullException( "fileSystemInfo" );

            try {
                Map map;
                if( mapImporter != null ) {
                    if( !mapImporter.ClaimsName( fileSystemInfo.FullName ) ) {
                        return false;
                    }
                    Console.Write( "Loading {0}... ", fileSystemInfo.Name );
                    map = mapImporter.Load( fileSystemInfo.FullName );

                } else {
                    Console.Write( "Checking {0}... ", fileSystemInfo.Name );
                    map = MapUtility.Load( fileSystemInfo.FullName );
                }

                string targetFileName;
                if( ( fileSystemInfo.Attributes & FileAttributes.Directory ) == FileAttributes.Directory ) {
                    targetFileName = fileSystemInfo.Name + imageFileExtension;
                } else {
                    targetFileName = Path.GetFileNameWithoutExtension( fileSystemInfo.Name ) + imageFileExtension;
                }

                string targetPath = Path.Combine( outputDirName, targetFileName );
                if( !overwrite && File.Exists( targetPath ) ) {
                    Console.WriteLine();
                    if( !ShowYesNo( "File \"{0}\" already exists. Overwrite?", targetFileName ) ) {
                        return false;
                    }
                }
                Console.Write( "Drawing... " );
                IsoCatResult result = renderer.Draw( map );
                Console.Write( "Saving {0}... ", Path.GetFileName( targetFileName ) );
                if( uncropped ) {
                    SaveImage( result.Bitmap, targetPath );
                } else {
                    SaveImage( result.Bitmap.Clone( result.CropRectangle, result.Bitmap.PixelFormat ), targetPath );
                }
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


        static void SaveImage( Bitmap image, string targetFileName ) {
            if( exportFormat.Equals( ImageFormat.Jpeg ) ) {
                EncoderParameters encoderParams = new EncoderParameters();
                encoderParams.Param[0] = new EncoderParameter( Encoder.Quality, jpegQuality );
                image.Save( targetFileName, imageEncoder, encoderParams );
            } else if( exportFormat.Equals( ImageFormat.Gif ) ) {
                OctreeQuantizer q = new OctreeQuantizer( 255, 8 );
                image = q.Quantize( image );
                image.Save( targetFileName, exportFormat );
            } else {
                image.Save( targetFileName, exportFormat );
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
                   importerName = null,
                   angleString = null,
                   isoCatModeName = null,
                   regionString = null;

            string importerList = MapUtility.GetImporters().JoinToString( c => c.Format.ToString() );

            bool printHelp = false;

            opts = new OptionSet()
                .Add( "a=|angle=",
                      "Angle (orientation) from which the map is drawn. May be -90, 0, 90, 180, or 270. Default is 0.",
                      o => angleString = o )

                .Add( "f=|filter=",
                      "Pattern to filter input filenames, e.g. \"*.dat\" or \"builder*\". " +
                      "Applicable only when a directory name is given as input.",
                      o => inputFilter = o )

                .Add( "i=|importer=",
                      "Optional: Converter used for importing/loading maps. " +
                      "Available importers: Auto (default), " + importerList,
                      o => importerName = o )

                .Add( "e=|export=",
                      "Image format to use for exporting. " +
                      "Supported formats: PNG (default), BMP, GIF, JPEG, TIFF.",
                      o => imageFormatName = o )

                .Add( "m=|mode=",
                      "Rendering mode. May be \"normal\" (default), \"cut\" (cuts out a quarter of the map, revealing inside), " +
                      "\"peeled\" (strips the outer-most layer of blocks), \"chunk\" (renders only a specified region of the map).",
                      o => isoCatModeName = o )

                .Add( "g|nogradient",
                      "Disables altitude-based gradient/shading on terrain.",
                      o => noGradient = ( o != null ) )

                .Add( "s|noshadows",
                      "Disables rendering of shadows.",
                      o => noShadows = ( o != null ) )

                .Add( "o=|output=",
                      "Path to save images to. " +
                      "If not specified, images will be saved to the maps' directories.",
                      o => outputDirName = o )

                .Add( "x|regex",
                      "Enable regular expessions in \"filter\".",
                      o => useRegex = ( o != null ) )

                .Add( "y|overwrite",
                      "Do not ask for confirmation to overwrite existing files.",
                      o => overwrite = ( o != null ) )

                .Add( "q=|quality=",
                      "Sets JPEG compression quality. Between 0 and 100. Default is 80. " +
                      "Applicable only when exporting images to .jpg or .jpeg.",
                      o => jpegQualityString = o )

                .Add( "r|recursive",
                      "Look through all subdirectories for map files. " +
                      "Applicable only when a directory name is given as input.",
                      o => recursive = ( o != null ) )

                .Add( "region=",
                      "Region of the map to render. Should be given in following format: \"region=x1,y1,z1,x2,y2,z2\" " +
                      "Applicable only when rendering mode is set to \"chunk\".",
                      o => regionString = o )

                .Add( "w|seethroughwater",
                      "Makes all water see-through, instead of mostly opaque.",
                      o => seeThroughWater = ( o != null ) )

                .Add( "l|seethroughlava",
                      "Makes all lava partially see-through, instead of opaque.",
                      o => seeThroughLava = ( o != null ) )

                .Add( "u|uncropped",
                      "Does not crop the finished map image, leaving some empty space around the edges.",
                      o => uncropped = ( o != null ) )

                .Add( "?|h|help",
                      "Prints out the options.",
                      o => printHelp = ( o != null ) );

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
            inputPathList = pathList.ToArray();

            // Parse angle
            if( angleString != null && ( !Int32.TryParse( angleString, out angle ) ||
                                         angle != -90 && angle != 0 && angle != 180 && angle != 270 ) ) {
                Console.Error.WriteLine( "MapRenderer: Angle must be a number: -90, 0, 90, 180, or 270" );
                return ReturnCode.ArgumentError;
            }

            // Parse mode
            if( isoCatModeName != null && !EnumUtil.TryParse( isoCatModeName, out mode, true ) ) {
                Console.Error.WriteLine(
                    "MapRenderer: Rendering mode should be: \"normal\", \"cut\", \"peeled\", or \"chunk\"." );
                return ReturnCode.ArgumentError;
            }

            // Parse region (if in chunk mode)
            if( mode == IsoCatMode.Chunk ) {
                if( regionString == null ) {
                    Console.Error.WriteLine( "MapRenderer: Region parameter is required when mode is set to \"chunk\"" );
                    return ReturnCode.ArgumentError;
                }
                try {
                    string[] regionParts = regionString.Split( ',' );
                    region = new BoundingBox( Int32.Parse( regionParts[0] ), Int32.Parse( regionParts[1] ),
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
                    exportFormat = ImageFormat.Bmp;
                    imageFileExtension = ".bmp";
                } else if( imageFormatName.Equals( "GIF", StringComparison.OrdinalIgnoreCase ) ) {
                    exportFormat = ImageFormat.Gif;
                    imageFileExtension = ".gif";
                } else if( imageFormatName.Equals( "JPEG", StringComparison.OrdinalIgnoreCase ) ||
                           imageFormatName.Equals( "JPG", StringComparison.OrdinalIgnoreCase ) ) {
                    exportFormat = ImageFormat.Jpeg;
                    imageFileExtension = ".jpg";
                } else if( imageFormatName.Equals( "PNG", StringComparison.OrdinalIgnoreCase ) ) {
                    exportFormat = ImageFormat.Png;
                    imageFileExtension = ".png";
                } else if( imageFormatName.Equals( "TIFF", StringComparison.OrdinalIgnoreCase ) ||
                           imageFormatName.Equals( "TIF", StringComparison.OrdinalIgnoreCase ) ) {
                    exportFormat = ImageFormat.Tiff;
                    imageFileExtension = ".tif";
                } else {
                    Console.Error.WriteLine(
                        "MapRenderer: Image file format should be: BMP, GIF, JPEG, PNG, or TIFF" );
                    return ReturnCode.ArgumentError;
                }
            }

            // Parse JPEG quality
            if( jpegQualityString != null ) {
                if( exportFormat == ImageFormat.Jpeg ) {
                    if( !Int32.TryParse( jpegQualityString, out jpegQuality ) || jpegQuality < 0 || jpegQuality > 100 ) {
                        Console.Error.WriteLine(
                            "MapRenderer: JpegQuality parameter should be a number between 0 and 100" );
                        return ReturnCode.ArgumentError;
                    }
                } else {
                    Console.Error.WriteLine(
                        "MapRenderer: JpegQuality parameter given, but image export format was not set to \"JPEG\"." );
                }
            }

            // parse importer name
            if( importerName != null && !importerName.Equals( "auto", StringComparison.OrdinalIgnoreCase ) ) {
                MapFormat importFormat;
                if( !EnumUtil.TryParse( importerName, out importFormat, true ) ||
                    ( mapImporter = MapUtility.GetImporter( importFormat ) ) == null ) {
                    Console.Error.WriteLine( "Unsupported importer \"{0}\"", importerName );
                    PrintUsage();
                    return ReturnCode.UnrecognizedImporter;
                }
            }

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