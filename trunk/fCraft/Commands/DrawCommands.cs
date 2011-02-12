// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// With contributions by Sean "SystemX17" Dolan (/line, /sphere)
// With contributions by Conrad "Redshift" Morgan (/ellipsoidhollow)
using System;
using System.Collections.Generic;

namespace fCraft {
    /// <summary>
    /// Commands for drawing commands - cuboid, ellipsoid, etc. Also copy/paste commands.
    /// </summary>
    static class DrawCommands {

        #region State Objects and Enums

        public enum DrawMode {
            Cuboid,
            CuboidHollow,
            CuboidWireframe,
            Ellipsoid,
            EllipsoidHollow,
            Sphere,
            SphereHollow,
            Replace,
            ReplaceNot,
            Line
        }


        public class CopyInformation {
            public byte[, ,] buffer;
            public int widthX, widthY, height;
        }


        struct ReplaceArgs {
            public bool doExclude;
            public Block[] types;
            public Block replacementBlock;
        }


        struct PasteArgs {
            public bool doInclude, doExclude;
            public Block[] types;
        }


        struct CuboidHollowArgs {
            public Block innerBlock;
            public Block outerBlock;
        }

        #endregion

        public static int MaxUndoCount = 2000000;
        const int DrawStride = 16;


        const string generalDrawingHelp = " Use &H/cancel&S to exit draw mode. " +
                                          "Use &H/undo&S to undo the last draw operation. " +
                                          "Use &H/lock&S to cancel drawing after it started.";

        internal static void Init() {
            cdCuboid.help += generalDrawingHelp;
            cdCuboidHollow.help += generalDrawingHelp;
            cdCuboidWireframe.help += generalDrawingHelp;
            cdEllipsoid.help += generalDrawingHelp;
            cdEllipsoidHollow.help += generalDrawingHelp;
            cdSphere.help += generalDrawingHelp;
            cdSphereHollow.help += generalDrawingHelp;
            cdLine.help += generalDrawingHelp;
            cdReplace.help += generalDrawingHelp;
            cdReplaceNot.help += generalDrawingHelp;
            cdCut.help += generalDrawingHelp;
            cdPasteNot.help += generalDrawingHelp;
            cdPaste.help += generalDrawingHelp;

            CommandList.RegisterCommand( cdCuboid );
            CommandList.RegisterCommand( cdCuboidHollow );
            CommandList.RegisterCommand( cdCuboidWireframe );
            CommandList.RegisterCommand( cdEllipsoid );
            CommandList.RegisterCommand( cdEllipsoidHollow );
            CommandList.RegisterCommand( cdSphere );
            CommandList.RegisterCommand( cdSphereHollow );
            CommandList.RegisterCommand( cdReplace );
            CommandList.RegisterCommand( cdReplaceNot );
            CommandList.RegisterCommand( cdLine );

            CommandList.RegisterCommand( cdMark );
            CommandList.RegisterCommand( cdCancel );
            CommandList.RegisterCommand( cdUndo );

            CommandList.RegisterCommand( cdCopy );
            CommandList.RegisterCommand( cdCut );
            CommandList.RegisterCommand( cdPasteNot );
            CommandList.RegisterCommand( cdPaste );
            CommandList.RegisterCommand( cdMirror );
            CommandList.RegisterCommand( cdRotate );

        }


        #region Command Descriptors

        static CommandDescriptor cdCuboid = new CommandDescriptor {
            name = "cuboid",
            aliases = new[] { "blb", "c", "cub", "z" },
            permissions = new[] { Permission.Draw },
            usage = "/cuboid [BlockName]",
            help = "Allows to fill a rectangular area (cuboid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Cuboid
        };

        internal static void Cuboid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Cuboid );
        }



        static CommandDescriptor cdCuboidHollow = new CommandDescriptor {
            name = "cubh",
            aliases = new[] { "cuboidh", "ch", "h", "bhb" },
            permissions = new[] { Permission.Draw },
            usage = "/cuboidh [OuterBlockName [InnerBlockName]]",
            help = "Allows to box a rectangular area (cuboid) with blocks. " +
                   "If OuterBlockName is omitted, uses the block that player is holding. " +
                   "Unless InnerBlockName is specified, the inside is left untouched.",
            handler = CuboidHollow
        };

        internal static void CuboidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidHollow );
        }



        static CommandDescriptor cdCuboidWireframe = new CommandDescriptor {
            name = "cubw",
            aliases = new[] { "cuboidw", "cw", "bfb" },
            permissions = new[] { Permission.Draw },
            usage = "/cuboidw [BlockName]",
            help = "Draws a wireframe box around selected area. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = CuboidWireframe
        };

        internal static void CuboidWireframe( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidWireframe );
        }



        static CommandDescriptor cdEllipsoid = new CommandDescriptor {
            name = "ellipsoid",
            aliases = new[] { "e" },
            permissions = new[] { Permission.Draw },
            usage = "/ellipsoid [BlockName]",
            help = "Fills a sphere-like (ellipsoidal) area with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Ellipsoid
        };

        internal static void Ellipsoid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Ellipsoid );
        }


        static CommandDescriptor cdEllipsoidHollow = new CommandDescriptor {
            name = "ellipsoidh",
            aliases = new[] { "eh" },
            permissions = new[] { Permission.Draw },
            usage = "/ellipsoidh [BlockName]",
            help = "Allows to fill a sphere-like (ellipsoidal) area with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = EllipsoidHollow
        };

        internal static void EllipsoidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.EllipsoidHollow );
        }


        static CommandDescriptor cdSphere = new CommandDescriptor {
            name = "sphere",
            aliases = new[] { "sp", "spheroid" },
            permissions = new[] { Permission.Draw },
            usage = "/sphere [BlockName]",
            help = "Fills a spherical area with blocks. " +
                   "First mark is the center of the sphere, second mark defines the radius." +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Sphere
        };

        internal static void Sphere( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Sphere );
        }


        static CommandDescriptor cdSphereHollow = new CommandDescriptor {
            name = "sphereh",
            aliases = new[] { "sph" },
            permissions = new[] { Permission.Draw },
            usage = "/sphereh [BlockName]",
            help = "Surrounds a spherical area with a shell of blocks. " +
                   "First mark is the center of the sphere, second mark defines the radius." +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = SphereHollow
        };

        internal static void SphereHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.SphereHollow );
        }


        static CommandDescriptor cdReplace = new CommandDescriptor {
            name = "replace",
            aliases = new[] { "r" },
            permissions = new[] { Permission.Draw },
            usage = "/replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            help = "Replaces all blocks of specified type(s) in an area.",
            handler = Replace
        };

        internal static void Replace( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Replace );
        }



        static CommandDescriptor cdReplaceNot = new CommandDescriptor {
            name = "replacenot",
            aliases = new[] { "rn" },
            permissions = new[] { Permission.Draw },
            usage = "/replacenot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            handler = ReplaceNot
        };

        internal static void ReplaceNot( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.ReplaceNot );
        }



        static CommandDescriptor cdLine = new CommandDescriptor {
            name = "line",
            aliases = new[] { "ln" },
            permissions = new[] { Permission.Draw },
            usage = "/line [BlockName]",
            help = "Draws a line between two points with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Line
        };

        internal static void Line( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Line );
        }

        #endregion


        internal static void Draw( Player player, Command cmd, DrawMode mode ) {
            string blockName = cmd.Next();
            Block block = Block.Undefined;

            Permission permission = Permission.Build;

            // if a type is specified in chat, try to parse it
            if( blockName != null ) {
                block = Map.GetBlockByName( blockName );

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
                    case Block.Undefined:
                        player.MessageNow( "{0}: Unrecognized block: {1}",
                                           mode, blockName );
                        return;
                }
            } // else { use the last-used-block }

            // ReplaceNot does not need permission (since the block is EXCLUDED)
            if( !player.Can( permission ) && mode != DrawMode.ReplaceNot ) {
                player.MessageNow( "{0}: You are not allowed to draw with this block ({1})",
                                   mode, blockName );
                return;
            }

            player.selectionArgs = (byte)block;
            switch( mode ) {
                case DrawMode.Cuboid:
                    player.selectionCallback = CuboidCallback;
                    break;

                case DrawMode.CuboidHollow:
                    player.selectionCallback = CuboidHollowCallback;
                    string innerBlockName = cmd.Next();
                    Block innerBlock = Block.Undefined;
                    if( innerBlockName != null ) {
                        innerBlock = Map.GetBlockByName( innerBlockName );
                        if( innerBlock == Block.Undefined ) {
                            player.Message( "{0}: Unrecognized block: {1}",
                                            mode, innerBlockName );
                        }
                    }
                    player.selectionArgs = new CuboidHollowArgs {
                        outerBlock = block,
                        innerBlock = innerBlock
                    };
                    break;

                case DrawMode.CuboidWireframe:
                    player.selectionCallback = CuboidWireframeCallback;
                    break;

                case DrawMode.Ellipsoid:
                    player.selectionCallback = EllipsoidCallback;
                    break;

                case DrawMode.EllipsoidHollow:
                    player.selectionCallback = EllipsoidHollowCallback;
                    break;

                case DrawMode.Sphere:
                    player.selectionCallback = SphereCallback;
                    break;

                case DrawMode.SphereHollow:
                    player.selectionCallback = SphereHollowCallback;
                    break;

                case DrawMode.Replace:
                case DrawMode.ReplaceNot:
                    List<Block> affectedTypes = new List<Block> { block };
                    Block affectedType;
                    while( cmd.NextBlockType( out affectedType ) ) {
                        if( affectedType != Block.Undefined ) {
                            affectedTypes.Add( affectedType );
                        } else {
                            player.MessageNow( "{0}: Unrecognized block type.", mode );
                            return;
                        }
                    }

                    if( affectedTypes.Count > 1 ) {
                        Block replacementType = affectedTypes[affectedTypes.Count - 1];
                        affectedTypes.RemoveAt( affectedTypes.Count - 1 );
                        player.selectionArgs = new ReplaceArgs {
                            doExclude = (mode == DrawMode.ReplaceNot),
                            types = affectedTypes.ToArray(),
                            replacementBlock = replacementType
                        };
                        string affectedString = "";
                        foreach( Block affectedBlock in affectedTypes ) {
                            affectedString += ", " + affectedBlock;
                        }
                        if( mode == DrawMode.ReplaceNot ) {
                            player.MessageNow( "ReplaceNot: Ready to replace everything EXCEPT ({0}) with {1}", affectedString.Substring( 2 ), replacementType );
                        } else {
                            player.MessageNow( "Replace: Ready to replace ({0}) with {1}", affectedString.Substring( 2 ), replacementType );
                        }
                        player.selectionCallback = ReplaceCallback;
                    } else {
                        if( mode == DrawMode.ReplaceNot ) {
                            cdReplaceNot.PrintUsage( player );
                        } else {
                            cdReplace.PrintUsage( player );
                        }
                        return;
                    }
                    break;

                case DrawMode.Line:
                    player.selectionCallback = LineCallback;
                    break;
            }

            player.selectionMarksExpected = 2;
            player.selectionMarkCount = 0;
            player.selectionMarks.Clear();
            if( block != Block.Undefined ) {
                player.MessageNow( "{0} ({1}): Click a block or use &H/mark",
                                   mode, block );
            } else {
                player.MessageNow( "{0}: Click a block or use &H/mark",
                   mode, block );
            }
        }


        #region Undo / Redo

        static CommandDescriptor cdUndo = new CommandDescriptor {
            name = "undo",
            permissions = new[] { Permission.Draw },
            aliases = new[] { "redo" },
            help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            handler = Undo
        };

        internal static void Undo( Player player, Command command ) {
            if( player.undoBuffer.Count > 0 ) {
                // no need to set player.drawingInProgress here because this is done on the user thread
                Logger.Log( "Player {0} initiated /undo affecting {1} blocks (on world {2})", LogType.UserActivity,
                            player.name,
                            player.undoBuffer.Count,
                            player.world.name );
                player.MessageNow( "Restoring {0} blocks...", player.undoBuffer.Count );
                Queue<BlockUpdate> redoBuffer = new Queue<BlockUpdate>();
                while( player.undoBuffer.Count > 0 ) {
                    BlockUpdate newBlock = player.undoBuffer.Dequeue();
                    BlockUpdate oldBlock = new BlockUpdate( null, newBlock.x, newBlock.y, newBlock.h,
                                                            player.world.map.GetBlock( newBlock.x, newBlock.y, newBlock.h ) );
                    player.world.map.QueueUpdate( newBlock );
                    redoBuffer.Enqueue( oldBlock );
                }
                player.undoBuffer = redoBuffer;
                redoBuffer.TrimExcess();
                player.MessageNow( "Type /undo again to reverse this command." );
                Server.RequestGC();

            } else {
                player.MessageNow( "There is currently nothing to undo." );
            }
        }

        #endregion


        #region Draw Callbacks

        static void DrawOneBlock( Player player, byte drawBlock, int x, int y, int h, ref int blocks, ref int blocksDenied, ref bool cannotUndo ) {
            if( !player.world.map.InBounds( x, y, h ) ) return;
            if( player.CanPlace( x, y, h, drawBlock ) != CanPlaceResult.Allowed ) {
                blocksDenied++;
                return;
            }
            byte block = player.world.map.GetBlock( x, y, h );
            if( block == drawBlock ) return;

            // this would've been an easy way to do block tracking for draw commands BUT
            // if i set "origin" to player, he will not receive the block update. I tried.
            player.world.map.QueueUpdate( new BlockUpdate( null, x, y, h, drawBlock ) );
            //player.SendDelayed( PacketWriter.MakeSetBlock( x, y, h, drawBlock ) );

            if( blocks < MaxUndoCount ) {
                player.undoBuffer.Enqueue( new BlockUpdate( null, x, y, h, block ) );
            } else if( !cannotUndo ) {
                player.undoBuffer.Clear();
                player.undoBuffer.TrimExcess();
                player.MessageNow( "NOTE: This draw command is too massive to undo." );
                if( player.Can( Permission.ManageWorlds ) ) {
                    player.MessageNow( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                }
                cannotUndo = true;
            }
            blocks++;
        }


        static void DrawingFinished( Player player, string verb, int blocks, int blocksDenied ) {
            if( blocks == 0 ) {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "No blocks could be {0} due to permission issues.", verb );
                } else {
                    player.MessageNow( "No blocks were {0}.", verb );
                }
            } else {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "{0} {1} blocks ({2} blocks skipped due to permission issues)... " +
                                       "The map is now being updated.", verb, blocks, blocksDenied );
                } else {
                    player.MessageNow( "{0} {1} blocks... The map is now being updated.", verb, blocks );
                }
            }
            if( blocks > 0 ) {
                player.info.ProcessDrawCommand( blocks );
                player.undoBuffer.TrimExcess();
                Server.RequestGC();
            }
        }


        #region Cuboid, CuboidHollow, CuboidWireframe

        internal static void CuboidCallback( Player player, Position[] marks, object tag ) {
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
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.info.rank.DrawLimit,
                                   volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                DrawOneBlock( player, drawBlock, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                            }
                        }
                    }
                }
            }
            DrawingFinished( player, "drawn", blocks, blocksDenied );
            Logger.Log( "{0} drew a cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
        }


        internal static void CuboidHollowCallback( Player player, Position[] marks, object tag ) {
            CuboidHollowArgs args = (CuboidHollowArgs)tag;
            byte drawBlock = (byte)args.outerBlock;
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

            bool fillInner = (args.innerBlock != Block.Undefined && (ex - sx) > 1 && (ey - sy) > 1 && (eh - sh) > 1);


            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !fillInner ) {
                volume -= (ex - sx - 1) * (ey - sy - 1) * (eh - sh - 1);
            }

            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.info.rank.DrawLimit,
                                   volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    DrawOneBlock( player, drawBlock, x, y, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, y, eh, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }
            for( int x = sx; x <= ex; x++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, x, sy, h, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, ey, h, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }
            for( int y = sy; y <= ey; y++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, sx, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, ex, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }

            if( fillInner ) {
                for( int x = sx + 1; x < ex; x += DrawStride ) {
                    for( int y = sy + 1; y < ey; y += DrawStride ) {
                        for( int h = sh + 1; h < eh; h++ ) {
                            for( int y3 = 0; y3 < DrawStride && y + y3 < ey; y3++ ) {
                                for( int x3 = 0; x3 < DrawStride && x + x3 < ex; x3++ ) {
                                    DrawOneBlock( player, (byte)args.innerBlock, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                                }
                            }
                        }
                    }
                }
            }

            Logger.Log( "{0} drew a hollow cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void CuboidWireframeCallback( Player player, Position[] marks, object tag ) {
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

            int volume = (ex - sx + 1) * 4 + (ey - sy + 1) * 4 + (eh - sh + 1) * 4 - 16;

            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.info.rank.DrawLimit,
                                   volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0, blocksDenied=0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                DrawOneBlock( player, drawBlock, x, sy, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, x, sy, eh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, x, ey, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, x, ey, eh, ref blocks, ref blocksDenied, ref cannotUndo );
            }

            for( int y = sy; y <= ey; y++ ) {
                DrawOneBlock( player, drawBlock, sx, y, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, sx, y, eh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, y, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, y, eh, ref blocks, ref blocksDenied, ref cannotUndo );
            }

            for( int h = sh; h <= eh; h++ ) {
                DrawOneBlock( player, drawBlock, sx, sy, h, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, sy, h, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, sx, ey, h, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, ey, h, ref blocks, ref blocksDenied, ref cannotUndo );
            }

            Logger.Log( "{0} drew a wireframe cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }

        #endregion


        unsafe internal static void ReplaceCallback( Player player, Position[] marks, object drawArgs ) {
            ReplaceArgs args = (ReplaceArgs)drawArgs;

            byte* specialTypes = stackalloc byte[args.types.Length];
            int specialTypeCount = args.types.Length;
            for( int i = 0; i < args.types.Length; i++ ) {
                specialTypes[i] = (byte)args.types[i];
            }
            byte replacementBlock = (byte)args.replacementBlock;
            bool doExclude = args.doExclude;

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                player.info.rank.DrawLimit,
                                volume );
                return;
            }

            player.undoBuffer.Clear();

            bool cannotUndo = false;
            int blocks = 0, blocksDenied = 0;
            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                byte block = player.world.map.GetBlock( x + x3, y + y3, h );

                                if( args.doExclude ) {
                                    bool skip = false;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = true;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                } else {
                                    bool skip = true;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = false;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                }

                                if( player.CanPlace( x + x3, y + y3, h, replacementBlock ) != CanPlaceResult.Allowed ) {
                                    blocksDenied++;
                                    continue;
                                }
                                player.world.map.QueueUpdate( new BlockUpdate( null, x + x3, y + y3, h, replacementBlock ) );
                                if( blocks < MaxUndoCount ) {
                                    player.undoBuffer.Enqueue( new BlockUpdate( null, x + x3, y + y3, h, block ) );
                                } else if( !cannotUndo ) {
                                    player.undoBuffer.Clear();
                                    player.undoBuffer.TrimExcess();
                                    player.MessageNow( "NOTE: This draw command is too massive to undo." );
                                    cannotUndo = true;
                                    if( player.Can( Permission.ManageWorlds ) ) {
                                        player.MessageNow( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                                    }
                                }
                                blocks++;

                            }
                        }
                    }
                }
            }

            string affectedString = "";
            for( int i = 0; i < specialTypeCount; i++ ) {
                affectedString += ", " + ((Block)specialTypes[i]);
            }
            Logger.Log( "{0} replaced {1} blocks {2} ({3}) with {4} (on world {5})", LogType.UserActivity,
                        player.name, blocks,
                        (doExclude ? "except" : "of"),
                        affectedString.Substring( 2 ), (Block)replacementBlock,
                        player.world.name );

            DrawingFinished( player, "replaced", blocks, blocksDenied );
        }


        #region Ellipsoid, Hollow Ellipsoid, Sphere, HollowSphere

        internal static void EllipsoidCallback( Player player, Position[] marks, object tag ) {
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
            double rx = (ex - sx + 1) / 2d;
            double ry = (ey - sy + 1) / 2d;
            double rh = (eh - sh + 1) / 2d;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rh2 = 1 / (rh * rh);

            // find center points
            double cx = (ex + sx) / 2d;
            double cy = (ey + sy) / 2d;
            double ch = (eh + sh) / 2d;


            int volume = (int)(4 / 3d * Math.PI * rx * ry * rh);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.info.rank.DrawLimit,
                                   volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
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
                                    DrawOneBlock( player, drawBlock, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                                }
                            }
                        }
                    }
                }
            }
            Logger.Log( "{0} drew an ellipsoid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void EllipsoidHollowCallback( Player player, Position[] marks, object tag ) {
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
            double rx = (ex - sx + 1) / 2d;
            double ry = (ey - sy + 1) / 2d;
            double rh = (eh - sh + 1) / 2d;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rh2 = 1 / (rh * rh);

            // find center points
            double cx = (ex + sx) / 2d;
            double cy = (ey + sy) / 2d;
            double ch = (eh + sh) / 2d;

            // rougher estimation than the non-hollow form, a voxelized surface is a bit funky
            int volume = (int)(4 / 3d * Math.PI * ((rx + .5) * (ry + .5) * (rh + .5) - (rx - .5) * (ry - .5) * (rh - .5)) * 0.85);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.info.rank.DrawLimit,
                                   volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {

                        double dx = (x - cx);
                        double dy = (y - cy);
                        double dh = (h - ch);

                        if( (dx * dx) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 <= 1 ) {
                            // we touched the surface
                            // keep drilling until we hit an internal block
                            do {
                                DrawOneBlock( player, drawBlock, x, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                                DrawOneBlock( player, drawBlock, x, y, (int)(ch - dh), ref blocks, ref blocksDenied, ref cannotUndo );
                                dh = (++h - ch);
                            } while( h <= (int)ch &&
                                    ((dx + 1) * (dx + 1) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx - 1) * (dx - 1) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy + 1) * (dy + 1) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy - 1) * (dy - 1) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy * dy) * ry2 + (dh + 1) * (dh + 1) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy * dy) * ry2 + (dh - 1) * (dh - 1) * rh2 > 1) );
                            break;
                        }
                    }
                }
            }
            Logger.Log( "{0} drew a hollow ellipsoid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void SphereCallback( Player player, Position[] marks, object tag ) {
            double radius = Math.Sqrt( (marks[0].x - marks[1].x) * (marks[0].x - marks[1].x) +
                                       (marks[0].y - marks[1].y) * (marks[0].y - marks[1].y) +
                                       (marks[0].h - marks[1].h) * (marks[0].h - marks[1].h) );

            marks[1].x = (short)Math.Round( marks[0].x - radius );
            marks[1].y = (short)Math.Round( marks[0].y - radius );
            marks[1].h = (short)Math.Round( marks[0].h - radius );

            marks[0].x = (short)Math.Round( marks[0].x + radius );
            marks[0].y = (short)Math.Round( marks[0].y + radius );
            marks[0].h = (short)Math.Round( marks[0].h + radius );

            EllipsoidCallback( player, marks, tag );
        }


        internal static void SphereHollowCallback( Player player, Position[] marks, object tag ) {
            double radius = Math.Sqrt( (marks[0].x - marks[1].x) * (marks[0].x - marks[1].x) +
                                       (marks[0].y - marks[1].y) * (marks[0].y - marks[1].y) +
                                       (marks[0].h - marks[1].h) * (marks[0].h - marks[1].h) );

            marks[1].x = (short)Math.Round( marks[0].x - radius );
            marks[1].y = (short)Math.Round( marks[0].y - radius );
            marks[1].h = (short)Math.Round( marks[0].h - radius );

            marks[0].x = (short)Math.Round( marks[0].x + radius );
            marks[0].y = (short)Math.Round( marks[0].y + radius );
            marks[0].h = (short)Math.Round( marks[0].h + radius );

            EllipsoidHollowCallback( player, marks, tag );
        }

        #endregion


        internal static void LineCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.lastUsedBlockType;
            }

            player.undoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            // LINE CODE

            int x1 = marks[0].x, y1 = marks[0].y, z1 = marks[0].h, x2 = marks[1].x, y2 = marks[1].y, z2 = marks[1].h;
            int i, dx, dy, dz, l, m, n, x_inc, y_inc, z_inc, err_1, err_2, dx2, dy2, dz2;
            int[] pixel = new int[3];
            pixel[0] = x1;
            pixel[1] = y1;
            pixel[2] = z1;
            dx = x2 - x1;
            dy = y2 - y1;
            dz = z2 - z1;
            x_inc = (dx < 0) ? -1 : 1;
            l = Math.Abs( dx );
            y_inc = (dy < 0) ? -1 : 1;
            m = Math.Abs( dy );
            z_inc = (dz < 0) ? -1 : 1;
            n = Math.Abs( dz );
            dx2 = l << 1;
            dy2 = m << 1;
            dz2 = n << 1;

            DrawOneBlock( player, drawBlock, x2, y2, z2, ref blocks, ref blocksDenied, ref cannotUndo );

            if( (l >= m) && (l >= n) ) {

                err_1 = dy2 - l;
                err_2 = dz2 - l;
                for( i = 0; i < l; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err_1 > 0 ) {
                        pixel[1] += y_inc;
                        err_1 -= dx2;
                    }
                    if( err_2 > 0 ) {
                        pixel[2] += z_inc;
                        err_2 -= dx2;
                    }
                    err_1 += dy2;
                    err_2 += dz2;
                    pixel[0] += x_inc;
                }
            } else if( (m >= l) && (m >= n) ) {
                err_1 = dx2 - m;
                err_2 = dz2 - m;
                for( i = 0; i < m; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err_1 > 0 ) {
                        pixel[0] += x_inc;
                        err_1 -= dy2;
                    }
                    if( err_2 > 0 ) {
                        pixel[2] += z_inc;
                        err_2 -= dy2;
                    }
                    err_1 += dx2;
                    err_2 += dz2;
                    pixel[1] += y_inc;
                }
            } else {
                err_1 = dy2 - n;
                err_2 = dx2 - n;
                for( i = 0; i < n; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err_1 > 0 ) {
                        pixel[1] += y_inc;
                        err_1 -= dz2;
                    }
                    if( err_2 > 0 ) {
                        pixel[0] += x_inc;
                        err_2 -= dz2;
                    }
                    err_1 += dy2;
                    err_2 += dx2;
                    pixel[2] += z_inc;
                }
            }

            // END LINE CODE
            Logger.Log( "{0} drew a line containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }

        #endregion


        #region Copy and Paste

        static CommandDescriptor cdCopy = new CommandDescriptor {
            name = "copy",
            permissions = new[] { Permission.CopyAndPaste },
            help = "Copy blocks for pasting. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            handler = Copy
        };

        internal static void Copy( Player player, Command cmd ) {
            player.SetCallback( 2, CopyCallback, null );
            player.MessageNow( "Copy: Place a block or type /mark to use your location." );
        }

        internal static void CopyCallback( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.info.rank.DrawLimit, volume ) );
                return;
            }

            // remember dimensions and orientation
            CopyInformation copyInfo = new CopyInformation {
                widthX = marks[1].x - marks[0].x,
                widthY = marks[1].y - marks[0].y,
                height = marks[1].h - marks[0].h,
                buffer = new byte[ex - sx + 1,ey - sy + 1,eh - sh + 1]
            };

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        copyInfo.buffer[x - sx, y - sy, h - sh] = player.world.map.GetBlock( x, y, h );
                    }
                }
            }

            player.copyInformation = copyInfo;
            player.MessageNow( "{0} blocks were copied. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.height > 0 ? "bottom" : "top"),
                               (copyInfo.widthY > 0 ? "south" : "north"),
                               (copyInfo.widthX > 0 ? "west" : "east") );

            Logger.Log( "{0} copied {1} blocks from {2}.", LogType.UserActivity,
                        player.name, volume, player.world.name );
        }



        static CommandDescriptor cdCut = new CommandDescriptor {
            name = "cut",
            permissions = new[] { Permission.CopyAndPaste },
            help = "Copies and removes blocks for pasting. Unless a different block type is specified, the area is filled with air. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/cut&S from.",
            usage = "/cut [FillBlock]",
            handler = Cut
        };

        internal static void Cut( Player player, Command cmd ) {
            Block fillBlock;
            if( cmd.NextBlockType( out fillBlock ) ) {
                if( fillBlock == Block.Undefined ) {
                    cmd.Rewind();
                    player.Message( "Cut: Unknown block type \"{0}\"", cmd.Next() );
                    return;
                }
            } else {
                fillBlock = Block.Air;
            }
            player.SetCallback( 2, CutCallback, fillBlock );
            player.MessageNow( "Cut: Place a block or type /mark to use your location." );
        }

        internal static void CutCallback( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            byte fillType = (byte)tag;

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.info.rank.DrawLimit, volume ) );
                return;
            }

            // remember dimensions and orientation
            CopyInformation copyInfo = new CopyInformation {
                widthX = marks[1].x - marks[0].x,
                widthY = marks[1].y - marks[0].y,
                height = marks[1].h - marks[0].h,
                buffer = new byte[ex - sx + 1, ey - sy + 1, eh - sh + 1]
            };

            player.undoBuffer.Clear();
            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        copyInfo.buffer[x - sx, y - sy, h - sh] = player.world.map.GetBlock( x, y, h );
                        DrawOneBlock( player, fillType, x, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                    }
                }
            }

            player.copyInformation = copyInfo;
            player.MessageNow( "{0} blocks were cut. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.height > 0 ? "bottom" : "top"),
                               (copyInfo.widthY > 0 ? "south" : "north"),
                               (copyInfo.widthX > 0 ? "west" : "east") );
            ;
            Logger.Log( "{0} cut {1} blocks from {2}, replacing {3} blocks with {4}.", LogType.UserActivity,
                        player.name, volume, player.world.name, blocks, (Block)fillType );

            player.undoBuffer.TrimExcess();
            Server.RequestGC();
        }



        static CommandDescriptor cdPasteNot = new CommandDescriptor {
            name = "pastenot",
            permissions = new[] { Permission.CopyAndPaste },
            help = "Paste previously copied blocks, excluding specified block type(s). " +
                   "Used together with &H/copy&S command. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from. ",
            usage = "/pastenot ExcludedBlock [AnotherOne [AndAnother]]",
            handler = PasteNot
        };

        internal static void PasteNot( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            PasteArgs args;
            List<Block> excludedTypes = new List<Block>();
            Block excludedType;
            while( cmd.NextBlockType( out excludedType ) ) {
                if( excludedType != Block.Undefined ) {
                    excludedTypes.Add( excludedType );
                } else {
                    player.MessageNow( "Paste: Unrecognized block type." );
                    return;
                }
            }

            if( excludedTypes.Count > 0 ) {
                args = new PasteArgs {
                    doExclude = true,
                    types = excludedTypes.ToArray()
                };
                string includedString = "";
                foreach( Block block in excludedTypes ) {
                    includedString += ", " + block;
                }
                player.MessageNow( "Ready to paste all EXCEPT {0}", includedString.Substring( 2 ) );
            } else {
                player.MessageNow( "PasteNot: Please specify block(s) to exclude." );
                return;
            }

            player.SetCallback( 1, PasteCallback, args );

            player.MessageNow( "PasteNot: Place a block or type /mark to use your location. " );
        }


        static CommandDescriptor cdPaste = new CommandDescriptor {
            name = "paste",
            permissions = new[] { Permission.CopyAndPaste },
            help = "Pastes previously copied blocks. Used together with &H/copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            usage = "/paste [IncludedBlock [AnotherOne [AndAnother]]]",
            handler = Paste
        };

        internal static void Paste( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            List<Block> includedTypes = new List<Block>();
            Block includedType;
            while( cmd.NextBlockType( out includedType ) ) {
                if( includedType != Block.Undefined ) {
                    includedTypes.Add( includedType );
                } else {
                    player.MessageNow( "Paste: Unrecognized block type." );
                    return;
                }
            }

            PasteArgs args;
            if( includedTypes.Count > 0 ) {
                args = new PasteArgs {
                    doInclude = true,
                    types = includedTypes.ToArray()
                };
                string includedString = "";
                foreach( Block block in includedTypes ) {
                    includedString += ", " + block;
                }
                player.MessageNow( "Ready to paste ONLY {0}", includedString.Substring( 2 ) );
            } else {
                args = new PasteArgs {
                    types = new Block[0]
                };
            }

            player.SetCallback( 1, PasteCallback, args );

            player.MessageNow( "Paste: Place a block or type /mark to use your location. " );
        }


        unsafe internal static void PasteCallback( Player player, Position[] marks, object tag ) {
            CopyInformation info = player.copyInformation;

            PasteArgs args = (PasteArgs)tag;
            byte* specialTypes = stackalloc byte[args.types.Length];
            int specialTypeCount = args.types.Length;
            for( int i = 0; i < args.types.Length; i++ ) {
                specialTypes[i] = (byte)args.types[i];
            }
            Map map = player.world.map;

            BoundingBox bounds = new BoundingBox( marks[0], info.widthX, info.widthY, info.height );

            if( bounds.xMin < 0 || bounds.xMax > map.widthX - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( bounds.yMin < 0 || bounds.yMax > map.widthY - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( bounds.hMin < 0 || bounds.hMax > map.height - 1 ) {
                player.MessageNow( "Warning: Not enough room vertically, paste cut off." );
            }

            player.undoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;
            byte block;

            for( int x = bounds.xMin; x <= bounds.xMax; x += DrawStride ) {
                for( int y = bounds.yMin; y <= bounds.yMax; y += DrawStride ) {
                    for( int h = bounds.hMin; h <= bounds.hMax; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= bounds.yMax; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= bounds.xMax; x3++ ) {
                                block = info.buffer[x + x3 - bounds.xMin, y + y3 - bounds.yMin, h - bounds.hMin];

                                if( args.doInclude ) {
                                    bool skip = true;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = false;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                } else if( args.doExclude ) {
                                    bool skip = false;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = true;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                }
                                DrawOneBlock( player, block, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                            }
                        }
                    }
                }
            }

            Logger.Log( "{0} pasted {1} blocks to {2}.", LogType.UserActivity,
                        player.name, blocks, player.world.name );
            DrawingFinished( player, "pasted", blocks, blocksDenied );
        }


        static CommandDescriptor cdMirror = new CommandDescriptor {
            name = "mirror",
            aliases = new[] { "flip" },
            permissions = new[] { Permission.CopyAndPaste },
            help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/copymirror X Y&S.",
            usage = "/mirror [X] [Y] [Z]",
            handler = Mirror
        };

        internal static void Mirror( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to flip! Copy something first." );
                return;
            }

            bool flipX = false, flipY = false, flipH = false;
            string axis;
            while( (axis = cmd.Next()) != null ) {
                foreach( char c in axis.ToLower() ) {
                    if( c == 'x' ) flipX = true;
                    if( c == 'y' ) flipY = true;
                    if( c == 'z' ) flipH = true;
                }
            }

            if( !flipX && !flipY && !flipH ) {
                cdMirror.PrintUsage( player );
                return;
            }

            byte block;
            byte[, ,] buffer = player.copyInformation.buffer;

            if( flipX ) {
                int left = 0;
                int right = buffer.GetLength( 0 ) - 1;
                while( left < right ) {
                    for( int y = player.copyInformation.buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                        for( int h = player.copyInformation.buffer.GetLength( 2 ) - 1; h >= 0; h-- ) {
                            block = buffer[left, y, h];
                            buffer[left, y, h] = buffer[right, y, h];
                            buffer[right, y, h] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipY ) {
                int left = 0;
                int right = buffer.GetLength( 1 ) - 1;
                while( left < right ) {
                    for( int x = player.copyInformation.buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int h = player.copyInformation.buffer.GetLength( 2 ) - 1; h >= 0; h-- ) {
                            block = buffer[x, left, h];
                            buffer[x, left, h] = buffer[x, right, h];
                            buffer[x, right, h] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipH ) {
                int left = 0;
                int right = buffer.GetLength( 2 ) - 1;
                while( left < right ) {
                    for( int x = player.copyInformation.buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int y = player.copyInformation.buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                            block = buffer[x, y, left];
                            buffer[x, y, left] = buffer[x, y, right];
                            buffer[x, y, right] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipX ) {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along all axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) and Y (north/south) axes." );
                    }
                } else {
                    if( flipH ) {
                        player.Message( "Flipped copy along X (east/west) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) axis." );
                    }
                }
            } else {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along Y (north/south) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along Y (north/south) axis." );
                    }
                } else {
                    player.Message( "Flipped copy along Z (vertical) axis." );
                }
            }
        }


        static CommandDescriptor cdRotate = new CommandDescriptor {
            name = "rotate",
            permissions = new[] { Permission.CopyAndPaste },
            help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            usage = "/rotate (-90|90|180|270) (X|Y|Z)",
            handler = Rotate
        };

        enum RotationAxis {
            X, Y, Z
        }
        internal static void Rotate( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to rotate! Copy something first." );
                return;
            }

            int degrees;
            if( !cmd.NextInt( out degrees ) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270) ) {
                cdRotate.PrintUsage( player );
                return;
            }

            string axisName = cmd.Next();
            RotationAxis axis = RotationAxis.Z;
            if( axisName != null ) {
                switch( axisName.ToLower() ) {
                    case "x":
                        axis = RotationAxis.X;
                        break;
                    case "y":
                        axis = RotationAxis.Y;
                        break;
                    case "z":
                    case "h":
                        axis = RotationAxis.Z;
                        break;
                    default:
                        cdRotate.PrintUsage( player );
                        return;
                }
            }


            // allocate the new buffer
            byte[, ,] oldBuffer = player.copyInformation.buffer;
            byte[, ,] newBuffer;

            if( degrees == 180 ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 2 )];

            } else if( axis == RotationAxis.X ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 )];
                int dimY = player.copyInformation.widthY;
                player.copyInformation.widthY = player.copyInformation.height;
                player.copyInformation.height = dimY;

            } else if( axis == RotationAxis.Y ) {
                newBuffer = new byte[oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 )];
                int dimX = player.copyInformation.widthX;
                player.copyInformation.widthX = player.copyInformation.height;
                player.copyInformation.height = dimX;

            } else {
                newBuffer = new byte[oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 )];
                int dimY = player.copyInformation.widthY;
                player.copyInformation.widthY = player.copyInformation.widthX;
                player.copyInformation.widthX = dimY;
            }


            // construct the rotation matrix
            int[,] matrix = new[,]{
                {1,0,0},
                {0,1,0},
                {0,0,1}
            };

            int a, b;
            switch( axis ) {
                case RotationAxis.X:
                    a = 1;
                    b = 2;
                    break;
                case RotationAxis.Y:
                    a = 0;
                    b = 2;
                    break;
                default:
                    a = 0;
                    b = 1;
                    break;
            }

            switch( degrees ) {
                case 90:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = -1;
                    matrix[b, a] = 1;
                    break;
                case 180:
                    matrix[a, a] = -1;
                    matrix[b, b] = -1;
                    break;
                case -90:
                case 270:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = 1;
                    matrix[b, a] = -1;
                    break;
            }

            // apply the rotation matrix
            int nx, ny, nz;
            for( int x = oldBuffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = oldBuffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = oldBuffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        nx = (matrix[0, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[0, 0] > 0 ? x : 0)) +
                             (matrix[0, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[0, 1] > 0 ? y : 0)) +
                             (matrix[0, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[0, 2] > 0 ? z : 0));
                        ny = (matrix[1, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[1, 0] > 0 ? x : 0)) +
                             (matrix[1, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[1, 1] > 0 ? y : 0)) +
                             (matrix[1, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[1, 2] > 0 ? z : 0));
                        nz = (matrix[2, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[2, 0] > 0 ? x : 0)) +
                             (matrix[2, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[2, 1] > 0 ? y : 0)) +
                             (matrix[2, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[2, 2] > 0 ? z : 0));
                        newBuffer[nx, ny, nz] = oldBuffer[x, y, z];
                    }
                }
            }

            player.Message( "Rotated copy by {0} degrees around {1} axis.", degrees, axis );
            player.copyInformation.buffer = newBuffer;
        }

        #endregion


        static CommandDescriptor cdMark = new CommandDescriptor {
            name = "mark",
            aliases = new[] { "m" },
            help = "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                   "You can mark in places where making blocks is difficult (e.g. mid-air).",
            handler = Mark
        };

        internal static void Mark( Player player, Command command ) {
            Position pos = new Position( (short)((player.pos.x - 1) / 32), (short)((player.pos.y - 1) / 32), (short)((player.pos.h - 1) / 32) );
            pos.x = (short)Math.Min( player.world.map.widthX - 1, Math.Max( 0, (int)pos.x ) );
            pos.y = (short)Math.Min( player.world.map.widthY - 1, Math.Max( 0, (int)pos.y ) );
            pos.h = (short)Math.Min( player.world.map.height - 1, Math.Max( 0, (int)pos.h ) );

            if( player.selectionMarksExpected > 0 ) {
                player.selectionMarks.Enqueue( pos );
                player.selectionMarkCount++;
                if( player.selectionMarkCount >= player.selectionMarksExpected ) {
                    player.selectionCallback( player, player.selectionMarks.ToArray(), player.selectionArgs );
                    player.selectionMarksExpected = 0;
                } else {
                    player.MessageNow( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                                       player.selectionMarkCount,
                                       pos.x, pos.y, pos.h,
                                       player.selectionMarkCount + 1 );
                }
            } else {
                player.MessageNow( "Cannot mark - no draw or zone commands initiated." );
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
                player.MessageNow( "Selection cancelled." );
            } else {
                player.MessageNow( "There is currently nothing to cancel." );
            }
        }
    }
}