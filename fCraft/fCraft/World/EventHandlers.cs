using System;


namespace fCraft {
    public delegate void LogEventHandler( string message, LogType type );
    public delegate void MessageEventHandler( string message );
    public delegate void ConnectionEventHandler( Session session );
    public delegate void WorldJoinedEventHandler( Player player, World newWorld );
    public delegate void WorldChangedEventHandler( Player player, World oldWorld, World newWorld );
    public delegate void WorldLeftEventHandler( Player player, World oldWorld );

    public delegate void PlayerClassChangeEventHandler( Player target, Player player, PlayerClass oldClass, PlayerClass newClass );

    public delegate void SimpleEventHandler();
}