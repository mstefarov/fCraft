// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Exception that is thrown when problems arise during map saving or loading.
    /// May be caused by map file's incorrect format or structure. </summary>
    public class MapFormatException : Exception {
        internal MapFormatException() {}

        internal MapFormatException( [NotNull] string message ) : base( message ) {}
    }


    /// <summary> Exception that is thrown when no importer or exporter could
    /// be found for the given map format, or a map format could not be identified at all. </summary>
    public sealed class NoMapConverterFoundException : MapFormatException {
        internal NoMapConverterFoundException() {}

        internal NoMapConverterFoundException( [NotNull] string message ) : base( message ) {}
    }
}