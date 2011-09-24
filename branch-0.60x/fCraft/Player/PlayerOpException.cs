// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    sealed class PlayerOpException : Exception {
        public PlayerOpException( Player player, PlayerInfo target, PlayerOpExceptionCode errorCode ) {
            Player = player;
            Target = target;
            ErrorCode = errorCode;
        }

        public Player Player { get; private set; }
        public PlayerInfo Target { get; private set; }
        public PlayerOpExceptionCode ErrorCode { get; private set; }
    }


    enum PlayerOpExceptionCode {
        CannotDoThatToSelf,
        NoActionNeeded,
        ReasonRequired,
        PermissionLimitTooLow,
        TargetIsExempt
    }
}
