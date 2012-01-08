// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Describes the circumstances of server shutdown. </summary>
    public sealed class ShutdownParams {

        /// <summary> Sets the parameters of a server shutdown. </summary>
        /// <param name="reason"> Reason for server shutdown. </param>
        /// <param name="delay"> (UTC) Timespan on how long to wait before shutting down. </param>
        /// <param name="killProcess"> whether fCraft should attempt to kill its own process after shutdown is complete. </param>
        /// <param name="restart"> Whether or not to restart the server after this shutdown. </param>
        public ShutdownParams( ShutdownReason reason, TimeSpan delay, bool killProcess, bool restart ) {
            Reason = reason;
            Delay = delay;
            KillProcess = killProcess;
            Restart = restart;
        }

        /// <summary> Sets the parameters of a server shutdown. </summary>
        /// <param name="reason"> Reason for server shutdown. </param>
        /// <param name="delay"> (UTC) Timespan on how long to wait before shutting down. </param>
        /// <param name="killProcess"> whether fCraft should attempt to kill its own process after shutdown is complete. </param>
        /// <param name="restart"> Whether or not to restart the server after this shutdown. </param>
        /// <param name="customReason"> "Overriding reason why server is being shutdown. </param>
        /// <param name="initiatedBy"> Player or entity who initiated the shutdown. </param>
        public ShutdownParams( ShutdownReason reason, TimeSpan delay, bool killProcess,
                               bool restart, [CanBeNull] string customReason, [CanBeNull] Player initiatedBy ) :
                                   this( reason, delay, killProcess, restart ) {
            customReasonString = customReason;
            InitiatedBy = initiatedBy;
        }

        /// <summary> Reason why the server is shutting down. </summary>
        public ShutdownReason Reason { get; private set; }

        readonly string customReasonString;

        /// <summary> Reason why the server is shutting down, if customReasonString is not null it overrides. </summary>
        [NotNull]
        public string ReasonString {
            get {
                return customReasonString ?? Reason.ToString();
            }
        }

        /// <summary> Delay before shutting down. </summary>
        public TimeSpan Delay { get; private set; }

        /// <summary> Whether fCraft should try to forcefully kill the current process. </summary>
        public bool KillProcess { get; private set; }

        /// <summary> Whether the server is expected to restart itself after shutting down. </summary>
        public bool Restart { get; private set; }

        /// <summary> Player who initiated the shutdown. May be null or Console. </summary>
        [CanBeNull]
        public Player InitiatedBy { get; private set; }
    }


    /// <summary> Categorizes conditions that lead to server shutdowns. </summary>
    public enum ShutdownReason {
        /// <summary> Cause of server shutdown is unknown. </summary>
        Unknown,

        /// <summary> Use for mod- or plugin-triggered shutdowns. </summary>
        Other,

        /// <summary> InitLibrary or InitServer failed. </summary>
        FailedToInitialize,

        /// <summary> StartServer failed. </summary>
        FailedToStart,

        /// <summary> Server is restarting, usually because someone called /Restart. </summary>
        Restarting,

        /// <summary> Server has experienced a non-recoverable crash. </summary>
        Crashed,

        /// <summary> Server is shutting down, usually because someone called /Shutdown. </summary>
        ShuttingDown,

        /// <summary> Server process is being closed/killed. </summary>
        ProcessClosing
    }
}