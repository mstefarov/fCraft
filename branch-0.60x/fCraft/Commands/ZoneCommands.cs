// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using fCraft.MapConversion;

namespace fCraft {
    /// <summary>
    /// Contains commands related to zone management.
    /// </summary>
    static class ZoneCommands {

        internal static void Init() {
            CommandManager.RegisterCommand( CdZoneEdit );
            CommandManager.RegisterCommand( CdZoneAdd );
            CommandManager.RegisterCommand( CdZoneTest );
            CommandManager.RegisterCommand( CdZoneList );
            CommandManager.RegisterCommand( CdZoneRemove );
            CommandManager.RegisterCommand( CdZoneInfo );
            CommandManager.RegisterCommand( CdZoneRename );
            CommandManager.RegisterCommand( CdZoneMark );
        }



        static readonly CommandDescriptor CdZoneMark = new CommandDescriptor {
            Name = "zmark",
            Category = CommandCategory.Zone | CommandCategory.Building,
            Usage = "/zmark ZoneName",
            Help = "Uses zone boundaries to make a selection.",
            Handler = ZoneMark
        };

        internal static void ZoneMark( Player player, Command cmd ) {
            if( player.SelectionMarksExpected == 0 ) {
                player.MessageNow( "Cannot zmark - no selection in progress." );
            } else if( player.SelectionMarksExpected == 2 ) {
                string zoneName = cmd.Next();
                if( zoneName == null ) {
                    CdZoneMark.PrintUsage( player );
                    return;
                }

                Zone zone = player.World.Map.Zones.Find( zoneName );
                if( zone == null ) {
                    player.MessageNoZone( zoneName );
                    return;
                }

                player.SelectionResetMarks();
                player.SelectionAddMark( zone.Bounds.MinVertex, false );
                player.SelectionAddMark( zone.Bounds.MaxVertex, true );
            } else {
                player.MessageNow( "ZMark can only be used for 2-block selection." );
            }
        }


        static readonly CommandDescriptor CdZoneEdit = new CommandDescriptor {
            Name = "zedit",
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Usage = "/zedit ZoneName [RankName] [+IncludedName] [-ExcludedName]",
            Help = "Allows editing the zone permissions after creation. " +
                   "You can change the rank restrictions, and include or exclude individual players.",
            Handler = ZoneEdit
        };

        internal static void ZoneEdit( Player player, Command cmd ) {
            bool changesWereMade = false;
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/help zedit" );
                return;
            }

            Zone zone = player.World.Map.Zones.Find( zoneName );
            if( zone == null ) {
                player.MessageNoZone( zoneName );
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
                        player.MessageNoPlayer( name.Substring( 1 ) );
                        return;
                    }

                    // prevent players from whitelisting themselves to bypass protection
                    if( !player.Info.Rank.AllowSecurityCircumvention && player.Info == info ) {
                        if( !zone.Controller.Check( info ) ) {
                            player.Message( "You must be {0}+&S to add yourself to this zone's whitelist.",
                                            zone.Controller.MinRank.ClassyName );
                            continue;
                        }
                    }

                    switch( zone.Controller.Include( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is no longer excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            break;
                    }

                } else if( name.StartsWith( "-" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        return;
                    }

                    if( info == null ) {
                        player.MessageNoPlayer( name.Substring( 1 ) );
                        return;
                    }

                    switch( zone.Controller.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is no longer included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                    }

                } else {
                    Rank minRank = RankManager.ParseRank( name );

                    if( minRank != null ) {
                        // prevent players from lowering rank so bypass protection
                        if( !player.Info.Rank.AllowSecurityCircumvention &&
                            zone.Controller.MinRank > player.Info.Rank && minRank <= player.Info.Rank ) {
                            player.Message( "You are not allowed to lower the zone's rank." );
                            continue;
                        }

                        if( zone.Controller.MinRank != minRank ) {
                            zone.Controller.MinRank = minRank;
                            player.Message( "Permission for zone \"{0}\" changed to {1}+",
                                            zone.Name,
                                            minRank.ClassyName );
                            changesWereMade = true;
                        }
                    } else {
                        player.MessageNoRank( name );
                    }
                }

                if( changesWereMade ) {
                    zone.Edit( player.Info );
                    player.World.Map.HasChangedSinceSave = true;
                } else {
                    player.Message( "No changes were made to the zone." );
                }
            }
        }



        static readonly CommandDescriptor CdZoneAdd = new CommandDescriptor {
            Name = "zadd",
            Category = CommandCategory.Zone,
            Aliases = new[] { "zone" },
            Permissions = new[] { Permission.ManageZones },
            Usage = "/zadd ZoneName RankName",
            Help = "Create a zone that overrides build permissions. " +
                   "This can be used to restrict access to an area (by setting RankName to a high rank) " +
                   "or to designate a guest area (by lowering RankName).",
            Handler = ZoneAdd
        };

        internal static void ZoneAdd( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                CdZoneAdd.PrintUsage( player );
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
                    player.MessageNoPlayer( zoneName.Substring( 1 ) );
                    return;
                }

                zone.Name = info.Name;
                zone.Controller.MinRank = info.Rank.NextRankUp ?? info.Rank;
                zone.Controller.Include( info );
                player.Message( "Zone: Creating a {0}+&S zone for player {1}&S. Place a block or type /mark to use your location.",
                                zone.Controller.MinRank.ClassyName, info.ClassyName );
                player.SelectionSetCallback( 2, ZoneAddCallback, zone, CdZoneAdd.Permissions );

            } else {
                if( !World.IsValidName( zoneName ) ) {
                    player.Message( "\"{0}\" is not a valid zone name", zoneName );
                    return;
                }

                if( player.World.Map.Zones.FindExact( zoneName ) != null ) {
                    player.Message( "A zone with this name already exists. Use &H/zedit&S to edit." );
                    return;
                }

                zone.Name = zoneName;

                string rankName = cmd.Next();
                if( rankName == null ) {
                    player.Message( "No rank was specified. See &H/help zone" );
                    return;
                }
                Rank minRank = RankManager.ParseRank( rankName );

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
                            player.MessageNoPlayer( name.Substring( 1 ) );
                            return;
                        }

                        if( name.StartsWith( "+" ) ) {
                            zone.Controller.Include( info );
                        } else if( name.StartsWith( "-" ) ) {
                            zone.Controller.Exclude( info );
                        }
                    }

                    zone.Controller.MinRank = minRank;
                    player.SelectionSetCallback( 2, ZoneAddCallback, zone, CdZoneAdd.Permissions );
                    player.Message( "Zone: Place a block or type /mark to use your location." );

                } else {
                    player.MessageNoRank( rankName );
                }
            }
        }

        internal static void ZoneAddCallback( Player player, Position[] marks, object tag ) {
            Zone zone = (Zone)tag;

            zone.Create( new BoundingBox( marks[0], marks[1] ), player.Info );

            player.Message( "Zone \"{0}\" created, {1} blocks total.",
                            zone.Name,
                            zone.Bounds.Volume );
            Logger.Log( "Player {0} created a new zone \"{1}\" containing {2} blocks.", LogType.UserActivity,
                        player.Name,
                        zone.Name,
                        zone.Bounds.Volume );

            player.World.Map.Zones.Add( zone );
        }



        static readonly CommandDescriptor CdZoneTest = new CommandDescriptor {
            Name = "ztest",
            Category = CommandCategory.Zone | CommandCategory.Info,
            Help = "Allows to test exactly which zones affect a particular block. Can be used to find and resolve zone overlaps.",
            Handler = ZoneTest
        };

        static void ZoneTest( Player player, Command cmd ) {
            player.SelectionSetCallback( 1, ZoneTestCallback, null );
            player.Message( "Click the block that you would like to test." );
        }

        internal static void ZoneTestCallback( Player player, Position[] marks, object tag ) {
            Zone[] allowed, denied;
            if( player.World.Map.Zones.CheckDetailed( marks[0].X, marks[0].Y, marks[0].H, player, out allowed, out denied ) ) {
                foreach( Zone zone in allowed ) {
                    SecurityCheckResult status = zone.Controller.CheckDetailed( player.Info );
                    player.Message( "> {0}: {1}{2}", zone.Name, Color.Lime, status );
                }
                foreach( Zone zone in denied ) {
                    SecurityCheckResult status = zone.Controller.CheckDetailed( player.Info );
                    player.Message( "> {0}: {1}{2}", zone.Name, Color.Red, status );
                }
            } else {
                player.Message( "No zones affect this block." );
            }
        }



        static readonly CommandDescriptor CdZoneRemove = new CommandDescriptor {
            Name = "zremove",
            Aliases = new[] { "zdelete" },
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Usage = "/zremove ZoneName",
            Help = "Removes a zone with the specified name from the map.",
            Handler = ZoneRemove
        };

        internal static void ZoneRemove( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                CdZoneRemove.PrintUsage( player );
                return;
            }

            Zone zone = player.World.Map.Zones.Find( zoneName );
            if( zone != null ) {
                if( !zone.Controller.Check( player.Info ) && !player.Info.Rank.AllowSecurityCircumvention ) {
                    player.Message( "You are not allowed to remove zone {0}", zone.ClassyName );
                    return;
                }
                if( !cmd.IsConfirmed ) {
                    player.AskForConfirmation( cmd, "You are about to remove zone {0}&S.", zone.ClassyName );
                    return;
                }

                if( player.World.Map.Zones.Remove( zoneName ) ) {
                    player.Message( "Zone \"{0}\" removed.", zoneName );
                }

            } else {
                player.MessageNoZone( zoneName );
            }
        }



        static readonly CommandDescriptor CdZoneList = new CommandDescriptor {
            Name = "zones",
            Category = CommandCategory.Zone | CommandCategory.Info,
            IsConsoleSafe = true,
            Usage = "/zones [WorldName]",
            Help = "Lists all zones defined on the current map/world.",
            Handler = ZoneList
        };

        internal static void ZoneList( Player player, Command cmd ) {
            World world = player.World;
            string worldName = cmd.Next();
            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;
                player.Message( "List of zones on {0}&S:",
                                world.ClassyName );
            } else if( world != null ) {
                player.Message( "List of zones on this world:" );
            } else {
                player.Message( "When used from console, &H/zones&S command requires a world name." );
                return;
            }

            Map map = world.Map;
            if( map == null ) {
                lock( world.WorldLock ) {
                    map = world.Map;
                    if( map == null ) {
                        if( !MapUtility.TryLoadHeader( world.GetMapName(), out map ) ) {
                            player.Message( "&WERROR:Could not load mapfile for world {0}.",
                                            world.ClassyName );
                        }
                    }
                }
            }

            Zone[] zones = map.Zones.Cache;
            if( zones.Length > 0 ) {
                foreach( Zone zone in zones ) {
                    player.Message( "   {0} ({1}&S) - {2} x {3} x {4}",
                                    zone.Name,
                                    zone.Controller.MinRank.ClassyName,
                                    zone.Bounds.WidthX,
                                    zone.Bounds.WidthY,
                                    zone.Bounds.Height );
                }
                player.Message( "   Type &H/zinfo ZoneName&S for details." );
            } else {
                player.Message( "   No zones defined." );
            }
        }



        static readonly CommandDescriptor CdZoneInfo = new CommandDescriptor {
            Name = "zinfo",
            Category = CommandCategory.Zone | CommandCategory.Info,
            Help = "Shows detailed information about a zone.",
            Usage = "/zinfo ZoneName",
            Handler = ZoneInfo
        };

        internal static void ZoneInfo( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/help zinfo" );
                return;
            }

            Zone zone = player.World.Map.Zones.Find( zoneName );
            if( zone == null ) {
                player.MessageNoZone( zoneName );
                return;
            }

            player.Message( "About zone \"{0}\": size {1} x {2} x {3}, contains {4} blocks, editable by {5}+.",
                            zone.Name,
                            zone.Bounds.WidthX, zone.Bounds.WidthY, zone.Bounds.Height,
                            zone.Bounds.Volume,
                            zone.Controller.MinRank.ClassyName );

            player.Message( "  Zone center is at ({0},{1},{2}).",
                            (zone.Bounds.XMin + zone.Bounds.XMax) / 2,
                            (zone.Bounds.YMin + zone.Bounds.YMax) / 2,
                            (zone.Bounds.HMin + zone.Bounds.HMax) / 2 );

            if( zone.CreatedBy != null ) {
                player.Message( "  Zone created by {0}&S on {1:MMM d} at {1:h:mm} ({2} ago).",
                                zone.CreatedBy.ClassyName,
                                zone.CreatedDate,
                                DateTime.UtcNow.Subtract( zone.CreatedDate ).ToMiniString() );
            }

            if( zone.EditedBy != null ) {
                player.Message( "  Zone last edited by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                zone.EditedBy.ClassyName,
                zone.EditedDate,
                DateTime.UtcNow.Subtract( zone.EditedDate ).Days,
                DateTime.UtcNow.Subtract( zone.EditedDate ).Hours );
            }

            PlayerExceptions zoneExceptions = zone.ExceptionList;

            if( zoneExceptions.Included.Length > 0 ) {
                player.Message( "  Zone whitelist includes: {0}",
                                zoneExceptions.Included.JoinToClassyString() );
            }

            if( zoneExceptions.Excluded.Length > 0 ) {
                player.Message( "  Zone blacklist excludes: {0}",
                                zoneExceptions.Excluded.JoinToClassyString() );
            }
        }



        static readonly CommandDescriptor CdZoneRename = new CommandDescriptor {
            Name = "zrename",
            Category = CommandCategory.Zone,
            Help = "Renames a zone",
            Usage = "/zrename OldName NewName",
            Handler = ZoneRename
        };

        internal static void ZoneRename( Player player, Command cmd ) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                CdZoneRename.PrintUsage( player );
                return;
            }

            if( !World.IsValidName( newName ) ) {
                player.Message( "\"{0}\" is not a valid zone name", newName );
                return;
            }

            var zones = player.World.Map.Zones;

            Zone oldZone = zones.Find( oldName );
            if( oldZone == null ) {
                player.MessageNoZone( oldName );
                return;
            }

            Zone newZone = zones.FindExact( newName );
            if( newZone!=null && newZone != oldZone ) {
                player.Message( "A zone with the name \"{0}\" already exists.", newName );
                return;
            }

            string fullOldName = oldZone.Name;

            zones.Rename( oldZone, newName );
            Logger.Log( "Player {0} renamed zone \"{1}\" to \"{2}\" on world {3}", LogType.UserActivity,
                        player.Name, fullOldName, newName, player.World.Name );
        }
    }
}