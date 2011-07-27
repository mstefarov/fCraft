// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class RandomBrushFactory : IBrushFactory {
        public static readonly RandomBrushFactory Instance = new RandomBrushFactory();

        RandomBrushFactory() { }

        public string Name {
            get { return "Random"; }
        }


        public IBrush MakeBrush( Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            List<Block> blocks = new List<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Add( block );
            }
            if( blocks.Count == 0 ) {
                return new RandomBrush( new Block[0] );
            } else if( blocks.Count == 1 ) {
                return new RandomBrush( blocks[0] );
            } else {
                return new RandomBrush( blocks.ToArray() );
            }
        }
    }


    public sealed class RandomBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; private set; }
        readonly Random rand = new Random();

        public RandomBrush( Block oneBlock ) {
            Blocks = new[] { oneBlock, Block.Undefined };
        }

        public RandomBrush( Block[] blocks ) {
            Blocks = blocks;
        }


        public RandomBrush( RandomBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return RandomBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks.Length == 0 ) {
                    return Factory.Name;
                } else {
                    return String.Format( "{0}({1})",
                                          Factory.Name,
                                          Blocks.JoinToString() );
                }
            }
        }


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            List<Block> blocks = new List<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Add( block );
            }

            if( blocks.Count == 0 ) {
                if( Blocks.Length == 0 ) {
                    player.Message( "{0}: Please specify at least one block.", Factory.Name );
                    return null;
                } else {
                    return new RandomBrush( this );
                }
            } else if( blocks.Count == 1 ) {
                return new RandomBrush( blocks[0] );
            } else {
                return new RandomBrush( blocks.ToArray() );
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
            return Blocks[rand.Next( Blocks.Length )];
        }


        public void End() { }

        #endregion
    }
}