// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using fCraft.Events;

namespace fCraft {
    /// <summary> A simple way to temporarily hook into fCraft's Logger.
    /// Make sure to dispose this class when you are done recording.
    /// The easiest way to ensure that is with a using(){...} block. </summary>
    public sealed class LogRecorder : IDisposable {
        readonly object locker = new object();
        readonly List<string> messages = new List<string>();
        readonly LogType[] thingsToLog;
        bool disposed;


        /// <summary> Creates a recorder for errors and warnings. </summary>
        public LogRecorder()
            : this( LogType.Error, LogType.Warning ) {
        }


        /// <summary> Creates a custom recorder. </summary>
        /// <param name="thingsToLog"> A list or array of LogTypes to record. </param>
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


        /// <summary> Whether any messages have been recorded. </summary>
        public bool HasMessages { get; private set; }

        /// <summary> An array of individual recorded messages. </summary>
        public string[] MessageList {
            get {
                lock( locker ) {
                    return messages.ToArray();
                }
            }
        }

        
        /// <summary> All messages in one block of text, separated by newlines. </summary>
        public string MessageString {
            get {
                lock( locker ) {
                    return String.Join( Environment.NewLine, messages.ToArray() );
                }
            }
        }


        /// <summary> Stops recording the messages (cannot be resumed).
        /// This method should be called when you are done with the object.
        /// If LogRecorder is in a using() block, this will be done for you. </summary>
        public void Dispose() {
            lock( locker ) {
                if( !disposed ) {
                    Logger.Logged -= HandleLog;
                    disposed = true;
                }
            }
        }
    }
}
