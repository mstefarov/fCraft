// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace fCraft {
    public sealed class Zone {
        public BoundingBox bounds;

        public string name;

        public HashSet<string> includedPlayers = new HashSet<string>();
        public HashSet<string> excludedPlayers = new HashSet<string>();

        public PlayerClass build;


        public Zone( string raw ) {
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            name = header[0];
            bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            int buildRank;
            if( Int32.TryParse( header[7], out buildRank ) ) {
                build = ClassList.ParseRank( buildRank );
            } else {
                build = ClassList.ParseClass( header[7] );
            }

            // if all else fails, fall back to lowest class
            if( build == null ) {
                Logger.Log( "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to \"{1}\"", LogType.Error, header[7], ClassList.lowestClass.name );
                build = ClassList.lowestClass;
            }

            foreach( string player in parts[1].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                includedPlayers.Add( player );
            }

            foreach( string player in parts[2].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                excludedPlayers.Add( player );
            }
        }


        public Zone() { }


        public string Serialize() {
            return String.Format( "{0},{1},{2}",
                                  String.Format( "{0} {1} {2} {3} {4} {5} {6} {7}",
                                                 name, bounds.xMin, bounds.yMin, bounds.hMin, bounds.xMax, bounds.yMax, bounds.hMax, build ),
                                  String.Join( " ", includedPlayers.ToArray() ),
                                  String.Join( " ", excludedPlayers.ToArray() ) );
        }


        public bool CanBuild( Player player ) {
            if( includedPlayers.Contains( player.name ) ) return true;
            if( excludedPlayers.Contains( player.name ) ) return false;
            return player.info.playerClass.rank >= build.rank;
        }
    }

    public enum ZoneOverride {
        None,
        Allow,
        Deny
    }
}