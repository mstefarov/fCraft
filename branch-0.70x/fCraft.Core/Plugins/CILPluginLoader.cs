// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace fCraft {
    // Loads CIL assemblies and instantiates IPlugin objects.
    sealed class CILPluginLoader : IPluginLoader {
        readonly Dictionary<string, Assembly> assemblyCache = new Dictionary<string, Assembly>();

        public PluginLoaderType LoaderType {
            get { return PluginLoaderType.CIL; }
        }


        public IPlugin LoadPlugin( PluginDescriptor descriptor ) {
            if( descriptor == null ) throw new ArgumentNullException( "descriptor" );
            string descriptorPath = Path.GetDirectoryName( descriptor.PluginDescriptorFileName );
            string fileName = Path.GetFullPath( Path.Combine( descriptorPath, descriptor.PluginFileName ) );

            Assembly assembly;
            if( !assemblyCache.TryGetValue( fileName, out assembly ) ) {
                assembly = Assembly.LoadFrom( fileName );
            }

            IPlugin pluginInstance = (IPlugin)assembly.CreateInstance( descriptor.PluginTypeName );
            if( pluginInstance == null ) {
                throw new Exception( "Could not find given plugin type" );
            }
            return pluginInstance;
        }
    }
}