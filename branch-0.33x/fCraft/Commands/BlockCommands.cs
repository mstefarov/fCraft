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

    sealed class BlockCommands {
        World world;

        // Register help commands
        internal BlockCommands( World _world, Commands commands ) {
            world = _world;
            commands.AddCommand( "grass", Grass, false );
            commands.AddCommand( "water", Water, false );
            commands.AddCommand( "lava", Lava, false );
            commands.AddCommand( "solid", Solid, false );
            commands.AddCommand( "s", Solid, false );
            commands.AddCommand( "paint", Paint, false );
            //CommandUtils.AddCommand( "sand", Sand ); // TODO: after sand sim is done
        }


        void Solid( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Solid ){
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Solid: OFF" );
            } else if( player.Can( Permissions.PlaceAdmincrete ) ) {
                player.mode = BlockPlacementMode.Solid;
                player.Message( "Solid: ON" );
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Paint( Player player, Command cmd ) {
            player.replaceMode = !player.replaceMode;
            if( player.replaceMode ){
                player.Message( "Replacement mode: ON" );
            } else {
                player.Message( "Replacement mode: OFF" );
            }
        }


        void Grass( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Grass ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Grass: OFF" );
            } else if( player.Can( Permissions.PlaceGrass ) ) {
                player.mode = BlockPlacementMode.Grass;
                player.Message( "Grass: ON. Dirt blocks are replaced with grass." );
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Water( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Water ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Water: OFF" );
            } else if( player.Can( Permissions.PlaceWater ) ) {
                player.mode = BlockPlacementMode.Water;
                player.Message( "Water: ON. Blue blocks are replaced with water." );
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Lava( Player player, Command cmd ) {
            if( player.mode == BlockPlacementMode.Lava ) {
                player.mode = BlockPlacementMode.Normal;
                player.Message( "Lava: OFF." );
            } else if( player.Can( Permissions.PlaceWater ) ) {
                player.mode = BlockPlacementMode.Lava;
                player.Message( "Lava: ON. Red blocks are replaced with lava." );
            } else {
                world.NoAccessMessage( player );
            }
        }
    }
}
