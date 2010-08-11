// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {
    static class BlockCommands {

        internal static void Init(){
            CommandList.RegisterCommand( cdSolid );
            CommandList.RegisterCommand( cdPaint );
            CommandList.RegisterCommand( cdGrass );
            CommandList.RegisterCommand( cdWater );
            CommandList.RegisterCommand( cdLava );
            CommandList.RegisterCommand( cdBind );
        }



        static CommandDescriptor cdSolid = new CommandDescriptor {
            name = "solid",
            aliases = new string[] { "s" },
            permissions = new Permission[] { Permission.PlaceAdmincrete },
            usage = "/solid &Sor&H /s",
            help = "Toggles the admincrete placement mode. When enabled, any stone block you place is replaced with admincrete.",
            handler = Solid
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



        static CommandDescriptor cdPaint = new CommandDescriptor {
            name = "paint",
            help = "Replaces a block instead of deleting it.",
            handler = Paint
        };

        internal static void Paint( Player player, Command cmd ) {
            player.replaceMode = !player.replaceMode;
            if( player.replaceMode ){
                player.Message( "Replacement mode: ON" );
            } else {
                player.Message( "Replacement mode: OFF" );
            }
        }



        static CommandDescriptor cdGrass = new CommandDescriptor {
            name = "grass",
            permissions = new Permission[] { Permission.PlaceGrass },
            help = "Toggles the grass placement mode. When enabled, any dirt block you place is replaced with a grass block.",
            handler = Grass
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



        static CommandDescriptor cdWater = new CommandDescriptor {
            name = "water",
            permissions = new Permission[] { Permission.PlaceWater },
            help = "Toggles the water placement mode. When enabled, any blue or cyan block you place is replaced with water.",
            handler = Water
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



        static CommandDescriptor cdLava = new CommandDescriptor {
            name = "lava",
            permissions = new Permission[] { Permission.PlaceLava },
            help = "Toggles the lava placement mode. When enabled, any red block you place is replaced with lava.",
            handler = Lava
        };

        internal static void Lava( Player player, Command cmd ) {
            if( player.GetBind( Block.Red ) == Block.Lava ){
                player.ResetBind( Block.Red );
                player.Message( "Lava: OFF" );
            } else {
                player.Bind( Block.Red, Block.Lava );
                player.Message( "Lava: ON. Red blocks are replaced with lava." );
            }
        }



        static CommandDescriptor cdBind = new CommandDescriptor {
            name = "bind",
            aliases = new string[] { "b" },
            permissions = new Permission[] { Permission.Build },
            help = "Assigns one blocktype to another. "+
                   "Allows to build blocktypes that are not normally buildable directly: admincrete, lava, water, grass, double step. "+
                   "Calling &H/bind BlockType&S without second parameter resets the binding. If used with no params, ALL bindings are reset.",
            usage = "/bind OriginalBlockType ReplacementBlockType",
            handler = Bind
        };

        internal static void Bind( Player player, Command cmd ){
            Block originalBlock, replacementBlock;
            if( !cmd.NextBlockType(out originalBlock)) {
                    player.Message( "All bindings have been reset." );
                    foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                        if( block != Block.Undefined ) {
                            player.ResetBind( block );
                        }
                    }
                return;
            }else if( originalBlock == Block.Undefined ) {
                player.Message( "Unrecognized original block name." );
                return;
            }

            if( !cmd.NextBlockType( out replacementBlock ) ) {
                if( player.GetBind( originalBlock ) != originalBlock ) {
                    player.Message( originalBlock.ToString() + " is no longer bound to " + player.GetBind( originalBlock ).ToString() );
                    player.ResetBind( originalBlock );
                } else {
                    player.Message( originalBlock.ToString() + " is not bound to anything." );
                }
            } else if( replacementBlock == Block.Undefined ) {
                player.Message( "Unrecognized replacement block name." );
            }else{
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
                    player.Message( originalBlock.ToString() + " is now replaced with " + replacementBlock.ToString() );
                }else{
                    player.Message( Color.Red + "You do not have " + permission.ToString() + " permission." );
                }
            }
        }
    }
}
