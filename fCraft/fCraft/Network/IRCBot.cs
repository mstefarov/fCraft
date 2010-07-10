// WARNING: This is a lot of text.
//#define DEBUG_IRC // Uncomment me to see debug stuff in console
///* 
// *  Copyright 2010 Jesse O'Brien <destroyer661@gmail.com>
// *
// *  Permission is hereby granted, free of charge, to any person obtaining a copy
// *  of this software and associated documentation files (the "Software"), to deal
// *  in the Software without restriction, including without limitation the rights
// *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// *  copies of the Software, and to permit persons to whom the Software is
// *  furnished to do so, subject to the following conditions:
// *
// *  The above copyright notice and this permission notice shall be included in
// *  all copies or substantial portions of the Software.
// *
// *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// *  THE SOFTWARE.
// *
// */

using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace fCraft {
    #region Structs and Enums
    // A package for authorized host/nick association
    public struct AuthPkg {
        public string host;
        public string user;
        public string nickname;
    }

    public enum IRCCommands {
        none,
        help,
        status,
        auth,
        chatmsg,
        kick = 100,
        ban,
        banip,
        banall,
        unban,
        unbanip,
        unbanall,
        slock,
        unlock,
        load = 200
    }

    public enum Destination {
        PM,
        Channels,
        Server,
        NOTICE,
        RAW
    }

    // A neat&tidy package for an irc message contents
    public struct IRCMessage {
        public string colour;
        public string host;
        public string user;
        public string to;
        public Destination destination;
        public string nickname;
        public string type;
        public string chatMessage;
        public string serverMessage;
        public IRCCommands cmd;
    }

    public static class IRCColours {

        public static string Black = '\x3' + "1",
                        NavyBlue = '\x3' + "2",
                        Green = '\x3' + "3",
                        Red = '\x3' + "4",
                        Brown = '\x3' + "5",
                        Purple = '\x3' + "6",
                        Olive = '\x3' + "7",
                        Yellow = '\x3' + "8",
                        LimeGreen = '\x3' + "9",
                        Teal = '\x3' + "10",
                        AquaLight = '\x3' + "11",
                        RoyalBlue = '\x3' + "12",
                        HotPink = '\x3' + "13",
                        DarkGray = '\x3' + "14",
                        LightGray = '\x3' + "15",
                        White = '\x3' + "16";
    }
    #endregion

    public static class IRCBot {

        #region Variables

        private static Thread thread; //self explaining

        // Server/Bot credentials
        public static string IRCSERVER;
        private static int PORT;
        private static string USER;
        private static string BOTNICK;
        private static List<string> CHANNELS;
        private static string SERVERHOST;
        private static string BOTHOST;
        private static string COMMA_PREFIX;
        private static string COLON_PREFIX;
        private static bool FORWARD_IRC;
        private static bool FORWARD_SERVER;

        // Temporary player to act as inside the server
        public static Player fBot = new Player( null, "fBot" );

        // Message stacks
        private static List<AuthPkg> authedHosts = new List<AuthPkg>();
        private static List<IRCMessage> messageStack = new List<IRCMessage>();
        private static List<IRCMessage> outMessages = new List<IRCMessage>(); //

        // Bool to identify if the IRC Bot needs to shutdown
        private static bool doShutdown;
        #endregion

        public static void Start() {
            try {
                // Start IRCCommunications
                IRCComm.Start();
                IRCSERVER = IRCComm.GetServer();
                PORT = IRCComm.GetPort();
                BOTNICK = IRCComm.GetBotNick();
                CHANNELS = IRCComm.GetChannels();
                USER = IRCComm.GetUser();
                FORWARD_IRC = IRCComm.GetSendIRC();
                FORWARD_SERVER = IRCComm.GetSendServer();
                COMMA_PREFIX = BOTNICK + ":";
                COLON_PREFIX = BOTNICK + ",";

                // Start the thread and start parsin' messages
                thread = new Thread( MessageHandler );
                thread.IsBackground = true;
                thread.Start();
            } catch( Exception e ) {
                Console.WriteLine( e.ToString() );
            }
        }

        // Check for player joins and send a message to the channels to notify
        public static void SendPlayerJoinMsg( Session session, ref bool cancel ) {
            session.player.Message( "This server's IRC Bot is" + Color.Red + " Online." );
            session.player.Message( "Use '#<message>' in chat to forward messages to the IRC Channel(s)." );
            IRCMessage newMsg = new IRCMessage() { chatMessage = session.player.GetLogName() + " has joined " + Config.GetString( ConfigKey.ServerName ) + ".", destination = Destination.Channels };
            outMessages.Add( newMsg );
            IRCComm.Process();
        }

        static void MessageHandler() {
            try {
                // After the IRCComm is online, 
                // start keeping an eye on the message stack
                while( true ) {

                    // Always ALWAYS keep the bot's nickname straight, 
                    // if the server throws 433 the nick will have a number appended
                    if( BOTNICK != IRCComm.GetBotNick() ) {
                        BOTNICK = IRCComm.GetBotNick();
                    }

                    List<IRCMessage> tempMsgStack = new List<IRCMessage>();
                    tempMsgStack.AddRange( messageStack );
                    if( tempMsgStack.Count > 0 ) {
                        foreach( IRCMessage message in tempMsgStack ) {
                            IRCMessage newMessage = new IRCMessage();
                            // If it's a private message to the bot, start handling IRCCommands
                            if( message.to == BOTNICK ) {
                                newMessage.to = message.nickname;
                                newMessage.destination = message.destination;
                                if( message.cmd == IRCCommands.status ) {
                                    #region StatusMessage
                                    // Put together all of the status variables from world and such
                                    newMessage.chatMessage = message.nickname + ", you have requested a status update.";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "Server Name: ** " + Config.GetString( ConfigKey.ServerName ) + " **";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "MOTD: ** " + Config.GetString( ConfigKey.MOTD ) + " **";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "Address: ** " + Config.ServerURL + " **";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "Players online: ** " + Server.GetPlayerCount() + " **";
                                    outMessages.Add( newMessage );

                                    // This is broken for now
                                    string[] playerList = Server.PlayerListToString().Split( ',' );
                                    //// List the players online if there are any
                                    if( playerList.Length > 0 ) {
                                        int count = 0;
                                        newMessage.chatMessage = "Players:";
                                        outMessages.Add( newMessage );
                                        foreach( string player in playerList ) {
                                            newMessage.chatMessage = " ** " + player + " ** ";
                                            outMessages.Add( newMessage );
                                            ++count;
                                        }
                                    }
                                    #endregion
                                } else if( message.cmd == IRCCommands.help ) {
                                    #region HelpMessage
                                    newMessage.chatMessage = "Hello, " + message.nickname + " , you have requested help!";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "** Be patient the help line is long **";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "***********************************************************";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "Public IRCCommands:";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "     !status - Gives the status of the server itself.";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "     !auth <password> - Authorize with the bot with the password you registered from inside the server.";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "     !help - Displays this message.";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = " Chat IRCCommands:";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "     # - initiates sending a chat message to the server from this PM.";
                                    outMessages.Add( newMessage );
                                    newMessage.chatMessage = "     <botname>: - initiates sending a chat message to the server from a channel.";
                                    outMessages.Add( newMessage );
                                    if( IsAuthed( message.nickname, message.host ) ) {
                                        newMessage.chatMessage = "***********************************************************";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "Authorized User IRCCommands:";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !kick <player> - initiates kicking a player from the server.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !ban <player> - initiates banning a player from the server by nickname.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !banip <ip address> - initiates banning a player from the server by IP.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !banall <player> - initiates banning a player from the server by Name, IP, and any players from the same IP.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !unban <player> - initiates banning a player from the server.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !unbanip <player> - initiates banning a player from the server.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !unbanip <player> - initiates banning a player from the server.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !slock - initiates Lockdown mode for the server.";
                                        outMessages.Add( newMessage );
                                        newMessage.chatMessage = "     !unlock - revokes Lockdown mode for the server.";
                                        outMessages.Add( newMessage );
                                    }
                                    #endregion

                                } else if( message.cmd == IRCCommands.auth ) {
                                    #region Authenticate
                                    string[] authLine = message.chatMessage.Split( ' ' );
                                    if( authLine.Length == 2 ) {
                                        // Need an authorization workup here
                                        // registerdnicks.contains(message.nickname)
                                        // password matches registered users password
                                        if( authLine[1] == "auth0riz3m3" )// Bot auth password
                                        {
                                            string authResponse = message.nickname + " Authenticated to host " + message.host;
                                            newMessage.chatMessage = message.nickname + ", you have authenticated with the host " + message.host + ".";
                                            outMessages.Add( newMessage );
                                            Logger.Log( message.nickname + " Authenticated to host " + message.host, LogType.IRC );
                                            AuthPkg newAuth = new AuthPkg() { host = message.host, nickname = message.nickname };
                                            authedHosts.Add( newAuth );
                                        } else {
                                            newMessage.chatMessage = "Sorry, that was the wrong password associated with the nickname - " + message.nickname;
                                            outMessages.Add( newMessage );
                                        }
                                    } else {
                                        newMessage.chatMessage = "Sorry, your auth request contained too many/few parameters. Try again or type !help for useage.";
                                        outMessages.Add( newMessage );
                                    }
                                } else if( message.cmd >= IRCCommands.kick ) {
                                    Invoke( ref newMessage, message );
                                } else if( message.chatMessage.Contains( "Hello" ) || message.chatMessage.Contains( "hello" ) ) {
                                    newMessage.chatMessage = "Hi there, " + message.nickname + "!";
                                    newMessage.chatMessage = "You can access help by typing '!help'.";
                                    outMessages.Add( newMessage );
                                } else {
                                    newMessage.chatMessage = "Sorry, unreadable Command. Try typing '!help' for help.";
                                    outMessages.Add( newMessage );
                                }
                                    #endregion
                            }
                            if( message.destination == Destination.Server && message.chatMessage != null && message.chatMessage != "" ) {
                                string stringToServer = "(IRC)" + message.nickname + ": " + message.chatMessage;
                                Logger.Log( stringToServer, LogType.IRC );
                                Server.SendToAll( stringToServer );
                            }
                            // Remove parsed messages from the message stack
                            messageStack.Remove( message );
                        }
                        // Clean the message stack that we copied
                        tempMsgStack.Clear();
                    } else Thread.Sleep( 1 ); // No messages? Sleeeep

                    if( doShutdown == true ) return;
                }
            } catch( ThreadAbortException tb ) {
                Console.WriteLine( tb.ToString() );
                thread.Abort();
            } catch( Exception e ) {
                Logger.Log( "IRC Message parser has crashed! It should recover now.", LogType.Error );
                Console.WriteLine( e.ToString() );
                messageStack.Clear();
                Thread.Sleep( 10 );
                MessageHandler();
            }
        }

        #region Parsing
        // Parse IRC Message into a nice package for use 
        public static void ParseMsg( ref IRCMessage newMsg, string input ) {

            // This code handles ping/pong to keep the irc bot alive and connected
            if( input.StartsWith( "PING :" ) ) {
                if( IRCComm.InitConnect() ) {
                    string pongresp = input.Substring( 6, input.Length - 6 );
                    newMsg.type = "RAW";
                    newMsg.chatMessage = "PONG :" + pongresp;
                    newMsg.destination = Destination.RAW;
                    outMessages.Add( newMsg );
                    return;
                } else {
                    SERVERHOST = input.Substring( 6, input.Length - 6 );
                    if( BOTHOST != "" ) {
                        newMsg.type = "RAW";
                        newMsg.chatMessage = "PONG :" + BOTHOST + " " + SERVERHOST;
                        newMsg.destination = Destination.RAW;
                        outMessages.Add( newMsg );
                        return;
                    } else
                        Console.WriteLine( "*** ERROR: BOTHOST was empty, this means it couldn't parse a host! ***" );
                }
                // Need this line to grab the bot's hostname to respond to pings
            } else if( input.Contains( "JOIN" ) ) {
                Regex exp = new Regex( @"^:([^!]+)!([^@]*)@([^ ]+) ([A-Z]+) :(#?[^ ]+)$" );
                MatchCollection matches = exp.Matches( input );
                foreach( Match match in matches ) {
                    GroupCollection tmpGroup = match.Groups;
                    BOTHOST = tmpGroup[3].ToString();
                }
            } else {
                Regex exp = new Regex( @"^:([^!]+)!([^@]*)@([^ ]+) ([A-Z]+) (#?[^ ]+) :(.*)$" );
                MatchCollection matches = exp.Matches( input );
                foreach( Match match in matches ) {
                    GroupCollection tmpGroup = match.Groups;
                    // tmpGroup contains:
                    // 0 is the raw IRC message itself, 1 Nickname, 2 Username, 3 Host, 4 Message type, 5 Channel/User the message was sent to, 6 Message content
                    newMsg.nickname = tmpGroup[1].ToString().Trim();
                    newMsg.user = tmpGroup[2].ToString();
                    newMsg.host = tmpGroup[3].ToString();
                    newMsg.type = tmpGroup[4].ToString();
                    newMsg.to = tmpGroup[5].ToString();
                    newMsg.chatMessage = tmpGroup[6].ToString().Trim();
                    if( newMsg.type == "MODE" || newMsg.type == "NOTICE" ) {
                        newMsg.chatMessage = null;
                        return;
                    }
                }

                if( CHANNELS.Contains( newMsg.to ) ) {
                    // check for commands
                    if( CheckCommands( ref newMsg ) ) {
                        newMsg.to = BOTNICK;
                        newMsg.destination = Destination.NOTICE;
                        return;
                    }
                    if( FORWARD_IRC ) {
                        newMsg.destination = Destination.Server;
                    } else {
                        if( newMsg.chatMessage != null && newMsg.chatMessage != "" ) {
                            if( newMsg.chatMessage.IndexOf( COMMA_PREFIX ) != -1 ) {
                                newMsg.chatMessage = newMsg.chatMessage.Substring( newMsg.chatMessage.IndexOf( COMMA_PREFIX ) + COMMA_PREFIX.Length ).Trim();
                                newMsg.destination = Destination.Server;
                            } else if( newMsg.chatMessage.IndexOf( COLON_PREFIX ) != -1 ) {
                                newMsg.chatMessage = newMsg.chatMessage.Substring( newMsg.chatMessage.IndexOf( COLON_PREFIX ) + COLON_PREFIX.Length ).Trim();
                                newMsg.destination = Destination.Server;
                            } else if( newMsg.chatMessage.IndexOf( BOTNICK ) != -1 ) {
                                newMsg.chatMessage = newMsg.chatMessage.Substring( newMsg.chatMessage.IndexOf( BOTNICK ) + BOTNICK.Length ).Trim();
                                newMsg.destination = Destination.Server;
                            }
                        }
                    }
                }

                if( newMsg.to == BOTNICK ) // Catch chat messages to the bot itself
                {
                    if( CheckCommands( ref newMsg ) ) return; // Find a command? Return!
                    if( newMsg.chatMessage.Contains( "#" ) ) {
                        newMsg.nickname = BOTNICK;
                        newMsg.chatMessage = newMsg.chatMessage.Substring( newMsg.chatMessage.IndexOf( "#" ) + 1 );
                        newMsg.destination = Destination.Server;
                    }
                }
            }
        }

        // TODO: Need a new way to detect command source
        private static bool CheckCommands( ref IRCMessage newMsg ) {
            string[] tmpMessage = newMsg.chatMessage.Split( ' ' );
            if( tmpMessage.Length == 1 ||
                tmpMessage.Length == 2 && newMsg.to == BOTNICK ||
                tmpMessage.Length == 2 && newMsg.to == COMMA_PREFIX ||
                tmpMessage.Length == 2 && newMsg.to == COLON_PREFIX ) {

                foreach( IRCCommands item in Enum.GetValues( typeof( IRCCommands ) ) ) {
                    if( newMsg.chatMessage.Contains( "!" + item.ToString() ) ) {
                        newMsg.cmd = item;
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        // TODO: rename this method, and rework it a bit
        private static bool Invoke( ref IRCMessage newMessage, IRCMessage message ) {
            if( IsAuthed( message.nickname, message.host ) ) {
                string[] cmdLine = message.chatMessage.Split( ' ' );
                if( cmdLine.Length == 2 ) // Should cover most bans etc
                {
                    // TODO: FIX THIS SHIT 
                    // It's bananas, it only handles players that are/have been online 
                    PlayerInfo OfflineOffender;
                    Player Offender;
                    bool pIsOnline = false;
                    if( (Offender = Server.FindPlayer( cmdLine[1] )) != null ) {
                        pIsOnline = true;
                        HandlePlayer( ref newMessage, ref message, cmdLine[1] );
                        return true;
                    } else if( pIsOnline == false ) {
                        PlayerDB.FindPlayerInfo( cmdLine[1], out OfflineOffender );
                        if( OfflineOffender != null && message.cmd != IRCCommands.kick ) {
                            HandlePlayer( ref newMessage, ref message, cmdLine[1] );
                            return true;
                        } else {
                            newMessage.chatMessage = "Sorry, no player by the name '" + cmdLine[1] + "' was found.";
                            outMessages.Add( newMessage );
                            return false;
                        }
                    }
                } else {
                    newMessage.chatMessage = "Incorrect Syntax for command '" + message.cmd.ToString() + ", try using !help.";
                }
                outMessages.Add( newMessage );

                return true;
            } else {
                newMessage.chatMessage = "Sorry, you're not authorized to do that";
                outMessages.Add( newMessage );
                return false;
            }
        }

        internal static void HandlePlayer( ref IRCMessage newMessage, ref IRCMessage message, string command ) {
            // Fix this with above 
            switch( message.cmd ) {
                case (IRCCommands.kick):
                    Commands.ParseCommand( fBot, "/kick " + command, true );
                    newMessage.chatMessage = " Kicked player: " + command + "!";
                    break;
                case (IRCCommands.ban):
                    Commands.ParseCommand( fBot, "/ban " + command, true );
                    newMessage.chatMessage = " Banned(player): " + command + "!";
                    break;
                case (IRCCommands.banip):
                    Commands.ParseCommand( fBot, "/banip " + command, true );
                    newMessage.chatMessage = "Banned(ip): " + command + "!";
                    break;
                case (IRCCommands.banall):
                    Commands.ParseCommand( fBot, "/banall " + command, true );
                    newMessage.chatMessage = " Banned(all): " + command + "!";
                    break;
                case (IRCCommands.unban):
                    Commands.ParseCommand( fBot, "/unban " + command, true );
                    newMessage.chatMessage = " Unbanned: " + command + "!";
                    break;
                case (IRCCommands.unbanip):
                    Commands.ParseCommand( fBot, "/unbanip " + command, true );
                    newMessage.chatMessage = " Unbanned(ip): " + command + "!";
                    break;
                case (IRCCommands.unbanall):
                    Commands.ParseCommand( fBot, "/unbanall " + command, true );
                    newMessage.chatMessage = " Unbanned(all): " + command + "!";
                    break;
                case (IRCCommands.slock):
                    Commands.ParseCommand( fBot, "/lock " + command, true );
                    newMessage.chatMessage = " Initiated a Lockdown on the server!";
                    break;
                case (IRCCommands.unlock):
                    Commands.ParseCommand( fBot, "/unlock " + command, true );
                    newMessage.chatMessage = " Revoked a Lockdown on the server!";
                    break;
            }
            Logger.Log( "(IRC)" + message.nickname + newMessage.chatMessage, LogType.IRC );
        }


        #region UtilityMethods
        public static void AddMessage( IRCMessage message ) {
            messageStack.Add( message );
        }

        public static void AddOutgoingMessage( IRCMessage msg ) {
            outMessages.Add( msg );
        }

        public static void RemoveOutgoingMessage( IRCMessage msg ) {
            outMessages.Remove( msg );
        }

        public static List<IRCMessage> GetOutgoingMessages() {
            return outMessages;
        }

        static bool IsAuthed( string nickname, string host ) {
            foreach( AuthPkg check in authedHosts ) {
                if( check.nickname == nickname && check.host == host )
                    return true;
            }
            return false;
        }

        public static bool IsOnline() {
            return IRCComm.CommStatus();
        }

        public static void Shutdown() {
            doShutdown = true;
            IRCComm.ShutDown();
            thread.Join( 1000 );
            if( thread != null && thread.IsAlive ) thread.Abort();
        }
        #endregion
    }
}