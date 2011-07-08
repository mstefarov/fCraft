// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;


namespace fCraft {

    public sealed class World : IClassy {

        [Obsolete]
        public static readonly string[] BackupEnum = new[] {
            "Never", "5 Minutes", "10 Minutes", "15 Minutes", "20 Minutes",
            "30 Minutes", "45 Minutes", "1 Hour", "2 Hours", "3 Hours",
            "4 Hours", "6 Hours", "8 Hours", "12 Hours", "24 Hours"
        };

        public Map Map;
        public string Name;
        readonly SortedDictionary<string, Player> playerIndex = new SortedDictionary<string, Player>();
        public Player[] Players { get; private set; }
        public bool IsLocked,
                    IsHidden,
                    PendingUnload,
                    IsFlushing;
        public bool NeverUnload { get; private set; }
        public SecurityController AccessSecurity = new SecurityController(),
                                  BuildSecurity = new SecurityController();

        public string LockedBy, UnlockedBy;
        public DateTime LockedDate, UnlockedDate;

        readonly object lockLock = new object(),
                        patrolLock = new object();

        internal readonly object WorldLock = new object();


        internal World( string name, bool neverUnload ) {
            if( name == null ) {
                throw new ArgumentException( "name" );
            }
            if( !IsValidName( name ) ) {
                throw new ArgumentException( "Incorrect world name format" );
            }
            Name = name;
            NeverUnload = neverUnload;
            UpdatePlayerList();
        }


        #region Map

        public bool IsLoaded {
            get { return Map != null; }
        }


        public Map EnsureMapLoaded() {
            Map map = Map;
            if( map != null ) {
                return map;
            } else {
                lock( WorldLock ) {
                    LoadMap();
                    return Map;
                }
            }
        }

        
        public void LoadMap() {
            lock( WorldLock ) {
                if( Map != null ) return;

                    try {
                        Map = MapUtility.Load( GetMapName() );
                    } catch( Exception ex ) {
                        Logger.Log( "World.LoadMap: Failed to load map ({0}): {1}", LogType.Error,
                                    GetMapName(), ex );
                    }

                // or generate a default one
                if( Map != null ) {
                    Map.World = this;
                } else {
                    Logger.Log( "World.LoadMap: Generating default flatgrass level.", LogType.SystemActivity );
                    Map = MapGenerator.GenerateFlatgrass( 128, 128, 64 );
                }
                StartTasks();

                if( OnLoaded != null ) OnLoaded();
            }
        }


        public void UnloadMap( bool expectedPendingFlag ) {
            lock( WorldLock ) {
                if( expectedPendingFlag != PendingUnload ) return;
                SaveMap();
                Map = null;
                StopTasks();
                PendingUnload = false;
                if( OnUnloaded != null ) OnUnloaded();
            }
            Server.RequestGC();
        }


        public string GetMapName() {
            return Path.Combine( Paths.MapPath, Name + ".fcm" );
        }


        public void SaveMap() {
            lock( WorldLock ) {
                if( Map != null ) {
                    Map.Save( GetMapName() );
                }
            }
        }


        public void ChangeMap( Map newMap ) {
            if( newMap == null ) throw new ArgumentNullException( "newMap" );
            lock( WorldLock ) {
                World newWorld = new World( Name, NeverUnload ) {
                    Map = newMap,
                    AccessSecurity = (SecurityController)AccessSecurity.Clone(),
                    BuildSecurity = (SecurityController)BuildSecurity.Clone(),
                    IsHidden = IsHidden
                };
                newMap.World = newWorld;
                WorldManager.ReplaceWorld( this, newWorld );
                Map = null;
                foreach( Player player in Players ) {
                    player.JoinWorld( newWorld );
                }
            }
        }


        public void ToggleNeverUnloadFlag( bool newValue ) {
            lock( WorldLock ) {
                if( NeverUnload == newValue ) return;
                NeverUnload = newValue;
                if( NeverUnload ) {
                    if( Map == null ) LoadMap();
                } else {
                    if( Map != null && playerIndex.Count == 0 ) UnloadMap( false );
                }
            }
        }


        public void BeginFlushMapBuffer() {
            lock( WorldLock ) {
                if( Map == null ) return;
                SendToAll( "&WMap is being flushed. Stay put, world will reload shortly." );
                IsFlushing = true;
            }
        }


        public void EndFlushMapBuffer() {
            lock( WorldLock ) {
                IsFlushing = false;
                SendToAll( "&WMap flushed. Reloading..." );
                foreach( Player player in Players ) {
                    player.JoinWorld( this, player.Position );
                }
            }
        }


        #endregion


        #region PlayerList

        public Map AcceptPlayer( Player player, bool announce ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( WorldLock ) {

                if( IsFull ) {
                    return null;
                }

                if( playerIndex.ContainsKey( player.Name.ToLower() ) ) {
                    Logger.Log( "This world already contains the player by name ({0}). " +
                                "Some sort of state corruption must have occured.", LogType.Error,
                                player.Name );
                    playerIndex.Remove( player.Name.ToLower() );
                }

                playerIndex.Add( player.Name.ToLower(), player );

                // load the map, if it's not yet loaded
                PendingUnload = false;
                if( Map == null ) {
                    LoadMap();
                }

                if( ConfigKey.BackupOnJoin.Enabled() ) {
                    string backupFileName = String.Format( "{0}_{1:yyyy-MM-dd_HH-mm}_{2}.fcm",
                                                           Name, DateTime.Now, player.Name ); // localized
                    Map.SaveBackup( Path.Combine( Paths.MapPath, GetMapName() ),
                                    Path.Combine( Paths.BackupPath, backupFileName ),
                                    true );
                }

                AddPlayerForPatrol( player );

                UpdatePlayerList();

                if( announce && ConfigKey.ShowJoinedWorldMessages.Enabled() ) {
                    Server.Players.CanSee( player ).Message( "&SPlayer {0}&S joined {1}",
                                                             player.ClassyName, ClassyName );
                }

                Logger.Log( "Player {0} joined world {1}.", LogType.UserActivity,
                            player.Name, Name );

                if( IsLocked ) {
                    player.Message( "&WThis map is currently locked (read-only)." );
                }

                if( player.IsHidden ) {
                    player.Message( "Reminder: You are still hidden." );
                }

                return Map;
            }
        }


        public bool ReleasePlayer( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( WorldLock ) {
                if( !playerIndex.Remove( player.Name.ToLower() ) ) {
                    return false;
                }

                RemovePlayerFromPatrol( player );

                // clear undo & selection
                player.UndoBuffer.Clear();
                player.UndoBuffer.TrimExcess();
                player.SelectionCancel();

                // update player list
                UpdatePlayerList();

                // unload map (if needed)
                if( playerIndex.Count == 0 && !NeverUnload ) {
                    PendingUnload = true;
                }
                return true;
            }
        }


        // Find player by name using autocompletion
        public Player FindPlayer( string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Player[] tempList = Players;
            Player result = null;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].Name.StartsWith( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( result == null ) {
                        result = tempList[i];
                    } else {
                        return null;
                    }
                }
            }
            return result;
        }


        public Player[] FindPlayers( Player player, string playerName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Player[] tempList = Players;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && player.CanSee( tempList[i] ) ) {
                    if( tempList[i].Name.Equals( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( tempList[i] );
                        break;
                    } else if( tempList[i].Name.StartsWith( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Add( tempList[i] );
                    }
                }
            }
            return results.ToArray();
        }


        /// <summary> Gets player by name (without autocompletion) </summary>
        public Player FindPlayerExact( string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Player[] tempList = Players;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].Name.Equals( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                    return tempList[i];
                }
            }
            return null;
        }


        /// <summary> Caches the player list to an array (Players -> PlayerList) </summary>
        public void UpdatePlayerList() {
            lock( WorldLock ) {
                Player[] newPlayerList = new Player[playerIndex.Count];
                int i = 0;
                foreach( Player player in playerIndex.Values ) {
                    newPlayerList[i++] = player;
                }
                Players = newPlayerList;
            }
        }


        /// <summary> Counts all players (optionally includes all hidden players). </summary>
        public int CountPlayers( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return Players.Length;
            } else {
                return Players.Count( player => !player.IsHidden );
            }
        }


        /// <summary> Counts only the players who are not hidden from a given observer. </summary>
        public int CountVisiblePlayers( Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return Players.Count( observer.CanSee );
        }


        public bool IsFull {
            get {
                return (Players.Length >= ConfigKey.MaxPlayersPerWorld.GetInt());
            }
        }

        #endregion


        #region Communication

        public void SendToAll( Packet packet ) {
            SendToAll( packet, null );
        }


        public void SendToAll( Packet packet, Player except ) {
            Player[] tempList = Players;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].Send( packet );
                }
            }
        }


        public void SendToAllDelayed( Packet packet, Player except ) {
            Player[] tempList = Players;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].SendLowPriority( packet );
                }
            }
        }

        public void SendToAll( string message, params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) message = String.Format( message, args );
            foreach( Packet p in LineWrapper.Wrap( message ) ) {
                SendToAll( p, null );
            }
        }

        public void SendToAllExcept( string message, Player except, params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) message = String.Format( message, args );
            foreach( Packet p in LineWrapper.Wrap( message ) ) {
                SendToAll( p, except );
            }
        }


        public void SendToSeeing( Packet packet, Player source ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            Player[] playerListCopy = Players;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }

        public void SendToBlind( Packet packet, Player source ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            Player[] playerListCopy = Players;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && !playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }

        #endregion


        #region Obsolete Events
        [Obsolete]
        public event Action OnLoaded;
        [Obsolete]
        public event Action OnUnloaded;
        [Obsolete]
        public event PlayerChangedBlockEventHandler OnPlayerChangedBlock;

        public bool FireChangedBlockEvent( ref BlockUpdate update ) {
            bool cancel = false;
            if( OnPlayerChangedBlock != null ) {
                OnPlayerChangedBlock( this, ref update, ref cancel );
            }
            return !cancel;
        }

        #endregion


        #region Lock / Unlock

        public bool Lock( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( lockLock ) {
                if( IsLocked ) {
                    return false;
                } else {
                    LockedBy = player.Name;
                    LockedDate = DateTime.UtcNow;
                    IsLocked = true;
                    if( Map != null ) Map.ClearUpdateQueue();
                    SendToAll( "&WMap was locked by {0}", player.ClassyName );
                    Logger.Log( "World {0} was locked by {1}", LogType.UserActivity,
                                Name, player.Name );
                    return true;
                }
            }
        }


        public bool Unlock( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( lockLock ) {
                if( IsLocked ) {
                    UnlockedBy = player.Name;
                    UnlockedDate = DateTime.UtcNow;
                    IsLocked = false;
                    SendToAll( "&WMap was unlocked by {0}", player.ClassyName );
                    Logger.Log( "World \"{0}\" was unlocked by {1}", LogType.UserActivity,
                                Name, player.Name );
                    return true;
                } else {
                    return false;
                }
            }
        }

        #endregion


        #region Patrol

        readonly LinkedList<Player> patrolList = new LinkedList<Player>();


        public Player GetNextPatrolTarget( Player except ) {
            lock( patrolLock ) {
                if( patrolList.Count == 0 ) {
                    return null;
                } else {
                    var node = patrolList.First;
                    Player target = node.Value;

                    while( target == except || target.LastPatrolTime > target.LastActiveTime ) {
                        node = node.Next;
                        if( node == null ) return null;
                        target = node.Value;
                    }

                    patrolList.RemoveFirst();
                    target.LastPatrolTime = DateTime.UtcNow;
                    patrolList.AddLast( target );
                    return target;
                }
            }
        }


        public Player GetNextPatrolTarget() {
            return GetNextPatrolTarget( null );
        }


        void RemovePlayerFromPatrol( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( patrolLock ) {
                if( patrolList.Contains( player ) ) {
                    patrolList.Remove( player );
                }
            }
        }


        void AddPlayerForPatrol( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Rank rankToPatrol = RankManager.ParseRank( ConfigKey.PatrolledRank.GetString() );
            if( player.Info.Rank <= rankToPatrol ) {
                lock( patrolLock ) {
                    patrolList.AddLast( player );
                }
            }
        }


        internal void CheckIfPlayerIsPatrollable( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Rank rankToPatrol = RankManager.ParseRank( ConfigKey.PatrolledRank.GetString() );
            lock( patrolLock ) {
                if( patrolList.Contains( player ) ) {
                    if( player.Info.Rank > rankToPatrol ) {
                        RemovePlayerFromPatrol( player );
                    }
                } else if( player.Info.Rank <= rankToPatrol ) {
                    AddPlayerForPatrol( player );
                }
            }
        }

        #endregion


        #region Scheduled Tasks

        SchedulerTask updateTask, saveTask, backupTask;
        readonly object taskLock = new object();


        internal void StopTasks() {
            lock( taskLock ) {
                if( updateTask != null ) {
                    updateTask.Stop();
                    updateTask = null;
                }
                if( saveTask != null ) {
                    saveTask.Stop();
                    saveTask = null;
                }
                if( backupTask != null ) {
                    backupTask.Stop();
                    backupTask = null;
                }
            }
        }


        internal void StartTasks() {
            lock( taskLock ) {
                updateTask = Scheduler.NewTask( UpdateTask );
                updateTask.RunForever( this,
                                       TimeSpan.FromMilliseconds( ConfigKey.TickInterval.GetInt() ),
                                       TimeSpan.Zero );

                if( ConfigKey.SaveInterval.GetInt() > 0 ) {
                    saveTask = Scheduler.NewTask( SaveTask );
                    saveTask.RunForever( this,
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ),
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ) );
                }

                if( ConfigKey.BackupInterval.GetInt() > 0 ) {
                    backupTask = Scheduler.NewTask( BackupTask );
                    TimeSpan interval = TimeSpan.FromMinutes( ConfigKey.BackupInterval.GetInt() );
                    backupTask.RunForever( this,
                                           interval,
                                           (ConfigKey.BackupOnStartup.Enabled() ? TimeSpan.Zero : interval) );
                }
            }
        }


        void UpdateTask( SchedulerTask task ) {
            Map tempMap = Map;
            if( tempMap != null ) {
                tempMap.ProcessUpdates();
            }
        }


        void BackupTask( SchedulerTask task ) {
            Map tempMap = Map;
            if( tempMap != null ) {
                tempMap.SaveBackup( Path.Combine( Paths.MapPath, GetMapName() ),
                                    Path.Combine( Paths.BackupPath, String.Format( "{0}_{1:yyyy-MM-dd_HH-mm}.fcm", Name, DateTime.Now ) ), // localized
                                    true );
            }
        }


        void SaveTask( SchedulerTask task ) {
            Map tempMap = Map;
            if( tempMap != null && tempMap.HasChangedSinceSave ) {
                SaveMap();
            }
        }

        #endregion


        // ensures that player name has the correct length and character set
        public static bool IsValidName( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 16 ) return false;
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( ch < '0' || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }


        public string ClassyName {
            get {
                string displayedName = Name;
                if( ConfigKey.RankColorsInWorldNames.Enabled() ) {
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        displayedName = BuildSecurity.MinRank.Prefix + displayedName;
                    }
                    if( ConfigKey.RankColorsInChat.Enabled() ) {
                        if( BuildSecurity.MinRank >= AccessSecurity.MinRank ) {
                            displayedName = BuildSecurity.MinRank.Color + displayedName;
                        } else {
                            displayedName = AccessSecurity.MinRank.Color + displayedName;
                        }
                    }
                }
                return displayedName;
            }
        }


        public override string ToString() {
            return String.Format( "World({0})", Name );
        }
    }
}


namespace fCraft.Events {


    public sealed class WorldCreatingEventArgs : EventArgs, ICancellableEvent {
        public WorldCreatingEventArgs( Player player, string worldName, Map map ) {
            Player = player;
            WorldName = worldName;
            Map = map;
        }

        public Player Player { get; private set; }
        public string WorldName { get; set; }
        public Map Map { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class WorldCreatedEventArgs : EventArgs, IPlayerEvent, IWorldEvent {
        public WorldCreatedEventArgs( Player player, World world ) {
            Player = player;
            World = world;
        }

        public Player Player { get; private set; }
        public World World { get; private set; }
    }

}