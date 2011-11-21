using System.Collections.Generic;
using System.Net;

namespace fCraft {
    public interface IPlayerDBProvider {
        /// <summary> Preloads the whole database. </summary>
        IEnumerable<PlayerInfo> Load();
        /// <summary> Saves the whole database. </summary>
        void Save();

        /// <summary> Adds a new PlayerInfo entry. </summary>
        void Add( PlayerInfo playerInfo );
        /// <summary> Removes a PlayerInfo entry. </summary>
        void Remove( PlayerInfo playerInfo );

        /// <summary> Reloads all data for given records from the backend in one transaction. </summary>
        void PullChanges( params PlayerInfo[] playerInfo );
        /// <summary> Writes out all data from the given records to the backend in one transaction. </summary>
        void PushChanges( params PlayerInfo[] playerInfo );

        /// <summary> Finds player by exact name. </summary>
        PlayerInfo FindExact( string fullName );

        /// <summary> Finds players by IP address. </summary>
        IEnumerable<PlayerInfo> FindByIP( IPAddress address, int limit );
        /// <summary> Finds players by partial name (prefix). </summary>
        IEnumerable<PlayerInfo> FindByPartialName( string partialName, int limit );
        /// <summary> Finds players by given wildcard pattern. </summary>
        IEnumerable<PlayerInfo> FindByPattern( string pattern, int limit );

        /// <summary> Changes ranks of all players in one transaction. </summary>
        void MassRankChange( Player player, Rank from, Rank to, string reason );

        /// <summary> Swaps records of two players in one transaction. </summary>
        void SwapInfo( PlayerInfo player1, PlayerInfo player2 );
    }
}
