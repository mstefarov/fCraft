// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    [Obsolete]
    public delegate void PlayerBanStatusChangedEventHandler( PlayerInfo target, Player banner, string reason );
}