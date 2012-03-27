// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Options;
using fCraft.Events;
using fCraft.MapConversion;

namespace fCraft.MapRenderer {
    class MapRenderer {
        static bool noGradient, noShadows, seeThroughWater, seeThroughLava, recursive, overwrite;
        static string inputPath, angleString, isoCatModeName, outputDirName, regionString, inputFilter;

        static void Main( string[] args ) {
            Logger.Logged += OnLogged;

            ParseOptions( args );
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
            string importerList = MapUtility.GetImporters().JoinToString( c => c.Format.ToString() );
            string exporterList = MapUtility.GetExporters().JoinToString( c => c.Format.ToString() );

            bool printHelp = false;

            opts = new OptionSet()
                .Add( "a=|angle=",
                      "Angle to view the map from. May be -90, 0, 90, 180, or 270. Default is 0.",
                      o => angleString = o )

                .Add( "f=|filter=",
                      "Pattern to filter input filenames, e.g. \"*.dat\" or \"builder*\". " +
                      "Applicable only when a directory name is given as input.",
                      o => inputFilter = o )

                .Add( "i=|image=",
                      "Image format to use for exporting. " +
                      "Supported formats: BMP, GIF, JPEG, PNG, TIFF. Default: PNG.",
                      o => outputDirName = o )

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
                      o => seeThroughLava = ( o != null ) )

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

            List<string> pathList = new List<string>();
            try {
                pathList = opts.Parse( args );
            } catch( OptionException ex ) {
                Console.Error.Write( "MapRenderer: " );
                Console.Error.WriteLine( ex.Message );
                PrintHelp();
                Environment.Exit( (int)ReturnCode.ArgumentParsingError );
            }

            if( printHelp ) {
                PrintHelp();
                Environment.Exit( (int)ReturnCode.Success );
            }

            if( pathList.Count != 1 ) {
                Console.Error.WriteLine( "MapRenderer: At least one file or directory name required." );
                PrintUsage();
                Environment.Exit( (int)ReturnCode.ArgumentParsingError );
            }
            inputPath = pathList[0];
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
