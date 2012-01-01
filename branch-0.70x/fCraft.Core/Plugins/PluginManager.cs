// fCraft is Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Manages all Plugin loaders and Plugin objects. </summary>
    [PublicAPI]
    public sealed class PluginManager {

        /// <summary> List of all PluginLoaders currently being managed by this PluginManager. </summary>
        public List<IPluginLoader> PluginLoaders { get; private set; }
        /// <summary> List of all Plugins currently being managed by this PluginManager. </summary>
        public List<IPlugin> Plugins { get; private set; }

        /// <summary> Is triggered when a plugin is successfully loaded. </summary>
        public EventHandler<PluginLoadedEventArgs> PluginLoaded;
        /// <summary> Is triggered when a plugin fails to load. </summary>
        public EventHandler<PluginLoadFailedEventArgs> PluginLoadFail;

        /// <summary> Creates a new instance of PluginManager, and initialises the Plugin and PluginLoader lists. </summary>
        public PluginManager() {
            PluginLoaders = new List<IPluginLoader> {
                new CILPluginLoader()
            };
            Plugins = new List<IPlugin>();
        }

        /// <summary> Adds the specified IPluginLoader to the list of PluginLoaders. </summary>
        /// <param name="loader"> PluginLoader to add to list. </param>
        public void AddLoader( IPluginLoader loader ) {
            PluginLoaders.Add( loader );
        }

        /// <summary> Loads all plugins in the plugin directory. </summary>
        public void LoadPlugins() {
            DirectoryInfo directoryInfo = new DirectoryInfo( "plugins" );
            if( directoryInfo.Exists ) {
                LoadPlugins( directoryInfo.EnumerateFiles().Select( file => file.Name ) );
            }
        }

        /// <summary> Loads a list of plugins by their filenames. </summary>
        /// <param name="fileNames"> List of plugin filenames to laod. </param>
        public void LoadPlugins( IEnumerable<string> fileNames ) {
            foreach( string fileName in fileNames ) {
                string filePath = Path.Combine( "plugins", fileName );
                foreach( IPluginLoader pluginLoader in PluginLoaders ) {
                    if( LoaderClaims( pluginLoader, filePath ) ) {
                        PluginLoadResult result = pluginLoader.LoadPlugins( filePath );
                        if( result.LoadSuccessful ) {
                            foreach( IPlugin newPlugin in result.LoadedPlugins ) {
                                newPlugin.Enable( this );
                                Plugins.Add( newPlugin );
                                OnPluginLoaded( this, new PluginLoadedEventArgs( newPlugin ) );
                            }
                        } else {
                            OnPluginLoadFail( this, new PluginLoadFailedEventArgs( fileName, result.Exception ) );
                        }
                    }
                }
            }
        }

        private static bool LoaderClaims( IPluginLoader pluginLoader, string fileName ) {
            foreach( string extension in pluginLoader.PluginExtensions ) {
                if( fileName.EndsWith( extension ) ) {
                    return true;
                }
            }
            return false;
        }

        private void OnPluginLoaded( object sender, PluginLoadedEventArgs e ) {
            EventHandler<PluginLoadedEventArgs> handler = PluginLoaded;
            if( handler != null ) {
                handler( sender, e );
            }
        }

        private void OnPluginLoadFail( object sender, PluginLoadFailedEventArgs e ) {
            EventHandler<PluginLoadFailedEventArgs> handler = PluginLoadFail;
            if( handler != null ) {
                handler( sender, e );
            }
        }
    }
}