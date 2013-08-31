﻿// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Rank of a player. Ranks are arranged in a linear hierarchy.
    /// One instance of a Rank object should be shared by players of the same rank.
    /// Rank objects are comparable - you can use comparison operators or CompareTo. </summary>
    public sealed class Rank : IClassy, IComparable<Rank> {
        /// <summary> Rank color code. Should not be left blank. </summary>
        [NotNull]
        public string Color { get; set; }

        /// <summary> String that prefixes the username of all members of this rank. </summary>
        [NotNull]
        public string Prefix { get; set; }

        /// <summary> Rank's displayed name.
        /// Use rank.FullName instead for serializing (to improve backwards compatibility). </summary>
        [NotNull]
        public string Name { get; internal set; }

        /// <summary> Unique rank ID. Generated by Rank.GenerateID. Assigned once at creation.
        /// Used to preserve compatibility in case a rank gets renamed. </summary>
        [NotNull]
        public string ID { get; private set; }

        /// <summary> Set of permissions given to this rank. Use Rank.Can() to access. </summary>
        [NotNull]
        public bool[] Permissions { get; private set; }

        /// <summary> Whether players of this rank are allowed to remove restrictions that affect themselves.
        /// Affects /WMain, /WAccess, /WBuild, /ZAdd, /ZEdit, and /ZRemove. </summary>
        public bool AllowSecurityCircumvention;

        /// <summary> Maximum number of buffered copies this rank is allowed to have. </summary>
        public int CopySlots = 2;

        /// <summary> Maximum number of blocks away the origin that a fill is allowed to travel.
        /// Applies to /Fill2D command. For example, a limit of 32, means that the maximum fill
        /// dimensions are (32 * 2 + 1), which is 65 x 65 </summary>
        public int FillLimit = 32;

        /// <summary> Maximum number of blocks that player is allowed to draw at a time using draw commands. 
        /// Value of 0 means "unlimited". </summary>
        public int DrawLimit;

        /// <summary> Time until the idle kicker will kick this Rank from the server. </summary>
        public int IdleKickTimer;

        /// <summary> Number of blocks that need to be modified in AntiGriefSeconds for the AntiGrief to kick in for this Rank. </summary>
        public int AntiGriefBlocks;

        /// <summary> The interval in seconds for which to count number of blocks broken for use in AntiGrief for this Rank. </summary>
        public int AntiGriefSeconds;

        /// <summary> Whether this rank has a reserved slot (is allowed to join the server even if it's full). </summary>
        public bool HasReservedSlot;

        /// <summary> Rank's relative index on the hierarchy. Index of the top rank is always 0.
        /// Subordinate ranks start at 1. Higher index = lower rank. </summary>
        public int Index;


        /// <summary> The Rank immediately above this Rank. Null if there is no higher rank. Set by RankManager. </summary>
        [CanBeNull]
        public Rank NextRankUp { get; internal set; }

        /// <summary> The Rank immediately below this Rank. Null if there is no lower rank. Set by RankManager. </summary>
        [CanBeNull]
        public Rank NextRankDown { get; internal set; }

        /// <summary> The main world for this Rank. Set and saved by WorldManager. </summary>
        [CanBeNull]
        public World MainWorld { get; set; }


        #region Constructors

        static readonly string[] PermissionNames = Enum.GetNames( typeof( Permission ) );
        Rank() {
            Permissions = new bool[PermissionNames.Length];
            PermissionLimits = new Rank[PermissionNames.Length];
            permissionLimitStrings = new string[PermissionNames.Length];
            Color = fCraft.Color.White;
            Prefix = "";
        }


        /// <summary> Sets the name and ID of this Rank. </summary>
        /// <param name="name"> Name to assign to this Rank. </param>
        /// <param name="id"> Unique ID to assign to this Rank. Use RankManager.GenerateID to generate. </param>
        /// <exception cref="ArgumentNullException"> name or id is null. </exception>
        public Rank( [NotNull] string name, [NotNull] string id )
            : this() {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( id == null ) throw new ArgumentNullException( "id" );
            Name = name;
            ID = id;
            FullName = Name + "#" + ID;
        }


        public Rank( [NotNull] XElement el )
            : this() {
            if( el == null ) throw new ArgumentNullException( "el" );

            // Name
            XAttribute attr = el.Attribute( "name" );
            if( attr == null ) {
                throw new RankDefinitionException( null, "Rank definition with no name was ignored." );

            } else if( !IsValidRankName( attr.Value.Trim() ) ) {
                throw new RankDefinitionException( Name,
                                                   "Invalid name specified for rank \"{0}\". " +
                                                   "Rank names can only contain letters, digits, and underscores. " +
                                                   "Rank definition was ignored.",
                                                   Name );

            } else {
                // duplicate Name check is done in RankManager.AddRank()
                Name = attr.Value.Trim();
            }


            // ID
            attr = el.Attribute( "id" );
            if( attr == null ) {
                ID = RankManager.GenerateID();
                Logger.Log( LogType.Warning,
                            "Rank({0}): No ID specified; issued a new unique ID: {1}",
                            Name,
                            ID );

            } else if( !IsValidID( attr.Value.Trim() ) ) {
                ID = RankManager.GenerateID();
                Logger.Log( LogType.Warning,
                            "Rank({0}): Invalid ID specified (must be alphanumeric, and exactly 16 characters long); issued a new unique ID: {1}",
                            Name,
                            ID );

            } else {
                ID = attr.Value.Trim();
                // duplicate ID check is done in RankManager.AddRank()
            }

            FullName = Name + "#" + ID;


            // Color (optional)
            if( (attr = el.Attribute( "color" )) != null ) {
                string color = fCraft.Color.Parse( attr.Value );
                if( color == null ) {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse rank color. Assuming default (none).",
                                Name );
                    Color = fCraft.Color.White;
                } else {
                    Color = color;
                }
            } else {
                Color = fCraft.Color.White;
            }


            // Prefix (optional)
            if( (attr = el.Attribute( "prefix" )) != null ) {
                if( IsValidPrefix( attr.Value ) ) {
                    Prefix = attr.Value;
                } else {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Invalid prefix format. Expecting 1 character.",
                                Name );
                }
            }


            // AntiGrief block limit (assuming unlimited if not given)
            int value;
            XAttribute agBlocks = el.Attribute( "antiGriefBlocks" );
            XAttribute agSeconds = el.Attribute( "antiGriefSeconds" );
            if( agBlocks != null && agSeconds != null ) {
                if( Int32.TryParse( agBlocks.Value, out value ) ) {
                    if( value >= 0 && value < 1000 ) {
                        AntiGriefBlocks = value;

                    } else {
                        Logger.Log( LogType.Warning,
                                    "Rank({0}): Value for antiGriefBlocks is not within valid range (0-1000). Assuming default ({1}).",
                                    Name,
                                    AntiGriefBlocks );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse the value for antiGriefBlocks. Assuming default ({1}).",
                                Name,
                                AntiGriefBlocks );
                }

                if( Int32.TryParse( agSeconds.Value, out value ) ) {
                    if( value >= 0 && value < 100 ) {
                        AntiGriefSeconds = value;
                    } else {
                        Logger.Log( LogType.Warning,
                                    "Rank({0}): Value for antiGriefSeconds is not within valid range (0-100). Assuming default ({1}).",
                                    Name,
                                    AntiGriefSeconds );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse the value for antiGriefSeconds. Assuming default ({1}).",
                                Name,
                                AntiGriefSeconds );
                }
            }


            // Draw command limit, in number-of-blocks (assuming unlimited if not given)
            if( (attr = el.Attribute( "drawLimit" )) != null ) {
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value >= 0 && value < 100000000 ) {
                        DrawLimit = value;
                    } else {
                        Logger.Log( LogType.Warning,
                                    "Rank({0}): Value for drawLimit is not within valid range (0-100000000). Assuming default ({1}).",
                                    Name,
                                    DrawLimit );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse the value for drawLimit. Assuming default ({1}).",
                                Name,
                                DrawLimit );
                }
            }


            // Idle kick timer, in minutes. (assuming 'never' if not given)
            if( (attr = el.Attribute( "idleKickAfter" )) != null ) {
                if( !Int32.TryParse( attr.Value, out IdleKickTimer ) ) {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse the value for idleKickAfter. Assuming 0 (never).",
                                Name );
                    IdleKickTimer = 0;
                }
            } else {
                IdleKickTimer = 0;
            }


            // Reserved slot. (assuming 'no' if not given)
            if( (attr = el.Attribute( "reserveSlot" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out HasReservedSlot ) ) {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse value for reserveSlot. Assuming \"false\".",
                                Name );
                    HasReservedSlot = false;
                }
            } else {
                HasReservedSlot = false;
            }


            // Security circumvention. (assuming 'no' if not given)
            if( (attr = el.Attribute( "allowSecurityCircumvention" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out AllowSecurityCircumvention ) ) {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse the value for allowSecurityCircumvention. Assuming \"false\".",
                                Name );
                    AllowSecurityCircumvention = false;
                }
            } else {
                AllowSecurityCircumvention = false;
            }


            // Copy slots (assuming default 2 if not given)
            if( (attr = el.Attribute( "copySlots" )) != null ) {
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value > 0 && value < 256 ) {
                        CopySlots = value;
                    } else {
                        Logger.Log( LogType.Warning,
                                    "Rank({0}): Value for copySlots is not within valid range (1-255). Assuming default ({1}).",
                                    Name,
                                    CopySlots );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse the value for copySlots. Assuming default ({1}).",
                                Name,
                                CopySlots );
                }
            }

            // Fill limit (assuming default 32 if not given)
            if( (attr = el.Attribute( "fillLimit" )) != null ) {
                if( Int32.TryParse( attr.Value, out value ) ) {
                    if( value < 1 ) {
                        Logger.Log( LogType.Warning,
                                    "Rank({0}): Value for fillLimit may not be negative. Assuming default ({1}).",
                                    Name,
                                    FillLimit );
                    } else if( value > 2048 ) {
                        FillLimit = 2048;
                    } else {
                        FillLimit = value;
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Rank({0}): Could not parse the value for fillLimit. Assuming default ({1}).",
                                Name,
                                FillLimit );
                }
            }

            // Permissions
            for( int i = 0; i < PermissionNames.Length; i++ ) {
                string permission = PermissionNames[i];
                XElement temp;
                if( (temp = el.Element( permission )) != null ) {
                    Permissions[i] = true;
                    if( (attr = temp.Attribute( "max" )) != null ) {
                        permissionLimitStrings[i] = attr.Value;
                    }
                }
            }

            // check consistency of ban permissions
            if( !Can( Permission.Ban ) && (Can( Permission.BanAll ) || Can( Permission.BanIP )) ) {
                Logger.Log( LogType.Warning,
                            "Rank({0}): Rank is allowed to BanIP and/or BanAll but not allowed to Ban. " +
                            "Assuming that all ban permissions were meant to be off.",
                            Name );
                Permissions[(int)Permission.BanIP] = false;
                Permissions[(int)Permission.BanAll] = false;
            }

            // check consistency of patrol permissions
            if( !Can( Permission.Teleport ) && Can( Permission.Patrol ) ) {
                Logger.Log( LogType.Warning,
                            "Rank({0}): Rank is allowed to Patrol but not allowed to Teleport. " +
                            "Assuming that Patrol permission was meant to be off.",
                            Name );
                Permissions[(int)Permission.Patrol] = false;
            }

            // check consistency of draw permissions
            if( !Can( Permission.Draw ) && Can( Permission.DrawAdvanced ) ) {
                Logger.Log( LogType.Warning,
                            "Rank({0}): Rank is allowed to DrawAdvanced but not allowed to Draw. " +
                            "Assuming that DrawAdvanced permission was meant to be off.",
                            Name );
                Permissions[(int)Permission.DrawAdvanced] = false;
            }

            // check consistency of Undo permissions
            if( !Can( Permission.UndoOthersActions ) && Can( Permission.UndoAll ) ) {
                Logger.Log( LogType.Warning,
                            "Rank({0}): Rank is allowed to UndoAll but not allowed to UndoOthersActions. " +
                            "Assuming that UndoAll permission was meant to be off.",
                            Name );
                Permissions[(int)Permission.UndoAll] = false;
            }
        }

        #endregion


        public XElement Serialize() {
            XElement rankTag = new XElement( "Rank" );
            rankTag.Add( new XAttribute( "name", Name ) );
            rankTag.Add( new XAttribute( "id", ID ) );
            string colorName = fCraft.Color.GetName( Color );
            if( colorName != null ) {
                rankTag.Add( new XAttribute( "color", colorName ) );
            }
            if( Prefix.Length > 0 ) rankTag.Add( new XAttribute( "prefix", Prefix ) );
            rankTag.Add( new XAttribute( "antiGriefBlocks", AntiGriefBlocks ) );
            rankTag.Add( new XAttribute( "antiGriefSeconds", AntiGriefSeconds ) );
            if( DrawLimit > 0 ) rankTag.Add( new XAttribute( "drawLimit", DrawLimit ) );
            if( IdleKickTimer > 0 ) rankTag.Add( new XAttribute( "idleKickAfter", IdleKickTimer ) );
            if( HasReservedSlot ) rankTag.Add( new XAttribute( "reserveSlot", HasReservedSlot ) );
            if( AllowSecurityCircumvention )
                rankTag.Add( new XAttribute( "allowSecurityCircumvention", AllowSecurityCircumvention ) );
            rankTag.Add( new XAttribute( "copySlots", CopySlots ) );
            rankTag.Add( new XAttribute( "fillLimit", FillLimit ) );

            for( int i = 0; i < PermissionNames.Length; i++ ) {
                if( Permissions[i] ) {
                    XElement temp = new XElement( PermissionNames[i] );

                    if( PermissionLimits[i] != null ) {
                        temp.Add( new XAttribute( "max", GetLimit( (Permission)i ).FullName ) );
                    }
                    rankTag.Add( temp );
                }
            }
            return rankTag;
        }


        #region Rank Comparison Operators

        // Somewhat counter-intuitive, but lower index number = higher up on the list = higher rank

        public int CompareTo( [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other.Index - Index;
        }

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

        /// <summary> Checks whether this rank is granted the given permission. </summary>
        [Pure]
        public bool Can( Permission permission ) {
            return Permissions[(int)permission];
        }


        /// <summary> Checks whether this rank is granted the given permission, and whether the limit is high </summary>
        [Pure]
        public bool Can( Permission permission, [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return Permissions[(int)permission] && GetLimit( permission ) >= other;
        }


        /// <summary> Whether players of this rank are allowed to see hidden players of the given rank. </summary>
        [Pure]
        public bool CanSee( [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return this > other.GetLimit( Permission.Hide );
        }

        #endregion


        #region Permission Limits

        public Rank[] PermissionLimits {
            get;
            private set;
        }

        readonly string[] permissionLimitStrings;


        /// <summary> Returns the highest rank that is allowed to be affected by this rank,
        /// in the context of the given permission. If no limit was explicitly specified, returns this/own rank. </summary>
        [NotNull]
        public Rank GetLimit( Permission permission ) {
            return PermissionLimits[(int)permission] ?? this;
        }

        /// <summary> Checks whether this rank has a rank limit explicitly set for the given permission. </summary>
        public bool HasLimitSet( Permission permission ) {
            return (PermissionLimits[(int)permission] != null);
        }

        /// <summary> Sets the rank limit for the given permission. </summary>
        public void SetLimit( Permission permission, [CanBeNull] Rank limit ) {
            PermissionLimits[(int)permission] = limit;
        }


        /// <summary> Resets the rank limit for the given permission to default ("own rank"). </summary>
        public void ResetLimit( Permission permission ) {
            SetLimit( permission, null );
        }


        internal void ParsePermissionLimits() {
            for( int i = 0; i < PermissionLimits.Length; i++ ) {
                if( permissionLimitStrings[i] == null ) continue;
                Rank limit = Parse( permissionLimitStrings[i] );
                if( limit == null ) {
                    Logger.Log( LogType.Warning,
                                "Could not parse \"{0}\" as a {1} permission limit for rank \"{2}\". Reset to default (same rank).",
                                permissionLimitStrings[i],
                                (Permission)i,
                                Name );
                }
                SetLimit( (Permission)i, limit );
            }
        }

        #endregion


        #region Validation

        public static bool IsValidRankName( [NotNull] string rankName ) {
            if( rankName == null ) throw new ArgumentNullException( "rankName" );
            if( rankName.Length < 1 || rankName.Length > 16 ) return false;
            for( int i = 0; i < rankName.Length; i++ ) {
                char ch = rankName[i];
                if( ch < '0' || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') ||
                    ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }


        public static bool IsValidID( [NotNull] string id ) {
            if( id == null ) throw new ArgumentNullException( "id" );
            if( id.Length != 16 ) return false;
            for( int i = 0; i < id.Length; i++ ) {
                char ch = id[i];
                if( ch < '0' || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }


        public static bool IsValidPrefix( [NotNull] string prefix ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( prefix.Length == 0 ) return true;
            if( prefix.Length > 1 ) return false;
            return !Chat.ContainsInvalidChars( prefix );
        }

        #endregion


        public override string ToString() {
            return String.Format( "Rank({0})", Name );
        }


        /// <summary> Fully qualified name of the rank. Format: "Name#ID".
        /// Should be used whenever rank name needs to be serialized. </summary>
        public string FullName { get; internal set; }


        /// <summary> Decorated name of the rank, including color and prefix
        /// (if enabled by the configuration). </summary>
        public string ClassyName {
            get {
                string displayedName = Name;
                if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                    displayedName = Prefix + displayedName;
                }
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    displayedName = Color + displayedName;
                }
                return displayedName;
            }
        }

        /// <summary> Shortcut to the list of all online players of this rank. </summary>
        [NotNull]
        public IEnumerable<Player> Players {
            get {
                return Server.Players.Ranked( this );
            }
        }

        /// <summary> Total number of players of this rank (online and offline). </summary>
        public int PlayerCount {
            get {
                return PlayerDB.PlayerInfoList.Count( t => t.Rank == this );
            }
        }


        /// <summary> Parses serialized rank. Accepts either the "name" or "name#ID" format.
        /// Uses legacy rank mapping table for unrecognized ranks.
        /// Does not autocomplete (use RankManager.FindRank for autocompletion).
        /// Name part is case-insensitive. ID part is case-sensitive. </summary>
        /// <param name="name"> Full rank name, or name and ID. </param>
        /// <returns> If name could be parsed, returns the corresponding Rank object. Otherwise returns null. </returns>
        [CanBeNull]
        public static Rank Parse( string name ) {
            if( name == null ) return null;

            Rank parsedRank;
            if( RankManager.RanksByFullName.TryGetValue( name, out parsedRank ) ) {
                return parsedRank;
            }

            int hashIndex = name.IndexOf( '#' );
            if( hashIndex > 0 ) {
                // new format
                string id = name.Substring( hashIndex + 1 );

                if( RankManager.RanksByID.TryGetValue( id, out parsedRank ) ) {
                    // current rank
                    return parsedRank;

                } else {
                    // unknown rank
                    int tries = 0;
                    while( RankManager.LegacyRankMapping.TryGetValue( id, out id ) ) {
                        if( RankManager.RanksByID.TryGetValue( id, out parsedRank ) ) {
                            return parsedRank;
                        }
                        // avoid infinite loops due to recursive definitions
                        tries++;
                        if( tries > 100 ) {
                            throw new RankDefinitionException( name, "Recursive legacy rank definition" );
                        }
                    }
                    string plainName = name.Substring( 0, hashIndex ).ToLowerInvariant();
                    // try to fall back to name-only
                    if( RankManager.RanksByName.TryGetValue( plainName, out parsedRank ) ) {
                        return parsedRank;
                    } else {
                        return null;
                    }
                }

            } else if( RankManager.RanksByName.TryGetValue( name.ToLowerInvariant(), out parsedRank ) ) {
                // old format
                return parsedRank; // LEGACY

            } else {
                // totally unknown rank
                return null;
            }
        }
    }


    /// <summary> Exception that is thrown when parsing a rank definition has failed. </summary>
    public sealed class RankDefinitionException : Exception {
        public RankDefinitionException( string rankName, string message )
            : base( message ) {
            RankName = rankName;
        }

        [StringFormatMethod( "message" )]
        public RankDefinitionException( string rankName, string message, params object[] args ) :
            base( String.Format( message, args ) ) {
            RankName=rankName;
        }


        public string RankName { get; private set; }
    }
}