using System;

namespace fCraft {
    public interface IPluginProvider {
        string Name { get; }
        string Author { get; }
        string Description { get; }
        Version Version { get; }
        Version MinFCraftVersion { get; }
        Version MaxFCraftVersion { get; }

        IPlugin CreateInstance( string dataPath );
    }

    public interface IPlugin {
        IPluginProvider Provider { get; }
    }
}
