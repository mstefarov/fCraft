// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Object used to store </summary>
    public sealed class UndoState {
        /// <summary> Creates a new UndoState for the given DrawOperation. <param name="op"/> can be null. </summary>
        public UndoState( [CanBeNull] DrawOperation op ) {
            Op = op;
        }


        /// <summary> Associated DrawOperation. May be null. </summary>
        [CanBeNull]
        public readonly DrawOperation Op;

        /// <summary> List of block changes that can be undone. </summary>
        [NotNull]
        public readonly List<UndoBlock> Buffer = new List<UndoBlock>();

        /// <summary> Whether the operation became too large to undo (in which case Buffer will be empty). </summary>
        public bool IsTooLargeToUndo;

        /// <summary> Object used to synchronize reading/writing of blocks.
        /// Necessary in case drawing/undo/redo end up running concurrently. </summary>
        [NotNull]
        public readonly object SyncRoot = new object();


        /// <summary> Records a new block change. Synchronized. </summary>
        /// <returns> True if block change was recorded; otherwise false.
        /// Changes will not be recorded if undo is disabled, or if max undo size was exceeded. </returns>
        public bool Add( Vector3I coord, Block block ) {
            lock( SyncRoot ) {
                if( BuildingCommands.MaxUndoCount < 1 || Buffer.Count <= BuildingCommands.MaxUndoCount ) {
                    Buffer.Add( new UndoBlock( coord, block ) );
                    return true;
                } else if( !IsTooLargeToUndo ) {
                    IsTooLargeToUndo = true;
                    Buffer.Clear();
                    Buffer.TrimExcess();
                }
                return false;
            }
        }


        /// <summary> Gets block change at the specified index. Synchronized. </summary>
        public UndoBlock Get( int index ) {
            lock( SyncRoot ) {
                return Buffer[index];
            }
        }


        /// <summary> Calculates the bounding box within which all recorded blocks are located.
        /// Quite slow, because every recorded block change needs to be checked in order. </summary>
        [NotNull]
        public BoundingBox CalculateBounds() {
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


    /// <summary> Stores state of a block at a particular coordinate, used by UndoState. </summary>
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