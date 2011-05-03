// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>


namespace fCraft {

    // See comment at the top of Config.cs for a history of changes.

    /// <summary> Enumeration of permission types/categories.
    /// Every rank definition contains a combination of these. </summary>
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
        ViewPlayerIPs,
        EditPlayerDB,
        Say,
        ReadStaffChat,
        UseColorCodes,

        UseSpeedHack,

        Kick,
        Ban,
        BanIP,
        BanAll,
        //TODO: MakeBanExceptions, 

        Promote,
        Demote,

        Hide,         // go invisible!
        //TODO: Spectate,

        Draw,
        CopyAndPaste,

        Teleport,
        Bring,
        BringAll,
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
