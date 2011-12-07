// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    public class MisconfigurationException : Exception {
        public MisconfigurationException()
            : base() { }

        public MisconfigurationException( string message )
            : base( message ) { }

        public MisconfigurationException( string message, Exception innerException )
            : base( message, innerException ) { }
    }
}