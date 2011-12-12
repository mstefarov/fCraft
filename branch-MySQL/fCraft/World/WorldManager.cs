// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using fCraft.Events;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.Runtime.Serialization;

namespace fCraft {
    public static class WorldManager {
        public static World[] Worlds { get; private set; }
        static readonly SortedDictionary<string, World> WorldIndex = new SortedDictionary<string, World>();

        internal static readonly object SyncRoot = new object();


        static World mainWorld;
        /// <summary> Gets or sets the default main world.
        /// That's the world that players first join upon connecting.
        /// The map of the new main world is preloaded, and old one is unloaded, if needed. </summary>
        /// <exception cref="System.ArgumentNullException" />
        /// <exception cref="fCraft.WorldOpException" />
        [NotNull]
        public static World MainWorld {
            get { return mainWorld; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value == mainWorld ) return;
                if( !RaiseMainWorldChangingEvent( mainWorld, value ) ) {
                    throw new WorldOpException( value.Name, WorldOpExceptionCode.Cancelled );
                }
                World oldWorld;
                lock( SyncRoot ) {
                    value.Preload = true;
                    oldWorld = mainWorld;
                    if( oldWorld != null ) {
                        oldWorld.Preload = false;
                    }
                    mainWorld = value;
                }
                RaiseMainWorldChangedEvent( oldWorld, value );
            }
        }


        #region World List Saving/Loading

        const string RankMainXmlTagName = "RankMain";

        internal static bool LoadWorldList() {
            World firstWorld = null;
            World newMainWorld = null;
            Worlds = new World[0];
            if( File.Exists( Paths.WorldListFileName ) ) {
                try {
                    XDocument doc = XDocument.Load( Paths.WorldListFileName );
                    XElement root = doc.Root;
                    if( root != null ) {
                        foreach( XElement el in root.Elements( "World" ) ) {
                            World newWorld;
#if DEBUG
                            newWorld = AddWorld( el );
#else
                            try {
                                newWorld = AddWorld( el );
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "An error occured while trying to parse one of the entries on the world list",
                                                          "fCraft", ex, false );
                            }
#endif
                            if( firstWorld == null ) {
                                firstWorld = newWorld;
                            }
                        }

                        XAttribute temp;
                        if( (temp = root.Attribute( "main" )) != null ) {
                            World suggestedMainWorld = FindWorldExact( temp.Value );

                            if( suggestedMainWorld != null ) {
                                newMainWorld = suggestedMainWorld;

                            } else if( firstWorld != null ) {
                                // if specified main world does not exist, use first-defined world
                                Logger.Log( LogType.Warning,
                                            "The specified main world \"{0}\" does not exist. " +
                                            "\"{1}\" was designated main instead. You can use /WMain to change it.",
                                            temp.Value, firstWorld.Name );
                                newMainWorld = firstWorld;
                            }
                            // if firstWorld was also null, LoadWorldList() should try creating a new mainWorld

                        } else if( firstWorld != null ) {
                            newMainWorld = firstWorld;
                        }

                        foreach( XElement mainedRankEl in root.Elements( RankMainXmlTagName ) ) {
                            temp = mainedRankEl.Attribute( "rank" );
                            if( temp == null ) {
                                Logger.Log( LogType.Warning,
                                            "WorldManager.Load: Malformed RankMain element: \"rank\" attribute missing." );
                                continue;
                            }

                            string rankName = temp.Value;
                            Rank rank = Rank.Parse( rankName );
                            if( rank == null ) {
                                Logger.Log( LogType.Warning,
                                            "Rank main defined for an unrecognized rank \"{0}\"",
                                            rankName );
                                continue;
                            }

                            temp = mainedRankEl.Attribute( "world" );
                            if( temp == null ) {
                                Logger.Log( LogType.Warning,
                                            "WorldManager.Load: Malformed RankMain element: \"world\" attribute missing." );
                                continue;
                            }

                            string worldName = temp.Value;
                            World world = FindWorldExact( mainedRankEl.Value );
                            if( world == null ) {
                                Logger.Log( LogType.Warning,
                                            "Rank main for \"{0}\" set to an unrecognized world \"{1}\"",
                                            rank.Name, worldName );
                                continue;
                            }

                            if( rank < world.AccessSecurity.MinRank ) {
                                world.AccessSecurity.MinRank = rank;
                                Logger.Log( LogType.Warning,
                                            "WorldManager: Lowered access MinRank of world {0} to {1}+ to allow it to be the main world for that rank.",
                                            world.Name, rank.Name );
                            }
                            rank.MainWorld = world;
                        }
                    }
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Error occured while trying to load the world list.", "fCraft", ex, true );
                    return false;
                }

                if( newMainWorld == null ) {
                    Logger.Log( LogType.Error,
                                "Server.Start: Could not load any of the specified worlds, or no worlds were specified. " +
                                "Creating default \"main\" world." );
                    newMainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
                }

            } else {
                Logger.Log( LogType.SystemActivity,
                            "Server.Start: No world list found. Creating default \"main\" world." );
                newMainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
            }

            // if there is no default world still, die.
            if( newMainWorld == null ) {
                throw new Exception( "Could not create any worlds." );

            } else if( newMainWorld.AccessSecurity.HasRestrictions ) {
                Logger.Log( LogType.Warning,
                            "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                            "Access permission for \"{0}\" has been reset.",
                             newMainWorld.Name );
                newMainWorld.AccessSecurity.Reset();
            }

            MainWorld = newMainWorld;

            return true;
        }


        // Makes sure that the map file exists, is properly named, and is loadable.
        static void CheckMapFile( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );

            // Check the world's map file
            string fullMapFileName = Path.Combine( Paths.MapPath, worldName + ".fcm" );
            string fileName = Path.GetFileName( fullMapFileName );

            if( Paths.FileExists( fullMapFileName, false ) ) {
                if( !Paths.FileExists( fullMapFileName, true ) ) {
                    // Map file has wrong capitalization
                    FileInfo[] matches = Paths.FindFiles( fullMapFileName );
                    if( matches.Length == 1 ) {
                        // Try to rename the map file to match world's capitalization
                        // ReSharper disable AssignNullToNotNullAttribute
                        Paths.ForceRename( matches[0].FullName, fileName );
                        // ReSharper restore AssignNullToNotNullAttribute
                        if( Paths.FileExists( fullMapFileName, true ) ) {
                            Logger.Log( LogType.Warning,
                                        "WorldManager.CheckMapFile: Map file for world \"{0}\" was renamed from \"{1}\" to \"{2}\"",
                                        worldName, matches[0].Name, fileName );
                        } else {
                            Logger.Log( LogType.Error,
                                        "WorldManager.CheckMapFile: Failed to rename map file of \"{0}\" from \"{1}\" to \"{2}\"",
                                        worldName, matches[0].Name, fileName );
                            return;
                        }
                    } else {
                        Logger.Log( LogType.Warning,
                                    "WorldManager.CheckMapFile: More than one map file exists matching the world name \"{0}\". " +
                                    "Please check the map directory and use /WLoad to load the correct file.",
                                    worldName );
                        return;
                    }
                }
                // Try loading the map header
                try {
                    MapUtility.LoadHeader( fullMapFileName );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Warning,
                                "WorldManager.CheckMapFile: Could not load map file for world \"{0}\": {1}",
                                worldName, ex );
                }
            } else {
                Logger.Log( LogType.Warning,
                            "WorldManager.CheckMapFile: Map file for world \"{0}\" was not found.",
                            worldName );
            }
        }


        const string WorldListTempFileName = Paths.WorldListFileName + ".tmp";

        /// <summary> Saves the current world list to worlds.xml. Thread-safe. </summary>
        public static void SaveWorldList() {
            // Save world list
            XDocument doc = new XDocument();
            XElement root = new XElement( "fCraftWorldList" );

            lock( SyncRoot ) {
                foreach( World world in Worlds ) {
                    root.Add( world.Serialize() );
                }

                foreach( Rank mainedRank in RankManager.Ranks ) {
                    if( mainedRank.MainWorld == null || mainedRank.MainWorld == MainWorld ) continue;
                    XElement mainedRankEl = new XElement( RankMainXmlTagName );
                    mainedRankEl.Add( new XAttribute( "rank", mainedRank.FullName ) );
                    mainedRankEl.Add( new XAttribute( "world", mainedRank.MainWorld.Name ) );
                    root.Add( mainedRankEl );
                }

                root.Add( new XAttribute( "main", MainWorld.Name ) );

                doc.Add( root );
                doc.Save( WorldListTempFileName );
                Paths.MoveOrReplace( WorldListTempFileName, Paths.WorldListFileName );
            }
        }

        #endregion


        #region Finding Worlds

        /// <summary> Finds a world by full name.
        /// Target world is not guaranteed to have a loaded map. </summary>
        /// <returns> World if found, or null if not found. </returns>
        [CanBeNull]
        public static World FindWorldExact( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            return Worlds.FirstOrDefault( w => w.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        /// <summary> Finds all worlds that match the given world name.
        /// Autocompletes. Does not raise SearchingForWorld event.
        /// Target worlds are not guaranteed to have a loaded map. </summary>
        public static World[] FindWorldsNoEvent( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            World[] worldListCache = Worlds;

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
        public static World[] FindWorlds( [CanBeNull] Player player, [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            World[] matches = FindWorldsNoEvent( name );
            var handler = SearchingForWorld;
            if( handler != null ) {
                SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, name, matches.ToList() );
                handler( null, e );
                matches = e.Matches.ToArray();
            }
            return matches;
        }


        /// <summary> Tries to find a single world by full or partial name.
        /// Returns null if zero or multiple worlds matched. </summary>
        /// <param name="player"> Player who will receive messages regarding zero or multiple matches. </param>
        /// <param name="worldName"> Full or partial world name. </param>
        [CanBeNull]
        public static World FindWorldOrPrintMatches( [NotNull] Player player, [NotNull] string worldName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            if( worldName == "-" ) {
                if( player.LastUsedWorldName != null ) {
                    worldName = player.LastUsedWorldName;
                } else {
                    player.Message( "Cannot repeat world name: you haven't used any names yet." );
                    return null;
                }
            }
            player.LastUsedWorldName = worldName;

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


        [NotNull]
        static World AddWorld( [NotNull] XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            XAttribute tempAttr;
            if( (tempAttr = el.Attribute( "name" )) == null ) {
                throw new SerializationException( "World tags must contain a name" );
            }
            string name = tempAttr.Value;

            if( !World.IsValidName( name ) ) {
                throw new WorldOpException( name, WorldOpExceptionCode.InvalidWorldName );
            }

            lock( SyncRoot ) {
                if( WorldIndex.ContainsKey( name.ToLower() ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.DuplicateWorldName );
                }

                CheckMapFile( name );

                if( !RaiseWorldCreatingEvent( null, name, null, true ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.Cancelled );
                }

                World newWorld = new World( name, el );

                WorldIndex.Add( name.ToLower(), newWorld );
                UpdateWorldList();
                RaiseWorldCreatedEvent( null, newWorld, true );

                return newWorld;
            }
        }


        [NotNull]
        public static World AddWorld( [CanBeNull] Player player, [NotNull] string name, [CanBeNull] Map map, bool preload ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            if( !World.IsValidName( name ) ) {
                throw new WorldOpException( name, WorldOpExceptionCode.InvalidWorldName );
            }

            lock( SyncRoot ) {
                if( WorldIndex.ContainsKey( name.ToLower() ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.DuplicateWorldName );
                }

                if( !RaiseWorldCreatingEvent( player, name, map, false ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.Cancelled );
                }

                World newWorld = new World( name ) {
                    Map = map
                };

                if( preload ) {
                    newWorld.Preload = true;
                }

                if( map != null ) {
                    newWorld.SaveMap();
                }

                WorldIndex.Add( name.ToLower(), newWorld );
                UpdateWorldList();

                RaiseWorldCreatedEvent( player, newWorld, false );

                return newWorld;
            }
        }


        /// <summary> Changes the name of the given world. </summary>
        public static void RenameWorld( [NotNull] World world, [NotNull] string newName, bool moveMapFile, bool overwrite ) {
            if( newName == null ) throw new ArgumentNullException( "newName" );
            if( world == null ) throw new ArgumentNullException( "world" );

            if( !World.IsValidName( newName ) ) {
                throw new WorldOpException( newName, WorldOpExceptionCode.InvalidWorldName );
            }

            lock( world.SyncRoot ) {
                string oldName = world.Name;
                if( oldName == newName ) {
                    throw new WorldOpException( world.Name, WorldOpExceptionCode.NoChangeNeeded );
                }

                lock( SyncRoot ) {
                    World newWorld = FindWorldExact( newName );
                    if( newWorld != null && newWorld != world ) {
                        if( overwrite ) {
                            RemoveWorld( newWorld );
                        } else {
                            throw new WorldOpException( newName, WorldOpExceptionCode.DuplicateWorldName );
                        }
                    }

                    WorldIndex.Remove( world.Name.ToLower() );
                    world.Name = newName;
                    WorldIndex.Add( newName.ToLower(), world );
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
                            string oldBlockDBFile = Path.Combine( Paths.BlockDBDirectory, oldName + ".fbdb" );
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

            lock( SyncRoot ) {
                if( oldWorld == newWorld ) {
                    throw new WorldOpException( oldWorld.Name, WorldOpExceptionCode.NoChangeNeeded );
                }

                if( !WorldIndex.ContainsValue( oldWorld ) ) {
                    throw new WorldOpException( oldWorld.Name, WorldOpExceptionCode.WorldNotFound );
                }

                if( WorldIndex.ContainsValue( newWorld ) ) {
                    throw new InvalidOperationException( "New world already exists on the list." );
                }

                // cycle load/unload on the new world to save it under the new name
                newWorld.Name = oldWorld.Name;
                if( newWorld.Preload ) {
                    newWorld.SaveMap();
                } else {
                    newWorld.UnloadMap( false );
                }

                WorldIndex[oldWorld.Name.ToLower()] = newWorld;
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

            lock( SyncRoot ) {
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
                    Logger.Log( LogType.Error,
                                "WorldManager.RemoveWorld: Could not delete BlockDB file: {0}", ex );
                }

                WorldIndex.Remove( worldToDelete.Name.ToLower() );
                UpdateWorldList();
            }
        }


        public static int CountLoadedWorlds() {
            return Worlds.Count( world => world.IsLoaded );
        }


        public static int CountLoadedWorlds( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return ListLoadedWorlds( observer ).Count();
        }


        public static IEnumerable<World> ListLoadedWorlds() {
            return Worlds.Where( world => world.IsLoaded );
        }


        public static IEnumerable<World> ListLoadedWorlds( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return Worlds.Where( w => w.Players.Any( observer.CanSee ) );
        }


        public static void UpdateWorldList() {
            lock( SyncRoot ) {
                Worlds = WorldIndex.Values.ToArray();
            }
        }


        [CanBeNull]
        public static string FindMapFile( [NotNull] Player player, [NotNull] string fileName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
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


        static bool RaiseMainWorldChangingEvent( World oldWorld, [NotNull] World newWorld ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            var handler = MainWorldChanging;
            if( handler == null ) return true;
            var e = new MainWorldChangingEventArgs( oldWorld, newWorld );
            handler( null, e );
            return !e.Cancel;
        }

        static void RaiseMainWorldChangedEvent( [CanBeNull] World oldWorld, [NotNull] World newWorld ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            var handler = MainWorldChanged;
            if( handler != null ) handler( null, new MainWorldChangedEventArgs( oldWorld, newWorld ) );
        }

        static bool RaiseWorldCreatingEvent( [CanBeNull] Player player, [NotNull] string worldName, [CanBeNull] Map map, bool fromXml ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            var handler = WorldCreating;
            if( handler == null ) return true;
            var e = new WorldCreatingEventArgs( player, worldName, map, fromXml );
            handler( null, e );
            return !e.Cancel;
        }

        static void RaiseWorldCreatedEvent( [CanBeNull] Player player, [NotNull] World world, bool fromXml ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            var handler = WorldCreated;
            if( handler != null ) handler( null, new WorldCreatedEventArgs( player, world, fromXml ) );
        }

        #endregion
    }
}