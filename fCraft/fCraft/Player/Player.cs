// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;


namespace fCraft {

    delegate void SelectionCallback( Player player, Position[] marks, object tag );

    public sealed class Player {

        public string name;
        internal Session session;
        public PlayerInfo info;
        public int id;
        public Position pos;
        public object locker = new object();
        internal BlockPlacementMode mode;
        public bool replaceMode;
        internal BlockPlacementMode hardenedMode;
        public bool isFrozen = false,
                    isHidden = false;
        public static Player Console;
        internal World world;
        public string nick;
        internal DateTime idleTimer = DateTime.UtcNow;
        internal Block lastUsedBlockType;

        const int maxRange = 6 * 32;


        // grief/spam detection
        public static int spamBlockCount = 35;
        public static int spamBlockTimer = 5;
        Queue<DateTime> spamBlockLog = new Queue<DateTime>( spamBlockCount );

        public static int spamChatCount = 3;
        public static int spamChatTimer = 4;
        Queue<DateTime> spamChatLog = new Queue<DateTime>( spamChatCount );

        int muteWarnings = 0;
        public static TimeSpan muteDuration = TimeSpan.FromSeconds( 5 );
        DateTime mutedUntil = DateTime.MinValue;

        // selection
        internal Queue<BlockUpdate> drawUndoBuffer = new Queue<BlockUpdate>();
        internal bool drawingInProgress = false;

        internal SelectionCallback selectionCallback;
        internal Stack<Position> marks = new Stack<Position>();
        internal int markCount = 0;
        internal int marksExpected = 0;
        internal object tag; // can be used for 'block' or 'zone' or whatever


        // This constructor is used to create dummy players (such as Console and /dummy)
        // It will soon be replaced by a generic Entity class
        internal Player( World _world, string _name ) {
            world = _world;
            name = _name;
            nick = name;
            info = new PlayerInfo( _name, ClassList.highestClass );
        }


        // Normal constructor
        internal Player( World _world, string _name, Session _session, Position _pos ) {
            world = _world;
            name = _name;
            nick = name;
            session = _session;
            pos = _pos;
            info = PlayerDB.FindPlayerInfo( this );
        }


        // ensures that player name has the correct length and character set
        public static bool IsValidName( string name ) {
            if( name.Length < 2 || name.Length > 16 ) return false;
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( ch < '0' || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }


        // Determines what OP-code to send to the player. It only matters for deleting admincrete.
        public byte GetOPPacketCode() {
            return (byte)(Can( Permissions.DeleteAdmincrete ) ? 100 : 0);
        }


        // Handles building/deleting by the player
        public bool SetTile( short x, short y, short h, bool buildMode, Block type ) {

            lastUsedBlockType = type;

            if( CheckBlockSpam() ) return true;

            if( world.isLocked ) {
                SendTileNow( x, y, h );
                return false;
            }

            /*if( world.lockDown ) { //TODO: streamload
                session.SendNow( PacketWriter.MakeSetBlock( x, y, h, world.map.GetBlock( x, y, h ) ) );
                Message( "Map is temporarily locked. Please wait." );
                return;
            }*/

            // check if player is too far away to legitimately place a block
            if( Math.Abs( x * 32 - pos.x ) > maxRange ||
                Math.Abs( y * 32 - pos.y ) > maxRange ||
                Math.Abs( h * 32 - pos.h ) > maxRange ) {
                SendTileNow( x, y, h );
                return false;
            }

            bool zoneOverride = false;
            string zoneName = "";
            if( world.map.CheckZones( x, y, h, this, ref zoneOverride, ref zoneName ) ) {
                if( !zoneOverride ) {
                    SendTileNow( x, y, h );
                    Message( "You are not allowed to build in \"" + zoneName + "\" zone." );
                    return false;
                }
            }

            // action block handling
            if( marksExpected > 0 ) {
                SendTileNow( x, y, h );
                marks.Push( new Position( x, y, h ) );
                markCount++;
                if( markCount >= marksExpected ) {
                    marksExpected = 0;
                    selectionCallback( this, marks.ToArray(), tag );
                } else {
                    Message( String.Format( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                                            markCount, x, y, h, markCount + 1 ) );
                }
                return false;
            }

            bool can = true;
            bool update = true;
            if( type == Block.Air ) buildMode = false;

            // handle special placement modes
            switch( mode ) {
                case BlockPlacementMode.Grass:
                    if( type == Block.Dirt )
                        type = Block.Grass;
                    break;
                case BlockPlacementMode.Lava:
                    if( type == Block.Orange || type == Block.Red )
                        type = Block.Lava;
                    break;
                case BlockPlacementMode.Solid:
                    if( type == Block.Stone )
                        type = Block.Admincrete;
                    break;
                case BlockPlacementMode.Water:
                    if( type == Block.Aqua || type == Block.Cyan || type == Block.Blue )
                        type = Block.Water;
                    break;
                default:
                    update = false;
                    break;
            }

            // check if the user has the permission to BUILD the block
            if( buildMode || replaceMode ) {
                if( type == Block.Lava || type == Block.StillLava ) {
                    can = Can( Permissions.PlaceLava );
                } else if( type == Block.Water || type == Block.StillWater ) {
                    can = Can( Permissions.PlaceWater );
                } else if( type == Block.Admincrete ) {
                    can = Can( Permissions.PlaceAdmincrete );
                } else {
                    can = zoneOverride || Can( Permissions.Build );
                }
            } else {
                type = Block.Air;
            }

            // check that the user has permission to DELETE/REPLACE the block
            if( world.map.GetBlock( x, y, h ) == (byte)Block.Admincrete ) {
                can &= Can( Permissions.DeleteAdmincrete );
            } else if( world.map.GetBlock( x, y, h ) != (byte)Block.Air ) {
                can &= zoneOverride || Can( Permissions.Delete );
            }

            // Check to see if the player has hardened mode on or not and make changes accordingly
            if( hardenedMode == BlockPlacementMode.Hardened ) {
                Message( "This mode doesn't work yet." );
            }

            // if all is well, try placing it
            if( can ) {
                BlockUpdate blockUpdate;
                if( type == Block.Stair && h > 0 && world.map.GetBlock( x, y, h - 1 ) == (byte)Block.Stair ) {
                    blockUpdate = new BlockUpdate( this, x, y, h - 1, (byte)Block.DoubleStair );
                    if( !world.FireChangedBlockEvent( ref blockUpdate ) ) {
                        SendTileNow( x, y, h );
                        return false;
                    }
                    world.map.QueueUpdate( blockUpdate );
                    session.SendNow( PacketWriter.MakeSetBlock( x, y, h - 1, (byte)Block.DoubleStair ) );
                    session.SendNow( PacketWriter.MakeSetBlock( x, y, h, (byte)Block.Air ) );
                } else {
                    blockUpdate = new BlockUpdate( this, x, y, h, (byte)type );
                    if( !world.FireChangedBlockEvent( ref blockUpdate ) ) {
                        SendTileNow( x, y, h );
                        return false;
                    }
                    world.map.QueueUpdate( blockUpdate );
                    if( update || replaceMode ) {
                        session.SendNow( PacketWriter.MakeSetBlock( x, y, h, (byte)type ) );
                    }
                }
            } else {
                Message( Color.Red, "You are not permitted to do that." );
                SendTileNow( x, y, h );
            }
            return false;
        }


        void SendTileNow( short x, short y, short h ) {
            session.SendNow( PacketWriter.MakeSetBlock( x, y, h, world.map.GetBlock( x, y, h ) ) );
        }

        bool CheckChatSpam() {
            if( spamChatLog.Count >= spamChatCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.Now.Subtract( oldestTime ).TotalSeconds < spamChatTimer ) {
                    muteWarnings++;
                    if( muteWarnings > Config.GetInt( ConfigKey.AntispamMaxWarnings ) ) {
                        session.KickNow( "You were kicked for repeated spamming." );
                        Server.SendToAll( Color.Red + GetLogName() + " was kicked for repeated spamming." );
                    } else {
                        mutedUntil = DateTime.Now.Add( muteDuration );
                        Message( "You have been muted for " + muteDuration.TotalSeconds + " seconds. Slow down." );
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.Now );
            return false;
        }


        bool CheckBlockSpam() {
            if( spamBlockLog.Count >= spamBlockCount ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.Now.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < spamBlockTimer ) {
                    session.KickNow( "You were kicked by antigrief system. Slow down." );
                    Server.SendToAll( Color.Red + GetLogName() + " was kicked for suspected griefing." );
                    Logger.Log( GetLogName() + " was kicked for block spam (" + spamBlockCount + " blocks in " + spamTimer + " seconds)", LogType.SuspiciousActivity );
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.Now );
            return false;
        }


        // Parses message incoming from the player
        public void ParseMessage( string message, bool fromConsole ) {
            if( DateTime.Now < mutedUntil ) return;
            if( world != null && !world.FireSentMessageEvent( this, ref message ) ) return;
            switch( Commands.GetMessageType( message ) ) {
                case MessageType.Chat:
                    if( CheckChatSpam() ) return;
                    info.linesWritten++;
                    string displayedName = nick;
                    if( Config.GetBool( ConfigKey.ClassPrefixesInChat ) ) {
                        displayedName = info.playerClass.prefix + displayedName;
                    }
                    if( Config.GetBool( ConfigKey.ClassColorsInChat ) && info.playerClass.color != "" && info.playerClass.color != Color.White ) {
                        displayedName = info.playerClass.color + displayedName + Color.White;
                    }
                    Server.SendToAll( displayedName + ": " + message, null );

                    // IRC Bot code for sending messages
                    if( IRCBot.IsOnline() ) {
                        if( IRCComm.FORWARD_SERVER ) {
                            IRCMessage newMsg = new IRCMessage();
                            newMsg.chatMessage = nick + ": " + message.Substring( message.IndexOf( "#" ) + 1 );
                            newMsg.destination = Destination.Channels;
                            IRCBot.AddOutgoingMessage( newMsg );
                            IRCComm.Process();
                        } else {
                            if( message.Contains( "#" ) ) {
                                IRCMessage newMsg = new IRCMessage();
                                string tmpChat = message.Substring( message.IndexOf( "#" ) + 1 );
                                if( tmpChat != "" ) {
                                    newMsg.chatMessage = nick + ": " + tmpChat;
                                    newMsg.destination = Destination.Channels;
                                    IRCBot.AddOutgoingMessage( newMsg );
                                    IRCComm.Process();
                                }
                            }
                        }
                    }
                    Logger.Log( "{0}: {1}", LogType.WorldChat, GetLogName(), message );
                    break;

                case MessageType.Command:
                    Logger.Log( "{0}: {1}", LogType.UserCommand, GetLogName(), message );
                    Commands.ParseCommand( this, message, fromConsole );
                    break;

                case MessageType.PrivateChat:
                    if( CheckChatSpam() ) return;
                    string otherPlayerName = message.Substring( 1, message.IndexOf( ' ' ) - 1 );
                    Player otherPlayer = Server.FindPlayer( otherPlayerName );
                    if( otherPlayer != null ) {
                        Logger.Log( "{0} to {1}: {2}", LogType.PrivateChat, GetLogName(), otherPlayer.GetLogName(), message );
                        otherPlayer.Message( Color.Gray, "from " + name + ": " + message.Substring( message.IndexOf( ' ' ) + 1 ) );
                        Message( Color.Gray, "to " + otherPlayer.name + ": " + message.Substring( message.IndexOf( ' ' ) + 1 ) );
                    } else {
                        NoPlayerMessage( otherPlayerName );
                    }
                    break;

                case MessageType.ClassChat:
                    if( CheckChatSpam() ) return;
                    string className = message.Substring( 2, message.IndexOf( ' ' ) - 2 );
                    PlayerClass playerClass = ClassList.FindClass( className );
                    if( playerClass != null ) {
                        Logger.Log( "{0} to class {1}: {2}", LogType.ClassChat, GetLogName(), playerClass.name, message );
                        Packet classMsg = PacketWriter.MakeMessage( Color.Gray + "[" + playerClass.color + playerClass.name + Color.Gray + "]" + nick + ": " + message.Substring( message.IndexOf( ' ' ) + 1 ) );
                        Server.SendToClass( classMsg, playerClass );
                        if( info.playerClass != playerClass ) {
                            Send( classMsg );
                        }
                    } else {
                        Message( "No class found matching \"" + className + "\"" );
                    }
                    break;
            }
        }


        // Checks permissions
        public bool Can( Permissions permission ) {
            if( world == null ) return true;
            if( (permission == Permissions.Build || permission == Permissions.Delete || permission == Permissions.Draw) && world.classBuild.rank > info.playerClass.rank ) {
                return false;
            } else {
                return info.playerClass.Can( permission );
            }
        }


        // safety wrapper for session.Send
        public void Send( Packet packet ) {
            if( session != null ) session.Send( packet );
        }

        public void Send( Packet packet, bool isHighPriority ) {
            if( session != null ) session.Send( packet, isHighPriority );
        }


        // Queues a system message
        public void Message( string message ) {
			Message(Color.Sys, message);
        }


        // Queues a system message with a custom color
        public void Message( string color, string message ) {
            if( session == null ) {
                Logger.LogConsole( message );
            } else {
                session.Send( PacketWriter.MakeMessage( color + message ) );
            }
        }


        // gets name with all the optional fluff (color/prefix)
        public string GetListName() {
            string displayedName = nick;
            if( Config.GetBool( ConfigKey.ClassPrefixesInList ) ) {
                displayedName = info.playerClass.prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.ClassColorsInChat ) && info.playerClass.color != "" && info.playerClass.color != Color.White ) {
                displayedName = info.playerClass.color + displayedName;
            }
            return displayedName;
        }


        public string GetLogName() {
            if( nick != name ) {
                return name + " (aka " + nick + ")";
            } else {
                return name;
            }
        }


        // Validates player name
        public static bool CheckForIllegalChars( string message ) {
            for( int i = 0; i < message.Length; i++ ) {
                char ch = message[i];
                if( ch < ' ' || ch == '&' || ch == '`' || ch == '^' || ch > '}' ) {
                    return true;
                }
            }
            return false;
        }


        internal void NoPlayerMessage( string name ) {
            Message( "No players found matching \"" + name + "\"" );
        }


        internal void ManyPlayersMessage( string name ) {
            Message( "More than one player found matching \"" + name + "\"" );
        }


        internal void NoAccessMessage( Permissions permission ) {
            Message( Color.Red, "You do not have access to this command." );
            Message( Color.Red, "You need " + permission.ToString() + " permission." );
        }

        internal void ResetIdleTimer() {
            idleTimer = DateTime.UtcNow;
        }

    }
}