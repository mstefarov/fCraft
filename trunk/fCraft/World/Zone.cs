// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.Xml.Linq;

namespace fCraft {

    public sealed class Zone : IClassy {

        public BoundingBox bounds;
        public SecurityController controller = new SecurityController();

        public string name;

        public SecurityController.PlayerListCollection GetPlayerList() {
            return controller.exceptionList;
        }

        public DateTime createdDate, editedDate;
        public PlayerInfo createdBy, editedBy;


        public Zone() { }


        public Zone( string raw, World world ) {
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            name = header[0];
            bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            controller.minRank = RankList.ParseRank( header[7] );

            // if all else fails, fall back to lowest class
            if( controller.minRank == null ) {
                if( world != null ) {
                    controller.minRank = world.buildSecurity.minRank;
                } else {
                    controller.minRank = RankList.LowestRank;
                }
                Logger.Log( "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to default ({1}).", LogType.Error,
                            header[7], controller.minRank.Name );
            }

            // Part 2:
            foreach( string player in parts[1].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player );
                if( info == null ) continue; // player name not found in the DB (discarded)
                controller.Include( info );
            }

            // Part 3: excluded list
            foreach( string player in parts[2].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player );
                if( info == null ) continue; // player name not found in the DB (discarded)
                controller.Exclude( info );
            }

            controller.UpdatePlayerListCache();

            // Part 4: extended header
            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );
                createdBy = PlayerDB.FindPlayerInfoExact( xheader[0] );
                if( createdBy != null ) createdDate = DateTime.Parse( xheader[1] );
                editedBy = PlayerDB.FindPlayerInfoExact( xheader[2] );
                if( editedBy != null ) editedDate = DateTime.Parse( xheader[3] );
            }
        }


        public string SerializeFCMv2() {
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

            SecurityController.PlayerListCollection list = controller.exceptionList;
            StringBuilder includedList = new StringBuilder();
            bool firstWord = true;
            foreach( PlayerInfo info in list.included ) {
                if( firstWord ) includedList.Append( ' ' );
                includedList.Append( info.name );
                firstWord = false;
            }

            firstWord = true;
            StringBuilder excludedList = new StringBuilder();
            foreach( PlayerInfo info in list.excluded ) {
                if( firstWord ) excludedList.Append( ' ' );
                excludedList.Append( info.name );
                firstWord = false;
            }

            return String.Format( "{0},{1},{2},{3}",
                                  String.Format( "{0} {1} {2} {3} {4} {5} {6} {7}",
                                                 name, bounds.xMin, bounds.yMin, bounds.hMin, bounds.xMax, bounds.yMax, bounds.hMax, controller.minRank ),
                                  includedList, excludedList, xheader );
        }


        public string GetClassyName() {
            return controller.minRank.Color + name;
        }


        #region Xml Serialization

        const string XmlRootElementName = "Zone";

        public Zone( XElement root ) {
            name = root.Element( "name" ).Value;

            if( root.Element( "created" ) != null ) {
                XElement created = root.Element( "created" );
                createdBy = PlayerDB.FindPlayerInfoExact( created.Attribute( "by" ).Value );
                createdDate = DateTime.Parse( created.Attribute( "on" ).Value );
            }

            if( root.Element( "edited" ) != null ) {
                XElement edited = root.Element( "edited" );
                editedBy = PlayerDB.FindPlayerInfoExact( edited.Attribute( "by" ).Value );
                editedDate = DateTime.Parse( edited.Attribute( "on" ).Value );
            }

            bounds = new BoundingBox( root.Element( BoundingBox.XmlRootElementName ) );
            controller = new SecurityController( root.Element( XmlRootElementName ) );
        }


        public XElement Serialize() {
            XElement root = new XElement( XmlRootElementName );
            root.Add( new XElement( "name", name ) );

            if( createdBy != null ) {
                XElement created = new XElement( "created" );
                created.Add( new XAttribute( "by", createdBy.name ) );
                created.Add( new XAttribute( "on", createdDate.ToCompactString()));
                root.Add( created );
            }

            if( editedBy != null ) {
                XElement edited = new XElement( "edited" );
                edited.Add( new XAttribute( "by", editedBy.name ) );
                edited.Add( new XAttribute( "on", editedDate.ToCompactString() ) );
                root.Add( edited );
            }

            root.Add( bounds.Serialize() );
            root.Add( controller.Serialize() );
            return root;
        }

        #endregion
    }
}