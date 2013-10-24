// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft {
    /// <summary> Minecraft protocol's opcodes. 
    /// For detailed explanation of Minecraft Classic protocol, see http://wiki.vg/Classic_Protocol </summary>
    public enum OpCode {
        /// <summary> Client/server packet. Client provides name and mppass.
        /// Server responds with name, MOTD, and permission byte. </summary>
        Handshake = 0,

        /// <summary> Server packet. Send periodically to test connection status. </summary>
        Ping = 1,

        /// <summary> Server packet. Notifies player of incoming level data. </summary>
        MapBegin = 2,

        /// <summary> Server packet. Contains a chunk of gzipped map. </summary>
        MapChunk = 3,

        /// <summary> Server packet. Sent after level data is complete and gives map dimensions. </summary>
        MapEnd = 4,

        /// <summary> Client packet. Sent when a user changes a block. </summary>
        SetBlockClient = 5,

        /// <summary> Server packet. Sent to indicate a block change. </summary>
        SetBlockServer = 6,

        /// <summary> Server packet. Spawns a player model. Also used to set player's respawn point. </summary>
        AddEntity = 7,

        /// <summary> Client/server packet. Used by client to update player's position. 
        /// Used by server to teleport player or update position of other players. </summary>
        Teleport = 8,

        /// <summary> Server packet. Updates relative location and rotation of other players. </summary>
        MoveRotate = 9,

        /// <summary> Server packet. Updates relative location of other players. </summary>
        Move = 10,

        /// <summary> Server packet. Updates rotation of other players. </summary>
        Rotate = 11,

        /// <summary> Server packet. De-spawns a player model. </summary>
        RemoveEntity = 12,

        /// <summary> Client/server packet. Used to send chat messages. </summary>
        Message = 13,

        /// <summary> Server packet. Tells client that they're being kicked. </summary>
        Kick = 14,

        /// <summary> Server packet. Sets permission to delete admincrete. </summary>
        SetPermission = 15,

        /// <summary> Extended client/server packet. Initiates CPE negotiation. </summary>
        ExtInfo = 16,

        /// <summary> Extended client/server packet. Lists supported extensions. </summary>
        ExtEntry = 17,

        /// <summary> Extended server packet. Changes player's allowed click distance. </summary>
        SetClickDistance = 18,

        /// <summary> Extended client/server packet. Declares CustomBlocks support level. </summary>
        CustomBlockSupportLevel = 19,

        /// <summary> Extended server packet. Tells client which block to hold. </summary>
        HoldThis = 20,

        /// <summary> Extended server packet. Defines chat macros ties to hotkeys. </summary>
        SetTextHotKey = 21,

        /// <summary> Extended server packet. Adds or updates a name to the player list. </summary>
        ExtAddPlayerName = 22,

        /// <summary> Extended server packet. Adds or updates an entity (replaces AddEntity). </summary>
        ExtAddEntity = 23,

        /// <summary> Extended server packet. Removes a name from the player list. </summary>
        ExtRemovePlayerName = 24,

        /// <summary> Extended server packet. Sets environmental colors (sky/cloud/fog/ambient/diffuse color). </summary>
        EnvSetColor = 25,

        /// <summary> Extended server packet. Adds or updates a selection cuboid. </summary>
        MakeSelection = 26,

        /// <summary> Extended server packet. Removes a selection cuboid. </summary>
        RemoveSelection = 27,

        /// <summary> Extended server packet. Sets permission to place/delete a block type (replaces SetPermission). </summary>
        SetBlockPermission = 28,

        /// <summary> Allows changing the 3D model that entity/player shows up as. </summary>
        ChangeModel = 29,

        /// <summary> This extension allows the server to specify custom terrain textures, and tweak appearance of map edges. </summary>
        EnvMapAppearance = 30
    }
}
