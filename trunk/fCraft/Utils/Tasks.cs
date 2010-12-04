// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Threading;

namespace fCraft {

    // used by Server.MainLoop
    internal sealed class ScheduledTask {
        public DateTime nextTime;
        public int interval;
        public TaskCallback callback;
        public object param;
        public bool enabled = true;
    }

    // used by Tasks
    public delegate void TaskCallback( object param );


    public static class Tasks {
        static object queueLock = new object();
        static Thread taskThread;
        static Queue<KeyValuePair<TaskCallback, object>> tasks = new Queue<KeyValuePair<TaskCallback, object>>();
        static bool keepGoing;


        public static void Start() {
            keepGoing = true;
            taskThread = new Thread( TaskLoop );
            taskThread.IsBackground = true;
            taskThread.Start();
        }


        public static void Shutdown() {
            keepGoing = false;
            if( taskThread != null && taskThread.IsAlive ) {
                taskThread.Join();
            }
        }


        public static void Add( TaskCallback callback, object param ) {
            if( keepGoing ) {
                KeyValuePair<TaskCallback, object> newTask = new KeyValuePair<TaskCallback, object>( callback, param );
                lock( queueLock ) {
                    tasks.Enqueue( newTask );
                }
            }
        }


        static void TaskLoop() {
            KeyValuePair<TaskCallback, object> task;
            while( keepGoing ) {
                if( tasks.Count > 0 ) {
                    lock( queueLock ) {
                        task = tasks.Dequeue();
                    }
#if DEBUG
                    task.Key( task.Value );
#else
                    try {
                        task.Key( task.Value );
                    } catch( Exception ex ) {
                        Logger.Log( "Error was thrown by Tasks thread: {0}", LogType.Error, ex );
                        Logger.UploadCrashReport( "Error was thrown by Tasks thread", "fCraft", ex );
                    }
#endif
                }
                Thread.Sleep( 10 );
            }
        }
    }
}