// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {
    public enum LeaveReason {
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


    /// <summary>
    /// Used to distinguish actual players from special-purpose entities/states.
    /// Real PlayerDB IDs start at 256
    /// </summary>
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


    public enum RankChangeMethod {
        Manual = 0,
        AutoRank = 1,
        MassRank = 2,
        Import = 3
    }


    public enum BanMethod {
        Ban = 0,
        BanIP = 1,
        BanAll = 2,
        Import = 3
    }


    public enum UnbanMethod {
        Unban = 0,
        UnbanIP = 1,
        UnbanAll = 2
    }


    public enum NameVerificationMode {
        Never,
        Balanced,
        Always
    }
}
