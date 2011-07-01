using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using fCraft.Events;
using fCraft.MapConversion;

namespace fCraft {
    public static class WorldManager {

        public static World[] WorldList { get; private set; }
        static readonly SortedDictionary<string, World> Worlds = new SortedDictionary<string, World>();

        internal static readonly object WorldListLock = new object();


        #region Main World

        public static World MainWorld { get; private set; }


        public static bool SetMainWorld( this World newWorld ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            if( RaiseMainWorldChangingEvent( MainWorld, newWorld ) ) return false;
            World oldWorld;
            lock( WorldListLock ) {
                newWorld.ToggleNeverUnloadFlag( true );
                oldWorld = MainWorld;
                oldWorld.ToggleNeverUnloadFlag( false );
                MainWorld = newWorld;
            }
            RaiseMainWorldChangedEvent( oldWorld, newWorld );
            return true;
        }

        #endregion


        #region World List Saving/Loading

        internal static bool LoadWorldList() {
            if( File.Exists( Paths.WorldListFileName ) ) {
                try {
                    LoadWorldListXml();
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Error occured while trying to load the world list.", "fCraft", ex, true );
                    return false;
                }
            } else {
                Logger.Log( "Server.Start: No world list found. Creating default \"main\" world.", LogType.SystemActivity );
                MainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
            }

            if( Worlds.Count == 0 ) {
                Logger.Log( "Server.Start: Could not load any of the specified worlds, or no worlds were specified. " +
                            "Creating default \"main\" world.", LogType.Error );
                MainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
            }

            // if there is no default world still, die.
            if( MainWorld == null ) {
                throw new Exception( "Could not create any worlds" );
            } else {
                if( MainWorld.AccessSecurity.HasRestrictions ) {
                    Logger.Log( "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                                "Access permission for \"{0}\" has been reset.", LogType.Warning,
                                 MainWorld.Name );
                    MainWorld.AccessSecurity.Reset();
                }
                MainWorld.ToggleNeverUnloadFlag( true );
            }

            return true;
        }


        static void LoadWorldListXml() {
            XDocument doc = XDocument.Load( Paths.WorldListFileName );
            XElement root = doc.Root;
            World firstWorld = null;
            XAttribute temp;

            foreach( XElement el in root.Elements( "World" ) ) {
                try {
                    if( (temp = el.Attribute( "name" )) == null ) {
                        Logger.Log( "Server.ParseWorldListXML: World tag with no name skipped.", LogType.Error );
                        continue;
                    }
                    string worldName = temp.Value;
                    if( !World.IsValidName( worldName ) ) {
                        Logger.Log( "Server.ParseWorldListXML: Invalid world name skipped: \"{0}\"", LogType.Error, worldName );
                        continue;
                    }

                    if( Worlds.ContainsKey( worldName.ToLower() ) ) {
                        Logger.Log( "Server.ParseWorldListXML: Duplicate world name ignored: \"{0}\"", LogType.Error, worldName );
                        continue;
                    }

                    World world;
                    try {
                        world = AddWorld( null, worldName, null, (el.Attribute( "noUnload" ) != null) );
                    } catch( WorldOpException ex ) {
                        Logger.Log( "Server.ParseWorldListXML: Error loading world \"{0}\": {1}", LogType.Error, worldName, ex.Message );
                        continue;
                    }

                    if( (temp = el.Attribute( "hidden" )) != null ) {
                        if( !Boolean.TryParse( temp.Value, out world.IsHidden ) ) {
                            Logger.Log( "Server.ParseWorldListXML: Could not parse \"hidden\" attribute of world \"{0}\", assuming NOT hidden.",
                                        LogType.Warning, worldName );
                            world.IsHidden = false;
                        }
                    }
                    if( firstWorld == null ) firstWorld = world;

                    if( el.Element( "accessSecurity" ) != null ) {
                        world.AccessSecurity = new SecurityController( el.Element( "accessSecurity" ) );
                    } else {
                        world.AccessSecurity.MinRank = LoadWorldRankRestriction( world, "access", el ); // LEGACY
                    }

                    if( el.Element( "buildSecurity" ) != null ) {
                        world.BuildSecurity = new SecurityController( el.Element( "buildSecurity" ) );
                    } else {
                        world.BuildSecurity.MinRank = LoadWorldRankRestriction( world, "build", el ); // LEGACY
                    }

                    // Check the world's map file
                    string mapFullName = world.GetMapName();
                    string mapName = Path.GetFileName( mapFullName );

                    if( Paths.FileExists( mapFullName, false ) ) {
                        if( !Paths.FileExists( mapFullName, true ) ) {
                            // Map file has wrong capitalization
                            FileInfo[] matches = Paths.FindFiles( mapFullName );
                            if( matches.Length == 1 ) {
                                // Try to rename the map file to match world's capitalization
                                Paths.ForceRename( matches[0].FullName, mapName );
                                if( Paths.FileExists( mapFullName, true ) ) {
                                    Logger.Log( "Server.LoadWorldListXML: Map file for world \"{0}\" was renamed from \"{1}\" to \"{2}\"", LogType.Warning,
                                                world.Name, matches[0].Name, mapName );
                                } else {
                                    Logger.Log( "Server.LoadWorldListXML: Failed to rename map file of \"{0}\" from \"{1}\" to \"{2}\"", LogType.Error,
                                                world.Name, matches[0].Name, mapName );
                                    continue;
                                }
                            } else {
                                Logger.Log( "Server.LoadWorldListXML: More than one map file exists matching the world name \"{0}\". " +
                                            "Please check the map directory and use /wload to load the correct file.", LogType.Warning,
                                            world.Name );
                                continue;
                            }
                        }
                        // Try loading the map header
                        try {
                            MapUtility.LoadHeader( world.GetMapName() );
                        } catch( Exception ex ) {
                            Logger.Log( "Server.LoadWorldListXML: Could not load map file for world \"{0}\": {1}", LogType.Warning,
                                        world.Name, ex );
                        }
                    } else {
                        Logger.Log( "Server.LoadWorldListXML: Map file for world \"{0}\" was not found.", LogType.Warning,
                                    world.Name );
                    }
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "An error occured while trying to parse one of the entries on the world list",
                                              "fCraft", ex, false );
                }
            }

            if( (temp = root.Attribute( "main" )) != null ) {
                MainWorld = FindWorldExact( temp.Value );
                // if specified main world does not exist, use first-defined world
                if( MainWorld == null && firstWorld != null ) {
                    Logger.Log( "The specified main world \"{0}\" does not exist. " +
                                "\"{1}\" was designated main instead. You can use /wmain to change it.",
                                LogType.Warning, temp.Value, firstWorld.Name );
                    MainWorld = firstWorld;
                }
                // if firstWorld was also null, LoadWorldList() should try creating a new mainWorld

            } else {
                MainWorld = firstWorld;
            }
        }


        static Rank LoadWorldRankRestriction( World world, string fieldType, XElement element ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            if( element == null ) throw new ArgumentNullException( "element" );
            XAttribute temp;
            if( (temp = element.Attribute( fieldType )) == null ) {
                return RankManager.LowestRank;
            }
            Rank rank;
            if( (rank = RankManager.ParseRank( temp.Value )) != null ) {
                return rank;
            }
            Logger.Log( "Server.ParseWorldListXML: Could not parse the specified {0} rank for world \"{1}\": \"{2}\". No {0} limit was set.",
                        LogType.Error, fieldType, world.Name, temp.Value );
            return RankManager.LowestRank;
        }


        public static void SaveWorldList() {
            const string worldListTempFileName = Paths.WorldListFileName + ".tmp";
            // Save world list
            try {
                XDocument doc = new XDocument();
                XElement root = new XElement( "fCraftWorldList" );
                XElement temp;
                World[] worldListCache = WorldList;

                foreach( World world in worldListCache ) {
                    temp = new XElement( "World" );
                    temp.Add( new XAttribute( "name", world.Name ) );
                    temp.Add( new XAttribute( "access", world.AccessSecurity.MinRank ) ); // LEGACY
                    temp.Add( new XAttribute( "build", world.BuildSecurity.MinRank ) ); // LEGACY
                    temp.Add( world.AccessSecurity.Serialize( "accessSecurity" ) );
                    temp.Add( world.BuildSecurity.Serialize( "buildSecurity" ) );
                    if( world.NeverUnload ) {
                        temp.Add( new XAttribute( "noUnload", true ) );
                    }
                    if( world.IsHidden ) {
                        temp.Add( new XAttribute( "hidden", true ) );
                    }
                    root.Add( temp );
                }
                root.Add( new XAttribute( "main", MainWorld.Name ) );

                doc.Add( root );
                doc.Save( worldListTempFileName );
                Paths.MoveOrReplace( worldListTempFileName, Paths.WorldListFileName );
            } catch( Exception ex ) {
                Logger.Log( "Server.SaveWorldList: An error occured while trying to save the world list: {0}", LogType.Error, ex );
            }
        }

        #endregion


        #region Finding Worlds

        public static World FindWorldExact( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            return WorldList.FirstOrDefault( w => w.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        public static World[] FindWorlds( string name ) {
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


        public static World FindWorldOrPrintMatches( Player player, string worldName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            List<World> matches = new List<World>( FindWorlds( worldName ) );
            SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, worldName, matches, false );
            RaiseSearchingForWorldEvent( e );
            matches = e.Matches;

            if( matches.Count == 0 ) {
                player.MessageNoWorld( worldName );
                return null;

            } else if( matches.Count > 1 ) {
                player.MessageManyMatches( "world", matches.ToArray() );
                return null;

            } else {
                return matches[0];
            }
        }

        #endregion


        public static World AddWorld( Player player, string name, Map map, bool neverUnload ) {
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

                World newWorld = new World( name, neverUnload );

                // If no map is given, and no file exists: make a flatgrass
                if( map == null && neverUnload && !File.Exists( newWorld.GetMapName() ) ) {
                    Logger.Log( "No mapfile found for world \"{0}\". A blank map will be generated.", LogType.Warning,
                                name );
                    map = MapGenerator.GenerateFlatgrass( 128, 128, 64 );
                }

                // if a map is given (or was generated)
                if( map != null ) {
                    newWorld.Map = map;
                    map.World = newWorld;
                    if( neverUnload ) {
                        newWorld.StartTasks();
                        newWorld.SaveMap();
                    }else{
                        newWorld.UnloadMap( false );
                    }

                } else if( neverUnload ) {
                    newWorld.LoadMap();
                }

                Worlds.Add( name.ToLower(), newWorld );
                UpdateWorldList();

                RaiseWorldCreatedEvent( player, newWorld );

                return newWorld;
            }
        }


        /// <summary> Changes the name of the given world. </summary>
        public static void RenameWorld( World world, string newName, bool moveMapFile ) {
            if( newName == null ) throw new ArgumentNullException( "newName" );

            if( !World.IsValidName( newName ) ) {
                throw new WorldOpException( newName, WorldOpExceptionCode.InvalidWorldName );
            }

            if( world == null ) {
                throw new WorldOpException( null, WorldOpExceptionCode.WorldNotFound );
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
                        string oldFullFileName = Path.Combine( Paths.MapPath, oldName + ".fcm" );
                        string newFileName = newName + ".fcm";
                        if( File.Exists( oldFullFileName ) ) {
                            try {
                                Paths.ForceRename( oldFullFileName, newFileName );
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


        internal static void ReplaceWorld( World oldWorld, World newWorld ) {
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
                newWorld.UnloadMap( false );

                Worlds[oldWorld.Name.ToLower()] = newWorld;

                // change the main world, if needed
                if( oldWorld == MainWorld ) {
                    MainWorld = newWorld;
                }

                UpdateWorldList();
            }
        }


        public static void RemoveWorld( World worldToDelete ) {
            if( worldToDelete == null ) throw new ArgumentNullException( "worldToDelete" );

            lock( WorldListLock ) {
                if( worldToDelete == MainWorld ) {
                    throw new WorldOpException( worldToDelete.Name, WorldOpExceptionCode.CannotDoThatToMainWorld );
                }

                Player[] worldPlayerList = worldToDelete.Players;
                worldToDelete.SendToAll( "&SYou have been moved to the main world." );
                foreach( Player player in worldPlayerList ) {
                    player.Session.JoinWorld( MainWorld );
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

        internal static void RaiseSearchingForWorldEvent( SearchingForWorldEventArgs e ) {
            var h = SearchingForWorld;
            if( h != null ) h( null, e );
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
        internal SearchingForWorldEventArgs( Player player, string searchTerm, List<World> matches, bool toJoin ) {
            Player = player;
            SearchTerm = searchTerm;
            Matches = matches;
            ToJoin = toJoin;
        }
        public Player Player { get; private set; }
        public string SearchTerm { get; private set; }
        public List<World> Matches { get; set; }
        public bool ToJoin { get; private set; }
    }

}