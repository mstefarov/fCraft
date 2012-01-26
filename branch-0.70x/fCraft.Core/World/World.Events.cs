using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Events {
    /// <summary> An EventArgs for an event that directly relates to a particular world. </summary>
    public interface IWorldEvent {
        /// <summary> World affected by the event. </summary>
        World World { get; }
    }


    public class MainWorldChangedEventArgs : EventArgs {
        internal MainWorldChangedEventArgs( [CanBeNull] World oldWorld, [NotNull] World newWorld ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            OldMainWorld = oldWorld;
            NewMainWorld = newWorld;
        }


        [CanBeNull]
        public World OldMainWorld { get; private set; }

        [NotNull]
        public World NewMainWorld { get; private set; }
    }


    public sealed class MainWorldChangingEventArgs : MainWorldChangedEventArgs, ICancellableEvent {
        internal MainWorldChangingEventArgs( World oldWorld, [NotNull] World newWorld )
            : base( oldWorld, newWorld ) { }

        public bool Cancel { get; set; }
    }


    public sealed class SearchingForWorldEventArgs : EventArgs, IPlayerEvent {
        internal SearchingForWorldEventArgs( [CanBeNull] Player player, [NotNull] string searchTerm, [NotNull] List<World> matches ) {
            if( searchTerm == null ) throw new ArgumentNullException( "searchTerm" );
            if( matches == null ) throw new ArgumentNullException( "matches" );
            Player = player;
            SearchTerm = searchTerm;
            Matches = matches;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        [NotNull]
        public string SearchTerm { get; private set; }

        [NotNull]
        public List<World> Matches { get; set; }
    }


    public sealed class WorldCreatingEventArgs : EventArgs, ICancellableEvent {
        internal WorldCreatingEventArgs( [CanBeNull] Player player, [NotNull] string worldName, [CanBeNull] Map map, bool fromXml ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Player = player;
            WorldName = worldName;
            Map = map;
            FromXml = fromXml;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        [NotNull]
        public string WorldName { get; set; }

        [CanBeNull]
        public Map Map { get; private set; }

        public bool FromXml { get; private set; }

        public bool Cancel { get; set; }
    }


    public sealed class WorldCreatedEventArgs : EventArgs, IPlayerEvent, IWorldEvent {
        internal WorldCreatedEventArgs( [CanBeNull] Player player, [NotNull] World world, bool fromXml ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            Player = player;
            World = world;
            FromXml = fromXml;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        [NotNull]
        public World World { get; private set; }


        public bool FromXml { get; private set; }
    }
}