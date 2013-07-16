// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Categorizes conditions that lead to server shutdowns. </summary>
    public enum ShutdownReason {
        /// <summary> Server is shutting down, because someone called /Shutdown. </summary>
        ShutdownCommand = 0,

        /// <summary> Server is restarting, because someone called /Restart. </summary>
        RestartCommand = 1,

        /// <summary> InitLibrary or InitServer failed. </summary>
        FailedToInitialize = 2,

        /// <summary> StartServer failed. </summary>
        FailedToStart = 3,

        /// <summary> Server has experienced a non-recoverable crash. </summary>
        Crashed = 4,

        /// <summary> Server process is being closed (e.g. frontend closed, Ctrl+C, etc) </summary>
        ProcessClosing = 5,

        /// <summary> AutoRestart timer triggered. </summary>
        RestartTimer = 6,

        /// <summary> Updater should be ran, then server should be restarted. </summary>
        RestartForUpdate = 7,

        /// <summary> Updater should be ran, then server should NOT be restarted. </summary>
        ShutdownForUpdate = 8
    }
}