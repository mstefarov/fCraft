// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using fCraft.Events;

namespace fCraft {
    public class LogRecorder : IDisposable {
        object locker = new object();
        List<string> messages = new List<string>();
        public bool HasMessages { get; private set; }
        LogType[] thingsToLog;
        bool disposed;

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
