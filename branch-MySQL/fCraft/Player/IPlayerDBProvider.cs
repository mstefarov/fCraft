using System.Collections.Generic;
using System.Net;
using JetBrains.Annotations;

namespace fCraft {
    public interface IPlayerDBProvider {
        object SyncRoot { get; }

        /// <summary> Preloads the whole database. </summary>
        [CanBeNull]
        IEnumerable<PlayerInfo> Load();
        /// <summary> Saves the whole database. </summary>
        void Save();

        /// <summary> Adds a new PlayerInfo entry for a player. </summary>
        [NotNull]
        PlayerInfo AddPlayer( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank, RankChangeType rankChangeType );

        /// <summary> Adds a new PlayerInfo entry for a player. </summary>
        [NotNull]
        PlayerInfo AddUnrecognizedPlayer( [NotNull] string name, [NotNull] Rank startingRank, RankChangeType rankChangeType );

        /// <summary> Adds a new PlayerInfo entry for a non-player. </summary>
        [NotNull]
        PlayerInfo AddSuper( ReservedPlayerIDs id, [NotNull] string name, [NotNull] Rank rank );

        /// <summary> Removes a PlayerInfo entry. </summary>
        void Remove( [NotNull] PlayerInfo playerInfo );

        /// <summary> Reloads all data for given records from the backend in one transaction. </summary>
        void PullChanges( [NotNull] params PlayerInfo[] playerInfo );
        /// <summary> Writes out all data from the given records to the backend in one transaction. </summary>
        void PushChanges( [NotNull] params PlayerInfo[] playerInfo );

        /// <summary> Finds player by exact name. </summary>
        [CanBeNull]
        PlayerInfo FindExact( [NotNull] string fullName );

        bool FindOneByPartialName( [NotNull] string partialName, [CanBeNull] out PlayerInfo result );

        /// <summary> Finds players by IP address. </summary>
        [NotNull]
        IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address, int limit );
        /// <summary> Finds players by partial name (prefix). </summary>
        [NotNull]
        IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string partialName, int limit );
        /// <summary> Finds players by given wildcard pattern. </summary>
        [NotNull]
        IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern, int limit );

        /// <summary> Changes ranks of all players in one transaction. </summary>
        void MassRankChange( [NotNull] Player player, [NotNull] Rank from, [NotNull] Rank to, [NotNull] string reason );

        /// <summary> Swaps records of two players in one transaction. </summary>
        void SwapInfo( [NotNull] PlayerInfo player1, [NotNull] PlayerInfo player2 );
    }
}
