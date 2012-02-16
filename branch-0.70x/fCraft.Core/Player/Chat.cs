// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Helper class for handling player-generated chat. </summary>
    public static class Chat {
        /// <summary> Sends a global (white) chat message. </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        /// <exception cref="ArgumentNullException"> If player or rawMessage is null. </exception>
        public static bool SendGlobal( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recipientList = Server.Players.NotIgnoring( player );

            string formattedMessage = String.Format( "{0}&F: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Global,
                                              recipientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "{0}: {1}", player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends world/local chat message. </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        /// <exception cref="ArgumentNullException"> If player or rawMessage is null. </exception>
        public static bool SendWorld( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            var recipientList = playerWorld.Players
                                           .NotIgnoring( player );

            string formattedMessage = String.Format( "({0}&F){1}: {2}",
                                                     playerWorld.ClassyName,
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.World,
                                              recipientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(world {0}){1}: {2}",
                        playerWorld.Name, player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends an action message (/Me). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        /// <exception cref="ArgumentNullException"> If player or rawMessage is null. </exception>
        public static bool SendMe( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recipientList = Server.Players.NotIgnoring( player );

            string formattedMessage = String.Format( "&M*{0} {1}",
                                                     player.Name,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Me,
                                              recipientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(me){0}: {1}", player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends a private message (PM). Does NOT send a copy of the message to the sender. </summary>
        /// <param name="from"> Sender player. </param>
        /// <param name="to"> recipient player. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        /// <exception cref="ArgumentNullException"> If from-player, to-player, or rawMessage is null. </exception>
        public static bool SendPM( [NotNull] Player from, [NotNull] Player to, [NotNull] string rawMessage ) {
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            var recipientList = new[] { to }
                                    .Union( Server.Players.Spectating( from ) )
                                    .Union( Server.Players.Spectating( to ) );

            string formattedMessage = String.Format( "&Pfrom {0}: {1}",
                                                     from.Name, rawMessage );

            var e = new ChatSendingEventArgs( from,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.PM,
                                              recipientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.PrivateChat,
                        "{0} to {1}: {2}",
                        from.Name, to.Name, rawMessage );
            return true;
        }


        /// <summary> Sends a rank-wide message (@@Rank message). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rank"> Target rank. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        /// <exception cref="ArgumentNullException"> If player, rank, or rawMessage is null. </exception>
        public static bool SendRank( [NotNull] Player player, [NotNull] Rank rank, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recipientList = rank.Players
                                    .NotIgnoring( player )
                                    .Union( player )
                                    .Union( Server.Players.Spectating( player ) );

            string formattedMessage = String.Format( "&P({0}&P){1}: {2}",
                                                     rank.ClassyName,
                                                     player.Name,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Rank,
                                              recipientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.RankChat,
                        "(rank {0}){1}: {2}",
                        rank.Name, player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends a global announcement (/Say). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        /// <exception cref="ArgumentNullException"> If player or rawMessage is null. </exception>
        public static bool SendSay( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recipientList = Server.Players.NotIgnoring( player );

            string formattedMessage = Color.Say + rawMessage;

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Say,
                                              recipientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(say){0}: {1}", player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends a staff message (/Staff). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        /// <exception cref="ArgumentNullException"> If player or rawMessage is null. </exception>
        public static bool SendStaff( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recipientList = Server.Players
                                      .Can( Permission.ReadStaffChat )
                                      .NotIgnoring( player )
                                      .Union( player );

            string formattedMessage = String.Format( "&P(staff){0}&P: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recipientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(staff){0}: {1}", player.Name, rawMessage );
            return true;
        }


        static bool SendInternal( [NotNull] ChatSendingEventArgs e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            SendingEvent.Raise( e );
            if( e.Cancel ) return false;

            int recipients = e.recipientList.Message( e.FormattedMessage );

            // Only increment the MessagesWritten count if someone other than
            // the player was on the recipient list.
            if( recipients > 1 || (recipients == 1 && e.recipientList.First() != e.Player) ) {
                e.Player.Info.ProcessMessageWritten();
            }

            SentEvent.Raise( new ChatSentEventArgs( e, recipients ) );
            return true;
        }


        /// <summary> Checks for unprintable or illegal characters in a message. </summary>
        /// <param name="message"> Message to check. May not be null. </param>
        /// <returns> True if message contains invalid chars. False if message is clean. </returns>
        /// <exception cref="ArgumentNullException"> If message is null. </exception>
        public static bool ContainsInvalidChars( [NotNull] IEnumerable<char> message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            return message.Any( t => t < ' ' || t == '&' || t > '~' );
        }


        /// <summary> Determines the type of player-supplies message based on its syntax. </summary>
        internal static RawMessageType GetRawMessageType( [CanBeNull] string message ) {
            if( string.IsNullOrEmpty( message ) ) return RawMessageType.Invalid;
            if( message == "/" ) return RawMessageType.RepeatCommand;
            if( message.Equals( "/ok", StringComparison.OrdinalIgnoreCase ) ) return RawMessageType.Confirmation;
            if( message.EndsWith( " /" ) ) return RawMessageType.PartialMessage;
            if( message.EndsWith( " //" ) ) message = message.Substring( 0, message.Length - 1 );

            switch( message[0] ) {
                case '!':
                    if( ConfigKey.SeparateWorldAndGlobalChat.Enabled() ) {
                        return RawMessageType.WorldChat;
                    } else {
                        return RawMessageType.Chat;
                    }

                case '/':
                    if( message.Length < 2 ) {
                        // message too short to be a command
                        return RawMessageType.Invalid;
                    }
                    if( message[1] == '/' ) {
                        // escaped slash in the beginning: "//blah"
                        return RawMessageType.Chat;
                    }
                    if( message[1] != ' ' ) {
                        // normal command: "/cmd"
                        return RawMessageType.Command;
                    }
                    return RawMessageType.Invalid;

                case '@':
                    if( message.Length < 4 || message.IndexOf( ' ' ) == -1 ) {
                        // message too short to be a PM or rank chat
                        return RawMessageType.Invalid;
                    }
                    if( message[1] == '@' ) {
                        return RawMessageType.RankChat;
                    }
                    if( message[1] == '-' && message[2] == ' ' ) {
                        // name shortcut: "@- blah"
                        return RawMessageType.PrivateChat;
                    }
                    if( message[1] == ' ' && message.IndexOf( ' ', 2 ) != -1 ) {
                        // alternative PM notation: "@ name blah"
                        return RawMessageType.PrivateChat;
                    }
                    if( message[1] != ' ' ) {
                        // primary PM notation: "@name blah"
                        return RawMessageType.PrivateChat;
                    }
                    return RawMessageType.Invalid;
            }
            return RawMessageType.Chat;
        }



        /// <summary> Replaces leading "//" with "/". </summary>
        /// <param name="rawMessage"> Message to unescape. </param>
        /// <returns> Unescaped message. </returns>
        /// <exception cref="ArgumentNullException"> If rawMessage is null. </exception>
        public static string UnescapeLeadingSlashes( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            if( rawMessage.StartsWith( "//" ) ) {
                rawMessage = rawMessage.Substring( 1 );
            }
            return rawMessage;
        }



        /// <summary> Replaces trailing "//" with "/". </summary>
        /// <param name="rawMessage"> Message to escape. </param>
        /// <returns> Escaped message. </returns>
        /// <exception cref="ArgumentNullException"> If rawMessage is null. </exception>
        public static string UnescapeTrailingSlashes( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            if( rawMessage.EndsWith( "//" ) ) {
                rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
            }
            return rawMessage;
        }


        #region Events

        /// <summary> Occurs when a chat message is about to be sent. Cancellable. </summary>
        public static event EventHandler<ChatSendingEventArgs> Sending {
            add { SendingEvent.Add( value, Priority.Normal ); }
            remove { SendingEvent.Remove( value ); }
        }
        public static void SendingPriority( [NotNull] EventHandler<ChatSendingEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            SendingEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<ChatSendingEventArgs> SendingEvent = new PriorityEvent<ChatSendingEventArgs>();


        /// <summary> Occurs after a chat message has been sent. </summary>
        public static event EventHandler<ChatSentEventArgs> Sent {
            add { SentEvent.Add( value, Priority.Normal ); }
            remove { SentEvent.Remove( value ); }
        }
        public static void SentPriority( [NotNull] EventHandler<ChatSentEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            SentEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<ChatSentEventArgs> SentEvent = new PriorityEvent<ChatSentEventArgs>();

        #endregion
    }


    /// <summary> Type of a broadcast chat message. </summary>
    public enum ChatMessageType {
        /// <summary> Unknown or custom chat message type. </summary>
        Other,

        /// <summary> Global (white) chat message. </summary>
        Global,

        /// <summary> Message directed to or from IRC. </summary>
        IRC,

        /// <summary> Message produced by /Me command (action). </summary>
        Me,

        /// <summary> Private message (@Player message). </summary>
        PM,

        /// <summary> Rank-wide message (@@Rank message). </summary>
        Rank,

        /// <summary> Message produced by /Say command (global announcement). </summary>
        Say,

        /// <summary> Message produced by /Staff command. </summary>
        Staff,

        /// <summary> Local (world) chat message. </summary>
        World
    }



    /// <summary> Type of message sent by the player. Determined by CommandManager.GetMessageType() </summary>
    public enum RawMessageType {
        /// <summary> Unparseable chat syntax (rare). </summary>
        Invalid,

        /// <summary> Normal/global (white) chat. Corresponds to ChatMessageType.Global </summary>
        Chat,

        /// <summary> Local (world) chat message. Corresponds to ChatMessageType.World </summary>
        WorldChat,

        /// <summary> Command call. </summary>
        Command,

        /// <summary> Confirmation (/ok) for a previous command. </summary>
        Confirmation,

        /// <summary> Partial message (ends with " /"). </summary>
        PartialMessage,

        /// <summary> Private message. Corresponds to ChatMessageType.PM </summary>
        PrivateChat,

        /// <summary> Rank chat. Corresponds to ChatMessageType.Rank </summary>
        RankChat,

        /// <summary> Repeat of the last command ("/"). </summary>
        RepeatCommand,
    }
}


namespace fCraft.Events {

    /// <summary> Provides data for Chat.Sending event. Cancellable.
    /// FormattedMessage and recipientList properties may be changed. </summary>
    public sealed class ChatSendingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent {
        internal ChatSendingEventArgs( Player player, string message, string formattedMessage,
                                       ChatMessageType messageType, IEnumerable<Player> recipientList ) {
            Player = player;
            Message = message;
            MessageType = messageType;
            recipientList = recipientList;
            FormattedMessage = formattedMessage;
        }

        public Player Player { get; private set; }
        public string Message { get; private set; }
        public string FormattedMessage { get; set; }
        public ChatMessageType MessageType { get; private set; }
        public IEnumerable<Player> recipientList { get; set; }
        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Chat.Sent event. Immutable. </summary>
    public sealed class ChatSentEventArgs : EventArgs, IPlayerEvent {
        internal ChatSentEventArgs( ChatSendingEventArgs e, int recipientCount ) {
            Player = e.Player;
            Message = e.Message;
            MessageType = e.MessageType;
            recipientList = e.recipientList;
            FormattedMessage = e.FormattedMessage;
            recipientCount = recipientCount;
        }

        public Player Player { get; private set; }
        public string Message { get; private set; }
        public string FormattedMessage { get; private set; }
        public ChatMessageType MessageType { get; private set; }
        public IEnumerable<Player> recipientList { get; private set; }
        public int recipientCount { get; private set; }
    }
}