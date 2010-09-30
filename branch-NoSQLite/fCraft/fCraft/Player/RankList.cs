using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace fCraft {
    public static class RankList {
        public static Dictionary<string, Rank> RanksByName = new Dictionary<string, Rank>();
        public static Dictionary<string, Rank> RanksByID = new Dictionary<string, Rank>();
        public static Dictionary<string, string> LegacyRankMapping = new Dictionary<string, string>();
        public static List<Rank> Ranks = new List<Rank>();
        public static Rank DefaultRank, LowestRank, HighestRank;


        public static bool AddRank( Rank rank ) {
            // check for duplicate class names
            if( RanksByName.ContainsKey( rank.Name.ToLower() ) ) {
                if( !Config.logToString ) {
                    Logger.Log( "PlayerClass.AddClass: Duplicate definition for \"{0}\" (rank {1}) class ignored.", LogType.Error,
                                    rank.Name, rank.legacyNumericRank );
                }
                return false;
            }

            Ranks.Add( rank );
            RanksByName[rank.Name.ToLower()] = rank;
            RanksByID[rank.ID] = rank;
            RebuildIndex();

            if( !Config.logToString ) {
                Logger.Log( "PlayerClass.AddClass: Added \"{0}\" (rank {1}) to the class list.", LogType.Debug,
                               rank.Name, rank.legacyNumericRank );
            }
            return true;
        }

        // parse rank from serialized string (with ID) - for loading from files
        public static Rank ParseRank( string name ) {
            if( name == null ) return null;

            if( name.Contains( "#" ) ) {
                // new format
                string id = name.Substring( name.IndexOf( "#" ) + 1 );

                if( RanksByID.ContainsKey( id ) ) {
                    // current class
                    return RanksByID[id];

                } else {
                    // unknown class
                    int tries = 0;
                    while( LegacyRankMapping.ContainsKey( id ) ) {
                        id = LegacyRankMapping[id];
                        if( RanksByID.ContainsKey( id ) ) {
                            return RanksByID[id];
                        }
                        // avoid infinite loops due to recursive definitions
                        tries++;
                        if( tries > 100 ) {
                            throw new Exception( "Recursive legacy rank definition" );
                        }
                    }
                    // try to fall back to name-only
                    name = name.Substring( 0, name.IndexOf( '#' ) ).ToLower();
                    if( RanksByName.ContainsKey( name ) ) {
                        return RanksByName[name];
                    } else {
                        return null;
                    }
                }

            } else if( RanksByName.ContainsKey( name.ToLower() ) ) {
                // old format
                return RanksByName[name.ToLower()]; // LEGACY

            } else {
                // totally unknown class
                return null;
            }
        }

        // find rank by name, with autocompletion - for parsing commands
        public static Rank FindRank( string name ) {
            if( name == null ) return null;

            Rank result = null;
            foreach( string rankName in RanksByName.Keys ) {
                if( rankName != null ) {
                    if( rankName.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        return RanksByName[rankName];
                    } else if( rankName.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        if( result == null ) {
                            result = RanksByName[rankName];
                        } else {
                            return null;
                        }
                    }
                }
            }
            return result;
        }


        public static Rank ParseIndex( int index ) {
            if( index == -1 || index > Ranks.Count - 1 ) {
                return null;
            }
            return Ranks[index];
        }

        public static int GetIndex( Rank pc ) {
            if( pc == null ) return 0;
            else return pc.Index + 1;
        }

        public static bool DeleteRank( int index, Rank replacement ) {
            bool rankLimitsChanged = false;
            Rank deletedClass = Ranks[index];
            Ranks.Remove( deletedClass );
            RanksByName.Remove( deletedClass.Name.ToLower() );
            LegacyRankMapping.Add( deletedClass.ID, replacement.ID );
            foreach( Rank pc in Ranks ) {
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
            if( Ranks.Count == 0 ) {
                LowestRank = null;
                HighestRank = null;
                DefaultRank = null;
                return;
            }

            // find highest/lowers ranks
            HighestRank = Ranks.First();
            LowestRank = Ranks.Last();

            // assign indices
            for( int i = 0; i < Ranks.Count; i++ ) {
                Ranks[i].Index = i;
            }

            // assign nextClassUp/nextClassDown
            if( Ranks.Count > 1 ) {
                for( int i = 0; i < Ranks.Count - 1; i++ ) {
                    Ranks[i + 1].NextRankUp = Ranks[i];
                    Ranks[i].NextRankDown = Ranks[i + 1];
                }
            } else {
                Ranks[0].NextRankUp = null;
                Ranks[0].NextRankDown = null;
            }
        }


        public static bool CanRenameRank( Rank rank, string newName ) {
            if( rank.Name.ToLower() == newName.ToLower() ) return true;
            if( RanksByName.ContainsKey( newName.ToLower() ) ) return false;
            return true;
        }

        public static void RenameRank( Rank rank, string newName ) {
            RanksByName.Remove( rank.Name.ToLower() );
            rank.Name = newName;
            RanksByName.Add( rank.Name.ToLower(), rank );
        }


        public static bool RaiseRank( Rank rank ) {
            if( rank != Ranks.First() ) {
                Rank nextRankUp = Ranks[rank.Index - 1];
                Ranks[rank.Index - 1] = rank;
                Ranks[rank.Index] = nextRankUp;
                RebuildIndex();
                return true;
            } else {
                return false;
            }
        }


        public static bool LowerRank( Rank rank ) {
            if( rank != Ranks.Last() ) {
                Rank nextRankDown = Ranks[rank.Index + 1];
                Ranks[rank.Index + 1] = rank;
                Ranks[rank.Index] = nextRankDown;
                RebuildIndex();
                return true;
            } else {
                return false;
            }
        }



        public static void ParsePermissionLimits() {
            foreach( Rank pc in Ranks ) {
                if( !pc.ParsePermissionLimits() ) {
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