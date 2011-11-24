using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    class FlatfilePlayerDBProvider : IPlayerDBProvider {

        public IEnumerable<PlayerInfo> Load() {
            throw new NotImplementedException();
        }

        public void Save() {
            throw new NotImplementedException();
        }

        public void Add( PlayerInfo playerInfo ) {
            throw new NotImplementedException();
        }

        public void Remove( PlayerInfo playerInfo ) {
            throw new NotImplementedException();
        }

        public void PullChanges( params PlayerInfo[] playerInfo ) {
            throw new NotImplementedException();
        }

        public void PushChanges( params PlayerInfo[] playerInfo ) {
            throw new NotImplementedException();
        }

        public PlayerInfo FindExact( string fullName ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByIP( System.Net.IPAddress address, int limit ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByPartialName( string partialName, int limit ) {
            throw new NotImplementedException();
        }

        public IEnumerable<PlayerInfo> FindByPattern( string pattern, int limit ) {
            throw new NotImplementedException();
        }

        public void MassRankChange( Player player, Rank from, Rank to, string reason ) {
            throw new NotImplementedException();
        }

        public void SwapInfo( PlayerInfo player1, PlayerInfo player2 ) {
            throw new NotImplementedException();
        }
    }
}
