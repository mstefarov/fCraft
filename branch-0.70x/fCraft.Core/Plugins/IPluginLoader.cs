// fCraft is Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).

namespace fCraft {
    /// <summary> Provides the ability to load plugins of a specific type, such a Python, .NET assmeblies or Ruby. </summary>
    public interface IPluginLoader {
        PluginLoadResult LoadPlugins( string pluginName );
        string[] PluginExtensions { get; }
    }
}