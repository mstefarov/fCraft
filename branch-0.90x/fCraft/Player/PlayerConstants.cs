// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

// This file condenses some of the player-related enumerations
namespace fCraft {
    /// <summary> List of possible reasons for players leaving the server. </summary>
    public enum LeaveReason : byte {
        /// <summary> Unknown leave reason (default) </summary>
        Unknown = 0x00,

        /// <summary> Client exited normally </summary>
        ClientQuit = 0x01,

        /// <summary> Client reconnected before old session timed out, or connected from another IP. </summary>
        ClientReconnect = 0x03,

        /// <summary> Manual or miscellaneous kick </summary>
        Kick = 0x10,

        /// <summary> Kicked for being idle/AFK. </summary>
        IdleKick = 0x11,

        /// <summary> Invalid characters in a message </summary>
        InvalidMessageKick = 0x12,

        /// <summary> Attempted to place invalid block type. </summary>
        /// <remarks> fCraft no longer uses this reason,
        /// but this enum item remains to display old PlayerDB records correctly. </remarks>
        [Obsolete]
        InvalidSetTileKick = 0x13,

        /// <summary> Unknown opCode or packet </summary>
        InvalidOpCodeKick = 0x14,

        /// <summary> Triggered antigrief / block spam </summary>
        BlockSpamKick = 0x15,

        /// <summary> Kicked for message spam (after warnings) </summary>
        MessageSpamKick = 0x16,

        /// <summary> Banned directly by name </summary>
        Ban = 0x20,

        /// <summary> Banned indirectly by /BanIP </summary>
        BanIP = 0x21,

        /// <summary> Banned indirectly by /BanAll </summary>
        BanAll = 0x22,


        /// <summary> Server-side error (uncaught exception in session's thread) </summary>
        ServerError = 0x30,

        /// <summary> Server is shutting down </summary>
        ServerShutdown = 0x31,

        /// <summary> Server was full or became full </summary>
        ServerFull = 0x32,

        /// <summary> World was full (forced join failed) </summary>
        WorldFull = 0x33,


        /// <summary> Login failed due to protocol violation/mismatch (e.g. SMP client) </summary>
        ProtocolViolation = 0x41,

        /// <summary> Login failed due to unverified player name </summary>
        UnverifiedName = 0x42,

        /// <summary> Login denied for some other reason </summary>
        LoginFailed = 0x43
    }


    /// <summary> Mode of player name verification. </summary>
    public enum NameVerificationMode {
        /// <summary> Player names are not checked.
        /// Any connecting player can assume any identity. </summary>
        Never,

        /// <summary> Security balanced with usability.
        /// If normal verification fails, an additional check is done:
        /// If player has previously verified for his current IP and has connected at least twice before, he is allowed in. </summary>
        Balanced,

        /// <summary> Strict verification checks.
        /// If name cannot be verified, player is kicked and a failed login attempt is logged.
        /// Note that players connecting from localhost (127.0.0.1) are always allowed. </summary>
        Always
    }


    /// <summary> Describes the way player's rank was set. </summary>
    public enum RankChangeType : byte {
        /// <summary> Default rank (never been promoted or demoted). </summary>
        Default = 0,

        /// <summary> Promoted manually by another player or by console. </summary>
        Promoted = 1,

        /// <summary> Demoted manually by another player or by console. </summary>
        Demoted = 2,

        /// <summary> Promoted automatically (e.g. by AutoRank). </summary>
        AutoPromoted = 3,

        /// <summary> Demoted automatically (e.g. by AutoRank). </summary>
        AutoDemoted = 4
    }


    /// <summary> Bandwidth use mode.
    /// This setting affects the way player receive movement updates. </summary>
    public enum BandwidthUseMode : byte {
        /// <summary> Use server default. </summary>
        Default = 0,

        /// <summary> Very low bandwidth (choppy player movement, players pop-in/pop-out in the distance). </summary>
        VeryLow = 1,

        /// <summary> Lower bandwidth use (less choppy, pop-in distance is further). </summary>
        Low = 2,

        /// <summary> Normal mode (pretty much no choppiness, pop-in only noticeable when teleporting). </summary>
        Normal = 3,

        /// <summary> High bandwidth use (pretty much no choppiness, pop-in only noticeable when teleporting on large maps). </summary>
        High = 4,

        /// <summary> Very high bandwidth use (no choppiness at all, no pop-in). </summary>
        VeryHigh = 5
    }


    /// <summary> A list of possible results of Player.CanPlace() permission test. </summary>
    public enum CanPlaceResult {

        /// <summary> Block may be placed/changed. </summary>
        Allowed,

        /// <summary> Block was out of bounds in the given map. </summary>
        OutOfBounds,

        /// <summary> Player was not allowed to place or replace blocks of this particular block type. </summary>
        BlockTypeDenied,

        /// <summary> Player was not allowed to build on this particular world. </summary>
        WorldDenied,

        /// <summary> Player was not allowed to build in this particular zone.
        /// Use World.Map.FindDeniedZone() to find the specific zone. </summary>
        ZoneDenied,

        /// <summary> Player's rank is not allowed to build or delete in general. </summary>
        RankDenied,

        /// <summary> A plugin callback cancelled block placement/deletion.
        /// To keep player's copy of the map in sync, he will be resent the old block type at that location. </summary>
        PluginDenied,

        /// <summary> A plugin callback cancelled block placement/deletion.
        /// A copy of the old block will not be sent to the player (he may go out of sync). </summary>
        PluginDeniedNoUpdate
    }


    /// <summary> List possible reasons for players joining/changing worlds. </summary>
    public enum WorldChangeReason {
        /// <summary> First world that the player joins upon entering the server (main). </summary>
        FirstWorld,

        /// <summary> Rejoining the same world (e.g. after /wflush or /wload). </summary>
        Rejoin,

        /// <summary> Manually by typing /join. </summary>
        ManualJoin,

        /// <summary> Teleporting to a player on another map. </summary>
        Tp,

        /// <summary> Bring brought by a player on another map. Also used by /bringall, /wbring, and /setspawn. </summary>
        Bring,

        /// <summary> Following the /spectate target to another world. </summary>
        SpectateTargetJoined,

        /// <summary> Previous world was removed with /wunload. </summary>
        WorldRemoved,

        /// <summary> Previous world's access permissions changed, and player was forced to main. </summary>
        PermissionChanged
    }


    /// <summary> Lists possible ban states of players (banned, not banned, and exempt). </summary>
    public enum BanStatus : byte {
        /// <summary> Player is not banned. </summary>
        NotBanned,

        /// <summary> Player cannot be banned or IP-banned. </summary>
        IPBanExempt,

        /// <summary> Player is banned. </summary>
        Banned
    }


    /// <summary> Describes the action that player performed to click a block (left or right click). </summary>
    public enum ClickAction : byte {
        /// <summary> Deleting a block (left-click in Minecraft). </summary>
        Delete = 0,

        /// <summary> Building a block (right-click in Minecraft). </summary>
        Build = 1
    }


    /// <summary> Describes the state of a connected Player's session. </summary>
    public enum SessionState {
        /// <summary> There is no session associated with this player (e.g. Console). </summary>
        Offline,

        /// <summary> Player is in the middle of the login sequence. </summary>
        Connecting,

        /// <summary> Player has logged in, and is loading the first world. </summary>
        LoadingMain,

        /// <summary> Player is fully connected and online. </summary>
        Online,

        /// <summary> Player was kicked, and is about to be disconnected. </summary>
        PendingDisconnect,

        /// <summary> Session has ended - player disconnected. </summary>
        Disconnected
    }


    /// <summary> Type of Minecraft.net account associated with the player. </summary>
    public enum AccountType : byte {
        /// <summary> Unknown (could be free or paid).
        /// Default value for players whose accounts haven't been checked. </summary>
        Unknown = 0,

        /// <summary> Free minecraft.net account.
        /// Keep in mind that any free account can become paid some time in the future. </summary>
        Free = 1,

        /// <summary> Paid minecraft.net account. </summary>
        Paid = 2
    }


    /// <summary> Options for kicking of players. Used by Player.Kick(...) </summary>
    [Flags]
    public enum KickOptions {
        /// <summary> Set none of the options. Kick will be silent,
        /// no events will be raised, and PlayerDB record will not be affected. </summary>
        None = 0,

        /// <summary> If set, causes the kick to be publicly announced (in-game and on IRC). </summary>
        Announce = 1,

        /// <summary> If set, causes Player.BeingKicked and Player.Kicked events to be raised.
        /// Note that BeingKicked event allows cancellation. </summary>
        RaiseEvents = 2,

        /// <summary> If set, the kick goes on player's permanent record (in /Info).
        /// Kick count is incremented, and kicked-by/kick-date/kick-reason are set. </summary>
        RecordToPlayerDB = 4,

        /// <summary> Default options ("Announce", "RaiseEvents", and "RecordToPlayerDB" are set). </summary>
        Default = Announce | RaiseEvents | RecordToPlayerDB
    }


    /// <summary> Options for promotion/demotion of players. Used by PlayerInfo.ChangeRank(...). </summary>
    [Flags]
    public enum ChangeRankOptions {
        /// <summary> Set none of the options. Rank change will be silent,
        /// no events will be raised, and "auto" flag will not be set. </summary>
        None = 0,

        /// <summary> If set, causes promotion/demotion to be publicly announced (in-game and on IRC). </summary>
        Announce = 1,

        /// <summary> If set, causes PlayerInfo.RankChanging and PlayerInfo.RankChanged events to be raised.
        /// Note that RankChanging event allows cancellation. </summary>
        RaiseEvents = 2,

        /// <summary> If set, rank change will be marked as "automatic" (as opposed to manual).
        /// Shown in /Info and may be used for AutoRank criteria. </summary>
        Auto = 4,

        /// <summary> Default options ("Announce" and "RaiseEvents" are set, "Auto" is NOT set). </summary>
        Default = Announce | RaiseEvents
    }


    /// <summary> Options for freezing/unfreezing players.
    /// Used by PlayerInfo.Freeze(...) and PlayerInfo.Unfreeze(...). </summary>
    [Flags]
    public enum FreezeOptions {
        /// <summary> Set none of the options.
        /// Freezing/unfreezing will be silent, and no events will be raised. </summary>
        None = 0,

        /// <summary> If set, announces freezing/unfreezing publicly on the server. </summary>
        Announce = 1,

        /// <summary> If set, causes PlayerInfo.FreezeChanging and PlayerInfo.FreezeChanged events to be raised.
        /// Note that FreezeChanging event allows cancellation. </summary>
        RaiseEvents = 2,

        /// <summary> No options ("Announce" and "RaiseEvents" are set). </summary>
        Default = Announce | RaiseEvents
    }
}