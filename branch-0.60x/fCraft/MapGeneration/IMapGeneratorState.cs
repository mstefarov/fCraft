// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System.ComponentModel;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Represents a single-use state object for a particular set of map generator parameters (IMapGeneratorParameters).
    /// Provides a synchronous method to carry out generation, asynchronous method to cancel generation,
    /// various properties to indicate progress, and an event to report progress changes. </summary>
    public interface IMapGeneratorState {
        /// <summary> Associated map generation parameters. </summary>
        [NotNull]
        IMapGeneratorParameters Parameters { get; }

        /// <summary> Flag indicating whether this generation task has been canceled. 
        /// Should be set by CancelAsync(), regardless of whether async cancelation is supported. </summary>
        bool Canceled { get; }

        /// <summary> Flag indicating whether this generation task has finished.
        /// Expected to be set right before Generate() exits. </summary>
        bool Finished { get; }

        /// <summary> Progress percentage -- an integer between 0 and 100.
        /// Should start at 0 before Generate() is called,
        /// get updated as Generate is working, and end up at exactly 100 by the time Generate() returns. </summary>
        int Progress { get; }

        /// <summary> String representing the current state of the map generator.
        /// This information will be shown to users in ConfigGUI. </summary>
        string StatusString { get; }

        /// <summary> Flag: whether this generation task will report progress. </summary>
        bool ReportsProgress { get; }

        /// <summary> Flag: whether this generation task supports async cancellation. </summary>
        bool SupportsCancellation { get; }

        /// <summary> Map that has been generated (may be null). </summary>
        [CanBeNull]
        Map Result { get; }


        /// <summary> Event that is raised when progress percentage or status string change. </summary>
        event ProgressChangedEventHandler ProgressChanged;


        /// <summary> Synchronously creates a map file. This will be invoked on a worker thread. </summary>
        /// <returns> Created map file, or null (if generation was aborted). </returns>
        [CanBeNull]
        Map Generate();

        /// <summary> Sigals this task to asynchronously finish executing. </summary>
        void CancelAsync();
    }
}