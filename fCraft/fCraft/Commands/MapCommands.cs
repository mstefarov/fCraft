// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Text;


namespace fCraft {
    static class MapCommands {
        internal static void Init() {
            CommandList.RegisterCommand( cdJoin );

            CommandList.RegisterCommand( cdSave );
            CommandList.RegisterCommand( cdWorldMain );
            CommandList.RegisterCommand( cdWorldAccess );
            CommandList.RegisterCommand( cdWorldBuild );
            CommandList.RegisterCommand( cdWorldList );
            CommandList.RegisterCommand( cdWorldLoad );
            CommandList.RegisterCommand( cdWorldRename );
            CommandList.RegisterCommand( cdWorldRemove );

            CommandList.RegisterCommand( cdZoneEdit );
            CommandList.RegisterCommand( cdZoneAdd );
            CommandList.RegisterCommand( cdZoneTest );
            CommandList.RegisterCommand( cdZoneList );
            CommandList.RegisterCommand( cdZoneRemove );

            CommandList.RegisterCommand( cdGenerate );

            CommandList.RegisterCommand( cdLock );
            CommandList.RegisterCommand( cdLockAll );
            CommandList.RegisterCommand( cdUnlock );
            CommandList.RegisterCommand( cdUnlockAll );
        }



        static CommandDescriptor cdJoin = new CommandDescriptor {
            name = "join",
            aliases = new string[] { "j", "load", "l", "goto", "map" },
            usage = "/join WorldName",
            help = "Teleports the player to a specified world. You can see the list of available worlds by using &H/worlds",
            handler = Join
        };

        internal static void Join( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdJoin.PrintUsage( player );
                return;
            }
            World world = Server.FindWorld( worldName );
            if( world != null ) {
                if( player.CanJoin( world ) ) {
                    if( !player.session.JoinWorldNow( world, true ) ) {
                        player.Message( "Failed to join world." );
                    }
                } else {
                    player.Message( "Cannot join world \"" + world.name + "\": must be " + world.classAccess.color + world.classAccess.name + Color.Sys + " or higher." );
                }
            } else {
                player.Message( "No world found with the name \"" + worldName + "\"." );
            }
        }



        static CommandDescriptor cdSave = new CommandDescriptor {
            name = "save",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/save FileName &Sor&H /save WorldName FileName",
            help = "Saves a map copy to a file with the specified name. " +
                   "The \".fcm\" file extension can be omitted. " +
                   "If a file with the same name already exists, it will be overwritten.",
            handler = Save
        };

        internal static void Save( Player player, Command cmd ) {
            string p1 = cmd.Next(), p2 = cmd.Next();
            if( p1 == null ) {
                cdSave.PrintUsage( player );
                return;
            }

            World world = player.world;
            string fileName;
            if( p2 == null ) {
                fileName = p1;
                if( world == null ) {
                    player.Message( "When called from console, /save requires WorldName. See \"/help save\" for details." );
                    return;
                }
            } else {
                fileName = p2;
                world = Server.FindWorld( p1 );
                if( world == null ) {
                    player.Message( "No world found named \"" + p1 + "\"." );
                    return;
                }
            }


            string mapFileName = "maps/" + fileName + ".fcm";

            player.Message( "Saving map to {0}", mapFileName );

            string mapSavingError = "Map saving failed. See server logs for details.";
            Map map = world.map;
            if( map == null ) {
                if( File.Exists( world.GetMapName() ) ) {
                    try {
                        File.Copy( world.GetMapName(), mapFileName, true );
                    } catch( Exception ex ) {
                        Logger.Log( "StandardCommands.Save: Error occured while trying to copy an unloaded map: " + ex, LogType.Error );
                        player.Message( mapSavingError );
                    }
                } else {
                    Logger.Log( "StandardCommands.Save: Map for world \"" + world.name + "\" is unloaded, and file does not exist.", LogType.Error );
                    player.Message( mapSavingError );
                }
            } else if( map.Save( mapFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( "StandardCommands.Save: Saving world \"" + world.name + "\" failed.", LogType.Error );
                player.Message( mapSavingError );
            }
        }



        #region World Commands

        static CommandDescriptor cdWorldMain = new CommandDescriptor {
            name = "wmain",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wmain [WorldName]",
            help = "Sets the specified world as the new main world. Main world is what newly-connected players join first.",
            handler = WorldMain
        };

        internal static void WorldMain( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdWorldMain.PrintUsage( player );
                return;
            }

            World world = Server.FindWorld( worldName );
            if( world == null ) {
                player.Message( "No world \"{0}\" found.", worldName );
            } else if( world == Server.mainWorld ) {
                player.Message( "World \"{0}\" is already set as main.", world.name );
            } else {
                if( world.classAccess != ClassList.lowestClass ) {
                    world.classAccess = ClassList.lowestClass;
                    player.Message( "The main world cannot have access restrictions." );
                    player.Message( "Access restrictions were removed from world \"{0}\"", world.name );
                }
                world.neverUnload = true;
                world.LoadMap();
                Server.mainWorld.neverUnload = false;
                Server.mainWorld = world;
                Server.SaveWorldList();

                Server.SendToAll( Color.Sys + player.nick + " set \"" + world.name + "\" to be the main world." );
                Logger.Log( "{0} set \"{1}\" to be the main world.", LogType.UserActivity,
                            player.GetLogName(), world.name );
            }
        }



        static CommandDescriptor cdWorldAccess = new CommandDescriptor {
            name = "waccess",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/waccess [WorldName [ClassName]]",
            help = "Shows access permission for player's current world. " +
                   "If optional WorldName parameter is given, shows access permission for another world. " +
                   "If ClassName parameter is also given, sets access permission for specified world.",
            handler = WorldAccess
        };

        internal static void WorldAccess( Player player, Command cmd ) {
            string worldName = cmd.Next();
            string className = cmd.Next();

            if( worldName == null ) {
                if( player.world != null ) {
                    if( player.world.classAccess == ClassList.lowestClass ) {
                        player.Message( "This world ({0}) can be visited by anyone.", player.world.name );
                    } else {
                        player.Message( "This world ({0}) can only be visited by {1}{2}+",
                                        player.world.name,
                                        player.world.classAccess.color,
                                        player.world.classAccess.name );
                    }
                } else {
                    player.Message( "When calling /waccess from console, you must specify the world name." );
                }
                return;
            }

            World world = Server.FindWorld( worldName );
            if( world == null ) {
                player.Message( "No world \"{0}\" found.", worldName );
            } else if( className == null ) {
                if( world.classAccess == ClassList.lowestClass ) {
                    player.Message( "World \"{0}\" can be visited by anyone.", world.name );
                } else {
                    player.Message( "World \"{0}\" can only be visited by {1}{2}+",
                        world.name,
                        world.classAccess.color,
                        world.classAccess.name );
                }
            } else {
                PlayerClass playerClass = ClassList.FindClass( className );
                if( playerClass == null ) {
                    player.Message( "No class \"{0}\" found.", className );
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
                    Logger.Log( "{0} made the world \"{1}\" accessible to {2}+", LogType.UserActivity,
                                player.GetLogName(),
                                world.name,
                                world.classAccess.name );
                }
            }
        }



        static CommandDescriptor cdWorldBuild = new CommandDescriptor {
            name = "wbuild",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wbuild [WorldName [ClassName]]",
            help = "Shows build permission for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If ClassName parameter is also given, sets build permission for specified world.",
            handler = WorldBuild
        };

        internal static void WorldBuild( Player player, Command cmd ) {
            string worldName = cmd.Next();
            string className = cmd.Next();

            if( worldName == null ) {
                if( player.world != null ) {
                    if( player.world.classBuild == ClassList.lowestClass ) {
                        player.Message( "This world ({0}) can be modified by anyone.", player.world.name );
                    } else {
                        player.Message( "This world ({0}) can only be modified by {1}{2}+",
                                        player.world.name,
                                        player.world.classBuild.color,
                                        player.world.classBuild.name );
                    }
                } else {
                    player.Message( "When calling /waccess from console, you must specify the world name." );
                }
                return;
            }

            World world = Server.FindWorld( worldName );
            if( world == null ) {
                player.Message( "No world \"{0}\" found.", worldName );
            } else if( className == null ) {
                if( world.classBuild == ClassList.lowestClass ) {
                    player.Message( "World \"{0}\" can be modified by anyone.", worldName );
                } else {
                    player.Message( "World \"{0}\" can be only modified by {1}{2}+",
                                    worldName,
                                    world.classBuild.color,
                                    world.classBuild.name );
                }
            } else {
                PlayerClass playerClass = ClassList.FindClass( className );
                if( playerClass == null ) {
                    player.Message( "No class \"{0}\" found.", className );
                } else {
                    world.classBuild = playerClass;
                    Server.SaveWorldList();
                    if( world.classBuild == ClassList.lowestClass ) {
                        Server.SendToAll( Color.Sys + player.nick + " made the world \"" + world.name + "\" modifiable by anyone." );
                    } else {
                        Server.SendToAll( Color.Sys + player.nick + " made the world \"" + world.name + "\" modifiable only by " + world.classBuild.color + world.classBuild.name + "+" );
                    }
                    Logger.Log( "{0} made the world \"{1}\" modifiable by {2}+", LogType.UserActivity,
                                player.GetLogName(),
                                world.name,
                                world.classBuild.name );
                }
            }
        }



        static CommandDescriptor cdWorldList = new CommandDescriptor {
            name = "worlds",
            consoleSafe = true,
            usage = "/worlds [all]",
            help = "Shows a list of worlds available for you to join. " +
                   "If the optional \"all\" is added, also shows unavailable (restricted) worlds.",
            handler = WorldList
        };

        internal static void WorldList( Player player, Command cmd ) {
            lock( Server.worldListLock ) {
                bool listAll = (cmd.Next() != null);
                StringBuilder sb = new StringBuilder();
                if( listAll ) {
                    sb.Append( "List of all worlds: " );
                } else {
                    sb.Append( "List of available worlds: " );
                }

                bool first = true;
                foreach( World world in Server.worlds.Values ) {
                    if( world.isHidden ) continue;
                    if( !first ) {
                        sb.Append( ", " );
                    }
                    if( player.CanJoin( world ) ) {
                        sb.Append( world.name );
                    } else if( listAll ) {
                        sb.Append( Color.Red ).Append( world.name ).Append( Color.Sys );
                    }
                    first = false;
                }
                player.MessagePrefixed( "&S    ", sb.ToString() );
            }
        }



        static CommandDescriptor cdWorldLoad = new CommandDescriptor {
            name = "wload",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wload FileName [WorldName]",
            help = "If WorldName parameter is not given, replaces the current world's map with the specified map. The old map is overwritten. " +
                   "If the world with the specified name exists, its map is replaced with the specified map file. " +
                   "Otherwise, a new world is created using the given name and map file. " +
                   "Supported formats: fCraft (fcm), MCSharp/MCZall (lvl), vanilla (server_level.dat), MinerCPP/LuaCraft (dat), " +
                   "JTE (gz), indev (mclevel). Note: infinite maps NOT supported.",
            handler = WorldLoad
        };

        internal static void WorldLoad( Player player, Command cmd ) {
            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if( worldName == null && player.world == null ) {
                player.Message( "When using /wload from console, you must specify the world name." );
                return;
            }

            if( fileName == null ) {
                // No params given at all
                cdWorldLoad.PrintUsage( player );
                return;
            }

            Logger.Log( "Player {0} is attempting to load map \"{1}\"...", LogType.UserActivity,
                        player.GetLogName(),
                        fileName );
            player.Message( "Loading {0}...", fileName );

            Map map = Map.Load( player.world, fileName );
            if( map == null ) {
                player.Message( "Could not load specified file." );
                return;
            }

            if( worldName == null ) {
                // Loading to current world
                player.world.ChangeMap( map );
                player.world.SendToAll( Color.Sys + player.nick + " loaded a new map for the world \"" + player.world.name + "\".", player );
                player.Message( "New map for the world \"{0}\" has been loaded.", player.world.name );

                Logger.Log( "{0} loaded new map for {1} from {2}", LogType.UserActivity,
                            player.GetLogName(),
                            player.world.name,
                            fileName );

            } else {
                // Loading to some other (or new) world
                if( !Player.IsValidName( worldName ) ) {
                    player.Message( "Invalid world name: \"{0}\".", worldName );
                    return;
                }

                lock( Server.worldListLock ) {
                    World world = Server.FindWorld( worldName );
                    if( world != null ) {
                        // Replacing existing world's map
                        world.ChangeMap( map );
                        world.SendToAll( Color.Sys + player.nick + " loaded a new map for the world \"" + world.name + "\".", player );
                        player.Message( "New map for the world \"{0}\" has been loaded.", world.name );
                        Logger.Log( "{0} loaded new map for world \"{1}\" from {2}", LogType.UserActivity,
                                    player.GetLogName(),
                                    world.name,
                                    fileName );

                    } else {
                        // Adding a new world
                        if( Server.AddWorld( worldName, map, false ) != null ) {
                            Server.SendToAll( Color.Sys + player.nick + " created a new world named \"" + worldName + "\"." );
                            Logger.Log( "{0} created a new world named \"{1}\". from {2}", LogType.UserActivity,
                                        player.GetLogName(),
                                        worldName,
                                        fileName );
                            Server.SaveWorldList();
                        } else {
                            player.Message( "Error occured while trying to create a new world." );
                        }
                    }
                }
            }

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }



        static CommandDescriptor cdWorldRename = new CommandDescriptor {
            name = "wrename",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wrename OldName NewName",
            help = "Changes the name of a world. Does not require any reloading.",
            handler = WorldRename
        };

        internal static void WorldRename( Player player, Command cmd ) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                cdWorldRename.PrintUsage( player );
                return;
            }

            lock( Server.worldListLock ) {
                World oldWorld = Server.FindWorld( oldName );
                World newWorld = Server.FindWorld( newName );

                if( oldWorld == null ) {
                    player.Message( "No world found with the specified name: {0}", oldName );
                } else if( newWorld != null ) {
                    player.Message( "A world with the specified name already exists: {0}", newName );
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
                                player.GetLogName(),
                                oldName,
                                newName );
                }
            }
        }



        static CommandDescriptor cdWorldRemove = new CommandDescriptor {
            name = "wremove",
            aliases = new string[] { "wdelete" },
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wremove WorldName",
            help = "Removes the specified world from the world list, and moves all players from it to the main world. " +
                   "The main world itself cannot be removed with this command. You will need to delete the map file manually.",
            handler = WorldRemove
        };

        internal static void WorldRemove( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdWorldRemove.PrintUsage( player );
                return;
            }

            lock( Server.worldListLock ) {
                World world = Server.FindWorld( worldName );
                if( world == null ) {
                    player.Message( "World not found: {0}", worldName );
                } else if( world == Server.mainWorld ) {
                    player.Message( "Deleting the main world is not allowed. Assign a new main first." );
                } else {
                    Server.RemoveWorld( worldName );
                    Server.SendToAll( Color.Sys + player.nick + " deleted the world \"" + world.name + "\"", player );
                    player.Message( "Removed \"{0}\" from the world list.", world.name );
                    player.Message( "You can now delete the map file ({0}.fcm) manually.", world.name );
                    Logger.Log( "{0} removed the world \"{1}\"", LogType.UserActivity, player.GetLogName(), worldName );
                }
            }

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }

        #endregion

        #region Zone Commands

        static CommandDescriptor cdZoneEdit = new CommandDescriptor {
            name = "zedit",
            permissions = new Permission[] { Permission.ManageZones },
            usage = "/zedit ZoneName ClassName",
            help = "Allows editing the zone permissions after creation.",
            handler = ZoneEdit
        };

        internal static void ZoneEdit( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( "No zone name specified. See &H/help zedit" );
                return;
            }

            Zone zone;
            if( player.world.map.zones.ContainsKey( name.ToLower() ) ) {
                zone = player.world.map.zones[name.ToLower()];
            } else {
                player.Message( "No zone found with the name \"{0}\". See &H/zones", name );
                return;
            }

            string property = cmd.Next();
            if( property == null ) {
                player.Message( "No class name specified. See &H/help zedit" );
                return;
            }

            PlayerClass minRank = ClassList.ParseClass( property );
            if( minRank == null ) {
                player.Message( "Unrecognized class name: \"{0}\"", property );
                return;
            } else {
                zone.build = minRank;
                player.world.map.changesSinceSave++;
                player.world.SaveMap( null );
                player.Message( "Permission for zone \"{0}\" changed to {1}{2}+",
                                name,
                                minRank.color,
                                minRank.name );
            }
        }



        static CommandDescriptor cdZoneAdd = new CommandDescriptor {
            name = "zadd",
            aliases = new string[] { "zone" },
            permissions = new Permission[] { Permission.ManageZones },
            usage = "/zadd ZoneName ClassName",
            help = "Create a zone that overrides build permissions. " +
                   "This can be used to restrict access to an area (by setting ClassName to a high rank) " +
                   "or to designate a guest area (by setting ClassName to a class that normally can't build).",
            handler = ZoneAdd
        };

        internal static void ZoneAdd( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdZoneAdd.PrintUsage( player );
                return;
            }

            if( !Player.IsValidName( name ) ) {
                player.Message( "\"{0}\" is not a valid zone name", name );
                return;
            }

            if( player.world.map.zones.ContainsKey( name.ToLower() ) ) {
                player.Message( "A zone with this name already exists. Use &H/zedit&S to edit." );
                return;
            }

            Zone zone = new Zone();
            zone.name = name;

            string property = cmd.Next();
            if( property == null ) {
                player.Message( "No zone rank/whitelist/blacklist specified. See &H/help zone" );
                return;
            }
            PlayerClass minRank = ClassList.ParseClass( property );

            if( minRank != null ) {
                zone.build = minRank;
                player.selectionArgs = zone;
                player.selectionMarksExpected = 2;
                player.selectionMarks.Clear();
                player.selectionMarkCount = 0;
                player.selectionCallback = ZoneAddCallback;
                player.Message( "Zone: Place a block or type /mark to use your location." );
            } else {
                player.Message( "Unrecognized player class: \"{0}\"", property );
            }
        }

        internal static void ZoneAddCallback( Player player, Position[] marks, object tag ) {//TODO: better method names
            Zone zone = (Zone)tag;
            zone.bounds = new BoundingBox( marks[0], marks[1] );
            player.Message( "Zone \"{0}\" created, {1} blocks total.",
                            zone.name,
                            zone.bounds.GetVolume() );
            Logger.Log( "Player {0} created a new zone \"{1}\" containing {2} blocks.", LogType.UserActivity,
                                  player.name,
                                  zone.name,
                                  zone.bounds.GetVolume() );
            player.world.map.AddZone( zone );
        }



        static CommandDescriptor cdZoneTest = new CommandDescriptor {
            name = "ztest",
            help = "Allows to test exactly which zones affect a particular block. Can be used to find and resolve zone overlaps.",
            handler = ZoneTest
        };

        static void ZoneTest( Player player, Command cmd ) {
            player.selectionMarksExpected = 1;
            player.selectionMarks.Clear();
            player.selectionMarkCount = 0;
            player.selectionCallback = ZoneTestCallback;
            player.Message( "Click the block that you would like to test." );
        }


        internal static void ZoneTestCallback( Player player, Position[] marks, object tag ) {
            Zone[] allowed, denied;
            if( player.world.map.TestZones( marks[0].x, marks[0].y, marks[0].h, player, out allowed, out denied ) ) {
                foreach( Zone zone in allowed ) {
                    player.Message( "> {0}: {1}allowed", zone.name, Color.Lime );
                }
                foreach( Zone zone in denied ) {
                    player.Message( "> {0}: {1}denied", zone.name, Color.Red );
                }
            } else {
                player.Message( "No zones affect this block." );
            }
        }



        static CommandDescriptor cdZoneRemove = new CommandDescriptor {
            name = "zremove",
            aliases = new string[] { "zdelete" },
            permissions = new Permission[] { Permission.ManageZones },
            usage = "/zremove ZoneName",
            help = "Removes a zone with the specified name from the map.",
            handler = ZoneRemove
        };

        internal static void ZoneRemove( Player player, Command cmd ) {
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



        static CommandDescriptor cdZoneList = new CommandDescriptor {
            name = "zones",
            help = "Lists all zones defined on the current map/world.",
            handler = ZoneList
        };

        internal static void ZoneList( Player player, Command cmd ) {
            Zone[] zones = player.world.map.ListZones();
            if( zones.Length > 0 ) {
                foreach( Zone zone in zones ) {
                    player.Message( String.Format( "  {0} ({1}{2}{3}) - {4}x{5}x{6}",
                                                   zone.name,
                                                   zone.build.color,
                                                   zone.build.name,
                                                   Color.Sys,
                                                   zone.bounds.GetWidthX(),
                                                   zone.bounds.GetWidthY(),
                                                   zone.bounds.GetHeight() ) );
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


        static CommandDescriptor cdGenerate = new CommandDescriptor {
            name = "gen",
            consoleSafe = true,
            permissions = new Permission[]{ Permission.ManageWorlds},
            usage = "/gen ThemeName TemplateName [Width Height Depth [FileName]]",
            helpHandler = delegate( Player player ) {
                return "Generates a new map. If no dimensions are given, uses current world's dimensions. " +
                       "If no filename is given, loads generated world into current world. Available themes:&N" +
                       String.Join( ",", Enum.GetNames( typeof( MapGenTheme ) ) ) + "&N" +
                       "Available terrain types:&N" +
                       "Flatgrass, " + String.Join( ", ", Enum.GetNames( typeof( MapGenTemplate ) ) ) + "&N" +
                       "NOTE: Map is saved TO FILE ONLY, use /wload to load it.";
            },
            handler = Generate
        };

        internal static void Generate( Player player, Command cmd ) {

            string themeName = cmd.Next();
            string templateName = cmd.Next();

            if( templateName == null ) {
                cdGenerate.PrintUsage( player );
                return;
            }

            MapGenTemplate template;
            MapGenTheme theme;

            int wx, wy, height;
            if( !(cmd.NextInt( out wx ) && cmd.NextInt( out wy ) && cmd.NextInt( out height )) ) {
                if( player.world != null ) {
                    wx = player.world.map.widthX;
                    wy = player.world.map.widthY;
                    height = player.world.map.height;
                } else {
                    player.Message( "When used from console, /gen requires map dimensions." );
                    cdGenerate.PrintUsage( player );
                    return;
                }
                cmd.Rewind();
                cmd.Next();
                cmd.Next();
            }


            string fileName = cmd.Next();
            if( fileName != null ) {
                if( !fileName.StartsWith( "maps/" ) ) {
                    fileName = "maps/" + fileName;
                }
                if( !fileName.ToLower().EndsWith( ".fcm" ) ) {
                    fileName += ".fcm";
                }

            } else if( player.world == null ) {
                player.Message( "When used from console, /gen requires FileName." );
                cdGenerate.PrintUsage( player );
                return;
            }

            Map map = null;

            if( templateName == "flatgrass" ) {
                player.Message( "Generating flatgrass map..." );
                map = new Map( player.world, wx, wy, height );
                map.ResetSpawn();
                MapGenerator.GenerateFlatgrass( map );

            } else {

                try {
                    theme = (MapGenTheme)Enum.Parse( typeof( MapGenTheme ), themeName, true );
                } catch( Exception ) {
                    player.Message( "Unrecognized theme \"" + themeName + "\". Available themes are:" );
                    player.Message( String.Join( ", ", Enum.GetNames( typeof( MapGenTheme ) ) ) );
                    return;
                }

                try {
                    template = (MapGenTemplate)Enum.Parse( typeof( MapGenTemplate ), templateName, true );
                } catch( Exception ) {
                    player.Message( "Unrecognized template \"" + templateName + "\". Available templates are:" );
                    player.Message( "Flatgrass, " + String.Join( ", ", Enum.GetNames( typeof( MapGenTemplate ) ) ) );
                    return;
                }

                MapGeneratorArgs args = MapGenerator.MakeTemplate( template );
                args.dimX = wx;
                args.dimY = wy;
                args.dimH = height;
                args.maxHeight = (int)(args.maxHeight / 80d * height);
                args.maxDepth = (int)(args.maxDepth / 80d * height);
                args.theme = theme;
                args.addTrees = (theme == MapGenTheme.Forest);
                try {
                    MapGenerator generator = new MapGenerator( args );
                    player.Message( "Generating " + theme.ToString() + " " + template.ToString() + "..." );
                    map = generator.Generate();
                    map.ResetSpawn();
                } catch( Exception ex ) {
                    Logger.Log( "MapGenerator: Generation failed: " + ex, LogType.Error );
                    player.Message( Color.Red + "An error occured while generating the map." );
                    return;
                }
            }

            if( map != null ) {
                if( fileName != null ) {
                    if( map.Save( fileName ) ) {
                        player.Message( "Generation done. Saved to " + fileName );
                    } else {
                        player.Message( Color.Red + "An error occured while saving generated map." );
                    }
                } else {
                    player.Message( "Generation done. Changing map..." );
                    player.world.ChangeMap( map );
                }
            } else {
                player.Message( Color.Red + "An error occured while generating the map." );
            }
        }

        #endregion

        #region Locking

        static CommandDescriptor cdLock = new CommandDescriptor {
            name = "lock",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Lock },
            usage = "/lock [WorldName]",
            help = "Puts the world into a locked, read-only mode. " +
                   "No one can place or delete blocks during lockdown. " +
                   "By default this locks the world you're on, but you can also lock any world by name. " +
                   "Call &H/unlock&S to release lock on a world, or &H/unlockall&S to release all worlds at once.",
            handler = Lock
        };

        internal static void Lock( Player player, Command cmd ) {
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

            if( !world.Lock( player ) ) {
                player.Message( "The world is already locked." );
            }
        }



        static CommandDescriptor cdLockAll = new CommandDescriptor {
            name = "lockall",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Lock },
            help = "Applies &H/lock&S to all available worlds.",
            handler = LockAll
        };

        internal static void LockAll( Player player, Command cmd ) {
            lock( Server.worldListLock ) {
                foreach( World world in Server.worlds.Values ) {
                    world.Lock( player );
                }
            }
            player.Message( "All worlds are now locked." );
        }



        static CommandDescriptor cdUnlock = new CommandDescriptor {
            name = "unlock",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Lock },
            usage = "/unlock [WorldName]",
            help = "Removes the lockdown set by &H/lock&S. See &H/help lock&S for more information.",
            handler = Unlock
        };

        internal static void Unlock( Player player, Command cmd ) {
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

            if( !world.Unlock( player ) ) {
                player.Message( "The world is already unlocked." );
            }
        }



        static CommandDescriptor cdUnlockAll = new CommandDescriptor {
            name = "unlockall",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Lock },
            help = "Applies &H/unlock&S to all available worlds",
            handler = UnlockAll
        };

        internal static void UnlockAll( Player player, Command cmd ) {
            lock( Server.worldListLock ) {
                foreach( World world in Server.worlds.Values ) {
                    world.Unlock( player );
                }
            }
            player.Message( "All worlds are now unlocked." );
        }
        #endregion
    }
}