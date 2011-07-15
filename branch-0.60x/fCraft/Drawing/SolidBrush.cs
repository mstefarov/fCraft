// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public class SolidBrush : IBrush {
        public readonly Block Block;
        
        public string Name {
            get { return "Solid"; }
        }

        public string Description {
            get {
                return String.Format( "Solid({0})", Block );
            }
        }

        public SolidBrush( Block block ) {
            if( block == Block.Undefined ) {
                throw new ArgumentException( "Undefined blocktype given.", "block" );
            }
            Block = block;
        }

        public IBrush MakeBrush( Player player, Command cmd, DrawOperationState op ) {
            Block targetBlock = cmd.NextOrLastUsedBlock( player );
            if( targetBlock == Block.Undefined ) {
                return null;
            } else {
                return new SolidBrush( targetBlock );
            }
        }

        public Block NextBlock( DrawOperationState op ) {
            return Block;
        }
    }
}
