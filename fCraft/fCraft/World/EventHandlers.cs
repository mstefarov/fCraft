using System;


namespace fCraft {
    public delegate void LogEventHandler( string message, LogType type );
    public delegate void URLChangeEventHandler( string message );
    public delegate void PlayerConnectedEventHandler( Session session, ref bool cancel );
    public delegate void PlayerDisconnectedEventHandler( Session session );

    public delegate void PlayerTriedToJoinWorldEventHandler( Player player, World newWorld, ref bool cancel );
    public delegate void PlayerLeftWorldEventHandler( Player player, World oldWorld );
    public delegate void PlayerJoinedWorldEventHandler( Player player, World newWorld );
    public delegate void PlayerChangedWorldEventHandler( Player player, World oldWorld, World newWorld );

    public delegate void PlayerChangedBlockEventHandler( World world, ref BlockUpdate update, ref bool cancel );
    public delegate void PlayerSentMessageEventHandler( Player player, World world, ref string message, ref bool cancel );

    public delegate void PlayerChangedClassEventHandler( Player target, Player player, PlayerClass oldClass, PlayerClass newClass, ref bool cancel );

    public delegate void SimpleEventHandler();
    public delegate void PlayerListChangedHandler( string[] newPlayerList );
}