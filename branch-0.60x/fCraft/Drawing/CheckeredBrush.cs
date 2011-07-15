// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public class CheckeredBrush : IBrush {
        public readonly Block Block1, Block2;
        
        public string Name {
            get { return "Checkered"; }
        }

        public string Description {
            get {
                return String.Format( "Checkered({0},{1})", Block1, Block2 );
            }
        }

        public CheckeredBrush( Block block1, Block block2 ) {
            if( block1 == Block.Undefined ) {
                throw new ArgumentException( "Undefined blocktype given.", "block1" );
            }
            if( block2 == Block.Undefined ) {
                throw new ArgumentException( "Undefined blocktype given.", "block2" );
            }
            Block1 = block1;
            Block2 = block2;
        }

        public IBrush MakeBrush( Player player, Command cmd, DrawOperationState op ) {
            if( cmd.HasNext() ) {
                Block targetBlock1 = cmd.NextBlock( player );
                Block targetBlock2 = cmd.NextBlock( player );
                if( targetBlock1 == Block.Undefined || targetBlock2 == Block.Undefined ) {
                    return null;
                } else {
                    return new CheckeredBrush( targetBlock1, targetBlock2 );
                }
            } else {
                return this;
            }
        }

        public Block NextBlock( DrawOperationState op ) {
            if( ((op.Coords.X + op.Coords.Y + op.Coords.Z) & 1) == 1 ) {
                return Block1;
            } else {
                return Block2;
            }
        }
    }
}
