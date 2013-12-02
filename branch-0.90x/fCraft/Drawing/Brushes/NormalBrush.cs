// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs NormalBrush. </summary>
    public sealed class NormalBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the NormalBrushFactory. </summary>
        public static readonly NormalBrushFactory Instance = new NormalBrushFactory();


        public string[] Aliases { get; private set; }

        public string Help {
            get {
                return "Normal brush: Fills the area with solid color. " +
                       "If no block name is given, uses the last block that player has placed.";
            }
        }

        public string Name {
            get { return "Normal"; }
        }


        NormalBrushFactory() {
            Aliases = new[] {"default", "-"};
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

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

        public IBrush MakeDefault() {
            return new NormalBrush();
        }
    }


    /// <summary> Brush that creates a solid, single-block fill. </summary>
    public sealed class NormalBrush : IBrush {
        public int AlternateBlocks {
            get { return Blocks.Length; }
        }


        public Block[] Blocks { get; private set; }


        public string Description {
            get {
                if( Blocks.Length == 0 ) {
                    return Factory.Name;
                } else {
                    return String.Format( "{0}({1})", Factory.Name, Blocks.JoinToString() );
                }
            }
        }


        public IBrushFactory Factory {
            get { return NormalBrushFactory.Instance; }
        }


        public NormalBrush() {}

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


        public void End() {}


        public IBrush Clone() {
            return new NormalBrush( Blocks );
        }
    }
}
