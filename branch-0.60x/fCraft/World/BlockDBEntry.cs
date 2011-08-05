// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Runtime.InteropServices;
using System.IO;

namespace fCraft{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BlockDBEntry {
        public BlockDBEntry( int timestamp, int playerID, short x, short y, short z, Block oldBlock, Block newBlock ) {
            Timestamp = timestamp;
            PlayerID = playerID;
            X = x;
            Y = y;
            Z = z;
            OldBlock = oldBlock;
            NewBlock = newBlock;
        }
        public readonly int Timestamp, PlayerID;
        public readonly short X, Y, Z;
        public readonly Block OldBlock, NewBlock;

        public void Serialize( BinaryWriter writer ) {
            writer.Write( Timestamp );
            writer.Write( PlayerID );
            writer.Write( X );
            writer.Write( Y );
            writer.Write( Z );
            writer.Write( (byte)OldBlock );
            writer.Write( (byte)NewBlock );
        }
    }
}
