// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Text;


namespace fCraft {
    /// <summary>
    /// Contains commands related to world management.
    /// </summary>
    static class WorldCommands {
        internal static void Init() {
            CommandList.RegisterCommand( cdJoin );

            CommandList.RegisterCommand( cdWorldSave );
            CommandList.RegisterCommand( cdWorldMain );
            CommandList.RegisterCommand( cdWorldAccess );
            CommandList.RegisterCommand( cdWorldBuild );
            CommandList.RegisterCommand( cdWorlds );
            CommandList.RegisterCommand( cdWorldLoad );
            CommandList.RegisterCommand( cdWorldRename );
            CommandList.RegisterCommand( cdWorldRemove );
            CommandList.RegisterCommand( cdWorldFlush );

            CommandList.RegisterCommand( cdWorldHide );
            CommandList.RegisterCommand( cdWorldUnhide );

            CommandList.RegisterCommand( cdGenerate );

            CommandList.RegisterCommand( cdLock );
            CommandList.RegisterCommand( cdLockAll );
            CommandList.RegisterCommand( cdUnlock );
            CommandList.RegisterCommand( cdUnlockAll );
        }


        static CommandDescriptor cdWorldHide = new CommandDescriptor {
            name = "whide",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/whide WorldName",
            help = "Hides the specified world from the &H/worlds&S list. " +
                   "Hidden worlds can be seen by typing &H/worlds all",
            handler = WorldHide
        };

        internal static void WorldHide( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdWorldAccess.PrintUsage( player );
                return;
            }

            World world;
            World[] worlds = Server.FindWorlds( worldName );
            if( worlds.Length == 0 ) {
                player.NoWorldMessage( worldName );
                return;
            } else if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );
                return;
            } else {
                world = worlds[0];
            }

            if( world.isHidden ) {
                player.Message( "World \"{0}&S\" is already hidden.", world.GetClassyName() );
            } else {
                player.Message( "World \"{0}&S\" is now hidden.", world.GetClassyName() );
                world.isHidden = true;
                Server.SaveWorldList();
            }
        }



        static CommandDescriptor cdWorldUnhide = new CommandDescriptor {
            name = "wunhide",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wunhide WorldName",
            help = "Unhides the specified world from the &H/worlds&S list. " +
                   "Hidden worlds can be listed by typing &H/worlds all",
            handler = WorldUnhide
        };

        internal static void WorldUnhide( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdWorldAccess.PrintUsage( player );
                return;
            }

            World world;
            World[] worlds = Server.FindWorlds( worldName );
            if( worlds.Length == 0 ) {
                player.NoWorldMessage( worldName );
                return;
            } else if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );
                return;
            } else {
                world = worlds[0];
            }

            if( world.isHidden ) {
                player.Message( "World \"{0}&S\" is no longer hidden.", world.GetClassyName() );
                world.isHidden = false;
                Server.SaveWorldList();
            } else {
                player.Message( "World \"{0}&S\" is not hidden.", world.GetClassyName() );
            }
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

            World[] worlds = Server.FindWorlds( worldName );
            if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );

            } else if( worlds.Length == 1 ) {
                World world = worlds[0];
                if( player.CanJoin( world ) ) {
                    if( !player.session.JoinWorldNow( world, false ) ) {
                        player.Message( "Failed to join world." );
                    }
                } else {
                    player.Message( "Cannot join world {0}&S: must be {1}+",
                                    world.GetClassyName(), world.accessRank.GetClassyName() );
                }

            } else {
                // no worlds found - see if player meant to type in "/join" and not "/tp"
                Player[] players = Server.FindPlayers( player, worldName );
                if( players.Length == 1 ) {
                    player.ParseMessage( "/tp " + players[0].name, false );
                } else {
                    player.NoWorldMessage( worldName );
                }
            }
        }


        #region World Commands



        static CommandDescriptor cdWorldSave = new CommandDescriptor {
            name = "wsave",
            consoleSafe = true,
            aliases = new string[] { "save" },
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wsave FileName &Sor&H /save WorldName FileName",
            help = "Saves a map copy to a file with the specified name. " +
                   "The \".fcm\" file extension can be omitted. " +
                   "If a file with the same name already exists, it will be overwritten.",
            handler = WorldSave
        };

        internal static void WorldSave( Player player, Command cmd ) {
            string p1 = cmd.Next(), p2 = cmd.Next();
            if( p1 == null ) {
                cdWorldSave.PrintUsage( player );
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
                World[] worlds = Server.FindWorlds( p1 );
                if( worlds.Length == 0 ) {
                    player.NoWorldMessage( p1 );
                } else if( worlds.Length > 1 ) {
                    player.ManyMatchesMessage( "world", worlds );
                } else {
                    world = worlds[0];
                }
            }

            if( !fileName.ToLower().EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                fileName += ".fcm";
            }
            string fullFileName = Path.Combine( Paths.MapPath, fileName );

            if( File.Exists( fullFileName ) ) {
                FileInfo targetFile = new FileInfo( fullFileName );
                FileInfo sourceFile = new FileInfo( world.GetMapName() );
                if( !targetFile.FullName.Equals( sourceFile.FullName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( !cmd.confirmed ) {
                        player.AskForConfirmation( cmd, "Target file \"{0}\" already exists, and will be overwritten.", targetFile.Name );
                        return;
                    }
                }
            }


            player.MessageNow( "Saving map to {0}", fileName );

            string mapSavingError = "Map saving failed. See server logs for details.";
            Map map = world.map;
            if( map == null ) {
                if( File.Exists( world.GetMapName() ) ) {
                    try {
                        File.Copy( world.GetMapName(), fullFileName, true );
                    } catch( Exception ex ) {
                        Logger.Log( "StandardCommands.Save: Error occured while trying to copy an unloaded map: {0}", LogType.Error, ex );
                        player.Message( mapSavingError );
                    }
                } else {
                    Logger.Log( "StandardCommands.Save: Map for world \"{0}\" is unloaded, and file does not exist.", LogType.Error, world.name );
                    player.Message( mapSavingError );
                }
            } else if( map.Save( fullFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( "StandardCommands.Save: Saving world \"{0}\" failed.", LogType.Error, world.name );
                player.Message( mapSavingError );
            }
        }

        static CommandDescriptor cdWorldFlush = new CommandDescriptor {
            name = "wflush",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wflush [WorldName]",
            help = "Flushes the update buffer on specified map by causing players to rejoin. " +
                   "Makes cuboids and other draw commands finish REALLY fast.",
            handler = WorldFlush
        };

        internal static void WorldFlush( Player player, Command cmd ) {
            string worldName = cmd.Next();
            World world = player.world;

            if( worldName != null ) {
                World[] worlds = Server.FindWorlds( worldName );
                if( worlds.Length == 0 ) {
                    player.NoWorldMessage( worldName );
                    return;
                } else if( worlds.Length > 1 ) {
                    player.ManyMatchesMessage( "world", worlds );
                    return;
                } else {
                    world = worlds[0];
                }
            } else if( player.world == null ) {
                player.Message( "When using /wflush from console, you must specify a world name." );
                return;
            }

            if( world.map == null ) {
                player.MessageNow( "WFlush: {0}&S has no updates to process.",
                                   world.GetClassyName() );
            } else {
                player.MessageNow( "WFlush: Flushing {0}&S ({1} blocks in queue)...",
                                   world.GetClassyName(),
                                   world.map.UpdateQueueSize() );

                world.BeginFlushMapBuffer();
            }
        }


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

            World world;
            World[] worlds = Server.FindWorlds( worldName );
            if( worlds.Length == 0 ) {
                player.NoWorldMessage( worldName );
                return;
            } else if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );
                return;
            } else {
                world = worlds[0];
            }

            if( world == null ) {
                player.NoWorldMessage( worldName );
            } else if( world == Server.mainWorld ) {
                player.Message( "World {0}&S is already set as main.", world.GetClassyName() );
            } else {
                if( world.accessRank != RankList.LowestRank ) {
                    world.accessRank = RankList.LowestRank;
                    player.Message( "The main world cannot have access restrictions." );
                    player.Message( "Access restrictions were removed from world {0}",
                                    world.GetClassyName() );
                }
                world.neverUnload = true;
                world.LoadMap();
                Server.mainWorld.neverUnload = false;
                Server.mainWorld = world;
                Server.SaveWorldList();

                Server.SendToAll( "{0}&S set {1}&S to be the main world.",
                                  player.GetClassyName(), world.GetClassyName() );
                Logger.Log( "{0} set {1} to be the main world.", LogType.UserActivity,
                            player.name, world.name );
            }
        }



        static CommandDescriptor cdWorldAccess = new CommandDescriptor {
            name = "waccess",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/waccess [WorldName [RankName]]",
            help = "Shows access permission for player's current world. " +
                   "If optional WorldName parameter is given, shows access permission for another world. " +
                   "If RankName parameter is also given, sets access permission for specified world.",
            handler = WorldAccess
        };

        internal static void WorldAccess( Player player, Command cmd ) {
            string worldName = cmd.Next();

            if( worldName == null ) {
                if( player == Player.Console ) {
                    player.Message( "When calling /waccess from console, you must specify the world name." );
                } else {
                    if( player.world.accessRank == RankList.LowestRank ) {
                        player.Message( "This world ({0}&S) can be visited by anyone.",
                                        player.world.GetClassyName() );
                    } else {
                        player.Message( "This world ({0}&S) can only be visited by {1}+",
                                        player.world.GetClassyName(),
                                        player.world.accessRank.GetClassyName() );
                    }
                }
                return;
            }

            World world;
            World[] worlds = Server.FindWorlds( worldName );
            if( worlds.Length == 0 ) {
                player.NoWorldMessage( worldName );
                return;
            } else if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );
                return;
            } else {
                world = worlds[0];
            }


            string name;
            bool changesWereMade = false;

            do {
                name = cmd.Next();
                if( name == null ) {
                    if( world.accessRank == RankList.LowestRank ) {
                        player.Message( "World {0}&S can be visited by anyone.",
                                        world.GetClassyName() );
                    } else {
                        player.Message( "World {0}&S can only be visited by {1}+",
                                        world.GetClassyName(),
                                        world.accessRank.GetClassyName() );
                    }
                    return;

                } else if( world == Server.mainWorld ) {
                    player.Message( "The main world cannot have access restrictions." );
                    return;

                } else if( name.Length < 2 ) {
                    continue;
                }

                if( name.StartsWith( "+" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        return;
                    }

                    if( info == null ) {
                        player.NoPlayerMessage( name.Substring( 1 ) );
                        return;
                    }

                    // prevent players from whitelisting themselves to bypass protection
                    if( player.info == info ) {
                        if( !world.accessSecurity.CanBuild( player ) ) {
                            player.Message( "You must be {0}+&S to add yourself to this zone's whitelist.",
                                            world.accessSecurity.minRank.GetClassyName() );
                            continue;
                        }
                    }

                    switch( world.accessSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is no longer excluded from world {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now included in world {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already included in world {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            break;
                    }

                } else if( name.StartsWith( "-" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        return;
                    }

                    if( info == null ) {
                        player.NoPlayerMessage( name.Substring( 1 ) );
                        return;
                    }

                    switch( world.accessSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already barred from zone {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from zone {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is no longer specially allowed in zone {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            changesWereMade = true;
                            break;
                    }

                } else {
                    Rank rank = RankList.FindRank( name );
                    if( rank == null ) {
                        player.NoRankMessage( name );
                    } else if( world.accessRank > rank && world.accessRank > player.info.rank ) {
                        player.Message( "Cannot lower access permission for world {0}&S: Must be {1}+",
                                        world.GetClassyName(), world.accessRank.GetClassyName() );
                    } else {
                        world.accessRank = rank;
                        changesWereMade = true;
                        if( world.accessRank == RankList.LowestRank ) {
                            Server.SendToAll( "{0}&S made the world {1}&S accessible to anyone.",
                                              player.GetClassyName(), world.GetClassyName() );
                        } else {
                            Server.SendToAll( "{0}&S made the world {1}&S accessible only to {2}+",
                                              player.GetClassyName(), world.GetClassyName(), world.accessRank.GetClassyName() );
                        }
                        Logger.Log( "{0} made the world \"{1}\" accessible to {2}+", LogType.UserActivity,
                                    player.name, world.name, world.accessRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                Server.SaveWorldList();
            }
        }



        static CommandDescriptor cdWorldBuild = new CommandDescriptor {
            name = "wbuild",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/wbuild [WorldName [RankName]]",
            help = "Shows build permission for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If RankName parameter is also given, sets build permission for specified world.",
            handler = WorldBuild
        };

        internal static void WorldBuild( Player player, Command cmd ) {
            string worldName = cmd.Next();
            string rankName = cmd.Next();

            if( worldName == null ) {
                if( player.world != null ) {
                    if( player.world.buildRank == RankList.LowestRank ) {
                        player.Message( "This world ({0}&S) can be modified by anyone.",
                                        player.world.GetClassyName() );
                    } else {
                        player.Message( "This world ({0}&S) can only be modified by {1}+",
                                        player.world.GetClassyName(),
                                        player.world.buildRank.GetClassyName() );
                    }
                } else {
                    player.Message( "When calling /waccess from console, you must specify the world name." );
                }
                return;
            }

            World world;
            World[] worlds = Server.FindWorlds( worldName );
            if( worlds.Length == 0 ) {
                player.NoWorldMessage( worldName );
                return;
            } else if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );
                return;
            } else {
                world = worlds[0];
            }

            if( rankName == null ) {
                if( world.buildRank == RankList.LowestRank ) {
                    player.Message( "World {0}&S can be modified by anyone.",
                                    world.GetClassyName() );
                } else {
                    player.Message( "World {0}&S can be only modified by {1}+",
                                    world.GetClassyName(),
                                    world.buildRank.GetClassyName() );
                }
            } else {
                Rank rank = RankList.FindRank( rankName );
                if( rank == null ) {
                    player.NoRankMessage( rankName );
                } else if( world.buildRank > rank && world.buildRank > player.info.rank ) {
                    player.Message( "Cannot lower build permission for world {0}&S: Must be {1}+",
                                    world.GetClassyName(), world.buildRank.GetClassyName() );
                } else {
                    world.buildRank = rank;
                    Server.SaveWorldList();
                    if( world.buildRank == RankList.LowestRank ) {
                        Server.SendToAll( "{0}&S made the world {1}&S modifiable by anyone.",
                                          player.GetClassyName(), world.GetClassyName() );
                    } else {
                        Server.SendToAll( "{0}&S made the world {1}&S modifiable only by {2}+",
                                          player.GetClassyName(), world.GetClassyName(), world.buildRank.GetClassyName() );
                    }
                    Logger.Log( "{0} made the world \"{1}\" modifiable by {2}+", LogType.UserActivity,
                                player.name, world.name, world.buildRank.Name );
                }
            }
        }



        static CommandDescriptor cdWorlds = new CommandDescriptor {
            name = "worlds",
            consoleSafe = true,
            aliases = new string[] { "maps", "levels" },
            usage = "/worlds [all|hidden]",
            help = "Shows a list of worlds available for you to join. " +
                   "If the optional \"all\" is added, also shows unavailable (restricted) worlds. " +
                   "If \"hidden\" is added, shows only hidden and inaccessible worlds.",
            handler = Worlds
        };

        internal static void Worlds( Player player, Command cmd ) {
            string param = cmd.Next();
            bool listVisible = true,
                 listHidden = false;
            if( !String.IsNullOrEmpty( param ) ) {
                if( param[0] == 'a' || param[0] == 'A' ) {
                    listHidden = true;
                } else if( param[0] == 'h' || param[0] == 'H' ) {
                    listVisible = false;
                    listHidden = true;
                } else {
                    cdWorlds.PrintUsage( player );
                    return;
                }
            }

            StringBuilder sb = new StringBuilder();
            bool first = true;
            int count = 0;

            lock( Server.worldListLock ) {
                foreach( World world in Server.worlds.Values ) {
                    bool visible = player.CanJoin( world ) && !world.isHidden;
                    if( (visible && listVisible) || (!visible && listHidden) ) {
                        if( !first ) {
                            sb.Append( ", " );
                        }
                        sb.Append( world.GetClassyName() );
                        count++;
                        first = false;
                    }
                }
            }

            if( listVisible && !listHidden ) {
                player.MessagePrefixed( "&S   ", "There are " + count + " available worlds: " + sb.ToString() );
            } else if( listHidden && !listVisible ) {
                player.MessagePrefixed( "&S   ", "There are " + count + " hidden worlds: " + sb.ToString() );
            } else {
                player.MessagePrefixed( "&S   ", "There are " + count + " worlds total: " + sb.ToString() );
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
                        player.name, fileName );
            player.MessageNow( "Loading {0}...", fileName );

            Map map = Map.Load( player.world, fileName );
            if( map == null ) {
                player.MessageNow( "Could not load specified file." );
                return;
            }

            if( !File.Exists( fileName ) && !Directory.Exists( fileName ) ) {
                if( File.Exists( Path.Combine( Paths.MapPath, fileName ) ) ) {
                    fileName = Path.Combine( Paths.MapPath, fileName );
                } else if( File.Exists( fileName + ".fcm" ) ) {
                    fileName += ".fcm";
                } else if( File.Exists( Path.Combine( Paths.MapPath, fileName + ".fcm" ) ) ) {
                    fileName = Path.Combine( Paths.MapPath, fileName + ".fcm" );
                } else if( Directory.Exists( Path.Combine( Paths.MapPath, fileName ) ) ) {
                    fileName = Path.Combine( Paths.MapPath, fileName );
                } else {
                    player.Message( "File/directory not found: {0}", fileName );
                    return;
                }
            }

            if( worldName == null ) {
                if( !cmd.confirmed ) {
                    player.AskForConfirmation( cmd, "About to replace THIS MAP with \"{0}\".", fileName );
                    return;
                }
                // Loading to current world
                player.world.ChangeMap( map );
                player.world.SendToAllExcept( "{0}&S loaded a new map for this world.", player,
                                              player.GetClassyName() );
                player.MessageNow( "New map loaded for the world {0}", player.world.GetClassyName() );

                Logger.Log( "{0} loaded new map for world \"{1}\" from {2}", LogType.UserActivity,
                            player.name, player.world.name, fileName );

            } else {
                // Loading to some other (or new) world
                if( !Player.IsValidName( worldName ) ) {
                    player.MessageNow( "Invalid world name: \"{0}\".", worldName );
                    return;
                }


                lock( Server.worldListLock ) {
                    World world = Server.FindWorld( worldName );
                    if( world != null ) {
                        if( !cmd.confirmed ) {
                            player.AskForConfirmation( cmd, "About to replace map for {0}&S with \"{1}\".", world.GetClassyName(), fileName );
                            return;
                        }
                        // Replacing existing world's map
                        world.ChangeMap( map );
                        world.SendToAllExcept( "{0}&S loaded a new map for the world {1}", player,
                                               player.GetClassyName(), world.GetClassyName() );
                        player.MessageNow( "New map for the world {0}&S has been loaded.", world.GetClassyName() );
                        Logger.Log( "{0} loaded new map for world \"{1}\" from {2}", LogType.UserActivity,
                                    player.name, world.name, fileName );

                    } else {
                        string targetFileName = Path.Combine( Paths.MapPath, worldName + ".fcm" );
                        if( worldName != fileName && File.Exists( targetFileName ) && File.Exists( fileName ) ) {
                            FileInfo targetFile = new FileInfo( targetFileName );
                            FileInfo sourceFile = new FileInfo( fileName );
                            if( !targetFile.FullName.Equals( sourceFile.FullName, StringComparison.OrdinalIgnoreCase ) ) {
                                if( !cmd.confirmed ) {
                                    player.AskForConfirmation( cmd, "A map named \"{0}\" already exists, and will be overwritten with \"{1}\".",
                                                               targetFile.Name, sourceFile.Name );
                                    return;
                                }
                            }
                        }

                        // Adding a new world
                        World newWorld = Server.AddWorld( worldName, map, false );
                        if( newWorld != null ) {
                            Rank newBuildRank = RankList.ParseRank( Config.GetString( ConfigKey.DefaultBuildRank ) );
                            if( newBuildRank != null ) {
                                newWorld.buildRank = newBuildRank;
                            }
                            Server.SendToAll( "{0}&S created a new world named {1}",
                                              player.GetClassyName(), newWorld.GetClassyName() );
                            Logger.Log( "{0} created a new world named \"{1}\" (loaded from \"{2}\")", LogType.UserActivity,
                                        player.name, worldName, fileName );
                            Server.SaveWorldList();
                            player.MessageNow( "Reminder: New world's access permission is {0}+&S, and build permission is {1}+",
                                               newWorld.accessRank.GetClassyName(),
                                               newWorld.buildRank.GetClassyName() );
                        } else {
                            player.MessageNow( "Error occured while trying to create a new world." );
                        }
                    }
                }
            }

            Server.RequestGC();
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
                World[] oldWorlds = Server.FindWorlds( oldName );
                if( oldWorlds.Length > 1 ) {
                    player.ManyMatchesMessage( "world", oldWorlds );
                    return;
                } else if( oldWorlds.Length == 0 ) {
                    player.NoWorldMessage( oldName );
                    return;
                }
                World oldWorld = oldWorlds[0];
                World newWorld = Server.FindWorld( newName );

                // the "oldWorld != newWorld" check allows changing capitalization without triggering "world already exists"
                if( newWorld != null && oldWorld != newWorld ) {
                    player.Message( "A world with the specified name already exists: {0}", newName );

                } else {
                    oldName = oldWorld.name;

                    lock( oldWorld.mapLock ) {
                        Server.RenameWorld( oldName, newName );

                        // Move files
                        string oldFileName = Path.Combine( Paths.MapPath, oldName + ".fcm" );
                        string newFileName = Path.Combine( Paths.MapPath, newName + ".fcm" );
                        try {
                            if( File.Exists( newFileName ) ) File.Replace( oldFileName, newFileName, null, true );
                            else File.Move( oldFileName, newFileName );
                        } catch( Exception ex ) {
                            Logger.Log( "MapCommands.WorldRename: A file with the same name as renamed world may already exist, " +
                                        "and an error occured while trying to use it: {0}", LogType.Error, ex );
                        }
                    }

                    Server.SaveWorldList();
                    Server.SendToAll( "{0}&S renamed the world \"{1}\" to \"{2}\"",
                                      player.GetClassyName(), oldName, newName );
                    Logger.Log( "{0} renamed the world \"{1}\" to \"{2}\".", LogType.UserActivity,
                                player.name, oldName, newName );
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
                World[] worlds = Server.FindWorlds( worldName );
                if( worlds.Length > 1 ) {
                    player.ManyMatchesMessage( "world", worlds );
                    return;
                } else if( worlds.Length == 0 ) {
                    player.NoWorldMessage( worldName );
                    return;
                }
                World world = worlds[0];

                if( world == Server.mainWorld ) {
                    player.Message( "Deleting the main world is not allowed. Assign a new main first." );
                } else if( Server.RemoveWorld( worldName ) ) {
                    Server.SendToAllExcept( "{0}&S removed {1}&S from the world list.", player,
                                            player.GetClassyName(), world.GetClassyName() );
                    player.Message( "Removed {0}&S from the world list. You can now delete the map file ({0}.fcm) manually.",
                                    world.GetClassyName(), world.name );
                    Logger.Log( "{0} removed \"{1}\" from the world list.", LogType.UserActivity,
                                player.name, worldName );
                } else {
                    player.Message( "&WDeleting the world failed. See log for details." );
                }
            }

            Server.RequestGC();
        }

        #endregion

        #region Generation


        static CommandDescriptor cdGenerate = new CommandDescriptor {
            name = "gen",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ManageWorlds },
            usage = "/gen ThemeName TemplateName [X Y Height [FileName]]",
            helpHandler = delegate( Player player ) {
                return "Generates a new map. If no dimensions are given, uses current world's dimensions. " +
                       "If no filename is given, loads generated world into current world.&N" +
                       "Available themes: Grass, " + String.Join( ", ", Enum.GetNames( typeof( MapGenTheme ) ) ) + "&N" +
                       "Available terrain types: " + String.Join( ", ", Enum.GetNames( typeof( MapGenTemplate ) ) ) + "&N" +
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
            if( fileName != null && !fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                fileName += ".fcm";
            } else if( player.world == null ) {
                player.Message( "When used from console, /gen requires FileName." );
                cdGenerate.PrintUsage( player );
                return;
            }

            if( fileName == null && !cmd.confirmed ) {
                player.AskForConfirmation( cmd, "About to replace THIS MAP with a generated map." );
                return;
            }

            Map map = null;

            bool noTrees;
            if( themeName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                theme = MapGenTheme.Forest;
                noTrees = true;
            } else {
                try {
                    theme = (MapGenTheme)Enum.Parse( typeof( MapGenTheme ), themeName, true );
                    noTrees = (theme != MapGenTheme.Forest);
                } catch( Exception ) {
                    player.MessageNow( "Unrecognized theme \"{0}\". Available themes are: Grass, {1}",
                                       themeName,
                                       String.Join( ", ", Enum.GetNames( typeof( MapGenTheme ) ) ) );
                    return;
                }
            }

            try {
                template = (MapGenTemplate)Enum.Parse( typeof( MapGenTemplate ), templateName, true );
            } catch( Exception ) {
                player.Message( "Unrecognized template \"{0}\". Available templates are: {1}",
                                templateName,
                                String.Join( ", ", Enum.GetNames( typeof( MapGenTemplate ) ) ) );
                return;
            }

            if( !Enum.IsDefined( typeof( MapGenTheme ), theme ) || !Enum.IsDefined( typeof( MapGenTemplate ), template ) ) {
                cdGenerate.PrintUsage( player );
                return;
            }

            MapGeneratorArgs args = MapGenerator.MakeTemplate( template );
            args.dimX = wx;
            args.dimY = wy;
            args.dimH = height;
            args.maxHeight = (int)(args.maxHeight / 80d * height);
            args.maxDepth = (int)(args.maxDepth / 80d * height);
            args.theme = theme;
            args.addTrees = !noTrees;

            try {
                if( theme == MapGenTheme.Forest && noTrees ) {
                    player.MessageNow( "Generating Grass {0}...", template );
                } else {
                    player.MessageNow( "Generating {0} {1}...", theme, template );
                }
                if( theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat ) {
                    map = new Map( null, args.dimX, args.dimY, args.dimH );
                    MapGenerator.GenerateFlatgrass( map );
                } else {
                    MapGenerator generator = new MapGenerator( args );
                    map = generator.Generate();
                }
                map.ResetSpawn();

            } catch( Exception ex ) {
                Logger.Log( "MapGenerator: Generation failed: {0}", LogType.Error,
                            ex );
                player.MessageNow( "&WAn error occured while generating the map." );
                return;
            }

            if( map != null ) {
                if( fileName != null ) {
                    string fullFileName = Path.Combine( Paths.MapPath, fileName );
                    if( map.Save( fullFileName ) ) {
                        player.MessageNow( "Generation done. Saved to {0}", fileName );
                    } else {
                        player.Message( "&WAn error occured while saving generated map to {0}", fileName );
                    }
                } else {
                    player.MessageNow( "Generation done. Changing map..." );
                    player.world.ChangeMap( map );
                }
            } else {
                player.Message( "&WAn error occured while generating the map." );
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
                World[] worlds = Server.FindWorlds( worldName );
                if( worlds.Length > 1 ) {
                    player.ManyMatchesMessage( "world", worlds );
                    return;
                } else if( worlds.Length == 0 ) {
                    player.NoWorldMessage( worldName );
                    return;
                }
                world = worlds[0];

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
                World[] worlds = Server.FindWorlds( worldName );
                if( worlds.Length > 1 ) {
                    player.ManyMatchesMessage( "world", worlds );
                    return;
                } else if( worlds.Length == 0 ) {
                    player.NoWorldMessage( worldName );
                    return;
                }
                world = worlds[0];

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