// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;

namespace fCraft {
    public sealed class PluginLoadedEventArgs : EventArgs {
        public PluginLoadedEventArgs( IPlugin plugin ) {
            Plugin = plugin;
        }
        public IPlugin Plugin { get; private set; }
    }
}