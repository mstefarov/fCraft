// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Describes a copied chunk of a map. </summary>
    public sealed class CopyState : ICloneable {

        /// <summary> Array of copied blocks. </summary>
        [NotNull]
        public Block[, ,] Buffer { get; set; }

        /// <summary> Dimensions of the copied chunk of blocks. </summary>
        public Vector3I Dimensions {
            get {
                return new Vector3I( Buffer.GetLength( 0 ), Buffer.GetLength( 1 ), Buffer.GetLength( 2 ) );
            }
        }

        /// <summary> Orientation of the copied chunk of blocks, relative to Dimensions.MinVector. </summary>
        public Vector3I Orientation { get; set; }


        /// <summary> Copy slot into which this CopyState was saves. </summary>
        public int Slot { get; set; }


        /// <summary> Name of the world or location where this chunk of blocks was copied from. </summary>
        public string OriginWorld { get; set; }


        /// <summary> Time (UTC) at which this chunk of blocks was copied. </summary>
        public DateTime CopyTime { get; set; }


        /// <summary> Creates an empty CopyState (filled with air) between two given points.
        /// Order of the marks does not matter. </summary>
        public CopyState( Vector3I mark1, Vector3I mark2 ) {
            BoundingBox box = new BoundingBox( mark1, mark2 );
            Orientation = new Vector3I( mark1.X <= mark2.X ? 1 : -1,
                                        mark1.Y <= mark2.Y ? 1 : -1,
                                        mark1.Z <= mark2.Z ? 1 : -1 );
            Buffer = new Block[box.Width, box.Length, box.Height];
        }


        /// <summary> Creates a copy of an existing CopyState object. Duplicates the buffer. </summary>
        public CopyState( [NotNull] CopyState original ) {
            if( original == null ) throw new ArgumentNullException();
            Buffer = (Block[, ,])original.Buffer.Clone();
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }


        /// <summary> Creates a copy of an existing CopyState object. Replaces the buffer. </summary>
        public CopyState( [NotNull] CopyState original, [NotNull] Block[, ,] buffer ) {
            if( original == null ) throw new ArgumentNullException();
            Buffer = buffer;
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }


        /// <summary> Creates a copy of this CopyState object. Duplicates the buffer. </summary>
        [Pure]
        public object Clone() {
            return new CopyState( this );
        }
    }
}