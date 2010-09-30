// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;


namespace fCraft {
    public sealed class Rank {

        public static bool operator >( Rank a, Rank b ) {
            return a.Index > b.Index;
        }

        public static bool operator <( Rank a, Rank b ) {
            return a.Index < b.Index;
        }

        public static bool operator >=( Rank a, Rank b ) {
            return a.Index >= b.Index;
        }

        public static bool operator <=( Rank a, Rank b ) {
            return a.Index <= b.Index;
        }


        public sealed class RankDefinitionException : Exception {
            public RankDefinitionException( string message ) : base( message ) { }
            public RankDefinitionException( string message, params string[] args ) :
                base( String.Format( message, args ) ) { }
        }


        public string Name { get; set; }

        public byte legacyNumericRank;

        public string Color { get; set; }

        public string ID { get; set; }

        public bool[] Permissions {
            get;
            private set;
        }


        #region Permission Limits

        Rank _maxPromote;
        public Rank maxPromote {
            get {
                if( _maxPromote == null ) return this;
                else return _maxPromote;
            }
            set {
                _maxPromote = value;
            }
        }

        Rank _maxDemote;
        public Rank maxDemote {
            get {
                if( _maxDemote == null ) return this;
                else return _maxDemote;
            }
            set {
                _maxDemote = value;
            }
        }

        Rank _maxKick;
        public Rank maxKick {
            get {
                if( _maxKick == null ) return this;
                else return _maxKick;
            }
            set {
                _maxKick = value;
            }
        }

        Rank _maxBan;
        public Rank maxBan {
            get {
                if( _maxBan == null ) return this;
                else return _maxBan;
            }
            set {
                _maxBan = value;
            }
        }

        Rank _maxHideFrom;
        public Rank maxHideFrom {
            get {
                if( _maxHideFrom == null ) return this;
                else return _maxHideFrom;
            }
            set {
                _maxHideFrom = value;
            }
        }

        #endregion


        public string Prefix = "";
        public int IdleKickTimer,
                   DrawLimit,
                   AntiGriefBlocks = 35,
                   AntiGriefSeconds = 5;
        public bool ReservedSlot;
        public int Index;

        public Rank NextRankUp, NextRankDown;

        // these need to be parsed after all ranks are added
        string maxPromoteVal = "",
               maxDemoteVal = "",
               maxKickVal = "",
               maxBanVal = "",
               maxHideFromVal = "";


        public Rank() {
            Permissions = new bool[Enum.GetValues( typeof( Permission ) ).Length];
        }


        public Rank( XElement el )
            : this() {

            // Name
            XAttribute attr = el.Attribute( "name" );
            if( attr == null ) {
                throw new RankDefinitionException( "Class definition with no name was ignored." );
            }
            if( !Rank.IsValidRankName( attr.Value.Trim() ) ) {
                throw new RankDefinitionException( "Invalid name specified for class \"{0}\". Class names can only contain letters, digits, and underscores. Class definition was ignored.", Name );
            }
            Name = attr.Value.Trim();

            if( RankList.RanksByName.ContainsKey( Name.ToLower() ) ) {
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
                if( RankList.RanksByID.ContainsKey( Name ) ) {
                    throw new RankDefinitionException( "Duplicate ID for {0}. Class definition was ignored.", Name );
                }
            }


            // Rank
            if( (attr = el.Attribute( "rank" )) == null ) {
                throw new RankDefinitionException( "No rank specified for {0}. Class definition was ignored.", Name );
            }
            if( !Byte.TryParse( attr.Value, out legacyNumericRank ) ) {
                throw new RankDefinitionException( "Cannot parse rank for {0}. Class definition was ignored.", Name );
            }


            // Color (optional)
            if( (attr = el.Attribute( "color" )) != null ) {
                if( (Color = fCraft.Color.Parse( attr.Value )) == null ) {
                    Logger.Log( "PlayerClass({0}): Could not parse class color. Assuming default (none).", LogType.Warning, Name );
                }
            } else {
                Color = fCraft.Color.Parse( attr.Value );
            }


            // Prefix (optional)
            if( (attr = el.Attribute( "prefix" )) != null ) {
                if( Rank.IsValidPrefix( attr.Value ) ) {
                    Prefix = attr.Value;
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
                                AntiGriefSeconds = value;
                                AntiGriefBlocks = value;
                            } else {
                                Logger.Log( "PlayerClass({0}): Values for antiGriefSeconds in not within valid range (0-1000). Assuming default ({1}).", LogType.Warning,
                                            Name, AntiGriefSeconds );
                            }
                        } else {
                            Logger.Log( "PlayerClass({0}): Could not parse the value for antiGriefSeconds. Assuming default ({1}).", LogType.Warning,
                                        Name, AntiGriefSeconds );
                        }

                    } else {
                        Logger.Log( "PlayerClass({0}): Values for antiGriefBlocks in not within valid range (0-1000). Assuming default ({1}).", LogType.Warning,
                                    Name, AntiGriefBlocks );
                    }
                } else {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for antiGriefBlocks. Assuming default ({1}).", LogType.Warning,
                                Name, AntiGriefBlocks );
                }
            }


            if( (attr = el.Attribute( "drawLimit" )) != null ) {
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value >= 0 && value < 100000000 ) {
                        DrawLimit = value;
                    } else {
                        Logger.Log( "PlayerClass({0}): Values for drawLimit in not within valid range (0-1000). Assuming default ({1}).", LogType.Warning,
                                    Name, DrawLimit );
                    }
                } else {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for drawLimit. Assuming default ({1}).", LogType.Warning,
                                Name, DrawLimit );
                }
            }



            if( (attr = el.Attribute( "idleKickAfter" )) != null ) {
                if( !Int32.TryParse( attr.Value, out IdleKickTimer ) ) {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for idleKickAfter. Assuming 0 (never).", LogType.Warning, Name );
                    IdleKickTimer = 0;
                }
            } else {
                IdleKickTimer = 0;
            }

            if( (attr = el.Attribute( "reserveSlot" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out ReservedSlot ) ) {
                    Logger.Log( "PlayerClass({0}): Could not parse the value for reserveSlot. Assuming \"false\".", LogType.Warning, Name );
                    ReservedSlot = false;
                }
            } else {
                ReservedSlot = false;
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
                            }
                            break;

                        case (int)Permission.Demote:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxDemoteVal = attr.Value;
                            }
                            break;

                        case (int)Permission.Kick:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxKickVal = attr.Value;
                            }
                            break;

                        case (int)Permission.Ban:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxBanVal = attr.Value;
                            }
                            break;

                        case (int)Permission.Hide:
                            if( (attr = temp.Attribute( "max" )) != null ) {
                                maxHideFromVal = attr.Value;
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
            classTag.Add( new XAttribute( "rank", legacyNumericRank ) );
            classTag.Add( new XAttribute( "color", fCraft.Color.GetName( Color ) ) );
            if( Prefix.Length > 0 ) classTag.Add( new XAttribute( "prefix", Prefix ) );
            classTag.Add( new XAttribute( "antiGriefBlocks", AntiGriefBlocks ) );
            classTag.Add( new XAttribute( "antiGriefSeconds", AntiGriefSeconds ) );
            if( DrawLimit > 0 ) classTag.Add( new XAttribute( "drawLimit", DrawLimit ) );
            if( IdleKickTimer > 0 ) classTag.Add( new XAttribute( "idleKickAfter", IdleKickTimer ) );
            if( ReservedSlot ) classTag.Add( new XAttribute( "reserveSlot", ReservedSlot ) );

            XElement temp;
            for( int i = 0; i < Enum.GetValues( typeof( Permission ) ).Length; i++ ) {
                if( Permissions[i] ) {
                    temp = new XElement( ((Permission)i).ToString() );

                    switch( i ) {
                        case (int)Permission.Kick:
                            if( _maxKick != null ) temp.Add( new XAttribute( "max", maxKick ) );
                            break;

                        case (int)Permission.Ban:
                            if( _maxBan != null ) temp.Add( new XAttribute( "max", maxBan ) );
                            break;

                        case (int)Permission.Promote:
                            if( _maxPromote != null ) temp.Add( new XAttribute( "max", maxPromote ) );
                            break;

                        case (int)Permission.Demote:
                            if( _maxDemote != null ) temp.Add( new XAttribute( "max", maxDemote ) );
                            break;

                        case (int)Permission.Hide:
                            if( _maxHideFrom != null ) temp.Add( new XAttribute( "max", maxHideFrom ) );
                            break;
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
            return maxKick >= other;
        }

        public bool CanBan( Rank other ) {
            return maxBan >= other;
        }

        public bool CanPromote( Rank other ) {
            return maxPromote >= other;
        }

        public bool CanDemote( Rank other ) {
            return maxDemote >= other;
        }

        public bool CanSee( Rank other ) {
            return this > other.maxHideFrom;
        }


        public int GetMaxKickIndex() {
            if( maxKick == null ) return 0;
            else return maxKick.Index + 1;
        }

        public int GetMaxBanIndex() {
            if( maxBan == null ) return 0;
            else return maxBan.Index + 1;
        }

        public int GetMaxPromoteIndex() {
            if( maxPromote == null ) return 0;
            else return maxPromote.Index + 1;
        }

        public int GetMaxDemoteIndex() {
            if( maxDemote == null ) return 0;
            else return maxDemote.Index + 1;
        }

        public int GetMaxHideFromIndex() {
            if( maxHideFrom == null ) return 0;
            else return maxHideFrom.Index + 1;
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
            return String.Format( "{0,3} {1,1}{2}", legacyNumericRank, Prefix, Name );
        }

        public override string ToString() {
            return Name + "#" + ID;
        }

        public string GetClassyName() {
            string displayedName = Name;
            if( Config.GetBool( ConfigKey.RankPrefixesInChat ) ) {
                displayedName = Prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.RankColorsInChat ) ) {
                displayedName = Color + displayedName;
            }
            return displayedName;
        }

        internal bool ParsePermissionLimits() {
            bool ok = true;
            if( maxKickVal.Length > 0 ) {
                maxKick = RankList.ParseRank( maxKickVal );
                ok &= (maxKick != null);
            }

            if( maxBanVal.Length > 0 ) {
                maxBan = RankList.ParseRank( maxBanVal );
                ok &= (maxBan != null);
            }

            if( maxPromoteVal.Length > 0 ) {
                maxPromote = RankList.ParseRank( maxPromoteVal );
                ok &= (maxPromote != null);
            }

            if( maxDemoteVal.Length > 0 ) {
                maxDemote = RankList.ParseRank( maxDemoteVal );
                ok &= (maxDemote != null);
            }

            if( maxHideFromVal.Length > 0 ) {
                maxHideFrom = RankList.ParseRank( maxHideFromVal );
                ok &= (maxHideFrom != null);
            }
            return ok;
        }
    }
}