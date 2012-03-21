// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> An undo state, including a list of all blocks affected by an operation. </summary>
    public sealed class UndoState {

        /// <summary> DrawOperation associated with this UndoState. May be null. </summary>
        [CanBeNull]
        public readonly DrawOperation Op;


        /// <summary> List of UndoBlocks in this state. </summary>
        [NotNull]
        public readonly List<UndoBlock> Buffer;


        /// <summary> Whether this UndoState has become too large to undo.
        /// If the limit is reached, Buffer is cleared. </summary>
        public bool IsTooLargeToUndo { get; set; }


        [NotNull]
        readonly object syncRoot = new object();


        internal UndoState( [CanBeNull]DrawOperation op ) {
            Op = op;
            Buffer = new List<UndoBlock>();
        }


        /// <summary> Adds a new UndoBlock to the list. </summary>
        /// <param name="coord"> Coordinate at which block was changed. </param>
        /// <param name="block"> Original blocktype at the affected coordinate (before change). </param>
        /// <returns> True if block was added to the list; false if this UndoState has filled up. </returns>
        public bool Add( Vector3I coord, Block block ) {
            lock( syncRoot ) {
                if( IsTooLargeToUndo ) return false;
                if( BuildingCommands.MaxUndoCount < 1 || Buffer.Count <= BuildingCommands.MaxUndoCount ) {
                    Buffer.Add( new UndoBlock( coord, block ) );
                    return true;
                } else {
                    IsTooLargeToUndo = true;
                    Buffer.Clear();
                }
                return false;
            }
        }


        /// <summary> Gets an UndoBlock at the given index.
        /// Use UndoState.Buffer.Count to determine the upper bound on the index. </summary>
        [Pure]
        public UndoBlock Get( int index ) {
            lock( syncRoot ) {
                return Buffer[index];
            }
        }


        /// <summary> Calculates bounds of the affected area.
        /// Can be very resource-intensive because it requires iteration over he whole buffer. </summary>
        /// <returns> A BoundingBox that bounds the affected area, or BoundingBox.Empty is buffer is empty. </returns>
        [Pure]
        [NotNull]
        public BoundingBox GetBounds() {
            lock( syncRoot ) {
                if( Buffer.Count == 0 ) return BoundingBox.Empty;
                Vector3I min = new Vector3I( int.MaxValue, int.MaxValue, int.MaxValue );
                Vector3I max = new Vector3I( int.MinValue, int.MinValue, int.MinValue );
                for( int i = 0; i < Buffer.Count; i++ ) {
                    if( Buffer[i].X < min.X ) min.X = Buffer[i].X;
                    if( Buffer[i].Y < min.Y ) min.Y = Buffer[i].Y;
                    if( Buffer[i].Z < min.Z ) min.Z = Buffer[i].Z;
                    if( Buffer[i].X > max.X ) max.X = Buffer[i].X;
                    if( Buffer[i].Y > max.Y ) max.Y = Buffer[i].Y;
                    if( Buffer[i].Z > max.Z ) max.Z = Buffer[i].Z;
                }
                return new BoundingBox( min, max );
            }
        }
    }


    /// <summary> A struct representing a single block (coordinate + blocktype) in an UndoState buffer. </summary>
    [StructLayout( LayoutKind.Sequential, Pack = 2 )]
    public struct UndoBlock {
        public UndoBlock( Vector3I coord, Block block ) {
            X = (short)coord.X;
            Y = (short)coord.Y;
            Z = (short)coord.Z;
            Block = block;
        }

        public readonly short X, Y, Z;
        public readonly Block Block;
    }
}