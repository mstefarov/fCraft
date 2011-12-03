using System;
using System.Text.RegularExpressions;

namespace fCraft {
    sealed class Player {

        public enum PlayerClass {
            Player = 0,
            Regular = 1,
            Admin = 2,
            Owner = 3
        }

        public string name;
        public PlayerClass playerClass;

        public static bool IsValidName( string name ) {
            Match match = Regex.Match( name, @"^[\w]{1,10}$" );
            return match.Success;
        }

        public byte GetPlayerClassCode() {
            switch( playerClass ) {
                case PlayerClass.Admin:
                    return 100;
                case PlayerClass.Owner:
                    return 100;
                default:
                    return 0;
            }
        }
    }
}
