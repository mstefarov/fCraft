// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;
using System.Net;
using JetBrains.Annotations;

namespace fCraft {
    public interface IPlayerDBProvider {
        object SyncRoot { get; }

        PlayerDBProviderType Type { get; }


        /// <summary> Adds a new PlayerInfo entry for an actual, logged-in player. </summary>
        /// <returns> A newly-created PlayerInfo entry. </returns>
        [NotNull]
        PlayerInfo AddPlayer( [NotNull] string name, [NotNull] Rank startingRank, RankChangeType rankChangeType, [NotNull] IPAddress address );


        /// <summary> Adds a new PlayerInfo entry for a player who has never been online, by name. </summary>
        /// <returns> A newly-created PlayerInfo entry. </returns>
        [NotNull]
        PlayerInfo AddUnrecognizedPlayer( [NotNull] string name, [NotNull] Rank startingRank, RankChangeType rankChangeType );


        /// <summary> Removes a PlayerInfo entry from the database. </summary>
        /// <returns> True if the entry is successfully found and removed; otherwise false. </returns>
        bool Remove( [NotNull] PlayerInfo playerInfo );


        /// <summary> Finds player by exact name. </summary>
        /// <param name="fullName"> Full, case-insensitive name of the player. </param>
        /// <returns> PlayerInfo if player was found, or null if not found. </returns>
        [CanBeNull]
        PlayerInfo FindExact( [NotNull] string fullName );


        /// <summary> Finds players by IP address. </summary>
        /// <param name="address"> Player's IP address. </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos who have logged in from given IP. </returns>
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
        /// <param name="pattern"> Pattern to search for.
        /// Asterisk (*) matches zero or more characters.
        /// Question mark (?) matches exactly one character. </param>
        /// <param name="limit"> Maximum number of results to return. </param>
        /// <returns> A sequence of zero or more PlayerInfos whose names match the pattern. </returns>
        [NotNull]
        IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern, int limit );


        /// <summary> Initializes the provider, and allocates PlayerInfo objects for all players. </summary>
        [CanBeNull]
        IEnumerable<PlayerInfo> Load();


        /// <summary> Saves the whole database. </summary>
        void Save();


        /// <summary> Changes ranks of all players in one transaction. </summary>
        void MassRankChange( [NotNull] Player player, [NotNull] Rank from, [NotNull] Rank to, [NotNull] string reason );


        /// <summary> Swaps records of two players in one transaction. </summary>
        void SwapInfo( [NotNull] PlayerInfo player1, [NotNull] PlayerInfo player2 );
    }


    public enum PlayerDBProviderType {
        Flatfile,
        MySql
    }
}