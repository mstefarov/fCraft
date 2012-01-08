// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Exception thrown if an error is caused by incorrect server configuration. </summary>
    public sealed class MisconfigurationException : Exception {
        public MisconfigurationException( [NotNull] string message )
            : base( message ) { }

        public MisconfigurationException( [NotNull] string message, Exception innerException )
            : base( message, innerException ) { }
    }
}