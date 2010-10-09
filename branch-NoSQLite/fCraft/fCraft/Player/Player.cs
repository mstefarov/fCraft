// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;


namespace fCraft {

    public delegate void SelectionCallback( Player player, Position[] marks, object tag );

    public sealed class Player {
        public string name, lowercaseName;
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
        internal DateTime idleTimer = DateTime.UtcNow;


        // This constructor is used to create dummy players (such as Console and /dummy)
        // It will soon be replaced by a generic Entity class
        internal Player( World _world, string _name ) {
            world = _world;
            name = _name;
            lowercaseName = name.ToLower();
            info = new PlayerInfo( _name, RankList.HighestRank );
            spamBlockLog = new Queue<DateTime>( info.rank.AntiGriefBlocks );
            ResetAllBinds();
        }


        // Normal constructor
        internal Player( World _world, string _name, Session _session, Position _pos ) {
            world = _world;
            name = _name;
            lowercaseName = name.ToLower();
            session = _session;
            pos = _pos;
            info = PlayerDB.FindPlayerInfo( this );
            spamBlockLog = new Queue<DateTime>( info.rank.AntiGriefBlocks );
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

        public void Mute( int seconds ) {
            mutedUntil = DateTime.UtcNow.AddSeconds( seconds );
        }

        bool DetectChatSpam() {
            if( this == Console ) return false;
            if( spamChatLog.Count >= spamChatCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < spamChatTimer ) {
                    muteWarnings++;
                    if( muteWarnings > Config.GetInt( ConfigKey.AntispamMaxWarnings ) ) {
                        session.KickNow( "You were kicked for repeated spamming." );
                        Server.SendToAll( GetClassyName() + Color.Red + " was kicked for repeated spamming." );
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
            switch( CommandList.GetMessageType( message ) ) {
                case MessageType.Chat:
                    if( !Can( Permission.Chat ) ) return;

                    if( DateTime.UtcNow < mutedUntil ) {
                        Message( "You are muted for another {0:0} seconds.",
                                 mutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                        return;
                    }

                    if( DetectChatSpam() ) return;

                    if( world != null && !world.FireSentMessageEvent( this, ref message ) ||
                        !Server.FireSentMessageEvent( this, ref message ) ) return;

                    info.linesWritten++;


                    Logger.Log( "{0}: {1}", LogType.GlobalChat, name, message );

                    // Escaped slash removed AFTER logging, to avoid confusion with real commands
                    if( message.StartsWith( "//" ) ) {
                        message = message.Substring( 1 );
                    }

                    Server.SendToAll( GetClassyName() + Color.White + ": " + message );
                    break;

                case MessageType.Command:
                    Logger.Log( "{0}: {1}", LogType.UserCommand,
                                name, message );
                    CommandList.ParseCommand( this, message, fromConsole );
                    break;

                case MessageType.PrivateChat:
                    if( !Can( Permission.Chat ) ) return;

                    if( DateTime.UtcNow < mutedUntil ) {
                        Message( "You are muted for another {0:0} seconds.",
                                 mutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                        return;
                    }

                    if( DetectChatSpam() ) return;

                    string otherPlayerName, messageText;
                    if( message[1] == ' ' ) {
                        otherPlayerName = message.Substring( 2, message.IndexOf( ' ', 2 ) - 2 );
                        messageText = message.Substring( message.IndexOf( ' ', 2 ) + 1 );
                    } else {
                        otherPlayerName = message.Substring( 1, message.IndexOf( ' ' ) - 1 );
                        messageText = message.Substring( message.IndexOf( ' ' ) + 1 );
                    }

                    // first, find ALL players (visible and hidden)
                    Player[] allPlayers = Server.FindPlayers( otherPlayerName );

                    // if there is more than 1 target player, exclude hidden players
                    if( allPlayers.Length > 1 ) {
                        allPlayers = Server.FindPlayers( this, otherPlayerName );
                    }

                    if( allPlayers.Length == 1 ) {
                        if( !PM( allPlayers[0], messageText ) ) {
                            NoPlayerMessage( otherPlayerName );
                        }

                    } else if( allPlayers.Length == 0 ) {
                        NoPlayerMessage( otherPlayerName );

                    } else {
                        ManyPlayersMessage( allPlayers );
                    }
                    break;

                case MessageType.ClassChat:
                    if( !Can( Permission.Chat ) ) return;

                    if( DateTime.UtcNow < mutedUntil ) {
                        Message( "You are muted for another {0:0} seconds.",
                            mutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                        return;
                    }

                    if( DetectChatSpam() ) return;

                    string rankName = message.Substring( 2, message.IndexOf( ' ' ) - 2 );
                    Rank rank = RankList.FindRank( rankName );
                    if( rank != null ) {
                        Logger.Log( "{0} to class {1}: {2}", LogType.RankChat,
                                    name, rank.Name, message );
                        string formattedMessage = String.Format( "{0}({1}{2}){3}{4}: {5}",
                                                                 rank.Color,
                                                                 (Config.GetBool( ConfigKey.RankPrefixesInChat ) ? rank.Prefix : ""),
                                                                 rank.Name,
                                                                 Color.PM,
                                                                 name,
                                                                 message.Substring( message.IndexOf( ' ' ) + 1 ) );
                        Server.SendToRank( formattedMessage, rank );
                        if( info.rank != rank ) {
                            Message( formattedMessage );
                        }
                    } else {
                        Message( "No class found matching \"{0}\"", rankName );
                    }
                    break;
            }
        }


        public bool PM( Player otherPlayer, string messageText ) {
            Logger.Log( "{0} to {1}: {2}", LogType.PrivateChat,
                        name, otherPlayer, messageText );
            otherPlayer.Message( "{0}from {1}: {2}",
                                 Color.PM, name, messageText );
            if( CanSee( otherPlayer ) ) {
                Message( "{0}to {1}: {2}",
                         Color.PM, otherPlayer.name, messageText );
                return true;

            } else {
                return false;
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

        public void MessageNow( string message, params object[] args ) {
            message = String.Format( message, args );
            if( session == null ) {
                Logger.LogConsole( message );
            } else {
                foreach( Packet p in PacketWriter.MakeWrappedMessage( ">", Color.Sys + message, false ) ) {
                    session.Send( p );
                }
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
            Message( "No players found matching \"{0}\"", name );
        }


        internal void ManyPlayersMessage( IEnumerable<Player> names ) {
            string playerString = "";
            foreach( Player player in names ) {
                playerString += ", " + player.GetClassyName();
            }
            Message( "More than one player matched: \"{0}\"", playerString.Substring( 2 ) );
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


        internal void NoRankMessage( string rankName ) {
            Message( "Unrecognized rank \"{0}\"", rankName );
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
            if( info.rank.AntiGriefBlocks == 0 && info.rank.AntiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= info.rank.AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < info.rank.AntiGriefSeconds ) {
                    session.KickNow( "You were kicked by antigrief system. Slow down." );
                    Server.SendToAll( GetClassyName() + Color.Red + " was kicked for suspected griefing." );
                    Logger.Log( "{0} was kicked for block spam ({1} blocks in {2} seconds)", LogType.SuspiciousActivity,
                                name, info.rank.AntiGriefBlocks, spamTimer );
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
            if( this == Console ) return true;
            foreach( Permission permission in permissions ) {
                if( !info.rank.Can( permission ) ) return false;
            }
            return true;
        }


        public bool CanDraw( int volume ) {
            if( this == Console ) return true;
            return (info.rank.DrawLimit > 0) && (volume > info.rank.DrawLimit);
        }


        public bool CanJoin( World world ) {
            if( this == Console ) return true;
            return info.rank >= world.accessRank;
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
                    if( world.buildRank > info.rank ) {
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
                    if( world.buildRank > info.rank ) {
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
                    if( world.buildRank > info.rank ) {
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

        public bool CanSee( Player other ) {
            if( this == Console ) return true;
            return !other.isHidden || info.rank.CanSee( other.info.rank );
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

        public void SetCallback( int marksExpected, SelectionCallback callback, object args ) {
            selectionArgs = args;
            selectionMarksExpected = marksExpected;
            selectionMarks.Clear();
            selectionMarkCount = 0;
            selectionCallback = callback;
        }

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
            string displayedName = name;
            if( Config.GetBool( ConfigKey.RankPrefixesInList ) ) {
                displayedName = info.rank.Prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.RankColorsInChat ) && info.rank.Color != Color.White ) {
                displayedName = info.rank.Color + displayedName;
            }
            return displayedName;
        }


        public string GetClassyName() {
            return info.GetClassyName();
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