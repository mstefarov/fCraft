// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class CopyInformation {
        public string OriginWorld;
        public DateTime CopyTime;
        public byte[, ,] Buffer;
        public int Width, Length, Height;
    }
}
