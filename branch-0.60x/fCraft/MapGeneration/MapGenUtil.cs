using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    public static class MapGenUtil {
        const string ParamsMetaGroup = "_Origin";
        const string ParamsMetaKey = "MapGeneratorParameters";
        const string GenNameMetaKey = "MapGeneratorName";
        const string GenVersionMetaKey = "MapGeneratorVersion";
        static readonly Dictionary<string, MapGenerator> Generators = new Dictionary<string, MapGenerator>();

        static MapGenUtil() {
            RegisterGenerator( FlatMapGen.Instance );
            RegisterGenerator( RealisticMapGen.Instance );
            RegisterGenerator( VanillaMapGen.Instance );
            RegisterGenerator( FloatingIslandMapGen.Instance );
        }


        /// <summary> Extracts embedded map generation parameters from a map file. </summary>
        /// <param name="map"> Map from which parameters will be read. </param>
        /// <returns> IMapGeneratorParameters if parsing successful; null if no data is embedded. </returns>
        /// <exception cref="ArgumentNullException"> mapGen or map is null. </exception>
        /// <exception cref="UnknownMapGeneratorException"> Unrecongized map generator was specified by the map. </exception>
        [CanBeNull]
        public static MapGeneratorParameters LoadParamsFromMap( [NotNull] Map map ) {
            if( map == null ) {
                throw new ArgumentNullException( "map" );
            }
            string genNameString;
            if( !map.Metadata.TryGetValue( ParamsMetaGroup, GenNameMetaKey, out genNameString ) ) {
                return null;
            }
            MapGenerator mapGen = GetGeneratorByName( genNameString );
            if( mapGen == null ) {
                throw new UnknownMapGeneratorException( genNameString );
            }

            string paramString;
            if( !map.Metadata.TryGetValue( ParamsMetaGroup, ParamsMetaKey, out paramString ) ) {
                return null;
            }
            XElement el = XElement.Parse( paramString );
            return mapGen.CreateParameters( el );
        }


        /// <summary> Embeds given map generation parameters in a map file. </summary>
        /// <param name="mapGenParams"> Parameters to embed. </param>
        /// <param name="map"> Map to embed parameters in. </param>
        /// <exception cref="ArgumentNullException"> mapGenParams or map is null. </exception>
        public static void SaveToMap( [NotNull] this MapGeneratorParameters mapGenParams, [NotNull] Map map ) {
            if( mapGenParams == null ) {
                throw new ArgumentNullException( "mapGenParams" );
            }
            if( map == null ) {
                throw new ArgumentNullException( "map" );
            }
            XElement el = new XElement( ParamsMetaKey );
            mapGenParams.Save( el );
            map.Metadata[ParamsMetaGroup, ParamsMetaKey] = el.ToString( SaveOptions.DisableFormatting );
            map.Metadata[ParamsMetaGroup, GenNameMetaKey] = mapGenParams.Generator.Name;
            map.Metadata[ParamsMetaGroup, GenVersionMetaKey] = mapGenParams.Generator.Version.ToString();
        }


        /// <summary> Determines whether given map contains embedded map generation parameters. </summary>
        /// <param name="map"> Map from which to read parameters. </param>
        /// <returns> True if map has embedded map generation parameters; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> map is null. </exception>
        public static bool ContainsMapGenParams( [NotNull] Map map ) {
            if( map == null ) {
                throw new ArgumentNullException( "map" );
            }
            return map.Metadata.ContainsKey( ParamsMetaGroup, GenNameMetaKey ) &&
                   map.Metadata.ContainsKey( ParamsMetaGroup, ParamsMetaKey );
        }


        /// <summary> Registers a new map generator. </summary>
        /// <param name="gen"> MapGenerator to register. Must have a unique name. </param>
        /// <exception cref="ArgumentNullException"> gen is null. </exception>
        /// <exception cref="ArgumentException"> A generator with the same name has already been registered. </exception>
        public static void RegisterGenerator( [NotNull] MapGenerator gen ) {
            if( gen == null ) {
                throw new ArgumentNullException( "gen" );
            }
            if( GetGeneratorByName( gen.Name ) != null ) {
                throw new ArgumentException( "A generator with the same name has already been registered." );
            }
            Generators.Add( gen.Name.ToLowerInvariant(), gen );
        }


        /// <summary> Finds a map generator by name. </summary>
        /// <param name="genName"> Generator name. Case-insensitive. </param>
        /// <returns> MapGenerator instance, if found. null if no matching generator was found. </returns>
        /// <exception cref="ArgumentNullException"> genName is null. </exception>
        [CanBeNull]
        public static MapGenerator GetGeneratorByName( [NotNull] string genName ) {
            if( genName == null ) {
                throw new ArgumentNullException( "genName" );
            }
            MapGenerator gen;
            if( Generators.TryGetValue( genName.ToLowerInvariant(), out gen ) ) {
                return gen;
            } else {
                return null;
            }
        }


        /// <summary> Returns an array of all registered generators. </summary>
        [NotNull]
        public static MapGenerator[] GeneratorList {
            get { return Generators.Values.ToArray(); }
        }


        /// <summary> Exception thrown when an attempt is made to
        /// parse parameters for an unknown/unrecognized map generator type. </summary>
        public class UnknownMapGeneratorException : Exception {
            /// <summary> Given map generator name, which was not recognized. </summary>
            public string GeneratorName { get; private set; }

            internal UnknownMapGeneratorException( string name )
                : base( "Unknown map generator: " + name ) {
                GeneratorName = name;
            }
        }
    }
}