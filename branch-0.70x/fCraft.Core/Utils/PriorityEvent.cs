// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;

namespace fCraft {
    static class EventTest {
        static readonly PriorityEvent<PlayerEventArgs> TestingEvent = new PriorityEvent<PlayerEventArgs>();

        public static event EventHandler<PlayerEventArgs> Testing {
            add { TestingEvent.Add( value, Priority.Normal ); }
            remove { TestingEvent.Remove( value ); }
        }

        public static void TestingPriority( EventHandler<PlayerEventArgs> callback, Priority priority ) {
            TestingEvent.Add( callback, priority );
        }
    }


    sealed class PriorityEvent<T> where T : EventArgs {
        static readonly List<KeyValuePair<Priority, EventHandler<T>>> Callbacks =
            new List<KeyValuePair<Priority, EventHandler<T>>>();
        EventHandler<T> combined;
        readonly object syncRoot = new object();


        public void Add( EventHandler<T> callback, Priority priority ) {
            lock( syncRoot ) {
                Callbacks.Add( new KeyValuePair<Priority, EventHandler<T>>( priority, callback ) );
                Combine();
            }
        }


        public void Remove( EventHandler<T> callback ) {
            lock( syncRoot ) {
                Callbacks.RemoveAt( Callbacks.FindLastIndex( pair => pair.Value == callback ) );
                Combine();
            }
        }


        void Combine() {
            Callbacks.Sort( ( pair1, pair2 ) => pair1.Key - pair2.Key );
            Delegate[] newCombined = Callbacks.Select( pair => pair.Value ).ToArray();
            combined = (EventHandler<T>)Delegate.Combine( newCombined );
        }


        public void Raise( object sender, T args ) {
            var handler = combined;
            if( handler != null ) handler( sender, args );
        }
    }


    enum Priority {
        Lowest = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Highest = 4
    }
}