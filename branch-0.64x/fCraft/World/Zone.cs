// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using LibNbt;

namespace fCraft {
    /// <summary> A bounding box selection that is designated as a sub area within a world.
    /// Zones can have restriction just like worlds on access, and block modification. </summary>
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

        /// <summary> Classy name of the player who created this zone.
        /// Returns "?" if CreatedBy name is unknown, unrecognized, or null. </summary>
        public string CreatedByClassy {
            get {
                return PlayerDB.FindExactClassyName( CreatedBy );
            }
        }

        /// <summary> Player who was the last to edit this zone. May be null if unknown. </summary>
        [CanBeNull]
        public string EditedBy { get; private set; }

        /// <summary> Decorated name of the player who was the last to edit this zone.
        /// Returns "?" if EditedBy name is unknown, unrecognized, or null. </summary>
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
        /// <param name="createdBy"> Player who created this zone. </param>
        public void Create( [NotNull] BoundingBox bounds, [NotNull] PlayerInfo createdBy ) {
            if( bounds == null ) throw new ArgumentNullException( "bounds" );
            if( createdBy == null ) throw new ArgumentNullException( "createdBy" );
            CreatedDate = DateTime.UtcNow;
            Bounds = bounds;
            CreatedBy = createdBy.Name;
        }


        /// <summary> Sets EditedBy and EditedDate fields, and raises Changed event. </summary>
        /// <param name="editedBy"> Name of player or entity who edited this zone. </param>
        /// <exception cref="ArgumentNullException"> editedBy is null. </exception>
        public void OnEdited( [NotNull] string editedBy ) {
            if( editedBy == null ) throw new ArgumentNullException( "editedBy" );
            EditedDate = DateTime.UtcNow;
            EditedBy = editedBy;
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

            // If no ranks are loaded (e.g. MapConverter/MapRenderer)(
            if( RankManager.Ranks.Count > 0 ) {
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
            }

            // If PlayerDB is not loaded (e.g. ConfigGUI)
            if( PlayerDB.IsLoaded ) {
                // Part 2:
                if( parts[1].Length > 0 ) {
                    foreach( string playerName in parts[1].Split( ' ' ) ) {
                        if( !Player.IsValidName( playerName ) ) {
                            Logger.Log( LogType.Warning,
                                        "Invalid entry in zone \"{0}\" whitelist: {1}", Name, playerName );
                            continue;
                        }
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact( playerName );
                        if( info == null ) {
                            Logger.Log( LogType.Warning,
                                        "Unrecognized player in zone \"{0}\" whitelist: {1}", Name, playerName );
                            continue; // player name not found in the DB (discarded)
                        }
                        Controller.Include( info );
                    }
                }

                // Part 3: excluded list
                if( parts[2].Length > 0 ) {
                    foreach( string playerName in parts[2].Split( ' ' ) ) {
                        if( !Player.IsValidName( playerName ) ) {
                            Logger.Log( LogType.Warning,
                                        "Invalid entry in zone \"{0}\" blacklist: {1}", Name, playerName );
                            continue;
                        }
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact( playerName );
                        if( info == null ) {
                            Logger.Log( LogType.Warning,
                                        "Unrecognized player in zone \"{0}\" whitelist: {1}", Name, playerName );
                            continue; // player name not found in the DB (discarded)
                        }
                        Controller.Exclude( info );
                    }
                }
            } else {
                RawWhitelist = parts[1];
                RawBlacklist = parts[2];
            }

            // Part 4: extended header
            if( parts.Length > 3 ) {
                string[] xheader = parts[3].Split( ' ' );
                if( xheader[0] == "-" ) {
                    CreatedBy = null;
                    CreatedDate = DateTime.MinValue;
                } else {
                    CreatedBy = xheader[0];
                    CreatedDate = DateTime.Parse( xheader[1] );
                }

                if( xheader[2] == "-" ) {
                    EditedBy = null;
                    EditedDate = DateTime.MinValue;
                } else {
                    EditedBy = xheader[2];
                    EditedDate = DateTime.Parse( xheader[3] );
                }
            }
        }

        internal readonly string RawWhitelist,
                                 RawBlacklist;


        public Zone( NbtCompound tag ) {
            NbtCompound boundsTag = tag.Get<NbtCompound>( "Bounds" );
            if( boundsTag == null ) {
                throw new SerializationException( "Bounds missing from zone definition tag." );
            }
            Bounds = new BoundingBox( boundsTag );

            NbtCompound controllerTag = tag.Get<NbtCompound>( "Controller" );
            if( controllerTag == null ) {
                throw new SerializationException( "Controller missing from zone definition tag." );
            }
            Controller = new SecurityController( controllerTag );
        }


        public string ClassyName {
            get {
                return Controller.MinRank.Color + Name;
            }
        }


        public event EventHandler Changed;

        void RaiseChangedEvent() {
            var h = Changed;
            if( h != null ) h( null, EventArgs.Empty );
        }
    }
}