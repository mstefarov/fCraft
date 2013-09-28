// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Represents a set of copied blocks, including metadata.
    /// Created by /Copy and /Cut commands. </summary>
    public sealed class CopyState : ICloneable {
        /// <summary> Creates a new CopyState between the two given marks.
        /// First mark is the origin. Bounds and Orientation are set accordingly.
        /// Also allocates Blocks array and sets CopyTime to UtcNow. </summary>
        public CopyState( Vector3I mark1, Vector3I mark2 ) {
            Bounds = new BoundingBox( mark1, mark2 );
            Orientation = new Vector3I( mark1.X <= mark2.X ? 1 : -1,
                                        mark1.Y <= mark2.Y ? 1 : -1,
                                        mark1.Z <= mark2.Z ? 1 : -1 );
            Blocks = new Block[Bounds.Width, Bounds.Length, Bounds.Height];
            CopyTime = DateTime.UtcNow;
        }


        /// <summary> Duplicates the given CopyState.
        /// Note that this is a deep copy -- Blocks array and everything else is duplicated too. </summary>
        public CopyState( [NotNull] CopyState original ) {
            if( original == null ) throw new ArgumentNullException();
            Blocks = (Block[, ,])original.Blocks.Clone();
            Bounds = new BoundingBox( original.Bounds );
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }



        /// <summary> Duplicates the given CopyState, but does not copy the Blocks array.
        /// Updates Bounds to match the new buffer's size, but preserves original Orientation. </summary>
        public CopyState( [NotNull] CopyState original, [NotNull] Block[, ,] buffer ) {
            if( original == null ) throw new ArgumentNullException();
            Blocks = buffer;
            Bounds = new BoundingBox( original.Bounds.MinVertex,
                                      buffer.GetLength( 0 ),
                                      buffer.GetLength( 1 ),
                                      buffer.GetLength( 2 ) );
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }


        /// <summary> 3D array of copies blocks. </summary>
        [NotNull]
        public Block[, ,] Blocks { get; private set; }


        /// <summary> Dimensions and coordinates of the copied blocks. </summary>
        [NotNull]
        public BoundingBox Bounds { get; private set; }


        /// <summary> Orientation of copying (relation of two marks to each other).
        /// Each value is either 1 (forwards along the axis) or -1 (backwards along the axis).
        /// Orientation is used by /Paste and /PasteNot commands to determine the direction of pasting from the clicked block. </summary>
        public Vector3I Orientation { get; private set; }


        /// <summary> Index of the copySlot into which this was copied.
        /// Defaults to 0. </summary>
        public int Slot { get; set; }


        /// <summary> Name of the world where this was copied from.
        /// Defaults to null. </summary>
        [CanBeNull]
        public string OriginWorld { get; set; }


        /// <summary> Time (UTC) at which the blocks were copied. 
        /// Defaults to DateTime.UtcNow. </summary>
        public DateTime CopyTime { get; set; }


        /// <summary> Description of the corner at which copying started (e.g. "bottom southeast") </summary>
        [NotNull]
        public string OriginCorner {
            get {
                return String.Format( "{0} {1}{2}",
                                      (Orientation.Z == 1 ? "bottom" : "top"),
                                      (Orientation.Y == 1 ? "south" : "north"),
                                      (Orientation.X == 1 ? "east" : "west") );
            }
        }


        public object Clone() {
            return new CopyState( this );
        }
    }
}