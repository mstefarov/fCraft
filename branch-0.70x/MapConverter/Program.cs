// Part of fCraft | Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | MIT License
using System;
using System.IO;
using System.Linq;
using fCraft.MapConversion;

namespace fCraft.MapConverter {
    static class Program {
        const int ExpectedArgs = 4;
        static IMapConverter[] allConverters;

        static int Main( string[] args ) {
            // init fCraft
            Server.InitLibrary( new string[0] );

            allConverters = MapUtility.GetConverters();

            // check number of arguments
            if( args.Length != ExpectedArgs ) {
                PrintUsage();
                return (int)ReturnCode.WrongArgCount;
            }

            string fromFormatName = args[0];
            string toFormatName = args[1];
            string inputPath = args[2];
            string outputPath = args[3];

            // parse from-format
            MapFormat fromFormat;
            if( !EnumUtil.TryParse( fromFormatName, out fromFormat, true ) ) {
                Console.Error.WriteLine( "Unrecognized format \"{0}\"", fromFormatName );
                PrintUsage();
                return (int)ReturnCode.UnrecognizedFromFormat;
            }

            // parse to-format
            MapFormat toFormat;
            if( !EnumUtil.TryParse( toFormatName, out toFormat, true ) ) {
                Console.Error.WriteLine( "Unrecognized format \"{0}\"", toFormatName );
                PrintUsage();
                return (int)ReturnCode.UnrecognizedToFormat;
            }

            // get converters
            IMapConverter fromConverter = MapUtility.GetConverter( fromFormat );
            IMapConverter toConverter = MapUtility.GetConverter( toFormat );
            if( !toConverter.SupportsExport ) {
                var supportedConverters = allConverters.Where( c => c.SupportsExport );
                Console.Error.WriteLine( "fCraft does not support exporting to {0} format. Supported formats: {1}",
                                         toFormat,
                                         supportedConverters.JoinToString( c => c.Format.ToString() ) );
                return (int)ReturnCode.UnsupportedSaveFormat;
            }

            // try to open the input directory
            DirectoryInfo inputDir;
            try {
                inputDir = new DirectoryInfo( inputPath );
            } catch( Exception ex ) {
                Console.Error.WriteLine( "{0}: {1}", ex.GetType().Name, ex.Message );
                return (int)ReturnCode.ErrorOpeningDirForLoading;
            }

            // verify that load directory exists
            if( !inputDir.Exists ) {
                Console.Error.WriteLine( "Can't find {0}", inputPath );
                PrintUsage();
                return (int)ReturnCode.InputDirNotFound;
            }

            // try to open the output directory
            LogRecorder logger = new LogRecorder();
            if( !Paths.TestDirectory( "output", outputPath, true ) ) {
                if( logger.HasMessages ) Console.Error.WriteLine( logger.MessageString );
                return (int)ReturnCode.ErrorOpeningDirForSaving;
            }

            if( fromConverter.StorageType == MapStorageType.Directory ) {
                // go through all directories
                foreach( var subdir in inputDir.EnumerateDirectories() ) {
                    try {
                        Console.Write( "Loading {0}... ", subdir.Name );
                        string targetName = Path.Combine( outputPath, subdir.Name + '.' + toConverter.FileExtension );
                        Map map = fromConverter.Load( subdir.FullName );
                        Console.Write( "Saving {0}... ", Path.GetFileNameWithoutExtension( targetName ) );
                        toConverter.Save( map, targetName );
                        Console.WriteLine( "ok" );
                    } catch( Exception ex ) {
                        Console.Error.WriteLine( "ERROR: {0}: {1}", ex.GetType().Name, ex.Message );
                    }
                }
            } else {
                // go through all files
                foreach( var fileInfo in inputDir.EnumerateFiles() ) {
                    try {
                        Console.Write( "Loading {0}... ", fileInfo.Name );
                        string targetName = Path.Combine( outputPath, Path.GetFileNameWithoutExtension( fileInfo.Name ) + '.' + toConverter.FileExtension );
                        Map map = fromConverter.Load( fileInfo.FullName );
                        Console.Write( "Saving {0}... ", Path.GetFileNameWithoutExtension( targetName ) );
                        map.Save( targetName );
                        Console.WriteLine( "ok" );
                    } catch( Exception ex ) {
                        Console.Error.WriteLine( "ERROR: {0}: {1}", ex.GetType().Name, ex.Message );
                    }
                }
            }

            return (int)ReturnCode.Success;
        }


        static void PrintUsage() {
            Console.WriteLine( "Usage: MapConverter FromFormat ToFormat \"LoadPath\" \"SavePath\"" );
            Console.WriteLine( "Supported FromFormats: {0}",
                               allConverters.JoinToString( c => c.Format.ToString() ) );
            Console.WriteLine( "Supported ToFormats: {0}",
                               allConverters.Where( c => c.SupportsExport ).JoinToString( c => c.Format.ToString() ) );
        }
    }

    enum ReturnCode {
        Success = 0,
        WrongArgCount = 1,
        UnrecognizedFromFormat = 2,
        UnrecognizedToFormat = 3,
        InputDirNotFound = 4,
        ErrorOpeningDirForLoading = 5,
        ErrorOpeningDirForSaving = 6,
        UnsupportedSaveFormat = 7,
        ErrorLoading = 8,
        ErrorSaving = 9
    }
}
