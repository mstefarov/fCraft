// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs ReplaceBrush. </summary>
    public sealed class ReplaceBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the ReplaceBrushFactory. </summary>
        public static readonly ReplaceBrushFactory Instance = new ReplaceBrushFactory();

        ReplaceBrushFactory() {
            Aliases = new[] { "r" };
        }

        public string Name {
            get { return "Replace"; }
        }

        public string[] Aliases { get; private set; }

        const string HelpString = "Replace brush: Replaces blocks of a given type(s) with another type. " +
                                  "Usage similar to &H/Replace&S command.";
        public string Help {
            get { return HelpString; }
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
                    return new ReplaceBrush();
                case 1:
                    return new ReplaceBrush( blocks.ToArray(), Block.None );
                default: {
                    Block replacement = blocks.Pop();
                    return new ReplaceBrush( blocks.ToArray(), replacement );
                }
            }
        }
    }


    /// <summary> Brush that replaces all blocks of given type(s) with a replacement block type. </summary>
    public class ReplaceBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; protected set; }
        public Block Replacement { get; protected set; }

        public ReplaceBrush() { }

        public ReplaceBrush( Block[] blocks, Block replacement ) {
            Blocks = blocks;
            Replacement = replacement;
        }


        public ReplaceBrush( [NotNull] ReplaceBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            Replacement = other.Replacement;
        }


        #region IBrush members

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

        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( op == null ) throw new ArgumentNullException( "op" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block;
                if( !cmd.NextBlock( player, false, out block ) ) return null;
                blocks.Push( block );
            }

            if( blocks.Count == 0 && Blocks == null ) {
                player.Message( "{0} brush requires at least 1 block.", Factory.Name );
                return null;
            }

            if( blocks.Count > 0 ) {
                if( blocks.Count > 1 ) Replacement = blocks.Pop();
                Blocks = blocks.ToArray();
            }

            return new ReplaceBrush( this );
        }

        #endregion


        #region IBrushInstance members

        public IBrush Brush {
            get { return this; }
        }


        public int AlternateBlocks {
            get { return 1; }
        }


        public string InstanceDescription {
            get { return Description; }
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


        public void End() { }

        #endregion
    }
}