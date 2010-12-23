using System.Collections.Generic;
using System.Linq;

namespace fCraft {

    public enum PermissionOverride {
        None,
        Allow,
        Deny
    }


    public enum PermissionType {
        Allowed,
        Denied,
        WhiteListed,
        BlackListed
    }


    public class PermissionController {
        public class PlayerListCollection {
            // keeping both lists on one object allows lock-free synchronization
            public PlayerInfo[] included;
            public PlayerInfo[] excluded;
        }

        public PlayerListCollection permissionList { get; private set; }

        protected Dictionary<string, PlayerInfo> includedPlayers = new Dictionary<string, PlayerInfo>();
        protected Dictionary<string, PlayerInfo> excludedPlayers = new Dictionary<string, PlayerInfo>();

        private Rank _minRank, _maxRank;

        public Rank minRank {
            get { return _minRank; }
            set {
                //if( value > _maxRank ) {
                //    _maxRank = value;
                //}
                _minRank = value;
            }
        }

        /*public Rank maxRank {
            get {
                return _maxRank;
            }
            set {
                if( value < _minRank ) {
                    _minRank = value;
                }
                _maxRank = value;
            }
        }*/

        protected object playerPermissionListLock = new object();

        protected void UpdatePlayerListCache() {
            lock( playerPermissionListLock ) {
                permissionList = new PlayerListCollection {
                    included = includedPlayers.Values.ToArray(),
                    excluded = excludedPlayers.Values.ToArray()
                };
            }
        }

        // returns the PREVIOUS state of the player
        public PermissionOverride Include( PlayerInfo info ) {
            lock( playerPermissionListLock ) {
                if( includedPlayers.ContainsValue( info ) ) {
                    UpdatePlayerListCache();
                    return PermissionOverride.Allow;
                } else if( excludedPlayers.ContainsValue( info ) ) {
                    excludedPlayers.Remove( info.name.ToLower() );
                    UpdatePlayerListCache();
                    return PermissionOverride.Deny;
                } else {
                    includedPlayers.Add( info.name.ToLower(), info );
                    UpdatePlayerListCache();
                    return PermissionOverride.None;
                }
            }
        }

        // returns the PREVIOUS state of the player
        public PermissionOverride Exclude( PlayerInfo info ) {
            lock( playerPermissionListLock ) {
                if( excludedPlayers.ContainsValue( info ) ) {
                    UpdatePlayerListCache();
                    return PermissionOverride.Deny;
                } else if( includedPlayers.ContainsValue( info ) ) {
                    includedPlayers.Remove( info.name.ToLower() );
                    UpdatePlayerListCache();
                    return PermissionOverride.Allow;
                } else {
                    excludedPlayers.Add( info.name.ToLower(), info );
                    UpdatePlayerListCache();
                    return PermissionOverride.None;
                }
            }
        }

        public bool CanBuild( Player player ) {
            PlayerListCollection listCache = permissionList;
            for( int i = 0; i < listCache.excluded.Length; i++ ) {
                if( player.info == listCache.excluded[i] ) return false;
            }

            if( player.info.rank >= minRank /*&& player.info.rank <= maxRank*/ ) return true; // TODO: implement maxrank

            for( int i = 0; i < permissionList.included.Length; i++ ) {
                if( player.info == permissionList.included[i] ) return true;
            }

            return false;
        }


        public PermissionType CanBuildDetailed( Player player ) {
            PlayerListCollection listCache = permissionList;
            for( int i = 0; i < listCache.excluded.Length; i++ ) {
                if( player.info == listCache.excluded[i] )
                    return PermissionType.BlackListed;
            }

            if( player.info.rank >= minRank /*&& player.info.rank <= maxRank*/ ) // TODO: implement maxrank
                return PermissionType.Allowed;

            for( int i = 0; i < listCache.included.Length; i++ ) {
                if( player.info == listCache.included[i] )
                    return PermissionType.WhiteListed;
            }

            return PermissionType.Denied;
        }
    }
}
