// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;


namespace fCraft {

    public sealed class World : IClassy {

        const string TimedBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}.fcm",
                     JoinBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}_{2}.fcm";

        [Obsolete]
        public static readonly string[] BackupEnum = new[] {
            "Never", "5 Minutes", "10 Minutes", "15 Minutes", "20 Minutes",
            "30 Minutes", "45 Minutes", "1 Hour", "2 Hours", "3 Hours",
            "4 Hours", "6 Hours", "8 Hours", "12 Hours", "24 Hours"
        };

        /// <summary> World name (no formatting).
        /// Use WorldManager.RenameWorld() method to change this. </summary>
        public string Name { get; internal set; }


        /// <summary> Whether the world shows up on the /worlds list.
        /// Can be assigned directly. </summary>
        public bool IsHidden { get; set; }


        /// <summary> Whether this world is currently pending unload 
        /// (waiting for block updates to finish processing before unloading). </summary>
        public bool IsPendingMapUnload { get; private set; }


        public SecurityController AccessSecurity { get; internal set; }

        public SecurityController BuildSecurity { get; internal set; }


        // used to synchronize player joining/parting with map loading/saving
        internal readonly object WorldLock = new object();


        internal World( string name ) {
            if( name == null ) {
                throw new ArgumentException( "name" );
            }
            if( !IsValidName( name ) ) {
                throw new ArgumentException( "Incorrect world name format" );
            }
            AccessSecurity = new SecurityController();
            BuildSecurity = new SecurityController();
            Name = name;
            UpdatePlayerList();
        }


        #region Map

        /// <summary> Map of this world. May be null if world is not loaded. </summary>
        public Map Map {
            get { return map; }
            set {
                string msg;
                if( map == null && value == null ) {
                    return;
                } else if( value != null ) {
                    msg = "null to map";
                } else if( map != null ) {
                    msg = "map to null";
                } else {
                    msg = "map to map";
                }
                Logger.Log( "----" + GetHashCode() +": "+ msg + "----" + Environment.NewLine +  Environment.StackTrace, LogType.Debug );
                map = value;
            }
        }
        Map map;

        /// <summary> Whether the map is currently loaded. </summary>
        public bool IsLoaded {
            get { return Map != null; }
        }


        /// <summary> Loads the map file, if needed.
        /// Generates a default map if mapfile is missing or not loadable.
        /// Guaranteed to return a Map object. </summary>
        public Map LoadMap() {
            lock( WorldLock ) {
                if( Map != null ) return Map;

                if( File.Exists( MapFileName ) ) {
                    try {
                        Map = MapUtility.Load( MapFileName );
                    } catch( Exception ex ) {
                        Logger.Log( "World.LoadMap: Failed to load map ({0}): {1}", LogType.Error,
                                    MapFileName, ex );
                    }
                }

                // or generate a default one
                if( Map == null ) {
                    Server.Message( "&WMapfile is missing for world {0}&W. A new map has been created.", ClassyName );
                    Logger.Log( "World.LoadMap: Map file missing for world {0}. Generating default flatgrass map.", LogType.SystemActivity,
                                Name );
                    Map = MapGenerator.GenerateFlatgrass( 128, 128, 64 );
                }
                Map.World = this;
                StartTasks();

                return Map;
            }
        }


        public void UnloadMap( bool expectedPendingFlag ) {
            lock( WorldLock ) {
                if( expectedPendingFlag != IsPendingMapUnload ) return;
                SaveMap();
                Map = null;
                StopTasks();
                IsPendingMapUnload = false;
            }
            Server.RequestGC();
        }


        /// <summary> Returns the map filename, including MapPath. </summary>
        public string MapFileName {
            get {
                return Path.Combine( Paths.MapPath, Name + ".fcm" );
            }
        }


        public void SaveMap() {
            lock( WorldLock ) {
                if( Map != null ) {
                    Map.Save( MapFileName );
                }
            }
        }


        public void ChangeMap( Map newMap ) {
            if( newMap == null ) throw new ArgumentNullException( "newMap" );
            lock( WorldLock ) {
                World newWorld = new World( Name ) {
                    Map = newMap,
                    AccessSecurity = (SecurityController)AccessSecurity.Clone(),
                    BuildSecurity = (SecurityController)BuildSecurity.Clone(),
                    IsHidden = IsHidden
                };
                newMap.World = newWorld;
                newWorld.NeverUnload = neverUnload;
                WorldManager.ReplaceWorld( this, newWorld );
                foreach( Player player in Players ) {
                    player.JoinWorld( newWorld );
                }
                lock( blockDBLock ) {
                    pendingEntries.Clear();
                    File.Delete( BlockDBFile );
                }
            }
        }


        bool neverUnload;
        public bool NeverUnload {
            get {
                return neverUnload;
            }
            set {
                lock( WorldLock ) {
                    if( neverUnload == value ) return;
                    neverUnload = value;
                    if( neverUnload ) {
                        if( Map == null ) LoadMap();
                    } else {
                        if( Map != null && playerIndex.Count == 0 ) UnloadMap( false );
                    }
                }
            }
        }

        #endregion


        #region Flush

        public bool IsFlushing { get; private set; }

        public void Flush() {
            lock( WorldLock ) {
                if( Map == null ) return;
                Players.Message( "&WMap is being flushed. Stay put, world will reload shortly." );
                IsFlushing = true;
            }
        }


        internal void EndFlushMapBuffer() {
            lock( WorldLock ) {
                IsFlushing = false;
                Players.Message( "&WMap flushed. Reloading..." );
                foreach( Player player in Players ) {
                    player.JoinWorld( this, player.Position );
                }
            }
        }


        #endregion


        #region PlayerList

        readonly SortedDictionary<string, Player> playerIndex = new SortedDictionary<string, Player>();
        public Player[] Players { get; private set; }

        public Map AcceptPlayer( Player player, bool announce ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            if( IsFull && player.Info.Rank.ReservedSlot ) {
                Player idlestPlayer = Players.OrderBy( p => p.LastActiveTime ).FirstOrDefault();
                if( idlestPlayer != null ) {
                    idlestPlayer.Kick( "Auto-kicked to make room (idle).", LeaveReason.IdleKick );
                    idlestPlayer.WaitForDisconnect();
                }
            }

            lock( WorldLock ) {
                if( IsFull ) return null;

                if( playerIndex.ContainsKey( player.Name.ToLower() ) ) {
                    Logger.Log( "This world already contains the player by name ({0}). " +
                                "Some sort of state corruption must have occured.", LogType.Error,
                                player.Name );
                    playerIndex.Remove( player.Name.ToLower() );
                }

                playerIndex.Add( player.Name.ToLower(), player );

                // load the map, if it's not yet loaded
                IsPendingMapUnload = false;
                Map = LoadMap();

                if( ConfigKey.BackupOnJoin.Enabled() ) {
                    string backupFileName = String.Format( JoinBackupFormat,
                                                           Name, DateTime.Now, player.Name ); // localized
                    Map.SaveBackup( MapFileName,
                                    Path.Combine( Paths.BackupPath, backupFileName ),
                                    true );
                }

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

                if( player.Info.IsHidden ) {
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

                // clear undo & selection
                player.UndoBuffer.Clear();
                player.UndoBuffer.TrimExcess();
                player.SelectionCancel();

                // update player list
                UpdatePlayerList();

                // unload map (if needed)
                if( playerIndex.Count == 0 && !neverUnload ) {
                    IsPendingMapUnload = true;
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
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].Name.Equals( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                    return tempList[i];
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return null;
        }


        /// <summary> Caches the player list to an array (Players -> PlayerList) </summary>
        public void UpdatePlayerList() {
            lock( WorldLock ) {
                Players = playerIndex.Values.ToArray();
            }
        }


        /// <summary> Counts all players (optionally includes all hidden players). </summary>
        public int CountPlayers( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return Players.Length;
            } else {
                return Players.Count( player => !player.Info.IsHidden );
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


        #region Lock / Unlock

        /// <summary> Whether the world is currently locked (in read-only mode). </summary>
        public bool IsLocked { get; private set; }

        public string LockedBy, UnlockedBy;
        public DateTime LockedDate, UnlockedDate;

        readonly object lockLock = new object();


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
                    Players.Message( "&WMap was locked by {0}", player.ClassyName );
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
                    Players.Message( "&WMap was unlocked by {0}", player.ClassyName );
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

        readonly object patrolLock = new object();
        static readonly TimeSpan MinPatrolInterval = TimeSpan.FromSeconds( 20 );

        public Player GetNextPatrolTarget( Player observer ) {
            lock( patrolLock ) {
                Player candidate = Players.RankedAtMost( RankManager.PatrolledRank )
                                          .CanBeSeen( observer )
                                          .Where( p => p.LastActiveTime > p.LastPatrolTime &&
                                                       DateTime.UtcNow.Subtract( p.LastPatrolTime ) > MinPatrolInterval )
                                          .OrderBy( p => p.LastPatrolTime.Ticks )
                                          .FirstOrDefault();
                if( candidate != null ) {
                    candidate.LastPatrolTime = DateTime.UtcNow;
                }
                return candidate;
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
                string backupFileName = String.Format( TimedBackupFormat, Name, DateTime.Now ); // localized
                tempMap.SaveBackup( MapFileName,
                                    Path.Combine( Paths.BackupPath, backupFileName ),
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


        /// <summary> Ensures that player name has the correct length (2-16 characters)
        /// and character set (alphanumeric chars and underscores allowed). </summary>
        public static bool IsValidName( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 16 ) return false;
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( ch < '0' ||
                    ch > '9' && ch < 'A' ||
                    ch > 'Z' && ch < '_' ||
                    ch > '_' && ch < 'a' ||
                    ch > 'z' ) {
                    return false;
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return true;
        }


        /// <summary> Returns a nicely formatted name, with optional color codes. </summary>
        public string ClassyName {
            get {
                if( ConfigKey.RankColorsInWorldNames.Enabled() ) {
                    string displayedName = Name;
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        displayedName = BuildSecurity.MinRank.Prefix + displayedName;
                    }
                    if( BuildSecurity.MinRank >= AccessSecurity.MinRank ) {
                        displayedName = BuildSecurity.MinRank.Color + displayedName;
                    } else {
                        displayedName = AccessSecurity.MinRank.Color + displayedName;
                    }
                    return displayedName;
                } else {
                    return Name;
                }
            }
        }


        public override string ToString() {
            return String.Format( "World({0})", Name );
        }


        #region Block Tracking

        public bool IsBlockTracked { get; set; }
        internal readonly object blockDBLock = new object();
        List<BlockDBEntry> pendingEntries = new List<BlockDBEntry>();

        internal void AddBlockDBEntry( BlockDBEntry newEntry ) {
            lock( blockDBLock ) {
                pendingEntries.Add( newEntry );
            }
        }

        public string BlockDBFile {
            get {
                return Path.Combine( Paths.BlockDBPath, Name + ".fbdb" );
            }
        }

        internal void FlushBlockDB() {
            if( pendingEntries.Count > 0 ) {
                lock( blockDBLock ) {
                    using( var stream = File.Open( BlockDBFile, FileMode.Append, FileAccess.Write ) ) {
                        BinaryWriter writer = new BinaryWriter( stream );
                        for( int i = 0; i < pendingEntries.Count; i++ ) {
                            writer.Write( pendingEntries[i].Timestamp );
                            writer.Write( pendingEntries[i].PlayerID );
                            writer.Write( pendingEntries[i].X );
                            writer.Write( pendingEntries[i].Y );
                            writer.Write( pendingEntries[i].Z );
                            writer.Write( (byte)pendingEntries[i].OldBlock );
                            writer.Write( (byte)pendingEntries[i].NewBlock );
                        }
                    }
                    pendingEntries.Clear();
                }
            }
        }

        unsafe internal BlockDBEntry[] LookupBlockInfo( short x, short y, short z ) {
            byte[] bytes;

            lock( blockDBLock ) {
                FlushBlockDB();
                bytes = File.ReadAllBytes( BlockDBFile );
            }
            List<BlockDBEntry> results = new List<BlockDBEntry>();
            BlockDBEntry* entries;
            int entryCount = bytes.Length / BlockDB.BlockDBEntrySize;
            fixed( byte* parr = bytes ) {
                entries = (BlockDBEntry*)parr;
                for( int i = 0; i < entryCount; i++ ) {
                    if( entries[i].X == x && entries[i].Y == y && entries[i].Z == z ) {
                        results.Add( entries[i] );
                    }
                }
            }
            return results.ToArray();
        }

        #endregion
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