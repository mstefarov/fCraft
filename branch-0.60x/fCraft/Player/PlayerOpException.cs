// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    sealed class PlayerOpException : Exception {
        public PlayerOpException( PlayerOpExceptionCode errorCode ) {
            ErrorCode = errorCode;
        }
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
