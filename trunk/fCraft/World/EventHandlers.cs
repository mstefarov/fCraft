// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
namespace fCraft {
    public delegate void LogEventHandler( string message, LogType type );
    public delegate void UrlChangeEventHandler( string message );
    public delegate void PlayerConnectedEventHandler( Session session, ref bool cancel );
    public delegate void PlayerDisconnectedEventHandler( Session session );
    public delegate void PlayerKickedEventHandler( Player player, Player kicker, string reason );

    public delegate void PlayerTriedToJoinWorldEventHandler( Player player, World newWorld, ref bool cancel );
    public delegate void PlayerLeftWorldEventHandler( Player player, World oldWorld );
    public delegate void PlayerJoinedWorldEventHandler( Player player, World newWorld );
    public delegate void PlayerChangedWorldEventHandler( Player player, World oldWorld, World newWorld );

    public delegate void PlayerChangedBlockEventHandler( World world, ref BlockUpdate update, ref bool cancel );
    public delegate void PlayerSentMessageEventHandler( Player player, World world, ref string message, ref bool cancel );

    public delegate void PlayerRankChangedEventHandler( PlayerInfo target, Player changer, Rank oldRank, Rank newRank, string reason, ref bool cancel );
    public delegate void PlayerBanStatusChangedEventHandler( PlayerInfo target, Player banner, string reason );

    /// <summary>
    /// Simple event without parameters.
    /// Used by Server (OnInit, OnStart, OnShutdownBegin, OnShutdownEnd), and World (OnLoaded, OnUnloaded).
    /// </summary>
    public delegate void SimpleEventHandler();

    public delegate void PlayerListChangedHandler(string[] newPlayerList);
}