// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft {
    public sealed class PermissionLimits {
        public PermissionLimits( [NotNull] PermissionController controller ) {
            if( controller == null ) throw new ArgumentNullException( "controller" );
            Controller = controller;
        }

        public PermissionController Controller { get; private set; }
        public int MaxQuantity { get; set; }
        public bool CanGrant { get; set; }
        public bool CanRevoke { get; set; }
        readonly HashSet<Rank> includedRanks = new HashSet<Rank>();
        readonly HashSet<Rank> excludedRanks = new HashSet<Rank>();

        public PermissionOverride CanTarget( Rank targetRank ) {
            if( includedRanks.Contains( targetRank ) ) {
                return PermissionOverride.Allow;
            } else if( excludedRanks.Contains( targetRank ) ) {
                return PermissionOverride.Deny;
            } else {
                return PermissionOverride.None;
            }
        }
    }
}