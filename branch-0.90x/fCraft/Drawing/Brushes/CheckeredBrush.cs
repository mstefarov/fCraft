// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;

namespace fCraft.Drawing {
    /// <summary> Constructs CheckeredBrush. </summary>
    public sealed class CheckeredBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the CheckeredBrushFactory. </summary>
        public static readonly CheckeredBrushFactory Instance = new CheckeredBrushFactory();


        public string Name {
            get { return "Checkered"; }
        }

        public string[] Aliases { get; private set; }

        public string Help {
            get {
                return "Checkered brush: Fills the area with alternating checkered pattern. " +
                       "If only one block name is given, leaves every other block untouched.";
            }
        }


        CheckeredBrushFactory() {
            Aliases = new[] {"ch"};
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            Block block, altBlock;

            // first block type is required
            if( !cmd.NextBlock( player, true, out block ) ) {
                player.Message( "{0}: Please specify at least one block type.", Name );
                return null;
            }

            // second block type is optional
            if( cmd.HasNext ) {
                if( !cmd.NextBlock( player, true, out altBlock ) ) return null;
            } else {
                altBlock = Block.None;
            }

            return new CheckeredBrush( block, altBlock );
        }


        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that alternates between two block types, in a checkered pattern. </summary>
    public sealed class CheckeredBrush : IBrush {
        public int AlternateBlocks {
            get { return 1; }
        }

        /// <summary> First block in the alternating pattern. </summary>
        public Block Block1 { get; private set; }

        /// <summary> Second block in the alternating pattern. </summary>
        public Block Block2 { get; private set; }

        public string Description {
            get {
                if( Block2 != Block.None ) {
                    return String.Format( "{0}({1},{2})", Factory.Name, Block1, Block2 );
                } else if( Block1 != Block.None ) {
                    return String.Format( "{0}({1})", Factory.Name, Block1 );
                } else {
                    return Factory.Name;
                }
            }
        }

        public IBrushFactory Factory {
            get { return CheckeredBrushFactory.Instance; }
        }


        /// <summary> Initializes a new instance of CheckeredBrush. </summary>
        public CheckeredBrush( Block block1, Block block2 ) {
            Block1 = block1;
            Block2 = block2;
        }


        public bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            return true;
        }

        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            if( ((state.Coords.X + state.Coords.Y + state.Coords.Z) & 1) == 1 ) {
                return Block1;
            } else {
                return Block2;
            }
        }

        public void End() {}

        public IBrush Clone() {
            return new CheckeredBrush( Block1, Block2 );
        }
    }
}
