// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Exception that is thrown when problems arise during map saving or loading.
    /// May be caused by map file's incorrect format or structure. </summary>
    [Serializable]
    public sealed class MapFormatException : Exception {
        internal MapFormatException() {}


        internal MapFormatException( [NotNull] string message )
            : base( message ) {}


        internal MapFormatException( [NotNull] string message, Exception innerException )
            : base( message, innerException ) {}


        MapFormatException( SerializationInfo info, StreamingContext context )
            : base( info, context ) {}
    }


    /// <summary> Exception that is thrown when no importer or exporter could
    /// be found for the given map format, or a map format could not be identified at all. </summary>
    [Serializable]
    public sealed class NoMapConverterFoundException : Exception {
        internal NoMapConverterFoundException() {}


        internal NoMapConverterFoundException( [NotNull] string message )
            : base( message ) {}


        internal NoMapConverterFoundException( [NotNull] string message, Exception innerException )
            : base( message, innerException ) {}


        NoMapConverterFoundException( SerializationInfo info, StreamingContext context )
            : base( info, context ) {}
    }
}