using System;
using System.Collections.Generic;
using System.Threading;

namespace fCraft {
    public sealed class ConcurrentQueue<T> {
        sealed class Node {
            public T value;
            public Pointer next;
            /// <summary>
            /// default constructor
            /// </summary>
            public Node() { }
        }

        struct Pointer {
            public long count;
            public Node ptr;

            /// <summary>
            /// copy constructor
            /// </summary>
            /// <param name="p"></param>
            public Pointer( Pointer p ) {
                ptr = p.ptr;
                count = p.count;
            }

            /// <summary>
            /// constructor that allows caller to specify ptr and count
            /// </summary>
            /// <param name="node"></param>
            /// <param name="c"></param>
            public Pointer( Node node, long c ) {
                ptr = node;
                count = c;
            }
        }
        Pointer Head;
        Pointer Tail;

        public ConcurrentQueue() {
            Node node = new Node();
            Head.ptr = Tail.ptr = node;
        }

        /// <summary>
        /// CAS
        /// stands for Compare And Swap
        /// Interlocked Compare and Exchange operation
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="compared"></param>
        /// <param name="exchange"></param>
        /// <returns></returns>
        static bool CAS( ref Pointer destination, Pointer compared, Pointer exchange ) {
            if( compared.ptr == Interlocked.CompareExchange( ref destination.ptr, exchange.ptr, compared.ptr ) ) {
                Interlocked.Exchange( ref destination.count, exchange.count );
                return true;
            }

            return false;
        }


        public bool Dequeue( ref T t ) {
            Pointer head;

            // Keep trying until deque is done
            bool bDequeNotDone = true;
            while( bDequeNotDone ) {
                // read head
                head = Head;

                // read tail
                Pointer tail = Tail;

                // read next
                Pointer next = head.ptr.next;

                // Are head, tail, and next consistent?
                if( head.count == Head.count && head.ptr == Head.ptr ) {
                    // is tail falling behind
                    if( head.ptr == tail.ptr ) {
                        // is the queue empty?
                        if( null == next.ptr ) {
                            // queue is empty cannnot dequeue
                            return false;
                        }

                        // Tail is falling behind. try to advance it
                        CAS( ref Tail, tail, new Pointer( next.ptr, tail.count + 1 ) );

                    } // endif
                    else // No need to deal with tail
                    {
                        // read value before CAS otherwise another deque might try to free the next node
                        t = next.ptr.value;

                        // try to swing the head to the next node
                        if( CAS( ref Head, head, new Pointer( next.ptr, head.count + 1 ) ) ) {
                            bDequeNotDone = false;
                        }
                    }

                } // endif

            } // endloop

            // dispose of head.ptr
            return true;
        }

        public void Enqueue( T t ) {
            // Allocate a new node from the free list
            Node node = new Node();

            // copy enqueued value into node
            node.value = t;

            // keep trying until Enqueue is done
            bool bEnqueueNotDone = true;

            while( bEnqueueNotDone ) {
                // read Tail.ptr and Tail.count together
                Pointer tail = Tail;

                // read next ptr and next count together
                Pointer next = tail.ptr.next;

                // are tail and next consistent
                if( tail.count == Tail.count && tail.ptr == Tail.ptr ) {
                    // was tail pointing to the last node?
                    if( null == next.ptr ) {
                        if( CAS( ref tail.ptr.next, next, new Pointer( node, next.count + 1 ) ) ) {
                            bEnqueueNotDone = false;
                        } // endif

                    } // endif
                    else // tail was not pointing to last node
                    {
                        // try to swing Tail to the next node
                        CAS( ref Tail, tail, new Pointer( next.ptr, tail.count + 1 ) );
                    }

                } // endif

            } // endloop
        }
    }
}
