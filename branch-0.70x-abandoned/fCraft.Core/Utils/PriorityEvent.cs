// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {
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
            Callbacks.Sort( ( pair1, pair2 ) => pair2.Key - pair1.Key );
            Delegate[] newCombined = Callbacks.Select( pair => pair.Value ).ToArray();
            combined = (EventHandler<T>)Delegate.Combine( newCombined );
        }


        public void Raise( object sender, T e ) {
            var handler = combined;
            if( handler != null ) {
                handler( sender, e );
            }
        }


        public void Raise( T e ) {
            var handler = combined;
            if( handler != null ) {
                handler( null, e );
            }
        }
    }


    /// <summary> Event callback priority.
    /// Lower-priority callbacks are invoked sooner.
    /// Higher-priority events are invoked later.</summary>
    public enum Priority {
        Lowest = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Highest = 4
    }
}