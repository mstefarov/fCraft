// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs PasteBrush. </summary>
    public sealed class PasteBrushFactory : IBrushFactory {
        /// <summary> Global singleton instance that provides "Paste" brushes. </summary>
        public static readonly PasteBrushFactory PasteInstance = new PasteBrushFactory( false );

        /// <summary> Global singleton instance that provides "PasteNot" brushes. </summary>
        public static readonly PasteBrushFactory PasteNotInstance = new PasteBrushFactory( true );


        /// <summary> Whether this factory creates inclusive-filtering (Paste) or
        /// exclusive-filtering (PasteNot) variation of the brush. </summary>
        public bool Not { get; private set; }

        public string Name {
            get { return (Not ? "PasteNot" : "Paste"); }
        }

        public string[] Aliases {
            get { return new[] {Not ? "PN" : "P"}; }
        }

        public string Help {
            get { return Name + " brush: Makes a tiled pattern out of copied blocks."; }
        }


        PasteBrushFactory( bool not ) {
            Not = not;
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            // read the block filter list
            HashSet<Block> blocks = new HashSet<Block>();
            while( cmd.HasNext ) {
                Block block;
                if( !cmd.NextBlock( player, false, out block ) ) {
                    return null;
                }
                if( !blocks.Add( block ) ) {
                    // just a warning -- don't abort
                    player.Message( "{0}: {1} was specified twice!", Name, block );
                }
            }

            // create a brush
            if( blocks.Count > 0 ) {
                return new PasteBrush( blocks.ToArray(), Not );
            } else if( Not ) {
                player.Message( "PasteNot brush requires at least 1 block." );
                return null;
            } else {
                return new PasteBrush();
            }
        }

        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that makes a tiled pattern out of copied blocks. </summary>
    public sealed class PasteBrush : IBrush {
        public int AlternateBlocks {
            get { return 1; }
        }

        /// <summary> Whether this is an inclusive-filtering (Paste) or
        /// exclusive-filtering (PasteNot) variation of the brush. </summary>
        public bool Not { get; private set; }

        /// <summary> List of block types for filtering.
        /// If this is a Paste brush (Not==false), and this list is not null, only these blocks are included.
        /// If this is a PasteNot brush (Not==true), this list must be non-empty/non-null, and these block types are excluded. </summary>
        [CanBeNull]
        public Block[] Blocks { get; private set; }

        /// <summary> CopyState from which blocks are pasted. </summary>
        public CopyState CopyInfo { get; private set; }

        public IBrushFactory Factory {
            get { return Not ? PasteBrushFactory.PasteNotInstance : PasteBrushFactory.PasteInstance; }
        }

        public string Description {
            get {
                if( Blocks == null ) {
                    return Factory.Name;
                } else {
                    return String.Format( "{0}({1})",
                                          Factory.Name,
                                          Blocks.JoinToString() );
                }
            }
        }


        /// <summary> Creates a new all-inclusive Paste brush. </summary>
        public PasteBrush() {}

        /// <summary> Creates a new filtering Paste or PasteNot brush. </summary>
        /// <param name="blocks"> Array of block types for filtering. May not be null or empty. </param>
        /// <param name="not"> Whether filtering should be inclusive (Paste) or exclusive (PasteNot). </param>
        /// <exception cref="ArgumentNullException"> blocks is null </exception>
        /// <exception cref="ArgumentException"> blocks is 0-length </exception>
        public PasteBrush( [NotNull] Block[] blocks, bool not ) {
            if( blocks == null ) throw new ArgumentNullException( "blocks" );
            if( blocks.Length == 0 ) {
                throw new ArgumentException( "At least one block type must be specified.", "blocks" );
            }
            Not = not;
            Blocks = blocks;
        }


        public bool Begin( Player player, DrawOperation op ) {
            CopyInfo = player.GetCopyState();
            if( CopyInfo == null ) {
                player.Message( "{0}: Nothing to paste! Copy something first.", Factory.Name );
                return false;
            }
            return true;
        }


        public Block NextBlock( DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );

            // TODO: offset op.Coords
            Vector3I pasteCoords = new Vector3I {
                X = op.Coords.X%CopyInfo.Bounds.Width,
                Y = op.Coords.Y%CopyInfo.Bounds.Length,
                Z = op.Coords.Z%CopyInfo.Bounds.Height
            };
            Block blockToPaste = CopyInfo.Blocks[pasteCoords.X, pasteCoords.Y, pasteCoords.Z];

            if( Blocks == null ) {
                return blockToPaste;
            } else if( Not ) {
                for( int i = 0; i < Blocks.Length; i++ ) {
                    if( blockToPaste == Blocks[i] ) return Block.None;
                }
                return blockToPaste;
            } else {
                for( int i = 0; i < Blocks.Length; i++ ) {
                    if( blockToPaste == Blocks[i] ) return blockToPaste;
                }
                return Block.None;
            }
        }


        public void End() {}


        public IBrush Clone() {
            return new PasteBrush {
                Blocks = Blocks,
                Not = Not,
                CopyInfo = CopyInfo
            };
        }
    }
}
