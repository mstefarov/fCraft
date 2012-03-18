// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Object representing persistent state ("record") of a player, online or offline.
    /// There is exactly one PlayerInfo object for each known Minecraft account. All data is stored in the PlayerDB. </summary>
    public sealed partial class PlayerInfo : IClassy, INotifyPropertyChanged {

        #region Properties
        /// <summary> Whether or not the data has changed since it was saved to database. </summary>
        public bool Changed { get; set; }

        /// <summary> Player's unique numeric ID. Immutable. Issued on first join. </summary>
        public int ID { get; private set; }


        /// <summary> Player's Minecraft account name. </summary>
        [NotNull]
        public string Name {
            get { return name; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( name != null && value.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    throw new ArgumentException( "You may only change capitalization of the name.", "value" );
                }
                if( name != value ) {
                    name = value;
                    OnChanged( "Name" );
                }
            }
        }
        [NotNull]
        string name;


        /// <summary> If set, replaces Name when printing name in chat. </summary>
        [CanBeNull]
        public string DisplayedName {
            get { return displayedName; }
            set {
                if( value != displayedName ) {
                    displayedName = value;
                    OnChanged( "DisplayedName" );
                }
            }
        }
        [CanBeNull]
        string displayedName;


        /// <summary> First time the player ever logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime FirstLoginDate {
            get { return firstLoginDate; }
            set {
                if( value == firstLoginDate ) return;
                firstLoginDate = value;
                OnChanged( "FirstLoginDate" );
            }
        }
        DateTime firstLoginDate;


        /// <summary> Most recent time the player logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastLoginDate {
            get { return lastLoginDate; }
            set {
                if( value != lastLoginDate ) {
                    lastLoginDate = value;
                    OnChanged( "LastLoginDate" );
                }
            }
        }
        DateTime lastLoginDate;


        /// <summary> Last time the player has been seen online (last logout), UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastSeen {
            get { return lastSeen; }
            set {
                if( value != lastSeen ) {
                    lastSeen = value;
                    OnChanged( "LastSeen" );
                }
            }
        }
        DateTime lastSeen;


        /// <summary> Reason for leaving the server last time. </summary>
        public LeaveReason LeaveReason {
            get { return leaveReason; }
            set {
                if( value != leaveReason ) {
                    leaveReason = value;
                    OnChanged( "LeaveReason" );
                }
            }
        }
        LeaveReason leaveReason;


        #region Rank

        /// <summary> Player's current rank.
        /// Should be set by using PlayerInfo.ChangeRank method. </summary>
        [NotNull]
        public Rank Rank {
            get { return rank; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value != rank ) {
                    rank = value;
                    OnChanged( "Rank" );
                }
            }
        }
        [NotNull]
        Rank rank;


        /// <summary> Player's previous rank.
        /// May be null if player has never been promoted/demoted before. </summary>
        [CanBeNull]
        public Rank PreviousRank {
            get { return previousRank; }
            set {
                if( value != previousRank ) {
                    previousRank = value;
                    OnChanged( "PreviousRank" );
                }
            }
        }
        [CanBeNull]
        Rank previousRank;


        /// <summary> Date of the most recent promotion/demotion, UTC.
        /// May be DateTime.MinValue if player has never been promoted/demoted before. </summary>
        public DateTime RankChangeDate {
            get { return rankChangeDate; }
            set {
                if( value != rankChangeDate ) {
                    rankChangeDate = value;
                    OnChanged( "RankChangeDate" );
                }
            }
        }
        DateTime rankChangeDate;


        /// <summary> Name of the entity that most recently promoted/demoted this player. May be null. </summary>
        [CanBeNull]
        public string RankChangedBy {
            get { return rankChangedBy; }
            set {
                if( value != rankChangedBy ) {
                    rankChangedBy = value;
                    OnChanged( "RankChangedBy" );
                }
            }
        }
        [CanBeNull]
        string rankChangedBy;


        /// <summary> Returns decorated name of RankChangedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string RankChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( rankChangedBy );
            }
        }


        /// <summary> Reason given for the most recent promotion/demotion. May be null. </summary>
        [CanBeNull]
        public string RankChangeReason {
            get { return rankChangeReason; }
            set {
                if( value != rankChangeReason ) {
                    rankChangeReason = value;
                    OnChanged( "RankChangeReason" );
                }
            }
        }
        [CanBeNull]
        string rankChangeReason;


        /// <summary> Type of the most recent promotion/demotion. </summary>
        public RankChangeType RankChangeType {
            get { return rankChangeType; }
            set {
                if( value != rankChangeType ) {
                    rankChangeType = value;
                    OnChanged( "RankChangeType" );
                }
            }
        }
        RankChangeType rankChangeType;

        #endregion


        #region Bans

        /// <summary> Player's current BanStatus: Banned, NotBanned, or Exempt. </summary>
        public BanStatus BanStatus {
            get { return banStatus; }
            set {
                if( value != banStatus ) {
                    banStatus = value;
                    OnChanged( "BanStatus" );
                }
            }
        }
        BanStatus banStatus;


        /// <summary> Returns whether player is name-banned or not. Read-only, not serialized. </summary>
        public bool IsBanned {
            get { return banStatus == BanStatus.Banned; }
        }


        /// <summary> Date of most recent ban, UTC. May be DateTime.MinValue if player was never banned. </summary>
        public DateTime BanDate {
            get { return banDate; }
            set {
                if( value != banDate ) {
                    banDate = value;
                    OnChanged( "BanDate" );
                }
            }
        }
        DateTime banDate;


        /// <summary> Name of the entity responsible for most recent ban. May be null. </summary>
        [CanBeNull]
        public string BannedBy {
            get { return bannedBy; }
            set {
                if( value != bannedBy ) {
                    bannedBy = value;
                    OnChanged( "BannedBy" );
                }
            }
        }
        [CanBeNull]
        string bannedBy;


        /// <summary> Returns decorated name of BannedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string BannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( bannedBy );
            }
        }


        /// <summary> Reason given for the most recent ban. May be null. </summary>
        [CanBeNull]
        public string BanReason {
            get { return banReason; }
            set {
                if( value != banReason ) {
                    banReason = value;
                    OnChanged( "BanReason" );
                }
            }
        }
        [CanBeNull]
        string banReason;


        /// <summary> Date of most recent unban, UTC. May be DateTime.MinValue if player was never unbanned. </summary>
        public DateTime UnbanDate {
            get { return unbanDate; }
            set {
                if( value != unbanDate ) {
                    unbanDate = value;
                    OnChanged( "UnbanDate" );
                }
            }
        }
        DateTime unbanDate;


        /// <summary> Name of the entity responsible for most recent unban. May be null. </summary>
        [CanBeNull]
        public string UnbannedBy {
            get { return unbannedBy; }
            set {
                if( value != unbannedBy ) {
                    unbannedBy = value;
                    OnChanged( "UnbannedBy" );
                }
            }
        }
        [CanBeNull]
        string unbannedBy;


        /// <summary> Returns decorated name of UnbannedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string UnbannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( unbannedBy );
            }
        }


        /// <summary> Reason given for the most recent unban. May be null. </summary>
        [CanBeNull]
        public string UnbanReason {
            get { return unbanReason; }
            set {
                if( value != unbanReason ) {
                    unbanReason = value;
                    OnChanged( "UnbanReason" );
                }
            }
        }
        [CanBeNull]
        string unbanReason;


        /// <summary> Date of most recent failed attempt to log in, UTC. </summary>
        public DateTime LastFailedLoginDate {
            get { return lastFailedLoginDate; }
            set {
                if( value != lastFailedLoginDate ) {
                    lastFailedLoginDate = value;
                    OnChanged( "LastFailedLoginDate" );
                }
            }
        }
        DateTime lastFailedLoginDate;


        /// <summary> IP from which player most recently tried (and failed) to log in, UTC. </summary>
        [NotNull]
        public IPAddress LastFailedLoginIP {
            get { return lastFailedLoginIP; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value != lastFailedLoginIP ) {
                    lastFailedLoginIP = value;
                    OnChanged( "LastFailedLoginIP" );
                }
            }
        }
        [NotNull]
        IPAddress lastFailedLoginIP = IPAddress.None;

        #endregion


        #region Stats

        /// <summary> Total amount of time the player spent on this server, excluding current session (if online). </summary>
        public TimeSpan TotalTime {
            get { return totalTime; }
            set {
                if( value != totalTime ) {
                    totalTime = value;
                    OnChanged( "TotalTime" );
                }
            }
        }
        TimeSpan totalTime;


        /// <summary> Total amount of time player spent on this server, including current session (if online). </summary>
        public TimeSpan TotalTimeIncludingSession {
            get {
                Player playerObj = playerObject;
                TimeSpan time = totalTime;
                if( playerObj != null ) {
                    time += playerObj.LastActiveTime.Subtract( playerObj.LoginTime );
                }
                return time;
            }
        }


        /// <summary> Total number of blocks manually built or painted by the player. </summary>
        public int BlocksBuilt {
            get { return blocksBuilt; }
            set {
                if( value != blocksBuilt ) {
                    blocksBuilt = value;
                    OnChanged( "BlocksBuilt" );
                }
            }
        }
        int blocksBuilt;


        /// <summary> Total number of blocks manually deleted by the player. </summary>
        public int BlocksDeleted {
            get { return blocksDeleted; }
            set {
                if( value != blocksDeleted ) {
                    blocksDeleted = value;
                    OnChanged( "BlocksDeleted" );
                }
            }
        }
        int blocksDeleted;


        /// <summary> Total number of blocks modified using draw and copy/paste commands. </summary>
        public long BlocksDrawn {
            get { return blocksDrawn; }
            set {
                if( value != blocksDrawn ) {
                    blocksDrawn = value;
                    OnChanged( "BlocksDrawn" );
                }
            }
        }
        long blocksDrawn;


        /// <summary> Number of sessions/logins. </summary>
        public int TimesVisited {
            get { return timesVisited; }
            set {
                if( value != timesVisited ) {
                    timesVisited = value;
                    OnChanged( "TimesVisited" );
                }
            }
        }
        int timesVisited;


        /// <summary> Total number of messages written. </summary>
        public int MessagesWritten {
            get { return messagesWritten; }
            set {
                if( value != messagesWritten ) {
                    messagesWritten = value;
                    OnChanged( "MessagesWritten" );
                }
            }
        }
        int messagesWritten;


        /// <summary> Number of kicks issues by this player. </summary>
        public int TimesKickedOthers {
            get { return timesKickedOthers; }
            set {
                if( value != timesKickedOthers ) {
                    timesKickedOthers = value;
                    OnChanged( "TimesKickedOthers" );
                }
            }
        }
        int timesKickedOthers;


        /// <summary> Number of bans issued by this player. </summary>
        public int TimesBannedOthers {
            get { return timesBannedOthers; }
            set {
                if( value != timesBannedOthers ) {
                    timesBannedOthers = value;
                    OnChanged( "TimesBannedOthers" );
                }
            }
        }
        int timesBannedOthers;

        #endregion


        #region Kicks

        /// <summary> Number of times that this player has been manually kicked. </summary>
        public int TimesKicked {
            get { return timesKicked; }
            set {
                if( value != timesKicked ) {
                    timesKicked = value;
                    OnChanged( "TimesKicked" );
                }
            }
        }
        int timesKicked;


        /// <summary> Date of the most recent kick.
        /// May be DateTime.MinValue if the player has never been kicked. </summary>
        public DateTime LastKickDate {
            get { return lastKickDate; }
            set {
                if( value != lastKickDate ) {
                    lastKickDate = value;
                    OnChanged( "LastKickDate" );
                }
            }
        }
        DateTime lastKickDate;


        /// <summary> Name of the entity that most recently kicked this player. May be null. </summary>
        [CanBeNull]
        public string LastKickBy {
            get { return lastKickBy; }
            set {
                if( value != lastKickBy ) {
                    lastKickBy = value;
                    OnChanged( "LastKickBy" );
                }
            }
        }
        [CanBeNull]
        string lastKickBy;


        /// <summary> Returns decorated name of LastKickByClassy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string LastKickByClassy {
            get {
                return PlayerDB.FindExactClassyName( lastKickBy );
            }
        }


        /// <summary> Reason given for the most recent kick. May be null. </summary>
        [CanBeNull]
        public string LastKickReason {
            get { return lastKickReason; }
            set {
                if( value != lastKickReason ) {
                    lastKickReason = value;
                    OnChanged( "LastKickReason" );
                }
            }
        }
        [CanBeNull]
        string lastKickReason;

        #endregion


        #region Freeze And Mute

        /// <summary> Whether this player is currently frozen. </summary>
        public bool IsFrozen {
            get { return isFrozen; }
            set {
                if( value != isFrozen ) {
                    isFrozen = value;
                    OnChanged( "IsFrozen" );
                }
            }
        }
        bool isFrozen;


        /// <summary> Date of the most recent freezing.
        /// May be DateTime.MinValue of the player has never been frozen. </summary>
        public DateTime FrozenOn {
            get { return frozenOn; }
            set {
                if( value != frozenOn ) {
                    frozenOn = value;
                    OnChanged( "FrozenOn" );
                }
            }
        }
        DateTime frozenOn;


        /// <summary> Name of the entity that most recently froze this player. May be null. </summary>
        [CanBeNull]
        public string FrozenBy {
            get { return frozenBy; }
            set {
                if( value != frozenBy ) {
                    frozenBy = value;
                    OnChanged( "FrozenBy" );
                }
            }
        }
        [CanBeNull]
        string frozenBy;


        /// <summary> Returns decorated name of FrozenBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string FrozenByClassy {
            get {
                return PlayerDB.FindExactClassyName( frozenBy );
            }
        }


        /// <summary> Whether this player is currently muted. Read-only, not serialized. </summary>
        public bool IsMuted {
            get {
                return DateTime.UtcNow < MutedUntil;
            }
        }


        /// <summary> Date until which the player is muted. If the date is in the past, player is NOT muted. </summary>
        public DateTime MutedUntil {
            get { return mutedUntil; }
            set {
                if( value != mutedUntil ) {
                    mutedUntil = value;
                    OnChanged( "MutedUntil" );
                }
            }
        }
        DateTime mutedUntil;


        /// <summary> Name of the entity that most recently muted this player. May be null. </summary>
        [CanBeNull]
        public string MutedBy {
            get { return mutedBy; }
            set {
                if( value != mutedBy ) {
                    mutedBy = value;
                    OnChanged( "MutedBy" );
                }
            }
        }
        [CanBeNull]
        string mutedBy;


        /// <summary> Returns decorated name of MutedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string MutedByClassy {
            get {
                return PlayerDB.FindExactClassyName( mutedBy );
            }
        }

        #endregion


        #region Session

        /// <summary> Whether the player is currently online.
        /// Another way to check online status is to check if PlayerObject is null. </summary>
        public bool IsOnline {
            get { return isOnline; }
            private set {
                if( value != isOnline ) {
                    isOnline = value;
                    OnChanged( "IsOnline" );
                }
            }
        }
        bool isOnline;


        /// <summary> If player is online, Player object associated with the session.
        /// If player is offline, null. </summary>
        [CanBeNull]
        public Player PlayerObject {
            get { return playerObject; }
            private set {
                if( value != playerObject ) {
                    playerObject = value;
                    OnChanged( "PlayerObject" );
                }
            }
        }
        Player playerObject;


        /// <summary> Whether the player is currently hidden.
        /// Use Player.CanSee() method to check visibility to specific observers. </summary>
        public bool IsHidden {
            get { return isHidden; }
            set {
                if( value != isHidden ) {
                    isHidden = value;
                    OnChanged( "IsHidden" );
                }
            }
        }

        bool isHidden;


        /// <summary> For offline players, last IP used to succesfully log in.
        /// For online players, current IP. </summary>
        [NotNull]
        public IPAddress LastIP {
            get { return lastIP; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value != lastIP ) {
                    lastIP = value;
                    OnChanged( "LastIP" );
                }
            }
        }
        IPAddress lastIP = IPAddress.None;

        /// <summary> Determines if the player has been granted all permissions </summary>
        public bool IsSuper { get; private set; }

        #endregion


        #region Unfinished / Not Implemented

        /// <summary> Not implemented (IRC/server password hash). </summary>
        [CanBeNull]
        public string Password { // TODO
            get { return password; }
            set {
                if( value != password ) {
                    password = value;
                    OnChanged( "Password" );
                }
            }
        }
        string password;


        /// <summary> Date/time of last modification to this PlayerInfo.
        /// Unlike other properties, setting LastModified does NOT raise PropertyChanged event. </summary>
        public DateTime LastModified { get; set; }

        /// <summary> The current bandwith usage mode of the player. </summary>
        public BandwidthUseMode BandwidthUseMode { // TODO
            get { return bandwidthUseMode; }
            set {
                if( value != bandwidthUseMode ) {
                    bandwidthUseMode = value;
                    OnChanged( "BandwidthUseMode" );
                }
            }
        }
        BandwidthUseMode bandwidthUseMode;


        /// <summary> Not implemented (for temp bans). </summary>
        public DateTime BannedUntil { // TODO
            get { return bannedUntil; }
            set {
                if( value != bannedUntil ) {
                    bannedUntil = value;
                    OnChanged( "BannedUntil" );
                }
            }
        }
        DateTime bannedUntil;

        #endregion

        #endregion


        [NotNull]
        readonly object syncRoot = new object();

         /// <summary>  Object used for synchronization (lock). </summary>
        [NotNull]
        public object SyncRoot {
            get { return syncRoot; }
        }


        #region Constructors and Serialization

        // creates a blank PlayerInfo record.
        public PlayerInfo( int id ) {
            ID = id;
        }


        // create a record for an unrecognized or a super player
        public PlayerInfo( int id, [NotNull] string name, [NotNull] Rank startingRank,
                           RankChangeType rankChangeType, bool isSuper )
            : this( id ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            this.name = name;
            rank = startingRank;
            this.rankChangeType = rankChangeType;
            LastModified = DateTime.UtcNow;
            IsSuper = isSuper;
        }


        // create a record for a newly logged-in player
        public PlayerInfo( int id, [NotNull] string name, [NotNull] Rank startingRank,
                           RankChangeType rankChangeType, [NotNull] IPAddress address )
            : this( id ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            if( address == null ) throw new ArgumentNullException( "address" );
            this.name = name;
            rank = startingRank;
            this.rankChangeType = rankChangeType;
            lastIP = address;
            firstLoginDate = DateTime.UtcNow;
            lastSeen = DateTime.UtcNow;
            lastLoginDate = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }

        #endregion


        #region Update Handlers

        public void ProcessMessageWritten() {
            lock( syncRoot ) {
                MessagesWritten++;
            }
        }


        public void ProcessLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( syncRoot ) {
                LastIP = player.IP;
                LastLoginDate = DateTime.UtcNow;
                LastSeen = DateTime.UtcNow;
                TimesVisited++;
                IsOnline = true;
                PlayerObject = player;
            }
        }


        public void ProcessFailedLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( syncRoot ) {
                LastFailedLoginDate = DateTime.UtcNow;
                LastFailedLoginIP = player.IP;
                LastModified = DateTime.UtcNow;
            }
        }


        public void ProcessLogout( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( syncRoot ) {
                TotalTime += player.LastActiveTime.Subtract( player.LoginTime );
                LastSeen = DateTime.UtcNow;
                IsOnline = false;
                PlayerObject = null;
                LeaveReason = player.LeaveReason;
                LastModified = DateTime.UtcNow;
            }
        }


        public void ProcessRankChange( [NotNull] Rank newRank, [NotNull] string changer, [CanBeNull] string reason, RankChangeType type ) {
            if( newRank == null ) throw new ArgumentNullException( "newRank" );
            if( changer == null ) throw new ArgumentNullException( "changer" );
            lock( syncRoot ) {
                PreviousRank = Rank;
                Rank = newRank;
                RankChangeDate = DateTime.UtcNow;

                RankChangedBy = changer;
                RankChangeReason = reason;
                RankChangeType = type;
                LastModified = DateTime.UtcNow;
            }
        }


        public void ProcessBlockPlaced( Block type ) {
            lock( syncRoot ) {
                if( type == Block.Air ) {
                    BlocksDeleted++;
                } else {
                    blocksBuilt++;
                }
            }
        }


        public void ProcessDrawCommand( int blocksToAdd ) {
            lock( syncRoot ) {
                blocksDrawn += blocksToAdd;
            }
        }


        internal void ProcessKick( [NotNull] Player kickedBy, [CanBeNull] string reason ) {
            if( kickedBy == null ) throw new ArgumentNullException( "kickedBy" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            lock( syncRoot ) {
                TimesKicked++;
                lock( kickedBy.Info.syncRoot ) {
                    kickedBy.Info.TimesKickedOthers++;
                }
                LastKickDate = DateTime.UtcNow;
                LastKickBy = kickedBy.Name;
                LastKickReason = reason;
                Unfreeze();
            }
        }

        #endregion


        // implements IClassy interface
        public string ClassyName {
            get {
                StringBuilder sb = new StringBuilder();
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    sb.Append( Rank.Color );
                }
                if( DisplayedName != null ) {
                    sb.Append( DisplayedName );
                } else {
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        sb.Append( Rank.Prefix );
                    }
                    sb.Append( Name );
                }
                if( IsBanned ) {
                    sb.Append( Color.Red ).Append( '*' );
                } else if( IsFrozen ) {
                    sb.Append( Color.Blue ).Append( '*' );
                }
                return sb.ToString();
            }
        }


        #region TimeSince_____ shortcuts
        /// <summary> Time (Utc) since the player's rank was last changed. </summary>
        public TimeSpan TimeSinceRankChange {
            get { return DateTime.UtcNow.Subtract( RankChangeDate ); }
        }
        /// <summary> Time (Utc) since the player was banned. </summary>
        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }
        /// <summary> Time (Utc) since the player was unbanned. </summary>
        public TimeSpan TimeSinceUnban {
            get { return DateTime.UtcNow.Subtract( UnbanDate ); }
        }
        /// <summary> Time (Utc) since the player first logged on. </summary>
        public TimeSpan TimeSinceFirstLogin {
            get { return DateTime.UtcNow.Subtract( FirstLoginDate ); }
        }
        /// <summary> Time (Utc) since the player last logged on. </summary>
        public TimeSpan TimeSinceLastLogin {
            get { return DateTime.UtcNow.Subtract( LastLoginDate ); }
        }
        /// <summary> Time (Utc) since the player was last kicked.. </summary>
        public TimeSpan TimeSinceLastKick {
            get { return DateTime.UtcNow.Subtract( LastKickDate ); }
        }
        /// <summary> Time (Utc) since the player was last seen. </summary>
        public TimeSpan TimeSinceLastSeen {
            get { return DateTime.UtcNow.Subtract( LastSeen ); }
        }
        /// <summary> Time (Utc) since the player was last frozen. </summary>
        public TimeSpan TimeSinceFrozen {
            get { return DateTime.UtcNow.Subtract( FrozenOn ); }
        }
        /// <summary> Time (Utc) until the player will be unmuted. </summary>
        public TimeSpan TimeMutedLeft {
            get { return MutedUntil.Subtract( DateTime.UtcNow ); }
        }
        /// <summary> Time (Utc) since the player's record was last modified. </summary>
        public TimeSpan TimeSinceLastModified {
            get { return DateTime.UtcNow.Subtract( LastModified ); }
        }

        #endregion


        /// <summary> Whether or not this player has the specified permission. </summary>
        /// <param name="permission"> Permission to check if the player has. </param>
        /// <returns> True if the player has permission, otherwise false. </returns>
        public bool Can( Permission permission ) {
            return IsSuper || Rank.Can( permission );
        }

        /// <summary> Whether or not this player has the ability to affect the target rank, using the specified permission. </summary>
        /// <param name="permission"> Permission to check if the player had. </param>
        /// <param name="targetRank"> Player to check if this player has permission to affect. </param>
        /// <returns> True if the player has permission, otherwise false. </returns>
        public bool Can( Permission permission, [NotNull] Rank targetRank ) {
            return IsSuper || Rank.Can( permission, targetRank );
        }


        void OnChanged( string propertyName ) {
            Changed = true;
            LastModified = DateTime.UtcNow;
            if( RaisePropertyChangedEvents ) {
                var handler = PropertyChanged;
                if( handler != null ) handler( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }

        /// <summary> Raised when a property is changed. </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal bool RaisePropertyChangedEvents { get; set; }

        public override string ToString() {
            return String.Format( "PlayerInfo({0},{1})", Name, Rank.Name );
        }


        public static readonly IComparer<PlayerInfo> ComparerByID = new PlayerIDComparer();

        sealed class PlayerIDComparer : IComparer<PlayerInfo> {
            public int Compare( PlayerInfo x, PlayerInfo y ) {
                return x.ID - y.ID;
            }
        }

    }


    public sealed class PlayerInfoComparer : IComparer<PlayerInfo> {
        readonly Player observer;

        public PlayerInfoComparer( Player observer ) {
            this.observer = observer;
        }

        public int Compare( PlayerInfo x, PlayerInfo y ) {
            Player xPlayer = x.PlayerObject;
            Player yPlayer = y.PlayerObject;
            bool xIsOnline = xPlayer != null && observer.CanSee( xPlayer );
            bool yIsOnline = yPlayer != null && observer.CanSee( yPlayer );

            if( !xIsOnline && yIsOnline ) {
                return 1;
            } else if( xIsOnline && !yIsOnline ) {
                return -1;
            }

            if( x.Rank == y.Rank ) {
                return Math.Sign( y.LastSeen.Ticks - x.LastSeen.Ticks );
            } else {
                return x.Rank.Index - y.Rank.Index;
            }
        }
    }
}