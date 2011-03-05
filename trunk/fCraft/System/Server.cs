// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace fCraft {
    public static partial class Server {

        static string[] args = new string[0]; // saved to allow restarting with same params
        public static DateTime serverStart;
        public static bool shuttingDown;

        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          MaxSessionPacketsPerTick = 128,
                          MaxBlockUpdatesPerTick = 60000; // used when there are no players in a world
        internal static float ticksPerSecond;


        // networking
        static TcpListener listener;
        public static IPAddress IP;

        const int MaxPortAttempts = 20;
        public static int Port;

        public static string URL;


        #region Initialization

        /// <summary>
        /// Reads command-line switches and sets up paths and logging.
        /// This should be called before any other library function.
        /// </summary>
        /// <param name="_args">string arguments passed to the frontend (if any)</param>
        public static void InitLibrary( string[] _args ) {
            args = _args;

            // try to parse arguments
            Dictionary<string, string> parsedArgs = new Dictionary<string, string>();
            foreach( string arg in args ) {
                if( arg.StartsWith( "--" ) && arg.Contains( '=' ) ) {
                    string argKey = arg.Substring( 2, arg.IndexOf( '=' ) - 2 ).ToLower().Trim();
                    string argValue = arg.Substring( arg.IndexOf( '=' ) + 1 ).Trim();
                    parsedArgs.Add( argKey, argValue );
                    Console.WriteLine( "{0} = {1}", argKey, argValue );
                }
            }


            // before we do anything, set path to the default location
            Directory.SetCurrentDirectory( Paths.WorkingPath );

            // set custom working path (if specified)
            if( parsedArgs.ContainsKey( "path" ) && Paths.TestDirectory( parsedArgs["path"], true ) ) {
                Paths.WorkingPath = Path.GetFullPath( parsedArgs["path"] );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else if( Paths.TestDirectory( Paths.WorkingPathDefault, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( Paths.WorkingPathDefault );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else {
                throw new Exception( "Could not set the working path." );
            }


            // set log path
            if( parsedArgs.ContainsKey( "logpath" ) && Paths.TestDirectory( parsedArgs["logpath"], true ) ) {
                Paths.LogPath = Path.GetFullPath( parsedArgs["logpath"] );
            } else if( Paths.TestDirectory( Paths.LogPathDefault, true ) ) {
                Paths.LogPath = Path.GetFullPath( Paths.LogPathDefault );
            } else {
                throw new Exception( "Could not set the log path." );
            }


            // set map path
            if( parsedArgs.ContainsKey( "mappath" ) && Paths.TestDirectory( parsedArgs["mappath"], true ) ) {
                Paths.MapPath = Path.GetFullPath( parsedArgs["mappath"] );
                Paths.IgnoreMapPathConfigKey = true;
            } else if( Paths.TestDirectory( Paths.MapPathDefault, true ) ) {
                Paths.MapPath = Path.GetFullPath( Paths.MapPathDefault );
            } else {
                throw new Exception( "Could not set the map path." );
            }


            // set config path
            Paths.ConfigFileName = Paths.ConfigFileNameDefault;
            if( parsedArgs.ContainsKey( "config" ) ) {
                string fileName = parsedArgs["config"];
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
            Logger.FindEvents();
#endif

#if DEBUG
            Logger.Log( "Working directory: {0}", LogType.Debug, Directory.GetCurrentDirectory() );
            Logger.Log( "Log path: {0}", LogType.Debug, Path.GetFullPath( Paths.LogPath ) );
            Logger.Log( "Map path: {0}", LogType.Debug, Path.GetFullPath( Paths.MapPath ) );
            Logger.Log( "Config path: {0}", LogType.Debug, Path.GetFullPath( Paths.ConfigFileName ) );
#endif
        }


        public static bool InitServer() {
            RaiseInitializingEvent( args );

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

            if( Config.GetBool( ConfigKey.AutoRankEnabled ) ) {
                AutoRank.Init();
            }

            if( OnInit != null ) OnInit(); // LEGACY

            RaiseEvent( Initialized );

            return true;
        }


        public static bool StartServer() {
            serverStart = DateTime.Now;

            RaiseEvent( Starting );

            if( CheckForFCraftProcesses() ) {
                Logger.Log( "Please close all other fCraft processes (fCraftUI, fCraftConsole, or ConfigTool) " +
                            "that are started from the same directory.", LogType.Warning );
            }

            Player.Console = new Player( null, Config.GetString( ConfigKey.ConsoleName ) );


            // try to load the world list
            if( !LoadWorldList() ) return false;
            SaveWorldList();

            // open the port
            bool portFound = false;
            int attempts = 0;
            Port = Config.GetInt( ConfigKey.Port );

            do {
                try {
                    listener = new TcpListener( IPAddress.Parse( Config.GetString( ConfigKey.IP ) ), Port );
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
                Logger.Log( "Could not start listening after {0} tries. Giving up!", LogType.FatalError,
                            MaxPortAttempts );
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
            foreach( string worldName in worlds.Keys ) {
                if( !firstPrintedWorld ) {
                    line.Append( ", " );
                }
                line.Append( worldName );
                firstPrintedWorld = false;
            }
            Logger.Log( line.ToString(), LogType.SystemActivity );
            Logger.Log( "Main world: {0}; default rank: {1}", LogType.SystemActivity,
                        mainWorld.name, RankList.DefaultRank.Name );


            // Check for incoming connections (every 250ms)
            Scheduler.AddTask( CheckConnections ).RunForever( CheckConnectionsInterval );

            // Check for idles (every 30s)
            Scheduler.AddTask( CheckIdles ).RunForever( CheckIdlesInterval );

            // Monitor CPU usage (every 30s)
            Scheduler.AddTask( MonitorProcessorUsage ).RunForever( MonitorProcessorUsageInterval );

            // Save PlayerDB in the background (every 60s)
            Scheduler.AddBackgroundTask( PlayerDB.SaveTask ).RunForever( PlayerDB.SaveInterval, TimeSpan.FromSeconds( 15 ) );

            // Announcements
            if( Config.GetInt( ConfigKey.AnnouncementInterval ) > 0 ) {
                Scheduler.AddTask( ShowRandomAnnouncement ).RunForever( TimeSpan.FromMinutes( Config.GetInt( ConfigKey.AnnouncementInterval ) ) );
            }

            // garbage collection
            Scheduler.AddTask( DoGC ).RunForever( GCInterval, TimeSpan.FromSeconds( 45 ) );

            // Write out initial (empty) playerlist cache
            UpdatePlayerList();

            // start the main loop - server is now connectible
            Scheduler.Start();

            Heartbeat.Start();

            if( Config.GetBool( ConfigKey.IRCBotEnabled ) ) IRC.Start();

            // fire OnStart event
            if( OnStart != null ) OnStart();

            RaiseEvent( Started );
            return true;
        }

        #endregion


        #region Shutdown

        // shuts down the server and aborts threads
        // NOTE: Do not call from any of the usual threads (main, heartbeat, tasks).
        // Call from UI thread or a new separate thread only.
        public static void ShutdownNow( ShutdownParams shutdownParams ) {
            if( shuttingDown ) return;
#if DEBUG
#else
            try {
#endif
                shuttingDown = true;
                RaiseShutdownBeganEvent( shutdownParams );
                if( OnShutdownBegin != null ) OnShutdownBegin();

                Scheduler.BeginShutdown();

                Logger.Log( "Server shutting down ({0})", LogType.SystemActivity,
                            shutdownParams.Reason );

                // kick all players
                if( PlayerList != null ) {
                    Player[] pListCached = PlayerList;
                    foreach( Player player in pListCached ) {
                        // NOTE: kick packet delivery here is not currently guaranteed
                        player.session.Kick( "Server shutting down (" + shutdownParams.Reason + Color.White + ")", LeaveReason.ServerShutdown );
                    }
                }

                // increase the chances of kick packets being delivered
                Thread.Sleep( 1000 );

                Scheduler.EndShutdown();

                // stop accepting new players
                if( listener != null ) {
                    listener.Stop();
                    listener = null;
                }

                // kill IRC bot
                IRC.Disconnect();

                lock( worldListLock ) {
                    // unload all worlds (includes saving)
                    foreach( World world in worlds.Values ) {
                        world.Shutdown();
                    }
                }

                if( PlayerDB.IsLoaded ) PlayerDB.Save();
                if( IPBanList.isLoaded ) IPBanList.Save();


                if( OnShutdownEnd != null ) OnShutdownEnd();
                RaiseShutdownEndedEvent( shutdownParams );
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error in Server.Shutdown", "fCraft", ex );
            }
#endif
        }


        public static void Shutdown( string reason, int delay, bool killProcess, bool restart ) {
            new Thread( delegate( object obj ) {
                ShutdownParams param = (ShutdownParams)obj;
                Thread.Sleep( param.Delay * 1000 );
                ShutdownNow( param );
                if( param.Restart ) {
                    Process.Start( Process.GetCurrentProcess().MainModule.FileName, String.Join( " ", args ) );
                }
                if( param.KillProcess ) {
                    Process.GetCurrentProcess().Kill();
                }
            } ).Start( new ShutdownParams {
                Reason = reason,
                Delay = delay,
                KillProcess = killProcess,
                Restart = restart
            } );
        }

        #endregion


        #region Worlds

        public static SortedDictionary<string, World> worlds = new SortedDictionary<string, World>();
        public static object worldListLock = new object();
        public const string WorldListFileName = "worlds.xml";
        public static World mainWorld;

        #region World List Saving/Loading

        static bool LoadWorldList() {
            if( File.Exists( WorldListFileName ) ) {
                try {
                    LoadWorldListXML();
                } catch( Exception ex ) {
                    Logger.Log( "An error occured while trying to parse the world list: {0}", LogType.FatalError, ex );
                    return false;
                }
            } else {
                Logger.Log( "Server.Start: No world list found. Creating default \"main\" world.", LogType.SystemActivity );
                CreateDefaultMainWorld();
            }

            if( worlds.Count == 0 ) {
                Logger.Log( "Server.Start: Could not load any of the specified worlds, or no worlds were specified. Creating default \"main\" world.", LogType.Error );
                CreateDefaultMainWorld();
            }

            // if there is no default world still, die.
            if( mainWorld == null ) {
                Logger.Log( "World creation failed. Shutting down.", LogType.FatalError );
                return false;
            } else {
                if( mainWorld.accessSecurity.HasRestrictions() ) {
                    Logger.Log( "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                                "Access permission for \"{0}\" has been reset.", LogType.Warning,
                                 mainWorld.name );
                    mainWorld.accessSecurity.Reset();
                }
                if( !mainWorld.neverUnload ) {
                    mainWorld.neverUnload = true;
                    mainWorld.LoadMap();
                }
            }

            return true;
        }

        static void CreateDefaultMainWorld( ) {
            Map map = new Map( null, 64, 64, 64 );
            MapGenerator.GenerateFlatgrass( map );
            map.ResetSpawn();
            mainWorld = AddWorld( "main", map, true );
        }


        static void LoadWorldListXML() {
            XDocument doc = XDocument.Load( WorldListFileName );
            XElement root = doc.Root;
            World firstWorld = null;
            XAttribute temp;
            string worldName;

            foreach( XElement el in root.Elements( "World" ) ) {
                try {
                    if( (temp = el.Attribute( "name" )) == null ) {
                        Logger.Log( "Server.ParseWorldListXML: World tag with no name skipped.", LogType.Error );
                        continue;
                    }
                    worldName = temp.Value;
                    if( !Player.IsValidName( worldName ) ) {
                        Logger.Log( "Server.ParseWorldListXML: Invalid world name skipped: \"{0}\"", LogType.Error, worldName );
                        continue;
                    }

                    World world = AddWorld( worldName, null, (el.Attribute( "noUnload" ) != null) );

                    if( world == null ) {
                        Logger.Log( "Server.ParseWorldListXML: Error loading world \"{0}\"", LogType.Error, worldName );
                    } else {
                        if( (temp = el.Attribute( "hidden" )) != null ) {
                            if( !Boolean.TryParse( temp.Value, out world.isHidden ) ) {
                                Logger.Log( "Server.ParseWorldListXML: Could not parse \"hidden\" attribute of world \"{0}\", assuming NOT hidden.",
                                            LogType.Warning, worldName );
                                world.isHidden = false;
                            }
                        }
                        if( firstWorld == null ) firstWorld = world;
                        Logger.Log( "Server.ParseWorldListXML: Loaded world \"{0}\"", LogType.Debug, worldName );

                        if( el.Element( "accessSecurity" ) != null ) {
                            world.accessSecurity = new SecurityController( el.Element( "accessSecurity" ) );
                        } else {
                            world.accessSecurity.MinRank = LoadWorldRankRestriction( world, "access", el );
                        }

                        if( el.Element( "buildSecurity" ) != null ) {
                            world.buildSecurity = new SecurityController( el.Element( "buildSecurity" ) );
                        } else {
                            world.buildSecurity.MinRank = LoadWorldRankRestriction( world, "build", el );
                        }
                    }
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "An error occured while trying to parse one of the entries on the world list",
                                              "fCraft", ex );
                }
            }

            if( (temp = root.Attribute( "main" )) != null ) {
                mainWorld = FindWorldExact( temp.Value );
                // if specified main world does not exist, use first-defined world
                if( mainWorld == null && firstWorld != null ) {
                    Logger.Log( "The specified main world \"{0}\" does not exist. " +
                                "\"{1}\" was designated main instead. You can use /wmain to change it.",
                                LogType.Warning, temp.Value, firstWorld.name );
                    mainWorld = firstWorld;
                }
                // if firstWorld was also null, LoadWorldList() should try creating a new mainWorld

            } else {
                mainWorld = firstWorld;
            }
        }


        static Rank LoadWorldRankRestriction( World world, string fieldType, XElement element ) {
            XAttribute temp;
            if( ( temp = element.Attribute( fieldType ) ) == null ) {
                return RankList.LowestRank;
            }
            Rank rank;
            if( ( rank = RankList.ParseRank( temp.Value ) ) != null ) {
                return rank;
            }
            Logger.Log( "Server.ParseWorldListXML: Could not parse the specified {0} rank for world \"{1}\": \"{2}\". No {0} limit was set.",
                        LogType.Error, fieldType, world.name, temp.Value );
            return RankList.LowestRank;
        }


        const string WorldListTempFile = WorldListFileName + ".tmp";
        public static void SaveWorldList() {
            // Save world list
            try {
                XDocument doc = new XDocument();
                XElement root = new XElement( "fCraftWorldList" );
                XElement temp;
                lock( worldListLock ) {
                    foreach( World world in worlds.Values ) {
                        temp = new XElement( "World" );
                        temp.Add( new XAttribute( "name", world.name ) );
                        temp.Add( new XAttribute( "access", world.accessSecurity.MinRank ) ); // LEGACY
                        temp.Add( new XAttribute( "build", world.buildSecurity.MinRank ) ); // LEGACY
                        temp.Add( world.accessSecurity.Serialize( "accessSecurity" ) );
                        temp.Add( world.buildSecurity.Serialize( "buildSecurity" ) );
                        if( world.neverUnload ) {
                            temp.Add( new XAttribute( "noUnload", true ) );
                        }
                        if( world.isHidden ) {
                            temp.Add( new XAttribute( "hidden", true ) );
                        }
                        root.Add( temp );
                    }
                    root.Add( new XAttribute( "main", mainWorld.name ) );
                }
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

        public static World AddWorld( string name, Map map, bool _neverUnload ) {
            if( !Player.IsValidName( name ) ) return null;
            lock( worldListLock ) {
                if( worlds.ContainsKey( name ) ) return null;
                World newWorld = new World( name ) { neverUnload = _neverUnload };

                if( map != null ) {
                    // if a map is given
                    newWorld.map = map;
                    map.world = newWorld;
                    if( !_neverUnload ) {
                        newWorld.UnloadMap( false );// UnloadMap also saves the map
                    } else {
                        newWorld.SaveMap();
                    }

                } else {
                    // generate default map
                    if( _neverUnload ) newWorld.LoadMap();
                }

                newWorld.UpdatePlayerList();
                newWorld.StartTasks();

                worlds.Add( name.ToLower(), newWorld );

                return newWorld;
            }
        }

        public static World FindWorldExact( string name ) {
            if( name == null ) return null;
            lock( worldListLock ) {
                return worlds.ContainsKey( name.ToLower() ) ? worlds[name.ToLower()] : null;
            }
        }


        public static World[] FindWorlds( string name ) {
            if( name == null ) return null;
            World[] tempList;
            lock( worldListLock ) {
                tempList = worlds.Values.ToArray();
            }

            List<World> results = new List<World>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null ) {
                    if( tempList[i].name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( tempList[i] );
                        break;
                    } else if( tempList[i].name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Add( tempList[i] );
                    }
                }
            }
            return results.ToArray();
        }


        public static World FindWorldOrPrintMatches( Player player, string worldName ) {
            World[] matches = FindWorlds( worldName );
            if( matches.Length == 0 ) {
                player.NoWorldMessage( worldName );
                return null;
            } else if( matches.Length > 1 ) {
                player.ManyMatchesMessage( "world", matches );
                return null;
            } else {
                return matches[0];
            }
        }


        public static bool RemoveWorld( string name ) {
            lock( worldListLock ) {
                World worldToDelete = FindWorldExact( name );
                if( worldToDelete == null || worldToDelete == mainWorld ) {
                    return false;
                } else {
                    Player[] worldPlayerList = worldToDelete.playerList;
                    worldToDelete.SendToAll( "&SYou have been moved to the main world." );
                    foreach( Player player in worldPlayerList ) {
                        player.session.JoinWorld( mainWorld, null );
                    }

                    worldToDelete.StopTasks();
                    worldToDelete.SaveMap();

                    worlds.Remove( name.ToLower() );
                    SaveWorldList();
                    return true;
                }
            }
        }


        // Note: no autocompletion
        public static bool RenameWorld( string oldName, string newName ) {
            lock( worldListLock ) {
                World oldWorld = FindWorldExact( oldName );
                World newWorld = FindWorldExact( newName );
                if( oldWorld == null || (newWorld != null && newWorld != oldWorld) ) return false;
                worlds.Remove( oldName.ToLower() );
                oldWorld.name = newName;
                worlds.Add( newName.ToLower(), oldWorld );
                return true;
            }
        }


        public static bool ReplaceWorld( string name, World newWorld ) {
            lock( worldListLock ) {
                World oldWorld = FindWorldExact( name );
                if( oldWorld == null ) return false;

                newWorld.name = oldWorld.name;
                if( oldWorld == mainWorld ) {
                    mainWorld = newWorld;
                }

                // initialize the player list cache
                newWorld.UpdatePlayerList();

                // swap worlds
                worlds[name.ToLower()] = newWorld;

                oldWorld.StopTasks();
                newWorld.StopTasks();

                Scheduler.UpdateCache();

                newWorld.StartTasks();
            }
            return true;
        }


        public static int CountLoadedWorlds() {
            lock( worldListLock ) {
                return worlds.Values.Count( world => world.map != null );
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
                    if( tempList[i] != except && !tempList[i].IsIgnoring( origin.info ) ) {
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
                if( tempList[i].info.rank == rank ) {
                    tempList[i].Send( packet );
                }
            }
        }


        // Sends a string to all players of a specific rank
        public static void SendToRank( Player origin, string message, Rank rank ) {
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                Player[] tempList = PlayerList;
                for( int i = 0; i < tempList.Length; i++ ) {
                    if( tempList[i].info.rank == rank && !tempList[i].IsIgnoring( origin.info ) ) {
                        tempList[i].Send( packet );
                    }
                }
            }
        }

        #endregion


        #region Events
        // events

        [Obsolete]
        public static event SimpleEventHandler OnInit;
        [Obsolete]
        public static event SimpleEventHandler OnStart;
        [Obsolete]
        public static event PlayerConnectedEventHandler OnPlayerConnected;
        [Obsolete]
        public static event PlayerDisconnectedEventHandler OnPlayerDisconnected;
        [Obsolete]
        public static event PlayerKickedEventHandler OnPlayerKicked;
        [Obsolete]
        public static event PlayerRankChangedEventHandler OnRankChanged;
        [Obsolete]
        public static event UrlChangeEventHandler OnURLChanged;
        [Obsolete]
        public static event SimpleEventHandler OnShutdownBegin;
        [Obsolete]
        public static event SimpleEventHandler OnShutdownEnd;
        [Obsolete]
        public static event PlayerChangedWorldEventHandler OnPlayerChangedWorld;
        [Obsolete]
        public static event LogEventHandler OnLog;
        [Obsolete]
        public static event PlayerListChangedHandler OnPlayerListChanged;
        [Obsolete]
        public static event PlayerSentMessageEventHandler OnPlayerSentMessage;
        [Obsolete]
        public static event PlayerBanStatusChangedEventHandler OnPlayerBanned;
        [Obsolete]
        public static event PlayerBanStatusChangedEventHandler OnPlayerUnbanned;


        internal static void FireURLChangeEvent( string newUrl ) {
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
                list[i] = playerListCache[i].info.rank.Name + " - " + playerListCache[i].name;
            }
            Array.Sort( list );
            OnPlayerListChanged( list );
        }

        internal static bool FireSentMessageEvent( Player player, ref string message ) {
            bool cancel = false;
            if( OnPlayerSentMessage != null ) {
                OnPlayerSentMessage( player, player.world, ref message, ref cancel );
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
                if( player.info.rank.IdleKickTimer <= 0 ) continue;
                if( DateTime.UtcNow.Subtract( player.idleTimer ).TotalMinutes >= player.info.rank.IdleKickTimer ) {
                    SendToAllExcept( "{0}&S was kicked for being idle for {1} min", player,
                                     player.GetClassyName(),
                                     player.info.rank.IdleKickTimer.ToString() );
                    AdminCommands.DoKick( Player.Console, player, "Idle for " + player.info.rank.IdleKickTimer + " minutes", true, LeaveReason.IdleKick );
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
            CPUUsageTotal = (float)(newCPUTime.TotalMilliseconds / (Environment.ProcessorCount * DateTime.Now.Subtract( serverStart ).TotalMilliseconds));
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
            RandomNumberGenerator prng = RNGCryptoServiceProvider.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while( sb.Length < 32 ) {
                prng.GetBytes( oneChar );
                if( !Char.IsControl( (char)oneChar[0] ) ) {
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
            byte[] data = hasher.ComputeHash( Encoding.ASCII.GetBytes( salt + name ) );
            for( int i = 0; i < 16; i += 2 ) {
                if( hash[i] + "" + hash[i + 1] != data[i / 2].ToString( "x2" ) ) {
                    return false;
                }
            }
            return true;
        }


        public static int CalculateMaxPacketsPerUpdate( World world ) {
            int packetsPerTick = (int)(packetsPerSecond / ticksPerSecond);
            int maxPacketsPerUpdate = (int)(maxUploadSpeed / ticksPerSecond * 128);

            int playerCount = world.playerList.Length;
            if( playerCount > 0 && !world.isFlushing ) {
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


        static Regex regexIP = new Regex( @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b", RegexOptions.Compiled );
        public static bool IsIP( string IPString ) {
            return regexIP.IsMatch( IPString );
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
        static Dictionary<int, Player> players = new Dictionary<int, Player>();
        public static Player[] PlayerList { get; private set; }
        static object playerListLock = new object();

        // session list
        static List<Session> sessions = new List<Session>();
        static object sessionLock = new object();


        public static void KickGhostsAndRegisterSession( Session newSession ) {
            List<Session> sessionsToKick = new List<Session>();
            lock( sessionLock ) {
                foreach( Session s in sessions ) {
                    if( s.player.name.Equals( newSession.player.name, StringComparison.OrdinalIgnoreCase ) ) {
                        sessionsToKick.Add( s );
                        s.Kick( "Connected from elsewhere!", LeaveReason.ClientReconnect );
                        Logger.Log( "Session.LoginSequence: Player {0} logged in. Ghost was kicked.", LogType.SuspiciousActivity,
                                    s.player.name );
                    }
                }
                sessions.Add( newSession );
            }
            foreach( Session ses in sessionsToKick ) {
                ses.WaitForDisconnect();
            }
        }


        public static string MakePlayerConnectedMessage( Player player, bool firstTime, World world ) {
            if( firstTime ) {
                return String.Format( "&S{0} ({1}&S) connected for the first time, joined {2}",
                                      player.name,
                                      player.info.rank.GetClassyName(),
                                      world.GetClassyName() );
            } else {
                return String.Format( "&S{0} ({1}&S) connected, joined {2}",
                                      player.name,
                                      player.info.rank.GetClassyName(),
                                      world.GetClassyName() );
            }
        }


        // Add a newly-logged-in player to the list, and notify existing players.
        public static bool RegisterPlayer( Player player ) {
            lock( playerListLock ) {
                if( players.Count >= Config.GetInt( ConfigKey.MaxPlayers ) && !player.info.rank.ReservedSlot ||
                    players.Count == Config.MaxPlayersSupported ) {
                    return false;
                }
                for( int i = 0; i < Config.MaxPlayersSupported; i++ ) {
                    if( players.ContainsKey( i ) ) continue;
                    player.id = i;
                    players[i] = player;
                    UpdatePlayerList();
                    player.session.hasRegistered = true;
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

            lock( playerListLock ) {
                if( !player.session.hasRegistered ) return;

                SendToAll( PacketWriter.MakeRemoveEntity( player.id ) );
                Logger.Log( "{0} left the server.", LogType.UserActivity,
                            player.name );
                if( Config.GetBool( ConfigKey.ShowConnectionMessages ) ) {
                    SendToAll( "&SPlayer {0}&S left the server.", player.GetClassyName() );
                }

                lock( worldListLock ) {
                    // better safe than sorry: go through ALL worlds looking for leftover players
                    foreach( World world in worlds.Values ) {
                        world.ReleasePlayer( player );
                    }
                }
                players.Remove( player.id );
                UpdatePlayerList();

                if( player.info != null ) player.info.ProcessLogout( player );
            }
        }


        public static void UnregisterSession( Session session ) {
            lock( sessionLock ) {
                if( sessions.Contains( session ) ) {
                    sessions.Remove( session );
                    if( OnPlayerDisconnected != null ) OnPlayerDisconnected( session );
                }
            }
        }


        public static void UpdatePlayerList() {
            lock( playerListLock ) {
                Player[] newPlayerList = new Player[players.Count];
                int i = 0;
                foreach( Player player in players.Values ) {
                    newPlayerList[i++] = player;
                }
                PlayerList = newPlayerList.OrderBy( player => player.name ).ToArray();
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
                if( tempList[i].name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Clear();
                    results.Add( tempList[i] );
                    break;
                } else if( tempList[i].name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
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
                    if( tempList[i].name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( tempList[i] );
                        break;
                    } else if( tempList[i].name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
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
                                          t.session.GetIP().ToString() == ip.ToString() ).ToArray();
        }


        /// <summary>Finds a player by name, without any kind of autocompletion.</summary>
        /// <param name="name">Name of the player (case-insensitive).</param>
        /// <returns>Player object, or null if player was not found.</returns>
        public static Player FindPlayerExact( string name ) {
            return PlayerList.FirstOrDefault( t => t != null &&
                                                   t.name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        /// <summary> Finds Player object associated with the given PlayerInfo object.</summary>
        /// <returns>Player object, or null if player is offline.</returns>
        public static Player FindPlayerExact( PlayerInfo info ) {
            if( info == null || !info.online ) {
                return null;
            } else {
                return FindPlayerExact( info.name );
            }
        }


        public static int GetPlayerCount( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return PlayerList.Length;
            } else {;
                return PlayerList.Count( player => !player.isHidden );
            }
        }

        #endregion
    }

    public class ShutdownParams {
        public string Reason;
        public int Delay;
        public bool KillProcess;
        public bool Restart;
    }
}