// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace fCraft {
    partial class Server {

        public static event EventHandler<SessionConnectingEventArgs> SessionConnecting;

        public static event EventHandler<SessionConnectedEventArgs> SessionConnected;

        public static event EventHandler<SessionDisconnectedEventArgs> SessionDisconnected;


        internal static bool RaiseSessionConnectingEvent( IPAddress IP ) {
            var h = SessionConnecting;
            var e = new SessionConnectingEventArgs( IP );
            if( h != null ) h( null, e );
            return e.Cancel;
        }


        internal static void RaiseSessionConnectedEvent( Session session ) {
            var h = SessionConnected;
            var e = new SessionConnectedEventArgs( session );
            if( h != null ) h( null, e );
        }


        internal static void RaiseSessionDisconnectedEvent( Session session, LeaveReason leaveReason ) {
            var h = SessionDisconnected;
            var e = new SessionDisconnectedEventArgs( session, leaveReason );
            if( h != null ) h( null, e );
        }
    }
}