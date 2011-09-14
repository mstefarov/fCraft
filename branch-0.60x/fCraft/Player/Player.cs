// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading;
using fCraft.Drawing;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Callback for a player-made selection of one or more blocks on a map.
    /// A command may request a number of marks/blocks to select, and a specify callback
    /// to be executed when the desired number of marks/blocks is reached. </summary>
    /// <param name="player"> Player who made the selection. </param>
    /// <param name="marks"> An array of 3D marks/blocks, in terms of block coordinates. </param>
    /// <param name="tag"> An optional argument to pass to the callback,
    /// the value of player.selectionArgs </param>
    public delegate void SelectionCallback( Player player, Position[] marks, object tag );


    /// <summary> Object representing volatile state of connected player.
    /// For persistent state of a known player account, see PlayerInfo. </summary>
    public sealed partial class Player : IClassy {

        /// <summary> The godly pseudo-player for commands called from the server console.
        /// Console has all the permissions granted.
        /// Note that Player.Console.World is always null,
        /// and that prevents console from calling certain commands (like /tp). </summary>
        public static Player Console, AutoRank;


        #region Properties

        public readonly bool IsSuper;

        /// <summary> Whether the player has completed the login sequence. </summary>
        public bool HasRegistered { get; internal set; }

        /// <summary> Whether the player registered and then finished loading the world. </summary>
        public bool HasFullyConnected { get; private set; }

        /// <summary> Whether the client is currently connected. </summary>
        public bool IsOnline { get; private set; }

        /// <summary> Whether the player name was verified at login. </summary>
        public bool IsVerified { get; private set; }

        /// <summary> Persistent information record associated with this player. </summary>
        public PlayerInfo Info { get; private set; }

        /// <summary> Whether the player is in paint mode (deleting blocks replaces them). Used by /paint. </summary>
        public bool IsPainting { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public bool IsDeaf { get; set; }


        /// <summary> The world that the player is currently on. May be null.
        /// Use .JoinWorld() to make players teleport to another world. </summary>
        public World World { get; private set; }

        /// <summary> Player's position in the current world. </summary>
        public Position Position;


        /// <summary> Time when the session connected. </summary>
        public DateTime LoginTime { get; private set; }

        /// <summary> Last time when the player was active (moving/messaging). UTC. </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary> Last time when this player was patrolled by someone. </summary>
        public DateTime LastPatrolTime { get; set; }


        /// <summary> Last command called by the player. </summary>
        public Command LastCommand { get; private set; }


        /// <summary> Plain version of the name (no formatting). </summary>
        [NotNull]
        public string Name {
            get { return Info.Name; }
        }

        /// <summary> Name formatted for display in the player list. </summary>
        [NotNull]
        public string ListName {
            get {
                string displayedName = Name;
                if( ConfigKey.RankPrefixesInList.Enabled() ) {
                    displayedName = Info.Rank.Prefix + displayedName;
                }
                if( ConfigKey.RankColorsInChat.Enabled() && Info.Rank.Color != Color.White ) {
                    displayedName = Info.Rank.Color + displayedName;
                }
                return displayedName;
            }
        }

        /// <summary> Name formatted for display in chat. </summary>
        [NotNull]
        public string ClassyName {
            get { return Info.ClassyName; }
        }

        /// <summary> Whether the client supports advanced WoM client functionality. </summary>
        public bool IsUsingWoM { get; private set; }


        /// <summary> Metadata associated with the session/player. </summary>
        [NotNull]
        public MetadataCollection<object> Metadata { get; private set; }

        #endregion


        // This constructor is used to create pseudoplayers (such as Console and /dummy).
        // Such players have unlimited permissions, but no world.
        // This should be replaced by a more generic solution, like an IEntity interface.
        internal Player( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Info = new PlayerInfo( name, RankManager.HighestRank, true, RankChangeType.AutoPromoted );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            ResetAllBinds();
            IsSuper = true;
        }


        #region Chat and Messaging

        const int ConfirmationTimeout = 60;

        int muteWarnings;
        string partialMessage;

        // Parses message incoming from the player
        public void ParseMessage( [NotNull] string rawMessage, bool fromConsole ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            if( rawMessage.Equals( "/nvm", StringComparison.OrdinalIgnoreCase ) ) {
                if( partialMessage != null ) {
                    MessageNow( "Partial message cancelled." );
                    partialMessage = null;
                } else {
                    MessageNow( "No partial message to cancel." );
                    return;
                }
            }

            if( partialMessage != null ) {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }

            switch( Chat.GetRawMessageType( rawMessage ) ) {
                case RawMessageType.Chat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

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

                        Chat.SendGlobal( this, rawMessage );
                    } break;


                case RawMessageType.Command: {
                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }
                        Command cmd = new Command( rawMessage );
                        CommandDescriptor commandDescriptor = CommandManager.GetDescriptor( cmd.Name, true );

                        if( Info.IsFrozen && !commandDescriptor.UsableByFrozenPlayers ) {
                            MessageNow( "&WYou cannot use this command while frozen." );
                            return;
                        }

                        Logger.Log( "{0}: {1}", LogType.UserCommand,
                                    Name, rawMessage );
                        CommandManager.ParseCommand( this, cmd, fromConsole );
                        if( !commandDescriptor.NotRepeatable ) {
                            LastCommand = cmd;
                        }
                    } break;


                case RawMessageType.RepeatCommand: {
                        if( Info.IsFrozen ) {
                            MessageNow( "&WYou cannot use any commands while frozen." );
                            return;
                        }
                        if( LastCommand == null ) {
                            Message( "No command to repeat." );
                        } else {
                            LastCommand.Rewind();
                            Logger.Log( "{0} repeated: {1}", LogType.UserCommand,
                                        Name, LastCommand.Message );
                            Message( "Repeat: {0}", LastCommand.Message );
                            CommandManager.ParseCommand( this, LastCommand, fromConsole );
                        }
                    } break;


                case RawMessageType.PrivateChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
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
                        Player[] allPlayers = Server.FindPlayers( otherPlayerName, true );

                        // if there is more than 1 target player, exclude hidden players
                        if( allPlayers.Length > 1 ) {
                            allPlayers = Server.FindPlayers( this, otherPlayerName, true );
                        }

                        if( allPlayers.Length == 1 ) {
                            Player target = allPlayers[0];
                            if( target == this ) {
                                MessageNow( "Trying to talk to yourself?" );
                                return;
                            }
                            if( target.IsIgnoring( Info ) ) {
                                if( CanSee( target ) ) {
                                    MessageNow( "&WCannot PM {0}&W: you are ignored.", target.ClassyName );
                                }
                            } else if( target.IsDeaf ) {
                                MessageNow( "&SCannot PM {0}&S: they are currently deaf.", target.ClassyName );
                            } else {
                                Chat.SendPM( this, target, messageText );
                                if( !CanSee( target ) ) {
                                    // message was sent to a hidden player
                                    MessageNoPlayer( otherPlayerName );

                                } else {
                                    // message was sent normally
                                    Message( "&Pto {0}: {1}",
                                             target.Name, messageText );
                                }
                            }

                        } else if( allPlayers.Length == 0 ) {
                            MessageNoPlayer( otherPlayerName );

                        } else {
                            MessageManyMatches( "player", allPlayers );
                        }
                    } break;


                case RawMessageType.RankChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        Rank rank;
                        if( rawMessage[2] == ' ' ) {
                            rank = Info.Rank;
                        } else {
                            string rankName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ' ) - 2 );
                            rank = RankManager.FindRank( rankName );
                            if( rank == null ) {
                                MessageNoRank( rankName );
                                break;
                            }
                        }

                        string messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );
                        if( messageText.Contains( "%" ) && Can( Permission.UseColorCodes ) ) {
                            messageText = Color.ReplacePercentCodes( messageText );
                        }

                        Chat.SendRank( this, rank, messageText );
                    } break;


                case RawMessageType.Confirmation: {
                        if( Info.IsFrozen ) {
                            MessageNow( "&WYou cannot use any commands while frozen." );
                            return;
                        }
                        if( ConfirmCommand != null ) {
                            if( DateTime.UtcNow.Subtract( ConfirmRequestTime ).TotalSeconds < ConfirmationTimeout ) {
                                ConfirmCommand.IsConfirmed = true;
                                CommandManager.ParseCommand( this, ConfirmCommand, fromConsole );
                                ConfirmCommand = null;
                            } else {
                                MessageNow( "Confirmation timed out. Enter the command again." );
                            }
                        } else {
                            MessageNow( "There is no command to confirm." );
                        }
                    } break;


                case RawMessageType.PartialMessage:
                    partialMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                    MessageNow( "Partial: &F{0}", partialMessage );
                    break;

                case RawMessageType.Invalid:
                    MessageNow( "Could not parse message." );
                    break;
            }
        }


        public void Message( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    Send( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        public void Message( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            Message( String.Format( message, args ) );
        }


        [StringFormatMethod( "message" )]
        public void MessagePrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in LineWrapper.WrapPrefixed( prefix, message ) ) {
                    Send( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNow( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
                }
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    SendNow( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNowPrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
                }
                foreach( Packet p in LineWrapper.WrapPrefixed( prefix, message ) ) {
                    Send( p );
                }
            }
        }


        #region Macros

        public void MessageNoPlayer( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Message( "No players found matching \"{0}\"", playerName );
        }


        public void MessageNoWorld( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Message( "No worlds found matching \"{0}\". See &H/worlds", worldName );
        }


        public void MessageManyMatches( [NotNull] string itemType, [NotNull] IEnumerable<IClassy> names ) {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            if( names == null ) throw new ArgumentNullException( "names" );

            string nameList = names.JoinToString( ", ",
                                                  p => p.ClassyName );
            Message( "More than one {0} matched: {1}",
                     itemType, nameList );
        }


        public void MessageNoAccess( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            Rank reqRank = RankManager.GetMinRankWithAllPermissions( permissions );
            if( reqRank == null ) {
                Message( "This command is disabled on the server." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.ClassyName );
            }
        }


        public void MessageNoAccess( [NotNull] CommandDescriptor cmd ) {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            Rank reqRank = cmd.MinRank;
            if( reqRank == null ) {
                Message( "This command is disabled on the server." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.ClassyName );
            }
        }


        public void MessageNoRank( [NotNull] string rankName ) {
            if( rankName == null ) throw new ArgumentNullException( "rankName" );
            Message( "Unrecognized rank \"{0}\". See &H/ranks", rankName );
        }


        public void MessageUnsafePath() {
            Message( "&WYou cannot access files outside the map folder." );
        }


        public void MessageNoZone( [NotNull] string zoneName ) {
            if( zoneName == null ) throw new ArgumentNullException( "zoneName" );
            Message( "No zones found matching \"{0}\". See &H/zones", zoneName );
        }


        public void MessageMuted() {
            Message( "You are muted for another {0:0} seconds.",
                     Info.MutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
        }

        #endregion


        #region Ignore

        readonly HashSet<PlayerInfo> ignoreList = new HashSet<PlayerInfo>();
        readonly object ignoreLock = new object();


        /// <summary> Checks whether this player is currently ignoring a given PlayerInfo.</summary>
        public bool IsIgnoring( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Contains( other );
            }
        }


        /// <summary> Adds a given PlayerInfo to the ignore list.
        /// Not that ignores are not persistent, and are reset when a player disconnects. </summary>
        /// <param name="other"> Player to ignore. </param>
        /// <returns> True if the player is now ignored,
        /// false is the player has already been ignored previously. </returns>
        public bool Ignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                if( !ignoreList.Contains( other ) ) {
                    ignoreList.Add( other );
                    return true;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Removes a given PlayerInfo from the ignore list. </summary>
        /// <param name="other"> PlayerInfo to unignore. </param>
        /// <returns> True if the player is no longer ignored,
        /// false if the player was already not ignored. </returns>
        public bool Unignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Remove( other );
            }
        }


        /// <summary> Returns a list of all currently-ignored players. </summary>
        public PlayerInfo[] IgnoreList {
            get {
                lock( ignoreLock ) {
                    return ignoreList.ToArray();
                }
            }
        }

        #endregion


        #region Confirmation

        /// <summary> Most recent command that needed confirmation. May be null. </summary>
        public Command ConfirmCommand { get; private set; }

        /// <summary> Time when the confirmation was requested. UTC. </summary>
        public DateTime ConfirmRequestTime { get; private set; }

        /// <summary> Request player to confirm continuing with the command.
        /// Player is prompted to type "/ok", and when he/she does,
        /// the command is called again with IsConfirmed flag set. </summary>
        /// <param name="cmd"> Command that needs confirmation. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="args"> Optional String.Format() arguments, for the message. </param>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] Command cmd, [NotNull] string message, [NotNull] params object[] args ) {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            ConfirmCommand = cmd;
            ConfirmRequestTime = DateTime.UtcNow;
            Message( "{0} Type &H/ok&S to continue.", String.Format( message, args ) );
            ConfirmCommand.Rewind();
        }

        #endregion


        #region AntiSpam

        public static int AntispamMessageCount = 3;
        public static int AntispamInterval = 4;
        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>( AntispamMessageCount );

        internal bool DetectChatSpam() {
            if( IsSuper ) return false;
            if( spamChatLog.Count >= AntispamMessageCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < AntispamInterval ) {
                    muteWarnings++;
                    if( muteWarnings > ConfigKey.AntispamMaxWarnings.GetInt() ) {
                        KickNow( "You were kicked for repeated spamming.", LeaveReason.MessageSpamKick );
                        Server.Message( "&W{0} was kicked for repeated spamming.", ClassyName );
                    } else {
                        TimeSpan autoMuteDuration = TimeSpan.FromSeconds( ConfigKey.AntispamMuteDuration.GetInt() );
                        Info.Mute( "(antispam)", autoMuteDuration );
                        Message( "You have been muted for {0} seconds. Slow down.", autoMuteDuration );
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion

        #endregion


        #region Placing Blocks

        // for grief/spam detection
        readonly Queue<DateTime> spamBlockLog = new Queue<DateTime>();

        /// <summary> Last blocktype used by the player.
        /// Make sure to use in conjunction with Player.GetBind() to ensure that bindings are properly applied. </summary>
        public Block LastUsedBlockType { get; private set; }

        /// <summary> Max distance that player may be from a block to reach it (hack detection). </summary>
        public static int MaxBlockPlacementRange { get; set; }


        /// <summary> Handles manually-placed/deleted blocks.
        /// Returns true if player's action should result in a kick. </summary>
        public bool PlaceBlock( short x, short y, short z, bool isBuilding, Block type ) {

            LastUsedBlockType = type;

            // check if player is frozen or too far away to legitimately place a block
            if( Info.IsFrozen ||
                Math.Abs( x * 32 - Position.X ) > MaxBlockPlacementRange ||
                Math.Abs( y * 32 - Position.Y ) > MaxBlockPlacementRange ||
                Math.Abs( z * 32 - Position.Z ) > MaxBlockPlacementRange ) {
                RevertBlockNow( x, y, z );
                return false;
            }

            if( IsSpectating ) {
                Message( "You cannot build or delete while spectating." );
                RevertBlockNow( x, y, z );
                return false;
            }

            if( World.IsLocked ) {
                RevertBlockNow( x, y, z );
                Message( "This map is currently locked (read-only)." );
                return false;
            }

            if( CheckBlockSpam() ) return true;

            // bindings
            bool requiresUpdate = (type != bindings[(byte)type] || IsPainting);
            if( !isBuilding && !IsPainting ) {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            // selection handling
            if( SelectionMarksExpected > 0 ) {
                RevertBlockNow( x, y, z );
                SelectionAddMark( new Position( x, y, z ), true );
                return false;
            }

            CanPlaceResult canPlaceResult;
            if( type == Block.Stair && z > 0 && World.Map.GetBlock( x, y, z - 1 ) == Block.Stair ) {
                // stair stacking
                canPlaceResult = CanPlace( x, y, z - 1, Block.DoubleStair, true );
            } else {
                // normal placement
                canPlaceResult = CanPlace( x, y, z, type, true );
            }

            // if all is well, try placing it
            switch( canPlaceResult ) {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if( type == Block.Stair && z > 0 && World.Map.GetBlock( x, y, z - 1 ) == Block.Stair ) {
                        // handle stair stacking
                        blockUpdate = new BlockUpdate( this, x, y, z - 1, Block.DoubleStair );
                        Info.ProcessBlockPlaced( (byte)Block.DoubleStair );
                        World.Map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, World.Map, x, y, (short)(z - 1), Block.Stair, Block.DoubleStair, true );
                        SendNow( PacketWriter.MakeSetBlock( x, y, z - 1, Block.DoubleStair ) );
                        RevertBlockNow( x, y, z );
                        break;

                    } else {
                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, x, y, z, type );
                        Info.ProcessBlockPlaced( (byte)type );
                        Block old = World.Map.GetBlock( x, y, z );
                        World.Map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, World.Map, x, y, z, old, type, true );
                        if( requiresUpdate || RelayAllUpdates ) {
                            SendNow( PacketWriter.MakeSetBlock( x, y, z, type ) );
                        }
                    }
                    break;

                case CanPlaceResult.BlocktypeDenied:
                    Message( "&WYou are not permitted to affect this block type." );
                    RevertBlockNow( x, y, z );
                    break;

                case CanPlaceResult.RankDenied:
                    Message( "&WYour rank is not allowed to build." );
                    RevertBlockNow( x, y, z );
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
                    RevertBlockNow( x, y, z );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = World.Map.Zones.FindDenied( x, y, z, this );
                    if( deniedZone != null ) {
                        Message( "&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name );
                    } else {
                        Message( "&WYou are not allowed to build here." );
                    }
                    RevertBlockNow( x, y, z );
                    break;

                case CanPlaceResult.PluginDenied:
                    RevertBlockNow( x, y, z );
                    break;

                //case CanPlaceResult.PluginDeniedNoUpdate:
                //    break;
            }
            return false;
        }


        /// <summary>  Gets the block from given location in player's world,
        /// and sends it (async) to the player.
        /// Used to undo player's attempted block placement/deletion. </summary>
        public void RevertBlock( short x, short y, short z ) {
            SendLowPriority( PacketWriter.MakeSetBlock( x, y, z, World.Map.GetBlockByte( x, y, z ) ) );
        }


        /// <summary>  Gets the block from given location in player's world, and sends it (sync) to the player.
        /// Used to undo player's attempted block placement/deletion.
        /// To avoid threading issues, only use this from this player's IoThread. </summary>
        internal void RevertBlockNow( short x, short y, short z ) {
            SendNow( PacketWriter.MakeSetBlock( x, y, z, World.Map.GetBlockByte( x, y, z ) ) );
        }


        // returns true if the player is spamming and should be kicked.
        bool CheckBlockSpam() {
            if( Info.Rank.AntiGriefBlocks == 0 || Info.Rank.AntiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= Info.Rank.AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < Info.Rank.AntiGriefSeconds ) {
                    KickNow( "You were kicked by antigrief system. Slow down.", LeaveReason.BlockSpamKick );
                    Server.Message( "{0}&W was kicked for suspected griefing.", ClassyName );
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
            if( types == null ) throw new ArgumentNullException( "types" );
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

        /// <summary> Returns true if player has ALL of the given permissions. </summary>
        public bool Can( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.All( Info.Rank.Can );
        }


        /// <summary> Returns true if player has ANY of the given permissions. </summary>
        public bool CanAny( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.Any( Info.Rank.Can );
        }


        /// <summary> Returns true if player has the given permission. </summary>
        public bool Can( Permission permission ) {
            return IsSuper || Info.Rank.Can( permission );
        }


        /// <summary> Returns true if player has the given permission,
        /// and is allowed to affect players of the given rank. </summary>
        public bool Can( Permission permission, [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return IsSuper || Info.Rank.Can( permission, other );
        }


        /// <summary> Returns true if player is allowed to run
        /// draw commands that affect a given number of blocks. </summary>
        public bool CanDraw( int volume ) {
            return IsSuper || (Info.Rank.DrawLimit == 0) || (volume <= Info.Rank.DrawLimit);
        }


        /// <summary> Returns true if player is allowed to join a given world. </summary>
        public bool CanJoin( [NotNull] World worldToJoin ) {
            if( worldToJoin == null ) throw new ArgumentNullException( "worldToJoin" );
            return IsSuper || worldToJoin.AccessSecurity.Check( Info );
        }


        /// <summary> Checks whether player is allowed to place a block on the current world at given coordinates.
        /// Raises the PlayerPlacingBlock event. </summary>
        public CanPlaceResult CanPlace( int x, int y, int z, Block newBlock, bool isManual ) {
            CanPlaceResult result;
            Map map = World.Map;

            // check deleting admincrete
            Block block = map.GetBlock( x, y, z );

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

            // check admincrete-related permissions
            if( block == Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check zones & world permissions
            PermissionOverride zoneCheckResult = map.Zones.Check( x, y, z, this );
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
                    // Check world's rank permissions
                    if( (Can( Permission.Build ) || newBlock == Block.Air) &&
                        (Can( Permission.Delete ) || block == Block.Air) ) {
                        result = CanPlaceResult.Allowed;
                    } else {
                        result = CanPlaceResult.RankDenied;
                    }
                    break;

                case SecurityCheckResult.WhiteListed:
                    result = CanPlaceResult.Allowed;
                    break;

                default:
                    result = CanPlaceResult.WorldDenied;
                    break;
            }

        eventCheck:
            return RaisePlayerPlacingBlockEvent( this, map, (short)x, (short)y, (short)z, block, newBlock, isManual, result );
        }


        /// <summary> Checks whether this player can currently see another.
        /// Visibility is determined by whether the other player is hiding or spectating. </summary>
        public bool CanSee( [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other == this || IsSuper || !other.Info.IsHidden || Info.Rank.CanSee( other.Info.Rank );
        }

        #endregion


        #region Drawing, Selection, and Undo

        public Queue<BlockUpdate> UndoBuffer = new Queue<BlockUpdate>();

        public IBrush Brush { get; set; }

        public DrawOperation LastDrawOp { get; set; }


        /// <summary> Whether player is currently making a selection. </summary>
        public bool IsMakingSelection {
            get { return SelectionMarksExpected > 0; }
        }

        /// <summary> Number of selection marks so far. </summary>
        public int SelectionMarkCount {
            get { return selectionMarks.Count; }
        }

        /// <summary> Number of marks expected to complete the selection. </summary>
        public int SelectionMarksExpected { get; private set; }


        SelectionCallback selectionCallback;
        readonly Queue<Position> selectionMarks = new Queue<Position>();
        object selectionArgs;
        Permission[] selectionPermissions;


        public void SelectionAddMark( Position pos, bool executeCallbackIfNeeded ) {
            if( !IsMakingSelection ) throw new InvalidOperationException( "No selection in progress." );
            selectionMarks.Enqueue( pos );
            if( SelectionMarkCount >= SelectionMarksExpected ) {
                if( executeCallbackIfNeeded ) {
                    SelectionExecute();
                } else {
                    Message( "Last block marked at ({0},{1},{2}). Type &H/mark&S or click any block to continue.",
                             pos.X, pos.Y, pos.Z );
                }
            } else {
                Message( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                         SelectionMarkCount, pos.X, pos.Y, pos.Z, SelectionMarkCount + 1 );
            }
        }


        public void SelectionExecute() {
            if( !IsMakingSelection ) throw new InvalidOperationException( "No selection in progress." );
            SelectionMarksExpected = 0;
            // check if player still has the permissions required to complete the selection.
            if( selectionPermissions == null || Can( selectionPermissions ) ) {
                selectionCallback( this, selectionMarks.ToArray(), selectionArgs );
            } else {
                // More complex permission checks can be done in the callback function itself.
                Message( "&WYou are no longer allowed to complete this action." );
                MessageNoAccess( selectionPermissions );
            }
        }


        public void SelectionStart( int marksExpected,
                                    [NotNull] SelectionCallback callback,
                                    object args,
                                    params Permission[] requiredPermissions ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            if( requiredPermissions == null ) throw new ArgumentNullException( "requiredPermissions" );
            selectionArgs = args;
            SelectionMarksExpected = marksExpected;
            selectionMarks.Clear();
            selectionCallback = callback;
            selectionPermissions = requiredPermissions;
        }


        public void SelectionResetMarks() {
            selectionMarks.Clear();
        }


        public void SelectionCancel() {
            selectionMarks.Clear();
            SelectionMarksExpected = 0;
            selectionCallback = null;
            selectionArgs = null;
        }

        #endregion


        #region Copy/Paste

        public CopyInformation[] CopyInformation;

        int copySlot;
        public int CopySlot {
            get {
                return copySlot;
            }
            set {
                if( value < 0 || value > Info.Rank.CopySlots ) {
                    throw new ArgumentOutOfRangeException( "value" );
                }
                copySlot = value;
            }
        }

        public CopyInformation GetCopyInformation() {
            return CopyInformation[copySlot];
        }

        public void SetCopyInformation( [CanBeNull] CopyInformation info ) {
            CopyInformation[copySlot] = info;
        }

        #endregion


        #region Spectating

        Player spectatedPlayer;
        /// <summary> Player currently being spectated. Use Spectate/StopSpectate methods to set. </summary>
        public Player SpectatedPlayer {
            get { return spectatedPlayer; }
        }

        public PlayerInfo LastSpectatedPlayer {
            get;
            private set;
        }


        public bool IsSpectating {
            get { return (spectatedPlayer != null); }
        }


        public bool Spectate( [NotNull] Player target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            if( target == this ) throw new ArgumentException( "Cannot spectate self.", "target" );
            Message( "Now spectating {0}&S. Type &H/unspec&S to stop.", target.ClassyName );
            LastSpectatedPlayer = target.Info;
            return (Interlocked.Exchange( ref spectatedPlayer, target ) == null);
        }


        public bool StopSpectating() {
            Player wasSpectating = Interlocked.Exchange( ref spectatedPlayer, null );
            if( wasSpectating != null ) {
                Message( "Stopped spectating {0}", wasSpectating.ClassyName );
                return true;
            } else {
                return false;
            }
        }

        #endregion


        #region Static Utilities

        static readonly Uri PaidCheckUri = new Uri( "http://www.minecraft.net/haspaid.jsp?user=" );
        const int PaidCheckTimeout = 5000;

        // binding delegate for checking the status
        static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            return new IPEndPoint( Server.IP, 0 );
        }


        /// <summary> Checks whether a given player has a paid minecraft.net account. </summary>
        /// <returns> True if the account is paid. False if it is not paid, or if information is unavailable. </returns>
        public static bool CheckPaidStatus( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( PaidCheckUri + Uri.EscapeDataString( name ) );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( BindIPEndPointCallback );
            request.Timeout = PaidCheckTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

            try {
                using( WebResponse response = request.GetResponse() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        string paidStatusString = responseReader.ReadToEnd();
                        bool isPaid;
                        return Boolean.TryParse( paidStatusString, out isPaid ) && isPaid;
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( "Could not check paid status of player {0}: {1}", LogType.Warning,
                            name, ex.Message );
                return false;
            }
        }


        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool IsValidName( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 16 ) return false;
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( (ch < '0' && ch != '.') || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return true;
        }

        #endregion


        public void TeleportTo( Position pos ) {
            StopSpectating();
            Send( PacketWriter.MakeSelfTeleport( pos ) );
        }


        /// <summary> Time since the player was last active (moved, talked, or clicked). </summary>
        public TimeSpan IdleTime {
            get {
                return DateTime.UtcNow.Subtract( LastActiveTime );
            }
        }


        /// <summary> Resets the IdleTimer to 0. </summary>
        public void ResetIdleTimer() {
            LastActiveTime = DateTime.UtcNow;
        }


        /// <summary> Name formatted for the debugger. </summary>
        public override string ToString() {
            if( Info != null ) {
                return String.Format( "Player({0})", Info.Name );
            } else {
                return String.Format( "Player({0})", IP );
            }
        }
    }


    sealed class PlayerListSorter : IComparer<Player> {
        public static readonly PlayerListSorter Instance = new PlayerListSorter();

        public int Compare( Player x, Player y ) {
            if( x.Info.Rank == y.Info.Rank ) {
                return StringComparer.OrdinalIgnoreCase.Compare( x.Name, y.Name );
            } else {
                return x.Info.Rank.Index - y.Info.Rank.Index;
            }
        }
    }
}