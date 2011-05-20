// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft {

    /// <summary> Basic struct, just a wrapper for a byte array. </summary>
    public struct Packet {
        public readonly byte[] Data;

        public Packet( int length ) {
            Data = new byte[length];
        }

        public OpCode OpCode {
            get { return (OpCode)Data[0]; }
        }
    }


    /// <summary> Minecraft protocol's opcodes. </summary>
    public enum OpCode {
        Handshake = 0,
        Ping = 1,
        LevelBegin = 2,
        LevelChunk = 3,
        LevelEnd = 4,
        SetTileClient = 5,
        SetTileServer = 6,
        AddEntity = 7,
        Teleport = 8,
        MoveRotate = 9,
        Move = 10,
        Rotate = 11,
        RemoveEntity = 12,
        Message = 13,
        Disconnect = 14,
        SetPermission = 15
    };
}