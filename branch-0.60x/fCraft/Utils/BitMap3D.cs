using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Specialized set data structure. Holds 1 bit of information per coordinate,
    /// with an easy way to enumerate all set coords. Used by /Fill3D and related commands. </summary>
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

            // round capacity up to nearest multiple of 32
            int bitCapacity = (int)((bounds.Volume + sizeof( uint ) - 1) & (uint.MaxValue ^ BitCoordMask));
            int intCapacity = bitCapacity/sizeof( uint );
            store = new uint[intCapacity];
        }


        void Index( Vector3I coord, out int intIndex, out uint bitMask ) {
            Vector3I localCoord = coord - offset;
            int index = (localCoord.Z*dimensions.Y + localCoord.Y)*dimensions.X + localCoord.X;
            intIndex = (index >> 5);
            int bitIndex = index & BitCoordMask;
            bitMask = 1u << bitIndex;
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
            readonly int dimX, dimY, dimZ;
            int startingVersion,
                x, y, z,
                intIndex,
                bitIndex;
            uint storeInt;


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
                dimX = bitmap.dimensions.X;
                dimY = bitmap.dimensions.Y;
                dimZ = bitmap.dimensions.Z;
                Reset();
            }


            public bool MoveNext() {
                // make sure bitmap has not been modified since Reset()
                if( bitmap.version != startingVersion ) {
                    throw new InvalidOperationException( "BitMap3D modified while enumerating." );
                }
                while( true ) {
                    // advance real coordinates
                    x++;
                    if( x >= dimX ) {
                        x = 0;
                        y++;
                        if( y >= dimY ) {
                            y = 0;
                            z++;
                            if( z >= dimZ ) {
                                return false;
                            }
                        }
                    }
                    // advance array coordinates
                    bitIndex++;
                    if( bitIndex > 31 ) {
                        bitIndex = 0;
                        intIndex++;
                        if( intIndex >= bitmap.store.Length ) {
                            return false;
                        }
                        storeInt = bitmap.store[intIndex];
                    }
                    // check if current bit is set
                    uint bitMask = 1u << bitIndex;
                    if( (storeInt & bitMask) != 0 ) {
                        Current = new Vector3I( x, y, z );
                        return true;
                    }
                }
            }


            public void Reset() {
                intIndex = 0;
                bitIndex = -1;
                startingVersion = bitmap.version;
                storeInt = bitmap.store[0];
                x = -1;
                y = 0;
                z = 0;
            }

            public void Dispose() {}
        }

        #endregion
    }
}