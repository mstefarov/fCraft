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

        public Map map;
        public string name;
        public Dictionary<int, Player> players = new Dictionary<int, Player>();
        public Player[] playerList;
        public bool isLocked,
                    isHidden,
                    isReadyForUnload,
                    neverUnload;
        public PlayerClass classAccess, classBuild;

        object playerListLock = new object(),
               mapLock = new object();

        internal int updateTaskId = -1, saveTaskId = -1, backupTaskId = -1;
        AutoResetEvent waiter = new AutoResetEvent( false );
        Thread thread;
        internal bool canDispose = false;

        public World( string _name ) {
            name = _name;
            classAccess = ClassList.lowestClass;
            classBuild = ClassList.lowestClass;
            thread = new Thread( WorldLoop );
            thread.IsBackground = true;
        }


        void WorldLoop() {
            while( !Server.shuttingDown ) {
                waiter.WaitOne(); // wait for players to connect
                LoadMap();
                while( true ) {
                    // update logic
                    lock( playerListLock ) {
                        if( players.Count == 0 ) break;
                    }
                }
                UnloadMap();
            }
            Shutdown();
            canDispose = true;
        }


        // Prepare for shutdown
        public void Shutdown() {
            lock ( mapLock ) {
                if ( Config.GetBool( ConfigKey.SaveOnShutdown ) && map != null ) {
                    SaveMap( null );
                }
            }
        }


        #region Map

        public void LoadMap() {
            lock( mapLock ) {
                if( map != null ) return;
                try {
                    map = Map.Load( this, GetMapName() );
                } catch( Exception ex ) {
                    Logger.Log( "Could not open the specified file ({0}): {1}", LogType.Error, GetMapName(), ex.Message );
                }

                // or generate a default one
                if( map == null ) {
                    Logger.Log( "World.Init: Generating default flatgrass level.", LogType.SystemActivity );
                    map = new Map( this, 64, 64, 64 );

                    map.spawn.Set( map.widthX / 2 * 32 + 16, map.widthY / 2 * 32 + 16, map.height * 32, 0, 0 );

                    MapCommands.GenerateFlatgrass( map, false );

                    SaveMap( null );
                }

                if( OnLoaded != null ) OnLoaded();
            }
        }


        public void UnloadMap() {
            lock ( mapLock ) {
                SaveMap( null );
                map = null;
                isReadyForUnload = false;
                if( OnUnloaded != null ) OnUnloaded();
            }
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        public string GetMapName() {
            return name + ".fcm";
        }


        public void SaveMap( object param ) {
            lock ( mapLock ) {
                if ( map != null ) {
                    map.Save( GetMapName() );
                }
            }
        }


        public void Lock() {
            isLocked = true;
            if ( map != null ) map.ClearUpdateQueue();
            SendToAll( Color.Red + "Map is now on lockdown!" );
        }


        public void Unlock() {
            isLocked = false;
            SendToAll( "Map lockdown has ended." );
        }


        public void ChangeMap( Map newMap ) {
            lock( playerListLock ) {
                lock( mapLock ) {
                    map = null;
                    World newWorld = new World( name );
                    newWorld.map = newMap;
                    newMap.world = newWorld;
                    Server.ReplaceWorld( name, newWorld );
                    foreach( Player player in playerList ) {
                        player.session.forcedWorldToJoin = newWorld;
                    }
                }
            }
        }

        #endregion

        #region PlayerList

        public void AcceptPlayer( Player player ) {
            lock( playerListLock ) {
                lock( mapLock ) {
                    isReadyForUnload = false;
                    if( map == null ) {
                        LoadMap();
                    }

                    if( Config.GetBool( ConfigKey.BackupOnJoin ) ) {
                        map.SaveBackup( GetMapName(), String.Format( "backups/{0}_{1:yyyy-MM-dd HH-mm}_{2}.fcm", name, DateTime.Now, player.name ) );
                    }
                }

                players.Add( player.id, player );
                UpdatePlayerList();

                // Reveal newcommer to existing players
                if( !player.isHidden ) {
                    SendToAll( PacketWriter.MakeAddEntity( player, player.pos ), player );
                    Server.SendToAll( String.Format( "{0}Player {1} joined \"{2}\".", Color.Sys, player.GetLogName(), name ), player );
                }
            }

            Logger.Log( "Player {0} joined \"{1}\".", LogType.UserActivity, player.GetLogName(), name );

            if( OnPlayerJoined != null ) OnPlayerJoined( player, this );

            if( isLocked ) {
                player.Message( Color.Red, "This map is currently locked." );
            }
        }


        public void ReleasePlayer( Player player ) {
            lock ( playerListLock ) {

                if ( !players.Remove( player.id ) ) {
                    //Logger.Log( "World.ReleasePlayer: Attempting to release a nonexistent id.", LogType.Error );
                    return;
                }

                // clear drawing status
                player.drawUndoBuffer.Clear();
                player.marksExpected = 0;
                player.marks.Clear();
                player.markCount = 0;

                // update player list
                UpdatePlayerList();
                if ( OnPlayerLeft != null ) OnPlayerLeft( player, this );
                SendToAll( PacketWriter.MakeRemoveEntity( player.id ) );

                // unload map (if needed)
                if ( players.Count == 0 && !neverUnload ) {
                    isReadyForUnload = true;
                }
            }
        }


        // Send a list of players to the specified new player
        internal void SendPlayerList( Player player ) {
            Player[] tempList = playerList;
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != null && tempList[i] != player && !tempList[i].isHidden ) {
                    player.session.Send( PacketWriter.MakeAddEntity( tempList[i], tempList[i].pos ) );
                }
            }
        }


        // re-adds player entity, in case of name or color change
        internal void UpdatePlayer( Player updatedPlayer ) {
            Player[] tempList = playerList;
            for ( int i = 1; i < tempList.Length; i++ ) {
                if ( tempList[i] != null && tempList[i] != updatedPlayer ) {
                    tempList[i].Send( PacketWriter.MakeRemoveEntity( updatedPlayer.id ) );
                    tempList[i].Send( PacketWriter.MakeAddEntity( updatedPlayer, updatedPlayer.pos ) );
                }
            }
        }


        public string GetPlayerListString() {
            Player[] tempList = playerList;
            string list = "";
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != null && !tempList[i].isHidden ) {
                    list += tempList[i].name + ",";
                }
            }
            if ( list.Length > 0 ) {
                return list.Substring( 0, list.Length - 1 );
            } else {
                return list;
            }
        }


        // Find player by name using autocompletion
        public Player FindPlayer( string name ) {
            if ( name == null ) return null;
            Player[] tempList = playerList;
            Player result = null;
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != null && tempList[i].name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if ( result == null ) {
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
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != null && tempList[i].name == name ) {
                    return tempList[i];
                }
            }
            return null;
        }


        // Cache the player list to an array (players -> playerList)
        public void UpdatePlayerList() {
            lock ( playerListLock ) {
                Player[] newPlayerList = new Player[players.Count];
                int i = 0;
                foreach ( Player player in players.Values ) {
                    newPlayerList[i++] = player;
                }
                playerList = newPlayerList;
            }
        }

        #endregion

        #region Communication

        public void SendToAll( Packet packet ) {
            SendToAll( packet, null );
        }


        public void SendToAll( Packet packet, Player except ) {
            Player[] tempList = playerList;
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != except ) {
                    tempList[i].Send( packet );
                }
            }
        }


        public void SendToAllDelayed( Packet packet, Player except ) {
            Player[] tempList = playerList;
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != except ) {
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

        #endregion

        #region Events
        public event SimpleEventHandler OnLoaded;
        public event SimpleEventHandler OnUnloaded;
        public event PlayerJoinedWorldEventHandler OnPlayerJoined;
        public event PlayerTriedToJoinWorldEventHandler OnPlayerTriedToJoin;
        public event PlayerLeftWorldEventHandler OnPlayerLeft;
        public event PlayerChangedBlockEventHandler OnPlayerChangedBlock;
        public event PlayerSentMessageEventHandler OnPlayerSentMessage;

        public bool FireChangedBlockEvent( ref BlockUpdate update ) {
            bool cancel = false;
            if ( OnPlayerChangedBlock != null ) {
                OnPlayerChangedBlock( this, ref update, ref cancel );
            }
            return !cancel;
        }

        public bool FireSentMessageEvent( Player player, ref string message ) {
            bool cancel = false;
            if ( OnPlayerSentMessage != null ) {
                OnPlayerSentMessage( player, this, ref message, ref cancel );
            }
            return !cancel;
        }

        public bool FirePlayerTriedToJoinEvent( Player player ) {
            bool cancel = false;
            if ( OnPlayerTriedToJoin != null ) {
                OnPlayerTriedToJoin( player, this, ref cancel );
            }
            return !cancel;
        }
        #endregion
    }
}