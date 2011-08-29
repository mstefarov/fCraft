// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

// ReSharper disable VirtualMemberNeverOverriden.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable MemberCanBeProtected.Global
namespace fCraft.Drawing {

    public abstract class DrawOperation {
        public readonly Player Player;
        public readonly Map Map;
        public Position[] Marks;
        public DateTime StartTime;

        public BoundingBox Bounds;

        public bool IsDone;

        public IBrushInstance Brush;

        public int BlocksProcessed,
                   BlocksUpdated,
                   BlocksDenied,
                   BlocksSkipped,
                   BlocksTotalEstimate;

        public bool CannotUndo;

        public Vector3I Coords;

        public bool UseAlternateBlock;

        public abstract string Name { get; }

        public abstract string Description { get; }


        protected DrawOperation( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.World == null || player.World.Map == null ) {
                throw new ArgumentException( "Player must have a world.", "player" );
            }

            Player = player;
            Map = player.World.Map;
        }


        public virtual bool Begin( Position[] marks ) {
            if( marks == null ) throw new ArgumentNullException( "marks" );
            Marks = marks;
            if( Player == null ) throw new InvalidOperationException( "Player not set" );
            if( Map == null ) throw new InvalidOperationException( "Map not set" );
            Bounds = new BoundingBox( Marks[0], Marks[1] );
            if( Bounds == null ) throw new InvalidOperationException( "Bounds not set" );
            if( !Brush.Begin( Player, this ) ) return false;
            Player.UndoBuffer.Clear();
            StartTime = DateTime.UtcNow;
            return true;
        }


        public abstract int DrawBatch( int maxBlocksToDraw );


        public virtual void End() {
            Player.Info.ProcessDrawCommand( BlocksUpdated );
            Brush.End();
        }


        protected bool DrawOneBlock() {
            BlocksProcessed++;

#if DEBUG
            TestForDuplicateModification();
#endif

            if( !Map.InBounds( Coords.X, Coords.Y, Coords.Z ) ) {
                BlocksSkipped++;
                return false;
            }

            Block newBlock = Brush.NextBlock( this );
            if( newBlock == Block.Undefined ) return false;

            int blockIndex = Map.Index( Coords.X, Coords.Y, Coords.Z );

            Block oldBlock = (Block)Map.Blocks[blockIndex];
            if( oldBlock == newBlock ) {
                BlocksSkipped++;
                return false;
            }

            if( Player.CanPlace( Coords.X, Coords.Y, Coords.Z, newBlock, false ) != CanPlaceResult.Allowed ) {
                BlocksDenied++;
                return false;
            }

            Map.Blocks[blockIndex] = (byte)newBlock;

            World world = Map.World;
            if( world != null && !world.IsFlushing ) {
                world.Players.SendLowPriority( PacketWriter.MakeSetBlock( Coords.X, Coords.Y, Coords.Z, newBlock ) );
            }

            Player.RaisePlayerPlacedBlockEvent( Player, Map, (short)Coords.X, (short)Coords.Y, (short)Coords.Z,
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

#if DEBUG

        // Single modification per block policy enforcement
        HashSet<int> modifiedBlockIndices = new HashSet<int>();
        void TestForDuplicateModification() {
            int index = Map.Index( Coords );
            if( modifiedBlockIndices.Contains( index ) ) {
                throw new InvalidOperationException( "Duplicate block modification." );
            }
            modifiedBlockIndices.Add( index );
        }


#endif
    }
}