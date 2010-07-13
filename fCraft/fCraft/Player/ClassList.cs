using System;
using System.Collections.Generic;

namespace fCraft {
    public static class ClassList {
        public static Dictionary<string, PlayerClass> classes = new Dictionary<string, PlayerClass>();
        public static List<PlayerClass> classesByIndex = new List<PlayerClass>();
        internal static PlayerClass defaultClass, lowestClass, highestClass;

        public static bool AddClass( PlayerClass playerClass ) {
            // check for duplicate class names
            if( classes.ContainsKey( playerClass.name.ToLower() ) ) {
                if( !Config.logToString ) {
                    Logger.Log( "PlayerClass.AddClass: Duplicate definition for \"{0}\" (rank {1}) class ignored.", LogType.Error,
                                    playerClass.name, playerClass.rank );
                }
                return false;
            }
            // check for duplicate ranks
            foreach( PlayerClass pc in classes.Values ) {
                if( pc.rank == playerClass.rank ) {
                    if( !Config.logToString ) {
                        Logger.Log( "PlayerClass.AddClass: Class definition was ignored because \"{0}\" has the same rank ({1}) as \"{2}\". Each class must have a unique rank number.", LogType.Error,
                                        playerClass.name, playerClass.rank, pc.name );
                    }
                    return false;
                }
            }

            // determine class's index based on its rank
            classes[playerClass.name.ToLower()] = playerClass;
            RebuildIndex();

            if( !Config.logToString ) {
                Logger.Log( "PlayerClass.AddClass: Added \"{0}\" (rank {1}) to the class list.", LogType.Debug,
                               playerClass.name, playerClass.rank );
            }
            return true;
        }

        public static PlayerClass ParseClass( string name ) {
            if( name == null ) return null;
            if( classes.ContainsKey( name.ToLower() ) ) {
                return classes[name.ToLower()];
            } else {
                return null;
            }
        }

        public static PlayerClass FindClass( string _name ) {
            if( _name == null ) return null;
            PlayerClass result = null;
            foreach( string className in classes.Keys ){
                if( className != null ){
                    if( className.Equals(_name, StringComparison.OrdinalIgnoreCase)){
                        return classes[className];
                    } else if( className.StartsWith( _name, StringComparison.OrdinalIgnoreCase ) ) {
                        if( result == null ) {
                            result = classes[className];
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
            foreach( PlayerClass c in classes.Values ) {
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

        public static bool DeleteClass( int index ) {
            bool rankLimitsChanged = false;
            PlayerClass deletedClass = classesByIndex[index];
            classesByIndex.Remove( deletedClass );
            classes.Remove( deletedClass.name.ToLower() );
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
            foreach( PlayerClass pc in classes.Values ) {
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
            if( classes.ContainsKey( newName.ToLower() ) ) return false;
            return true;
        }

        public static void ChangeName( PlayerClass pc, string newName ) {
            classes.Remove( pc.name.ToLower() );
            pc.name = newName;
            classes.Add( pc.name.ToLower(), pc );
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
            if( pc.maxKickVal == "" ) {
                pc.maxKick = pc;
            } else {
                pc.maxKick = ParseClass( pc.maxKickVal );
                ok &= (pc.maxKick != null);
            }
            if( pc.maxBanVal == "" ) {
                pc.maxBan = pc;
            } else {
                pc.maxBan = ParseClass( pc.maxBanVal );
                ok &= (pc.maxBan != null);
            }
            if( pc.maxPromoteVal == "" ) {
                pc.maxPromote = pc;
            } else {
                pc.maxPromote = ParseClass( pc.maxPromoteVal );
                ok &= (pc.maxPromote != null);
            }
            if( pc.maxDemoteVal == "" ) {
                pc.maxDemote = pc;
            } else {
                pc.maxDemote = ParseClass( pc.maxDemoteVal );
                ok &= (pc.maxDemote != null);
            }
            return ok;
        }
    }
}
