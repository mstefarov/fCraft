// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {

    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public struct Packet {

        public readonly byte[] Data;

        public OpCode OpCode {
            get { return (OpCode)Data[0]; }
        }

        public Packet( byte[] data ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            Data = data;
        }


        /// <summary> Creates a packet of correct size for a given opcode,
        /// and sets the first (opcode) byte. </summary>
        public Packet( OpCode opcode ) {
            Data = new byte[PacketSizes[(int)opcode]];
            Data[0] = (byte)opcode;
        }


        /// <summary> Returns packet size (in bytes) for a given opcode.
        /// Size includes the opcode byte itself. </summary>
        public static int GetSize( OpCode opcode ) {
            return PacketSizes[(int)opcode];
        }


        static readonly int[] PacketSizes = {
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