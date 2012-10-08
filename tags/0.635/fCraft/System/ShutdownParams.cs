// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Describes the circumstances of server shutdown. </summary>
    public sealed class ShutdownParams {
        public ShutdownParams( ShutdownReason reason, TimeSpan delay, bool restart ) {
            Reason = reason;
            Delay = delay;
            Restart = restart;
        }


        public ShutdownParams( ShutdownReason reason, TimeSpan delay,
                               bool restart, [CanBeNull] string customReason,
                               [CanBeNull] Player initiatedBy ) :
                                   this( reason, delay, restart ) {
            customReasonString = customReason;
            InitiatedBy = initiatedBy;
        }


        public ShutdownReason Reason { get; private set; }

        readonly string customReasonString;

        [NotNull]
        public string ReasonString {
            get { return customReasonString ?? Reason.ToString(); }
        }

        /// <summary> Delay before shutting down. </summary>
        public TimeSpan Delay { get; private set; }

        /// <summary> Whether the server is expected to restart itself after shutting down. </summary>
        public bool Restart { get; private set; }

        /// <summary> Player who initiated the shutdown. May be null or Console. </summary>
        [CanBeNull]
        public Player InitiatedBy { get; private set; }
    }
}