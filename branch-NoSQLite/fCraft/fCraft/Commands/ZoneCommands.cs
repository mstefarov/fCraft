using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    class ZoneCommands {

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
            usage = "/zedit ZoneName [ClassName] [+IncludedName] [-ExcludedName]",
            help = "Allows editing the zone permissions after creation. " +
                   "You can change the class restrictions, and include or exclude individual players.",
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
                string subName = name.Substring( 1 );
                if( name.StartsWith( "+" ) ) {
                    if( Player.IsValidName( subName ) ) {
                        switch( zone.Include( subName ) ) {
                            case ZonePlayerStatus.Excluded:
                                player.Message( "{0} is no longer excluded from zone {1}", subName, zone.name );
                                changesWereMade = true;
                                break;
                            case ZonePlayerStatus.Neutral:
                                player.Message( "{0} is now included in zone {1}", subName, zone.name );
                                changesWereMade = true;
                                break;
                            case ZonePlayerStatus.Included:
                                player.Message( "{0} is already included in zone {1}", subName, zone.name );
                                break;
                        }
                    } else {
                        player.Message( "Invalid player name: {0}", subName );
                    }
                } else if( name.StartsWith( "-" ) ) {
                    if( Player.IsValidName( subName ) ) {
                        switch( zone.Exclude( subName ) ) {
                            case ZonePlayerStatus.Excluded:
                                player.Message( "{0} is already excluded to zone {1}", subName, zone.name );
                                break;
                            case ZonePlayerStatus.Neutral:
                                player.Message( "{0} is now excluded to zone {1}", subName, zone.name );
                                changesWereMade = true;
                                break;
                            case ZonePlayerStatus.Included:
                                player.Message( "{0} is no longer included in zone {1}", subName, zone.name );
                                changesWereMade = true;
                                break;
                        }
                    } else {
                        player.Message( "Invalid player name: \"{0}\"", subName );
                    }
                } else {
                    PlayerClass minRank = ClassList.ParseClass( name );
                    if( minRank != null ) {
                        player.Message( "Unrecognized class name: \"{0}\"", name );
                    } else {
                        if( zone.playerClass != minRank ) {
                            zone.playerClass = minRank;
                            player.Message( "Permission for zone \"{0}\" changed to {1}+",
                                            zone.name,
                                            minRank.GetClassyName() );
                            changesWereMade = true;
                        }
                    }
                }

                if( changesWereMade ) {
                    player.world.map.changesSinceSave++;
                    player.world.SaveMap( null );
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
            usage = "/zadd ZoneName ClassName",
            help = "Create a zone that overrides build permissions. " +
                   "This can be used to restrict access to an area (by setting ClassName to a high rank) " +
                   "or to designate a guest area (by setting ClassName to a class that normally can't build).",
            handler = ZoneAdd
        };

        internal static void ZoneAdd( Player player, Command cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                cdZoneAdd.PrintUsage( player );
                return;
            }

            if( !Player.IsValidName( zoneName ) ) {
                player.Message( "\"{0}\" is not a valid zone name", zoneName );
                return;
            }

            if( player.world.map.FindZone( zoneName ) != null ) {
                player.Message( "A zone with this name already exists. Use &H/zedit&S to edit." );
                return;
            }

            Zone zone = new Zone();
            zone.name = zoneName;

            string className = cmd.Next();
            if( className == null ) {
                player.Message( "No class was specified. See &H/help zone" );
                return;
            }
            PlayerClass minRank = ClassList.ParseClass( className );

            if( minRank != null ) {
                string name;
                while( (name = cmd.Next()) != null ) {
                    if( name.StartsWith( "+" ) ) {
                        if( Player.IsValidName( name.Substring( 1 ) ) ) {
                            zone.Include( name.Substring( 1 ) );
                        } else {
                            player.Message( "Invalid player name: {0}", name.Substring( 1 ) );
                        }
                    } else if( name.StartsWith( "-" ) ) {
                        if( Player.IsValidName( name.Substring( 1 ) ) ) {
                            zone.Exclude( name.Substring( 1 ) );
                        } else {
                            player.Message( "Invalid player name: {0}", name.Substring( 1 ) );
                        }
                    }
                }

                zone.playerClass = minRank;
                player.SetCallback( 2, ZoneAddCallback, zone );
                player.Message( "Zone: Place a block or type /mark to use your location." );
            } else {
                player.Message( "Unrecognized player class: \"{0}\"", className );
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
                cdZoneRemove.PrintUsage( player );
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
                    player.Message( String.Format( "  {0} ({1}&S) - {2} x {3} x {4}",
                                                   zone.name,
                                                   zone.playerClass.GetClassyName(),
                                                   zone.bounds.GetWidthX(),
                                                   zone.bounds.GetWidthY(),
                                                   zone.bounds.GetHeight() ) );
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
                            zone.name, zone.bounds.GetWidthX(), zone.bounds.GetWidthY(), zone.bounds.GetHeight(),
                            zone.bounds.GetVolume(),
                            zone.playerClass.GetClassyName() );

            if( zone.createdBy != null ) {
                player.Message( "  Zone created by {0}&S on {1} ({2} ago).",
                                zone.createdBy.GetClassyName(),
                                zone.createdDate,
                                DateTime.Now.Subtract( zone.createdDate ) );
            }

            if( zone.editedBy != null ) {
                player.Message( "  Zone last edited by {0}&S on {1} ({2} ago).",
                zone.editedBy.GetClassyName(),
                zone.editedDate,
                DateTime.Now.Subtract( zone.editedDate ) );
            }

            if( zone.includedPlayers.Count > 0 ) {
                player.Message( "  Zone whitelist includes: {0}",
                                String.Join( ", ", zone.includedPlayers.ToArray() ) );
            }

            if( zone.excludedPlayers.Count > 0 ) {
                player.Message( "  Zone blacklist excludes: {0}",
                                String.Join( ", ", zone.excludedPlayers.ToArray() ) );
            }
        }
    }
}
