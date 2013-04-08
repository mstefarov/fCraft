using System;
using System.ComponentModel;

namespace fCraft {
    public interface IMapGenerator {
        string Name { get; }
        Version Version { get; }

        bool ReportsProgress { get; }
        bool SupportsCancellation { get; }

        IMapGeneratorParameters GetDefaultParameters();
        IMapGeneratorParameters CreateParameters( string args );
        IMapGeneratorParameters CreateParameters( CommandReader args );
    }


    public interface IMapGeneratorParameters : ICloneable {
        IMapGenerator Generator { get; }
        string SummaryString { get; }
        string Save();

        IMapGeneratorState CreateGenerator( int width, int height, int length );
    }


    public interface IMapGeneratorState {
        IMapGeneratorParameters Parameters { get; }

        event ProgressChangedEventHandler ProgressChanged;

        Map Generate();
        void CancelAsync();
    }
}