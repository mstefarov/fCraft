// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.MapConversion {
    [Flags]
    public enum WorldDataCategory {
        None = 0,
        MapData = 1,
        Spawn = 2,
        BackupSettings = 4,
        AccessPermissions = 8,
        BuildPermissions = 16,
        Environment = 32,
        BlockDBSettings = 64,
        BlockDBData = 128,
        Zones = 256,
        MapMetadata = 512,
        WorldMetadata = 1024,
        WorldEvents = 2048,

        LoadByDefault = MapData | Spawn | BlockDBData | Zones | MapMetadata
    }
}