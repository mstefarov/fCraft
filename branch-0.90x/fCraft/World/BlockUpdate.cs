// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Structure representing a pending update to the map's block array.
    /// Contains information about the block coordinates, type, and change's origin. </summary>
    public struct BlockUpdate {
        /// <summary> Player who initiated the block change. Can be null. </summary>
        [CanBeNull] public readonly Player Origin;

        public readonly short X, Y, Z;

        /// <summary> Type of block to set at the given coordinates. </summary>
        public readonly Block BlockType;

        public BlockUpdate( Player origin, short x, short y, short z, Block blockType ) {
            Origin = origin;
            X = x;
            Y = y;
            Z = z;
            BlockType = blockType;
        }

        public BlockUpdate( Player origin, Vector3I coord, Block blockType ) {
            Origin = origin;
            X = (short)coord.X;
            Y = (short)coord.Y;
            Z = (short)coord.Z;
            BlockType = blockType;
        }
    }
}