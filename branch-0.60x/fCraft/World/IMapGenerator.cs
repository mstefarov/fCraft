using System;
using System.ComponentModel;

namespace fCraft {
    /// <summary> Represents a type of map generator.
    /// Provides general information about this generator, and ways to create IMapGeneratorParameters objects. </summary>
    public interface IMapGenerator {
        string Name { get; }
        Version Version { get; }

        bool ReportsProgress { get; }
        bool SupportsCancellation { get; }

        IMapGeneratorParameters GetDefaultParameters();
        IMapGeneratorParameters CreateParameters( string args );
        IMapGeneratorParameters CreateParameters( CommandReader args );
    }


    /// <summary> Represets a set of map generator parameters.
    /// Provides a way to serialize these parameters to string, and a way to create single-use IMapGeneratorState objects. </summary>
    public interface IMapGeneratorParameters : ICloneable {
        IMapGenerator Generator { get; }
        string SummaryString { get; }
        string Save();

        IMapGeneratorState CreateGenerator( int width, int height, int length );
    }


    /// <summary> Represents a single-use state object for a paricular set of map generator parameters (IMapGeneratorParameters).
    /// Provides a synchronous method to carry out generation, asynchronous method to cancel generation,
    /// various properties to indicate progress, and an event to report progress changes. </summary>
    public interface IMapGeneratorState {
        IMapGeneratorParameters Parameters { get; }
        bool Canceled { get; }
        bool Finished { get; }
        int Progress { get; }
        string StatusString { get; }

        event ProgressChangedEventHandler ProgressChanged;

        Map Generate();
        void CancelAsync();
    }
}