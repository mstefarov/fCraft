// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs RandomBrush. </summary>
    public sealed class RandomBrushFactory : IBrushFactory {
        public static readonly RandomBrushFactory Instance = new RandomBrushFactory();

        RandomBrushFactory() {
            Aliases = new[] {"Rand"};
        }

        public string Name {
            get { return "Random"; }
        }

        public string[] Aliases { get; private set; }

        public string Help {
            get {
                return "Random brush: Chaotic pattern of two or more random block types. " +
                       "If only one block name is given, leaves every other block untouched.";
            }
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio;
                Block block;
                if( !cmd.NextBlockWithParam( player, true, out block, out ratio ) ) return null;
                if( ratio < 1 || ratio > RandomBrush.MaxRatio ) {
                    player.Message( "RandomBrush: Invalid block ratio ({0}). Must be between 1 and {1}.",
                                    ratio,
                                    RandomBrush.MaxRatio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            switch( blocks.Count ) {
                case 0:
                    player.Message( "{0} brush: Please specify at least one block.", Name );
                    return null;
                case 1:
                    return new RandomBrush( blocks[0], blockRatios[0] );
                default:
                    return new RandomBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }

        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that creates a random pattern,
    /// with individually adjustable probabilities for each block type. </summary>
    public sealed class RandomBrush : IBrush {
        public const int MaxRatio = 10000;

        readonly Block[] actualBlocks;
        readonly int seed = new Random().Next();


        public int AlternateBlocks {
            get { return 1; }
        }

        public Block[] Blocks { get; private set; }

        public int[] BlockRatios { get; private set; }

        public string Description {
            get {
                if( Blocks.Length == 0 ) {
                    return Factory.Name;
                } else if( Blocks.Length == 1 || (Blocks.Length == 2 && Blocks[1] == Block.None) ) {
                    return String.Format( "{0}({1})", Factory.Name, Blocks[0] );
                } else {
                    StringBuilder sb = new StringBuilder();
                    sb.Append( Factory.Name );
                    sb.Append( '(' );
                    for( int i = 0; i < Blocks.Length; i++ ) {
                        if( i != 0 ) sb.Append( ',' ).Append( ' ' );
                        sb.Append( Blocks[i] );
                        if( BlockRatios[i] > 1 ) {
                            sb.Append( '/' );
                            sb.Digits( BlockRatios[i] );
                        }
                    }
                    sb.Append( ')' );
                    return sb.ToString();
                }
            }
        }

        public IBrushFactory Factory {
            get { return RandomBrushFactory.Instance; }
        }


        public RandomBrush( Block oneBlock, int ratio ) {
            Blocks = new[] {oneBlock, Block.None};
            BlockRatios = new[] {ratio, 1};
            actualBlocks = new[] {oneBlock, Block.None};
        }


        public RandomBrush( [NotNull] Block[] blocks, int[] ratios ) {
            if( blocks == null ) throw new ArgumentNullException( "blocks" );
            Blocks = blocks;
            BlockRatios = ratios;
            actualBlocks = new Block[BlockRatios.Sum()];
            int c = 0;
            for( int i = 0; i < Blocks.Length; i++ ) {
                for( int j = 0; j < BlockRatios[i]; j++ ) {
                    actualBlocks[c] = Blocks[i];
                    c++;
                }
            }
        }


        public bool Begin( Player player, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }
            return true;
        }

        public Block NextBlock( DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            int n = seed ^ (op.Coords.X + 1290*op.Coords.Y + 1664510*op.Coords.Z);
            n = (n << 13) ^ n;
            n = (n*(n*n*15731 + 789221) + 1376312589) & 0x7FFFFFFF;
            double derp = (n/(double) 0x7FFFFFFF)*actualBlocks.Length;
            return actualBlocks[(int) Math.Floor( derp )];
        }

        public void End() {}

        public IBrush Clone() {
            return new RandomBrush( Blocks, BlockRatios );
        }
    }
}
