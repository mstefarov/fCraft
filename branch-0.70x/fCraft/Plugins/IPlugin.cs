// fCraft is Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;

namespace fCraft {
    /// <summary> Defines information about a plugin. 
    /// There may be multiple of these types defined in an assembly or module. </summary>
    public interface IPlugin {
        string Name { get; }
        string Author { get; }
        string Description { get; }
        Version Version { get; }
        Version MinFCraftVersion { get; }
        Version MaxFCraftVersion { get; }

        void Enable( PluginManager manager );
        void Disable( PluginManager manager );
    }
}