// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace fCraft {

    public enum ZonePlayerStatus {
        Included,
        Neutral,
        Excluded
    }



    public sealed class Zone {

        public class ZonePlayerList {
            // keeping both lists on one object allows lock-free synchronization
            public PlayerInfo[] included;
            public PlayerInfo[] excluded;
        }

        public BoundingBox bounds;

        public string name;

        SortedDictionary<string, PlayerInfo> includedPlayers = new SortedDictionary<string, PlayerInfo>();
        SortedDictionary<string, PlayerInfo> excludedPlayers = new SortedDictionary<string, PlayerInfo>();

        ZonePlayerList playerList;

        public ZonePlayerList GetPlayerList() {
            return playerList;
        }

        void UpdatePlayerLists() {
            lock( locker ) {
                ZonePlayerList newLists = new ZonePlayerList();
                newLists.included = includedPlayers.Values.ToArray();
                newLists.excluded = excludedPlayers.Values.ToArray();
                playerList = newLists;
            }
        }

        object locker = new object();

        public PlayerClass playerClass;

        public DateTime createdDate, editedDate;
        public PlayerInfo createdBy, editedBy;

        // returns the PREVIOUS state of the player
        public ZonePlayerStatus Include( PlayerInfo info ) {
            lock( locker ) {
                if( includedPlayers.ContainsValue( info ) ) {
                    return ZonePlayerStatus.Included;
                } else if( excludedPlayers.ContainsValue( info ) ) {
                    excludedPlayers.Remove( info.name.ToLower() );
                    return ZonePlayerStatus.Excluded;
                } else {
                    includedPlayers.Add( info.name.ToLower(), info );
                    return ZonePlayerStatus.Neutral;
                }
            }
        }

        // returns the PREVIOUS state of the player
        public ZonePlayerStatus Exclude( PlayerInfo info ) {
            lock( locker ) {
                if( excludedPlayers.ContainsValue( info ) ) {
                    UpdatePlayerLists();
                    return ZonePlayerStatus.Excluded;
                } else if( includedPlayers.ContainsValue( info ) ) {
                    UpdatePlayerLists();
                    includedPlayers.Remove( info.name.ToLower() );
                    return ZonePlayerStatus.Included;
                } else {
                    UpdatePlayerLists();
                    excludedPlayers.Add( info.name.ToLower(), info );
                    return ZonePlayerStatus.Neutral;
                }
            }
        }

        public Zone( string raw ) {
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            name = header[0];
            bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            int buildRank;
            if( Int32.TryParse( header[7], out buildRank ) ) {
                playerClass = ClassList.ParseRank( buildRank );
            } else {
                playerClass = ClassList.ParseClass( header[7] );
            }

            // if all else fails, fall back to lowest class
            if( playerClass == null ) {
                Logger.Log( "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to \"{1}\"", LogType.Error, header[7], ClassList.lowestClass.name );
                playerClass = ClassList.lowestClass;
            }

            foreach( string player in parts[1].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player );
                if( info == null ) continue;
                includedPlayers.Add( info.name.ToLower(), info );
            }

            foreach( string player in parts[2].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player );
                if( info == null ) continue;
                excludedPlayers.Add( info.name.ToLower(), info );
            }

            UpdatePlayerLists();

            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );
                createdBy = PlayerDB.FindPlayerInfoExact( xheader[0] );
                if( createdBy != null ) createdDate = DateTime.Parse( xheader[1] );
                editedBy = PlayerDB.FindPlayerInfoExact( xheader[2] );
                if( editedBy != null ) editedDate = DateTime.Parse( xheader[3] );
            }
        }


        public Zone() {
            UpdatePlayerLists();
        }


        public string Serialize() {
            lock( locker ) {
                string xheader;
                if( createdBy != null ) {
                    xheader = createdBy.name + " " + createdDate.ToString( PlayerInfo.DateFormat ) + " ";
                } else {
                    xheader = "- - ";
                }

                if( editedBy != null ) {
                    xheader += editedBy.name + " " + editedDate.ToString( PlayerInfo.DateFormat );
                } else {
                    xheader += "- -";
                }

                return String.Format( "{0},{1},{2},{3}",
                                      String.Format( "{0} {1} {2} {3} {4} {5} {6} {7}",
                                                     name, bounds.xMin, bounds.yMin, bounds.hMin, bounds.xMax, bounds.yMax, bounds.hMax, playerClass ),
                                      String.Join( " ", includedPlayers.Keys.ToArray() ),
                                      String.Join( " ", excludedPlayers.Keys.ToArray() ),
                                      xheader );
            }
        }


        public bool CanBuild( Player player ) {
            ZonePlayerList list = playerList;
            for( int i = 0; i < list.excluded.Length; i++ ) {
                if( player.info == list.excluded[i] ) return false;
            }

            if( player.info.playerClass.rank >= playerClass.rank ) return true;

            for( int i = 0; i < list.included.Length; i++ ) {
                if( player.info == list.included[i] ) return true;
            }

            return false;
        }

        /*
        public ZonePermissionType CanBuildDetailed( Player player ) {
            ZonePlayerList list = playerList;
            for( int i = 0; i < list.excluded.Length; i++ ) {
                if( player.info == list.excluded[i] ) return ZonePermissionType.ListDenied;
            }

            if( player.info.playerClass.rank >= playerClass.rank ) return ZonePermissionType.RankAllowed;

            for( int i = 0; i < list.included.Length; i++ ) {
                if( player.info == list.included[i] ) return ZonePermissionType.ListAllowed;
            }

            return ZonePermissionType.RankDenied;
        }

        public enum ZonePermissionType {
            RankAllowed,
            RankDenied,
            ListAllowed,
            ListDenied
        }
        */
    }

    public enum ZoneOverride {
        None,
        Allow,
        Deny
    }
}