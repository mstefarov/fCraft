// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem based heavily on code contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using JetBrains.Annotations;
using fCraft.Events;

namespace fCraft {
    /// <summary> Manages all Plugin loaders and Plugin objects. </summary>
    public static class PluginManager {
        static IPluginLoader cilPluginLoader;
        static IPluginLoader pythonPluginLoader;

        /// <summary> List of all Plugins currently being managed by this PluginManager. </summary>
        public static Dictionary<string,IPlugin> Plugins { get; private set; }

        public static Dictionary<string, PluginDescriptor> PluginDescriptors { get; private set; }


        const string PythonPluginLoaderType = "fCraft.Python.PythonPluginLoader";
        static bool initialized;

        /// <summary> Initializes plugin loaders, and attempts to load all plugins. </summary>
        /// <exception cref="InvalidOperationException"> If PluginManager is already initialized. </exception>
        public static void Init() {
            if( initialized ) throw new InvalidOperationException( "PluginManager is already initialized." );
            initialized = true;

            Plugins = new Dictionary<string, IPlugin>();
            PluginDescriptors = new Dictionary<string, PluginDescriptor>();

            cilPluginLoader = new CILPluginLoader();

            string pythonPath = Path.Combine( Paths.WorkingPath, Paths.PythonPluginLoaderModule );
            if( File.Exists( pythonPath ) ) {
                Assembly pythonLoaderAsm = Assembly.LoadFile( pythonPath );
                IPluginLoader pythonLoader = (IPluginLoader)pythonLoaderAsm.CreateInstance( PythonPluginLoaderType );
                if( pythonLoader != null ) {
                    pythonPluginLoader = pythonLoader;
                    Logger.Log( LogType.Debug, "PluginManager: Python plugin support enabled." );
                } else {
                    Logger.Log( LogType.Error, "PluginManager: Failed to load Python plugin support." );
                }
            }

            DirectoryInfo pluginsDir = new DirectoryInfo( Paths.PluginDirectory );
            if( pluginsDir.Exists ) {
                foreach( FileInfo file in pluginsDir.EnumerateFiles( ".fpi", SearchOption.AllDirectories ) ) {
                    LoadDescriptor( file.FullName );
                }
            }
        }


        static void LoadDescriptor( string fullName ) {
            try {
                XDocument descriptorXml = XDocument.Load( fullName );
                PluginDescriptor descriptor = new PluginDescriptor( descriptorXml.Root );
                if( descriptor.LoaderType == PluginLoaderType.Python && pythonPluginLoader == null ) {
                    Logger.Log( LogType.Warning,
                                "PluginManager: Could not load {0}: python support is disabled.",
                                fullName );
                } else {
                    PluginDescriptors.Add( descriptor.Name, descriptor );
                }
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "Could not load plugin descriptor from {0}: {1}", fullName, ex );
            }
        }


        /// <summary> Activates all loaded plugins. </summary>
        /// <exception cref="InvalidOperationException"> If PluginManager is not initialized. </exception>
        public static void ActivatePlugins() {
            if( !initialized ) throw new InvalidOperationException( "PluginManager is not initialized." );
            foreach( PluginDescriptor descriptor in PluginDescriptors.Values ) {
                try {
                    IPluginLoader loader;
                    if( descriptor.LoaderType == PluginLoaderType.Python ) {
                        loader = pythonPluginLoader;
                    } else {
                        loader = cilPluginLoader;
                    }
                    IPlugin plugin = loader.LoadPlugin( descriptor );
                    RaisePluginActivatedEvent( plugin );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "Could not activate plugin {0} {1}: {2}",
                                descriptor.Name, descriptor.Version, ex );
                }
            }
        }


        /// <summary> Tries to find a plugin by name. </summary>
        /// <param name="pluginName"> Case-insensitive full name of the plugin. </param>
        /// <returns> Relevant IPlugin object if found; null if not found. </returns>
        /// <exception cref="ArgumentNullException"> If pluginName is null. </exception>
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
        public static event EventHandler<PluginAddedEventArgs> PluginAdded;
         
        /// <summary> Occurs when a plugin is succesfully activated. </summary>
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


        [Pure]
        public static bool IsValidPluginName( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 32 ) return false;
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( ( ch < '0' && ch != '.' ) || ( ch > '9' && ch < 'A' ) ||
                    ( ch > 'Z' && ch < '_' ) || ( ch > '_' && ch < 'a' ) || ch > 'z' ) {
                    return false;
                }
            }
            return true;
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