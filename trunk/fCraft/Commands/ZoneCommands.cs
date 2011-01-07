// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {
    /// <summary>
    /// Contains commands related to zone management.
    /// </summary>
    static class ZoneCommands {

        internal static void Init() {
            CommandList.RegisterCommand( cdZoneEdit );
            CommandList.RegisterCommand( cdZoneAdd );
            CommandList.RegisterCommand( cdZoneTest );
            CommandList.RegisterCommand( cdZoneList );
            CommandList.RegisterCommand( cdZoneRemove );
            CommandList.RegisterCommand( cdZoneInfo );
        }

        static CommandDescriptor cdZoneEdit = new CommandDescriptor {
            name = "zedit",
            permissions = new Permission[] { Permission.ManageZones },
            usage = "/zedit ZoneName [RankName] [+IncludedName] [-ExcludedName]",
            help = "Allows editing the zone permissions after creation. " +
                   "You can change the rank restrictions, and include or exclude individual players.",
            handler = ZoneEdit
        };

        internal static void ZoneEdit( Player player, Command cmd ) {
            bool changesWereMade = false;
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/help zedit" );
                return;
            }

            Zone zone = player.world.map.FindZone( zoneName );
            if( zone == null ) {
                player.Message( "No zone found with the name \"{0}\". See &H/zones", zoneName );
                return;
            }

            string name;
            while( (name = cmd.Next()) != null ) {
                if( name.Length < 2 ) continue;

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
                        if( !zone.controller.CanBuild( player ) ) {
                            player.Message( "You must be {0}+&S to add yourself to this zone's whitelist.",
                                            zone.controller.minRank.GetClassyName() );
                            continue;
                        }
                    }

                    switch( zone.controller.Include( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is no longer excluded from zone {1}",
                                            info.GetClassyName(), zone.GetClassyName() );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now included in zone {1}",
                                            info.GetClassyName(), zone.GetClassyName() );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already included in zone {1}",
                                            info.GetClassyName(), zone.GetClassyName() );
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

                    switch( zone.controller.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already excluded from zone {1}",
                                            info.GetClassyName(), zone.GetClassyName() );
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now excluded from zone {1}",
                                            info.GetClassyName(), zone.GetClassyName() );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is no longer included in zone {1}",
                                            info.GetClassyName(), zone.GetClassyName() );
                            changesWereMade = true;
                            break;
                    }

                } else {
                    Rank minRank = RankList.ParseRank( name );

                    if( minRank != null ) {
                        // prevent players from lowering rank so bypass protection
                        if( zone.controller.minRank > player.info.rank && minRank <= player.info.rank ) {
                            player.Message( "You are not allowed to lower the zone's rank." );
                            continue;
                        }

                        if( zone.controller.minRank != minRank ) {
                            zone.controller.minRank = minRank;
                            player.Message( "Permission for zone \"{0}\" changed to {1}+",
                                            zone.name,
                                            minRank.GetClassyName() );
                            changesWereMade = true;
                        }
                    } else {
                        player.NoRankMessage( name );
                    }
                }

                if( changesWereMade ) {
                    player.world.map.changedSinceSave = true;
                    zone.editedBy = player.info;
                    zone.editedDate = DateTime.Now;
                } else {
                    player.Message( "No changes were made to the zone." );
                }
            }
        }



        static CommandDescriptor cdZoneAdd = new CommandDescriptor {
            name = "zadd",
            aliases = new string[] { "zone" },
            permissions = new Permission[] { Permission.ManageZones },
            usage = "/zadd ZoneName RankName",
            help = "Create a zone that overrides build permissions. " +
                   "This can be used to restrict access to an area (by setting RankName to a high rank) " +
                   "or to designate a guest area (by setting RankName to a class that normally can't build).",
            handler = ZoneAdd
        };

        internal static void ZoneAdd( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                cdZoneAdd.PrintUsage( player );
                return;
            }

            Zone zone = new Zone();

            if( zoneName.StartsWith( "+" ) ) {
                PlayerInfo info;
                if( !PlayerDB.FindPlayerInfo( zoneName.Substring( 1 ), out info ) ) {
                    player.Message( "More than one player found matching \"{0}\"", zoneName.Substring( 1 ) );
                    return;
                }
                if( info == null ) {
                    player.NoPlayerMessage( zoneName.Substring( 1 ) );
                    return;
                }

                zone.name = info.name;
                if( info.rank.NextRankUp != null ) {
                    zone.controller.minRank = info.rank.NextRankUp;
                } else {
                    zone.controller.minRank = info.rank;
                }
                zone.controller.Include( info );
                player.Message( "Zone: Creating a {0}+&S zone for player {1}&S. Place a block or type /mark to use your location.",
                                zone.controller.minRank.GetClassyName(), info.GetClassyName() );
                player.SetCallback( 2, ZoneAddCallback, zone );

            } else {
                if( !Player.IsValidName( zoneName ) ) {
                    player.Message( "\"{0}\" is not a valid zone name", zoneName );
                    return;
                }

                if( player.world.map.FindZone( zoneName ) != null ) {
                    player.Message( "A zone with this name already exists. Use &H/zedit&S to edit." );
                    return;
                }

                zone.name = zoneName;

                string rankName = cmd.Next();
                if( rankName == null ) {
                    player.Message( "No class was specified. See &H/help zone" );
                    return;
                }
                Rank minRank = RankList.ParseRank( rankName );

                if( minRank != null ) {
                    string name;
                    while( (name = cmd.Next()) != null ) {

                        if( name.Length == 0 ) continue;

                        PlayerInfo info;
                        if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                            player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                            return;
                        }
                        if( info == null ) {
                            player.NoPlayerMessage( name.Substring( 1 ) );
                            return;
                        }

                        if( name.StartsWith( "+" ) ) {
                            zone.controller.Include( info );
                        } else if( name.StartsWith( "-" ) ) {
                            zone.controller.Exclude( info );
                        }
                    }

                    zone.controller.minRank = minRank;
                    player.SetCallback( 2, ZoneAddCallback, zone );
                    player.Message( "Zone: Place a block or type /mark to use your location." );

                } else {
                    player.NoRankMessage( rankName );
                }
            }
        }

        internal static void ZoneAddCallback( Player player, Position[] marks, object tag ) {
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

            zone.createdBy = player.info;
            zone.createdDate = DateTime.Now;
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
                    PermissionType status = zone.controller.CanBuildDetailed( player );
                    player.Message( "> {0}: {1}{2}", zone.name, Color.Lime, status );
                }
                foreach( Zone zone in denied ) {
                    PermissionType status = zone.controller.CanBuildDetailed( player );
                    player.Message( "> {0}: {1}{2}", zone.name, Color.Red, status );
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
                cdZoneRemove.PrintUsage( player );
                return;
            }

            if( player.world.map.FindZone( zoneName ) != null ) {
                if( !cmd.confirmed ) {
                    player.AskForConfirmation( cmd, "You are about to remove \"{0}\" zone.", zoneName );
                    return;
                }

                if( player.world.map.RemoveZone( zoneName ) ) {
                    player.Message( "Zone \"{0}\" removed.", zoneName );
                }

            } else {
                player.Message( "No zone with the name \"{0}\" was found.", zoneName );
            }
        }



        static CommandDescriptor cdZoneList = new CommandDescriptor {
            name = "zones",
            help = "Lists all zones defined on the current map/world.",
            handler = ZoneList
        };

        internal static void ZoneList( Player player, Command cmd ) {
            Zone[] zones = player.world.map.zoneList;
            if( zones.Length > 0 ) {
                player.Message( "List of zones (see &H/zinfo ZoneName&S for details):" );
                foreach( Zone zone in zones ) {
                    player.Message( "  {0} ({1}&S) - {2} x {3} x {4}",
                                    zone.name,
                                    zone.controller.minRank.GetClassyName(),
                                    zone.bounds.GetWidthX(),
                                    zone.bounds.GetWidthY(),
                                    zone.bounds.GetHeight() );
                }
            } else {
                player.Message( "No zones are defined for this map." );
            }
        }



        static CommandDescriptor cdZoneInfo = new CommandDescriptor {
            name = "zinfo",
            help = "Shows information about a zone",
            usage = "/zinfo ZoneName",
            handler = ZoneInfo
        };


        internal static void ZoneInfo( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/help zinfo" );
                return;
            }

            Zone zone = player.world.map.FindZone( zoneName );
            if( zone == null ) {
                player.Message( "No zone found with the name \"{0}\". See &H/zones", zoneName );
                return;
            }

            player.Message( "About zone \"{0}\": size {1} x {2} x {3}, contains {4} blocks, editable by {5}+.",
                            zone.name,
                            zone.bounds.GetWidthX(), zone.bounds.GetWidthY(), zone.bounds.GetHeight(),
                            zone.bounds.GetVolume(),
                            zone.controller.minRank.GetClassyName() );

            player.Message( "  Zone centre is at ({0},{1},{2}).",
                            (zone.bounds.xMin + zone.bounds.xMax) / 2,
                            (zone.bounds.yMin + zone.bounds.yMax) / 2,
                            (zone.bounds.hMin + zone.bounds.hMax) / 2 );

            if( zone.createdBy != null ) {
                player.Message( "  Zone created by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                                zone.createdBy.GetClassyName(),
                                zone.createdDate,
                                DateTime.Now.Subtract( zone.createdDate ).Days,
                                DateTime.Now.Subtract( zone.createdDate ).Hours );
            }

            if( zone.editedBy != null ) {
                player.Message( "  Zone last edited by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                zone.editedBy.GetClassyName(),
                zone.editedDate,
                DateTime.Now.Subtract( zone.editedDate ).Days,
                DateTime.Now.Subtract( zone.editedDate ).Hours );
            }

            SecurityController.PlayerListCollection playerList = zone.GetPlayerList();

            if( playerList.included.Length > 0 ) {
                player.Message( "  Zone whitelist includes: {0}",
                                PlayerInfo.PlayerInfoArrayToString( playerList.included ) );
            }

            if( playerList.excluded.Length > 0 ) {
                player.Message( "  Zone blacklist excludes: {0}",
                                PlayerInfo.PlayerInfoArrayToString( playerList.excluded ) );
            }
        }
    }
}