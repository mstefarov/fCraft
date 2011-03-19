// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft {

    /// <summary>
    /// Basic struct, just a wrapper for a byte array
    /// </summary>
    public struct Packet {
        public byte[] Data;

        public Packet( int length ) {
            Data = new byte[length];
        }

        public OutputCode OpCode {
            get { return (OutputCode)Data[0]; }
        }
    }


    /// <summary>
    /// Minecraft protocol's opcodes for client-to-server (incoming) packets
    /// </summary>
    public enum InputCode {
        Handshake = 0,
        Ping = 1,
        SetTile = 5,
        MoveRotate = 8,
        Message = 13
    };


    /// <summary>
    /// Minecraft protocol's opcodes for server-to-client (outgoing) packets
    /// </summary>
    public enum OutputCode {
        Handshake = 0,
        Ping = 1,
        LevelBegin = 2,
        LevelChunk = 3,
        LevelEnd = 4,
        SetTile = 6,
        AddEntity = 7,
        Teleport = 8,
        MoveRotate = 9,
        Move = 10,
        Rotate = 11, // thanks liq3
        RemoveEntity = 12,
        Message = 13,
        Disconnect = 14,
        SetPermission = 15
    };
}