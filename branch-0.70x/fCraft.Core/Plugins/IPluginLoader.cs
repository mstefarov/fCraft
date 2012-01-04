// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).

namespace fCraft {
    /// <summary> Provides the ability to load plugins of a specific type,
    /// such as IronPython, .NET assemblies, etc. </summary>
    public interface IPluginLoader {
        PluginLoadResult LoadPlugins( string pluginName );
        string[] PluginExtensions { get; }
    }
}