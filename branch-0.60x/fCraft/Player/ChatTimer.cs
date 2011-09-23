﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    public sealed class ChatTimer {
        public static readonly TimeSpan MinDuration = TimeSpan.FromSeconds( 1 ),
                                        Hour = TimeSpan.FromHours( 1 );

        public string Message { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public TimeSpan Duration { get; private set; }

        public TimeSpan TimeLeft {
            get {
                return EndTime.Subtract( DateTime.UtcNow );
            }
        }

        readonly SchedulerTask task;

        int announceIntervalIndex;

        public bool IsRunning { get; private set; }

        public void Stop() {
            IsRunning = false;
            task.Stop();
        }

        ChatTimer( TimeSpan duration, string message ) {
            Message = message;
            StartTime = DateTime.UtcNow;
            EndTime = StartTime.Add( duration );
            Duration = duration;
            int oneSecondRepeats = (int)duration.TotalSeconds + 1;
            if( duration > Hour ) {
                announceIntervalIndex = AnnounceIntervals.Length - 1;
                lastHourAnnounced = (int)duration.TotalHours;
            } else {
                for( int i = 0; i < AnnounceIntervals.Length; i++ ) {
                    if( duration <= AnnounceIntervals[i] ) {
                        announceIntervalIndex = i - 1;
                        break;
                    }
                }
            }
            task = Scheduler.NewTask( TimerCallback, this );
            IsRunning = true;
            task.RunRepeating( TimeSpan.Zero,
                               TimeSpan.FromSeconds( 1 ),
                               oneSecondRepeats );
        }


        int lastHourAnnounced;
        static void TimerCallback( SchedulerTask task ) {
            ChatTimer timer = (ChatTimer)task.UserState;
            if( task.MaxRepeats == 1 ) {
                if( String.IsNullOrEmpty( timer.Message ) ) {
                    Chat.SendSay( Player.Console, "(Timer Up)" );
                } else {
                    Chat.SendSay( Player.Console, "(Timer Up) " + timer.Message );
                }
                timer.IsRunning = false;

            } else if( timer.announceIntervalIndex >= 0 ) {
                if( timer.lastHourAnnounced != (int)timer.TimeLeft.TotalHours ) {
                    timer.lastHourAnnounced = (int)timer.TimeLeft.TotalHours;
                    timer.Announce( TimeSpan.FromHours( Math.Ceiling( timer.TimeLeft.TotalHours ) ) );
                }
                if( timer.TimeLeft <= AnnounceIntervals[timer.announceIntervalIndex] ) {
                    timer.Announce( AnnounceIntervals[timer.announceIntervalIndex] );
                    timer.announceIntervalIndex--;
                }
            }
        }

        void Announce( TimeSpan timeLeft ) {
            if( String.IsNullOrEmpty( Message ) ) {
                Chat.SendSay( Player.Console, "(Timer) " + timeLeft.ToMiniString() );
            } else {
                Chat.SendSay( Player.Console,
                              String.Format( "(Timer) {0} until {1}",
                                             timeLeft.ToMiniString(),
                                             Message ) );
            }
        }


        public static ChatTimer Start( TimeSpan duration, string message ) {
            if( duration < MinDuration ) {
                throw new ArgumentException( "Timer duration should be at least 1s", "duration" );
            }
            return new ChatTimer( duration, message );
        }


        static readonly TimeSpan[] AnnounceIntervals = new[] {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(40),
            TimeSpan.FromSeconds(50),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(4),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(40),
            TimeSpan.FromMinutes(50)
        };
    }
}