// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public class SolidBrushFactory : IBrushFactory, IBrush {
        public static readonly SolidBrushFactory Instance = new SolidBrushFactory();
        SolidBrushFactory() { }

        public string Name {
            get { return "Solid"; }
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


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperationState state ) {
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

            return new SolidBrush( block, altBlock );
        }
    }


    public class SolidBrush : IBrushInstance {
        Block Block, AltBlock;

        public IBrush Brush {
            get { return SolidBrushFactory.Instance; }
        }


        public SolidBrush( Block block, Block altBlock ) {
            if( block == Block.Undefined && altBlock != Block.Undefined ) {
                throw new ArgumentException( "Block must not be undefined if altblock is set.", "block" );
            }
            Block = block;
            AltBlock = altBlock;
        }


        public bool Begin( Player player, DrawOperationState state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Block == Block.Undefined ) {
                if( player.LastUsedBlockType == Block.Undefined ) {
                    player.Message( "Cannot imply desired blocktype. Click a block or type out the blocktype name." );
                    return false;
                } else {
                    Block = player.LastUsedBlockType;
                }
            }
            return true;
        }


        public Block NextBlock( DrawOperationState state ) {
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