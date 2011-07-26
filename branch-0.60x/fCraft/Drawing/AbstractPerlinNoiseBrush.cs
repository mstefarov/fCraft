using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public abstract class AbstractPerlinNoiseBrush : IBrushInstance {
        public Block Block1 { get; protected set; }
        public Block Block2 { get; protected set; }
        public int Seed { get; protected set; }
        public float Coverage { get; protected set; }


        protected AbstractPerlinNoiseBrush( Block block1, Block block2, int seed ) {
            Block1 = block1;
            Block2 = block2;
            Seed = seed;
            Coverage = .5f;
        }


        protected AbstractPerlinNoiseBrush( AbstractPerlinNoiseBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Block1 = other.Block1;
            Block2 = other.Block2;
            Seed = other.Seed;
            Coverage = other.Coverage;
        }


        protected Block[, ,] data;

        public virtual bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            PerlinNoise3D noise3D = new PerlinNoise3D( new Random( Seed ) ) {
                Amplitude = 1,
                Frequency = 0.08f,
                Octaves = 3,
                Persistence = .8f
            };

            float[, ,] rawData = new float[state.Bounds.Width, state.Bounds.Length, state.Bounds.Height];
            for( int x = 0; x < state.Bounds.Width; x++ ) {
                for( int y = 0; y < state.Bounds.Length; y++ ) {
                    for( int z = 0; z < state.Bounds.Height; z++ ) {
                        rawData[x, y, z] = noise3D.Compute( x, y, z );
                    }
                }
            }

            data = new Block[state.Bounds.Width, state.Bounds.Length, state.Bounds.Height];

            ProcessData( rawData, data );
            return true;
        }


        protected abstract void ProcessData( float[, ,] rawData, Block[,,] data );


        public virtual Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            Vector3I relativeCoords = state.Coords - state.Bounds.MinVertexV;
            return data[relativeCoords.X, relativeCoords.Y, relativeCoords.Z];
        }


        public virtual void End() {
            data = null;
        }


        public abstract IBrush Brush { get; }

        public abstract string InstanceDescription { get; }

        public bool HasAlternateBlock {
            get { return false; }
        }
    }
}