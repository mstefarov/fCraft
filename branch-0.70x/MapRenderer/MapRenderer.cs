// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using JetBrains.Annotations;
using Mono.Options;
using fCraft.Events;
using fCraft.GUI;
using fCraft.MapConversion;

namespace fCraft.MapRenderer {
    static class MapRenderer {
        static int angle;
        static IsoCatMode mode = IsoCatMode.Normal;
        static ImageFormat format = ImageFormat.Png;
        static BoundingBox region = BoundingBox.Empty;
        static int jpegQuality = 80;
        static IMapImporter importer;

        static bool noGradient, noShadows, seeThroughWater, seeThroughLava, recursive, overwrite;
        static string inputPath, angleString, isoCatModeName, outputDirName, regionString, inputFilter, imageFormatName, jpegQualityString, importerName;

        static int Main( string[] args ) {
            Logger.Logged += OnLogged;

            ReturnCode optionParsingResult = ParseOptions( args );
            if( optionParsingResult != ReturnCode.Success ) {
                return (int)optionParsingResult;
            }

            // check if input path exists, and if it's a file or directory
            bool directoryMode;
            try {
                if( File.Exists( inputPath ) ) {
                    directoryMode = false;
                    if( outputDirName == null ) {
                        outputDirName = Paths.GetDirNameOrPathRoot( inputPath );
                    }

                } else if( Directory.Exists( inputPath ) ) {
                    directoryMode = true;
                    if( outputDirName == null ) {
                        outputDirName = Paths.GetDirNameOrPathRoot( inputPath );
                    }

                } else {
                    Console.Error.WriteLine( "MapRenderer: Cannot locate \"{0}\"", inputPath );
                    return (int)ReturnCode.InputDirNotFound;
                }

                if( !Directory.Exists( outputDirName ) ) {
                    Directory.CreateDirectory( outputDirName );
                }

            } catch( Exception ex ) {
                Console.Error.WriteLine( "MapRenderer: {0}: {1}",
                                         ex.GetType().Name,
                                         ex.Message );
                return (int)ReturnCode.PathError;
            }

            // check recursive flag
            if( recursive && !directoryMode ) {
                Console.Error.WriteLine( "MapRenderer: Recursive flag is given, but input is not a directory." );
            }

            // check input filter
            if( inputFilter != null && !directoryMode ) {
                Console.Error.WriteLine( "MapRenderer: Filter param is given, but input is not a directory." );
            }

            return (int)ReturnCode.Success;
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

            bool printHelp = false;

            opts = new OptionSet()
                .Add( "a=|angle=",
                      "Angle to view the map from. May be -90, 0, 90, 180, or 270. Default is 0.",
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
                      "Supported formats: BMP, GIF, JPEG, PNG, TIFF. Default: PNG.",
                      o => imageFormatName = o )

                .Add( "m=|mode=",
                      "Rendering mode. May be \"normal\", \"cut\" (cuts out a quarter of the map, revealing inside), " +
                      "\"peeled\" (strips the outer-most layer of blocks), \"chunk\" (renders only a specified region of the map). " +
                      "Default is \"normal\".",
                      o => isoCatModeName = o )

                .Add( "g|nogradient",
                      "Disables gradient shading.",
                      o => noGradient = ( o != null ) )

                .Add( "s|noshadows",
                      "Disables rendering of shadows.",
                      o => noShadows = ( o != null ) )

                .Add( "o=|output=",
                      "Path to save images to. " +
                      "If not specified, images will be saved to the original maps' directory.",
                      o => outputDirName = o )

                .Add( "y|overwrite",
                      "Do not ask for confirmation to overwrite existing files.",
                      o => overwrite = ( o != null ) )

                .Add( "q=|quality=",
                      "Sets JPEG compression quality. Between 0 and 100. Default is 80." +
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
                      "Makes all lava partially see-through, instead of mostly opaque.",
                      o => seeThroughLava = ( o != null ) )

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
                return ReturnCode.ArgumentParsingError;
            }

            if( printHelp ) {
                PrintHelp();
                return ReturnCode.Success;
            }

            if( pathList.Count != 1 ) {
                Console.Error.WriteLine( "MapRenderer: At least one file or directory name required." );
                PrintUsage();
                return ReturnCode.ArgumentParsingError;
            }
            inputPath = pathList[0];
            
            // Parse angle
            if( angleString != null && ( !Int32.TryParse( angleString, out angle ) ||
                                         angle != -90 && angle != 0 && angle != 180 && angle != 270 ) ) {
                Console.Error.WriteLine( "MapRenderer: Angle must be a number: -90, 0, 90, 180, or 270" );
                return ReturnCode.ArgumentParsingError;
            }

            // Parse mode
            if( isoCatModeName != null && !Enum.TryParse( isoCatModeName, out mode ) ) {
                Console.Error.WriteLine( "MapRenderer: Rendering mode should be: \"normal\", \"cut\", \"peel\", or \"chunk\"." );
                return ReturnCode.ArgumentParsingError;
            }

            // Parse region (if in chunk mode)
            if( mode == IsoCatMode.Chunk ) {
                if( regionString == null ) {
                    Console.Error.WriteLine( "MapRenderer: Region parameter is required when mode is set to \"chunk\"" );
                    return ReturnCode.ArgumentParsingError;
                }
                try {
                    string[] regionParts = regionString.Split( ',' );
                    region = new BoundingBox( Int32.Parse( regionParts[0] ), Int32.Parse( regionParts[1] ), Int32.Parse( regionParts[2] ),
                                              Int32.Parse( regionParts[3] ), Int32.Parse( regionParts[4] ), Int32.Parse( regionParts[5] ) );
                } catch {
                    Console.Error.WriteLine( "MapRenderer: Region should be specified in the following format: \"--region=x1,y1,z1,x2,y2,z2\"" );
                }
            } else if( regionString != null ) {
                Console.Error.WriteLine( "MapRenderer: Region parameter is given, but rendering mode was not set to \"chunk\"" );
            }

            // Parse given image format
            if( imageFormatName != null ) {
                if( imageFormatName.Equals( "BMP", StringComparison.OrdinalIgnoreCase ) ) {
                    format = ImageFormat.Bmp;
                }else if( imageFormatName.Equals( "GIF", StringComparison.OrdinalIgnoreCase ) ) {
                    format = ImageFormat.Gif;
                } else if( imageFormatName.Equals( "JPEG", StringComparison.OrdinalIgnoreCase ) || imageFormatName.Equals( "JPG", StringComparison.OrdinalIgnoreCase ) ) {
                    format = ImageFormat.Jpeg;
                }else if( imageFormatName.Equals( "PNG", StringComparison.OrdinalIgnoreCase ) ) {
                    format = ImageFormat.Png;
                } else if( imageFormatName.Equals( "TIFF", StringComparison.OrdinalIgnoreCase ) || imageFormatName.Equals( "TIF", StringComparison.OrdinalIgnoreCase ) ) {
                    format = ImageFormat.Tiff;
                } else {
                    Console.Error.WriteLine( "MapRenderer: Image file format should be: BMP, GIF, JPEG, PNG, or TIFF" );
                    return ReturnCode.ArgumentParsingError;
                }
            }

            // Parse JPEG quality
            if( jpegQualityString != null ) {
                if( format == ImageFormat.Jpeg ) {
                    if( !Int32.TryParse( jpegQualityString, out jpegQuality ) || jpegQuality < 0 || jpegQuality > 100 ) {
                        Console.Error.WriteLine( "MapRenderer: JpegQuality parameter should be a number between 0 and 100" );
                        return ReturnCode.ArgumentParsingError;
                    }
                } else {
                    Console.Error.WriteLine( "MapRenderer: JpegQuality parameter given, but image export format was not set to \"JPEG\"." );
                }
            }

            // parse importer name
            if( importerName != null && !importerName.Equals( "auto", StringComparison.OrdinalIgnoreCase ) ) {
                MapFormat importFormat;
                if( !Enum.TryParse( importerName, true, out importFormat ) ||
                    ( importer = MapUtility.GetImporter( importFormat ) ) == null ) {
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
