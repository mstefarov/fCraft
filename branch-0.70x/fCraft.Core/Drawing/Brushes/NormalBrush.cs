// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class NormalBrushFactory : IBrushFactory, IBrush {
        public static readonly NormalBrushFactory Instance = new NormalBrushFactory();

        NormalBrushFactory() {
            Aliases = new[] { "default", "-" };
        }


        /// <summary> Name of the specific IBrushFactory implementation. </summary>
        public string Name {
            get { return "Normal"; }
        }

        /// <summary> List of aliases/alternate names for this brush. May be null. </summary>
        public string[] Aliases { get; private set; }

        const string HelpString = "Normal brush: Fills the area with solid color. " +
                                  "If no block name is given, uses the last block that player has placed.";

        /// <summary> Help string to display to users. </summary>
        public string Help {
            get { return HelpString; }
        }


        /// <summary> A compact readable summary of brush type and configuration. </summary>
        public string Description {
            get { return Name; }
        }

        /// <summary> IBrushFactory associated with this brush type. </summary>
        public IBrushFactory Factory {
            get { return this; }
        }


        /// <summary> Creates a new brush for a player, based on given parameters. </summary>
        /// <param name="player"> Player who will be using this brush.
        /// Errors and warnings about the brush creation should be communicated by messaging the player. </param>
        /// <param name="cmd"> Parameters passed to the /Brush command (after the brush name). </param>
        /// <returns> A newly-made brush, or null if there was some problem with parameters/permissions. </returns>
        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            return this;
        }


        /// <summary> Creates an instance for this configured brush, for use with a specific DrawOperation. </summary>
        /// <param name="player"> Player who will be using this brush.
        /// Errors and warnings about the brush creation should be communicated by messaging the player. </param>
        /// <param name="cmd"> Parameters passed to the DrawOperation.
        /// If any are given, these parameters should generally replace any stored configuration. </param>
        /// <param name="op"> DrawOperation that will be using this brush. </param>
        /// <returns> A newly-made brush, or null if there was some problem with parameters/permissions. </returns>
        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( op == null ) throw new ArgumentNullException( "op" );
            Block block = Block.None,
                  altBlock = Block.None;

            if( cmd.HasNext ) {
                if( !cmd.NextBlock( player, true, out block ) ) return null;
                if( cmd.HasNext ) {
                    if( !cmd.NextBlock( player, true, out altBlock ) ) return null;
                }
            }

            return new NormalBrush( block, altBlock );
        }
    }


    public sealed class NormalBrush : IBrushInstance {
        public Block Block { get; set; }
        public Block AltBlock { get; set; }

        /// <summary> Whether the brush is capable of providing alternate blocks (e.g. for filling hollow DrawOps).</summary>
        public bool HasAlternateBlock {
            get { return AltBlock != Block.None; }
        }

        /// <summary> A compact readable summary of brush type, configuration, and state. </summary>
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

        /// <summary> Configured brush that created this instance. </summary>
        public IBrush Brush {
            get { return NormalBrushFactory.Instance; }
        }

        public NormalBrush( Block block ) {
            Block = block;
            AltBlock = Block.None;
        }

        public NormalBrush( Block block, Block altBlock ) {
            if( block == Block.None && altBlock != Block.None ) {
                throw new ArgumentException( "Block must not be undefined if altblock is set.", "block" );
            }
            Block = block;
            AltBlock = altBlock;
        }


        /// <summary> Called after the DrawOperation has been prepared.
        /// Should be used to verify that the brush is ready for use.
        /// Resources used by the brush should be obtained here. </summary>
        /// <param name="player"> Player who started the DrawOperation. </param>
        /// <param name="op"> DrawOperation that will be using this brush. </param>
        /// <returns> Whether this brush instance has successfully began or not. </returns>
        public bool Begin( Player player, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );
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


        /// <summary> Provides the next Block type for the given DrawOperation. </summary>
        /// <returns> Block type to place, or Block.Undefined to skip. </returns>
        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            if( state.UseAlternateBlock ) {
                return AltBlock;
            } else {
                return Block;
            }
        }


        /// <summary> Called when the DrawOperation is done or cancelled.
        /// Resources used by the brush should be freed/disposed here. </summary>
        public void End() { }
    }
}