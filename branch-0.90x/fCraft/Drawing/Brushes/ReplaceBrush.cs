// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    /// <summary> Constructs ReplaceBrush. </summary>
    public sealed class ReplaceBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the ReplaceBrushFactory. </summary>
        public static readonly ReplaceBrushFactory Instance = new ReplaceBrushFactory();

        ReplaceBrushFactory() {
            Aliases = new[] {"r"};
        }

        public string Name {
            get { return "Replace"; }
        }

        public string[] Aliases { get; private set; }

        public string Help {
            get {
                return "Replace brush: Replaces blocks of a given type(s) with another type. " +
                       "Usage similar to &H/Replace&S command.";
            }
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block;
                if( !cmd.NextBlock( player, false, out block ) ) return null;
                blocks.Push( block );
            }
            switch( blocks.Count ) {
                case 0:
                    player.Message( "{0}: Please specify at least one block type.", Name );
                    return null;
                case 1:
                    return new ReplaceBrush( blocks.ToArray(), Block.None );
                default:
                    Block replacement = blocks.Pop();
                    return new ReplaceBrush( blocks.ToArray(), replacement );
            }
        }


        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that replaces all blocks of given type(s) with a replacement block type. </summary>
    public class ReplaceBrush : IBrush {
        public Block[] Blocks { get; protected set; }

        public Block Replacement { get; protected set; }


        public ReplaceBrush( Block[] blocks, Block replacement ) {
            Blocks = blocks;
            Replacement = replacement;
        }


        public virtual IBrushFactory Factory {
            get { return ReplaceBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks == null ) {
                    return Factory.Name;
                } else if( Replacement == Block.None ) {
                    return String.Format( "{0}({1} -> ?)",
                                          Factory.Name,
                                          Blocks.JoinToString() );
                } else {
                    return String.Format( "{0}({1} -> {2})",
                                          Factory.Name,
                                          Blocks.JoinToString(),
                                          Replacement );
                }
            }
        }


        public int AlternateBlocks {
            get { return 1; }
        }


        public bool Begin( Player player, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }
            if( Replacement == Block.None ) {
                if( player.LastUsedBlockType == Block.None ) {
                    player.Message( "Cannot deduce desired replacement block. Click a block or type out the block name." );
                    return false;
                } else {
                    Replacement = player.GetBind( player.LastUsedBlockType );
                }
            }
            op.Context |= BlockChangeContext.Replaced;
            return true;
        }


        public virtual Block NextBlock( DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            Block block = op.Map.GetBlock( op.Coords );
            for( int i = 0; i < Blocks.Length; i++ ) {
                if( block == Blocks[i] ) {
                    return Replacement;
                }
            }
            return Block.None;
        }


        public void End() {}


        public IBrush Clone() {
            return new ReplaceBrush( Blocks, Replacement );
        }
    }
}
