// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;

namespace fCraft {
    /// <summary> Defines information about a plugin. 
    /// There may be multiple of these types defined in an assembly or module. </summary>
    public interface IPlugin {
        /// <summary> Name of the this plugin. </summary>
        string Name { get; }
        /// <summary> Name of the person or organisation who created this plugin. </summary>
        string Author { get; }
        /// <summary> A short paragraph or sentence descibing what this plugin does. </summary>
        string Description { get; }
        /// <summary> Version of this plugin. </summary>
        Version Version { get; }
        /// <summary> The minimum version of fCraft required to run this plugin. </summary>
        Version MinFCraftVersion { get; }
        /// <summary> The maximum version of fCraft that this plugins runs on. </summary>
        Version MaxFCraftVersion { get; }
        /// <summary> Enables this plugin using the specified PluginManager. </summary>
        /// <param name="manager"> PluginManager to enable this plugin with. </param>
        void Enable( PluginManager manager );
        /// <summary> Disables this plugin using the specified PluginManager. </summary>
        /// <param name="manager"> PluginManager to disable this plugin with. </param>
        void Disable( PluginManager manager );
    }
}