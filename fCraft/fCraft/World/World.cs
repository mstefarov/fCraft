/*
 *  Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;


namespace fCraft {

    public sealed class World {
        public event SimpleEventHandler OnLoaded;
        public event SimpleEventHandler OnUnloaded;
        public event WorldJoinedEventHandler OnPlayerJoined;
        public event WorldLeftEventHandler OnPlayerLeft;

        public Map map;
        public string name;
        public Dictionary<int, Player> players = new Dictionary<int, Player>();
        public Player[] playerList;

        object playerListLock = new object(),
               mapLock = new object();

        public bool neverUnload = false;

        //internal bool requestLockDown, lockDown, lockDownReady;
        //internal bool loadInProgress, loadSendingInProgress, loadProgressReported;
        //internal int totalBlockUpdates, completedBlockUpdates;


        public World( string _name ) {
            name = _name;
        }

        public void AcceptPlayer( Player player ) {
            lock( playerListLock ) {
                lock( mapLock ) {
                    if( map == null ) {
                        LoadMap();
                    }
                }
                players.Add( player.id, player );
                UpdatePlayerList();
            }
            //UpdatePlayerList();
            if( Config.GetBool( "BackupOnJoin" ) ) {
                map.SaveBackup( String.Format( "backups/{0}_{1:yyyy-MM-dd HH-mm}_{2}.fcm", name, DateTime.Now, player.name ) );
            }

            SendToAll( PacketWriter.MakeAddEntity( player, player.pos ), player );
            // Reveal newcommer to existing players
            Logger.Log( "{0}Player {1} joined \"{2}\".", LogType.UserActivity, Color.Sys, player.name, name );
            Server.SendToAll( String.Format( "{1} joined {2}", player.GetListName(), player.world.name ), player );

            if( OnPlayerJoined != null ) OnPlayerJoined( player, this );
        }

        public void ReleasePlayer( Player player ) {
            lock( playerListLock ) {
                players.Remove( player.id );
                UpdatePlayerList();
                if( players.Count == 0 && !neverUnload ) {
                    lock( mapLock ) {
                        UnloadMap();
                    }
                }
                if( OnPlayerLeft != null ) OnPlayerLeft( player, this );
                SendToAll( PacketWriter.MakeRemoveEntity( player.id ) );
            }
        }


        public void LoadMap() {
            string mapName = name + ".fcm";
            try {
                map = Map.Load( this, mapName );
            } catch( Exception ex ) {
                Logger.Log( "Could not open the specified file ({0}): {1}", LogType.Error, mapName, ex.Message );
            }

            // or generate a default one
            if( map == null ) {
                Logger.Log( "World.Init: Generating default flatgrass level.", LogType.SystemActivity );
                map = new Map( this, 64, 64, 64 );

                map.spawn.Set( map.widthX / 2 * 32 + 16, map.widthY / 2 * 32 + 16, map.height * 32, 0, 0 );

                MapCommands.GenerateFlatgrass( map, false );

                if( !map.Save() ) throw new Exception( "Could not save file." );
            }

            if( OnLoaded != null ) OnLoaded();
        }

        public void UnloadMap() {
            map.Save( name + ".fcm" );
            map = null;
            if( OnUnloaded != null ) OnUnloaded();
            GC.Collect();
        }

        // Warning: do NOT call this from Tasks threads
        /*internal void BeginLockDown() { //TODO: lockdown
            requestLockDown = true;
            if( Thread.CurrentThread == mainThread ) {
                lockDown = true;
                Tasks.Restart();
                requestLockDown = false;
                Thread.Sleep( 100 ); // buffer time for all threads to catch up
                map.ClearUpdateQueue();
                lockDownReady = true;
            } else {
                while( !lockDownReady ) Thread.Sleep( 1 );
            }
        }

        internal void EndLockDown() {
            lockDownReady = false;
            lockDown = false;
        }*/



        // Send a list of players to the specified new player
        internal void SendPlayerList( Player player ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i] != player && !tempList[i].isHidden ) {
                    player.session.SendNow( PacketWriter.MakeAddEntity( tempList[i], tempList[i].pos ) );
                }
            }
        }


        internal void UpdatePlayer( Player updatedPlayer ) {
            Player[] tempList = playerList;
            for( int i = 1; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i] != updatedPlayer ) {
                    tempList[i].Send( PacketWriter.MakeRemoveEntity( updatedPlayer.id ) );
                    tempList[i].Send( PacketWriter.MakeAddEntity( updatedPlayer, updatedPlayer.pos ) );
                }
            }
        }


        public string GetPlayerListString() {
            Player[] tempList = playerList;
            string list = "";
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && !tempList[i].isHidden ) {
                    list += tempList[i].name + ",";
                }
            }
            if( list.Length > 0 ) {
                return list.Substring( 0, list.Length - 1 );
            } else {
                return list;
            }
        }


        // Find player by name using autocompletion
        public Player FindPlayer( string name ) {
            if( name == null ) return null;
            Player[] tempList = playerList;
            Player result = null;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if( result == null ) {
                        result = tempList[i];
                    } else {
                        return null;
                    }
                }
            }
            return result;
        }


        // Get player by name without autocompletion
        public Player FindPlayerExact( string name ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && tempList[i].name == name ) {
                    return tempList[i];
                }
            }
            return null;
        }


        // Disconnect all players
        public void Shutdown() {
            try {
                if( Config.GetBool( "SaveOnShutdown" ) && map != null ) {
                    map.Save();
                }
            } catch( Exception ex ) {
                Logger.Log( "Error occured while trying to shut down: {0}", LogType.Error, ex.Message );
            }
        }


        // === Messaging ======================================================

        // Broadcast

        public void SendToAll( Packet packet ) {
            SendToAll( packet, null );
        }
        public void SendToAll( Packet packet, Player except ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].Send( packet );
                }
            }
        }
        public void SendToAllDelayed( Packet packet, Player except ) {
            Player[] tempList = playerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].Send( packet, false );
                }
            }
        }
        public void SendToAll( string message ) {
            SendToAll( PacketWriter.MakeMessage( message ), null );
        }
        public void SendToAll( string message, Player except ) {
            SendToAll( PacketWriter.MakeMessage( message ), except );
        }



        /*
        public void UpdatePlayerList() { //TODO
            List<string> playerList = new List<string>();
            Player p;
            for( int i = 1; i < players.Length; i++ ) {
                p = players[i];
                if( p != null ) playerList.Add( p.info.playerClass.name + " - " + p.name );
            }
            //Server.FirePlayerListChange( playerList.ToArray() ); //TODO
        }*/

        public void UpdatePlayerList() {
            lock( playerListLock ) {
                Player[] newPlayerList = new Player[players.Count];
                int i = 0;
                foreach( Player player in players.Values ) {
                    newPlayerList[i++] = player;
                }
                playerList = newPlayerList;
            }
        }
    }
}