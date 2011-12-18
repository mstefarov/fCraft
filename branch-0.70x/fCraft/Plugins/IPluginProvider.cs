// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using fCraft.Events;

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

    public class NoFsAllowedPluginProvider : IPluginProvider {
        public string Name {
            get { return "NoFsAllowed"; }
        }

        public string Author {
            get { return "fragmer"; }
        }

        public string Description {
            get { return "Kicks all players whose names start with the letter 'f'"; }
        }

        public Version Version {
            get { return new Version(1,0); }
        }

        public Version MinFCraftVersion {
            get { return new Version(0,700); }
        }

        public Version MaxFCraftVersion {
            get { return new Version(0,700); }
        }

        public IPlugin CreateInstance( string dataPath ) {
            return new NoFsAllowedPlugin( this );
        }


        class NoFsAllowedPlugin : IPlugin {
            public NoFsAllowedPlugin( IPluginProvider provider ) {
                Provider = provider;
                Player.Connecting += OnPlayerConnecting;
            }

            void OnPlayerConnecting( object sender, PlayerConnectingEventArgs e ) {
                string name = e.Player.Name.ToLower();
                if( name.StartsWith( "f" ) ) {
                    e.Cancel = true;
                }
            }

            public IPluginProvider Provider { get; private set; }
        }
    }
}
