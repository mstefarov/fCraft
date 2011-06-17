// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
namespace fCraft {
    public delegate void PlayerTriedToJoinWorldEventHandler( Player player, World newWorld, ref bool cancel );
    public delegate void PlayerLeftWorldEventHandler( Player player, World oldWorld );
    public delegate void PlayerJoinedWorldEventHandler( Player player, World newWorld );
    public delegate void PlayerChangedWorldEventHandler( Player player, World oldWorld, World newWorld );

    public delegate void PlayerChangedBlockEventHandler( World world, ref BlockUpdate update, ref bool cancel );

    public delegate void PlayerBanStatusChangedEventHandler( PlayerInfo target, Player banner, string reason );
}