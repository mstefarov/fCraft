using System;

namespace fCraft {
    /// <summary> Represents a type of map generator.
    /// Provides general information about this generator, and ways to create IMapGeneratorParameters objects. </summary>
    public interface IMapGenerator {
        /// <summary> Name of the map generator. Uses same rules as command and world names. </summary>
        string Name { get; }

        /// <summary> Current version of the map generator. </summary>
        Version Version { get; }

        IMapGeneratorParameters GetDefaultParameters();
        IMapGeneratorParameters CreateParameters( string args );
        IMapGeneratorParameters CreateParameters( CommandReader args );
    }
}