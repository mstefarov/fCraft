// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Structure representing a pending update to the map's block array.
    /// Contains information about the block coordinates, type, and change's origin.
    /// Immutable. </summary>
    public struct BlockUpdate {
        /// <summary> Player who initiated the block change. May be null. </summary>
        [CanBeNull]
        public readonly Player Origin;

        /// <summary> X coordinate (along the width). </summary>
        public readonly short X;

        /// <summary> Y coordinate (along the length). </summary>
        public readonly short Y;

        /// <summary> X coordinate (along the height). </summary>
        public readonly short Z;

        /// <summary> Type of block to set at the given coordinates. </summary>
        public readonly Block BlockType;

        /// <summary> Creates a new BlockUpdate struct. </summary>
        public BlockUpdate( [CanBeNull] Player origin, Vector3I coord, Block blockType ) {
            Origin = origin;
            X = (short)coord.X;
            Y = (short)coord.Y;
            Z = (short)coord.Z;
            BlockType = blockType;
        }
    }
}
