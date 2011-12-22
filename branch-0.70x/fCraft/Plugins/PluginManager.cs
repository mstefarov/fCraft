// fCraft is Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fCraft {
    /// <summary> Manages all Plugin loaders and Plugin objects. </summary>
    public class PluginManager {

        public List<IPluginLoader> PluginLoaders { get; private set; }
        public List<IPlugin> Plugins { get; private set; }

        public EventHandler<PluginLoadedEventArgs> PluginLoaded;
        public EventHandler<PluginLoadFailedEventArgs> PluginLoadFail;

        public PluginManager() {
            PluginLoaders = new List<IPluginLoader>() {
                new CLIPluginLoader()
            };
            Plugins = new List<IPlugin>();
        }

        public void AddLoader( IPluginLoader loader ) {
            PluginLoaders.Add( loader );
        }

        public void LoadPlugins() {
            DirectoryInfo directoryInfo = new DirectoryInfo( "plugins" );
            if( directoryInfo.Exists ) {
                LoadPlugins( directoryInfo.EnumerateFiles().Select( file => file.Name ) );
            }
        }

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

        private bool LoaderClaims( IPluginLoader pluginLoader, string fileName ) {
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