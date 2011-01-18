// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;


namespace fCraft {
    public static class Scheduler {
        static HashSet<Task> tasks = new HashSet<Task>();
        static Queue<Task> backgroundTasks = new Queue<Task>();
        static Task[] taskList;
        static object taskListLock = new object(),
                      backgroundTaskListLock = new object();

        static Thread schedulerThread;
        static Thread backgroundThread;


        public static void Start() {
            schedulerThread = new Thread( MainLoop );
            schedulerThread.Start();
            backgroundThread = new Thread( BackgroundLoop );
            backgroundThread.Start();
        }


        static void MainLoop() {
            while( !Server.shuttingDown ) {
                DateTime ticksNow = DateTime.UtcNow;
                Task[] taskListCache = taskList;

                for( int i = 0; i < taskListCache.Length && !Server.shuttingDown; i++ ) {
                    Task task = taskListCache[i];
                    if( !task.IsStopped && task.NextTime <= ticksNow ) {

                        if( task.IsRecurring && task.AdjustForExecutionTime ) {
                            task.NextTime = ticksNow + task.Interval;
                        }

                        if( task.IsBackground ) {
                            lock( backgroundTaskListLock ) {
                                backgroundTasks.Enqueue( task );
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
                                Logger.LogAndReportCrash( "Exception thrown by ScheduledTask callback", "fCraft", ex );
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
                }

                Thread.Sleep( 10 );
            }
        }


        static void BackgroundLoop() {
            while( !Server.shuttingDown ) {
                if( backgroundTasks.Count > 0 ) {
                    Task task;
                    lock( backgroundTaskListLock ) {
                        task = backgroundTasks.Dequeue();
                    }
                    task.Callback( task );
                }
                Thread.Sleep( 10 );
            }
        }


        public static void AddTask( Task task ) {
            lock( taskListLock ) {
                Logger.Log( "Scheduler.AddTask: Added {0}", LogType.Debug, task );
                task.IsStopped = false;
                tasks.Add( task );
                UpdateCache();
            }
        }


        public static void UpdateCache() {
            List<Task> newList = new List<Task>();
            List<Task> deletionList = new List<Task>();
            lock( taskListLock ) {
                foreach( Task task in tasks ) {
                    if( task.IsStopped ) {
                        deletionList.Add( task );
                    } else {
                        newList.Add( task );
                    }
                }
                foreach( Task task in deletionList ) {
                    tasks.Remove( task );
                    Logger.Log( "Scheduler.UpdateCache: Removed {0}", LogType.Debug, task );
                }
            }
            taskList = newList.ToArray();
        }


        public static Task AddTask( SchedulerCallback _callback ) {
            return new Task( _callback, false );
        }


        public static Task AddBackgroundTask( SchedulerCallback _callback ) {
            return new Task( _callback, true );
        }

        public static void BeginShutdown() {
            lock( taskListLock ) {
                foreach( Task activeTask in tasks ) {
                    activeTask.Stop();
                }
                tasks.Clear();
                taskList = tasks.ToArray();
            }
        }

        public static void EndShutdown() {
            if( schedulerThread.IsAlive ) {
                schedulerThread.Join();
            }
            if( backgroundThread.IsAlive ) {
                backgroundThread.Join();
            }
        }


        public static void PrintTasks( Player player ) {
            lock( taskListLock ) {
                foreach( Task task in tasks ) {
                    player.Message( task.ToString() );
                }
            }
        }


        public class Task {

            public Task() { }

            public Task( SchedulerCallback _callback, bool _isBackground ) {
                Callback = _callback;
                IsBackground = _isBackground;
            }

            public DateTime NextTime;
            public TimeSpan Delay = TimeSpan.Zero;

            public bool IsRecurring = false;
            public bool IsBackground = false;
            public bool IsStopped = false;
            public bool IsExecuting = false;
            public bool AdjustForExecutionTime = true;
            public TimeSpan Interval = TimeSpan.FromMinutes( 1 );
            public int MaxRepeats = -1;

            public SchedulerCallback Callback;
            public object UserState;


            #region Run Once

            public Task RunOnce() {
                NextTime = DateTime.UtcNow.Add( Delay );
                IsRecurring = false;
                Scheduler.AddTask( this );
                return this;
            }


            public Task RunOnce( TimeSpan _delay ) {
                Delay = _delay;
                return RunOnce();
            }


            public Task RunOnce( DateTime time ) {
                Delay = time.Subtract( DateTime.UtcNow );
                NextTime = time;
                IsRecurring = false;
                Scheduler.AddTask( this );
                return this;
            }


            public Task RunOnce( object _userState, TimeSpan _delay ) {
                UserState = _userState;
                return RunOnce( _delay );
            }


            public Task RunOnce( object _userState, DateTime time ) {
                UserState = _userState;
                return RunOnce( time );
            }

            #endregion


            #region Run Forever

            Task RunForever() {
                IsRecurring = true;
                NextTime = DateTime.UtcNow.Add( Delay );
                Scheduler.AddTask( this );
                return this;
            }


            public Task RunForever( TimeSpan _interval ) {
                Interval = _interval;
                return RunForever();
            }


            public Task RunForever( TimeSpan _interval, TimeSpan _delay ) {
                Interval = _interval;
                Delay = _delay;
                return RunForever();
            }


            public Task RunForever( object _userState, TimeSpan _interval, TimeSpan _delay ) {
                UserState = _userState;
                return RunForever( _interval, _delay );
            }

            #endregion


            #region Run Repeating

            public Task RunRepeating( TimeSpan _delay, TimeSpan _interval, int _times ) {
                MaxRepeats = _times;
                return RunForever( _interval, _delay );
            }


            public Task RunRepeating( object _userState, TimeSpan _delay, TimeSpan _interval, int _times ) {
                UserState = _userState;
                MaxRepeats = _times;
                return RunForever( _interval, _delay );
            }

            #endregion


            public Task Stop() {
                IsStopped = true;
                return this;
            }


            public override string ToString() {
                StringBuilder sb = new StringBuilder("Task(");

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