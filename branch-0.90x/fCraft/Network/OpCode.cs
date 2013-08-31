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

        /// <summary> Server packet. Sent when a player is opped/deopped. </summary>
        SetPermission = 15
    }
}