// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class CloudyBrushFactory : IBrushFactory {
        public static readonly CloudyBrushFactory Instance = new CloudyBrushFactory();

        CloudyBrushFactory() { }

        public string Name {
            get { return "Cloudy"; }
        }

        static Random rand = new Random();
        public static int NextSeed() {
            lock( rand ) {
                return rand.Next();
            }
        }

        public IBrush MakeBrush( Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            Block block = cmd.NextBlock( player );
            Block altBlock = cmd.NextBlock( player );

            int seed;
            if( !cmd.NextInt( out seed ) ) seed = NextSeed();

            return new CloudyBrush( block, altBlock, seed );
        }
    }


    public sealed class CloudyBrush : IBrushInstance, IBrush {
        public Block Block1 { get; private set; }
        public Block Block2 { get; private set; }
        Noise noise;
        public int Seed { get; private set; }


        public CloudyBrush( Block block1, Block block2, int seed ) {
            Block1 = block1;
            Block2 = block2;
            Seed = seed;
            noise = new Noise( seed, NoiseInterpolationMode.Cosine );
        }


        public CloudyBrush( CloudyBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Block1 = other.Block1;
            Block2 = other.Block2;
            Seed = other.Seed;
            noise = other.noise;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return CloudyBrushFactory.Instance; }
        }


        public string Description {
            get {
                return String.Format( "{0}({1},{2},{3})", Factory.Name, Block1, Block2, Seed );
            }
        }


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            if( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                Block altBlock = cmd.NextBlock( player );
                Block1 = block;
                Block2 = altBlock;
                int seed;
                if( !cmd.NextInt( out seed ) ) seed = CloudyBrushFactory.NextSeed();

            } else if( Block1 == Block.Undefined ) {
                player.Message( "{0}: Please specify at least one block.", Factory.Name );
                return null;
            }

            return new CloudyBrush( this );
        }

        #endregion


        #region IBrushInstance members

        public IBrush Brush {
            get { return this; }
        }


        public bool HasAlternateBlock {
            get { return false; }
        }


        public string InstanceDescription {
            get {
                return Description;
            }
        }

        Block[, ,] data;

        public unsafe bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            float[, ,] rawData = new float[state.Bounds.Width, state.Bounds.Length, state.Bounds.Height];
            data = new Block[state.Bounds.Width, state.Bounds.Length, state.Bounds.Height];
            int maxPow = (int)Math.Log(Math.Max(state.Bounds.Width,Math.Max(state.Bounds.Height,state.Bounds.Length)),2);
            noise.PerlinNoise( rawData, 2, maxPow, .5f, state.Marks[0].X, state.Marks[0].Y, state.Marks[0].Z );
            Noise.Normalize( rawData );
            int length = rawData.GetLength( 0 ) * rawData.GetLength( 1 ) * rawData.GetLength( 2 );
            fixed( float* rawPtr = rawData ) {
                fixed( Block* ptr = data ) {
                    for( int i = 0; i < length; i++ ) {
                        if( rawPtr[i] < .5 ) ptr[i] = Block1;
                        else ptr[i] = Block2;
                    }
                }
            }
            return true;
        }


        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            Vector3I relativeCoords = state.Coords - state.Bounds.MinVertexV;
            return data[relativeCoords.X, relativeCoords.Y, relativeCoords.Z];
        }


        public void End() {
            data = null;
        }

        #endregion
    }
}