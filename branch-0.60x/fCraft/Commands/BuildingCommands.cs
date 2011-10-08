// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Drawing;
using fCraft.MapConversion;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Commands for placing specific blocks (solid, water, grass),
    /// and switching block placement modes (paint, bind). </summary>
    static class BuildingCommands {

        public static int MaxUndoCount = 2000000;

        const string GeneralDrawingHelp = " Use &H/cancel&S to exit draw mode. " +
                                          "Use &H/undo&S to stop and undo the last draw operation.";

        internal static void Init() {
            CommandManager.RegisterCommand( CdBind );
            CommandManager.RegisterCommand( CdGrass );
            CommandManager.RegisterCommand( CdLava );
            CommandManager.RegisterCommand( CdPaint );
            CommandManager.RegisterCommand( CdSolid );
            CommandManager.RegisterCommand( CdWater );

            CdCuboid.Help += GeneralDrawingHelp;
            CdCuboidHollow.Help += GeneralDrawingHelp;
            CdCuboidWireframe.Help += GeneralDrawingHelp;
            CdEllipsoid.Help += GeneralDrawingHelp;
            CdEllipsoidHollow.Help += GeneralDrawingHelp;
            CdSphere.Help += GeneralDrawingHelp;
            CdSphereHollow.Help += GeneralDrawingHelp;
            CdLine.Help += GeneralDrawingHelp;
            CdReplace.Help += GeneralDrawingHelp;
            CdReplaceNot.Help += GeneralDrawingHelp;
            CdCut.Help += GeneralDrawingHelp;
            CdPasteNot.Help += GeneralDrawingHelp;
            CdPaste.Help += GeneralDrawingHelp;
            CdRestore.Help += GeneralDrawingHelp;

            CommandManager.RegisterCommand( CdReplace );
            CommandManager.RegisterCommand( CdReplaceNot );

            CommandManager.RegisterCommand( CdCancel );
            CommandManager.RegisterCommand( CdMark );
            CommandManager.RegisterCommand( CdUndo );

            CommandManager.RegisterCommand( CdCopySlot );
            CommandManager.RegisterCommand( CdCopy );
            CommandManager.RegisterCommand( CdCut );
            CommandManager.RegisterCommand( CdPaste );
            CommandManager.RegisterCommand( CdPasteNot );
            CommandManager.RegisterCommand( CdMirror );
            CommandManager.RegisterCommand( CdRotate );

            CommandManager.RegisterCommand( CdRestore );

            CommandManager.RegisterCommand( CdCuboid );
            CommandManager.RegisterCommand( CdCuboidWireframe );
            CommandManager.RegisterCommand( CdCuboidHollow );
            CommandManager.RegisterCommand( CdEllipsoid );
            CommandManager.RegisterCommand( CdEllipsoidHollow );
            CommandManager.RegisterCommand( CdLine );
            CommandManager.RegisterCommand( CdSphere );
            CommandManager.RegisterCommand( CdSphereHollow );
            CommandManager.RegisterCommand( CdTorus );

            //CommandManager.RegisterCommand( CdTree );

            CommandManager.RegisterCommand( CdUndoArea );
            CommandManager.RegisterCommand( CdUndoPlayer );
        }


        #region DrawOperations & Brushes

        static readonly CommandDescriptor CdCuboid = new CommandDescriptor {
            Name = "cuboid",
            Aliases = new[] { "blb", "c", "z" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Fills a rectangular area (cuboid) with blocks.",
            Handler = CuboidHandler
        };

        static void CuboidHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidWireframe = new CommandDescriptor {
            Name = "cubw",
            Aliases = new[] { "cuboidw", "cw", "bfb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Draws a wireframe box (a frame) around the selected rectangular area.",
            Handler = CuboidWireframeHandler
        };

        static void CuboidWireframeHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidWireframeDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidHollow = new CommandDescriptor {
            Name = "cubh",
            Aliases = new[] { "cuboidh", "ch", "h", "bhb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Surrounds the selected rectangular area with a box of blocks. " +
                   "Unless two blocks are specified, leaves the inside untouched.",
            Handler = CuboidHollowHandler
        };

        static void CuboidHollowHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdEllipsoid = new CommandDescriptor {
            Name = "ellipsoid",
            Aliases = new[] { "e" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Fills an ellipsoid-shaped area (elongated sphere) with blocks.",
            Handler = EllipsoidHandler
        };

        static void EllipsoidHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdEllipsoidHollow = new CommandDescriptor {
            Name = "ellipsoidh",
            Aliases = new[] { "eh" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Surrounds the selected an ellipsoid-shaped area (elongated sphere) with a shell of blocks.",
            Handler = EllipsoidHollowHandler
        };

        static void EllipsoidHollowHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdSphere = new CommandDescriptor {
            Name = "sphere",
            Aliases = new[] { "sp", "spheroid" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "Fills a spherical area with blocks. " +
                   "The first mark denotes the CENTER of the sphere, and " +
                   "distance to the second mark denotes the radius.",
            Handler = SphereHandler
        };

        static void SphereHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new SphereDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdSphereHollow = new CommandDescriptor {
            Name = "sphereh",
            Aliases = new[] { "sph", "hsphere" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "Surrounds a spherical area with a shell of blocks. " +
                   "The first mark denotes the CENTER of the sphere, and " +
                   "distance to the second mark denotes the radius.",
            Handler = SphereHollowHandler
        };

        static void SphereHollowHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new SphereHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdLine = new CommandDescriptor {
            Name = "line",
            Aliases = new[] { "ln" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Draws a continuous line between two points with blocks. " +
                   "Marks to not need to be aligned.",
            Handler = LineHandler
        };

        static void LineHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new LineDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdTorus = new CommandDescriptor {
            Name = "torus",
            Aliases = new[] { "donut", "bagel" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "EXPERIMENTAL: Draws a horizontally-oriented torus. The first mark denotes the CENTER of the torus, horizontal " +
                   "distance to the second mark denotes the ring radius, and the vertical distance to the second mark denotes the " +
                   "tube radius",
            Handler = TorusHandler
        };

        static void TorusHandler( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new TorusDrawOperation( player ) );
        }



        static void DrawOperationBegin( Player player, Command cmd, DrawOperation op ) {
            IBrushInstance brush = player.Brush.MakeInstance( player, cmd, op );
            if( brush == null ) return;
            op.Brush = brush;
            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.Message( "{0}: Click 2 blocks or use &H/mark&S to make a selection.",
                            op.DescriptionWithBrush );
        }


        static void DrawOperationCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Begin( marks ) ) return;
            if( !player.CanDraw( op.BlocksTotalEstimate ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   op.Bounds.Volume );
                op.Cancel();
                return;
            }
            op.Map.QueueDrawOp( op );
            player.Message( "{0}: Now processing ~{1} blocks.",
                            op.DescriptionWithBrush, op.BlocksTotalEstimate );
        }

        #endregion


        #region Block Commands

        static readonly CommandDescriptor CdSolid = new CommandDescriptor {
            Name = "solid",
            Aliases = new[] { "s" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceAdmincrete },
            Help = "Toggles the admincrete placement mode. When enabled, any stone block you place is replaced with admincrete.",
            Handler = SolidHandler
        };

        static void SolidHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Stone ) == Block.Admincrete ) {
                player.ResetBind( Block.Stone );
                player.Message( "Solid: OFF" );
            } else {
                player.Bind( Block.Stone, Block.Admincrete );
                player.Message( "Solid: ON. Stone blocks are replaced with admincrete." );
            }
        }



        static readonly CommandDescriptor CdPaint = new CommandDescriptor {
            Name = "paint",
            Aliases = new[] { "p" },
            Category = CommandCategory.Building,
            Help = "When paint mode is on, any block you delete will be replaced with the block you are holding. " +
                   "Paint command toggles this behavior on and off.",
            Handler = PaintHandler
        };

        static void PaintHandler( Player player, Command cmd ) {
            player.IsPainting = !player.IsPainting;
            if( player.IsPainting ) {
                player.Message( "Paint mode: ON" );
            } else {
                player.Message( "Paint mode: OFF" );
            }
        }



        static readonly CommandDescriptor CdGrass = new CommandDescriptor {
            Name = "grass",
            Aliases = new[] { "g" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceGrass },
            Help = "Toggles the grass placement mode. When enabled, any dirt block you place is replaced with a grass block.",
            Handler = GrassHandler
        };

        static void GrassHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Dirt ) == Block.Grass ) {
                player.ResetBind( Block.Dirt );
                player.Message( "Grass: OFF" );
            } else {
                player.Bind( Block.Dirt, Block.Grass );
                player.Message( "Grass: ON. Dirt blocks are replaced with grass." );
            }
        }



        static readonly CommandDescriptor CdWater = new CommandDescriptor {
            Name = "water",
            Aliases = new[] { "w" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceWater },
            Help = "Toggles the water placement mode. When enabled, any blue or cyan block you place is replaced with water.",
            Handler = WaterHandler
        };

        static void WaterHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Aqua ) == Block.Water ||
                player.GetBind( Block.Cyan ) == Block.Water ||
                player.GetBind( Block.Blue ) == Block.Water ) {
                player.ResetBind( Block.Aqua, Block.Cyan, Block.Blue );
                player.Message( "Water: OFF" );
            } else {
                player.Bind( Block.Aqua, Block.Water );
                player.Bind( Block.Cyan, Block.Water );
                player.Bind( Block.Blue, Block.Water );
                player.Message( "Water: ON. Blue blocks are replaced with water." );
            }
        }



        static readonly CommandDescriptor CdLava = new CommandDescriptor {
            Name = "lava",
            Aliases = new[] { "l" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceLava },
            Help = "Toggles the lava placement mode. When enabled, any red block you place is replaced with lava.",
            Handler = LavaHandler
        };

        static void LavaHandler( Player player, Command cmd ) {
            if( player.GetBind( Block.Red ) == Block.Lava ) {
                player.ResetBind( Block.Red );
                player.Message( "Lava: OFF" );
            } else {
                player.Bind( Block.Red, Block.Lava );
                player.Message( "Lava: ON. Red blocks are replaced with lava." );
            }
        }



        static readonly CommandDescriptor CdBind = new CommandDescriptor {
            Name = "bind",
            Aliases = new[] { "b" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            Help = "Assigns one blocktype to another. " +
                   "Allows to build blocktypes that are not normally buildable directly: admincrete, lava, water, grass, double step. " +
                   "Calling &H/bind BlockType&S without second parameter resets the binding. If used with no params, ALL bindings are reset.",
            Usage = "/bind OriginalBlockType ReplacementBlockType",
            Handler = BindHandler
        };

        static void BindHandler( Player player, Command cmd ) {
            string originalBlockName = cmd.Next();
            if( originalBlockName == null ) {
                player.Message( "All bindings have been reset." );
                player.ResetAllBinds();
                return;
            }
            Block originalBlock = Map.GetBlockByName( originalBlockName );
            if( originalBlock == Block.Undefined ) {
                player.Message( "Bind: Unrecognized block name: {0}", originalBlockName );
                return;
            }

            string replacementBlockName = cmd.Next();
            if( replacementBlockName == null ) {
                if( player.GetBind( originalBlock ) != originalBlock ) {
                    player.Message( "{0} is no longer bound to {1}",
                                    originalBlock,
                                    player.GetBind( originalBlock ) );
                    player.ResetBind( originalBlock );
                } else {
                    player.Message( "{0} is not bound to anything.",
                                    originalBlock );
                }
                return;
            }

            Block replacementBlock = Map.GetBlockByName( replacementBlockName );
            if( replacementBlock == Block.Undefined ) {
                player.Message( "Bind: Unrecognized block name: {0}", replacementBlockName );
            } else {
                Permission permission = Permission.Build;
                switch( replacementBlock ) {
                    case Block.Grass:
                        permission = Permission.PlaceGrass;
                        break;
                    case Block.Admincrete:
                        permission = Permission.PlaceAdmincrete;
                        break;
                    case Block.Water:
                        permission = Permission.PlaceWater;
                        break;
                    case Block.Lava:
                        permission = Permission.PlaceLava;
                        break;
                }
                if( player.Can( permission ) ) {
                    player.Bind( originalBlock, replacementBlock );
                    player.Message( "{0} is now replaced with {1}", originalBlock, replacementBlock );
                } else {
                    player.Message( "&WYou do not have {0} permission.", permission );
                }
            }
        }

        #endregion


        static void DrawOneBlock( [NotNull] Player player, byte drawBlock, int x, int y, int z,
                                  BlockChangeContext context, ref int blocks, ref int blocksDenied, ref bool cannotUndo ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( !player.World.Map.InBounds( x, y, z ) ) return;
            byte block = player.World.Map.GetBlockByte( x, y, z );
            if( block == drawBlock ) return;

            if( player.CanPlace( x, y, z, (Block)drawBlock, context ) != CanPlaceResult.Allowed ) {
                blocksDenied++;
                return;
            }

            // this would've been an easy way to do block tracking for draw commands BUT
            // if i set "origin" to player, he will not receive the block update. I tried.
            player.World.Map.QueueUpdate( new BlockUpdate( null, x, y, z, drawBlock ) );
            Player.RaisePlayerPlacedBlockEvent( player, player.World.Map, (short)x, (short)y, (short)z, (Block)block, (Block)drawBlock, context );

            if( MaxUndoCount < 1 || blocks < MaxUndoCount ) {
                player.UndoBuffer.Enqueue( new BlockUpdate( null, x, y, z, block ) );
            } else if( !cannotUndo ) {
                player.LastDrawOp = null;
                player.UndoBuffer.Clear();
                player.UndoBuffer.TrimExcess();
                player.MessageNow( "NOTE: This draw command is too massive to undo." );
                if( player.Can( Permission.ManageWorlds ) ) {
                    player.MessageNow( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                }
                cannotUndo = true;
            }
            blocks++;
        }


        static void DrawingFinished( [NotNull] Player player, string verb, int blocks, int blocksDenied ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( blocks == 0 ) {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "No blocks could be {0} due to permission issues.", verb.ToLower() );
                } else {
                    player.MessageNow( "No blocks were {0}.", verb.ToLower() );
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
                player.Info.ProcessDrawCommand( blocks );
                player.UndoBuffer.TrimExcess();
                Server.RequestGC();
            }
        }


        #region Replace

        static readonly CommandDescriptor CdReplace = new CommandDescriptor {
            Name = "replace",
            Aliases = new[] { "r" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            Help = "Replaces all blocks of specified type(s) in an area.",
            Handler = ReplaceHandler
        };

        static void ReplaceHandler( Player player, Command cmd ) {
            var replaceBrush = ReplaceBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null ) return;

            CuboidDrawOperation op = new CuboidDrawOperation( player );
            IBrushInstance brush = replaceBrush.MakeInstance( player, cmd, op );
            if( brush == null ) return;
            op.Brush = brush;

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.MessageNow( "{0}: Click 2 blocks or use &H/mark&S to make a selection.",
                               op.Brush.InstanceDescription );
        }



        static readonly CommandDescriptor CdReplaceNot = new CommandDescriptor {
            Name = "replacenot",
            Aliases = new[] { "rn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/replacenot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            Help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            Handler = ReplaceNotHandler
        };

        static void ReplaceNotHandler( Player player, Command cmd ) {
            var replaceBrush = ReplaceNotBrushFactory.Instance.MakeBrush( player, cmd );
            if( replaceBrush == null ) return;

            CuboidDrawOperation op = new CuboidDrawOperation( player );
            IBrushInstance brush = replaceBrush.MakeInstance( player, cmd, op );
            if( brush == null ) return;
            op.Brush = brush;

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.MessageNow( "{0}: Click 2 blocks or use &H/mark&S to make a selection.",
                               op.Brush.InstanceDescription );
        }

        #endregion


        #region Undo

        const BlockChangeContext UndoContext = BlockChangeContext.Drawn | BlockChangeContext.UndoneSelf;

        static readonly CommandDescriptor CdUndo = new CommandDescriptor {
            Name = "undo",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = UndoHandler
        };

        static void UndoHandler( Player player, Command command ) {
            if( command.HasNext ) {
                player.Message( "Undo command takes no parameters. Did you mean to do &H/UndoPlayer&S or &H/UndoArea&S?" );
                return;
            }

            Queue<BlockUpdate> oldBuffer = player.UndoBuffer;
            if( oldBuffer.Count > 0 ) {
                string msg = "Undo: ";
                DrawOperation lastDrawOp = player.LastDrawOp;
                if( lastDrawOp != null && !lastDrawOp.IsDone ) {
                    lastDrawOp.Cancel();
                    msg = String.Format( "Cancelled {0} (was {1}% done). ",
                                         lastDrawOp.DescriptionWithBrush,
                                         lastDrawOp.PercentDone );
                }
                // no need to set player.drawingInProgress here because this is done on the user thread
                Logger.Log( "Player {0} initiated /undo affecting {1} blocks (on world {2})", LogType.UserActivity,
                            player.Name,
                            player.UndoBuffer.Count,
                            player.World.Name );
                msg += String.Format( "Restoring ~{0} blocks. Type &H/undo&S again to reverse.",
                                      player.UndoBuffer.Count );
                player.MessageNow( msg );
                player.UndoBuffer = new Queue<BlockUpdate>();
                int blocks = 0, blocksDenied = 0;
                bool cannotUndo = false;
                while( oldBuffer.Count > 0 ) {
                    BlockUpdate changeToUndo = oldBuffer.Dequeue();
                    DrawOneBlock( player, changeToUndo.BlockType, changeToUndo.X, changeToUndo.Y, changeToUndo.Z, UndoContext,
                                  ref blocks, ref blocksDenied, ref cannotUndo );
                }
                DrawingFinished( player, "Undone", blocks, blocksDenied );

            } else {
                player.MessageNow( "There is currently nothing to undo." );
            }
        }

        #endregion


        #region Copy and Paste

        static readonly CommandDescriptor CdCopySlot = new CommandDescriptor {
            Name = "copyslot",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Usage = "/copyslot [#]",
            Help = "Selects a slot for copying to / pasting from. The maximum number of slots is limited per-rank.",
            Handler = CopySlotHandler
        };

        static void CopySlotHandler( Player player, Command cmd ) {
            int slotNumber;
            if( cmd.NextInt( out slotNumber ) ) {
                if( slotNumber < 1 || slotNumber > player.Info.Rank.CopySlots ) {
                    player.Message( "CopySlot: Select a number between 1 and {0}", player.Info.Rank.CopySlots );
                } else {
                    player.CopySlot = slotNumber - 1;
                    CopyInformation info = player.GetCopyInformation();
                    if( info == null ) {
                        player.Message( "Selected copy slot {0} (unused).", slotNumber );
                    } else {
                        player.Message( "Selected copy slot {0}: {1} blocks from {2}, {3} old.",
                                        slotNumber, info.Buffer.Length,
                                        info.OriginWorld, DateTime.UtcNow.Subtract( info.CopyTime ).ToMiniString() );
                    }
                }
            } else {
                CopyInformation[] slots = player.CopyInformation;
                player.Message( "Using {0} of {1} slots. Selected slot: {2}",
                                slots.Count( info => info != null ), player.Info.Rank.CopySlots, player.CopySlot + 1 );
                for( int i = 0; i < slots.Length; i++ ) {
                    if( slots[i] != null ) {
                        player.Message( "  {0}: {1} blocks from {2}, {3} old",
                                        i + 1, slots[i].Buffer.Length,
                                        slots[i].OriginWorld, DateTime.UtcNow.Subtract( slots[i].CopyTime ).ToMiniString() );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdCopy = new CommandDescriptor {
            Name = "copy",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Copy blocks for pasting. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            Handler = CopyHandler
        };

        static void CopyHandler( Player player, Command cmd ) {
            player.SelectionStart( 2, CopyCallback, null, CdCopy.Permissions );
            player.MessageNow( "Copy: Place a block or type /mark to use your location." );
        }


        static void CopyCallback( Player player, Vector3I[] marks, object tag ) {
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (ez - sz + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, volume ) );
                return;
            }

            // remember dimensions and orientation
            CopyInformation copyInfo = new CopyInformation( marks[0], marks[1] );

            Map map = player.World.Map;
            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int z = sz; z <= ez; z++ ) {
                        copyInfo.Buffer[x - sx, y - sy, z - sz] = map.GetBlockByte( x, y, z );
                    }
                }
            }

            copyInfo.OriginWorld = player.World.Name;
            copyInfo.CopyTime = DateTime.UtcNow;
            player.SetCopyInformation( copyInfo );

            player.MessageNow( "{0} blocks copied into slot #{1}. You can now &H/paste",
                               volume, player.CopySlot + 1 );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.Orientation.X == 1 ? "bottom" : "top"),
                               (copyInfo.Orientation.Y == 1 ? "south" : "north"),
                               (copyInfo.Orientation.Z == 1 ? "east" : "west") );

            Logger.Log( "{0} copied {1} blocks from {2}.", LogType.UserActivity,
                        player.Name, volume, player.World.Name );
        }



        static readonly CommandDescriptor CdCut = new CommandDescriptor {
            Name = "cut",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Copies and removes blocks for pasting. Unless a different block type is specified, the area is filled with air. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/cut&S from.",
            Usage = "/cut [FillBlock]",
            Handler = CutHandler
        };

        static void CutHandler( Player player, Command cmd ) {
            Block fillBlock = Block.Air;
            if( cmd.HasNext ) {
                fillBlock = cmd.NextBlock( player );
                if( fillBlock == Block.Undefined ) return;
            }

            CutDrawOperation op = new CutDrawOperation( player ) {
                Brush = new NormalBrush( fillBlock, Block.Undefined )
            };

            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            if( fillBlock != Block.Air ) {
                player.Message( "Cut/{0}: Click 2 blocks or use &H/mark&S to make a selection.",
                                fillBlock );
            } else {
                player.Message( "Cut: Click 2 blocks or use &H/mark&S to make a selection." );
            }
        }


        static readonly CommandDescriptor CdMirror = new CommandDescriptor {
            Name = "mirror",
            Aliases = new[] { "flip" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/copymirror X Y",
            Usage = "/mirror [X] [Y] [Z]",
            Handler = MirrorHandler
        };

        static void MirrorHandler( Player player, Command cmd ) {
            CopyInformation info = player.GetCopyInformation();
            if( info == null ) {
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
                CdMirror.PrintUsage( player );
                return;
            }

            byte block;

            if( flipX ) {
                int left = 0;
                int right = info.Dimensions.X - 1;
                while( left < right ) {
                    for( int y = info.Dimensions.Y - 1; y >= 0; y-- ) {
                        for( int z = info.Dimensions.Z - 1; z >= 0; z-- ) {
                            block = info.Buffer[left, y, z];
                            info.Buffer[left, y, z] = info.Buffer[right, y, z];
                            info.Buffer[right, y, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipY ) {
                int left = 0;
                int right = info.Dimensions.Y - 1;
                while( left < right ) {
                    for( int x = info.Dimensions.X - 1; x >= 0; x-- ) {
                        for( int z = info.Dimensions.Z - 1; z >= 0; z-- ) {
                            block = info.Buffer[x, left, z];
                            info.Buffer[x, left, z] = info.Buffer[x, right, z];
                            info.Buffer[x, right, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipH ) {
                int left = 0;
                int right = info.Dimensions.Z - 1;
                while( left < right ) {
                    for( int x = info.Dimensions.X - 1; x >= 0; x-- ) {
                        for( int y = info.Dimensions.Y - 1; y >= 0; y-- ) {
                            block = info.Buffer[x, y, left];
                            info.Buffer[x, y, left] = info.Buffer[x, y, right];
                            info.Buffer[x, y, right] = block;
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



        static readonly CommandDescriptor CdRotate = new CommandDescriptor {
            Name = "rotate",
            Aliases = new[] { "spin" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            Usage = "/rotate (-90|90|180|270) (X|Y|Z)",
            Handler = RotateHandler
        };

        static void RotateHandler( Player player, Command cmd ) {
            CopyInformation info = player.GetCopyInformation();
            if( info == null ) {
                player.MessageNow( "Nothing to rotate! Copy something first." );
                return;
            }

            int degrees;
            if( !cmd.NextInt( out degrees ) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270) ) {
                CdRotate.PrintUsage( player );
                return;
            }

            string axisName = cmd.Next();
            Axis axis = Axis.Z;
            if( axisName != null ) {
                switch( axisName.ToLower() ) {
                    case "x":
                        axis = Axis.X;
                        break;
                    case "y":
                        axis = Axis.Y;
                        break;
                    case "z":
                    case "h":
                        axis = Axis.Z;
                        break;
                    default:
                        CdRotate.PrintUsage( player );
                        return;
                }
            }


            // allocate the new buffer
            byte[, ,] oldBuffer = info.Buffer;
            byte[, ,] newBuffer;

            if( degrees == 180 ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 2 )];

            } else if( axis == Axis.X ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 )];

            } else if( axis == Axis.Y ) {
                newBuffer = new byte[oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 )];

            } else { // axis == Axis.Z
                newBuffer = new byte[oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 )];
            }

            // construct the rotation matrix
            int[,] matrix = new[,]{
                {1,0,0},
                {0,1,0},
                {0,0,1}
            };

            int a, b;
            switch( axis ) {
                case Axis.X:
                    a = 1;
                    b = 2;
                    break;
                case Axis.Y:
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
            for( int x = oldBuffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = oldBuffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = oldBuffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        int nx = (matrix[0, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[0, 0] > 0 ? x : 0)) +
                                 (matrix[0, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[0, 1] > 0 ? y : 0)) +
                                 (matrix[0, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[0, 2] > 0 ? z : 0));
                        int ny = (matrix[1, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[1, 0] > 0 ? x : 0)) +
                                 (matrix[1, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[1, 1] > 0 ? y : 0)) +
                                 (matrix[1, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[1, 2] > 0 ? z : 0));
                        int nz = (matrix[2, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[2, 0] > 0 ? x : 0)) +
                                 (matrix[2, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[2, 1] > 0 ? y : 0)) +
                                 (matrix[2, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[2, 2] > 0 ? z : 0));
                        newBuffer[nx, ny, nz] = oldBuffer[x, y, z];
                    }
                }
            }

            player.Message( "Rotated copy (slot {0}) by {1} degrees around {2} axis.",
                            info.Slot, degrees, axis );
            info.Buffer = newBuffer;
        }




        static readonly CommandDescriptor CdPaste = new CommandDescriptor {
            Name = "paste",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "EXPERIMENTAL. Pastes previously copied blocks. Used together with &H/copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Alignment semantics are... complicated.",
            Usage = "/paste [IncludedBlock [AnotherOne etc]]",
            Handler = PasteHandler
        };

        static void PasteHandler( Player player, Command cmd ) {
            PasteDrawOperation op = new PasteDrawOperation( player, false );
            if( !op.ReadParams( cmd ) ) return;
            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            player.MessageNow( "{0}: Click 2 blocks or use &H/mark&S to make a selection.",
                               op.Description );
        }


        static readonly CommandDescriptor CdPasteNot = new CommandDescriptor {
            Name = "pastenot",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "EXPERIMENTAL. Pastes previously copied blocks, except the given block type(s). " +
                    "Used together with &H/copy&S command. " +
                   "Alignment semantics are... complicated.",
            Usage = "/PasteNot ExcludedBlock [AnotherOne etc]",
            Handler = PasteNotHandler
        };

        static void PasteNotHandler( Player player, Command cmd ) {
            PasteDrawOperation op = new PasteDrawOperation( player, true );
            if( !op.ReadParams( cmd ) ) return;
            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw, Permission.CopyAndPaste );
            player.MessageNow( "{0}: Click 2 blocks or use &H/mark&S to make a selection.",
                               op.Description );
        }

        #endregion


        #region Restore

        const BlockChangeContext RestoreContext = BlockChangeContext.Drawn | BlockChangeContext.Restored;


        static readonly CommandDescriptor CdRestore = new CommandDescriptor {
            Name = "restore",
            Category = CommandCategory.World,
            Permissions = new[] {
                Permission.Draw,
                Permission.DrawAdvanced,
                Permission.CopyAndPaste,
                Permission.ManageWorlds
            },
            Usage = "/restore FileName",
            Help = "Selectively restores/pastes part of mapfile into the current world.",
            Handler = RestoreHandler
        };

        static void RestoreHandler( Player player, Command cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                CdRestore.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if( fullFileName == null ) return;

            Map map;
            if( !MapUtility.TryLoad( fullFileName, out map ) ) {
                player.Message( "Could not load the given map file ({0})", fileName );
                return;
            }

            Map playerMap = player.World.Map;
            if( playerMap.Width != map.Width || playerMap.Length != map.Length || playerMap.Height != map.Height ) {
                player.Message( "Mapfile dimensions must match your current world's dimensions ({0}x{1}x{2})",
                                playerMap.Width,
                                playerMap.Length,
                                playerMap.Height );
                return;
            }

            map.Metadata["fCraft.Temp", "FileName"] = fullFileName;
            player.SelectionStart( 2, RestoreCallback, map, CdRestore.Permissions );
            player.MessageNow( "Restore: Select the area to restore. To mark a corner, place/click a block or type &H/mark" );
        }


        static void RestoreCallback( Player player, Vector3I[] marks, object tag ) {
            BoundingBox selection = new BoundingBox( marks[0], marks[1] );
            Map map = (Map)tag;

            if( !player.CanDraw( selection.Volume ) ) {
                player.MessageNow( "You are only allowed to restore up to {0} blocks at a time. This would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   selection.Volume );
                return;
            }

            int blocksDrawn = 0,
                blocksSkipped = 0;
            bool cannotUndo = false;
            player.LastDrawOp = null;
            player.UndoBuffer.Clear();

            for( int x = selection.XMin; x <= selection.XMax; x++ ) {
                for( int y = selection.YMin; y <= selection.YMax; y++ ) {
                    for( int z = selection.ZMin; z <= selection.ZMax; z++ ) {
                        DrawOneBlock( player, map.GetBlockByte( x, y, z ), x, y, z, RestoreContext,
                                                       ref blocksDrawn, ref blocksSkipped, ref cannotUndo );
                    }
                }
            }

            Logger.Log( "{0} restored {1} blocks on world {2} (@{3},{4},{5} - {6},{7},{8}) from file {9}.", LogType.UserActivity,
                        player.Name, blocksDrawn,
                        player.World.Name,
                        selection.XMin, selection.YMin, selection.ZMin,
                        selection.XMax, selection.YMax, selection.ZMax,
                        map.Metadata["fCraft.Temp", "FileName"] );

            DrawingFinished( player, "Restored", blocksDrawn, blocksSkipped );
        }

        #endregion


        #region Tree

        static readonly CommandDescriptor CdTree = new CommandDescriptor {
            Name = "Tree",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Usage = "/Tree Shape Height",
            Help = "Plants a tree of given shape and height. Available shapes: Normal, Bamboo, Palm, Round, Cone, Rainforest, Mangrove.",
            Handler = TreeHandler
        };

        static void TreeHandler( Player player, Command cmd ) {
            string shapeName = cmd.Next();
            int height;
            Forester.TreeShape shape;

            // that's one ugly if statement... does the job though.
            if( shapeName == null ||
                !cmd.NextInt( out height ) ||
                !EnumUtil.TryParse( shapeName, out shape, true ) ||
                shape == Forester.TreeShape.Stickly ||
                shape == Forester.TreeShape.Procedural ) {

                CdTree.PrintUsage( player );
                return;
            }

            if( height < 2 || height > 1024 ) {
                player.Message( "Tree height must be between 2 and 1024 blocks." );
                return;
            }

            Map map = player.World.Map;

            ForesterArgs args = new ForesterArgs {
                Height = height,
                Shape = shape,
                Map = map,
                Rand = new Random()
            };

            player.SelectionStart( 1, TreeCallback, args, CdTree.Permissions );
        }


        static void TreeCallback( Player player, Vector3I[] marks, object tag ) {
            ForesterArgs args = (ForesterArgs)tag;
            int blocksPlaced = 0, blocksDenied = 0;
            bool cannotUndo = false;
            args.BlockPlacing +=
                ( sender, e ) =>
                DrawOneBlock( player, (byte)e.Block, e.Coordinate.X, e.Coordinate.Y, e.Coordinate.Z,
                              BlockChangeContext.Drawn,
                              ref blocksPlaced, ref blocksDenied, ref cannotUndo );
            Forester.Plant( args, marks[0] );
            DrawingFinished( player, "planted", blocksPlaced, blocksDenied );
        }

        #endregion


        #region Mark, Cancel

        static readonly CommandDescriptor CdMark = new CommandDescriptor {
            Name = "mark",
            Aliases = new[] { "m" },
            Category = CommandCategory.Building,
            Usage = "/mark&S or &H/mark X Y H",
            Help = "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                   "If three numbers are given, those coordinates are used instead.",
            Handler = MarkHandler
        };

        static void MarkHandler( Player player, Command command ) {
            int x, y, z;
            Position pos;
            if( command.NextInt( out x ) && command.NextInt( out y ) && command.NextInt( out z ) ) {
                pos = new Position( x, y, z );
            } else {
                pos = new Position( (player.Position.X - 1) / 32,
                                    (player.Position.Y - 1) / 32,
                                    (player.Position.Z - 1) / 32 );
            }
            pos.X = (short)Math.Min( player.World.Map.Width - 1, Math.Max( 0, (int)pos.X ) );
            pos.Y = (short)Math.Min( player.World.Map.Length - 1, Math.Max( 0, (int)pos.Y ) );
            pos.Z = (short)Math.Min( player.World.Map.Height - 1, Math.Max( 0, (int)pos.Z ) );

            if( player.SelectionMarksExpected > 0 ) {
                player.SelectionAddMark( pos.ToVector3I(), true );
            } else {
                player.MessageNow( "Cannot mark - no selection in progress." );
            }
        }



        static readonly CommandDescriptor CdCancel = new CommandDescriptor {
            Name = "cancel",
            Category = CommandCategory.Building,
            Help = "Cancels current selection (for drawing or zoning) operation, for instance if you misclicked on the first block. " +
                   "If you wish to stop a drawing in-progress, use &H/lock&S instead.",
            Handler = CancelHandler
        };

        static void CancelHandler( Player player, Command command ) {
            if( player.IsMakingSelection ) {
                player.SelectionCancel();
                player.MessageNow( "Selection cancelled." );
            } else {
                player.MessageNow( "There is currently nothing to cancel." );
            }
        }

        #endregion


        #region UndoPlayer and UndoArea

        struct UndoAreaCountArgs {
            public PlayerInfo Target;
            public World World;
            public int MaxBlocks;
            public BoundingBox Area;
        }

        struct UndoAreaTimeArgs {
            public PlayerInfo Target;
            public World World;
            public TimeSpan Time;
            public BoundingBox Area;
        }

        static readonly CommandDescriptor CdUndoArea = new CommandDescriptor {
            Name = "UndoArea",
            Aliases = new[] { "ua" },
            Category = CommandCategory.Moderation | CommandCategory.Building,
            Permissions = new[] { Permission.UndoOthersActions },
            Usage = "/UndoArea PlayerName [TimeSpan|BlockCount]",
            Help = "Reverses changes made by a given player in the current world.",
            Handler = UndoAreaHandler
        };

        static void UndoAreaHandler( Player player, Command cmd ) {
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            World world = player.World;
            if( !world.BlockDB.IsEnabled ) {
                player.Message( "&WBlockDB is disabled in this world." );
                return;
            }

            string name = cmd.Next();
            string range = cmd.Next();
            if( name == null || range == null ) {
                CdUndoArea.PrintUsage( player );
                return;
            }

            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, name );
            if( target == null ) return;

            if( player.Info != target && !player.Can( Permission.UndoOthersActions, target.Rank ) ) {
                player.Message( "You may only undo actions of players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.UndoOthersActions ).ClassyName );
                player.Message( "Player {0}&S is ranked {1}", target.ClassyName, target.Rank.ClassyName );
                return;
            }

            int count;
            TimeSpan span;
            if( Int32.TryParse( range, out count ) ) {
                UndoAreaCountArgs args = new UndoAreaCountArgs {
                    Target = target,
                    World = player.World,
                    MaxBlocks = count
                };
                player.SelectionStart( 2, UndoAreaCountSelectionCallback, args, Permission.UndoOthersActions );

            } else if( range.TryParseMiniTimespan( out span ) ) {
                UndoAreaTimeArgs args = new UndoAreaTimeArgs {
                    Target = target,
                    Time = span,
                    World = player.World
                };
                player.SelectionStart( 2, UndoAreaTimeSelectionCallback, args, Permission.UndoOthersActions );

            } else {
                CdUndoArea.PrintUsage( player );
                return;
            }

            player.MessageNow( "UndoPlayer: Click 2 blocks or use &H/mark&S to make a selection." );
        }


        static void UndoAreaCountSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            UndoAreaCountArgs args = (UndoAreaCountArgs)tag;
            args.World = player.World;
            args.Area = new BoundingBox( marks[0], marks[1] );
            BlockDBEntry[] changes = args.World.BlockDB.Lookup( args.Target, args.Area, args.MaxBlocks );
            if( changes.Length > 0 ) {
                player.Confirm( UndoAreaCountConfirmCallback, args, "Undo last {0} changes made by player {1}&S in this area?",
                                changes.Length, args.Target.ClassyName );
            } else {
                player.Message( "UndoArea: Nothing to undo in this area." );
            }
        }


        static void UndoAreaTimeSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            UndoAreaTimeArgs args = (UndoAreaTimeArgs)tag;
            args.World = player.World;
            args.Area = new BoundingBox( marks[0], marks[1] );
            BlockDBEntry[] changes = args.World.BlockDB.Lookup( args.Target, args.Area, args.Time );
            if( changes.Length > 0 ) {
                player.Confirm( UndoAreaTimeConfirmCallback, args, "Undo changes ({0}) made by {1}&S in this area in the last {2}?",
                                changes.Length, args.Target.ClassyName, args.Time.ToMiniString() );
            } else {
                player.Message( "UndoArea: Nothing to undo in this area." );
            }
        }


        static void UndoAreaCountConfirmCallback( Player player, object tag, bool fromConsole ) {
            UndoAreaCountArgs args = (UndoAreaCountArgs)tag;
            BlockDBEntry[] changes = args.World.BlockDB.Lookup( args.Target, args.Area, args.MaxBlocks );

            BlockChangeContext context = BlockChangeContext.Drawn;
            if( player.Info == args.Target ) {
                context |= BlockChangeContext.UndoneSelf;
            } else {
                context |= BlockChangeContext.UndoneOther;
            }

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;
            player.LastDrawOp = null;
            player.UndoBuffer.Clear();

            for( int i = 0; i < changes.Length; i++ ) {
                DrawOneBlock( player, (byte)changes[i].OldBlock,
                              changes[i].X, changes[i].Y, changes[i].Z, context,
                              ref blocks, ref blocksDenied, ref cannotUndo );
            }

            Logger.Log( "{0} undid {1} blocks changed by player {2} (in a selection  on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        args.Target.Name,
                        args.World.Name );

            DrawingFinished( player, "UndoArea'd", blocks, blocksDenied );
        }


        static void UndoAreaTimeConfirmCallback( Player player, object tag, bool fromConsole ) {
            UndoAreaTimeArgs args = (UndoAreaTimeArgs)tag;
            BlockDBEntry[] changes = args.World.BlockDB.Lookup( args.Target, args.Area, args.Time );

            BlockChangeContext context = BlockChangeContext.Drawn;
            if( player.Info == args.Target ) {
                context |= BlockChangeContext.UndoneSelf;
            } else {
                context |= BlockChangeContext.UndoneOther;
            }

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;
            player.LastDrawOp = null;
            player.UndoBuffer.Clear();

            for( int i = 0; i < changes.Length; i++ ) {
                DrawOneBlock( player, (byte)changes[i].OldBlock,
                              changes[i].X, changes[i].Y, changes[i].Z, context,
                              ref blocks, ref blocksDenied, ref cannotUndo );
            }

            Logger.Log( "{0} undid {1} blocks changed by player {2} (in a selection on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        args.Target.Name,
                        args.World.Name );

            DrawingFinished( player, "UndoArea'd", blocks, blocksDenied );
        }



        static readonly CommandDescriptor CdUndoPlayer = new CommandDescriptor {
            Name = "undoplayer",
            Aliases = new[] { "up", "undox" },
            Category = CommandCategory.Moderation | CommandCategory.Building,
            Permissions = new[] { Permission.UndoOthersActions },
            Usage = "/UndoPlayer PlayerName [TimeSpan|BlockCount]",
            Help = "Reverses changes made by a given player in the current world.",
            Handler = UndoPlayerHandler
        };

        static void UndoPlayerHandler( Player player, Command cmd ) {
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            World world = player.World;
            if( !world.BlockDB.IsEnabled ) {
                player.Message( "&WBlockDB is disabled in this world." );
                return;
            }

            string name = cmd.Next();
            string range = cmd.Next();
            if( name == null || range == null ) {
                CdUndoPlayer.PrintUsage( player );
                return;
            }

            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, name );
            if( target == null ) return;

            if( player.Info != target && !player.Can( Permission.UndoOthersActions, target.Rank ) ) {
                player.Message( "You may only undo actions of players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.UndoOthersActions ).ClassyName );
                player.Message( "Player {0}&S is ranked {1}", target.ClassyName, target.Rank.ClassyName );
                return;
            }

            int count;
            TimeSpan span;
            BlockDBEntry[] changes;
            if( Int32.TryParse( range, out count ) ) {
                if( !cmd.IsConfirmed ) {
                    player.Message( "Searching for last {0} changes made by {1}&s...",
                                    count, target.ClassyName );
                }
                changes = world.BlockDB.Lookup( target, count );
                if( changes.Length > 0 && !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Undo last {0} changes made by player {1}&S?",
                                    changes.Length, target.ClassyName );
                    return;
                }

            } else if( range.TryParseMiniTimespan( out span ) ) {
                if( !cmd.IsConfirmed ) {
                    player.Message( "Searching for changes made by {0}&s in the last {1}...",
                                    target.ClassyName, span.ToMiniString() );
                }
                changes = world.BlockDB.Lookup( target, span );
                if( changes.Length > 0 && !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Undo changes ({0}) made by {1}&S in the last {2}?",
                                    changes.Length, target.ClassyName, span.ToMiniString() );
                    return;
                }

            } else {
                CdUndoPlayer.PrintUsage( player );
                return;
            }

            if( changes.Length == 0 ) {
                player.Message( "UndoPlayer: Found nothing to undo." );
                return;
            }

            BlockChangeContext context = BlockChangeContext.Drawn;
            if( player.Info == target ) {
                context |= BlockChangeContext.UndoneSelf;
            } else {
                context |= BlockChangeContext.UndoneOther;
            }

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;
            player.LastDrawOp = null;
            player.UndoBuffer.Clear();
            for( int i = 0; i < changes.Length; i++ ) {
                DrawOneBlock( player, (byte)changes[i].OldBlock,
                              changes[i].X, changes[i].Y, changes[i].Z, context,
                              ref blocks, ref blocksDenied, ref cannotUndo );
            }

            Logger.Log( "{0} undid {1} blocks changed by player {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        target.Name,
                        world.Name );

            DrawingFinished( player, "UndoPlayer'ed", blocks, blocksDenied );
        }

        #endregion
    }
}