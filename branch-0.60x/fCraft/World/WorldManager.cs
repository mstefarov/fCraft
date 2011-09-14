// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using fCraft.Events;
using fCraft.MapConversion;
using JetBrains.Annotations;

namespace fCraft {
    public static class WorldManager {

        public static World[] WorldList { get; private set; }
        static readonly SortedDictionary<string, World> Worlds = new SortedDictionary<string, World>();

        internal static readonly object WorldListLock = new object();


        static World mainWorld;
        /// <summary> Gets or sets the default main world.
        /// That's the world that players first join upon connecting.
        /// The map of the new main world is preloaded, and old one is unloaded, if needed. </summary>
        /// <exception cref="System.ArgumentNullException" />
        /// <exception cref="fCraft.WorldOpException" />
        public static World MainWorld {
            get { return mainWorld; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value == mainWorld ) return;
                if( RaiseMainWorldChangingEvent( mainWorld, value ) ) {
                    throw new WorldOpException( value.Name, WorldOpExceptionCode.PluginDenied );
                }
                World oldWorld;
                lock( WorldListLock ) {
                    value.NeverUnload = true;
                    oldWorld = mainWorld;
                    if( oldWorld != null ) {
                        oldWorld.NeverUnload = false;
                    }
                    mainWorld = value;
                }
                RaiseMainWorldChangedEvent( oldWorld, value );
            }
        }


        #region World List Saving/Loading

        internal static bool LoadWorldList() {
            WorldList = new World[0];
            if( File.Exists( Paths.WorldListFileName ) ) {
                try {
                    XDocument doc = XDocument.Load( Paths.WorldListFileName );
                    XElement root = doc.Root;
                    if( root != null ) {
                        World firstWorld = null;
                        foreach( XElement el in root.Elements( "World" ) ) {
#if !DEBUG
                            try {
#endif
                                LoadWorldListEntry( el, ref firstWorld );
#if !DEBUG
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "An error occured while trying to parse one of the entries on the world list",
                                                          "fCraft", ex, false );
                            }
#endif
                        }

                        XAttribute temp;
                        if( (temp = root.Attribute( "main" )) != null ) {
                            World suggestedMainWorld = FindWorldExact( temp.Value );

                            if( suggestedMainWorld != null ) {
                                MainWorld = suggestedMainWorld;

                            } else if( firstWorld != null ) {
                                // if specified main world does not exist, use first-defined world
                                Logger.Log( "The specified main world \"{0}\" does not exist. " +
                                            "\"{1}\" was designated main instead. You can use /wmain to change it.",
                                            LogType.Warning, temp.Value, firstWorld.Name );
                                MainWorld = firstWorld;
                            }
                            // if firstWorld was also null, LoadWorldList() should try creating a new mainWorld

                        } else if( firstWorld != null ) {
                            MainWorld = firstWorld;
                        }
                    }
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Error occured while trying to load the world list.", "fCraft", ex, true );
                    return false;
                }

                if( mainWorld == null ) {
                    Logger.Log( "Server.Start: Could not load any of the specified worlds, or no worlds were specified. " +
                                "Creating default \"main\" world.", LogType.Error );
                    MainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
                }

            } else {
                Logger.Log( "Server.Start: No world list found. Creating default \"main\" world.", LogType.SystemActivity );
                MainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
            }

            // if there is no default world still, die.
            if( MainWorld == null ) {
                throw new Exception( "Could not create any worlds" );

            } else if( MainWorld.AccessSecurity.HasRestrictions ) {
                Logger.Log( "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                            "Access permission for \"{0}\" has been reset.", LogType.Warning,
                             MainWorld.Name );
                MainWorld.AccessSecurity.Reset();
            }

            return true;
        }


        static void LoadWorldListEntry( XElement el, ref World firstWorld ) {
            XAttribute temp;
            if( (temp = el.Attribute( "name" )) == null ) {
                Logger.Log( "WorldManager: World tag with no name skipped.",
                            LogType.Error );
                return;
            }

            string worldName = temp.Value;

            bool neverUnload = (el.Attribute( "noUnload" ) != null);

            World world;
            try {
                world = AddWorld( null, worldName, null, neverUnload );
            } catch( WorldOpException ex ) {
                Logger.Log( "WorldManager: Error adding world \"{0}\": {1}",
                            LogType.Error,
                            worldName, ex.Message );
                return;
            }

            if( (temp = el.Attribute( "hidden" )) != null ) {
                bool isHidden;
                if( Boolean.TryParse( temp.Value, out isHidden ) ) {
                    world.IsHidden = isHidden;
                } else {
                    Logger.Log( "WorldManager: Could not parse \"hidden\" attribute of world \"{0}\", assuming NOT hidden.",
                                LogType.Warning,
                                worldName );
                }
            }
            if( firstWorld == null ) firstWorld = world;

            if( el.Element( "accessSecurity" ) != null ) {
                world.AccessSecurity = new SecurityController( el.Element( "accessSecurity" ), true );
            }

            if( el.Element( "buildSecurity" ) != null ) {
                world.BuildSecurity = new SecurityController( el.Element( "buildSecurity" ), true );
            }

            if( (temp = el.Attribute( "backup" )) != null ) {
                TimeSpan backupInterval;
                if( !temp.Value.ToTimeSpan( out backupInterval ) ) {
                    Logger.Log( "WorldManager: Could not parse \"backup\" attribute of world \"{0}\", assuming default ({1}).",
                                LogType.Warning,
                                worldName,
                                backupInterval.ToMiniString() );
                }
                world.BackupInterval = backupInterval;
            } else {
                world.BackupInterval = World.DefaultBackupInterval;
            }

            XElement blockEl = el.Element( "blockDB" );
            if( blockEl != null ) {
                world.BlockDB.Enabled = true;
                if( (temp = blockEl.Attribute( "preload" )) != null ) {
                    bool isPreloaded;
                    if( Boolean.TryParse( temp.Value, out isPreloaded ) ) {
                        world.BlockDB.IsPreloaded = isPreloaded;
                    } else {
                        Logger.Log( "WorldManager: Could not parse BlockDB \"preload\" attribute of world \"{0}\", assuming NOT preloaded.",
                                    LogType.Warning,
                                    worldName );
                    }
                }
                if( (temp = blockEl.Attribute( "limit" )) != null ) {
                    int limit;
                    if( Int32.TryParse( temp.Value, out limit ) ) {
                        world.BlockDB.Limit = limit;
                    } else {
                        Logger.Log( "WorldManager: Could not parse BlockDB \"limit\" attribute of world \"{0}\", assuming NO limit.",
                                    LogType.Warning,
                                    worldName );
                    }
                }
                if( (temp = blockEl.Attribute( "timeLimit" )) != null ) {
                                        int timeLimitSeconds;
                    if( Int32.TryParse( temp.Value, out timeLimitSeconds ) ) {
                        world.BlockDB.TimeLimit = TimeSpan.FromSeconds( timeLimitSeconds );
                    } else {
                        Logger.Log( "WorldManager: Could not parse BlockDB \"timeLimit\" attribute of world \"{0}\", assuming NO time limit.",
                                    LogType.Warning,
                                    worldName );
                    }
                }
            }

            foreach( XElement mainedRankEl in el.Elements( "RankMainWorld" ) ) {
                Rank rank = Rank.Parse( mainedRankEl.Value );
                if( rank != null ) {
                    if( rank < world.AccessSecurity.MinRank ) {
                        world.AccessSecurity.MinRank = rank;
                        Logger.Log( "WorldManager: Lowered access MinRank of world {0} to allow it to be the main world for that rank.",
                                    LogType.Warning,
                                    rank.Name );
                    }
                    rank.MainWorld = world;
                }
            }

            CheckMapFile( world );
        }


        // Makes sure that the map file exists, is properly named, and is loadable.
        static void CheckMapFile( World world ) {
            // Check the world's map file
            string fullMapFileName = world.MapFileName;
            string fileName = Path.GetFileName( fullMapFileName );

            if( Paths.FileExists( fullMapFileName, false ) ) {
                if( !Paths.FileExists( fullMapFileName, true ) ) {
                    // Map file has wrong capitalization
                    FileInfo[] matches = Paths.FindFiles( fullMapFileName );
                    if( matches.Length == 1 ) {
                        // Try to rename the map file to match world's capitalization
                        Paths.ForceRename( matches[0].FullName, fileName );
                        if( Paths.FileExists( fullMapFileName, true ) ) {
                            Logger.Log( "WorldManager.CheckMapFile: Map file for world \"{0}\" was renamed from \"{1}\" to \"{2}\"",
                                        LogType.Warning,
                                        world.Name, matches[0].Name, fileName );
                        } else {
                            Logger.Log( "WorldManager.CheckMapFile: Failed to rename map file of \"{0}\" from \"{1}\" to \"{2}\"",
                                        LogType.Error,
                                        world.Name, matches[0].Name, fileName );
                            return;
                        }
                    } else {
                        Logger.Log( "WorldManager.CheckMapFile: More than one map file exists matching the world name \"{0}\". " +
                                    "Please check the map directory and use /wload to load the correct file.", LogType.Warning,
                                    world.Name );
                        return;
                    }
                }
                // Try loading the map header
                try {
                    MapUtility.LoadHeader( world.MapFileName );
                } catch( Exception ex ) {
                    Logger.Log( "WorldManager.CheckMapFile: Could not load map file for world \"{0}\": {1}", LogType.Warning,
                                world.Name, ex );
                }
            } else {
                Logger.Log( "WorldManager.CheckMapFile: Map file for world \"{0}\" was not found.", LogType.Warning,
                            world.Name );
            }
        }


        /// <summary> Saves the current world list to worlds.xml. Thread-safe. </summary>
        public static void SaveWorldList() {
            const string worldListTempFileName = Paths.WorldListFileName + ".tmp";
            // Save world list
            lock( WorldListLock ) {
                XDocument doc = new XDocument();
                XElement root = new XElement( "fCraftWorldList" );

                foreach( World world in WorldList ) {
                    XElement temp = new XElement( "World" );
                    temp.Add( new XAttribute( "name", world.Name ) );
                    temp.Add( world.AccessSecurity.Serialize( "accessSecurity" ) );
                    temp.Add( world.BuildSecurity.Serialize( "buildSecurity" ) );
                    if( world.NeverUnload ) {
                        temp.Add( new XAttribute( "noUnload", true ) );
                    }
                    if( world.IsHidden ) {
                        temp.Add( new XAttribute( "hidden", true ) );
                    }
                    if( world.BlockDB.Enabled ) {
                        XElement blockDB = new XElement( "blockDB" );
                        blockDB.Add( new XAttribute( "preload", world.BlockDB.IsPreloaded ) );
                        blockDB.Add( new XAttribute( "limit", world.BlockDB.Limit ) );
                        blockDB.Add( new XAttribute( "timeLimit", world.BlockDB.TimeLimit.ToTickString() ) );
                        temp.Add( blockDB );
                    }

                    World world1 = world;
                    foreach( Rank mainedRank in RankManager.Ranks.Where( r => r.MainWorld == world1 ) ) {
                        temp.Add( new XElement( "RankMainWorld", mainedRank.FullName ) );
                    }

                    root.Add( temp );
                }
                root.Add( new XAttribute( "main", MainWorld.Name ) );

                doc.Add( root );
                doc.Save( worldListTempFileName );
                Paths.MoveOrReplace( worldListTempFileName, Paths.WorldListFileName );
            }
        }

        #endregion


        #region Finding Worlds

        /// <summary> Finds a world by full name.
        /// Target world is not guaranteed to have a loaded map. </summary>
        /// <returns> World if found, or null if not found. </returns>
        public static World FindWorldExact( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            return WorldList.FirstOrDefault( w => w.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        /// <summary> Finds all worlds that match the given world name.
        /// Autocompletes. Does not raise SearchingForWorld event.
        /// Target worlds are not guaranteed to have a loaded map. </summary>
        public static World[] FindWorldsNoEvent( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            World[] worldListCache = WorldList;

            List<World> results = new List<World>();
            for( int i = 0; i < worldListCache.Length; i++ ) {
                if( worldListCache[i] != null ) {
                    if( worldListCache[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( worldListCache[i] );
                        break;
                    } else if( worldListCache[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Add( worldListCache[i] );
                    }
                }
            }
            return results.ToArray();
        }


        /// <summary> Finds all worlds that match the given name.
        /// Autocompletes. Raises SearchingForWorld event.
        /// Target worlds are not guaranteed to have a loaded map.</summary>
        /// <param name="player"> Player who is calling the query. May be null. </param>
        /// <param name="name"> Full or partial world name. </param>
        /// <returns> An array of 0 or more worlds that matched the name. </returns>
        public static World[] FindWorlds( Player player, string name ) {
            World[] matches = FindWorldsNoEvent( name );
            var h = SearchingForWorld;
            if( h != null ) {
                SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, name, matches.ToList() );
                h( null, e );
                matches = e.Matches.ToArray();
            }
            return matches;
        }


        /// <summary> Tries to find a single world by full or partial name.
        /// Returns null if zero or multiple worlds matched. </summary>
        /// <param name="player"> Player who will receive messages regarding zero or multiple matches. </param>
        /// <param name="worldName"> Full or partial world name. </param>
        public static World FindWorldOrPrintMatches( [NotNull] Player player, [NotNull] string worldName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( worldName == null ) throw new ArgumentNullException( "worldName" );

            World[] matches = FindWorlds( player, worldName );

            if( matches.Length == 0 ) {
                player.MessageNoWorld( worldName );
                return null;
            }

            if( matches.Length > 1 ) {
                player.MessageManyMatches( "world", matches );
                return null;
            }

            return matches[0];
        }

        #endregion


        public static World AddWorld( [CanBeNull] Player player, [NotNull] string name, [CanBeNull] Map map, bool neverUnload ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            if( !World.IsValidName( name ) ) {
                throw new WorldOpException( name, WorldOpExceptionCode.InvalidWorldName );
            }

            lock( WorldListLock ) {
                if( Worlds.ContainsKey( name.ToLower() ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.DuplicateWorldName );
                }

                if( RaiseWorldCreatingEvent( player, name, map ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.PluginDenied );
                }

                World newWorld = new World( name ) {
                    Map = map
                };

                if( neverUnload ) {
                    newWorld.NeverUnload = true;
                }

                if( map != null ) {
                    newWorld.SaveMap();
                }

                Worlds.Add( name.ToLower(), newWorld );
                UpdateWorldList();

                RaiseWorldCreatedEvent( player, newWorld );

                return newWorld;
            }
        }


        /// <summary> Changes the name of the given world. </summary>
        public static void RenameWorld( [NotNull] World world, [NotNull] string newName, bool moveMapFile ) {
            if( newName == null ) throw new ArgumentNullException( "newName" );
            if( world == null ) throw new ArgumentNullException( "world" );

            if( !World.IsValidName( newName ) ) {
                throw new WorldOpException( newName, WorldOpExceptionCode.InvalidWorldName );
            }

            lock( world.WorldLock ) {
                string oldName = world.Name;
                if( oldName == newName ) {
                    throw new WorldOpException( world.Name, WorldOpExceptionCode.NoChangeNeeded );
                }

                lock( WorldListLock ) {
                    World newWorld = FindWorldExact( newName );
                    if( newWorld != null && newWorld != world ) {
                        throw new WorldOpException( newName, WorldOpExceptionCode.DuplicateWorldName );
                    }

                    Worlds.Remove( world.Name.ToLower() );
                    world.Name = newName;
                    Worlds.Add( newName.ToLower(), world );
                    UpdateWorldList();

                    if( moveMapFile ) {
                        string oldMapFile = Path.Combine( Paths.MapPath, oldName + ".fcm" );
                        string newMapFile = newName + ".fcm";
                        if( File.Exists( oldMapFile ) ) {
                            try {
                                Paths.ForceRename( oldMapFile, newMapFile );
                            } catch( Exception ex ) {
                                throw new WorldOpException( world.Name,
                                                            WorldOpExceptionCode.MapMoveError,
                                                            ex );
                            }
                        }

                        lock( world.BlockDB.SyncRoot ) {
                            string oldBlockDBFile = Path.Combine( Paths.BlockDBPath, oldName + ".fbdb" );
                            string newBockDBFile = newName + ".fbdb";
                            if( File.Exists( oldBlockDBFile ) ) {
                                try {
                                    Paths.ForceRename( oldBlockDBFile, newBockDBFile );
                                } catch( Exception ex ) {
                                    throw new WorldOpException( world.Name,
                                                                WorldOpExceptionCode.MapMoveError,
                                                                ex );
                                }
                            }
                        }
                    }
                }
            }
        }


        internal static void ReplaceWorld( [NotNull] World oldWorld, [NotNull] World newWorld ) {
            if( oldWorld == null ) throw new ArgumentNullException( "oldWorld" );
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );

            lock( WorldListLock ) {
                if( oldWorld == newWorld ) {
                    throw new WorldOpException( oldWorld.Name, WorldOpExceptionCode.NoChangeNeeded );
                }

                if( !Worlds.ContainsValue( oldWorld ) ) {
                    throw new WorldOpException( oldWorld.Name, WorldOpExceptionCode.WorldNotFound );
                }

                if( Worlds.ContainsValue( newWorld ) ) {
                    throw new InvalidOperationException( "New world already exists on the list." );
                }

                // cycle load/unload on the new world to save it under the new name
                newWorld.Name = oldWorld.Name;
                if( newWorld.NeverUnload ) {
                    newWorld.SaveMap();
                } else {
                    newWorld.UnloadMap( false );
                }

                Worlds[oldWorld.Name.ToLower()] = newWorld;
                oldWorld.Map = null;

                // change the main world, if needed
                if( oldWorld == MainWorld ) {
                    MainWorld = newWorld;
                }

                UpdateWorldList();
            }
        }


        public static void RemoveWorld( [NotNull] World worldToDelete ) {
            if( worldToDelete == null ) throw new ArgumentNullException( "worldToDelete" );

            lock( WorldListLock ) {
                if( worldToDelete == MainWorld ) {
                    throw new WorldOpException( worldToDelete.Name, WorldOpExceptionCode.CannotDoThatToMainWorld );
                }

                Player[] worldPlayerList = worldToDelete.Players;
                worldToDelete.Players.Message( "&SYou have been moved to the main world." );
                foreach( Player player in worldPlayerList ) {
                    player.JoinWorld( MainWorld, WorldChangeReason.WorldRemoved );
                }

                try {
                    worldToDelete.BlockDB.Clear();
                } catch( Exception ex ) {
                    Logger.Log( "WorldManager.RemoveWorld: Could not delete BlockDB file: {0}", LogType.Error, ex );
                }

                Worlds.Remove( worldToDelete.Name.ToLower() );
                UpdateWorldList();
            }
        }


        public static int CountLoadedWorlds() {
            return WorldList.Count( world => world.IsLoaded );
        }


        public static int CountLoadedWorlds( Player observer ) {
            return ListLoadedWorlds( observer ).Count();
        }


        public static IEnumerable<World> ListLoadedWorlds() {
            return WorldList.Where( world => world.IsLoaded );
        }


        public static IEnumerable<World> ListLoadedWorlds( Player observer ) {
            return WorldList.Where( w => w.Players.Any( observer.CanSee ) );
        }


        public static void UpdateWorldList() {
            lock( WorldListLock ) {
                WorldList = Worlds.Values.ToArray();
            }
        }


        public static string FindMapFile( Player player, string fileName ) {
            // Check if path contains missing drives or invalid characters
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid filename or path." );
                return null;
            }

            // Look for the file
            string sourceFullFileName = Path.Combine( Paths.MapPath, fileName );
            if( !File.Exists( sourceFullFileName ) && !Directory.Exists( sourceFullFileName ) ) {

                if( File.Exists( sourceFullFileName + ".fcm" ) ) {
                    // Try with extension added
                    sourceFullFileName += ".fcm";

                } else if( MonoCompat.IsCaseSensitive ) {
                    try {
                        // If we're on a case-sensitive OS, try case-insensitive search
                        FileInfo[] candidates = Paths.FindFiles( sourceFullFileName + ".fcm" );
                        if( candidates.Length == 0 ) {
                            candidates = Paths.FindFiles( sourceFullFileName );
                        }

                        if( candidates.Length == 0 ) {
                            player.Message( "File/directory not found: {0}", fileName );

                        } else if( candidates.Length == 1 ) {
                            player.Message( "Filenames are case-sensitive! Did you mean to load \"{0}\"?", candidates[0].Name );

                        } else {
                            player.Message( "Filenames are case-sensitive! Did you mean to load one of these: {0}",
                                            String.Join( ", ", candidates.Select( c => c.Name ).ToArray() ) );
                        }
                    } catch( DirectoryNotFoundException ex ) {
                        player.Message( ex.Message );
                    }
                    return null;

                } else {
                    // Nothing found!
                    player.Message( "File/directory not found: {0}", fileName );
                    return null;
                }
            }

            // Make sure that the given file is within the map directory
            if( !Paths.Contains( Paths.MapPath, sourceFullFileName ) ) {
                player.MessageUnsafePath();
                return null;
            }

            return sourceFullFileName;
        }


        #region Events

        /// <summary> Occurs when the main world is being changed (cancellable). </summary>
        public static event EventHandler<MainWorldChangingEventArgs> MainWorldChanging;


        /// <summary> Occurs after the main world has been changed. </summary>
        public static event EventHandler<MainWorldChangedEventArgs> MainWorldChanged;


        /// <summary> Occurs when a player is searching for worlds (with autocompletion).
        /// The list of worlds in the search results may be replaced. </summary>
        public static event EventHandler<SearchingForWorldEventArgs> SearchingForWorld;


        /// <summary> Occurs before a new world is created/added (cancellable). </summary>
        public static event EventHandler<WorldCreatingEventArgs> WorldCreating;


        /// <summary> Occurs after a new world is created/added. </summary>
        public static event EventHandler<WorldCreatedEventArgs> WorldCreated;


        static bool RaiseMainWorldChangingEvent( World oldWorld, World newWorld ) {
            var h = MainWorldChanging;
            if( h == null ) return false;
            var e = new MainWorldChangingEventArgs( oldWorld, newWorld );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseMainWorldChangedEvent( World oldWorld, World newWorld ) {
            var h = MainWorldChanged;
            if( h != null ) h( null, new MainWorldChangedEventArgs( oldWorld, newWorld ) );
        }

        static bool RaiseWorldCreatingEvent( Player player, string worldName, Map map ) {
            var h = WorldCreating;
            if( h == null ) return false;
            var e = new WorldCreatingEventArgs( player, worldName, map );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseWorldCreatedEvent( Player player, World world ) {
            var h = WorldCreated;
            if( h != null ) h( null, new WorldCreatedEventArgs( player, world ) );
        }

        #endregion
    }
}


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


    public sealed class SearchingForWorldEventArgs : EventArgs {
        internal SearchingForWorldEventArgs( Player player, string searchTerm, List<World> matches ) {
            Player = player;
            SearchTerm = searchTerm;
            Matches = matches;
        }
        public Player Player { get; private set; }
        public string SearchTerm { get; private set; }
        public List<World> Matches { get; set; }
    }

}