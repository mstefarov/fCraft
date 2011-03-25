// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using fCraft.Events;
using fCraft.MapConversion;
using ThreadState = System.Threading.ThreadState;

namespace fCraft {
    public static partial class Server {

        public static DateTime ServerStart;

        public static int MaxUploadSpeed,   // set by Config.ApplyConfig
                          PacketsPerSecond; // used when there are no players in a world

        public const int MaxSessionPacketsPerTick = 128, // used when there are no players in a world
                         MaxBlockUpdatesPerTick = 100000; // used when there are no players in a world
        internal static float TicksPerSecond;


        // networking
        static TcpListener listener;
        public static IPAddress IP;

        const int MaxPortAttempts = 20;
        public static int Port;

        public static string Url;


        #region Command-line args

        static readonly Dictionary<ArgKey, string> Args = new Dictionary<ArgKey, string>();
        public static string GetArg( ArgKey key ) {
            if( Args.ContainsKey( key ) ) {
                return Args[key];
            } else {
                return null;
            }
        }

        public static bool HasArg( ArgKey key ) {
            return Args.ContainsKey( key );
        }


        public static string GetArgString() {
            return Args.Aggregate( new StringBuilder(),
                                   ( sb, pair ) => sb.AppendFormat( " {0}={1}", pair.Key, pair.Value ) ).ToString();
        }


        public static string[] GetArgList() {
            return Args.Select( pair => (pair.Key + "=" + pair.Value) ).ToArray();
        }

        #endregion


        #region Initialization

        /// <summary>
        /// Reads command-line switches and sets up paths and logging.
        /// This should be called before any other library function.
        /// Note to frontend devs: Subscribe to log-related events before calling this.
        /// </summary>
        /// <param name="rawArgs">string arguments passed to the frontend (if any)</param>
        public static void InitLibrary( string[] rawArgs ) {

            // try to parse arguments
            foreach( string arg in rawArgs ) {
                if( !arg.StartsWith( "--" ) || !arg.Contains( '=' ) ) continue;
                string argKeyName = arg.Substring( 2, arg.IndexOf( '=' ) - 2 ).ToLower().Trim();
                string argValue = arg.Substring( arg.IndexOf( '=' ) + 1 ).Trim();
                try {
                    ArgKey tryKey = (ArgKey)Enum.Parse( typeof( ArgKey ), argKeyName, true );
                    Args.Add( tryKey, argValue );
                } catch( ArgumentException ) {
                    Console.Error.WriteLine( "Unknown argument: {0}", arg );
                }
#if DEBUG
                Console.WriteLine( "{0} = {1}", argKeyName, argValue );
#endif
            }


            // before we do anything, set path to the default location
            Directory.SetCurrentDirectory( Paths.WorkingPath );

            // set custom working path (if specified)
            if( HasArg( ArgKey.Path ) && Paths.TestDirectory( "WorkingPath", GetArg( ArgKey.Path ), true ) ) {
                Paths.WorkingPath = Path.GetFullPath( GetArg( ArgKey.Path ) );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else if( Paths.TestDirectory( "WorkingPath", Paths.WorkingPathDefault, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( Paths.WorkingPathDefault );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else {
                throw new Exception( "Could not set the working path." );
            }


            // set log path
            if( HasArg( ArgKey.LogPath ) && Paths.TestDirectory( "LogPath", GetArg( ArgKey.LogPath ), true ) ) {
                Paths.LogPath = Path.GetFullPath( GetArg( ArgKey.LogPath ) );
            } else if( Paths.TestDirectory( "LogPath", Paths.LogPathDefault, true ) ) {
                Paths.LogPath = Path.GetFullPath( Paths.LogPathDefault );
            } else {
                throw new Exception( "Could not set the log path." );
            }


            // set map path
            if( HasArg( ArgKey.MapPath ) && Paths.TestDirectory( "MapPath", GetArg( ArgKey.MapPath ), true ) ) {
                Paths.MapPath = Path.GetFullPath( GetArg( ArgKey.MapPath ) );
                Paths.IgnoreMapPathConfigKey = true;
            } else if( Paths.TestDirectory( "MapPath", Paths.MapPathDefault, true ) ) {
                Paths.MapPath = Path.GetFullPath( Paths.MapPathDefault );
            } else {
                throw new Exception( "Could not set the map path." );
            }


            // set config path
            Paths.ConfigFileName = Paths.ConfigFileNameDefault;
            if( HasArg( ArgKey.Config ) ) {
                string fileName = GetArg( ArgKey.Config );
                try {
                    if( File.Exists( fileName ) ) {
                        using( File.OpenWrite( fileName ) ) { }
                    } else {
                        using( File.Create( fileName ) ) { }
                    }
                    FileInfo info = new FileInfo( fileName );
                    Paths.ConfigFileName = info.FullName;

                } catch( Exception ex ) {
                    if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                        Logger.Log( "Specified config path is invalid or incorrectly formatted ({0}: {1}).", LogType.Error,
                                    ex.GetType().ToString(), ex.Message );
                    } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                        Logger.Log( "Cannot create config file, check permissions ({0}: {1}).", LogType.Error,
                                    ex.GetType().ToString(), ex.Message );
                    } else if( ex is DirectoryNotFoundException ) {
                        Logger.Log( "Cannot create config file: directory/drive/volume does not exist or is not mounted ({0}).", LogType.Error,
                                    ex.Message );
                    } else if( ex is IOException ) {
                        Logger.Log( "Cannot write to specified directory ({0}: {1}).", LogType.Error,
                                    ex.GetType().ToString(), ex.Message );
                    } else {
                        throw;
                    }
                }
            }

#if DEBUG_EVENTS
            Logger.PrepareEventTracing();
#endif

            Logger.Log( "Working directory: {0}", LogType.Debug, Directory.GetCurrentDirectory() );
            Logger.Log( "Log path: {0}", LogType.Debug, Path.GetFullPath( Paths.LogPath ) );
            Logger.Log( "Map path: {0}", LogType.Debug, Path.GetFullPath( Paths.MapPath ) );
            Logger.Log( "Config path: {0}", LogType.Debug, Path.GetFullPath( Paths.ConfigFileName ) );
        }


        public static bool InitServer() {
            RaiseInitializingEvent( Args );

            // warnings/disclaimers
            if( Updater.IsDev ) {
                Logger.Log( "You are using an unreleased developer version of fCraft. " +
                            "Do not use this version unless are are ready to deal with bugs and potential data loss. " +
                            "Consider using the lastest stable version instead, available from www.fcraft.net",
                            LogType.Warning );
            }
            if( Updater.IsBroken ) {
                Logger.Log( "This build has been marked as BROKEN. " +
                            "Do not use except for debugging purposes. " +
                            "Latest non-broken build is {0}.", LogType.Warning,
                            Updater.LatestStable );
            }


#if DEBUG
            Config.RunSelfTest();
#endif

            // try to load the config
            if( !Config.Load( false, false ) ) return false;
            Config.ApplyConfig();
            Salt = GenerateSalt();

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            // prepare the list of commands
            CommandList.Init();

            // Init IRC
            IRC.Init();

            if( ConfigKey.AutoRankEnabled.GetBool() ) {
                AutoRank.Init();
            }

            if( OnInit != null ) OnInit(); // LEGACY

            RaiseEvent( Initialized );

            return true;
        }


        public static bool StartServer() {
            ServerStart = DateTime.Now;

            RaiseEvent( Starting );

            if( CheckForFCraftProcesses() ) {
                Logger.Log( "Please close all other fCraft processes (fCraftUI, fCraftConsole, or ConfigTool) " +
                            "that are started from the same directory.", LogType.Warning );
            }

            Player.Console = new Player( null, ConfigKey.ConsoleName.GetString() );


            // try to load the world list
            if( !LoadWorldList() ) return false;
            SaveWorldList();

            // open the port
            bool portFound = false;
            int attempts = 0;
            Port = ConfigKey.Port.GetInt();
            IP = IPAddress.Parse( ConfigKey.IP.GetString() );

            do {
                try {
                    listener = new TcpListener( IP, Port );
                    listener.Start();
                    portFound = true;

                } catch( Exception ex ) {
                    // if the port is unavailable, try next one
                    Logger.Log( "Could not start listening on port {0}, trying next port. ({1})", LogType.Error,
                                Port, ex.Message );
                    Port++;
                    attempts++;
                }
            } while( !portFound && attempts < MaxPortAttempts );

            // if the port still cannot be opened after [maxPortAttempts] attemps, die.
            if( !portFound ) {
                Logger.LogAndReportCrash( "Could not start listening on any IP/port. Giving up after " + MaxPortAttempts + " tries.",
                                          "fCraft", null, true );
                return false;
            }

            IP = ((IPEndPoint)listener.LocalEndpoint).Address;

            if( IP.ToString() != IPAddress.Any.ToString() ) {
                Logger.Log( "Server.Run: now accepting connections at {0}:{1}.", LogType.SystemActivity,
                            IP, Port );
            } else {
                Logger.Log( "Server.Run: now accepting connections at port {0}.", LogType.SystemActivity,
                            Port );
            }

            // list loaded worlds
            StringBuilder line = new StringBuilder( "All available worlds: " );
            bool firstPrintedWorld = true;
            UpdateWorldList();
            foreach( string worldName in Worlds.Keys ) {
                if( !firstPrintedWorld ) {
                    line.Append( ", " );
                }
                line.Append( worldName );
                firstPrintedWorld = false;
            }
            Logger.Log( line.ToString(), LogType.SystemActivity );

            Logger.Log( "Main world: {0}; default rank: {1}", LogType.SystemActivity,
                        MainWorld.Name, RankList.DefaultRank.Name );

            // Check for incoming connections (every 250ms)
            Scheduler.AddTask( CheckConnections ).RunForever( CheckConnectionsInterval );

            // Check for idles (every 30s)
            Scheduler.AddTask( CheckIdles ).RunForever( CheckIdlesInterval );

            // Monitor CPU usage (every 30s)
            Scheduler.AddTask( MonitorProcessorUsage ).RunForever( MonitorProcessorUsageInterval );

            // Save PlayerDB in the background (every 60s)
            Scheduler.AddBackgroundTask( PlayerDB.SaveTask ).RunForever( PlayerDB.SaveInterval, TimeSpan.FromSeconds( 15 ) );

            // Announcements
            if( ConfigKey.AnnouncementInterval.GetInt() > 0 ) {
                Scheduler.AddTask( ShowRandomAnnouncement ).RunForever( TimeSpan.FromMinutes( ConfigKey.AnnouncementInterval.GetInt() ) );
            }

            // garbage collection
            Scheduler.AddTask( DoGC ).RunForever( GCInterval, TimeSpan.FromSeconds( 45 ) );

            // Write out initial (empty) playerlist cache
            UpdatePlayerList();

            // start the main loop - server is now connectible
            Scheduler.Start();

            Heartbeat.Start();

            if( ConfigKey.IRCBotEnabled.GetBool() ) IRC.Start();

            // fire OnStart event
            if( OnStart != null ) OnStart();

            RaiseEvent( Started );
            return true;
        }

        #endregion


        #region Shutdown

        public static bool IsShuttingDown;

        // shuts down the server and aborts threads
        // NOTE: Do not call from any of the usual threads (main, heartbeat, tasks).
        // Call from UI thread or a new separate thread only.
        public static void ShutdownNow( ShutdownParams shutdownParams ) {
            if( IsShuttingDown ) return; // to avoid starting shutdown twice
            IsShuttingDown = true;
#if DEBUG
#else
            try {
#endif
                RaiseShutdownBeganEvent( shutdownParams );
                if( OnShutdownBegin != null ) OnShutdownBegin();

                Scheduler.BeginShutdown();

                Logger.Log( "Server shutting down ({0})", LogType.SystemActivity,
                            shutdownParams.ReasonString );

                // kick all players
                if( PlayerList != null ) {
                    Player[] pListCached = PlayerList;
                    foreach( Player player in pListCached ) {
                        // NOTE: kick packet delivery here is not currently guaranteed
                        player.Session.Kick( "Server shutting down (" + shutdownParams.ReasonString + Color.White + ")", LeaveReason.ServerShutdown );
                    }
                }

                // increase the chances of kick packets being delivered
                if( PlayerList != null && PlayerList.Length > 0 ) {
                    Thread.Sleep( 1000 );
                }

                // stop accepting new players
                if( listener != null ) {
                    listener.Stop();
                    listener = null;
                }

                // kill IRC bot
                IRC.Disconnect();

                lock( WorldListLock ) {
                    // unload all worlds (includes saving)
                    foreach( World world in Worlds.Values ) {
                        world.Shutdown();
                    }
                }

                Scheduler.EndShutdown();

                if( PlayerDB.IsLoaded ) PlayerDB.Save();
                if( IPBanList.IsLoaded ) IPBanList.Save();

                if( OnShutdownEnd != null ) OnShutdownEnd();
                RaiseShutdownEndedEvent( shutdownParams );
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error in Server.Shutdown", "fCraft", ex, true );
            }
#endif
        }

        static readonly AutoResetEvent ShutdownWaiter = new AutoResetEvent( false );

        static Thread shutdownThread;
        public static void Shutdown( ShutdownParams shutdownParams, bool waitForShutdown ) {
            if( !CancelShutdown() ) return;
            shutdownThread = new Thread( ShutdownThread );
            shutdownThread.Start( shutdownParams );
            if( waitForShutdown ) {
                ShutdownWaiter.WaitOne();
            }
        }

        public static bool CancelShutdown() {
            if( shutdownThread != null ) {
                if( IsShuttingDown || shutdownThread.ThreadState != ThreadState.WaitSleepJoin ) {
                    return false;
                }
                shutdownThread.Abort();
                shutdownThread = null;
            }
            return true;
        }


        static void ShutdownThread( object obj ) {
            ShutdownParams param = (ShutdownParams)obj;
            Thread.Sleep( param.Delay * 1000 );
            ShutdownNow( param );
            ShutdownWaiter.Set();
            if( param.Restart ) {
                string binaryFile = Assembly.GetEntryAssembly().Location;
                switch( Environment.OSVersion.Platform ) {
                    case PlatformID.MacOSX:
                    case PlatformID.Unix:
                        Process.Start( "mono", binaryFile + GetArgString() + " &" );
                        break;
                    default:
                        Process.Start( binaryFile, GetArgString() );
                        break;
                }
            }
            if( param.KillProcess ) {
                Process.GetCurrentProcess().Kill();
            }
        }

        #endregion


        #region Worlds

        public static World[] WorldList { get; private set; }
        static readonly SortedDictionary<string, World> Worlds = new SortedDictionary<string, World>();
        internal static readonly object WorldListLock = new object();
        public const string WorldListFileName = "worlds.xml";

        public static bool SetMainWorld( this World newWorld ) {
            if( RaiseMainWorldChangingEvent( MainWorld, newWorld ) ) return false;
            World oldWorld;
            lock( WorldListLock ) {
                lock( newWorld.MapLock ) {
                    newWorld.NeverUnload = true;
                    if( newWorld.Map == null ) {
                        newWorld.LoadMap();
                    }
                }
                oldWorld = MainWorld;
                oldWorld.NeverUnload = false;
                MainWorld = newWorld;
            }
            RaiseMainWorldChangedEvent( oldWorld, newWorld );
            return true;
        }


        public static World MainWorld { get; private set; }


        #region World List Saving/Loading

        static bool LoadWorldList() {
            if( File.Exists( WorldListFileName ) ) {
                try {
                    LoadWorldListXml();
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Error occured while trying to load the world list.", "fCraft", ex, true );
                    return false;
                }
            } else {
                Logger.Log( "Server.Start: No world list found. Creating default \"main\" world.", LogType.SystemActivity );
                CreateDefaultMainWorld();
            }

            if( Worlds.Count == 0 ) {
                Logger.Log( "Server.Start: Could not load any of the specified worlds, or no worlds were specified. Creating default \"main\" world.", LogType.Error );
                CreateDefaultMainWorld();
            }

            // if there is no default world still, die.
            if( MainWorld == null ) {
                Logger.LogAndReportCrash( "Could not create any worlds", "fCraft", null, true );
                return false;
            } else {
                if( MainWorld.AccessSecurity.HasRestrictions() ) {
                    Logger.Log( "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                                "Access permission for \"{0}\" has been reset.", LogType.Warning,
                                 MainWorld.Name );
                    MainWorld.AccessSecurity.Reset();
                }
                if( !MainWorld.NeverUnload ) {
                    MainWorld.NeverUnload = true;
                    MainWorld.LoadMap();
                }
            }

            return true;
        }

        static void CreateDefaultMainWorld() {
            Map map = new Map( null, 64, 64, 64 );
            MapGenerator.GenerateFlatgrass( map );
            map.ResetSpawn();
            MainWorld = AddWorld( "main", map, true );
        }


        static void LoadWorldListXml() {
            XDocument doc = XDocument.Load( WorldListFileName );
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
                    if( !Player.IsValidName( worldName ) ) {
                        Logger.Log( "Server.ParseWorldListXML: Invalid world name skipped: \"{0}\"", LogType.Error, worldName );
                        continue;
                    }

                    World world = AddWorld( worldName, null, (el.Attribute( "noUnload" ) != null) );

                    if( world == null ) {
                        Logger.Log( "Server.ParseWorldListXML: Error loading world \"{0}\"", LogType.Error, worldName );
                    } else {
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
                            world.AccessSecurity.MinRank = LoadWorldRankRestriction( world, "access", el );
                        }

                        if( el.Element( "buildSecurity" ) != null ) {
                            world.BuildSecurity = new SecurityController( el.Element( "buildSecurity" ) );
                        } else {
                            world.BuildSecurity.MinRank = LoadWorldRankRestriction( world, "build", el );
                        }
                    }


                    if( File.Exists( world.GetMapName() ) ) {
                        try {
                            Map map = MapUtility.LoadHeader( world.GetMapName() );
                            if( map == null ) {
                                throw new Exception();
                            }
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
            XAttribute temp;
            if( (temp = element.Attribute( fieldType )) == null ) {
                return RankList.LowestRank;
            }
            Rank rank;
            if( (rank = RankList.ParseRank( temp.Value )) != null ) {
                return rank;
            }
            Logger.Log( "Server.ParseWorldListXML: Could not parse the specified {0} rank for world \"{1}\": \"{2}\". No {0} limit was set.",
                        LogType.Error, fieldType, world.Name, temp.Value );
            return RankList.LowestRank;
        }


        const string WorldListTempFile = WorldListFileName + ".tmp";
        public static void SaveWorldList() {
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
                doc.Save( WorldListTempFile );
                if( File.Exists( WorldListFileName ) ) {
                    File.Replace( WorldListTempFile, WorldListFileName, null, true );
                } else {
                    File.Move( WorldListTempFile, WorldListFileName );
                }
            } catch( Exception ex ) {
                Logger.Log( "Server.SaveWorldList: An error occured while trying to save the world list: {0}", LogType.Error, ex );
            }
        }

        #endregion


        public static World AddWorld( string name, Map map, bool neverUnload ) {
            if( !Player.IsValidName( name ) ) return null;
            lock( WorldListLock ) {
                if( Worlds.ContainsKey( name ) ) return null;
                World newWorld = new World( name ) { NeverUnload = neverUnload };

                if( map != null ) {
                    // if a map is given
                    newWorld.Map = map;
                    map.World = newWorld;
                    if( !neverUnload ) {
                        newWorld.UnloadMap( false );// UnloadMap also saves the map
                    } else {
                        newWorld.SaveMap();
                    }

                } else {
                    // generate default map
                    if( neverUnload ) newWorld.LoadMap();
                }

                newWorld.UpdatePlayerList();
                newWorld.StartTasks();

                Worlds.Add( name.ToLower(), newWorld );
                UpdateWorldList();

                return newWorld;
            }
        }


        public static World FindWorldExact( string name ) {
            if( name == null ) return null;
            return WorldList.FirstOrDefault( w => w.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        public static World[] FindWorlds( string name ) {
            if( name == null ) return null;
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
            List<World> matches = new List<World>( FindWorlds( worldName ) );
            SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, worldName, matches, false );
            RaiseSearchingForWorldEvent( e );
            matches = e.Matches;

            if( matches.Count == 0 ) {
                player.NoWorldMessage( worldName );
                return null;
            } else if( matches.Count > 1 ) {
                player.ManyMatchesMessage( "world", matches.ToArray() );
                return null;
            } else {
                return matches[0];
            }
        }


        public static void RemoveWorld( this World worldToDelete ) {
            if( worldToDelete == null ) {
                throw new ArgumentNullException( "worldToDelete" );
            }

            lock( WorldListLock ) {
                if( worldToDelete == MainWorld ) {
                    throw new EnumException<WorldCmdError>( WorldCmdError.CannotDoThatToMainWorld );
                }

                Player[] worldPlayerList = worldToDelete.PlayerList;
                worldToDelete.SendToAll( "&SYou have been moved to the main world." );
                foreach( Player player in worldPlayerList ) {
                    player.Session.JoinWorld( MainWorld, null );
                }

                worldToDelete.StopTasks();
                worldToDelete.SaveMap();

                Worlds.Remove( worldToDelete.Name.ToLower() );
                UpdateWorldList();
            }
        }


        // Note: no autocompletion
        public static void RenameWorld( this World world, string newName, bool moveMapFile ) {
            if( !Player.IsValidName( newName ) ) {
                throw new EnumException<WorldCmdError>( WorldCmdError.InvalidNewWorldName );
            }
            if( world == null ) {
                throw new EnumException<WorldCmdError>( WorldCmdError.WorldNotFound );
            }

            string oldName = world.Name;
            if( oldName == newName ) {
                throw new EnumException<WorldCmdError>( WorldCmdError.NoChangeNeeded );
            }

            lock( WorldListLock ) {
                World newWorld = FindWorldExact( newName );
                if( newWorld != null && newWorld != world ) {
                    throw new EnumException<WorldCmdError>( WorldCmdError.DuplicateWorldName );
                }

                Worlds.Remove( world.Name.ToLower() );
                world.Name = newName;
                Worlds.Add( newName.ToLower(), world );
                UpdateWorldList();

                if( moveMapFile ) {
                    FileInfo oldFile = new FileInfo( Path.Combine( Paths.MapPath, oldName + ".fcm" ) );
                    FileInfo newFile = new FileInfo( Path.Combine( Paths.MapPath, newName + ".fcm" ) );
                    try {
                        if( oldFile.Exists ) {
                            if( newFile.Exists && !oldName.Equals( newName, StringComparison.OrdinalIgnoreCase ) ) {
                                File.Replace( oldFile.FullName, newFile.FullName, null, true );
                            } else {
                                File.Move( oldFile.FullName, newFile.FullName );
                            }
                        }
                    } catch( Exception ex ) {
                        throw new EnumException<WorldCmdError>( WorldCmdError.MapMoveError,
                                                                "Unexpected error moving/renaming mapfile.",
                                                                ex );
                    }
                }
            }
        }


        public static bool ReplaceWorld( string name, World newWorld ) {
            lock( WorldListLock ) {
                World oldWorld = FindWorldExact( name );
                if( oldWorld == null ) return false;

                newWorld.Name = oldWorld.Name;
                if( oldWorld == MainWorld ) {
                    MainWorld = newWorld;
                }

                // initialize the player list cache
                newWorld.UpdatePlayerList();

                // swap worlds
                Worlds[name.ToLower()] = newWorld;

                oldWorld.StopTasks();
                newWorld.StopTasks();

                Scheduler.UpdateCache();

                newWorld.StartTasks();
                UpdateWorldList();
                return true;
            }
        }


        public static int CountLoadedWorlds() {
            return WorldList.Count( world => (world.Map != null) );
        }


        public static void UpdateWorldList() {
            lock( WorldListLock ) {
                WorldList = Worlds.Values.ToArray();
            }
        }

        #endregion


        #region Messaging / Packet Sending

        // Send a low-priority packet to everyone
        // If 'except' is not null, excludes specified player
        public static void SendToAllDelayed( Packet packet, Player except ) {
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].SendDelayed( packet );
                }
            }
        }


        // Send a normal priority packet to everyone
        public static void SendToAll( Packet packet ) {
            SendToAll( packet, null );
        }


        // Send a normal priority packet to everyone
        // If 'except' is not null, excludes specified player
        public static void SendToAll( Packet packet, Player except ) {
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].Send( packet );
                }
            }
        }


        // Send a message to everyone (except a specified player)
        // Wraps String.Format() for easy formatting
        public static void SendToAllExcept( string message, Player except, params object[] formatArgs ) {
            if( formatArgs.Length > 0 ) message = String.Format( message, formatArgs );
            //if( except != Player.Console ) Logger.LogConsole( message );
            foreach( Packet p in PacketWriter.MakeWrappedMessage( "> ", message, false ) ) {
                SendToAll( p, except );
            }
        }


        // Send a message to everyone
        // Wraps String.Format() for easy formatting
        public static void SendToAll( string message, params object[] formatArgs ) {
            SendToAllExcept( message, null, formatArgs );
        }

        public static void SendToAllExceptIgnored( Player origin, string message, Player except, params object[] formatArgs ) {
            if( formatArgs.Length > 0 ) message = String.Format( message, formatArgs );
            foreach( Packet p in PacketWriter.MakeWrappedMessage( "> ", message, false ) ) {
                Player[] tempList = PlayerList;
                for( int i = 0; i < tempList.Length; i++ ) {
                    if( tempList[i] != except && !tempList[i].IsIgnoring( origin.Info ) ) {
                        tempList[i].Send( p );
                    }
                }
            }
        }


        // Sends a packet to everyone who CAN see 'source' player
        public static void SendToSeeing( Packet packet, Player source ) {
            Player[] playerListCopy = PlayerList;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }


        // Sends a string to everyone who CAN see 'source' player
        public static void SendToSeeing( string message, Player source ) {
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                SendToSeeing( packet, source );
            }
        }


        // Sends a packet to everyone who CAN'T see 'source' player
        public static void SendToBlind( Packet packet, Player source ) {
            Player[] playerListCopy = PlayerList;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && !playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }


        // Sends a string to everyone who CAN'T see 'source' player
        public static void SendToBlind( string message, Player source ) {
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                SendToBlind( packet, source );
            }
        }

        // Sends a packet to all players of a specific rank
        public static void SendToRank( Packet packet, Rank rank ) {
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i].Info.Rank == rank ) {
                    tempList[i].Send( packet );
                }
            }
        }


        // Sends a string to all players of a specific rank
        public static void SendToRank( Player origin, string message, Rank rank ) {
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                Player[] tempList = PlayerList;
                for( int i = 0; i < tempList.Length; i++ ) {
                    if( tempList[i].Info.Rank == rank && !tempList[i].IsIgnoring( origin.Info ) ) {
                        tempList[i].Send( packet );
                    }
                }
            }
        }

        #endregion


        #region Obsolete Events
        // events

        [Obsolete( "Use Server.Initializing or Server.Initialized instead" )]
        public static event SimpleEventHandler OnInit;

        [Obsolete( "Use Server.Starting or Server.Started instead" )]
        public static event SimpleEventHandler OnStart;

        [Obsolete( "Use Server.PlayerConnecting or Server.PlayerConnected instead" )]
        public static event PlayerConnectedEventHandler OnPlayerConnected;

        [Obsolete( "Use Server.PlayerDisconnected instead" )]
        public static event PlayerDisconnectedEventHandler OnPlayerDisconnected;

        [Obsolete]
        public static event PlayerKickedEventHandler OnPlayerKicked;

        [Obsolete]
        public static event PlayerRankChangedEventHandler OnRankChanged;

        [Obsolete( "Use Heartbeat.UrlChanged instead" )]
        public static event UrlChangeEventHandler OnURLChanged;

        [Obsolete( "Use Server.ShutdownBegan instead" )]
        public static event SimpleEventHandler OnShutdownBegin;

        [Obsolete( "Use Server.ShutdownEnded instead" )]
        public static event SimpleEventHandler OnShutdownEnd;

        [Obsolete]
        public static event PlayerChangedWorldEventHandler OnPlayerChangedWorld;

        [Obsolete( "Use Logger.Logged instead" )]
        public static event LogEventHandler OnLog;

        [Obsolete]
        public static event PlayerListChangedHandler OnPlayerListChanged;

        [Obsolete]
        public static event PlayerSentMessageEventHandler OnPlayerSentMessage;

        [Obsolete]
        public static event PlayerBanStatusChangedEventHandler OnPlayerBanned;

        [Obsolete]
        public static event PlayerBanStatusChangedEventHandler OnPlayerUnbanned;


        internal static void FireUrlChangeEvent( string newUrl ) {
            if( OnURLChanged != null ) OnURLChanged( newUrl );
        }

        internal static void FireLogEvent( string message, LogType type ) {
            if( OnLog != null ) OnLog( message, type );
        }

        internal static bool FirePlayerConnectedEvent( Session session ) {
            bool cancel = false;
            if( OnPlayerConnected != null ) OnPlayerConnected( session, ref cancel );
            return !cancel;
        }

        internal static bool FirePlayerRankChange( PlayerInfo target, Player player, Rank oldRank, Rank newRank, string reason ) {
            bool cancel = false;
            if( OnRankChanged != null ) OnRankChanged( target, player, oldRank, newRank, reason, ref cancel );
            return !cancel;
        }

        internal static void FireWorldChangedEvent( Player player, World oldWorld, World newWorld ) {
            if( OnPlayerChangedWorld != null ) OnPlayerChangedWorld( player, oldWorld, newWorld );
        }

        internal static void FirePlayerListChangedEvent() {
            if( OnPlayerListChanged == null ) return;
            Player[] playerListCache = PlayerList;
            string[] list = new string[playerListCache.Length];
            for( int i = 0; i < list.Length; i++ ) {
                list[i] = playerListCache[i].Info.Rank.Name + " - " + playerListCache[i].Name;
            }
            Array.Sort( list );
            OnPlayerListChanged( list );
        }

        internal static bool FireSentMessageEvent( Player player, ref string message ) {
            bool cancel = false;
            if( OnPlayerSentMessage != null ) {
                OnPlayerSentMessage( player, player.World, ref message, ref cancel );
            }
            return !cancel;
        }

        internal static void FirePlayerKickedEvent( Player player, Player kicker, string reason ) {
            if( OnPlayerKicked != null ) {
                OnPlayerKicked( player, kicker, reason );
            }
        }

        internal static void FirePlayerBannedEvent( PlayerInfo player, Player banner, string reason ) {
            if( OnPlayerBanned != null ) {
                OnPlayerBanned( player, banner, reason );
            }
        }

        internal static void FirePlayerUnbannedEvent( PlayerInfo player, Player unbanner, string reason ) {
            if( OnPlayerUnbanned != null ) {
                OnPlayerUnbanned( player, unbanner, reason );
            }
        }

        #endregion


        #region Scheduled Tasks

        // checks for incoming connections
        static readonly TimeSpan CheckConnectionsInterval = TimeSpan.FromMilliseconds( 250 );

        internal static void CheckConnections( Scheduler.Task param ) {
            if( listener.Pending() ) {
                try {
                    Session newSession = new Session( listener.AcceptTcpClient() );
                    newSession.Start();
                } catch( Exception ex ) {
                    Logger.Log( "Server.CheckConnections: Could not accept incoming connection: " + ex, LogType.Error );
                }
            }
        }


        // checks for idle players
        static readonly TimeSpan CheckIdlesInterval = TimeSpan.FromSeconds( 30 );

        static void CheckIdles( object param ) {
            Player[] tempPlayerList = PlayerList;
            foreach( Player player in tempPlayerList ) {
                if( player.Info.Rank.IdleKickTimer <= 0 ) continue;
                if( DateTime.UtcNow.Subtract( player.IdleTimer ).TotalMinutes >= player.Info.Rank.IdleKickTimer ) {
                    SendToAllExcept( "{0}&S was kicked for being idle for {1} min", player,
                                     player.GetClassyName(),
                                     player.Info.Rank.IdleKickTimer.ToString() );
                    AdminCommands.DoKick( Player.Console, player, "Idle for " + player.Info.Rank.IdleKickTimer + " minutes", true, LeaveReason.IdleKick );
                    player.ResetIdleTimer(); // to prevent kick from firing more than once
                }
            }
        }


        // collects garbage (forced collection is necessary under Mono)
        static readonly TimeSpan GCInterval = TimeSpan.FromSeconds( 60 );

        static void DoGC( object param ) {
            if( !GCRequested ) return;
            GCRequested = false;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            Logger.Log( "Server.DoGC: Collected on schedule.", LogType.Debug );
        }


        // shows announcements
        public const string AnnouncementsFile = "announcements.txt";

        static void ShowRandomAnnouncement( object param ) {
            if( !File.Exists( AnnouncementsFile ) ) return;
            string[] lines = File.ReadAllLines( AnnouncementsFile );
            if( lines.Length == 0 ) return;
            string line = lines[new Random().Next( 0, lines.Length )].Trim();
            if( line.Length > 0 ) {
                if( line.StartsWith( "&" ) ) {
                    SendToAll( "{0}", line );
                } else {
                    SendToAll( "{0}{1}", Color.Announcement, line );
                }
            }
        }


        // measures CPU usage
        static TimeSpan oldCPUTime = new TimeSpan( 0 );
        public static float CPUUsageTotal, CPUUsageLastMinute;
        const int CPUMonitorInterval = 60000; // 1 minute
        static readonly TimeSpan MonitorProcessorUsageInterval = TimeSpan.FromSeconds( 30 );

        public static void MonitorProcessorUsage( object param ) {
            TimeSpan newCPUTime = Process.GetCurrentProcess().TotalProcessorTime;
            CPUUsageLastMinute = (float)((newCPUTime - oldCPUTime).TotalMilliseconds / (Environment.ProcessorCount * CPUMonitorInterval));
            CPUUsageTotal = (float)(newCPUTime.TotalMilliseconds / (Environment.ProcessorCount * DateTime.Now.Subtract( ServerStart ).TotalMilliseconds));
            oldCPUTime = newCPUTime;
        }

        #endregion


        #region Utilities

        static bool GCRequested;
        public static void RequestGC() {
            GCRequested = true;
        }


        public static string Salt { get; private set; }


        static string GenerateSalt() {
            RandomNumberGenerator prng = RandomNumberGenerator.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while( sb.Length < 32 ) {
                prng.GetBytes( oneChar );
                if( oneChar[0] >= 33 && oneChar[0] <= 126 ) {
                    sb.Append( (char)oneChar[0] );
                }
            }
            return sb.ToString();
        }

        public static bool VerifyName( string name, string hash, string salt ) {
            while( hash.Length < 32 ) {
                hash = "0" + hash;
            }
            MD5 hasher = MD5.Create();
            StringBuilder sb = new StringBuilder( 32 );
            foreach( byte b in hasher.ComputeHash( Encoding.ASCII.GetBytes( salt + name ) ) ) {
                sb.AppendFormat( "{0:x2}", b );
            }
            return sb.ToString().Equals( hash, StringComparison.OrdinalIgnoreCase );
        }


        public static int CalculateMaxPacketsPerUpdate( World world ) {
            int packetsPerTick = (int)(PacketsPerSecond / TicksPerSecond);
            int maxPacketsPerUpdate = (int)(MaxUploadSpeed / TicksPerSecond * 128);

            int playerCount = world.PlayerList.Length;
            if( playerCount > 0 && !world.IsFlushing ) {
                maxPacketsPerUpdate /= playerCount;
                if( maxPacketsPerUpdate > packetsPerTick ) {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            } else {
                maxPacketsPerUpdate = MaxBlockUpdatesPerTick;
            }

            return maxPacketsPerUpdate;
        }

        public static bool CheckForFCraftProcesses() {
            try {
                Process[] processList = Process.GetProcesses();

                foreach( Process process in processList ) {
                    if( process.ProcessName.StartsWith( "fcraftui", StringComparison.OrdinalIgnoreCase ) ||
                        process.ProcessName.StartsWith( "configtool", StringComparison.OrdinalIgnoreCase ) ||
                        process.ProcessName.StartsWith( "fcraftconsole", StringComparison.OrdinalIgnoreCase ) ) {
                        if( process.Id != Process.GetCurrentProcess().Id ) {
                            Logger.Log( "Another fCraft process detected running: {0}", LogType.Warning, process.ProcessName );
                            return true;
                        }
                    }
                }
                return false;

            } catch( Exception ex ) {
                Logger.Log( "Server.CheckForFCraftProcesses: {0}", LogType.Debug, ex );
                return false;
            }
        }

        static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );

        public static long DateTimeToTimestamp( DateTime timestamp ) {
            return (long)(timestamp - UnixEpoch).TotalSeconds;
        }

        public static DateTime TimestampToDateTime( long timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }


        static readonly Regex RegexIP = new Regex( @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b", RegexOptions.Compiled );
        public static bool IsIP( string ipString ) {
            return RegexIP.IsMatch( ipString );
        }

        #region Extension Methods

        public static bool IsLAN( this IPAddress addr ) {
            byte[] bytes = addr.GetAddressBytes();
            return (bytes[0] == 192 && bytes[1] == 168);
        }


        public static string ToCompactString( this TimeSpan span ) {
            return String.Format( "{0}.{1:00}:{2:00}:{3:00}",
                span.Days, span.Hours, span.Minutes, span.Seconds );
        }


        public static string ToCompactString( this DateTime date ) {
            return date.ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ssK" );
        }


        public static string ToMiniString( this TimeSpan span ) {
            if( span.TotalSeconds < 60 ) {
                return String.Format( "{0}s", span.Seconds );
            } else if( span.TotalMinutes < 60 ) {
                return String.Format( "{0:0}m{1}s", span.TotalMinutes, span.Seconds );
            } else if( span.TotalHours < 48 ) {
                return String.Format( "{0:0}h{1}m", span.TotalHours, span.Minutes );
            } else if( span.TotalDays < 14 ) {
                return String.Format( "{0:0}d{1}h", span.TotalDays, span.Hours );
            } else {
                return String.Format( "{0:0}w{1:0}d", span.TotalDays / 7, span.TotalDays % 7 );
            }
        }

        public static TimeSpan ParseMiniTimespan( string text ) {
            if( text == null ) throw new ArgumentNullException( "text" );
            text = text.Trim();
            bool expectingDigit = true;
            TimeSpan result = new TimeSpan( 0 );
            int digitOffset = 0;
            for( int i = 0; i < text.Length; i++ ) {
                if( expectingDigit ) {
                    if( text[i] < '0' || text[i] > '9' ) {
                        throw new FormatException();
                    }
                    expectingDigit = false;
                } else {
                    if( text[i] >= '0' && text[i] <= '9' ) {
                        continue;
                    } else {
                        string numberString = text.Substring( digitOffset, i - digitOffset );
                        digitOffset = i + 1;
                        int number = Int32.Parse( numberString );
                        switch( Char.ToLower( text[i] ) ) {
                            case 's':
                                result += TimeSpan.FromSeconds( number );
                                break;
                            case 'm':
                                result += TimeSpan.FromMinutes( number );
                                break;
                            case 'h':
                                result += TimeSpan.FromHours( number );
                                break;
                            case 'd':
                                result += TimeSpan.FromDays( number );
                                break;
                            case 'w':
                                result += TimeSpan.FromDays( number * 7 );
                                break;
                            default:
                                throw new FormatException();
                        }
                    }
                }
            }
            return result;
        }

        #endregion

        #endregion


        #region PlayerList

        // player list
        static readonly Dictionary<int, Player> Players = new Dictionary<int, Player>();
        public static Player[] PlayerList { get; private set; }
        static readonly object PlayerListLock = new object();

        // session list
        static readonly List<Session> Sessions = new List<Session>();
        static readonly object SessionLock = new object();


        public static void KickGhostsAndRegisterSession( Session newSession ) {
            List<Session> sessionsToKick = new List<Session>();
            lock( SessionLock ) {
                foreach( Session s in Sessions ) {
                    if( s.Player.Name.Equals( newSession.Player.Name, StringComparison.OrdinalIgnoreCase ) ) {
                        sessionsToKick.Add( s );
                        s.Kick( "Connected from elsewhere!", LeaveReason.ClientReconnect );
                        Logger.Log( "Session.LoginSequence: Player {0} logged in. Ghost was kicked.", LogType.SuspiciousActivity,
                                    s.Player.Name );
                    }
                }
                Sessions.Add( newSession );
            }
            foreach( Session ses in sessionsToKick ) {
                ses.WaitForDisconnect();
            }
        }


        public static string MakePlayerConnectedMessage( Player player, bool firstTime, World world ) {
            if( firstTime ) {
                return String.Format( "&S{0} ({1}&S) connected for the first time, joined {2}",
                                      player.Name,
                                      player.Info.Rank.GetClassyName(),
                                      world.GetClassyName() );
            } else {
                return String.Format( "&S{0} ({1}&S) connected, joined {2}",
                                      player.Name,
                                      player.Info.Rank.GetClassyName(),
                                      world.GetClassyName() );
            }
        }


        // Add a newly-logged-in player to the list, and notify existing players.
        public static bool RegisterPlayer( Player player ) {
            lock( PlayerListLock ) {
                if( Players.Count >= ConfigKey.MaxPlayers.GetInt() && !player.Info.Rank.ReservedSlot ||
                    Players.Count == Config.MaxPlayersSupported ) {
                    return false;
                }
                for( int i = 0; i < Config.MaxPlayersSupported; i++ ) {
                    if( Players.ContainsKey( i ) ) continue;
                    player.ID = i;
                    Players[i] = player;
                    UpdatePlayerList();
                    player.Session.HasRegistered = true;
                    return true;
                }
                return false;
            }
        }


        // Remove player from the list, and notify remaining players
        public static void UnregisterPlayer( Player player ) {
            if( player == null ) {
                throw new ArgumentNullException( "player", "Server.UnregisterPlayer: player cannot be null." );
            }

            lock( PlayerListLock ) {
                if( !player.Session.HasRegistered ) return;

                SendToAll( PacketWriter.MakeRemoveEntity( player.ID ) );
                Logger.Log( "{0} left the server.", LogType.UserActivity,
                            player.Name );
                if( ConfigKey.ShowConnectionMessages.GetBool() ) {
                    SendToAll( "&SPlayer {0}&S left the server.", player.GetClassyName() );
                }

                World[] worldListCache = WorldList;
                // better safe than sorry: go through ALL worlds looking for leftover players
                foreach( World world in worldListCache ) {
                    world.ReleasePlayer( player );
                }
                Players.Remove( player.ID );
                UpdatePlayerList();

                if( player.Info != null ) player.Info.ProcessLogout( player );
            }
        }


        public static void UnregisterSession( Session session ) {
            lock( SessionLock ) {
                if( Sessions.Contains( session ) ) {
                    Sessions.Remove( session );
                    if( OnPlayerDisconnected != null ) OnPlayerDisconnected( session );
                }
            }
        }


        public static void UpdatePlayerList() {
            lock( PlayerListLock ) {
                Player[] newPlayerList = new Player[Players.Count];
                int i = 0;
                foreach( Player player in Players.Values ) {
                    newPlayerList[i++] = player;
                }
                PlayerList = newPlayerList.OrderBy( player => player.Name ).ToArray();
            }
            FirePlayerListChangedEvent();
        }


        /// <summary>Finds a player by name, using autocompletion.
        /// Count ALL players, including hidden ones.</summary>
        /// <returns>An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches.</returns>
        public static Player[] FindPlayers( string name ) {
            Player[] tempList = PlayerList;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] == null ) continue;
                if( tempList[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Clear();
                    results.Add( tempList[i] );
                    break;
                } else if( tempList[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Add( tempList[i] );
                }
            }
            return results.ToArray();
        }


        /// <summary>Finds a player by name, using autocompletion.
        /// Does not count hidden players.</summary>
        /// <param name="player">Player who initiated the search.
        /// Used to determine whether others are hidden or not.</param>
        /// <param name="name">Full or partial name of the search target.</param>
        /// <returns>An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches.</returns>
        public static Player[] FindPlayers( Player player, string name ) {
            List<Player> results = new List<Player>();
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && player.CanSee( tempList[i] ) ) {
                    if( tempList[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( tempList[i] );
                        break;
                    } else if( tempList[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Add( tempList[i] );
                    }
                }
            }
            return results.ToArray();
        }


        /// <summary>Find player by name using autocompletion (returns only whose whom player can see)
        /// Returns null and prints message if none or multiple players matched.</summary>
        /// <param name="player">Player who initiated the search. This is where messages are sent.</param>
        /// <param name="playerName">Full or partial name of the search target.</param>
        /// <param name="includeHidden">Whether to include hidden players in the search.</param>
        /// <returns>Player object, or null if no player was found.</returns>
        public static Player FindPlayerOrPrintMatches( Player player, string playerName, bool includeHidden ) {
            Player[] matches;
            if( includeHidden ) {
                matches = FindPlayers( playerName );
            } else {
                matches = FindPlayers( player, playerName );
            }

            if( matches.Length == 0 ) {
                player.NoPlayerMessage( playerName );
                return null;

            } else if( matches.Length > 1 ) {
                player.ManyMatchesMessage( "player", matches );
                return null;

            } else {
                return matches[0];
            }
        }


        /// <summary>Finds any player(s) online from given IP address.</summary>
        /// <returns>An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches.</returns>
        public static Player[] FindPlayers( IPAddress ip ) {
            return PlayerList.Where( t => t != null &&
                                          t.Session.GetIP().ToString() == ip.ToString() ).ToArray();
        }


        /// <summary>Finds a player by name, without any kind of autocompletion.</summary>
        /// <param name="name">Name of the player (case-insensitive).</param>
        /// <returns>Player object, or null if player was not found.</returns>
        public static Player FindPlayerExact( string name ) {
            return PlayerList.FirstOrDefault( t => t != null &&
                                                   t.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        /// <summary> Finds Player object associated with the given PlayerInfo object.</summary>
        /// <returns>Player object, or null if player is offline.</returns>
        public static Player FindPlayerExact( PlayerInfo info ) {
            if( info == null || !info.Online ) {
                return null;
            } else {
                return FindPlayerExact( info.Name );
            }
        }


        public static int GetPlayerCount( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return PlayerList.Length;
            } else {
                ;
                return PlayerList.Count( player => !player.IsHidden );
            }
        }

        #endregion
    }


    /// <summary> Describes the circumstances of server shutdown. </summary>
    public sealed class ShutdownParams {
        public ShutdownParams( ShutdownReason reason, int delay, bool killProcess, bool restart ) {
            Reason = reason;
            Delay = delay;
            KillProcess = killProcess;
            Restart = restart;
        }
        public ShutdownParams( string customReason, int delay, bool killProcess, bool restart, Player initiatedBy ) :
            this( ShutdownReason.Custom, delay, killProcess, restart ) {
            CustomReasonString = customReason;
            InitiatedBy = initiatedBy;
        }
        public ShutdownReason Reason { get; private set; }
        string CustomReasonString;
        public string ReasonString {
            get {
                if( CustomReasonString != null ) {
                    return CustomReasonString;
                } else {
                    return Reason.ToString();
                }
            }
        }
        public int Delay { get; private set; }
        public bool KillProcess { get; private set; }
        public bool Restart { get; private set; }
        public Player InitiatedBy { get; private set; }
    }

    public enum ShutdownReason {
        Unknown,
        Custom,
        FailedToInitialize,
        FailedToStart,
        RestartingForUpdate,
        Restarting,
        Crashed,
        ShuttingDown,
        ProcessClosing
    }


    /// <summary> Enumerates the recognized command-line switches/arguments. </summary>
    public enum ArgKey {
        Path,
        LogPath,
        MapPath,
        Config,
        NoRestart,
        ExitOnCrash
    };
}