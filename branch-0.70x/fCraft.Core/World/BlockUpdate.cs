// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Structure representing a pending update to the map's block array.
    /// Contains information about the block coordinates, type, and change's origin. </summary>
    public struct BlockUpdate {
        /// <summary>  Used for stat tracking. Can be null (to avoid crediting any stats at all). </summary>
        public readonly Player Origin;
 
        /// <summary> Position of the block update </summary>
        public readonly short X, Y, Z;

        /// <summary> The type of block being updated to </summary>
        public readonly Block BlockType;

        /// <summary> Creates a block update with the specified block, and at the specified location. </summary>
        /// <param name="origin"> Player who initiated the block update. Can be null. </param>
        /// <param name="x"> X coordinate of the position of the block change </param>
        /// <param name="y"> Y coordinate of the position of the block change </param>
        /// <param name="z"> Z coordinate of the position of the block change </param>
        /// <param name="blockType"> Block type to update to</param>
        public BlockUpdate( [CanBeNull] Player origin, short x, short y, short z, Block blockType ) {
            Origin = origin;
            X = x;
            Y = y;
            Z = z;
            BlockType = blockType;
        }

        /// <summary> Creates a block update with the specified block, and at the specified location. </summary>
        /// <param name="origin"> Player who initiated the block update. Can be null. </param>
        /// <param name="coord"> Vector3I representing the position of the block change</param>
        /// <param name="blockType"> Block type to update to </param>
        public BlockUpdate( [CanBeNull] Player origin, Vector3I coord, Block blockType ) {
            Origin = origin;
            X = (short)coord.X;
            Y = (short)coord.Y;
            Z = (short)coord.Z;
            BlockType = blockType;
        }
    }
}