// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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

        private Rank _minRank;
        public Rank MinRank {
            get {
                if( _minRank != null ) {
                    return _minRank;
                } else {
                    return RankList.LowestRank;
                }
            }
            set {
                _minRank = value;
            }
        }
        // TODO: maxRank;
        public bool NoRankRestriction {
            get { return (_minRank == null); }
        }

        readonly object playerPermissionListLock = new object();


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
            if( listCache.excluded.Any( t => (info == t) ) ) {
                return false;
            }

            if( info.rank >= MinRank /*&& player.info.rank <= maxRank*/ ) return true; // TODO: implement maxrank

            return exceptionList.included.Any( t => (info == t) );
        }


        public SecurityCheckResult CheckDetailed( PlayerInfo info ) {
            PlayerListCollection listCache = exceptionList;
            if( listCache.excluded.Any( t => info == t ) ) {
                return SecurityCheckResult.BlackListed;
            }

            if( info.rank >= MinRank /*&& player.info.rank <= maxRank*/ ) // TODO: implement maxrank
                return SecurityCheckResult.Allowed;

            if( listCache.included.Any( t => info == t ) ) {
                return SecurityCheckResult.WhiteListed;
            }

            return SecurityCheckResult.RankTooLow;
        }


        public void PrintDescription( Player player, IClassy world, string noun, string verb ) {
            PlayerListCollection list = exceptionList;

            noun = Char.ToUpper( noun[0] ) + noun.Substring( 1 ); // capitalize first letter

            StringBuilder message = new StringBuilder();

            if( NoRankRestriction ) {
                message.AppendFormat( "{0} {1}&S can be {2} by anyone",
                                      noun, world.GetClassyName(), verb );

            } else {
                message.AppendFormat( "{0} {1}&S can only be {2} by {3}+&S",
                                      noun, world.GetClassyName(),
                                      verb, MinRank.GetClassyName() );
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


        public bool HasRestrictions() {
            return MinRank > RankList.LowestRank ||
                   exceptionList.excluded.Length > 0;
        }


        #region XML Serialization

        public const string XmlRootElementName = "PermissionController";


        public SecurityController( XElement root ) {
            if( root.Element( "minRank" ) != null ) {
                _minRank = RankList.ParseRank( root.Element( "minRank" ).Value );
            } else {
                _minRank = null;
            }

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
            if( !NoRankRestriction ) {
                root.Add( new XElement( "minRank", MinRank ) );
            }
            //root.Add( new XElement( "maxRank", maxRank ) );

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


        #region Resetting

        public void ResetIncludedList() {
            lock( playerPermissionListLock ) {
                includedPlayers.Clear();
                UpdatePlayerListCache();
            }
        }


        public void ResetExcludedList() {
            lock( playerPermissionListLock ) {
                excludedPlayers.Clear();
                UpdatePlayerListCache();
            }
        }


        public void Reset() {
            MinRank = RankList.LowestRank;
            ResetIncludedList();
            ResetExcludedList();
        }

        #endregion
    }
}