// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Threading;

namespace fCraft {

    public delegate void Task( object param );

    public static class Tasks {
        static object queueLock = new object(),
                      priorityQueueLock = new object();
        static Thread taskThread;
        static Queue<KeyValuePair<Task, object>> tasks = new Queue<KeyValuePair<Task, object>>(),
                                                 priorityTasks = new Queue<KeyValuePair<Task, object>>();
        static bool keepGoing;


        public static void Init() {
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
            Init();
        }


        public static void Add( Task callback, object param, bool isPriority ) {
            if( keepGoing ) {
                KeyValuePair<Task, object> newTask = new KeyValuePair<Task, object>( callback, param );
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
            KeyValuePair<Task, object> task;
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
