using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fCraft {

    public enum BlockChangeCause : byte {
        Built       = 0,
        Deleted     = 1,
        Painted     = 2,
        Drawn       = 3,
        Replaced    = 4,
        Pasted      = 5,
        Undone      = 6,
        Restored    = 7
    }


    public struct BlockChangeRecord {
        public int PlayerID;
        public short X;
        public short Y;
        public short H;
        public Block OldBlock, NewBlock;
        public int Timestamp;
        public BlockChangeCause Cause;

        public BlockChangeRecord( BinaryReader reader ) {
            PlayerID = reader.ReadInt32();
            Cause = (BlockChangeCause)((PlayerID >> 24) & 0xFF);
            PlayerID &= 0x00FFFFFF;
            X = reader.ReadInt16();
            Y = reader.ReadInt16();
            H = reader.ReadInt16();
            OldBlock = (Block)reader.ReadByte();
            NewBlock = (Block)reader.ReadByte();
            Timestamp = reader.ReadInt32();
        }

        public void Serialize( BinaryWriter writer ) {
            writer.Write( PlayerID );
            writer.Write( X );
            writer.Write( Y );
            writer.Write( H );
            writer.Write( (byte)OldBlock );
            writer.Write( (byte)NewBlock );
            writer.Write( Timestamp );
            writer.Write( (byte)Cause );
        }

        static readonly DateTime UnixEpoch = new DateTime( 1970, 1, 1 );

        static int DateTimeToTimestamp( DateTime timestamp ) {
            return (int)(timestamp - UnixEpoch).TotalSeconds;
        }

        static DateTime TimestampToDateTime( int timestamp ) {
            return UnixEpoch.AddSeconds( timestamp );
        }
    }


    class BlockChangeDB {
    }
}
