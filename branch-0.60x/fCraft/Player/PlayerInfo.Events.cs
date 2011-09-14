// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using fCraft.Events;

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


        internal static void RaiseCreatingEvent( PlayerInfoCreatingEventArgs e ) {
            var h = Creating;
            if( h != null ) h( null, e );
        }

        internal static void RaiseCreatedEvent( PlayerInfo info, bool isUnrecognized ) {
            var h = Created;
            if( h != null ) h( null, new PlayerInfoCreatedEventArgs( info, isUnrecognized ) );
        }

        internal static bool RaiseRankChangingEvent( PlayerInfo playerInfo, Player rankChanger, Rank newRank, string reason, RankChangeType rankChangeType ) {
            var h = RankChanging;
            if( h == null ) return false;
            var e = new PlayerInfoRankChangingEventArgs( playerInfo, rankChanger, newRank, reason, rankChangeType );
            h( null, e );
            return e.Cancel;
        }

        internal static void RaiseRankChangedEvent( PlayerInfo playerInfo, Player rankChanger, Rank oldRank, string reason, RankChangeType rankChangeType ) {
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
    }
}

namespace fCraft.Events {

    // ReSharper disable MemberCanBeProtected.Global
    public class PlayerInfoEventArgs : EventArgs {
        public PlayerInfoEventArgs( PlayerInfo playerInfo ) {
            PlayerInfo = playerInfo;
        }
        public PlayerInfo PlayerInfo { get; private set; }
    }
    // ReSharper restore MemberCanBeProtected.Global


    public sealed class PlayerInfoCreatingEventArgs : EventArgs, ICancellableEvent {
        public PlayerInfoCreatingEventArgs( string name, IPAddress ip, Rank startingRank, bool isUnrecognized ) {
            Name = name;
            StartingRank = startingRank;
            IP = ip;
            IsUnrecognized = isUnrecognized;
        }
        public string Name { get; private set; }
        public Rank StartingRank { get; set; }
        public IPAddress IP { get; private set; }
        public bool IsUnrecognized { get; private set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoCreatedEventArgs : PlayerInfoEventArgs {
        public PlayerInfoCreatedEventArgs( PlayerInfo playerInfo, bool isUnrecognized )
            : base( playerInfo ) {
            IsUnrecognized = isUnrecognized;
        }
        public bool IsUnrecognized { get; private set; }
    }


    public class PlayerInfoRankChangedEventArgs : PlayerInfoEventArgs {
        public PlayerInfoRankChangedEventArgs( PlayerInfo playerInfo, Player rankChanger, Rank oldRank, string reason, RankChangeType rankChangeType )
            : base( playerInfo ) {
            RankChanger = rankChanger;
            OldRank = oldRank;
            NewRank = playerInfo.Rank;
            Reason = reason;
            RankChangeType = rankChangeType;
        }

        public Player RankChanger { get; private set; }
        public Rank OldRank { get; protected set; }
        public Rank NewRank { get; protected set; }
        public string Reason { get; private set; }
        public RankChangeType RankChangeType { get; private set; }
    }


    public sealed class PlayerInfoRankChangingEventArgs : PlayerInfoRankChangedEventArgs, ICancellableEvent {
        public PlayerInfoRankChangingEventArgs( PlayerInfo playerInfo, Player rankChanger, Rank newRank, string reason, RankChangeType rankChangeType )
            : base( playerInfo, rankChanger, playerInfo.Rank, reason, rankChangeType ) {
            NewRank = newRank;
        }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerInfoBanChangedEventArgs : PlayerInfoEventArgs {
        public PlayerInfoBanChangedEventArgs( PlayerInfo target, Player banner, bool isBeingUnbanned,  string reason )
            : base( target ) {
            Banner = banner;
            IsBeingUnbanned = isBeingUnbanned;
            Reason = reason;
        }

        public Player Banner { get; private set; }
        public bool IsBeingUnbanned { get; private set; }
        public string Reason { get; private set; }
    }


    public sealed class PlayerInfoBanChangingEventArgs : PlayerInfoEventArgs, ICancellableEvent {
        public PlayerInfoBanChangingEventArgs( PlayerInfo target, Player banner, bool isBeingUnbanned, string reason )
            : base( target ) {
            Banner = banner;
            IsBeingUnbanned = isBeingUnbanned;
            Reason = reason;
        }

        public Player Banner { get; private set; }
        public bool IsBeingUnbanned { get; private set; }
        public string Reason { get; set; }
        public bool Cancel { get; set; }
    }
}