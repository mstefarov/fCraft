// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace fCraft {

    /// <summary> Controller for setting and checking per-rank and per-player permissions. </summary>
    public sealed class SecurityController : ICloneable {

        readonly Dictionary<string, PlayerInfo> includedPlayers = new Dictionary<string, PlayerInfo>();
        readonly Dictionary<string, PlayerInfo> excludedPlayers = new Dictionary<string, PlayerInfo>();

        public PlayerListCollection ExceptionList { get; private set; }
        readonly object playerPermissionListLock = new object();

        private Rank minRank;
        public Rank MinRank {
            get {
                return minRank ?? RankManager.LowestRank;
            }
            set {
                minRank = value;
            }
        }
        // TODO: maxRank;
        public bool NoRankRestriction {
            get { return (minRank == null); }
        }


        public SecurityController() {
            UpdatePlayerListCache();
        }


        public SecurityController( SecurityController other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            if( other.NoRankRestriction ) {
                MinRank = null;
            } else {
                MinRank = other.MinRank;
            }
            includedPlayers = new Dictionary<string, PlayerInfo>( other.includedPlayers );
            excludedPlayers = new Dictionary<string, PlayerInfo>( other.excludedPlayers );
            UpdatePlayerListCache();
        }


        public object Clone() {
            return new SecurityController( this );
        }


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
            if( info == null ) throw new ArgumentNullException( "info" );
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
            if( info == null ) throw new ArgumentNullException( "info" );
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
            if( info == null ) throw new ArgumentNullException( "info" );
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
            if( info == null ) throw new ArgumentNullException( "info" );
            PlayerListCollection listCache = ExceptionList;
            for( int i = 0; i < listCache.Excluded.Length; i++ ) {
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


        public void PrintDescription( Player player, IClassy target, string noun, string verb ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( noun == null ) throw new ArgumentNullException( "noun" );
            if( verb == null ) throw new ArgumentNullException( "verb" );
            PlayerListCollection list = ExceptionList;

            noun = Char.ToUpper( noun[0] ) + noun.Substring( 1 ); // capitalize first letter

            StringBuilder message = new StringBuilder();

            if( NoRankRestriction ) {
                message.AppendFormat( "{0} {1}&S can be {2} by anyone",
                                      noun, target.GetClassyName(), verb );

            } else {
                message.AppendFormat( "{0} {1}&S can only be {2} by {3}+&S",
                                      noun, target.GetClassyName(),
                                      verb, MinRank.GetClassyName() );
            }

            if( list.Included.Length > 0 ) {
                message.AppendFormat( " and {0}&S", list.Included.JoinToClassyString() );
            }

            if( list.Excluded.Length > 0 ) {
                message.AppendFormat( ", except {0}", list.Excluded.JoinToClassyString() );
            }

            message.Append( '.' );
            player.Message( message.ToString() );
        }


        public bool HasRestrictions() {
            return MinRank > RankManager.LowestRank ||
                   ExceptionList.Excluded.Length > 0;
        }


        #region XML Serialization

        public const string XmlRootElementName = "PermissionController";


        public SecurityController( XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            if( el.Element( "minRank" ) != null ) {
                minRank = RankManager.ParseRank( el.Element( "minRank" ).Value );
            } else {
                minRank = null;
            }

            //maxRank = RankManager.ParseRank( root.Element( "maxRank" ).Value );
            foreach( XElement player in el.Elements( "included" ) ) {
                if( !Player.IsValidName( player.Value ) ) continue;
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( player.Value );
                if( info != null ) Include( info );
            }

            foreach( XElement player in el.Elements( "excluded" ) ) {
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
            if( tagName == null ) throw new ArgumentNullException( "tagName" );

            XElement root = new XElement( tagName );
            if( !NoRankRestriction ) {
                root.Add( new XElement( "minRank", MinRank.GetFullName() ) );
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
            MinRank = RankManager.LowestRank;
            ResetIncludedList();
            ResetExcludedList();
        }

        #endregion


        public sealed class PlayerListCollection : ICloneable {
            public PlayerListCollection() { }
            public PlayerListCollection( PlayerListCollection other ) {
                if( other == null ) throw new ArgumentNullException( "other" );
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
    }


    #region Enums

    /// <summary> Indicates what kind of per-entity override/exception is defined in a security controller. </summary>
    public enum PermissionOverride {
        /// <summary> No permission exception. </summary>
        None,

        /// <summary> Entity is explicitly allowed / whitelisted. </summary>
        Allow,

        /// <summary> Entity is explicitly denied / blacklisted. </summary>
        Deny
    }


    /// <summary> Possible results of a SecurityController permission check. </summary>
    public enum SecurityCheckResult {
        /// <summary> Allowed, no permission involved. </summary>
        Allowed,

        /// <summary> Denied, rank too low. </summary>
        RankTooLow,

        /// <summary> Denied, rank too high (not yet implemented). </summary>
        RankTooHigh,

        /// <summary> Allowed, this entity was explicitly allowed / whitelisted. </summary>
        WhiteListed,

        /// <summary> Denied, this entity was explicitly denied / blacklisted. </summary>
        BlackListed
    }

    #endregion
}