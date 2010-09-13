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
                player.Message( "Permission for zone \"{0}\" changed to {1}+",
                                zone.name,
                                minRank.GetClassyName() );
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
                player.SetCallback( 2, ZoneAddCallback, zone );
                player.Message( "Zone: Place a block or type /mark to use your location." );
            } else {
                player.Message( "Unrecognized player class: \"{0}\"", property );
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
                    player.Message( String.Format( "  {0} ({1}&S) - {2} x {3} x {4}",
                                                   zone.name,
                                                   zone.build.GetClassyName(),
                                                   zone.bounds.GetWidthX(),
                                                   zone.bounds.GetWidthY(),
                                                   zone.bounds.GetHeight() ) );
                }
            } else {
                player.Message( "No zones are defined for this map." );
            }
        }
    }
}
