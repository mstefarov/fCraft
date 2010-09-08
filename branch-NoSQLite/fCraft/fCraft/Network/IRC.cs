/*
 * Based, in part, on SmartIrc4net code. Original license is reproduced below.
 * 
 *
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 *
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace fCraft {
    class IRC {
        const int Timeout = 10000; // socket timeout (ms)
        internal static int SendDelay; // set by ApplyConfig
        const int ReconnectDelay = 1000;

        static TcpClient client;
        static StreamReader reader;
        static StreamWriter writer;
        static Thread thread;

        public static bool connected, reconnect, registered;

        static string hostName;
        static int port;
        static string[] channelNames;
        static string botNick;

        static Regex nonPrintableChars = new Regex( "\x03\\d{1,2}(,\\d{1,2})?|[\x00-\x1F\x7E-\xFF]", RegexOptions.Compiled );

        public static void Init() {
            if( !Config.GetBool( ConfigKey.IRCBot ) ) return;
            hostName = Config.GetString( ConfigKey.IRCBotNetwork );
            port = Config.GetInt( ConfigKey.IRCBotPort );
            channelNames = Config.GetString( ConfigKey.IRCBotChannels ).Split( ',' );
            for( int i = 0; i < channelNames.Length; i++ ) {
                channelNames[i] = channelNames[i].Trim();
                if( !channelNames[i].StartsWith( "#" ) ) {
                    channelNames[i] = '#' + channelNames[i].Trim();
                }
            }
            botNick = Config.GetString( ConfigKey.IRCBotNick );

            Server.OnPlayerSentMessage += PlayerMessageHandler;
            Server.OnPlayerConnected += PlayerConnectedHandler;
            Server.OnPlayerDisconnected += PlayerDisconnectedHandler;
        }


        public static void PlayerMessageHandler( Player player, World world, ref string message, ref bool cancel ) {
            if( Config.GetBool( ConfigKey.IRCBotForwardFromServer ) ) {
                SendToAllChannels( player.name + ": " + message );
            } else if( message.StartsWith( "#" ) ) {
                SendToAllChannels( player.name + ": " + message.Substring( 1 ) );
            }
        }

        public static void PlayerConnectedHandler( Session session, ref bool cancel ) {
            if( Config.GetBool( ConfigKey.IRCBotAnnounceServerJoins ) ) {
                SendToAllChannels( "* " + session.player.name + " connected." );
            }
        }

        public static void PlayerDisconnectedHandler( Session session ) {
            if( Config.GetBool( ConfigKey.IRCBotAnnounceServerJoins ) && session.player != null ) {
                SendToAllChannels( "* " + session.player.name + " left the server." );
            }
        }


        public static bool Start() {
            try {
                // start the machinery!
                thread = new Thread( IoThread );
                thread.IsBackground = true;
                thread.Start();
                return true;
            } catch( Exception ex ) {
                Logger.Log( "IRC: Could not start the bot: " + ex, LogType.Error );
                return false;
            }
        }


        public static void Reconnect() {
            reconnect = true;
        }


        static void Connect() {
            // initialize the client
            client = new TcpClient();
            client.NoDelay = true;
            client.ReceiveTimeout = Timeout;
            client.SendTimeout = Timeout;
            client.Client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1 );

            // connect
            client.Connect( hostName, port );

            // prepare to read/write
            reader = new StreamReader( client.GetStream() );
            writer = new StreamWriter( client.GetStream() );
            connected = true;
        }


        static ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();
        static DateTime lastMessageSend;
        // runs in its own thread, started from Connect()
        static void IoThread() {
            string outputLine = "";
            lastMessageSend = DateTime.UtcNow;

            do {
                try {
                    reconnect = false;
                    Logger.Log( "Connecting to {0}:{1} as {2}", LogType.IRC,
                                hostName, port, botNick );
                    Connect();

                    // register
                    Send( IRCCommands.User( botNick, 8, Config.GetString( ConfigKey.ServerName ) ) );
                    Send( IRCCommands.Nick( botNick ) );
                    registered = false;

                    while( connected && !reconnect ) {
                        Thread.Sleep( 10 );

                        if( outputQueue.Length > 0 && DateTime.UtcNow.Subtract( lastMessageSend ).TotalMilliseconds >= SendDelay ) {
                            if( outputQueue.Dequeue( ref outputLine ) ) {
                                writer.Write( outputLine + "\r\n" );
                                lastMessageSend = DateTime.UtcNow;
                                writer.Flush();
                            }
                        }

                        if( client.Client.Available > 0 ) {
                            HandleMessage( reader.ReadLine() );
                        }
                    }

                } catch( Exception ex ) {
                    Logger.Log( "IRC: "+ex, LogType.Error );
                    reconnect = true;
                }

                if( reconnect ) Thread.Sleep( ReconnectDelay );
            } while( reconnect );
        }


        static void HandleMessage( string message ) {

            IRCMessage msg = MessageParser( message );

            switch( msg.Type ) {
                case IRCMessageType.Login:
                    foreach( string channel in channelNames ) {
                        Send( IRCCommands.Join( channel ) );
                    }
                    registered = true;
                    return;


                case IRCMessageType.Ping:
                    // ping-pong
                    Send( IRCCommands.Pong( msg.RawMessageArray[1].Substring( 1 ) ) );
                    return;


                case IRCMessageType.ChannelMessage:
                    // channel chat
                    if( msg.Nick != botNick ) {
                        string processedMessage = nonPrintableChars.Replace( msg.Message, "" ).Trim();
                        if( processedMessage.Length > 0 ) {
                            if( Config.GetBool( ConfigKey.IRCBotForwardFromIRC ) ) {
                                Server.SendToAll( Color.IRC + "(IRC) " + msg.Nick + Color.White + ": " + processedMessage );
                            } else if( msg.Message.StartsWith( "#" ) ) {
                                Server.SendToAll( Color.IRC + "(IRC) " + msg.Nick + Color.White + ": " + processedMessage.Substring( 1 ) );
                            }
                        }
                    }
                    return;


                case IRCMessageType.Join:
                    if( Config.GetBool( ConfigKey.IRCBotAnnounceIRCJoins ) ) {
                        Server.SendToAll( Color.IRC + "(IRC) " + msg.Nick + " joined " + msg.Channel );
                    }
                    return;


                case IRCMessageType.Kick:
                    Logger.Log( "IRC Bot was kicked from {0} by {1} ({2}), rejoining.", LogType.IRC,
                                msg.Channel, msg.Nick, msg.Message );
                    Send( IRCCommands.Join( msg.Channel ) );
                    return;


                case IRCMessageType.Part:
                case IRCMessageType.Quit:
                    if( Config.GetBool( ConfigKey.IRCBotAnnounceIRCJoins ) ) {
                        Server.SendToAll( Color.IRC + "(IRC) " + msg.Nick + " left " + msg.Channel );
                    }
                    return;


                case IRCMessageType.ErrorMessage:
                case IRCMessageType.Error:
                    if( !registered && msg.ReplyCode == IRCReplyCode.ErrorNicknameInUse ) {
                        botNick += "_";
                        Send( IRCCommands.Nick( botNick ) );
                    } else {
                        Logger.Log( "IRC Error (" + msg.ReplyCode + "): " + msg.RawMessage, LogType.IRC );
                    }
                    return;


                case IRCMessageType.QueryAction:
                    // TODO: PMs
                    Logger.Log( "IRC PM: " + msg.RawMessage, LogType.IRC );
                    break;


                case IRCMessageType.Kill:
                    Logger.Log( "IRC Bot was killed from {0} by {1} ({2}), reconnecting.", LogType.IRC,
                                hostName, msg.Nick, msg.Message );
                    reconnect = true;
                    connected = false;
                    return;


                case IRCMessageType.Unknown:
                    //Logger.Log( msg.RawMessage, LogType.IRC );
                    break;


                default:
                    //Logger.Log( "[" + msg.Type + "]: " + msg.RawMessage, LogType.IRC );
                    return;
            }
        }


        public static void Send( string line ) {
            outputQueue.Enqueue( line );
        }

        public static void SendToAllChannels( string line ) {
            foreach( string channel in channelNames ) {
                Send( IRCCommands.Privmsg( channel, line ) );
            }
        }


        public static void Disconnect() {
            connected = false;
            if( thread != null && thread.IsAlive ) {
                thread.Join( 1000 );
                if( thread != null && thread.IsAlive ) {
                    thread.Abort();
                }
            }
            try {
                if( reader != null ) reader.Close();
            } catch( ObjectDisposedException ) { }
            try {
                if( writer != null ) reader.Close();
            } catch( ObjectDisposedException ) { }
            try {
                if( client != null ) reader.Close();
            } catch( ObjectDisposedException ) { }
        }


        private static IRCReplyCode[] _ReplyCodes = (IRCReplyCode[])Enum.GetValues( typeof( IRCReplyCode ) );
        private static IRCMessageType _GetMessageType( string rawline ) {
            Match found = _ReplyCodeRegex.Match( rawline );
            if( found.Success ) {
                string code = found.Groups[1].Value;
                IRCReplyCode replycode = (IRCReplyCode)int.Parse( code );

                // check if this replycode is known in the RFC
                if( Array.IndexOf( _ReplyCodes, replycode ) == -1 ) {
                    return IRCMessageType.Unknown;
                }

                switch( replycode ) {
                    case IRCReplyCode.Welcome:
                    case IRCReplyCode.YourHost:
                    case IRCReplyCode.Created:
                    case IRCReplyCode.MyInfo:
                    case IRCReplyCode.Bounce:
                        return IRCMessageType.Login;
                    case IRCReplyCode.LuserClient:
                    case IRCReplyCode.LuserOp:
                    case IRCReplyCode.LuserUnknown:
                    case IRCReplyCode.LuserMe:
                    case IRCReplyCode.LuserChannels:
                        return IRCMessageType.Info;
                    case IRCReplyCode.MotdStart:
                    case IRCReplyCode.Motd:
                    case IRCReplyCode.EndOfMotd:
                        return IRCMessageType.Motd;
                    case IRCReplyCode.NamesReply:
                    case IRCReplyCode.EndOfNames:
                        return IRCMessageType.Name;
                    case IRCReplyCode.WhoReply:
                    case IRCReplyCode.EndOfWho:
                        return IRCMessageType.Who;
                    case IRCReplyCode.ListStart:
                    case IRCReplyCode.List:
                    case IRCReplyCode.ListEnd:
                        return IRCMessageType.List;
                    case IRCReplyCode.BanList:
                    case IRCReplyCode.EndOfBanList:
                        return IRCMessageType.BanList;
                    case IRCReplyCode.Topic:
                    case IRCReplyCode.TopicSetBy:
                    case IRCReplyCode.NoTopic:
                        return IRCMessageType.Topic;
                    case IRCReplyCode.WhoIsUser:
                    case IRCReplyCode.WhoIsServer:
                    case IRCReplyCode.WhoIsOperator:
                    case IRCReplyCode.WhoIsIdle:
                    case IRCReplyCode.WhoIsChannels:
                    case IRCReplyCode.EndOfWhoIs:
                        return IRCMessageType.WhoIs;
                    case IRCReplyCode.WhoWasUser:
                    case IRCReplyCode.EndOfWhoWas:
                        return IRCMessageType.WhoWas;
                    case IRCReplyCode.UserModeIs:
                        return IRCMessageType.UserMode;
                    case IRCReplyCode.ChannelModeIs:
                        return IRCMessageType.ChannelMode;
                    default:
                        if( ((int)replycode >= 400) &&
                            ((int)replycode <= 599) ) {
                            return IRCMessageType.ErrorMessage;
                        } else {
                            return IRCMessageType.Unknown;
                        }
                }
            }

            found = _PingRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Ping;
            }

            found = _ErrorRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Error;
            }

            found = _ActionRegex.Match( rawline );
            if( found.Success ) {
                switch( found.Groups[1].Value ) {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelAction;
                    default:
                        return IRCMessageType.QueryAction;
                }
            }

            found = _CtcpRequestRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.CtcpRequest;
            }

            found = _MessageRegex.Match( rawline );
            if( found.Success ) {
                switch( found.Groups[1].Value ) {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelMessage;
                    default:
                        return IRCMessageType.QueryMessage;
                }
            }

            found = _CtcpReplyRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.CtcpReply;
            }

            found = _NoticeRegex.Match( rawline );
            if( found.Success ) {
                switch( found.Groups[1].Value ) {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelNotice;
                    default:
                        return IRCMessageType.QueryNotice;
                }
            }

            found = _InviteRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Invite;
            }

            found = _JoinRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Join;
            }

            found = _TopicRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.TopicChange;
            }

            found = _NickRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.NickChange;
            }

            found = _KickRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Kick;
            }

            found = _PartRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Part;
            }

            found = _ModeRegex.Match( rawline );
            if( found.Success ) {
                if( found.Groups[1].Value == botNick ) {
                    return IRCMessageType.UserModeChange;
                } else {
                    return IRCMessageType.ChannelModeChange;
                }
            }

            found = _QuitRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Quit;
            }

            found = _KillRegex.Match( rawline );
            if( found.Success ) {
                return IRCMessageType.Kill;
            }

            return IRCMessageType.Unknown;
        }


        public static IRCMessage MessageParser( string rawline ) {
            string line;
            string[] linear;
            string messagecode;
            string from;
            string nick = null;
            string ident = null;
            string host = null;
            string channel = null;
            string message = null;
            IRCMessageType type;
            IRCReplyCode replycode;
            int exclamationpos;
            int atpos;
            int colonpos;

            if( rawline[0] == ':' ) {
                line = rawline.Substring( 1 );
            } else {
                line = rawline;
            }

            linear = line.Split( new char[] { ' ' } );

            // conform to RFC 2812
            from = linear[0];
            messagecode = linear[1];
            exclamationpos = from.IndexOf( "!" );
            atpos = from.IndexOf( "@" );
            colonpos = line.IndexOf( " :" );
            if( colonpos != -1 ) {
                // we want the exact position of ":" not beginning from the space
                colonpos += 1;
            }
            if( exclamationpos != -1 ) {
                nick = from.Substring( 0, exclamationpos );
            }
            if( (atpos != -1) &&
                (exclamationpos != -1) ) {
                ident = from.Substring( exclamationpos + 1, (atpos - exclamationpos) - 1 );
            }
            if( atpos != -1 ) {
                host = from.Substring( atpos + 1 );
            }

            try {
                replycode = (IRCReplyCode)int.Parse( messagecode );
            } catch( FormatException ) {
                replycode = IRCReplyCode.Null;
            }
            type = _GetMessageType( rawline );
            if( colonpos != -1 ) {
                message = line.Substring( colonpos + 1 );
            }

            switch( type ) {
                case IRCMessageType.Join:
                case IRCMessageType.Kick:
                case IRCMessageType.Part:
                case IRCMessageType.TopicChange:
                case IRCMessageType.ChannelModeChange:
                case IRCMessageType.ChannelMessage:
                case IRCMessageType.ChannelAction:
                case IRCMessageType.ChannelNotice:
                    channel = linear[2];
                    break;
                case IRCMessageType.Who:
                case IRCMessageType.Topic:
                case IRCMessageType.Invite:
                case IRCMessageType.BanList:
                case IRCMessageType.ChannelMode:
                    channel = linear[3];
                    break;
                case IRCMessageType.Name:
                    channel = linear[4];
                    break;
            }

            if( (channel != null) &&
                (channel[0] == ':') ) {
                channel = channel.Substring( 1 );
            }

            return new IRCMessage( from, nick, ident, host, channel, message, rawline, type, replycode );
        }


        static Regex _ReplyCodeRegex = new Regex( "^:[^ ]+? ([0-9]{3}) .+$", RegexOptions.Compiled );
        static Regex _PingRegex = new Regex( "^PING :.*", RegexOptions.Compiled );
        static Regex _ErrorRegex = new Regex( "^ERROR :.*", RegexOptions.Compiled );
        static Regex _ActionRegex = new Regex( "^:.*? PRIVMSG (.).* :" + "\x1" + "ACTION .*" + "\x1" + "$", RegexOptions.Compiled );
        static Regex _CtcpRequestRegex = new Regex( "^:.*? PRIVMSG .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled );
        static Regex _MessageRegex = new Regex( "^:.*? PRIVMSG (.).* :.*$", RegexOptions.Compiled );
        static Regex _CtcpReplyRegex = new Regex( "^:.*? NOTICE .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled );
        static Regex _NoticeRegex = new Regex( "^:.*? NOTICE (.).* :.*$", RegexOptions.Compiled );
        static Regex _InviteRegex = new Regex( "^:.*? INVITE .* .*$", RegexOptions.Compiled );
        static Regex _JoinRegex = new Regex( "^:.*? JOIN .*$", RegexOptions.Compiled );
        static Regex _TopicRegex = new Regex( "^:.*? TOPIC .* :.*$", RegexOptions.Compiled );
        static Regex _NickRegex = new Regex( "^:.*? NICK .*$", RegexOptions.Compiled );
        static Regex _KickRegex = new Regex( "^:.*? KICK .* .*$", RegexOptions.Compiled );
        static Regex _PartRegex = new Regex( "^:.*? PART .*$", RegexOptions.Compiled );
        static Regex _ModeRegex = new Regex( "^:.*? MODE (.*) .*$", RegexOptions.Compiled );
        static Regex _QuitRegex = new Regex( "^:.*? QUIT :.*$", RegexOptions.Compiled );
        static Regex _KillRegex = new Regex( "^:.*? KILL (.*) :.*$", RegexOptions.Compiled );
    }


    enum IRCReplyCode {
        Null = 000,
        Welcome = 001,
        YourHost = 002,
        Created = 003,
        MyInfo = 004,
        Bounce = 005,
        TraceLink = 200,
        TraceConnecting = 201,
        TraceHandshake = 202,
        TraceUnknown = 203,
        TraceOperator = 204,
        TraceUser = 205,
        TraceServer = 206,
        TraceService = 207,
        TraceNewType = 208,
        TraceClass = 209,
        TraceReconnect = 210,
        StatsLinkInfo = 211,
        StatsCommands = 212,
        EndOfStats = 219,
        UserModeIs = 221,
        ServiceList = 234,
        ServiceListEnd = 235,
        StatsUptime = 242,
        StatsOLine = 243,
        LuserClient = 251,
        LuserOp = 252,
        LuserUnknown = 253,
        LuserChannels = 254,
        LuserMe = 255,
        AdminMe = 256,
        AdminLocation1 = 257,
        AdminLocation2 = 258,
        AdminEmail = 259,
        TraceLog = 261,
        TraceEnd = 262,
        TryAgain = 263,
        Away = 301,
        UserHost = 302,
        IsOn = 303,
        UnAway = 305,
        NowAway = 306,
        WhoIsUser = 311,
        WhoIsServer = 312,
        WhoIsOperator = 313,
        WhoWasUser = 314,
        EndOfWho = 315,
        WhoIsIdle = 317,
        EndOfWhoIs = 318,
        WhoIsChannels = 319,
        ListStart = 321,
        List = 322,
        ListEnd = 323,
        ChannelModeIs = 324,
        UniqueOpIs = 325,
        NoTopic = 331,
        Topic = 332,
        TopicSetBy = 333,
        Inviting = 341,
        Summoning = 342,
        InviteList = 346,
        EndOfInviteList = 347,
        ExceptionList = 348,
        EndOfExceptionList = 349,
        Version = 351,
        WhoReply = 352,
        NamesReply = 353,
        Links = 364,
        EndOfLinks = 365,
        EndOfNames = 366,
        BanList = 367,
        EndOfBanList = 368,
        EndOfWhoWas = 369,
        Info = 371,
        Motd = 372,
        EndOfInfo = 374,
        MotdStart = 375,
        EndOfMotd = 376,
        YouAreOper = 381,
        Rehashing = 382,
        YouAreService = 383,
        Time = 391,
        UsersStart = 392,
        Users = 393,
        EndOfUsers = 394,
        NoUsers = 395,
        ErrorNoSuchNickname = 401,
        ErrorNoSuchServer = 402,
        ErrorNoSuchChannel = 403,
        ErrorCannotSendToChannel = 404,
        ErrorTooManyChannels = 405,
        ErrorWasNoSuchNickname = 406,
        ErrorTooManyTargets = 407,
        ErrorNoSuchService = 408,
        ErrorNoOrigin = 409,
        ErrorNoRecipient = 411,
        ErrorNoTextToSend = 412,
        ErrorNoTopLevel = 413,
        ErrorWildTopLevel = 414,
        ErrorBadMask = 415,
        ErrorUnknownCommand = 421,
        ErrorNoMotd = 422,
        ErrorNoAdminInfo = 423,
        ErrorFileError = 424,
        ErrorNoNicknameGiven = 431,
        ErrorErroneusNickname = 432,
        ErrorNicknameInUse = 433,
        ErrorNicknameCollision = 436,
        ErrorUnavailableResource = 437,
        ErrorUserNotInChannel = 441,
        ErrorNotOnChannel = 442,
        ErrorUserOnChannel = 443,
        ErrorNoLogin = 444,
        ErrorSummonDisabled = 445,
        ErrorUsersDisabled = 446,
        ErrorNotRegistered = 451,
        ErrorNeedMoreParams = 461,
        ErrorAlreadyRegistered = 462,
        ErrorNoPermissionForHost = 463,
        ErrorPasswordMismatch = 464,
        ErrorYouAreBannedCreep = 465,
        ErrorYouWillBeBanned = 466,
        ErrorKeySet = 467,
        ErrorChannelIsFull = 471,
        ErrorUnknownMode = 472,
        ErrorInviteOnlyChannel = 473,
        ErrorBannedFromChannel = 474,
        ErrorBadChannelKey = 475,
        ErrorBadChannelMask = 476,
        ErrorNoChannelModes = 477,
        ErrorBanListFull = 478,
        ErrorNoPrivileges = 481,
        ErrorChannelOpPrivilegesNeeded = 482,
        ErrorCannotKillServer = 483,
        ErrorRestricted = 484,
        ErrorUniqueOpPrivilegesNeeded = 485,
        ErrorNoOperHost = 491,
        ErrorUserModeUnknownFlag = 501,
        ErrorUsersDoNotMatch = 502
    }


    enum IRCMessageType {
        Ping,
        Info,
        Login,
        Motd,
        List,
        Join,
        Kick,
        Part,
        Invite,
        Quit,
        Kill,
        Who,
        WhoIs,
        WhoWas,
        Name,
        Topic,
        BanList,
        NickChange,
        TopicChange,
        UserMode,
        UserModeChange,
        ChannelMode,
        ChannelModeChange,
        ChannelMessage,
        ChannelAction,
        ChannelNotice,
        QueryMessage,
        QueryAction,
        QueryNotice,
        CtcpReply,
        CtcpRequest,
        Error,
        ErrorMessage,
        Unknown
    }
}