using System;
using System.IO;
using System.Text;

namespace fCraft {
    // Basic packet, essentially a byte array
    sealed class Packet {
        public byte[] data;
        public uint opcode;
        private static int packetCounter = 0;

        public Packet( BinaryReader reader ) {
            opcode = reader.ReadByte();
            Logger.Log( "Packet: received packet, opcode " + opcode );
            int length = Packet.GetInputPacketLength( opcode );
            data = reader.ReadBytes( length );
            packetCounter++;

            // per-packet logging
            FileStream fs = File.Open( "packet" + packetCounter.ToString().PadLeft(5,'0') + ".dat", FileMode.Create );
            BinaryWriter bw = new BinaryWriter( fs );
            bw.Write( data );
            bw.Close();
        }

        public Packet( uint _opcode ) {
            opcode = _opcode;
            data = new byte[Packet.GetOutputPacketLength(opcode)];
        }


        // STATIC METHODS
        public static int GetOutputPacketLength( uint _opcode ) {
            if( _opcode < outputPacketLengths.Length && outputPacketLengths[_opcode] != UNUSED ) {
                return outputPacketLengths[_opcode];
            } else {
                Logger.LogError( "Packet.GetOutputPacketSize: unknown output opcode " + _opcode );
                return -1;
            }
        }

        public static int GetInputPacketLength( uint _opcode ) {
            if( _opcode < inputPacketLengths.Length && inputPacketLengths[_opcode] != UNUSED ) {
                return inputPacketLengths[_opcode];
            } else {
                Logger.LogError( "Packet.GetOutputPacketSize: unknown output opcode " + _opcode );
                return -1;
            }
        }

        public const int UNUSED = -1;

        // note: INCLUDES the opcode byte
        private static int[] outputPacketLengths = {
                                                       131, // Handshake
                                                       UNUSED,
                                                       1,   // LevelInit
                                                       1028,// LevelBlock
                                                       7,   // LevelFinish
                                                       UNUSED,
                                                       8,   // SetTile
                                                       74,  // AddEntity
                                                       10,  // Teleport
                                                       7,   // MoveRotate
                                                       4,   // Rotate
                                                       5,   // Move
                                                       2,   // RemoveEntity
                                                       66,  // Message
                                                       65,  // Disconnect
                                                       2    // SetPermission
                                                   };

        // note: EXCLUDES the opcode byte
        private static int[] inputPacketLengths = {
                                                       130, // Handshake
                                                       UNUSED,UNUSED,UNUSED,UNUSED,
                                                       8,   // SetTile
                                                       UNUSED,UNUSED,
                                                       9,  // MoveRotate
                                                       UNUSED,UNUSED,UNUSED,UNUSED,
                                                       65  // Message
                                                   };

        public static OutputCodes GetOutputCode( uint opcode ) {
            if( Enum.IsDefined( typeof( OutputCodes ), opcode ) ) {
                return (OutputCodes)opcode;
            } else {
                Logger.LogError( "Packet.GetOutputCode: packet length is undefined for output opcode " + opcode );
                return 0;
            }
        }

        public static InputCodes GetInputCode( uint opcode ) {
            if( Enum.IsDefined( typeof( InputCodes ), opcode ) ) {
                return (InputCodes)opcode;
            } else {
                Logger.LogError( "Protocol.GetInputCode: packet length is undefined for input opcode " + opcode );
                return 0;
            }
        }
    }

    public enum DataTypes {
        Byte = 1,
        Short = 2,
        Int = 4,
        Long = 8,
        String = 64,
        Data = 1024
    };

    public enum InputCodes {
        Handshake = 0,
        SetTile = 5,
        MoveRotate = 8,
        Message = 13
    };

    public enum OutputCodes {
        Handshake = 0,
        LevelInit = 2,
        LevelBlock = 3,
        LevelFinish = 4,
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