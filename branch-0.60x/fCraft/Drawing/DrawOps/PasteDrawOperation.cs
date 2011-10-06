// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class PasteDrawOperation : DrawOperation, IBrushFactory, IBrush, IBrushInstance {
        const BlockChangeContext PasteContext = BlockChangeContext.Drawn | BlockChangeContext.Pasted;

        public override string Name {
            get { return "Paste"; }
        }

        public override string DescriptionWithBrush {
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

        public Block[] Blocks { get; private set; }

        public CopyInformation CopyInfo { get; private set; }


        public PasteDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Begin( Vector3I[] marks ) {
            if( marks == null ) throw new ArgumentNullException( "marks" );
            if( marks.Length < 2 ) throw new ArgumentException( "At least two marks needed.", "marks" );

            // Make sure that we have something to paste
            CopyInfo = Player.GetCopyInformation();
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
            marks[1] = marks[0] + new Vector3I( orientation.X * CopyInfo.Dimensions.X,
                                                orientation.Y * CopyInfo.Dimensions.Y,
                                                orientation.Z * CopyInfo.Dimensions.Z );
            Bounds = new BoundingBox( marks[0], marks[1] );
            Marks = marks;

            // Warn if paste will be cut off
            if( Bounds.XMin < 0 || Bounds.XMax > Map.Width - 1 ) {
                Player.MessageNow( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( Bounds.YMin < 0 || Bounds.YMax > Map.Length - 1 ) {
                Player.MessageNow( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( Bounds.ZMin < 0 || Bounds.ZMax > Map.Height - 1 ) {
                Player.MessageNow( "Warning: Not enough room vertically, paste cut off." );
            }

            // Clip bounds to the map, to aboid unnecessary iteration beyond the map boundaries
            Bounds = Bounds.GetIntersection( Map.Bounds );

            // Set everything up for pasting
            Brush = this;
            Coords = Bounds.MinVertex;
            Player.LastDrawOp = this;
            Player.UndoBuffer.Clear();
            StartTime = DateTime.UtcNow;
            Context = PasteContext;
            BlocksTotalEstimate = Bounds.Volume;
            return true;
        }

        
        public override int DrawBatch( int maxBlocksToDraw ) {
            // basically same as CuboidDrawOp
            StartBatch();
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( !DrawOneBlock() ) continue;
                        blocksDone++;
                        if( blocksDone >= maxBlocksToDraw ) {
                            Coords.Z++;
                            return blocksDone;
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        public bool ReadParams( Command cmd ) {
            if( Player.GetCopyInformation() == null ) {
                Player.Message( "Nothing to paste! Copy something first." );
                return false;
            }
            List<Block> blocks = new List<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( Player );
                if( block == Block.Undefined ) return false;
                blocks.Add( block );
            }

            if( blocks.Count > 0 ) {
                Blocks = blocks.ToArray();
            }
            return true;
        }


        Block IBrushInstance.NextBlock( DrawOperation op ) {
            Block block = (Block)CopyInfo.Buffer[Coords.X - Bounds.XMin, Coords.Y - Bounds.YMin, Coords.Z - Bounds.ZMin];
            if( Blocks != null ) {
                // ReSharper disable LoopCanBeConvertedToQuery
                for( int i = 0; i < Blocks.Length; i++ ) {
                    // ReSharper restore LoopCanBeConvertedToQuery
                    if( block == Blocks[i] ) return block;
                }
                return Block.Undefined;
            } else {
                return block;
            }
        }


        #region IBrushFactory Members

        string IBrushFactory.Name {
            get { return Name; }
        }

        string IBrushFactory.Help {
            get { throw new NotImplementedException(); }
        }

        string[] IBrushFactory.Aliases {
            get { throw new NotImplementedException(); }
        }

        IBrush IBrushFactory.MakeBrush( Player player, Command cmd ) {
            return this;
        }

        #endregion

        #region IBrush Members

        IBrushFactory IBrush.Factory {
            get { return this; }
        }

        string IBrush.Description {
            get { throw new NotImplementedException(); }
        }

        IBrushInstance IBrush.MakeInstance( Player player, Command cmd, DrawOperation op ) {
            if( ReadParams( cmd ) ) {
                return this;
            } else {
                return null;
            }
        }

        #endregion

        #region IBrushInstance Members

        IBrush IBrushInstance.Brush {
            get { return this; }
        }

        string IBrushInstance.InstanceDescription {
            get { return DescriptionWithBrush; }
        }

        bool IBrushInstance.HasAlternateBlock {
            get { return false; }
        }

        bool IBrushInstance.Begin( Player player, DrawOperation op ) {
            return true;
        }

        void IBrushInstance.End() { }

        #endregion
    }
}