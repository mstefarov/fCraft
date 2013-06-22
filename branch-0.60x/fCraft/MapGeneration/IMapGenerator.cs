// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Represents a type of map generator.
    /// Provides general information about this generator, and ways to create IMapGeneratorParameters objects. </summary>
    public interface IMapGenerator {
        /// <summary> Name of the map generator. Uses same rules as command and world names. </summary>
        [NotNull]
        string Name { get; }

        /// <summary> Current version of the map generator. </summary>
        [NotNull]
        Version Version { get; }

        /// <summary> Creates a IMapGeneratorParameters object containing default parameters. </summary>
        [NotNull]
        MapGeneratorParameters GetDefaultParameters();

        /// <summary> Parses serialized map generation parameters into a IMapGeneratorParameters object,
        /// (to load settings stored in template files or map metadata). </summary>
        /// <remarks> Throw appropriate exceptions on failure (do not return null). </remarks>
        MapGeneratorParameters CreateParameters( [NotNull] XElement serializedParameters );

        /// <summary> Parses command arguments to the generator, coming from in-game commands. </summary>
        /// <remarks> In case of command-parsing problems, inform the player and return null. </remarks>
        MapGeneratorParameters CreateParameters( [NotNull] Player player, [NotNull] CommandReader cmd );

        /// <summary> Creates parameters for a given preset name. </summary>
        /// <remarks> Throw ArgumentException if preset name is unrecognized. </remarks>
        [NotNull]
        MapGeneratorParameters CreateParameters( [NotNull] string presetName );

        /// <summary> Returns list of presets for this generator. </summary>
        /// <remarks> May be blank (but not null). </remarks>
        [NotNull]
        string[] Presets { get; }
    }
}