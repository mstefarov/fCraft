// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    sealed partial class PlayerInfo {

        /// <summary> Occurs when a new PlayerDB entry is being created.
        /// Allows editing the starting rank. Cancellable (kicks the player). </summary>
        public static event EventHandler<PlayerInfoCreatingEventArgs> Creating;

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



        internal static void RaiseCreatingEvent( [NotNull] PlayerInfoCreatingEventArgs e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            var h = Creating;
            if( h != null ) h( null, e );
        }

        internal static void RaiseCreatedEvent( PlayerInfo info, bool isUnrecognized ) {
            var h = Created;
            if( h != null ) h( null, new PlayerInfoCreatedEventArgs( info, isUnrecognized ) );
        }


        static bool RaiseRankChangingEvent( PlayerInfo playerInfo, Player rankChanger, Rank newRank, string reason, RankChangeType rankChangeType ) {
            var h = RankChanging;
            if( h == null ) return false;
            var e = new PlayerInfoRankChangingEventArgs( playerInfo, rankChanger, newRank, reason, rankChangeType );
            h( null, e );
            return e.Cancel;
        }


        static void RaiseRankChangedEvent( PlayerInfo playerInfo, Player rankChanger, Rank oldRank, string reason, RankChangeType rankChangeType ) {
            var h = RankChanged;
            if( h != null ) h( null, new PlayerInfoRankChangedEventArgs( playerInfo, rankChanger, oldRank, reason, rankChangeType ) );
        }

        internal static void RaiseBanChangingEvent( PlayerInfoBanChangingEventArgs e ) {
            var h = BanChanging;
            if( h != null ) h( null, e );
        }

        internal static void RaiseBanChangedEvent( PlayerInfoBanChangingEventArgs e ) {
            var h = BanChanged;
            if( h != null ) h( null, new PlayerInfoBanChangedEventArgs( e.PlayerInfo, e.Banner, e.IsBeingUnbanned, e.Reason ) );
        }


        static bool RaiseFreezeChangingEvent( PlayerInfo target, Player freezer, bool unfreezing ) {
            var h = FreezeChanging;
            if( h == null ) return false;
            var e = new PlayerInfoFrozenChangingEventArgs( target, freezer, unfreezing );
            h( null, e );
            return e.Cancel;
        }


        static void RaiseFreezeChangedEvent( PlayerInfo target, Player freezer, bool unfreezing ) {
            var h = FreezeChanged;
            if( h != null ) h( null, new PlayerInfoFrozenChangedEventArgs( target, freezer, unfreezing ) );
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


    public sealed class PlayerInfoCreatingEventArgs : EventArgs, ICancellableEvent {
        internal PlayerInfoCreatingEventArgs( [NotNull] string name, IPAddress ip, Rank startingRank, bool isUnrecognized ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Name = name;
            StartingRank = startingRank;
            IP = ip;
            IsUnrecognized = isUnrecognized;
        }


        [NotNull]
        public string Name { get; private set; }
        public Rank StartingRank { get; set; }
        public IPAddress IP { get; private set; }
        public bool IsUnrecognized { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoCreatedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoCreatedEventArgs( PlayerInfo playerInfo, bool isUnrecognized )
            : base( playerInfo ) {
            IsUnrecognized = isUnrecognized;
        }
        public bool IsUnrecognized { get; private set; }
    }


    public class PlayerInfoRankChangedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoRankChangedEventArgs( [NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger,
                                                 Rank oldRank, string reason, RankChangeType rankChangeType )
            : base( playerInfo ) {
            if( rankChanger == null ) throw new ArgumentNullException( "rankChanger" );
            RankChanger = rankChanger;
            OldRank = oldRank;
            NewRank = playerInfo.Rank;
            Reason = reason;
            RankChangeType = rankChangeType;
        }


        [NotNull]
        public Player RankChanger { get; private set; }
        public Rank OldRank { get; protected set; }
        public Rank NewRank { get; protected set; }
        public string Reason { get; private set; }
        public RankChangeType RankChangeType { get; private set; }
    }


    public sealed class PlayerInfoRankChangingEventArgs : PlayerInfoRankChangedEventArgs, ICancellableEvent {
        internal PlayerInfoRankChangingEventArgs( [NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger,
                                                  [NotNull] Rank newRank, string reason, RankChangeType rankChangeType )
            : base( playerInfo, rankChanger, playerInfo.Rank, reason, rankChangeType ) {
            NewRank = newRank;
        }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoBanChangedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoBanChangedEventArgs( [NotNull] PlayerInfo target, [NotNull] Player banner,
                                                bool isBeingUnbanned, string reason )
            : base( target ) {
            if( banner == null ) throw new ArgumentNullException( "banner" );
            Banner = banner;
            IsBeingUnbanned = isBeingUnbanned;
            Reason = reason;
        }


        [NotNull]
        public Player Banner { get; private set; }
        public bool IsBeingUnbanned { get; private set; }
        public string Reason { get; private set; }
    }


    public sealed class PlayerInfoBanChangingEventArgs : PlayerInfoEventArgs, ICancellableEvent {
        internal PlayerInfoBanChangingEventArgs( [NotNull] PlayerInfo target, [NotNull] Player banner,
                                                 bool isBeingUnbanned, string reason )
            : base( target ) {
            Banner = banner;
            IsBeingUnbanned = isBeingUnbanned;
            Reason = reason;
        }


        [NotNull]
        public Player Banner { get; private set; }
        public bool IsBeingUnbanned { get; private set; }
        public string Reason { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoFrozenChangingEventArgs : PlayerInfoFrozenChangedEventArgs, ICancellableEvent {
        internal PlayerInfoFrozenChangingEventArgs( [NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing )
            : base( target, freezer, unfreezing ) {
        }


        public bool Cancel { get; set; }
    }


    public class PlayerInfoFrozenChangedEventArgs : PlayerInfoEventArgs {
        internal PlayerInfoFrozenChangedEventArgs( [NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing )
            : base( target ) {
            if( freezer == null ) throw new ArgumentNullException( "freezer" );
            Freezer = freezer;
            Unfreezing = unfreezing;
        }

        [NotNull]
        public Player Freezer { get; private set; }
        public bool Unfreezing { get; private set; }
    }
}