// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Defines information about a plugin. 
    /// There may be multiple of these types defined in an assembly or module. </summary>
    public interface IPlugin {
        /// <summary> Name of the this plugin. </summary>
        [NotNull]
        string Name { get; }

        /// <summary> Name of the person or organisation who created this plugin. </summary>
        [NotNull]
        string Author { get; }

        /// <summary> A short paragraph or sentence descibing what this plugin does. </summary>
        [NotNull]
        string Description { get; }

        /// <summary> Version of this plugin. </summary>
        [NotNull]
        Version Version { get; }

        /// <summary> The minimum version of fCraft required to run this plugin. </summary>
        [NotNull]
        Version MinFCraftVersion { get; }

        /// <summary> The maximum version of fCraft that this plugins runs on. </summary>
        [NotNull]
        Version MaxFCraftVersion { get; }

        /// <summary> Whether this plugin has been activated. </summary>
        bool IsActivated { get; }

        /// <summary> Enables this plugin using the specified PluginManager. </summary>
        void Activate();
    }
}