// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace fCraft {
    public static class Server {

        static List<Session> sessions = new List<Session>();
        static Dictionary<int, Player> players = new Dictionary<int, Player>( 255 );
        static Player[] playerList;
        static object playerListLock = new object();

        public static Dictionary<string, World> worlds = new Dictionary<string, World>();
        public static World defaultWorld;

        static TcpListener listener;

        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          maxSessionPacketsPerTick = 128;
        internal static float ticksPerSecond; //TODO: move to server

        public static IRCBot ircbot;
        //static bool IRCBotOnline;

        const int maxPortAttempts = 20;

        public static bool Init() {
            Color.Init();
            Map.Init();

            Logger.Init( "fCraft.log" );
            if( !Config.Load() ) return false;
            Config.ApplyConfig();
            if( !Config.Save() ) return false;

            if( Config.GetBool( "IRCBot" ) == true ) {
                //ircbot = new IRCBot();
                //IRCBotOnline = true;
            }

            // allocate player list
            Tasks.Init();

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            OnInit();

            return true;
        }


        // Opens a socket for listening for incoming connections
        public static bool Start() {

            //Player.Console = new Player( this, "(console)" ); //TODO

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

            int saveMapTaskId, autoBackupTaskId;

            // queue up some tasks to run on the scheduler
            AddTask( CheckConnections, 250 );
            AddTask( UpdateBlocks, Config.GetInt( "TickInterval" ) );
            saveMapTaskId = AddTask( SaveMap, Config.GetInt( "SaveInterval" ) * 1000 );
            TaskToggle( saveMapTaskId, Config.GetInt( "SaveInterval" ) > 0 );
            autoBackupTaskId = AddTask( AutoBackup, Config.GetInt( "BackupInterval" ) * 1000 * 60 );
            TaskToggle( autoBackupTaskId, Config.GetInt( "BackupInterval" ) > 0 );

            mainThread = new Thread( MainLoop );
            mainThread.Start();

            /*
            if( IRCBotOnline ) {
                ircbot.Start(); //TODO: IRC
            }
            */

            OnStart();

            //TODO: Config.GetBool( "BackupOnStartup" )

            return true;
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
                OnPlayerDisconnect( sessions[i] );
                if( sessions[i].canDispose ) {
                    sessions[i].Disconnect();
                    sessions.RemoveAt( i );
                    i--;
                    Logger.Log( "Session disposed. Active sessions left: {0}.", LogType.Debug, sessions.Count );
                    GC.Collect();
                }
            }
        }


        // shuts down the server and aborts threads
        // NOTE: heartbeat should stop automatically
        public static void Shutdown() {
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
            //if( IRCBotOnline == true ) ircbot.ShutDown();
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
        }


        // Return player count
        public static int GetPlayerCount() {
            return sessions.Count;
        }


        #region Events
        // events
        public static event SimpleEventHandler OnInit;
        public static event SimpleEventHandler OnStart;
        public static event ConnectionEventHandler OnPlayerConnect;
        public static event ConnectionEventHandler OnPlayerDisconnect;
        public static event MessageEventHandler OnClassChange;
        public static event MessageEventHandler OnURLChange;
        public static event MessageEventHandler OnShutdown;
        public static event LogEventHandler OnLog;

        internal static void FireURLChangeEvent( string URL ) {
            if( OnURLChange != null ) OnURLChange( URL );
        }
        internal static void FireLogEvent( string message, LogType type ) {
            if( OnLog != null ) OnLog( message, type );
        }
        internal static void FirePlayerConnectEvent( Session session ) {
            OnPlayerConnect( session );
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
                    if( task.enabled && task.nextTime < DateTime.Now ) {
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
            world.map.SaveBackup( String.Format( "backups/{0:yyyy-MM-ddTHH-mm}.fcm", DateTime.Now ) );
        }

        static void SaveMap( object param ) {
            World world = (World)param;
            if( world.map.changesSinceSave > 0 ) {
                Tasks.Add(
                    delegate {
                        world.map.Save();
                    }, null, false );
            }
        }

        static void UpdateBlocks( object param ) {
            World world = (World)param;
            world.map.ProcessUpdates();
        }

        internal static int AddTask( TaskCallback task, int interval ) {
            return AddTask( task, interval, null );
        }

        internal static int AddTask( TaskCallback task, int interval, object param ) {
            ScheduledTask newTask = new ScheduledTask();
            newTask.nextTime = DateTime.Now;
            newTask.callback = task;
            newTask.interval = interval;
            updateTasks.Add( updateTaskCounter, newTask );
            return updateTaskCounter++;
        }

        internal static void TaskToggle( int id, bool enabled ) {
            updateTasks[id].nextTime = DateTime.Now;
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

        #endregion

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
                    PlayerDB.ProcessLogout( player );
                    PlayerDB.Save();
                    SendToAll( PacketWriter.MakeRemoveEntity( player.id ) );
                    SendToAll( Color.Sys + player.name + " left the server." );
                    UpdatePlayerList();
                } else {
                    Logger.Log( "World.UnregisterPlayer: Trying to unregister a non-existent (unknown id) player.", LogType.Warning );
                }
            }
        }

        public static void UpdatePlayerList() {
            lock( playerListLock ) {
                foreach( Player player in players.Values ) {
                    Player[] newPlayerList = new Player[players.Count];
                    int i = 0;
                    foreach( Player p in players.Values ) {
                        playerList[i++] = p;
                    }
                    playerList = newPlayerList;
                }
            }
        }
    }
}