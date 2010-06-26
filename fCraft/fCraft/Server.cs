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


namespace fCraft {
    public static class Server {
        static List<Session> sessions = new List<Session>();
        static Dictionary<int, Player> players = new Dictionary<int, Player>( 255 );
        internal static Player[] playerList;
        static object playerListLock = new object();
        public static object worldListLock = new object();

        const string WorldListFile = "worlds.txt";
        public static Dictionary<string, World> worlds = new Dictionary<string, World>();
        public static World defaultWorld;

        static TcpListener listener;

        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          maxSessionPacketsPerTick = 128,
                          maxBlockUpdatesPerTick = 50000; // used when there are no players in a world
        internal static float ticksPerSecond; //TODO: move to server

        const int maxPortAttempts = 20;
        public static int port;

        public static bool Init() {
            Color.Init();
            Map.Init();

            Logger.Init();
            if( !Config.Load() ) return false;
            Config.ApplyConfig();
            if( !Config.Save() ) return false;

            // allocate player list
            Tasks.Init();

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();
            Commands.Init();

            if (Config.GetBool("IRCBot") && IRCComm.commStatus() && Config.GetBool("IRCMsgs")) 
                Server.OnPlayerConnected += IRCBot.sendPlayerJoinMsg;

            if( OnInit != null ) OnInit();

            return true;
        }


        // Opens a socket for listening for incoming connections
        public static bool Start() {

            Player.Console = new Player( null, "(console)" ); //TODO

            // Read world list
            if( File.Exists( WorldListFile ) ) {
                string[] worldList = File.ReadAllLines( WorldListFile );
                bool first = true;
                foreach( string worldName in worldList ) {
                    World world = AddWorld( worldName, null, first );
                    if( world != null ) {
                        if( first ) defaultWorld = world;
                        first = false;
                        Logger.Log( "Server.Start: Loaded world \"" + worldName + "\".", LogType.Debug );
                    } else {
                        Logger.Log( "Server.Start: Error loading world \"" + worldName + "\"", LogType.Error );
                    }
                }
                if( worlds.Count == 0 ) {
                    Logger.Log( "Server.Start: Could not load any of the specified worlds. Creating default \"main\" world.", LogType.Error );
                    defaultWorld = AddWorld( "main", null, true );
                }
            } else {
                Logger.Log( "Server.Start: No world list found. Creating default \"main\" world.", LogType.SystemActivity );
                defaultWorld = AddWorld( "main", null, true );
            }

            // if there is no default world still, die.
            if( defaultWorld == null ) {
                Logger.Log( "Could not create the default world!", LogType.FatalError );
                return false;
            }

            SaveWorldList();

            // open the port
            bool portFound = false;
            int attempts = 0;
            port = Config.GetInt( "Port" );

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

            Logger.Log( "Server.Run: now accepting connections at port {0}.", LogType.Debug,
                        port );

            serverStart = DateTime.Now;

            // list loaded worlds
            string line = "Available worlds: ";
            bool firstPrintedWorld = true;
            foreach( string worldName in Server.worlds.Keys ) {
                if( !firstPrintedWorld ) {
                    line += ", ";
                }
                line += worldName;
                firstPrintedWorld = false;
            }
            Logger.Log( line, LogType.SystemActivity );

            // Check for incoming connections 4 times per second
            AddTask( CheckConnections, 250 );

            // Check for idle people every 30 seconds
            AddTask( CheckIdles, 30000 );

            // Write out initial (empty) playerlist cache
            UpdatePlayerList();

            // start the main loop
            mainThread = new Thread( MainLoop );
            mainThread.Start();

            Heartbeat.Start();

            if( Config.GetBool( "IRCBot" ) ) IRCBot.Start();

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
            continueMainLoop = false;
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
            if( IRCBot.isOnline() == true ) IRCBot.ShutDown();

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

                newWorld.updateTaskId = AddTask( UpdateBlocks, Config.GetInt( "TickInterval" ), newWorld );

                if( Config.GetInt( "SaveInterval" ) > 0 ) {
                    int saveInterval = Config.GetInt( "SaveInterval" ) * 1000;
                    newWorld.saveTaskId = AddTask( SaveMap, saveInterval, newWorld, saveInterval );
                }

                if( Config.GetInt( "BackupInterval" ) > 0 ) {
                    int backupInterval = Config.GetInt( "BackupInterval" ) * 1000 * 60;
                    newWorld.backupTaskId = AddTask( AutoBackup, backupInterval, newWorld, ( Config.GetBool( "BackupOnStartup" ) ? 0 : backupInterval ) );
                }

                newWorld.UpdatePlayerList();

                return newWorld;
            }
        }


        public static void SaveWorldList() {
            // Save world list
            using( StreamWriter writer = File.CreateText( WorldListFile ) ) {
                lock ( worldListLock ) {
                    writer.WriteLine( defaultWorld.name );
                    foreach ( string worldName in worlds.Keys ) {
                        if ( worldName != defaultWorld.name ) {
                            writer.WriteLine( worldName );
                        }
                    }
                }
            }
        }


        public static World FindWorld( string name ) {
            if( name == null ) return null;
            lock( worldListLock ) {
                if ( worlds.ContainsKey( name.ToLower() ) ) {
                    return worlds[name.ToLower()];
                } else {
                    return null;
                }
            }
        }


        public static bool RemoveWorld( string name ) {
            lock ( worldListLock ) {
                World worldToDelete = FindWorld( name );
                if ( worldToDelete == null || worldToDelete == defaultWorld ) {
                    return false;
                } else {
                    Player[] worldPlayerList = worldToDelete.playerList;
                    worldToDelete.SendToAll( Color.Sys+"You have been moved to the default map." );
                    foreach( Player player in worldPlayerList ) {
                        player.session.forcedWorldToJoin = defaultWorld;
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
            lock ( worldListLock ) {
                World oldWorld = FindWorld( oldName );
                World newWorld = FindWorld( newName );
                if ( oldWorld == null || newWorld != null ) return false;
                worlds.Remove( oldName.ToLower() );
                oldWorld.name = newName;
                worlds.Add( newName.ToLower(), oldWorld );
                return true;
            }
        }


        public static bool ReplaceWorld( string name, World newWorld ) {
            lock ( worldListLock ) {
                World oldWorld = FindWorld( name );
                if ( oldWorld == null ) return false;

                newWorld.name = oldWorld.name;
                if ( oldWorld == defaultWorld ) {
                    defaultWorld = newWorld;
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
                newWorld.updateTaskId = AddTask( UpdateBlocks, Config.GetInt( "TickInterval" ), newWorld );

                if( Config.GetInt( "SaveInterval" ) > 0) {
                    int saveInterval = Config.GetInt( "SaveInterval" ) * 1000;
                    newWorld.saveTaskId = AddTask( SaveMap, saveInterval, newWorld, saveInterval );
                }

                if( Config.GetInt( "BackupInterval" ) > 0) {
                    int backupInterval = Config.GetInt( "BackupInterval" ) * 1000 * 60;
                    newWorld.backupTaskId = AddTask( AutoBackup, backupInterval, newWorld, ( Config.GetBool( "BackupOnStartup" ) ? 0 : backupInterval ) );
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
                    sessions.Add( new Session( defaultWorld, listener.AcceptTcpClient() ) );
                } catch( Exception ex ) {
                    Logger.Log( "ERROR: Could not accept incoming connection: " + ex.Message, LogType.Error );
                }
            }
            for( int i = 0; i < sessions.Count; i++ ) {
                if( sessions[i].canDispose ) {
                    sessions[i].Disconnect();
                    if( OnPlayerDisconnected != null ) OnPlayerDisconnected( sessions[i] );
                    Server.FirePlayerListChangedEvent();
                    sessions.RemoveAt( i );
                    i--;
                    Logger.Log( "Session disposed. Active sessions left: {0}.", LogType.Debug, sessions.Count );
                    GC.Collect();
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
                    list[i] = playerListCache[i].info.playerClass.name + " - " + playerListCache[i].name;
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
        static bool continueMainLoop = true;
        static object taskListLock = new object();

        internal static void MainLoop() {
            ScheduledTask[] taskCache;
            ScheduledTask task;
            while( continueMainLoop ) {
                taskCache = taskList;
                for( int i=0; i<taskCache.Length; i++){
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
            byte[] data = hasher.ComputeHash( Encoding.ASCII.GetBytes( Config.Salt + name ) );
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

        public static int htons( int value ) {
            return IPAddress.HostToNetworkOrder( value );
        }

        public static short htons( short value ) {
            return IPAddress.HostToNetworkOrder( value );
        }

        public static string PlayerListToString() {
            String players = "";
            foreach (Player player in playerList) {
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
                if( players.Count >= Config.GetInt( "MaxPlayers" ) ) {
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
            if( player == null ) {
                Logger.Log( "Server.UnregisterPlayer: Trying to unregister a non-existent (null) player.", LogType.Debug );
                return;
            }

            lock( playerListLock ) {
                if( players.ContainsKey( player.id ) ) {
                    SendToAll( PacketWriter.MakeRemoveEntity( player.id ) );
                    SendToAll( Color.Sys + player.name + " left the server." );
                    Logger.Log( "{0} left the server.", LogType.UserActivity, player.name );
                    
                    // better safe than sorry: go through ALL worlds looking for leftover players
                    lock ( worldListLock ) {
                        foreach ( World world in worlds.Values ) {
                            world.ReleasePlayer( player );
                        }
                    }

                    players.Remove( player.id );
                    UpdatePlayerList();
                    PlayerDB.ProcessLogout( player );
                    PlayerDB.Save();
                    return;
                }
            }

            Logger.Log( "World.UnregisterPlayer: Trying to unregister a non-existent (unknown id) player.", LogType.Warning );
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
        #endregion
    }
}