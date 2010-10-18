    // Copyright 2009, 2010 Matvei Stefarov <me@matvei.org> and Jesse O'Brien <destroyer661@gmail.com>
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using System.Linq;


namespace fCraft {
    public static class Server {
        static List<Session> sessions = new List<Session>();
        static Dictionary<int, Player> players = new Dictionary<int, Player>();
        internal static Player[] playerList;
        static object playerListLock = new object();
        public static object worldListLock = new object();

        const string WorldListFile = "worlds.xml";
        public static SortedDictionary<string, World> worlds = new SortedDictionary<string, World>();
        public static World mainWorld;

        static TcpListener listener;
        public static IPAddress IP;

        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          MaxSessionPacketsPerTick = 128,
                          MaxBlockUpdatesPerTick = 60000; // used when there are no players in a world
        internal static float ticksPerSecond;

        const int MaxPortAttempts = 20;

        public static int Port;
        public static string URL;


        public static bool Init() {
            serverStart = DateTime.Now;

            ResetWorkingDirectory();

            // try to load the config
            if( !Config.Load( false ) ) return false;
            Config.ApplyConfig();
            GenerateSalt();
            if( !Config.Save(true) ) return false;

            CheckMapDirectory();

            // start the task thread
            Tasks.Start();

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            // prepare the list of commands
            CommandList.Init();

            // Init IRC
            IRC.Init();

            if( OnInit != null ) OnInit();

            ZLibStream.Init();

            if( Config.GetBool( ConfigKey.AutoRankEnabled ) ) {
                AutoRank.Init();
            }

            return true;
        }


        public static bool Start() {

            if( CheckForFCraftProcesses() ) {
                Logger.Log( "Please close all other fCraft processes (fCraftUI, fCraftConsole, or ConfigTool) "+
                            "that are started form the same directory.", LogType.Warning );
            }

            Player.Console = new Player( null, "(console)" );


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
            string line = "All available worlds: ";
            bool firstPrintedWorld = true;
            foreach( string worldName in Server.worlds.Keys ) {
                if( !firstPrintedWorld ) {
                    line += ", ";
                }
                line += worldName;
                firstPrintedWorld = false;
            }
            Logger.Log( line, LogType.SystemActivity );
            Logger.Log( "Main world: {0}; default rank: {1}", LogType.SystemActivity,
                        mainWorld.name, RankList.DefaultRank.Name );

            // Check for incoming connections 4 times per second
            AddTask( CheckConnections, 250 );

            // Check for idle people every 30 seconds
            AddTask( CheckIdles, 30000 );

            // Monitor CPU usage every 60 seconds
            AddTask( MonitorProcessorUsage, CPUMonitorInterval );

            AddTask( SavePlayerDB, PlayerDB.SaveInterval );

            // Write out initial (empty) playerlist cache
            UpdatePlayerList();

            // apply AutoRank
            if( Config.GetBool( ConfigKey.AutoRankEnabled ) ) {
                AddTask( AutoRankTick, AutoRankTickInterval );
            }

            // Announcements
            if( Config.GetInt( ConfigKey.AnnouncementInterval ) > 0 ) {
                AddTask( ShowRandomAnnouncement, Config.GetInt( ConfigKey.AnnouncementInterval ) * 60000 );
            }

            // start the main loop - server is now connectible
            mainThread = new Thread( MainLoop );
            mainThread.Start();

            Heartbeat.Start();

            if( Config.GetBool( ConfigKey.IRCBot ) ) IRC.Start();

            // fire OnStart event
            if( OnStart != null ) OnStart();
            return true;
        }


        // shuts down the server and aborts threads
        // NOTE: Do not call from any of the usual threads (main, heartbeat, tasks).
        // Call from UI thread or a new separate thread only.
        public static void Shutdown( string reason ) {
            if( shuttingDown ) return;
#if DEBUG
#else
            try {
#endif
                shuttingDown = true;
                if( OnShutdownBegin != null ) OnShutdownBegin();

                Logger.Log( "Server shutting down ({0})", LogType.SystemActivity, reason );


                // kick all players
                if( playerList != null ) {
                    Player[] pListCached = playerList;
                    foreach( Player player in pListCached ) {
                        // NOTE: kick packet delivery here is not currently guaranteed
                        player.session.Kick( "Server shutting down (" + reason + ")" );
                    }
                }

                // kill the main thread
                if( mainThread != null && mainThread.IsAlive ) {
                    mainThread.Join( 5000 );
                    if( mainThread.IsAlive ) {
                        mainThread.Abort(); // temporary kludge until i find a real cause
                    }
                }

                // stop accepting new players
                if( listener != null ) {
                    listener.Stop();
                    listener = null;
                }

                // kill the heartbeat
                Heartbeat.Shutdown();

                // kill IRC bot
                IRC.Disconnect();

                // kill background tasks
                Tasks.Shutdown();

                lock( worldListLock ) {
                    // unload all worlds (includes saving)
                    foreach( World world in worlds.Values ) {
                        world.Shutdown();
                    }
                }

                if( PlayerDB.isLoaded ) PlayerDB.Save(null);
                if( IPBanList.isLoaded ) IPBanList.Save();

                if( OnShutdownEnd != null ) OnShutdownEnd();
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.Log( "Server.Shutdown: Unexpected error: {0}", LogType.Error, ex );
                Logger.UploadCrashReport( "Unexpected error on shutdown", "fCraft", ex );
            }
#endif
        }


        class ShutdownParams {
            public string Reason;
            public int Delay;
            public bool KillProcess;
        }


        public static void InitiateShutdown( string reason, int delay, bool killProcess ) {
            new Thread( delegate( object obj ) {
                ShutdownParams param = (ShutdownParams)obj;
                Thread.Sleep( param.Delay * 1000 );
                Server.Shutdown( param.Reason );
                if( param.KillProcess ) {
                    Process.GetCurrentProcess().Kill();
                }
            } ).Start( new ShutdownParams {
                Reason = reason,
                Delay = delay,
                KillProcess = killProcess
            } );
        }

        #region Worlds

        #region World List Saving/Loading

        static bool LoadWorldList() {
            if( File.Exists( WorldListFile ) ) {
                try {
                    LoadWorldListXML();
                } catch( Exception ex ) {
                    Logger.Log( "An error occured while trying to parse the world list: " + Environment.NewLine + ex.ToString(), LogType.FatalError );
                    return false;
                }
            } else if( File.Exists( "worlds.txt" ) ) { // LEGACY
                LoadWorldListTXT();
            } else {
                Logger.Log( "Server.Start: No world list found. Creating default \"main\" world.", LogType.SystemActivity );
                mainWorld = AddWorld( "main", null, true );
            }

            if( worlds.Count == 0 ) {
                Logger.Log( "Server.Start: Could not load any of the specified worlds, or no worlds were specified. Creating default \"main\" world.", LogType.Error );
                mainWorld = AddWorld( "main", null, true );
            }

            // if there is no default world still, die.
            if( mainWorld == null ) {
                Logger.Log( "World creation failed. Shutting down.", LogType.FatalError );
                return false;
            } else {
                if( mainWorld.accessRank != RankList.LowestRank ) {
                    Logger.LogWarning( "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                                       "Access permission for \"{0}\" has been reset.", WarningLogSubtype.WorldListWarning,
                                       mainWorld.name );
                    mainWorld.accessRank = RankList.LowestRank;
                }
                if( !mainWorld.neverUnload ) {
                    mainWorld.neverUnload = true;
                    mainWorld.LoadMap();
                }
            }

            return true;
        }


        static void LoadWorldListXML() {
            XDocument doc = XDocument.Load( WorldListFile );
            XElement root = doc.Root;
            World firstWorld = null;
            XAttribute temp = null;
            string worldName;

            foreach( XElement el in root.Elements( "World" ) ) {
                if( (temp = el.Attribute( "name" )) == null ) {
                    Logger.Log( "Server.ParseWorldListXML: World tag with no name skipped.", LogType.Error );
                    continue;
                }
                worldName = temp.Value;
                if( !Player.IsValidName( worldName ) ) {
                    Logger.Log( "Server.ParseWorldListXML: Invalid world name skipped: \"" + worldName + "\"", LogType.Error );
                    continue;
                }

                World world = AddWorld( worldName, null, (el.Attribute( "noUnload" ) != null) );

                if( world == null ) {
                    Logger.Log( "Server.ParseWorldListXML: Error loading world \"" + worldName + "\"", LogType.Error );
                } else {
                    if( (temp = el.Attribute( "hidden" )) != null ) {
                        if( !Boolean.TryParse( temp.Value, out world.isHidden ) ) {
                            Logger.LogWarning( "Server.ParseWorldListXML: Could not parse \"hidden\" attribute of world \"{0}\", assuming NOT hidden.",
                                               WarningLogSubtype.WorldListWarning,
                                               worldName );
                            world.isHidden = false;
                        }
                    }
                    if( firstWorld == null ) firstWorld = world;
                    Logger.Log( "Server.ParseWorldListXML: Loaded world \"" + worldName + "\"", LogType.Debug );

                    LoadWorldClassRestriction( world, ref world.accessRank, "access", el );
                    LoadWorldClassRestriction( world, ref world.buildRank, "build", el );
                }
            }

            if( (temp = root.Attribute( "main" )) != null ) {
                mainWorld = FindWorld( temp.Value );
                // if specified main world does not exist, use first-defined world
                if( mainWorld == null && firstWorld != null ) {
                    Logger.LogWarning( "The specified main world \"{0}\" does not exist. \"{1}\" was designated main instead. You can use /wmain to change it.",
                                       WarningLogSubtype.WorldListWarning,
                                       temp.Value, firstWorld.name );
                    mainWorld = firstWorld;
                }
                // if firstWorld was also null, LoadWorldList() should try creating a new mainWorld

            } else {
                mainWorld = firstWorld;
            }
        }


        static void LoadWorldClassRestriction( World world, ref Rank field, string fieldType, XElement element ) {
            XAttribute temp;
            Rank rank;
            if( (temp = element.Attribute( fieldType )) != null ) {
                if( (rank = RankList.ParseRank( temp.Value )) != null ) {
                    field = rank;
                } else {
                    Logger.Log( "Server.ParseWorldListXML: Could not parse the specified {0} class for world \"{1}\": \"{2}\". No access limit was set.", LogType.Error,
                                fieldType,
                                world.name,
                                temp.Value );
                    field = RankList.LowestRank;
                }
            } else {
                field = RankList.LowestRank;
            }
        }


        static void LoadWorldListTXT() { // LEGACY
            string[] worldList = File.ReadAllLines( "worlds.txt" );
            bool first = true;
            foreach( string worldName in worldList ) {
                World world = AddWorld( worldName, null, first );
                if( world != null ) {
                    if( first ) mainWorld = world;
                    first = false;
                    Logger.Log( "Server.ParseWorldListTXT: Loaded world \"{0}\"", LogType.Debug, worldName );
                } else {
                    Logger.Log( "Server.ParseWorldListTXT: Error loading world \"{0}\"", LogType.Error, worldName );
                }
            }
            try {
                File.Delete( "worlds.txt" );
            } catch( Exception ex ) {
                Logger.Log( "Server.LoadWorldListTXT: An error occured while trying to delete \"worlds.txt\": " + ex, LogType.Error );
            }
        }


        public static void SaveWorldList() {
            // Save world list
            lock( worldListLock ) {
                XDocument doc = new XDocument();
                XElement root = new XElement( "fCraftWorldList" );
                XElement temp;
                foreach( World world in worlds.Values ) {
                    temp = new XElement( "World" );
                    temp.Add( new XAttribute( "name", world.name ) );
                    temp.Add( new XAttribute( "access", world.accessRank ) );
                    temp.Add( new XAttribute( "build", world.buildRank ) );
                    if( world.neverUnload ) {
                        temp.Add( new XAttribute( "noUnload", true ) );
                    }
                    if( world.isHidden ) {
                        temp.Add( new XAttribute( "hidden", true ) );
                    }
                    root.Add( temp );
                }
                root.Add( new XAttribute( "main", mainWorld.name ) );
                doc.Add( root );
                doc.Save( WorldListFile );
            }
        }

        #endregion

        public static World AddWorld( string name, Map map, bool neverUnload ) {
            lock( worldListLock ) {
                if( worlds.ContainsKey( name ) ) return null;
                if( !Player.IsValidName( name ) ) return null;
                World newWorld = new World( name );
                newWorld.neverUnload = neverUnload;

                if( map != null ) {
                    // if a map is given
                    newWorld.map = map;
                    if( !neverUnload ) {
                        newWorld.UnloadMap();// UnloadMap also saves the map
                    } else {
                        newWorld.SaveMap( null );
                    }

                } else {
                    // generate default map
                    if( neverUnload ) newWorld.LoadMap();
                }
                worlds.Add( name.ToLower(), newWorld );

                newWorld.UpdatePlayerList();

                newWorld.updateTaskId = AddTask( UpdateBlocks, Config.GetInt( ConfigKey.TickInterval ), newWorld );

                if( Config.GetInt( ConfigKey.SaveInterval ) > 0 ) {
                    int saveInterval = Config.GetInt( ConfigKey.SaveInterval ) * 1000;
                    newWorld.saveTaskId = AddTask( SaveMap, saveInterval, newWorld, saveInterval );
                }

                if( Config.GetInt( ConfigKey.BackupInterval ) > 0 ) {
                    int backupInterval = Config.GetInt( ConfigKey.BackupInterval ) * 1000 * 60;
                    newWorld.backupTaskId = AddTask( AutoBackup, backupInterval, newWorld, (Config.GetBool( ConfigKey.BackupOnStartup ) ? 0 : backupInterval) );
                }


                return newWorld;
            }
        }


        public static World FindWorld( string name ) {
            if( name == null ) return null;
            lock( worldListLock ) {
                if( worlds.ContainsKey( name.ToLower() ) ) {
                    return worlds[name.ToLower()];
                } else {
                    return null;
                }
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


        public static bool RemoveWorld( string name ) {
            lock( worldListLock ) {
                World worldToDelete = FindWorld( name );
                if( worldToDelete == null || worldToDelete == mainWorld ) {
                    return false;
                } else {
                    Player[] worldPlayerList = worldToDelete.playerList;
                    worldToDelete.SendToAll( Color.Sys + "You have been moved to the main world." );
                    foreach( Player player in worldPlayerList ) {
                        player.session.JoinWorld( mainWorld, null );
                    }
                    worldToDelete.SaveMap( null );
                    lock( taskListLock ) {
                        tasks.Remove( worldToDelete.updateTaskId );
                        // If saveTaskId or backupTaskId were not defined, Remove does nothing
                        // because default value for saveTaskId and backupTaskId is -1
                        tasks.Remove( worldToDelete.saveTaskId );
                        tasks.Remove( worldToDelete.backupTaskId );
                        UpdateTaskListCache();
                    }
                    worlds.Remove( name.ToLower() );
                    SaveWorldList();
                    return true;
                }
            }
        }


        // Note: no autocompletion
        public static bool RenameWorld( string oldName, string newName ) {
            lock( worldListLock ) {
                World oldWorld = FindWorld( oldName );
                World newWorld = FindWorld( newName );
                if( oldWorld == null || (newWorld != null && newWorld != oldWorld) ) return false;
                worlds.Remove( oldName.ToLower() );
                oldWorld.name = newName;
                worlds.Add( newName.ToLower(), oldWorld );
                return true;
            }
        }


        public static bool ReplaceWorld( string name, World newWorld ) {
            lock( worldListLock ) {
                World oldWorld = FindWorld( name );
                if( oldWorld == null ) return false;

                newWorld.name = oldWorld.name;
                if( oldWorld == mainWorld ) {
                    mainWorld = newWorld;
                }

                // initialize the player list cache
                newWorld.UpdatePlayerList();

                // swap worlds
                worlds[name.ToLower()] = newWorld;
                lock( taskListLock ) {
                    // removes tasks associated with the old world
                    tasks.Remove( oldWorld.updateTaskId );
                    tasks.Remove( oldWorld.saveTaskId );
                    tasks.Remove( oldWorld.backupTaskId );

                    // if the new world was previously associated with some tasks, remove the old one, and reregister
                    tasks.Remove( newWorld.updateTaskId );
                    tasks.Remove( newWorld.saveTaskId );
                    tasks.Remove( newWorld.backupTaskId );

                    UpdateTaskListCache();
                }

                // adds tasks to the new world
                newWorld.updateTaskId = AddTask( UpdateBlocks, Config.GetInt( ConfigKey.TickInterval ), newWorld );

                if( Config.GetInt( ConfigKey.SaveInterval ) > 0 ) {
                    int saveInterval = Config.GetInt( ConfigKey.SaveInterval ) * 1000;
                    newWorld.saveTaskId = AddTask( SaveMap, saveInterval, newWorld, saveInterval );
                }

                if( Config.GetInt( ConfigKey.BackupInterval ) > 0 ) {
                    int backupInterval = Config.GetInt( ConfigKey.BackupInterval ) * 1000 * 60;
                    newWorld.backupTaskId = AddTask( AutoBackup, backupInterval, newWorld, (Config.GetBool( ConfigKey.BackupOnStartup ) ? 0 : backupInterval) );
                }
                return true;
            }
        }


        public static int CountLoadedWorlds() {
            int counter = 0;
            lock( worldListLock ) {
                foreach( World world in worlds.Values ) {
                    if( world.map != null ) counter++;
                }
            }
            return counter;
        }

        #endregion


        #region Networking

        public static void SendToAllDelayed( Packet packet, Player except ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].SendDelayed( packet );
                }
            }
        }

        public static void SendToAll( Packet packet ) {
            SendToAll( packet, null );
        }

        public static void SendToAll( Packet packet, Player except ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].Send( packet );
                }
            }
        }

        public static void SendToAll( string message ) {
            SendToAll( ">", message, null );
        }

        public static void SendToAll( string prefix, string message ) {
            SendToAll( prefix, message, null );
        }

        public static void SendToAll( string message, Player except ) {
            SendToAll( ">", message, except );
        }

        public static void SendToAll( string prefix, string message, Player except ) {
            foreach( Packet p in PacketWriter.MakeWrappedMessage( prefix, message, false ) ) {
                SendToAll( p, except );
            }
        }

        public static void SendToSeeing( Packet packet, Player source ) {
            Player[] playerListCopy = playerList;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }

        public static void SendToBlind( Packet packet, Player source ) {
            Player[] playerListCopy = playerList;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && !playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }

        // Broadcast to a specific class
        public static void SendToRank( Packet packet, Rank rank ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i].info.rank == rank ) {
                    tempList[i].Send( packet );
                }
            }
        }

        // Broadcast to a specific class
        public static void SendToRank( string message, Rank rank ) {
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                SendToRank( packet, rank );
            }
        }

        static object sessionLock = new object();
        // checks for incoming connections and disposes old sessions
        internal static void CheckConnections( object param ) {
            if( listener.Pending() ) {
                try {
                    lock( sessionLock ) {
                        Session newSession = new Session( listener.AcceptTcpClient() );
                        newSession.Start();
                        sessions.Add( newSession );
                    }
                } catch( Exception ex ) {
                    Logger.Log( "Server.CheckConnections: Could not accept incoming connection: " + ex, LogType.Error );
                }
            }
        }

        #endregion


        #region Events
        // events
        public static event SimpleEventHandler OnInit;
        public static event SimpleEventHandler OnStart;
        public static event PlayerConnectedEventHandler OnPlayerConnected;
        public static event PlayerDisconnectedEventHandler OnPlayerDisconnected;
        public static event RankChangedEventHandler OnRankChanged;
        public static event URLChangeEventHandler OnURLChanged;
        public static event SimpleEventHandler OnShutdownBegin;
        public static event SimpleEventHandler OnShutdownEnd;
        public static event PlayerChangedWorldEventHandler OnPlayerChangedWorld;
        public static event LogEventHandler OnLog;
        public static event PlayerListChangedHandler OnPlayerListChanged;
        public static event PlayerSentMessageEventHandler OnPlayerSentMessage;

        internal static void FireURLChangeEvent( string URL ) {
            if( OnURLChanged != null ) OnURLChanged( URL );
        }
        internal static void FireLogEvent( string message, LogType type ) {
            if( OnLog != null ) OnLog( message, type );
        }
        internal static bool FirePlayerConnectedEvent( Session session ) {
            bool cancel = false;
            if( OnPlayerConnected != null ) OnPlayerConnected( session, ref cancel );
            return !cancel;
        }
        internal static bool FirePlayerRankChange( PlayerInfo target, Player player, Rank oldClass, Rank newClass ) {
            bool cancel = false;
            if( OnRankChanged != null ) OnRankChanged( target, player, oldClass, newClass, ref cancel );
            return !cancel;
        }
        internal static void FireWorldChangedEvent( Player player, World oldWorld, World newWorld ) {
            if( OnPlayerChangedWorld != null ) OnPlayerChangedWorld( player, oldWorld, newWorld );
        }
        internal static void FirePlayerListChangedEvent() {
            if( OnPlayerListChanged != null ) {
                Player[] playerListCache = playerList;
                string[] list = new string[playerListCache.Length];
                for( int i = 0; i < list.Length; i++ ) {
                    list[i] = playerListCache[i].info.rank.Name + " - " + playerListCache[i].name;
                }
                Array.Sort<string>( list );
                OnPlayerListChanged( list );
            }
        }
        internal static bool FireSentMessageEvent( Player player, ref string message ) {
            bool cancel = false;
            if( OnPlayerSentMessage != null ) {
                OnPlayerSentMessage( player, player.world, ref message, ref cancel );
            }
            return !cancel;
        }

        #endregion


        #region Scheduler

        static int taskIdCounter;
        static Dictionary<int, ScheduledTask> tasks = new Dictionary<int, ScheduledTask>();
        static ScheduledTask[] taskList;
        static Thread mainThread;
        public static DateTime serverStart;
        public static bool shuttingDown;
        static object taskListLock = new object();

        internal static void MainLoop() {
#if DEBUG
#else
            try {
#endif
                ScheduledTask[] taskCache;
                ScheduledTask task;
                while( !shuttingDown ) {
                    taskCache = taskList;
                    for( int i = 0; i < taskCache.Length; i++ ) {
                        task = taskCache[i];
                        if( task.enabled && task.nextTime < DateTime.UtcNow ) {
#if DEBUG
                        task.callback( task.param );
#else
                            try {
                                task.callback( task.param );
                            } catch( Exception ex ) {
                                Logger.Log( "Server.MainLoop: Exception was thrown by a scheduled task: " + ex, LogType.Error );
                                Logger.UploadCrashReport( "Exception was thrown by a scheduled task", "fCraft", ex );
                            }
#endif
                            task.nextTime += TimeSpan.FromMilliseconds( task.interval );
                        }
                    }
                    Thread.Sleep( 1 );
                }
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.Log( "Fatal error in fCraft.Server main loop: " + ex, LogType.FatalError );
                Logger.UploadCrashReport( "Misc unnhandled exception in fCraft.Server.MainLoop", "fCraft", ex );
            }
#endif
        }

        const int AutoRankTickInterval = 60000; // 60 seconds
        static void AutoRankTick( object param ) {
            AutoRankCommands.DoAutoRankAll( Player.Console, PlayerDB.GetPlayerListCopy(), false );
        }

        static void AutoBackup( object param ) {
            World world = (World)param;
            if( world.map == null ) return;
            world.map.SaveBackup( world.GetMapName(), String.Format( "backups/{0}_{1:yyyy-MM-ddTHH-mm}.fcm", world.name, DateTime.Now ), true );
        }

        static void SaveMap( object param ) {
            World world = (World)param;
            if( world.map == null ) return;
            if( world.map.changesSinceSave > 0 ) {
                Tasks.Add( world.SaveMap, null, false );
            }
        }

        static void SavePlayerDB( object param ) {
            Tasks.Add( PlayerDB.Save, null, false );
        }

        static void UpdateBlocks( object param ) {
            World world = (World)param;
            if( world.map == null ) return;
            world.map.ProcessUpdates();
        }

        static void CheckIdles( object param ) {
            Player[] tempPlayerList = playerList;
            foreach( Player player in tempPlayerList ) {
                if( player.info.rank.IdleKickTimer > 0 ) {
                    if( DateTime.UtcNow.Subtract( player.idleTimer ).TotalMinutes >= player.info.rank.IdleKickTimer ) {
                        SendToAll( String.Format( "{0}&S was kicked for being idle for {1} min",
                                                  player.GetClassyName(),
                                                  player.info.rank.IdleKickTimer ) );
                        AdminCommands.DoKick( Player.Console, player, "Idle for " + player.info.rank.IdleKickTimer + " minutes", true );
                        player.ResetIdleTimer(); // to prevent kick from firing more than once
                    }
                }
            }
        }

        internal static int AddTask( TaskCallback task, int interval ) {
            return AddTask( task, interval, null, 0 );
        }

        internal static int AddTask( TaskCallback task, int interval, object param ) {
            return AddTask( task, interval, param, 0 );
        }

        internal static int AddTask( TaskCallback task, int interval, object param, int delay ) {
            ScheduledTask newTask = new ScheduledTask();
            newTask.nextTime = DateTime.UtcNow.AddMilliseconds( delay );
            newTask.callback = task;
            newTask.interval = interval;
            newTask.param = param;
            lock( taskListLock ) {
                tasks.Add( ++taskIdCounter, newTask );
                UpdateTaskListCache();
            }
            return taskIdCounter;
        }

        internal static void TaskToggle( int id, bool enabled ) {
            tasks[id].nextTime = DateTime.UtcNow;
            tasks[id].enabled = enabled;
            UpdateTaskListCache();
        }

        static void UpdateTaskListCache() {
            List<ScheduledTask> tempTaskList = new List<ScheduledTask>();
            lock( taskListLock ) {
                foreach( ScheduledTask task in tasks.Values ) {
                    if( task.enabled ) {
                        tempTaskList.Add( task );
                    }
                }
            }
            taskList = tempTaskList.ToArray();
        }

        public const string AnnouncementsFile = "announcements.txt";
        static void ShowRandomAnnouncement( object param ) {
            if( File.Exists( AnnouncementsFile ) ) {
                string[] lines = File.ReadAllLines( AnnouncementsFile );
                if( lines.Length == 0 ) return;
                string line = lines[new Random().Next( 0, lines.Length )];
                if( line.Trim().Length > 0 ) {
                    if( line.StartsWith( "&" ) ) {
                        SendToAll( line );
                    } else {
                        SendToAll( Color.Announcement + line );
                    }
                }
            }
        }


        static TimeSpan oldCPUTime = new TimeSpan( 0 );
        public static float CPUUsageTotal, CPUUsageLastMinute;
        const int CPUMonitorInterval = 60000; // 1 minute
        public static void MonitorProcessorUsage( object param ) {
            TimeSpan newCPUTime = Process.GetCurrentProcess().TotalProcessorTime;
            CPUUsageLastMinute = (float)((newCPUTime - oldCPUTime).TotalMilliseconds / (Environment.ProcessorCount * CPUMonitorInterval));
            CPUUsageTotal = (float)(newCPUTime.TotalMilliseconds / (Environment.ProcessorCount * DateTime.Now.Subtract( serverStart ).TotalMilliseconds));
            oldCPUTime = newCPUTime;
        }

        #endregion


        #region Utilities

        public static void CheckMapDirectory() {
            // move files, if necessary
            if( !Directory.Exists( "maps" ) ) { // LEGACY
                Directory.CreateDirectory( "maps" );
                string[] files = Directory.GetFiles( Directory.GetCurrentDirectory(), "*.fcm" );
                if( files.Length > 0 ) {
                    Logger.Log( "Server.CheckMapDirectory: fCraft now uses a dedicated /maps/ directory for storing map files. " +
                                "Your maps have been moved automatically.", LogType.SystemActivity );

                    foreach( string file in files ) {
                        string newFile = "maps/" + new FileInfo( file ).Name;
                        File.Move( file, newFile );
                        Logger.Log( "Server.CheckMapDirectory: Moved " + newFile, LogType.SystemActivity );
                    }
                }
            }
        }

        public static void CheckForCommonErrors( Exception ex ) {
            if( ex.Message.StartsWith( "Could not load file or assembly 'System.Xml.Linq" ) ) {
                Logger.Log( "Your crash was likely caused by using an outdated version of .NET or Mono runtime. " +
                            "Please update to Microsoft .NET Framework 3.5+ (Windows) OR Mono 2.6.4+ (Linux, Unix, Mac OS X).", LogType.Warning );
            }
        }

        public static void ResetWorkingDirectory() {
            // reset working directory to same directory as the executable
            Directory.SetCurrentDirectory( new FileInfo( Process.GetCurrentProcess().MainModule.FileName ).Directory.FullName );
        }

        internal static string Salt = "";
        internal static string OldSalt = "";
        static void GenerateSalt() {
            // generate random salt
            Random rand = new Random();
            int saltLength = rand.Next( 12, 16 );
            string saltChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-.~";
            for( int i = 0; i < saltLength; i++ ) {
                Salt += saltChars[rand.Next( 0, saltChars.Length - 1 )];
            }
        }

        public static string UrlEncode( string input ) {
            StringBuilder output = new StringBuilder();
            for( int i = 0; i < input.Length; i++ ) {
                if( (input[i] >= '0' && input[i] <= '9') ||
                    (input[i] >= 'a' && input[i] <= 'z') ||
                    (input[i] >= 'A' && input[i] <= 'Z') ||
                    input[i] == '-' || input[i] == '_' || input[i] == '.' || input[i] == '~' ) {
                    output.Append( input[i] );
                } else {
                    output.Append( '%' ).Append( ((int)input[i]).ToString( "X2" ) );
                }
            }
            return output.ToString();
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

        public static bool CheckForFCraftProcesses(){
            try {
                Process[] processList = Process.GetProcesses();

                foreach( Process process in processList ) {
                    if( process.ProcessName.ToLower() == "fcraftui" ||
                        process.ProcessName.ToLower() == "configtool" ||
                        process.ProcessName.ToLower() == "fcraftconsole" ) {
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

        #endregion


        #region PlayerList

        public static void ShowPlayerConnectedMessage( Player player, bool firstTime, World world ) {
            if( firstTime ) {
                SendToAll( String.Format( "&S{0} ({1}&S) connected for the first time, joined {2}",
                                          player.name,
                                          player.info.rank.GetClassyName(),
                                          world.GetClassyName() ),
                                          player );
            } else {
                SendToAll( String.Format( "&S{0} ({1}&S) connected, joined {2}",
                                          player.name,
                                          player.info.rank.GetClassyName(),
                                          world.GetClassyName() ),
                                          player );
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
                    if( !players.ContainsKey( i ) ) {
                        player.id = i;
                        players[i] = player;
                        UpdatePlayerList();
                        return true;
                    }
                }
                return false;
            }
        }


        // Remove player from the list, and notify remaining players
        public static void UnregisterPlayer( Player player ) {
            if( player == null ) {
                throw new ArgumentNullException( "Server.UnregisterPlayer: player cannot be null." );
            }

            lock( worldListLock ) {
                lock( playerListLock ) {
                    if( players.ContainsKey( player.id ) ) {
                        SendToAll( PacketWriter.MakeRemoveEntity( player.id ) );
                        if( player.session.hasRegistered ) {
                            SendToAll( "&SPlayer " + player.GetClassyName() + "&S left the server." );
                        }
                        Logger.Log( "{0} left the server.", LogType.UserActivity,
                                    player.name );

                        if( player.session.hasRegistered ) {
                            // better safe than sorry: go through ALL worlds looking for leftover players

                            foreach( World world in worlds.Values ) {
                                world.ReleasePlayer( player );
                            }
                            players.Remove( player.id );
                            UpdatePlayerList();
                        }

                        if( player.info != null ) player.info.ProcessLogout(player);
                    } else {
                        Logger.LogWarning( "Server.UnregisterPlayer: Trying to unregister a non-existent player.",
                                           WarningLogSubtype.OtherWarning );
                    }
                }
            }
        }


        public static void UnregisterSession( Session session ) {
            lock( sessionLock ) {
                if( sessions.Contains( session ) ) {
                    sessions.Remove( session );
                    if(OnPlayerDisconnected!=null) OnPlayerDisconnected( session );
                } else {
                    Logger.LogWarning( "Server.UnregisterPlayer: Trying to unregister a non-existent session.",
                                       WarningLogSubtype.OtherWarning );
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
                playerList = newPlayerList.OrderBy( player => player.name ).ToArray<Player>();
            }
            FirePlayerListChangedEvent();
        }

        // Find player by name using autocompletion (IGNORES HIDDEN PERMISSIONS)
        public static Player[] FindPlayers( string name ) {
            Player[] tempList = playerList;
            List<Player> results = new List<Player>();
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

        // Find player by name using autocompletion
        public static Player[] FindPlayers( Player player, string name ) {
            Player[] tempList = playerList;
            List<Player> results = new List<Player>();
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


        // Find player by IP
        public static List<Player> FindPlayers( IPAddress ip ) {
            Player[] tempList = playerList;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].session.GetIP().ToString() == ip.ToString() ) {
                    results.Add( tempList[i] );
                }
            }
            return results;
        }


        // Get player by name without autocompletion
        public static Player FindPlayerExact( string name ) {
            name = name.ToLower();
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].lowercaseName == name ) {
                    return tempList[i];
                }
            }
            return null;
        }


        public static int GetPlayerCount( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return playerList.Length;
            } else {
                int count = 0;
                Player[] playerListCache = playerList;
                foreach( Player player in playerListCache ) {
                    if( !player.isHidden ) count++;
                }
                return count;
            }
        }

        #endregion
    }
}