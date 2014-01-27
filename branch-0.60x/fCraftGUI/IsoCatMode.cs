// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft.GUI {
    /// <summary> Drawing/clipping mode of IsoCat map renderer. </summary>
    public enum IsoCatMode {
        /// <summary> Normal isometric view. </summary>
        Normal,

        /// <summary> Isometric view with the outermost layer of blocks stripped (useful for boxed maps). </summary>
        Peeled,

        /// <summary> Isometric view with a front-facing quarter of the map cut out (to show map cross-section). </summary>
        Cut,

        /// <summary> Only a specified chunk of the map is drawn. </summary>
        Chunk
    }
}