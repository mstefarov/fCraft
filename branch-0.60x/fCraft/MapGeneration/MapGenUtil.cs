using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft {
    public static class MapGenUtil {
        static readonly Dictionary<string, IMapGenerator> Generators = new Dictionary<string, IMapGenerator>();

        static MapGenUtil() {
            RegisterGenerator( FlatMapGen.Instance );
            RegisterGenerator( RealisticMapGen.Instance );
            RegisterGenerator( VanillaMapGen.Instance );
            RegisterGenerator( FloatingIslandMapGen.Instance );
        }


        /// <summary> Registers a new map generator. </summary>
        /// <param name="gen"> IMapGenerator to register. Must have a unique name. </param>
        /// <exception cref="ArgumentNullException"> gen is null. </exception>
        /// <exception cref="ArgumentException"> A generator with the same name has already been registered. </exception>
        public static void RegisterGenerator( [NotNull] IMapGenerator gen ) {
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
        /// <returns> IMapGenerator instance, if found. null if no matching generator was found. </returns>
        /// <exception cref="ArgumentNullException"> genName is null. </exception>
        [CanBeNull]
        public static IMapGenerator GetGeneratorByName( [NotNull] string genName ) {
            if( genName == null ) {
                throw new ArgumentNullException( "genName" );
            }
            IMapGenerator gen;
            if( Generators.TryGetValue( genName.ToLowerInvariant(), out gen ) ) {
                return gen;
            } else {
                return null;
            }
        }


        /// <summary> Returns an array of all registered generators. </summary>
        [NotNull]
        public static IMapGenerator[] GeneratorList {
            get { return Generators.Values.ToArray(); }
        }
    }
}