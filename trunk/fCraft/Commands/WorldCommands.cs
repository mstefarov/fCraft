// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Linq;
using System.Text;
using fCraft.Events;
using fCraft.MapConversion;

namespace fCraft {
    /// <summary>
    /// Contains commands related to world management.
    /// </summary>
    static class WorldCommands {
        internal static void Init() {
            CommandManager.RegisterCommand( cdJoin );

            CommandManager.RegisterCommand( cdWorldInfo );

            CommandManager.RegisterCommand( cdWorldSave );
            CommandManager.RegisterCommand( cdWorldMain );
            CommandManager.RegisterCommand( cdWorldAccess );
            CommandManager.RegisterCommand( cdWorldBuild );
            CommandManager.RegisterCommand( cdWorlds );
            CommandManager.RegisterCommand( cdWorldLoad );
            CommandManager.RegisterCommand( cdWorldRename );
            CommandManager.RegisterCommand( cdWorldUnload );
            CommandManager.RegisterCommand( cdWorldFlush );

            CommandManager.RegisterCommand( cdWorldHide );
            CommandManager.RegisterCommand( cdWorldUnhide );

            CommandManager.RegisterCommand( cdGenerate );

            CommandManager.RegisterCommand( cdLock );
            CommandManager.RegisterCommand( cdLockAll );
            CommandManager.RegisterCommand( cdUnlock );
            CommandManager.RegisterCommand( cdUnlockAll );
        }


        static readonly CommandDescriptor cdWorldInfo = new CommandDescriptor {
            Name = "winfo",
            Aliases = new[] { "mapinfo" },
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/winfo [WorldName]",
            Help = "Shows information about a world: player count, map dimensions, permissions, etc." +
                   "If no WorldName is given, shows info for current world.",
            Handler = WorldInfo
        };

        internal static void WorldInfo( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "Please specify a world name when calling /winfo form console." );
                    return;
                } else {
                    worldName = player.World.Name;
                }
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            player.Message( "World {0}&S has {1} player(s) on.",
                            world.GetClassyName(),
                            world.CountVisiblePlayers(player) );

            Map map = world.Map;

            // If map is not currently loaded, grab its header from disk
            if( map == null ) {
                try {
                    map = MapUtility.LoadHeader( Path.Combine( Paths.MapPath, world.GetMapName() ) );
                } catch( Exception ex ) {
                    player.Message( "Map information could not be loaded: {0}: {1}",
                                    ex.GetType().Name, ex.Message );
                }
            }

            if( map != null ) {
                player.Message( "Map dimensions are {0} x {1} x {2}",
                                map.WidthX, map.WidthY, map.Height );
            }

            // Print access/build limits
            world.AccessSecurity.PrintDescription( player, world, "world", "accessed" );
            world.BuildSecurity.PrintDescription( player, world, "world", "modified" );

            // Print lock/unlock information
            if( world.IsLocked ) {
                player.Message( "{0}&S was locked {1} ago by {2}",
                                world.GetClassyName(),
                                DateTime.UtcNow.Subtract( world.LockedDate ).ToMiniString(),
                                world.LockedBy );
            } else if( world.UnlockedBy != null ) {
                player.Message( "{0}&S was unlocked {1} ago by {2}",
                                world.GetClassyName(),
                                DateTime.UtcNow.Subtract( world.UnlockedDate ).ToMiniString(),
                                world.UnlockedBy );
            }
        }



        static readonly CommandDescriptor cdJoin = new CommandDescriptor {
            Name = "join",
            Aliases = new[] { "j", "load", "l", "goto", "map" },
            Category = CommandCategory.World,
            Usage = "/join WorldName",
            Help = "Teleports the player to a specified world. You can see the list of available worlds by using &H/worlds",
            Handler = Join
        };

        internal static void Join( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdJoin.PrintUsage( player );
                return;
            }

            World[] worlds = WorldManager.FindWorlds( worldName );

            SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, worldName, worlds.ToList(), true );
            WorldManager.RaiseSearchingForWorldEvent( e );
            worlds = e.Matches.ToArray();

            if( worlds.Length > 1 ) {
                player.ManyMatchesMessage( "world", worlds );

            } else if( worlds.Length == 1 ) {
                World world = worlds[0];
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if( world.IsFull() ) {
                            player.Message( "Cannot join {0}&S: world is full.", world.GetClassyName() );
                            return;
                        }
                        if( !player.Session.JoinWorldNow( world, false ) ) {
                            player.Message( "ERROR: Failed to join world. See log for details." );
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot join world {0}&S: you are blacklisted",
                                        world.GetClassyName(), world.AccessSecurity.MinRank.GetClassyName() );
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot join world {0}&S: must be {1}+",
                                        world.GetClassyName(), world.AccessSecurity.MinRank.GetClassyName() );
                        break;
                }

            } else {
                // no worlds found - see if player meant to type in "/join" and not "/tp"
                Player[] players = Server.FindPlayers( player, worldName );
                if( players.Length == 1 ) {
                    player.ParseMessage( "/tp " + players[0].Name, false );
                } else {
                    player.NoWorldMessage( worldName );
                }
            }
        }


        #region World Commands

        static readonly CommandDescriptor cdWorldSave = new CommandDescriptor {
            Name = "wsave",
            Aliases = new[] { "save" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wsave FileName &Sor&H /save WorldName FileName",
            Help = "Saves a map copy to a file with the specified name. " +
                   "The \".fcm\" file extension can be omitted. " +
                   "If a file with the same name already exists, it will be overwritten.",
            Handler = WorldSave
        };

        internal static void WorldSave( Player player, Command cmd ) {
            string p1 = cmd.Next(), p2 = cmd.Next();
            if( p1 == null ) {
                cdWorldSave.PrintUsage( player );
                return;
            }

            World world = player.World;
            string fileName;
            if( p2 == null ) {
                fileName = p1;
                if( world == null ) {
                    player.Message( "When called from console, /save requires WorldName. See \"/help save\" for details." );
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, p1 );
                if( world == null ) return;
                fileName = p2;
            }

            // normalize the path
            fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
            if( fileName.EndsWith( "/" ) && fileName.EndsWith( @"\" ) ) {
                fileName += world.Name + ".fcm";
            } else if( !fileName.ToLower().EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                fileName += ".fcm";
            }
            string fullFileName = Path.Combine( Paths.MapPath, fileName );
            if( !Paths.IsValidPath( fullFileName ) ) {
                player.Message( "Invalid filename." );
                return;
            }
            if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            // Ask for confirmation if overwriting
            if( File.Exists( fullFileName ) ) {
                FileInfo targetFile = new FileInfo( fullFileName );
                FileInfo sourceFile = new FileInfo( world.GetMapName() );
                if( !targetFile.FullName.Equals( sourceFile.FullName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( !cmd.Confirmed ) {
                        player.AskForConfirmation( cmd, "Target file \"{0}\" already exists, and will be overwritten.", targetFile.Name );
                        return;
                    }
                }
            }

            // Create the target directory if it does not exist
            string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
            if( !Directory.Exists( dirName ) ) {
                Directory.CreateDirectory( dirName );
            }

            player.MessageNow( "Saving map to {0}", fileName );

            const string mapSavingErrorMessage = "Map saving failed. See server logs for details.";
            Map map = world.Map;
            if( map == null ) {
                if( File.Exists( world.GetMapName() ) ) {
                    try {
                        File.Copy( world.GetMapName(), fullFileName, true );
                    } catch( Exception ex ) {
                        Logger.Log( "StandardCommands.Save: Error occured while trying to copy an unloaded map: {0}", LogType.Error, ex );
                        player.Message( mapSavingErrorMessage );
                    }
                } else {
                    Logger.Log( "StandardCommands.Save: Map for world \"{0}\" is unloaded, and file does not exist.", LogType.Error, world.Name );
                    player.Message( mapSavingErrorMessage );
                }
            } else if( map.Save( fullFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( "StandardCommands.Save: Saving world \"{0}\" failed.", LogType.Error, world.Name );
                player.Message( mapSavingErrorMessage );
            }
        }



        static readonly CommandDescriptor cdWorldFlush = new CommandDescriptor {
            Name = "wflush",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wflush [WorldName]",
            Help = "Flushes the update buffer on specified map by causing players to rejoin. " +
                   "Makes cuboids and other draw commands finish REALLY fast.",
            Handler = WorldFlush
        };

        internal static void WorldFlush( Player player, Command cmd ) {
            string worldName = cmd.Next();
            World world = player.World;

            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

            } else if( player.World == null ) {
                player.Message( "When using /wflush from console, you must specify a world name." );
                return;
            }

            if( world.Map == null ) {
                player.MessageNow( "WFlush: {0}&S has no updates to process.",
                                   world.GetClassyName() );
            } else {
                player.MessageNow( "WFlush: Flushing {0}&S ({1} blocks in queue)...",
                                   world.GetClassyName(),
                                   world.Map.UpdateQueueSize() );

                world.BeginFlushMapBuffer();
            }
        }



        static readonly CommandDescriptor cdWorldMain = new CommandDescriptor {
            Name = "wmain",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wmain [WorldName]",
            Help = "Sets the specified world as the new main world. Main world is what newly-connected players join first.",
            Handler = WorldMain
        };

        internal static void WorldMain( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                player.Message( "Main world is {0}", WorldManager.MainWorld.GetClassyName() );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) {
                return;

            } else if( world == WorldManager.MainWorld ) {
                player.Message( "World {0}&S is already set as main.", world.GetClassyName() );

            } else if( !player.Info.Rank.AllowSecurityCircumvention && !player.CanJoin( world ) ) {
                // Prevent players from exploiting /wmain to gain access to restricted maps
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.RankTooHigh:
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "You are not allowed to set {0}&S as the main world (by rank).", world.GetClassyName() );
                        return;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "You are not allowed to set {0}&S as the main world (blacklisted).", world.GetClassyName() );
                        return;
                }

            } else {
                if( world.AccessSecurity.HasRestrictions() ) {
                    world.AccessSecurity.Reset();
                    player.Message( "The main world cannot have access restrictions. " +
                                    "All access restrictions were removed from world {0}",
                                    world.GetClassyName() );
                }

                if( !world.SetMainWorld() ) {
                    player.Message( "Main world was not changed." );
                    return;
                }
                WorldManager.SaveWorldList();

                Server.SendToAll( "{0}&S set {1}&S to be the main world.",
                                  player.GetClassyName(), world.GetClassyName() );
                Logger.Log( "{0} set {1} to be the main world.", LogType.UserActivity,
                            player.Name, world.Name );
            }
        }



        static readonly CommandDescriptor cdWorldAccess = new CommandDescriptor {
            Name = "waccess",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/waccess [WorldName [RankName]]",
            Help = "Shows access permission for player's current world. " +
                   "If optional WorldName parameter is given, shows access permission for another world. " +
                   "If RankName parameter is also given, sets access permission for specified world.",
            Handler = WorldAccess
        };

        internal static void WorldAccess( Player player, Command cmd ) {
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player == Player.Console ) {
                    player.Message( "When calling /waccess from console, you must specify a world name." );
                } else {
                    player.World.AccessSecurity.PrintDescription( player, player.World, "world", "accessed" );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name = cmd.Next();
            if( name == null ) {
                world.AccessSecurity.PrintDescription( player, world, "world", "accessed" );
                return;
            }
            if( world == WorldManager.MainWorld ) {
                player.Message( "The main world cannot have access restrictions." );
                return;
            }

            bool changesWereMade = false;
            do {
                if( name.Length < 2 ) continue;
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
                    if( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the access whitelist of {0}",
                                                world.AccessSecurity.MinRank.GetClassyName(),
                                                world.GetClassyName() );
                                continue;
                            // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the access blacklist of {0}",
                                                world.GetClassyName() );
                                continue;
                        }
                    }

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to access {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.AccessSecurity.Check( info ) ) {
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
                                        player.Name, info.Name, world.Name );
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
                                        player.Name, info.Name, world.Name );
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

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Exclude( info ) ) {
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
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.AccessSecurity.Check( info ) ) {
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
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( name );
                    if( rank == null ) {
                        player.NoRankMessage( name );

                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.AccessSecurity.MinRank > rank &&
                               world.AccessSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower the access rank for world {1}",
                                        world.AccessSecurity.MinRank.GetClassyName(), world.GetClassyName() );

                    } else {
                        // list players who are redundantly blacklisted
                        SecurityController.PlayerListCollection lists = world.AccessSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = lists.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerExcluded ) );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = lists.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerIncluded ) );
                        }

                        // apply changes
                        world.AccessSecurity.MinRank = rank;
                        changesWereMade = true;
                        if( world.AccessSecurity.MinRank == RankManager.LowestRank ) {
                            Server.SendToAll( "{0}&S made the world {1}&S accessible to everyone.",
                                              player.GetClassyName(), world.GetClassyName() );
                        } else {
                            Server.SendToAll( "{0}&S made the world {1}&S accessible only by {2}+",
                                              player.GetClassyName(), world.GetClassyName(),
                                              world.AccessSecurity.MinRank.GetClassyName() );
                        }
                        Logger.Log( "{0} set access rank for world {1} to {2}+", LogType.UserActivity,
                                    player.Name, world.Name, world.AccessSecurity.MinRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                WorldManager.SaveWorldList();
            }
        }



        static readonly CommandDescriptor cdWorldBuild = new CommandDescriptor {
            Name = "wbuild",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wbuild [WorldName [RankName]]",
            Help = "Shows build permissions for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If RankName parameter is also given, sets build permission for specified world.",
            Handler = WorldBuild
        };

        internal static void WorldBuild( Player player, Command cmd ) {
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player == Player.Console ) {
                    player.Message( "When calling /wbuild from console, you must specify a world name." );
                } else {
                    player.World.BuildSecurity.PrintDescription( player, player.World, "world", "modified" );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name = cmd.Next();
            if( name == null ) {
                world.BuildSecurity.PrintDescription( player, world, "world", "modified" );
                return;
            }

            bool changesWereMade = false;
            do {
                if( name.Length < 2 ) continue;
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
                    if( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch( world.BuildSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the build whitelist of {0}",
                                                world.BuildSecurity.MinRank.GetClassyName(),
                                                world.GetClassyName() );
                                continue;
                            // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the build blacklist of {0}",
                                                world.GetClassyName() );
                                continue;
                        }
                    }

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.BuildSecurity.Check( info ) ) {
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
                                        player.Name, info.Name, world.Name );
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
                                        player.Name, info.Name, world.Name );
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

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from building in {1}&S (by rank)",
                                        info.GetClassyName(), world.GetClassyName() );
                        continue;
                    }

                    Player target = Server.FindPlayerExact( info );
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Exclude( info ) ) {
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
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.BuildSecurity.Check( info ) ) {
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
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( name );
                    if( rank == null ) {
                        player.NoRankMessage( name );
                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.BuildSecurity.MinRank > rank &&
                               world.BuildSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower build restrictions for world {1}",
                                        world.BuildSecurity.MinRank.GetClassyName(), world.GetClassyName() );
                    } else {
                        // list players who are redundantly blacklisted
                        SecurityController.PlayerListCollection lists = world.BuildSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = lists.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted on world {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerExcluded ) );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = lists.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted on world {0}&S: {1}",
                                            world.GetClassyName(),
                                            PlayerInfo.PlayerInfoArrayToString( noLongerIncluded ) );
                        }

                        // apply changes
                        world.BuildSecurity.MinRank = rank;
                        changesWereMade = true;
                        if( world.BuildSecurity.MinRank == RankManager.LowestRank ) {
                            Server.SendToAll( "{0}&S allowed anyone to build on world {1}",
                                              player.GetClassyName(), world.GetClassyName() );
                        } else {
                            Server.SendToAll( "{0}&S allowed only {1}+&S to build in world {2}",
                                              player.GetClassyName(), world.BuildSecurity.MinRank.GetClassyName(), world.GetClassyName() );
                        }
                        Logger.Log( "{0} set build rank for world {1} to {2}+", LogType.UserActivity,
                                    player.Name, world.Name, world.BuildSecurity.MinRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                WorldManager.SaveWorldList();
            }
        }



        static readonly CommandDescriptor cdWorlds = new CommandDescriptor {
            Name = "worlds",
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            Aliases = new[] { "maps", "levels" },
            Usage = "/worlds [all|hidden]",
            Help = "Shows a list of worlds available for you to join. " +
                   "If the optional \"all\" is added, also shows unavailable (restricted) worlds. " +
                   "If \"hidden\" is added, shows only hidden and inaccessible worlds.",
            Handler = Worlds
        };

        internal static void Worlds( Player player, Command cmd ) {
            string param = cmd.Next();
            bool listVisible = true,
                 listHidden = false;
            if( !String.IsNullOrEmpty( param ) ) {
                switch( param[0] ) {
                    case 'A':
                    case 'a':
                        listHidden = true;
                        break;
                    case 'H':
                    case 'h':
                        listVisible = false;
                        listHidden = true;
                        break;
                    default:
                        cdWorlds.PrintUsage( player );
                        return;
                }
            }

            StringBuilder sb = new StringBuilder();
            bool first = true;
            int count = 0;

            World[] worldListCache = WorldManager.WorldList;
            foreach( World world in worldListCache ) {
                bool visible = player.CanJoin( world ) && !world.IsHidden;
                if( (visible && listVisible) || (!visible && listHidden) ) {
                    if( !first ) {
                        sb.Append( ", " );
                    }
                    sb.Append( world.GetClassyName() );
                    count++;
                    first = false;
                }
            }

            if( listVisible && !listHidden ) {
                player.MessagePrefixed( "&S   ", "There are " + count + " available worlds: " + sb );
            } else if( !listVisible ) {
                player.MessagePrefixed( "&S   ", "There are " + count + " hidden worlds: " + sb );
            } else {
                player.MessagePrefixed( "&S   ", "There are " + count + " worlds total: " + sb );
            }
        }



        static readonly CommandDescriptor cdWorldLoad = new CommandDescriptor {
            Name = "wload",
            Aliases = new[] { "wadd" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wload FileName [WorldName]",
            Help = "If WorldName parameter is not given, replaces the current world's map with the specified map. The old map is overwritten. " +
                   "If the world with the specified name exists, its map is replaced with the specified map file. " +
                   "Otherwise, a new world is created using the given name and map file. NOTE: For security reasons, you may only load files from the map folder. " +
                   "Supported formats: fCraft (.fcm), MCSharp/MCZall/MCLawl (lvl), D3 (.map), vanilla (.dat), MinerCPP/LuaCraft (.dat), " +
                   "JTE (.gz), indev (.mclevel), iCraft/Myne, Opticraft (.save).",
            Handler = WorldLoad
        };

        internal static void WorldLoad( Player player, Command cmd ) {
            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if( worldName == null && player.World == null ) {
                player.Message( "When using /wload from console, you must specify the world name." );
                return;
            }

            if( fileName == null ) {
                // No params given at all
                cdWorldLoad.PrintUsage( player );
                return;
            }

            // Check if path contains missing drives or invalid characters
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid filename or path." );
                return;
            }

            player.MessageNow( "Looking for \"{0}\"...", fileName );

            // Look for the file
            string sourceFullFileName = Path.Combine( Paths.MapPath, fileName );
            if( !File.Exists( sourceFullFileName ) && !Directory.Exists( sourceFullFileName ) ) {

                if( File.Exists( sourceFullFileName + ".fcm" ) ) {
                    // Try with extension added
                    fileName += ".fcm";
                    sourceFullFileName += ".fcm";

                } else if( MonoCompat.IsCaseSensitive ) {
                    // If we're on a case-sensitive OS, try case-insensitive search
                    FileInfo[] candidates = Paths.FindFiles( sourceFullFileName + ".fcm" );
                    if( candidates.Length == 0 ) {
                        candidates = Paths.FindFiles( sourceFullFileName );
                    }

                    if( candidates.Length == 0 ) {
                        player.Message( "File/directory not found: {0}", fileName );

                    } else if( candidates.Length == 1 ) {
                        player.Message( "Filenames are case-sensitive! Did you mean to load \"{0}\"?", candidates[0].Name );

                    } else {
                        player.Message( "Filenames are case-sensitive! Did you mean to load one of these: {0}",
                                        String.Join( ", ", candidates.Select( c => c.Name ).ToArray() ) );
                    }
                    return;

                } else {
                    // Nothing found!
                    player.Message( "File/directory not found: {0}", fileName );
                    return;
                }
            }

            // Make sure that the given file is within the map directory
            if( !Paths.Contains( Paths.MapPath, sourceFullFileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            // Loading map into current world
            if( worldName == null ) {
                if( !cmd.Confirmed ) {
                    player.AskForConfirmation( cmd, "About to replace THIS MAP with \"{0}\".", fileName );
                    return;
                }
                Map map;
                try {
                    map = MapUtility.Load( sourceFullFileName );
                } catch( Exception ex ) {
                    player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                    return;
                }

                // Loading to current world
                player.World.ChangeMap( map );
                player.World.SendToAllExcept( "{0}&S loaded a new map for this world.", player,
                                              player.GetClassyName() );
                player.MessageNow( "New map loaded for the world {0}", player.World.GetClassyName() );

                Logger.Log( "{0} loaded new map for world \"{1}\" from {2}", LogType.UserActivity,
                            player.Name, player.World.Name, fileName );


            } else {
                // Loading to some other (or new) world
                if( !Player.IsValidName( worldName ) ) {
                    player.MessageNow( "Invalid world name: \"{0}\".", worldName );
                    return;
                }

                lock( WorldManager.WorldListLock ) {
                    World world = WorldManager.FindWorldExact( worldName );
                    if( world != null ) {
                        // Replacing existing world's map
                        if( !cmd.Confirmed ) {
                            player.AskForConfirmation( cmd, "About to replace map for {0}&S with \"{1}\".",
                                                       world.GetClassyName(), fileName );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( sourceFullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                            return;
                        }

                        world.ChangeMap( map );
                        world.SendToAllExcept( "{0}&S loaded a new map for the world {1}", player,
                                               player.GetClassyName(), world.GetClassyName() );
                        player.MessageNow( "New map for the world {0}&S has been loaded.", world.GetClassyName() );
                        Logger.Log( "{0} loaded new map for world \"{1}\" from {2}", LogType.UserActivity,
                                    player.Name, world.Name, sourceFullFileName );

                    } else {
                        // Adding a new world
                        string targetFullFileName = Path.Combine( Paths.MapPath, worldName + ".fcm" );
                        if( !cmd.Confirmed &&
                            File.Exists( targetFullFileName ) && // target file already exists
                            !Paths.Compare( targetFullFileName, sourceFullFileName ) ) { // and is different from sourceFile
                            player.AskForConfirmation( cmd, "A map named \"{0}\" already exists, and will be overwritten with \"{1}\".",
                                                       Path.GetFileName( targetFullFileName ), Path.GetFileName( sourceFullFileName ) );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( sourceFullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load \"{0}\": {1}: {2}",
                                               fileName, ex.GetType().Name, ex.Message );
                            return;
                        }

                        World newWorld;
                        try {
                            newWorld = WorldManager.AddWorld( player, worldName, map, false );
                        } catch( WorldOpException ex ) {
                            player.Message( "WLoad: {0}", ex.Message );
                            return;
                        }

                        if( newWorld != null ) {
                            newWorld.BuildSecurity.MinRank = RankManager.ParseRank( ConfigKey.DefaultBuildRank.GetString() );
                            Server.SendToAll( "{0}&S created a new world named {1}",
                                              player.GetClassyName(), newWorld.GetClassyName() );
                            Logger.Log( "{0} created a new world named \"{1}\" (loaded from \"{2}\")", LogType.UserActivity,
                                        player.Name, worldName, fileName );
                            WorldManager.SaveWorldList();
                            player.MessageNow( "Reminder: New world's access permission is {0}+&S, and build permission is {1}+",
                                               newWorld.AccessSecurity.MinRank.GetClassyName(),
                                               newWorld.BuildSecurity.MinRank.GetClassyName() );
                        } else {
                            player.MessageNow( "Failed to create a new world." );
                        }
                    }
                }
            }

            Server.RequestGC();
        }



        static readonly CommandDescriptor cdWorldRename = new CommandDescriptor {
            Name = "wrename",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wrename OldName NewName",
            Help = "Changes the name of a world. Does not require any reloading.",
            Handler = WorldRename
        };

        internal static void WorldRename( Player player, Command cmd ) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                cdWorldRename.PrintUsage( player );
                return;
            }

            World oldWorld = WorldManager.FindWorldOrPrintMatches( player, oldName );
            if( oldWorld == null ) return;
            oldName = oldWorld.Name;

            try {
                WorldManager.RenameWorld( oldWorld, newName, true );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.NoChangeNeeded:
                        player.MessageNow( "Rename: World is already named \"{0}\"", oldName );
                        return;
                    case WorldOpExceptionCode.DuplicateWorldName:
                        player.MessageNow( "Rename: Another world named \"{0}\" already exists.", newName );
                        return;
                    case WorldOpExceptionCode.InvalidWorldName:
                        player.MessageNow( "Rename: Invalid world name: \"{0}\"", newName );
                        return;
                    case WorldOpExceptionCode.MapMoveError:
                        player.MessageNow( "Rename: World \"{0}\" was renamed to \"{1}\", but the map file could not be moved due to an error: {2}",
                                            oldName, newName, ex.InnerException );
                        return;
                    default:
                        player.MessageNow( "Unexpected error occured while renaming world \"{0}\"", oldName );
                        Logger.Log( "WorldCommands.Rename: Unexpected error while renaming world {0} to {1}: {2}",
                                    LogType.Error, oldWorld.Name, newName, ex );
                        return;
                }
            }

            WorldManager.SaveWorldList();
            Logger.Log( "{0} renamed the world \"{1}\" to \"{2}\".", LogType.UserActivity,
                        player.Name, oldName, newName );
            Server.SendToAll( "{0}&S renamed the world \"{1}\" to \"{2}\"",
                              player.GetClassyName(), oldName, newName );
        }



        static readonly CommandDescriptor cdWorldUnload = new CommandDescriptor {
            Name = "wunload",
            Aliases = new[] { "wremove", "wdelete" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wunload WorldName",
            Help = "Removes the specified world from the world list, and moves all players from it to the main world. " +
                   "The main world itself cannot be removed with this command. You will need to delete the map file manually.",
            Handler = WorldUnload
        };

        internal static void WorldUnload( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdWorldUnload.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            try {
                WorldManager.RemoveWorld( world );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        player.MessageNow( "&WWorld {0}&W is set as the main world. " +
                                           "Assign a new main world before deleting this one.",
                                           world.GetClassyName() );
                        return;
                    case WorldOpExceptionCode.WorldNotFound:
                        player.MessageNow( "&WWorld {0}&W is already unloaded.",
                                           world.GetClassyName() );
                        return;
                    default:
                        player.MessageNow( "&WUnexpected error occured while unloading world {0}&W: {1}",
                                           world.GetClassyName(), ex.GetType().Name );
                        Logger.Log( "WorldCommands.WorldUnload: Unexpected error while unloading world {0}: {1}",
                                    LogType.Error, world.Name, ex );
                        return;
                }
            }

            WorldManager.SaveWorldList();
            Server.SendToAllExcept( "{0}&S removed {1}&S from the world list.", player,
                                    player.GetClassyName(), world.GetClassyName() );
            player.Message( "Removed {0}&S from the world list. You can now delete the map file ({1}.fcm) manually.",
                            world.GetClassyName(), world.Name );
            Logger.Log( "{0} removed \"{1}\" from the world list.", LogType.UserActivity,
                        player.Name, worldName );

            Server.RequestGC();
        }

        #endregion


        #region Hide / Unhide

        static readonly CommandDescriptor cdWorldHide = new CommandDescriptor {
            Name = "whide",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/whide WorldName",
            Help = "Hides the specified world from the &H/worlds&S list. " +
                   "Hidden worlds can be seen by typing &H/worlds all",
            Handler = WorldHide
        };

        internal static void WorldHide( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdWorldAccess.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            if( world.IsHidden ) {
                player.Message( "World \"{0}&S\" is already hidden.", world.GetClassyName() );
            } else {
                player.Message( "World \"{0}&S\" is now hidden.", world.GetClassyName() );
                world.IsHidden = true;
                WorldManager.SaveWorldList();
            }
        }



        static readonly CommandDescriptor cdWorldUnhide = new CommandDescriptor {
            Name = "wunhide",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wunhide WorldName",
            Help = "Unhides the specified world from the &H/worlds&S list. " +
                   "Hidden worlds can be listed by typing &H/worlds all",
            Handler = WorldUnhide
        };

        internal static void WorldUnhide( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                cdWorldAccess.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            if( world.IsHidden ) {
                player.Message( "World \"{0}&S\" is no longer hidden.", world.GetClassyName() );
                world.IsHidden = false;
                WorldManager.SaveWorldList();
            } else {
                player.Message( "World \"{0}&S\" is not hidden.", world.GetClassyName() );
            }
        }

        #endregion


        #region Generation

        static readonly CommandDescriptor cdGenerate = new CommandDescriptor {
            Name = "gen",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/gen ThemeName TemplateName [X Y Height [FileName]]",
            HelpHandler = delegate {
                return "Generates a new map. If no dimensions are given, uses current world's dimensions. " +
                       "If no filename is given, loads generated world into current world.&N" +
                       "Available themes: Grass, " + String.Join( ", ", Enum.GetNames( typeof( MapGenTheme ) ) ) + "&N" +
                       "Available terrain types: " + String.Join( ", ", Enum.GetNames( typeof( MapGenTemplate ) ) ) + "&N" +
                       "NOTE: Map is saved TO FILE ONLY, use /wload to load it.";
            },
            Handler = Generate
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
                if( player.World != null ) {
                    wx = player.World.Map.WidthX;
                    wy = player.World.Map.WidthY;
                    height = player.World.Map.Height;
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
            string fullFileName = null;

            if( fileName == null ) {
                if( player.World == null ) {
                    player.Message( "When used from console, /gen requires FileName." );
                    cdGenerate.PrintUsage( player );
                    return;
                }
                if( !cmd.Confirmed ) {
                    player.AskForConfirmation( cmd, "Replace this world's map with a generated one?" );
                    return;
                }
            } else {
                fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
                if( !fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                    fileName += ".fcm";
                }
                fullFileName = Path.Combine( Paths.MapPath, fileName );
                if( !Paths.IsValidPath( fullFileName ) ) {
                    player.Message( "Invalid filename." );
                    return;
                }
                if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                    player.MessageUnsafePath();
                    return;
                }
                string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
                if( !Directory.Exists( dirName ) ) {
                    Directory.CreateDirectory( dirName );
                }
                if( !cmd.Confirmed && File.Exists( fullFileName ) ) {
                    player.AskForConfirmation( cmd, "The mapfile \"{0}\" already exists. Overwrite?", fileName );
                    return;
                }
            }

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
            args.WidthX = wx;
            args.WidthY = wy;
            args.Height = height;
            args.MaxHeight = (int)(args.MaxHeight / 80d * height);
            args.MaxDepth = (int)(args.MaxDepth / 80d * height);
            args.Theme = theme;
            args.AddTrees = !noTrees;

            Map map;
            try {
                if( theme == MapGenTheme.Forest && noTrees ) {
                    player.MessageNow( "Generating Grass {0}...", template );
                } else {
                    player.MessageNow( "Generating {0} {1}...", theme, template );
                }
                if( theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat ) {
                    map = new Map( null, args.WidthX, args.WidthY, args.Height, true );
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

            if( fileName != null ) {
                if( map.Save( fullFileName ) ) {
                    player.MessageNow( "Generation done. Saved to {0}", fileName );
                } else {
                    player.Message( "&WAn error occured while saving generated map to {0}", fileName );
                }
            } else {
                player.MessageNow( "Generation done. Changing map..." );
                player.World.ChangeMap( map );
            }
        }

        #endregion


        #region Lock / Unlock

        static readonly CommandDescriptor cdLock = new CommandDescriptor {
            Name = "lock",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/lock [WorldName]",
            Help = "Puts the world into a locked, read-only mode. " +
                   "No one can place or delete blocks during lockdown. " +
                   "By default this locks the world you're on, but you can also lock any world by name. " +
                   "Call &H/unlock&S to release lock on a world, or &H/unlockall&S to release all worlds at once.",
            Handler = Lock
        };

        internal static void Lock( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /lock requires a world name." );
                return;
            }

            if( !world.Lock( player ) ) {
                player.Message( "The world is already locked." );
            }
        }



        static readonly CommandDescriptor cdLockAll = new CommandDescriptor {
            Name = "lockall",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Help = "Applies &H/lock&S to all available worlds.",
            Handler = LockAll
        };

        internal static void LockAll( Player player, Command cmd ) {
            World[] worldListCache = WorldManager.WorldList;
            foreach( World world in worldListCache ) {
                world.Lock( player );
            }
            player.Message( "All worlds are now locked." );
        }



        static readonly CommandDescriptor cdUnlock = new CommandDescriptor {
            Name = "unlock",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/unlock [WorldName]",
            Help = "Removes the lockdown set by &H/lock&S. See &H/help lock&S for more information.",
            Handler = Unlock
        };

        internal static void Unlock( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /lock requires a world name." );
                return;
            }

            if( !world.Unlock( player ) ) {
                player.Message( "The world is already unlocked." );
            }
        }



        static readonly CommandDescriptor cdUnlockAll = new CommandDescriptor {
            Name = "unlockall",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Help = "Applies &H/unlock&S to all available worlds",
            Handler = UnlockAll
        };

        internal static void UnlockAll( Player player, Command cmd ) {
            World[] worldListCache = WorldManager.WorldList;
            foreach( World world in worldListCache ) {
                world.Unlock( player );
            }
            player.Message( "All worlds are now unlocked." );
        }

        #endregion
    }
}