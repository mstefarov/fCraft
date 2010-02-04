using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    sealed class PlayerClass {
        public static Dictionary<string, PlayerClass> classes = new Dictionary<string, PlayerClass>();
        public static PlayerClass lowestClass;

        public static void AddClass( PlayerClass playerClass ) {
            classes[playerClass.name] = playerClass;
            if( lowestClass == null || lowestClass.rank > playerClass.rank ) {
                lowestClass = playerClass;
            }
        }

        public static PlayerClass GetDefaultClass() {
            return lowestClass;
        }

        public static PlayerClass Parse( string name ) {
            if( classes.ContainsKey( name ) ) {
                return classes[name];
            } else {
                return null;
            }
        }

        public string name;
        public byte rank;
        public char color;
        public bool[] permissions;

        public PlayerClass() {
            permissions = new bool[Enum.GetValues( typeof( Permissions ) ).Length];
        }

        public bool IsAllowed( Permissions permission ) {
            return permissions[(int)permission];
        }

        public override string ToString() {
            return name;
        }
    }

    enum Permissions {
        Build = 0,
        Delete = 1,
        BuildAdmin = 2, // build admincrete
        DeleteAdmin = 3, // delete admincrete
        Announce = 4, //  allows /say
        Kick = 5,
        Ban = 6,
        IPBan = 7,
        Promote = 8,
        Demote = 9,
        Hide = 10, // go invisible!
        Alias = 11, // change own name
        Backup = 12, // live backup/rollback
        ControlWater = 13, // includes placing water blocks and changing water sim parameters
        ControlLava = 14  // same as above, but with lava
    }
}
