using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.GUI {
    public static class MapGenGuiUtil {
        static readonly Dictionary<string, IMapGeneratorGuiProvider> Generators =
            new Dictionary<string, IMapGeneratorGuiProvider>();

        static MapGenGuiUtil() {
            RegisterGui( RealisticMapGenGuiProvider.Instance, RealisticMapGen.Instance.Name );
        }


        /// <summary> Registers a new map generator. If another GUI provider already
        /// exists for this generator name, this new provider will replace it. </summary>
        /// <param name="provider"> IMapGeneratorGuiProvider to register. </param>
        /// <param name="genName"> Name of the generator for which GuiProvider is being registered. </param>
        /// <exception cref="ArgumentNullException"> provider or genName is null. </exception>
        public static void RegisterGui( [NotNull] IMapGeneratorGuiProvider provider, [NotNull] string genName ) {
            if( provider == null ) {
                throw new ArgumentNullException( "provider" );
            }
            IMapGeneratorGuiProvider oldProvider;
            if( Generators.TryGetValue( genName.ToLowerInvariant(), out oldProvider ) ) {
                Logger.Log( LogType.Warning,
                            "More than one GUI has been registered for \"{0}\" map generator. {1} now overrides {2}.",
                            oldProvider.Name,
                            provider.Name,
                            genName );
            }
            Generators[genName.ToLowerInvariant()] = provider;
        }


        /// <summary> Finds the best GUI provider for given map generator. </summary>
        /// <param name="genName"> Name of the generator for which a GUI is needed. </param>
        /// <returns> Either specialized IMapGeneratorGuiProvider for given generator,
        /// or default GUI provider as fallback. </returns>
        /// <exception cref="ArgumentNullException"> genName is null. </exception>
        [NotNull]
        public static IMapGeneratorGuiProvider GetGuiForGenerator( [NotNull] string genName ) {
            if( genName == null ) {
                throw new ArgumentNullException( "genName" );
            }
            IMapGeneratorGuiProvider provider;
            if( Generators.TryGetValue( genName.ToLowerInvariant(), out provider ) ) {
                return provider;
            } else {
                return DefaultMapGenGuiProvider.Instance;
            }
        }


        /// <summary> Finds the best GUI provider for given IMapGenerator. </summary>
        /// <param name="gen"> Generator for which a GUI is needed. </param>
        /// <returns> Either specialized IMapGeneratorGuiProvider for given generator,
        /// or default GUI provider as fallback. </returns>
        /// <exception cref="ArgumentNullException"> gen is null. </exception>
        [NotNull]
        public static IMapGeneratorGuiProvider GetGuiForGenerator( [NotNull] IMapGenerator gen ) {
            if( gen == null ) {
                throw new ArgumentNullException( "gen" );
            }
            return GetGuiForGenerator( gen.Name );
        }


        /// <summary> Returns an array of all registered GUI providers. </summary>
        [NotNull]
        public static IMapGeneratorGuiProvider[] ProviderList {
            get { return Generators.Values.ToArray(); }
        }
    }
}