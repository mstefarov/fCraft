// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Minecraft protocol's opcodes. </summary>
    public enum OpCode {
        /// <summary> Exchanges initial information between client and server </summary>
        Handshake = 0,
        Ping = 1,
        /// <summary> Signals the beginning of a map transfer </summary>
        MapBegin = 2,
        /// <summary> A single chunk of the map </summary>
        MapChunk = 3,
        /// <summary> Signals the end of a map transfer </summary>
        MapEnd = 4,
        /// <summary> Signals a block change to be made </summary>
        SetBlockClient = 5,
        /// <summary> Signals a block change to be made </summary>
        SetBlockServer = 6,
        /// <summary> Signals the client to add an entity </summary>
        AddEntity = 7,
        /// <summary> Teleports the player to a location </summary>
        Teleport = 8,
        /// <summary> Moves the player's position, and rotates where they are looking </summary>
        MoveRotate = 9,
        /// <summary> Moves the player's position </summary>
        Move = 10,
        /// <summary> Rotates where a player is facing </summary>
        Rotate = 11,
        /// <summary> Signals the client to remove an entity </summary>
        RemoveEntity = 12,
        /// <summary> A text message </summary>
        Message = 13,
        /// <summary> Signals the client that is has been kicked </summary>
        Kick = 14,
        /// <summary> Used to set Operator status, toggles client-side destruction of bedrock </summary>
        SetPermission = 15
    }
}