// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace fCraft {

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

        public Rank rank;

        public DateTime createdDate, editedDate;
        public PlayerInfo createdBy, editedBy;

        // returns the PREVIOUS state of the player
        public ZoneOverride Include( PlayerInfo info ) {
            lock( locker ) {
                if( includedPlayers.ContainsValue( info ) ) {
                    UpdatePlayerLists();
                    return ZoneOverride.Allow;
                } else if( excludedPlayers.ContainsValue( info ) ) {
                    excludedPlayers.Remove( info.name.ToLower() );
                    UpdatePlayerLists();
                    return ZoneOverride.Deny;
                } else {
                    includedPlayers.Add( info.name.ToLower(), info );
                    UpdatePlayerLists();
                    return ZoneOverride.None;
                }
            }
        }

        // returns the PREVIOUS state of the player
        public ZoneOverride Exclude( PlayerInfo info ) {
            lock( locker ) {
                if( excludedPlayers.ContainsValue( info ) ) {
                    UpdatePlayerLists();
                    return ZoneOverride.Deny;
                } else if( includedPlayers.ContainsValue( info ) ) {
                    includedPlayers.Remove( info.name.ToLower() );
                    UpdatePlayerLists();
                    return ZoneOverride.Allow;
                } else {
                    excludedPlayers.Add( info.name.ToLower(), info );
                    UpdatePlayerLists();
                    return ZoneOverride.None;
                }
            }
        }

        public Zone( string raw, World world ) {
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            name = header[0];
            bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            rank = RankList.ParseRank( header[7] );

            // if all else fails, fall back to lowest class
            if( rank == null ) {
                rank = world.buildRank;
                Logger.Log( "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to default ({1}).", LogType.Error,
                            header[7], rank.Name );
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
                                                     name, bounds.xMin, bounds.yMin, bounds.hMin, bounds.xMax, bounds.yMax, bounds.hMax, rank ),
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

            if( player.info.rank >= rank ) return true;

            for( int i = 0; i < list.included.Length; i++ ) {
                if( player.info == list.included[i] ) return true;
            }

            return false;
        }

        
        public ZonePermissionType CanBuildDetailed( Player player ) {
            ZonePlayerList list = playerList;
            for( int i = 0; i < list.excluded.Length; i++ ) {
                if( player.info == list.excluded[i] ) return ZonePermissionType.BlackListed;
            }

            if( player.info.rank >= rank ) return ZonePermissionType.Allowed;

            for( int i = 0; i < list.included.Length; i++ ) {
                if( player.info == list.included[i] ) return ZonePermissionType.WhiteListed;
            }

            return ZonePermissionType.Denied;
        }
    }


    public enum ZoneOverride {
        None,
        Allow,
        Deny
    }


    public enum ZonePermissionType {
        Allowed,
        Denied,
        WhiteListed,
        BlackListed
    }
}