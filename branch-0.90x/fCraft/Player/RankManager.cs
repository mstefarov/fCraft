// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Manages all the ranks on a server. Controls what ranks are available and in what order they exist in. </summary>
    public static class RankManager {
        [NotNull]
        internal static Dictionary<string, string> LegacyRankMapping { get; private set; }

        /// <summary> List of Ranks, indexed by their name. </summary>
        [NotNull]
        public static Dictionary<string, Rank> RanksByName { get; private set; }

        /// <summary> List of Ranks, indexed by their fully qualified name. </summary>
        [NotNull]
        public static Dictionary<string, Rank> RanksByFullName { get; private set; }

        /// <summary> List of Ranks, indexed by their ID. </summary>
        [NotNull]
        public static Dictionary<string, Rank> RanksByID { get; private set; }

        /// <summary> List of all Ranks, in no particular order. </summary>
        [NotNull]
        public static List<Rank> Ranks { get; private set; }

        /// <summary> Default Rank of a new user. </summary>
        public static Rank DefaultRank { get; set; }

        /// <summary> Lowest Rank available in the server. </summary>
        public static Rank LowestRank { get; private set; }

        /// <summary> Highest Rank available in the server. </summary>
        public static Rank HighestRank { get; private set; }

        /// <summary> Highest Rank that /Patrol will consider when selecting candidates. </summary>
        public static Rank PatrolledRank { get; set; }

        /// <summary> The default minimum Rank required to build in newly created worlds. </summary>
        public static Rank DefaultBuildRank { get; set; }

        /// <summary> Rank used by BlockDB to determine whether it should be auto-enabled on a world or not.
        /// Worlds where BuildSecurity.MinRank is equal or lower than this rank WILL have BlockDB auto-enabled. </summary>
        public static Rank BlockDBAutoEnableRank { get; set; }


        static RankManager() {
            Reset();
            LegacyRankMapping = new Dictionary<string, string>();
        }


        /// <summary> Clears the list of ranks. </summary>
        internal static void Reset() {
            if( PlayerDB.IsLoaded ) {
                throw new InvalidOperationException( "You may not reset ranks after PlayerDB has already been loaded." );
            }
            RanksByName = new Dictionary<string, Rank>();
            RanksByFullName = new Dictionary<string, Rank>();
            RanksByID = new Dictionary<string, Rank>();
            Ranks = new List<Rank>();
            DefaultRank = null;
            PatrolledRank = null;
            DefaultBuildRank = null;
            BlockDBAutoEnableRank = null;
        }


        /// <summary> Adds a new rank to the list. Checks for duplicates. </summary>
        public static void AddRank( [NotNull] Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( PlayerDB.IsLoaded ) {
                throw new InvalidOperationException( "You may not add ranks after PlayerDB has already been loaded." );
            }
            // check for duplicate rank names
            if( RanksByName.ContainsKey( rank.Name.ToLower() ) ) {
                throw new RankDefinitionException( rank.Name,
                                                   "Duplicate definition for rank \"{0}\" (by Name) was ignored.",
                                                   rank.Name );
            }

            if( RanksByID.ContainsKey( rank.ID ) ) {
                throw new RankDefinitionException( rank.Name,
                                                   "Duplicate definition for rank \"{0}\" (by ID) was ignored.",
                                                   rank.Name );
            }

            Ranks.Add( rank );
            RanksByName[rank.Name.ToLower()] = rank;
            RanksByFullName[rank.FullName] = rank;
            RanksByID[rank.ID] = rank;
            RebuildIndex();
        }


        /// <summary> Parses rank name (without the ID) using autocompletion. </summary>
        /// <param name="name"> Full or partial rank name. May be null. </param>
        /// <returns> If name could be parsed, returns the corresponding Rank object. Otherwise returns null. 
        /// If null was given instead of rank name, returns null. </returns>
        [CanBeNull]
        public static Rank FindRank( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Rank result = null;
            foreach( string rankName in RanksByName.Keys ) {
                if( rankName.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    return RanksByName[rankName];
                }
                if( rankName.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if( result == null ) {
                        result = RanksByName[rankName];
                    } else {
                        return null;
                    }
                }
            }
            return result;
        }


        /// <summary> Finds rank by index. Rank at index 0 is the highest. </summary>
        /// <returns> If name could be parsed, returns the corresponding Rank object. Otherwise returns null. </returns>
        [CanBeNull]
        public static Rank FindRank( int index ) {
            if( index < 0 || index >= Ranks.Count ) {
                return null;
            }
            return Ranks[index];
        }


        public static int GetIndex( Rank rank ) {
            return (rank == null) ? 0 : (rank.Index + 1);
        }


        /// <summary> Removes the specified rank from the list of available ranks </summary>
        /// <param name="deletedRank"> Rank to be deleted. </param>
        /// <param name="replacementRank"> Rank that will replace the deleted rank. </param>
        /// <returns> Whether or not the rank was successfully deleted/replaced. </returns>
        /// <exception cref="ArgumentNullException"> deletedRank or replacementRank is null. </exception>
        /// <exception cref="InvalidOperationException"> PlayerDB is already loaded. </exception>
        public static bool DeleteRank( [NotNull] Rank deletedRank, [NotNull] Rank replacementRank ) {
            if( deletedRank == null ) throw new ArgumentNullException( "deletedRank" );
            if( replacementRank == null ) throw new ArgumentNullException( "replacementRank" );
            if( PlayerDB.IsLoaded ) {
                throw new InvalidOperationException( "You may not modify ranks after PlayerDB has been loaded." );
            }
            bool rankLimitsChanged = false;
            Ranks.Remove( deletedRank );
            RanksByName.Remove( deletedRank.Name.ToLower() );
            RanksByID.Remove( deletedRank.ID );
            RanksByFullName.Remove( deletedRank.FullName );
            LegacyRankMapping.Add( deletedRank.ID, replacementRank.ID );
            foreach( Rank rank in Ranks ) {
                for( int i = 0; i < rank.PermissionLimits.Length; i++ ) {
                    if( rank.GetLimit( (Permission)i ) == deletedRank ) {
                        rank.ResetLimit( (Permission)i );
                        rankLimitsChanged = true;
                    }
                }
            }
            RebuildIndex();
            return rankLimitsChanged;
        }


        static void RebuildIndex() {
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

            // assign nextRankUp/nextRankDown
            if( Ranks.Count > 1 ) {
                for( int i = 0; i < Ranks.Count - 1; i++ ) {
                    Ranks[i + 1].NextRankUp = Ranks[i];
                    Ranks[i].NextRankDown = Ranks[i + 1];
                }
            }
            HighestRank.NextRankUp = null;
            LowestRank.NextRankDown = null;
        }


        public static bool CanRenameRank( [NotNull] Rank rank, [NotNull] string newName ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( newName == null ) throw new ArgumentNullException( "newName" );
            if( rank.Name.Equals( newName, StringComparison.OrdinalIgnoreCase ) ) {
                return true;
            } else {
                return !RanksByName.ContainsKey( newName.ToLower() );
            }
        }


        public static void RenameRank( [NotNull] Rank rank, [NotNull] string newName ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( newName == null ) throw new ArgumentNullException( "newName" );
            RanksByName.Remove( rank.Name.ToLower() );
            rank.Name = newName;
            rank.FullName = rank.Name + "#" + rank.ID;
            RanksByName.Add( rank.Name.ToLower(), rank );
        }


        /// <summary> Raises the index value of the specified Rank, and then lowers the rank that was previously in that position. </summary>
        /// <param name="rank"> Rank to raise. </param>
        /// <returns> True if the Rank index was raised; false if it is already the highest rank. </returns>
        /// <exception cref="ArgumentNullException"> rank is null. </exception>
        /// <exception cref="InvalidOperationException"> PlayerDB is already loaded. </exception>
        public static bool RaiseRank( [NotNull] Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( rank == Ranks.First() ) {
                return false;
            }
            Rank nextRankUp = Ranks[rank.Index - 1];
            Ranks[rank.Index - 1] = rank;
            Ranks[rank.Index] = nextRankUp;
            RebuildIndex();
            return true;
        }


        /// <summary> Lowers the index value of the specified Rank, and then raises the rank that was previously in that position. </summary>
        /// <param name="rank"> Rank to lower. </param>
        /// <returns> True if the Rank index was lowered; false if it is already the lowest rank. </returns>
        /// <exception cref="ArgumentNullException"> rank is null. </exception>
        /// <exception cref="InvalidOperationException"> PlayerDB is already loaded. </exception>
        public static bool LowerRank( [NotNull] Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( rank == Ranks.Last() ) {
                return false;
            }
            Rank nextRankDown = Ranks[rank.Index + 1];
            Ranks[rank.Index + 1] = rank;
            Ranks[rank.Index] = nextRankDown;
            RebuildIndex();
            return true;
        }


        internal static void ParsePermissionLimits() {
            foreach( Rank rank in Ranks ) {
                rank.ParsePermissionLimits();
            }
        }


        /// <summary> Creates a 16 character unique rank ID, via Server.GetRandomString(). </summary>
        /// <returns> 16 character unique rank ID. </returns>
        [NotNull]
        public static string GenerateID() {
            return Server.GetRandomString( 16 );
        }


        /// <summary> Finds the lowest rank that has all the required permissions. </summary>
        /// <param name="permissions"> One or more permissions to check for. </param>
        /// <returns> A relevant Rank object, or null of none were found. </returns>
        [CanBeNull]
        public static Rank GetMinRankWithAllPermissions( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            for( int r = Ranks.Count - 1; r >= 0; r-- ) {
                int r1 = r;
                if( permissions.All( t => Ranks[r1].Can( t ) ) ) {
                    return Ranks[r];
                }
            }
            return null;
        }

        /// <summary> Finds the lowest rank that has all the required permissions. </summary>
        /// <param name="permissions"> One or more permissions to check for. </param>
        /// <returns> A relevant Rank object, or null of none were found. </returns>
        [CanBeNull]
        public static Rank GetMinRankWithAnyPermission( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            for( int r = Ranks.Count - 1; r >= 0; r-- ) {
                int r1 = r;
                if( permissions.Any( t => Ranks[r1].Can( t ) ) ) {
                    return Ranks[r];
                }
            }
            return null;
        }
    }
}
