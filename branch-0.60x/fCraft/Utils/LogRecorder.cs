// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using fCraft.Events;

namespace fCraft {
    public sealed class LogRecorder : IDisposable {
        readonly object locker = new object();
        readonly List<string> messages = new List<string>();
        readonly LogType[] thingsToLog;
        bool disposed;

        public bool HasMessages { get; private set; }

        public LogRecorder()
            : this( LogType.Error, LogType.Warning ) {
        }

        ~LogRecorder() {
            Dispose();
        }


        public LogRecorder( params LogType[] thingsToLog ) {
            Logger.Logged += HandleLog;
            this.thingsToLog = thingsToLog;
        }


        void HandleLog( object sender, LogEventArgs e ) {
            for( int i = 0; i < thingsToLog.Length; i++ ) {
                if( thingsToLog[i] == e.MessageType ) {
                    HasMessages = true;
                    lock( locker ) {
                        messages.Add( e.MessageType + ": " + e.RawMessage );
                    }
                }
            }
        }


        public string[] MessageList {
            get {
                lock( locker ) {
                    return messages.ToArray();
                }
            }
        }


        public string MessageString {
            get {
                lock( locker ) {
                    return String.Join( Environment.NewLine, messages.ToArray() );
                }
            }
        }


        public void Dispose() {
            if( !disposed ) {
                Logger.Logged -= HandleLog;
                disposed = true;
            }
        }
    }
}
