// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;


namespace fCraft {

    public enum PermissionOverride {
        None,
        Allow,
        Deny
    }


    public enum SecurityCheckResult {
        Allowed,
        RankTooLow,
        RankTooHigh,
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

        Dictionary<string, PlayerInfo> includedPlayers = new Dictionary<string, PlayerInfo>();
        Dictionary<string, PlayerInfo> excludedPlayers = new Dictionary<string, PlayerInfo>();

        public PlayerListCollection exceptionList { get; private set; }

        private Rank _minRank;//, _maxRank;

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
                exceptionList = new PlayerListCollection {
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

        public bool Check( PlayerInfo info ) {
            PlayerListCollection listCache = exceptionList;
            for( int i = 0; i < listCache.excluded.Length; i++ ) {
                if( info == listCache.excluded[i] ) return false;
            }

            if( info.rank >= minRank /*&& player.info.rank <= maxRank*/ ) return true; // TODO: implement maxrank

            for( int i = 0; i < exceptionList.included.Length; i++ ) {
                if( info == exceptionList.included[i] ) return true;
            }

            return false;
        }


        public SecurityCheckResult CheckDetailed( PlayerInfo info ) {
            PlayerListCollection listCache = exceptionList;
            for( int i = 0; i < listCache.excluded.Length; i++ ) {
                if( info == listCache.excluded[i] )
                    return SecurityCheckResult.BlackListed;
            }

            if( info.rank >= minRank /*&& player.info.rank <= maxRank*/ ) // TODO: implement maxrank
                return SecurityCheckResult.Allowed;

            for( int i = 0; i < listCache.included.Length; i++ ) {
                if( info == listCache.included[i] )
                    return SecurityCheckResult.WhiteListed;
            }

            return SecurityCheckResult.RankTooLow;
        }


        #region XML Serialization

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
            UpdatePlayerListCache();
        }

        public XElement Serialize() {
            return Serialize( XmlRootElementName );
        }

        public XElement Serialize( string tagName ) {
            XElement root = new XElement( tagName );

            if( minRank != null ) root.Add( new XElement( "minRank", minRank ) );
            //if(maxRank!=null) root.Add( new XElement( "maxRank", maxRank ) );

            lock( playerPermissionListLock ) {
                foreach( string playerName in includedPlayers.Keys ) {
                    root.Add( new XElement( "included", playerName ) );
                }
                foreach( string playerName in excludedPlayers.Keys ) {
                    root.Add( new XElement( "excluded", playerName ) );
                }
            }
            return root;
        }

        #endregion

        public void PrintDescription( Player player, IClassy world, string noun, string verb ) {
            SecurityController.PlayerListCollection list = exceptionList;

            noun = Char.ToUpper( noun[0] ) + noun.Substring( 1 ); // capitalize first letter

            StringBuilder message = new StringBuilder();

            if( minRank == RankList.LowestRank ) {
                message.AppendFormat( "{0} {1}&S can be {2} by anyone",
                                      noun, world.GetClassyName(), verb );

            } else {
                message.AppendFormat( "{0} {1}&S can only be {2} by {3}+&S",
                                      noun, world.GetClassyName(),
                                      verb, minRank.GetClassyName() );
            }

            if( list.included.Length > 0 ) {
                message.AppendFormat( " and {0}&S", PlayerInfo.PlayerInfoArrayToString( list.included ) );
            }

            if( list.excluded.Length > 0 ) {
                message.AppendFormat( ", except {0}", PlayerInfo.PlayerInfoArrayToString( list.excluded ) );
            }

            message.Append( '.' );
            player.Message( message.ToString() );
        }

        public void ClearIncludedList() {
            lock( playerPermissionListLock ) {
                includedPlayers.Clear();
                UpdatePlayerListCache();
            }
        }

        public void ClearExcludedList() {
            lock( playerPermissionListLock ) {
                excludedPlayers.Clear();
                UpdatePlayerListCache();
            }
        }

        public bool HasRestrictions() {
            return minRank > RankList.LowestRank ||
                   exceptionList.excluded.Length > 0;
        }

        public void Reset() {
            minRank = RankList.LowestRank;
            ClearIncludedList();
            ClearExcludedList();
        }
    }
}