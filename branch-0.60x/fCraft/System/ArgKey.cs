// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Enumerates the recognized command-line switches/arguments.
    /// Args are parsed in Server.InitLibrary </summary>
    public enum ArgKey {
        /// <summary> Working path (directory) that fCraft should use. </summary>
        Path,

        /// <summary> Path (directory) where the log files should be placed. </summary>
        LogPath,

        /// <summary> Path (directory) where the map files should be loaded from/saved to. </summary>
        MapPath,

        /// <summary> Path (file) of the configuration file. </summary>
        Config,

        /// <summary> If NoRestart flag is present, fCraft will shutdown instead of restarting.
        /// This flag is used by AutoRestarter. </summary>
        NoRestart,

        /// <summary> If ExitOnCrash flag is present, fCraft will exit
        /// at once in the event of an unrecoverable crash, instead of showing a message. </summary>
        ExitOnCrash,

        /// <summary> Disables all logging. </summary>
        NoLog,

        /// <summary>  Disables colors in CLI frontends. </summary>
        NoConsoleColor
    };
}
