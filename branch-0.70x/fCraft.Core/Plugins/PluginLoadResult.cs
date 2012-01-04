// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> The result of a plugin load event, whether it was successful or not and the exception thrown. </summary>
    public sealed class PluginLoadResult {
        /// <summary> List of plugins that were attempted to be loaded. </summary>
        public List<IPlugin> LoadedPlugins { get; set; }
        /// <summary> Whether or not the plugins were loaded successfuly. </summary>
        public bool LoadSuccessful { get; set; }
        /// <summary> The exception that occurred, Null if no error occurred. </summary>
        [CanBeNull]
        public Exception Exception { get; set; }
    }
}