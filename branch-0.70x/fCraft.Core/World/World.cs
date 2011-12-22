// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using fCraft.MapConversion;
using JetBrains.Annotations;

namespace fCraft {
    public sealed class World : IClassy {
        /// <summary> World name (no formatting).
        /// Use WorldManager.RenameWorld() method to change this. </summary>
        [NotNull]
        public string Name { get; internal set; }


        /// <summary> Whether the world shows up on the /Worlds list. </summary>
        public bool IsHidden { get; set; }


        /// <summary> Whether this world is currently pending unload 
        /// (waiting for block updates to finish processing before unloading). </summary>
        public bool IsPendingMapUnload { get; private set; }


        [NotNull]
        public SecurityController AccessSecurity { get; internal set; }

        [NotNull]
        public SecurityController BuildSecurity { get; internal set; }



        public DateTime LoadedOn { get; internal set; }

        [CanBeNull]
        public string LoadedBy { get; internal set; }

        [NotNull]
        public string LoadedByClassy {
            get {
                return PlayerDB.FindExactClassyName( LoadedBy );
            }
        }


        public DateTime MapChangedOn { get; private set; }

        [CanBeNull]
        public string MapChangedBy { get; internal set; }

        [NotNull]
        public string MapChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( MapChangedBy );
            }
        }


        // used to synchronize player joining/parting with map loading/saving
        internal readonly object SyncRoot = new object();


        public BlockDB BlockDB { get; private set; }


        internal World( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( !IsValidName( name ) ) {
                throw new ArgumentException( "Unacceptible world name." );
            }
            BlockDB = new BlockDB( this );
            AccessSecurity = new SecurityController();
            BuildSecurity = new SecurityController();
            Name = name;
            UpdatePlayerList();
        }


        #region Map

        /// <summary> Map of this world. May be null if world is not loaded. </summary>
        [CanBeNull]
        public Map Map {
            get { return map; }
            set {
                if( map != null && value == null ) StopTasks();
                if( map == null && value != null ) StartTasks();
                if( value != null ) value.World = this;
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
        [NotNull]
        public Map LoadMap() {
            var tempMap = Map;
            if( tempMap != null ) return tempMap;

            lock( SyncRoot ) {
                if( Map != null ) return Map;

                if( File.Exists( MapFileName ) ) {
                    try {
                        Map = MapUtility.Load( MapFileName );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "World.LoadMap: Failed to load map ({0}): {1}",
                                    MapFileName, ex );
                    }
                }

                // or generate a default one
                if( Map == null ) {
                    Server.Message( "&WMapfile is missing for world {0}&W. A new map has been created.", ClassyName );
                    Logger.Log( LogType.SystemActivity,
                                "World.LoadMap: Map file missing for world {0}. Generating default flatgrass map.",
                                Name );
                    Map = MapGenerator.GenerateFlatgrass( 128, 128, 64 );
                }

                return Map;
            }
        }


        internal void UnloadMap( bool expectedPendingFlag ) {
            lock( SyncRoot ) {
                if( expectedPendingFlag != IsPendingMapUnload ) return;
                SaveMap();
                Map = null;
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
            lock( SyncRoot ) {
                if( Map != null ) {
                    Map.Save( MapFileName );
                }
            }
        }


        public void ChangeMap( [NotNull] Map newMap ) {
            if( newMap == null ) throw new ArgumentNullException( "newMap" );
            MapChangedOn = DateTime.UtcNow;
            lock( SyncRoot ) {
                World newWorld = new World( Name ) {
                    AccessSecurity = (SecurityController)AccessSecurity.Clone(),
                    BuildSecurity = (SecurityController)BuildSecurity.Clone(),
                    IsHidden = IsHidden,
                    BlockDB = BlockDB,
                    BackupInterval = BackupInterval,
                    lastBackup = lastBackup,
                    LoadedBy = LoadedBy,
                    LoadedOn = LoadedOn,
                    MapChangedBy = MapChangedBy,
                    MapChangedOn = MapChangedOn,
                    FogColor = FogColor,
                    CloudColor = CloudColor,
                    SkyColor = SkyColor,
                    EdgeLevel = EdgeLevel,
                    EdgeBlock = EdgeBlock,
                    IsLocked = IsLocked,
                    LockedBy = LockedBy,
                    UnlockedBy = UnlockedBy,
                    LockDate = LockDate,
                    UnlockDate = UnlockDate
                };
                newMap.World = newWorld;
                newWorld.Map = newMap;
                newWorld.Preload = preload;
                WorldManager.ReplaceWorld( this, newWorld );
                lock( BlockDB.SyncRoot ) {
                    BlockDB.Clear();
                    BlockDB.World = newWorld;
                }
                foreach( Player player in Players ) {
                    player.JoinWorld( newWorld, WorldChangeReason.Rejoin );
                }
            }
        }


        bool preload;
        public bool Preload {
            get {
                return preload;
            }
            set {
                lock( SyncRoot ) {
                    if( preload == value ) return;
                    preload = value;
                    if( preload ) {
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
            lock( SyncRoot ) {
                if( Map == null ) return;
                Players.Message( "&WMap is being flushed. Stay put, world will reload shortly." );
                IsFlushing = true;
            }
        }


        internal void EndFlushMapBuffer() {
            lock( SyncRoot ) {
                IsFlushing = false;
                Players.Message( "&WMap flushed. Reloading..." );
                foreach( Player player in Players ) {
                    player.JoinWorld( this, WorldChangeReason.Rejoin, player.Position );
                }
            }
        }

        #endregion


        #region PlayerList

        readonly Dictionary<string, Player> playerIndex = new Dictionary<string, Player>();
        public Player[] Players { get; private set; }

        [CanBeNull]
        public Map AcceptPlayer( [NotNull] Player player, bool announce ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( SyncRoot ) {
                if( IsFull ) {
                    if( player.Info.Rank.ReservedSlot ) {
                        Player idlestPlayer = Players.Where( p => p.Info.Rank.IdleKickTimer != 0 )
                                                     .OrderBy( p => p.LastActiveTime )
                                                     .FirstOrDefault();
                        if( idlestPlayer != null ) {
                            idlestPlayer.Kick( Player.Console, "Auto-kicked to make room (idle).",
                                               LeaveReason.IdleKick, false, false, false );
                            Server.Message( "Player {0}&S was auto-kicked to make room for {1}",
                                            idlestPlayer.ClassyName, player.ClassyName );
                        } else {
                            return null;
                        }
                    } else {
                        return null;
                    }
                }

                if( playerIndex.ContainsKey( player.Name.ToLower() ) ) {
                    Logger.Log( LogType.Error,
                                "This world already contains the player by name ({0}). " +
                                "Some sort of state corruption must have occured.",
                                player.Name );
                    playerIndex.Remove( player.Name.ToLower() );
                }

                playerIndex.Add( player.Name.ToLower(), player );

                // load the map, if it's not yet loaded
                IsPendingMapUnload = false;
                Map = LoadMap();

                if( ConfigKey.BackupOnJoin.Enabled() && (Map.HasChangedSinceBackup || !ConfigKey.BackupOnlyWhenChanged.Enabled()) ) {
                    string backupFileName = String.Format( JoinBackupFormat,
                                                           Name, DateTime.Now, player.Name ); // localized
                    Map.SaveBackup( MapFileName,
                                    Path.Combine( Paths.BackupPath, backupFileName ) );
                }

                UpdatePlayerList();

                if( announce && ConfigKey.ShowJoinedWorldMessages.Enabled() ) {
                    Server.Players.CanSee( player )
                                  .Message( "&SPlayer {0}&S joined {1}",
                                            player.ClassyName, ClassyName );
                }

                Logger.Log( LogType.UserActivity,
                            "Player {0} joined world {1}.",
                            player.Name, Name );

                if( IsLocked ) {
                    player.Message( "&WThis map is currently locked (read-only)." );
                }

                if( player.Info.IsHidden ) {
                    player.Message( "&8Reminder: You are still hidden." );
                }

                return Map;
            }
        }


        public bool ReleasePlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( SyncRoot ) {
                if( !playerIndex.Remove( player.Name.ToLower() ) ) {
                    return false;
                }

                // clear undo & selection
                player.LastDrawOp = null;
                player.UndoClear();
                player.RedoClear();
                player.SelectionCancel();

                // update player list
                UpdatePlayerList();

                // unload map (if needed)
                if( playerIndex.Count == 0 && !preload ) {
                    IsPendingMapUnload = true;
                }
                return true;
            }
        }


        public Player[] FindPlayers( [NotNull] Player player, [NotNull] string playerName ) {
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
        [CanBeNull]
        public Player FindPlayerExact( [NotNull] string playerName ) {
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
            lock( SyncRoot ) {
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
        public int CountVisiblePlayers( [NotNull] Player observer ) {
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
        public DateTime LockDate, UnlockDate;

        readonly object lockLock = new object();


        public bool Lock( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( lockLock ) {
                if( IsLocked ) {
                    return false;
                } else {
                    LockedBy = player.Name;
                    LockDate = DateTime.UtcNow;
                    IsLocked = true;
                    Map mapCache = Map;
                    if( mapCache != null ) {
                        mapCache.ClearUpdateQueue();
                        mapCache.StopAllDrawOps();
                    }
                    Players.Message( "&WWorld was locked by {0}", player.ClassyName );
                    Logger.Log( LogType.UserActivity,
                                "World {0} was locked by {1}",
                                Name, player.Name );
                    return true;
                }
            }
        }


        public bool Unlock( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( lockLock ) {
                if( IsLocked ) {
                    UnlockedBy = player.Name;
                    UnlockDate = DateTime.UtcNow;
                    IsLocked = false;
                    Players.Message( "&WMap was unlocked by {0}", player.ClassyName );
                    Logger.Log( LogType.UserActivity,
                                "World \"{0}\" was unlocked by {1}",
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

        public Player GetNextPatrolTarget( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            lock( patrolLock ) {
                Player candidate = Players.RankedAtMost( RankManager.PatrolledRank )
                                          .CanBeSeen( observer )
                                          .Where( p => p.LastActiveTime > p.LastPatrolTime &&
                                                       p.HasFullyConnected &&
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

        SchedulerTask updateTask, saveTask;
        readonly object taskLock = new object();


        void StopTasks() {
            lock( taskLock ) {
                if( updateTask != null ) {
                    updateTask.Stop();
                    updateTask = null;
                }
                if( saveTask != null ) {
                    saveTask.Stop();
                    saveTask = null;
                }
            }
        }


        void StartTasks() {
            lock( taskLock ) {
                updateTask = Scheduler.NewTask( UpdateTask );
                updateTask.RunForever( this,
                                       TimeSpan.FromMilliseconds( ConfigKey.TickInterval.GetInt() ),
                                       TimeSpan.Zero );

                if( ConfigKey.SaveInterval.GetInt() > 0 ) {
                    saveTask = Scheduler.NewBackgroundTask( SaveTask );
                    saveTask.RunForever( this,
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ),
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ) );
                }
            }
        }


        void UpdateTask( SchedulerTask task ) {
            Map tempMap = Map;
            if( tempMap != null ) {
                tempMap.ProcessUpdates();
            }
        }


        const string TimedBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}.fcm",
                     JoinBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}_{2}.fcm";

        void SaveTask( SchedulerTask task ) {
            if( !IsLoaded ) return;
            lock( SyncRoot ) {
                if( Map == null ) return;

                lock( backupLock ) {
                    if( BackupsEnabled &&
                        DateTime.UtcNow.Subtract( lastBackup ) > BackupInterval &&
                        ( Map.HasChangedSinceBackup || !ConfigKey.BackupOnlyWhenChanged.Enabled() ) ) {

                        string backupFileName = String.Format( TimedBackupFormat, Name, DateTime.Now ); // localized
                        Map.SaveBackup( MapFileName,
                                        Path.Combine( Paths.BackupPath, backupFileName ) );
                        lastBackup = DateTime.UtcNow;
                    }
                }

                if( Map.HasChangedSinceSave ) {
                    SaveMap();
                }
            }
        }

        #endregion


        #region Backups

        DateTime lastBackup = DateTime.UtcNow;
        static readonly object backupLock = new object();

        /// <summary> Whether timed backups are enabled (either manually or by default) on this world. </summary>
        public bool BackupsEnabled {
            get {
                switch( BackupEnabledState ) {
                    case YesNoAuto.Yes:
                        return BackupInterval > TimeSpan.Zero;
                    case YesNoAuto.No:
                        return false;
                    default: //case YesNoAuto.Auto:
                        return DefaultBackupsEnabled;
                }
            }
        }


        /// <summary> Backup state. Use "Yes" to enable, "No" to disable, and "Auto" to use default settings.
        /// If setting to "Yes", make sure to set BackupInterval property value first. </summary>
        public YesNoAuto BackupEnabledState {
            get { return backupEnabledState; }
            set {
                lock( backupLock ) {
                    if( value == backupEnabledState ) return;
                    if( value == YesNoAuto.Yes && BackupInterval <= TimeSpan.Zero ) {
                        throw new InvalidOperationException( "Set BackupInterval before setting BackupEnabledState to Yes." );
                    }
                    backupEnabledState = value;
                }
            }
        }
        YesNoAuto backupEnabledState = YesNoAuto.Auto;


        /// <summary> Timed backup interval.
        /// If BackupEnabledState is set to "Yes", value must be positive.
        /// If BackupEnabledState is set to "No" or "Auto", this property has no effect. </summary>
        public TimeSpan BackupInterval {
            get {
                switch( backupEnabledState ) {
                    case YesNoAuto.Yes:
                        return backupInterval;
                    case YesNoAuto.No:
                        return TimeSpan.Zero;
                    default: //case YesNoAuto.Auto:
                        return DefaultBackupInterval;
                }
            }
            set {
                lock( backupLock ) {
                    if( BackupEnabledState == YesNoAuto.Yes && value >= TimeSpan.Zero ) {
                        throw new InvalidOperationException( "BackupInterval must be positive if BackupEnabledState is set to Yes." );
                    }
                    backupInterval = value;
                }
            }
        }
        TimeSpan backupInterval;


        /// <summary> Default backup interval, for worlds that have BackupEnabledState set to "Auto". </summary>
        public static TimeSpan DefaultBackupInterval { get; set; }


        /// <summary> Whether timed backups are enabled by default for worlds that have BackupEnabledState set to "Auto". </summary>
        public static bool DefaultBackupsEnabled {
            get { return DefaultBackupInterval > TimeSpan.Zero; }
        }


        internal string BackupSettingDescription {
            get {
                switch( backupEnabledState ) {
                    case YesNoAuto.No:
                        return "disabled (manual)";
                    case YesNoAuto.Yes:
                        return String.Format( "enabled ({0})", backupInterval.ToMiniString() );
                    default: //case YesNoAuto.Auto:
                        if( DefaultBackupsEnabled ) {
                            return String.Format( "default ({0})", DefaultBackupInterval.ToMiniString() );
                        } else {
                            return "default (disabled)";
                        }
                }
            }
        }

        #endregion


        #region WoM Extensions

        public int CloudColor = -1,
                   FogColor = -1,
                   SkyColor = -1,
                   EdgeLevel = -1;

        public Block EdgeBlock = Block.Water;

        public string GenerateWoMConfig( bool sendMotd ) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "server.name = " + ConfigKey.ServerName.GetString() );
            if( sendMotd ) {
                sb.AppendLine( "server.detail = " + ConfigKey.MOTD.GetString() );
            } else {
                sb.AppendLine( "server.detail = " + ClassyName );
            }
            sb.AppendLine( "user.detail = World " + ClassyName );
            if( CloudColor > -1 ) sb.AppendLine( "environment.cloud = " + CloudColor );
            if( FogColor > -1 ) sb.AppendLine( "environment.fog = " + FogColor );
            if( SkyColor > -1 ) sb.AppendLine( "environment.sky = " + SkyColor );
            if( EdgeLevel > -1 ) sb.AppendLine( "environment.level = " + EdgeLevel );
            if( EdgeBlock != Block.Water ) {
                string edgeTexture = Map.GetEdgeTexture( EdgeBlock );
                if( edgeTexture != null ) {
                    sb.AppendLine( "environment.edge = " + edgeTexture );
                }
            }
            sb.AppendLine( "server.sendwomid = true" );
            return sb.ToString();
        }

        #endregion


        /// <summary> Ensures that player name has the correct length (2-16 characters)
        /// and character set (alphanumeric chars and underscores allowed). </summary>
        public static bool IsValidName( [NotNull] string name ) {
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
                    Rank maxRank;
                    if( BuildSecurity.MinRank >= AccessSecurity.MinRank ) {
                        maxRank = BuildSecurity.MinRank;
                    } else {
                        maxRank = AccessSecurity.MinRank;
                    }
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        return maxRank.Color + maxRank.Prefix + Name;
                    } else {
                        return maxRank.Color + Name;
                    }
                } else {
                    return Name;
                }
            }
        }


        public override string ToString() {
            return String.Format( "World({0})", Name );
        }


        #region Serialization
        public const string BuildSecurityXmlTagName = "BuildSecurity",
                            AccessSecurityXmlTagName = "AccessSecurity",
                            EnvironmentXmlTagName = "Environment";

        public const string XmlRootName = "World";

        public World( [NotNull] string name, [NotNull] XElement el ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( el == null ) throw new ArgumentNullException( "el" );
            if( !IsValidName( name ) ) {
                throw new ArgumentException( "Unacceptible world name." );
            }
            Name = name;
            BlockDB = new BlockDB( this );
            UpdatePlayerList();

            XAttribute tempAttr;

            // load hidden status
            if( (tempAttr = el.Attribute( "hidden" )) != null ) {
                bool isHidden;
                if( Boolean.TryParse( tempAttr.Value, out isHidden ) ) {
                    IsHidden = isHidden;
                } else {
                    Logger.Log( LogType.Warning,
                                "World: Could not parse \"hidden\" attribute of world \"{0}\", assuming NOT hidden.",
                                Name );
                }
            }

            // load access and build security
            XElement tempEl;
            if( (tempEl = el.Element( AccessSecurityXmlTagName )) != null ) {
                AccessSecurity = new SecurityController( tempEl, true );
            } else if( (tempEl = el.Element( "accessSecurity" )) != null ) {
                AccessSecurity = new SecurityController( tempEl, true );
            }
            if( (tempEl = el.Element( BuildSecurityXmlTagName )) != null ) {
                BuildSecurity = new SecurityController( tempEl, true );
            } else if( (tempEl = el.Element( "buildSecurity" )) != null ) {
                BuildSecurity = new SecurityController( tempEl, true );
            }

            // load backup interval
            if( (tempAttr = el.Attribute( "backup" )) != null ) {
                TimeSpan backupInterval;
                if( tempAttr.Value.ToTimeSpan( out backupInterval ) ) {
                    BackupInterval = backupInterval;
                } else {
                    BackupInterval = DefaultBackupInterval;
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse \"backup\" attribute of world \"{0}\", assuming default ({1}).",
                                Name,
                                BackupInterval.ToMiniString() );
                }
            } else {
                BackupInterval = DefaultBackupInterval;
            }

            // load BlockDB settings
            XElement blockEl = el.Element( BlockDB.XmlRootName );
            if( blockEl != null ) {
                BlockDB.LoadSettings( blockEl );
            }

            // load map (if needed)
            Preload = (el.Attribute( "noUnload" ) != null);

            // load environment settings
            XElement envEl = el.Element( EnvironmentXmlTagName );
            if( envEl != null ) {
                if( (tempAttr = envEl.Attribute( "cloud" )) != null ) {
                    if( !Int32.TryParse( tempAttr.Value, out CloudColor ) ) {
                        CloudColor = -1;
                        Logger.Log( LogType.Warning,
                                    "WorldManager: Could not parse \"cloud\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    Name );
                    }
                }
                if( (tempAttr = envEl.Attribute( "fog" )) != null ) {
                    if( !Int32.TryParse( tempAttr.Value, out FogColor ) ) {
                        FogColor = -1;
                        Logger.Log( LogType.Warning,
                                    "WorldManager: Could not parse \"fog\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    Name );
                    }
                }
                if( (tempAttr = envEl.Attribute( "sky" )) != null ) {
                    if( !Int32.TryParse( tempAttr.Value, out SkyColor ) ) {
                        SkyColor = -1;
                        Logger.Log( LogType.Warning,
                                    "WorldManager: Could not parse \"sky\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    Name );
                    }
                }
                if( (tempAttr = envEl.Attribute( "level" )) != null ) {
                    if( !Int32.TryParse( tempAttr.Value, out EdgeLevel ) ) {
                        EdgeLevel = -1;
                        Logger.Log( LogType.Warning,
                                    "WorldManager: Could not parse \"level\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    Name );
                    }
                }
                if( (tempAttr = envEl.Attribute( "edge" )) != null ) {
                    Block block = Map.GetBlockByName( tempAttr.Value );
                    if( block == Block.Undefined ) {
                        EdgeBlock = Block.Water;
                        Logger.Log( LogType.Warning,
                                    "WorldManager: Could not parse \"edge\" attribute of Environment settings for world \"{0}\", assuming default (Water).",
                                    Name );
                    } else {
                        if( Map.GetEdgeTexture( block ) == null ) {
                            EdgeBlock = Block.Water;
                            Logger.Log( LogType.Warning,
                                        "WorldManager: Unacceptable blocktype given for \"edge\" attribute of Environment settings for world \"{0}\", assuming default (Water).",
                                        Name );
                        } else {
                            EdgeBlock = block;
                        }
                    }
                }
            }
        }


        [NotNull]
        public XElement Serialize() {
            return Serialize( XmlRootName );
        }


        [NotNull]
        public XElement Serialize( [NotNull] string elementName ) {
            if( elementName == null ) throw new ArgumentNullException( "elementName" );

            XElement root = new XElement( elementName );
            if( AccessSecurity.HasRestrictions ) {
                root.Add( AccessSecurity.Serialize( AccessSecurityXmlTagName ) );
            }
            if( BuildSecurity.HasRestrictions ) {
                root.Add( BuildSecurity.Serialize( BuildSecurityXmlTagName ) );
            }

            if( BackupInterval != DefaultBackupInterval ) {
                root.Add( new XAttribute( "backup", BackupInterval.ToSecondsString() ) );
            }

            if( Preload ) {
                root.Add( new XAttribute( "noUnload", true ) );
            }
            if( IsHidden ) {
                root.Add( new XAttribute( "hidden", true ) );
            }
            root.Add( BlockDB.SaveSettings() );

            if( !String.IsNullOrEmpty( LoadedBy ) ) {
                root.Add( new XElement( "LoadedBy", LoadedBy ) );
            }
            if( LoadedOn != DateTime.MinValue ) {
                root.Add( new XElement( "LoadedOn", LoadedOn.ToUnixTime() ) );
            }
            if( !String.IsNullOrEmpty( MapChangedBy ) ) {
                root.Add( new XElement( "MapChangedBy", MapChangedBy ) );
            }
            if( MapChangedOn != DateTime.MinValue ) {
                root.Add( new XElement( "MapChangedOn", MapChangedOn.ToUnixTime() ) );
            }

            XElement elEnv = new XElement( EnvironmentXmlTagName );
            if( CloudColor > -1 ) elEnv.Add( new XAttribute( "cloud", CloudColor ) );
            if( FogColor > -1 ) elEnv.Add( new XAttribute( "fog", FogColor ) );
            if( SkyColor > -1 ) elEnv.Add( new XAttribute( "sky", SkyColor ) );
            if( EdgeLevel > -1 ) elEnv.Add( new XAttribute( "level", EdgeLevel ) );
            if( EdgeBlock != Block.Water ) elEnv.Add( new XAttribute( "edge", EdgeBlock ) );
            if( elEnv.HasAttributes ) {
                root.Add( elEnv );
            }
            return root;
        }

        #endregion
    }
}