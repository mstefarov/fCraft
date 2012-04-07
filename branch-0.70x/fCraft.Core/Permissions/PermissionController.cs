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

        readonly HashSet<Rank> rankInclusions = new HashSet<Rank>();
        readonly HashSet<Rank> rankExclusions = new HashSet<Rank>();
        readonly Dictionary<Rank, PermissionLimits> rankLimitOverrides = new Dictionary<Rank, PermissionLimits>();
        readonly HashSet<string> playerInclusions = new HashSet<string>();
        readonly HashSet<string> playerExclusions = new HashSet<string>();
        readonly Dictionary<string, PermissionLimits> playerLimitOverrides = new Dictionary<string, PermissionLimits>();


        public bool Can( PlayerInfo player ) {
            if( playerInclusions.Contains( player.Name ) ) {
                return true;

            } else if( rankInclusions.Contains( player.Rank ) ) {
                return true;

            } else if( Parent != null ) {
                return Parent.Can( player );

            } else {
                return false;
            }
        }


        public bool Can( PlayerInfo player, PlayerInfo targetPlayer ) {
            if( !Can( player ) ) return false;
            PermissionLimits limit = GetLimits( player );
        }


        public bool Can( PlayerInfo player, Rank targetRank ) {
            return false; // todo
        }


        public bool Can( PlayerInfo player, int quantity ) {
            return false; // todo
        }


        public bool Can( PlayerInfo player, PlayerInfo targetPlayer, Rank targetRank ) {
            return false; // todo
        }


        public bool Can( PlayerInfo player, PlayerInfo targetPlayer, int quantity ) {
            return false; // todo
        }


        public PermissionOverride Include( Rank rank ) {
            if( rankInclusions.Contains( rank ) ) {
                return PermissionOverride.Allow;

            } else if( rankExclusions.Contains( rank ) ) {
                rankExclusions.Remove( rank );
                return PermissionOverride.Deny;

            } else {
                rankInclusions.Add( rank );
                return PermissionOverride.None;
            }
        }


        public PermissionOverride Exclude( Rank rank ) {
            if( rankInclusions.Contains( rank ) ) {
                rankInclusions.Remove( rank );
                return PermissionOverride.Allow;

            } else if( rankExclusions.Contains( rank ) ) {
                return PermissionOverride.Deny;

            } else {
                rankInclusions.Add( rank );
                return PermissionOverride.None;
            }
        }


        public PermissionOverride Include( string name ) {
            if( playerInclusions.Contains( name ) ) {
                return PermissionOverride.Allow;

            } else if( playerExclusions.Contains( name ) ) {
                playerExclusions.Remove( name );
                return PermissionOverride.Deny;

            } else {
                playerInclusions.Add( name );
                return PermissionOverride.None;
            }
        }


        public PermissionOverride Exclude( string name ) {
            if( playerInclusions.Contains( name ) ) {
                playerInclusions.Remove( name );
                return PermissionOverride.Allow;
            } else if( playerExclusions.Contains( name ) ) {
                return PermissionOverride.Deny;
            } else {
                playerInclusions.Add( name );
                return PermissionOverride.None;
            }
        }


        public PermissionLimits GetLimits( PlayerInfo player ) {
            PermissionLimits result;
            if( playerLimitOverrides.TryGetValue( player.Name, out result ) ) {
                return result;

            } else if( rankLimitOverrides.TryGetValue( player.Rank, out result ) ) {
                return result;

            } else if( Parent != null ) {
                return Parent.GetLimits( player );

            } else {
                return null;
            }
        }


        public PermissionLimits GetLimit( string name ) {
            PermissionLimits result;
            if( playerLimitOverrides.TryGetValue( name, out result ) ) {
                return result;
            } else {
                return null;
            }
        }
    }
}