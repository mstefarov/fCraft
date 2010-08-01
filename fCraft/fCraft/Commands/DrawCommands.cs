using System;
using System.Collections.Generic;


namespace fCraft {

    enum DrawMode {
        Cuboid,
        CuboidHollow,
        Ellipsoid,
        Fill,
        Replace
    }

    static class DrawCommands {

        const int MaxUndoCount = 2000000;
        const int DrawStride = 16;

        public class ReplaceArgs {
            public Block oldBlock, replacementBlock;
        }

        internal static void Init() {
            Commands.AddCommand( "cuboid", Cuboid, false );
            Commands.AddCommand( "cub", Cuboid, false );
            Commands.AddCommand( "blb", Cuboid, false );

            Commands.AddCommand( "cuboidh", CuboidHollow, false );
            Commands.AddCommand( "cubh", CuboidHollow, false );
            Commands.AddCommand( "bhb", CuboidHollow, false );

            Commands.AddCommand( "replace", Replace, false );

            Commands.AddCommand( "ellipsoid", Ellipsoid, false );
            Commands.AddCommand( "ell", Ellipsoid, false );

            Commands.AddCommand( "mark", Mark, false );
            Commands.AddCommand( "undo", UndoDraw, false );
            Commands.AddCommand( "cancel", CancelDraw, false );
        }


        internal static void Cuboid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Cuboid );
        }

        internal static void Replace( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Replace );
        }

        internal static void CuboidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidHollow );
        }

        internal static void Ellipsoid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Ellipsoid );
        }

        internal static void Fill( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Fill );
        }


        internal static void Draw( Player player, Command cmd, DrawMode mode ) {
            if( !player.Can( Permissions.Draw ) ) {
                player.NoAccessMessage( Permissions.Draw );
                return;
            }
            if( player.drawingInProgress ) {
                player.Message( "Another draw command is already in progress. Please wait." );
                return;
            }
            string blockName = cmd.Next();
            Block block = Block.Undefined;

            Permissions permission = Permissions.Build;

            // if a type is specified in chat, try to parse it
            if( blockName != null ) {
                try {
                    block = Map.GetBlockByName( blockName );
                } catch( Exception ) {
                    player.Message( "Unknown block name: " + blockName );
                    return;
                }

                switch( block ) {
                    case Block.Admincrete: permission = Permissions.PlaceAdmincrete; break;
                    case Block.Air: permission = Permissions.Delete; break;
                    case Block.Water:
                    case Block.StillWater: permission = Permissions.PlaceWater; break;
                    case Block.Lava:
                    case Block.StillLava: permission = Permissions.PlaceLava; break;
                }
            }
            // otherwise, use the last-used-block

            if( !player.Can( permission ) ) {
                player.Message( "You are not allowed to draw with this block." );
                return;
            }

            player.drawArgs = (object)block;
            switch( mode ) {
                case DrawMode.Cuboid:
                    player.selectionCallback = DrawCuboid;
                    player.marksExpected = 2;
                    break;
                case DrawMode.CuboidHollow:
                    player.selectionCallback = DrawCuboidHollow;
                    player.marksExpected = 2;
                    break;
                case DrawMode.Ellipsoid:
                    player.selectionCallback = DrawEllipsoid;
                    player.marksExpected = 2;
                    break;
                case DrawMode.Fill:
                    player.selectionCallback = DoFill;
                    player.marksExpected = 1;
                    break;
                case DrawMode.Replace:
                    string replacementBlockName = cmd.Next();
                    if( replacementBlockName == null ) {
                        player.Message( "See " + Color.Help + "/help replace" + Color.Sys + " for usage information." );
                        return;
                    }
                    Block replacementBlock;
                    try {
                        replacementBlock = Map.GetBlockByName( replacementBlockName );
                    } catch( Exception ) {
                        player.Message( "Unknown block name: " + replacementBlockName );
                        return;
                    }
                    player.selectionCallback = DrawReplace;
                    player.marksExpected = 2;
                    player.drawArgs = new ReplaceArgs() {
                        oldBlock = block,
                        replacementBlock = replacementBlock
                    };
                    player.Message( "Replacing " + block + " with " + replacementBlock );
                    break;
            }
            player.drawMarkCount = 0;
            player.drawMarks.Clear();
            player.Message( mode.ToString() + ": Place a block or type /mark to use your location." );
        }


        internal static void Mark( Player player, Command command ) {
            Position pos = new Position( (short)(player.pos.x / 32), (short)(player.pos.y / 32), (short)(player.pos.h / 32) );
            if( player.marksExpected > 0 ) {
                player.drawMarks.Push( pos );
                player.drawMarkCount++;
                if( player.drawMarkCount >= player.marksExpected ) {
                    player.selectionCallback( player, player.drawMarks.ToArray(), player.drawArgs );
                    player.marksExpected = 0;
                } else {
                    player.Message( String.Format( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                                                   player.drawMarkCount, pos.x, pos.y, pos.h, player.drawMarkCount + 1 ) );
                }
            } else {
                player.Message( "Cannot mark - no draw or zone commands initiated." );
            }
        }


        internal static void CancelDraw( Player player, Command command ) {
            if( player.marksExpected > 0 ) {
                player.marksExpected = 0;
            } else {
                player.Message( "There is currently nothing to cancel." );
            }
        }


        internal static void UndoDraw( Player player, Command command ) {
            if( !player.Can( Permissions.Draw ) ) {
                player.NoAccessMessage( Permissions.Draw );
                return;
            }
            if( player.drawUndoBuffer.Count > 0 ) {
                if( player.drawingInProgress ) {
                    player.Message( "Cannot undo a drawing-in-progress. Wait for it to finish." );
                } else {
                    player.world.SendToAll( Color.Sys + player.nick + " initiated /undo. " + player.drawUndoBuffer.Count + " blocks to replace...", null );
                    while( player.drawUndoBuffer.Count > 0 ) {
                        player.world.map.QueueUpdate( player.drawUndoBuffer.Dequeue() );
                    }
                }
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            } else {
                player.Message( "There is currently nothing to undo." );
            }
        }


        internal static void DrawReplace( Player player, Position[] marks, object drawArgs ) {
            player.drawingInProgress = true;

            byte oldBlock = (byte)((ReplaceArgs)drawArgs).oldBlock,
                 replacementBlock = (byte)((ReplaceArgs)drawArgs).replacementBlock;

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            byte block;

            bool cannotUndo = false;
            int blocks = 0;
            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                block = player.world.map.GetBlock( x + x3, y + y3, h );
                                if( block != oldBlock ) continue;
                                if( block == (byte)Block.Admincrete && !player.Can( Permissions.DeleteAdmincrete ) ) continue;
                                player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x + x3, y + y3, h, replacementBlock ) );
                                if( blocks < MaxUndoCount ) {
                                    player.drawUndoBuffer.Enqueue( new BlockUpdate( Player.Console, x + x3, y + y3, h, oldBlock ) );
                                }else if( !cannotUndo ){
                                    player.Message( "NOTE: This draw command is too massive to undo." );
                                    cannotUndo = true;
                                }
                                blocks++;
                            }
                        }
                    }
                }
            }

            player.Message( "Replacing " + blocks + " blocks... The map is now being updated." );
            Logger.Log( "{0} initiated drawing a cuboid containing {1} blocks of type {2}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  oldBlock.ToString() );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            player.drawingInProgress = false;
        }


        internal static void DrawCuboid( Player player, Position[] marks, object tag ) {
            player.drawingInProgress = true;

            Block drawBlock;
            if( tag == null ) {
                drawBlock = player.lastUsedBlockType;
            } else {
                drawBlock = (Block)tag;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            byte block;

            int blocks = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                block = player.world.map.GetBlock( x + x3, y + y3, h );
                                if( block == (byte)drawBlock ) continue;
                                if( block == (byte)Block.Admincrete && !player.Can( Permissions.DeleteAdmincrete ) ) continue;
                                
                                player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x + x3, y + y3, h, (byte)drawBlock ) );
                                if( blocks < MaxUndoCount ) {
                                    player.drawUndoBuffer.Enqueue( new BlockUpdate( Player.Console, x + x3, y + y3, h, block ) );
                                } else if( !cannotUndo ) {
                                    player.Message( "NOTE: This draw command is too massive to undo." );
                                    cannotUndo = true;
                                }
                                blocks++;
                            }
                        }
                    }
                }
            }
            player.Message( "Drawing " + blocks + " blocks... The map is now being updated." );
            Logger.Log( "{0} initiated drawing a cuboid containing {1} blocks of type {2}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  drawBlock.ToString() );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            player.drawingInProgress = false;
        }



        internal static void DrawCuboidHollow( Player player, Position[] marks, object tag ) {
            player.drawingInProgress = true;

            byte drawBlock;
            if( tag == null ) {
                drawBlock = (byte)player.lastUsedBlockType;
            } else {
                drawBlock = (byte)tag;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int blocks = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    DrawOneBlock( player, drawBlock, x, y, sh, ref blocks, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, y, eh, ref blocks, ref cannotUndo );
                }
            }
            for( int x = sx; x <= ex; x++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, x, sy, h, ref blocks, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, ey, h, ref blocks, ref cannotUndo );
                }
            }
            for( int y = sy; y <= ey; y++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, sx, y, h, ref blocks, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, ex, y, h, ref blocks, ref cannotUndo );
                }
            }

            player.Message( "Drawing " + blocks + " blocks... The map is now being updated." );
            Logger.Log( "{0} initiated drawing a hollow cuboid containing {1} blocks of type {2}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  ((Block)drawBlock).ToString() );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            player.drawingInProgress = false;
        }


        static void DrawOneBlock( Player player, byte drawBlock, int x, int y, int h, ref int blocks, ref bool cannotUndo ) {
            byte block = player.world.map.GetBlock( x, y, h );
            if( block == drawBlock || block == (byte)Block.Admincrete && !player.Can( Permissions.DeleteAdmincrete ) ) return;

            player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x, y, h, drawBlock ) );
            if( blocks < MaxUndoCount ) {
                player.drawUndoBuffer.Enqueue( new BlockUpdate( Player.Console, x, y, h, block ) );
            } else if( !cannotUndo ) {
                player.Message( "NOTE: This draw command is too massive to undo." );
                cannotUndo = true;
            }
            blocks++;
        }


        internal static void DrawEllipsoid( Player player, Position[] marks, object tag ) {
            player.drawingInProgress = true;

            Block drawBlock;
            if( tag == null ) {
                drawBlock = player.lastUsedBlockType;
            } else {
                drawBlock = (Block)tag;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            byte block;

            // find axis lengths
            double rx = (ex - sx + 1) / 2 + .25;
            double ry = (ey - sy + 1) / 2 + .25;
            double rh = (eh - sh + 1) / 2 + .25;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rh2 = 1 / (rh * rh);

            // find center points
            double cx = (ex + sx) / 2;
            double cy = (ey + sy) / 2;
            double ch = (eh + sh) / 2;

            // prepare to draw
            player.drawUndoBuffer.Clear();

            int blocks = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                // get relative coordinates
                                double dx = (x + x3 - cx);
                                double dy = (y + y3 - cy);
                                double dh = (h - ch);

                                // test if it's inside ellipse
                                if( (dx * dx) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 <= 1 ) {
                                    block = player.world.map.GetBlock( x + x3, y + y3, h );
                                    if( block == (byte)drawBlock ) continue;
                                    if( block == (byte)Block.Admincrete && !player.Can( Permissions.DeleteAdmincrete ) ) continue;

                                    player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x + x3, y + y3, h, (byte)drawBlock ) );
                                    if( blocks < MaxUndoCount ) {
                                        player.drawUndoBuffer.Enqueue( new BlockUpdate( Player.Console, x + x3, y + y3, h, block ) );
                                    } else if( !cannotUndo ) {
                                        player.Message( "Warning: This draw command is too massive to undo." );
                                        cannotUndo = true;
                                    }
                                    blocks++;
                                }
                            }
                        }
                    }
                }
            }
            player.drawingInProgress = false;
            player.Message( "Drawing " + blocks + " blocks... The map is now being updated." );
            Logger.Log( "{0} initiated drawing a cuboid containing {1} blocks of type {2}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  drawBlock.ToString() );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        internal static void DoFill( Player player, Position[] marks, object tag ) {
            player.drawingInProgress = true;
            player.drawingInProgress = false;
        }
    }
}