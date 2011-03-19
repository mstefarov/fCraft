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



        static readonly CommandDescriptor cdZoneEdit = new CommandDescriptor {
            Name = "zedit",
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

            Zone zone = player.World.Map.FindZone( zoneName );
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
                    if( !player.Info.Rank.AllowSecurityCircumvention && player.Info == info ) {
                        if( !zone.Controller.Check( info ) ) {
                            player.Message( "You must be {0}+&S to add yourself to this zone's whitelist.",
                                            zone.Controller.MinRank.GetClassyName() );
                            continue;
                        }
                    }

                    switch( zone.Controller.Include( info ) ) {
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

                    switch( zone.Controller.Exclude( info ) ) {
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
                        if( !player.Info.Rank.AllowSecurityCircumvention &&
                            zone.Controller.MinRank > player.Info.Rank && minRank <= player.Info.Rank ) {
                            player.Message( "You are not allowed to lower the zone's rank." );
                            continue;
                        }

                        if( zone.Controller.MinRank != minRank ) {
                            zone.Controller.MinRank = minRank;
                            player.Message( "Permission for zone \"{0}\" changed to {1}+",
                                            zone.Name,
                                            minRank.GetClassyName() );
                            changesWereMade = true;
                        }
                    } else {
                        player.NoRankMessage( name );
                    }
                }

                if( changesWereMade ) {
                    zone.Edit( player.Info );
                    player.World.Map.ChangedSinceSave = true;
                } else {
                    player.Message( "No changes were made to the zone." );
                }
            }
        }



        static readonly CommandDescriptor cdZoneAdd = new CommandDescriptor {
            Name = "zadd",
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

                zone.Name = info.Name;
                zone.Controller.MinRank = info.Rank.NextRankUp ?? info.Rank;
                zone.Controller.Include( info );
                player.Message( "Zone: Creating a {0}+&S zone for player {1}&S. Place a block or type /mark to use your location.",
                                zone.Controller.MinRank.GetClassyName(), info.GetClassyName() );
                player.SetCallback( 2, ZoneAddCallback, zone );

            } else {
                if( !Player.IsValidName( zoneName ) ) {
                    player.Message( "\"{0}\" is not a valid zone name", zoneName );
                    return;
                }

                if( player.World.Map.FindZone( zoneName ) != null ) {
                    player.Message( "A zone with this name already exists. Use &H/zedit&S to edit." );
                    return;
                }

                zone.Name = zoneName;

                string rankName = cmd.Next();
                if( rankName == null ) {
                    player.Message( "No rank was specified. See &H/help zone" );
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
                            zone.Controller.Include( info );
                        } else if( name.StartsWith( "-" ) ) {
                            zone.Controller.Exclude( info );
                        }
                    }

                    zone.Controller.MinRank = minRank;
                    player.SetCallback( 2, ZoneAddCallback, zone );
                    player.Message( "Zone: Place a block or type /mark to use your location." );

                } else {
                    player.NoRankMessage( rankName );
                }
            }
        }

        internal static void ZoneAddCallback( Player player, Position[] marks, object tag ) {
            Zone zone = (Zone)tag;

            zone.Create( new BoundingBox( marks[0], marks[1] ), player.Info );

            player.Message( "Zone \"{0}\" created, {1} blocks total.",
                            zone.Name,
                            zone.Bounds.GetVolume() );
            Logger.Log( "Player {0} created a new zone \"{1}\" containing {2} blocks.", LogType.UserActivity,
                        player.Name,
                        zone.Name,
                        zone.Bounds.GetVolume() );

            player.World.Map.AddZone( zone );
        }



        static readonly CommandDescriptor cdZoneTest = new CommandDescriptor {
            Name = "ztest",
            Help = "Allows to test exactly which zones affect a particular block. Can be used to find and resolve zone overlaps.",
            Handler = ZoneTest
        };

        static void ZoneTest( Player player, Command cmd ) {
            player.SelectionMarksExpected = 1;
            player.SelectionMarks.Clear();
            player.SelectionMarkCount = 0;
            player.SelectionCallback = ZoneTestCallback;
            player.Message( "Click the block that you would like to test." );
        }

        internal static void ZoneTestCallback( Player player, Position[] marks, object tag ) {
            Zone[] allowed, denied;
            if( player.World.Map.TestZones( marks[0].X, marks[0].Y, marks[0].H, player, out allowed, out denied ) ) {
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



        static readonly CommandDescriptor cdZoneRemove = new CommandDescriptor {
            Name = "zremove",
            Aliases = new[] { "zdelete" },
            Permissions = new[] { Permission.ManageZones },
            Usage = "/zremove ZoneName",
            Help = "Removes a zone with the specified name from the map.",
            Handler = ZoneRemove
        };

        internal static void ZoneRemove( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                cdZoneRemove.PrintUsage( player );
                return;
            }

            Zone zone = player.World.Map.FindZone( zoneName );
            if( zone != null ) {
                if( !zone.Controller.Check( player.Info ) && !player.Info.Rank.AllowSecurityCircumvention ) {
                    player.Message( "You are not allowed to remove zone {0}.", zone.GetClassyName() );
                    return;
                }
                if( !cmd.Confirmed ) {
                    player.AskForConfirmation( cmd, "You are about to remove zone {0}.", zone.GetClassyName() );
                    return;
                }

                if( player.World.Map.RemoveZone( zoneName ) ) {
                    player.Message( "Zone \"{0}\" removed.", zoneName );
                }

            } else {
                player.Message( "No zone with the name \"{0}\" was found.", zoneName );
            }
        }



        static readonly CommandDescriptor cdZoneList = new CommandDescriptor {
            Name = "zones",
            Help = "Lists all zones defined on the current map/world.",
            Handler = ZoneList
        };

        internal static void ZoneList( Player player, Command cmd ) {
            Zone[] zones = player.World.Map.ZoneList;
            if( zones.Length > 0 ) {
                player.Message( "List of zones (see &H/zinfo ZoneName&S for details):" );
                foreach( Zone zone in zones ) {
                    player.Message( "  {0} ({1}&S) - {2} x {3} x {4}",
                                    zone.Name,
                                    zone.Controller.MinRank.GetClassyName(),
                                    zone.Bounds.GetWidthX(),
                                    zone.Bounds.GetWidthY(),
                                    zone.Bounds.GetHeight() );
                }
            } else {
                player.Message( "No zones are defined for this map." );
            }
        }



        static readonly CommandDescriptor cdZoneInfo = new CommandDescriptor {
            Name = "zinfo",
            Help = "Shows information about a zone",
            Usage = "/zinfo ZoneName",
            Handler = ZoneInfo
        };

        internal static void ZoneInfo( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/help zinfo" );
                return;
            }

            Zone zone = player.World.Map.FindZone( zoneName );
            if( zone == null ) {
                player.Message( "No zone found with the name \"{0}\". See &H/zones", zoneName );
                return;
            }

            player.Message( "About zone \"{0}\": size {1} x {2} x {3}, contains {4} blocks, editable by {5}+.",
                            zone.Name,
                            zone.Bounds.GetWidthX(), zone.Bounds.GetWidthY(), zone.Bounds.GetHeight(),
                            zone.Bounds.GetVolume(),
                            zone.Controller.MinRank.GetClassyName() );

            player.Message( "  Zone centre is at ({0},{1},{2}).",
                            (zone.Bounds.xMin + zone.Bounds.xMax) / 2,
                            (zone.Bounds.yMin + zone.Bounds.yMax) / 2,
                            (zone.Bounds.hMin + zone.Bounds.hMax) / 2 );

            if( zone.CreatedBy != null ) {
                player.Message( "  Zone created by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                                zone.CreatedBy.GetClassyName(),
                                zone.CreatedDate,
                                DateTime.Now.Subtract( zone.CreatedDate ).Days,
                                DateTime.Now.Subtract( zone.CreatedDate ).Hours );
            }

            if( zone.EditedBy != null ) {
                player.Message( "  Zone last edited by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                zone.EditedBy.GetClassyName(),
                zone.EditedDate,
                DateTime.Now.Subtract( zone.EditedDate ).Days,
                DateTime.Now.Subtract( zone.EditedDate ).Hours );
            }

            SecurityController.PlayerListCollection playerList = zone.GetPlayerList();

            if( playerList.Included.Length > 0 ) {
                player.Message( "  Zone whitelist includes: {0}",
                                PlayerInfo.PlayerInfoArrayToString( playerList.Included ) );
            }

            if( playerList.Excluded.Length > 0 ) {
                player.Message( "  Zone blacklist excludes: {0}",
                                PlayerInfo.PlayerInfoArrayToString( playerList.Excluded ) );
            }
        }
    }
}