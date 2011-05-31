// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {
    /// <summary> List of possible reasons for players leaving the server. </summary>
    public enum LeaveReason {
        /// <summary> Unknown leave reason (default) </summary>
        Unknown = 0x00,

        /// <summary> Client exited normally </summary>
        Quit = 0x01,

        /// <summary> Client reconnected before old session timed out, or connected from another IP. </summary>
        ClientReconnect = 0x03,

        /// <summary> Manual or miscellaneous kick </summary>
        Kick = 0x10,

        /// <summary> Kicked for being idle/AFK. </summary>
        IdleKick = 0x11,

        /// <summary> Invalid characters in a message </summary>
        InvalidMessageKick = 0x12,

        /// <summary> Attempted to place invalid blocktype </summary>
        InvalidSetTileKick = 0x13,

        /// <summary> Unknown opcode or packet </summary>
        InvalidOpcodeKick = 0x14,

        /// <summary> Triggered antigrief / block spam </summary>
        BlockSpamKick = 0x15,

        /// <summary> Kicked for message spam (after warnings) </summary>
        MessageSpamKick = 0x16,

        /// <summary> Banned directly by name </summary>
        Ban = 0x20,

        /// <summary> Banned indirectly by /banip </summary>
        BanIP = 0x21,

        /// <summary> Banned indirectly by /banall </summary>
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
        LoginFailed = 0x43,
    }


    /// <summary> Used to distinguish actual players from special-purpose entities/states.
    /// Real PlayerDB IDs start at 256 </summary>
    public enum ReservedPlayerID {
        None = 0, // no one (certain) - initial state for generated maps
        Unknown = 1, // unknown (uncertain) - initial state for imported maps
        Console = 2,
        IRCBot = 3, // IRC bot
        Automatic = 4, // For auto-bans / auto-kicks / etc
        Physics = 5

        // 6-31 are reserved for fCraft
        // 32-255 are available for plugins
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
    public enum RankChangeType {
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
    public enum BandwidthUseMode {
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

        /// <summary> Player was not allowed to place or replace blocks of this particular blocktype. </summary>
        BlocktypeDenied,

        /// <summary> Player was not allowed to build on this particular world. </summary>
        WorldDenied,

        /// <summary> Player was not allowed to build in this particular zone.
        /// Use World.Map.FindDeniedZone() to find the specific zone. </summary>
        ZoneDenied,

        /// <summary> Player's rank is not allowed to build or delete in general. </summary>
        RankDenied,

        /// <summary> A plugin callback cancelled block placement/deletion.
        /// To keep player's copy of the map in sync, he will be resent the old blocktype at that location. </summary>
        PluginDenied,

        /// <summary> A plugin callback cancelled block placement/deletion.
        /// A copy of the old block will not be sent to the player (he may go out of sync). </summary>
        PluginDeniedNoUpdate
    }
}