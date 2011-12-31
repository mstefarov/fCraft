// fCraft is Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace fCraft {
    /// <summary> Loads CIL assemblies and instantiates IPlugin objects. </summary>
    public sealed class CILPluginLoader : IPluginLoader {
        public string[] PluginExtensions {
            get { return new[] { ".dll" }; }
        }

        public PluginLoadResult LoadPlugins( string pluginName ) {
            try {
                List<IPlugin> plugins = new List<IPlugin>();
                Assembly assembly = Assembly.LoadFrom( pluginName );
                foreach( Type pluginType in assembly.GetTypes() ) {
                    if( pluginType.GetInterfaces().Contains( typeof( IPlugin ) ) && pluginType.IsClass ) {
                        plugins.Add( (IPlugin)Activator.CreateInstance( pluginType ) );
                    }
                }
                return new PluginLoadResult {
                    LoadSuccessful = true,
                    LoadedPlugins = plugins
                };
            } catch( Exception exception ) {
                return new PluginLoadResult {
                    Exception = exception,
                    LoadSuccessful = false
                };
            }
        }
    }
}