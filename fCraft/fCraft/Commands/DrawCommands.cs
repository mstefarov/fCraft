using System;
using System.Collections.Generic;


namespace fCraft {

    enum DrawMode {
        Cuboid,
        CuboidHollow,
        Ellipsoid,
        Replace
    }


    class CopyInformation {
        public byte[, ,] buffer;
        public int widthX, widthY, height;
    }


    struct ReplaceArgs {
        public Block oldBlock, replacementBlock;
    }

    struct PasteArgs {
        public bool doInclude, doExclude;
        public Block type;
    }


    static class DrawCommands {

        const int MaxUndoCount = 2000000;
        const int DrawStride = 16;


        internal static void Init() {
            string generalDrawingHelp = "Type " + Color.Help + "/cancel" + Color.Sys + " to exit draw mode. " +
                                 "Type " + Color.Help + "/undo" + Color.Sys + " to undo the last draw operation." +
                                 "Use " + Color.Help + "/lock" + Color.Sys + " to cancel drawing after it started.";

            cdCuboid.help += generalDrawingHelp;
            cdCuboidHollow.help += generalDrawingHelp;
            cdEllipsoid.help += generalDrawingHelp;
            cdReplace.help += generalDrawingHelp;

            CommandList.RegisterCommand( cdCuboid );
            CommandList.RegisterCommand( cdCuboidHollow );
            CommandList.RegisterCommand( cdEllipsoid );
            CommandList.RegisterCommand( cdReplace );

            CommandList.RegisterCommand( cdMark );
            CommandList.RegisterCommand( cdCancel );
            CommandList.RegisterCommand( cdUndo );

            CommandList.RegisterCommand( cdCopy );
            CommandList.RegisterCommand( cdPaste );
            CommandList.RegisterCommand( cdPasteOnly );
        }


        static CommandDescriptor cdCuboid = new CommandDescriptor {
            name = "cuboid",
            aliases = new string[] { "c", "cub", "blb" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/cuboid [BlockName]",
            help = "Allows to fill a rectangular area (cuboid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Cuboid
        };

        internal static void Cuboid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Cuboid );
        }



        static CommandDescriptor cdCuboidHollow = new CommandDescriptor {
            name = "cuboidh",
            aliases = new string[] { "h", "cubh", "bhb" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/cuboidh [BlockName]",
            help = "Allows to box a rectangular area (cuboid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = CuboidHollow
        };

        internal static void CuboidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidHollow );
        }



        static CommandDescriptor cdEllipsoid = new CommandDescriptor {
            name = "ellipsoid",
            aliases = new string[] { "e", "ell", "spheroid" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/ellipsoid [BlockName]",
            help = "Allows to fill a sphere-like area (ellipsoid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Ellipsoid
        };

        internal static void Ellipsoid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Ellipsoid );
        }



        static CommandDescriptor cdReplace = new CommandDescriptor {
            name = "replace",
            aliases = new string[] { "r" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/replace BlockName ReplacementName",
            help = "Replaces all blocks of specified type in an area.",
            handler = Replace
        };

        internal static void Replace( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Replace );
        }



        internal static void Draw( Player player, Command cmd, DrawMode mode ) {
            if( player.drawingInProgress ) {
                player.Message( "Another draw command is already in progress. Please wait." );
                return;
            }
            string blockName = cmd.Next();
            Block block = Block.Undefined;

            Permission permission = Permission.Build;

            // if a type is specified in chat, try to parse it
            if( blockName != null ) {
                try {
                    block = Map.GetBlockByName( blockName );
                } catch( Exception ) {
                    player.Message( "Draw: Unrecognized block name: {0}", blockName );
                    return;
                }

                switch( block ) {
                    case Block.Admincrete:
                        permission = Permission.PlaceAdmincrete; break;
                    case Block.Air:
                        permission = Permission.Delete; break;
                    case Block.Water:
                    case Block.StillWater:
                        permission = Permission.PlaceWater; break;
                    case Block.Lava:
                    case Block.StillLava:
                        permission = Permission.PlaceLava; break;
                }
            }
            // otherwise, use the last-used-block

            if( !player.Can( permission ) ) {
                player.Message( "You are not allowed to draw with this block." );
                return;
            }

            player.selectionArgs = (byte)block;
            switch( mode ) {
                case DrawMode.Cuboid:
                    player.selectionCallback = DrawCuboid;
                    player.selectionMarksExpected = 2;
                    break;
                case DrawMode.CuboidHollow:
                    player.selectionCallback = DrawCuboidHollow;
                    player.selectionMarksExpected = 2;
                    break;
                case DrawMode.Ellipsoid:
                    player.selectionCallback = DrawEllipsoid;
                    player.selectionMarksExpected = 2;
                    break;
                case DrawMode.Replace:
                    Block replacementBlock;
                    if( !cmd.NextBlockType( out replacementBlock ) ) {
                        cdReplace.PrintUsage( player );
                        return;
                    } else if( replacementBlock == Block.Undefined ) {
                        player.Message( "Replace: Unrecognized block name" );
                        return;
                    }
                    player.selectionCallback = DrawReplace;
                    player.selectionMarksExpected = 2;
                    player.selectionArgs = new ReplaceArgs() {
                        oldBlock = block,
                        replacementBlock = replacementBlock
                    };
                    player.Message( "Replacing {0} with {1}", block, replacementBlock );
                    break;
            }
            player.selectionMarkCount = 0;
            player.selectionMarks.Clear();
            player.Message( "{0}: Place a block or type /mark to use your location.", mode );
        }



        static CommandDescriptor cdMark = new CommandDescriptor {
            name = "mark",
            aliases = new string[] { "m" },
            help = "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                   "You can mark in places where making blocks is difficult (e.g. mid-air).",
            handler = Mark
        };

        internal static void Mark( Player player, Command command ) {
            Position pos = new Position( (short)(player.pos.x / 32), (short)(player.pos.y / 32), (short)(player.pos.h / 32) );
            if( player.selectionMarksExpected > 0 ) {
                player.selectionMarks.Enqueue( pos );
                player.selectionMarkCount++;
                if( player.selectionMarkCount >= player.selectionMarksExpected ) {
                    player.selectionCallback( player, player.selectionMarks.ToArray(), player.selectionArgs );
                    player.selectionMarksExpected = 0;
                } else {
                    player.Message( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                                    player.selectionMarkCount,
                                    pos.x, pos.y, pos.h,
                                    player.selectionMarkCount + 1 );
                }
            } else {
                player.Message( "Cannot mark - no draw or zone commands initiated." );
            }
        }



        static CommandDescriptor cdCancel = new CommandDescriptor {
            name = "cancel",
            help = "Cancels current selection (for drawing or zoning) operation, for instance if you misclicked on the first block. " +
                   "If you wish to stop a drawing in-progress, use &H/lock&S instead.",
            handler = Cancel
        };

        internal static void Cancel( Player player, Command command ) {
            if( player.selectionMarksExpected > 0 ) {
                player.selectionMarksExpected = 0;
            } else {
                player.Message( "There is currently nothing to cancel." );
            }
        }



        static CommandDescriptor cdUndo = new CommandDescriptor {
            name = "undo",
            help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            handler = Undo
        };

        internal static void Undo( Player player, Command command ) {
            if( !player.Can( Permission.Draw ) ) {
                player.NoAccessMessage( Permission.Draw );
                return;
            }
            if( player.undoBuffer.Count > 0 ) {
                if( player.drawingInProgress ) {
                    player.Message( "Cannot undo a drawing-in-progress. Wait for it to finish." );
                } else {
                    player.world.SendToAll( Color.Sys + player.nick + " initiated /undo. " + player.undoBuffer.Count + " blocks to replace...", null );
                    while( player.undoBuffer.Count > 0 ) {
                        player.world.map.QueueUpdate( player.undoBuffer.Dequeue() );
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

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( player.CanDraw( volume ) ) {
                player.Message( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                player.info.playerClass.drawLimit,
                                volume );
                return;
            }

            player.undoBuffer.Clear();

            bool cannotUndo = false;
            int blocks = 0;
            byte block;
            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                block = player.world.map.GetBlock( x + x3, y + y3, h );
                                if( block != oldBlock ) continue;
                                if( block == (byte)Block.Admincrete && !player.Can( Permission.DeleteAdmincrete ) ) continue;
                                player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x + x3, y + y3, h, replacementBlock ) );
                                if( blocks < MaxUndoCount ) {
                                    player.undoBuffer.Enqueue( new BlockUpdate( Player.Console, x + x3, y + y3, h, oldBlock ) );
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

            player.Message( "Replacing {0} blocks... The map is now being updated.", blocks );
            Logger.Log( "{0} initiated replacing {1} {2} blocks with {3}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  (Block)oldBlock,
                                  (Block)replacementBlock );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            player.drawingInProgress = false;
        }


        internal static void DrawCuboid( Player player, Position[] marks, object tag ) {
            player.drawingInProgress = true;

            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.lastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( player.CanDraw( volume ) ) {
                player.Message( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                player.info.playerClass.drawLimit,
                                volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0;
            bool cannotUndo = false;

            byte block;
            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                block = player.world.map.GetBlock( x + x3, y + y3, h );
                                if( block == (byte)drawBlock ) continue;
                                if( block == (byte)Block.Admincrete && !player.Can( Permission.DeleteAdmincrete ) ) continue;

                                player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x + x3, y + y3, h, (byte)drawBlock ) );
                                if( blocks < MaxUndoCount ) {
                                    player.undoBuffer.Enqueue( new BlockUpdate( Player.Console, x + x3, y + y3, h, block ) );
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
            player.Message( "Drawing {0} blocks... The map is now being updated.", blocks );
            Logger.Log( "{0} initiated drawing a cuboid containing {1} blocks of type {2}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  drawBlock.ToString() );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            player.drawingInProgress = false;
        }


        internal static void DrawCuboidHollow( Player player, Position[] marks, object tag ) {
            player.drawingInProgress = true;

            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.lastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1) - (ex - sx - 1) * (ey - sy - 1) * (eh - sh - 1);
            if( player.CanDraw( volume ) ) {
                player.Message( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                player.info.playerClass.drawLimit,
                                volume );
                return;
            }

            player.undoBuffer.Clear();

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

            player.Message( "Drawing {0} blocks... The map is now being updated.", blocks );
            Logger.Log( "{0} initiated drawing a hollow cuboid containing {1} blocks of type {2}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  ((Block)drawBlock).ToString() );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            player.drawingInProgress = false;
        }


        internal static void DrawEllipsoid( Player player, Position[] marks, object tag ) {
            player.drawingInProgress = true;

            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.lastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

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


            int volume = (int)((3 / 4d) * Math.PI * rx * ry * rh);
            if( player.CanDraw( volume ) ) {
                player.Message( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                 player.info.playerClass.drawLimit,
                                 volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0;
            bool cannotUndo = false;

            byte block;
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
                                    if( block == (byte)Block.Admincrete && !player.Can( Permission.DeleteAdmincrete ) ) continue;

                                    player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x + x3, y + y3, h, (byte)drawBlock ) );
                                    if( blocks < MaxUndoCount ) {
                                        player.undoBuffer.Enqueue( new BlockUpdate( Player.Console, x + x3, y + y3, h, block ) );
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
            player.Message( "Drawing {0} blocks... The map is now being updated.", blocks );
            Logger.Log( "{0} initiated drawing an ellipsoid containing {1} blocks of type {2}.", LogType.UserActivity,
                                  player.GetLogName(),
                                  blocks,
                                  drawBlock.ToString() );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        static void DrawOneBlock( Player player, byte drawBlock, int x, int y, int h, ref int blocks, ref bool cannotUndo ) {
            byte block = player.world.map.GetBlock( x, y, h );
            if( block == drawBlock || block == (byte)Block.Admincrete && !player.Can( Permission.DeleteAdmincrete ) ) return;

            player.world.map.QueueUpdate( new BlockUpdate( Player.Console, x, y, h, drawBlock ) );
            if( blocks < MaxUndoCount ) {
                player.undoBuffer.Enqueue( new BlockUpdate( Player.Console, x, y, h, block ) );
            } else if( !cannotUndo ) {
                player.Message( "NOTE: This draw command is too massive to undo." );
                cannotUndo = true;
            }
            blocks++;
        }


        #region Copy and Paste

        static CommandDescriptor cdCopy = new CommandDescriptor {
            name = "copy",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Copy blocks for pasting. Used together with &H/paste&S command. Note that pasting starts at the same corner that you started &H/copy&S from.",
            handler = Copy
        };

        internal static void Copy( Player player, Command cmd ) {
            player.selectionCallback = DoCopy;
            player.selectionMarksExpected = 2;
            player.selectionMarkCount = 0;
            player.selectionMarks.Clear();
            player.Message( "Copy: Place a block or type /mark to use your location." );
        }

        internal static void DoCopy( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( player.CanDraw( volume ) ) {
                player.Message( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.info.playerClass.drawLimit, volume ) );
                return;
            }

            CopyInformation copyInfo = new CopyInformation();

            // remember dimensions and orientation
            copyInfo.widthX = marks[1].x - marks[0].x;
            copyInfo.widthY = marks[1].y - marks[0].y;
            copyInfo.height = marks[1].h - marks[0].h;

            copyInfo.buffer = new byte[ex - sx + 1, ey - sy + 1, eh - sh + 1];

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        copyInfo.buffer[x - sx, y - sy, h - sh] = player.world.map.GetBlock( x, y, h );
                    }
                }
            }

            player.copyInformation = copyInfo;
            player.Message( "{0} blocks were copied. You can now &H/paste", volume );
            player.Message( "Origin at {0} {1}{2} corner.",
                            (copyInfo.height > 0 ? "bottom " : "top "),
                            (copyInfo.widthY > 0 ? "south" : "north"),
                            (copyInfo.widthX > 0 ? "west" : "east") );

            Logger.Log( "{0} copied {1} blocks.", LogType.UserActivity,
                        player.GetLogName(),
                        volume );
        }


        static CommandDescriptor cdPaste = new CommandDescriptor {
            name = "paste",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Paste previously copied blocks. Used together with &H/copy&S command. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from. " +
                   "If the optional parameter is given, blocks of specified type are excluded while pasting.",
            usage = "/paste [ExcludedBlockType]",
            handler = Paste
        };

        internal static void Paste( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.Message( "Nothing to paste! Copy something first." );
                return;
            }

            Block excludedType;
            if( !cmd.NextBlockType( out excludedType ) ) {
                player.selectionArgs = new PasteArgs();
            } else if( excludedType == Block.Undefined ) {
                player.Message( "Paste: Unrecognized block type." );
            } else {
                player.selectionArgs = new PasteArgs {
                    doExclude = true,
                    type = excludedType
                };
                player.Message( "Ready to paste all EXCEPT {0}", excludedType );
            }

            player.selectionCallback = DoPaste;
            player.selectionMarksExpected = 1;
            player.selectionMarkCount = 0;
            player.selectionMarks.Clear();

            player.Message( "Paste: Place a block or type /mark to use your location. " );
        }

        static CommandDescriptor cdPasteOnly = new CommandDescriptor {
            name = "pasteonly",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Paste previously copied blocks ONLY of specified blocktype. Used together with &H/copy&S command. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            usage = "/pasteonly IncludedBlockType",
            handler = PasteOnly
        };

        internal static void PasteOnly( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.Message( "Nothing to paste! Copy something first." );
                return;
            }

            Block includedType;
            if( !cmd.NextBlockType( out includedType ) ) {
                cdPasteOnly.PrintUsage( player );
                return;
            } else if( includedType == Block.Undefined ) {
                player.Message( "PasteOnly: Unrecognized block type." );
                return;
            }

            player.selectionCallback = DoPaste;
            player.selectionMarksExpected = 1;
            player.selectionMarkCount = 0;
            player.selectionMarks.Clear();

            player.selectionArgs = new PasteArgs {
                doInclude = true,
                type = includedType
            };

            player.Message( "Ready to paste ONLY {0}", includedType );
            player.Message( "Paste: Place a block or type /mark to use your location. " );
        }

        internal static void DoPaste( Player player, Position[] marks, object tag ) {
            if( player.drawingInProgress ) {
                player.Message( "Another draw command is already in progress. Please wait." );
                return;
            }
            player.drawingInProgress = true;
            CopyInformation info = player.copyInformation;

            PasteArgs args = (PasteArgs)tag;
            byte specialType = (byte)args.type;
            Map map = player.world.map;

            int sx = Math.Min( marks[0].x, marks[0].x + info.widthX );
            int ex = Math.Max( marks[0].x, marks[0].x + info.widthX );
            int sy = Math.Min( marks[0].y, marks[0].y + info.widthY );
            int ey = Math.Max( marks[0].y, marks[0].y + info.widthY );
            int sh = Math.Min( marks[0].h, marks[0].h + info.height );
            int eh = Math.Max( marks[0].h, marks[0].h + info.height );

            if( sx < 0 || ex > map.widthX - 1 ) {
                player.Message( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( sy < 0 || ey > map.widthY - 1 ) {
                player.Message( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( sh < 0 || eh > map.height - 1 ) {
                player.Message( "Warning: Not enough room vertically, paste cut off." );
            }

            int blocks = 0;
            bool cannotUndo = false;

            byte block;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        block = info.buffer[x - sx, y - sy, h - sh];
                        if( !(args.doExclude && block == specialType) &&
                            !(args.doInclude && block != specialType) ) {
                            DrawOneBlock( player, block, x, y, h, ref blocks, ref cannotUndo );
                        }
                    }
                }
            }

            player.Message( "{0} blocks pasted. The map is now being updated...", blocks );
            player.drawingInProgress = false;

            Logger.Log( "{0} pasted {1} blocks.", LogType.UserActivity,
                        player.GetLogName(),
                        blocks );
        }

        #endregion
    }
}