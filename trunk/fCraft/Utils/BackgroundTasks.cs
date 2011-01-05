// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Threading;


namespace fCraft {

    /// <summary>
    /// Generic callback method with one param/arg, used by both Server.MainLoop and BackgroundTasks
    /// </summary>
    /// <param name="param"></param>
    public delegate void TaskCallback( object param );


    /// <summary>
    /// Used for offloading operations to a background thread.
    /// </summary>
    public static class BackgroundTasks {
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
                        Logger.LogAndReportCrash( "Error in Tasks thread", "fCraft", ex );
                    }
#endif
                }
                Thread.Sleep( 10 );
            }
        }
    }
}