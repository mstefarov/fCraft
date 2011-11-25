// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.ComponentModel;
using JetBrains.Annotations;
using System.IO;

namespace fCraft {
    /// <summary> Object representing persistent state ("record") of a player, online or offline.
    /// There is exactly one PlayerInfo object for each known Minecraft account. All data is stored in the PlayerDB. </summary>
    public partial class PlayerInfo : IClassy, INotifyPropertyChanged {
        public const int MinFieldCount = 24;

        #region Properties

        bool changed;

        /// <summary> Player's unique numeric ID. Immutable. Issued on first join. </summary>
        public int ID { get; protected set; }


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
        protected string name;


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
        protected string displayedName;


        /// <summary> First time the player ever logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime FirstLoginDate {
            get { return firstLoginDate; }
            set {
                if( value != firstLoginDate ) {
                    firstLoginDate = value;
                    OnChanged("FirstLoginDate");
                }
            }
        }
        protected DateTime firstLoginDate;


        /// <summary> Most recent time the player logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastLoginDate {
            get { return lastLoginDate; }
            set {
                if( value != lastLoginDate ) {
                    lastLoginDate = value;
                    OnChanged("LastLoginDate");
                }
            }
        }
        protected DateTime lastLoginDate;


        /// <summary> Last time the player has been seen online (last logout), UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastSeen {
            get { return lastSeen; }
            set {
                if( value != lastSeen ) {
                    lastSeen = value;
                    OnChanged("LastSeen");
                }
            }
        }
        protected DateTime lastSeen;


        /// <summary> Reason for leaving the server last time. </summary>
        public LeaveReason LeaveReason {
            get { return leaveReason; }
            set {
                if( value != leaveReason ) {
                    leaveReason = value;
                    OnChanged("LeaveReason");
                }
            }
        }
        protected LeaveReason leaveReason;
            

        #region Rank

        /// <summary> Player's current rank.
        /// Should be set by using PlayerInfo.ChangeRank method. </summary>
        [NotNull]
        public Rank Rank {
            get { return rank; }
            internal set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value != rank ) {
                    rank = value;
                    OnChanged( "Rank" );
                }
            }
        }
        [NotNull]
        protected Rank rank;


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
        protected Rank previousRank;


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
        protected DateTime rankChangeDate;


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
        protected string rankChangedBy;


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
        protected string rankChangeReason;


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
        protected RankChangeType rankChangeType;

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
        protected BanStatus banStatus;


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
        protected DateTime banDate;


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
        protected string bannedBy;


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
        protected string banReason;


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
        protected DateTime unbanDate;


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
        protected string unbannedBy;


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
        protected string unbanReason;


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
        protected DateTime lastFailedLoginDate;


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
        protected IPAddress lastFailedLoginIP = IPAddress.None;

        #endregion


        #region Stats

        /// <summary> Total amount of time the player spent on this server. </summary>
        public TimeSpan TotalTime {
            get { return totalTime; }
            set {
                if( value != totalTime ) {
                    totalTime = value;
                    OnChanged( "TotalTime" );
                }
            }
        }
        protected TimeSpan totalTime;


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
        protected int blocksBuilt;


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
        protected int blocksDeleted;


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
        protected long blocksDrawn;


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
        protected int timesVisited;


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
        protected int messagesWritten;


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
        protected int timesKickedOthers;


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
        protected int timesBannedOthers;

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
        protected int timesKicked;


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
        protected DateTime lastKickDate;


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
        protected string lastKickBy;


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
        protected string lastKickReason;

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
        protected bool isFrozen;


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
        protected DateTime frozenOn;


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
        protected string frozenBy;


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
        [CanBeNull]
        protected DateTime mutedUntil;


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
        protected string mutedBy;


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
        protected bool isOnline;


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
        protected Player playerObject;


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
        protected bool isHidden;


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
        protected IPAddress lastIP = IPAddress.None;

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
        protected string password;


        /// <summary> Date/time of last modification to this PlayerInfo.
        /// Unlike other properties, setting LastModified does NOT raise PropertyChanged event. </summary>
        public DateTime LastModified { get; protected set; }


        public BandwidthUseMode BandwidthUseMode { // TODO
            get { return bandwidthUseMode; }
            set {
                if( value != bandwidthUseMode ) {
                    bandwidthUseMode = value;
                    OnChanged( "BandwidthUseMode" );
                }
            }
        }
        protected BandwidthUseMode bandwidthUseMode;


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
        protected DateTime bannedUntil;

        #endregion

        #endregion


        protected readonly object syncRoot = new object();
        public object SyncRoot {
            get { return syncRoot; }
        }


        #region Constructors and Serialization

        internal PlayerInfo( int id ) {
            ID = id;
            isLoaded = true;
        }


        // fabricate info for an unrecognized player
        public PlayerInfo( [NotNull] string name, [NotNull] Rank rank,
                           bool setLoginDate, RankChangeType rankChangeType ){
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            this.name = name;
            this.rank = rank;
            if( setLoginDate ) {
                firstLoginDate = DateTime.UtcNow;
                lastLoginDate = firstLoginDate;
                lastSeen = firstLoginDate;
                timesVisited = 1;
            }
            this.rankChangeType = rankChangeType;
            LastModified = DateTime.UtcNow;
            isLoaded = true;
        }


        // generate blank info for a new player
        public PlayerInfo( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank ){
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            firstLoginDate = DateTime.UtcNow;
            lastSeen = DateTime.UtcNow;
            lastLoginDate = DateTime.UtcNow;
            rank = startingRank;
            this.name = name;
            ID = PlayerDB.GetNextID();
            this.lastIP = lastIP;
            LastModified = DateTime.UtcNow;
            isLoaded = true;
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

        public TimeSpan TimeSinceRankChange {
            get { return DateTime.UtcNow.Subtract( RankChangeDate ); }
        }

        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }

        public TimeSpan TimeSinceUnban {
            get { return DateTime.UtcNow.Subtract( UnbanDate ); }
        }

        public TimeSpan TimeSinceFirstLogin {
            get { return DateTime.UtcNow.Subtract( FirstLoginDate ); }
        }

        public TimeSpan TimeSinceLastLogin {
            get { return DateTime.UtcNow.Subtract( LastLoginDate ); }
        }

        public TimeSpan TimeSinceLastKick {
            get { return DateTime.UtcNow.Subtract( LastKickDate ); }
        }

        public TimeSpan TimeSinceLastSeen {
            get { return DateTime.UtcNow.Subtract( LastSeen ); }
        }

        public TimeSpan TimeSinceFrozen {
            get { return DateTime.UtcNow.Subtract( FrozenOn ); }
        }

        public TimeSpan TimeMutedLeft {
            get { return MutedUntil.Subtract( DateTime.UtcNow ); }
        }

        public TimeSpan TimeSinceLastModified {
            get { return DateTime.UtcNow.Subtract( LastModified ); }
        }

        #endregion


        public bool Can( Permission permission ) {
            return Rank.Can( permission );
        }

        public bool Can( Permission permission, Rank targetRank ) {
            return Rank.Can( permission, targetRank );
        }


        void OnChanged( string propertyName ) {
            changed = true;
            LastModified = DateTime.UtcNow;
            if( RaisePropertyChangedEvents ) {
                var h = PropertyChanged;
                if( h != null ) h( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool RaisePropertyChangedEvents { get; set; }

        public override string ToString() {
            return String.Format( "PlayerInfo({0},{1})", Name, Rank.Name );
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