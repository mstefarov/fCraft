// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs ReplaceNotBrush. </summary>
    public sealed class ReplaceNotBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the ReplaceNotBrushFactory. </summary>
        public static readonly ReplaceNotBrushFactory Instance = new ReplaceNotBrushFactory();

        ReplaceNotBrushFactory() {
            Aliases = new[] {"rn"};
        }

        public string Name {
            get { return "ReplaceNot"; }
        }

        public string[] Aliases { get; private set; }

        const string HelpString = "ReplaceNot brush: Replaces all blocks except the given type(s) with another type. " +
                                  "Usage similar to &H/ReplaceNot&S command.";

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
                    player.Message( "{0} brush: Please specify the replacement block type.", Name );
                    return null;
                case 1:
                    return new ReplaceNotBrush( blocks.ToArray(), Block.None );
                default: {
                    Block replacement = blocks.Pop();
                    return new ReplaceNotBrush( blocks.ToArray(), replacement );
                }
            }
        }

        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that replaces all blocks EXCEPT those of given type(s) with a replacement block type. </summary>
    public sealed class ReplaceNotBrush : ReplaceBrush {
        public ReplaceNotBrush( Block[] blocks, Block replacement )
            : base( blocks, replacement ) {}


        public override IBrushFactory Factory {
            get { return ReplaceNotBrushFactory.Instance; }
        }


        public override Block NextBlock( DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            Block block = op.Map.GetBlock( op.Coords );
            for( int i = 0; i < Blocks.Length; i++ ) {
                if( block == Blocks[i] ) {
                    return Block.None;
                }
            }
            return Replacement;
        }
    }
}
