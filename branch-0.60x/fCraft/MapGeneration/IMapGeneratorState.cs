using System.ComponentModel;

namespace fCraft {
    /// <summary> Represents a single-use state object for a paricular set of map generator parameters (IMapGeneratorParameters).
    /// Provides a synchronous method to carry out generation, asynchronous method to cancel generation,
    /// various properties to indicate progress, and an event to report progress changes. </summary>
    public interface IMapGeneratorState {
        /// <summary> Associated map generation parameters. </summary>
        IMapGeneratorParameters Parameters { get; }

        /// <summary> Flag indicating whether this generation task has been canceled. 
        /// Should be set by CancelAsync(), regardless of whether async cancelation is supported. </summary>
        bool Canceled { get; }

        /// <summary> Flag indicating whether this generation task has finished.
        /// Expected to be set right before Generate() exits. </summary>
        bool Finished { get; }

        int Progress { get; }

        /// <summary> String representing the current state of the map generator.
        /// This information will be shown to users in ConfigGUI. </summary>
        string StatusString { get; }

        /// <summary> Flag: whether this generation task will report progress. </summary>
        bool ReportsProgress { get; }

        /// <summary> Flag: whether this generation task supports async cancellation. </summary>
        bool SupportsCancellation { get; }

        /// <summary> Event that is raised when progress percentage or status string change. </summary>
        event ProgressChangedEventHandler ProgressChanged;

        /// <summary> Synchronously creates a map file. This will be invoked on a worker thread. </summary>
        /// <returns></returns>
        Map Generate();

        /// <summary> Sigals this task to asynchronously finish executing. </summary>
        void CancelAsync();
    }
}