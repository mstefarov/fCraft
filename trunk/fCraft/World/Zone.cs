// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.Xml.Linq;

namespace fCraft {

    public sealed class Zone : IClassy {

        public BoundingBox Bounds { get; private set; }
        public SecurityController Controller = new SecurityController();

        public string Name;

        public SecurityController.PlayerListCollection GetPlayerList() {
            return Controller.ExceptionList;
        }

        public DateTime CreatedDate { get; private set; }
        public DateTime EditedDate { get; private set; }
        public PlayerInfo CreatedBy { get; private set; }
        public PlayerInfo EditedBy { get; private set; }


        public void Create( BoundingBox bounds, PlayerInfo createdBy ) {
            CreatedDate = DateTime.UtcNow;
            Bounds = bounds;
            CreatedBy = createdBy;
        }

        public void Edit( PlayerInfo editedBy ) {
            EditedDate = DateTime.UtcNow;
            EditedBy = editedBy;
        }


        public Zone() { }


        public Zone( string raw, World world ) {
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            Name = header[0];
            Bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            Rank buildRank = RankList.ParseRank( header[7] );
            // if all else fails, fall back to lowest class
            if( buildRank == null ) {
                if( world != null ) {
                    Controller.MinRank = world.BuildSecurity.MinRank;
                } else {
                    Controller.MinRank = null;
                }
                Logger.Log( "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to default ({1}).", LogType.Error,
                            header[7], Controller.MinRank.Name );
            } else {
                Controller.MinRank = buildRank;
            }


            // Part 2:
            foreach( string player in parts[1].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player );
                if( info == null ) continue; // player name not found in the DB (discarded)
                Controller.Include( info );
            }

            // Part 3: excluded list
            foreach( string player in parts[2].Split( ' ' ) ) {
                if( !Player.IsValidName( player ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player );
                if( info == null ) continue; // player name not found in the DB (discarded)
                Controller.Exclude( info );
            }

            Controller.UpdatePlayerListCache();

            // Part 4: extended header
            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );
                CreatedBy = PlayerDB.FindPlayerInfoExact( xheader[0] );
                if( CreatedBy != null ) CreatedDate = DateTime.Parse( xheader[1] );
                EditedBy = PlayerDB.FindPlayerInfoExact( xheader[2] );
                if( EditedBy != null ) EditedDate = DateTime.Parse( xheader[3] );
            }
        }


        public string SerializeFCMv2() {
            string xheader;
            if( CreatedBy != null ) {
                xheader = CreatedBy.Name + " " + CreatedDate.ToCompactString() + " ";
            } else {
                xheader = "- - ";
            }

            if( EditedBy != null ) {
                xheader += EditedBy.Name + " " + EditedDate.ToCompactString();
            } else {
                xheader += "- -";
            }

            SecurityController.PlayerListCollection list = Controller.ExceptionList;
            StringBuilder includedList = new StringBuilder();
            bool firstWord = true;
            foreach( PlayerInfo info in list.Included ) {
                if( firstWord ) includedList.Append( ' ' );
                includedList.Append( info.Name );
                firstWord = false;
            }

            firstWord = true;
            StringBuilder excludedList = new StringBuilder();
            foreach( PlayerInfo info in list.Excluded ) {
                if( firstWord ) excludedList.Append( ' ' );
                excludedList.Append( info.Name );
                firstWord = false;
            }

            return String.Format( "{0},{1},{2},{3}",
                                  String.Format( "{0} {1} {2} {3} {4} {5} {6} {7}",
                                                 Name, Bounds.xMin, Bounds.yMin, Bounds.hMin, Bounds.xMax, Bounds.yMax, Bounds.hMax, Controller.MinRank ),
                                  includedList, excludedList, xheader );
        }


        public string GetClassyName() {
            return Controller.MinRank.Color + Name;
        }


        #region Xml Serialization

        const string XmlRootElementName = "Zone";

        public Zone( XElement root ) {
            Name = root.Element( "name" ).Value;

            if( root.Element( "created" ) != null ) {
                XElement created = root.Element( "created" );
                CreatedBy = PlayerDB.FindPlayerInfoExact( created.Attribute( "by" ).Value );
                CreatedDate = DateTime.Parse( created.Attribute( "on" ).Value );
            }

            if( root.Element( "edited" ) != null ) {
                XElement edited = root.Element( "edited" );
                EditedBy = PlayerDB.FindPlayerInfoExact( edited.Attribute( "by" ).Value );
                EditedDate = DateTime.Parse( edited.Attribute( "on" ).Value );
            }

            Bounds = new BoundingBox( root.Element( BoundingBox.XmlRootElementName ) );
            Controller = new SecurityController( root.Element( XmlRootElementName ) );
        }


        public XElement Serialize() {
            XElement root = new XElement( XmlRootElementName );
            root.Add( new XElement( "name", Name ) );

            if( CreatedBy != null ) {
                XElement created = new XElement( "created" );
                created.Add( new XAttribute( "by", CreatedBy.Name ) );
                created.Add( new XAttribute( "on", CreatedDate.ToCompactString() ) );
                root.Add( created );
            }

            if( EditedBy != null ) {
                XElement edited = new XElement( "edited" );
                edited.Add( new XAttribute( "by", EditedBy.Name ) );
                edited.Add( new XAttribute( "on", EditedDate.ToCompactString() ) );
                root.Add( edited );
            }

            root.Add( Bounds.Serialize() );
            root.Add( Controller.Serialize() );
            return root;
        }

        #endregion
    }
}