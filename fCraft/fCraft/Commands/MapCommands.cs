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
                    player.Message( "Cannot join world \"" + world.name + "\": must be " + world.classAccess.color + world.classAccess.name + Color.Sys + " or higher." );
                } else {
                    if( !player.session.JoinWorldNow( world, true ) ) {
                        player.Message( "Failed to join world." );
                    }
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

            string fileName = cmd.Next();
            if( fileName == null ) {
                player.Message( "Syntax: " + Color.Help + "/save mapName" );
                return;
            }

            string mapFileName = "maps/" + fileName + ".fcm";
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
                world.neverUnload = true;
                world.LoadMap();
                Server.mainWorld.neverUnload = false;
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
                } else {
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
                    } else if( !first ) {
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

                    lock( oldWorld.mapLock ) {
                        Server.RenameWorld( oldName, newName );

                        // Move files
                        string oldFileName = "maps/" + oldName + ".fcm";
                        string newFileName = "maps/" + newName + ".fcm";
                        try {
                            File.Delete( newFileName );
                            File.Move( oldFileName, newFileName );
                        } catch( Exception ex ) {
                            Logger.Log( "MapCommands.WorldRename: A file with the same name as renamed world may already exist, " +
                                        "and an error occured while trying to use it: " + ex, LogType.Error );
                        }
                    }

                    Server.SaveWorldList();
                    Server.SendToAll( Color.Sys + player.nick + " renamed the world \"" + oldName + "\" to \"" + newName + "\"." );
                    Logger.Log( "{0} renamed the world \"{1}\" to \"{2}\".", LogType.UserActivity,
                                player.GetLogName(), oldName, newName );
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
                } else {
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
                player.drawArgs = zone;
                player.marksExpected = 2;
                player.drawMarks.Clear();
                player.drawMarkCount = 0;
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
            player.world.map.AddZone( zone );
        }


        static void ZoneTest( Player player, Command cmd ) {
            player.marksExpected = 1;
            player.drawMarks.Clear();
            player.drawMarkCount = 0;
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
                    player.Message( "See " + Color.Help + "/help gen" + Color.Sys + " for usage information." );
                    return;
                }
                cmd.Rewind();
            }

            string themeName = cmd.Next();
            string typeName = cmd.Next();
            string fileName = cmd.Next();
            if( fileName == null ) {
                player.Message( "See " + Color.Help + "/help gen" + Color.Sys + " for usage information." );
                return;
            }

            if( !fileName.StartsWith( "maps/" ) ) {
                fileName = "maps/" + fileName;
            }
            if( !fileName.ToLower().EndsWith( ".fcm" ) ) {
                fileName += ".fcm";
            }

            Map map = new Map( player.world, wx, wy, height );
            map.ResetSpawn();

            if( typeName == "flatgrass" ) {
                player.Message( "Generating flatgrass map..." );
                MapGenerator.GenerateFlatgrass( map );

                if( map.Save( fileName ) ) {
                    player.Message( "Map generation: Done." );
                } else {
                    player.Message( Color.Red, "An error occured while generating the map." );
                }
            } else if( typeName == "empty" ) {
                player.Message( "Generating empty map..." );
                map.MakeFloodBarrier();

                if( map.Save( fileName ) ) {
                    player.Message( "Map generation: Done." );
                } else {
                    player.Message( Color.Red, "An error occured while generating the map." );
                }
            } else {
                MapGenType type;
                MapGenTheme theme;
                try {
                    theme = (MapGenTheme)Enum.Parse( typeof( MapGenTheme ), themeName, true );
                } catch( Exception ) {
                    player.Message( "Unrecognized theme \"" + themeName + "\". Available themes are:" );
                    player.Message( String.Join( ", ", Enum.GetNames( typeof( MapGenTheme ) ) ) );
                    return;
                }

                try {
                    type = (MapGenType)Enum.Parse( typeof( MapGenType ), typeName, true );
                } catch( Exception ) {
                    player.Message( "Unrecognized terrain type \"" + themeName + "\". Available types are:" );
                    player.Message( "Empty,Flatgrass," + String.Join( ", ", Enum.GetNames( typeof( MapGenType ) ) ) );
                    return;
                }

                Tasks.Add( MapGenerator.GenerationTask, new MapGenerator( map, player, fileName, type, theme ), false );
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

            World world;
            if( worldName != null ) {
                world = Server.FindWorld( worldName );
                if( world == null ) {
                    player.Message( "No world found with the name \"" + worldName + "\"." );
                    return;
                }
            } else if( player.world != null ) {
                world = player.world;
            } else {
                player.Message( "When called from console, /lock requires a world name." );
                return;
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
            } else {
                lock( Server.worldListLock ) {
                    foreach( World world in Server.worlds.Values ) {
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

            World world;
            if( worldName != null ) {
                world = Server.FindWorld( worldName );
                if( world == null ) {
                    player.Message( "No world found with the name \"" + worldName + "\"." );
                    return;
                }
            } else if( player.world != null ) {
                world = player.world;
            } else {
                player.Message( "When called from console, /lock requires a world name." );
                return;
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