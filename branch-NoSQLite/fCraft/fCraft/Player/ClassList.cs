using System;
using System.Collections.Generic;
using System.Text;

namespace fCraft {
    public static class ClassList {
        public static Dictionary<string, PlayerClass> classesByName = new Dictionary<string, PlayerClass>();
        public static Dictionary<string, PlayerClass> classesByID = new Dictionary<string, PlayerClass>();
        public static Dictionary<string, string> legacyRankMapping = new Dictionary<string, string>();
        public static List<PlayerClass> classesByIndex = new List<PlayerClass>();
        public static PlayerClass defaultClass, lowestClass, highestClass;


        public static bool AddClass( PlayerClass playerClass ) {
            // check for duplicate class names
            if( classesByName.ContainsKey( playerClass.name.ToLower() ) ) {
                if( !Config.logToString ) {
                    Logger.Log( "PlayerClass.AddClass: Duplicate definition for \"{0}\" (rank {1}) class ignored.", LogType.Error,
                                    playerClass.name, playerClass.rank );
                }
                return false;
            }

            // check for duplicate ranks
            foreach( PlayerClass pc in classesByName.Values ) {
                if( pc.rank == playerClass.rank ) {
                    if( !Config.logToString ) {
                        Logger.Log( "PlayerClass.AddClass: Class definition was ignored because \"{0}\" has the same rank ({1}) as \"{2}\". Each class must have a unique rank number.", LogType.Error,
                                        playerClass.name, playerClass.rank, pc.name );
                    }
                    return false;
                }
            }

            // determine class's index based on its rank
            classesByName[playerClass.name.ToLower()] = playerClass;
            classesByID[playerClass.ID] = playerClass;
            RebuildIndex();

            if( !Config.logToString ) {
                Logger.Log( "PlayerClass.AddClass: Added \"{0}\" (rank {1}) to the class list.", LogType.Debug,
                               playerClass.name, playerClass.rank );
            }
            return true;
        }

        public static PlayerClass ParseClass( string name ) {
            if( name == null ) return null;

            if( name.Contains( "#" ) ) {
                // new format
                string id = name.Substring( name.IndexOf( "#" ) + 1 );

                if( classesByID.ContainsKey( id ) ) {
                    // current class
                    return classesByID[id];

                } else {
                    // legacy class
                    int tries = 0;
                    while( legacyRankMapping.ContainsKey( id ) ) {
                        id = legacyRankMapping[id];
                        if( classesByID.ContainsKey( id ) ) {
                            return classesByID[id];
                        }
                        // avoid infinite loops due to recursive definitions
                        tries++;
                        if( tries > 100 ) {
                            throw new Exception( "Recursive legacy rank definition" );
                        }
                    }
                    // try to fall back to name-only
                    name = name.Substring( 0, name.IndexOf( '#' ) ).ToLower();
                    if( classesByName.ContainsKey( name ) ) {
                        return classesByName[name];
                    } else {
                        return null;
                    }
                }

            } else if( classesByName.ContainsKey( name.ToLower() ) ) {
                // old format
                return classesByName[name.ToLower()]; // LEGACY

            } else {
                // totally unknown class
                return null;
            }
        }

        public static PlayerClass FindClass( string name ) {
            if( name == null ) return null;
            PlayerClass result = null;
            foreach( string className in classesByName.Keys ) {
                if( className != null ) {
                    if( className.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        return classesByName[className];
                    } else if( className.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        if( result == null ) {
                            result = classesByName[className];
                        } else {
                            return null;
                        }
                    }
                }
            }
            return result;
        }

        public static PlayerClass ParseRank( int maxRank ) {
            PlayerClass temp = lowestClass;
            if( temp.rank > maxRank ) return null;
            foreach( PlayerClass c in classesByName.Values ) {
                if( c.rank > temp.rank && c.rank <= maxRank ) {
                    temp = c;
                }
            }
            return temp;
        }


        public static bool ContainsRank( int rank ) {
            foreach( PlayerClass pc in classesByIndex ) {
                if( pc.rank == rank ) {
                    return true;
                }
            }
            return false;
        }

        public static PlayerClass ParseIndex( int index ) {
            if( index == -1 || index > classesByIndex.Count - 1 ) {
                return null;
            }
            return classesByIndex[index];
        }

        public static int GetIndex( PlayerClass pc ) {
            if( pc == null ) return 0;
            else return pc.index + 1;
        }

        public static bool DeleteClass( int index, PlayerClass replacement ) {
            bool rankLimitsChanged = false;
            PlayerClass deletedClass = classesByIndex[index];
            classesByIndex.Remove( deletedClass );
            classesByName.Remove( deletedClass.name.ToLower() );
            legacyRankMapping.Add( deletedClass.ID, replacement.ID );
            foreach( PlayerClass pc in classesByIndex ) {
                if( pc.maxKick == deletedClass ) {
                    pc.maxKick = null;
                    rankLimitsChanged = true;
                }
                if( pc.maxBan == deletedClass ) {
                    pc.maxBan = null;
                    rankLimitsChanged = true;
                }
                if( pc.maxPromote == deletedClass ) {
                    pc.maxPromote = null;
                    rankLimitsChanged = true;
                }
                if( pc.maxDemote == deletedClass ) {
                    pc.maxDemote = null;
                    rankLimitsChanged = true;
                }
            }
            RebuildIndex();
            return rankLimitsChanged;
        }

        public static void RebuildIndex() {
            lowestClass = null;
            highestClass = null;
            classesByIndex.Clear();
            foreach( PlayerClass pc in classesByName.Values ) {
                int i = 0;
                while( i < classesByIndex.Count && classesByIndex[i].rank > pc.rank ) i++;
                classesByIndex.Insert( i, pc );
                if( lowestClass == null || lowestClass.rank > pc.rank ) {
                    lowestClass = pc;
                }
                if( highestClass == null || pc.rank > highestClass.rank ) {
                    highestClass = pc;
                }
            }
            for( int i = 0; i < classesByIndex.Count; i++ ) {
                classesByIndex[i].index = i;
            }
        }


        public static bool CanChangeName( PlayerClass pc, string newName ) {
            if( pc.name.ToLower() == newName.ToLower() ) return true;
            if( classesByName.ContainsKey( newName.ToLower() ) ) return false;
            return true;
        }

        public static void ChangeName( PlayerClass pc, string newName ) {
            classesByName.Remove( pc.name.ToLower() );
            pc.name = newName;
            classesByName.Add( pc.name.ToLower(), pc );
        }


        public static bool CanChangeRank( PlayerClass pc, byte newRank ) {
            if( pc.rank == newRank ) return true;
            foreach( PlayerClass pc2 in classesByIndex ) {
                if( pc2.rank == newRank ) {
                    return false;
                }
            }
            return true;
        }

        public static void ChangeRank( PlayerClass pc, byte newRank ) {
            pc.rank = newRank;
            RebuildIndex();
        }


        public static bool ParseClassLimits( PlayerClass pc ) {
            bool ok = true;
            if( pc.maxKickVal.Length == 0 ) {
                pc.maxKick = pc;
            } else {
                pc.maxKick = ParseClass( pc.maxKickVal );
                ok &= (pc.maxKick != null);
            }
            if( pc.maxBanVal.Length == 0 ) {
                pc.maxBan = pc;
            } else {
                pc.maxBan = ParseClass( pc.maxBanVal );
                ok &= (pc.maxBan != null);
            }
            if( pc.maxPromoteVal.Length == 0 ) {
                pc.maxPromote = pc;
            } else {
                pc.maxPromote = ParseClass( pc.maxPromoteVal );
                ok &= (pc.maxPromote != null);
            }
            if( pc.maxDemoteVal.Length == 0 ) {
                pc.maxDemote = pc;
            } else {
                pc.maxDemote = ParseClass( pc.maxDemoteVal );
                ok &= (pc.maxDemote != null);
            }
            if( pc.maxHideFromVal.Length == 0 ) {
                pc.maxHideFrom = pc;
            } else {
                pc.maxHideFrom = ParseClass( pc.maxHideFromVal );
                ok &= (pc.maxHideFrom != null);
            }
            return ok;
        }


        static Random rand = new Random();
        public static string GenerateID() {
            StringBuilder ID = new StringBuilder();
            string IDChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for( int i = 0; i < 16; i++ ) {
                ID.Append( IDChars[rand.Next( 0, IDChars.Length - 1 )] );
            }
            return ID.ToString();
        }
    }
}