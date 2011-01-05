﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using System.Net; // for CheckPaidStatus
using System.Net.Cache; // for CheckPaidStatus
using System.IO; // for CheckPaidStatus
using System.Linq;


namespace fCraft {
    /// <summary>
    /// Callback for a player-made selection of one or more blocks on a map.
    /// A command may request a number of marks/blocks to select, and a specify callback
    /// to be executed when the desired number of marks/blocks is reached.
    /// </summary>
    /// <param name="player">Player who made the selection.</param>
    /// <param name="marks">An array of 3D marks/blocks, in terms of block coordinates.</param>
    /// <param name="tag">An optional argument to pass to the callback, the value of player.selectionArgs</param>
    public delegate void SelectionCallback( Player player, Position[] marks, object tag );


    /// <summary>
    /// Object representing a connected player.
    /// </summary>
    public sealed class Player : IClassy {
        public static bool relayAllUpdates = false;

        public string name; // always same as PlayerInfo.name
        // use Player.GetClassyName() to get the colorful version

        internal Session session;
        public PlayerInfo info;

        public Position pos,
                        lastValidPosition; // used in speedhack detection

        public object locker = new object();

        public bool isPainting,
                    isHidden;
        internal World world;
        internal DateTime idleTimer = DateTime.UtcNow; // used for afk kicks

        // the godly pseudo-player for commands called from the server console
        public static Player Console;

        // confirmation
        public Command commandToConfirm;
        public DateTime commandToConfirmDate;

        // for block tracking
        [CLSCompliant( false )]
        public ushort localPlayerID = (ushort)ReservedPlayerID.None; // map-specific PlayerID
        // if no ID is assigned, set to ReservedPlayerID.None

        public int id = -1; // global PlayerID (currently unused)



        // This constructor is used to create dummy players (such as Console and /dummy)
        // It will soon be replaced by a generic Entity class
        internal Player( World _world, string _name ) {
            world = _world;
            name = _name;
            info = new PlayerInfo( _name, RankList.HighestRank, true, RankChangeType.AutoPromoted );
            spamBlockLog = new Queue<DateTime>( info.rank.AntiGriefBlocks );
            ResetAllBinds();
        }


        // Normal constructor
        internal Player( World _world, string _name, Session _session, Position _pos ) {
            world = _world;
            name = _name;
            session = _session;
            pos = _pos;
            info = PlayerDB.FindOrCreateInfoForPlayer( this );
            spamBlockLog = new Queue<DateTime>( info.rank.AntiGriefBlocks );
            ResetAllBinds();
        }


        // safe wrapper for session.Send
        public void Send( Packet packet ) {
            if( session != null ) session.Send( packet );
        }

        public void SendDelayed( Packet packet ) {
            if( session != null ) session.SendDelayed( packet );
        }


        #region Messaging

        public static int spamChatCount = 3;
        public static int spamChatTimer = 4;
        Queue<DateTime> spamChatLog = new Queue<DateTime>( spamChatCount );

        int muteWarnings;
        public static TimeSpan autoMuteDuration = TimeSpan.FromSeconds( 5 );

        const int confirmationTimeout = 60;


        public void Mute( string by, int seconds ) {
            info.mutedUntil = DateTime.UtcNow.AddSeconds( seconds );
            info.mutedBy = by;
        }

        public void Unmute() {
            info.mutedUntil = DateTime.UtcNow;
        }


        public bool IsMuted() {
            return DateTime.UtcNow < info.mutedUntil;
        }


        public void MutedMessage() {
            Message( "You are muted for another {0:0} seconds.",
                     info.mutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
        }


        bool DetectChatSpam() {
            if( this == Console ) return false;
            if( spamChatLog.Count >= spamChatCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < spamChatTimer ) {
                    muteWarnings++;
                    if( muteWarnings > Config.GetInt( ConfigKey.AntispamMaxWarnings ) ) {
                        session.KickNow( "You were kicked for repeated spamming." );
                        Server.SendToAll( "&W{0} was kicked for repeated spamming.", GetClassyName() );
                    } else {
                        info.mutedUntil = DateTime.UtcNow.Add( autoMuteDuration );
                        Message( "You have been muted for {0} seconds. Slow down.", autoMuteDuration.TotalSeconds );
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

                    if( IsMuted() ) {
                        MutedMessage();
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

                    Server.SendToAllExceptIgnored( this, "{0}{1}: {2}", Player.Console,
                                                   GetClassyName(), Color.White, message );
                    break;

                case MessageType.Command:
                    Logger.Log( "{0}: {1}", LogType.UserCommand,
                                name, message );
                    CommandList.ParseCommand( this, message, fromConsole );
                    break;

                case MessageType.PrivateChat:
                    if( !Can( Permission.Chat ) ) return;

                    if( IsMuted() ) {
                        MutedMessage();
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
                        if( allPlayers[0].IsIgnored( this.info ) ) {
                            MessageNow( "&WCannot PM {0}&W: you are ignored.", allPlayers[0].GetClassyName() );
                        } else if( !PM( allPlayers[0], messageText ) ) {
                            NoPlayerMessage( otherPlayerName );
                        }

                    } else if( allPlayers.Length == 0 ) {
                        NoPlayerMessage( otherPlayerName );

                    } else {
                        ManyMatchesMessage( "player", allPlayers );
                    }
                    break;

                case MessageType.RankChat:
                    if( !Can( Permission.Chat ) ) return;

                    if( IsMuted() ) {
                        MutedMessage();
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
                        Server.SendToRank( this, formattedMessage, rank );
                        if( info.rank != rank ) {
                            Message( formattedMessage );
                        }
                    } else {
                        Message( "No class found matching \"{0}\"", rankName );
                    }
                    break;

                case MessageType.Confirmation:
                    if( commandToConfirm != null ) {
                        if( DateTime.UtcNow.Subtract( commandToConfirmDate ).TotalSeconds < confirmationTimeout ) {
                            commandToConfirm.confirmed = true;
                            CommandList.ParseCommand( this, commandToConfirm, fromConsole );
                            commandToConfirm = null;
                        } else {
                            MessageNow( "Confirmation timed out. Enter the command again." );
                        }
                    } else {
                        MessageNow( "There is no command to confirm." );
                    }
                    break;
            }
        }


        public bool PM( Player targetPlayer, string messageText ) {
            Logger.Log( "{0} to {1}: {2}", LogType.PrivateChat,
                        name, targetPlayer.name, messageText );
            targetPlayer.Message( "{0}from {1}: {2}",
                                 Color.PM, name, messageText );
            if( CanSee( targetPlayer ) ) {
                Message( "{0}to {1}: {2}",
                         Color.PM, targetPlayer.name, messageText );
                return true;

            } else {
                return false;
            }
        }


        public void Message( string _message ) {
            MessagePrefixed( ">", _message );
        }

        public void Message( string _message, params object[] args ) {
            MessagePrefixed( ">", String.Format( _message, args ) );
        }


        // Queues a system message with a custom color
        public void MessagePrefixed( string prefix, string message ) {
            if( this == Console ) {
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


        // Sends a message directly (synchronously). Should only be used from Session.IoThread
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


        // Makes sure that there are no unprintable or illegal characters in the message
        public static bool CheckForIllegalChars( string message ) {
            for( int i = 0; i < message.Length; i++ ) {
                char ch = message[i];
                if( ch < ' ' || ch == '&' || ch == '`' || ch == '^' || ch > '}' ) {
                    return true;
                }
            }
            return false;
        }


        internal void NoPlayerMessage( string playerName ) {
            Message( "No players found matching \"{0}\"", playerName );
        }


        internal void NoWorldMessage( string worldName ) {
            Message( "No world found with the name \"{0}\"", worldName );
        }


        internal void ManyMatchesMessage( string itemType, IClassy[] names ) {
            bool first = true;
            StringBuilder list = new StringBuilder();
            foreach( IClassy item in names ) {
                if( !first ) {
                    list.Append( ", " );
                }
                list.Append( item.GetClassyName() );
                first = false;
            }
            Message( "More than one {0} matched: {1}", itemType, list );
        }


        internal void AskForConfirmation( Command cmd, string message, params object[] args ) {
            commandToConfirm = cmd;
            commandToConfirmDate = DateTime.UtcNow;
            Message( message + " Type &H/ok&S to continue.", args );
            commandToConfirm.Rewind();
        }


        internal void NoAccessMessage( params Permission[] permissions ) {
            Rank reqRank = RankList.GetMinRankWithPermission( permissions );
            if( reqRank == null ) {
                Message( "This command is disabled on the server." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.GetClassyName() );
            }
        }


        internal void NoRankMessage( string rankName ) {
            Message( "Unrecognized rank \"{0}\"", rankName );
        }


        #region Ignore

        HashSet<PlayerInfo> ignoreList = new HashSet<PlayerInfo>();
        object ignoreLock = new object();

        public bool IsIgnored( PlayerInfo other ) {
            lock( ignoreLock ) {
                return ignoreList.Contains( other );
            }
        }

        public bool Ignore( PlayerInfo other ) {
            lock( ignoreLock ) {
                if( !ignoreList.Contains( other ) ) {
                    ignoreList.Add( other );
                    return true;
                } else {
                    return false;
                }
            }
        }

        public bool Unignore( PlayerInfo other ) {
            lock( ignoreLock ) {
                if( ignoreList.Contains( other ) ) {
                    ignoreList.Remove( other );
                    return true;
                } else {
                    return false;
                }
            }
        }

        public PlayerInfo[] GetIgnoreList() {
            lock( ignoreLock ) {
                return ignoreList.ToArray();
            }
        }

        #endregion

        #endregion


        #region Placing Blocks

        // grief/spam detection
        Queue<DateTime> spamBlockLog;
        internal Block lastUsedBlockType;
        const int maxRange = 6 * 32;

        /// <summary>
        /// Handles manually-placed/deleted blocks. Returns true if player's action should result in a kick.
        /// </summary>
        public bool PlaceBlock( short x, short y, short h, bool buildMode, Block type ) {

            lastUsedBlockType = type;

            // check if player is frozen or too far away to legitimately place a block
            if( info.isFrozen ||
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

            // bindings
            bool requiresUpdate = (type != bindings[(byte)type] || isPainting);
            if( !buildMode && !isPainting ) {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            // selection handling
            if( selectionMarksExpected > 0 ) {
                SendBlockNow( x, y, h );
                selectionMarks.Enqueue( new Position( x, y, h ) );
                selectionMarkCount++;
                if( selectionMarkCount >= selectionMarksExpected ) {
                    selectionMarksExpected = 0;
                    selectionCallback( this, selectionMarks.ToArray(), selectionArgs );
                } else {
                    Message( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                             selectionMarkCount, x, y, h, selectionMarkCount + 1 );
                }
                return false;
            }

            CanPlaceResult canPlaceResult;
            if( type == Block.Stair && h > 0 && world.map.GetBlock( x, y, h - 1 ) == (byte)Block.Stair ) {
                // stair stacking
                canPlaceResult = CanPlace( x, y, h - 1, (byte)Block.DoubleStair );
            } else {
                // normal placement
                canPlaceResult = CanPlace( x, y, h, (byte)type );
            }

            // if all is well, try placing it
            switch( canPlaceResult ) {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if( type == Block.Stair && h > 0 && world.map.GetBlock( x, y, h - 1 ) == (byte)Block.Stair ) {
                        // handle stair stacking
                        blockUpdate = new BlockUpdate( this, x, y, h - 1, (byte)Block.DoubleStair );
                        if( !world.FireChangedBlockEvent( ref blockUpdate ) ) {
                            SendBlockNow( x, y, h );
                            return false;
                        }
                        info.ProcessBlockPlaced( (byte)Block.DoubleStair );
                        world.map.QueueUpdate( blockUpdate );
                        session.SendNow( PacketWriter.MakeSetBlock( x, y, h - 1, (byte)Block.DoubleStair ) );
                        SendBlockNow( x, y, h );
                        break;

                    } else {
                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, x, y, h, (byte)type );
                        if( !world.FireChangedBlockEvent( ref blockUpdate ) ) {
                            SendBlockNow( x, y, h );
                            return false;
                        }
                        info.ProcessBlockPlaced( (byte)type );
                        world.map.QueueUpdate( blockUpdate );
                        if( requiresUpdate || relayAllUpdates ) {
                            session.SendNow( PacketWriter.MakeSetBlock( x, y, h, (byte)type ) );
                        }
                    }
                    break;

                case CanPlaceResult.BlocktypeDenied:
                    Message( "&WYou are not permitted to affect this block type." );
                    SendBlockNow( x, y, h );
                    break;

                case CanPlaceResult.RankDenied:
                    Message( "&WYour rank is not allowed to build." );
                    SendBlockNow( x, y, h );
                    break;

                case CanPlaceResult.WorldDenied:
                    Message( "&WYour rank is not allowed to build on this world." );
                    SendBlockNow( x, y, h );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = world.map.FindDeniedZone( x, y, h, this );
                    if( deniedZone != null ) {
                        Message( "&WYou are not allowed to build in zone \"{0}\".", deniedZone.name );
                    } else {
                        Message( "&WYou are not allowed to build here." );
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
                    Server.SendToAll( "{0}&W was kicked for suspected griefing.", GetClassyName() );
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
            return (this == Console) || (info.rank.DrawLimit == 0) || (volume <= info.rank.DrawLimit);
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
            PermissionOverride zoneCheckResult = world.map.CheckZones( x, y, h, this );
            if( zoneCheckResult == PermissionOverride.Allow ) {
                return CanPlaceResult.Allowed;
            } else if( zoneCheckResult == PermissionOverride.Deny ) {
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
                    return CanPlaceResult.RankDenied;
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
                    return CanPlaceResult.RankDenied;
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
                    return CanPlaceResult.RankDenied;
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

        internal DrawCommands.CopyInformation copyInformation;

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


        const string PaidCheckURL = "http://minecraft.net/haspaid.jsp?user=";
        const int PaidCheckTimeout = 5000;

        public static bool CheckPaidStatus( string name ) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( PaidCheckURL + Uri.EscapeDataString( name ) );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( Heartbeat.BindIPEndPointCallback );
            request.Timeout = PaidCheckTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

            using( WebResponse response = request.GetResponse() ) {
                using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                    string paidStatusString = responseReader.ReadToEnd();
                    bool isPaid;
                    if( Boolean.TryParse( paidStatusString, out isPaid ) && isPaid ) {
                        return true;
                    } else {
                        return false;
                    }
                }
            }
        }
    }


    public enum CanPlaceResult {
        Allowed,
        BlocktypeDenied,
        WorldDenied,
        ZoneDenied,
        RankDenied
    }
}