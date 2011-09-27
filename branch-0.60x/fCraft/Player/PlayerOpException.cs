// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft {
    sealed class PlayerOpException : Exception {
        public PlayerOpException( [NotNull] Player player, PlayerInfo target,
                                  PlayerOpExceptionCode errorCode,
                                  [NotNull] string message, [NotNull] string messageColored )
            : base( message ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( messageColored == null ) throw new ArgumentNullException( "messageColored" );
            Player = player;
            Target = target;
            ErrorCode = errorCode;
            MessageColored = messageColored;
        }

        public Player Player { get; private set; }
        public PlayerInfo Target { get; private set; }
        public PlayerOpExceptionCode ErrorCode { get; private set; }
        public string MessageColored { get; private set; }


        [TerminatesProgram]
        public static void CannotTargetSelf( [NotNull] Player player, PlayerInfo target, [NotNull] string action ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( action == null ) throw new ArgumentNullException( "action" );
            string msg = String.Format( "You cannot {0} yourself.", action );
            string colorMsg = String.Format( "&WYou cannot {0} yourself.", action );
            throw new PlayerOpException( player, target, PlayerOpExceptionCode.CannotTargetSelf, msg, colorMsg );
        }


        [TerminatesProgram]
        public static void PermissionMissing( [NotNull] Player player, PlayerInfo target,
                                              [NotNull] string action, [NotNull] params Permission[] permissions ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( action == null ) throw new ArgumentNullException( "action" );
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            string msg = String.Format( "You need to be ranked {0}+ to {1}.",
                                        RankManager.GetMinRankWithAllPermissions( permissions ).Name, action );
            string colorMsg = String.Format( "&SYou need to be ranked {0}&S+ to {1}.",
                                             RankManager.GetMinRankWithAllPermissions( permissions ).ClassyName, action );
            throw new PlayerOpException( player, target, PlayerOpExceptionCode.PermissionMissing, msg, colorMsg );
        }
    }


    enum PlayerOpExceptionCode {
        CannotTargetSelf,
        NoActionNeeded,
        ReasonRequired,
        PermissionMissing,
        PermissionLimitTooLow,
        TargetIsExempt
    }
}
