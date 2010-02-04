using System;
using System.IO;
using System.Text;

namespace fCraft {
    // Basic packet, essentially a byte array
    struct Packet {
        public byte[] data;
        public Packet( int length ) {
            data = new byte[length];
        }
    }

    public enum InputCodes {
        Handshake = 0,
        SetTile = 5,
        MoveRotate = 8,
        Message = 13
    };

    public enum OutputCodes {
        Handshake = 0,
        Ping = 1,
        LevelBegin = 2,
        LevelChunk = 3,
        LevelEnd = 4,
        SetTile = 6,
        AddEntity = 7,
        Teleport = 8,
        MoveRotate = 9,
        Rotate = 10,
        Move = 11,
        RemoveEntity = 12,
        Message = 13,
        Disconnect = 14,
        SetPermission = 15
    };
}