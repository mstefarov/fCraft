// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


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


    public sealed class SecurityController {
        public class PlayerListCollection {
            // keeping both lists on one object allows lock-free synchronization
            public PlayerInfo[] included;
            public PlayerInfo[] excluded;
        }

        public SecurityController() {
            UpdatePlayerListCache();
        }

        public PlayerListCollection permissionList { get; private set; }

        Dictionary<string, PlayerInfo> includedPlayers = new Dictionary<string, PlayerInfo>();
        Dictionary<string, PlayerInfo> excludedPlayers = new Dictionary<string, PlayerInfo>();

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

        object playerPermissionListLock = new object();

        public void UpdatePlayerListCache() {
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

        public const string XmlRootElementName = "PermissionController";

        public SecurityController( XElement root ) {
            minRank = RankList.ParseRank( root.Element( "minRank" ).Value );
            //maxRank = RankList.ParseRank( root.Element( "maxRank" ).Value );
            foreach( XElement player in root.Elements( "included" ) ) {
                if( !Player.IsValidName( player.Value ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player.Value );
                if( info != null ) Include( info );
            }
            foreach( XElement player in root.Elements( "excluded" ) ) {
                if( !Player.IsValidName( player.Value ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player.Value );
                if( info != null ) Exclude( info );
            }
        }

        public XElement Serialize() {
            XElement root = new XElement( XmlRootElementName );

            if( minRank != null ) root.Add( new XElement( "minRank", minRank ) );
            //if(maxRank!=null) root.Add( new XElement( "maxRank", maxRank ) );

            lock( playerPermissionListLock ) {
                foreach( string name in includedPlayers.Keys ) {
                    root.Add( new XElement( "included", name ) );
                }
                foreach( string name in excludedPlayers.Keys ) {
                    root.Add( new XElement( "excluded", name ) );
                }
            }
            return root;
        }
    }
}