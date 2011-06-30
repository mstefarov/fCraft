/*
 *  Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;


namespace fCraft {
    public delegate void LogHandler( string message, LogType type );
    public delegate void MessageEventHandler( string message );
    public delegate void PlayerListChangeHandler( string[] message );
    public delegate void VoidEventHandler();

    public class World {
        public Server server;
        public Map map;
        public Logger log;
        public Config config;
        public Heartbeat heartbeat;
        public Tasks tasks;
        public PlayerDB db;
        public IPBanList bans;
        public Commands cmd;
        public ClassList classes;

        public Player[] players;
        public string path;

        int playerCount;
        object playerListLock = new object();

        internal bool requestLockDown, lockDown, lockDownReady;
        internal bool loadInProgress, loadSendingInProgress, loadProgressReported;
        internal int totalBlockUpdates, completedBlockUpdates;
        bool firstBackup = true;

        // events
        //public event MessageEventHandler OnOutput;
        //public event MessageEventHandler OnError;
        public event PlayerListChangeHandler OnPlayerListChange;
        //public event MessageEventHandler OnClassChange;
        //public event MessageEventHandler OnMapChange;
        public event MessageEventHandler OnURLChange;
        public event LogHandler OnLog;

        internal void FireURLChange( string URL ) {
            if( OnURLChange != null ) OnURLChange( URL );
        }
        internal void FireLog( string message, LogType type ) {
            if( OnLog != null ) OnLog( message, type );
        }


        public World( string _path ) {
            path = _path;
            Color.Init();
            Map.Init();

            // start the logger
            log = new Logger( this );

            // load config
            classes = new ClassList( this );
            config = new Config( this, classes, log );
            
            // start tasks service
            tasks = new Tasks();

            db = new PlayerDB( this );
            bans = new IPBanList( this );

            cmd = new Commands( this );
            heartbeat = new Heartbeat( this );
        }


        public void LoadMap( string mapName ) {
            try {
                map = Map.Load( this, mapName );
            } catch( Exception ex ) {
                log.Log( "Could not open the specified file ({0}): {1}", LogType.Error, mapName, ex.Message );
            }

            // or generate a default one
            if( map == null ) {
                log.Log( "World.Init: Generating default flatgrass level.", LogType.SystemActivity );
                map = new Map( this, 64, 64, 64 );

                map.spawn.Set( map.widthX / 2 * 32 + 16, map.widthY / 2 * 32 + 16, map.height * 32, 0, 0 );

                MapCommands.GenerateFlatgrass( map, false );
                
                if( !map.Save() ) throw new Exception( "Could not save file." );
            }
        }


        public bool Init() {
            log.Init( "fCraft.log" );
            if( !config.Load( "config.xml" ) ) return false;
            config.ApplyConfig();
            config.Save( "config.xml" );

            // allocate player list
            players = new Player[config.GetInt( "MaxPlayers" ) + 1];
            tasks.Init();

            // load player DB
            db.Load();
            bans.Load();

            return true;
        }
        

        // World initialization
        // NOTE: Logger, Config, and Color are initialized by now
        public bool Start() {
            Player.Console = new Player( this, "(console)" );

            // start listening
            server = new Server( this );

            if( !server.Start() ) {
                return false;
            }

            serverStart = DateTime.Now;

            // queue up some tasks to run on the scheduler
            AddTask( server.CheckForIncomingConnections, 0 );
            AddTask( UpdateBlocks, config.GetInt( "TickInterval" ) );
            saveMapTaskId = AddTask( SaveMap, config.GetInt( "SaveInterval" ) * 1000 );
            TaskToggle( saveMapTaskId, config.GetInt( "SaveInterval" ) > 0 );
            autoBackupTaskId = AddTask( AutoBackup, config.GetInt( "BackupInterval" ) * 1000 * 60 );
            TaskToggle( autoBackupTaskId, config.GetInt( "BackupInterval" ) > 0 );

            mainThread = new Thread( Update );
            mainThread.Start();
            heartbeat.Start();
            return true;
        }


        int saveMapTaskId, autoBackupTaskId;

        internal int AddTask(  Task task, int interval ){
            UpdateTask newTask = new UpdateTask();
            newTask.nextTime = DateTime.Now;
            newTask.callback = task;
            newTask.interval = interval;
            updateTasks.Add( updateTaskCounter, newTask );
            return updateTaskCounter++;
        }

        internal void TaskToggle( int id, bool enabled ) {
            updateTasks[id].nextTime = DateTime.Now;
            updateTasks[id].enabled = enabled;
        }


        // === Main Loop ======================================================

        internal float ticksPerSecond;

        class UpdateTask {
            public DateTime nextTime;
            public int interval;
            public Task callback;
            public object param = null;
            public bool enabled = true;
        }
        int updateTaskCounter = 0;
        Dictionary<int, UpdateTask> updateTasks = new Dictionary<int, UpdateTask>();
        Thread mainThread;
        DateTime serverStart;
        bool keepGoing = true;


        internal void Update() {
            while( keepGoing ) {
                foreach( UpdateTask task in updateTasks.Values ) {
                    if( task.enabled && task.nextTime < DateTime.Now ) {
                        task.callback( task.param );
                        task.nextTime += TimeSpan.FromMilliseconds( task.interval );
                    }
                }

                if( requestLockDown ) {
                    lockDown = true;
                    tasks.Restart();
                    requestLockDown = false;
                    Thread.Sleep( 100 ); // buffer time for all threads to catch up
                    map.ClearUpdateQueue();
                    lockDownReady = true;
                }

                Thread.Sleep( 1 );
            }
        }

        void AutoBackup( object param ) {
            if( lockDown ) return;
            if( !firstBackup || config.GetBool( "BackupOnStartup" ) ) {
                map.SaveBackup( String.Format( "backups/{0:yyyy-MM-ddTHH-mm}.fcm", DateTime.Now ) );
            }
            firstBackup = false;
        }

        void SaveMap( object param ) {
            if( lockDown ) return;
            if( map.changesSinceSave > 0 ) {
                tasks.Add(
                    delegate {
                        map.changesSinceSave = 0;
                        map.Save();
                    }, null, false );
            }
        }

        void UpdateBlocks( object param ) {
            map.ProcessUpdates();
        }


        // Warning: do NOT call this from Tasks threads
        internal void BeginLockDown() {
            requestLockDown = true;
            if( Thread.CurrentThread == mainThread ) {
                lockDown = true;
                tasks.Restart();
                requestLockDown = false;
                Thread.Sleep( 100 ); // buffer time for all threads to catch up
                map.ClearUpdateQueue();
                lockDownReady = true;
            } else {
                while( !lockDownReady ) Thread.Sleep( 1 );
            }
        }

        internal void EndLockDown() {
            lockDownReady = false;
            lockDown = false;
        }


        // === Player list handling ===========================================

        // Add a newly-logged-in player to the list, and notify existing players.
        public bool RegisterPlayer( Player player ) {
            lock( playerListLock ) {
                for( int i = 1; i < players.Length; i++ ) {
                    if( players[i] == null ) {
                        player.id = i;
                        players[i] = player;
                        playerCount++;
                        if( config.GetBool( "BackupOnJoin" ) ) {
                            map.SaveBackup( String.Format( "backups/{0:yyyy-MM-dd HH-mm}_{1}.fcm", DateTime.Now, player.name ) );
                        }
                        UpdatePlayerList();
                        return true;
                    }
                }
                return false;
            }
        }


        // Remove player from the list, and notify remaining players
        public void UnregisterPlayer( Player player ) {
            if( player == null ) {
                log.Log( "World.UnregisterPlayer: Trying to unregister a non-existent (null) player.", LogType.Debug );
                return;
            }

            lock( playerListLock ) {
                if( players[player.id] == player ) {
                    log.Log( "{0} left the server.", LogType.UserActivity, player.name );
                    db.ProcessLogout( player );
                    db.Save();
                    players[player.id] = null;
                    playerCount--;
                    SendToAll( PacketWriter.MakeRemoveEntity( player.id ), null );
                    SendToAll( PacketWriter.MakeMessage( Color.Sys + player.name + " left the server." ), null );
                    UpdatePlayerList();
                }else{
                    log.Log( "World.UnregisterPlayer: Trying to unregister a non-existent (unknown id) player.", LogType.Warning );
                }
            }
        }


        // Send a list of players to the specified new player
        internal void SendPlayerList( Player player ) {
            Player temp;
            for( int i = 1; i < players.Length; i++ ) {
                temp = players[i];
                if( temp != null && temp != player && !temp.isHidden ) {
                    player.session.SendNow( PacketWriter.MakeAddEntity( temp, temp.pos ) );
                }
            }
        }


        internal void UpdatePlayer( Player updatedPlayer ) {
            Player player;
            for( int i = 1; i < players.Length; i++ ) {
                player = players[i];
                if( player != null && player != updatedPlayer ) {
                    player.Send( PacketWriter.MakeRemoveEntity( updatedPlayer.id ) );
                    player.Send( PacketWriter.MakeAddEntity( updatedPlayer, updatedPlayer.pos ) );
                }
            }
        }


        public string GetPlayerListString() {
            Player player;
            string list="";
            for( int i = 1; i < players.Length; i++ ) {
                player = players[i];
                if( player != null && !player.isHidden ) {
                    list += player.name + ",";
                }
            }
            if( list.Length > 0 ) {
                return list.Substring( 0, list.Length - 1 );
            } else {
                return list;
            }
        }


        // Find player by name using autocompletion
        public Player FindPlayer( string name ) {
            if( name == null ) return null;
            Player result = null, player;
            for( int i = 1; i < players.Length; i++ ) {
                player = players[i];
                if( player != null && player.name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if( result == null ) {
                        result = player;
                    } else {
                        return null;
                    }
                }
            }
            return result;
        }


        // Find player by name using autocompletion
        public Player FindPlayer( System.Net.IPAddress ip ) {
            Player player;
            for( int i = 1; i < players.Length; i++ ) {
                player = players[i];
                if( player != null && player.session.GetIP().ToString() == ip.ToString() ) {
                    return player;
                }
            }
            return null;
        }


        // Get player by name without autocompletion
        public Player FindPlayerExact( string name ) {
            Player player;
            for( int i = 1; i < players.Length; i++ ) {
                player = players[i];
                if( player != null && player.name == name ) {
                    return player;
                }
            }
            return null;
        }


        // Return player count
        public int GetPlayerCount() {
            return playerCount;
        }


        // Disconnect all players
        public void ShutDown() {
            try {
                log.Log( "Server shutting down.", LogType.SystemActivity );
                keepGoing = false;
                if( mainThread != null && mainThread.IsAlive ) {
                    mainThread.Join();
                }

                if( heartbeat != null ) heartbeat.ShutDown();
                if( tasks != null ) tasks.ShutDown();

                lock( playerListLock ) {
                    for( int i = 1; i < players.Length; i++ ) {
                        if( players[i] != null ) {
                            players[i].session.Kick( "Server shutting down." );
                        }
                        players[i] = null;
                    }
                    playerCount = 0;
                }

                if( config.GetBool( "SaveOnShutdown" ) && map != null ) {
                    map.Save();
                }

                if( db != null ) db.Save();
                if( bans != null ) bans.Save();
                if( server != null ) server.ShutDown();
            } catch( Exception ex ) {
                log.Log( "Error occured while trying to shut down: {0}", LogType.FatalError, ex.Message );
            }
        }


        // === Messaging ======================================================

        // Broadcast
        public void SendToAll( string message, Player except ) {
            SendToAll( PacketWriter.MakeMessage(message), except, true );
        }

        public void SendToAll( Packet packet, Player except ) {
            SendToAll( packet, except, true );
        }

        public void SendToAll( Packet packet, Player except, bool isHighPriority ) {
            Player player;
            for( int i = 1; i < players.Length; i++ ) {
                player = players[i];
                if( player != null && player != except ) {
                    player.Send( packet, isHighPriority );
                }
            }
        }

        public void SendToAll( string message, string prefix, Player except, bool isHighPriority ) {
            if( message.Length <= 64 ) {
                SendToAll( PacketWriter.MakeMessage( message ), except, isHighPriority );
            } else {
                string[] words = message.Split( ' ' );
                int ll = 0, j = 0;
                bool first = true;
                for( int i = 0; i < words.Length; i++) {
                    if( ll + words[i].Length > 64 ) {
                        if( first ) {
                            SendToAll( PacketWriter.MakeMessage( String.Join( " ", words, j, i-j-1 ) ), null, isHighPriority );
                            first = false;
                        } else {
                            SendToAll( PacketWriter.MakeMessage( prefix + String.Join( " ", words, j, i - j-1 ) ), null, isHighPriority );
                        }
                        j = i;
                        ll = prefix.Length;
                        i--;
                    } else {
                        ll += words[i].Length + 1;
                    }
                }
                if( ll != prefix.Length ) {
                }
            }
        }

        // Broadcast to a specific class
        public void SendToClass( Packet packet, PlayerClass playerClass ) {
            Player player;
            for( int i = 1; i < players.Length; i++ ) {
                player = players[i];
                if( player != null && player.info.playerClass == playerClass ) {
                    player.Send( packet, true );
                }
            }
        }


        internal void NoPlayerMessage( Player player, string name ) {
            player.Message( "No players found matching \"" + name + "\"" );
        }


        internal void ManyPlayersMessage( Player player, string name ) {
            player.Message( "More than one player found matching \"" + name + "\"" );
        }


        internal void NoAccessMessage( Player player ) {
            player.Message( Color.Red, "You do not have access to this command." );
        }

        public void UpdatePlayerList() {
            List<string> playerList = new List<string>();
            Player p;
            for( int i = 1; i < players.Length; i++ ) {
                p = players[i];
                if( p != null ) playerList.Add( p.info.playerClass.name + " - " + p.name );
            }
            if( OnPlayerListChange != null ) OnPlayerListChange( playerList.ToArray() );
        }
    }
}