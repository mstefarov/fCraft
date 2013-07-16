// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Represents a single-use state object for a particular set of map generator parameters (IMapGeneratorParameters).
    /// Provides a synchronous method to carry out generation, asynchronous method to cancel generation,
    /// various properties to indicate progress, and an event to report progress changes. </summary>
    public abstract class MapGeneratorState {
        /// <summary> Associated map generation parameters. </summary>
        [NotNull]
        public MapGeneratorParameters Parameters { get; protected set; }

        /// <summary> Flag indicating whether this generation task has been canceled. 
        /// Should be set by CancelAsync(), regardless of whether async cancelation is supported. </summary>
        public bool Canceled { get; protected set; }

        /// <summary> Flag indicating whether this generation task has finished.
        /// Expected to be set right before Generate() exits. </summary>
        public bool Finished { get; protected set; }

        /// <summary> Progress percentage -- an integer between 0 and 100.
        /// Should start at 0 before Generate() is called,
        /// get updated as Generate is working, and end up at exactly 100 by the time Generate() returns. </summary>
        public int Progress { get; protected set; }

        /// <summary> String representing the current state of the map generator.
        /// This information will be shown to users in ConfigGUI. </summary>
        public string StatusString { get; protected set; }

        /// <summary> Flag: whether this generation task will report progress. </summary>
        public bool ReportsProgress { get; protected set; }

        /// <summary> Flag: whether this generation task supports async cancellation. </summary>
        public bool SupportsCancellation { get; protected set; }

        /// <summary> Map that has been generated (may be null). </summary>
        [CanBeNull]
        public Map Result { get; protected set; }


        /// <summary> Event that is raised when progress percentage or status string change. </summary>
        public event ProgressChangedEventHandler ProgressChanged;


        /// <summary> Synchronously creates a map file. This will be invoked on a worker thread. </summary>
        /// <returns> Created map file, or null (if generation was aborted). </returns>
        [CanBeNull]
        public abstract Map Generate();

        /// <summary> Sigals this task to asynchronously finish executing. </summary>
        public virtual void CancelAsync() {
            Canceled = true;
        }


        protected virtual void ReportProgress( int progressPercent, [NotNull] string statusString ) {
            if( statusString == null ) {
                throw new ArgumentNullException( "statusString" );
            }
            Progress = progressPercent;
            StatusString = statusString;
            var handler = ProgressChanged;
            if( handler != null ) {
                ProgressChangedEventArgs args = new ProgressChangedEventArgs( progressPercent, statusString );
                handler( this, args );
            }
        }
    }
}