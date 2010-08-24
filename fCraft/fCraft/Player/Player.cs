// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;


namespace fCraft {

    delegate void SelectionCallback( Player player, Position[] marks, object tag );

    public sealed class Player {
        public string name;
        internal Session session;
        public PlayerInfo info;
        public int id = -1; // should not default to any valid id
        public Position pos, lastNonHackingPosition;
        public object locker = new object();
        public bool isPainting,
                    isFrozen,
                    isHidden;
        public static Player Console;
        internal World world;
        public string nick;
        internal DateTime idleTimer = DateTime.UtcNow;


        // This constructor is used to create dummy players (such as Console and /dummy)
        // It will soon be replaced by a generic Entity class
        internal Player( World _world, string _name ) {
            world = _world;
            name = _name;
            nick = name;
            info = new PlayerInfo( _name, ClassList.highestClass );
            spamBlockLog = new Queue<DateTime>( info.playerClass.antiGriefBlocks );
            ResetAllBinds();
        }


        // Normal constructor
        internal Player( World _world, string _name, Session _session, Position _pos ) {
            world = _world;
            name = _name;
            nick = name;
            session = _session;
            pos = _pos;
            info = PlayerDB.FindPlayerInfo( this );
            spamBlockLog = new Queue<DateTime>( info.playerClass.antiGriefBlocks );
            ResetAllBinds();
        }


        // safe wrapper for session.Send
        public void Send( Packet packet ) {
            if( session != null ) session.Send( packet );
        }

        public void Send( Packet packet, bool isHighPriority ) {
            if( session != null ) session.Send( packet, isHighPriority );
        }


        #region Messaging

        public static int spamChatCount = 3;
        public static int spamChatTimer = 4;
        Queue<DateTime> spamChatLog = new Queue<DateTime>( spamChatCount );

        int muteWarnings;
        public static TimeSpan muteDuration = TimeSpan.FromSeconds( 5 );
        DateTime mutedUntil = DateTime.MinValue;

        bool DetectChatSpam() {
            if( spamChatLog.Count >= spamChatCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < spamChatTimer ) {
                    muteWarnings++;
                    if( muteWarnings > Config.GetInt( ConfigKey.AntispamMaxWarnings ) ) {
                        session.KickNow( "You were kicked for repeated spamming." );
                        Server.SendToAll( Color.Red + GetLogName() + " was kicked for repeated spamming." );
                    } else {
                        mutedUntil = DateTime.UtcNow.Add( muteDuration );
                        Message( "You have been muted for {0} seconds. Slow down.", muteDuration.TotalSeconds );
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        // Parses message incoming from the player
        public void ParseMessage( string message, bool fromConsole ) {
            if( DateTime.Now < mutedUntil ) return;
            if( world != null && !world.FireSentMessageEvent( this, ref message ) ) return;
            switch( CommandList.GetMessageType( message ) ) {
                case MessageType.Chat:
                    if( !Can( Permission.Chat ) ) return;
                    if( DetectChatSpam() ) return;
                    info.linesWritten++;
                    string displayedName = nick;
                    if( Config.GetBool( ConfigKey.ClassPrefixesInChat ) ) {
                        displayedName = info.playerClass.prefix + displayedName;
                    }
                    if( Config.GetBool( ConfigKey.ClassColorsInChat ) && info.playerClass.color.Length > 0 && info.playerClass.color != Color.White ) {
                        displayedName = info.playerClass.color + displayedName + Color.White;
                    }

                    if( name == "fragmer" ) displayedName = "&4f&cr&ea&ag&bm&9e&5r&f";
                    Server.SendToAll( displayedName + ": " + message );

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
                                if( tmpChat.Length > 0 ) {
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
                    CommandList.ParseCommand( this, message, fromConsole );
                    break;

                case MessageType.PrivateChat:
                    if( !Can( Permission.Chat ) ) return;
                    if( DetectChatSpam() ) return;
                    string otherPlayerName = message.Substring( 1, message.IndexOf( ' ' ) - 1 );
                    Player otherPlayer = Server.FindPlayer( otherPlayerName );
                    if( otherPlayer != null ) {
                        Logger.Log( "{0} to {1}: {2}", LogType.PrivateChat,
                                    GetLogName(),
                                    otherPlayer.GetLogName(),
                                    message );
                        otherPlayer.Message( "{0}from {1}: {2}",
                                             Color.Gray,
                                             name,
                                             message.Substring( message.IndexOf( ' ' ) + 1 ) );
                        Message( "{0}to {1}: {2}",
                                 Color.Gray,
                                 otherPlayer.name,
                                 message.Substring( message.IndexOf( ' ' ) + 1 ) );
                    } else {
                        NoPlayerMessage( otherPlayerName );
                    }
                    break;

                case MessageType.ClassChat:
                    if( !Can( Permission.Chat ) ) return;
                    if( DetectChatSpam() ) return;
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



        public void Message( string message, params object[] args ) {
            MessagePrefixed( ">", String.Format( message, args ) );
        }


        // Queues a system message with a custom color
        public void MessagePrefixed( string prefix, string message ) {
            if( session == null ) {
                Logger.LogConsole( message );
            } else {
                foreach( Packet p in PacketWriter.MakeWrappedMessage( prefix, Color.Sys + message, false ) ) {
                    session.Send( p );
                }
            }
        }

        public void MessagePrefixed( string prefix, string message, params object[] args ) {
            MessagePrefixed( prefix, string.Format( message, args ) );
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


        internal void NoAccessMessage( params Permission[] permissions ) {
            Message( Color.Red + "You do not have access to this command." );
            if( permissions.Length == 1 ) {
                Message( Color.Red + "You need {0} permission.", permissions[0] );
            } else {
                Message( Color.Red + "You need the following permissions:" );
                foreach( Permission permission in permissions ) {
                    Message( Color.Red + permission.ToString() );
                }
            }
        }

        #endregion


        #region Placing Blocks

        // grief/spam detection
        Queue<DateTime> spamBlockLog;
        internal Block lastUsedBlockType;
        const int maxRange = 6 * 32;

        // Handles building/deleting by the player
        public bool PlaceBlock( short x, short y, short h, bool buildMode, Block type ) {

            lastUsedBlockType = type;

            // check if player is frozen or too far away to legitimately place a block
            if( isFrozen ||
                Math.Abs( x * 32 - pos.x ) > maxRange ||
                Math.Abs( y * 32 - pos.y ) > maxRange ||
                Math.Abs( h * 32 - pos.h ) > maxRange ) {
                SendBlockNow( x, y, h );
                return false;
            }

            if( world.isLocked ) {
                SendBlockNow( x, y, h );
                Message( "This map is currently locked (read-only)." );
                return false;
            }

            if( CheckBlockSpam() ) return true;

            // selection handling
            if( selectionMarksExpected > 0 ) {
                SendBlockNow( x, y, h );
                selectionMarks.Enqueue( new Position( x, y, h ) );
                selectionMarkCount++;
                if( selectionMarkCount >= selectionMarksExpected ) {
                    selectionMarksExpected = 0;
                    selectionCallback( this, selectionMarks.ToArray(), selectionArgs );
                } else {
                    Message( String.Format( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                                            selectionMarkCount, x, y, h, selectionMarkCount + 1 ) );
                }
                return false;
            }

            // bindings
            bool requiresUpdate = (type != bindings[(byte)type] || isPainting);
            if( !buildMode && !isPainting ) {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            // if all is well, try placing it
            switch( CanPlace( x, y, h, (byte)type ) ) {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if( type == Block.Stair && h > 0 && world.map.GetBlock( x, y, h - 1 ) == (byte)Block.Stair ) {

                        // handle stair stacking
                        blockUpdate = new BlockUpdate( this, x, y, h - 1, (byte)Block.DoubleStair );
                        if( !world.FireChangedBlockEvent( ref blockUpdate ) ) {
                            SendBlockNow( x, y, h );
                            return false;
                        }
                        world.map.QueueUpdate( blockUpdate );
                        session.SendNow( PacketWriter.MakeSetBlock( x, y, h - 1, (byte)Block.DoubleStair ) );
                        session.SendNow( PacketWriter.MakeSetBlock( x, y, h, (byte)Block.Air ) );
                    } else {

                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, x, y, h, (byte)type );
                        if( !world.FireChangedBlockEvent( ref blockUpdate ) ) {
                            SendBlockNow( x, y, h );
                            return false;
                        }
                        world.map.QueueUpdate( blockUpdate );
                        if( requiresUpdate ) {
                            session.SendNow( PacketWriter.MakeSetBlock( x, y, h, (byte)type ) );
                        }
                    }
                    break;

                case CanPlaceResult.BlocktypeDenied:
                    Message( "{0}You are not permitted to affect this block type.", Color.Red );
                    SendBlockNow( x, y, h );
                    break;

                case CanPlaceResult.ClassDenied:
                    Message( "{0}Your class is not allowed to build.", Color.Red );
                    SendBlockNow( x, y, h );
                    break;

                case CanPlaceResult.WorldDenied:
                    Message( "{0}Your class is not allowed to build on this world.", Color.Red );
                    SendBlockNow( x, y, h );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = world.map.FindDeniedZone( x, y, h, this );
                    if( deniedZone != null ) {
                        Message( "{0}You are not allowed to build in zone \"{1}\".", Color.Red, deniedZone.name );
                    } else {
                        Message( "{0}You are not allowed to build here.", Color.Red );
                    }
                    SendBlockNow( x, y, h );
                    break;
            }
            return false;
        }


        void SendBlockNow( short x, short y, short h ) {
            session.SendNow( PacketWriter.MakeSetBlock( x, y, h, world.map.GetBlock( x, y, h ) ) );
        }


        bool CheckBlockSpam() {
            if( info.playerClass.antiGriefBlocks == 0 && info.playerClass.antiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= info.playerClass.antiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < info.playerClass.antiGriefSeconds ) {
                    session.KickNow( "You were kicked by antigrief system. Slow down." );
                    Server.SendToAll( Color.Red + GetLogName() + " was kicked for suspected griefing." );
                    Logger.Log( GetLogName() + " was kicked for block spam (" + info.playerClass.antiGriefBlocks + " blocks in " + spamTimer + " seconds)", LogType.SuspiciousActivity );
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion


        #region Binding

        Block[] bindings = new Block[50];

        public void Bind( Block type, Block replacement ) {
            bindings[(byte)type] = replacement;
        }

        public void ResetBind( params Block[] types ) {
            foreach( Block type in types ) {
                bindings[(byte)type] = type;
            }
        }

        public Block GetBind( Block type ) {
            return bindings[(byte)type];
        }

        public void ResetAllBinds() {
            foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                if( block != Block.Undefined ) {
                    ResetBind( block );
                }
            }
        }

        #endregion


        #region Permission Checks

        public bool Can( params Permission[] permissions ) {
            if( world == null ) return true;
            foreach( Permission permission in permissions ) {
                if( !info.playerClass.Can( permission ) ) return false;
            }
            return true;
        }


        public bool CanDraw( int volume ) {
            return (info.playerClass.drawLimit > 0) && (volume > info.playerClass.drawLimit);
        }


        public bool CanJoin( World world ) {
            return info.playerClass.rank >= world.classAccess.rank;
        }


        public CanPlaceResult CanPlace( int x, int y, int h, byte drawBlock ) {
            // check special blocktypes
            if( drawBlock == (byte)Block.Admincrete && !Can( Permission.PlaceAdmincrete ) ) return CanPlaceResult.BlocktypeDenied;
            if( (drawBlock == (byte)Block.Water || drawBlock == (byte)Block.StillWater) && !Can( Permission.PlaceWater ) ) return CanPlaceResult.BlocktypeDenied;
            if( (drawBlock == (byte)Block.Lava || drawBlock == (byte)Block.StillLava) && !Can( Permission.PlaceLava ) ) return CanPlaceResult.BlocktypeDenied;

            // check deleting admincrete
            byte block = world.map.GetBlock( x, y, h );
            if( block == (byte)Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) return CanPlaceResult.BlocktypeDenied;

            // check zones & world permissions
            ZoneOverride zoneCheckResult = world.map.CheckZones( x, y, h, this );
            if( zoneCheckResult == ZoneOverride.Allow ) {
                return CanPlaceResult.Allowed;
            } else if( zoneCheckResult == ZoneOverride.Deny ) {
                return CanPlaceResult.ZoneDenied;
            } else if( drawBlock == (byte)Block.Air ) {

                // deleting a block
                if( Can( Permission.Delete ) ) {
                    if( world.classBuild.rank > info.playerClass.rank ) {
                        return CanPlaceResult.WorldDenied;
                    } else {
                        return CanPlaceResult.Allowed;
                    }
                } else {
                    return CanPlaceResult.ClassDenied;
                }

            } else if( block == (byte)Block.Air ) {

                // building a block
                if( Can( Permission.Build ) ) {
                    if( world.classBuild.rank > info.playerClass.rank ) {
                        return CanPlaceResult.WorldDenied;
                    } else {
                        return CanPlaceResult.Allowed;
                    }
                } else {
                    return CanPlaceResult.ClassDenied;
                }

            } else {

                // replacing a block
                if( Can( Permission.Delete, Permission.Build ) ) {
                    if( world.classBuild.rank > info.playerClass.rank ) {
                        return CanPlaceResult.WorldDenied;
                    } else {
                        return CanPlaceResult.Allowed;
                    }
                } else {
                    return CanPlaceResult.ClassDenied;
                }
            }
        }

        // Determines what OP-code to send to the player. It only matters for deleting admincrete.
        public byte GetOPPacketCode() {
            return (byte)(Can( Permission.DeleteAdmincrete ) ? 100 : 0);
        }

        #endregion


        #region Drawing, Selection, and Undo

        internal Queue<BlockUpdate> undoBuffer = new Queue<BlockUpdate>();

        internal SelectionCallback selectionCallback;
        internal Queue<Position> selectionMarks = new Queue<Position>();
        internal int selectionMarkCount,
                     selectionMarksExpected;
        internal object selectionArgs; // can be used for 'block' or 'zone' or whatever

        internal CopyInformation copyInformation;

        #endregion


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


        // gets name with all the optional fluff (color/prefix) for player list
        public string GetListName() {
            string displayedName = nick;
            if( Config.GetBool( ConfigKey.ClassPrefixesInList ) ) {
                displayedName = info.playerClass.prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.ClassColorsInChat ) && info.playerClass.color.Length > 0 && info.playerClass.color != Color.White ) {
                displayedName = info.playerClass.color + displayedName;
            }
            return displayedName;
        }


        // gets name + nickname (if any) for logging
        public string GetLogName() {
            if( nick != name ) {
                return name + " (aka " + nick + ")";
            } else {
                return name;
            }
        }


        internal void ResetIdleTimer() {
            idleTimer = DateTime.UtcNow;
        }
    }

    public enum CanPlaceResult {
        Allowed,
        BlocktypeDenied,
        WorldDenied,
        ZoneDenied,
        ClassDenied
    }
}