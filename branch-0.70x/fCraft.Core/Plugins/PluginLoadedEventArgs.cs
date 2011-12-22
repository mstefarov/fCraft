// fCraft is Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;

namespace fCraft {
    public class PluginLoadedEventArgs : EventArgs {
        public PluginLoadedEventArgs( IPlugin plugin )
            : base() {
            Plugin = plugin;
        }
        public IPlugin Plugin { get; private set; }
    }
}