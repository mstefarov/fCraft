// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides a way for printing an object's name beautified with Minecraft color codes.
    /// It was "classy" in a sense that it was colored based on "class" (rank) of a player/world/zone. </summary>
    public interface IClassy {
        /// <summary> Name optionally formatted with minecraft color codes or other decorations. </summary>
        [NotNull]
        string ClassyName { get; }
    }
}
