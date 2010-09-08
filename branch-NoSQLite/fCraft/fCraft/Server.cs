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

        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          MaxSessionPacketsPerTick = 128,
                          MaxBlockUpdatesPerTick = 60000; // used when there are no players in a world
        internal static float ticksPerSecond;

        const int MaxPortAttempts = 20;
        public static int port;

        internal static string Salt = "";


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

        public static bool Init() {
            ResetWorkingDirectory();

            // try to load the config
            if( !Config.Load(false) ) return false;
            Config.ApplyConfig();
            if( !Config.Save() ) return false;

            GenerateSalt();

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

            return true;
        }


        // Opens a socket for listening for incoming connections
        public static bool Start() {

            Player.Console = new Player( null, "(console)" );

            // try to load the world list
            if( !LoadWorldList() ) return false;
            SaveWorldList();

            // open the port
            bool portFound = false;
            int attempts = 0;
            port = Config.GetInt( ConfigKey.Port );

            do {
                try {
                    listener = new TcpListener( IPAddress.Any, port );
                    listener.Start();
                    portFound = true;

                } catch( Exception ex ) {
                    // if the port is unavailable, try next one
                    Logger.Log( "Could not start listening on port {0}, trying next port. ({1})", LogType.Error,
                                   port, ex.Message );
                    port++;
                    attempts++;
                }
            } while( !portFound && attempts < MaxPortAttempts );

            // if the port still cannot be opened after [maxPortAttempts] attemps, die.
            if( !portFound ) {
                Logger.Log( "Could not start listening after {0} tries. Giving up!", LogType.FatalError,
                               MaxPortAttempts );
                return false;
            }

            serverStart = DateTime.Now;
            Logger.Log( "Server.Run: now accepting connections at port {0}.", LogType.Debug,
                        port );

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
            Logger.Log( "Main world: \"" + mainWorld.name + "\".", LogType.SystemActivity );

            // Check for incoming connections 4 times per second
            AddTask( CheckConnections, 250 );

            // Check for idle people every 30 seconds
            AddTask( CheckIdles, 30000 );

            // Write out initial (empty) playerlist cache
            UpdatePlayerList();

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
        public static void Shutdown() {
            if( OnShutdownStart != null ) OnShutdownStart();

            // kill the main thread
            Logger.Log( "Server shutting down.", LogType.SystemActivity );
            shuttingDown = true;
            if( mainThread != null && mainThread.IsAlive ) {
                mainThread.Join();
            }

            // stop accepting new players
            if( listener != null ) {
                listener.Stop();
                listener = null;
            }

            // kill the heartbeat
            Heartbeat.Shutdown();

            // kill IRC bot
            if( IRC.connected ) IRC.Disconnect();

            // kill background tasks
            Tasks.Shutdown();

            // kick all players
            lock( playerListLock ) {
                foreach( Session session in sessions ) {
                    session.Kick( "Server shutting down." );
                }
            }

            // unload all worlds (includes saving 
            foreach( World world in worlds.Values ) {
                world.Shutdown();
            }

            PlayerDB.Save();
            IPBanList.Save();
            if( OnShutdownEnd != null ) OnShutdownEnd();
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
                if( mainWorld.classAccess != ClassList.lowestClass ) {
                    Logger.LogWarning( "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                                       "Access permission for \"{0}\" has been reset.", WarningLogSubtype.WorldListWarning,
                                       mainWorld.name );
                    mainWorld.classAccess = ClassList.lowestClass;
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

                    LoadWorldClassRestriction( world, ref world.classAccess, "access", el );
                    LoadWorldClassRestriction( world, ref world.classBuild, "build", el );
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


        static void LoadWorldClassRestriction( World world, ref PlayerClass field, string fieldType, XElement element ) {
            XAttribute temp;
            PlayerClass playerClass;
            if( (temp = element.Attribute( fieldType )) != null ) {
                if( (playerClass = ClassList.ParseClass( temp.Value )) != null ) {
                    field = playerClass;
                } else {
                    Logger.Log( "Server.ParseWorldListXML: Could not parse the specified {0} class for world \"{1}\": \"{2}\". No access limit was set.", LogType.Error,
                                fieldType,
                                world.name,
                                temp.Value );
                    field = ClassList.lowestClass;
                }
            } else {
                field = ClassList.lowestClass;
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
                    temp.Add( new XAttribute( "access", world.classAccess ) );
                    temp.Add( new XAttribute( "build", world.classBuild ) );
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

                newWorld.updateTaskId = AddTask( UpdateBlocks, Config.GetInt( ConfigKey.TickInterval ), newWorld );

                if( Config.GetInt( ConfigKey.SaveInterval ) > 0 ) {
                    int saveInterval = Config.GetInt( ConfigKey.SaveInterval ) * 1000;
                    newWorld.saveTaskId = AddTask( SaveMap, saveInterval, newWorld, saveInterval );
                }

                if( Config.GetInt( ConfigKey.BackupInterval ) > 0 ) {
                    int backupInterval = Config.GetInt( ConfigKey.BackupInterval ) * 1000 * 60;
                    newWorld.backupTaskId = AddTask( AutoBackup, backupInterval, newWorld, (Config.GetBool( ConfigKey.BackupOnStartup ) ? 0 : backupInterval) );
                }

                newWorld.UpdatePlayerList();

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
                    lock( taskListLock ) {
                        tasks.Remove( worldToDelete.updateTaskId );
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


        public static bool RenameWorld( string oldName, string newName ) {
            lock( worldListLock ) {
                World oldWorld = FindWorld( oldName );
                World newWorld = FindWorld( newName );
                if( oldWorld == null || newWorld != null ) return false;
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

        #endregion


        #region Networking
        public static void SendToAllDelayed( Packet packet, Player except ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].Send( packet, false );
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

        // Broadcast to a specific class
        public static void SendToClass( Packet packet, PlayerClass playerClass ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i].info.playerClass == playerClass ) {
                    tempList[i].Send( packet );
                }
            }
        }

        // Broadcast to a specific class
        public static void SendToClass( string message, PlayerClass playerClass ) {
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                SendToClass( packet, playerClass );
            }
        }

        // checks for incoming connections and disposes old sessions
        internal static void CheckConnections( object param ) {
            if( listener.Pending() ) {
                try {
                    sessions.Add( new Session( listener.AcceptTcpClient() ) );
                } catch( Exception ex ) {
                    Logger.Log( "Server.CheckConnections: Could not accept incoming connection: " + ex, LogType.Error );
                }
            }
            // this loop does not need to be thread-safe since only mainthread can alter session list
            for( int i = 0; i < sessions.Count; i++ ) {
                if( sessions[i].canDispose ) {
                    if( OnPlayerDisconnected != null ) OnPlayerDisconnected( sessions[i] );
                    sessions[i].Disconnect();
                    Server.FirePlayerListChangedEvent();
                    sessions.RemoveAt( i );
                    i--;
                    Logger.Log( "Server.CheckConnections: Session disposed. Active sessions left: {0}.", LogType.Debug, sessions.Count );
                    GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
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
        public static event PlayerChangedClassEventHandler OnPlayerClassChanged;
        public static event URLChangeEventHandler OnURLChanged;
        public static event SimpleEventHandler OnShutdownStart;
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
        internal static bool FirePlayerClassChange( PlayerInfo target, Player player, PlayerClass oldClass, PlayerClass newClass ) {
            bool cancel = false;
            if( OnPlayerClassChanged != null ) OnPlayerClassChanged( target, player, oldClass, newClass, ref cancel );
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
                    list[i] = playerListCache[i].info.playerClass.name + " - " + playerListCache[i].name;
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

        static void UpdateBlocks( object param ) {
            World world = (World)param;
            if( world.map == null ) return;
            world.map.ProcessUpdates();
        }

        static void CheckIdles( object param ) {
            Player[] tempPlayerList = playerList;
            foreach( Player player in tempPlayerList ) {
                if( player.info.playerClass.idleKickTimer > 0 ) {
                    if( DateTime.UtcNow.Subtract( player.idleTimer ).TotalMinutes >= player.info.playerClass.idleKickTimer ) {
                        SendToAll( String.Format( "{0}&S was kicked for being idle for {1} min",
                                                  player.GetClassyName(),
                                                  player.info.playerClass.idleKickTimer ) );
                        StandardCommands.DoKick( Player.Console, player, "Idle for " + player.info.playerClass.idleKickTimer + " minutes", true );
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
            tasks.Add( ++taskIdCounter, newTask );
            UpdateTaskListCache();
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

        #endregion


        #region Utilities

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
                    output.Append( '%' ).Append( ((int)input[i]).ToString( "X" ) );
                }
            }
            return output.ToString();
        }

        public static bool VerifyName( string name, string hash ) {
            while( hash.Length < 32 ) {
                hash = "0" + hash;
            }
            MD5 hasher = MD5.Create();
            byte[] data = hasher.ComputeHash( Encoding.ASCII.GetBytes( Server.Salt + name ) );
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

        #endregion


        #region PlayerList

        public static void ShowPlayerConnectedMessage( Player player, bool firstTime, World world ) {
            if( firstTime ) {
                SendToAll( String.Format( "&S{0} ({1}&S) connected for the first time, joined {2}",
                                          player.name,
                                          player.info.playerClass.GetClassyName(),
                                          world.GetClassyName() ),
                                          player );
            } else {
                SendToAll( String.Format( "&S{0} ({1}&S) connected, joined {2}",
                                          player.name,
                                          player.info.playerClass.GetClassyName(),
                                          world.GetClassyName() ),
                                          player );
            }
        }

        // Return player count
        public static int GetPlayerCount() {
            return sessions.Count;
        }

        // Add a newly-logged-in player to the list, and notify existing players.
        public static bool RegisterPlayer( Player player ) {
            lock( playerListLock ) {
                if( players.Count >= Config.GetInt( ConfigKey.MaxPlayers ) && !player.info.playerClass.reservedSlot ) {
                    return false;
                }
                for( int i = 0; i < 255; i++ ) {
                    if( !players.ContainsKey( i ) ) {
                        player.id = i;
                        players[i] = player;
                        Server.UpdatePlayerList();
                        return true;
                    }
                }
                return false;
            }
        }


        // Remove player from the list, and notify remaining players
        public static void UnregisterPlayer( Player player ) {
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
                        lock( worldListLock ) {
                            foreach( World world in worlds.Values ) {
                                world.ReleasePlayer( player );
                            }
                        }
                        players.Remove( player.id );
                        UpdatePlayerList();
                    }

                    PlayerDB.ProcessLogout( player );
                    PlayerDB.Save();
                } else {
                    Logger.LogWarning( "World.UnregisterPlayer: Trying to unregister a non-existent (unknown id) player.",
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
        }

        // Find player by name using autocompletion
        public static List<Player> FindPlayers( string name ) {
            Player[] tempList = playerList;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Add( tempList[i] );
                }
            }
            return results;
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
                if( tempList[i] != null && tempList[i].name.ToLower() == name ) {
                    return tempList[i];
                }
            }
            return null;
        }

        #endregion
    }
}