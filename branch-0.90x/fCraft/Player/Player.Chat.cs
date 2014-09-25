// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    public sealed partial class Player {
        static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds( 60 );
        int muteWarnings;

        [CanBeNull]
        string partialMessage;

        /// <summary> Most recently used player name, used to substitute "-" in commands.
        /// May be null if no player names have been used yet. </summary>
        [CanBeNull]
        public string LastUsedPlayerName { get; set; }

        /// <summary> Most recently used world name, used to substitute "-" in commands.
        /// May be null if no world names have been used yet. </summary>
        [CanBeNull]
        public string LastUsedWorldName { get; set; }

        // Used by /Re, set by Chat.SendPM
        [CanBeNull]
        internal string LastPrivateMessageSender;


        /// <summary> Parses a message on behalf of this player. </summary>
        /// <param name="rawMessage"> Message to parse. </param>
        /// <param name="fromConsole"> Whether the message originates from console. </param>
        /// <exception cref="ArgumentNullException"> rawMessage is null. </exception>
        public void ParseMessage( [NotNull] string rawMessage, bool fromConsole ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            // handle canceling selections and partial messages
            if( rawMessage.StartsWith( "/nvm", StringComparison.OrdinalIgnoreCase ) ||
                rawMessage.StartsWith( "/cancel", StringComparison.OrdinalIgnoreCase ) ) {
                if( partialMessage != null ) {
                    MessageNow( "Partial message cancelled." );
                    partialMessage = null;
                } else if( IsMakingSelection ) {
                    SelectionCancel();
                    MessageNow( "Selection cancelled." );
                } else {
                    MessageNow( "There is currently nothing to cancel." );
                }
                return;
            }

            if( partialMessage != null ) {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }

            // replace %-codes with &-codes
            if( Can( Permission.UseColorCodes ) ) {
                rawMessage = Chat.ReplacePercentColorCodes( rawMessage, true );
            }
            // replace emotes
            if( Can( Permission.UseEmotes ) ) {
                rawMessage = Chat.ReplaceEmoteKeywords( rawMessage );
            }
            rawMessage = Chat.UnescapeBackslashes( rawMessage );

            switch( Chat.GetRawMessageType( rawMessage ) ) {
                case RawMessageType.Chat:
                    HandleChatMessage( rawMessage );
                    break;

                case RawMessageType.Command:
                    HandleCommandMessage( rawMessage, fromConsole );
                    break;

                case RawMessageType.PrivateChat:
                    HandlePrivateChatMessage( rawMessage );
                    break;

                case RawMessageType.RankChat:
                    HandleRankChatMessage( rawMessage );
                    break;

                case RawMessageType.RepeatCommand:
                    if( LastCommand == null ) {
                        Message( "No command to repeat." );
                    } else {
                        if( Info.IsFrozen && (LastCommand.Descriptor == null ||
                                              !LastCommand.Descriptor.UsableByFrozenPlayers) ) {
                            MessageNow( "&WYou cannot use this command while frozen." );
                            return;
                        }
                        LastCommand.Rewind();
                        Logger.Log( LogType.UserCommand,
                                    "{0} repeated: {1}",
                                    Name,
                                    LastCommand.RawMessage );
                        Message( "Repeat: {0}", LastCommand.RawMessage );
                        SendToSpectators( LastCommand.RawMessage );
                        CommandManager.ParseCommand( this, LastCommand, fromConsole );
                    }
                    break;

                case RawMessageType.Confirmation:
                    if( Info.IsFrozen ) {
                        MessageNow( "&WYou cannot use any commands while frozen." );
                        return;
                    }
                    if( ConfirmCallback != null ) {
                        if( DateTime.UtcNow.Subtract( ConfirmRequestTime ) < ConfirmationTimeout ) {
                            Logger.Log( LogType.UserCommand, "{0}: /ok", Name );
                            SendToSpectators( "/ok" );
                            ConfirmCallback( this, ConfirmParameter, fromConsole );
                            ConfirmCancel();
                        } else {
                            MessageNow( "Confirmation timed out. Enter the command again." );
                        }
                    } else {
                        MessageNow( "There is no command to confirm." );
                    }
                    break;

                case RawMessageType.PartialMessage:
                    partialMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                    MessageNow( "Partial: &F{0}", partialMessage );
                    break;

                case RawMessageType.Invalid:
                    MessageNow( "Could not parse message." );
                    break;
            }
        }


        void HandleRankChatMessage( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
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
                    return;
                }
            }

            string messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );

            Player[] spectators = Server.Players.NotRanked( Info.Rank )
                                        .Where( p => p.spectatedPlayer == this )
                                        .ToArray();
            if( spectators.Length > 0 ) {
                spectators.Message( "[Spectate]: &Fto rank {0}&F: {1}", rank.ClassyName, messageText );
            }

            Chat.SendRank( this, rank, messageText );
        }


        void HandlePrivateChatMessage( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
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

            if( otherPlayerName == "-" ) {
                if( LastUsedPlayerName != null ) {
                    otherPlayerName = LastUsedPlayerName;
                } else {
                    Message( "Cannot repeat player name: you haven't used any names yet." );
                    return;
                }
            }

            // first, find ALL players (visible and hidden)
            Player[] allPlayers = Server.FindPlayers( otherPlayerName, SearchOptions.Default );

            // if there is more than 1 target player, exclude hidden players
            if( allPlayers.Length > 1 ) {
                allPlayers = Server.FindPlayers( this, otherPlayerName, SearchOptions.ReturnSelfIfOnlyMatch );
            }

            switch( allPlayers.Length ) {
                case 0:
                    MessageNoPlayer( otherPlayerName );
                    break;
                case 1: {
                    Player target = allPlayers[0];
                    if( target == this ) {
                        Message( "Trying to talk to yourself?" );
                        return;
                    }
                    bool messageSent = false;
                    if( target.CanHear(this) ) {
                        messageSent = Chat.SendPM(this, target, messageText);
                        // Echo this message to spectators,
                        // excluding the PM target, and anyone from whom the target is hiding.
                        Server.Players
                              .Where(p => p.spectatedPlayer == this && p != target && p.CanSee(target))
                              .Message("[Spectate]: &Fto {0}&F: {1}", target.ClassyName, messageText);
                    }

                    if( !CanSee( target ) ) {
                        // message was sent to a hidden player
                        MessageNoPlayer( otherPlayerName );
                        if( messageSent ) {
                            Info.DecrementMessageWritten();
                        }
                    } else {
                        // message was sent normally
                        LastUsedPlayerName = target.Name;
                        if( target.IsIgnoring( Info ) ) {
                            if( CanSee( target ) ) {
                                MessageNow( "&WCannot PM {0}&W: you are ignored.", target.ClassyName );
                            }
                        } else if( target.IsDeaf ) {
                            MessageNow( "Cannot PM {0}&S: they are currently deaf.", target.ClassyName );
                        } else {
                            MessageNow( "&Pto {0}: {1}",
                                        target.Name,
                                        messageText );
                        }
                    }
                }
                    break;
                default:
                    MessageManyMatches( "player", allPlayers );
                    break;
            }
        }


        void HandleCommandMessage( [NotNull] string rawMessage, bool fromConsole ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            if( rawMessage.EndsWith( "//" ) ) {
                rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
            }
            CommandReader cmd = new CommandReader( rawMessage );

            if( cmd.Descriptor == null ) {
                MessageNow( "Unknown command \"{0}\". See &H/Commands", cmd.Name );
            } else if( Info.IsFrozen && !cmd.Descriptor.UsableByFrozenPlayers ) {
                MessageNow( "&WYou cannot use this command while frozen." );
            } else {
                if( !cmd.Descriptor.DisableLogging ) {
                    Logger.Log( LogType.UserCommand,
                                "{0}: {1}",
                                Name,
                                rawMessage );
                }
                if( cmd.Descriptor.RepeatableSelection ) {
                    selectionRepeatCommand = cmd;
                }
                SendToSpectators( cmd.RawMessage );
                CommandManager.ParseCommand( this, cmd, fromConsole );
                if( !cmd.Descriptor.NotRepeatable ) {
                    LastCommand = cmd;
                }
            }
        }


        void HandleChatMessage( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
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

            Chat.SendGlobal( this, rawMessage );
        }


        /// <summary> Sends a message to all players who are spectating this player, e.g. to forward typed-in commands and PMs.
        /// "System color" code (&amp;S) will be prepended to the message.
        /// If the message does not fit on one line, prefix ">" is prepended to each wrapped line. </summary>
        /// <param name="message"> A composite format string for the message. Same semantics as String.Format(). </param>
        /// <param name="formatArgs"> An object array that contains zero or more objects to format.  </param>
        /// <exception cref="ArgumentNullException"> message or formatArgs is null. </exception>
        /// <exception cref="FormatException"> Message format is invalid. </exception>
        public void SendToSpectators( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            Player[] spectators = Server.Players.Where( p => p.spectatedPlayer == this ).ToArray();
            if( spectators.Length > 0 ) {
                spectators.Message( "[Spectate]: &F" + message, formatArgs );
            }
        }


        /// <summary> Sends a text message to this player. "System color" code (&amp;S) will be prepended to the message. 
        /// If the message does not fit on one line, prefix ">" is prepended to each wrapped line. </summary>
        /// <param name="message"> A composite format string for the message. Same semantics as String.Format(). </param>
        /// <param name="formatArgs"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> message or formatArgs is null. </exception>
        /// <exception cref="FormatException"> Message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void Message( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
            }
            if( IsSuper ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in LineWrapper.Wrap( ChatColor.Sys + message ) ) {
                    Send( p );
                }
            }
        }


        /// <summary> Sends a text message to this player, prefixing each line. </summary>
        /// <param name="prefix"> Prefix to prepend to prepend to each line after the 1st,
        /// if any line-wrapping occurs. Does NOT get prepended to first line. </param>
        /// <param name="message"> A composite format string for the message. Same semantics as String.Format(). </param>
        /// <param name="formatArgs"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> prefix, message, or formatArgs is null. </exception>
        /// <exception cref="FormatException"> Message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void MessagePrefixed( [NotNull] string prefix, [NotNull] string message,
                                     [NotNull] params object[] formatArgs ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
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
                foreach( Packet p in LineWrapper.Wrap( ChatColor.Sys + message ) ) {
                    SendNow( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNowPrefixed( [NotNull] string prefix, [NotNull] string message,
                                          [NotNull] params object[] args ) {
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


        /// <summary> Checks whether this player can hear messages from given sender. 
        /// Deaf and ignoring players will not hear the messages. </summary>
        /// <returns> True if this player will see messages from sender; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool CanHear( [NotNull] Player sender ) {
            if( sender == null ) throw new ArgumentNullException( "sender" );
            return !IsDeaf && !IsIgnoring( sender.Info );
        }

        #region Macros

        const int MatchesToPrint = 30;

        /// <summary> Prints a comma-separated list of matches (up to 30): "More than one ___ matched: ___, ___, ..." </summary>
        /// <param name="itemType"> Type of item in the list. Should be singular (e.g. "player" or "world"). </param>
        /// <param name="items"> List of zero or more matches. ClassyName properties are used in the list. </param>
        /// <exception cref="ArgumentNullException"> itemType or items is null. </exception>
        public void MessageManyMatches( [NotNull] string itemType, [NotNull] IEnumerable<IClassy> items ) {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            if( items == null ) throw new ArgumentNullException( "items" );

            IClassy[] itemsEnumerated = items.ToArray();
            string nameList = itemsEnumerated.Take( MatchesToPrint ).JoinToString( ", ", p => p.ClassyName );
            int count = itemsEnumerated.Length;
            if( count > MatchesToPrint ) {
                Message( "More than {0} {1} matched: {2}",
                         count,
                         itemType,
                         nameList );
            } else {
                Message( "More than one {0} matched: {1}",
                         itemType,
                         nameList );
            }
        }


        /// <summary> Prints "No players found matching ___" message. </summary>
        /// <param name="playerName"> Given name, for which no players were found. </param>
        /// <exception cref="ArgumentNullException"> playerName is null. </exception>
        public void MessageNoPlayer( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Message( "No players found matching \"{0}\"", playerName );
        }


        /// <summary> Prints "No worlds found matching ___" message. </summary>
        /// <param name="worldName"> Given name, for which no worlds were found. </param>
        /// <exception cref="ArgumentNullException"> worldName is null. </exception>
        public void MessageNoWorld( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Message( "No worlds found matching \"{0}\". See &H/Worlds", worldName );
        }


        /// <summary> Prints "This command requires ___+ rank" message. </summary>
        /// <param name="permissions"> List of permissions required for the command. </param>
        /// <exception cref="ArgumentNullException"> permissions is null. </exception>
        /// <exception cref="ArgumentException"> permissions array is empty. </exception>
        public void MessageNoAccess( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            if( permissions.Length == 0 ) throw new ArgumentException( "At least one permission required.", "permissions" );
            Rank reqRank = RankManager.GetMinRankWithAllPermissions( permissions );
            if( reqRank == null ) {
                Message( "None of the ranks have permissions for this command." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.ClassyName );
            }
        }


        /// <summary> Prints "This command requires ___+ rank" message. </summary>
        /// <param name="cmd"> Command to check. </param>
        /// <exception cref="ArgumentNullException"> cmd is null. </exception>
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


        /// <summary> Prints "Unrecognized rank ___" message. </summary>
        /// <param name="rankName"> Given name, for which no rank was found. </param>
        public void MessageNoRank( [NotNull] string rankName ) {
            if( rankName == null ) throw new ArgumentNullException( "rankName" );
            Message( "Unrecognized rank \"{0}\". See &H/Ranks", rankName );
        }


        /// <summary> Prints "You cannot access files outside the map folder." message. </summary>
        public void MessageUnsafePath() {
            Message( "&WYou cannot access files outside the map folder." );
        }


        /// <summary> Prints "No zones found matching ___" message. </summary>
        /// <param name="zoneName"> Given name, for which no zones was found. </param>
        public void MessageNoZone( [NotNull] string zoneName ) {
            if( zoneName == null ) throw new ArgumentNullException( "zoneName" );
            Message( "No zones found matching \"{0}\". See &H/Zones", zoneName );
        }


        /// <summary> Prints "Unacceptable world name" message, and requirements for world names. </summary>
        /// <param name="worldName"> Given world name, deemed to be invalid. </param>
        public void MessageInvalidWorldName( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Message( "Unacceptable world name: \"{0}\"", worldName );
            Message( "World names must be 1-16 characters long, and only contain letters, numbers, and underscores." );
        }


        /// <summary> Prints "___ is not a valid player name" message. </summary>
        /// <param name="playerName"> Given player name, deemed to be invalid. </param>
        public void MessageInvalidPlayerName( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Message( "\"{0}\" is not a valid player name.", playerName );
        }


        /// <summary> Prints "You are muted for ___ longer" message. </summary>
        public void MessageMuted() {
            Message( "You are muted for {0} longer.",
                     Info.TimeMutedLeft.ToMiniString() );
        }


        /// <summary> Prints "Specify a time range up to ___" message </summary>
        public void MessageMaxTimeSpan() {
            Message( "Specify a time range up to {0}", DateTimeUtil.MaxTimeSpan.ToMiniString() );
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
        [NotNull]
        public PlayerInfo[] IgnoreList {
            get {
                lock( ignoreLock ) {
                    return ignoreList.ToArray();
                }
            }
        }

        #endregion

        #region Confirmation

        /// <summary> Callback to be called when player types in "/ok" to confirm an action.
        /// Use Player.Confirm(...) methods to set this. </summary>
        [CanBeNull]
        public ConfirmationCallback ConfirmCallback { get; private set; }


        /// <summary> Custom parameter to be passed to Player.ConfirmCallback. </summary>
        [CanBeNull]
        public object ConfirmParameter { get; private set; }


        /// <summary> Time when the confirmation was requested. UTC. </summary>
        public DateTime ConfirmRequestTime { get; private set; }


        static void ConfirmCommandCallback( [NotNull] Player player, [NotNull] object tag, bool fromConsole ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( tag == null ) throw new ArgumentNullException( "tag" );
            CommandReader cmd = (CommandReader)tag;
            cmd.Rewind();
            cmd.IsConfirmed = true;
            CommandManager.ParseCommand( player, cmd, fromConsole );
        }


        /// <summary> Request player to confirm continuing with the command.
        /// Player is prompted to type "/ok", and when he/she does,
        /// the command is called again with IsConfirmed flag set. </summary>
        /// <param name="cmd"> Command that needs confirmation. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="formatArgs"> Optional String.Format() arguments, for the message. </param>
        /// <exception cref="ArgumentNullException"> cmd, message, or formatArgs is null. </exception>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] CommandReader cmd, [NotNull] string message, [NotNull] params object[] formatArgs ) {
            Confirm( ConfirmCommandCallback, cmd, message, formatArgs );
        }


        /// <summary> Request player to confirm an action.
        /// Player is prompted to type "/ok", and when he/she does, custom callback will be called </summary>
        /// <param name="callback"> Method to call when player confirms. </param>
        /// <param name="callbackParameter"> Argument to pass to the callback. May be null. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="formatArgs"> Optional String.Format() arguments, for the message. </param>
        /// <exception cref="ArgumentNullException"> callback, message, or formatArgs is null. </exception>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] ConfirmationCallback callback, [CanBeNull] object callbackParameter,
                             [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            ConfirmCallback = callback;
            ConfirmParameter = callbackParameter;
            ConfirmRequestTime = DateTime.UtcNow;
            Message( "{0} Type &H/ok&S to continue.", String.Format( message, formatArgs ) );
        }


        /// <summary> Cancels any pending confirmation (/ok) prompt. </summary>
        /// <returns> True if a confirmation prompt was pending; otherwise false. </returns>
        public bool ConfirmCancel() {
            if( ConfirmCallback != null ) {
                ConfirmCallback = null;
                ConfirmParameter = null;
                return true;
            } else {
                return false;
            }
        }

        #endregion

        #region AntiSpam

        /// <summary> Number of messages in a AntiSpamInterval seconds required to trigger the anti-spam filter </summary>
        public static int AntispamMessageCount = 3;

        /// <summary> Interval in seconds to record number of message for anti-spam filter </summary>
        public static int AntispamInterval = 4;

        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>( AntispamMessageCount );


        internal bool DetectChatSpam() {
            if( IsSuper || AntispamMessageCount < 1 || AntispamInterval < 1 ) return false;
            if( spamChatLog.Count >= AntispamMessageCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < AntispamInterval ) {
                    muteWarnings++;
                    int maxMuteWarnings = ConfigKey.AntispamMaxWarnings.GetInt();
                    if( maxMuteWarnings > 0 && muteWarnings > maxMuteWarnings ) {
                        KickNow( "You were kicked for repeated spamming.", LeaveReason.MessageSpamKick );
                        Server.Message( "&WPlayer {0}&W was kicked for spamming.", ClassyName );
                    } else {
                        TimeSpan autoMuteDuration = TimeSpan.FromSeconds( ConfigKey.AntispamMuteDuration.GetInt() );
                        if( autoMuteDuration > TimeSpan.Zero ) {
                            Info.Mute( Console, autoMuteDuration, false, true );
                            Message( "&WYou have been muted for {0} seconds. Slow down.", autoMuteDuration );
                        } else {
                            Message( "&WYou are sending messages too quickly. Slow down." );
                        }
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion
    }


    /// <summary> Represents the method that responds to a confirmation command. </summary>
    /// <param name="player"> Player who confirmed the action. </param>
    /// <param name="tag"> Parameter that was passed to Player.Confirm() </param>
    /// <param name="fromConsole"> Whether player is console. </param>
    public delegate void ConfirmationCallback( Player player, object tag, bool fromConsole );
}
