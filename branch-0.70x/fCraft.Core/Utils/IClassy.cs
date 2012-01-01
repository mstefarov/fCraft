// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides a way for printing an object's name beautified with Minecraft color codes.
    /// It was "classy" in a sense that it was colored based on "class" (rank) of a player/world/zone. </summary>
    public interface IClassy {
        /// <summary> Name formatted with minecraft colour codes, stored as a string. </summary>
        [NotNull]
        string ClassyName { get; }
    }
}
