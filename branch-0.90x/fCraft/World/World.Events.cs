// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Events {
    /// <summary> An EventArgs for an event that directly relates to a particular world. </summary>
    public interface IWorldEvent {
        /// <summary> World affected by the event. </summary>
        [NotNull]
        World World { get; }
    }


    /// <summary> Provides data for WorldManager.MainWorldChanged event. Immutable. </summary>
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


    /// <summary> Provides data for WorldManager.MainWorldChanging event. Cancelable. </summary>
    public sealed class MainWorldChangingEventArgs : MainWorldChangedEventArgs, ICancelableEvent {
        internal MainWorldChangingEventArgs( [CanBeNull] World oldWorld, [NotNull] World newWorld )
            : base( oldWorld, newWorld ) { }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for WorldManager.SearchingForWorld event. Allows changing the results. </summary>
    public sealed class SearchingForWorldEventArgs : EventArgs {
        internal SearchingForWorldEventArgs( [CanBeNull] Player player, [NotNull] string searchQuery, [NotNull] List<World> matches ) {
            if( searchQuery == null ) throw new ArgumentNullException( "searchQuery" );
            if( matches == null ) throw new ArgumentNullException( "matches" );
            Player = player;
            SearchQuery = searchQuery;
            Matches = matches;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        [NotNull]
        public string SearchQuery { get; private set; }

        [NotNull]
        public List<World> Matches { get; set; }
    }


    /// <summary> Provides data for WorldManager.WorldCreating event. Allows renaming. Cancelable. </summary>
    public sealed class WorldCreatingEventArgs : EventArgs, ICancelableEvent {
        internal WorldCreatingEventArgs( [CanBeNull] Player player, [NotNull] string worldName, [CanBeNull] Map map ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Player = player;
            WorldName = worldName;
            Map = map;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        [NotNull]
        public string WorldName { get; set; }

        [CanBeNull]
        public Map Map { get; private set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for WorldManager.WorldCreated event. Immutable. </summary>
    public sealed class WorldCreatedEventArgs : EventArgs, IWorldEvent {
        internal WorldCreatedEventArgs( [CanBeNull] Player player, [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            Player = player;
            World = world;
        }

        [CanBeNull]
        public Player Player { get; private set; }

        public World World { get; private set; }
    }
}