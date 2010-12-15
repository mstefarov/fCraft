// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace fCraft {
    /// <summary>
    /// Flags that indicate what action resulted in block changing (currently unused, and subject to change).
    /// </summary>
    [Flags]
    public enum BlockChangeCauses {
        // value of 0 means "unaltered" and default for new maps

        Manual = 1, // On if block was changed by manual building (no tools)

        Painted = 2, // On if block was replaced by /paint or client-side paint
                     // (Also /replace or /rn, if combined with Drawn flag)

        Drawn = 4, // On if block was changed by /e, /c, /ch, /cw, or /cut
                   // Also applies to any future draw commands

        Pasted = 8, // On if block was changed by /paste or /pastenot

        Restored = 16, // On if block was restored using BlockDB or /undo

        Physicsed = 32, // On if block was changed by physics

        Overwritten = 64  // On if this is block overwrote another altered block

        // 128 reserved for future use
    }

    /*
     *  Action              | Manu | Pain | Draw | Past | Rest | Phys | Over
     * 
     * unaltered block
     * manual build/delete      X                                         ~
     * /paint                   X      ~                                  ~
     * draw (/c, /e, /r, etc)          ~      X                           ~
     * draw with air                          X                           ~
     * /cut                                   X                           ~
     * /paste                          ~      X      X                    ~
     * /undo                    .      .      .      .      X      .      ~
     * /restore                 .      .      .      .      X      .      ~
     * /regen                          ~                    X             ~
     * 
     * LEGEND:  X = always on
     *          ~ = on depending on previous block type
     *          . = flag copied from original/restored block
     */
}
