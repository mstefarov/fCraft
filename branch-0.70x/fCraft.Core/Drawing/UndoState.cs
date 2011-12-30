// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class UndoState {
        public UndoState( DrawOperation op ) {
            Op = op;
            Buffer = new List<UndoBlock>();
        }

        public readonly DrawOperation Op;
        public readonly List<UndoBlock> Buffer;
        public bool IsTooLargeToUndo;
        public readonly object SyncRoot = new object();

        public bool Add( Vector3I coord, Block block ) {
            lock( SyncRoot ) {
                if( BuildingCommands.MaxUndoCount < 1 || Buffer.Count <= BuildingCommands.MaxUndoCount ) {
                    Buffer.Add( new UndoBlock( coord, block ) );
                    return true;
                } else if( !IsTooLargeToUndo ) {
                    IsTooLargeToUndo = true;
                    Buffer.Clear();
                }
                return false;
            }
        }

        [Pure]
        public UndoBlock Get( int index ) {
            lock( SyncRoot ) {
                return Buffer[index];
            }
        }

        /// <summary> Calculates bounds of the affected area.
        /// Can be very resource-intensive because it requires iteration over he whole buffer. </summary>
        /// <returns> A BoundingBox that bounds the affected area, or BoundingBox.Empty is buffer is empty. </returns>
        [Pure]
        [NotNull]
        public BoundingBox GetBounds() {
            lock( SyncRoot ) {
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