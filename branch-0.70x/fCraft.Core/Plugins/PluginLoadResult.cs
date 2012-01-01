// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;

namespace fCraft {
    public sealed class PluginLoadResult {
        public List<IPlugin> LoadedPlugins { get; set; }
        public bool LoadSuccessful { get; set; }
        public Exception Exception { get; set; }
    }
}