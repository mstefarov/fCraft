// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace fCraft {
    /// <summary>
    /// Contains commands related to world management.
    /// </summary>
    static class WorldCommands {
        internal static void Init() {
            CommandList.RegisterCommand( cdJoin );

            CommandList.RegisterCommand( cdWorldInfo );

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


        static CommandDescriptor cdWorldInfo = new CommandDescriptor {
            name = "winfo",
            aliases = new string[] { "mapinfo" },
            consoleSafe = true,
            usage = "/winfo [WorldName]",
            help = "Shows information about a world: player count, map dimensions, permissions, etc." +
                   "If no WorldName is given, shows info for current world.",
            handler = WorldInfo
        };

        internal static void WorldInfo( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                if( player.world == null ) {
                    player.Message( "Please specify a world name when calling /winfo form console." );
                    return;
                } else {
                    worldName = player.world.name;
                }
            }

            World world = Server.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            player.Message( "World {0}&S has {1} player(s) on.",
                            world.GetClassyName(),
                            world.playerList.Length );

            // If map is not currently loaded, grab its header from disk
            Map map = world.map;
            if( map == null ) {
                map = Map.LoadHeaderOnly( world.GetMapName() );
            }
            if( map == null ) {
                player.Message( "Map information could not be loaded." );
            } else {
                player.Message( "Map dimensions are {0} x {1} x {2}",
                                map.widthX, map.widthY, map.height );
            }

            // Print access/build limits
            world.accessSecurity.PrintDescription( player, world, "world", "accessed" );
            world.buildSecurity.PrintDescription( player, world, "world", "modified" );

            // Print lock/unlock information
            if( world.isLocked ) {
                player.Message( "{0}&S was locked {1:0}min ago by {2}",
                                world.GetClassyName(),
                                DateTime.UtcNow.Subtract( world.lockedDate ).TotalMinutes,
                                world.lockedBy );
            } else if( world.unlockedBy != null ) {
                player.Message( "{0}&S was unlocked {1:0}min ago by {2}",
                                world.GetClassyName(),
                                DateTime.UtcNow.Subtract( world.lockedDate ).TotalMinutes,
                                world.lockedBy );
            }
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

            World world = Server.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

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

            World world = Server.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

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
                switch( world.accessSecurity.CheckDetailed( player.info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if( !player.session.JoinWorldNow( world, false ) ) {
                            player.Message( "ERROR: Failed to join world. See log for details." );
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot join world {0}&S: you are blacklisted",
                                        world.GetClassyName(), world.accessSecurity.minRank.GetClassyName() );
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot join world {0}&S: must be {1}+",
                                        world.GetClassyName(), world.accessSecurity.minRank.GetClassyName() );
                        break;
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
                world = Server.FindWorldOrPrintMatches( player, p1 );
                if( world == null ) return;
                fileName = p2;
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
                world = Server.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

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

            World world = Server.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) {
                return;

            } else if( world == Server.mainWorld ) {
                player.Message( "World {0}&S is already set as main.", world.GetClassyName() );

            } else if( !player.info.rank.AllowSecurityCircumvention && !player.CanJoin( world ) ) {
                // Prevent players from exploiting /wmain to gain access to restricted maps
                switch( world.accessSecurity.CheckDetailed( player.info ) ) {
                    case SecurityCheckResult.RankTooHigh:
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "You are not allowed to set {0}&S as the main world (by rank).", world.GetClassyName() );
                        return;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "You are not allowed to set {0}&S as the main world (blacklisted).", world.GetClassyName() );
                        return;
                }

            } else {
                if( world.accessSecurity.minRank != RankList.LowestRank ) {
                    world.accessSecurity.minRank = RankList.LowestRank;
                    PlayerInfo[] excludedPlayers = world.accessSecurity.exceptionList.excluded;
                    foreach( PlayerInfo excludedPlayer in excludedPlayers ) {
                        world.accessSecurity.Include( excludedPlayer );
                        Logger.Log( "Player {0} was removed from the access blacklist on world {1} (wmain).", LogType.SystemActivity );
                    }
                    player.Message( "The main world cannot have access restrictions. " +
                                    "All access restrictions were removed from world {0}",
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

            // Print information about the current world
            if( worldName == null ) {
                if( player == Player.Console ) {
                    player.Message( "When calling /waccess from console, you must specify a world name." );
                } else {
                    player.world.accessSecurity.PrintDescription( player, player.world, "world", "accessed" );
                }
                return;
            }

            // Find a world by name
            World world = Server.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name;
            bool changesWereMade = false;
            do {
                name = cmd.Next();
                if( name == null ) {
                    world.accessSecurity.PrintDescription( player, world, "world", "accessed" );
                    return;

                } else if( world == Server.mainWorld ) {
                    player.Message( "The main world cannot have access restrictions." );
                    return;

                } else if( name.Length < 2 ) {
                    continue;
                }

                // Whitelisting individuals
                if( name.StartsWith( "+" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;

                    } else if( info == null ) {
                        player.NoPlayerMessage( name.Substring( 1 ) );
                        continue;
                    }

                    // prevent players from whitelisting themselves to bypass protection
                    if( player.info == info && !player.info.rank.AllowSecurityCircumvention ) {
                        switch( world.accessSecurity.CheckDetailed( player.info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the access whitelist of {0}",
                                                world.accessSecurity.minRank.GetClassyName(),
                                                world.GetClassyName() );
                                continue;
                            // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the access blacklist of {0}",
                                                world.GetClassyName() );
                                continue;
                        }
                    }

                    if( world.accessSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to access {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.accessSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.accessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from accessing {1}",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "You can now access world {0}&S (removed from blacklist by {1}&S).",
                                                    world.GetClassyName(), player.GetClassyName() );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the access blacklist of {1}&S. " +
                                                "Player is still NOT allowed to join (by rank).",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "You were removed from the access blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to join (by rank).",
                                                    player.GetClassyName(), world.GetClassyName() );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the access blacklist of {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to access {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            if( target != null ) {
                                target.Message( "You can now access world {0}&S (whitelisted by {1}&S).",
                                                world.GetClassyName(), player.GetClassyName() );
                            }
                            Logger.Log( "{0} added {1} to the access whitelist on world {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the access whitelist of {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            break;
                    }

                    // Blacklisting individuals
                } else if( name.StartsWith( "-" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;
                    } else if( info == null ) {
                        player.NoPlayerMessage( name.Substring( 1 ) );
                        continue;
                    }

                    if( world.accessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.accessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.accessSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on access blacklist of {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from accessing {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from accessing world {1}",
                                                player.GetClassyName(), world.GetClassyName() );
                            }
                            Logger.Log( "{0} added {1} to the access blacklist on world {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.accessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the access whitelist of {1}&S. " +
                                                "Player is still allowed to join (by rank).",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "You were removed from the access whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to join (by rank).",
                                                    player.GetClassyName(), world.GetClassyName() );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to access {1}",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer access world {0}&W (removed from whitelist by {1}&W).",
                                                    world.GetClassyName(), player.GetClassyName() );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the access whitelist on world {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankList.FindRank( name );
                    if( rank == null ) {
                        player.NoRankMessage( name );

                    } else if( !player.info.rank.AllowSecurityCircumvention &&
                               world.accessSecurity.minRank > rank &&
                               world.accessSecurity.minRank > player.info.rank ) {
                        player.Message( "&WYou must be ranked {1}&W+ to lower the access rank for world {0}",
                                        world.accessSecurity.minRank.GetClassyName(), world.GetClassyName() );

                    } else {
                        // list players who are redundantly blacklisted
                        SecurityController.PlayerListCollection lists = world.accessSecurity.exceptionList;
                        List<PlayerInfo> noLongerExcluded = new List<PlayerInfo>();
                        foreach( PlayerInfo excludedPlayer in lists.excluded ) {
                            if( excludedPlayer.rank >= rank ) {
                                noLongerExcluded.Add( excludedPlayer );
                            }
                        }
                        if( noLongerExcluded.Count > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerExcluded.ToArray() ) );
                        }

                        // list players who are redundantly whitelisted
                        List<PlayerInfo> noLongerIncluded = new List<PlayerInfo>();
                        foreach( PlayerInfo includedPlayer in lists.included ) {
                            if( includedPlayer.rank >= rank ) {
                                noLongerExcluded.Add( includedPlayer );
                            }
                        }
                        if( noLongerIncluded.Count > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerIncluded.ToArray() ) );
                        }

                        // apply changes
                        world.accessSecurity.minRank = rank;
                        changesWereMade = true;
                        if( world.accessSecurity.minRank == RankList.LowestRank ) {
                            Server.SendToAll( "{0}&S made the world {1}&S accessible to everyone.",
                                              player.GetClassyName(), world.GetClassyName() );
                        } else {
                            Server.SendToAll( "{0}&S made the world {1}&S accessible only by {2}+",
                                              player.GetClassyName(), world.GetClassyName(),
                                              world.accessSecurity.minRank.GetClassyName() );
                        }
                        Logger.Log( "{0} set access rank for world {1} to {2}+", LogType.UserActivity,
                                    player.name, world.name, world.accessSecurity.minRank.Name );
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
            help = "Shows build permissions for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If RankName parameter is also given, sets build permission for specified world.",
            handler = WorldBuild
        };

        internal static void WorldBuild( Player player, Command cmd ) {
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player == Player.Console ) {
                    player.Message( "When calling /wbuild from console, you must specify a world name." );
                } else {
                    player.world.buildSecurity.PrintDescription( player, player.world, "world", "modified" );
                }
                return;
            }

            // Find a world by name
            World world = Server.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name;
            bool changesWereMade = false;
            do {
                name = cmd.Next();
                if( name == null ) {
                    world.buildSecurity.PrintDescription( player, world, "world", "modified" );
                    return;

                } else if( name.Length < 2 ) {
                    continue;
                }

                // Whitelisting individuals
                if( name.StartsWith( "+" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;
                    } else if( info == null ) {
                        player.NoPlayerMessage( name.Substring( 1 ) );
                        continue;
                    }

                    // prevent players from whitelisting themselves to bypass protection
                    if( player.info == info && !player.info.rank.AllowSecurityCircumvention ) {
                        switch( world.buildSecurity.CheckDetailed( player.info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the build whitelist of {0}",
                                                world.buildSecurity.minRank.GetClassyName(),
                                                world.GetClassyName() );
                                continue;
                                // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the build blacklist of {0}",
                                                world.GetClassyName() );
                                continue;
                        }
                    }

                    if( world.buildSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.buildSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.buildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from building in {1}",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "You can now build in world {0}&S (removed from blacklist by {1}&S).",
                                                    world.GetClassyName(), player.GetClassyName() );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the build blacklist of {1}&S. " +
                                                "Player is still NOT allowed to build (by rank).",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "You were removed from the build blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to build (by rank).",
                                                    player.GetClassyName(), world.GetClassyName() );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the build blacklist of {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to build in {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            if( target != null ) {
                                target.Message( "You can now build in world {0}&S (whitelisted by {1}&S).",
                                                world.GetClassyName(), player.GetClassyName() );
                            }
                            Logger.Log( "{0} added {1} to the build whitelist on world {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the build whitelist of {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            break;
                    }

                    // Blacklisting individuals
                } else if( name.StartsWith( "-" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;
                    } else if( info == null ) {
                        player.NoPlayerMessage( name.Substring( 1 ) );
                        continue;
                    }

                    if( world.buildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.buildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from building in {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.buildSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on build blacklist of {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from building in {1}",
                                            info.GetClassyName(), world.GetClassyName() );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from building in world {1}",
                                                player.GetClassyName(), world.GetClassyName() );
                            }
                            Logger.Log( "{0} added {1} to the build blacklist on world {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.buildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the build whitelist of {1}&S. " +
                                                "Player is still allowed to build (by rank).",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "You were removed from the build whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to build (by rank).",
                                                    player.GetClassyName(), world.GetClassyName() );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to build in {1}",
                                                info.GetClassyName(), world.GetClassyName() );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer build in world {0}&W (removed from whitelist by {1}&W).",
                                                    world.GetClassyName(), player.GetClassyName() );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the build whitelist on world {2}", LogType.UserActivity,
                                        player.name, info.name, world.name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankList.FindRank( name );
                    if( rank == null ) {
                        player.NoRankMessage( name );
                    } else if( !player.info.rank.AllowSecurityCircumvention &&
                               world.buildSecurity.minRank > rank &&
                               world.buildSecurity.minRank > player.info.rank ) {
                        player.Message( "&WYou must be ranked {1}&W+ to lower build restrictions for world {0}",
                                        world.buildSecurity.minRank.GetClassyName(), world.GetClassyName() );
                    } else {
                        // list players who are redundantly blacklisted
                        SecurityController.PlayerListCollection lists = world.buildSecurity.exceptionList;
                        List<PlayerInfo> noLongerExcluded = new List<PlayerInfo>();
                        foreach( PlayerInfo excludedPlayer in lists.excluded ) {
                            if( excludedPlayer.rank >= rank ) {
                                noLongerExcluded.Add( excludedPlayer );
                            }
                        }
                        if( noLongerExcluded.Count > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted on world {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerExcluded.ToArray() ) );
                        }

                        // list players who are redundantly whitelisted
                        List<PlayerInfo> noLongerIncluded = new List<PlayerInfo>();
                        foreach( PlayerInfo includedPlayer in lists.included ) {
                            if( includedPlayer.rank >= rank ) {
                                noLongerExcluded.Add( includedPlayer );
                            }
                        }
                        if( noLongerIncluded.Count > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted on world {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerIncluded.ToArray() ) );
                        }

                        // apply changes
                        world.buildSecurity.minRank = rank;
                        changesWereMade = true;
                        if( world.buildSecurity.minRank == RankList.LowestRank ) {
                            Server.SendToAll( "{0}&S allowed anyone to build on world {1}",
                                              player.GetClassyName(), world.GetClassyName() );
                        } else {
                            Server.SendToAll( "{0}&S allowed only {1}+&S to build in world {2}",
                                              player.GetClassyName(), world.buildSecurity.minRank.GetClassyName(), world.GetClassyName());
                        }
                        Logger.Log( "{0} set build rank for world {1} to {2}+", LogType.UserActivity,
                                    player.name, world.name, world.buildSecurity.minRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                Server.SaveWorldList();
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
                   "Supported formats: fCraft (.fcm), MCSharp/MCZall/MCLawl (lvl), D3 (.map), vanilla (.dat), MinerCPP/LuaCraft (.dat), " +
                   "JTE (.gz), indev (.mclevel), iCraft/Myne.",
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

            player.MessageNow( "Loading {0}...", fileName );

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
                Map map = Map.Load( player.world, fileName );
                if( map == null ) {
                    player.MessageNow( "Could not load specified file." );
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
                    World world = Server.FindWorldExact( worldName );
                    if( world != null ) {
                        if( !cmd.confirmed ) {
                            player.AskForConfirmation( cmd, "About to replace map for {0}&S with \"{1}\".", world.GetClassyName(), fileName );
                            return;
                        }


                        Map map = Map.Load( player.world, fileName );
                        if( map == null ) {
                            player.MessageNow( "Could not load specified file." );
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

                        Map map = Map.Load( player.world, fileName );
                        if( map == null ) {
                            player.MessageNow( "Could not load specified file." );
                            return;
                        }

                        // Adding a new world
                        World newWorld = Server.AddWorld( worldName, map, false );
                        if( newWorld != null ) {
                            Rank newBuildRank = RankList.ParseRank( Config.GetString( ConfigKey.DefaultBuildRank ) );
                            if( newBuildRank != null ) {
                                newWorld.buildSecurity.minRank = newBuildRank;
                            }
                            Server.SendToAll( "{0}&S created a new world named {1}",
                                              player.GetClassyName(), newWorld.GetClassyName() );
                            Logger.Log( "{0} created a new world named \"{1}\" (loaded from \"{2}\")", LogType.UserActivity,
                                        player.name, worldName, fileName );
                            Server.SaveWorldList();
                            player.MessageNow( "Reminder: New world's access permission is {0}+&S, and build permission is {1}+",
                                               newWorld.accessSecurity.minRank.GetClassyName(),
                                               newWorld.buildSecurity.minRank.GetClassyName() );
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
                World oldWorld = Server.FindWorldOrPrintMatches( player, oldName );
                if( oldWorld == null ) return;

                World newWorld = Server.FindWorldExact( newName );

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
                World world = Server.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

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

            if( !Map.IsValidDimension( wx ) ) {
                player.Message( "Cannot make map with width {0}: dimensions must be multiples of 16.", wx );
                return;
            } else if( !Map.IsValidDimension( wy ) ) {
                player.Message( "Cannot make map with length {0}: dimensions must be multiples of 16.", wy );
                return;
            } else if( !Map.IsValidDimension( height ) ) {
                player.Message( "Cannot make map with height {0}: dimensions must be multiples of 16.", height );
                return;
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
                world = Server.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

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
                world = Server.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

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