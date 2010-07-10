// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace fCraft {
    public sealed class Zone {
        public int xMin, yMin, hMin, xMax, yMax, hMax;
        public int GetWidthX() { return xMax - xMin+1; }
        public int GetWidthY() { return yMax - yMin+1; }
        public int GetHeight() { return hMax - hMin+1; }
        public int GetVolume() { return GetWidthX() * GetWidthY() * GetHeight(); }

        public string name;

        public HashSet<string> includedPlayers = new HashSet<string>();
        public HashSet<string> excludedPlayers = new HashSet<string>();

        public int buildRank = 0;


        public Zone( string raw ) {
            string[] parts = raw.Split( ',' );
            if( parts.Length < 3 ) throw new Exception( "Corrupt zone definition" );
            string[] header = parts[0].Split( ' ' );
            name = header[0];
            xMin = Int32.Parse( header[1] );
            yMin = Int32.Parse( header[2] );
            hMin = Int32.Parse( header[3] );
            xMax = Int32.Parse( header[4] );
            yMax = Int32.Parse( header[5] );
            hMax = Int32.Parse( header[6] );
            buildRank = Int32.Parse( header[7] );

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
                                                 name, xMin, yMin, hMin, xMax, yMax, hMax, buildRank ),
                                  String.Join( " ", includedPlayers.ToArray() ),
                                  String.Join( " ", excludedPlayers.ToArray() ) );
        }


        public bool CanBuild( Player player ) {
            if( includedPlayers.Contains( player.name ) ) return true;
            if( excludedPlayers.Contains( player.name ) ) return false;
            return player.info.playerClass.rank >= buildRank;
        }


        public bool Contains( int x, int y, int h ) {
            return x >= xMin && x <= xMax &&
                   y >= yMin && y <= yMax &&
                   h >= hMin && h <= hMax;
        }
    }
}