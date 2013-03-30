// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft {
    [Flags]
    public enum PermissionFlags {
        None = 0,
        NeedsTarget = 1,
        NeedsWorld = 2,
        NeedsRank = 4,
        NeedsQuantity = 8,
        NeedsCoordinates = 16
    }
}