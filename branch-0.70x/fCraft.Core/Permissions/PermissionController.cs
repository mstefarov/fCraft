// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System.Collections.Generic;

namespace fCraft {
    class PermissionController {
        public PermissionController( PermissionNode node ) {
            Node = node;
        }

        public PermissionNode Node { get; private set; }
        public Rank[] RankInclusions { get; private set; }
        public Rank[] RankExclusions { get; private set; }
        Dictionary<Rank, PermissionLimits> RankLimitOverrides;
        public PlayerInfo[] PlayerInclusions { get; private set; }
        public PlayerInfo[] PlayerExclusions { get; private set; }
        Dictionary<PlayerInfo, PermissionLimits> PlayerLimitOverrides;


        public bool Can( PlayerInfo player ) {
            return false; // todo
        }

        public bool Can( PlayerInfo player, PlayerInfo targetPlayer ) {
            return false; // todo
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
    }
}