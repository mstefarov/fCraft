// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class NormalBrushFactory : IBrushFactory, IBrush {
        public static readonly NormalBrushFactory Instance = new NormalBrushFactory();

        NormalBrushFactory() {
            Aliases = new[] { "default", "-" };
        }


        public string Name {
            get { return "Normal"; }
        }

        public string[] Aliases { get; private set; }

        const string HelpString = "Normal brush: Fills the area with solid color. " +
                                  "If no block name is given, uses the last block that player has placed.";
        public string Help {
            get { return HelpString; }
        }


        public string Description {
            get { return Name; }
        }

        public IBrushFactory Factory {
            get { return this; }
        }


        public IBrush MakeBrush( [NotNull] Player player, [NotNull] CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            return this;
        }


        [CanBeNull]
        public IBrushInstance MakeInstance( [NotNull] Player player, [NotNull] CommandReader cmd, [NotNull] DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );
            Block block = Block.None,
                  altBlock = Block.None;

            if( cmd.HasNext ) {
                if( !cmd.NextBlock( player, true, out block ) ) {
                    return null;
                }
                if( cmd.HasNext && !cmd.NextBlock( player, true, out altBlock ) ) {
                    return null;
                }
            }

            return new NormalBrush( block, altBlock );
        }
    }


    public sealed class NormalBrush : IBrushInstance {
        public Block Block { get; set; }
        public Block AltBlock { get; set; }

        public bool HasAlternateBlock {
            get { return AltBlock != Block.None; }
        }

        public string InstanceDescription {
            get {
                if( Block == Block.None ) {
                    return Brush.Factory.Name;
                } else if( AltBlock == Block.None ) {
                    return String.Format( "{0}({1})", Brush.Factory.Name, Block );
                } else {
                    return String.Format( "{0}({1},{2})", Brush.Factory.Name, Block, AltBlock );
                }
            }
        }

        public IBrush Brush {
            get { return NormalBrushFactory.Instance; }
        }

        public NormalBrush( Block block ) {
            Block = block;
            AltBlock = Block.None;
        }

        public NormalBrush( Block block, Block altBlock ) {
            Block = block;
            AltBlock = altBlock;
        }


        public bool Begin( [NotNull] Player player, [NotNull] DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Block == Block.None ) {
                if( player.LastUsedBlockType == Block.None ) {
                    player.Message( "Cannot deduce desired block. Click a block or type out the block name." );
                    return false;
                } else {
                    Block = player.GetBind( player.LastUsedBlockType );
                }
            }
            return true;
        }


        public Block NextBlock( [NotNull] DrawOperation state ) {
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