using System;

namespace fCraft {
    /// <summary>
    /// See comment at the top of Config.cs for a history of changes.
    /// </summary>
    public enum Permission {
        Chat,
        Build,
        Delete,

        PlaceGrass,
        PlaceWater, // includes placing water blocks and changing water sim parameters
        PlaceLava,  // same as above, but with lava
        PlaceAdmincrete,  // build admincrete
        DeleteAdmincrete, // delete admincrete

        ViewOthersInfo,
        EditPlayerDB,
        Say,
        ReadStaffChat,

        UseSpeedHack,

        Kick,
        Ban,
        BanIP,
        BanAll,

        Promote,
        Demote,
        Hide,         // go invisible!
        //Spectate,   // spectate others

        Draw,
        CopyAndPaste,

        Teleport,
        Bring,
        Patrol,
        Freeze,
        Mute,
        SetSpawn,
        Lock,

        ManageZones,
        ManageWorlds,
        Import,

        ReloadConfig,
        ShutdownServer
    }
}
