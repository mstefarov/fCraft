using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Events {
    public class MainWorldChangedEventArgs : EventArgs {
        internal MainWorldChangedEventArgs( World oldWorld, World newWorld ) {
            OldMainWorld = oldWorld;
            NewMainWorld = newWorld;
        }
        public World OldMainWorld { get; private set; }
        public World NewMainWorld { get; private set; }
    }


    public sealed class MainWorldChangingEventArgs : MainWorldChangedEventArgs, ICancellableEvent {
        internal MainWorldChangingEventArgs( World oldWorld, World newWorld ) : base( oldWorld, newWorld ) { }
        public bool Cancel { get; set; }
    }


    public sealed class SearchingForWorldEventArgs : EventArgs, IPlayerEvent {
        internal SearchingForWorldEventArgs( Player player, string searchTerm, List<World> matches ) {
            Player = player;
            SearchTerm = searchTerm;
            Matches = matches;
        }

        public Player Player { get; private set; }
        public string SearchTerm { get; private set; }
        public List<World> Matches { get; set; }
    }


    public sealed class WorldCreatingEventArgs : EventArgs, ICancellableEvent {
        public WorldCreatingEventArgs( Player player, string worldName, Map map ) {
            Player = player;
            WorldName = worldName;
            Map = map;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        public string WorldName { get; set; }
        public Map Map { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class WorldCreatedEventArgs : EventArgs, IPlayerEvent, IWorldEvent {
        public WorldCreatedEventArgs( Player player, World world ) {
            Player = player;
            World = world;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        public World World { get; private set; }
    }
}