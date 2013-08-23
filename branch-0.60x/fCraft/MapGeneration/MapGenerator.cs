// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.MapGeneration {
    /// <summary> Represents a type of map generator.
    /// Provides general information about this generator, and ways to create IMapGeneratorParameters objects. </summary>
    public abstract class MapGenerator {
        protected MapGenerator() {
            Version = new Version( 1, 0 );
            Presets = new[] {"Defaults"};
        }

        /// <summary> Name of the map generator. Uses same rules as command and world names. </summary>
        [NotNull]
        public string Name { get; protected set; }
        // TODO: name aliases/shortcuts

        /// <summary> Current version of the map generator. </summary>
        [NotNull]
        public Version Version { get; protected set; }

        /// <summary> Returns list of presets for this generator. Do not include default preset. </summary>
        /// <remarks> May be blank (but not null). </remarks>
        [NotNull]
        public string[] Presets { get; protected set; }

        /// <summary> Help string, printed when players call "/Help Gen ThisMapGensName" </summary>
        public string Help { get; set; }

        /// <summary> Creates a IMapGeneratorParameters object containing default parameters. </summary>
        [NotNull]
        public abstract MapGeneratorParameters CreateDefaultParameters();

        /// <summary> Parses serialized map generation parameters into a IMapGeneratorParameters object,
        /// (to load settings stored in template files or map metadata). </summary>
        /// <remarks> Throw appropriate exceptions on failure (do not return null). </remarks>
        [NotNull]
        public abstract MapGeneratorParameters CreateParameters( [NotNull] XElement serializedParameters );

        /// <summary> Parses command arguments to the generator, coming from in-game commands. </summary>
        /// <remarks> In case of command-parsing problems, inform the player and return null. </remarks>
        [CanBeNull]
        public abstract MapGeneratorParameters CreateParameters( [NotNull] Player player, [NotNull] CommandReader cmd );

        /// <summary> Creates parameters for a given preset name.
        /// Returns null if preset name was not recognized. Throws exceptions in case of other failures. </summary>
        /// <param name="presetName"> Name of preset. May be null (meaning "return defaults"). </param>
        /// <returns> MapGeneratorParameters object for given preset; null if presetName was not recognized. </returns>
        [CanBeNull]
        public abstract MapGeneratorParameters CreateParameters( [NotNull] string presetName );
    }
}