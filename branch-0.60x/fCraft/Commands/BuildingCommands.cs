// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.MapConversion;
using fCraft.Drawing;

namespace fCraft {
    /// <summary> Commands for placing specific blocks (solid, water, grass),
    /// and switching block placement modes (paint, bind). </summary>
    static class BuildingCommands {

        #region State Objects and Enums

        /// <summary> A type of drawing operation. Does not include Cut/Copy/Paste. </summary>
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


        public sealed class CopyInformation {
            public byte[, ,] Buffer;
            public int Width, Length, Height;
        }


        sealed class ReplaceArgs {
            public bool DoExclude;
            public Block[] Types;
            public Block ReplacementBlock;
        }


        sealed class PasteArgs {
            public bool DoInclude, DoExclude;
            public Block[] BlockTypes;
        }


        sealed class HollowShapeArgs {
            public Block InnerBlock;
            public Block OuterBlock;
        }

        #endregion

        public static int MaxUndoCount = 2000000;
        const int DrawStride = 16;

        const string GeneralDrawingHelp = " Use &H/cancel&S to exit draw mode. " +
                                          "Use &H/undo&S to undo the last draw operation. " +
                                          "Use &H/lock&S to cancel drawing after it started.";


        internal static void Init() {
            CommandManager.RegisterCommand( CdSolid );
            CommandManager.RegisterCommand( CdPaint );
            CommandManager.RegisterCommand( CdGrass );
            CommandManager.RegisterCommand( CdWater );
            CommandManager.RegisterCommand( CdLava );
            CommandManager.RegisterCommand( CdBind );

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

            CommandManager.RegisterCommand( CdCuboid );
            CommandManager.RegisterCommand( CdCuboidHollow );
            CommandManager.RegisterCommand( CdCuboidWireframe );
            CommandManager.RegisterCommand( CdEllipsoid );
            CommandManager.RegisterCommand( CdEllipsoidHollow );
            CommandManager.RegisterCommand( CdSphere );
            CommandManager.RegisterCommand( CdSphereHollow );
            CommandManager.RegisterCommand( CdReplace );
            CommandManager.RegisterCommand( CdReplaceNot );
            CommandManager.RegisterCommand( CdLine );

            CommandManager.RegisterCommand( CdMark );
            CommandManager.RegisterCommand( CdCancel );
            CommandManager.RegisterCommand( CdUndo );

            CommandManager.RegisterCommand( CdCopy );
            CommandManager.RegisterCommand( CdCut );
            CommandManager.RegisterCommand( CdPasteNot );
            CommandManager.RegisterCommand( CdPaste );
            CommandManager.RegisterCommand( CdMirror );
            CommandManager.RegisterCommand( CdRotate );

            CommandManager.RegisterCommand( CdRestore );

            CommandManager.RegisterCommand( CdSetBrush );
            CommandManager.RegisterCommand( CdCuboidX );
            CommandManager.RegisterCommand( CdCuboidWireframeX );
            CommandManager.RegisterCommand( CdCuboidHollowX );
            CommandManager.RegisterCommand( CdEllipsoidX );
            CommandManager.RegisterCommand( CdEllipsoidHollowX );
            CommandManager.RegisterCommand( CdLineX );
            CommandManager.RegisterCommand( CdSphereX );
            CommandManager.RegisterCommand( CdSphereHollowX );

            CommandManager.RegisterCommand( CdUndoX );
        }


        #region DrawOperations & Brushes

        static readonly CommandDescriptor CdSetBrush = new CommandDescriptor {
            Name = "brush",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "Gets or sets the current brush. Available brushes are: " +
                   "normal (default), checkered, cloudy, marbled, rainbow, random, replace, and replacenot.",
            Handler = SetBrush
        };

        static void SetBrush( Player player, Command cmd ) {
            string brushName = cmd.Next();
            if( brushName == null ) {
                player.Message( player.Brush.Description );
            } else {
                IBrushFactory brushFactory = BrushManager.GetBrushFactory( brushName );
                if( brushFactory == null ) {
                    player.Message( "Unrecognized brush \"{0}\"", brushName );
                } else {
                    IBrush newBrush = brushFactory.MakeBrush( player, cmd );
                    if( player.Brush != null ) {
                        player.Brush = newBrush;
                        player.Message( "Brush set to {0}", player.Brush.Description );
                    }
                }
            }
        }


        static readonly CommandDescriptor CdCuboidX = new CommandDescriptor {
            Name = "cx",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved cuboid, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = CuboidX
        };

        static void CuboidX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidWireframeX = new CommandDescriptor {
            Name = "cwx",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved wireframe cuboid, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = CuboidWireframeX
        };

        static void CuboidWireframeX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidWireframeDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdCuboidHollowX = new CommandDescriptor {
            Name = "chx",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved hollow cuboid, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = CuboidHollowX
        };

        static void CuboidHollowX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new CuboidHollowDrawOperation( player ) );
        }



        static readonly CommandDescriptor CdEllipsoidX = new CommandDescriptor {
            Name = "ex",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved ellipsoid, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = EllipsoidX
        };

        static void EllipsoidX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidDrawOperation( player ) );
        }


        static readonly CommandDescriptor CdLineX = new CommandDescriptor {
            Name = "linex",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved line, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = LineX
        };

        static void LineX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new LineDrawOperation( player ) );
        }


        static readonly CommandDescriptor CdSphereX = new CommandDescriptor {
            Name = "spx",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved sphere, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = SphereX
        };

        static void SphereX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new SphereDrawOperation( player ) );
        }

        static readonly CommandDescriptor CdSphereHollowX = new CommandDescriptor {
            Name = "sphx",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved hollow sphere, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = SphereHollowX
        };

        static void SphereHollowX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new SphereHollowDrawOperation( player ) );
        }


        static readonly CommandDescriptor CdEllipsoidHollowX = new CommandDescriptor {
            Name = "ehx",
            Category = CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "New and improved hollow ellipsoid, with brush support and low overhead. Work in progress, may be crashy.",
            Handler = EllipsoidHollowX
        };

        static void EllipsoidHollowX( Player player, Command cmd ) {
            DrawOperationBegin( player, cmd, new EllipsoidHollowDrawOperation( player ) );
        }



        static void DrawOperationBegin( Player player, Command cmd, DrawOperation op ) {
            op.Brush = player.Brush.MakeInstance( player, cmd, op );
            if( op.Brush == null ) return;
            player.SelectionStart( 2, DrawOperationCallback, op, Permission.Draw );
            player.MessageNow( "{0}: Click 2 blocks or use &H/mark&S to make a selection.",
                               op.Description );
        }


        static void DrawOperationCallback( Player player, Position[] marks, object tag ) {
            DrawOperation op = (DrawOperation)tag;
            if( !op.Begin( marks ) ) return;
            if( !player.CanDraw( op.BlocksTotalEstimate ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   op.Bounds.Volume );
                return;
            }
            op.Map.QueueDrawOp( op );
            player.Message( "{0}: Now processing ~{1} blocks.",
                            op.Description, op.BlocksTotalEstimate );
        }

        #endregion


        #region Block Commands

        static readonly CommandDescriptor CdSolid = new CommandDescriptor {
            Name = "solid",
            Aliases = new[] { "s" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceAdmincrete },
            Help = "Toggles the admincrete placement mode. When enabled, any stone block you place is replaced with admincrete.",
            Handler = Solid
        };

        internal static void Solid( Player player, Command cmd ) {
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
            Handler = Paint
        };

        internal static void Paint( Player player, Command cmd ) {
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
            Handler = Grass
        };

        internal static void Grass( Player player, Command cmd ) {
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
            Handler = Water
        };

        internal static void Water( Player player, Command cmd ) {
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
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceLava },
            Help = "Toggles the lava placement mode. When enabled, any red block you place is replaced with lava.",
            Handler = Lava
        };

        internal static void Lava( Player player, Command cmd ) {
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
            Handler = Bind
        };

        internal static void Bind( Player player, Command cmd ) {
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


        #region Command Descriptors

        static readonly CommandDescriptor CdCuboid = new CommandDescriptor {
            Name = "cuboid",
            Aliases = new[] { "blb", "c", "cub", "z" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/cuboid [BlockName]",
            Help = "Allows to fill a rectangular area (cuboid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Cuboid
        };

        static void Cuboid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Cuboid );
        }



        static readonly CommandDescriptor CdCuboidHollow = new CommandDescriptor {
            Name = "cubh",
            Aliases = new[] { "cuboidh", "ch", "h", "bhb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/cubh [OuterBlock [InnerBlock]]",
            Help = "Allows to box a rectangular area (cuboid) with blocks. " +
                   "If OuterBlockName is omitted, uses the block that player is holding. " +
                   "Unless InnerBlockName is specified, the inside is left untouched.",
            Handler = CuboidHollow
        };

        static void CuboidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidHollow );
        }



        static readonly CommandDescriptor CdCuboidWireframe = new CommandDescriptor {
            Name = "cubw",
            Aliases = new[] { "cuboidw", "cw", "bfb" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/cubw [BlockName]",
            Help = "Draws a wireframe box around selected area. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = CuboidWireframe
        };

        static void CuboidWireframe( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidWireframe );
        }



        static readonly CommandDescriptor CdEllipsoid = new CommandDescriptor {
            Name = "ellipsoid",
            Aliases = new[] { "e" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/ellipsoid [BlockName]",
            Help = "Fills a sphere-like (ellipsoidal) area with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Ellipsoid
        };

        static void Ellipsoid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Ellipsoid );
        }


        static readonly CommandDescriptor CdEllipsoidHollow = new CommandDescriptor {
            Name = "ellipsoidh",
            Aliases = new[] { "eh" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/ellipsoidh [OuterBlock [InnerBlock]]",
            Help = "Allows to fill a sphere-like (ellipsoidal) area with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = EllipsoidHollow
        };

        static void EllipsoidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.EllipsoidHollow );
        }


        static readonly CommandDescriptor CdSphere = new CommandDescriptor {
            Name = "sphere",
            Aliases = new[] { "sp", "spheroid" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/sphere [BlockName]",
            Help = "Fills a spherical area with blocks. " +
                   "First mark is the center of the sphere, second mark defines the radius." +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Sphere
        };

        static void Sphere( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Sphere );
        }


        static readonly CommandDescriptor CdSphereHollow = new CommandDescriptor {
            Name = "sphereh",
            Aliases = new[] { "sph", "hsphere" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/sphereh [OuterBlock [InnerBlock]]",
            Help = "Surrounds a spherical area with a shell of blocks. " +
                   "First mark is the center of the sphere, second mark defines the radius." +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = SphereHollow
        };

        static void SphereHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.SphereHollow );
        }


        static readonly CommandDescriptor CdReplace = new CommandDescriptor {
            Name = "replace",
            Aliases = new[] { "r" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            Help = "Replaces all blocks of specified type(s) in an area.",
            Handler = Replace
        };

        static void Replace( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Replace );
        }



        static readonly CommandDescriptor CdReplaceNot = new CommandDescriptor {
            Name = "replacenot",
            Aliases = new[] { "rn" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/replacenot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            Help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            Handler = ReplaceNot
        };

        static void ReplaceNot( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.ReplaceNot );
        }



        static readonly CommandDescriptor CdLine = new CommandDescriptor {
            Name = "line",
            Aliases = new[] { "ln" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Usage = "/line [BlockName]",
            Help = "Draws a line between two points with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Line
        };

        static void Line( Player player, Command cmd ) {
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

            object selectionArgs = (byte)block;
            SelectionCallback callback;

            switch( mode ) {
                case DrawMode.Cuboid:
                    callback = CuboidCallback;
                    break;

                case DrawMode.CuboidHollow:
                case DrawMode.EllipsoidHollow:
                case DrawMode.SphereHollow:
                    if( mode == DrawMode.CuboidHollow ) {
                        callback = CuboidHollowCallback;
                    } else if( mode == DrawMode.EllipsoidHollow ) {
                        callback = EllipsoidHollowCallback;
                    } else {
                        callback = SphereHollowCallback;
                    }
                    string innerBlockName = cmd.Next();
                    Block innerBlock = Block.Undefined;
                    if( innerBlockName != null ) {
                        innerBlock = Map.GetBlockByName( innerBlockName );
                        if( innerBlock == Block.Undefined ) {
                            player.Message( "{0}: Unrecognized block: {1}",
                                            mode, innerBlockName );
                        }
                    }
                    selectionArgs = new HollowShapeArgs {
                        OuterBlock = block,
                        InnerBlock = innerBlock
                    };
                    break;

                case DrawMode.CuboidWireframe:
                    callback = CuboidWireframeCallback;
                    break;

                case DrawMode.Ellipsoid:
                    callback = EllipsoidCallback;
                    break;

                case DrawMode.Sphere:
                    callback = SphereCallback;
                    break;

                case DrawMode.Replace:
                case DrawMode.ReplaceNot:

                    string affectedBlockName = cmd.Next();

                    if( affectedBlockName == null ) {
                        if( mode == DrawMode.ReplaceNot ) {
                            CdReplaceNot.PrintUsage( player );
                        } else {
                            CdReplace.PrintUsage( player );
                        }
                        return;
                    }

                    List<Block> affectedTypes = new List<Block> { block };

                    do {
                        Block affectedType = Map.GetBlockByName( affectedBlockName );
                        if( affectedType != Block.Undefined ) {
                            affectedTypes.Add( affectedType );
                        } else {
                            player.MessageNow( "{0}: Unrecognized block type: {1}", mode, affectedBlockName );
                            return;
                        }
                    } while( (affectedBlockName = cmd.Next()) != null );

                    Block[] replacedTypes = affectedTypes.Take( affectedTypes.Count - 1 ).ToArray();
                    Block replacementType = affectedTypes.Last();
                    selectionArgs = new ReplaceArgs {
                        DoExclude = (mode == DrawMode.ReplaceNot),
                        Types = replacedTypes,
                        ReplacementBlock = replacementType
                    };
                    callback = ReplaceCallback;

                    if( mode == DrawMode.ReplaceNot ) {
                        player.MessageNow( "ReplaceNot: Ready to replace everything EXCEPT ({0}) with {1}",
                                           replacedTypes.JoinToString(),
                                           replacementType );
                    } else {
                        player.MessageNow( "Replace: Ready to replace ({0}) with {1}",
                                           replacedTypes.JoinToString(),
                                           replacementType );
                    }

                    break;

                case DrawMode.Line:
                    callback = LineCallback;
                    break;

                default:
                    throw new ArgumentOutOfRangeException( "mode" );
            }

            player.SelectionStart( 2, callback, selectionArgs, Permission.Draw );

            if( block != Block.Undefined ) {
                player.MessageNow( "{0} ({1}): Click a block or use &H/mark",
                                   mode, block );
            } else {
                player.MessageNow( "{0}: Click a block or use &H/mark",
                   mode, block );
            }
        }


        #region Undo / Redo

        static readonly CommandDescriptor CdUndo = new CommandDescriptor {
            Name = "undo",
            Aliases = new[] { "redo" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = Undo
        };

        internal static void Undo( Player player, Command command ) {
            if( player.UndoBuffer.Count > 0 ) {
                // no need to set player.drawingInProgress here because this is done on the user thread
                Logger.Log( "Player {0} initiated /undo affecting {1} blocks (on world {2})", LogType.UserActivity,
                            player.Name,
                            player.UndoBuffer.Count,
                            player.World.Name );
                player.MessageNow( "Restoring {0} blocks...", player.UndoBuffer.Count );
                Queue<BlockUpdate> redoBuffer = new Queue<BlockUpdate>();
                while( player.UndoBuffer.Count > 0 ) {
                    BlockUpdate newBlock = player.UndoBuffer.Dequeue();
                    BlockUpdate oldBlock = new BlockUpdate( null, newBlock.X, newBlock.Y, newBlock.Z,
                                                            player.World.Map.GetBlock( newBlock.X, newBlock.Y, newBlock.Z ) );
                    player.World.Map.QueueUpdate( newBlock );
                    redoBuffer.Enqueue( oldBlock );
                }
                player.UndoBuffer = redoBuffer;
                redoBuffer.TrimExcess();
                player.MessageNow( "Type /undo again to reverse this command." );
                Server.RequestGC();

            } else {
                player.MessageNow( "There is currently nothing to undo." );
            }
        }

        #endregion


        #region Draw Callbacks

        static void DrawOneBlock( Player player, byte drawBlock, int x, int y, int z, ref int blocks, ref int blocksDenied, ref bool cannotUndo ) {
            if( !player.World.Map.InBounds( x, y, z ) ) return;
            byte block = player.World.Map.GetBlockByte( x, y, z );
            if( block == drawBlock ) return;

            if( player.CanPlace( x, y, z, (Block)drawBlock, false ) != CanPlaceResult.Allowed ) {
                blocksDenied++;
                return;
            }

            // this would've been an easy way to do block tracking for draw commands BUT
            // if i set "origin" to player, he will not receive the block update. I tried.
            player.World.Map.QueueUpdate( new BlockUpdate( null, x, y, z, drawBlock ) );
            Server.RaisePlayerPlacedBlockEvent( player, player.World.Map, (short)x, (short)y, (short)z, (Block)block, (Block)drawBlock, false );

            if( MaxUndoCount < 1 || blocks < MaxUndoCount ) {
                player.UndoBuffer.Enqueue( new BlockUpdate( null, x, y, z, block ) );
            } else if( !cannotUndo ) {
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


        static void DrawingFinished( Player player, string verb, int blocks, int blocksDenied ) {
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


        #region Cuboid, CuboidHollow, CuboidWireframe

        internal static void CuboidCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.GetBind( player.LastUsedBlockType );
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (ez - sz + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int z = sz; z <= ez; z++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                DrawOneBlock( player, drawBlock, x + x3, y + y3, z, ref blocks, ref blocksDenied, ref cannotUndo );
                            }
                        }
                    }
                }
            }
            DrawingFinished( player, "drawn", blocks, blocksDenied );
            Logger.Log( "{0} drew a cuboid containing {1} blocks of type {2} (on world {3} @ {4},{5},{6} - {7},{8},{9})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name,
                        sx, sy, sz,
                        ex, ey, ez );
        }


        internal static void CuboidHollowCallback( Player player, Position[] marks, object tag ) {
            HollowShapeArgs args = (HollowShapeArgs)tag;
            byte drawBlock = (byte)args.OuterBlock;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.GetBind( player.LastUsedBlockType );
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            bool fillInner = (args.InnerBlock != Block.Undefined && (ex - sx) > 1 && (ey - sy) > 1 && (ez - sz) > 1);


            int volume = (ex - sx + 1) * (ey - sy + 1) * (ez - sz + 1);
            if( !fillInner ) {
                volume -= (ex - sx - 1) * (ey - sy - 1) * (ez - sz - 1);
            }

            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    DrawOneBlock( player, drawBlock, x, y, sz, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, y, ez, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }
            for( int x = sx; x <= ex; x++ ) {
                for( int z = sz; z <= ez; z++ ) {
                    DrawOneBlock( player, drawBlock, x, sy, z, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, ey, z, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }
            for( int y = sy; y <= ey; y++ ) {
                for( int z = sz; z <= ez; z++ ) {
                    DrawOneBlock( player, drawBlock, sx, y, z, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, ex, y, z, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }

            if( fillInner ) {
                for( int x = sx + 1; x < ex; x += DrawStride ) {
                    for( int y = sy + 1; y < ey; y += DrawStride ) {
                        for( int z = sz + 1; z < ez; z++ ) {
                            for( int y3 = 0; y3 < DrawStride && y + y3 < ey; y3++ ) {
                                for( int x3 = 0; x3 < DrawStride && x + x3 < ex; x3++ ) {
                                    DrawOneBlock( player, (byte)args.InnerBlock, x + x3, y + y3, z, ref blocks, ref blocksDenied, ref cannotUndo );
                                }
                            }
                        }
                    }
                }
            }

            Logger.Log( "{0} drew a hollow cuboid containing {1} blocks of type {2} (on world {3} @ {4},{5},{6} - {7},{8},{9})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name,
                        sx, sy, sz,
                        ex, ey, ez );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void CuboidWireframeCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.GetBind( player.LastUsedBlockType );
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            // Calculate the upper limit on the volume
            int solidVolume = (ex - sx + 1) * (ey - sy + 1) * (ez - sz + 1);
            int hollowVolume = Math.Max( 0, ex - sx - 1 ) * Math.Max( 0, ey - sy - 1 ) * Math.Max( 0, ez - sz - 1 );
            int sideVolume = Math.Max( 0, ex - sx - 1 ) * Math.Max( 0, ey - sy - 1 ) * (ex != sx ? 2 : 1) +
                             Math.Max( 0, ey - sy - 1 ) * Math.Max( 0, ez - sz - 1 ) * (ey != sy ? 2 : 1) +
                             Math.Max( 0, ez - sz - 1 ) * Math.Max( 0, ex - sx - 1 ) * (ez != sz ? 2 : 1);
            int volume = solidVolume - hollowVolume - sideVolume;

            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            // Draw cuboid vertices
            DrawOneBlock( player, drawBlock, sx, sy, sz, ref blocks, ref blocksDenied, ref cannotUndo );
            if( sx != ex ) DrawOneBlock( player, drawBlock, ex, sy, sz, ref blocks, ref blocksDenied, ref cannotUndo );
            if( sy != ey ) DrawOneBlock( player, drawBlock, sx, ey, sz, ref blocks, ref blocksDenied, ref cannotUndo );
            if( sx != ex && sy != ey ) DrawOneBlock( player, drawBlock, ex, ey, sz, ref blocks, ref blocksDenied, ref cannotUndo );
            if( sz != ez ) DrawOneBlock( player, drawBlock, sx, sy, ez, ref blocks, ref blocksDenied, ref cannotUndo );
            if( sx != ex && sz != ez ) DrawOneBlock( player, drawBlock, ex, sy, ez, ref blocks, ref blocksDenied, ref cannotUndo );
            if( sy != ey && sz != ez ) DrawOneBlock( player, drawBlock, sx, ey, ez, ref blocks, ref blocksDenied, ref cannotUndo );
            if( sx != ex && sy != ey && sz != ez ) DrawOneBlock( player, drawBlock, ex, ey, ez, ref blocks, ref blocksDenied, ref cannotUndo );

            // Draw edges along the X axis
            if( ex - sx > 1 ) {
                for( int x = sx + 1; x < ex; x++ ) {
                    DrawOneBlock( player, drawBlock, x, sy, sz, ref blocks, ref blocksDenied, ref cannotUndo );
                    if( sz != ez ) DrawOneBlock( player, drawBlock, x, sy, ez, ref blocks, ref blocksDenied, ref cannotUndo );
                    if( sy != ey ) {
                        DrawOneBlock( player, drawBlock, x, ey, sz, ref blocks, ref blocksDenied, ref cannotUndo );
                        if( sz != ez ) DrawOneBlock( player, drawBlock, x, ey, ez, ref blocks, ref blocksDenied, ref cannotUndo );
                    }
                }
            }

            // Draw edges along the Y axis
            if( ey - sy > 1 ) {
                for( int y = sy + 1; y < ey; y++ ) {
                    DrawOneBlock( player, drawBlock, sx, y, sz, ref blocks, ref blocksDenied, ref cannotUndo );
                    if( sz != ez ) DrawOneBlock( player, drawBlock, sx, y, ez, ref blocks, ref blocksDenied, ref cannotUndo );
                    if( sx != ex ) {
                        DrawOneBlock( player, drawBlock, ex, y, sz, ref blocks, ref blocksDenied, ref cannotUndo );
                        if( sz != ez ) DrawOneBlock( player, drawBlock, ex, y, ez, ref blocks, ref blocksDenied, ref cannotUndo );
                    }
                }
            }

            // Draw edges along the H axis
            if( ez - sz > 1 ) {
                for( int z = sz + 1; z < ez; z++ ) {
                    DrawOneBlock( player, drawBlock, sx, sy, z, ref blocks, ref blocksDenied, ref cannotUndo );
                    if( sy != ey ) DrawOneBlock( player, drawBlock, sx, ey, z, ref blocks, ref blocksDenied, ref cannotUndo );
                    if( sx != ex ) {
                        DrawOneBlock( player, drawBlock, ex, ey, z, ref blocks, ref blocksDenied, ref cannotUndo );
                        if( sy != ey ) DrawOneBlock( player, drawBlock, ex, sy, z, ref blocks, ref blocksDenied, ref cannotUndo );
                    }
                }
            }

            Logger.Log( "{0} drew a wireframe cuboid containing {1} blocks of type {2} (on world {3} @ {4},{5},{6} - {7},{8},{9})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name,
                        sx, sy, sz,
                        ex, ey, ez );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }

        #endregion


        unsafe static void ReplaceCallback( Player player, Position[] marks, object drawArgs ) {
            ReplaceArgs args = (ReplaceArgs)drawArgs;

            byte* specialTypes = stackalloc byte[args.Types.Length];
            int specialTypeCount = args.Types.Length;
            for( int i = 0; i < args.Types.Length; i++ ) {
                specialTypes[i] = (byte)args.Types[i];
            }

            bool doExclude = args.DoExclude;

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (ez - sz + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                    player.Info.Rank.DrawLimit,
                                    volume );
                return;
            }

            player.UndoBuffer.Clear();

            bool cannotUndo = false;
            int blocks = 0, blocksDenied = 0;
            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int z = sz; z <= ez; z++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                byte block = player.World.Map.GetBlockByte( x + x3, y + y3, z );

                                bool skip = !args.DoExclude;
                                for( int i = 0; i < specialTypeCount; i++ ) {
                                    if( block == specialTypes[i] ) {
                                        skip = args.DoExclude;
                                        break;
                                    }
                                }
                                if( skip ) continue;

                                if( player.CanPlace( x + x3, y + y3, z, args.ReplacementBlock, false ) != CanPlaceResult.Allowed ) {
                                    blocksDenied++;
                                    continue;
                                }
                                player.World.Map.QueueUpdate( new BlockUpdate( null, x + x3, y + y3, z, args.ReplacementBlock ) );
                                Server.RaisePlayerPlacedBlockEvent( player, player.World.Map, (short)(x + x3), (short)(y + y3), (short)z,
                                                                    (Block)block, args.ReplacementBlock, false );

                                if( MaxUndoCount < 1 || blocks < MaxUndoCount ) {
                                    player.UndoBuffer.Enqueue( new BlockUpdate( null, x + x3, y + y3, z, block ) );
                                } else if( !cannotUndo ) {
                                    player.UndoBuffer.Clear();
                                    player.UndoBuffer.TrimExcess();
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


            Logger.Log( "{0} replaced {1} blocks {2} ({3}) with {4} (on world {5} @ {6},{7},{8} - {9},{10},{11})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (doExclude ? "except" : "of"),
                        args.Types.JoinToString(),
                        args.ReplacementBlock,
                        player.World.Name,
                        sx, sy, sz,
                        ex, ey, ez );

            DrawingFinished( player, "replaced", blocks, blocksDenied );
        }


        #region Ellipsoid, Hollow Ellipsoid, Sphere, HollowSphere

        internal static void EllipsoidCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.GetBind( player.LastUsedBlockType );
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            // find axis lengths
            double rx = (ex - sx + 1) / 2d;
            double ry = (ey - sy + 1) / 2d;
            double rh = (ez - sz + 1) / 2d;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rh2 = 1 / (rh * rh);

            // find center points
            double cx = (ex + sx) / 2d;
            double cy = (ey + sy) / 2d;
            double ch = (ez + sz) / 2d;


            int volume = (int)(4 / 3d * Math.PI * rx * ry * rh);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int z = sz; z <= ez; z++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                // get relative coordinates
                                double dx = (x + x3 - cx);
                                double dy = (y + y3 - cy);
                                double dh = (z - ch);

                                // test if it's inside ellipse
                                if( (dx * dx) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 <= 1 ) {
                                    DrawOneBlock( player, drawBlock, x + x3, y + y3, z, ref blocks, ref blocksDenied, ref cannotUndo );
                                }
                            }
                        }
                    }
                }
            }
            Logger.Log( "{0} drew an ellipsoid containing {1} blocks of type {2} (on world {3} @ {4},{5},{6} - {7},{8},{9})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name,
                        sx, sy, sz,
                        ex, ey, ez );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void EllipsoidHollowCallback( Player player, Position[] marks, object tag ) {

            HollowShapeArgs args = (HollowShapeArgs)tag;
            byte drawBlock = (byte)args.OuterBlock;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.GetBind( player.LastUsedBlockType );
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            bool fillInner = (args.InnerBlock != Block.Undefined && (ex - sx) > 1 && (ey - sy) > 1 && (ez - sz) > 1);

            // find axis lengths
            double rx = (ex - sx + 1) / 2d;
            double ry = (ey - sy + 1) / 2d;
            double rz = (ez - sz + 1) / 2d;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rz2 = 1 / (rz * rz);

            // find center points
            double cx = (ex + sx) / 2d;
            double cy = (ey + sy) / 2d;
            double cz = (ez + sz) / 2d;

            int volume;
            if( fillInner ) {
                volume = (int)(4 / 3d * Math.PI * rx * ry * rz);
            } else {
                // rougher estimation than the non-hollow form, a voxelized surface is a bit funky
                volume = (int)(4 / 3d * Math.PI * ((rx + .5) * (ry + .5) * (rz + .5) - (rx - .5) * (ry - .5) * (rz - .5)) * 0.85);
            }

            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int z = sz; z <= ez; z++ ) {

                        double dx = (x - cx);
                        double dy = (y - cy);
                        double dz = (z - cz);

                        if( (dx * dx) * rx2 + (dy * dy) * ry2 + (dz * dz) * rz2 > 1 ) continue;

                        // we touched the surface
                        // keep drilling until we hit an internal block
                        do {
                            DrawOneBlock( player, drawBlock, x, y, z, ref blocks, ref blocksDenied, ref cannotUndo );
                            DrawOneBlock( player, drawBlock, x, y, (int)(cz - dz), ref blocks, ref blocksDenied, ref cannotUndo );
                            dz = (++z - cz);
                        } while( z <= (int)cz &&
                                 ((dx + 1) * (dx + 1) * rx2 + (dy * dy) * ry2 + (dz * dz) * rz2 > 1 ||
                                  (dx - 1) * (dx - 1) * rx2 + (dy * dy) * ry2 + (dz * dz) * rz2 > 1 ||
                                  (dx * dx) * rx2 + (dy + 1) * (dy + 1) * ry2 + (dz * dz) * rz2 > 1 ||
                                  (dx * dx) * rx2 + (dy - 1) * (dy - 1) * ry2 + (dz * dz) * rz2 > 1 ||
                                  (dx * dx) * rx2 + (dy * dy) * ry2 + (dz + 1) * (dz + 1) * rz2 > 1 ||
                                  (dx * dx) * rx2 + (dy * dy) * ry2 + (dz - 1) * (dz - 1) * rz2 > 1)
                            );
                        if( fillInner ) {
                            for( ; z <= (int)(cz - dz); z++ ) {
                                DrawOneBlock( player, (byte)args.InnerBlock, x, y, z, ref blocks, ref blocksDenied, ref cannotUndo );
                            }
                        }
                        break;
                    }
                }
            }
            Logger.Log( "{0} drew a hollow ellipsoid containing {1} blocks of type {2} (on world {3} @ {4},{5},{6} - {7},{8},{9})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name,
                        sx, sy, sz,
                        ex, ey, ez );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void SphereCallback( Player player, Position[] marks, object tag ) {
            double radius = Math.Sqrt( (marks[0].X - marks[1].X) * (marks[0].X - marks[1].X) +
                                       (marks[0].Y - marks[1].Y) * (marks[0].Y - marks[1].Y) +
                                       (marks[0].Z - marks[1].Z) * (marks[0].Z - marks[1].Z) );

            marks[1].X = (short)Math.Round( marks[0].X - radius );
            marks[1].Y = (short)Math.Round( marks[0].Y - radius );
            marks[1].Z = (short)Math.Round( marks[0].Z - radius );

            marks[0].X = (short)Math.Round( marks[0].X + radius );
            marks[0].Y = (short)Math.Round( marks[0].Y + radius );
            marks[0].Z = (short)Math.Round( marks[0].Z + radius );

            EllipsoidCallback( player, marks, tag );
        }


        internal static void SphereHollowCallback( Player player, Position[] marks, object tag ) {
            double radius = Math.Sqrt( (marks[0].X - marks[1].X) * (marks[0].X - marks[1].X) +
                                       (marks[0].Y - marks[1].Y) * (marks[0].Y - marks[1].Y) +
                                       (marks[0].Z - marks[1].Z) * (marks[0].Z - marks[1].Z) );

            marks[1].X = (short)Math.Round( marks[0].X - radius );
            marks[1].Y = (short)Math.Round( marks[0].Y - radius );
            marks[1].Z = (short)Math.Round( marks[0].Z - radius );

            marks[0].X = (short)Math.Round( marks[0].X + radius );
            marks[0].Y = (short)Math.Round( marks[0].Y + radius );
            marks[0].Z = (short)Math.Round( marks[0].Z + radius );

            EllipsoidHollowCallback( player, marks, tag );
        }


        #endregion


        static void LineCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.GetBind( player.LastUsedBlockType );
            }

            int volume = Math.Max( Math.Max( Math.Abs( marks[0].X - marks[1].X ) + 1,
                                            Math.Abs( marks[0].Y - marks[1].Y ) + 1 ),
                                            Math.Abs( marks[0].Z - marks[1].Z ) + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                    player.Info.Rank.DrawLimit,
                                    volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            // LINE CODE

            int x1 = marks[0].X,
                y1 = marks[0].Y,
                z1 = marks[0].Z,
                x2 = marks[1].X,
                y2 = marks[1].Y,
                z2 = marks[1].Z;
            int i, err1, err2;
            int[] pixel = new int[3];
            pixel[0] = x1;
            pixel[1] = y1;
            pixel[2] = z1;
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dz = z2 - z1;
            int xInc = (dx < 0) ? -1 : 1;
            int l = Math.Abs( dx );
            int yInc = (dy < 0) ? -1 : 1;
            int m = Math.Abs( dy );
            int zInc = (dz < 0) ? -1 : 1;
            int n = Math.Abs( dz );
            int dx2 = l << 1;
            int dy2 = m << 1;
            int dz2 = n << 1;

            DrawOneBlock( player, drawBlock, x2, y2, z2, ref blocks, ref blocksDenied, ref cannotUndo );

            if( (l >= m) && (l >= n) ) {
                err1 = dy2 - l;
                err2 = dz2 - l;
                for( i = 0; i < l; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err1 > 0 ) {
                        pixel[1] += yInc;
                        err1 -= dx2;
                    }
                    if( err2 > 0 ) {
                        pixel[2] += zInc;
                        err2 -= dx2;
                    }
                    err1 += dy2;
                    err2 += dz2;
                    pixel[0] += xInc;
                }

            } else if( (m >= l) && (m >= n) ) {
                err1 = dx2 - m;
                err2 = dz2 - m;
                for( i = 0; i < m; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err1 > 0 ) {
                        pixel[0] += xInc;
                        err1 -= dy2;
                    }
                    if( err2 > 0 ) {
                        pixel[2] += zInc;
                        err2 -= dy2;
                    }
                    err1 += dx2;
                    err2 += dz2;
                    pixel[1] += yInc;
                }

            } else {
                err1 = dy2 - n;
                err2 = dx2 - n;
                for( i = 0; i < n; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err1 > 0 ) {
                        pixel[1] += yInc;
                        err1 -= dz2;
                    }
                    if( err2 > 0 ) {
                        pixel[0] += xInc;
                        err2 -= dz2;
                    }
                    err1 += dy2;
                    err2 += dx2;
                    pixel[2] += zInc;
                }
            }

            // END LINE CODE
            Logger.Log( "{0} drew a line containing {1} blocks of type {2} (on world {3} @ {4},{5},{6} - {7},{8},{9})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name,
                        x1, y1, z1,
                        x2, y2, z2 );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }

        #endregion


        #region Copy and Paste

        static readonly CommandDescriptor CdCopy = new CommandDescriptor {
            Name = "copy",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Copy blocks for pasting. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            Handler = Copy
        };

        internal static void Copy( Player player, Command cmd ) {
            player.SelectionStart( 2, CopyCallback, null, CdCopy.Permissions );
            player.MessageNow( "Copy: Place a block or type /mark to use your location." );
        }

        internal static void CopyCallback( Player player, Position[] marks, object tag ) {
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
            CopyInformation copyInfo = new CopyInformation {
                Width = marks[1].X - marks[0].X,
                Length = marks[1].Y - marks[0].Y,
                Height = marks[1].Z - marks[0].Z,
                Buffer = new byte[ex - sx + 1, ey - sy + 1, ez - sz + 1]
            };

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int z = sz; z <= ez; z++ ) {
                        copyInfo.Buffer[x - sx, y - sy, z - sz] = player.World.Map.GetBlockByte( x, y, z );
                    }
                }
            }

            player.CopyInformation = copyInfo;
            player.MessageNow( "{0} blocks were copied. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.Height > 0 ? "bottom" : "top"),
                               (copyInfo.Length > 0 ? "south" : "north"),
                               (copyInfo.Width > 0 ? "west" : "east") );

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
            Handler = Cut
        };

        internal static void Cut( Player player, Command cmd ) {
            Block fillBlock = Block.Air;
            string fillBlockName = cmd.Next();
            if( fillBlockName != null ) {
                fillBlock = Map.GetBlockByName( fillBlockName );
                if( fillBlock == Block.Undefined ) {
                    player.Message( "Cut: Unknown block type \"{0}\"", fillBlockName );
                    return;
                }
            }
            player.SelectionStart( 2, CutCallback, fillBlock, CdCut.Permissions );
            player.MessageNow( "Cut: Place a block or type /mark to use your location." );
        }

        internal static void CutCallback( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sz = Math.Min( marks[0].Z, marks[1].Z );
            int ez = Math.Max( marks[0].Z, marks[1].Z );

            byte fillType = (byte)tag;

            int volume = (ex - sx + 1) * (ey - sy + 1) * (ez - sz + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, volume ) );
                return;
            }

            // remember dimensions and orientation
            CopyInformation copyInfo = new CopyInformation {
                Width = marks[1].X - marks[0].X,
                Length = marks[1].Y - marks[0].Y,
                Height = marks[1].Z - marks[0].Z,
                Buffer = new byte[ex - sx + 1, ey - sy + 1, ez - sz + 1]
            };

            player.UndoBuffer.Clear();
            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int z = sz; z <= ez; z++ ) {
                        copyInfo.Buffer[x - sx, y - sy, z - sz] = player.World.Map.GetBlockByte( x, y, z );
                        DrawOneBlock( player, fillType, x, y, z, ref blocks, ref blocksDenied, ref cannotUndo );
                    }
                }
            }

            player.CopyInformation = copyInfo;
            player.MessageNow( "{0} blocks were cut. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.Height > 0 ? "bottom" : "top"),
                               (copyInfo.Length > 0 ? "south" : "north"),
                               (copyInfo.Width > 0 ? "west" : "east") );

            Logger.Log( "{0} cut {1} blocks from world {2} (@{3},{4},{5} - {6},{7},{8}), replacing {9} blocks with {10}.", LogType.UserActivity,
                        player.Name, volume,
                        player.World.Name,
                        sx, sy, sz,
                        ex, ey, ez,
                        blocks, (Block)fillType );

            player.UndoBuffer.TrimExcess();
            Server.RequestGC();
        }



        static readonly CommandDescriptor CdPasteNot = new CommandDescriptor {
            Name = "pastenot",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Paste previously copied blocks, excluding specified block type(s). " +
                   "Used together with &H/copy&S command. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from. ",
            Usage = "/pastenot ExcludedBlock [AnotherOne [AndAnother]]",
            Handler = PasteNot
        };

        internal static void PasteNot( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            PasteArgs args;

            List<Block> excludedTypes = new List<Block>();
            string excludedBlockName = cmd.Next();
            if( excludedBlockName != null ) {
                do {
                    Block excludedType = Map.GetBlockByName( excludedBlockName );
                    if( excludedType != Block.Undefined ) {
                        excludedTypes.Add( excludedType );
                    } else {
                        player.MessageNow( "PasteNot: Unrecognized block type: {0}", excludedBlockName );
                        return;
                    }
                } while( (excludedBlockName = cmd.Next()) != null );
            }

            if( excludedTypes.Count > 0 ) {
                args = new PasteArgs {
                    DoExclude = true,
                    BlockTypes = excludedTypes.ToArray()
                };
                player.MessageNow( "Ready to paste all EXCEPT {0}",
                                   excludedTypes.JoinToString() );
            } else {
                player.MessageNow( "PasteNot: Please specify block(s) to exclude." );
                return;
            }

            player.SelectionStart( 1, PasteCallback, args, CdPasteNot.Permissions );

            player.MessageNow( "PasteNot: Place a block or type /mark to use your location." );
        }


        static readonly CommandDescriptor CdPaste = new CommandDescriptor {
            Name = "paste",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Pastes previously copied blocks. Used together with &H/copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            Usage = "/paste [IncludedBlock [AnotherOne [AndAnother]]]",
            Handler = Paste
        };

        internal static void Paste( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            List<Block> includedTypes = new List<Block>();
            string includedBlockName = cmd.Next();
            if( includedBlockName != null ) {
                do {
                    Block includedType = Map.GetBlockByName( includedBlockName );
                    if( includedType != Block.Undefined ) {
                        includedTypes.Add( includedType );
                    } else {
                        player.MessageNow( "PasteNot: Unrecognized block type: {0}", includedBlockName );
                        return;
                    }
                } while( (includedBlockName = cmd.Next()) != null );
            }

            PasteArgs args;
            if( includedTypes.Count > 0 ) {
                args = new PasteArgs {
                    DoInclude = true,
                    BlockTypes = includedTypes.ToArray()
                };
                player.MessageNow( "Ready to paste ONLY {0}", includedTypes.JoinToString() );
            } else {
                args = new PasteArgs {
                    BlockTypes = new Block[0]
                };
            }

            player.SelectionStart( 1, PasteCallback, args, CdPaste.Permissions );

            player.MessageNow( "Paste: Place a block or type /mark to use your location." );
        }


        unsafe internal static void PasteCallback( Player player, Position[] marks, object tag ) {
            CopyInformation info = player.CopyInformation;

            PasteArgs args = (PasteArgs)tag;
            byte* specialTypes = stackalloc byte[args.BlockTypes.Length];
            int specialTypeCount = args.BlockTypes.Length;
            for( int i = 0; i < args.BlockTypes.Length; i++ ) {
                specialTypes[i] = (byte)args.BlockTypes[i];
            }
            Map map = player.World.Map;

            Position mark2 = new Position {
                X = (short)(marks[0].X + info.Width),
                Y = (short)(marks[0].Y + info.Length),
                Z = (short)(marks[0].Z + info.Height)
            };

            BoundingBox bounds = new BoundingBox( marks[0], mark2 );

            int pasteVolume = bounds.GetIntersection( map.Bounds ).Volume;
            if( !player.CanDraw( pasteVolume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   pasteVolume );
                return;
            }

            if( bounds.XMin < 0 || bounds.XMax > map.Width - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( bounds.YMin < 0 || bounds.YMax > map.Length - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( bounds.ZMin < 0 || bounds.ZMax > map.Height - 1 ) {
                player.MessageNow( "Warning: Not enough room vertically, paste cut off." );
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = bounds.XMin; x <= bounds.XMax; x += DrawStride ) {
                for( int y = bounds.YMin; y <= bounds.YMax; y += DrawStride ) {
                    for( int z = bounds.ZMin; z <= bounds.ZMax; z++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= bounds.YMax; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= bounds.XMax; x3++ ) {
                                byte block = info.Buffer[x + x3 - bounds.XMin, y + y3 - bounds.YMin, z - bounds.ZMin];

                                if( args.DoInclude ) {
                                    bool skip = true;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = false;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                } else if( args.DoExclude ) {
                                    bool skip = false;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = true;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                }
                                DrawOneBlock( player, block, x + x3, y + y3, z, ref blocks, ref blocksDenied, ref cannotUndo );
                            }
                        }
                    }
                }
            }

            Logger.Log( "{0} pasted {1} blocks to world {2} (@ {3},{4},{5} - {6},{7},{8}).", LogType.UserActivity,
                        player.Name, blocks, player.World.Name,
                        bounds.XMin, bounds.YMin, bounds.ZMin,
                        bounds.XMax, bounds.YMax, bounds.ZMax );
            DrawingFinished( player, "pasted", blocks, blocksDenied );
        }


        static readonly CommandDescriptor CdMirror = new CommandDescriptor {
            Name = "mirror",
            Aliases = new[] { "flip" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/copymirror X Y&S.",
            Usage = "/mirror [X] [Y] [Z]",
            Handler = Mirror
        };

        internal static void Mirror( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
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
            byte[, ,] buffer = player.CopyInformation.Buffer;

            if( flipX ) {
                int left = 0;
                int right = buffer.GetLength( 0 ) - 1;
                while( left < right ) {
                    for( int y = player.CopyInformation.Buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                        for( int z = player.CopyInformation.Buffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                            block = buffer[left, y, z];
                            buffer[left, y, z] = buffer[right, y, z];
                            buffer[right, y, z] = block;
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
                    for( int x = player.CopyInformation.Buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int z = player.CopyInformation.Buffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                            block = buffer[x, left, z];
                            buffer[x, left, z] = buffer[x, right, z];
                            buffer[x, right, z] = block;
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
                    for( int x = player.CopyInformation.Buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int y = player.CopyInformation.Buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
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


        static readonly CommandDescriptor CdRotate = new CommandDescriptor {
            Name = "rotate",
            Aliases = new[] { "spin" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            Usage = "/rotate (-90|90|180|270) (X|Y|Z)",
            Handler = Rotate
        };

        internal static void Rotate( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
                player.MessageNow( "Nothing to rotate! Copy something first." );
                return;
            }

            int degrees;
            if( !cmd.NextInt( out degrees ) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270) ) {
                CdRotate.PrintUsage( player );
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
                        CdRotate.PrintUsage( player );
                        return;
                }
            }


            // allocate the new buffer
            byte[, ,] oldBuffer = player.CopyInformation.Buffer;
            byte[, ,] newBuffer;

            if( degrees == 180 ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 2 )];

            } else if( axis == RotationAxis.X ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 )];
                int dimY = player.CopyInformation.Length;
                player.CopyInformation.Length = player.CopyInformation.Height;
                player.CopyInformation.Height = dimY;

            } else if( axis == RotationAxis.Y ) {
                newBuffer = new byte[oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 )];
                int dimX = player.CopyInformation.Width;
                player.CopyInformation.Width = player.CopyInformation.Height;
                player.CopyInformation.Height = dimX;

            } else {
                newBuffer = new byte[oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 )];
                int dimY = player.CopyInformation.Length;
                player.CopyInformation.Length = player.CopyInformation.Width;
                player.CopyInformation.Width = dimY;
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

            player.Message( "Rotated copy by {0} degrees around {1} axis.", degrees, axis );
            player.CopyInformation.Buffer = newBuffer;
        }

        #endregion


        #region Restore

        static readonly CommandDescriptor CdRestore = new CommandDescriptor {
            Name = "restore",
            Category = CommandCategory.World,
            Permissions = new[] { Permission.CopyAndPaste },
            Usage = "/restore FileName",
            Help = "Selectively restores/pastes part of mapfile into the current world.",
            Handler = Restore
        };


        internal static void Restore( Player player, Command cmd ) {
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


        static void RestoreCallback( Player player, Position[] marks, object tag ) {
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
            player.UndoBuffer.Clear();

            for( int x = selection.XMin; x <= selection.XMax; x++ ) {
                for( int y = selection.YMin; y <= selection.YMax; y++ ) {
                    for( int z = selection.ZMin; z <= selection.ZMax; z++ ) {
                        DrawOneBlock( player, map.GetBlockByte( x, y, z ), x, y, z,
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

            DrawingFinished( player, "restored", blocksDrawn, blocksSkipped );
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
            Handler = Mark
        };

        internal static void Mark( Player player, Command command ) {
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
                player.SelectionAddMark( pos, true );
            } else {
                player.MessageNow( "Cannot mark - no selection in progress." );
            }
        }



        static readonly CommandDescriptor CdCancel = new CommandDescriptor {
            Name = "cancel",
            Category = CommandCategory.Building,
            Help = "Cancels current selection (for drawing or zoning) operation, for instance if you misclicked on the first block. " +
                   "If you wish to stop a drawing in-progress, use &H/lock&S instead.",
            Handler = Cancel
        };

        internal static void Cancel( Player player, Command command ) {
            if( player.IsMakingSelection ) {
                player.SelectionCancel();
                player.MessageNow( "Selection cancelled." );
            } else {
                player.MessageNow( "There is currently nothing to cancel." );
            }
        }

        #endregion



        static readonly CommandDescriptor CdUndoX = new CommandDescriptor {
            Name = "undox",
            Category = CommandCategory.Moderation | CommandCategory.Building,
            IsHidden = true,
            Permissions = new[] { Permission.UndoOthersActions },
            Usage = "/undox PlayerName [TimeSpan|BlockCount]",
            Help = "Enables or disabled BlockDB on a given world.",
            Handler = UndoX
        };

        static void UndoX( Player player, Command cmd ) {
            if( !BlockDB.IsEnabled ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            World world = player.World;
            if( !world.IsBlockTracked ) {
                player.Message( "&WBlockDB is disabled in this world." );
                return;
            }

            string name = cmd.Next();
            string range = cmd.Next();
            if( name == null || range == null ) {
                CdUndoX.PrintUsage( player );
                return;
            }

            PlayerInfo target = PlayerDB.FindPlayerInfoExact( name );
            if( target == null ) {
                PlayerInfo[] targets = PlayerDB.FindPlayers( name );
                if( targets.Length == 0 ) {
                    player.MessageNoPlayer( name );
                    return;

                } else if( targets.Length > 0 ) {
                    Array.Sort( targets, new PlayerInfoComparer( player ) );
                    player.MessageManyMatches( "player", targets.Take( 25 ).ToArray() );
                    return;
                }
                target = targets[0];
            }

            if( !player.Can( Permission.UndoOthersActions, target.Rank ) ) {
                player.Message( "You may only undo actions of players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.UndoOthersActions ).ClassyName );
                player.Message( "Player {0}&S is ranked {1}", target.ClassyName, target.Rank.ClassyName );
                return;
            }

            int count;
            TimeSpan span;
            BlockDBEntry[] changes;
            if( Int32.TryParse( range, out count ) ) {
                player.Message( "Searching for changes made by {0}&s...", target.ClassyName );
                changes = world.LookupBlockInfo( target, count );
                if( changes.Length > 0 && !cmd.IsConfirmed ) {
                    player.AskForConfirmation( cmd, "Undo last {0} changes made by player {1}&S?",
                                               changes.Length, target.ClassyName );
                    return;
                }

            } else if( DateTimeUtil.TryParseMiniTimespan( range, out span ) ) {
                player.Message( "Searching for changes made by {0}&s...", target.ClassyName );
                changes = world.LookupBlockInfo( target, span );
                if( changes.Length > 0 && !cmd.IsConfirmed ) {
                    player.AskForConfirmation( cmd, "Undo changes ({0}) made by {1}&S in the last {2}?",
                                               changes.Length, target.ClassyName, span.ToMiniString() );
                    return;
                }

            } else {
                CdUndoX.PrintUsage( player );
                return;
            }

            if( changes.Length == 0 ) {
                player.Message( "UndoX: Found nothing to undo." );
                return;
            }

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;
            player.UndoBuffer.Clear();
            for( int i = 0; i < changes.Length; i++ ) {
                DrawOneBlock( player, (byte)changes[i].OldBlock,
                              changes[i].X, changes[i].Y, changes[i].Z,
                              ref blocks, ref blocksDenied, ref cannotUndo );
            }

            Logger.Log( "{0} undid {1} blocks changed by player {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        target.Name,
                        world.Name );

            DrawingFinished( player, "Undone", blocks, blocksDenied );
        }
    }


    public enum RotationAxis {
        X, Y, Z
    }
}