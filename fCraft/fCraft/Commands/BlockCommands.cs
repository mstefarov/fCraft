// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {
    enum BlockPlacementMode {
        Normal,
        Grass,
        Lava,
        Solid,
        Water,
        Hardened
    }

    static class BlockCommands {

        // Register help commands
        internal static void Init(){
            Commands.AddCommand( "grass", Grass, false );
            Commands.AddCommand( "water", Water, false );
            Commands.AddCommand( "lava", Lava, false );
            Commands.AddCommand( "solid", Solid, false );
            Commands.AddCommand( "s", Solid, false );
            Commands.AddCommand( "paint", Paint, false );
            Commands.AddCommand( "harden", Hardened, false );
            Commands.AddCommand( "h", Hardened, false );
        }


        internal static void Solid( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Solid ){
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Solid: OFF" );
            } else if( player.Can( Permissions.PlaceAdmincrete ) ) {
                player.mode = BlockPlacementMode.Solid;
                player.Message( "Solid: ON" );
            } else {
                player.NoAccessMessage( Permissions.PlaceAdmincrete );
            }
        }

        internal static void Hardened(Player player, Command cmd){
            if (player.hardenedMode == BlockPlacementMode.Hardened) {
                player.hardenedMode = BlockPlacementMode.Normal;
                player.Message("Hardened blocks: OFF");
            } else if (player.Can(Permissions.PlaceHardenedBlocks)) {
                player.hardenedMode = BlockPlacementMode.Hardened;
                player.Message("Hardened blocks: ON");
            } else {
                player.NoAccessMessage( Permissions.PlaceHardenedBlocks );
            }
        }


        internal static void Paint( Player player, Command cmd ) {
            player.replaceMode = !player.replaceMode;
            if( player.replaceMode ){
                player.Message( "Replacement mode: ON" );
            } else {
                player.Message( "Replacement mode: OFF" );
            }
        }


        internal static void Grass( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Grass ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Grass: OFF" );
            } else if( player.Can( Permissions.PlaceGrass ) ) {
                player.mode = BlockPlacementMode.Grass;
                player.Message( "Grass: ON. Dirt blocks are replaced with grass." );
            } else {
                player.NoAccessMessage( Permissions.PlaceGrass );
            }
        }


        internal static void Water( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Water ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Water: OFF" );
            } else if( player.Can( Permissions.PlaceWater ) ) {
                player.mode = BlockPlacementMode.Water;
                player.Message( "Water: ON. Blue blocks are replaced with water." );
            } else {
                player.NoAccessMessage( Permissions.PlaceWater );
            }
        }


        internal static void Lava( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Lava ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Lava: OFF." );
            } else if( player.Can( Permissions.PlaceLava ) ) {
                player.mode = BlockPlacementMode.Lava;
                player.Message( "Lava: ON. Red blocks are replaced with lava." );
            } else {
                player.NoAccessMessage( Permissions.PlaceLava );
            }
        }
    }
}
