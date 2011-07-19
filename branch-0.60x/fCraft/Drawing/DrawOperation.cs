// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {

    public abstract class DrawOperation {
        public readonly Player Player;
        public readonly Map Map;
        public Position[] Marks;
        public DateTime StartTime;

        public BoundingBox Bounds;

        public bool IsDone;

        public byte[] UndoBuffer;
        public IBrushInstance Brush;

        public int BlocksChecked,
                   BlocksUpdated,
                   BlocksDenied,
                   BlocksTotalEstimate;

        public bool CannotUndo;

        public Vector3I Coords;

        public bool UseAlternateBlock;



        protected DrawOperation( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.World == null || player.World.Map == null ) {
                throw new ArgumentException( "Player must have a world.", "player" );
            }

            Player = player;
            Map = player.World.Map;
        }


        public virtual void Begin() {
            if( Player == null ) throw new InvalidOperationException( "Player not set" );
            if( Map == null ) throw new InvalidOperationException( "Map not set" );
            if( Marks == null ) throw new InvalidOperationException( "Marks not set" );
            if( Bounds == null ) throw new InvalidOperationException( "Bounds not set" );
            Brush.Begin( Player, this );
            StartTime = DateTime.UtcNow;
        }


        public abstract int DrawBatch( int maxBlocksToDraw );


        public virtual void Cancel() {
            End();
        }


        public virtual void End() {
            Player.Info.ProcessDrawCommand( BlocksUpdated );
            Brush.End();
        }


        public virtual bool DrawOneBlock() {
            Block newBlock = Brush.NextBlock( this );

            if( !Map.InBounds( Coords ) ) return false;
            int blockIndex = Map.Index( Coords );

            Block oldBlock = (Block)Map.Blocks[blockIndex];
            if( oldBlock == newBlock ) return false;

            if( Player.CanPlace( Coords.X, Coords.Y, Coords.Z, newBlock, false ) != CanPlaceResult.Allowed ) {
                BlocksDenied++;
                return false;
            }

            Map.Blocks[blockIndex] = (byte)newBlock;

            World world = Map.World;
            if( world != null && !world.IsFlushing ) {
                world.Players.SendLowPriority( PacketWriter.MakeSetBlock( Coords.X, Coords.Y, Coords.Z, newBlock ) );
            }

            Server.RaisePlayerPlacedBlockEvent( Player, (short)Coords.X, (short)Coords.Y, (short)Coords.Z,
                                                oldBlock, newBlock, false );

            if( BuildingCommands.MaxUndoCount < 1 || BlocksUpdated < BuildingCommands.MaxUndoCount ) {
                Player.UndoBuffer.Enqueue( new BlockUpdate( null, Coords.X, Coords.Y, Coords.Z, oldBlock ) );
            } else if( !CannotUndo ) {
                Player.UndoBuffer.Clear();
                Player.UndoBuffer.TrimExcess();
                Player.Message( "NOTE: This draw command is too massive to undo." ); //TODO: Adjust message
                if( Player.Can( Permission.ManageWorlds ) ) {
                    Player.Message( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                }
                CannotUndo = true;
            }
            BlocksUpdated++;
            return true;
        }
    }
}