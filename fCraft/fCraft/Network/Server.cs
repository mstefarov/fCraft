// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
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
        static Player[] playerList;
        static object playerListLock = new object();
        public static object worldListLock = new object();

        const string WorldListFile = "worlds.txt";
        public static Dictionary<string, World> worlds = new Dictionary<string, World>();
        public static World defaultWorld;

        static TcpListener listener;

        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          maxSessionPacketsPerTick = 128;
        internal static float ticksPerSecond; //TODO: move to server

        const int maxPortAttempts = 20;


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
                    World world = AddWorld( worldName, first );
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
                    defaultWorld = AddWorld( "main", true );
                }
            } else {
                Logger.Log( "Server.Start: No world list found. Creating default \"main\" world.", LogType.SystemActivity );
                defaultWorld = AddWorld( "main", true );
            }

            if( defaultWorld == null ) {
                Logger.Log( "Could not create the default world!", LogType.FatalError );
                return false;
            }
            SaveWorldList();

            bool portFound = false;
            int attempts = 0;
            int port = Config.GetInt( "Port" );

            do {
                try {
                    listener = new TcpListener( IPAddress.Any, port );
                    listener.Start();
                    portFound = true;
                } catch( Exception ex ) {
                    Logger.Log( "Could not start listening on port {0}, trying next port. ({1})", LogType.Error,
                                   port, ex.Message );
                    port++;
                    attempts++;
                }
            } while( !portFound && attempts < maxPortAttempts );

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

            // queue up some tasks to run on the scheduler
            int saveMapTaskId, autoBackupTaskId;
            AddTask( CheckConnections, 250 );
            foreach( World world in worlds.Values ) {
                AddTask( UpdateBlocks, Config.GetInt( "TickInterval" ), world );


                if( Config.GetInt( "SaveInterval" ) > 0 ) {
                    int saveInterval = Config.GetInt( "SaveInterval" ) * 1000;
                    saveMapTaskId = AddTask( SaveMap, saveInterval, world, saveInterval );
                }

                if( Config.GetInt( "BackupInterval" ) > 0 ) {
                    int backupInterval = Config.GetInt( "BackupInterval" ) * 1000 * 60;
                    autoBackupTaskId = AddTask( AutoBackup, backupInterval, world, (Config.GetBool( "BackupOnStartup" ) ? 0 : backupInterval) );
                }

                AddTask(CheckIdles, 30000); // Check for idle people every 30 seconds

                world.UpdatePlayerList();
            }

            UpdatePlayerList();

            mainThread = new Thread( MainLoop );
            mainThread.Start();

            Heartbeat.Start();

            if( Config.GetBool( "IRCBot" ) ) IRCBot.Start();

            if( OnStart != null ) OnStart();
            return true;
        }

        
        // shuts down the server and aborts threads
        // NOTE: heartbeat should stop automatically
        public static void Shutdown() {
            if( OnShutdownStart != null ) OnShutdownStart();
            if( listener != null ) {
                listener.Stop();
                listener = null;
            }

            Logger.Log( "Server shutting down.", LogType.SystemActivity );
            continueMainLoop = false;
            if( mainThread != null && mainThread.IsAlive ) {
                mainThread.Join();
            }

            Heartbeat.ShutDown();
            if( IRCBot.isOnline() == true ) IRCBot.ShutDown();
            Tasks.ShutDown();

            lock( playerListLock ) {
                foreach( Session session in sessions ) {
                    session.Kick( "Server shutting down." );
                }
            }

            foreach( World world in worlds.Values ) {
                world.Shutdown();
            }

            PlayerDB.Save();
            IPBanList.Save();
            if( OnShutdownEnd != null ) OnShutdownEnd();
        }


        public static World AddWorld( string name, bool neverUnload ) {
            lock( worldListLock ) {
                if( worlds.ContainsKey( name ) ) return null;
                if( !Player.IsValidName( name ) ) return null;
                World newWorld = new World( name );
                newWorld.neverUnload = neverUnload;
                if( neverUnload ) newWorld.LoadMap();
                worlds.Add( name, newWorld );
                return newWorld;
            }
        }


        public static void SaveWorldList() {
            // Save world list
            using( StreamWriter writer = File.CreateText( WorldListFile ) ) {
                writer.WriteLine( defaultWorld.name );
                foreach( string worldName in worlds.Keys ) {
                    if( worldName != defaultWorld.name ) {
                        writer.WriteLine( worldName );
                    }
                }
            }
        }


        public static World FindWorld( string name ) {
            if( name == null ) return null;
            lock( worldListLock ) {
                foreach( World world in worlds.Values ) {
                    if( world.name.ToLowerInvariant() == name.ToLowerInvariant() ) {
                        return world;
                    }
                }
            }
            return null;
        }


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
                if( OnPlayerDisconnected != null ) OnPlayerDisconnected( sessions[i] );
                if( sessions[i].canDispose ) {
                    sessions[i].Disconnect();
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
        #endregion


        #region Scheduler

        static int updateTaskCounter = 0;
        static Dictionary<int, ScheduledTask> updateTasks = new Dictionary<int, ScheduledTask>();
        static Thread mainThread;
        static DateTime serverStart;
        static bool continueMainLoop = true;


        internal static void MainLoop() {
            while( continueMainLoop ) {
                foreach( ScheduledTask task in updateTasks.Values ) {
                    if( task.enabled && task.nextTime < DateTime.UtcNow ) {
                        task.callback( task.param );
                        task.nextTime += TimeSpan.FromMilliseconds( task.interval );
                    }
                }

                /*if( requestLockDown ) { //TODO
                    lockDown = true;
                    Tasks.Restart();
                    requestLockDown = false;
                    Thread.Sleep( 100 ); // buffer time for all threads to catch up
                    map.ClearUpdateQueue();
                    lockDownReady = true;
                }*/

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
            updateTasks.Add( updateTaskCounter, newTask );
            return updateTaskCounter++;
        }

        internal static void TaskToggle( int id, bool enabled ) {
            updateTasks[id].nextTime = DateTime.UtcNow;
            updateTasks[id].enabled = enabled;
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

            int playerCount = sessions.Count;
            if( playerCount > 0 ) {
                maxPacketsPerUpdate /= playerCount;
                if( maxPacketsPerUpdate > packetsPerTick ) {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            } else {
                maxPacketsPerUpdate = Int32.MaxValue;
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
            foreach (Player plyr in playerList) {
                players += plyr.name + ",";
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
                    Logger.Log( "{0} left the server.", LogType.UserActivity, player.name );

                    // if IRC Bot is online, send update to IRC bot
                    /*if (ircbot.isOnline() == true) //TODO: IRC
                    {
                        ircbot.SendMsgChannel(player.name + "(" + player.info.playerClass.name + ") has left ** " + Config.GetString("ServerName") + " **");
                    }*/

                    if( player.world != null ) {
                        player.world.ReleasePlayer( player );
                    }

                    SendToAll( PacketWriter.MakeRemoveEntity( player.id ) );
                    SendToAll( Color.Sys + player.name + " left the server." );
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
        #endregion
    }
}