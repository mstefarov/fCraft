// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Utilities used to handle different map formats, including loading, parsing, and saving. </summary>
    public static class MapUtility {
        static readonly Dictionary<MapFormat, IMapImporter> Importers = new Dictionary<MapFormat, IMapImporter>();
        static readonly Dictionary<MapFormat, IMapExporter> Exporters = new Dictionary<MapFormat, IMapExporter>();


        /// <summary> Registers a new map importer or exporter.
        /// Only one importer/exporter may be registered for each supported format.
        /// If an importer/exporter for the given format has already been registered, it will be replaced. </summary>
        /// <param name="converter"> New converter to add. </param>
        /// <exception cref="ArgumentException"> Given IMapConverter is nether an IMapImporter, nor an IMapExporter. </exception>
        public static void RegisterConverter( IMapConverter converter ) {
            IMapImporter asImporter = converter as IMapImporter;
            IMapExporter asExporter = converter as IMapExporter;
            if( asImporter != null ) Importers.Add( asImporter.Format, asImporter );
            if( asExporter != null ) Exporters.Add( asExporter.Format, asExporter );
            if( asImporter == null && asExporter == null ) {
                throw new ArgumentException( "Given converter is neither an IMapImporter nor an IMapExporter." );
            }
        }


        static MapUtility() {
            RegisterConverter( new MapFCMv3() );
            RegisterConverter( new MapFCMv2() );
            RegisterConverter( new MapDat() );
            RegisterConverter( new MapMCSharp() );
            RegisterConverter( new MapD3() );
            RegisterConverter( new MapJTE() );
            RegisterConverter( new MapMinerCPP() );
            RegisterConverter( new MapMyne() );
            RegisterConverter( new MapIndev() );
            RegisterConverter( new MapOpticraft() );
            RegisterConverter( new MapRaw() );
        }


        /// <summary> Identifies the map format from the specified file name. </summary>
        /// <param name="path"> Path to the file or directory to be identified. </param>
        /// <param name="tryFallbackConverters"> Whether or this method should try ALL converters,
        /// including ones that do not typically handle files with the given file extension. </param>
        /// <returns> Map format of the specified file. MapFormat.Unknown, if the format could not be identified. </returns>
        /// <exception cref="ArgumentNullException"> fileName is null. </exception>
        /// <exception cref="FileNotFoundException"> The file specified in path was not found. </exception>
        public static MapFormat Identify( [NotNull] string path, bool tryFallbackConverters ) {
            if( path == null ) throw new ArgumentNullException( "path" );
            MapStorageType targetType = MapStorageType.SingleFile;
            if( !File.Exists( path ) ) {
                if( Directory.Exists( path ) ) {
                    targetType = MapStorageType.Directory;
                } else {
                    throw new FileNotFoundException( "Given file/directory could no be found." );
                }
            }

            List<IMapImporter> fallbackConverters = new List<IMapImporter>();
            foreach( IMapImporter converter in Importers.Values ) {
                try {
                    if( converter.StorageType == targetType && converter.ClaimsName( path ) ) {
                        if( converter.Claims( path ) ) {
                            return converter.Format;
                        }
                    } else {
                        fallbackConverters.Add( converter );
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                } catch {}
                // ReSharper restore EmptyGeneralCatchClause
            }

            if( tryFallbackConverters ) {
                foreach( IMapImporter converter in fallbackConverters ) {
                    try {
                        if( converter.Claims( path ) ) {
                            return converter.Format;
                        }
                        // ReSharper disable EmptyGeneralCatchClause
                    } catch {}
                    // ReSharper restore EmptyGeneralCatchClause
                }
            }

            return MapFormat.Unknown;
        }


        /// <summary> Attempts to load the map excluding the block data from it's header using the specified file name. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <param name="tryFallbackConverters"> Whether or this method should try ALL converters,
        /// including ones that do not typically handle files with the given file extension. </param>
        /// <param name="map"> Where the loaded map should be stored. </param>
        /// <returns> Whether or not the map excluding block data was loaded successfully. </returns>
        /// <exception cref="ArgumentNullException"> fileName is null. </exception>
        public static bool TryLoadHeader( [NotNull] string fileName, bool tryFallbackConverters, out Map map ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                map = LoadHeader( fileName, tryFallbackConverters );
                return true;
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "MapUtility.TryLoadHeader: {0}: {1}",
                            ex.GetType().Name,
                            ex.Message );
                map = null;
                return false;
            }
        }


        /// <summary> Loads the map excluding block data from it's header using the specified file name. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <param name="tryFallbackConverters"> Whether or this method should try ALL converters,
        /// including ones that do not typically handle files with the given file extension. </param>
        /// <returns> The loaded map excluding block data. </returns>
        /// <exception cref="ArgumentNullException"> fileName is null. </exception>
        /// <exception cref="FileNotFoundException"> File/directory with the given name is missing. </exception>
        /// <exception cref="NoMapConverterFoundException"> No converter can be found to load the map. </exception>
        [NotNull]
        public static Map LoadHeader( [NotNull] string fileName, bool tryFallbackConverters ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );

            MapStorageType targetType = MapStorageType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapStorageType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapImporter> fallbackConverters = new List<IMapImporter>();

            // first try all converters for the file extension
            foreach( IMapImporter converter in Importers.Values ) {
                bool claims = false;
                try {
                    claims = (converter.StorageType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                    // ReSharper disable EmptyGeneralCatchClause
                } catch {}
                // ReSharper restore EmptyGeneralCatchClause
                if( claims ) {
                    try {
                        Map map = converter.LoadHeader( fileName );
                        map.HasChangedSinceSave = false;
                        return map;
                    } catch( NotImplementedException ) {}
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            if( tryFallbackConverters ) {
                foreach( IMapImporter converter in fallbackConverters ) {
                    try {
                        Map map = converter.LoadHeader( fileName );
                        map.HasChangedSinceSave = false;
                        return map;
                        // ReSharper disable EmptyGeneralCatchClause
                    } catch {}
                    // ReSharper restore EmptyGeneralCatchClause
                }
            }

            throw new NoMapConverterFoundException( "Could not find any converter to load the given file." );
        }


        /// <summary> Attempts to load the map including the block data from it's header using the specified file name. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <param name="tryFallbackConverters"> Whether or this method should try ALL converters,
        /// including ones that do not typically handle files with the given file extension. </param>
        /// <param name="map"> Where the loaded map should be stored. </param>
        /// <returns> Whether or not the map was loaded successfully. </returns>
        /// <exception cref="ArgumentNullException"> fileName is null. </exception>
        public static bool TryLoad( [NotNull] string fileName, bool tryFallbackConverters, out Map map ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                map = Load( fileName, tryFallbackConverters );
                return true;
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "MapUtility.TryLoad: {0}",
                            ex );
                map = null;
                return false;
            }
        }


        /// <summary> Loads the map from it's header using the specified file name. </summary>
        /// <param name="fileName"> The name of the file. </param>
        /// <param name="tryFallbackConverters"> Whether or this method should try ALL converters,
        /// including ones that do not typically handle files with the given file extension. </param>
        /// <returns> The loaded map excluding block data. </returns>
        /// <exception cref="ArgumentNullException"> fileName is null. </exception>
        /// <exception cref="FileNotFoundException"> File/directory with the given name is missing. </exception>
        /// <exception cref="NoMapConverterFoundException"> No converter can be found to load the given map file. </exception>
        [NotNull]
        public static Map Load( [NotNull] string fileName, bool tryFallbackConverters ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            MapStorageType targetType = MapStorageType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapStorageType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapImporter> fallbackConverters = new List<IMapImporter>();

            // first try all converters for the file extension
            foreach( IMapImporter converter in Importers.Values ) {
                bool claims = false;
                try {
                    claims = (converter.StorageType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                    // ReSharper disable EmptyGeneralCatchClause
                } catch {}
                // ReSharper restore EmptyGeneralCatchClause
                if( claims ) {
                    Map map = converter.Load( fileName );
                    map.HasChangedSinceSave = false;
                    return map;
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            if( tryFallbackConverters ) {
                foreach( IMapImporter converter in fallbackConverters ) {
                    try {
                        Map map = converter.Load( fileName );
                        map.HasChangedSinceSave = false;
                        return map;
                        // ReSharper disable EmptyGeneralCatchClause
                    } catch {}
                    // ReSharper restore EmptyGeneralCatchClause
                }
            }

            throw new NoMapConverterFoundException( "Could not find any converter to load the given file." );
        }


        /// <summary> Attempts to save the map, under the specified file name using the specified format. </summary>
        /// <param name="mapToSave"> Map file to be saved. </param>
        /// <param name="fileName">The name of the file to save to. </param>
        /// <param name="mapFormat"> The format to use when saving the map. </param>
        /// <returns> Whether or not the map save completed successfully. </returns>
        /// <exception cref="ArgumentNullException"> mapToSave or fileName are null. </exception>
        /// <exception cref="ArgumentException"> mapFormat is set to MapFormat.Unknown. </exception>
        /// <exception cref="NoMapConverterFoundException"> No exporter could be found for the given format. </exception>
        public static bool TrySave( [NotNull] Map mapToSave, [NotNull] string fileName, MapFormat mapFormat ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( mapFormat == MapFormat.Unknown )
                throw new ArgumentException( "Format may not be \"Unknown\"", "mapFormat" );

            if( Exporters.ContainsKey( mapFormat ) ) {
                IMapExporter converter = Exporters[mapFormat];
                if( converter.SupportsExport ) {
                    try {
                        converter.Save( mapToSave, fileName );
                        return true;
                    } catch( Exception ex ) {
                        Logger.LogAndReportCrash( "Map failed to save", "MapConversion", ex, false );
                        return false;
                    }
                }
            }

            throw new NoMapConverterFoundException( "No exporter could be found for the given format." );
        }


        /// <summary> Save the map, under the specified file name using the specified format. </summary>
        /// <param name="mapToSave"> Map file to be saved. </param>
        /// <param name="fileName">The name of the file to save to. </param>
        /// <param name="mapFormat"> The format to use when saving the map. </param>
        /// <exception cref="ArgumentNullException"> mapToSave or fileName are null. </exception>
        /// <exception cref="ArgumentException"> mapFormat is set to MapFormat.Unknown. </exception>
        /// <exception cref="NoMapConverterFoundException"> No exporter could be found for the given format. </exception>
        /// <exception cref="Exception"> Other kinds of exceptions may be thrown by the map exporter. </exception>
        public static void Save( [NotNull] Map mapToSave, [NotNull] string fileName, MapFormat mapFormat ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( mapFormat == MapFormat.Unknown )
                throw new ArgumentException( "Format may not be \"Unknown\"", "mapFormat" );

            if( Exporters.ContainsKey( mapFormat ) ) {
                IMapExporter converter = Exporters[mapFormat];
                if( converter.SupportsExport ) {
                    converter.Save( mapToSave, fileName );
                }
            }

            throw new NoMapConverterFoundException( "No exporter could be found for the given format." );
        }


        /// <summary> Looks up an appropriate importer for the given MapFormat. </summary>
        /// <param name="format"> Map format for which an importer should be looked up. </param>
        /// <returns> IMapImporter object if an importer was found for the given format; otherwise null. </returns>
        [CanBeNull]
        public static IMapImporter GetImporter( MapFormat format ) {
            IMapImporter result;
            if( Importers.TryGetValue( format, out result ) ) {
                return result;
            } else {
                return null;
            }
        }


        /// <summary> Looks up an appropriate exporter for the given MapFormat. </summary>
        /// <param name="format"> Map format for which an exporter should be looked up. </param>
        /// <returns> IMapExporter object if an exporter was found for the given format; otherwise null. </returns>
        [CanBeNull]
        public static IMapExporter GetExporter( MapFormat format ) {
            IMapExporter result;
            if( Exporters.TryGetValue( format, out result ) ) {
                return result;
            } else {
                return null;
            }
        }


        /// <summary> Returns an array of all available map importers. </summary>
        public static IMapImporter[] GetImporters() {
            return Importers.Values.ToArray();
        }


        /// <summary> Returns an array of all available map exporters. </summary>
        public static IMapExporter[] GetExporters() {
            return Exporters.Values.ToArray();
        }
    }
}