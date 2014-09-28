// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    sealed partial class PlayerInfo {
        /// <summary> Occurs when a new PlayerDB entry is being created.
        /// Allows editing the starting rank. Cancelable (kicks the player). </summary>
        public static event EventHandler<PlayerInfoBeingCreatedEventArgs> BeingCreated;

        /// <summary> Occurs after a new PlayerDB entry has been created. </summary>
        public static event EventHandler<PlayerInfoCreatedEventArgs> Created;

        /// <summary> Occurs when a player's rank is about to be changed (automatically or manually). </summary>
        public static event EventHandler<PlayerInfoRankChangingEventArgs> RankChanging;

        /// <summary> Occurs after a player's rank was changed (automatically or manually). </summary>
        public static event EventHandler<PlayerInfoRankChangedEventArgs> RankChanged;

        /// <summary> Occurs when a player is about to be banned or unbanned. Cancelable. </summary>
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


        internal static void RaiseCreatingEvent([NotNull] PlayerInfoBeingCreatedEventArgs e) {
            var h = BeingCreated;
            if (h != null) h(null, e);
        }


        internal static void RaiseCreatedEvent([NotNull] PlayerInfo info, bool isUnrecognized) {
            var h = Created;
            if (h != null) h(null, new PlayerInfoCreatedEventArgs(info, isUnrecognized));
        }


        static bool RaiseRankChangingEvent([NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger,
                                           [NotNull] Rank newRank,
                                           [CanBeNull] string reason, RankChangeType rankChangeType, bool announce) {
            var h = RankChanging;
            if (h == null) return false;
            var e = new PlayerInfoRankChangingEventArgs(playerInfo,
                                                        rankChanger,
                                                        newRank,
                                                        reason,
                                                        rankChangeType,
                                                        announce);
            h(null, e);
            return e.Cancel;
        }


        static void RaiseRankChangedEvent([NotNull] PlayerInfo playerInfo, [NotNull] Player rankChanger,
                                          [NotNull] Rank oldRank,
                                          [CanBeNull] string reason, RankChangeType rankChangeType, bool announce) {
            var h = RankChanged;
            if (h != null)
                h(null,
                  new PlayerInfoRankChangedEventArgs(playerInfo,
                                                     rankChanger,
                                                     oldRank,
                                                     reason,
                                                     rankChangeType,
                                                     announce));
        }


        internal static void RaiseBanChangingEvent([NotNull] PlayerInfoBanChangingEventArgs e) {
            if (e == null) throw new ArgumentNullException("e");
            var h = BanChanging;
            if (h != null) h(null, e);
        }


        internal static void RaiseBanChangedEvent([NotNull] PlayerInfoBanChangingEventArgs e) {
            if (e == null) throw new ArgumentNullException("e");
            var h = BanChanged;
            if (h != null)
                h(null,
                  new PlayerInfoBanChangedEventArgs(e.PlayerInfo, e.Banner, e.IsBeingUnbanned, e.Reason, e.Announce));
        }


        static bool RaiseFreezeChangingEvent([NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing,
                                             bool announce) {
            var h = FreezeChanging;
            if (h == null) return false;
            var e = new PlayerInfoFrozenChangingEventArgs(target, freezer, unfreezing, announce);
            h(null, e);
            return e.Cancel;
        }


        static void RaiseFreezeChangedEvent([NotNull] PlayerInfo target, [NotNull] Player freezer, bool unfreezing,
                                            bool announce) {
            var h = FreezeChanged;
            if (h != null) h(null, new PlayerInfoFrozenChangedEventArgs(target, freezer, unfreezing, announce));
        }


        static bool RaiseMuteChangingEvent([NotNull] PlayerInfo target, [NotNull] Player muter,
                                           TimeSpan duration, bool unmuting, bool announce) {
            var h = MuteChanging;
            if (h == null) return false;
            var e = new PlayerInfoMuteChangingEventArgs(target, muter, duration, unmuting, announce);
            h(null, e);
            return !e.Cancel;
        }


        static void RaiseMuteChangedEvent([NotNull] PlayerInfo target, [NotNull] Player muter,
                                          TimeSpan duration, bool unmuting, bool announce) {
            var h = MuteChanged;
            if (h != null) h(null, new PlayerInfoMuteChangedEventArgs(target, muter, duration, unmuting, announce));
        }
    }
}

namespace fCraft.Events {
    /// <summary> An EventArgs for an event that directly related to a particular PlayerInfo. </summary>
    public interface IPlayerInfoEvent {
        /// <summary> Player affected by the event. </summary>
        [NotNull]
        PlayerInfo PlayerInfo { get; }
    }


    /// <summary> Provides data for PlayerInfo.BeingCreated event. Cancelable.
    /// Allows changing StartingRank. </summary>
    public sealed class PlayerInfoBeingCreatedEventArgs : EventArgs, ICancelableEvent {
        internal PlayerInfoBeingCreatedEventArgs([NotNull] string name, [CanBeNull] IPAddress ip,
                                                 [NotNull] Rank startingRank, bool isUnrecognized) {
            if (name == null) throw new ArgumentNullException("name");
            if (startingRank == null) throw new ArgumentNullException("startingRank");
            Name = name;
            StartingRank = startingRank;
            IP = ip;
            IsUnrecognized = isUnrecognized;
        }


        /// <summary> Name of the new player for whom PlayerInfo is being created. </summary>
        [NotNull]
        public string Name { get; private set; }

        /// <summary> Rank to assign to the new player. Defaults to RankManager.DefaultRank. </summary>
        [NotNull]
        public Rank StartingRank { get; set; }

        /// <summary> IP Address from which player has connected. May be null if adding an unrecognized/offline player. </summary>
        [CanBeNull]
        public IPAddress IP { get; private set; }

        /// <summary> Whether new player is unrecognized. </summary>
        /// <value> False if a real player is actually connected. </value>
        /// <value> True if the player is just being added by name. </value>
        public bool IsUnrecognized { get; private set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for PlayerInfo.Created event. Immutable. </summary>
    public sealed class PlayerInfoCreatedEventArgs : EventArgs, IPlayerInfoEvent {
        internal PlayerInfoCreatedEventArgs([NotNull] PlayerInfo playerInfo, bool isUnrecognized) {
            if (playerInfo == null) throw new ArgumentNullException("playerInfo");
            PlayerInfo = playerInfo;
            IsUnrecognized = isUnrecognized;
        }


        /// <summary> Newly-added PlayerInfo object. </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Whether new player is unrecognized. </summary>
        /// <value> False if a real player is actually connected. </value>
        /// <value> True if the player is just being added by name. </value>
        public bool IsUnrecognized { get; private set; }
    }


    /// <summary> Provides data for PlayerInfo.RankChanging event. Cancelable. </summary>
    public sealed class PlayerInfoRankChangingEventArgs : EventArgs, IPlayerInfoEvent, ICancelableEvent {
        internal PlayerInfoRankChangingEventArgs([NotNull] PlayerInfo target, [NotNull] Player rankChanger,
                                                 [NotNull] Rank newRank, [CanBeNull] string reason,
                                                 RankChangeType rankChangeType, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (rankChanger == null) throw new ArgumentNullException("rankChanger");
            if (newRank == null) throw new ArgumentNullException("newRank");
            PlayerInfo = target;
            RankChanger = rankChanger;
            OldRank = target.Rank;
            NewRank = newRank;
            Reason = reason;
            RankChangeType = rankChangeType;
            Announce = announce;
            NewRank = newRank;
        }


        /// <summary> Player whose rank will be changed (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated promotion/demotion. </summary>
        [NotNull]
        public Player RankChanger { get; private set; }

        /// <summary> Player's current (old) rank. </summary>
        [NotNull]
        public Rank OldRank { get; private set; }

        /// <summary> Player's proposed (new) rank. </summary>
        [NotNull]
        public Rank NewRank { get; private set; }

        /// <summary> Given promotion/demotion reason. May be null. Can be changed. </summary>
        [CanBeNull]
        public string Reason { get; set; }

        /// <summary> Type of rank change. </summary>
        public RankChangeType RankChangeType { get; private set; }

        /// <summary> Whether the promotion/demotion should be announced in-game and on IRC. Can be changed. </summary>
        public bool Announce { get; set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for PlayerInfo.RankChanged event. Immutable. </summary>
    public sealed class PlayerInfoRankChangedEventArgs : EventArgs, IPlayerInfoEvent {
        internal PlayerInfoRankChangedEventArgs([NotNull] PlayerInfo target, [NotNull] Player rankChanger,
                                                [NotNull] Rank oldRank, [CanBeNull] string reason,
                                                RankChangeType rankChangeType, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (rankChanger == null) throw new ArgumentNullException("rankChanger");
            if (oldRank == null) throw new ArgumentNullException("oldRank");
            PlayerInfo = target;
            RankChanger = rankChanger;
            OldRank = oldRank;
            NewRank = target.Rank;
            Reason = reason;
            RankChangeType = rankChangeType;
            Announce = announce;
        }


        /// <summary> Player whose rank was just changed (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated promotion/demotion. </summary>
        [NotNull]
        public Player RankChanger { get; private set; }

        /// <summary> Player's previous (old) rank. </summary>
        [NotNull]
        public Rank OldRank { get; private set; }

        /// <summary> Player's current (new) rank. </summary>
        [NotNull]
        public Rank NewRank { get; private set; }

        /// <summary> Given promotion/demotion reason. May be null. </summary>
        [CanBeNull]
        public string Reason { get; private set; }

        /// <summary> Type of rank change. </summary>
        public RankChangeType RankChangeType { get; private set; }

        /// <summary> Whether the promotion/demotion was announced in-game and on IRC. </summary>
        public bool Announce { get; private set; }
    }


    /// <summary> Provides data for PlayerInfo.BanChanging event. Cancelable.
    /// Reason and Announce properties may be changed. </summary>
    public sealed class PlayerInfoBanChangingEventArgs : EventArgs, IPlayerInfoEvent, ICancelableEvent {
        internal PlayerInfoBanChangingEventArgs([NotNull] PlayerInfo target, [NotNull] Player banner,
                                                bool isBeingUnbanned, [CanBeNull] string reason, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (banner == null) throw new ArgumentNullException("banner");
            PlayerInfo = target;
            Banner = banner;
            IsBeingUnbanned = isBeingUnbanned;
            Reason = reason;
            Announce = announce;
        }


        /// <summary> Player who is being banned/unbanned (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated ban/unban. </summary>
        [NotNull]
        public Player Banner { get; private set; }

        /// <summary> Whether player is being banned or unbanned. </summary>
        public bool IsBeingUnbanned { get; private set; }

        /// <summary> Given ban/unban reason. May be null. Can be changed. </summary>
        [CanBeNull]
        public string Reason { get; set; }

        /// <summary> Whether the promotion/demotion should be announced in-game and on IRC. Can be changed. </summary>
        public bool Announce { get; set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for PlayerInfo.BanChanged event. Immutable. </summary>
    public sealed class PlayerInfoBanChangedEventArgs : EventArgs, IPlayerInfoEvent {
        internal PlayerInfoBanChangedEventArgs([NotNull] PlayerInfo target, [NotNull] Player banner,
                                               bool wasUnbanned, [CanBeNull] string reason, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (banner == null) throw new ArgumentNullException("banner");
            PlayerInfo = target;
            Banner = banner;
            WasUnbanned = wasUnbanned;
            Reason = reason;
            Announce = announce;
        }


        /// <summary> Player who was just banned/unbanned (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated ban/unban. </summary>
        [NotNull]
        public Player Banner { get; private set; }

        /// <summary> Whether player was banned or unbanned. </summary>
        public bool WasUnbanned { get; private set; }

        /// <summary> Given ban/unban reason. May be null. </summary>
        [CanBeNull]
        public string Reason { get; private set; }

        /// <summary> Whether the ban/unban was announced in-game and on IRC. </summary>
        public bool Announce { get; private set; }
    }


    /// <summary> Provides data for PlayerInfo.FrozenChanging event. Cancelable. 
    /// Announce property may be changed. </summary>
    public sealed class PlayerInfoFrozenChangingEventArgs : EventArgs, IPlayerInfoEvent, ICancelableEvent {
        internal PlayerInfoFrozenChangingEventArgs([NotNull] PlayerInfo target, [NotNull] Player freezer,
                                                   bool isBeingUnfrozen, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (freezer == null) throw new ArgumentNullException("freezer");
            PlayerInfo = target;
            Freezer = freezer;
            IsBeingUnfrozen = isBeingUnfrozen;
            Announce = announce;
        }


        /// <summary> Player who is being frozen/unfrozen (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated freeze/unfreeze. </summary>
        [NotNull]
        public Player Freezer { get; private set; }

        /// <summary> Whether target player is being frozen or unfrozen. </summary>
        public bool IsBeingUnfrozen { get; private set; }

        /// <summary> Whether the freeze/unfreeze should be announced in-game. Can be changed. </summary>
        public bool Announce { get; set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for PlayerInfo.FrozenChanged event. Immutable. </summary>
    public sealed class PlayerInfoFrozenChangedEventArgs : EventArgs, IPlayerInfoEvent {
        internal PlayerInfoFrozenChangedEventArgs([NotNull] PlayerInfo target, [NotNull] Player freezer,
                                                  bool wasUnfrozen, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (freezer == null) throw new ArgumentNullException("freezer");
            PlayerInfo = target;
            Freezer = freezer;
            WasUnfrozen = wasUnfrozen;
            Announce = announce;
        }


        /// <summary> Player who was just frozen/unfrozen (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated freeze/unfreeze. </summary>
        [NotNull]
        public Player Freezer { get; private set; }

        /// <summary> Whether target player was frozen or unfrozen. </summary>
        public bool WasUnfrozen { get; private set; }

        /// <summary> Whether the freeze/unfreeze has been announced in-game. </summary>
        public bool Announce { get; private set; }
    }


    /// <summary> Provides data for PlayerInfo.MuteChanging event. Cancelable. 
    /// Duration and Announce properties may be changed. </summary>
    public sealed class PlayerInfoMuteChangingEventArgs : EventArgs, IPlayerInfoEvent, ICancelableEvent {
        internal PlayerInfoMuteChangingEventArgs([NotNull] PlayerInfo target, [NotNull] Player muter,
                                                 TimeSpan duration, bool unmuting, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (muter == null) throw new ArgumentNullException("muter");
            PlayerInfo = target;
            Muter = muter;
            Duration = duration;
            IsBeingUnmuted = unmuting;
            Announce = announce;
        }


        /// <summary> Player who is being muted/unmuted (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated mute/unmute. </summary>
        [NotNull]
        public Player Muter { get; private set; }

        /// <summary> Mute duration. Must not be negative. May be changed.
        /// If player is being unmuted, this is the current remaining mute duration.
        /// If player is being muted, this is the desired mute duration, counting from now. </summary>
        public TimeSpan Duration { get; set; }

        /// <summary> Whether player is being muted or unmuted. </summary>
        public bool IsBeingUnmuted { get; private set; }

        /// <summary> Whether the mute/unmute should be announced in-game. Can be changed. </summary>
        public bool Announce { get; set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for PlayerInfo.MuteChanged event. Immutable. </summary>
    public sealed class PlayerInfoMuteChangedEventArgs : EventArgs, IPlayerInfoEvent {
        internal PlayerInfoMuteChangedEventArgs([NotNull] PlayerInfo target, [NotNull] Player muter,
                                                TimeSpan duration, bool unmuting, bool announce) {
            if (target == null) throw new ArgumentNullException("target");
            if (muter == null) throw new ArgumentNullException("muter");
            PlayerInfo = target;
            Muter = muter;
            Duration = duration;
            WasUnmuted = unmuting;
            Announce = announce;
        }


        /// <summary> Player who was just muted/unmuted (target). </summary>
        public PlayerInfo PlayerInfo { get; private set; }

        /// <summary> Player who initiated mute/unmute. </summary>
        [NotNull]
        public Player Muter { get; private set; }

        /// <summary> Mute duration.
        /// If player was unmuted, this is the remaining mute duration before unmute.
        /// If player was muted, this is the new mute duration, counting from now. </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary> Whether player was muted or unmuted. </summary>
        public bool WasUnmuted { get; private set; }

        /// <summary> Whether the mute/unmute was announced in-game. </summary>
        public bool Announce { get; private set; }
    }
}
