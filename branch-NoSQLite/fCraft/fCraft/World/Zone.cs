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
        public BoundingBox bounds;

        public string name;

        public HashSet<string> includedPlayers = new HashSet<string>();
        public HashSet<string> excludedPlayers = new HashSet<string>();

        public PlayerClass playerClass;

        public DateTime createdDate, editedDate;
        public PlayerInfo createdBy, editedBy;

        // returns the PREVIOUS state of the player
        public ZonePlayerStatus Include( string playerName ) {
            if( includedPlayers.Contains( playerName.ToLower() ) ) {
                return ZonePlayerStatus.Included;
            } else if( excludedPlayers.Contains( playerName.ToLower() ) ) {
                excludedPlayers.Remove( playerName );
                return ZonePlayerStatus.Excluded;
            } else {
                includedPlayers.Add( playerName );
                return ZonePlayerStatus.Neutral;
            }
        }

        // returns the PREVIOUS state of the player
        public ZonePlayerStatus Exclude( string playerName ) {
            if( excludedPlayers.Contains( playerName.ToLower() ) ) {
                return ZonePlayerStatus.Excluded;
            } else if( includedPlayers.Contains( playerName.ToLower() ) ) {
                includedPlayers.Remove( playerName );
                return ZonePlayerStatus.Included;
            } else {
                excludedPlayers.Add( playerName );
                return ZonePlayerStatus.Neutral;
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
                includedPlayers.Add( player );
            }

            foreach( string player in parts[2].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                excludedPlayers.Add( player );
            }

            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );
                createdBy = PlayerDB.FindPlayerInfoExact( xheader[0] );
                if( createdBy != null ) createdDate = DateTime.Parse( xheader[1] );
                editedBy = PlayerDB.FindPlayerInfoExact( xheader[2] );
                if( editedBy != null ) editedDate = DateTime.Parse( xheader[3] );
            }
        }


        public Zone() { }


        public string Serialize() {
            string xheader;
            if( createdBy != null ) {
                xheader = createdBy.name + " " + createdDate.ToString(PlayerInfo.DateFormat) + " ";
            }else{
                xheader = "- - ";
            }
            if( editedBy != null ) {
                xheader = editedBy.name + " " + editedDate.ToString( PlayerInfo.DateFormat );
            }else{
                xheader += "- -";
            }

            return String.Format( "{0},{1},{2},{3}",
                                  String.Format( "{0} {1} {2} {3} {4} {5} {6} {7}",
                                                 name, bounds.xMin, bounds.yMin, bounds.hMin, bounds.xMax, bounds.yMax, bounds.hMax, playerClass ),
                                  String.Join( " ", includedPlayers.ToArray() ),
                                  String.Join( " ", excludedPlayers.ToArray() ),
                                  xheader );
        }


        public bool CanBuild( Player player ) {
            if( includedPlayers.Contains( player.lowercaseName ) ) return true;
            if( excludedPlayers.Contains( player.lowercaseName ) ) return false;
            return player.info.playerClass.rank >= playerClass.rank;
        }
    }

    public enum ZoneOverride {
        None,
        Allow,
        Deny
    }
}