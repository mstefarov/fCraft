// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    /// <summary> Draw operation that handles aligned (two-mark) pasting for
    /// /PasteX and /PasteNotX commands. Also used internally by /Paste and /PasteNot. </summary>
    public class PasteDrawOperation : DrawOpWithBrush {
        public override string Name {
            get { return Not ? "PasteNotX" : "PasteX"; }
        }

        public override int ExpectedMarks {
            get { return 2; }
        }

        public override string Description {
            get {
                if( Blocks == null ) {
                    return Name;
                } else {
                    return String.Format( "{0}({1})",
                                          Name,
                                          Blocks.JoinToString() );
                }
            }
        }

        public bool Not { get; private set; }

        public Block[] Blocks { get; private set; }

        public Vector3I Start { get; private set; }

        public CopyState CopyInfo { get; private set; }


        public PasteDrawOperation( Player player, bool not )
            : base( player ) {
            Not = not;
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( marks == null ) throw new ArgumentNullException( "marks" );
            if( marks.Length < 2 ) throw new ArgumentException( "At least two marks needed.", "marks" );

            // Make sure that we have something to paste
            CopyInfo = Player.GetCopyState();
            if( CopyInfo == null ) {
                Player.Message( "Nothing to paste! Copy something first." );
                return false;
            }

            // Calculate the buffer orientation
            Vector3I delta = marks[1] - marks[0];
            Vector3I orientation = new Vector3I {
                X = (delta.X == 0 ? CopyInfo.Orientation.X : Math.Sign( delta.X )),
                Y = (delta.Y == 0 ? CopyInfo.Orientation.Y : Math.Sign( delta.Y )),
                Z = (delta.Z == 0 ? CopyInfo.Orientation.Z : Math.Sign( delta.Z ))
            };

            // Calculate the start/end coordinates for pasting
            marks[1] = marks[0] + new Vector3I( orientation.X*(CopyInfo.Bounds.Width - 1),
                                                orientation.Y*(CopyInfo.Bounds.Length - 1),
                                                orientation.Z*(CopyInfo.Bounds.Height - 1) );
            Bounds = new BoundingBox( marks[0], marks[1] );
            Marks = marks;

            // Warn if paste will be cut off
            if( Bounds.XMin < 0 || Bounds.XMax > Map.Width - 1 ) {
                Player.Message( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( Bounds.YMin < 0 || Bounds.YMax > Map.Length - 1 ) {
                Player.Message( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( Bounds.ZMin < 0 || Bounds.ZMax > Map.Height - 1 ) {
                Player.Message( "Warning: Not enough room vertically, paste cut off." );
            }

            // Clip bounds to the map, to avoid unnecessary iteration beyond the map boundaries
            Start = Bounds.MinVertex;
            Bounds = Bounds.GetIntersection( Map.Bounds );

            // Set everything up for pasting
            Brush = this;
            Coords = Bounds.MinVertex;

            StartTime = DateTime.UtcNow;
            Context = BlockChangeContext.Drawn | BlockChangeContext.Pasted;
            BlocksTotalEstimate = Bounds.Volume;
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            return DrawBatchWithinBounds( maxBlocksToDraw );
        }


        public override bool ReadParams( CommandReader cmd ) {
            if( Player.GetCopyState() == null ) {
                Player.Message( "Nothing to paste! Copy something first." );
                return false;
            }
            List<Block> blocks = new List<Block>();
            while( cmd.HasNext ) {
                Block block;
                if( !cmd.NextBlock( Player, false, out block ) ) return false;
                blocks.Add( block );
            }
            if( blocks.Count > 0 ) {
                Blocks = blocks.ToArray();
            } else if( Not ) {
                Player.Message( "PasteNot requires at least 1 block." );
                return false;
            }
            Brush = this;
            return true;
        }


        protected override Block NextBlock() {
            Block block = CopyInfo.Blocks[Coords.X - Start.X, Coords.Y - Start.Y, Coords.Z - Start.Z];
            if( Blocks == null ) return block;
            if( Not ) {
                for( int i = 0; i < Blocks.Length; i++ ) {
                    if( block == Blocks[i] ) return Block.None;
                }
                return block;
            } else {
                for( int i = 0; i < Blocks.Length; i++ ) {
                    if( block == Blocks[i] ) return block;
                }
                return Block.None;
            }
        }
    }
}
