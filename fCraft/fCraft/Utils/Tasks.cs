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
        public object param = null;
        public bool enabled = true;
    }

    // used by Tasks
    public delegate void TaskCallback( object param );


    public static class Tasks {
        static object queueLock = new object(),
                      priorityQueueLock = new object();
        static Thread taskThread;
        static Queue<KeyValuePair<TaskCallback, object>> tasks = new Queue<KeyValuePair<TaskCallback, object>>(),
                                                 priorityTasks = new Queue<KeyValuePair<TaskCallback, object>>();
        static bool keepGoing;


        public static void Start() {
            keepGoing = true;
            taskThread = new Thread( TaskLoop );
            taskThread.IsBackground = true;
            taskThread.Start();
        }


        public static void ShutDown() {
            keepGoing = false;
            if( taskThread != null && taskThread.IsAlive ) {
                taskThread.Join();
            }
        }


        public static void Restart() {
            ShutDown();
            tasks.Clear();
            priorityTasks.Clear();
            Start();
        }


        public static void Add( TaskCallback callback, object param, bool isPriority ) {
            if( keepGoing ) {
                KeyValuePair<TaskCallback, object> newTask = new KeyValuePair<TaskCallback, object>( callback, param );
                if( isPriority ) {
                    lock( priorityQueueLock ) {
                        priorityTasks.Enqueue( newTask );
                    }
                } else {
                    lock( queueLock ) {
                        tasks.Enqueue( newTask );
                    }
                }
            }
        }


        static void TaskLoop() {
            KeyValuePair<TaskCallback, object> task;
            while( keepGoing ) {
                while( priorityTasks.Count > 0 ) {
                    lock( priorityQueueLock ) {
                        task = priorityTasks.Dequeue();
                    }
                    task.Key( task.Value );
                }
                if( tasks.Count > 0 ) {
                    lock( queueLock ) {
                        task = tasks.Dequeue();
                    }
                    task.Key( task.Value );
                }
                Thread.Sleep( 1 );
            }
        }
    }
}
