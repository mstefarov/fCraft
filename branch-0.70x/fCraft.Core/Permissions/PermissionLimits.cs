// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using JetBrains.Annotations;

namespace fCraft {
    public sealed class PermissionLimits {
        public PermissionLimits( [NotNull] PermissionController controller ) {
            if( controller == null ) throw new ArgumentNullException( "controller" );
            Controller = controller;
        }

        public PermissionController Controller { get; private set; }
        public int Quantity;
        public Rank[] IncludedRanks;
        public Rank[] ExcludedRanks;
    }
}