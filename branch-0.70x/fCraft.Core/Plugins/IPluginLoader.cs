// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides the ability to load plugins of a specific type. </summary>
    public interface IPluginLoader {
        [NotNull]
        IPlugin LoadPlugin( [NotNull] PluginDescriptor descriptor );
    }

    public enum PluginLoaderType {
        CIL,
        Python
    }
}