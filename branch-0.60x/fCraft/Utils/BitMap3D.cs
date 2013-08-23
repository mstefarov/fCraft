using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft {
    public class BitMap3D : IEnumerable<Vector3I> {
        const int BitCoordMask = 31;

        readonly uint[] store;
        readonly Vector3I offset,
                          dimensions;
        int version;


        /// <summary> Number of set bits within this BitMap. </summary>
        public int Count { get; private set; }

        /// <summary> Bounding box within which coordinates are stored. </summary>
        public BoundingBox Bounds { get; set; }


        /// <summary> Creates a new 3D bit array, within the given bounds. </summary>
        /// <param name="bounds"> Bounding box inside which the coordinates are stored. </param>
        public BitMap3D( BoundingBox bounds ) {
            dimensions = bounds.Dimensions;
            offset = bounds.MinVertex;
            Bounds = bounds;

            // round width up to nearest multiple of 32
            int bitCapacity = (int)((bounds.Volume + sizeof( uint ) - 1) & (uint.MaxValue ^ BitCoordMask));
            int intCapacity = bitCapacity/sizeof( uint );
            store = new uint[intCapacity];
        }


        void Index( Vector3I coord, out int intIndex, out uint bitMask ) {
            Vector3I localCoord = coord - offset;
            int index = (localCoord.Z*dimensions.Y + localCoord.Y)*dimensions.X + localCoord.X;
            intIndex = index & (int.MaxValue ^ BitCoordMask);
            int bitIndex = index & BitCoordMask;
            bitMask = 1u << (bitIndex + 1);
        }


        /// <summary> Gets value of a bit at given coordinate. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Given coordinate is outside the bounds. </exception>
        public bool Get( Vector3I coord ) {
            if( !Bounds.Contains( coord ) ) throw new ArgumentOutOfRangeException( "coord" );
            int intIndex;
            uint bitMask;
            Index( coord, out intIndex, out bitMask );

            return (store[intIndex] & bitMask) != 0;
        }


        /// <summary> Sets bit at given coordinate to 1. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Given coordinate is outside the bounds. </exception>
        public bool Set( Vector3I coord ) {
            if( !Bounds.Contains( coord ) ) throw new ArgumentOutOfRangeException( "coord" );
            int intIndex;
            uint bitMask;
            Index( coord, out intIndex, out bitMask );

            bool oldVal = (store[intIndex] & bitMask) != 0;
            if( !oldVal ) {
                store[intIndex] |= bitMask;
                Count++;
                version++;
            }
            return oldVal;
        }


        /// <summary> Sets bit at given coordinate to 0. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> Given coordinate is outside the bounds. </exception>
        public bool Unset( Vector3I coord ) {
            if( !Bounds.Contains( coord ) ) throw new ArgumentOutOfRangeException( "coord" );
            int intIndex;
            uint bitMask;
            Index( coord, out intIndex, out bitMask );

            bool oldVal = (store[intIndex] & bitMask) != 0;
            if( oldVal ) {
                store[intIndex] &= ~bitMask;
                Count--;
                version++;
            }
            return oldVal;
        }


        /// <summary> Resets all bits to 0. </summary>
        public void Clear() {
            Array.Clear( store, 0, store.Length );
            Count = 0;
            version++;
        }


        #region Implementation of IEnumerable

        public IEnumerator<Vector3I> GetEnumerator() {
            return new BitMap3DEnumerator( this );
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }


        class BitMap3DEnumerator : IEnumerator<Vector3I> {
            readonly BitMap3D bitmap;
            readonly int startingVersion;
            int intIndex,
                bitIndex;
            uint currentBlock;


            public Vector3I Current { get; private set; }

            object IEnumerator.Current {
                get { return Current; }
            }


            public BitMap3DEnumerator( [NotNull] BitMap3D bitmap ) {
                if( bitmap == null ) {
                    throw new ArgumentNullException( "bitmap" );
                }
                this.bitmap = bitmap;
                startingVersion = bitmap.version;
                currentBlock = bitmap.store[0];
            }


            public bool MoveNext() {
                // Make sure bitmap has not been modified
                if( bitmap.version != startingVersion ) {
                    throw new InvalidOperationException( "BitMap3D collection modified while enumerating." );
                }
                // If we're reached the end already, abort.
                if( intIndex >= bitmap.store.Length ) return false;

                bitIndex++;
                ITriedToAvoidGotoButICannotThinkOfABetterWayRightNow:
                // If we reached the end of block, look for next non-zero block
                if( bitIndex > 31 ) {
                    bitIndex = 0;
                    intIndex++;
                    // Look for the next non-zero blocks
                    while( intIndex < bitmap.store.Length && bitmap.store[intIndex] == 0 ) {
                        intIndex++;
                    }
                    // If we're reached the end, abort.
                    if( intIndex >= bitmap.store.Length ) return false;
                    currentBlock = bitmap.store[intIndex];
                }

                // Look for next set bit.
                uint bitMask = (1u << bitIndex);
                while( bitIndex > 31 && (currentBlock & bitMask) == 0 ) {
                    bitIndex++;
                    bitMask <<= 1;
                }

                if( bitIndex > 31 ) goto ITriedToAvoidGotoButICannotThinkOfABetterWayRightNow;
                // we're guaranteed to have found the right bit by now
                int globalBitIndex = intIndex*sizeof( uint ) + bitIndex;

                // derive (x,y,z) coordinates from current block index
                int x = globalBitIndex%bitmap.dimensions.X + bitmap.offset.X;
                int y = (globalBitIndex/bitmap.dimensions.X)%bitmap.dimensions.Y + bitmap.offset.Y;
                int z = y/(bitmap.dimensions.X*bitmap.dimensions.Y) + bitmap.offset.Z;
                Current = new Vector3I( x, y, z );
                return true;
            }


            public void Reset() {
                intIndex = 0;
                bitIndex = -1;
                currentBlock = bitmap.store[0];
            }

            public void Dispose() {}
        }

        #endregion
    }
}