// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Exception that is thrown when problems arise during map saving or loading.
    /// May be caused by map file's incorrect format or structure. </summary>
    public sealed class MapFormatException : Exception {
        internal MapFormatException() { }
        internal MapFormatException( [NotNull] string message ) : base( message ) { }
    }
}
