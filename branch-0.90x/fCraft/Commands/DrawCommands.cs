using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Drawing;
using fCraft.MapConversion;
using JetBrains.Annotations;

namespace fCraft {
    static class DrawCommands {
        const string GeneralDrawingHelp =
            " Use &H/Cancel&S to cancel selection mode. Use &H/Undo&S to stop and undo the last command.";

        internal static void Init() {
            CommandManager.RegisterCommand( CdReplace );
            CommandManager.RegisterCommand( CdReplaceNot );
            CommandManager.RegisterCommand( CdReplaceBrush );
            CdReplace.Help += GeneralDrawingHelp;
            CdReplaceNot.Help += GeneralDrawingHelp;
            CdReplaceBrush.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdCut );
            CommandManager.RegisterCommand( CdPaste );
            CommandManager.RegisterCommand( CdPasteNot );
            CommandManager.RegisterCommand( CdPasteX );
            CommandManager.RegisterCommand( CdPasteNotX );
            CdCut.Help += GeneralDrawingHelp;
            CdPaste.Help += GeneralDrawingHelp;
            CdPasteNot.Help += GeneralDrawingHelp;
            CdPasteX.Help += GeneralDrawingHelp;
            CdPasteNotX.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdRestore );
            CdRestore.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdCuboid );
            CommandManager.RegisterCommand( CdCuboidWireframe );
            CommandManager.RegisterCommand( CdCuboidHollow );
            CdCuboid.Help += GeneralDrawingHelp;
            CdCuboidHollow.Help += GeneralDrawingHelp;
            CdCuboidWireframe.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdEllipsoid );
            CommandManager.RegisterCommand( CdEllipsoidHollow );
            CdEllipsoid.Help += GeneralDrawingHelp;
            CdEllipsoidHollow.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdLine );
            CommandManager.RegisterCommand( CdTriangle );
            CommandManager.RegisterCommand( CdTriangleWireframe );
            CdLine.Help += GeneralDrawingHelp;
            CdTriangle.Help += GeneralDrawingHelp;
            CdTriangleWireframe.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdSphere );
            CommandManager.RegisterCommand( CdSphereHollow );
            CommandManager.RegisterCommand( CdTorus );
            CdSphere.Help += GeneralDrawingHelp;
            CdSphereHollow.Help += GeneralDrawingHelp;
            CdTorus.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdFill2D );
            CommandManager.RegisterCommand( CdFill3D );
            CdFill2D.Help += GeneralDrawingHelp;
            CdFill3D.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdUndoArea );
            CommandManager.RegisterCommand( CdUndoPlayer );
            CommandManager.RegisterCommand( CdUndoAreaNot );
            CommandManager.RegisterCommand( CdUndoPlayerNot );
            CdUndoArea.Help += GeneralDrawingHelp;
            CdUndoAreaNot.Help += GeneralDrawingHelp;
        }


        #region DrawOperations & Brushes

        static readonly CommandDescriptor CdCuboid = new CommandDescriptor {
            Name = "Cuboid",
            Aliases = new[] {
                "blb", "c", "z"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help = "Fills a rectangular area (cuboid) with blocks.",
            Handler = CuboidHandler
        };

        static void CuboidHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new CuboidDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidWireframe = new CommandDescriptor {
            Name = "CuboidW",
            Aliases = new[] {
                "cubw", "cw", "bfb"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help = "Draws a wireframe box (a frame) around the selected rectangular area.",
            Handler = CuboidWireframeHandler
        };

        static void CuboidWireframeHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new CuboidWireframeDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidHollow = new CommandDescriptor {
            Name = "CuboidH",
            Aliases = new[] {
                "cubh", "ch", "h", "bhb"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help =
                "Surrounds the selected rectangular area with a box of blocks. " +
                "Unless two blocks are specified, leaves the inside untouched.",
            Handler = CuboidHollowHandler
        };

        static void CuboidHollowHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new CuboidHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdEllipsoid = new CommandDescriptor {
            Name = "Ellipsoid",
            Aliases = new[] {
                "e"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help = "Fills an ellipsoid-shaped area (elongated sphere) with blocks.",
            Handler = EllipsoidHandler
        };

        static void EllipsoidHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdEllipsoidHollow = new CommandDescriptor {
            Name = "EllipsoidH",
            Aliases = new[] {
                "eh"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help = "Surrounds the selected an ellipsoid-shaped area (elongated sphere) with a shell of blocks.",
            Handler = EllipsoidHollowHandler
        };

        static void EllipsoidHollowHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdSphere = new CommandDescriptor {
            Name = "Sphere",
            Aliases = new[] {
                "sp", "spheroid"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw, Permission.DrawAdvanced
            },
            RepeatableSelection = true,
            Help =
                "Fills a spherical area with blocks. " + "The first mark denotes the CENTER of the sphere, and " +
                "distance to the second mark denotes the radius.",
            Handler = SphereHandler
        };

        static void SphereHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new SphereDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdSphereHollow = new CommandDescriptor {
            Name = "SphereH",
            Aliases = new[] {
                "sph", "hsphere"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw, Permission.DrawAdvanced
            },
            RepeatableSelection = true,
            Help =
                "Surrounds a spherical area with a shell of blocks. " +
                "The first mark denotes the CENTER of the sphere, and " +
                "distance to the second mark denotes the radius.",
            Handler = SphereHollowHandler
        };

        static void SphereHollowHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new SphereHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdLine = new CommandDescriptor {
            Name = "Line",
            Aliases = new[] {
                "ln"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help = "Draws a continuous line between two points with blocks. " + "Marks do not have to be aligned.",
            Handler = LineHandler
        };

        static void LineHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new LineDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTriangleWireframe = new CommandDescriptor {
            Name = "TriangleW",
            Aliases = new[] {
                "tw"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help = "Draws lines between three points, to form a triangle.",
            Handler = TriangleWireframeHandler
        };

        static void TriangleWireframeHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new TriangleWireframeDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTriangle = new CommandDescriptor {
            Name = "Triangle",
            Aliases = new[] {
                "t"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Help = "Draws a triangle between three points.",
            Handler = TriangleHandler
        };

        static void TriangleHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new TriangleDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTorus = new CommandDescriptor {
            Name = "Torus",
            Aliases = new[] {
                "doughnut", "donut", "bagel"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw, Permission.DrawAdvanced
            },
            RepeatableSelection = true,
            Help =
                "Draws a horizontally-oriented torus. The first mark denotes the CENTER of the torus, horizontal " +
                "distance to the second mark denotes the ring radius, and the vertical distance to the second mark denotes the " +
                "tube radius",
            Handler = TorusHandler
        };

        static void TorusHandler( Player player, CommandReader cmd ) {
            DrawOperationBegin( player, cmd, new TorusDrawOperation( player ) );
        }



        static void DrawOperationBegin( Player player, CommandReader cmd, DrawOperation op ) {
            // try to create instance of player's currently selected brush
            // all command parameters are passed to the brush
            IBrushInstance brush = player.Brush.MakeInstance( player, cmd, op );

            // MakeInstance returns null if there were problems with syntax, abort
            if( brush == null )
                return;
            op.Brush = brush;
            player.SelectionStart( op.ExpectedMarks, DrawOperationCallback, op, Permission.Draw );
            player.Message( "{0}: Click or &H/Mark&S {1} blocks.", op.Description, op.ExpectedMarks );
        }


        static void DrawOperationCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Prepare( marks ) )
                return;
            if( !player.CanDraw( op.BlocksTotalEstimate ) ) {
                player.MessageNow(
                    "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                    player.Info.Rank.DrawLimit,
                    op.Bounds.Volume );
                op.Cancel();
                return;
            }
            player.Message( "{0}: Processing ~{1} blocks.", op.Description, op.BlocksTotalEstimate );
            op.Begin();
        }

        #endregion


        #region Fill

        static readonly CommandDescriptor CdFill2D = new CommandDescriptor {
            Name = "Fill2D",
            Aliases = new[] {
                "f2d"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw, Permission.DrawAdvanced
            },
            RepeatableSelection = true,
            Help =
                "Fills a continuous area with blocks, in 2D. " +
                "Takes just 1 mark, and replaces blocks of the same type as the block you clicked. " +
                "Works similar to \"Paint Bucket\" tool in Photoshop. " +
                "Direction of effect is determined by where the player is looking.",
            Handler = Fill2DHandler
        };

        static void Fill2DHandler( Player player, CommandReader cmd ) {
            Fill2DDrawOperation op = new Fill2DDrawOperation( player );

            IBrushInstance brush = player.Brush.MakeInstance( player, cmd, op );
            if( brush == null )
                return;
            op.Brush = brush;

            player.SelectionStart( 1, Fill2DCallback, op, Permission.Draw );
            player.Message( "{0}: Click a block to start filling.", op.Description );
        }


        static void Fill2DCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Prepare( marks ) )
                return;
            if( player.WorldMap.GetBlock( marks[0] ) == Block.Air ) {
                Logger.Log( LogType.UserActivity,
                            "Fill2D: Asked {0} to confirm replacing air on world {1}",
                            player.Name,
                            // ReSharper disable PossibleNullReferenceException
                            player.World.Name );
                // ReSharper restore PossibleNullReferenceException
                player.Confirm( Fill2DConfirmCallback, op, "{0}: Replace air?", op.Description );
            } else {
                Fill2DConfirmCallback( player, op, false );
            }
        }


        static void Fill2DConfirmCallback( Player player, object tag, bool fromConsole ) {
            Fill2DDrawOperation op = (Fill2DDrawOperation)tag;
            int maxDim = Math.Max( op.Bounds.Width, Math.Max( op.Bounds.Length, op.Bounds.Height ) );
            int otherDim = op.Bounds.Volume/maxDim;
            player.Message( "{0}: Filling in a {1}x{2} area...", op.Description, maxDim, otherDim );
            op.Begin();
        }


        static readonly CommandDescriptor CdFill3D = new CommandDescriptor {
            Name = "Fill3D",
            Aliases = new[] {
                "f3d"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw, Permission.DrawAdvanced
            },
            RepeatableSelection = true,
            Help =
                "Fills a continuous volume with blocks, in 3D. " +
                "Takes just 1 mark, and replaces blocks of the same type as the block you clicked.",
            Handler = Fill3DHandler
        };

        static void Fill3DHandler( Player player, CommandReader cmd ) {
            Fill3DDrawOperation op = new Fill3DDrawOperation( player );

            IBrushInstance brush = player.Brush.MakeInstance( player, cmd, op );
            if( brush == null )
                return;
            op.Brush = brush;

            player.SelectionStart( 1, Fill3DCallback, op, Permission.Draw );
            player.Message( "{0}: Click a block to start filling.", op.Description );
        }


        static void Fill3DCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Prepare( marks ) )
                return;
            if( player.WorldMap.GetBlock( marks[0] ) == Block.Air ) {
                Logger.Log( LogType.UserActivity,
                            "Fill3D: Asked {0} to confirm replacing air on world {1}",
                            player.Name,
                            player.World.Name );
                player.Confirm( Fill3DConfirmCallback, op, "{0}: Replace air?", op.Description );
            } else {
                Fill3DConfirmCallback( player, op, false );
            }
        }


        static void Fill3DConfirmCallback( Player player, object tag, bool fromConsole ) {
            Fill3DDrawOperation op = (Fill3DDrawOperation)tag;
            player.Message( "{0}: Filling in a {1}x{2}x{3} area...",
                            op.Description,
                            op.Bounds.Width,
                            op.Bounds.Length,
                            op.Bounds.Height );
            op.Begin();
        }

        #endregion


        #region Replace

        static void ReplaceHandlerInternal( IBrush factory, Player player, CommandReader cmd ) {
            CuboidDrawOperation op = new CuboidDrawOperation( player );
            IBrushInstance brush = factory.MakeInstance( player, cmd, op );
            if( brush == null )
                return;
            op.Brush = brush;

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.MessageNow( "{0}: Click or &H/Mark&S 2 blocks.", op.Brush.InstanceDescription );
        }


        static readonly CommandDescriptor CdReplace = new CommandDescriptor {
            Name = "Replace",
            Aliases = new[] {
                "r"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Usage = "/Replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            Help = "Replaces all blocks of specified type(s) in an area.",
            Handler = ReplaceHandler
        };

        static void ReplaceHandler( Player player, CommandReader cmd ) {
            var replaceBrush = ReplaceBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null )
                return;
            ReplaceHandlerInternal( replaceBrush, player, cmd );
        }


        static readonly CommandDescriptor CdReplaceNot = new CommandDescriptor {
            Name = "ReplaceNot",
            Aliases = new[] {
                "rn"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw
            },
            RepeatableSelection = true,
            Usage = "/ReplaceNot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            Help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            Handler = ReplaceNotHandler
        };

        static void ReplaceNotHandler( Player player, CommandReader cmd ) {
            var replaceBrush = ReplaceNotBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null )
                return;
            ReplaceHandlerInternal( replaceBrush, player, cmd );
        }


        static readonly CommandDescriptor CdReplaceBrush = new CommandDescriptor {
            Name = "ReplaceBrush",
            Aliases = new[] {
                "rb"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.Draw, Permission.DrawAdvanced
            },
            RepeatableSelection = true,
            Usage = "/ReplaceBrush Block BrushName [Params]",
            Help =
                "Replaces all blocks of specified type(s) in an area with output of a given brush. " +
                "See &H/Help brush&S for a list of available brushes.",
            Handler = ReplaceBrushHandler
        };

        static void ReplaceBrushHandler( Player player, CommandReader cmd ) {
            var replaceBrush = ReplaceBrushBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null )
                return;
            ReplaceHandlerInternal( replaceBrush, player, cmd );
        }

        #endregion


        #region Cut

        static readonly CommandDescriptor CdCut = new CommandDescriptor {
            Name = "Cut",
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.CopyAndPaste
            },
            RepeatableSelection = true,
            Help =
                "Copies and removes blocks for pasting. Unless a different block type is specified, the area is filled with air. " +
                "Used together with &H/Paste&S and &H/PasteNot&S commands. " +
                "Note that pasting starts at the same corner that you started &H/Cut&S from.",
            Usage = "/Cut [FillBlock]",
            Handler = CutHandler
        };

        static void CutHandler( Player player, CommandReader cmd ) {
            Block fillBlock = Block.Air;
            if( cmd.HasNext ) {
                if( !cmd.NextBlock( player, false, out fillBlock ) )
                    return;
                if( cmd.HasNext ) {
                    CdCut.PrintUsage( player );
                    return;
                }
            }

            CutDrawOperation op = new CutDrawOperation( player ) {
                Brush = new NormalBrush( fillBlock )
            };

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.Message( "{0}: Click 2 or &H/Mark&S 2 blocks.", op.Description );
        }

        #endregion


        #region Paste

        static readonly CommandDescriptor CdPasteX = new CommandDescriptor {
            Name = "PasteX",
            Aliases = new[] {
                "px"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.CopyAndPaste
            },
            RepeatableSelection = true,
            Help =
                "Pastes previously copied blocks, aligned. Used together with &H/Copy&S command. " +
                "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                "Takes 2 marks: first sets the origin of pasting, and second sets the direction where to paste.",
            Usage = "/PasteX [IncludedBlock [AnotherOne etc]]",
            Handler = PasteXHandler
        };

        static void PasteXHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 2, new PasteDrawOperation( player, false ) );
        }


        static readonly CommandDescriptor CdPasteNotX = new CommandDescriptor {
            Name = "PasteNotX",
            Aliases = new[] {
                "pnx", "pxn"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.CopyAndPaste
            },
            RepeatableSelection = true,
            Help =
                "Pastes previously copied blocks, aligned, except the given block type(s). " +
                "Used together with &H/Copy&S command. " +
                "Takes 2 marks: first sets the origin of pasting, and second sets the direction where to paste.",
            Usage = "/PasteNotX ExcludedBlock [AnotherOne etc]",
            Handler = PasteNotXHandler
        };

        static void PasteNotXHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 2, new PasteDrawOperation( player, true ) );
        }


        static readonly CommandDescriptor CdPaste = new CommandDescriptor {
            Name = "Paste",
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.CopyAndPaste
            },
            RepeatableSelection = true,
            Help =
                "Pastes previously copied blocks. Used together with &H/Copy&S command. " +
                "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                "Alignment semantics are... complicated.",
            Usage = "/Paste [IncludedBlock [AnotherOne etc]]",
            Handler = PasteHandler
        };

        static void PasteHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 1, new QuickPasteDrawOperation( player, false ) );
        }


        static readonly CommandDescriptor CdPasteNot = new CommandDescriptor {
            Name = "PasteNot",
            Aliases = new[] {
                "pn"
            },
            Category = CommandCategory.Building,
            Permissions = new[] {
                Permission.CopyAndPaste
            },
            RepeatableSelection = true,
            Help =
                "Pastes previously copied blocks, except the given block type(s). " +
                "Used together with &H/Copy&S command. " + "Alignment semantics are... complicated.",
            Usage = "/PasteNot ExcludedBlock [AnotherOne etc]",
            Handler = PasteNotHandler
        };

        static void PasteNotHandler( Player player, CommandReader cmd ) {
            PasteOpHandler( player, cmd, 1, new QuickPasteDrawOperation( player, true ) );
        }


        static void PasteOpHandler( Player player, CommandReader cmd, int expectedMarks, DrawOpWithBrush op ) {
            if( !op.ReadParams( cmd ) )
                return;
            player.SelectionStart( expectedMarks, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            CopyState copyInfo = player.GetCopyState();
            if( copyInfo != null ) {
                player.MessageNow( "{0}: Click or &H/Mark&S the {1} corner.", op.Description, copyInfo.OriginCorner );
            } else {
                player.MessageNow( "{0}: Click or &H/Mark&S a block.", op.Description );
            }
        }

        #endregion


        #region Restore

        // TODO: convert /restore to a DrawOperation
        const BlockChangeContext RestoreContext = BlockChangeContext.Drawn | BlockChangeContext.Restored;


        static readonly CommandDescriptor CdRestore = new CommandDescriptor {
            Name = "Restore",
            Category = CommandCategory.World,
            Permissions = new[] {
                Permission.Draw, Permission.DrawAdvanced, Permission.CopyAndPaste, Permission.ManageWorlds
            },
            RepeatableSelection = true,
            Usage = "/Restore FileName",
            Help =
                "Selectively restores/pastes part of map file into the current world. " +
                "Map file must have the same dimensions as the current world. " +
                "If the file name contains spaces, surround it with quote marks.",
            Handler = RestoreHandler
        };

        static void RestoreHandler( Player player, CommandReader cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                CdRestore.PrintUsage( player );
                return;
            }
            if( cmd.HasNext ) {
                CdRestore.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if( fullFileName == null )
                return;

            Map map;
            if( !MapUtility.TryLoad( fullFileName, true, out map ) ) {
                player.Message( "Could not load the given map file ({0})", fileName );
                return;
            }

            Map playerMap = player.WorldMap;
            if( playerMap.Width != map.Width || playerMap.Length != map.Length || playerMap.Height != map.Height ) {
                player.Message( "Map file dimensions must match your current world's dimensions ({0}x{1}x{2})",
                                playerMap.Width,
                                playerMap.Length,
                                playerMap.Height );
                return;
            }

            map.Metadata["fCraft.Temp", "FileName"] = fullFileName;
            player.SelectionStart( 2, RestoreCallback, map, CdRestore.Permissions );
            player.MessageNow( "Restore: Click or &H/Mark&S 2 blocks." );
        }

        static void RestoreCallback( Player player, Vector3I[] marks, object tag ) {
            BoundingBox selection = new BoundingBox( marks[0], marks[1] );
            Map map = (Map)tag;

            if( !player.CanDraw( selection.Volume ) ) {
                player.MessageNow(
                    "You are only allowed to restore up to {0} blocks at a time. This would affect {1} blocks.",
                    player.Info.Rank.DrawLimit,
                    selection.Volume );
                return;
            }

            int blocksDrawn = 0,
                blocksSkipped = 0;
            UndoState undoState = player.DrawBegin( null );

            World playerWorld = player.World;
            if( playerWorld == null )
                PlayerOpException.ThrowNoWorld( player );
            Map playerMap = player.WorldMap;
            Vector3I coord = new Vector3I();
            for( coord.X = selection.XMin; coord.X <= selection.XMax; coord.X++ ) {
                for( coord.Y = selection.YMin; coord.Y <= selection.YMax; coord.Y++ ) {
                    for( coord.Z = selection.ZMin; coord.Z <= selection.ZMax; coord.Z++ ) {
                        DrawOneBlock( player,
                                      playerMap,
                                      map.GetBlock( coord ),
                                      coord,
                                      RestoreContext,
                                      ref blocksDrawn,
                                      ref blocksSkipped,
                                      undoState );
                    }
                }
            }

            Logger.Log( LogType.UserActivity,
                        "{0} restored {1} blocks on world {2} (@{3},{4},{5} - {6},{7},{8}) from file {9}.",
                        player.Name,
                        blocksDrawn,
                        playerWorld.Name,
                        selection.XMin,
                        selection.YMin,
                        selection.ZMin,
                        selection.XMax,
                        selection.YMax,
                        selection.ZMax,
                        map.Metadata["fCraft.Temp", "FileName"] );

            DrawingFinished( player, "Restored", blocksDrawn, blocksSkipped );
        }


        static void DrawOneBlock( [NotNull] Player player, [NotNull] Map map, Block drawBlock, Vector3I coord,
                                  BlockChangeContext context, ref int blocks, ref int blocksDenied, UndoState undoState ) {
            if( player == null )
                throw new ArgumentNullException( "player" );

            if( !map.InBounds( coord ) )
                return;
            Block block = map.GetBlock( coord );
            if( block == drawBlock )
                return;

            if( player.CanPlace( map, coord, drawBlock, context ) != CanPlaceResult.Allowed ) {
                blocksDenied++;
                return;
            }

            map.QueueUpdate( new BlockUpdate( null, coord, drawBlock ) );
            Player.RaisePlayerPlacedBlockEvent( player, map, coord, block, drawBlock, context );

            if( !undoState.IsTooLargeToUndo ) {
                if( !undoState.Add( coord, block ) ) {
                    player.MessageNow( "NOTE: This draw command is too massive to undo." );
                    player.LastDrawOp = null;
                }
            }
            blocks++;
        }


        static void DrawingFinished( [NotNull] Player player, string verb, int blocks, int blocksDenied ) {
            if( player == null )
                throw new ArgumentNullException( "player" );
            if( blocks == 0 ) {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "No blocks could be {0} due to permission issues.", verb.ToLower() );
                } else {
                    player.MessageNow( "No blocks were {0}.", verb.ToLower() );
                }
            } else {
                if( blocksDenied > 0 ) {
                    player.MessageNow(
                        "{0} {1} blocks ({2} blocks skipped due to permission issues)... " +
                        "The map is now being updated.",
                        verb,
                        blocks,
                        blocksDenied );
                } else {
                    player.MessageNow( "{0} {1} blocks... The map is now being updated.", verb, blocks );
                }
            }
            if( blocks > 0 ) {
                player.Info.ProcessDrawCommand( blocks );
                Server.RequestGC();
            }
        }

        #endregion


        #region UndoPlayer and UndoArea

        sealed class BlockDBUndoArgs {
            public Player Player;
            public PlayerInfo[] Targets;
            public World World;
            public int CountLimit;
            public TimeSpan AgeLimit;
            public BlockDBEntry[] Entries;
            public BoundingBox Area;
            public bool Not;
        }


        // parses and checks command parameters (for both UndoPlayer and UndoArea)

        [CanBeNull]
        static BlockDBUndoArgs ParseBlockDBUndoParams( Player player, CommandReader cmd, string cmdName, bool not ) {
            // check if command's being called by a worldless player (e.g. console)
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            // ensure that BlockDB is enabled
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&W{0}: BlockDB is disabled on this server.", cmdName );
                return null;
            }
            if( !playerWorld.BlockDB.IsEnabled ) {
                player.Message( "&W{0}: BlockDB is disabled in this world.", cmdName );
                return null;
            }

            // parse the first parameter - either numeric or time limit
            string range = cmd.Next();
            if( range == null ) {
                CdUndoPlayer.PrintUsage( player );
                return null;
            }
            int countLimit;
            TimeSpan ageLimit = TimeSpan.Zero;
            if( !Int32.TryParse( range, out countLimit ) && !range.TryParseMiniTimeSpan( out ageLimit ) ) {
                player.Message( "{0}: First parameter should be a number or a timespan.", cmdName );
                return null;
            }
            if( ageLimit > DateTimeUtil.MaxTimeSpan ) {
                player.MessageMaxTimeSpan();
                return null;
            }

            // parse second and consequent parameters (player names)
            HashSet<PlayerInfo> targets = new HashSet<PlayerInfo>();
            bool allPlayers = false;
            while( true ) {
                string name = cmd.Next();
                if( name == null ) {
                    break;
                } else if( name == "*" ) {
                    // all players
                    if( not ) {
                        player.Message( "{0}: \"*\" not allowed (cannot undo \"everyone except everyone\")", cmdName );
                        return null;
                    }
                    if( allPlayers ) {
                        player.Message( "{0}: \"*\" was listed twice.", cmdName );
                        return null;
                    }
                    allPlayers = true;

                } else {
                    // individual player
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, name, SearchOptions.IncludeSelf );
                    if( target == null ) {
                        return null;
                    }
                    if( targets.Contains( target ) ) {
                        player.Message( "{0}: Player {1}&S was listed twice.",
                                        target.ClassyName, cmdName );
                        return null;
                    }
                    // make sure player has the permission
                    if( !not &&
                        player.Info != target && !player.Can( Permission.UndoAll ) &&
                        !player.Can( Permission.UndoOthersActions, target.Rank ) ) {
                        player.Message( "&W{0}: You may only undo actions of players ranked {1}&S or lower.",
                                        cmdName,
                                        player.Info.Rank.GetLimit( Permission.UndoOthersActions ).ClassyName );
                        player.Message( "Player {0}&S is ranked {1}",
                                        target.ClassyName, target.Rank.ClassyName );
                        return null;
                    }
                    targets.Add( target );
                }
            }
            if( targets.Count == 0 && !allPlayers ) {
                player.Message( "{0}: Specify at least one player name, or \"*\" to undo everyone.", cmdName );
                return null;
            }
            if( targets.Count > 0 && allPlayers ) {
                player.Message( "{0}: Cannot mix player names and \"*\".", cmdName );
                return null;
            }

            // undoing everyone ('*' in place of player name) requires UndoAll permission
            if( (not || allPlayers) && !player.Can( Permission.UndoAll ) ) {
                player.MessageNoAccess( Permission.UndoAll );
                return null;
            }

            // Queue UndoPlayerCallback to run
            return new BlockDBUndoArgs {
                Player = player,
                AgeLimit = ageLimit,
                CountLimit = countLimit,
                Area = player.WorldMap.Bounds,
                World = playerWorld,
                Targets = targets.ToArray(),
                Not = not
            };
        }


        // called after player types "/ok" to the confirmation prompt.

        static void BlockDBUndoConfirmCallback( Player player, object tag, bool fromConsole ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            string cmdName = (args.Area == null ? "UndoArea" : "UndoPlayer");
            if( args.Not ) cmdName += "Not";

            // Produce 
            Vector3I[] coords;
            if( args.Area != null ) {
                coords = new[] { args.Area.MinVertex, args.Area.MaxVertex };
            } else {
                coords = new Vector3I[0];
            }

            // Produce a brief param description for BlockDBDrawOperation
            string description;
            if( args.CountLimit > 0 ) {
                if( args.Targets.Length == 0 ) {
                    description = args.CountLimit.ToStringInvariant();
                } else if( args.Not ) {
                    description = String.Format( "{0} by everyone except {1}",
                                                 args.CountLimit,
                                                 args.Targets.JoinToString( p => p.Name ) );
                } else {
                    description = String.Format( "{0} by {1}",
                                                 args.CountLimit,
                                                 args.Targets.JoinToString( p => p.Name ) );
                }
            } else {
                if( args.Targets.Length == 0 ) {
                    description = args.AgeLimit.ToMiniString();
                } else if( args.Not ) {
                    description = String.Format( "{0} by everyone except {1}",
                                                 args.AgeLimit.ToMiniString(),
                                                 args.Targets.JoinToString( p => p.Name ) );
                } else {
                    description = String.Format( "{0} by {1}",
                                                 args.AgeLimit.ToMiniString(),
                                                 args.Targets.JoinToString( p => p.Name ) );
                }
            }

            // start undoing (using DrawOperation infrastructure)
            var op = new BlockDBDrawOperation( player, cmdName, description, coords.Length );
            op.Prepare( coords, args.Entries );

            // log operation
            string targetList;
            if( args.Targets.Length == 0 ) {
                targetList = "(everyone)";
            } else if( args.Not ) {
                targetList = "(everyone) except " + args.Targets.JoinToClassyString();
            } else {
                targetList = args.Targets.JoinToClassyString();
            }
            Logger.Log( LogType.UserActivity,
                        "{0}: Player {1} will undo {2} changes (limit of {3}) by {4} on world {5}",
                        cmdName,
                        player.Name,
                        args.Entries.Length,
                        args.CountLimit == 0 ? args.AgeLimit.ToMiniString() : args.CountLimit.ToStringInvariant(),
                        targetList,
                        args.World.Name );

            op.Begin();
        }

        #region UndoArea

        static readonly CommandDescriptor CdUndoArea = new CommandDescriptor {
            Name = "UndoArea",
            Aliases = new[] { "ua" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions },
            RepeatableSelection = true,
            Usage = "/UndoArea (TimeSpan|BlockCount) PlayerName [AnotherName...]",
            Help = "Reverses changes made by the given player(s). " +
                   "Applies to a selected area in the current world. " +
                   "More than one player name can be given at a time. " +
                   "Players with UndoAll permission can use '*' in place of player name to undo everyone's changes at once.",
            Handler = UndoAreaHandler
        };

        static void UndoAreaHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, "UndoArea", false );
            if( args == null ) return;

            Permission permission;
            if( args.Targets.Length == 0 ) {
                permission = Permission.UndoAll;
            } else {
                permission = Permission.UndoOthersActions;
            }
            player.SelectionStart( 2, UndoAreaSelectionCallback, args, permission );
            player.MessageNow( "UndoArea: Click or &H/Mark&S 2 blocks." );
        }


        static readonly CommandDescriptor CdUndoAreaNot = new CommandDescriptor {
            Name = "UndoAreaNot",
            Aliases = new[] { "uan", "una" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions, Permission.UndoAll },
            RepeatableSelection = true,
            Usage = "/UndoAreaNot (TimeSpan|BlockCount) PlayerName [AnotherName...]",
            Help = "Reverses changes made by everyone EXCEPT the given player(s). " +
                   "Applies to a selected area in the current world. " +
                   "More than one player name can be given at a time.",
            Handler = UndoAreaNotHandler
        };

        static void UndoAreaNotHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, "UndoAreaNot", true );
            if( args == null ) return;

            player.SelectionStart( 2, UndoAreaSelectionCallback, args, CdUndoAreaNot.Permissions );
            player.MessageNow( "UndoAreaNot: Click or &H/Mark&S 2 blocks." );
        }


        // Queues UndoAreaLookup to run in the background

        static void UndoAreaSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)tag;
            args.Area = new BoundingBox( marks[0], marks[1] );
            Scheduler.NewBackgroundTask( UndoAreaLookup )
                     .RunOnce( args, TimeSpan.Zero );
        }


        // Looks up the changes in BlockDB and prints a confirmation prompt. Runs on a background thread.

        static void UndoAreaLookup( SchedulerTask task ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            bool allPlayers = (args.Targets.Length == 0);
            string cmdName = (args.Not ? "UndoAreaNot" : "UndoArea");

            // prepare to look up
            string targetList;
            if( allPlayers ) {
                targetList = "EVERYONE";
            } else if( args.Not ) {
                targetList = "EVERYONE except " + args.Targets.JoinToClassyString();
            } else {
                targetList = args.Targets.JoinToClassyString();
            }
            BlockDBEntry[] changes;

            if( args.CountLimit > 0 ) {
                // count-limited lookup
                if( args.Targets.Length == 0 ) {
                    changes = args.World.BlockDB.Lookup( args.CountLimit, args.Area );
                } else {
                    changes = args.World.BlockDB.Lookup( args.CountLimit, args.Area, args.Targets, args.Not );
                }
                if( changes.Length > 0 ) {
                    Logger.Log( LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name );
                    args.Player.Confirm( BlockDBUndoConfirmCallback, args,
                                         "Undo last {0} changes made here by {1}&S?",
                                         changes.Length, targetList );
                }

            } else {
                // time-limited lookup
                if( args.Targets.Length == 0 ) {
                    changes = args.World.BlockDB.Lookup( Int32.MaxValue, args.Area, args.AgeLimit );
                } else {
                    changes = args.World.BlockDB.Lookup( Int32.MaxValue, args.Area, args.Targets, args.Not, args.AgeLimit );
                }
                if( changes.Length > 0 ) {
                    Logger.Log( LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name );
                    args.Player.Confirm( BlockDBUndoConfirmCallback, args,
                                         "Undo changes ({0}) made here by {1}&S in the last {2}?",
                                         changes.Length, targetList, args.AgeLimit.ToMiniString() );
                }
            }

            // stop if there's nothing to undo
            if( changes.Length == 0 ) {
                args.Player.Message( "{0}: Found nothing to undo.", cmdName );
            } else {
                args.Entries = changes;
            }
        }

        #endregion


        #region UndoPlayer

        static readonly CommandDescriptor CdUndoPlayer = new CommandDescriptor {
            Name = "UndoPlayer",
            Aliases = new[] { "up", "undox" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions },
            Usage = "/UndoPlayer (TimeSpan|BlockCount) PlayerName [AnotherName...]",
            Help = "Reverses changes made by a given player in the current world. " +
                   "More than one player name can be given at a time. " +
                   "Players with UndoAll permission can use '*' in place of player name to undo everyone's changes at once.",
            Handler = UndoPlayerHandler
        };

        static void UndoPlayerHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, "UndoPlayer", false );
            if( args == null ) return;
            Scheduler.NewBackgroundTask( UndoPlayerLookup )
                     .RunOnce( args, TimeSpan.Zero );
        }


        static readonly CommandDescriptor CdUndoPlayerNot = new CommandDescriptor {
            Name = "UndoPlayerNot",
            Aliases = new[] { "upn", "unp" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.UndoOthersActions, Permission.UndoAll },
            Usage = "/UndoPlayerNot (TimeSpan|BlockCount) PlayerName [AnotherName...]",
            Help = "Reverses changes made by everyone EXCEPT the given player(s). " +
                   "Applies to the whole world. " +
                   "More than one player name can be given at a time.",
            Handler = UndoPlayerNotHandler
        };

        static void UndoPlayerNotHandler( Player player, CommandReader cmd ) {
            BlockDBUndoArgs args = ParseBlockDBUndoParams( player, cmd, "UndoPlayerNot", true );
            if( args == null ) return;
            Scheduler.NewBackgroundTask( UndoPlayerLookup )
                     .RunOnce( args, TimeSpan.Zero );
        }


        // Looks up the changes in BlockDB and prints a confirmation prompt. Runs on a background thread.

        static void UndoPlayerLookup( SchedulerTask task ) {
            BlockDBUndoArgs args = (BlockDBUndoArgs)task.UserState;
            bool allPlayers = (args.Targets.Length == 0);
            string cmdName = (args.Not ? "UndoPlayerNot" : "UndoPlayer");

            // prepare to look up
            string targetList;
            if( allPlayers ) {
                targetList = "EVERYONE";
            } else if( args.Not ) {
                targetList = "EVERYONE except " + args.Targets.JoinToClassyString();
            } else {
                targetList = args.Targets.JoinToClassyString();
            }
            BlockDBEntry[] changes;

            if( args.CountLimit > 0 ) {
                // count-limited lookup
                if( args.Targets.Length == 0 ) {
                    changes = args.World.BlockDB.Lookup( args.CountLimit );
                } else {
                    changes = args.World.BlockDB.Lookup( args.CountLimit, args.Targets, args.Not );
                }
                if( changes.Length > 0 ) {
                    Logger.Log( LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name );
                    args.Player.Confirm( BlockDBUndoConfirmCallback, args,
                                         "Undo last {0} changes made by {1}&S?",
                                         changes.Length, targetList );
                }

            } else {
                // time-limited lookup
                if( args.Targets.Length == 0 ) {
                    changes = args.World.BlockDB.Lookup( Int32.MaxValue, args.AgeLimit );
                } else {
                    changes = args.World.BlockDB.Lookup( Int32.MaxValue, args.Targets, args.Not, args.AgeLimit );
                }
                if( changes.Length > 0 ) {
                    Logger.Log( LogType.UserActivity,
                                "{0}: Asked {1} to confirm undo on world {2}",
                                cmdName, args.Player.Name, args.World.Name );
                    args.Player.Confirm( BlockDBUndoConfirmCallback, args,
                                         "Undo changes ({0}) made by {1}&S in the last {2}?",
                                         changes.Length, targetList, args.AgeLimit.ToMiniString() );
                }
            }

            // stop if there's nothing to undo
            if( changes.Length == 0 ) {
                args.Player.Message( "{0}: Found nothing to undo.", cmdName );
            } else {
                args.Entries = changes;
            }
        }

        #endregion

        #endregion
    }
}