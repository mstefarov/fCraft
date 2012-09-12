// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs CloudyBrush. </summary>
    public sealed class CloudyBrushFactory : IBrushFactory {
        public static readonly CloudyBrushFactory Instance = new CloudyBrushFactory();

        CloudyBrushFactory() {}

        public string Name {
            get { return "Cloudy"; }
        }

        public string[] Aliases {
            get { return null; }
        }

        const string HelpString = "Cloudy brush: Creates a swirling pattern of two or more block types. " +
                                  "If only one block name is given, leaves every other block untouched.";

        public string Help {
            get { return HelpString; }
        }


        [CanBeNull]
        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            bool scaleSpecified = false,
                 turbulenceSpecified = false,
                 seedSpecified = false;
            int scale = 100,
                turbulence = 100,
                seed = CloudyBrush.NextSeed();

            while( true ) {
                int offset = cmd.Offset;
                string rawNextParam = cmd.Next();
                if( rawNextParam == null ) break;

                if( rawNextParam.EndsWith( "%" ) ) {
                    string numPart = rawNextParam.Substring( 0, rawNextParam.Length - 1 );
                    int tempScale;
                    if( !Int32.TryParse( numPart, out tempScale ) ) {
                        player.Message( "Cloudy brush: To specify scale, write a number followed by a percentage (e.g. 100%)." );
                        return null;
                    }
                    if( scaleSpecified ) {
                        player.Message( "Cloudy brush: Scale has been specified twice." );
                        return null;
                    }
                    if( scale < 1 || tempScale > CloudyBrush.MaxScale ) {
                        player.Message( "Cloudy brush: Invalid scale ({0}). Must be between 1 and {1}",
                                        scale, CloudyBrush.MaxScale );
                        return null;
                    }
                    scale = tempScale;
                    scaleSpecified = true;
                    continue;

                } else if( rawNextParam.EndsWith( "T", StringComparison.OrdinalIgnoreCase ) ) {
                    string numPart = rawNextParam.Substring( 0, rawNextParam.Length - 1 );
                    int tempTurbulence;
                    if( Int32.TryParse( numPart, out tempTurbulence ) ) {
                        if( turbulenceSpecified ) {
                            player.Message( "Cloudy brush: Turbulence has been specified twice." );
                            return null;
                        }
                        if( turbulence < 1 || tempTurbulence > CloudyBrush.MaxScale ) {
                            player.Message( "Cloudy brush: Invalid turbulence ({0}). Must be between 1 and {1}",
                                            turbulence, CloudyBrush.MaxScale );
                            return null;
                        }
                        turbulence = tempTurbulence;
                        turbulenceSpecified = true;
                        continue;
                    }

                } else if( rawNextParam.EndsWith( "S", StringComparison.OrdinalIgnoreCase ) ) {
                    string numPart = rawNextParam.Substring( 0, rawNextParam.Length - 1 );
                    int tempSeed;
                    if( Int32.TryParse( numPart, out tempSeed ) ) {
                        if( seedSpecified ) {
                            player.Message( "Cloudy brush: Seed has been specified twice." );
                            return null;
                        }
                        seed = tempSeed;
                        seedSpecified = true;
                        continue;
                    } else {
                        try {
                            seed = (int)UInt32.Parse( numPart, System.Globalization.NumberStyles.HexNumber );
                            if( seedSpecified ) {
                                player.Message( "Cloudy brush: Seed has been specified twice." );
                                return null;
                            }
                            seed = tempSeed;
                            seedSpecified = true;
                            continue;
                        } catch {
                        }
                    }
                }

                cmd.Offset = offset;
                int ratio;
                Block block;
                if( !cmd.NextBlockWithParam( player, true, out block, out ratio ) ) return null;
                if( ratio < 1 || ratio > CloudyBrush.MaxRatio ) {
                    player.Message( "Cloudy brush: Invalid block ratio ({0}). Must be between 1 and {1}.",
                                    ratio, CloudyBrush.MaxRatio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            CloudyBrush madeBrush;
            if( blocks.Count == 0 ) {
                madeBrush = new CloudyBrush();
            } else if( blocks.Count == 1 ) {
                madeBrush = new CloudyBrush( blocks[0], blockRatios[0] );
            } else {
                madeBrush = new CloudyBrush( blocks.ToArray(), blockRatios.ToArray() );
            }

            madeBrush.Frequency /= ( scale / 100f );
            madeBrush.Persistence *= ( turbulence / 100f );
            madeBrush.Seed = seed;

            return madeBrush;
        }
    }


    /// <summary> Brush that uses 3D perlin noise to create "cloudy" patterns. </summary>
    public sealed class CloudyBrush : IBrush, IBrushInstance {
        public int Seed { get; set; }
        public float Frequency { get; set; }
        public int Octaves { get; set; }
        public float Persistence { get; set; }

        public Block[] Blocks { get; private set; }
        public int[] BlockRatios { get; private set; }

        float[] computedThresholds;
        float normMultiplier, normConstant;
        PerlinNoise3D noise3D;

        static readonly object SeedGenLock = new object();
        static readonly Random SeedGenerator = new Random();


        public const int MaxRatio = 10000,
                         ExtraLargeThreshold = 20 * 20 * 20,
                         MaxTurbulence = Int32.MaxValue,
                         MaxScale = Int32.MaxValue;


        public const float PersistenceDefault = 0.75f,
                           FrequencyDefault = 0.08f;

        public CloudyBrush() {
            Seed = NextSeed();
            Blocks = new Block[0];
            BlockRatios = new int[0];
            Persistence = PersistenceDefault;
            Frequency = FrequencyDefault;
            Octaves = 3;
        }


        public CloudyBrush( Block oneBlock, int ratio )
            : this() {
            Blocks = new[] { oneBlock, Block.None };
            BlockRatios = new[] { ratio, 1 };
        }


        public CloudyBrush( Block[] blocks, int[] ratios )
            : this() {
            Blocks = blocks;
            BlockRatios = ratios;
        }


        public CloudyBrush( CloudyBrush other ) {
            Blocks = other.Blocks;
            BlockRatios = other.BlockRatios;
            Seed = other.Seed;
            Frequency = other.Frequency;
            Octaves = other.Octaves;
            Persistence = other.Persistence;
        }


        public bool Begin( Player player, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );

            bool extraLarge = ( op.Bounds.Volume > ExtraLargeThreshold );

            if( extraLarge ) {
                player.MessageNow( "{0} brush: Preparing, please wait...", Brush.Factory.Name );
            }

            noise3D = new PerlinNoise3D( new Random( Seed ) ) {
                Amplitude = 1,
                Frequency = Frequency,
                Octaves = Octaves,
                Persistence = Persistence
            };

            BoundingBox samplerBox = op.Bounds;
            int sampleScale = 1;
            if( extraLarge ) {
                samplerBox = new BoundingBox( op.Bounds.MinVertex, op.Bounds.Width / 2, op.Bounds.Length / 2,
                                              op.Bounds.Height / 2 );
                sampleScale = 2;
            }

            // generate and normalize the raw (float) data
            float[,,] rawData = new float[samplerBox.Width,samplerBox.Length,samplerBox.Height];
            for( int x = 0; x < samplerBox.Width; x++ ) {
                for( int y = 0; y < samplerBox.Length; y++ ) {
                    for( int z = 0; z < samplerBox.Height; z++ ) {
                        rawData[x, y, z] = noise3D.Compute( x * sampleScale, y * sampleScale, z * sampleScale );
                    }
                }
            }
            Noise.Normalize( rawData, out normMultiplier, out normConstant );

            // create a mapping of raw data to blocks
            int totalBlocks = BlockRatios.Sum();
            int blocksSoFar = BlockRatios[0];
            computedThresholds = new float[Blocks.Length];
            computedThresholds[0] = 0;
            for( int i = 1; i < Blocks.Length; i++ ) {
                float desiredCoverage = blocksSoFar / (float)totalBlocks;
                computedThresholds[i] = Noise.FindThreshold( rawData, desiredCoverage );
                blocksSoFar += BlockRatios[i];
            }
            return true;
        }


        public Block NextBlock( DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            Vector3I relativeCoords = op.Coords - op.Bounds.MinVertex;
            float value = noise3D.Compute( relativeCoords.X, relativeCoords.Y, relativeCoords.Z );

            // normalize value
            value = value * normMultiplier + normConstant;

            // find the right block type for given value
            for( int i = 1; i < Blocks.Length; i++ ) {
                if( computedThresholds[i] > value ) {
                    return Blocks[i - 1];
                }
            }
            return Blocks[Blocks.Length - 1];
        }


        public static int NextSeed() {
            lock( SeedGenLock ) {
                return SeedGenerator.Next();
            }
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return CloudyBrushFactory.Instance; }
        }


        public string Description {
            get {
                StringBuilder sb = new StringBuilder( Factory.Name );
                if( Blocks.Length == 0 ) {
                    return sb.ToString();
                }
                sb.Append( '(' );

                if( BlockRatios[0]==1 && (Blocks.Length == 1 || Blocks.Length == 2 && Blocks[1] == Block.None ) ) {
                    sb.Append( Blocks[0] );
                } else {
                    for( int i = 0; i < Blocks.Length; i++ ) {
                        if( i != 0 ) sb.Append( ',' ).Append( ' ' );
                        sb.Append( Blocks[i] );
                        if( BlockRatios[i] > 1 ) {
                            sb.Append( '/' );
                            sb.Digits( BlockRatios[i] );
                        }
                    }
                }

                sb.Append( " |" );

                if( Math.Abs( Frequency - FrequencyDefault ) > 0.00001f ) {
                    int scale = (int)Math.Round( ( FrequencyDefault * 100 ) / Frequency );
                    sb.AppendFormat( " {0:0}%", scale );
                }

                if( Math.Abs( Persistence - PersistenceDefault ) > 0.00001f ) {
                    int turbulence = (int)Math.Round( ( Persistence * 100 ) / PersistenceDefault );
                    sb.AppendFormat( " {0:0}T", turbulence );
                }

                sb.AppendFormat( " {0:X}S", Seed );
                sb.Append( ')' );
                return sb.ToString();
            }
        }


        [CanBeNull]
        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio;
                Block block;
                if( !cmd.NextBlockWithParam( player, true, out block, out ratio ) ) return null;
                if( ratio < 1 || ratio > MaxRatio ) {
                    player.Message( "Cloudy brush: Invalid block ratio ({0}). Must be between 1 and {1}.",
                                    ratio, MaxRatio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            if( blocks.Count == 0 ) {
                if( Blocks.Length == 0 ) {
                    player.Message( "{0} brush: Please specify at least one block.", Factory.Name );
                    return null;
                } else {
                    return new CloudyBrush( this );
                }
            } else if( blocks.Count == 1 ) {
                return new CloudyBrush( blocks[0], blockRatios[0] );
            } else {
                return new CloudyBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }

        #endregion


        #region IBrushInstance members

        public int AlternateBlocks {
            get { return 1; }
        }


        public IBrush Brush {
            get { return this; }
        }


        public string InstanceDescription {
            get { return Description; }
        }


        public void End() {}

        #endregion
    }
}