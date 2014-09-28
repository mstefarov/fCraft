// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Linq;
using fCraft.Drawing;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Commands for placing specific blocks (solid, water, grass),
    /// switching block placement modes (paint, bind),
    /// and draw command support commands. </summary>
    internal static class BuildingCommands {
        internal static void Init() {
            CommandManager.RegisterCommand(CdBind);
            CommandManager.RegisterCommand(CdGrass);
            CommandManager.RegisterCommand(CdLava);
            CommandManager.RegisterCommand(CdWater);
            CommandManager.RegisterCommand(CdSolid);
            CommandManager.RegisterCommand(CdPaint);

            CommandManager.RegisterCommand(CdCancel);
            CommandManager.RegisterCommand(CdMark);
            CommandManager.RegisterCommand(CdDoNotMark);
            CommandManager.RegisterCommand(CdUndo);
            CommandManager.RegisterCommand(CdRedo);

            CommandManager.RegisterCommand(CdCopySlot);
            CommandManager.RegisterCommand(CdCopy);
            CommandManager.RegisterCommand(CdMirror);
            CommandManager.RegisterCommand(CdRotate);

            CommandManager.RegisterCommand(CdStatic);
        }

        #region Block Commands

        static readonly CommandDescriptor CdSolid = new CommandDescriptor {
            Name = "Solid",
            Aliases = new[] { "S" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build, Permission.PlaceAdmincrete },
            Help =
                "Toggles the admincrete placement mode. When enabled, any stone block you place is replaced with admincrete.",
            Usage = "/Solid [on/off]",
            Handler = SolidHandler
        };


        static void SolidHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            bool turnSolidOn = (player.GetBind(Block.Stone) != Block.Admincrete);

            if (cmd.HasNext && !cmd.NextOnOff(out turnSolidOn)) {
                CdSolid.PrintUsage(player);
                return;
            }

            if (turnSolidOn) {
                player.Bind(Block.Stone, Block.Admincrete);
                player.Message("Solid: ON. Stone blocks are replaced with admincrete.");
            } else {
                player.ResetBind(Block.Stone);
                player.Message("Solid: OFF");
            }
        }


        static readonly CommandDescriptor CdPaint = new CommandDescriptor {
            Name = "Paint",
            Aliases = new[] { "P" },
            Permissions = new[] { Permission.Build, Permission.Delete },
            Category = CommandCategory.Building,
            Help = "When paint mode is on, any block you delete will be replaced with the block you are holding. " +
                   "Paint command toggles this behavior on and off.",
            Usage = "/Paint [on/off]",
            Handler = PaintHandler
        };


        static void PaintHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            bool turnPaintOn = (!player.IsPainting);

            if (cmd.HasNext && !cmd.NextOnOff(out turnPaintOn)) {
                CdPaint.PrintUsage(player);
                return;
            }

            if (turnPaintOn) {
                player.IsPainting = true;
                player.Message("Paint mode: ON");
            } else {
                player.IsPainting = false;
                player.Message("Paint mode: OFF");
            }
        }


        static readonly CommandDescriptor CdGrass = new CommandDescriptor {
            Name = "Grass",
            Aliases = new[] { "G" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build, Permission.PlaceGrass },
            Help =
                "Toggles the grass placement mode. When enabled, any dirt block you place is replaced with a grass block.",
            Usage = "/Grass [on/off]",
            Handler = GrassHandler
        };


        static void GrassHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            bool turnGrassOn = (player.GetBind(Block.Dirt) != Block.Grass);

            if (cmd.HasNext && !cmd.NextOnOff(out turnGrassOn)) {
                CdGrass.PrintUsage(player);
                return;
            }

            if (turnGrassOn) {
                player.Bind(Block.Dirt, Block.Grass);
                player.Message("Grass: ON. Dirt blocks are replaced with grass.");
            } else {
                player.ResetBind(Block.Dirt);
                player.Message("Grass: OFF");
            }
        }


        static readonly CommandDescriptor CdWater = new CommandDescriptor {
            Name = "Water",
            Aliases = new[] { "W" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build, Permission.PlaceWater },
            Help =
                "Toggles the water placement mode. When enabled, any blue or cyan block you place is replaced with water.",
            Usage = "/Water [on/off]",
            Handler = WaterHandler
        };


        static void WaterHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            bool turnWaterOn = (player.GetBind(Block.Aqua) != Block.Water ||
                                player.GetBind(Block.Cyan) != Block.Water ||
                                player.GetBind(Block.Blue) != Block.Water);

            if (cmd.HasNext && !cmd.NextOnOff(out turnWaterOn)) {
                CdWater.PrintUsage(player);
                return;
            }

            if (turnWaterOn) {
                player.Bind(Block.Aqua, Block.Water);
                player.Bind(Block.Cyan, Block.Water);
                player.Bind(Block.Blue, Block.Water);
                player.Message("Water: ON. Blue blocks are replaced with water.");
            } else {
                player.ResetBind(Block.Aqua, Block.Cyan, Block.Blue);
                player.Message("Water: OFF");
            }
        }


        static readonly CommandDescriptor CdLava = new CommandDescriptor {
            Name = "Lava",
            Aliases = new[] { "L" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build, Permission.PlaceLava },
            Help = "Toggles the lava placement mode. When enabled, any red block you place is replaced with lava.",
            Usage = "/Lava [on/off]",
            Handler = LavaHandler
        };


        static void LavaHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            bool turnLavaOn = (player.GetBind(Block.Red) != Block.Lava);

            if (cmd.HasNext && !cmd.NextOnOff(out turnLavaOn)) {
                CdLava.PrintUsage(player);
                return;
            }

            if (turnLavaOn) {
                player.Bind(Block.Red, Block.Lava);
                player.Message("Lava: ON. Red blocks are replaced with lava.");
            } else {
                player.ResetBind(Block.Red);
                player.Message("Lava: OFF");
            }
        }


        static readonly CommandDescriptor CdBind = new CommandDescriptor {
            Name = "Bind",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Build },
            Help = "Assigns one block type to another. " +
                   "Allows to build block types that are not normally buildable directly: admincrete, lava, water, grass, double step. " +
                   "Calling &H/Bind BlockType&S without second parameter resets the binding. If used with no params, ALL bindings are reset.",
            Usage = "/Bind OriginalBlockType ReplacementBlockType",
            Handler = BindHandler
        };


        static void BindHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            if (!cmd.HasNext) {
                player.Message("All bindings have been reset.");
                player.ResetAllBinds();
                return;
            }

            Block originalBlock;
            if (!cmd.NextBlock(player, false, out originalBlock)) return;

            if (!cmd.HasNext) {
                if (player.GetBind(originalBlock) != originalBlock) {
                    player.Message("{0} is no longer bound to {1}",
                                   originalBlock,
                                   player.GetBind(originalBlock));
                    player.ResetBind(originalBlock);
                } else {
                    player.Message("{0} is not bound to anything.",
                                   originalBlock);
                }
                return;
            }

            Block replacementBlock;
            if (!cmd.NextBlock(player, false, out replacementBlock)) return;

            if (cmd.HasNext) {
                CdBind.PrintUsage(player);
                return;
            }

            Permission permission = Permission.Build;
            switch (replacementBlock) {
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
            if (player.Can(permission)) {
                player.Bind(originalBlock, replacementBlock);
                player.Message("{0} is now replaced with {1}", originalBlock, replacementBlock);
            } else {
                player.Message("&WYou do not have {0} permission.", permission);
            }
        }

        #endregion

        #region Undo / Redo

        static readonly CommandDescriptor CdUndo = new CommandDescriptor {
            Name = "Undo",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = UndoHandler
        };


        static void UndoHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            World playerWorld = player.World;
            if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);
            if (cmd.HasNext) {
                player.Message("Undo command takes no parameters. Did you mean to do &H/UndoPlayer&S or &H/UndoArea&S?");
                return;
            }

            string msg = "Undo: ";
            UndoState undoState = player.UndoPop();
            if (undoState == null) {
                player.MessageNow("There is currently nothing to undo.");
                return;
            }

            // Cancel the last DrawOp, if still in progress
            if (undoState.Op != null && !undoState.Op.IsDone && !undoState.Op.IsCancelled) {
                undoState.Op.Cancel();
                msg += String.Format("Cancelled {0} (was {1}% done). ",
                                     undoState.Op.Description,
                                     undoState.Op.PercentDone);
            }

            // Check if command was too massive.
            if (undoState.IsTooLargeToUndo) {
                if (undoState.Op != null) {
                    player.MessageNow("Cannot undo {0}: too massive.", undoState.Op.Description);
                } else {
                    player.MessageNow("Cannot undo: too massive.");
                }
                return;
            }

            // no need to set player.drawingInProgress here because this is done on the user thread
            Logger.Log(LogType.UserActivity,
                       "Player {0} initiated /Undo affecting {1} blocks (on world {2})",
                       player.Name,
                       undoState.Buffer.Count,
                       playerWorld.Name);

            msg += String.Format("Restoring {0} blocks. Type &H/Redo&S to reverse.",
                                 undoState.Buffer.Count);
            player.MessageNow(msg);

            var op = new UndoDrawOperation(player, undoState, false);
            op.Prepare(new Vector3I[0]);
            op.Begin();
        }


        static readonly CommandDescriptor CdRedo = new CommandDescriptor {
            Name = "Redo",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw },
            Help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = RedoHandler
        };


        static void RedoHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            if (cmd.HasNext) {
                CdRedo.PrintUsage(player);
                return;
            }

            World playerWorld = player.World;
            if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);

            UndoState redoState = player.RedoPop();
            if (redoState == null) {
                player.MessageNow("There is currently nothing to redo.");
                return;
            }

            string msg = "Redo: ";
            if (redoState.Op != null && !redoState.Op.IsDone) {
                redoState.Op.Cancel();
                msg += String.Format("Cancelled {0} (was {1}% done). ",
                                     redoState.Op.Description,
                                     redoState.Op.PercentDone);
            }

            // no need to set player.drawingInProgress here because this is done on the user thread
            Logger.Log(LogType.UserActivity,
                       "Player {0} initiated /Redo affecting {1} blocks (on world {2})",
                       player.Name,
                       redoState.Buffer.Count,
                       playerWorld.Name);

            msg += String.Format("Restoring {0} blocks. Type &H/Undo&S to reverse.",
                                 redoState.Buffer.Count);
            player.MessageNow(msg);

            var op = new UndoDrawOperation(player, redoState, true);
            op.Prepare(new Vector3I[0]);
            op.Begin();
        }

        #endregion

        #region Copy and Paste

        static readonly CommandDescriptor CdCopySlot = new CommandDescriptor {
            Name = "CopySlot",
            Aliases = new[] { "CS" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Usage = "/CopySlot [#]",
            Help = "Selects a slot to copy to/paste from. The maximum number of slots is limited per-rank.",
            Handler = CopySlotHandler
        };


        static void CopySlotHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            int slotNumber;
            if (cmd.NextInt(out slotNumber)) {
                if (cmd.HasNext) {
                    CdCopySlot.PrintUsage(player);
                    return;
                }
                if (slotNumber < 1 || slotNumber > player.Info.Rank.CopySlots) {
                    player.Message("CopySlot: Select a number between 1 and {0}", player.Info.Rank.CopySlots);
                } else {
                    player.CopySlot = slotNumber - 1;
                    CopyState info = player.GetCopyState();
                    if (info == null) {
                        player.Message("Selected copy slot {0} (unused).", slotNumber);
                    } else {
                        player.Message("Selected copy slot {0}: {1} blocks from {2}, {3} old.",
                                       slotNumber,
                                       info.Blocks.Length,
                                       info.OriginWorld,
                                       DateTime.UtcNow.Subtract(info.CopyTime).ToMiniString());
                    }
                }
            } else {
                CopyState[] slots = player.CopyStates;
                player.Message("Using {0} of {1} slots. Selected slot: {2}",
                               slots.Count(info => info != null),
                               player.Info.Rank.CopySlots,
                               player.CopySlot + 1);
                for (int i = 0; i < slots.Length; i++) {
                    if (slots[i] != null) {
                        player.Message("  {0}: {1} blocks from {2}, {3} old",
                                       i + 1,
                                       slots[i].Blocks.Length,
                                       slots[i].OriginWorld,
                                       DateTime.UtcNow.Subtract(slots[i].CopyTime).ToMiniString());
                    }
                }
            }
        }


        static readonly CommandDescriptor CdCopy = new CommandDescriptor {
            Name = "Copy",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Copy blocks for pasting. " +
                   "Used together with &H/Paste&S and &H/PasteNot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/Copy&S from.",
            Handler = CopyHandler
        };


        static void CopyHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            if (cmd.HasNext) {
                CdCopy.PrintUsage(player);
                return;
            }
            player.SelectionStart(2, CopyCallback, null, CdCopy.Permissions);
            player.MessageNow("Copy: Click or &H/Mark&S 2 blocks.");
        }


        static void CopyCallback([NotNull] Player player, [NotNull] Vector3I[] marks, [NotNull] object tag) {
            int sx = Math.Min(marks[0].X, marks[1].X);
            int ex = Math.Max(marks[0].X, marks[1].X);
            int sy = Math.Min(marks[0].Y, marks[1].Y);
            int ey = Math.Max(marks[0].Y, marks[1].Y);
            int sz = Math.Min(marks[0].Z, marks[1].Z);
            int ez = Math.Max(marks[0].Z, marks[1].Z);
            BoundingBox bounds = new BoundingBox(sx, sy, sz, ex, ey, ez);

            int volume = bounds.Volume;
            if (!player.CanDraw(volume)) {
                player.MessageNow(
                    "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                    player.Info.Rank.DrawLimit,
                    volume);
                return;
            }

            // remember dimensions and orientation
            CopyState copyInfo = new CopyState(marks[0], marks[1]);

            Map map = player.WorldMap;
            World playerWorld = player.World;
            if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);

            for (int x = sx; x <= ex; x++) {
                for (int y = sy; y <= ey; y++) {
                    for (int z = sz; z <= ez; z++) {
                        copyInfo.Blocks[x - sx, y - sy, z - sz] = map.GetBlock(x, y, z);
                    }
                }
            }

            copyInfo.OriginWorld = playerWorld.Name;
            copyInfo.CopyTime = DateTime.UtcNow;
            player.SetCopyState(copyInfo);

            player.MessageNow("{0} blocks copied into slot #{1}, origin at {2} corner. You can now &H/Paste",
                              volume,
                              player.CopySlot + 1,
                              copyInfo.OriginCorner);

            Logger.Log(LogType.UserActivity,
                       "{0} copied {1} blocks from world {2} (between {3} and {4}).",
                       player.Name,
                       volume,
                       playerWorld.Name,
                       bounds.MinVertex,
                       bounds.MaxVertex);
        }


        static readonly CommandDescriptor CdMirror = new CommandDescriptor {
            Name = "Mirror",
            Aliases = new[] { "Flip" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/Mirror X Y",
            Usage = "/Mirror [X] [Y] [Z]",
            Handler = MirrorHandler
        };


        static void MirrorHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            CopyState originalInfo = player.GetCopyState();
            if (originalInfo == null) {
                player.MessageNow("Nothing to flip! Copy something first.");
                return;
            }

            // clone to avoid messing up any paste-in-progress
            CopyState info = new CopyState(originalInfo);

            bool flipX = false,
                 flipY = false,
                 flipH = false;
            string axis;
            while ((axis = cmd.Next()) != null) {
                foreach (char c in axis.ToLower()) {
                    if (c == 'x') flipX = true;
                    if (c == 'y') flipY = true;
                    if (c == 'z') flipH = true;
                }
            }

            if (!flipX && !flipY && !flipH) {
                CdMirror.PrintUsage(player);
                return;
            }

            Block block;

            if (flipX) {
                int left = 0;
                int right = info.Bounds.Width - 1;
                while (left < right) {
                    for (int y = info.Bounds.Length - 1; y >= 0; y--) {
                        for (int z = info.Bounds.Height - 1; z >= 0; z--) {
                            block = info.Blocks[left, y, z];
                            info.Blocks[left, y, z] = info.Blocks[right, y, z];
                            info.Blocks[right, y, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if (flipY) {
                int left = 0;
                int right = info.Bounds.Length - 1;
                while (left < right) {
                    for (int x = info.Bounds.Width - 1; x >= 0; x--) {
                        for (int z = info.Bounds.Height - 1; z >= 0; z--) {
                            block = info.Blocks[x, left, z];
                            info.Blocks[x, left, z] = info.Blocks[x, right, z];
                            info.Blocks[x, right, z] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if (flipH) {
                int left = 0;
                int right = info.Bounds.Height - 1;
                while (left < right) {
                    for (int x = info.Bounds.Width - 1; x >= 0; x--) {
                        for (int y = info.Bounds.Length - 1; y >= 0; y--) {
                            block = info.Blocks[x, y, left];
                            info.Blocks[x, y, left] = info.Blocks[x, y, right];
                            info.Blocks[x, y, right] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if (flipX) {
                if (flipY) {
                    if (flipH) {
                        player.Message("Flipped copy along all axes.");
                    } else {
                        player.Message("Flipped copy along X (east/west) and Y (north/south) axes.");
                    }
                } else {
                    if (flipH) {
                        player.Message("Flipped copy along X (east/west) and Z (vertical) axes.");
                    } else {
                        player.Message("Flipped copy along X (east/west) axis.");
                    }
                }
            } else {
                if (flipY) {
                    if (flipH) {
                        player.Message("Flipped copy along Y (north/south) and Z (vertical) axes.");
                    } else {
                        player.Message("Flipped copy along Y (north/south) axis.");
                    }
                } else {
                    player.Message("Flipped copy along Z (vertical) axis.");
                }
            }

            player.SetCopyState(info);
        }


        static readonly CommandDescriptor CdRotate = new CommandDescriptor {
            Name = "Rotate",
            Aliases = new[] { "Spin" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            Usage = "/Rotate (-90|90|180|270) (X|Y|Z)",
            Handler = RotateHandler
        };


        static void RotateHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            CopyState originalInfo = player.GetCopyState();
            if (originalInfo == null) {
                player.MessageNow("Nothing to rotate! Copy something first.");
                return;
            }

            int degrees;
            if (!cmd.NextInt(out degrees) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270)) {
                CdRotate.PrintUsage(player);
                return;
            }

            string axisName = cmd.Next();
            Axis axis = Axis.Z;
            if (axisName != null) {
                switch (axisName.ToLower()) {
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
                        CdRotate.PrintUsage(player);
                        return;
                }
            }

            // allocate the new buffer
            Block[,,] oldBuffer = originalInfo.Blocks;
            Block[,,] newBuffer;

            if (degrees == 180) {
                newBuffer = new Block[oldBuffer.GetLength(0), oldBuffer.GetLength(1), oldBuffer.GetLength(2)];
            } else if (axis == Axis.X) {
                newBuffer = new Block[oldBuffer.GetLength(0), oldBuffer.GetLength(2), oldBuffer.GetLength(1)];
            } else if (axis == Axis.Y) {
                newBuffer = new Block[oldBuffer.GetLength(2), oldBuffer.GetLength(1), oldBuffer.GetLength(0)];
            } else {
                // axis == Axis.Z
                newBuffer = new Block[oldBuffer.GetLength(1), oldBuffer.GetLength(0), oldBuffer.GetLength(2)];
            }

            // clone to avoid messing up any paste-in-progress
            CopyState info = new CopyState(originalInfo, newBuffer);

            // construct the rotation matrix
            int[,] matrix = {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 1 }
            };

            int a,
                b;
            switch (axis) {
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

            switch (degrees) {
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
            for (int x = oldBuffer.GetLength(0) - 1; x >= 0; x--) {
                for (int y = oldBuffer.GetLength(1) - 1; y >= 0; y--) {
                    for (int z = oldBuffer.GetLength(2) - 1; z >= 0; z--) {
                        int nx = (matrix[0, 0] < 0 ? oldBuffer.GetLength(0) - 1 - x : (matrix[0, 0] > 0 ? x : 0)) +
                                 (matrix[0, 1] < 0 ? oldBuffer.GetLength(1) - 1 - y : (matrix[0, 1] > 0 ? y : 0)) +
                                 (matrix[0, 2] < 0 ? oldBuffer.GetLength(2) - 1 - z : (matrix[0, 2] > 0 ? z : 0));
                        int ny = (matrix[1, 0] < 0 ? oldBuffer.GetLength(0) - 1 - x : (matrix[1, 0] > 0 ? x : 0)) +
                                 (matrix[1, 1] < 0 ? oldBuffer.GetLength(1) - 1 - y : (matrix[1, 1] > 0 ? y : 0)) +
                                 (matrix[1, 2] < 0 ? oldBuffer.GetLength(2) - 1 - z : (matrix[1, 2] > 0 ? z : 0));
                        int nz = (matrix[2, 0] < 0 ? oldBuffer.GetLength(0) - 1 - x : (matrix[2, 0] > 0 ? x : 0)) +
                                 (matrix[2, 1] < 0 ? oldBuffer.GetLength(1) - 1 - y : (matrix[2, 1] > 0 ? y : 0)) +
                                 (matrix[2, 2] < 0 ? oldBuffer.GetLength(2) - 1 - z : (matrix[2, 2] > 0 ? z : 0));
                        newBuffer[nx, ny, nz] = oldBuffer[x, y, z];
                    }
                }
            }

            player.Message("Rotated copy (slot {0}) by {1} degrees around {2} axis.",
                           info.Slot + 1,
                           degrees,
                           axis);
            player.SetCopyState(info);
        }

        #endregion

        #region Mark, Cancel

        static readonly CommandDescriptor CdMark = new CommandDescriptor {
            Name = "Mark",
            Aliases = new[] { "M" },
            Category = CommandCategory.Building,
            Usage = "/Mark&S or &H/Mark X Y Z",
            Help =
                "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                "If three numbers are given, those coordinates are used instead.",
            Handler = MarkHandler
        };


        static void MarkHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            Map map = player.WorldMap;
            int x, y, z;
            Vector3I coords;
            if (cmd.NextInt(out x) && cmd.NextInt(out y) && cmd.NextInt(out z)) {
                if (cmd.HasNext) {
                    CdMark.PrintUsage(player);
                    return;
                }
                coords = new Vector3I(x, y, z);
            } else {
                coords = player.Position.ToBlockCoords();
            }
            coords.X = Math.Min(map.Width - 1, Math.Max(0, coords.X));
            coords.Y = Math.Min(map.Length - 1, Math.Max(0, coords.Y));
            coords.Z = Math.Min(map.Height - 1, Math.Max(0, coords.Z));

            if (player.SelectionMarksExpected > 0) {
                player.SelectionAddMark(coords, true, true);
            } else {
                player.MessageNow("Cannot mark - no selection in progress.");
            }
        }


        static readonly CommandDescriptor CdDoNotMark = new CommandDescriptor {
            Name = "DoNotMark",
            Aliases = new[] { "DontMark", "DNM", "DM" },
            Category = CommandCategory.Building,
            Usage = "/DoNotMark",
            Help = "Toggles whether clicking blocks adds to a selection.",
            Handler = DoNotMarkHandler
        };


        static void DoNotMarkHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            bool doNotMark = !player.DisableClickToMark;
            if (cmd.HasNext && !cmd.NextOnOff(out doNotMark)) {
                CdDoNotMark.PrintUsage(player);
            }
            player.DisableClickToMark = doNotMark;
            if (doNotMark) {
                player.Message("Click-to-mark disabled.");
            } else {
                player.Message("Click-to-mark re-enabled.");
            }
        }


        static readonly CommandDescriptor CdCancel = new CommandDescriptor {
            Name = "Cancel",
            Aliases = new[] { "Nvm" },
            Category = CommandCategory.Building | CommandCategory.Chat,
            NotRepeatable = true,
            Help = "If you are writing a partial/multiline message, it's cancelled. " +
                   "Otherwise, cancels current selection (for drawing or zoning). " +
                   "If you wish to stop a drawing in-progress, use &H/Undo&S instead. ",
            Handler = CancelHandler
        };


        static void CancelHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            throw new NotSupportedException(
                "/Cancel handler may not be used directly. Use Player.SelectionCancel() instead.");
        }

        #endregion

        static readonly CommandDescriptor CdStatic = new CommandDescriptor {
            Name = "Static",
            Category = CommandCategory.Building,
            Help = "Toggles repetition of last selection on or off.",
            Usage = "/Static [on/off]",
            Handler = StaticHandler
        };


        static void StaticHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            bool turnStaticOn = (!player.IsRepeatingSelection);

            if (cmd.HasNext && !cmd.NextOnOff(out turnStaticOn)) {
                CdStatic.PrintUsage(player);
                return;
            }

            if (turnStaticOn) {
                player.Message("Static: On");
                player.IsRepeatingSelection = true;
            } else {
                player.Message("Static: Off");
                player.IsRepeatingSelection = false;
                player.SelectionCancel();
            }
        }
    }
}
