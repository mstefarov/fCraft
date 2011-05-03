// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    /// <summary> Command categories. A command may belong to more than one category.
    /// Use binary flag logic (value & flag == flag) to test whether a command belongs to a particular category. </summary>
    [Flags]
    public enum CommandCategory {
        None = 0,
        Building = 1,
        Chat = 2,
        Info = 4,
        Moderation = 8,
        Maintenance = 16,
        World = 32,
        Zone = 64
    }
}
