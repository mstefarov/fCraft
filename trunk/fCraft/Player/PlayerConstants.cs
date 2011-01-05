// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {
    public enum LeaveReason {
        Unknown = 0,
        UserQuit = 1,
        UserTimeout = 2,
        Error = 3,
        Kick = 4,
        AFKKick = 5,
        AntiGriefKick = 6,
        InvalidMessageKick = 7,
        InvalidSetTileKick = 8,
        InvalidOpcodeKick = 9,
        AntiBlockSpamKick = 10,
        AntiMessageSpamKick = 11,
        PacketSpamKick = 12,
        Ban = 13,
        BanIP = 14,
        BanAll = 15,
        ServerShutdown = 16
    }

    // Used to distinguish actual players from special cases
    // real PlayerDB IDs start at 256


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
