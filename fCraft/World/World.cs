using System;
using System.Collections.Generic;
using System.Threading;

namespace fCraft {
    static class World {
        public static Server server;
        public static Level level;
        public static Player[] players;
        static int playerCount;
        static UI ui;

        // World initialization
        // NOTE: Logger, Config, and Color are initialized by now
        public static void Init( UI _ui ) {
            ui = _ui;

            // player list
            players = new Player[Config.GetInt( "MaxPlayers" ) + 1];

            // TODO: level loading
            level = new Level( 32, 32, 32 );
            level.spawn.Set( 16 * 32, 16 * 32, 16 * 32, 0, 0 );
            for( int i = 0; i < level.widthX; i++ ) {
                for( int j = 0; j < level.widthY; j++ ) {
                    for( int k = 0; k < 3; k++ ) {
                        level.SetBlock( i, j, k, 3 );
                    }
                    level.SetBlock( i, j, 3, 2 );
                }
            }
            for( int i = 0; i < level.widthX; i++ ) {
                for( int j = 0; j < level.height/2; j++ ) {
                    level.SetBlock( i, 0, j, 7 );
                    level.SetBlock( i, level.widthY - 1, j, 7 );
                }
            }
            for( int i = 0; i < level.widthY; i++ ) {
                for( int j = 0; j < level.height/2; j++ ) {
                    level.SetBlock( 0, i,j, 7 );
                    level.SetBlock( level.widthX - 1,i, j, 7 );
                }
            }

            // load player DB
            PlayerDB.Load();
            StandardCommands.Init();
            HelpCommands.Init();

            serverStart = DateTime.Now;

            // start listening
            server = new Server();
            server.Run();
        }

        const int worldUpdateInterval = 100;
        static DateTime serverStart, lastWorldUpdate;


        // === Main Loop ======================================================

        public static void Update() {

            if( DateTime.Now.Subtract( lastWorldUpdate ).TotalMilliseconds > worldUpdateInterval ) {
                lastWorldUpdate = DateTime.Now;

                // update player positions once in a while
                if( playerCount > 1 ) {
                    for( int i = 1; i < players.Length; i++ ) {
                        if( players[i] != null ) {
                            if( players[i].pos.HasChanged(players[i].newPos) ) {
                                SendToAll( PacketWriter.MakeTeleport( players[i], players[i].newPos ), players[i] );
                            }
                            players[i].pos = players[i].newPos;
                        }
                    }
                }

                // process block updates
                level.ProcessUpdates();
            }

            Thread.Sleep( 1 );
        }


        // === Player list handling ===========================================

        // Add a newly-logged-in player to the list, and notify existing players.
        public static void RegisterPlayer( Player player ) {
            for( int i = 1; i < players.Length; i++ ) {
                if( players[i] == null ) {
                    player.id = i;
                    players[i] = player;
                    playerCount++;

                    World.SendToAll( PacketWriter.MakeAddEntity( player, player.pos ), player );
                    World.SendToAll( PacketWriter.MakeSysMessage( player.name + " (" + player.info.playerClass.name + ") has joined the server." ), player );
                    return;
                }
            }
        }

        // Remove player from the list, and notift remaining players
        public static void UnregisterPlayer( Player player ) {
            if( player != null ) {
                if( players[player.id] == player ) {
                    PlayerDB.ProcessLogout( player );
                    PlayerDB.Save();
                    players[player.id] = null;
                    playerCount--;
                    SendToAll( PacketWriter.MakeRemoveEntity( player.id ), null );
                    SendToAll( PacketWriter.MakeSysMessage( player.name + " left the server." ), null );
                    return;
                }
            }
        }

        // Disconnect all players
        public static void ShutDown() {
            for( int i = 1; i < players.Length; i++ ) {
                if( players[i] != null ) {
                    players[i].session.Kick( "Server shutting down." );
                }
                players[i] = null;
            }
            playerCount = 0;
        }

        // Send a list of players to the specified new player
        public static void SendPlayerList( Player player ) {
            for( int i = 1; i < players.Length; i++ ) {
                if( players[i] != null && players[i] != player ) {
                    player.session.Send( PacketWriter.MakeAddEntity( players[i], players[i].pos ) );
                }
            }
        }

        public static Player FindPlayer( string name ) {
            for( int i = 1; i < players.Length; i++ ) {
                if( players[i] != null && players[i].name == name ) {
                    return players[i];
                }
            }
            return null;
        }

        public static void NoPlayerMessage( Player player, string name ) {
            player.SysMessage( "No players found matching the name \"" + name + "\"" );
        }

        // Return player count
        public static int GetPlayerCount() {
            return playerCount;
        }


        // === Player list handling ===========================================

        // Broadcast
        public static void SendToAll( Packet packet, Player except ) {
            for( int i = 1; i < players.Length; i++ ) {
                if( players[i] != null && players[i] != except ) {
                    players[i].session.Send( packet );
                }
            }
        }

        // Broadcast
        public static void SendToClass( Packet packet, PlayerClass playerClass ) {
            for( int i = 1; i < players.Length; i++ ) {
                if( players[i] != null && players[i].info.playerClass == playerClass ) {
                    players[i].session.Send( packet );
                }
            }
        }
    }
}