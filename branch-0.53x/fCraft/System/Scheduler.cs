// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace fCraft {
    /// <summary> A general-purpose task scheduler. </summary>
    public static class Scheduler {
        static readonly HashSet<SchedulerTask> Tasks = new HashSet<SchedulerTask>();
        static SchedulerTask[] taskCache;
        static readonly Queue<SchedulerTask> BackgroundTasks = new Queue<SchedulerTask>();
        static readonly object TaskListLock = new object(),
                               BackgroundTaskListLock = new object();

        static Thread schedulerThread,
                      backgroundThread;


        internal static void Start() {
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

                SchedulerTask[] taskListCache = taskCache;

                for( int i = 0; i < taskListCache.Length && !Server.IsShuttingDown; i++ ) {
                    SchedulerTask task = taskListCache[i];
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

#if DEBUG_SCHEDULER
                        FireEvent( TaskExecuting, task );
#endif

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

#if DEBUG_SCHEDULER
                        FireEvent( TaskExecuted, task );
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
                    SchedulerTask task;
                    lock( BackgroundTaskListLock ) {
                        task = BackgroundTasks.Dequeue();
                    }
#if DEBUG_SCHEDULER
                    FireEvent( TaskExecuting, task );
                    task.Callback( task );
                    FireEvent( TaskExecuted, task );
#else
                    task.Callback( task );
#endif
                }
                Thread.Sleep( 10 );
            }
        }


        /// <summary> Schedules a given task for execution. </summary>
        /// <param name="task"> Task to schedule. </param>
        internal static void AddTask( SchedulerTask task ) {
            if( task == null ) throw new ArgumentNullException( "task" );
            lock( TaskListLock ) {
                if( Server.IsShuttingDown ) return;
                task.IsStopped = false;
#if DEBUG_SCHEDULER
                FireEvent( TaskAdded, task );
                if( Tasks.Add( task ) ) {
                    UpdateCache();
                    Logger.Log( "Scheduler.AddTask: Added {0}", LogType.Debug, task );
                }else{
                    Logger.Log( "Scheduler.AddTask: Added duplicate {0}", LogType.Debug, task );
                }
#else
                if( Tasks.Add( task ) ) {
                    UpdateCache();
                }
#endif
            }
        }


        /// <summary> Creates a new SchedulerTask object to run in the main thread.
        /// Use this if your task is time-sensitive or frequent, and your callback won't take too long to execute. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewTask( SchedulerCallback callback ) {
            return new SchedulerTask( callback, false );
        }


        /// <summary> Creates a new SchedulerTask object to run in the background thread.
        /// Use this if your task is not very time-sensitive or frequent, or if your callback is resource-intensive. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewBackgroundTask( SchedulerCallback callback ) {
            return new SchedulerTask( callback, true );
        }


        /// <summary> Creates a new SchedulerTask object to run in the main thread.
        /// Use this if your task is time-sensitive or frequent, and your callback won't take too long to execute. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <param name="userState"> Parameter to pass to the method. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewTask( SchedulerCallback callback, object userState ) {
            return new SchedulerTask( callback, false, userState );
        }


        /// <summary> Creates a new SchedulerTask object to run in the background thread.
        /// Use this if your task is not very time-sensitive or frequent, or if your callback is resource-intensive. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <param name="userState"> Parameter to pass to the method. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewBackgroundTask( SchedulerCallback callback, object userState ) {
            return new SchedulerTask( callback, true, userState );
        }


        // Removes stopped tasks from the list
        internal static void UpdateCache() {
            List<SchedulerTask> newList = new List<SchedulerTask>();
            List<SchedulerTask> deletionList = new List<SchedulerTask>();
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    if( task.IsStopped ) {
                        deletionList.Add( task );
                    } else {
                        newList.Add( task );
                    }
                }
                for( int i = 0; i < deletionList.Count; i++ ) {
                    Tasks.Remove( deletionList[i] );
#if DEBUG_SCHEDULER
                    FireEvent( TaskRemoved, deletionList[i] );
                    Logger.Log( "Scheduler.UpdateCache: Removed {0}", LogType.Debug, deletionList[i] );
#endif
                }
            }
            taskCache = newList.ToArray();
        }


        // Clears the task list
        internal static void BeginShutdown() {
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    task.Stop();
                }
                UpdateCache();
            }
        }


        // Makes sure that both scheduler threads finish and quit.
        internal static void EndShutdown() {
            try {
                if( schedulerThread != null && schedulerThread.IsAlive ) {
                    schedulerThread.Join();
                }
                schedulerThread = null;
            } catch( ThreadStateException ) { }
            try {
                if( backgroundThread != null && backgroundThread.IsAlive ) {
                    backgroundThread.Join();
                }
                backgroundThread = null;
            } catch( ThreadStateException ) { }
        }


        /// <summary> Prints a list of active tasks (which may be quite long) to a given player. </summary>
        public static void PrintTasks( Player player ) {
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    player.Message( task.ToString() );
                }
            }
        }


 #if DEBUG_SCHEDULER
        public static event EventHandler<SchedulerTaskEventArgs> TaskAdded;

        public static event EventHandler<SchedulerTaskEventArgs> TaskExecuting;

        public static event EventHandler<SchedulerTaskEventArgs> TaskExecuted;

        public static event EventHandler<SchedulerTaskEventArgs> TaskRemoved;


        static void FireEvent( EventHandler<SchedulerTaskEventArgs> eventToFire, SchedulerTask task ) {
            var h = eventToFire;
            if( h != null ) h( null, new SchedulerTaskEventArgs( task ) );
        }
#endif
    }


    public sealed class SchedulerTask {

        internal SchedulerTask( SchedulerCallback callback, bool isBackground ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            Callback = callback;
            IsBackground = isBackground;
        }

        internal SchedulerTask( SchedulerCallback callback, bool isBackground, object userState ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
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

        public SchedulerTask RunOnce() {
            NextTime = DateTime.UtcNow.Add( Delay );
            IsRecurring = false;
            Scheduler.AddTask( this );
            return this;
        }


        public SchedulerTask RunOnce( TimeSpan delay ) {
            Delay = delay;
            return RunOnce();
        }


        public SchedulerTask RunOnce( DateTime time ) {
            Delay = time.Subtract( DateTime.UtcNow );
            NextTime = time;
            IsRecurring = false;
            Scheduler.AddTask( this );
            return this;
        }


        public SchedulerTask RunOnce( object userState, TimeSpan delay ) {
            UserState = userState;
            return RunOnce( delay );
        }


        public SchedulerTask RunOnce( object userState, DateTime time ) {
            UserState = userState;
            return RunOnce( time );
        }

        #endregion


        #region Run Forever

        SchedulerTask RunForever() {
            IsRecurring = true;
            NextTime = DateTime.UtcNow.Add( Delay );
            Scheduler.AddTask( this );
            return this;
        }


        public SchedulerTask RunForever( TimeSpan interval ) {
            Interval = interval;
            return RunForever();
        }


        public SchedulerTask RunForever( TimeSpan interval, TimeSpan delay ) {
            Interval = interval;
            Delay = delay;
            return RunForever();
        }


        public SchedulerTask RunForever( object userState, TimeSpan interval, TimeSpan delay ) {
            UserState = userState;
            return RunForever( interval, delay );
        }

        #endregion


        #region Run Repeating

        public SchedulerTask RunRepeating( TimeSpan delay, TimeSpan interval, int times ) {
            MaxRepeats = times;
            return RunForever( interval, delay );
        }


        public SchedulerTask RunRepeating( object userState, TimeSpan delay, TimeSpan interval, int times ) {
            UserState = userState;
            MaxRepeats = times;
            return RunForever( interval, delay );
        }

        #endregion


        #region Run Manual

        static readonly TimeSpan CloseEnoughToForever = TimeSpan.FromDays( 36525 ); // >100 years
        public SchedulerTask RunManual() {
            Delay = TimeSpan.Zero;
            IsRecurring = true;
            NextTime = DateTime.UtcNow;
            MaxRepeats = -1;
            Interval = CloseEnoughToForever;
            Scheduler.AddTask( this );
            return this;
        }

        public SchedulerTask RunManual( TimeSpan delay ) {
            Delay = delay;
            IsRecurring = true;
            NextTime = DateTime.UtcNow.Add( Delay );
            MaxRepeats = -1;
            Interval = CloseEnoughToForever;
            Scheduler.AddTask( this );
            return this;
        }

        public SchedulerTask RunManual( DateTime time ) {
            Delay = time.Subtract( DateTime.UtcNow );
            IsRecurring = true;
            NextTime = time;
            MaxRepeats = -1;
            Interval = CloseEnoughToForever;
            Scheduler.AddTask( this );
            return this;
        }

        #endregion


        public SchedulerTask Stop() {
            IsStopped = true;
            return this;
        }


        public override string ToString() {
            StringBuilder sb = new StringBuilder( "Task(" );

            if( IsStopped ) {
                sb.Append( "STOPPED " );
            }

            if( Callback.Target != null ) {
                sb.Append( Callback.Target ).Append( "::" );
            }
            sb.Append( Callback.Method.DeclaringType.Name );
            sb.Append( '.' );
            sb.Append( Callback.Method.Name );
            sb.Append( " @ " );

            if( IsRecurring ) {
                sb.Append( Interval.ToCompactString() );
            }
            sb.Append( "+" ).Append( Delay.ToCompactString() );

            if( UserState != null ) {
                sb.Append( " -> " );
                sb.Append( UserState );
            }
            sb.Append( ')' );
            return sb.ToString();
        }
    }


    public delegate void SchedulerCallback( SchedulerTask task );


#if DEBUG_SCHEDULER
    public class SchedulerTaskEventArgs : EventArgs {
        public SchedulerTaskEventArgs( SchedulerTask task ) {
            Task = task;
        }
        public SchedulerTask Task { get; private set; }
    }
#endif
}