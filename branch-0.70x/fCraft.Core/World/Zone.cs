// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Runtime.Serialization;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {

    public sealed class Zone : IClassy, INotifiesOnChange {

        /// <summary> Zone boundaries. </summary>
        [NotNull]
        public BoundingBox Bounds { get; private set; }

        /// <summary> Zone build permission controller. </summary>
        [NotNull]
        public readonly SecurityController Controller = new SecurityController();

        /// <summary> Zone name (case-preserving but case-insensitive). </summary>
        [NotNull]
        public string Name { get; set; }

        /// <summary> List of exceptions (included and excluded players). </summary>
        public PlayerExceptions ExceptionList {
            get { return Controller.ExceptionList; }
        }

        /// <summary> Zone creation date, UTC. </summary>
        public DateTime CreatedDate { get; private set; }

        /// <summary> Zone editing date, UTC. </summary>
        public DateTime EditedDate { get; private set; }

        /// <summary> Player who created this zone. May be null if unknown. </summary>
        [CanBeNull]
        public string CreatedBy { get; private set; }

        [NotNull]
        public string CreatedByClassy {
            get {
                return PlayerDB.FindExactClassyName( CreatedBy );
            }
        }

        /// <summary> Player who was the last to edit this zone. May be null if unknown. </summary>
        [CanBeNull]
        public string EditedBy { get; private set; }

        [NotNull]
        public string EditedByClassy {
            get {
                return PlayerDB.FindExactClassyName( EditedBy );
            }
        }

        /// <summary> Map that this zone is on. </summary>
        [NotNull]
        public Map Map { get; set; }


        /// <summary> Creates the zone boundaries, and sets CreatedDate/CreatedBy. </summary>
        /// <param name="bounds"> New zone boundaries. </param>
        /// <param name="createdBy"> Player who created this zone. May not be null. </param>
        public void Create( [NotNull] BoundingBox bounds, [NotNull] PlayerInfo createdBy ) {
            if( bounds == null ) throw new ArgumentNullException( "bounds" );
            if( createdBy == null ) throw new ArgumentNullException( "createdBy" );
            CreatedDate = DateTime.UtcNow;
            Bounds = bounds;
            CreatedBy = createdBy.Name;
        }


        public void Edit( [NotNull] PlayerInfo editedBy ) {
            if( editedBy == null ) throw new ArgumentNullException( "editedBy" );
            EditedDate = DateTime.UtcNow;
            EditedBy = editedBy.Name;
            RaiseChangedEvent();
        }


        public Zone() {
            Controller.Changed += ( o, e ) => RaiseChangedEvent();
        }


        public Zone( [NotNull] string raw, [CanBeNull] World world )
            : this() {
            if( raw == null ) throw new ArgumentNullException( "raw" );
            string[] parts = raw.Split( ',' );

            string[] header = parts[0].Split( ' ' );
            Name = header[0];
            Bounds = new BoundingBox( Int32.Parse( header[1] ), Int32.Parse( header[2] ), Int32.Parse( header[3] ),
                                      Int32.Parse( header[4] ), Int32.Parse( header[5] ), Int32.Parse( header[6] ) );

            Rank buildRank = Rank.Parse( header[7] );
            // if all else fails, fall back to lowest class
            if( buildRank == null ) {
                if( world != null ) {
                    Controller.MinRank = world.BuildSecurity.MinRank;
                } else {
                    Controller.ResetMinRank();
                }
                Logger.Log( LogType.Error,
                            "Zone: Error parsing zone definition: unknown rank \"{0}\". Permission reset to default ({1}).",
                            header[7], Controller.MinRank.Name );
            } else {
                Controller.MinRank = buildRank;
            }

            if( PlayerDB.IsLoaded ) {
                // Part 2:
                foreach( string playerName in parts[1].Split( ' ' ) ) {
                    if( !Player.IsValidName( playerName ) ) {
                        Logger.Log( LogType.Warning,
                                    "Invalid entry in zone \"{0}\" whitelist: {1}", Name, playerName );
                        continue;
                    }
                    PlayerInfo info = PlayerDB.FindExact( playerName );
                    if( info == null ) {
                        Logger.Log( LogType.Warning,
                                    "Unrecognized player in zone \"{0}\" whitelist: {1}", Name, playerName );
                        continue; // player name not found in the DB (discarded)
                    }
                    Controller.Include( info );
                }

                // Part 3: excluded list
                foreach( string playerName in parts[2].Split( ' ' ) ) {
                    if( !Player.IsValidName( playerName ) ) {
                        Logger.Log( LogType.Warning,
                                    "Invalid entry in zone \"{0}\" blacklist: {1}", Name, playerName );
                        continue;
                    }
                    PlayerInfo info = PlayerDB.FindExact( playerName );
                    if( info == null ) {
                        Logger.Log( LogType.Warning,
                                    "Unrecognized player in zone \"{0}\" whitelist: {1}", Name, playerName );
                        continue; // player name not found in the DB (discarded)
                    }
                    Controller.Exclude( info );
                }
            }

            // Part 4: extended header
            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );
                CreatedBy = xheader[0];
                if( CreatedBy != null ) CreatedDate = DateTime.Parse( xheader[1] );
                EditedBy = xheader[2];
                if( EditedBy != null ) EditedDate = DateTime.Parse( xheader[3] );
            }
        }


        public string ClassyName {
            get {
                return Controller.MinRank.Color + Name;
            }
        }


        #region Xml Serialization

        const string XmlRootName = "Zone";

        public Zone( [NotNull] XContainer root ) {
            if( root == null ) throw new ArgumentNullException( "root" );
            Name = root.Element( "name" ).Value;

            if( root.Element( "created" ) != null ) {
                XElement created = root.Element( "created" );
                CreatedBy = created.Attribute( "by" ).Value;
                DateTime createdDate;
                created.Attribute( "on" ).Value.ToDateTime( out createdDate );
                CreatedDate = createdDate;
            }

            if( root.Element( "edited" ) != null ) {
                XElement edited = root.Element( "edited" );
                EditedBy = edited.Attribute( "by" ).Value;
                DateTime editedDate;
                edited.Attribute( "on" ).Value.ToDateTime( out editedDate );
                EditedDate = editedDate;
            }

            XElement temp = root.Element( BoundingBox.XmlRootName );
            if( temp == null ) throw new SerializationException( "No BoundingBox specified for zone." );
            Bounds = new BoundingBox( temp );

            temp = root.Element( SecurityController.XmlRootName );
            if( temp == null ) throw new SerializationException( "No SecurityController specified for zone." );
            Controller = new SecurityController( temp, PlayerDB.IsLoaded );
        }


        public XElement Serialize() {
            XElement root = new XElement( XmlRootName );
            root.Add( new XElement( "name", Name ) );

            if( CreatedBy != null ) {
                XElement created = new XElement( "created" );
                created.Add( new XAttribute( "by", CreatedBy ) );
                created.Add( new XAttribute( "on", CreatedDate.ToUnixTimeString() ) );
                root.Add( created );
            }

            if( EditedBy != null ) {
                XElement edited = new XElement( "edited" );
                edited.Add( new XAttribute( "by", EditedBy ) );
                edited.Add( new XAttribute( "on", EditedDate.ToUnixTimeString() ) );
                root.Add( edited );
            }

            root.Add( Bounds.Serialize() );
            root.Add( Controller.Serialize() );
            return root;
        }

        #endregion


        public event EventHandler Changed;

        void RaiseChangedEvent() {
            var handler = Changed;
            if( handler != null ) handler( null, EventArgs.Empty );
        }
    }
}