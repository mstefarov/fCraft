// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {
    public enum LeaveReason {
        Unknown = 0x00,
        UserQuit = 0x01,
        UserTimeout = 0x02,
        UserReconnect = 0x03,

        Kick = 0x10,
        AFKKick = 0x11,
        InvalidMessageKick = 0x12,
        InvalidSetTileKick = 0x13,
        InvalidOpcodeKick = 0x14,
        AntiBlockSpamKick = 0x15,
        AntiMessageSpamKick = 0x16,
        PacketSpamKick = 0x17,

        Ban = 0x20,
        BanIP = 0x21,
        BanAll = 0x22,

        SoftwareError = 0x30,

        ServerShutdown = 0x40,
        WrongProtocol = 0x41,
        UnverifiedName = 0x42,
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
