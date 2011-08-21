// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;

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