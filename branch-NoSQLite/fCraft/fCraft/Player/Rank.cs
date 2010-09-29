// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;


namespace fCraft {
    public sealed class Rank {

        public sealed class RankDefinitionException : Exception {
            public RankDefinitionException( string message ) : base( message ) { }
            public RankDefinitionException( string message, params string[] args ) :
                base( String.Format( message, args ) ) { }
        }

        public string Name { get; set; }

        public byte rank;

        public string Color { get; set; }

        public string ID { get; set; }

        public bool[] Permissions {
            get;
            private set;
        }

        public Rank maxPromote,
                           maxDemote,
                           maxKick,
                           maxBan,
                           maxHideFrom;

        public string prefix = "";
        public int idleKickTimer,
                   drawLimit,
                   antiGriefBlocks = 35,
                   antiGriefSeconds = 5;
        public bool reservedSlot;
        public int index;

        public Rank nextRankUp, nextRankDown;

        // these need to be parsed after all ranks are added
        internal string maxPromoteVal = "",
                        maxDemoteVal = "",
                        maxKickVal = "",
                        maxBanVal = "",
                        maxHideFromVal = "";


        public Rank() {
            Permissions = new bool[Enum.GetValues( typeof( Permission ) ).Length];
        }


        public Rank( XElement el ) : this() {

            // Name
            XAttribute attr = el.Attribute( "name" );
            if( attr == null ) {
                throw new RankDefinitionException( "Class definition with no name was ignored." );
            }
            if( !Rank.IsValidRankName( attr.Value.Trim() ) ) {
                throw new RankDefinitionException( "Invalid name specified for class \"{0}\". Class names can only contain letters, digits, and underscores. Class definition was ignored.", Name );
            }
            Name = attr.Value.Trim();

            if( RankList.ranksByName.ContainsKey( Name.ToLower() ) ) {
                throw new RankDefinitionException( "Duplicate name for class \"{0}\". Class definition was ignored.", Name );
            }


            // ID
            attr = el.Attribute( "id" );
            if( attr == null ) {
                Logger.Log( "PlayerClass({0}): Issued a new unique ID.", LogType.Warning, Name );
                ID = RankList.GenerateID();

            } else if( !Rank.IsValidID( attr.Value.Trim() ) ) {
                throw new RankDefinitionException( "Invalid ID specified for class \"{0}\". ID must be alphanumeric, and exactly 16 characters long. Class definition was ignored.", Name );

            } else {
                ID = attr.Value.Trim();
                if( RankList.ranksByID.ContainsKey( Name ) ) {
                    throw new RankDefinitionException( "Duplicate ID for {0}. Class definition was ignored.", Name );
                }
            }


            // Rank
            if( (attr = el.Attribute( "rank" )) == null ) {
                throw new RankDefinitionException( "No rank specified for {0}. Class definition was ignored.", Name );
            }
            if( !Byte.TryParse( attr.Value, out rank ) ) {
                throw new RankDefinitionException( "Cannot parse rank for {0}. Class definition was ignored.", Name );
            }


            // Color (optional)
            if( (attr = el.Attribute( "color" )) != null ){
                if( (Color = fCraft.Color.Parse( attr.Value )) == null ) {
                    Logger.Log( "PlayerClass({0}): Could not parse class color. Assuming default (none).", LogType.Warning, Name );
                }
            } else {
                Color = fCraft.Color.Parse( attr.Value );
            }


            // Prefix (optional)
            if( (attr = el.Attribute( "prefix" )) != null ) {
                if( Rank.IsValidPrefix( attr.Value ) ) {
                    prefix = attr.Value;
                } else {
                    Logger.Log( "PlayerClass({0}): Invalid prefix format. Expecting 1 character.", LogType.Warning, Name );
                }
            }


            // AntiGrief block limit (assuming unlimited if not given)
            int value = 0;
            if( (el.Attribute( "antiGriefBlocks" ) != null) && (el.Attribute( "antiGriefSeconds" ) != null) ) {
                attr = el.Attribute( "antiGriefBlocks" );
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value >= 0 && value < 1000 ) {

                        attr = el.Attribute( "antiGriefSeconds" );
                        if( Int32.TryParse( attr.Value, out value ) ) {
                            if( value >= 0 && value < 100 ) {
                                antiGriefSeconds = value;
                                antiGriefBlocks = value;
                            } else {
                                Logger.Log( "PlayerClass({0}): Values for antiGriefSeconds in not within valid range (0-1000). Assuming default ({1}).", LogType.Warning,
                                            Name, antiGriefSeconds );
                            }
                        } else {
                            Logger.Log( "PlayerClass({0}): Could not parse the value for antiGriefSeconds. Assuming default ({1}).", LogType.Warning,
                                        Name, antiGriefSeconds );
                        }

                    } else {
                        Logger.Log( "PlayerClass({0}): Values for antiGriefBlocks in not within valid range (0-1000). Assuming default ({1}).", LogType.Warning,
                                    Name, antiGriefBlocks );
                    }
                } else {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for antiGriefBlocks. Assuming default ({1}).", LogType.Warning,
                                Name, antiGriefBlocks );
                }
            }


            if( (attr = el.Attribute( "drawLimit" )) != null ) {
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value >= 0 && value < 100000000 ) {
                        drawLimit = value;
                    } else {
                        Logger.Log( "PlayerClass({0}): Values for drawLimit in not within valid range (0-1000). Assuming default ({1}).", LogType.Warning,
                                    Name, drawLimit );
                    }
                } else {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for drawLimit. Assuming default ({1}).", LogType.Warning,
                                Name, drawLimit );
                }
            }



            if( (attr = el.Attribute( "idleKickAfter" )) != null ) {
                if( !Int32.TryParse( attr.Value, out idleKickTimer ) ) {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for idleKickAfter. Assuming 0 (never).", LogType.Warning, Name );
                    idleKickTimer = 0;
                }
            } else {
                idleKickTimer = 0;
            }

            if( (attr = el.Attribute( "reserveSlot" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out reservedSlot ) ) {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for reserveSlot. Assuming \"false\".", LogType.Warning, Name );
                    reservedSlot = false;
                }
            } else {
                reservedSlot = false;
            }


            // read permissions
            XElement temp;
            for( int i = 0; i < Enum.GetValues( typeof( Permission ) ).Length; i++ ) {
                string permission = ((Permission)i).ToString();
                if( (temp = el.Element( permission )) != null ) {
                    Permissions[i] = true;
                    switch( i ) {
                        case (int)Permission.Promote:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxPromoteVal = attr.Value;
                            } else {
                                maxPromoteVal = "";
                            }
                            break;

                        case (int)Permission.Demote:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxDemoteVal = attr.Value;
                            } else {
                                maxDemoteVal = "";
                            }
                            break;

                        case (int)Permission.Kick:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxKickVal = attr.Value;
                            } else {
                                maxKickVal = "";
                            }
                            break;

                        case (int)Permission.Ban:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxBanVal = attr.Value;
                            } else {
                                maxBanVal = "";
                            }
                            break;

                        case (int)Permission.Hide:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxHideFromVal = attr.Value;
                            } else {
                                maxHideFromVal = "";
                            }
                            break;
                    }
                }
            }

            // check consistency of ban permissions
            if( !Can( Permission.Ban ) && (Can( Permission.BanAll ) || Can( Permission.BanIP )) ) {
                Logger.Log( "PlayerClass({0}): Class is allowed to BanIP and/or BanAll but not allowed to Ban. " +
                            "Assuming that all ban permissions were ment to be off.", LogType.Warning, Name );
                Permissions[(int)Permission.BanIP] = false;
                Permissions[(int)Permission.BanAll] = false;
            }

            // check consistency of pantrol permissions
            if( !Can( Permission.Teleport ) && Can( Permission.Patrol ) ) {
                Logger.Log( "PlayerClass({0}): Class is allowed to Patrol but not allowed to Teleport. " +
                            "Assuming that Patrol permission was ment to be off.", LogType.Warning, Name );
                Permissions[(int)Permission.Patrol] = false;
            }
        }


        public XElement Serialize() {
            XElement classTag = new XElement( "Rank" );
            classTag.Add( new XAttribute( "name", Name ) );
            classTag.Add( new XAttribute( "id", ID ) );
            classTag.Add( new XAttribute( "rank", rank ) );
            classTag.Add( new XAttribute( "color", fCraft.Color.GetName( Color ) ) );
            if( prefix.Length > 0 ) classTag.Add( new XAttribute( "prefix", prefix ) );
            classTag.Add( new XAttribute( "antiGriefBlocks", antiGriefBlocks ) );
            classTag.Add( new XAttribute( "antiGriefSeconds", antiGriefSeconds ) );
            if( drawLimit > 0 ) classTag.Add( new XAttribute( "drawLimit", drawLimit ) );
            if( idleKickTimer > 0 ) classTag.Add( new XAttribute( "idleKickAfter", idleKickTimer ) );
            if( reservedSlot ) classTag.Add( new XAttribute( "reserveSlot", reservedSlot ) );
            XElement temp;
            for( int i = 0; i < Enum.GetValues( typeof( Permission ) ).Length; i++ ) {
                if( Permissions[i] ) {
                    temp = new XElement( ((Permission)i).ToString() );
                    if( i == (int)Permission.Ban && maxBan != null ) {
                        temp.Add( new XAttribute( "max", maxBan ) );
                    } else if( i == (int)Permission.Kick && maxKick != null ) {
                        temp.Add( new XAttribute( "max", maxKick ) );
                    } else if( i == (int)Permission.Promote && maxPromote != null ) {
                        temp.Add( new XAttribute( "max", maxPromote ) );
                    } else if( i == (int)Permission.Demote && maxDemote != null ) {
                        temp.Add( new XAttribute( "max", maxDemote ) );
                    } else if( i == (int)Permission.Hide && maxHideFrom != null ) {
                        temp.Add( new XAttribute( "max", maxHideFrom ) );
                    }
                    classTag.Add( temp );
                }
            }
            return classTag;
        }


        #region Permissions
        public bool Can( Permission permission ) {
            return Permissions[(int)permission];
        }


        public bool CanKick( Rank other ) {
            return maxKick.rank >= other.rank;
        }

        public bool CanBan( Rank other ) {
            return maxBan.rank >= other.rank;
        }

        public bool CanPromote( Rank other ) {
            return maxPromote.rank >= other.rank;
        }

        public bool CanDemote( Rank other ) {
            return maxDemote.rank >= other.rank;
        }

        public bool CanSee( Rank other ) {
            return rank > other.maxHideFrom.rank;
        }


        public int GetMaxKickIndex() {
            if( maxKick == null ) return 0;
            else return maxKick.index + 1;
        }

        public int GetMaxBanIndex() {
            if( maxBan == null ) return 0;
            else return maxBan.index + 1;
        }

        public int GetMaxPromoteIndex() {
            if( maxPromote == null ) return 0;
            else return maxPromote.index + 1;
        }

        public int GetMaxDemoteIndex() {
            if( maxDemote == null ) return 0;
            else return maxDemote.index + 1;
        }

        public int GetMaxHideFromIndex() {
            if( maxHideFrom == null ) return 0;
            else return maxHideFrom.index + 1;
        }

        #endregion


        #region Validation

        public static bool IsValidRankName( string rankName ) {
            if( rankName.Length < 1 || rankName.Length > 16 ) return false;
            for( int i = 0; i < rankName.Length; i++ ) {
                char ch = rankName[i];
                if( ch < '0' || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsValidID( string ID ) {
            if( ID.Length != 16 ) return false;
            for( int i = 0; i < ID.Length; i++ ) {
                char ch = ID[i];
                if( ch < '0' || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsValidPrefix( string val ) {
            if( val.Length == 0 ) return true;
            if( val.Length > 1 ) return false;
            return val[0] > ' ' && val[0] != '&' && val[0] != '`' && val[0] != '^' && val[0] <= '}';
        }

        #endregion


        public string ToComboBoxOption() {
            return String.Format( "{0,3} {1,1}{2}", rank, prefix, Name );
        }

        public override string ToString() {
            return Name + "#" + ID;
        }

        public string GetClassyName() {
            string displayedName = Name;
            if( Config.GetBool( ConfigKey.RankPrefixesInChat ) ) {
                displayedName = prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.RankColorsInChat ) ) {
                displayedName = Color + displayedName;
            }
            return displayedName;
        }
    }
}