using System;
using System.Collections.Generic;
using System.Text;

namespace fCraft {
    public static class RankList {
        public static Dictionary<string, Rank> ranksByName = new Dictionary<string, Rank>();
        public static Dictionary<string, Rank> ranksByID = new Dictionary<string, Rank>();
        public static Dictionary<string, string> legacyRankMapping = new Dictionary<string, string>();
        public static List<Rank> ranksByIndex = new List<Rank>();
        public static Rank defaultRank, lowestRank, highestRank;


        public static bool AddRank( Rank rank ) {
            // check for duplicate class names
            if( ranksByName.ContainsKey( rank.Name.ToLower() ) ) {
                if( !Config.logToString ) {
                    Logger.Log( "PlayerClass.AddClass: Duplicate definition for \"{0}\" (rank {1}) class ignored.", LogType.Error,
                                    rank.Name, rank.rank );
                }
                return false;
            }

            // check for duplicate ranks
            foreach( Rank pc in ranksByName.Values ) {
                if( pc.rank == rank.rank ) {
                    if( !Config.logToString ) {
                        Logger.Log( "PlayerClass.AddClass: Class definition was ignored because \"{0}\" has the same rank ({1}) as \"{2}\". Each class must have a unique rank number.", LogType.Error,
                                        rank.Name, rank.rank, pc.Name );
                    }
                    return false;
                }
            }

            // determine class's index based on its rank
            ranksByName[rank.Name.ToLower()] = rank;
            ranksByID[rank.ID] = rank;
            RebuildIndex();

            if( !Config.logToString ) {
                Logger.Log( "PlayerClass.AddClass: Added \"{0}\" (rank {1}) to the class list.", LogType.Debug,
                               rank.Name, rank.rank );
            }
            return true;
        }


        public static Rank ParseRank( string name ) {
            if( name == null ) return null;

            if( name.Contains( "#" ) ) {
                // new format
                string id = name.Substring( name.IndexOf( "#" ) + 1 );

                if( ranksByID.ContainsKey( id ) ) {
                    // current class
                    return ranksByID[id];

                } else {
                    // legacy class
                    int tries = 0;
                    while( legacyRankMapping.ContainsKey( id ) ) {
                        id = legacyRankMapping[id];
                        if( ranksByID.ContainsKey( id ) ) {
                            return ranksByID[id];
                        }
                        // avoid infinite loops due to recursive definitions
                        tries++;
                        if( tries > 100 ) {
                            throw new Exception( "Recursive legacy rank definition" );
                        }
                    }
                    // try to fall back to name-only
                    name = name.Substring( 0, name.IndexOf( '#' ) ).ToLower();
                    if( ranksByName.ContainsKey( name ) ) {
                        return ranksByName[name];
                    } else {
                        return null;
                    }
                }

            } else if( ranksByName.ContainsKey( name.ToLower() ) ) {
                // old format
                return ranksByName[name.ToLower()]; // LEGACY

            } else {
                // totally unknown class
                return null;
            }
        }

        public static Rank FindRank( string name ) {
            if( name == null ) return null;
            Rank result = null;
            foreach( string rankName in ranksByName.Keys ) {
                if( rankName != null ) {
                    if( rankName.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        return ranksByName[rankName];
                    } else if( rankName.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        if( result == null ) {
                            result = ranksByName[rankName];
                        } else {
                            return null;
                        }
                    }
                }
            }
            return result;
        }


        public static bool ContainsRank( int rank ) {
            foreach( Rank pc in ranksByIndex ) {
                if( pc.rank == rank ) {
                    return true;
                }
            }
            return false;
        }

        public static Rank ParseIndex( int index ) {
            if( index == -1 || index > ranksByIndex.Count - 1 ) {
                return null;
            }
            return ranksByIndex[index];
        }

        public static int GetIndex( Rank pc ) {
            if( pc == null ) return 0;
            else return pc.index + 1;
        }

        public static bool DeleteRank( int index, Rank replacement ) {
            bool rankLimitsChanged = false;
            Rank deletedClass = ranksByIndex[index];
            ranksByIndex.Remove( deletedClass );
            ranksByName.Remove( deletedClass.Name.ToLower() );
            legacyRankMapping.Add( deletedClass.ID, replacement.ID );
            foreach( Rank pc in ranksByIndex ) {
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
            lowestRank = null;
            highestRank = null;
            ranksByIndex.Clear();
            foreach( Rank pc in ranksByName.Values ) {
                int i = 0;
                while( i < ranksByIndex.Count && ranksByIndex[i].rank > pc.rank ) i++;
                ranksByIndex.Insert( i, pc );
                if( lowestRank == null || lowestRank.rank > pc.rank ) {
                    lowestRank = pc;
                }
                if( highestRank == null || pc.rank > highestRank.rank ) {
                    highestRank = pc;
                }
            }
            for( int i = 0; i < ranksByIndex.Count; i++ ) {
                ranksByIndex[i].index = i;
            }
        }


        public static bool CanRenameRank( Rank rank, string newName ) {
            if( rank.Name.ToLower() == newName.ToLower() ) return true;
            if( ranksByName.ContainsKey( newName.ToLower() ) ) return false;
            return true;
        }

        public static void RenameRank( Rank rank, string newName ) {
            ranksByName.Remove( rank.Name.ToLower() );
            rank.Name = newName;
            ranksByName.Add( rank.Name.ToLower(), rank );
        }


        public static bool CanChangeClassRank( Rank rank, byte newRank ) {
            if( rank.rank == newRank ) return true;
            foreach( Rank pc2 in ranksByIndex ) {
                if( pc2.rank == newRank ) {
                    return false;
                }
            }
            return true;
        }

        public static void ChangeClassRank( Rank rank, byte newRank ) {
            rank.rank = newRank;
            RebuildIndex();
        }


        public static void ParseRankRelations() {
            foreach( Rank pc in ranksByIndex ) {

                bool ok = true;
                if( pc.maxKickVal.Length == 0 ) {
                    pc.maxKick = pc;
                } else {
                    pc.maxKick = ParseRank( pc.maxKickVal );
                    ok &= (pc.maxKick != null);
                }

                if( pc.maxBanVal.Length == 0 ) {
                    pc.maxBan = pc;
                } else {
                    pc.maxBan = ParseRank( pc.maxBanVal );
                    ok &= (pc.maxBan != null);
                }

                if( pc.maxPromoteVal.Length == 0 ) {
                    pc.maxPromote = pc;
                } else {
                    pc.maxPromote = ParseRank( pc.maxPromoteVal );
                    ok &= (pc.maxPromote != null);
                }

                if( pc.maxDemoteVal.Length == 0 ) {
                    pc.maxDemote = pc;
                } else {
                    pc.maxDemote = ParseRank( pc.maxDemoteVal );
                    ok &= (pc.maxDemote != null);
                }

                if( pc.maxHideFromVal.Length == 0 ) {
                    pc.maxHideFrom = pc;
                } else {
                    pc.maxHideFrom = ParseRank( pc.maxHideFromVal );
                    ok &= (pc.maxHideFrom != null);
                }

                // assign nextClassUp/nextClassDown
                if( ranksByIndex.Count > 1 ) {
                    for( int i = 0; i < ranksByIndex.Count - 1; i++ ) {
                        ranksByIndex[i + 1].nextRankUp = ranksByIndex[i];
                        ranksByIndex[i].nextRankDown = ranksByIndex[i + 1];
                    }
                }

                if( !ok ) {
                    Logger.Log( "Could not parse one of the rank-limits for kick, ban, promote, and/or demote permissions for {0}. " +
                         "Any unrecognized limits were reset to defaults.", LogType.Warning, pc.Name );
                }
            }
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