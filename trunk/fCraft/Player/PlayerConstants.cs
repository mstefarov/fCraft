// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {
    public enum LeaveReason {
        Unknown = 0x00,             // default
        ClientQuit = 0x01,          // client exited
        ClientTimeout = 0x02,       // client timed out
        ClientReconnect = 0x03,     // client reconnected before old session timed out

        Kick = 0x10,                // manual/misc kick
        IdleKick = 0x11,            // afk kick
        InvalidMessageKick = 0x12,  // invalid characters in message
        InvalidSetTileKick = 0x13,  // invalid blocktype
        InvalidOpcodeKick = 0x14,   // unknown opcode/packet
        BlockSpamKick = 0x15,       // triggered antigrief / block spam
        MessageSpamKick = 0x16,     // message spam (after warnings)
        MovementSpamKick = 0x17,    // movement packet spam (if speedhacks are not allowed)

        Ban = 0x20,                 // banned directly
        BanIP = 0x21,               // banned indirectly by /banip
        BanAll = 0x22,              // banned indirectly by /banall

        ServerError = 0x30,         // serverside error (uncaught exception)
        ServerShutdown = 0x31,      // server is shutting down
        ServerFull = 0x32,          // server is full

        ProtocolViolation = 0x41,   // login failed due to protocol violation/mismatch
        UnverifiedName = 0x42,      // login failed due to unverified player name
        LoginFailed = 0x43,         // login denied for some other reason
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
}
