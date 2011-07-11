// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
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
            if( File.Exists( Paths.WorldListFileName ) ) {
                try {
                    LoadWorldListXml();
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Error occured while trying to load the world list.", "fCraft", ex, true );
                    return false;
                }

                if( MainWorld == null ) {
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


        static void LoadWorldListXml() {
            XDocument doc = XDocument.Load( Paths.WorldListFileName );
            XElement root = doc.Root;
            World firstWorld = null;
            XAttribute temp;

            foreach( XElement el in root.Elements( "World" ) ) {
                try {
                    if( (temp = el.Attribute( "name" )) == null ) {
                        Logger.Log( "WorldManager: World tag with no name skipped.", LogType.Error );
                        continue;
                    }

                    string worldName = temp.Value;
                    if( !World.IsValidName( worldName ) ) {
                        Logger.Log( "WorldManager: Invalid world name skipped: \"{0}\"", LogType.Error, worldName );
                        continue;
                    }

                    if( Worlds.ContainsKey( worldName.ToLower() ) ) {
                        Logger.Log( "WorldManager: Duplicate world name ignored: \"{0}\"", LogType.Error, worldName );
                        continue;
                    }

                    bool neverUnload = (el.Attribute( "noUnload" ) != null);

                    World world;
                    try {
                        world = AddWorld( null, worldName, null, neverUnload );
                    } catch( WorldOpException ex ) {
                        Logger.Log( "WorldManager: Error adding world \"{0}\": {1}", LogType.Error, worldName, ex.Message );
                        continue;
                    }

                    if( (temp = el.Attribute( "hidden" )) != null ) {
                        if( !Boolean.TryParse( temp.Value, out world.IsHidden ) ) {
                            Logger.Log( "WorldManager: Could not parse \"hidden\" attribute of world \"{0}\", assuming NOT hidden.",
                                        LogType.Warning, worldName );
                            world.IsHidden = false;
                        }
                    }
                    if( firstWorld == null ) firstWorld = world;

                    if( el.Element( "accessSecurity" ) != null ) {
                        world.AccessSecurity = new SecurityController( el.Element( "accessSecurity" ) );
                    }

                    if( el.Element( "buildSecurity" ) != null ) {
                        world.BuildSecurity = new SecurityController( el.Element( "buildSecurity" ) );
                    }

                    CheckMapFile( world );

                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "An error occured while trying to parse one of the entries on the world list",
                                              "fCraft", ex, false );
                }
            }

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


        // Make sure that the map file exists, is properly named, and is loadable.
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
                            Logger.Log( "WorldManager.CheckMapFile: Map file for world \"{0}\" was renamed from \"{1}\" to \"{2}\"", LogType.Warning,
                                        world.Name, matches[0].Name, fileName );
                        } else {
                            Logger.Log( "WorldManager.CheckMapFile: Failed to rename map file of \"{0}\" from \"{1}\" to \"{2}\"", LogType.Error,
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


        public static void SaveWorldList() {
            const string worldListTempFileName = Paths.WorldListFileName + ".tmp";
            // Save world list
            try {
                lock( WorldListLock ) {
                    XDocument doc = new XDocument();
                    XElement root = new XElement( "fCraftWorldList" );
                    XElement temp;

                    foreach( World world in WorldList ) {
                        temp = new XElement( "World" );
                        temp.Add( new XAttribute( "name", world.Name ) );
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
                }
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

            var h = SearchingForWorld;
            World[] matches = FindWorlds( worldName );
            if( h != null ) {
                SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, worldName, new List<World>(matches), false );
                h( null, e );
                matches = e.Matches.ToArray();
            }

            if( matches.Length == 0 ) {
                player.MessageNoWorld( worldName );
                return null;

            } else if( matches.Length > 1 ) {
                player.MessageManyMatches( "world", matches );
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

                World newWorld = new World( name );

                newWorld.Map = map;
                newWorld.NeverUnload = neverUnload;

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
                    player.JoinWorld( MainWorld );
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