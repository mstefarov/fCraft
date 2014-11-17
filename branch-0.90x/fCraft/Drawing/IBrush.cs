// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Class that describes a configured brush, and allows creating instances for specific DrawOperations.
    /// Configuration-free brush types may combine IBrushFactory and IBrush into one class. </summary>
    public interface IBrush {
        /// <summary> IBrushFactory associated with this brush type. </summary>
        [NotNull]
        IBrushFactory Factory { get; }

        /// <summary> A compact readable summary of brush type and configuration. </summary>
        [NotNull]
        string Description { get; }

        /// <summary> Whether the brush is capable of providing alternate blocks (e.g. for filling hollow DrawOps).</summary>
        int AlternateBlocks { get; }


        /// <summary> Called when the DrawOperation starts. Should be used to verify that the brush is ready for use.
        /// Resources used by the brush should be obtained here. </summary>
        /// <param name="player"> Player who started the DrawOperation. </param>
        /// <param name="op"> DrawOperation that will be using this brush. </param>
        /// <returns> Whether this brush instance has successfully began or not. </returns>
        bool Begin([NotNull] Player player, [NotNull] DrawOperation op);


        /// <summary> Provides the next Block type for the given DrawOperation. </summary>
        /// <returns> Block type to place, or Block.Undefined to skip. </returns>
        Block NextBlock([NotNull] DrawOperation op);


        /// <summary> Called when the DrawOperation is done or cancelled.
        /// Resources used by the brush should be freed/disposed here. </summary>
        void End();


        /// <summary> Creates a copy of this brush.
        /// If the brush is stateless, Brush may return "this" without any copying.
        /// Otherwise a deep or a shallow copy may be returned. </summary>
        [NotNull]
        IBrush Clone();
    }
}
