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


namespace fCraft {
    public static class Server {
        static List<Session> sessions = new List<Session>();
        static Dictionary<int, Player> players = new Dictionary<int, Player>( 255 );
        internal static Player[] playerList;
        static object playerListLock = new object();
        public static object worldListLock = new object();

        const string WorldListFile = "worlds.xml";
        public static Dictionary<string, World> worlds = new Dictionary<string, World>();
        public static World mainWorld;

        static TcpListener listener;

        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          maxSessionPacketsPerTick = 128,
                          maxBlockUpdatesPerTick = 50000; // used when there are no players in a world
        internal static float ticksPerSecond; //TODO: move to server

        const int maxPortAttempts = 20;
        public static int port;

        internal static string Salt = "";


        public static bool Init() {
            Logger.Init();
            GenerateSalt();

            // try to load the config
            if( !Config.Load() ) return false;
            Config.ApplyConfig();
            if( !Config.Save() ) return false;

            // start the task thread
            Tasks.Start();

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            // prepare the list of commands
            Commands.Init();

            // hook up IRC
            if( Config.GetBool( ConfigKey.IRCBot ) && IRCComm.CommStatus() && Config.GetBool( ConfigKey.IRCMsgs ) )
                Server.OnPlayerConnected += IRCBot.SendPlayerJoinMsg;


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
            } while( !portFound && attempts < maxPortAttempts );

            // if the port still cannot be opened after [maxPortAttempts] attemps, die.
            if( !portFound ) {
                Logger.Log( "Could not start listening after {0} tries. Giving up!", LogType.FatalError,
                               maxPortAttempts );
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

            // start the main loop - server is now connectible
            mainThread = new Thread( MainLoop );
            mainThread.Start();

            Heartbeat.Start();

            if( Config.GetBool( ConfigKey.IRCBot ) ) IRCBot.Start();

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
            Heartbeat.ShutDown();

            // kill IRC bot
            if( IRCBot.IsOnline() == true ) IRCBot.Shutdown();

            // kill background tasks
            Tasks.ShutDown();

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
            } else if( File.Exists( "worlds.txt" ) ) {
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
                    Logger.Log( "Server.LoadWorldList: Main world cannot have any access restrictions. Access permission for \"" + mainWorld.name + "\" has been reset.", LogType.Warning );
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
                    world.isHidden = (el.Attribute( "hidden" ) != null);
                    if( firstWorld == null ) firstWorld = world;
                    Logger.Log( "Server.ParseWorldListXML: Loaded world \"" + worldName + "\"", LogType.Debug );

                    LoadWorldClassRestriction( world, ref world.classAccess, "access", el );
                    LoadWorldClassRestriction( world, ref world.classBuild, "build", el );
                }
            }

            temp = root.Attribute( "main" );

            // note: both of these MAY be null, and ParseWorldList() should catch it
            if( temp != null ) {
                mainWorld = FindWorld( temp.Value );
            } else {
                mainWorld = firstWorld;
            }
        }


        static void LoadWorldClassRestriction( World world, ref PlayerClass field, string fieldType, XElement element ) {
            XAttribute temp;
            PlayerClass playerClass;
            if( (temp = element.Attribute( fieldType )) != null ) {
                if( (playerClass = ClassList.FindClass( temp.Value )) != null ) {
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


        static void LoadWorldListTXT() {
            string[] worldList = File.ReadAllLines( "worlds.txt" );
            bool first = true;
            foreach( string worldName in worldList ) {
                World world = AddWorld( worldName, null, first );
                if( world != null ) {
                    if( first ) mainWorld = world;
                    first = false;
                    Logger.Log( "Server.ParseWorldListTXT: Loaded world \"" + worldName + "\"", LogType.Debug );
                } else {
                    Logger.Log( "Server.ParseWorldListTXT: Error loading world \"" + worldName + "\"", LogType.Error );
                }
            }
            File.Delete( "worlds.txt" );
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
                    temp.Add( new XAttribute( "access", world.classAccess.name ) );
                    temp.Add( new XAttribute( "build", world.classBuild.name ) );
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
                worlds.Add( name, newWorld );

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
                        player.session.forcedWorldToJoin = mainWorld;
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
            SendToAll( PacketWriter.MakeMessage( message ), null );
        }
        public static void SendToAll( string message, Player except ) {
            SendToAll( PacketWriter.MakeMessage( message ), except );
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

        // checks for incoming connections and disposes old sessions
        internal static void CheckConnections( object param ) {
            if( listener.Pending() ) {
                Logger.Log( "Server.ListenerHandler: Incoming connection", LogType.Debug );
                try {
                    sessions.Add( new Session( listener.AcceptTcpClient() ) );
                } catch( Exception ex ) {
                    Logger.Log( "ERROR: Could not accept incoming connection: " + ex.Message, LogType.Error );
                }
            }
            // this loop does not need to be thread-safe since only mainthread can alter session list
            for( int i = 0; i < sessions.Count; i++ ) {
                if( sessions[i].canDispose ) {
                    sessions[i].Disconnect();
                    if( OnPlayerDisconnected != null ) OnPlayerDisconnected( sessions[i] );
                    Server.FirePlayerListChangedEvent();
                    sessions.RemoveAt( i );
                    i--;
                    Logger.Log( "Session disposed. Active sessions left: {0}.", LogType.Debug, sessions.Count );
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
        internal static bool FirePlayerClassChange( Player target, Player player, PlayerClass oldClass, PlayerClass newClass ) {
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
                    list[i] = playerListCache[i].info.playerClass.name + " - " + playerListCache[i].GetLogName();
                }
                Array.Sort<string>( list );
                OnPlayerListChanged( list );
            }
        }
        #endregion

        #region Scheduler

        static int taskIdCounter = 0;
        static Dictionary<int, ScheduledTask> tasks = new Dictionary<int, ScheduledTask>();
        static ScheduledTask[] taskList;
        static Thread mainThread;
        static DateTime serverStart;
        public static bool shuttingDown = false;
        static object taskListLock = new object();

        internal static void MainLoop() {
            ScheduledTask[] taskCache;
            ScheduledTask task;
            while( !shuttingDown ) {
                taskCache = taskList;
                for( int i = 0; i < taskCache.Length; i++ ) {
                    task = taskCache[i];
                    if( task.enabled && task.nextTime < DateTime.UtcNow ) {
                        task.callback( task.param );
                        task.nextTime += TimeSpan.FromMilliseconds( task.interval );
                    }
                }
                Thread.Sleep( 1 );
            }
        }


        static void AutoBackup( object param ) {
            World world = (World)param;
            if( world.map == null ) return;
            world.map.SaveBackup( world.GetMapName(), String.Format( "backups/{0}_{1:yyyy-MM-ddTHH-mm}.fcm", world.name, DateTime.Now ) );
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
                        StandardCommands.Kick( Player.Console, new Command( String.Format(
                            "kick {0} Idle for {1} minutes.",
                            player.name,
                            player.info.playerClass.idleKickTimer ) ) );
                        player.ResetIdleTimer();
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

        public static char[] reservedChars = { ' ', '!', '*', '\'', '(', ')', ';', ':', '@', '&',
                                                 '=', '+', '$', ',', '/', '?', '%', '#', '[', ']' };
        public static string UrlEncode( string input ) {
            StringBuilder output = new StringBuilder();
            for( int i = 0; i < input.Length; i++ ) {
                if( (input[i] >= '0' && input[i] <= '9') ||
                    (input[i] >= 'a' && input[i] <= 'z') ||
                    (input[i] >= 'A' && input[i] <= 'Z') ||
                    input[i] == '-' || input[i] == '_' || input[i] == '.' || input[i] == '~' ) {
                    output.Append( input[i] );
                } else if( Array.IndexOf<char>( reservedChars, input[i] ) != -1 ) {
                    output.Append( '%' ).Append( ((int)input[i]).ToString( "X" ) );
                }
            }
            return output.ToString();
        }

        public static bool VerifyName( string name, string hash ) {
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
            if( playerCount > 0 ) {
                maxPacketsPerUpdate /= playerCount;
                if( maxPacketsPerUpdate > packetsPerTick ) {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            } else {
                maxPacketsPerUpdate = maxBlockUpdatesPerTick;
            }

            return maxPacketsPerUpdate;
        }

        public static int SwapBytes( int value ) {
            return IPAddress.HostToNetworkOrder( value );
        }

        public static short SwapBytes( short value ) {
            return IPAddress.HostToNetworkOrder( value );
        }

        public static string PlayerListToString() {
            String players = "";
            foreach( Player player in playerList ) {
                players += player.name + ",";
            }
            return players;
        }

        #endregion

        #region PlayerList
        // Return player count
        public static int GetPlayerCount() {
            return sessions.Count;
        }

        // Add a newly-logged-in player to the list, and notify existing players.
        public static bool RegisterPlayer( Player player ) {
            lock( playerListLock ) {
                if( players.Count >= Config.GetInt( ConfigKey.MaxPlayers ) ) {
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
                    SendToAll( Color.Sys + player.GetLogName() + " left the server." );
                    Logger.Log( "{0} left the server.", LogType.UserActivity, player.name );

                    // better safe than sorry: go through ALL worlds looking for leftover players
                    lock( worldListLock ) {
                        foreach( World world in worlds.Values ) {
                            world.ReleasePlayer( player );
                        }
                    }

                    players.Remove( player.id );
                    UpdatePlayerList();
                    PlayerDB.ProcessLogout( player );
                    PlayerDB.Save();
                } else {
                    Logger.Log( "World.UnregisterPlayer: Trying to unregister a non-existent (unknown id) player.", LogType.Warning );
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
                playerList = newPlayerList;
            }
        }

        // Find player by name using autocompletion
        public static Player FindPlayer( string name ) {
            if( name == null ) return null;
            Player[] tempList = playerList;
            Player result = null;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if( result == null ) {
                        result = tempList[i];
                    } else {
                        return null;
                    }
                }
            }
            return result;
        }


        // Find player by name using autocompletion
        public static Player FindPlayer( System.Net.IPAddress ip ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].session.GetIP().ToString() == ip.ToString() ) {
                    return tempList[i];
                }
            }
            return null;
        }


        // Get player by name without autocompletion
        public static Player FindPlayerExact( string name ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].name == name ) {
                    return tempList[i];
                }
            }
            return null;
        }


        // Get player by name without autocompletion
        public static Player FindPlayerByNick( string nick ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].nick == nick ) {
                    return tempList[i];
                }
            }
            return null;
        }
        #endregion
    }
}