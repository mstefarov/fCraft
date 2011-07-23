// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class NormalBrushFactory : IBrushFactory, IBrush {
        public static readonly NormalBrushFactory Instance = new NormalBrushFactory();

        NormalBrushFactory() { }


        public string Name {
            get { return "Normal"; }
        }

        public string Description {
            get { return Name; }
        }

        public IBrushFactory Factory {
            get { return this; }
        }


        public IBrush MakeBrush( Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            return this;
        }


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );
            Block block = Block.Undefined,
                  altBlock = Block.Undefined;

            if( cmd.HasNext ) {
                block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;

                if( cmd.HasNext ) {
                    altBlock = cmd.NextBlock( player );
                    if( altBlock == Block.Undefined ) return null;
                }
            }

            return new NormalBrush( block, altBlock );
        }
    }


    public sealed class NormalBrush : IBrushInstance {
        public Block Block { get; private set; }
        public Block AltBlock { get; private set; }

        public bool HasAlternateBlock {
            get { return AltBlock != Block.Undefined; }
        }

        public string InstanceDescription {
            get {
                if( Block == Block.Undefined ) {
                    return Brush.Factory.Name;
                } else if( AltBlock == Block.Undefined ) {
                    return String.Format( "{0}({1})", Brush.Factory.Name, Block );
                } else {
                    return String.Format( "{0}({1},{2})", Brush.Factory.Name, Block, AltBlock );
                }
            }
        }

        public IBrush Brush {
            get { return NormalBrushFactory.Instance; }
        }


        public NormalBrush( Block block, Block altBlock ) {
            if( block == Block.Undefined && altBlock != Block.Undefined ) {
                throw new ArgumentException( "Block must not be undefined if altblock is set.", "block" );
            }
            Block = block;
            AltBlock = altBlock;
        }


        public bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Block == Block.Undefined ) {
                if( player.LastUsedBlockType == Block.Undefined ) {
                    player.Message( "Cannot imply desired blocktype. Click a block or type out the blocktype name." );
                    return false;
                } else {
                    Block = player.GetBind( player.LastUsedBlockType );
                }
            }
            return true;
        }


        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            if( state.UseAlternateBlock ) {
                return AltBlock;
            } else {
                return Block;
            }
        }


        public void End() { }
    }
}