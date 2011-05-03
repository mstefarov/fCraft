// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;

namespace fCraft.MapConversion {

    public static class MapUtility {

        static readonly Dictionary<MapFormat, IMapConverter> AvailableConverters = new Dictionary<MapFormat, IMapConverter>();


        static MapUtility() {
            AvailableConverters.Add( MapFormat.D3, new MapD3() );
            AvailableConverters.Add( MapFormat.Creative, new MapDAT() );
            AvailableConverters.Add( MapFormat.FCMv2, new MapFCMv2() );
            AvailableConverters.Add( MapFormat.FCMv3, new MapFCMv3() );
            AvailableConverters.Add( MapFormat.JTE, new MapJTE() );
            AvailableConverters.Add( MapFormat.MCSharp, new MapMCSharp() );
            AvailableConverters.Add( MapFormat.MinerCPP, new MapMinerCPP() );
            AvailableConverters.Add( MapFormat.Myne, new MapMyne() );
            AvailableConverters.Add( MapFormat.NBT, new MapNBT() );
            AvailableConverters.Add( MapFormat.Opticraft, new MapOpticraft() );
        }


        public static MapFormat Identify( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            MapFormatType targetType = MapFormatType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapFormatType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                try {
                    if( converter.FormatType == targetType && converter.ClaimsName( fileName ) ) {
                        if( converter.Claims( fileName ) ) {
                            return converter.Format;
                        }
                    } else {
                        fallbackConverters.Add( converter );
                    }
                } catch { }
            }

            foreach( IMapConverter converter in fallbackConverters ) {
                try {
                    if( converter.Claims( fileName ) ) {
                        return converter.Format;
                    }
                } catch { }
            }

            return MapFormat.Unknown;
        }


        public static bool TryLoadHeader( string fileName, out Map map ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                map = LoadHeader( fileName );
                return true;
            } catch( Exception ex ) {
                Logger.Log( "MapUtility.TryLoadHeader: {0}: {1}", LogType.Error,
                            ex.GetType().Name, ex.Message );
                map = null;
                return false;
            }
        }


        public static Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );

            MapFormatType targetType = MapFormatType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapFormatType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();

            // first try all converters for the file extension
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                bool claims = false;
                try {
                    claims = (converter.FormatType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                } catch( Exception ) { }
                if( claims ) {
                    try {
                        Map map = converter.LoadHeader( fileName );
                        map.ChangedSinceSave = false;
                        return map;
                    } catch( NotImplementedException ) { }
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            foreach( IMapConverter converter in fallbackConverters ) {
                try {
                    Map map = converter.LoadHeader( fileName );
                    map.ChangedSinceSave = false;
                    return map;
                } catch { }
            }

            throw new MapFormatException( "Unknown map format." );
        }


        public static bool TryLoad( string fileName, out Map map ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                map = Load( fileName );
                return true;
            } catch( Exception ex ) {
                Logger.Log( "MapUtility.TryLoad: {0}: {1}", LogType.Error,
                            ex.GetType().Name, ex.Message );
                map = null;
                return false;
            }
        }


        public static Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            MapFormatType targetType = MapFormatType.SingleFile;
            if( !File.Exists( fileName ) ) {
                if( Directory.Exists( fileName ) ) {
                    targetType = MapFormatType.Directory;
                } else {
                    throw new FileNotFoundException();
                }
            }

            List<IMapConverter> fallbackConverters = new List<IMapConverter>();

            // first try all converters for the file extension
            foreach( IMapConverter converter in AvailableConverters.Values ) {
                bool claims = false;
                try {
                    claims = (converter.FormatType == targetType) &&
                             converter.ClaimsName( fileName ) &&
                             converter.Claims( fileName );
                } catch { }
                if( claims ) {
                    Map map = converter.Load( fileName );
                    map.ChangedSinceSave = false;
                    return map;
                } else {
                    fallbackConverters.Add( converter );
                }
            }

            foreach( IMapConverter converter in fallbackConverters ) {
                try {
                    Map map = converter.Load( fileName );
                    map.ChangedSinceSave = false;
                    return map;
                } catch { }
            }

            throw new MapFormatException( "Unknown map format." );
        }


        public static bool TrySave( Map mapToSave, string fileName, MapFormat format ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( format == MapFormat.Unknown ) throw new ArgumentException( "Format may not be \"Unknown\"", "format" );

            if( AvailableConverters.ContainsKey( format ) ) {
                IMapConverter converter = AvailableConverters[format];
                try {
                    return converter.Save( mapToSave, fileName );
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Map failed to save", "Mcc", ex, false );
                    return false;
                }
            }

            throw new MapFormatException( "Unknown map format for saving." );
        }


        internal static void ReadAll( Stream source, byte[] destination ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            int read = 0;
            while( read < destination.Length ) {
                int readPass = source.Read( destination, read, destination.Length - read );
                if( readPass == 0 ) throw new EndOfStreamException();
                read += readPass;
            }
        }
    }
}