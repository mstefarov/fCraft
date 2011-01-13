// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace fCraft {
    public static class Scheduler {
        static HashSet<SchedulerTask> tasks = new HashSet<SchedulerTask>();
        static Queue<SchedulerTask> backgroundTasks = new Queue<SchedulerTask>();
        static SchedulerTask[] taskList;
        static object taskListLock = new object(),
                      backgroundTaskListLock = new object();

        static Thread backgroundThread;


        public static void Start() {
            backgroundThread = new Thread( BackgroundLoop );
            backgroundThread.Start();
            MainLoop();
        }


        static void MainLoop() {
            while( !Server.shuttingDown ) {
                long ticksNow = DateTime.UtcNow.Ticks;
                SchedulerTask[] taskListCache = taskList;

                for( int i = 0; i < taskListCache.Length; i++ ) {
                    SchedulerTask task = taskListCache[i];
                    if( task.NextTime <= ticksNow ) {

                        if( task.IsRecurring && task.AdjustForExecutionTime ) {
                            task.NextTime = ticksNow;
                        }

                        if( task.IsBackground ) {
                            lock( backgroundTaskListLock ) {
                                backgroundTasks.Enqueue( task );
                            }
                        } else {
                            task.State = SchedulerTaskState.Running;
#if DEBUG
                            task.Callback(task);
#else
                            try {
                                task.Callback( task );
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "Exception thrown by ScheduledTask callback", "fCraft", ex );
                            }
#endif
                        }

                        if( !task.IsRecurring ) {
                            RemoveTask( task );
                            continue;
                        } else if( task.MaxRepeats > 0 ) {
                            task.MaxRepeats--;
                            if( task.MaxRepeats == 0 ) {
                                RemoveTask( task );
                                continue;
                            }
                        }

                        ticksNow = DateTime.UtcNow.Ticks;
                        if( !task.AdjustForExecutionTime ) {
                            task.NextTime = ticksNow + task.Interval * 10000L;
                            task.State = SchedulerTaskState.Waiting;
                        }
                    }
                }

                Thread.Sleep( 10 );
            }
        }


        static void BackgroundLoop() {
            while( !Server.shuttingDown ) {
                if( backgroundTasks.Count > 0 ) {
                    SchedulerTask task;
                    lock( backgroundTaskListLock ) {
                        task = backgroundTasks.Dequeue();
                    }
                    task.Callback( task );
                }
                Thread.Sleep( 10 );
            }
        }


        public static void AddTask( SchedulerTask task ) {
            lock( taskListLock ) {
                tasks.Add( task );
                task.State = SchedulerTaskState.Waiting;
                taskList = tasks.ToArray();
            }
        }



        public static SchedulerTask AddRepeatingTask( SchedulerCallback callback, int delay, int interval ) {
            return AddRepeatingTask( callback, delay, interval, -1 );
        }

        public static SchedulerTask AddRepeatingTask( SchedulerCallback callback, int delay, int interval, int maxRepeats ) {
            SchedulerTask task = new SchedulerTask {
                Callback = callback,
                NextTime = DateTime.UtcNow.Ticks + delay * 10000L,
                Interval = interval,
                MaxRepeats = maxRepeats
            };
            AddTask( task );
            return task;
        }





        public static void RemoveTask( SchedulerTask task ) {
            lock( taskListLock ) {
                task.State = SchedulerTaskState.Finished;
                tasks.Remove( task );
                taskList = tasks.ToArray();
            }
        }
    }


    public class SchedulerTask {

        public SchedulerTask() {
        }

        public SchedulerTask( bool _isBackground ) {
            IsBackground = _isBackground;
        }

        public bool Enabled = true;

        public long NextTime;
        public int Delay = 0;

        public bool IsRecurring = false;
        public bool IsBackground = false;
        public bool AdjustForExecutionTime = false;
        public int Interval = 60000;
        public int MaxRepeats = -1;

        public SchedulerCallback Callback;
        public object UserState;
        public SchedulerTaskState State = SchedulerTaskState.Inactive;


        public void Run() {
            Scheduler.AddTask( this );
        }


        #region Run Once

        public void RunOnce() {
            NextTime = DateTime.UtcNow.Ticks + Delay * 10000L;
            IsRecurring = false;
            Run();
        }

        public void RunOnce( int _delay ) {
            Delay = _delay;
            RunOnce();
        }

        public void RunOnce( TimeSpan _delay ) {
            Delay = (int)(_delay.Ticks / 10000L);
            RunOnce();
        }

        public void RunOnce( DateTime time ) {
            Delay = (int)time.Subtract( DateTime.UtcNow ).TotalMilliseconds;
            NextTime = time.Ticks;
            IsRecurring = false;
            Run();
        }

        public void RunOnce( object _userState, int _delay ) {
            UserState = _userState;
            RunOnce( _delay );
        }

        public void RunOnce( object _userState, TimeSpan _delay ) {
            UserState = _userState;
            RunOnce( _delay );
        }

        public void RunOnce( object _userState, DateTime time ) {
            UserState = _userState;
            RunOnce( time );
        }

        #endregion


        #region Run Forever

        void RunForever() {
            IsRecurring = true;
            NextTime = DateTime.UtcNow.Ticks + Delay * 10000L;
            Run();
        }

        public void RunForever( int _interval, int _delay ) {
            Interval = _interval;
            Delay = _delay;
            RunForever();
        }

        public void RunForever( TimeSpan _interval, TimeSpan _delay ) {
            Interval = (int)(_interval.Ticks / 10000L);
            Delay = (int)(_delay.Ticks / 10000L);
            RunForever();
        }

        public void RunForever( object _userState, int _interval, int _delay ) {
            UserState = _userState;
            RunForever( _interval, _delay );
        }

        public void RunForever( object _userState, TimeSpan _interval, TimeSpan _delay ) {
            UserState = _userState;
            RunForever( _interval, _delay );
        }

        #endregion


        #region Run Repeating

        public void RunRepeating( int _interval, int _times, int _delay ) {
            MaxRepeats = _times;
            RunForever( _interval, _delay );
        }

        public void RunRepeating( TimeSpan _interval, int _times, TimeSpan _delay ) {
            MaxRepeats = _times;
            RunForever( _interval, _delay );
        }

        public void RunRepeating( object _userState, int _interval, int _times, int _delay ) {
            UserState = _userState;
            MaxRepeats = _times;
            RunForever( _interval, _delay );
        }

        public void RunRepeating( object _userState, TimeSpan _interval, int _times, TimeSpan _delay ) {
            UserState = _userState;
            MaxRepeats = _times;
            RunForever( _interval, _delay );
        }

        #endregion
    }


    public enum SchedulerTaskState {
        Inactive,
        Waiting,
        Running,
        Finished
    }


    public delegate void SchedulerCallback( SchedulerTask task );
}