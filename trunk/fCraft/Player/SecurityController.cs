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


    public sealed class SecurityController : ICloneable {
        public sealed class PlayerListCollection : ICloneable {
            public PlayerListCollection() { }
            public PlayerListCollection( PlayerListCollection other ) {
                Included = (PlayerInfo[])other.Included.Clone();
                Excluded = (PlayerInfo[])other.Excluded.Clone();
            }
            // keeping both lists on one object allows lock-free synchronization
            public PlayerInfo[] Included;
            public PlayerInfo[] Excluded;
            public object Clone() {
                return new PlayerListCollection( this );
            }
        }

        public SecurityController() {
            UpdatePlayerListCache();
        }

        public SecurityController( SecurityController other ) {
            if( other.NoRankRestriction ) {
                MinRank = null;
            } else {
                MinRank = other.MinRank;
            }
            includedPlayers = new Dictionary<string, PlayerInfo>( other.includedPlayers );
            excludedPlayers = new Dictionary<string, PlayerInfo>( other.excludedPlayers );
        }

        public object Clone() {
            return new SecurityController( this );
        }


        readonly Dictionary<string, PlayerInfo> includedPlayers = new Dictionary<string, PlayerInfo>();
        readonly Dictionary<string, PlayerInfo> excludedPlayers = new Dictionary<string, PlayerInfo>();

        public PlayerListCollection ExceptionList { get; private set; }

        private Rank minRank;
        public Rank MinRank {
            get {
                if( minRank != null ) {
                    return minRank;
                } else {
                    return RankList.LowestRank;
                }
            }
            set {
                minRank = value;
            }
        }
        // TODO: maxRank;
        public bool NoRankRestriction {
            get { return (minRank == null); }
        }

        readonly object playerPermissionListLock = new object();


        public void UpdatePlayerListCache() {
            lock( playerPermissionListLock ) {
                ExceptionList = new PlayerListCollection {
                    Included = includedPlayers.Values.ToArray(),
                    Excluded = excludedPlayers.Values.ToArray()
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
                    excludedPlayers.Remove( info.Name.ToLower() );
                    UpdatePlayerListCache();
                    return PermissionOverride.Deny;
                } else {
                    includedPlayers.Add( info.Name.ToLower(), info );
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
                    includedPlayers.Remove( info.Name.ToLower() );
                    UpdatePlayerListCache();
                    return PermissionOverride.Allow;
                } else {
                    excludedPlayers.Add( info.Name.ToLower(), info );
                    UpdatePlayerListCache();
                    return PermissionOverride.None;
                }
            }
        }


        public bool Check( PlayerInfo info ) {
            PlayerListCollection listCache = ExceptionList;
            for( int i = 0; i < listCache.Excluded.Length; i++ ) {
                if( listCache.Excluded[i] == info ) {
                    return false;
                }
            }

            if( info.Rank >= MinRank /*&& player.info.rank <= maxRank*/ ) return true; // TODO: implement maxrank

            for( int i = 0; i < listCache.Included.Length; i++ ) {
                if( listCache.Included[i] == info ) {
                    return true;
                }
            }

            return false;
        }


        public SecurityCheckResult CheckDetailed( PlayerInfo info ) {
            PlayerListCollection listCache = ExceptionList;
            for( int i=0; i<listCache.Excluded.Length; i++){
                if( listCache.Excluded[i] == info ) {
                    return SecurityCheckResult.BlackListed;
                }
            }

            if( info.Rank >= MinRank /*&& player.info.rank <= maxRank*/ ) // TODO: implement maxrank
                return SecurityCheckResult.Allowed;

            for( int i = 0; i < listCache.Included.Length; i++ ) {
                if( listCache.Included[i] == info ) {
                    return SecurityCheckResult.WhiteListed;
                }
            }

            return SecurityCheckResult.RankTooLow;
        }


        public void PrintDescription( Player player, IClassy world, string noun, string verb ) {
            PlayerListCollection list = ExceptionList;

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

            if( list.Included.Length > 0 ) {
                message.AppendFormat( " and {0}&S", PlayerInfo.PlayerInfoArrayToString( list.Included ) );
            }

            if( list.Excluded.Length > 0 ) {
                message.AppendFormat( ", except {0}", PlayerInfo.PlayerInfoArrayToString( list.Excluded ) );
            }

            message.Append( '.' );
            player.Message( message.ToString() );
        }


        public bool HasRestrictions() {
            return MinRank > RankList.LowestRank ||
                   ExceptionList.Excluded.Length > 0;
        }


        #region XML Serialization

        public const string XmlRootElementName = "PermissionController";


        public SecurityController( XElement root ) {
            if( root.Element( "minRank" ) != null ) {
                minRank = RankList.ParseRank( root.Element( "minRank" ).Value );
            } else {
                minRank = null;
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