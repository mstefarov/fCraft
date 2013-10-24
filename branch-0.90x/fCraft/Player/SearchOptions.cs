// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;

namespace fCraft {
    /// <summary> Flags usable with Server and PlayerDB player-search methods. </summary>
    [Flags]
    public enum SearchOptions {
        /// <summary> Default behavior is: do not include player themself in results;
        /// do not consider hidden players to be "online"; do raise events if applicable;
        /// do print no-players-found message when applicable. </summary>
        Default = 0,

        /// <summary> Whether player themself should be considered in search. </summary>
        IncludeSelf = 1,

        /// <summary> Whether hidden players should be considered "online" for search purposes, if applicable. </summary>
        IncludeHidden = 2,

        /// <summary> Whether to raise Server.SearchingForPlayer event, if applicable. </summary>
        SuppressEvent = 4,

        /// <summary> This flag controls what search methods do when IncludeSelf flag is not set, and player's
        /// own info is the only result. By default, search methods print "no results" message and return null.
        /// This flag changes that behavior. When ReturnSelfIfOnlyMatch is set, search methods do not
        /// print any message, and return player's own PlayerInfo. This is useful if you want to make a custom
        /// "you cannot do this to yourself" message - just check if returned PlayerInfo is player's own. </summary>
        ReturnSelfIfOnlyMatch = 8
    }
}
