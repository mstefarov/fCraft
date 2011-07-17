// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public class CheckeredBrushFactory : IBrushFactory {
        public static readonly CheckeredBrushFactory Instance = new CheckeredBrushFactory();

        CheckeredBrushFactory() { }

        public string Name {
            get { return "Checkered"; }
        }


        public IBrush MakeBrush( Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            Block block = cmd.NextBlock( player );
            if( block == Block.Undefined ) return null;
            Block altBlock = cmd.NextBlock( player );
            if( block == Block.Undefined ) return null;
            return new CheckeredBrush( block, altBlock );
        }
    }


    public class CheckeredBrush : IBrushInstance, IBrush {
        Block Block, AltBlock;

        public IBrushFactory Factory {
            get { return CheckeredBrushFactory.Instance; }
        }

        public string Description {
            get {
                return String.Format( "{0}({1},{2})", Factory.Name, Block, AltBlock );
            }
        }

        public IBrush Brush {
            get { return this; }
        }


        public CheckeredBrush( Block block, Block altBlock ) {
            if( block == Block.Undefined ) {
                throw new ArgumentException( "Block must not be undefined.", "block" );
            }
            if( altBlock == Block.Undefined ) {
                throw new ArgumentException( "AltBlock must not be undefined.", "altBlock" );
            }
            Block = block;
            AltBlock = altBlock;
        }

        public CheckeredBrush( CheckeredBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Block = other.Block;
            AltBlock = other.AltBlock;
        }


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperationState state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                Block altBlock = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                Block = block;
                AltBlock = altBlock;
            }
            return new CheckeredBrush( this );
        }


        public bool Begin( Player player, DrawOperationState state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            return true;
        }


        public Block NextBlock( DrawOperationState state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            if( ((state.Coords.X + state.Coords.Y + state.Coords.Z) & 1) == 1 ) {
                return Block;
            } else {
                return AltBlock;
            }
        }


        public void End() { }
    }
}