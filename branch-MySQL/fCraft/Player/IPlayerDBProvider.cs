using System.Collections.Generic;
using System.Net;
using JetBrains.Annotations;

namespace fCraft {
    public interface IPlayerDBProvider {
        object SyncRoot { get; }

        /// <summary> Adds a new PlayerInfo entry for a player. </summary>
        [NotNull]
        PlayerInfo AddPlayer( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank, RankChangeType rankChangeType );


        /// <summary> Adds a new PlayerInfo entry for a player. </summary>
        [NotNull]
        PlayerInfo AddUnrecognizedPlayer( [NotNull] string name, [NotNull] Rank startingRank, RankChangeType rankChangeType );


        /// <summary> Adds a new PlayerInfo entry for a non-player. </summary>
        [NotNull]
        PlayerInfo AddSuperPlayer( ReservedPlayerID id, [NotNull] string name, [NotNull] Rank rank );


        /// <summary> Removes a PlayerInfo entry. </summary>
        bool Remove( [NotNull] PlayerInfo playerInfo );


        /// <summary> Finds player by exact name. </summary>
        [CanBeNull]
        PlayerInfo FindExact( [NotNull] string fullName );


        /// <summary> Finds players by IP address. </summary>
        [NotNull]
        IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address, int limit );


        /// <summary> Finds players by partial name (prefix). </summary>
        /// <param name="partialName"> Full or partial name of the player. </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos whose names start with partialName. </returns>
        [NotNull]
        IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string partialName, int limit );


        /// <summary> Searches for player names starting with namePart, returning just one or none of the matches. </summary>
        /// <param name="partialName"> Partial or full player name. </param>
        /// <param name="result"> PlayerInfo to output (will be set to null if no single match was found). </param>
        /// <returns> true if one or zero matches were found, false if multiple matches were found. </returns>
        bool FindOneByPartialName( [NotNull] string partialName, [CanBeNull] out PlayerInfo result );


        /// <summary> Finds player by name pattern. </summary>
        /// <param name="pattern"> Pattern to search for. Asterisk (*) matches zero or more characters.
        /// Question mark (?) matches exactly one character. </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos whose names match the pattern. </returns>
        [NotNull]
        IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern, int limit );


        /// <summary> Preloads the whole database. </summary>
        [CanBeNull]
        IEnumerable<PlayerInfo> Load();


        /// <summary> Saves the whole database. </summary>
        void Save();


        /// <summary> Changes ranks of all players in one transaction. </summary>
        void MassRankChange( [NotNull] Player player, [NotNull] Rank from, [NotNull] Rank to, [NotNull] string reason );

        /// <summary> Swaps records of two players in one transaction. </summary>
        void SwapInfo( [NotNull] PlayerInfo player1, [NotNull] PlayerInfo player2 );
    }
}