// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary>
    /// Commands for placing specific blocks (solid, water, grass),
    /// looking up block information (whodid),
    /// and switching block placement modes (paint, bind).
    /// </summary>
    static class BlockCommands {

        internal static void Init() {
            CommandList.RegisterCommand( cdSolid );
            CommandList.RegisterCommand( cdPaint );
            CommandList.RegisterCommand( cdGrass );
            CommandList.RegisterCommand( cdWater );
            CommandList.RegisterCommand( cdLava );
            CommandList.RegisterCommand( cdBind );
            //CommandList.RegisterCommand( cdWhoDid );
        }


        static readonly CommandDescriptor cdSolid = new CommandDescriptor {
            Name = "solid",
            Aliases = new[] { "s" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.PlaceAdmincrete },
            Usage = "/solid &Sor&H /s",
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



        static readonly CommandDescriptor cdPaint = new CommandDescriptor {
            Name = "paint",
            Aliases = new[] { "p" },
            Category = CommandCategory.Building,
            Help = "Replaces a block instead of deleting it.",
            Handler = Paint
        };

        internal static void Paint( Player player, Command cmd ) {
            player.IsPainting = !player.IsPainting;
            if( player.IsPainting ) {
                player.Message( "Replacement mode: ON" );
            } else {
                player.Message( "Replacement mode: OFF" );
            }
        }



        static readonly CommandDescriptor cdGrass = new CommandDescriptor {
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



        static readonly CommandDescriptor cdWater = new CommandDescriptor {
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



        static readonly CommandDescriptor cdLava = new CommandDescriptor {
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



        static readonly CommandDescriptor cdBind = new CommandDescriptor {
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
            if( originalBlockName==null ) {
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


        // DISABLED
        static CommandDescriptor cdWhoDid = new CommandDescriptor {
            Name = "whodid",
            Help = "Checks who last modified a block.",
            Handler = WhoDid
        };

        static void WhoDid( Player player, Command cmd ) {
            player.SetCallback( 1, WhoDidCallback, player.World.Map );
            player.Message( "Click the block that you would like to test." );
        }

        internal static void WhoDidCallback( Player player, Position[] marks, object tag ) {
            Map map = (Map)tag;
            ushort ownership = map.BlockOwnership[map.Index( marks[0].X, marks[0].Y, marks[0].H )];
            if( ownership < 256 ) {
                switch( (ReservedPlayerID)ownership ) {

                    case ReservedPlayerID.Automatic:
                        player.Message( "Block at ({0},{1},{2}) edited automatically.",
                                        marks[0].X, marks[0].Y, marks[0].H );
                        break;

                    case ReservedPlayerID.Console:
                        player.Message( "Block at ({0},{1},{2}) edited by {0}.",
                                        marks[0].X, marks[0].Y, marks[0].H, Player.Console.GetClassyName() );
                        break;

                    case ReservedPlayerID.IRCBot:
                        player.Message( "Block at ({0},{1},{2}) edited by IRC Bot.",
                                        marks[0].X, marks[0].Y, marks[0].H );
                        break;

                    case ReservedPlayerID.None:
                        player.Message( "Block at ({0},{1},{2}) was never touched.",
                                        marks[0].X, marks[0].Y, marks[0].H );
                        break;

                    case ReservedPlayerID.Physics:
                        player.Message( "Block at ({0},{1},{2}) was modified by physics.",
                                        marks[0].X, marks[0].Y, marks[0].H );
                        break;

                    default: // includes "Unknown"
                        player.Message( "No information available for block at ({0},{1},{2}).",
                                        marks[0].X, marks[0].Y, marks[0].H );
                        break;
                }
            } else {
                string name = map.FindPlayerName( ownership );
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name );
                if( info == null ) {
                    player.Message( "Block at ({0},{1},{2}) edited by \"{3}\" (unrecognized)",
                                    marks[0].X, marks[0].Y, marks[0].H, name );
                } else {
                    player.Message( "Block at ({0},{1},{2}) edited by {3}",
                                    marks[0].X, marks[0].Y, marks[0].H, info.GetClassyName() );
                }
            }
        }
    }
}