// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft.MapGeneration {
    /// <summary> Map generator themes. A theme defines what type of blocks are used to fill the map. </summary>
    public enum MapGenTheme {
        Arctic,
        Desert,
        Forest, // just like "Grass", but with trees (when available)
        Grass,
        Hell,
        Swamp, // also with trees
        /* TODO:
        Winter,
        Tropical,
        Jungle,
        Wasteland
        */
    }
}