// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft {
    /// <summary> Represents a type of map generator.
    /// Provides general information about this generator, and ways to create IMapGeneratorParameters objects. </summary>
    public interface IMapGenerator {
        /// <summary> Name of the map generator. Uses same rules as command and world names. </summary>
        string Name { get; }

        /// <summary> Current version of the map generator. </summary>
        Version Version { get; }

        /// <summary> Creates a IMapGeneratorParameters object containing default parameters. </summary>
        IMapGeneratorParameters GetDefaultParameters();

        /// <summary> Parses serialized map generation parameters into a IMapGeneratorParameters object,
        /// (to load settings stored in template files or map metadata). </summary>
        IMapGeneratorParameters CreateParameters( string args );

        /// <summary> Parses command arguments to the generator, coming from in-game commands. </summary>
        IMapGeneratorParameters CreateParameters( CommandReader args );
    }
}