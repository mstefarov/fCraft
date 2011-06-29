// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
namespace fCraft {
    public delegate void PlayerChangedBlockEventHandler( World world, ref BlockUpdate update, ref bool cancel );

    public delegate void PlayerBanStatusChangedEventHandler( PlayerInfo target, Player banner, string reason );
}