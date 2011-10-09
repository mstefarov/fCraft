using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace fCraft.Drawing {
    public sealed class UndoState {
        public UndoState( DrawOperation op ) {
            Op = op;
            Buffer = new List<UndoBlock>();
        }

        public readonly DrawOperation Op;
        public readonly List<UndoBlock> Buffer;
        public bool IsTooLargeToUndo;

        public bool Add( Vector3I coord, Block block ) {
            if( BuildingCommands.MaxUndoCount < 1 || Buffer.Count <= BuildingCommands.MaxUndoCount ) {
                Buffer.Add( new UndoBlock( coord, block ) );
                return true;
            } else if( !IsTooLargeToUndo ) {
                IsTooLargeToUndo = true;
                Buffer.Clear();
            }
            return false;
        }

        public bool Add( int x, int y, int z, Block block ) {
            if( BuildingCommands.MaxUndoCount < 1 || Buffer.Count <= BuildingCommands.MaxUndoCount ) {
                Buffer.Add( new UndoBlock( x,y,z, block ) );
                return true;
            } else if( !IsTooLargeToUndo ) {
                IsTooLargeToUndo = true;
                Buffer.Clear();
            }
            return false;
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 2 )]
    public struct UndoBlock {
        public UndoBlock( Vector3I coord, Block block ) {
            X = (short)coord.X;
            Y = (short)coord.Y;
            Z = (short)coord.Z;
            Block = block;
        }
        public UndoBlock( int x, int y, int z, Block block ) {
            X = (short)x;
            Y = (short)y;
            Z = (short)z;
            Block = block;
        }
        public readonly short X, Y, Z;
        public readonly Block Block;
    }
}