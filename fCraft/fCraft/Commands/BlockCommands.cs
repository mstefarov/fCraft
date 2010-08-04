// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {
    enum BlockPlacementMode {
        Normal,
        Grass,
        Lava,
        Solid,
        Water
    }

    static class BlockCommands {

        internal static void Init(){
            CommandList.RegisterCommand( cdSolid );
            CommandList.RegisterCommand( cdPaint );
            CommandList.RegisterCommand( cdGrass );
            CommandList.RegisterCommand( cdWater );
            CommandList.RegisterCommand( cdLava );
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
            if( player.mode == BlockPlacementMode.Solid ){
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Solid: OFF" );
            } else {
                player.mode = BlockPlacementMode.Solid;
                player.Message( "Solid: ON" );
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
            if( player.mode == BlockPlacementMode.Grass ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Grass: OFF" );
            } else {
                player.mode = BlockPlacementMode.Grass;
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
            if( player.mode == BlockPlacementMode.Water ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Water: OFF" );
            } else {
                player.mode = BlockPlacementMode.Water;
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
            if( player.mode == BlockPlacementMode.Lava ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Lava: OFF." );
            } else {
                player.mode = BlockPlacementMode.Lava;
                player.Message( "Lava: ON. Red blocks are replaced with lava." );
            }
        }
    }
}
