// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;


namespace fCraft {
    static class MapCommands {
        static object loadLock = new object();

        internal static void Init() {
            Commands.AddCommand( "join", Join, false );
            Commands.AddCommand( "j", Join, false );
            Commands.AddCommand( "load", Join, false );
            Commands.AddCommand( "l", Join, false );
            Commands.AddCommand( "goto", Join, false );

            Commands.AddCommand( "lock", Lock, true );
            Commands.AddCommand( "unlock", Unlock, true );
            Commands.AddCommand( "lockall", LockAll, true );
            Commands.AddCommand( "unlockall", UnlockAll, true );

            Commands.AddCommand( "gen", Generate, true );

            Commands.AddCommand( "zone", ZoneAdd, false );
            Commands.AddCommand( "zones", ZoneList, false );
            Commands.AddCommand( "zremove", ZoneRemove, false );
            Commands.AddCommand( "ztest", ZoneTest, false );

            Commands.AddCommand( "worlds", WorldList, true );
            Commands.AddCommand( "wload", WorldLoad, true );
            Commands.AddCommand( "wremove", WorldRemove, true );
            Commands.AddCommand( "wrename", WorldRename, true );
            Commands.AddCommand( "waccess", WorldAccess, true );
            Commands.AddCommand( "wmain", WorldMain, true );
            Commands.AddCommand( "wbuild", WorldBuild, true );
            Commands.AddCommand( "save", Save, true );

            //Commands.AddCommand( "landmark", AddLandmark, false);
        }


        internal static void Join( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                player.Message( "Usage: " + Color.Help + "/join worldName" );
                return;
            }
            World world = Server.FindWorld( worldName );
            if( world != null ) {
                if( world.classAccess.rank > player.info.playerClass.rank ) {
                    player.Message( "Cannot join world \"" + world.name + "\": must be " + world.classAccess.name + " or higher." );
                } else {
                    player.session.JoinWorld( world, true );
                }
            } else {
                player.Message( "No world found with the name \"" + worldName + "\"." );
            }
        }


        internal static void Save( Player player, Command cmd ) {
            if( !player.Can( Permissions.ManageWorlds ) ) {
                player.NoAccessMessage( Permissions.ManageWorlds );
                return;
            }

            string mapName = cmd.Next();
            if( mapName == null ) {
                player.Message( "Syntax: " + Color.Help + "/save mapName" );
                return;
            }

            string mapFileName = Path.GetFileName( mapName ) + ".fcm";
            player.Message( "Saving map to \"" + mapFileName + "\"..." );
            if( player.world.map.Save( mapFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                player.Message( "Map saving failed. See server logs for details." );
            }
        }

        #region World Commands

        internal static void WorldMain( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                player.Message( "Usage: " + Color.Help + "/wmain WorldName" );
                return;
            }
            World world = Server.FindWorld( worldName );
            if( world == null ) {
                player.Message( "No world \"" + worldName + "\" found." );
            } else if( world == Server.mainWorld ) {
                player.Message( "World \"" + world.name + "\" is already set as main." );
            } else if( player.Can( Permissions.ManageWorlds ) ) {
                if( world.classAccess != ClassList.lowestClass ) {
                    world.classAccess = ClassList.lowestClass;
                    player.Message( "The main world cannot have access restrictions." );
                    player.Message( "Access restrictions were removed from world \"" + world.name + "\"" );
                }
                world.neverUnload = false;
                world.LoadMap();
                Server.mainWorld.neverUnload = true;
                Server.mainWorld = world;
                Server.SaveWorldList();

                Server.SendToAll( Color.Sys + player.nick + " set \"" + world.name + "\" to be the main world." );
                Logger.Log( player.GetLogName() + " set \"" + world.name + "\" to be the main world.", LogType.UserActivity );
            } else {
                player.NoAccessMessage( Permissions.ManageWorlds );
            }
        }


        internal static void WorldAccess( Player player, Command cmd ) {
            string worldName = cmd.Next();
            string className = cmd.Next();

            if( worldName == null ) {
                if( player.world != null ) {
                    if( player.world.classAccess == ClassList.lowestClass ) {
                        player.Message( "This world (" + player.world.name + ") can be visited by anyone." );
                    } else {
                        player.Message( "This world (" + player.world.name + ") can only be visited by " + player.world.classAccess.color + player.world.classAccess.name + "+" );
                    }
                } else {
                    player.Message( "When calling /waccess from console, you must specify the world name." );
                }
                return;
            }

            World world = Server.FindWorld( worldName );
            if( world == null ) {
                player.Message( "No world \"" + worldName + "\" found." );
            } else if( className == null ) {
                if( world.classAccess == ClassList.lowestClass ) {
                    player.Message( "World \"" + world.name + "\" can be visited by anyone." );
                } else {
                    player.Message( "World \"" + world.name + "\" can only be visited by " + world.classAccess.color + world.classAccess.name + "+" );
                }
            } else if( player.Can( Permissions.ManageWorlds ) ) {
                PlayerClass playerClass = ClassList.FindClass( className );
                if( playerClass == null ) {
                    player.Message( "No class \"" + className + "\" found." );
                } else if( world == Server.mainWorld ) {
                    player.Message( "The main world cannot have access restrictions." );
                } else {
                    world.classAccess = playerClass;
                    Server.SaveWorldList();
                    if( world.classAccess == ClassList.lowestClass ) {
                        Server.SendToAll( Color.Sys + player.nick + " made the world \"" + world.name + "\" accessible to anyone." );
                    } else {
                        Server.SendToAll( Color.Sys + player.nick + " made the world \"" + world.name + "\" accessible only to " + world.classAccess.color + world.classAccess.name + "+" );
                    }
                    Logger.Log( player.GetLogName() + " made the world \"" + world.name + "\" accessible to " + world.classAccess.name + "+", LogType.UserActivity );
                }
            } else {
                player.NoAccessMessage( Permissions.ManageWorlds );
            }
        }


        internal static void WorldBuild( Player player, Command cmd ) {
            string worldName = cmd.Next();
            string className = cmd.Next();

            if( worldName == null ) {
                if( player.world != null ) {
                    if( player.world.classBuild == ClassList.lowestClass ) {
                        player.Message( "This world (" + player.world.name + ") can be modified by anyone." );
                    } else {
                        player.Message( "This world (" + player.world.name + ") can only be modified by " + player.world.classBuild.color + player.world.classBuild.name + "+" );
                    }
                } else {
                    player.Message( "When calling /waccess from console, you must specify the world name." );
                }
                return;
            }

            World world = Server.FindWorld( worldName );
            if( world == null ) {
                player.Message( "No world \"" + worldName + "\" found." );
            } else if( className == null ) {
                if( world.classBuild == ClassList.lowestClass ) {
                    player.Message( "World \"" + world.name + "\" can be modified by anyone." );
                } else {
                    player.Message( "World \"" + world.name + "\" can be only modified by " + world.classBuild.color + world.classBuild.name + "+" );
                }
            } else if( player.Can( Permissions.ManageWorlds ) ) {
                PlayerClass playerClass = ClassList.FindClass( className );
                if( playerClass == null ) {
                    player.Message( "No class \"" + className + "\" found." );
                } else{
                    world.classBuild = playerClass;
                    Server.SaveWorldList();
                    if( world.classBuild == ClassList.lowestClass ) {
                        Server.SendToAll( Color.Sys + player.nick + " made the world \"" + world.name + "\" modifiable by anyone." );
                    } else {
                        Server.SendToAll( Color.Sys + player.nick + " made the world \"" + world.name + "\" modifiable only by " + world.classBuild.color + world.classBuild.name + "+" );
                    }
                    Logger.Log( player.GetLogName() + " made the world \"" + world.name + "\" modifiable by " + world.classBuild.name + "+", LogType.UserActivity );
                }
            } else {
                player.NoAccessMessage( Permissions.ManageWorlds );
            }
        }


        internal static void WorldList( Player player, Command cmd ) {
            lock( Server.worldListLock ) {
                string line = "List of worlds: ";
                bool first = true;
                foreach( World world in Server.worlds.Values ) {
                    if( world.isHidden ) continue;
                    if( line.Length + world.name.Length > 62 ) {
                        player.Message( line );
                        line = "";
                    } else if(!first) {
                        line += ", ";
                    }
                    line += world.name;
                    first = false;
                }
                player.Message( line );
            }
        }


        internal static void WorldLoad( Player player, Command cmd ) {
            if( !player.Can( Permissions.ManageWorlds ) ) {
                player.NoAccessMessage( Permissions.ManageWorlds );
                return;
            }

            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if( worldName == null && player.world == null ) {
                player.Message( "When using /wload from console, you must specify the world name." );
                return;
            }

            if( fileName == null ) {
                // No params given at all
                player.Message( "See " + Color.Help + "/help wload" + Color.Sys + " for usage syntax." );
                return;
            }

            Logger.Log( "Player {0} is attempting to load map \"{1}\"...", LogType.UserActivity,
                        player.GetLogName(),
                        fileName );
            player.Message( "Attempting to load " + fileName + "..." );

            Map map = Map.Load( player.world, fileName );
            if( map == null ) {
                player.Message( "Could not load specified file." );
                return;
            }

            if( worldName == null ) {
                // Loading to current world
                player.world.ChangeMap( map );
                player.world.SendToAll( Color.Sys + player.nick + " loaded a new map for the world \"" + player.world.name + "\".", player );
                player.Message( "New map for the world \"" + player.world.name + "\" has been loaded." );

                Logger.Log( player.GetLogName() + " loaded new map for " + player.world.name + " from " + fileName, LogType.UserActivity );

            } else {
                // Loading to some other (or new) world
                if( !Player.IsValidName( worldName ) ) {
                    player.Message( "Invalid world name: \"" + worldName + "\"." );
                    return;
                }

                lock( Server.worldListLock ) {
                    World world = Server.FindWorld( worldName );
                    if( world != null ) {
                        // Replacing existing world's map
                        world.ChangeMap( map );
                        world.SendToAll( Color.Sys + player.nick + " loaded a new map for the world \"" + world.name + "\".", player );
                        player.Message( "New map for the world \"" + world.name + "\" has been loaded." );
                        Logger.Log( player.GetLogName() + " loaded new map for world \"" + world.name + "\" from " + fileName, LogType.UserActivity );

                    } else {
                        // Adding a new world
                        if( Server.AddWorld( worldName, map, false ) != null ) {
                            Server.SendToAll( Color.Sys + player.nick + " created a new world named \"" + worldName + "\"." );
                            Logger.Log( player.GetLogName() + " created a new world named \"" + worldName + "\".", LogType.UserActivity );
                            Server.SaveWorldList();
                        } else {
                            player.Message( "Error occured while trying to create a new world." );
                        }
                    }
                }
            }

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        internal static void WorldRename( Player player, Command cmd ) {
            if( !player.Can( Permissions.ManageWorlds ) ) {
                player.NoAccessMessage( Permissions.ManageWorlds );
                return;
            }

            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                player.Message( "Syntax: " + Color.Help + "/wrename OldName NewName" );
                return;
            }

            lock( Server.worldListLock ) {
                World oldWorld = Server.FindWorld( oldName );
                World newWorld = Server.FindWorld( newName );

                if( oldWorld == null ) {
                    player.Message( "No world found with the specified name: " + oldName );
                } else if( newWorld != null ) {
                    player.Message( "A world with the specified name already exists: " + newName );
                } else {
                    oldName = oldWorld.name;
                    Server.RenameWorld( oldName, newName );
                    File.Move( oldName + ".fcm", newName + ".fcm" );
                    Server.SaveWorldList();
                    Server.SendToAll( Color.Sys + player.nick + " renamed the world \"" + oldName + "\" to \"" + newName + "\"." );
                    Logger.Log( player.GetLogName() + " renamed the world \"" + oldName + "\" to \"" + newName + "\".", LogType.UserActivity );
                }
            }
        }


        internal static void WorldRemove( Player player, Command cmd ) {
            if( !player.Can( Permissions.ManageWorlds ) ) {
                player.NoAccessMessage( Permissions.ManageWorlds );
                return;
            }

            string worldName = cmd.Next();
            if( worldName == null ) {
                player.Message( "Syntax: " + Color.Help + "/wremove WorldName" );
                return;
            }

            lock( Server.worldListLock ) {
                World world = Server.FindWorld( worldName );
                if( world == null ) {
                    player.Message( "World not found: " + worldName );
                } else if( world == Server.mainWorld ) {
                    player.Message( "Deleting the main world is not allowed. Assign a new main first." );
                } else{
                    Server.RemoveWorld( worldName );
                    Server.SendToAll( Color.Sys + player.nick + " deleted the world \"" + world.name + "\"", player );
                    player.Message( "Removed \"" + world.name + "\" from the world list." );
                    player.Message( "You can now delete the map file (" + world.name + ".fcm) manually." );
                }
            }

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }

        #endregion

        #region Zone Commands

        internal static void ZoneAdd( Player player, Command cmd ) {//TODO: better method names & documentation
            if( !player.Can( Permissions.ManageZones ) ) {
                player.NoAccessMessage( Permissions.ManageZones );
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
                player.selectionCallback = ZoneAddCallback;
                player.Message( "Zone: Place a block or type /mark to use your location." );
            } else {
                player.Message( "Unknown player class: " + property );
            }
        }


        internal static void ZoneAddCallback( Player player, Position[] marks, object tag ) {//TODO: better method names
            Zone zone = (Zone)tag;
            zone.xMin = Math.Min( marks[0].x, marks[1].x );
            zone.xMax = Math.Max( marks[0].x, marks[1].x );
            zone.yMin = Math.Min( marks[0].y, marks[1].y );
            zone.yMax = Math.Max( marks[0].y, marks[1].y );
            zone.hMin = Math.Min( marks[0].h, marks[1].h );
            zone.hMax = Math.Max( marks[0].h, marks[1].h );
            player.Message( "Zone \"" + zone.name + "\" created, " + zone.GetVolume() + " blocks total." );
            Logger.Log( "Player {0} created a new zone \"{1}\" containing {2} blocks.", LogType.UserActivity,
                                  player.name,
                                  zone.name,
                                  zone.GetVolume() );
            player.world.map.AddZone(zone);
        }


        static void ZoneTest( Player player, Command cmd ) {
            player.marksExpected = 1;
            player.marks.Clear();
            player.markCount = 0;
            player.selectionCallback = ZoneTestCallback;
            player.Message( "Click the block that you would like to test." );
        }


        internal static void ZoneTestCallback( Player player, Position[] marks, object tag ) {
            Zone[] allowed, denied;
            if( player.world.map.TestZones( marks[0].x, marks[0].y, marks[0].h, player, out allowed, out denied ) ) {
                foreach( Zone zone in allowed ) {
                    player.Message( "> " + zone.name + ": " + Color.Lime + "allowed" );
                }
                foreach( Zone zone in denied ) {
                    player.Message( "> " + zone.name + ": " + Color.Red + "denied" );
                }
            } else {
                player.Message( "No zones affect this block." );
            }
        }


        internal static void ZoneRemove( Player player, Command cmd ) {
            if( !player.Can( Permissions.ManageZones ) ) {
                player.NoAccessMessage( Permissions.ManageZones );
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


        internal static void ZoneList( Player player, Command cmd ) {
            Zone[] zones = player.world.map.ListZones();
            if( zones.Length > 0 ) {
                foreach( Zone zone in zones ) {
                    PlayerClass rank = ClassList.ParseRank( zone.buildRank );
                    if( rank != null ) {
                        player.Message( "  " + zone.name + " (" + rank.color + rank.name + Color.Sys + ") - " + zone.GetWidthX() + "x" + zone.GetWidthY() + "x" + zone.GetHeight() );
                    } else {
                        player.Message( "  " + zone.name + " - " + zone.GetWidthX() + "x" + zone.GetWidthY() + "x" + zone.GetHeight() );
                    }
                }
            } else {
                player.Message( "No zones are defined for this map." );
            }
        }

        #endregion

        #region Commented Out Stuff

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


        // old stream loading code
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

        #endregion

        #region Generation

        internal static void Generate( Player player, Command cmd ) {
            if( !player.Can( Permissions.ManageWorlds ) ) {
                player.NoAccessMessage( Permissions.ManageWorlds );
                return;
            }
            int wx, wy, height;
            if( !(cmd.NextInt( out wx ) && cmd.NextInt( out wy ) && cmd.NextInt( out height )) ) {
                if( player.world != null ) {
                    wx = player.world.map.widthX;
                    wy = player.world.map.widthY;
                    height = player.world.map.height;
                } else {
                    player.Message( "Usage: " + Color.Help + "/gen widthX widthY height type filename" );
                    return;
                }
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

        #endregion

        #region Locking

        internal static void Lock( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                player.NoAccessMessage( Permissions.Lock );
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
            if( world.isLocked ) {
                player.Message( "The world is already locked." );
            } else {
                world.Lock();
            }
        }


        internal static void LockAll( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                player.NoAccessMessage( Permissions.Lock );
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
                player.NoAccessMessage( Permissions.Lock );
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
            if( !world.isLocked ) {
                player.Message( "The world is already unlocked." );
            } else {
                world.Unlock();
            }
        }


        internal static void UnlockAll( Player player, Command cmd ) {
            if( !player.Can( Permissions.Lock ) ) {
                player.NoAccessMessage( Permissions.Lock );
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


        #endregion
    }
}
