// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;


namespace fCraft {

    public sealed class Zone : PermissionController, IClassy {

        public BoundingBox bounds;

        public string name;

        public PlayerListCollection GetPlayerList() {
            return permissionList;
        }

        public DateTime createdDate, editedDate;
        public PlayerInfo createdBy, editedBy;


        public Zone( string raw, World world ) {
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            name = header[0];
            bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            minRank = RankList.ParseRank( header[7] );

            // if all else fails, fall back to lowest class
            if( minRank == null ) {
                minRank = world.buildRank;
                Logger.Log( "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to default ({1}).", LogType.Error,
                            header[7], minRank.Name );
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

            UpdatePlayerListCache();

            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );
                createdBy = PlayerDB.FindPlayerInfoExact( xheader[0] );
                if( createdBy != null ) createdDate = DateTime.Parse( xheader[1] );
                editedBy = PlayerDB.FindPlayerInfoExact( xheader[2] );
                if( editedBy != null ) editedDate = DateTime.Parse( xheader[3] );
            }
        }


        public Zone() {
            UpdatePlayerListCache();
        }


        public string Serialize() {
            lock( playerPermissionListLock ) {
                string xheader;
                if( createdBy != null ) {
                    xheader = createdBy.name + " " + createdDate.ToCompactString() + " ";
                } else {
                    xheader = "- - ";
                }

                if( editedBy != null ) {
                    xheader += editedBy.name + " " + editedDate.ToCompactString();
                } else {
                    xheader += "- -";
                }

                return String.Format( "{0},{1},{2},{3}",
                                      String.Format( "{0} {1} {2} {3} {4} {5} {6} {7}",
                                                     name, bounds.xMin, bounds.yMin, bounds.hMin, bounds.xMax, bounds.yMax, bounds.hMax, minRank ),
                                      String.Join( " ", includedPlayers.Keys.ToArray() ),
                                      String.Join( " ", excludedPlayers.Keys.ToArray() ),
                                      xheader );
            }
        }

        public string GetClassyName() {
            return minRank.Color + name;
        }
    }
}