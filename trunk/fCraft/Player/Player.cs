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
        // the godly pseudo-player for commands called from the server console
        public static Player Console;

        public static bool RelayAllUpdates;


        public string Name { get { return Info.Name; } } // always same as PlayerInfo.name
        // use Player.GetClassyName() to get the colorful version

        public readonly Session Session;
        public readonly PlayerInfo Info;

        public Position Position,
                        LastValidPosition; // used in speedhack detection

        public bool IsPainting,
                    IsHidden,
                    IsDeaf;
        public World World;
        internal DateTime IdleTimer = DateTime.UtcNow; // used for afk kicks

        // confirmation
        public Command CommandToConfirm;
        public DateTime CommandToConfirmDate;

        // last command (to be able to repeat)
        public Command LastCommand;

        // for block tracking
        public ushort LocalPlayerID = (ushort)ReservedPlayerID.None; // map-specific PlayerID
        // if no ID is assigned, set to ReservedPlayerID.None

        public int ID = -1;



        // This constructor is used to create dummy players (such as Console and /dummy)
        // It will soon be replaced by a generic Entity class
        internal Player( World world, string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            World = world;
            Info = new PlayerInfo( name, RankManager.HighestRank, true, RankChangeType.AutoPromoted );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            ResetAllBinds();
        }


        // Normal constructor
        internal Player( World world, string name, Session session, Position position ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( session == null ) throw new ArgumentNullException( "session" );
            World = world;
            Session = session;
            Position = position;
            Info = PlayerDB.FindOrCreateInfoForPlayer( name, session.GetIP() );
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
        public void ParseMessage( string rawMessage, bool fromConsole ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            if( partialMessage != null ) {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }

            switch( CommandManager.GetMessageType( rawMessage ) ) {
                case MessageType.Chat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted() ) {
                            MutedMessage();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( World != null && !World.FireSentMessageEvent( this, ref rawMessage ) ||
                            !Server.FireSentMessageEvent( this, ref rawMessage ) ) return;

                        Info.LinesWritten++;

                        Logger.Log( "{0}: {1}", LogType.GlobalChat, Name, rawMessage );

                        // Escaped slash removed AFTER logging, to avoid confusion with real commands
                        if( rawMessage.StartsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 1 );
                        }

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        if( Can( Permission.UseColorCodes ) && rawMessage.Contains( "%" ) ) {
                            rawMessage = Color.ReplacePercentCodes( rawMessage );
                        }

                        Server.SendToAllExceptIgnored( this, "{0}{1}: {2}", Console,
                                                       GetClassyName(), Color.White, rawMessage );
                    } break;


                case MessageType.Command: {
                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }
                        Logger.Log( "{0}: {1}", LogType.UserCommand,
                                    Name, rawMessage );
                        Command cmd = new Command( rawMessage );
                        LastCommand = cmd;
                        CommandManager.ParseCommand( this, cmd, fromConsole );
                    } break;


                case MessageType.RepeatCommand: {
                        if( LastCommand == null ) {
                            Message( "No command to repeat." );
                        } else {
                            Logger.Log( "{0}: repeat {1}", LogType.UserCommand,
                                        Name, LastCommand.Message );
                            Message( "Repeat: {0}", LastCommand.Message );
                            CommandManager.ParseCommand( this, LastCommand, fromConsole );
                        }
                    } break;


                case MessageType.PrivateChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted() ) {
                            MutedMessage();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        string otherPlayerName, messageText;
                        if( rawMessage[1] == ' ' ) {
                            otherPlayerName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ', 2 ) - 2 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ', 2 ) + 1 );
                        } else {
                            otherPlayerName = rawMessage.Substring( 1, rawMessage.IndexOf( ' ' ) - 1 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );
                        }

                        if( messageText.Contains( "%" ) && Can( Permission.UseColorCodes ) ) {
                            messageText = Color.ReplacePercentCodes( messageText );
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
                    } break;


                case MessageType.RankChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted() ) {
                            MutedMessage();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        string rankName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ' ) - 2 );
                        Rank rank = RankManager.FindRank( rankName );
                        if( rank != null ) {
                            Logger.Log( "{0} to rank {1}: {2}", LogType.RankChat,
                                        Name, rank.Name, rawMessage );
                            string messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );
                            if( messageText.Contains( "%" ) && Can( Permission.UseColorCodes ) ) {
                                messageText = Color.ReplacePercentCodes( messageText );
                            }

                            string formattedMessage = String.Format( "{0}({1}{2}){3}{4}: {5}",
                                                                     rank.Color,
                                                                     (ConfigKey.RankPrefixesInChat.GetBool() ? rank.Prefix : ""),
                                                                     rank.Name,
                                                                     Color.PM,
                                                                     Name,
                                                                     messageText );
                            Server.SendToRank( this, formattedMessage, rank );
                            if( Info.Rank != rank ) {
                                Message( formattedMessage );
                            }
                        } else {
                            Message( "No rank found matching \"{0}\"", rankName );
                        }
                    } break;


                case MessageType.Confirmation: {
                        if( CommandToConfirm != null ) {
                            if( DateTime.UtcNow.Subtract( CommandToConfirmDate ).TotalSeconds < ConfirmationTimeout ) {
                                CommandToConfirm.Confirmed = true;
                                CommandManager.ParseCommand( this, CommandToConfirm, fromConsole );
                                CommandToConfirm = null;
                            } else {
                                MessageNow( "Confirmation timed out. Enter the command again." );
                            }
                        } else {
                            MessageNow( "There is no command to confirm." );
                        }
                    } break;


                case MessageType.PartialMessage:
                    partialMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                    MessageNow( "Partial: &F{0}", partialMessage );
                    break;

                case MessageType.Invalid: {
                        Message( "Unknown command." );
                    } break;
            }
        }

        string partialMessage;

        public void Message( string message, params object[] args ) {
            MessagePrefixed( ">", message, args );
        }


        // Queues a system message with a custom color
        public void MessagePrefixed( string prefix, string message, params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in PacketWriter.MakeWrappedMessage( prefix, Color.Sys + message, false ) ) {
                    Session.Send( p );
                }
            }
        }


        // Sends a message directly (synchronously). Should only be used from Session.IoThread
        internal void MessageNow( string message, params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
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


        public void NoPlayerMessage( string playerName ) {
            Message( "No players found matching \"{0}\"", playerName );
        }


        public void NoWorldMessage( string worldName ) {
            Message( "No world found with the name \"{0}\"", worldName );
        }


        public void ManyMatchesMessage( string itemType, IEnumerable<IClassy> names ) {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            if( names == null ) throw new ArgumentNullException( "names" );
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
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( message == null ) throw new ArgumentNullException( "message" );
            CommandToConfirm = cmd;
            CommandToConfirmDate = DateTime.UtcNow;
            Message( "{0} Type &H/ok&S to continue.", String.Format( message, args ) );
            CommandToConfirm.Rewind();
        }


        public void NoAccessMessage( params Permission[] permissions ) {
            Rank reqRank = RankManager.GetMinRankWithPermission( permissions );
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

        public void MessageUnsafePath() {
            Message( "You cannot access files outside the map folder." );
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

        // for grief/spam detection
        readonly Queue<DateTime> spamBlockLog;

        /// <summary> Last blocktype used by the player.
        /// Make sure to use in conjunction with Player.GetBind() to ensure that bindings are properly applied. </summary>
        public Block LastUsedBlockType { get; private set; }

        /// <summary> Max distance that player may be from a block to reach it (hack detection). </summary>
        const int MaxRange = 6 * 32;


        /// <summary> Handles manually-placed/deleted blocks.
        /// Returns true if player's action should result in a kick. </summary>
        public bool PlaceBlock( short x, short y, short h, bool buildMode, Block type ) {

            LastUsedBlockType = type;

            // check if player is frozen or too far away to legitimately place a block
            if( Info.IsFrozen ||
                Math.Abs( x * 32 - Position.X ) > MaxRange ||
                Math.Abs( y * 32 - Position.Y ) > MaxRange ||
                Math.Abs( h * 32 - Position.H ) > MaxRange ) {
                RevertBlockNow( x, y, h );
                return false;
            }

            if( World.IsLocked ) {
                RevertBlockNow( x, y, h );
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
                RevertBlockNow( x, y, h );
                AddSelectionMark( new Position( x, y, h ), true );
                return false;
            }

            CanPlaceResult canPlaceResult;
            if( type == Block.Stair && h > 0 && World.Map.GetBlock( x, y, h - 1 ) == Block.Stair ) {
                // stair stacking
                canPlaceResult = CanPlace( x, y, h - 1, Block.DoubleStair, true );
            } else {
                // normal placement
                canPlaceResult = CanPlace( x, y, h, type, true );
            }

            // if all is well, try placing it
            switch( canPlaceResult ) {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if( type == Block.Stair && h > 0 && World.Map.GetBlock( x, y, h - 1 ) == Block.Stair ) {
                        // handle stair stacking
                        blockUpdate = new BlockUpdate( this, x, y, h - 1, Block.DoubleStair );
                        if( !World.FireChangedBlockEvent( ref blockUpdate ) ) {
                            RevertBlockNow( x, y, h );
                            return false;
                        }
                        Info.ProcessBlockPlaced( (byte)Block.DoubleStair );
                        World.Map.QueueUpdate( blockUpdate );
                        Server.RaisePlayerPlacedBlockEvent( this, x, y, h, Block.DoubleStair, true );
                        Session.SendNow( PacketWriter.MakeSetBlock( x, y, h - 1, Block.DoubleStair ) );
                        RevertBlockNow( x, y, h );
                        break;

                    } else {
                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, x, y, h, type );
                        if( !World.FireChangedBlockEvent( ref blockUpdate ) ) {
                            RevertBlockNow( x, y, h );
                            return false;
                        }
                        Info.ProcessBlockPlaced( (byte)type );
                        World.Map.QueueUpdate( blockUpdate );
                        Server.RaisePlayerPlacedBlockEvent( this, x, y, h, type, true );
                        if( requiresUpdate || RelayAllUpdates ) {
                            Session.SendNow( PacketWriter.MakeSetBlock( x, y, h, type ) );
                        }
                    }
                    break;

                case CanPlaceResult.BlocktypeDenied:
                    Message( "&WYou are not permitted to affect this block type." );
                    RevertBlockNow( x, y, h );
                    break;

                case CanPlaceResult.RankDenied:
                    Message( "&WYour rank is not allowed to build." );
                    RevertBlockNow( x, y, h );
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
                    RevertBlockNow( x, y, h );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = World.Map.FindDeniedZone( x, y, h, this );
                    if( deniedZone != null ) {
                        Message( "&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name );
                    } else {
                        Message( "&WYou are not allowed to build here." );
                    }
                    RevertBlockNow( x, y, h );
                    break;

                case CanPlaceResult.PluginDenied:
                    RevertBlockNow( x, y, h );
                    break;

                //case CanPlaceResult.PluginDeniedNoUpdate:
                //    break;
            }
            return false;
        }


        /// <summary>  Gets the block from given location in player's world, and sends it (async) to the player.
        /// Used to undo player's attempted block placement/deletion. </summary>
        public void RevertBlock( short x, short y, short h ) {
            Session.SendDelayed( PacketWriter.MakeSetBlock( x, y, h, World.Map.GetBlockByte( x, y, h ) ) );
        }


        /// <summary>  Gets the block from given location in player's world, and sends it (sync) to the player.
        /// Used to undo player's attempted block placement/deletion.
        /// To avoid threading issues, only use this from this player's IoThread. </summary>
        internal void RevertBlockNow( short x, short y, short h ) {
            Session.SendNow( PacketWriter.MakeSetBlock( x, y, h, World.Map.GetBlockByte( x, y, h ) ) );
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

        public void ResetBind( Block type ) {
            bindings[(byte)type] = type;
        }

        public void ResetBind( params Block[] types ) {
            foreach( Block type in types ) {
                ResetBind( type );
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

        public bool Can( Permission permission ) {
            return (this == Console) || Info.Rank.Can( permission );
        }


        public bool Can( Permission permission, Rank other ) {
            return Info.Rank.Can( permission, other );
        }


        public bool CanDraw( int volume ) {
            return (this == Console) || (Info.Rank.DrawLimit == 0) || (volume <= Info.Rank.DrawLimit);
        }


        public bool CanJoin( World worldToJoin ) {
            if( worldToJoin == null ) throw new ArgumentNullException( "worldToJoin" );
            return (this == Console) || worldToJoin.AccessSecurity.Check( Info );
        }


        public CanPlaceResult CanPlace( int x, int y, int h, Block newBlock, bool isManual ) {
            CanPlaceResult result;
            // check special blocktypes
            if( newBlock == Block.Admincrete && !Can( Permission.PlaceAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Water || newBlock == Block.StillWater) && !Can( Permission.PlaceWater ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Lava || newBlock == Block.StillLava) && !Can( Permission.PlaceLava ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check deleting admincrete
            Block block = World.Map.GetBlock( x, y, h );
            if( block == Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check zones & world permissions
            PermissionOverride zoneCheckResult = World.Map.CheckZones( x, y, h, this );
            if( zoneCheckResult == PermissionOverride.Allow ) {
                result = CanPlaceResult.Allowed;
                goto eventCheck;
            } else if( zoneCheckResult == PermissionOverride.Deny ) {
                result = CanPlaceResult.ZoneDenied;
                goto eventCheck;
            }

            // Check world permissions
            switch( World.BuildSecurity.CheckDetailed( Info ) ) {
                case SecurityCheckResult.Allowed:
                    // Check rank permissions
                    if( (Can( Permission.Build ) || newBlock == Block.Air) &&
                        (Can( Permission.Delete ) || block == Block.Air) ) {
                        result = CanPlaceResult.Allowed;
                        goto eventCheck;
                    } else {
                        result = CanPlaceResult.RankDenied;
                        goto eventCheck;
                    }
                case SecurityCheckResult.WhiteListed:
                    result = CanPlaceResult.Allowed;
                    goto eventCheck;
                default:
                    result = CanPlaceResult.WorldDenied;
                    goto eventCheck;
            }

        eventCheck:
            return Server.RaisePlayerPlacingBlockEvent( this, (short)x, (short)y, (short)h, newBlock, isManual, result );
        }


        // Determines what OP-code to send to the player. It only matters for deleting admincrete.
        public byte GetOpPacketCode() {
            return (byte)(Can( Permission.DeleteAdmincrete ) ? 100 : 0);
        }


        public bool CanSee( Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
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
        internal Permission[] SelectionPermissions;

        internal BuildingCommands.CopyInformation CopyInformation;

        public void AddSelectionMark( Position pos, bool executeCallbackIfNeeded ) {
            SelectionMarks.Enqueue( pos );
            SelectionMarkCount++;
            if( SelectionMarkCount >= SelectionMarksExpected ) {
                if( executeCallbackIfNeeded ) {
                    ExecuteSelectionCallback();
                } else {
                    Message( "Last block marked at ({0},{1},{2}). Type &H/mark&S or click any block to continue.",
                             pos.X, pos.Y, pos.H );
                }
            } else {
                Message( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                         SelectionMarkCount, pos.X, pos.Y, pos.H, SelectionMarkCount + 1 );
            }
        }

        public void ExecuteSelectionCallback() {
            SelectionMarksExpected = 0;
            if( SelectionPermissions == null || Can( SelectionPermissions ) ) {
                SelectionCallback( this, SelectionMarks.ToArray(), SelectionArgs );
            } else {
                Message( "&WYou are no longer allowed to complete this action." );
                NoAccessMessage( SelectionPermissions );
            }
        }

        public void SetCallback( int marksExpected, SelectionCallback callback, object args, params Permission[] requiredPermissions ) {
            SelectionArgs = args;
            SelectionMarksExpected = marksExpected;
            SelectionMarks.Clear();
            SelectionMarkCount = 0;
            SelectionCallback = callback;
            SelectionPermissions = requiredPermissions;
        }

        #endregion


        // ensures that player name has the correct length and character set
        public static bool IsValidName( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 16 ) return false;
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( ch < '0' || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z' || ch != '.' ) {
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
            if( name == null ) throw new ArgumentNullException( "name" );
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
        RankDenied,
        PluginDenied,
        PluginDeniedNoUpdate
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


    public sealed class PlayerClickingEventArgs : PlayerEventArgs {
        internal PlayerClickingEventArgs( Player player, short x, short y, short h, bool mode, Block block )
            : base( player ) {
            X = x;
            Y = y;
            H = h;
            Block = block;
            Mode = mode;
        }

        public short X { get; private set; }
        public short Y { get; private set; }
        public short H { get; private set; }
        public Block Block { get; set; }
        public bool Mode { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class PlayerClickedEventArgs : PlayerEventArgs {
        internal PlayerClickedEventArgs( Player player, short x, short y, short h, bool mode, Block block )
            : base( player ) {
            X = x;
            Y = y;
            H = h;
            Block = block;
            Mode = mode;
        }

        public short X { get; private set; }
        public short Y { get; private set; }
        public short H { get; private set; }
        public Block Block { get; private set; }
        public bool Mode { get; private set; }
    }


    public sealed class PlayerPlacingBlockEventArgs : PlayerEventArgs {
        internal PlayerPlacingBlockEventArgs( Player player, short x, short y, short h, Block block, bool isManual, CanPlaceResult result )
            : base( player ) {
            X = x;
            Y = y;
            H = h;
            Block = block;
            IsManual = isManual;
            Result = result;
        }

        public short X { get; private set; }
        public short Y { get; private set; }
        public short H { get; private set; }
        public bool IsManual { get; private set; }
        public Block Block { get; private set; }
        public CanPlaceResult Result { get; set; }
    }


    public sealed class PlayerPlacedBlockEventArgs : PlayerEventArgs {
        internal PlayerPlacedBlockEventArgs( Player player, short x, short y, short h, Block block, bool isManual )
            : base( player ) {
            X = x;
            Y = y;
            H = h;
            Block = block;
            IsManual = isManual;
        }

        public short X { get; private set; }
        public short Y { get; private set; }
        public short H { get; private set; }
        public bool IsManual { get; private set; }
        public Block Block { get; private set; }
    }


    public sealed class PlayerBeingKickedEventArgs : PlayerKickedEventArgs {
        internal PlayerBeingKickedEventArgs( Player player, Player kicker, string reason, bool isSilent, bool recordToPlayerDB, LeaveReason context )
            : base( player, kicker, reason, isSilent, recordToPlayerDB, context ) {
        }

        public bool Cancel { get; set; }
    }


    public class PlayerKickedEventArgs : PlayerEventArgs {
        internal PlayerKickedEventArgs( Player player, Player kicker, string reason, bool isSilent, bool recordToPlayerDB, LeaveReason context )
            : base( player ) {
            Kicker = kicker;
            Reason = reason;
            IsSilent = isSilent;
            RecordToPlayerDB = recordToPlayerDB;
            Context = context;
        }

        public Player Kicker { get; protected set; }
        public string Reason { get; protected set; }
        public bool IsSilent { get; protected set; }
        public bool RecordToPlayerDB { get; protected set; }
        public LeaveReason Context { get; protected set; }
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