// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs CloudyBrush. </summary>
    public sealed class CloudyBrushFactory : IBrushFactory {
        /// <summary> Global singleton instance of CloudyBrushFactory. </summary>
        public static readonly CloudyBrushFactory Instance = new CloudyBrushFactory();

        public string Name {
            get { return "Cloudy"; }
        }

        public string[] Aliases {
            get { return null; }
        }

        public string Help {
            get { return "Cloudy brush: Creates a swirling pattern of two or more block types. " +
                         "If only one block name is given, leaves every other block untouched."; }
        }


        CloudyBrushFactory() { }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            bool scaleSpecified = false,
                 turbulenceSpecified = false,
                 seedSpecified = false;
            int scale = 100,
                turbulence = 100;
            UInt16 seed = CloudyBrush.NextSeed();

            while( true ) {
                int offset = cmd.Offset;
                string rawNextParam = cmd.Next();
                if( rawNextParam == null ) break;

                if( rawNextParam.EndsWith( "%" ) ) {
                    string numPart = rawNextParam.Substring( 0, rawNextParam.Length - 1 );
                    int tempScale;
                    if( !Int32.TryParse( numPart, out tempScale ) ) {
                        player.Message(
                            "Cloudy brush: To specify scale, write a number followed by a percentage (e.g. 100%)." );
                        return null;
                    }
                    if( scaleSpecified ) {
                        player.Message( "Cloudy brush: Scale has been specified twice." );
                        return null;
                    }
                    if( scale < 1 || tempScale > CloudyBrush.MaxScale ) {
                        player.Message( "Cloudy brush: Invalid scale ({0}). Must be between 1 and {1}",
                                        scale,
                                        CloudyBrush.MaxScale );
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
                        if( turbulence < 1 || tempTurbulence > CloudyBrush.MaxTurbulence ) {
                            player.Message( "Cloudy brush: Invalid turbulence ({0}). Must be between 1 and {1}",
                                            turbulence,
                                            CloudyBrush.MaxTurbulence );
                            return null;
                        }
                        turbulence = tempTurbulence;
                        turbulenceSpecified = true;
                        continue;
                    }

                } else if( rawNextParam.EndsWith( "S", StringComparison.OrdinalIgnoreCase ) ) {
                    string numPart = rawNextParam.Substring( 0, rawNextParam.Length - 1 );
                    try {
                        seed = UInt16.Parse( numPart, System.Globalization.NumberStyles.HexNumber );
                        if( seedSpecified ) {
                            player.Message( "Cloudy brush: Seed has been specified twice." );
                            return null;
                        }
                        seedSpecified = true;
                        continue;
                    } catch {
                        seed = CloudyBrush.NextSeed();
                    }
                }

                cmd.Offset = offset;
                int ratio;
                Block block;
                if( !cmd.NextBlockWithParam( player, true, out block, out ratio ) ) return null;
                if( ratio < 1 || ratio > CloudyBrush.MaxRatio ) {
                    player.Message( "{0} brush: Invalid block ratio ({1}). Must be between 1 and {2}.",
                                    Name, ratio, CloudyBrush.MaxRatio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            CloudyBrush madeBrush;
            switch( blocks.Count ) {
                case 0:
                    player.Message( "{0} brush: Please specify at least one block type.", Name );
                    return null;
                case 1:
                    madeBrush = new CloudyBrush( blocks[0], blockRatios[0] );
                    break;
                default:
                    madeBrush = new CloudyBrush( blocks.ToArray(), blockRatios.ToArray() );
                    break;
            }

            madeBrush.Frequency /= (scale/100f);
            madeBrush.Turbulence *= (turbulence/100f);
            madeBrush.Seed = seed;

            return madeBrush;
        }
    }


    /// <summary> Brush that uses 3D perlin noise to create "cloudy" patterns. </summary>
    public sealed class CloudyBrush : IBrush {
        static readonly object SeedGenLock = new object();
        static readonly Random SeedGenerator = new Random();

        const int ExtraLargeThreshold = 20*20*20;

        public const int MaxRatio = 10000,
                         MaxTurbulence = Int32.MaxValue,
                         MaxScale = Int32.MaxValue;

        public const float TurbulenceDefault = 0.75f,
                           FrequencyDefault = 0.08f;


        float[] computedThresholds;

        float normMultiplier,
              normConstant;

        PerlinNoise3D noise3D;


        public int AlternateBlocks {
            get { return 1; }
        }

        /// <summary> Seed of the random generator (unsigned short). </summary>
        public UInt16 Seed { get; set; }

        /// <summary> Number of octaves in the perlin noise generator. Defaults to 3. </summary>
        public int Octaves { get; set; }

        /// <summary> Frequency of the perlin noise generator.
        /// Higher frequency = lower "scale" of the brush. </summary>
        public float Frequency { get; set; }

        /// <summary> Turbulence of the perlin noise generator. </summary>
        public float Turbulence { get; set; }

        /// <summary> Array of blocks (at least one) used in the brush pattern. </summary>
        [NotNull]
        public Block[] Blocks { get; private set; }

        /// <summary> Corresponding ratios of each block type in Blocks array.
        /// A block with ratio of N will fill (N / SumOfRatios) of the drawn volume. 
        /// Thus, higher ratio means more a abundant block type. </summary>
        [NotNull]
        public int[] BlockRatios { get; private set; }


        public IBrushFactory Factory {
            get { return CloudyBrushFactory.Instance; }
        }


        public string Description {
            get {
                StringBuilder sb = new StringBuilder(Factory.Name);
                if (Blocks.Length == 0) {
                    return sb.ToString();
                }
                sb.Append('(');

                if (BlockRatios.All(r => r == 1) &&
                    (Blocks.Length == 1 || Blocks.Length == 2 && Blocks[1] == Block.None)) {
                    sb.Append(Blocks[0]);
                } else {
                    for (int i = 0; i < Blocks.Length; i++) {
                        if (i != 0) sb.Append(',').Append(' ');
                        sb.Append(Blocks[i]);
                        if (BlockRatios[i] > 1) {
                            sb.Append('/');
                            sb.Digits(BlockRatios[i]);
                        }
                    }
                }

                sb.Append(" -");

                if (Math.Abs(Frequency - FrequencyDefault) > 0.00001f) {
                    int scale = (int)Math.Round((FrequencyDefault * 100) / Frequency);
                    sb.AppendFormat(" {0:0}%", scale);
                }

                if (Math.Abs(Turbulence - TurbulenceDefault) > 0.00001f) {
                    int turbulence = (int)Math.Round((Turbulence * 100) / TurbulenceDefault);
                    sb.AppendFormat(" {0:0}T", turbulence);
                }

                sb.AppendFormat(" {0:X})", Seed);
                return sb.ToString();
            }
        }


        CloudyBrush() {
            Seed = NextSeed();
            Blocks = new Block[0];
            BlockRatios = new int[0];
            Turbulence = TurbulenceDefault;
            Frequency = FrequencyDefault;
            Octaves = 3;
        }


        public CloudyBrush( Block oneBlock, int ratio )
            : this() {
            Blocks = new[] {oneBlock, Block.None};
            BlockRatios = new[] {ratio, 1};
        }


        public CloudyBrush( [NotNull] Block[] blocks, [NotNull] int[] ratios )
            : this() {
            if( blocks == null ) throw new ArgumentNullException( "blocks" );
            if( ratios == null ) throw new ArgumentNullException( "ratios" );
            if( blocks.Length == 0 ) throw new ArgumentException( "At least one block type required." );
            if( blocks.Length != ratios.Length ) throw new ArgumentException( "Number of ratios must match number of blocks." );
            Blocks = blocks;
            BlockRatios = ratios;
        }


        public static UInt16 NextSeed() {
            lock (SeedGenLock) {
                return (UInt16)SeedGenerator.Next(UInt16.MaxValue);
            }
        }


        public bool Begin( Player player, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );

            bool extraLarge = (op.Bounds.Volume > ExtraLargeThreshold);

            if( extraLarge ) {
                player.MessageNow( "{0} brush: Preparing, please wait...", Factory.Name );
            }

            noise3D = new PerlinNoise3D( new Random( Seed ) ) {
                Amplitude = 1,
                Frequency = Frequency,
                Octaves = Octaves,
                Persistence = Turbulence
            };

            BoundingBox samplerBox = op.Bounds;
            int sampleScale = 1;
            if( extraLarge ) {
                samplerBox = new BoundingBox( op.Bounds.MinVertex,
                                              op.Bounds.Width/2,
                                              op.Bounds.Length/2,
                                              op.Bounds.Height/2 );
                sampleScale = 2;
            }

            // generate and normalize the raw (float) data
            float[,,] rawData = new float[samplerBox.Width, samplerBox.Length, samplerBox.Height];
            for( int x = 0; x < samplerBox.Width; x++ ) {
                for( int y = 0; y < samplerBox.Length; y++ ) {
                    for( int z = 0; z < samplerBox.Height; z++ ) {
                        rawData[x, y, z] = noise3D.Compute( x*sampleScale, y*sampleScale, z*sampleScale );
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
                float desiredCoverage = blocksSoFar/(float)totalBlocks;
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
            value = value*normMultiplier + normConstant;

            // find the right block type for given value
            for( int i = 1; i < Blocks.Length; i++ ) {
                if( computedThresholds[i] > value ) {
                    return Blocks[i - 1];
                }
            }
            return Blocks[Blocks.Length - 1];
        }


        public void End() { }
    }
}
