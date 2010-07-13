// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;


namespace fCraft {
    public sealed class PlayerClass {
        public string name;
        public byte rank;
        public string color;
        public bool[] permissions;
        public PlayerClass maxPromote, maxDemote, maxKick, maxBan;
        public string prefix = "";
        public int spamKickThreshold, spamBanThreshold, idleKickTimer;
        public bool reservedSlot;
        public int index;

        // these need to be parsed after all classes are added
        internal string maxPromoteVal = "", maxDemoteVal = "", maxKickVal = "", maxBanVal = "";

        public PlayerClass() {
            permissions = new bool[Enum.GetValues( typeof( Permissions ) ).Length];
        }


        public bool Can( Permissions permission ) {
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

        public static bool IsValidPrefix( string val ) {
            if( val.Length == 0 ) return true;
            if( val.Length > 1 ) return false;
            return val[0] > ' ' && val[0] != '&' && val[0] != '`' && val[0] != '^' && val[0] <= '}';
        }

    }
}