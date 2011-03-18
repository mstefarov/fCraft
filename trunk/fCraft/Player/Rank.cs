// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml.Linq;

namespace fCraft {
    /// <summary>
    /// Interface that provides a method for printing an object's name beautified with Minecraft color codes.
    /// It was "classy" in a sense that it was colored based on "class" (rank) of a player/world/zone.
    /// </summary>
    public interface IClassy {
        string GetClassyName();
    }

    public sealed class Rank : IClassy {

        public string Name { get; set; }

        public byte legacyNumericRank;

        public string Color { get; set; }

        public string ID { get; set; }

        public bool[] Permissions {
            get;
            private set;
        }

        public bool AllowSecurityCircumvention;

        public string Prefix = "";
        public int IdleKickTimer,
                   DrawLimit,
                   AntiGriefBlocks,
                   AntiGriefSeconds;
        public bool ReservedSlot;
        public int Index;

        public Rank NextRankUp, NextRankDown;


        public Rank() {
            Permissions = new bool[Enum.GetValues( typeof( Permission ) ).Length];
            PermissionLimits = new Rank[Permissions.Length];
            PermissionLimitStrings = new string[Permissions.Length];
            Color = "";
        }

        public Rank( XElement el )
            : this() {

            // Name
            XAttribute attr = el.Attribute( "name" );
            if( attr == null ) {
                throw new RankDefinitionException( "Rank definition with no name was ignored." );

            } else if( !IsValidRankName( attr.Value.Trim() ) ) {
                throw new RankDefinitionException( "Invalid name specified for rank \"{0}\". "+
                                                   "Rank names can only contain letters, digits, and underscores. " +
                                                   "Rank definition was ignored.", Name );

            } else {
                // duplicate Name check is done in RankList.AddRank()
                Name = attr.Value.Trim();
            }


            // ID
            attr = el.Attribute( "id" );
            if( attr == null ) {
                Logger.Log( "Rank({0}): Issued a new unique ID.", LogType.Warning, Name );
                ID = RankList.GenerateID();

            } else if( !IsValidID( attr.Value.Trim() ) ) {
                throw new RankDefinitionException( "Invalid ID specified for rank \"{0}\". "+
                                                   "ID must be alphanumeric, and exactly 16 characters long. "+
                                                   "Rank definition was ignored.", Name );

            } else {
                ID = attr.Value.Trim();
                // duplicate ID check is done in RankList.AddRank()
            }


            // Rank
            if( (attr = el.Attribute( "rank" )) != null  ) {
                Byte.TryParse( attr.Value, out legacyNumericRank );
            }


            // Color (optional)
            if( (attr = el.Attribute( "color" )) != null ) {
                if( (Color = fCraft.Color.Parse( attr.Value )) == null ) {
                    Logger.Log( "Rank({0}): Could not parse rank color. Assuming default (none).", LogType.Warning, Name );
                    Color = "";
                }
            } else {
                Color = "";
            }


            // Prefix (optional)
            if( (attr = el.Attribute( "prefix" )) != null ) {
                if( IsValidPrefix( attr.Value ) ) {
                    Prefix = attr.Value;
                } else {
                    Logger.Log( "Rank({0}): Invalid prefix format. Expecting 1 character.", LogType.Warning, Name );
                }
            }


            // AntiGrief block limit (assuming unlimited if not given)
            int value;
            if( (el.Attribute( "antiGriefBlocks" ) != null) && (el.Attribute( "antiGriefSeconds" ) != null) ) {
                attr = el.Attribute( "antiGriefBlocks" );
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value >= 0 && value < 1000 ) {
                        AntiGriefBlocks = value;

                    } else {
                        Logger.Log( "Rank({0}): Value for antiGriefBlocks is not within valid range (0-1000). Assuming default ({1}).", LogType.Warning,
                                    Name, AntiGriefBlocks );
                    }
                } else {
                    Logger.Log( "Rank({0}): Could not parse the value for antiGriefBlocks. Assuming default ({1}).", LogType.Warning,
                                Name, AntiGriefBlocks );
                }

                attr = el.Attribute( "antiGriefSeconds" );
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value >= 0 && value < 100 ) {
                        AntiGriefSeconds = value;
                    } else {
                        Logger.Log( "Rank({0}): Value for antiGriefSeconds is not within valid range (0-100). Assuming default ({1}).", LogType.Warning,
                                    Name, AntiGriefSeconds );
                    }
                } else {
                    Logger.Log( "Rank({0}): Could not parse the value for antiGriefSeconds. Assuming default ({1}).", LogType.Warning,
                                Name, AntiGriefSeconds );
                }
            }


            // Draw command limit, in number-of-blocks (assuming unlimited if not given)
            if( (attr = el.Attribute( "drawLimit" )) != null ) {
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value >= 0 && value < 100000000 ) {
                        DrawLimit = value;
                    } else {
                        Logger.Log( "Rank({0}): Value for drawLimit is not within valid range (0-100000000). Assuming default ({1}).", LogType.Warning,
                                    Name, DrawLimit );
                    }
                } else {
                    Logger.Log( "Rank({0}): Could not parse the value for drawLimit. Assuming default ({1}).", LogType.Warning,
                                Name, DrawLimit );
                }
            }


            // Idle kick timer, in minutes. (assuming 'never' if not given)
            if( (attr = el.Attribute( "idleKickAfter" )) != null ) {
                if( !Int32.TryParse( attr.Value, out IdleKickTimer ) ) {
                    Logger.Log( "Rank({0}): Could not parse the value for idleKickAfter. Assuming 0 (never).", LogType.Warning, Name );
                    IdleKickTimer = 0;
                }
            } else {
                IdleKickTimer = 0;
            }


            // Reserved slot. (assuming 'no' if not given)
            if( (attr = el.Attribute( "reserveSlot" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out ReservedSlot ) ) {
                    Logger.Log( "Rank({0}): Could not parse value for reserveSlot. Assuming \"false\".", LogType.Warning, Name );
                    ReservedSlot = false;
                }
            } else {
                ReservedSlot = false;
            }


            // Security circumvention. (assuming 'no' if not given)
            if( (attr = el.Attribute( "allowSecurityCircumvention" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out AllowSecurityCircumvention ) ) {
                    Logger.Log( "Rank({0}): Could not parse the value for allowSecurityCircumvention. Assuming \"false\".", LogType.Warning, Name );
                    AllowSecurityCircumvention = false;
                }
            } else {
                AllowSecurityCircumvention = false;
            }


            // Permissions
            XElement temp;
            for( int i = 0; i < Enum.GetValues( typeof( Permission ) ).Length; i++ ) {
                string permission = ((Permission)i).ToString();
                if( (temp = el.Element( permission )) != null ) {
                    Permissions[i] = true;
                    if ((attr = temp.Attribute( "max" )) != null){
                        PermissionLimitStrings[i] = attr.Value;
                    }
                }
            }

            // check consistency of ban permissions
            if( !Can( Permission.Ban ) && (Can( Permission.BanAll ) || Can( Permission.BanIP )) ) {
                Logger.Log( "Rank({0}): Rank is allowed to BanIP and/or BanAll but not allowed to Ban. " +
                            "Assuming that all ban permissions were meant to be off.", LogType.Warning, Name );
                Permissions[(int)Permission.BanIP] = false;
                Permissions[(int)Permission.BanAll] = false;
            }

            // check consistency of pantrol permissions
            if( !Can( Permission.Teleport ) && Can( Permission.Patrol ) ) {
                Logger.Log( "Rank({0}): Rank is allowed to Patrol but not allowed to Teleport. " +
                            "Assuming that Patrol permission was meant to be off.", LogType.Warning, Name );
                Permissions[(int)Permission.Patrol] = false;
            }
        }


        public XElement Serialize() {
            XElement rankTag = new XElement( "Rank" );
            rankTag.Add( new XAttribute( "name", Name ) );
            rankTag.Add( new XAttribute( "id", ID ) );
            rankTag.Add( new XAttribute( "color", fCraft.Color.GetName( Color ) ) );
            if( Prefix.Length > 0 ) rankTag.Add( new XAttribute( "prefix", Prefix ) );
            rankTag.Add( new XAttribute( "antiGriefBlocks", AntiGriefBlocks ) );
            rankTag.Add( new XAttribute( "antiGriefSeconds", AntiGriefSeconds ) );
            if( DrawLimit > 0 ) rankTag.Add( new XAttribute( "drawLimit", DrawLimit ) );
            if( IdleKickTimer > 0 ) rankTag.Add( new XAttribute( "idleKickAfter", IdleKickTimer ) );
            if( ReservedSlot ) rankTag.Add( new XAttribute( "reserveSlot", ReservedSlot ) );
            if( AllowSecurityCircumvention ) rankTag.Add( new XAttribute( "allowSecurityCircumvention", AllowSecurityCircumvention ) );

            XElement temp;
            for( int i = 0; i < Enum.GetValues( typeof( Permission ) ).Length; i++ ) {
                if( Permissions[i] ) {
                    temp = new XElement( ((Permission)i).ToString() );

                    if( PermissionLimits[i] != null ) {
                        temp.Add( new XAttribute( "max", GetLimit((Permission)i) ) );
                    }
                    rankTag.Add( temp );
                }
            }
            return rankTag;
        }


        #region Rank Comparison Operators

        // Somewhat counterintuitive, but lower index number = higher up on the list = higher rank

        public static bool operator >( Rank a, Rank b ) {
            return a.Index < b.Index;
        }

        public static bool operator <( Rank a, Rank b ) {
            return a.Index > b.Index;
        }

        public static bool operator >=( Rank a, Rank b ) {
            return a.Index <= b.Index;
        }

        public static bool operator <=( Rank a, Rank b ) {
            return a.Index >= b.Index;
        }
        #endregion

        #region Permissions
        public bool Can( Permission permission ) {
            return Permissions[(int)permission];
        }


        public bool CanKick( Rank other ) {
            return GetLimit(Permission.Kick) >= other;
        }

        public bool CanBan( Rank other ) {
            return GetLimit( Permission.Ban ) >= other;
        }

        public bool CanPromote( Rank other ) {
            return GetLimit( Permission.Promote ) >= other;
        }

        public bool CanDemote( Rank other ) {
            return GetLimit( Permission.Demote ) >= other;
        }

        public bool CanSee( Rank other ) {
            return this > other.GetLimit( Permission.Hide );
        }

        public bool CanFreeze( Rank other ) {
            return GetLimit( Permission.Freeze ) >= other;
        }

        public bool CanMute( Rank other ) {
            return GetLimit( Permission.Mute ) >= other;
        }

        #endregion

        #region Permission Limits

        public Rank[] PermissionLimits {
            get;
            private set;
        }
        public string[] PermissionLimitStrings;

        public Rank GetLimit( Permission permission ) {
            return PermissionLimits[(int)permission] ?? this;
        }


        public void SetLimit( Permission permission, Rank limit ) {
            PermissionLimits[(int)permission] = limit;
        }

        public void ResetLimit( Permission permission ) {
            SetLimit( permission, null );
        }

        public bool IsLimitDefault( Permission permission ) {
            return (PermissionLimits[(int)permission] == null);
        }

        public int GetLimitIndex( Permission permission ) {
            if( PermissionLimits[(int)permission] == null ) {
                return 0;
            } else {
                return PermissionLimits[(int)permission].Index + 1;
            }
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
            return String.Format( "{0,1}{1}", Prefix, Name );
        }

        public override string ToString() {
            return Name + "#" + ID;
        }

        public string GetClassyName() {
            string displayedName = Name;
            if( ConfigKey.RankPrefixesInChat.GetBool() ) {
                displayedName = Prefix + displayedName;
            }
            if( ConfigKey.RankColorsInChat.GetBool() ) {
                displayedName = Color + displayedName;
            }
            return displayedName;
        }

        internal bool ParsePermissionLimits() {
            bool ok = true;
            for( int i = 0; i < PermissionLimits.Length; i++ ) {
                if( PermissionLimitStrings[i] == null ) continue;
                SetLimit( (Permission)i, RankList.ParseRank( PermissionLimitStrings[i] ) );
                ok &= (GetLimit((Permission)i) != null);
            }
            return ok;
        }
    }


    public sealed class RankDefinitionException : Exception {
        public RankDefinitionException( string message ) : base( message ) { }
        public RankDefinitionException( string message, params string[] args ) :
            base( String.Format( message, args ) ) { }
    }
}