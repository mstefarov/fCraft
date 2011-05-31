// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft {

    /// <summary> Basic struct, just a wrapper for a byte array. </summary>
    public struct Packet {
        public readonly byte[] Data;

        public Packet( OpCode opcode ) {
            Data = new byte[PacketSizes[(int)opcode]];
            Data[0] = (byte)opcode;
        }

        public OpCode OpCode {
            get { return (OpCode)Data[0]; }
        }

        static int[] PacketSizes = {
            131,    // Handshake
            1,      // Ping
            1,      // LevelBegin
            1028,   // LevelChunk
            7,      // LevelEnd
            9,      // SetTile (clientside)
            8,      // SetTile (serverside)
            74,     // AddEntity
            10,     // Teleport
            7,      // MoveRotate
            5,      // Move
            4,      // Rotate
            2,      // RemoveEntity
            66,     // Message
            65,     // Disconnect
            2       // SetPermission
        };
    }
}