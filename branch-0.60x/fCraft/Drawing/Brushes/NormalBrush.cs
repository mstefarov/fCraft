// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class NormalBrushFactory : IBrushFactory, IBrush {
        public static readonly NormalBrushFactory Instance = new NormalBrushFactory();


        NormalBrushFactory() {
            Aliases = new[] { "default", "-" };
        }


        public string Name {
            get { return "Normal"; }
        }


        public string[] Aliases { get; private set; }


        const string HelpString = "Normal brush: Fills the area with solid color. " +
                                  "If no block name is given, uses the last block that player has placed.";
        public string Help {
            get { return HelpString; }
        }


        public string Description {
            get { return Name; }
        }


        public IBrushFactory Factory {
            get { return this; }
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            return this;
        }


        [CanBeNull]
        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );
            List<Block> blocks = new List<Block>();

            while( cmd.HasNext ) {
                Block block;
                if( !cmd.NextBlock( player, true, out block ) ) {
                    return null;
                }
                blocks.Add( block );
            }

            return new NormalBrush( blocks.ToArray() );
        }
    }


    public sealed class NormalBrush : IBrushInstance {
        public Block[] Blocks { get; set; }


        public string InstanceDescription {
            get {
                if( Blocks.Length == 0 ) {
                    return Brush.Factory.Name;
                } else {
                    return String.Format( "{0}({1})", Brush.Factory.Name, Blocks.JoinToString() );
                }
            }
        }


        public IBrush Brush {
            get { return NormalBrushFactory.Instance; }
        }


        public NormalBrush( params Block[] blocks ) {
            Blocks = blocks;
        }


        public bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Blocks == null || Blocks.Length == 0 ) {
                if( player.LastUsedBlockType == Block.None ) {
                    player.Message( "Cannot deduce desired block. Click a block or type out the block name." );
                    return false;
                } else {
                    Blocks = new[] {
                        player.GetBind( player.LastUsedBlockType )
                    };
                }
            }
            return true;
        }


        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            if( state.AlternateBlockIndex < Blocks.Length ) {
                return Blocks[state.AlternateBlockIndex];
            } else {
                return Block.None;
            }
        }


        public int AlternateBlocks {
            get { return Blocks.Length; }
        }


        public void End() { }
    }
}