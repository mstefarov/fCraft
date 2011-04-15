// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace fCraft {
    public static class Scheduler {
        static readonly HashSet<Task> Tasks = new HashSet<Task>();
        static readonly Queue<Task> BackgroundTasks = new Queue<Task>();
        static Task[] taskList;
        static readonly object TaskListLock = new object(),
                               BackgroundTaskListLock = new object();

        static Thread schedulerThread;
        static Thread backgroundThread;


        public static void Start() {
            schedulerThread = new Thread( MainLoop ) {
                Name = "fCraft.Main"
            };
            schedulerThread.Start();
            backgroundThread = new Thread( BackgroundLoop ) {
                Name = "fCraft.Background"
            };
            backgroundThread.Start();
        }


        static void MainLoop() {
            while( !Server.IsShuttingDown ) {
                DateTime ticksNow = DateTime.UtcNow;
                Task[] taskListCache = taskList;

                for( int i = 0; i < taskListCache.Length && !Server.IsShuttingDown; i++ ) {
                    Task task = taskListCache[i];
                    if( task.IsStopped || task.NextTime > ticksNow ) continue;
                    if( task.IsRecurring && task.AdjustForExecutionTime ) {
                        task.NextTime = ticksNow + task.Interval;
                    }

                    if( task.IsBackground ) {
                        lock( BackgroundTaskListLock ) {
                            BackgroundTasks.Enqueue( task );
                        }
                    } else {
                        task.IsExecuting = true;
#if DEBUG
                    task.Callback( task );
                    task.IsExecuting = false;
#else
                        try {
                            task.Callback( task );
                        } catch( Exception ex ) {
                            Logger.LogAndReportCrash( "Exception thrown by ScheduledTask callback", "fCraft", ex, false );
                        } finally {
                            task.IsExecuting = false;
                        }
#endif
                    }

                    if( !task.IsRecurring || task.MaxRepeats == 1 ) {
                        task.IsStopped = true;
                        continue;
                    }
                    task.MaxRepeats--;

                    ticksNow = DateTime.UtcNow;
                    if( !task.AdjustForExecutionTime ) {
                        task.NextTime = ticksNow.Add( task.Interval );
                    }
                }

                Thread.Sleep( 10 );
            }
        }


        static void BackgroundLoop() {
            while( !Server.IsShuttingDown ) {
                if( BackgroundTasks.Count > 0 ) {
                    Task task;
                    lock( BackgroundTaskListLock ) {
                        task = BackgroundTasks.Dequeue();
                    }
                    task.Callback( task );
                }
                Thread.Sleep( 10 );
            }
        }


        public static void AddTask( Task task ) {
            lock( TaskListLock ) {
                task.IsStopped = false;
                if( Tasks.Add( task ) ) {
                    UpdateCache();
                }
#if DEBUG_SCHEDULER
                Logger.Log( "Scheduler.AddTask: Added {0}", LogType.Debug, task );
#endif
            }
        }


        public static Task AddTask( SchedulerCallback callback ) {
            return new Task( callback, false );
        }


        public static Task AddBackgroundTask( SchedulerCallback callback ) {
            return new Task( callback, true );
        }


        public static Task AddTask( SchedulerCallback callback, object userState ) {
            return new Task( callback, false, userState );
        }


        public static Task AddBackgroundTask( SchedulerCallback callback, object userState ) {
            return new Task( callback, true, userState );
        }


        public static void UpdateCache() {
            List<Task> newList = new List<Task>();
            List<Task> deletionList = new List<Task>();
            lock( TaskListLock ) {
                foreach( Task task in Tasks ) {
                    if( task.IsStopped ) {
                        deletionList.Add( task );
                    } else {
                        newList.Add( task );
                    }
                }
                foreach( Task task in deletionList ) {
                    Tasks.Remove( task );
#if DEBUG_SCHEDULER
                    Logger.Log( "Scheduler.UpdateCache: Removed {0}", LogType.Debug, task );
#endif
                }
            }
            taskList = newList.ToArray();
        }


        public static void BeginShutdown() {
            lock( TaskListLock ) {
                foreach( Task activeTask in Tasks ) {
                    activeTask.Stop();
                }
                Tasks.Clear();
                taskList = Tasks.ToArray();
            }
        }

        public static void EndShutdown() {
            try {
                if( schedulerThread != null && schedulerThread.IsAlive ) {
                    schedulerThread.Join();
                }
            } catch( ThreadStateException ) { }
            try {
                if( backgroundThread != null && backgroundThread.IsAlive ) {
                    backgroundThread.Join();
                }
            } catch( ThreadStateException ) { }
        }


        public static void PrintTasks( Player player ) {
            lock( TaskListLock ) {
                foreach( Task task in Tasks ) {
                    player.Message( task.ToString() );
                }
            }
        }


        public sealed class Task {

            public Task() { }

            public Task( SchedulerCallback callback, bool isBackground ) {
                Callback = callback;
                IsBackground = isBackground;
            }

            public Task( SchedulerCallback callback, bool isBackground, object userState ) {
                Callback = callback;
                IsBackground = isBackground;
                UserState = userState;
            }

            public DateTime NextTime;
            public TimeSpan Delay = TimeSpan.Zero;

            public bool IsRecurring;
            public bool IsBackground;
            public bool IsStopped;
            public bool IsExecuting;
            public bool AdjustForExecutionTime = true;
            public TimeSpan Interval = TimeSpan.FromMinutes( 1 );
            public int MaxRepeats = -1;

            public SchedulerCallback Callback;
            public object UserState;


            #region Run Once

            public Task RunOnce() {
                NextTime = DateTime.UtcNow.Add( Delay );
                IsRecurring = false;
                AddTask( this );
                return this;
            }


            public Task RunOnce( TimeSpan delay ) {
                Delay = delay;
                return RunOnce();
            }


            public Task RunOnce( DateTime time ) {
                Delay = time.Subtract( DateTime.UtcNow );
                NextTime = time;
                IsRecurring = false;
                AddTask( this );
                return this;
            }


            public Task RunOnce( object userState, TimeSpan delay ) {
                UserState = userState;
                return RunOnce( delay );
            }


            public Task RunOnce( object userState, DateTime time ) {
                UserState = userState;
                return RunOnce( time );
            }

            #endregion


            #region Run Forever

            Task RunForever() {
                IsRecurring = true;
                NextTime = DateTime.UtcNow.Add( Delay );
                AddTask( this );
                return this;
            }


            public Task RunForever( TimeSpan interval ) {
                Interval = interval;
                return RunForever();
            }


            public Task RunForever( TimeSpan interval, TimeSpan delay ) {
                Interval = interval;
                Delay = delay;
                return RunForever();
            }


            public Task RunForever( object userState, TimeSpan interval, TimeSpan delay ) {
                UserState = userState;
                return RunForever( interval, delay );
            }

            #endregion


            #region Run Repeating

            public Task RunRepeating( TimeSpan delay, TimeSpan interval, int times ) {
                MaxRepeats = times;
                return RunForever( interval, delay );
            }


            public Task RunRepeating( object userState, TimeSpan delay, TimeSpan interval, int times ) {
                UserState = userState;
                MaxRepeats = times;
                return RunForever( interval, delay );
            }

            #endregion


            #region Run Manual

            static readonly TimeSpan CloseEnoughToForever = TimeSpan.FromDays( 36525 ); // >100 years
            public Task RunManual() {
                Delay = TimeSpan.Zero;
                IsRecurring = true;
                NextTime = DateTime.UtcNow;
                MaxRepeats = -1;
                Interval = CloseEnoughToForever;
                AddTask( this );
                return this;
            }

            public Task RunManual( TimeSpan delay ) {
                Delay = delay;
                IsRecurring = true;
                NextTime = DateTime.UtcNow.Add( Delay );
                MaxRepeats = -1;
                Interval = CloseEnoughToForever;
                AddTask( this );
                return this;
            }

            public Task RunManual( DateTime time ) {
                Delay = time.Subtract( DateTime.UtcNow );
                IsRecurring = true;
                NextTime = time;
                MaxRepeats = -1;
                Interval = CloseEnoughToForever;
                AddTask( this );
                return this;
            }

            #endregion


            public Task Stop() {
                IsStopped = true;
                return this;
            }


            public override string ToString() {
                StringBuilder sb = new StringBuilder( "Task(" );

                if( Callback.Target != null ) {
                    sb.Append( Callback.Target ).Append( "::" );
                }
                sb.Append( Callback.Method ).Append( " @ " );

                if( IsRecurring ) {
                    sb.Append( Interval.ToCompactString() );
                }
                sb.Append( "+" ).Append( Delay.ToCompactString() );

                if( UserState != null ) {
                    sb.Append( " -> " );
                    if( UserState is IClassy ) {
                        sb.Append( (UserState as IClassy).GetClassyName() );
                    } else {
                        sb.Append( UserState );
                    }
                }
                sb.Append( ')' );
                return sb.ToString();
            }
        }
    }


    public delegate void SchedulerCallback( Scheduler.Task task );
}