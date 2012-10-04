// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Helper class for handling player-generated chat. </summary>
    public static class Chat {
        /// <summary> Sends a global (white) chat. </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendGlobal( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recepientList = Server.Players.NotIgnoring( player );

            string formattedMessage = String.Format( "{0}&F: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Global,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "{0}: {1}", player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends an action message (/Me). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendMe( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recepientList = Server.Players.NotIgnoring( player );

            string formattedMessage = String.Format( "&M*{0} {1}",
                                                     player.Name,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Me,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(me){0}: {1}", player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends a private message (PM). Does NOT send a copy of the message to the sender. </summary>
        /// <param name="from"> Sender player. </param>
        /// <param name="to"> Recepient player. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendPM( [NotNull] Player from, [NotNull] Player to, [NotNull] string rawMessage ) {
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            var recepientList = new[] { to };

            string formattedMessage = String.Format( "&Pfrom {0}: {1}",
                                                     from.Name, rawMessage );

            var e = new ChatSendingEventArgs( from,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.PM,
                                              recepientList );

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
        public static bool SendRank( [NotNull] Player player, [NotNull] Rank rank, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recepientList = rank.Players.NotIgnoring( player ).Union( player );

            string formattedMessage = String.Format( "&P({0}&P){1}: {2}",
                                                     rank.ClassyName,
                                                     player.Name,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Rank,
                                              recepientList );

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
        public static bool SendSay( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recepientList = Server.Players;

            string formattedMessage = Color.Say + rawMessage;

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Say,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(say){0}: {1}", player.Name, rawMessage );
            return true;
        }


        /// <summary> Sends a staff message (/Staff). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendStaff( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            var recepientList = Server.Players.Can( Permission.ReadStaffChat )
                .NotIgnoring( player )
                .Union( player );

            string formattedMessage = String.Format( "&P(staff){0}&P: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(staff){0}: {1}", player.Name, rawMessage );
            return true;
        }


        static bool SendInternal( [NotNull] ChatSendingEventArgs e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            if( RaiseSendingEvent( e ) ) return false;

            Player[] players = e.RecepientList.ToArray();
            int packets = players.Message( e.FormattedMessage );

            // Only increment the MessagesWritten count if someone other than
            // the player was on the recepient list.
            if( players.Length > 1 || ( players.Length == 1 && players[0] != e.Player ) ) {
                e.Player.Info.ProcessMessageWritten();
            }

            RaiseSentEvent( e, packets );
            return true;
        }


        /// <summary> Checks for unprintable or illegal characters in a message. </summary>
        /// <param name="message"> Message to check. </param>
        /// <returns> True if message contains invalid chars. False if message is clean. </returns>
        public static bool ContainsInvalidChars( string message ) {
            return message.Any( t => t < ' ' || t == '&' || t > '~' );
        }


        /// <summary> Determines the type of player-supplies message based on its syntax. </summary>
        internal static RawMessageType GetRawMessageType( string message ) {
            if( string.IsNullOrEmpty( message ) ) return RawMessageType.Invalid;
            if( message == "/" ) return RawMessageType.RepeatCommand;
            if( message.Equals( "/ok", StringComparison.OrdinalIgnoreCase ) ) return RawMessageType.Confirmation;
            if( message.EndsWith( " /" ) ) return RawMessageType.PartialMessage;
            if( message.EndsWith( " //" ) ) message = message.Substring( 0, message.Length - 1 );

            switch( message[0] ) {
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


        /// <summary> Replaces keywords with appropriate values.
        /// See http://www.fcraft.net/wiki/Constants </summary>
        [NotNull]
        public static string ReplaceTextKeywords( [NotNull] Player player, [NotNull] string input ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            sb.Replace( "{SERVER_NAME}", ConfigKey.ServerName.GetString() );
            sb.Replace( "{RANK}", player.Info.Rank.ClassyName );
            sb.Replace( "{TIME}", DateTime.Now.ToShortTimeString() ); // localized
            if( player.World == null ) {
                sb.Replace( "{WORLD}", "(No World)" );
            } else {
                sb.Replace( "{WORLD}", player.World.ClassyName );
            }
            sb.Replace( "{WORLDS}", WorldManager.Worlds.Length.ToStringInvariant() );
            sb.Replace( "{MOTD}", ConfigKey.MOTD.GetString() );
            sb.Replace( "{VERSION}", Updater.CurrentRelease.VersionString );
            if( input.IndexOfOrdinal( "{PLAYER" ) != -1 ) {
                Player[] playerList = Server.Players.CanBeSeen( player ).Union( player ).ToArray();
                sb.Replace( "{PLAYER_NAME}", player.ClassyName );
                sb.Replace( "{PLAYER_LIST}", playerList.JoinToClassyString() );
                sb.Replace( "{PLAYERS}", playerList.Length.ToStringInvariant() );
            }
            return sb.ToString();
        }


        #region Emotes

        static readonly char[] UnicodeReplacements = " ☺☻♥♦♣♠•◘○\n♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼".ToCharArray();

        static readonly Dictionary<string, string> EmoteMacros = new Dictionary<string, string> {
            { "{:)}", "\u0001" }, // ☺
            { "{smile}", "\u0001" },

            { "{smile2}", "\u0002" }, // ☻

            { "{heart}", "\u0003" }, // ♥
            { "{hearts}", "\u0003" },
            { "{<3}", "\u0003" },

            { "{diamond}", "\u0004" }, // ♦
            { "{diamonds}", "\u0004" },
            { "{diams}", "\u0004" },
            { "{rhombus}", "\u0004" },

            { "{club}", "\u0005" }, // ♣
            { "{clubs}", "\u0005" },
            { "{clover}", "\u0005" },
            { "{shamrock}", "\u0005" },

            { "{spade}", "\u0006" }, // ♠
            { "{spades}", "\u0006" },

            { "{*}", "\u0007" }, // •
            { "{bull}", "\u0007" },
            { "{bullet}", "\u0007" },
            { "{dot}", "\u0007" },
            { "{point}", "\u0007" },

            { "{hole}", "\u0008" }, // ◘

            { "{circle}", "\u0009" }, // ○
            { "{o}", "\u0009" },

            { "{male}", "\u000B" }, // ♂
            { "{mars}", "\u000B" },

            { "{female}", "\u000C" }, // ♀
            { "{venus}", "\u000C" },

            { "{8}", "\u000D" }, // ♪
            { "{note}", "\u000D" },
            { "{quaver}", "\u000D" },

            { "{notes}", "\u000E" }, // ♫
            { "{music}", "\u000E" },

            { "{sun}", "\u000F" }, // ☼
            { "{celestia}", "\u000F" },
            
            { "{>>}", "\u0010" }, // ►
            { "{right2}", "\u0010" },

            { "{<<}", "\u0011" }, // ◄
            { "{left2}", "\u0011" },
            
            { "{updown}", "\u0012" }, // ↕
            { "{^v}", "\u0012" },

            { "{!!}", "\u0013" }, // ‼

            { "{P}", "\u0014" }, // ¶
            { "{para}", "\u0014" },
            { "{pilcrow}", "\u0014" },
            { "{paragraph}", "\u0014" },

            { "{S}", "\u0015" }, // §
            { "{sect}", "\u0015" },
            { "{section}", "\u0015" },

            { "{-}", "\u0016" }, // ▬
            { "{_}", "\u0016" },
            { "{bar}", "\u0016" },
            { "{half}", "\u0016" },

            { "{updown2}", "\u0017" }, // ↨
            { "{^v_}", "\u0017" },

            { "{^}", "\u0018" }, // ↑
            { "{up}", "\u0018" },
            { "{uarr}", "\u0018" },

            { "{v}", "\u0019" }, // ↓
            { "{down}", "\u0019" },
            { "{darr}", "\u0019" },
            
            { "{>}", "\u001A" }, // →
            { "{->}", "\u001A" },
            { "{right}", "\u001A" },
            { "{rarr}", "\u001A" },
            
            { "{<}", "\u001B" }, // ←
            { "{<-}", "\u001B" },
            { "{left}", "\u001B" },
            { "{larr}", "\u001B" },

            { "{L}", "\u001C" }, // ∟
            { "{angle}", "\u001C" },
            { "{corner}", "\u001C" },

            { "{<>}", "\u001D" }, // ↔
            { "{<->}", "\u001D" },
            { "{leftright}", "\u001D" },
            { "{harrow}", "\u001D" },
            
            { "{^^}", "\u001E" }, // ▲
            { "{up2}", "\u001E" },

            { "{vv}", "\u001F" }, // ▼
            { "{down2}", "\u001F" },
        };


        [NotNull]
        public static string ReplaceEmoteMacros( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            foreach( var pair in EmoteMacros ) {
                sb.Replace( pair.Key, pair.Value );
            }
            return sb.ToString();
        }


        /// <summary> Replaces UTF-8 symbol characters with ASCII control characters, matching Code Page 437.
        /// Opposite of ReplaceEmotesWithUncode. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> Processed string, with its UTF-8 symbol characters replaced. </returns>
        /// <exception cref="ArgumentNullException"> If input is null. </exception>
        [NotNull]
        public static string ReplaceUncodeWithEmotes( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            for( int i = 1; i < UnicodeReplacements.Length; i++ ) {
                sb.Replace( UnicodeReplacements[i], (char)i );
            }
            return sb.ToString();
        }


        /// <summary> Replaces ASCII control characters with UTF-8 symbol characters, matching Code Page 437. 
        /// Opposite of ReplaceUncodeWithEmotes. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> Processed string, with its ASCII control characters replaced. </returns>
        /// <exception cref="ArgumentNullException"> If input is null. </exception>
        [NotNull]
        public static string ReplaceEmotesWithUncode( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            for( int i = 1; i < UnicodeReplacements.Length; i++ ) {
                sb.Replace( (char)i, UnicodeReplacements[i] );
            }
            return sb.ToString();
        }

        #endregion


        #region Events

        static bool RaiseSendingEvent( ChatSendingEventArgs args ) {
            var h = Sending;
            if( h == null ) return false;
            h( null, args );
            return args.Cancel;
        }


        static void RaiseSentEvent( ChatSendingEventArgs args, int count ) {
            var h = Sent;
            if( h != null )
                h( null, new ChatSentEventArgs( args.Player, args.Message, args.FormattedMessage,
                                                args.MessageType, args.RecepientList, count ) );
        }


        /// <summary> Occurs when a chat message is about to be sent. Cancelable. </summary>
        public static event EventHandler<ChatSendingEventArgs> Sending;

        /// <summary> Occurs after a chat message has been sent. </summary>
        public static event EventHandler<ChatSentEventArgs> Sent;

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

        /// <summary> Normal (white) chat. </summary>
        Chat,

        /// <summary> Command. </summary>
        Command,

        /// <summary> Confirmation (/ok) for a previous command. </summary>
        Confirmation,

        /// <summary> Partial message (ends with " /"). </summary>
        PartialMessage,

        /// <summary> Private message. </summary>
        PrivateChat,

        /// <summary> Rank chat. </summary>
        RankChat,

        /// <summary> Repeat of the last command ("/"). </summary>
        RepeatCommand,
    }
}


namespace fCraft.Events {
    /// <summary> Provides data for Chat.Sending event. Cancelable.
    /// FormattedMessage and recipientList properties may be changed. </summary>
    public sealed class ChatSendingEventArgs : EventArgs, IPlayerEvent, ICancelableEvent {
        internal ChatSendingEventArgs( Player player, string message, string formattedMessage,
                                       ChatMessageType messageType, IEnumerable<Player> recepientList ) {
            Player = player;
            Message = message;
            MessageType = messageType;
            RecepientList = recepientList;
            FormattedMessage = formattedMessage;
        }


        /// <summary> Player who is sending the message. </summary>
        public Player Player { get; private set; }

        /// <summary> Raw text of the message. </summary>
        public string Message { get; private set; }

        /// <summary> Formatted message, as it will appear to the recepients. </summary>
        public string FormattedMessage { get; set; }

        /// <summary> Type of the message that's being sent. </summary>
        public ChatMessageType MessageType { get; private set; }

        /// <summary> List of intended recepients. </summary>
        public readonly IEnumerable<Player> RecepientList;

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Chat.Sent event. Immutable. </summary>
    public sealed class ChatSentEventArgs : EventArgs, IPlayerEvent {
        internal ChatSentEventArgs( Player player, string message, string formattedMessage,
                                    ChatMessageType messageType, IEnumerable<Player> recepientList, int packetCount ) {
            Player = player;
            Message = message;
            MessageType = messageType;
            RecepientList = recepientList;
            FormattedMessage = formattedMessage;
            PacketCount = packetCount;
        }


        /// <summary> Player who sent the message. </summary>
        public Player Player { get; private set; }

        /// <summary> Raw text of the message. </summary>
        public string Message { get; private set; }

        /// <summary> Formatted message, as it appeared to the recepients. </summary>
        public string FormattedMessage { get; private set; }

        /// <summary> Type of message that was sent. </summary>
        public ChatMessageType MessageType { get; private set; }

        /// <summary> List of players who received the message. </summary>
        public IEnumerable<Player> RecepientList { get; private set; }

        /// <summary> Number of message packets that were sent out. </summary>
        public int PacketCount { get; private set; }
    }
}