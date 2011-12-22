// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    partial class PlayerInfo {

        /// <summary> Occurs when a new PlayerDB entry is being created.
        /// Allows editing the starting rank. Cancellable (kicks the player). </summary>
        public static event EventHandler<PlayerInfoBeingCreatedEventArgs> BeingCreated;

        /// <summary> Occurs after a new PlayerDB entry has been created. </summary>
        public static event EventHandler<PlayerInfoCreatedEventArgs> Created;

        /// <summary> Occurs when a player's rank is about to be changed (automatically or manually). </summary>
        public static event EventHandler<PlayerInfoRankChangingEventArgs> RankChanging;

        /// <summary> Occurs after a player's rank was changed (automatically or manually). </summary>
        public static event EventHandler<PlayerInfoRankChangedEventArgs> RankChanged;

        /// <summary> Occurs when a player is about to be banned or unbanned. Cancellable. </summary>
        public static event EventHandler<PlayerInfoBanChangingEventArgs> BanChanging;

        /// <summary> Occurs after a player has been banned or unbanned. </summary>
        public static event EventHandler<PlayerInfoBanChangedEventArgs> BanChanged;

        /// <summary> Occurs when a player is about to be frozen or unfrozen. </summary>
        public static event EventHandler<PlayerInfoFrozenChangingEventArgs> FreezeChanging;

        /// <summary> Occurs after a player has been frozen or unfrozen. </summary>
        public static event EventHandler<PlayerInfoFrozenChangedEventArgs> FreezeChanged;

        /// <summary> Occurs when a player is about to be muted or unmuted. </summary>
        public static event EventHandler<PlayerInfoMuteChangingEventArgs> MuteChanging;

        /// <summary> Occurs after a player has been muted or unmuted. </summary>
        public static event EventHandler<PlayerInfoMuteChangedEventArgs> MuteChanged;


        internal static void RaiseBeingCreatedEvent( [NotNull] PlayerInfoBeingCreatedEventArgs e ) {
            var handler = BeingCreated;
            if( handler != null ) handler( null, e );
        }


        internal static void RaiseCreatedEvent( [NotNull] PlayerInfo info, bool isUnrecognized ) {
            var handler = Created;
            if( handler != null ) handler( null, new PlayerInfoCreatedEventArgs( info, isUnrecognized ) );
        }


        static bool RaiseRankChangingEvent( [NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger, [NotNull] Rank newRank,
                                            [CanBeNull] string reason, RankChangeType rankChangeType, bool announce ) {
            var handler = RankChanging;
            if( handler == null ) return true;
            var e = new PlayerInfoRankChangingEventArgs( playerInfo, rankChanger, newRank, reason, rankChangeType, announce );
            handler( null, e );
            return !e.Cancel;
        }


        static void RaiseRankChangedEvent( [NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger, [NotNull] Rank oldRank,
                                           [CanBeNull] string reason, RankChangeType rankChangeType, bool announce ) {
            var handler = RankChanged;
            if( handler != null ) handler( null, new PlayerInfoRankChangedEventArgs( playerInfo, rankChanger, oldRank, reason, rankChangeType, announce ) );
        }


        internal static void RaiseBanChangingEvent( [NotNull] PlayerInfoBanChangingEventArgs e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            var handler = BanChanging;
            if( handler != null ) handler( null, e );
        }


        internal static void RaiseBanChangedEvent( [NotNull] PlayerInfoBanChangingEventArgs e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            var handler = BanChanged;
            if( handler != null ) handler( null, new PlayerInfoBanChangedEventArgs( e.PlayerInfo, e.Banner, e.IsBeingUnbanned, e.Reason, e.Announce ) );
        }


        static bool RaiseFreezeChangingEvent( [NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing, bool announce ) {
            var handler = FreezeChanging;
            if( handler == null ) return true;
            var e = new PlayerInfoFrozenChangingEventArgs( target, freezer, unfreezing, announce );
            handler( null, e );
            return !e.Cancel;
        }


        static void RaiseFreezeChangedEvent( [NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing, bool announce ) {
            var handler = FreezeChanged;
            if( handler != null ) handler( null, new PlayerInfoFrozenChangedEventArgs( target, freezer, unfreezing, announce ) );
        }


        static bool RaiseMuteChangingEvent( [NotNull] PlayerInfo target, [NotNull] Player muter,
                                            TimeSpan duration, bool unmuting, bool announce ) {
            var handler = MuteChanging;
            if( handler == null ) return true;
            var e = new PlayerInfoMuteChangingEventArgs( target, muter, duration, unmuting, announce );
            handler( null, e );
            return !e.Cancel;
        }


        static void RaiseMuteChangedEvent( [NotNull] PlayerInfo target, [NotNull] Player muter,
                                           TimeSpan duration, bool unmuting, bool announce ) {
            var handler = MuteChanged;
            if( handler != null ) handler( null, new PlayerInfoMuteChangedEventArgs( target, muter, duration, unmuting, announce ) );
        }
    }
}


namespace fCraft.Events {
    public class PlayerInfoEventArgs : EventArgs {
        protected PlayerInfoEventArgs( [NotNull] PlayerInfo playerInfo ) {
            if( playerInfo == null ) throw new ArgumentNullException( "playerInfo" );
            PlayerInfo = playerInfo;
        }

        [NotNull]
        public PlayerInfo PlayerInfo { get; private set; }
    }


    public sealed class PlayerInfoBeingCreatedEventArgs : EventArgs, ICancellableEvent {
        internal PlayerInfoBeingCreatedEventArgs( [NotNull] string name, [CanBeNull] IPAddress ip,
                                              [NotNull] Rank startingRank, bool isUnrecognized ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            Name = name;
            StartingRank = startingRank;
            IP = ip;
            IsUnrecognized = isUnrecognized;
        }

        [NotNull]
        public string Name { get; private set; }

        [NotNull]
        public Rank StartingRank { get; set; }

        [CanBeNull]
        public IPAddress IP { get; private set; }
        public bool IsUnrecognized { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoCreatedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoCreatedEventArgs( [NotNull] PlayerInfo playerInfo, bool isUnrecognized )
            : base( playerInfo ) {
            IsUnrecognized = isUnrecognized;
        }

        public bool IsUnrecognized { get; private set; }
    }


    public class PlayerInfoRankChangedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoRankChangedEventArgs( [NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger,
                                                 [NotNull] Rank oldRank, [CanBeNull] string reason,
                                                 RankChangeType rankChangeType, bool announce )
            : base( playerInfo ) {
            if( rankChanger == null ) throw new ArgumentNullException( "rankChanger" );
            if( oldRank == null ) throw new ArgumentNullException( "oldRank" );
            RankChanger = rankChanger;
            OldRank = oldRank;
            NewRank = playerInfo.Rank;
            Reason = reason;
            RankChangeType = rankChangeType;
            Announce = announce;
        }

        [NotNull]
        public Player RankChanger { get; private set; }

        [NotNull]
        public Rank OldRank { get; private set; }

        [NotNull]
        public Rank NewRank { get; protected set; }

        [CanBeNull] 
        public string Reason { get; private set; }

        public bool Announce { get; private set; }

        public RankChangeType RankChangeType { get; private set; }
    }


    public sealed class PlayerInfoRankChangingEventArgs : PlayerInfoRankChangedEventArgs, ICancellableEvent {
        internal PlayerInfoRankChangingEventArgs( [NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger,
                                                  [NotNull] Rank newRank, [CanBeNull] string reason,
                                                  RankChangeType rankChangeType, bool announce )
            : base( playerInfo, rankChanger, playerInfo.Rank, reason, rankChangeType, announce ) {
            NewRank = newRank;
        }

        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoBanChangedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoBanChangedEventArgs( [NotNull] PlayerInfo target, [NotNull] Player banner,
                                                bool isBeingUnbanned, string reason, bool announce )
            : base( target ) {
            if( banner == null ) throw new ArgumentNullException( "banner" );
            Banner = banner;
            IsBeingUnbanned = isBeingUnbanned;
            Reason = reason;
            Announce = announce;
        }

        [NotNull]
        public Player Banner { get; private set; }
        public bool IsBeingUnbanned { get; private set; }
        public bool Announce { get; private set; }
        public string Reason { get; private set; }
    }


    public sealed class PlayerInfoBanChangingEventArgs : PlayerInfoEventArgs, ICancellableEvent {
        internal PlayerInfoBanChangingEventArgs( [NotNull] PlayerInfo target, [NotNull] Player banner,
                                                 bool isBeingUnbanned, [CanBeNull] string reason, bool announce )
            : base( target ) {
            Banner = banner;
            IsBeingUnbanned = isBeingUnbanned;
            Reason = reason;
            Announce = announce;
        }

        [NotNull]
        public Player Banner { get; private set; }
        public bool IsBeingUnbanned { get; private set; }
        [CanBeNull]
        public string Reason { get; set; }
        public bool Announce { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoFrozenChangingEventArgs : PlayerInfoFrozenChangedEventArgs, ICancellableEvent {
        internal PlayerInfoFrozenChangingEventArgs( [NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing, bool announce )
            : base( target, freezer, unfreezing, announce ) {
        }

        public bool Cancel { get; set; }
    }


    public class PlayerInfoFrozenChangedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoFrozenChangedEventArgs( [NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing, bool announce )
            : base( target ) {
            if( freezer == null ) throw new ArgumentNullException( "freezer" );
            Freezer = freezer;
            Unfreezing = unfreezing;
            Announce = announce;
        }

        [NotNull]
        public Player Freezer { get; private set; }
        public bool Unfreezing { get; private set; }
        public bool Announce { get; private set; }
    }


    public sealed class PlayerInfoMuteChangingEventArgs : PlayerInfoMuteChangedEventArgs, ICancellableEvent {
        internal PlayerInfoMuteChangingEventArgs( [NotNull] PlayerInfo target, [NotNull] Player muter,
                                                  TimeSpan duration, bool unmuting, bool announce )
            : base( target, muter, duration, unmuting, announce ) {
        }
        public bool Cancel { get; set; }
    }


    public class PlayerInfoMuteChangedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoMuteChangedEventArgs( [NotNull] PlayerInfo target, [NotNull] Player muter,
                                                 TimeSpan duration, bool unmuting, bool announce )
            : base( target ) {
            if( muter == null ) throw new ArgumentNullException( "muter" );
            Muter = muter;
            Duration = duration;
            Unmuting = unmuting;
            Announce = announce;
        }


        [NotNull]
        public Player Muter { get; private set; }
        public TimeSpan Duration { get; private set; }
        public bool Unmuting { get; private set; }
        public bool Announce { get; private set; }
    }
}