// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;


namespace fCraft {
    sealed class MapCommands {
        static object loadLock = new object();

        internal static void Init() {
            Commands.AddCommand( "load", Load, false ); //TODO: streamload
            Commands.AddCommand( "join", Join, false );
            Commands.AddCommand( "save", Save, true );

            Commands.AddCommand( "lock", Lock, true );
            Commands.AddCommand( "unlock", Unlock, true );
            Commands.AddCommand( "lockall", LockAll, true );
            Commands.AddCommand( "unlockall", UnlockAll, true );

            Commands.AddCommand( "gen", Generate, true );
            Commands.AddCommand( "genh", GenerateHollow, true );

            Commands.AddCommand( "zone", DoZone, false );
            Commands.AddCommand( "zones", ListZones, true );
            Commands.AddCommand( "zremove", ZoneRemove, true );

            Commands.AddCommand( "worlds", ListWorlds, true );
            Commands.AddCommand( "wload", AddWorld, true );

            //Commands.AddCommand( "landmark", AddLandmark, false);
        }

        internal static void ListWorlds( Player player, Command cmd ) {
            lock( Server.worldListLock ) {
                string line = "List of worlds: ";
                bool first = true;
                foreach( string worldName in Server.worlds.Keys ) {
                    if( line.Length + worldName.Length > 62 ) {
                        player.Message( line );
                        line = "";
                    } else if(!first) {
                        line += ", ";
                    }
                    line += worldName;
                    first = false;
                }
                player.Message( line );
            }
        }

        internal static void AddWorld( Player player, Command cmd ) {
            if( player.Can( Permissions.ManageWorlds ) ) {
                string worldName = cmd.Next();
                if( worldName == null || !Player.IsValidName( worldName ) ) {
                    player.Message( "Invalid world name: \"" + worldName + "\"." );
                } else {
                    if( Server.AddWorld( worldName, false ) != null ) {
                        Server.SendToAll( Color.Sys + player.name + " created a new world named \"" + worldName + "\"." );
                        Logger.Log( player.name + " created a new world named \"" + worldName + "\".", LogType.UserActivity );
                        Server.SaveWorldList();
                    } else {
                        player.Message( "Error occured while trying to create a new world." );
                    }
                }
            } else {
                player.NoAccessMessage();
            }
        }

        internal static void Join( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                player.Message( "Usage: " + Color.Help + "/join worldName" );
                return;
            }
            World world = Server.FindWorld( worldName );
            if( world != null ) {
                player.world.ReleasePlayer( player );
                player.session.JoinWorld( world, true );
            } else {
                player.Message( "No world found with the name \"" + worldName + "\"." );
            }
        }


        internal static void DoZone( Player player, Command cmd ) {//TODO: better method names
            if( !player.Can( Permissions.ManageZones ) ) {
                player.NoAccessMessage();
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
            PlayerClass minRank = ClassList.ParseClass( property );
            
            if( minRank != null ) {
                zone.buildRank = minRank.rank;
                player.tag = zone;
                player.marksExpected = 2;
                player.marks.Clear();
                player.markCount = 0;
                player.selectionCallback = MakeZone;
            }
        }



        internal static void MakeZone( Player player, Position[] marks, object tag ) {//TODO: better method names
            Zone zone = (Zone)tag;
            zone.xMin = Math.Min( marks[0].x, marks[1].x );
            zone.xMax = Math.Max( marks[0].x, marks[1].x );
            zone.yMin = Math.Min( marks[0].y, marks[1].y );
            zone.yMax = Math.Max( marks[0].y, marks[1].y );
            zone.hMin = Math.Min( marks[0].h, marks[1].h );
            zone.hMax = Math.Max( marks[0].h, marks[1].h );
            player.Message( "Zone \"" + zone.name + "\" created, " + zone.getVolume() + " blocks total." );
            Logger.Log( "Player {0} created a new zone \"{1}\" containing {2} blocks.", LogType.UserActivity,
                                  player.name,
                                  zone.name,
                                  zone.getVolume() );
            player.world.map.AddZone(zone);
        }


        internal static void ZoneRemove( Player player, Command cmd ) {
            if( !player.Can( Permissions.ManageZones ) ) {
                player.NoAccessMessage();
                return;
            }
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "Usage: " + Color.Help + "/zremove ZoneName" );
                return;
            }
            if( player.world.map.RemoveZone( zoneName ) ) {
                player.Message( "Zone \"" + zoneName + "\" removed." );
            } else {
                player.Message( "No zone with the name \"" + zoneName + "\" was found." );
            }
        }


        internal static void ListZones( Player player, Command cmd ) {
            Zone[] zones = player.world.map.ListZones();
            foreach( Zone zone in zones ) {
                PlayerClass rank = ClassList.ParseRank( zone.buildRank );
                if( rank != null ) {
                    player.Message( "  " + zone.name + " (" + rank.color + rank.name + Color.Sys + ") - " + zone.getWidthX() + "x" + zone.getWidthY() + "x" + zone.getHeight() );
                } else {
                    player.Message( "  " + zone.name + " - " + zone.getWidthX() + "x" + zone.getWidthY() + "x" + zone.getHeight() );
                }
            }
        }


        //internal static void AddLandmark( Player player, Command cmd ) {
        //    if(!player.Can(Permissions.AddLandmarks)){
        //        player.NoAccessMessage();
        //        return;
        //    }

        //    string name = cmd.Next();
        //    if (name == null) {
        //        player.Message("No landmark name specified. See " + Color.Help + "/help landmark");
        //        return;
        //    }

        //    player.world.map.AddLandmark(player.pos);

        //}

        internal static void Load( Player player, Command cmd ) {
            if ( !player.Can( Permissions.SaveAndLoad ) ) {
                player.NoAccessMessage();
                return;
            }

            string fileName = cmd.Next();
            if ( fileName == null ) {
                player.Message( "Syntax: " + Color.Help + "/load mapName.ext" + Color.Sys + " or " + Color.Help + "/load mapName formatName" );
                return;
            }

            player.Message( "Attempting to load " + fileName + "..." );

            Map map = Map.Load( player.world, fileName, cmd.Next() );
            if ( map != null ) {
                player.world.ChangeMap( map );
            } else {
                player.Message( "Could not load specified file." );
            }
        }
            

        /*internal static void Load( Player player, Command cmd ) {//TODO: streamload
            lock( loadLock ) {
                if( player.world.loadInProgress || player.world.loadSendingInProgress ) {
                    player.Message( "Loading already in progress, please wait." );
                    return;
                }
                player.world.loadInProgress = true;
            }

            if( !player.Can( Permissions.SaveAndLoad ) ) {
                player.NoAccessMessage();
                player.world.loadInProgress = false;
                return;
            }

            string mapName = cmd.Next();
            if( mapName == null ) {
                player.Message( "Syntax: " + Color.Help + "/load mapName" );
                player.world.loadInProgress = false;
                return;
            }

            string mapFileName = mapName + ".fcm";
            if( !File.Exists( mapFileName ) ) {
                player.Message( "No backup file \"" + mapName + "\" found." );
                player.world.loadInProgress = false;
                return;
            }

            Map newMap = Map.Load( player.world, mapFileName );
            if( newMap == null ) {
                player.Message( "Could not load \"" + mapFileName + "\". Check logfile for details." );
                player.world.loadInProgress = false;
                return;
            }

            if( newMap.widthX != player.world.map.widthX ||
                newMap.widthY != player.world.map.widthY ||
                newMap.height != player.world.map.height ) {
                player.Message( "Map sizes of \"" + mapName + "\" and the current map do not match." );
                player.world.loadInProgress = false;
                return;
            }

            Logger.Log( "{0} is loading the map \"{1}\".", LogType.UserActivity, player.name, mapName );
            player.Message( "Loading map \"" + mapName + "\"..." );
            //player.world.BeginLockDown();
            MapSenderParams param = new MapSenderParams() {
                map = newMap,
                player = player,
                world = player.world
            };
            Tasks.Add( MapSender.StreamLoad, param, true );
        }*/


        internal static void Save( Player player, Command cmd ) {
            if( !player.Can( Permissions.SaveAndLoad ) ) {
                player.NoAccessMessage();
                return;
            }

            string mapName = cmd.Next();
            if( mapName == null ) {
                player.Message( "Syntax: " + Color.Help + "/save mapName" );
                return;
            }

            string mapFileName = Path.GetFileName(mapName) + ".fcm";
            player.Message( "Saving map to \""+mapFileName+"\"..." );
            if( player.world.map.Save( mapFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                player.Message( "Map saving failed. See server logs for details." );
            }
        }


        internal static void Generate( Player player, Command cmd ) {
            if( !player.Can( Permissions.SaveAndLoad ) ) {
                player.NoAccessMessage();
                return;
            }
            int wx, wy, height;
            if( !(cmd.NextInt( out wx ) && cmd.NextInt( out wy ) && cmd.NextInt( out height )) ) {
                wx = player.world.map.widthX;
                wy = player.world.map.widthY;
                height = player.world.map.height;
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
            //player.Message( "Seed: " + Convert.ToBase64String( BitConverter.GetBytes( seed ) ) );

            Map map = new Map( player.world, wx, wy, height );
            map.spawn.Set( map.widthX / 2 * 32 + 16, map.widthY / 2 * 32 + 16, map.height * 32, 0, 0 );

            DoGenerate( map, player, mode, filename, rand, false );
        }


        internal static void GenerateHollow( Player player, Command cmd ) {
            if( !player.Can( Permissions.SaveAndLoad ) ) {
                player.NoAccessMessage();
                return;
            }
            int wx, wy, height;
            if( !(cmd.NextInt( out wx ) && cmd.NextInt( out wy ) && cmd.NextInt( out height )) ) {
                wx = player.world.map.widthX;
                wy = player.world.map.widthY;
                height = player.world.map.height;
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

            Map map = new Map( player.world, wx, wy, height );
            map.spawn.Set( map.widthX / 2 * 32 + 16, map.widthY / 2 * 32 + 16, map.height * 32, 0, 0 );

            DoGenerate( map, player, mode, filename, rand, true );
        }

        internal static void GenerateFlatgrass( Map map, bool hollow ) {
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

        internal static void DoGenerate( Map map, Player player, string mode, string filename, Random rand, bool hollow ) {
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
                    Tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              1, 1, 0.5, 0.5, 0, 0.5, hollow ), false );
                    break;

                case "mountains":
                    player.Message( "Generating terrain..." );
                    Tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              4, 1, 0.5, 0.5, 0.1, 0.5, hollow ), false );
                    break;

                case "lake":
                    player.Message( "Generating terrain..." );
                    Tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              1, 0.6, 0.9, 0.5, -0.35, 0.55, hollow ), false );
                    break;

                case "island":
                    player.Message( "Generating terrain..." );
                    Tasks.Add( MapGenerator.GenerationTask, new MapGenerator( rand, map, player, filename,
                                                                              1, 0.6, 1, 0.5, 0.3, 0.35, hollow ), false );
                    break;

                default:
                    player.Message( "Unknown map generation mode: " + mode );
                    break;
            }
        }


        internal static void Lock( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                player.NoAccessMessage();
                return;
            }
            string worldName = cmd.Next();
            World world = player.world;
            if( worldName != null ) {
                world = Server.FindWorld( worldName );
                if( world == null ) {
                    player.Message( "No world found with the name \"" + worldName + "\"." );
                    return;
                }
            }
            if( world.locked ) {
                player.Message( "The world is already locked." );
            } else {
                world.Lock();
            }
        }


        internal static void LockAll( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                player.NoAccessMessage();
                return;
            }else{
                lock(Server.worldListLock){
                    foreach(World world in Server.worlds.Values){
                        world.Lock();
                    }
                }
                player.Message( "All worlds are now locked." );
            }
        }


        internal static void Unlock( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                player.NoAccessMessage();
                return;
            }
            string worldName = cmd.Next();
            World world = player.world;
            if( worldName != null ) {
                world = Server.FindWorld( worldName );
                if( world == null ) {
                    player.Message( "No world found with the name \"" + worldName + "\"." );
                    return;
                }
            }
            if( !world.locked ) {
                player.Message( "The world is already unlocked." );
            } else {
                world.Unlock();
            }
        }


        internal static void UnlockAll( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                player.NoAccessMessage();
                return;
            } else {
                lock( Server.worldListLock ) {
                    foreach( World world in Server.worlds.Values ) {
                        world.Unlock();
                    }
                }
                player.Message( "All worlds are now unlocked." );
            }
        }
    }
}
