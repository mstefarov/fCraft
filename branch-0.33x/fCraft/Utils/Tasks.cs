// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Threading;

namespace fCraft {

    public delegate void Task( object param );

    public class Tasks {
        object queueLock = new object(),
                      priorityQueueLock = new object();
        Thread taskThread;
        Queue<KeyValuePair<Task, object>> tasks = new Queue<KeyValuePair<Task, object>>(),
                                          priorityTasks = new Queue<KeyValuePair<Task, object>>();
        bool keepGoing;


        public void Init() {
            keepGoing = true;
            taskThread = new Thread( TaskLoop );
            taskThread.IsBackground = true;
            taskThread.Start();
        }


        public void ShutDown() {
            keepGoing = false;
            if( taskThread != null && taskThread.IsAlive ) {
                taskThread.Join();
            }
        }


        public void Restart() {
            ShutDown();
            tasks.Clear();
            priorityTasks.Clear();
            Init();
        }


        public void Add( Task callback, object param, bool isPriority ) {
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


        void TaskLoop() {
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
