// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides the ability to load plugins of a specific type. </summary>
    public interface IPluginLoader {
        [NotNull]
        IPlugin LoadPlugin( [NotNull] PluginDescriptor fileName );
    }

    public enum PluginLoaderType {
        CIL,
        Python
    }
}