// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;

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
        public static bool RelayAllUpdates;

        public string Name; // always same as PlayerInfo.name
        // use Player.GetClassyName() to get the colorful version

        internal Session Session;
        public PlayerInfo Info;

        public Position Position,
                        LastValidPosition; // used in speedhack detection

        public bool IsPainting,
                    IsHidden,
                    IsDeaf;
        internal World World;
        internal DateTime IdleTimer = DateTime.UtcNow; // used for afk kicks

        // the godly pseudo-player for commands called from the server console
        public static Player Console;

        // confirmation
        public Command CommandToConfirm;
        public DateTime CommandToConfirmDate;

        // for block tracking
        public ushort LocalPlayerID = (ushort)ReservedPlayerID.None; // map-specific PlayerID
        // if no ID is assigned, set to ReservedPlayerID.None

        public int ID = -1; // global PlayerID (currently unused)



        // This constructor is used to create dummy players (such as Console and /dummy)
        // It will soon be replaced by a generic Entity class
        internal Player( World world, string name ) {
            World = world;
            Name = name;
            Info = new PlayerInfo( name, RankList.HighestRank, true, RankChangeType.AutoPromoted );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            ResetAllBinds();
        }


        // Normal constructor
        internal Player( World world, string name, Session session, Position position ) {
            World = world;
            Name = name;
            Session = session;
            Position = position;
            Info = PlayerDB.FindOrCreateInfoForPlayer( this );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            ResetAllBinds();
        }


        // safe wrapper for session.Send
        public void Send( Packet packet ) {
            if( Session != null ) Session.Send( packet );
        }

        public void SendDelayed( Packet packet ) {
            if( Session != null ) Session.SendDelayed( packet );
        }


        #region Messaging

        public static int SpamChatCount = 3;
        public static int SpamChatTimer = 4;
        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>( SpamChatCount );

        int muteWarnings;
        public static TimeSpan AutoMuteDuration = TimeSpan.FromSeconds( 5 );

        const int ConfirmationTimeout = 60;


        public void MutedMessage() {
            Message( "You are muted for another {0:0} seconds.",
                     Info.MutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
        }


        bool DetectChatSpam() {
            if( this == Console ) return false;
            if( spamChatLog.Count >= SpamChatCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < SpamChatTimer ) {
                    muteWarnings++;
                    if( muteWarnings > ConfigKey.AntispamMaxWarnings.GetInt() ) {
                        Session.KickNow( "You were kicked for repeated spamming.", LeaveReason.MessageSpamKick );
                        Server.SendToAll( "&W{0} was kicked for repeated spamming.", GetClassyName() );
                    } else {
                        Info.MutedUntil = DateTime.UtcNow.Add( AutoMuteDuration );
                        Message( "You have been muted for {0} seconds. Slow down.", AutoMuteDuration.TotalSeconds );
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

                    if( Info.IsMuted() ) {
                        MutedMessage();
                        return;
                    }

                    if( DetectChatSpam() ) return;

                    if( World != null && !World.FireSentMessageEvent( this, ref message ) ||
                        !Server.FireSentMessageEvent( this, ref message ) ) return;

                    Info.LinesWritten++;

                    Logger.Log( "{0}: {1}", LogType.GlobalChat, Name, message );

                    // Escaped slash removed AFTER logging, to avoid confusion with real commands
                    if( message.StartsWith( "//" ) ) {
                        message = message.Substring( 1 );
                    }

                    Server.SendToAllExceptIgnored( this, "{0}{1}: {2}", Console,
                                                   GetClassyName(), Color.White, message );
                    break;

                case MessageType.Command:
                    Logger.Log( "{0}: {1}", LogType.UserCommand,
                                Name, message );
                    CommandList.ParseCommand( this, message, fromConsole );
                    break;

                case MessageType.PrivateChat:
                    if( !Can( Permission.Chat ) ) return;

                    if( Info.IsMuted() ) {
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
                        Player target = allPlayers[0];
                        if( target.IsIgnoring( Info ) ) {
                            if( CanSee( target ) ) {
                                MessageNow( "&WCannot PM {0}&W: you are ignored.", target.GetClassyName() );
                            }
                        } else {
                            Logger.Log( "{0} to {1}: {2}", LogType.PrivateChat,
                                        Name, target.Name, messageText );
                            target.Message( "{0}from {1}: {2}",
                                                 Color.PM, Name, messageText );
                            if( CanSee( target ) ) {
                                Message( "{0}to {1}: {2}",
                                         Color.PM, target.Name, messageText );

                            } else {
                                NoPlayerMessage( otherPlayerName );
                            }
                        }

                    } else if( allPlayers.Length == 0 ) {
                        NoPlayerMessage( otherPlayerName );

                    } else {
                        ManyMatchesMessage( "player", allPlayers );
                    }
                    break;

                case MessageType.RankChat:
                    if( !Can( Permission.Chat ) ) return;

                    if( Info.IsMuted() ) {
                        MutedMessage();
                        return;
                    }

                    if( DetectChatSpam() ) return;

                    string rankName = message.Substring( 2, message.IndexOf( ' ' ) - 2 );
                    Rank rank = RankList.FindRank( rankName );
                    if( rank != null ) {
                        Logger.Log( "{0} to rank {1}: {2}", LogType.RankChat,
                                    Name, rank.Name, message );
                        string formattedMessage = String.Format( "{0}({1}{2}){3}{4}: {5}",
                                                                 rank.Color,
                                                                 (ConfigKey.RankPrefixesInChat.GetBool() ? rank.Prefix : ""),
                                                                 rank.Name,
                                                                 Color.PM,
                                                                 Name,
                                                                 message.Substring( message.IndexOf( ' ' ) + 1 ) );
                        Server.SendToRank( this, formattedMessage, rank );
                        if( Info.Rank != rank ) {
                            Message( formattedMessage );
                        }
                    } else {
                        Message( "No rank found matching \"{0}\"", rankName );
                    }
                    break;

                case MessageType.Confirmation:
                    if( CommandToConfirm != null ) {
                        if( DateTime.UtcNow.Subtract( CommandToConfirmDate ).TotalSeconds < ConfirmationTimeout ) {
                            CommandToConfirm.Confirmed = true;
                            CommandList.ParseCommand( this, CommandToConfirm, fromConsole );
                            CommandToConfirm = null;
                        } else {
                            MessageNow( "Confirmation timed out. Enter the command again." );
                        }
                    } else {
                        MessageNow( "There is no command to confirm." );
                    }
                    break;
            }
        }


        public void Message( string message ) {
            MessagePrefixed( ">", message );
        }

        public void Message( string message, params object[] args ) {
            MessagePrefixed( ">", String.Format( message, args ) );
        }


        // Queues a system message with a custom color
        public void MessagePrefixed( string prefix, string message ) {
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in PacketWriter.MakeWrappedMessage( prefix, Color.Sys + message, false ) ) {
                    Session.Send( p );
                }
            }
        }


        public void MessagePrefixed( string prefix, string message, params object[] args ) {
            MessagePrefixed( prefix, string.Format( message, args ) );
        }


        // Sends a message directly (synchronously). Should only be used from Session.IoThread
        public void MessageNow( string message, params object[] args ) {
            message = String.Format( message, args );
            if( Session == null ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in PacketWriter.MakeWrappedMessage( ">", Color.Sys + message, false ) ) {
                    Session.Send( p );
                }
            }
        }


        // Makes sure that there are no unprintable or illegal characters in the message
        public static bool CheckForIllegalChars( IEnumerable<char> message ) {
            return message.Any( ch => (ch < ' ' || ch == '&' || ch == '`' || ch == '^' || ch > '}') );
        }


        internal void NoPlayerMessage( string playerName ) {
            Message( "No players found matching \"{0}\"", playerName );
        }


        internal void NoWorldMessage( string worldName ) {
            Message( "No world found with the name \"{0}\"", worldName );
        }


        internal void ManyMatchesMessage( string itemType, IEnumerable<IClassy> names ) {
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


        public void AskForConfirmation( Command cmd, string message, params object[] args ) {
            CommandToConfirm = cmd;
            CommandToConfirmDate = DateTime.UtcNow;
            Message( "{0} Type &H/ok&S to continue.", String.Format( message, args ) );
            CommandToConfirm.Rewind();
        }


        public void NoAccessMessage( params Permission[] permissions ) {
            Rank reqRank = RankList.GetMinRankWithPermission( permissions );
            if( reqRank == null ) {
                Message( "This command is disabled on the server." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.GetClassyName() );
            }
        }


        public void NoRankMessage( string rankName ) {
            Message( "Unrecognized rank \"{0}\"", rankName );
        }


        #region Ignore

        readonly HashSet<PlayerInfo> ignoreList = new HashSet<PlayerInfo>();
        readonly object ignoreLock = new object();

        public bool IsIgnoring( PlayerInfo other ) {
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
        readonly Queue<DateTime> spamBlockLog;
        internal Block LastUsedBlockType;
        const int MaxRange = 6 * 32;

        /// <summary>
        /// Handles manually-placed/deleted blocks. Returns true if player's action should result in a kick.
        /// </summary>
        public bool PlaceBlock( short x, short y, short h, bool buildMode, Block type ) {

            LastUsedBlockType = type;

            // check if player is frozen or too far away to legitimately place a block
            if( Info.IsFrozen ||
                Math.Abs( x * 32 - Position.X ) > MaxRange ||
                Math.Abs( y * 32 - Position.Y ) > MaxRange ||
                Math.Abs( h * 32 - Position.H ) > MaxRange ) {
                SendBlockNow( x, y, h );
                return false;
            }

            if( World.IsLocked ) {
                SendBlockNow( x, y, h );
                Message( "This map is currently locked (read-only)." );
                return false;
            }

            if( CheckBlockSpam() ) return true;

            // bindings
            bool requiresUpdate = (type != bindings[(byte)type] || IsPainting);
            if( !buildMode && !IsPainting ) {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            // selection handling
            if( SelectionMarksExpected > 0 ) {
                SendBlockNow( x, y, h );
                SelectionMarks.Enqueue( new Position( x, y, h ) );
                SelectionMarkCount++;
                if( SelectionMarkCount >= SelectionMarksExpected ) {
                    SelectionMarksExpected = 0;
                    if( selectionPermissions == null || Can( selectionPermissions ) ) {
                        SelectionCallback( this, SelectionMarks.ToArray(), SelectionArgs );
                    } else {
                        Message( "&WYou are no longer allowed to complete this action." );
                        NoAccessMessage( selectionPermissions );
                    }
                } else {
                    Message( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                             SelectionMarkCount, x, y, h, SelectionMarkCount + 1 );
                }
                return false;
            }

            CanPlaceResult canPlaceResult;
            if( type == Block.Stair && h > 0 && World.Map.GetBlock( x, y, h - 1 ) == (byte)Block.Stair ) {
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
                    if( type == Block.Stair && h > 0 && World.Map.GetBlock( x, y, h - 1 ) == (byte)Block.Stair ) {
                        // handle stair stacking
                        blockUpdate = new BlockUpdate( this, x, y, h - 1, (byte)Block.DoubleStair );
                        if( !World.FireChangedBlockEvent( ref blockUpdate ) ) {
                            SendBlockNow( x, y, h );
                            return false;
                        }
                        Info.ProcessBlockPlaced( (byte)Block.DoubleStair );
                        World.Map.QueueUpdate( blockUpdate );
                        Session.SendNow( PacketWriter.MakeSetBlock( x, y, h - 1, (byte)Block.DoubleStair ) );
                        SendBlockNow( x, y, h );
                        break;

                    } else {
                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, x, y, h, (byte)type );
                        if( !World.FireChangedBlockEvent( ref blockUpdate ) ) {
                            SendBlockNow( x, y, h );
                            return false;
                        }
                        Info.ProcessBlockPlaced( (byte)type );
                        World.Map.QueueUpdate( blockUpdate );
                        if( requiresUpdate || RelayAllUpdates ) {
                            Session.SendNow( PacketWriter.MakeSetBlock( x, y, h, (byte)type ) );
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
                    switch( World.BuildSecurity.CheckDetailed( Info ) ) {
                        case SecurityCheckResult.RankTooLow:
                        case SecurityCheckResult.RankTooHigh:
                            Message( "&WYour rank is not allowed to build in this world." );
                            break;
                        case SecurityCheckResult.BlackListed:
                            Message( "&WYou are not allowed to build in this world." );
                            break;
                    }
                    SendBlockNow( x, y, h );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = World.Map.FindDeniedZone( x, y, h, this );
                    if( deniedZone != null ) {
                        Message( "&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name );
                    } else {
                        Message( "&WYou are not allowed to build here." );
                    }
                    SendBlockNow( x, y, h );
                    break;
            }
            return false;
        }


        void SendBlockNow( short x, short y, short h ) {
            Session.SendNow( PacketWriter.MakeSetBlock( x, y, h, World.Map.GetBlock( x, y, h ) ) );
        }


        bool CheckBlockSpam() {
            if( Info.Rank.AntiGriefBlocks == 0 || Info.Rank.AntiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= Info.Rank.AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < Info.Rank.AntiGriefSeconds ) {
                    Session.KickNow( "You were kicked by antigrief system. Slow down.", LeaveReason.BlockSpamKick );
                    Server.SendToAll( "{0}&W was kicked for suspected griefing.", GetClassyName() );
                    Logger.Log( "{0} was kicked for block spam ({1} blocks in {2} seconds)", LogType.SuspiciousActivity,
                                Name, Info.Rank.AntiGriefBlocks, spamTimer );
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion


        #region Binding

        readonly Block[] bindings = new Block[50];

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
            return (this == Console) || permissions.All( permission => Info.Rank.Can( permission ) );
        }


        public bool CanDraw( int volume ) {
            return (this == Console) || (Info.Rank.DrawLimit == 0) || (volume <= Info.Rank.DrawLimit);
        }


        public bool CanJoin( World worldToJoin ) {
            return (this == Console) || worldToJoin.AccessSecurity.Check( Info );
        }


        public CanPlaceResult CanPlace( int x, int y, int h, byte drawBlock ) {
            // check special blocktypes
            if( drawBlock == (byte)Block.Admincrete && !Can( Permission.PlaceAdmincrete ) ) {
                return CanPlaceResult.BlocktypeDenied;
            } else if( (drawBlock == (byte)Block.Water || drawBlock == (byte)Block.StillWater) && !Can( Permission.PlaceWater ) ) {
                return CanPlaceResult.BlocktypeDenied;
            } else if( (drawBlock == (byte)Block.Lava || drawBlock == (byte)Block.StillLava) && !Can( Permission.PlaceLava ) ) {
                return CanPlaceResult.BlocktypeDenied;
            }

            // check deleting admincrete
            byte block = World.Map.GetBlock( x, y, h );
            if( block == (byte)Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) return CanPlaceResult.BlocktypeDenied;

            // check zones & world permissions
            PermissionOverride zoneCheckResult = World.Map.CheckZones( x, y, h, this );
            if( zoneCheckResult == PermissionOverride.Allow ) {
                return CanPlaceResult.Allowed;
            } else if( zoneCheckResult == PermissionOverride.Deny ) {
                return CanPlaceResult.ZoneDenied;
            }

            // Check world permissions
            switch( World.BuildSecurity.CheckDetailed( Info ) ) {
                case SecurityCheckResult.Allowed:
                    // Check rank permissions
                    if( (Can( Permission.Build ) || drawBlock == (byte)Block.Air) &&
                        (Can( Permission.Delete ) || block == (byte)Block.Air) ) {
                        return CanPlaceResult.Allowed;
                    } else {
                        return CanPlaceResult.RankDenied;
                    }
                case SecurityCheckResult.WhiteListed:
                    return CanPlaceResult.Allowed;
                default:
                    return CanPlaceResult.WorldDenied;
            }
        }


        // Determines what OP-code to send to the player. It only matters for deleting admincrete.
        public byte GetOpPacketCode() {
            return (byte)(Can( Permission.DeleteAdmincrete ) ? 100 : 0);
        }

        public bool CanSee( Player other ) {
            if( this == Console ) return true;
            return !other.IsHidden || Info.Rank.CanSee( other.Info.Rank );
        }

        #endregion


        #region Drawing, Selection, and Undo

        internal Queue<BlockUpdate> UndoBuffer = new Queue<BlockUpdate>();


        public SelectionCallback SelectionCallback { get; private set; }
        public readonly Queue<Position> SelectionMarks = new Queue<Position>();
        public int SelectionMarkCount,
                   SelectionMarksExpected;
        internal object SelectionArgs { get; private set; } // can be used for 'block' or 'zone' or whatever
        internal Permission[] selectionPermissions;

        internal DrawCommands.CopyInformation CopyInformation;

        public void SetCallback( int marksExpected, SelectionCallback callback, object args, params Permission[] requiredPermissions ) {
            SelectionArgs = args;
            SelectionMarksExpected = marksExpected;
            SelectionMarks.Clear();
            SelectionMarkCount = 0;
            SelectionCallback = callback;
            selectionPermissions = requiredPermissions;
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
            string displayedName = Name;
            if( ConfigKey.RankPrefixesInList.GetBool() ) {
                displayedName = Info.Rank.Prefix + displayedName;
            }
            if( ConfigKey.RankColorsInChat.GetBool() && Info.Rank.Color != Color.White ) {
                displayedName = Info.Rank.Color + displayedName;
            }
            return displayedName;
        }


        public string GetClassyName() {
            return Info.GetClassyName();
        }


        internal void ResetIdleTimer() {
            IdleTimer = DateTime.UtcNow;
        }


        const string PaidCheckUrl = "http://www.minecraft.net/haspaid.jsp?user=";
        const int PaidCheckTimeout = 5000;

        static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            return new IPEndPoint( Server.IP, 0 );
        }
        public static bool CheckPaidStatus( string name ) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( PaidCheckUrl + Uri.EscapeDataString( name ) );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( BindIPEndPointCallback );
            request.Timeout = PaidCheckTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

            using( WebResponse response = request.GetResponse() ) {
                using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                    string paidStatusString = responseReader.ReadToEnd();
                    bool isPaid;
                    return Boolean.TryParse( paidStatusString, out isPaid ) && isPaid;
                }
            }
        }


        public override string ToString() {
            return String.Format( "Player({0})", Info.Name );
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

#region Events
namespace fCraft.Events {

    public class PlayerEventArgs : EventArgs {
        internal PlayerEventArgs( Player player ) {
            Player = player;
        }

        public Player Player { get; private set; }
    }


    public sealed class PlayerConnectingEventArgs : PlayerEventArgs {
        internal PlayerConnectingEventArgs( Player player )
            : base( player ) {
        }

        public bool Cancel { get; set; }
    }


    public sealed class PlayerConnectedEventArgs : PlayerEventArgs {
        internal PlayerConnectedEventArgs( Player player, World startingWorld )
            : base( player ) {
            StartingWorld = startingWorld;
        }

        public World StartingWorld { get; set; }
    }


    public sealed class PlayerMovingEventArgs : PlayerEventArgs {
        internal PlayerMovingEventArgs( Player player, Position newPos )
            : base( player ) {
            OldPosition = player.Position;
            NewPosition = newPos;
        }
        public Position OldPosition { get; private set; }
        public Position NewPosition { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerMovedEventArgs : PlayerEventArgs {
        internal PlayerMovedEventArgs( Player player, Position oldPos )
            : base( player ) {
            OldPosition = oldPos;
            NewPosition = player.Position;
        }

        public Position OldPosition { get; private set; }
        public Position NewPosition { get; private set; }
    }


    public sealed class PlayerDisconnectedEventArgs : PlayerEventArgs {
        internal PlayerDisconnectedEventArgs( Player player, LeaveReason leaveReason )
            : base( player ) {
            LeaveReason = leaveReason;
        }
        public LeaveReason LeaveReason { get; private set; }
    }
}
#endregion