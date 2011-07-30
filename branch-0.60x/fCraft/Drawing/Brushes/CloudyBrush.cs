// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;

namespace fCraft.Drawing {
    public sealed class CloudyBrushFactory : IBrushFactory {
        public static readonly CloudyBrushFactory Instance = new CloudyBrushFactory();

        CloudyBrushFactory() { }

        public string Name {
            get { return "Cloudy"; }
        }

        public IBrush MakeBrush( Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio = 1;
                Block block = cmd.NextBlockWithParam( player, ref ratio );
                if( block == Block.Undefined ) return null;
                if( ratio < 0 || ratio > 1000 ) {
                    player.Message( "{0} brush: Invalid block ratio ({1}). Must be between 1 and 1000.",
                                    Name, ratio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            if( blocks.Count == 0 ) {
                return new CloudyBrush();
            } else if( blocks.Count == 1 ) {
                return new CloudyBrush( blocks[0] );
            } else {
                return new CloudyBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }
    }


    public sealed class CloudyBrush : AbstractPerlinNoiseBrush, IBrush {

        public CloudyBrush()
            : base() {
        }

        public CloudyBrush( Block oneBlock )
            : base( oneBlock ) {
        }

        public CloudyBrush( Block[] blocks, int[] ratios )
            : base( blocks, ratios ) {
        }

        public CloudyBrush( CloudyBrush other )
            : base( other ) {
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return CloudyBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks.Length == 0 ) {
                    return Factory.Name;
                } else if( Blocks.Length == 1 ) {
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


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio = 1;
                Block block = cmd.NextBlockWithParam( player, ref ratio );
                if( ratio < 0 || ratio > 1000 ) {
                    player.Message( "Invalid block ratio ({0}). Must be between 1 and 1000.", ratio );
                    return null;
                }
                if( block == Block.Undefined ) return null;
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
                return new CloudyBrush( blocks[0] );
            } else {
                return new CloudyBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }

        #endregion


        #region AbstractPerlinNoiseBrush members

        public override IBrush Brush {
            get { return this; }
        }

        public override string InstanceDescription {
            get {
                return Description;
            }
        }


        protected override float ProcessBlock( float rawValue, DrawOperation state ) {
            return rawValue;
        }

        #endregion
    }
}