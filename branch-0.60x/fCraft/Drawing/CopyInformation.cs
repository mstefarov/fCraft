// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class CopyInformation {
        // using "string" instead of "World" here
        // to avoid keeping World on the GC after it has been removed.
        public string OriginWorld { get; set; }

        public DateTime CopyTime { get; set; }
        public byte[, ,] Buffer { get; set; }
        public int Width, Length, Height;
    }
}
