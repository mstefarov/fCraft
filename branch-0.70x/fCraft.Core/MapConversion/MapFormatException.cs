// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Exception that is thrown when problems arise during map saving or loading.
    /// May be caused by map file's incorrect format or structure. </summary>
    public class MapFormatException : Exception {
        internal MapFormatException() { }
        internal MapFormatException( [NotNull] string message ) : base( message ) { }
    }


    public sealed class NoMapConverterFoundException : MapFormatException {
        internal NoMapConverterFoundException() { }
        internal NoMapConverterFoundException( [NotNull] string message ) : base( message ) { }
    }
}
