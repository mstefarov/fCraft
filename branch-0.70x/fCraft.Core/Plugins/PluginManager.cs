// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem based heavily on code contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using fCraft.Events;

namespace fCraft {
    /// <summary> Manages all Plugin loaders and Plugin objects. </summary>
    public static class PluginManager {
        static List<IPluginLoader> PluginLoaders { get; set; }

        /// <summary> List of all Plugins currently being managed by this PluginManager. </summary>
        public static Dictionary<string,IPlugin> Plugins { get; private set; }


        const string PythonPluginLoaderType = "fCraft.Python.PythonPluginLoader";
        static bool initialized;

        /// <summary> Initializes plugin loaders, and attempts to load all plugins. </summary>
        /// <exception cref="InvalidOperationException"> If PluginManager is already initialized. </exception>
        [PublicAPI]
        public static void Init() {
            if( initialized ) throw new InvalidOperationException( "PluginManager is already initialized." );
            initialized = true;

            Plugins = new Dictionary<string, IPlugin>();
            PluginLoaders = new List<IPluginLoader>();

            AddLoader( new CILPluginLoader() );

            string pythonPath = Path.Combine( Paths.WorkingPath, Paths.PythonPluginLoaderModule );
            if( File.Exists( pythonPath ) ) {
                Assembly pythonLoaderAsm = Assembly.LoadFile( pythonPath );
                IPluginLoader pythonLoader = (IPluginLoader)pythonLoaderAsm.CreateInstance( PythonPluginLoaderType );
                if( pythonLoader != null ) {
                    AddLoader( pythonLoader );
                    Logger.Log( LogType.Debug, "PluginManager: Python plugin support enabled." );
                } else {
                    Logger.Log( LogType.Error, "PluginManager: Failed to load Python plugin support." );
                }
            }

            DirectoryInfo pluginsDir = new DirectoryInfo( Paths.PluginDirectory );
            if( pluginsDir.Exists ) {
                foreach( FileInfo file in pluginsDir.EnumerateFiles() ) {
                    AddPlugin( file.FullName );
                }
            }
        }


        static bool LoaderClaims( [NotNull] IPluginLoader pluginLoader, [NotNull] string fileName ) {
            if( pluginLoader == null ) throw new ArgumentNullException( "pluginLoader" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return pluginLoader.PluginExtensions
                               .Any( ext => fileName.EndsWith( ext, StringComparison.OrdinalIgnoreCase ) );
        }


        static void AddLoader( [NotNull] IPluginLoader loader ) {
            if( loader == null ) throw new ArgumentNullException( "loader" );
            PluginLoaders.Add( loader );
        }


        /// <summary> Attempts to load all plugins from the given file.
        /// Does not activate any plugins. </summary>
        /// <param name="fileName"> Relative or absolute path to the file. </param>
        /// <exception cref="ArgumentNullException"> If fileName is null. </exception>
        [PublicAPI]
        public static void AddPlugin( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );

            // build number is stripped
            Version fVersion = new Version( Updater.CurrentRelease.Version.Major,
                                            Updater.CurrentRelease.Version.Minor );

            foreach( IPluginLoader pluginLoader in PluginLoaders ) {
                if( LoaderClaims( pluginLoader, fileName ) ) {
                    PluginLoadResult result = pluginLoader.LoadPlugins( fileName );
                    if( result.LoadSuccessful ) {
                        foreach( IPlugin newPlugin in result.LoadedPlugins ) {

                            if( newPlugin.MinFCraftVersion > fVersion ) {
                                Logger.Log( LogType.Error,
                                            "PluginLoader: Plugin \"{0} {1}\" requires a newer version of fCraft ({2}) and will not work.",
                                            newPlugin.Name, newPlugin.Version, newPlugin.MinFCraftVersion );
                                continue;
                            }
                            if( newPlugin.MaxFCraftVersion < fVersion ) {
                                Logger.Log( LogType.Warning,
                                            "PluginLoader: Plugin \"{0} {1}\" was designed to work with " +
                                            "older versions of fCraft ({2} through {3}) and may not work correctly.",
                                            newPlugin.Name, newPlugin.Version,
                                            newPlugin.MinFCraftVersion, newPlugin.MaxFCraftVersion );
                            }

                            Plugins.Add( newPlugin.Name.ToLower(), newPlugin );
                            Logger.Log( LogType.SystemActivity,
                                        "PluginLoader: Added {0} {1}",
                                        newPlugin.Name, newPlugin.Version );
                            RaisePluginAddedEvent( pluginLoader, fileName, newPlugin );
                        }
                    }
                }
            }
        }


        /// <summary> Activates all loaded plugins. </summary>
        /// <exception cref="InvalidOperationException"> If PluginManager is not initialized. </exception>
        [PublicAPI]
        public static void ActivatePlugins() {
            if( !initialized ) throw new InvalidOperationException( "PluginManager is not initialized." );
            foreach( IPlugin plugin in Plugins.Values ) {
                plugin.Activate();
                Logger.Log( LogType.SystemActivity,
                            "PluginLoader: Activated {0} {1}",
                            plugin.Name, plugin.Version );
                RaisePluginActivatedEvent( plugin );
            }
        }


        /// <summary> Tries to find a plugin by name. </summary>
        /// <param name="pluginName"> Case-insensitive full name of the plugin. </param>
        /// <returns> Relevant IPlugin object if found; null if not found. </returns>
        /// <exception cref="ArgumentNullException"> If pluginName is null. </exception>
        [PublicAPI]
        public static IPlugin Find( [NotNull] string pluginName ) {
            if( pluginName == null ) throw new ArgumentNullException( "pluginName" );
            IPlugin result;
            if( Plugins.TryGetValue( pluginName, out result ) ) {
                return result;
            } else {
                return null;
            }
        }


        /// <summary> Occurs when a plugin is successfully loaded. </summary>
        [PublicAPI]
        public static event EventHandler<PluginAddedEventArgs> PluginAdded;
         
        /// <summary> Occurs when a plugin is succesfully activated. </summary>
        [PublicAPI]
        public static event EventHandler<PluginActivatedEventArgs> PluginActivated;


        static void RaisePluginAddedEvent( [NotNull] IPluginLoader loader, [NotNull] string fileName, [NotNull] IPlugin plugin ) {
            var handler = PluginAdded;
            if( handler != null ) {
                handler( null, new PluginAddedEventArgs( loader, fileName, plugin ) );
            }
        }


        static void RaisePluginActivatedEvent( IPlugin plugin ) {
            var handler = PluginActivated;
            if( handler != null ) {
                handler( null, new PluginActivatedEventArgs( plugin ) );
            }
        }
    }
}


namespace fCraft.Events {
    /// <summary> Provides data for PluginManager.PluginAdded event. Immutable. </summary>
    public sealed class PluginAddedEventArgs : EventArgs {
        internal PluginAddedEventArgs( [NotNull] IPluginLoader loader, [NotNull] string fileName, [NotNull] IPlugin plugin ) {
            if( loader == null ) throw new ArgumentNullException( "loader" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( plugin == null ) throw new ArgumentNullException( "plugin" );
            FileName = fileName;
            Plugin = plugin;
            Loader=loader;
        }


        /// <summary> IPluginLoader responsible for loading this plugin. </summary>
        [NotNull]
        public IPluginLoader Loader { get; private set; }

        /// <summary> Full name of the file that this plugin was loaded from. </summary>
        [NotNull]
        public string FileName { get; set; }

        /// <summary> Newly-added plugin. </summary>
        [NotNull]
        public IPlugin Plugin { get; private set; }
    }


    /// <summary> Provides data for PluginManager.PluginActivated event. Immutable. </summary>
    public sealed class PluginActivatedEventArgs : EventArgs {
        internal PluginActivatedEventArgs( [NotNull] IPlugin plugin ) {
            if( plugin == null ) throw new ArgumentNullException( "plugin" );
            Plugin = plugin;
        }

        /// <summary> Newly-activated plugin. </summary>
        [NotNull]
        public IPlugin Plugin { get; private set; }
    }
}