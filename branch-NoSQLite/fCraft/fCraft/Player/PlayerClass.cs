// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;


namespace fCraft {
    public sealed class PlayerClass {
        public string name;
        public byte rank;
        public string color;
        public string ID;
        public bool[] permissions;
        public PlayerClass maxPromote,
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

        // these need to be parsed after all classes are added
        internal string maxPromoteVal = "",
                        maxDemoteVal = "",
                        maxKickVal = "",
                        maxBanVal = "",
                        maxHideFromVal = "";

        public PlayerClass() {
            permissions = new bool[Enum.GetValues( typeof( Permission ) ).Length];
        }


        public bool Can( Permission permission ) {
            return permissions[(int)permission];
        }


        public bool CanKick( PlayerClass other ) {
            return maxKick.rank >= other.rank;
        }

        public bool CanBan( PlayerClass other ) {
            return maxBan.rank >= other.rank;
        }

        public bool CanPromote( PlayerClass other ) {
            return maxPromote.rank >= other.rank;
        }

        public bool CanDemote( PlayerClass other ) {
            return maxDemote.rank >= other.rank;
        }

        public bool CanSee( PlayerClass other ) {
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


        public static bool IsValidClassName( string className ) {
            if( className.Length < 1 || className.Length > 16 ) return false;
            for( int i = 0; i < className.Length; i++ ) {
                char ch = className[i];
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


        public string ToComboBoxOption() {
            return String.Format( "{0,3} {1,1}{2}", rank, prefix, name );
        }

        public override string ToString() {
            return name + "#" + ID;
        }

        public string GetClassyName() {
            string displayedName = name;
            if( Config.GetBool( ConfigKey.ClassPrefixesInChat ) ) {
                displayedName = prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.ClassColorsInChat ) ) {
                displayedName = color + displayedName;
            }
            return displayedName;
        }
    }
}