using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    enum LeaveReason {
        Normal = 0,
        Error = 1,
        Kick = 2,
        AFKKick = 3,
        AntiGriefKick = 4,
        InvalidMessageKick = 5,
        InvalidSetTileKick = 6,
        InvalidOpcodeKick = 7,
        AntiBlockSpamKick = 8,
        AntiMessageSpamKick = 9,
        AntiMovementSpamKick = 10,
        LeavingMapKick = 11,
        Ban = 12,
        BanIP = 13,
        BanAll = 14,
        ServerShutdown = 15
    }

    public enum ReservedPlayerID {
        Unknown = 0,
        None = 1,
        Console = 2,
        Bot = 3
    }

    enum BanMethod {
        Ban = 0,
        IPBan = 1,
        BanAll = 2,
        Import = 3
    }

    enum UnbanMethod {
        Unban = 0,
        UnbanIP = 1,
        UnbanAll = 2
    }

    enum PlayerState {
        Unknown,
        Online,
        Offline,
        Banned
    }
}
