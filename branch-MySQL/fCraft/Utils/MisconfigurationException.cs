// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    public sealed class MisconfigurationException : Exception {
        public MisconfigurationException() { }

        public MisconfigurationException( string message )
            : base( message ) { }

        public MisconfigurationException( string message, Exception innerException )
            : base( message, innerException ) { }
    }
}