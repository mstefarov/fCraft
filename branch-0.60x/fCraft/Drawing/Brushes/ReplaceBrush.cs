// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class ReplaceBrushFactory : IBrushFactory {
        public static readonly ReplaceBrushFactory Instance = new ReplaceBrushFactory();

        ReplaceBrushFactory() { }

        public string Name {
            get { return "Replace"; }
        }

        static readonly string[] aliases = new[] { "r" };
        public string[] Aliases {
            get { return aliases; }
        }

        const string help = "Replace brush: Replaces blocks of a given type(s) with another type. " +
                            "Usage similar to &H/replace&S command.";
        public string Help {
            get { return help; }
        }


        public IBrush MakeBrush( Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Push( block );
            }
            if( blocks.Count == 0 ) {
                return new ReplaceBrush();
            } else if( blocks.Count == 1 ) {
                player.Message( "Replace brush requires at least 2 blocks." );
                return null;
            } else {
                Block replacement = blocks.Pop();
                return new ReplaceBrush( blocks.ToArray(), replacement );
            }
        }
    }


    public sealed class ReplaceBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; private set; }
        public Block Replacement { get; private set; }

        public ReplaceBrush() { }

        public ReplaceBrush( Block[] blocks, Block replacement ) {
            Blocks = blocks;
            Replacement = replacement;
        }


        public ReplaceBrush( ReplaceBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            Replacement = other.Replacement;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return ReplaceBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks == null ) {
                    return Factory.Name;
                } else {
                    return String.Format( "{0}({1} -> {2})",
                                          Factory.Name,
                                          Blocks.JoinToString(),
                                          Replacement );
                }
            }
        }


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Push( block );
            }

            if( blocks.Count > 1 ) {
                Block replacement = blocks.Pop();
                return new ReplaceBrush( blocks.ToArray(), replacement );
            } else if( Blocks != null ) {
                return new ReplaceBrush( this );
            } else {
                player.Message( "Replace brush requires at least 2 blocks." );
                return null;
            }
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
            get { return Description; }
        }


        public bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }
            return true;
        }


        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            Block block = state.Map.GetBlock( state.Coords.X, state.Coords.Y, state.Coords.Z );
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < Blocks.Length; i++ ) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if( block == Blocks[i] ) {
                    return Replacement;
                }
            }
            return Block.Undefined;
        }


        public void End() { }

        #endregion
    }
}