// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.MapConversion {

    // ReSharper disable EmptyGeneralCatchClause
    /// <summary> Utilities used to handle different map formats, including loading, parsing, and saving. </summary>
    public static class MapUtility {

        static readonly Dictionary<MapFormat, IMapConverter> AvailableConverters = new Dictionary<MapFormat, IMapConverter>();


        static MapUtility() {
            AvailableConverters.Add( MapFormat.FCMv4, new MapFCMv4() );
            AvailableConverters.Add( MapFormat.FCMv3, new MapFCMv3() );
            AvailableConverters.Add( MapFormat.FCMv2, new MapFCMv2() );
            AvailableConverters.Add( MapFormat.Creative, new MapDat() );
            AvailableConverters.Add( MapFormat.MCSharp, new MapMCSharp() );
            AvailableConverters.Add( MapFormat.D3, new MapD3() );
            AvailableConverters.Add( MapFormat.JTE, new MapJTE() );
            AvailableConverters.Add( MapFormat.MinerCPP, new MapMinerCPP() );
            AvailableConverters.Add( MapFormat.Myne, new MapMyne() );
            AvailableConverters.Add( MapFormat.NBT, new MapNBT() );
            AvailableConverters.Add( MapFormat.Opticraft, new MapOpticraft() );
        }


        /// <summary> Identifies the map format from the specified filename. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <param name="tryFallbackConverters"> Whether or not to attempt to try other converters if this fails. </param>
        /// <returns> Map format of the specified file. </returns>
        /// <exception cref="ArgumentNullException"> If fileName is null. </exception>
        /// <exception cref="FileNotFoundException"> If file/directory with the given name is missing. </exception>
        public static MapFormat Identify( [NotNull] string fileName, bool tryFallbackConverters ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            MapStorageType targetType = MapStorageType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapStorageType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                try {
                    if( converter.StorageType == targetType && converter.ClaimsName( fileName ) ) {
                        if( converter.Claims( fileName ) ) {
                            return converter.Format;
                        }
                    } else {
                        fallbackConverters.Add( converter );
                    }
                } catch { }
            }

            if( tryFallbackConverters ) {
                foreach( IMapConverter converter in fallbackConverters ) {
                    try {
                        if( converter.Claims( fileName ) ) {
                            return converter.Format;
                        }
                    } catch { }
                }
            }

            return MapFormat.Unknown;
        }


        /// <summary> Attempts to load the map excluding the block data from it's header using the specified filename. </summary>
        /// <param name="fileName"> The name of the file.</param>
        /// <param name="map"> Where the loaded map should be stored. </param>
        /// <returns> Whether or not the map excluding block data was loaded successfully. </returns>
        /// <exception cref="ArgumentNullException"> If fileName is null. </exception>
        public static bool TryLoadHeader( [NotNull] string fileName, out Map map ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                map = LoadHeader( fileName );
                return true;
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "MapUtility.TryLoadHeader: {0}: {1}",
                            ex.GetType().Name, ex.Message );
                map = null;
                return false;
            }
        }


        /// <summary> Loads the map excluding block data from it's header using the specified filename. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <returns> The loaded map excluding block data. </returns>
        /// <exception cref="ArgumentNullException"> If fileName is null. </exception>
        /// <exception cref="FileNotFoundException"> If file/directory with the given name is missing. </exception>
        /// <exception cref="MapFormatException"> If no converter could be found to load the map. </exception>
        [NotNull]
        public static Map LoadHeader( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );

            MapStorageType targetType = MapStorageType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapStorageType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();

            // first try all converters for the file extension
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                bool claims = false;
                try {
                    claims = (converter.StorageType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                } catch{ }
                if( claims ) {
                    try {
                        Map map = converter.LoadHeader( fileName );
                        map.HasChangedSinceSave = false;
                        return map;
                    } catch( NotImplementedException ) { }
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            foreach( IMapConverter converter in fallbackConverters ) {
                try {
                    Map map = converter.LoadHeader( fileName );
                    map.HasChangedSinceSave = false;
                    return map;
                } catch { }
            }

            throw new MapFormatException( "Could not find any converter to load the given file." );
        }


        /// <summary> Attempts to load the map including the block data from it's header using the specified filename. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <param name="map"> Where the loaded map should be stored. </param>
        /// <returns> Whether or not the map was loaded successfully. </returns>
        /// <exception cref="ArgumentNullException"> If fileName is null. </exception>
        public static bool TryLoad( [NotNull] string fileName, out Map map ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                map = Load( fileName );
                return true;
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "MapUtility.TryLoad: {0}", ex );
                map = null;
                return false;
            }
        }


        /// <summary> Loads the map from it's header using the specified filename. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <returns> The loaded map excluding block data. </returns>
        /// <exception cref="ArgumentNullException"> If fileName is null. </exception>
        /// <exception cref="FileNotFoundException"> If file/directory with the given name is missing. </exception>
        /// <exception cref="MapFormatException"> If no converter could be found to load the map. </exception>
        [NotNull]
        public static Map Load( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            MapStorageType targetType = MapStorageType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapStorageType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();

            // first try all converters for the file extension
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                bool claims = false;
                try {
                    claims = (converter.StorageType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                } catch { }
                if( claims ) {
                    Map map = converter.Load( fileName );
                    map.HasChangedSinceSave = false;
                    return map;
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            foreach( IMapConverter converter in fallbackConverters ) {
                try {
                    Map map = converter.Load( fileName );
                    map.HasChangedSinceSave = false;
                    return map;
                } catch { }
            }

            throw new MapFormatException( "Could not find any converter to load the given file." );
        }


        /// <summary> Attempts to save the map, under the specified filename using the specified format. </summary>
        /// <param name="mapToSave"> Map file to be saved.</param>
        /// <param name="fileName">The name of the file to save to. </param>
        /// <param name="format"> The format to use when saving the map. </param>
        /// <returns> Whether or not the map save completed successfully. </returns>
        /// <exception cref="ArgumentNullException"> If mapToSave or fileName are null. </exception>
        /// <exception cref="ArgumentException"> If format is set to MapFormat.Unknown. </exception>
        /// <exception cref="MapFormatException">  If no converter could be found for the given format. </exception>
        /// <exception cref="NotImplementedException"> If saving to this format is not implemented or supported. </exception>
        public static bool TrySave( [NotNull] Map mapToSave, [NotNull] string fileName, MapFormat format ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( format == MapFormat.Unknown ) throw new ArgumentException( "Format may not be \"Unknown\"", "format" );

            if( AvailableConverters.ContainsKey( format ) ) {
                IMapConverter converter = AvailableConverters[format];
                try {
                    return converter.Save( mapToSave, fileName );
                } catch( NotImplementedException ) {
                    throw;
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Map failed to save", "MapConversion", ex, false );
                    return false;
                }
            }

            throw new MapFormatException( "No converter could be found for the given format." );
        }


        internal static void ReadAll( [NotNull] Stream source, [NotNull] byte[] destination ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            int bytesRead = 0;
            int bytesLeft = destination.Length;
            while( bytesLeft > 0 ) {
                int readPass = source.Read( destination, bytesRead, bytesLeft );
                if( readPass == 0 ) throw new EndOfStreamException();
                bytesRead += readPass;
                bytesLeft -= readPass;
            }
        }


        public static IMapConverter GetConverter( MapFormat format ) {
            return AvailableConverters[format];
        }


        public static IMapConverter[] GetConverters() {
            return AvailableConverters.Values.ToArray();
        }
    }
    // ReSharper restore EmptyGeneralCatchClause
}