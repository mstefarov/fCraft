using System;

namespace fCraft {
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
        Say,

        UseSpeedHack,

        Kick,
        Ban,
        BanIP,
        BanAll,

        Promote,
        Demote,
        Hide,         // go invisible!
        //Spectate,     // spectate others

        Draw,
        CopyAndPaste,

        Teleport,
        Bring,
        Freeze,
        SetSpawn,
        Lock,

        ManageZones,
        ManageWorlds,
        Import,
        
        ControlPhysics,

        AddLandmarks
    }
}
