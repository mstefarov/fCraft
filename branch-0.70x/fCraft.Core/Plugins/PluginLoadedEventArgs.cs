// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;

namespace fCraft {
    /// <summary> Triggered when a plugin is loaded. </summary>
    public sealed class PluginLoadedEventArgs : EventArgs {
        /// <summary> Creates a new PluginLoadedEventArgs, and records the plugin being loaded. </summary>
        /// <param name="plugin"> Plugin being loaded. </param>
        public PluginLoadedEventArgs( IPlugin plugin ) {
            Plugin = plugin;
        }
        /// <summary> Plugin being Loaded. </summary>
        public IPlugin Plugin { get; private set; }
    }
}