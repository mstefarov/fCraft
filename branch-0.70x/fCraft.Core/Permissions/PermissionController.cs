// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft {
    public sealed class PermissionController {
        public PermissionController( [NotNull] PermissionController parent ) {
            if( parent == null ) throw new ArgumentNullException( "parent" );
            Parent = parent;
            Node = parent.Node;
        }


        public PermissionController( [NotNull] PermissionNode node ) {
            if( node == null ) throw new ArgumentNullException( "node" );
            Node = node;
        }


        public PermissionController Parent { get; private set; }
        public PermissionNode Node { get; private set; }

        readonly Dictionary<Rank, PermissionLimits> rankInclusions = new Dictionary<Rank, PermissionLimits>();
        readonly HashSet<Rank> rankExclusions = new HashSet<Rank>();
        readonly Dictionary<string, PermissionLimits> playerInclusions = new Dictionary<string, PermissionLimits>();
        readonly HashSet<string> playerExclusions = new HashSet<string>();


        public bool Can( PlayerInfo player ) {
            return GetLimit( player ) != null;
        }


        public bool Can( PlayerInfo player, PlayerInfo targetPlayer ) {
            return false; // todo
        }


        public bool Can( PlayerInfo player, Rank targetRank ) {
            return false; // todo
        }


        public bool Can( PlayerInfo player, int quantity ) {
            if( ( Node.Flags & PermissionFlags.NeedsQuantity ) == 0 ) {
                throw new PermissionCheckException( "Quantity limit is not applicable to " + Node.Name );
            }
            PermissionLimits limits = GetLimit( player );
            if( limits == null ) {
                return false;
            } else {
                return ( limits.MaxQuantity <= quantity );
            }
        }


        public bool CanGrant( PlayerInfo player ) {
            PermissionLimits limits = GetLimit( player );
            if( limits == null ) {
                return false;
            } else {
                return limits.CanGrant;
            }
        }


        public bool CanRevoke( PlayerInfo player ) {
            PermissionLimits limits = GetLimit( player );
            if( limits == null ) {
                return false;
            } else {
                return limits.CanRevoke;
            }
        }


        public PermissionLimits GetLimit( PlayerInfo player ) {
            return GetPlayerLimit( player ) ?? GetRankLimit( player.Rank );
        }


        PermissionLimits GetPlayerLimit( PlayerInfo player ) {
            PermissionLimits limiter;
            if( playerInclusions.TryGetValue( player.Name, out limiter ) ) {
                return limiter;
            } else if( Parent != null ) {
                return Parent.GetPlayerLimit( player );
            } else {
                return null;
            }
        }


        PermissionLimits GetRankLimit( Rank rank ) {
            PermissionLimits limiter;
            if( rankInclusions.TryGetValue( rank, out limiter ) ) {
                return limiter;
            } else if( Parent != null ) {
                return Parent.GetRankLimit( rank );
            } else {
                return null;
            }
        }


        public bool Can( PlayerInfo player, PlayerInfo targetPlayer, Rank targetRank ) {
            return false; // todo
        }


        public bool Can( PlayerInfo player, PlayerInfo targetPlayer, int quantity ) {
            return false; // todo
        }


        #region Including / Excluding

        public PermissionOverride Include( Rank rank, PermissionLimits limits ) {
            if( rankInclusions.ContainsKey( rank ) ) {
                return PermissionOverride.Allow;

            } else if( rankExclusions.Contains( rank ) ) {
                rankExclusions.Remove( rank );
                return PermissionOverride.Deny;

            } else {
                rankInclusions.Add( rank, limits );
                return PermissionOverride.None;
            }
        }


        public PermissionOverride Exclude( Rank rank ) {
            if( rankInclusions.Remove( rank ) ) {
                return PermissionOverride.Allow;

            } else if( rankExclusions.Contains( rank ) ) {
                return PermissionOverride.Deny;

            } else {
                rankExclusions.Add( rank );
                return PermissionOverride.None;
            }
        }


        public PermissionOverride Include( string name, PermissionLimits limits ) {
            if( playerInclusions.ContainsKey( name ) ) {
                return PermissionOverride.Allow;

            } else if( playerExclusions.Contains( name ) ) {
                playerExclusions.Remove( name );
                return PermissionOverride.Deny;

            } else {
                playerInclusions.Add( name, limits );
                return PermissionOverride.None;
            }
        }


        public PermissionOverride Exclude( string name ) {
            if( playerInclusions.Remove( name ) ) {
                return PermissionOverride.Allow;

            } else if( playerExclusions.Contains( name ) ) {
                return PermissionOverride.Deny;

            } else {
                playerExclusions.Add( name );
                return PermissionOverride.None;
            }
        }

        #endregion
    }


    public class PermissionCheckException : Exception {
        public PermissionCheckException( string message ) : base( message ) {}
    }
}