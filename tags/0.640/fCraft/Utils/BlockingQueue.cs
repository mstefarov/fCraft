using System.Collections.Generic;
using System.Threading;

namespace fCraft {
    /// <summary> Multiple-producer, single-consumer queue.
    /// Dequeue blocks calling thread (consumer) until it has something to return. </summary>
    public class BlockingQueue<T> {
        readonly Queue<T> store = new Queue<T>();
        readonly AutoResetEvent signal = new AutoResetEvent( false );
        public int Count { get; private set; }
        public int QueuedCount { get; private set; }

        readonly object storeMutex = new object(),
                        consumerMutex = new object();


        public bool TryDequeue( out T result ) {
            lock( consumerMutex ) {
                lock( storeMutex ) {
                    if( store.Count > 0 ) {
                        result = store.Dequeue();
                        return true;
                    } else {
                        result = default(T);
                        return false;
                    }
                }
            }
        }


        public T WaitDequeue() {
            lock( consumerMutex ) {
                while( true ) {
                    lock( storeMutex ) {
                        if( store.Count > 0 ) {
                            Count = store.Count - 1;
                            return store.Dequeue();
                        } // else loop around, wait again
                    }
                    // waits for signal from Enqueue(), resets automatically
                    signal.WaitOne();
                }
            }
        }

        public void Enqueue( T item ) {
            lock( storeMutex ) {
                store.Enqueue( item );
                Count = store.Count;
                QueuedCount++;
                // signal that queue is no longer empty
                signal.Set();
            }
        }
    }
}