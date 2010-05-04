// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;


namespace fCraft {
    sealed class MapCommands {
        World world;
        object loadLock = new object();

        internal MapCommands( World _world, Commands commands ) {
            world = _world;

            commands.AddCommand( "load", Load, true );
            commands.AddCommand( "save", Save, true );

            commands.AddCommand( "lock", Lock, true );
            commands.AddCommand( "unlock", Unlock, true );

            commands.AddCommand( "gen", Generate, true );
            commands.AddCommand( "genh", GenerateHollow, true );

            commands.AddCommand( "zone", DoZone, false );
        }


        void DoZone( Player player, Command cmd ) {
            if( !player.Can( Permissions.SetSpawn ) ) {
                world.NoAccessMessage( player );
                return;
            }

            string name = cmd.Next();
            if( name == null ) {
                player.Message( "No zone name specified. See " + Color.Help + "/help zone" );
                return;
            }
            if( !Player.IsValidName( name ) ) {
                player.Message( "\"" + name + "\" is not a valid zone name" );
                return;
            }
            Zone zone = new Zone();
            zone.name = name;

            string property = cmd.Next();
            if( property == null ) {
                player.Message( "No zone rank/whitelist/blacklist specified. See " + Color.Help + "/help zone" );
                return;
            }
            PlayerClass minRank = world.classes.ParseClass( property );
            
            if( minRank != null ) {
                zone.buildRank = minRank.rank;
                player.tag = zone;
                player.marksExpected = 2;
                player.marks.Clear();
                player.markCount = 0;
                player.selectionCallback = MakeZone;
            }
        }

        static void MakeZone( Player player, Position[] marks, object tag ) {
            Zone zone = (Zone)tag;
            zone.xMin = Math.Min( marks[0].x, marks[1].x );
            zone.xMax = Math.Max( marks[0].x, marks[1].x );
            zone.yMin = Math.Min( marks[0].y, marks[1].y );
            zone.yMax = Math.Max( marks[0].y, marks[1].y );
            zone.hMin = Math.Min( marks[0].h, marks[1].h );
            zone.hMax = Math.Max( marks[0].h, marks[1].h );
            player.Message( "Zone \"" + zone.name + "\" created, " + zone.getVolume() + " blocks total." );
            player.world.log.Log( "Player {0} created a new zone \"{1}\" containing {2} blocks.", LogType.UserActivity,
                                  player.name,
                                  zone.name,
                                  zone.getVolume() );
            player.world.map.zones.Add( zone );
        }


        void Load( Player player, Command cmd ) {
            lock( loadLock ) {
                if( world.loadInProgress || world.loadSendingInProgress ) {
                    player.Message( "Loading already in progress, please wait." );
                    return;
                }
                world.loadInProgress = true;
            }

            if( !player.Can( Permissions.SaveAndLoad ) ) {
                world.NoAccessMessage( player );
                world.loadInProgress = false;
                return;
            }

            string mapName = cmd.Next();
            if( mapName == null ) {
                player.Message( "Syntax: " + Color.Help + "/load mapName" );
                world.loadInProgress = false;
                return;
            }

            string mapFileName = mapName + ".fcm";
            if( !File.Exists( mapFileName ) ) {
                player.Message( "No backup file \"" + mapName + "\" found." );
                world.loadInProgress = false;
                return;
            }

            Map newMap = Map.Load( world, mapFileName );
            if( newMap == null ) {
                player.Message( "Could not load \"" + mapFileName + "\". Check logfile for details." );
                world.loadInProgress = false;
                return;
            }

            if( newMap.widthX != world.map.widthX ||
                newMap.widthY != world.map.widthY ||
                newMap.height != world.map.height ) {
                player.Message( "Map sizes of \"" + mapName + "\" and the current map do not match." );
                world.loadInProgress = false;
                return;
            }

            world.log.Log( "{0} is loading the map \"{1}\".", LogType.UserActivity, player.name, mapName );
            player.Message( "Loading map \"" + mapName + "\"..." );
            world.BeginLockDown();
            MapSenderParams param = new MapSenderParams() {
                map = newMap,
                player = player,
                world = world
            };
            world.tasks.Add( MapSender.StreamLoad, param, true );
        }


        void Save( Player player, Command cmd ) {
            if( !player.Can( Permissions.SaveAndLoad ) ) {
                world.NoAccessMessage( player );
                return;
            }

            string mapName = cmd.Next();
            if( mapName == null ) {
                player.Message( "Syntax: " + Color.Help + "/backup backupName" );
                return;
            }

            string mapFileName = Path.GetFileName(mapName) + ".fcm";
            player.Message( "Saving backup..." );
            if( world.map.Save( mapFileName ) ) {
                player.Message( "Backup succesful." );
            } else {
                player.Message( "Backup failed. See logfile for details." );
            }
        }


        void Generate( Player player, Command cmd ) {
            if( !player.Can( Permissions.SaveAndLoad ) ) {
                world.NoAccessMessage( player );
                return;
            }
            int wx, wy, height;
            if( !(cmd.NextInt( out wx ) && cmd.NextInt( out wy ) && cmd.NextInt( out height )) ) {
                wx = world.map.widthX;
                wy = world.map.widthY;
                height = world.map.height;
                cmd.Rewind();
            }
            string mode = cmd.Next();
            string filename = cmd.Next();
            if( mode == null || filename == null ) {
                player.Message( "Usage: " + Color.Help + "/gen widthX widthY height type filename" );
                return;
            }
            filename += ".fcm";

            int seed;
            if( !cmd.NextInt( out seed ) ) {
                seed = new Random().Next();
            }
            Random rand = new Random( seed );
            player.Message( "Seed: " + Convert.ToBase64String( BitConverter.GetBytes( seed ) ) );

            Map map = new Map( world, wx, wy, height );
            map.spawn.Set( map.widthX / 2 * 32 + 16, map.widthY / 2 * 32 + 16, map.height * 32, 0, 0 );

            DoGenerate( map, player, mode, filename, rand, false );
        }


        void GenerateHollow( Player player, Command cmd ) {
            if( !player.Can( Permissions.SaveAndLoad ) ) {
                world.NoAccessMessage( player );
                return;
            }
            int wx, wy, height;
            if( !(cmd.NextInt( out wx ) && cmd.NextInt( out wy ) && cmd.NextInt( out height )) ) {
                wx = world.map.widthX;
                wy = world.map.widthY;
                height = world.map.height;
                cmd.Rewind();
            }
            string mode = cmd.Next();
            string filename = cmd.Next();
            if( mode == null || filename == null ) {
                player.Message( "Usage: " + Color.Help + "/genh widthX widthY height type filename" );
                return;
            }
            filename += ".fcm";

            int seed;
            if( !cmd.NextInt( out seed ) ) {
                seed = new Random().Next();
            }
            Random rand = new Random( seed );
            player.Message( "Seed: " + Convert.ToBase64String( BitConverter.GetBytes( seed ) ) );

            Map map = new Map( world, wx, wy, height );
            map.spawn.Set( map.widthX / 2 * 32 + 16, map.widthY / 2 * 32 + 16, map.height * 32, 0, 0 );

            DoGenerate( map, player, mode, filename, rand, true );
        }

        internal static void GenerateFlatgrass( Map map,bool hollow ) {
            for ( int i = 0; i < map.widthX; i++ ) {
                for ( int j = 0; j < map.widthY; j++ ) {
                    if ( !hollow ) {
                        for ( int k = 1; k < map.height / 2 - 1; k++ ) {
                            if ( k < map.height / 2 - 5 ) {
                                map.SetBlock( i, j, k, Block.Stone );
                            } else {
                                map.SetBlock( i, j, k, Block.Dirt );
                            }
                        }
                    }
                    map.SetBlock( i, j, map.height / 2 - 1, Block.Grass );
                }
            }

            map.MakeFloodBarrier();
        }

        void DoGenerate( Map map, Player player, string mode, string filename, Random rand, bool hollow ) {
            switch( mode ) {
                case "flatgrass":
                    player.Message( "Generating flatgrass map..." );
                    GenerateFlatgrass( map, hollow );

                    if( map.Save( filename ) ) {
                        player.Message( "Map generation: Done." );
                    } else {
                        player.Message( Color.Red, "An error occured while generating the map." );
                    }
                    break;

                case "lag":
                    player.Message( "Generating laggy map..." );
                    for( int x = 0; x < map.widthX; x+=2 ) {
                        for( int y = 0; y < map.widthY; y+=2 ) {
                            for( int h = 0; h < map.widthY; h+=2 ) {
                                map.SetBlock( x, y, h, Block.Lava );
                            }
                        }
                    }

                    if( map.Save( filename ) ) {
                        player.Message( "Map generation: Done." );
                    } else {
                        player.Message( Color.Red, "An error occured while generating the map." );
                    }
                    break;

                case "empty":
                    player.Message( "Generating empty map..." );
                    map.MakeFloodBarrier();

                    if( map.Save( filename ) ) {
                        player.Message( "Map generation: Done." );
                    } else {
                        player.Message( Color.Red, "An error occured while generating the map." );
                    }

                    break;

                case "hills":
                    player.Message( "Generating terrain..." );
                    world.tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              1, 1, 0.5, 0.5, 0, 0.5, hollow ), false );
                    break;

                case "mountains":
                    player.Message( "Generating terrain..." );
                    world.tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              4, 1, 0.5, 0.5, 0.1, 0.5, hollow ), false );
                    break;

                case "lake":
                    player.Message( "Generating terrain..." );
                    world.tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              1, 0.6, 0.9, 0.5, -0.35, 0.55, hollow ), false );
                    break;

                case "island":
                    player.Message( "Generating terrain..." );
                    world.tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              1, 0.6, 1, 0.5, 0.3, 0.35, hollow ), false );
                    break;

                default:
                    player.Message( "Unknown map generation mode: " + mode );
                    break;
            }
        }


        void Lock( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                world.NoAccessMessage( player );
                return;
            }
            world.SendToAll( PacketWriter.MakeMessage( Color.Red + "Server is now on lockdown!" ), null );
            world.BeginLockDown();
        }


        void Unlock( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                world.NoAccessMessage( player );
                return;
            }
            world.SendToAll( PacketWriter.MakeMessage( Color.Red + "Lockdown has ended." ), null );
            world.EndLockDown();
        }
    }
}
