// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Represents a set of copied blocks, including metadata.
    /// Created by /Copy and /Cut commands. </summary>
    public sealed class CopyState : ICloneable {
        public CopyState( Vector3I mark1, Vector3I mark2 ) {
            Bounds = new BoundingBox( mark1, mark2 );
            Orientation = new Vector3I( mark1.X <= mark2.X ? 1 : -1,
                                        mark1.Y <= mark2.Y ? 1 : -1,
                                        mark1.Z <= mark2.Z ? 1 : -1 );
            Blocks = new Block[Bounds.Width, Bounds.Length, Bounds.Height];
        }


        public CopyState( [NotNull] CopyState original ) {
            if( original == null ) throw new ArgumentNullException();
            Blocks = (Block[, ,])original.Blocks.Clone();
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }


        public CopyState( [NotNull] CopyState original, [NotNull] Block[, ,] buffer ) {
            if( original == null ) throw new ArgumentNullException();
            Blocks = buffer;
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }


        /// <summary> 3D array of copies blocks. </summary>
        public Block[, ,] Blocks { get; private set; }


        /// <summary> Dimensions and coordinates of the copied blocks. </summary>
        public BoundingBox Bounds { get; private set; }


        /// <summary> Orientation of copying (relation of two marks to each other).
        /// Each value is either 1 (forwards along the axis) or -1 (backwards along the axis).
        /// Orientation is used by /Paste and /PasteNot commands to determine the direction of pasting from the clicked block. </summary>
        public Vector3I Orientation { get; private set; }


        /// <summary> Index of the copyslot into which this was copied. </summary>
        public int Slot { get; set; }


        /// <summary> Name of the world where this was copied from. </summary>
        public string OriginWorld { get; set; }


        /// <summary> Time (UTC) at which the blocks were copied. </summary>
        public DateTime CopyTime { get; set; }


        /// <summary> Description of the corner at which copying started (e.g. "bottom southeast") </summary>
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