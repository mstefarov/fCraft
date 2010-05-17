/*
 *  Copyright 2010 Jesse O'Brien <destroyer661@gmail.com>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */

// UnIRCComment this define to get IRC debugging data
// WARNING: This is a lot of text.
//#define DEBUG_IRC

using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;

namespace fCraft
{

    // A package for authorized host/nick association
    public struct AuthPkg
    {
        public string host;
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
            unlock 
    }

        // A neat&tidy package for an irc message contents
    public struct IRCMessage{
        public string host;
        public string to;
        public string nickname;
        public string type;
        public string chatMessage;
        public string serverMessage;
        public IRCCommands cmd;
        public bool priority; // true = high
    }

    public static class IRCBot
    {
        private static Thread thread;

        public static string SERVER;
        private static int PORT;
        private static string USER;
        private static string NICK;
        private static string[] CHANNELS;
        private static string SERVERHOST;
        private static string BOTHOST;
        private static string PREFIX;
        private static bool FORWARD_ALL;

        public static Player fBot = new Player(null, "fBot");

        private static List<AuthPkg> authedHosts = new List<AuthPkg>();
        private static List<IRCMessage> messageStack = new List<IRCMessage>();
        private static List<IRCMessage> hpStack = new List<IRCMessage>(); // High priority message stack
        private static List<IRCMessage> lpStack = new List<IRCMessage>(); // Low priority message stack


        public static StreamWriter writer;
        public static TcpClient connection;
        public static NetworkStream stream;
        public static StreamReader reader;

        private static bool doShutdown;
        

        public static void Start()
        {
            thread = new Thread(IRCHandler);
            thread.IsBackground = true;
            thread.Start();
            try
            {
                // Start IRCCommunications
                IRCComm.Start();
                SERVER = IRCComm.getServer();
                PORT = IRCComm.getPort();
                NICK = IRCComm.getNick();
                CHANNELS = IRCComm.getChannels();
                USER = IRCComm.getUser();
                FORWARD_ALL = IRCComm.getForward();
                PREFIX = NICK + ":";

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void IRCHandler()
        {
            try
            {
                // After the IRCCommunications are online, start processing messages
                while (true)
                {
                    List<IRCMessage> tempMsgStack = new List<IRCMessage>();
                    tempMsgStack.AddRange(messageStack);
                    if (tempMsgStack.Count > 0)
                    {
                        foreach (IRCMessage message in tempMsgStack)
                        {
                            if (message.chatMessage != "")
                            {
                                IRCMessage newMessage = new IRCMessage();
                                bool isPm = false;
                                // If it's a private message (the message target is the bot's nickname), start handling pm IRCCommands
                                if (message.to == NICK)
                                {
                                    newMessage.to = message.nickname;
                                    isPm = true;
                                    if (message.cmd == IRCCommands.status)
                                    {
                                        // Put together all of the status variables from world and such
                                        string serverName = Config.GetString("ServerName");
                                        string MOTD = Config.GetString("MOTD");
                                        string serverAddress = File.ReadAllText("externalurl.txt", ASCIIEncoding.ASCII);
                                        int playersOnline = Server.GetPlayerCount();
                                        newMessage.chatMessage = message.nickname + ", you have requested a status update.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "Server Name: ** " + serverName + " **";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "MOTD: ** " + MOTD + " **";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "Address: ** " + serverAddress + " **";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "Players online: ** " + playersOnline.ToString() + " **";
                                        lpStack.Add(newMessage);
                                        
                                        // This is broken for now
                                        //string[] playerList = GetPlayerListString().Split(',');
                                        //// List the players online if there are any
                                        //if (playersOnline > 0)
                                        //{
                                        //    int count = 0;
                                        //    newMessage.chatMessage = "Players:";
                                        //    lpStack.Add(newMessage);
                                        //    foreach (string player in playerList)
                                        //    {
                                        //        newMessage.chatMessage = " ** " + player + " ** ";
                                        //        lpStack.Add(newMessage);
                                        //        ++count;
                                        //    }
                                        //}
                                    }
                                    else if (message.cmd == IRCCommands.help)
                                    {
                                        newMessage.chatMessage = "Hello, " + message.nickname + " , you have requested help!";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "** Be patient the help line is long **";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "***********************************************************";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "Public IRCCommands:";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     !status - Gives the status of the server itself.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     !auth <password> - Authorize with the bot with the password you registered from inside the server.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     !help - Displays this message.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = " Chat IRCCommands:";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     # - initiates sending a chat message to the server from this PM.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     <botname>: - initiates sending a chat message to the server from a channel.";
                                        lpStack.Add(newMessage);
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            newMessage.chatMessage = "***********************************************************";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "Authorized User IRCCommands:";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !kick <player> - initiates kicking a player from the server.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !ban <player> - initiates banning a player from the server by nickname.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !banip <ip address> - initiates banning a player from the server by IP.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !banall <player> - initiates banning a player from the server by Name, IP, and any players from the same IP.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !unban <player> - initiates banning a player from the server.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !unbanip <player> - initiates banning a player from the server.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !unbanip <player> - initiates banning a player from the server.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !slock - initiates Lockdown mode for the server.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !unlock - revokes Lockdown mode for the server.";
                                            lpStack.Add(newMessage);
                                        }

                                    }
                                    else if (message.cmd == IRCCommands.auth) // Authenticate clients
                                    {
                                        string[] authLine = message.chatMessage.Split(' ');
                                        if (authLine.Length == 2)
                                        {
                                            // Need an authorization workup here
                                            // registerdnicks.contains(message.nickname)
                                            // password matches registered users password
                                            if (authLine[1] == "auth0riz3m3")// Bot auth password
                                            {
                                                string authResponse = message.nickname + " Authenticated to host " + message.host;
                                                newMessage.chatMessage = message.nickname + ", you have authenticated with the host " + message.host + ".";
                                                lpStack.Add(newMessage);
                                                Logger.Log(message.nickname + " Authenticated to host " + message.host, LogType.IRC);
                                                AuthPkg newAuth = new AuthPkg() { host = message.host, nickname = message.nickname };
                                                authedHosts.Add(newAuth);
                                            }
                                            else
                                            {
                                                newMessage.chatMessage = "Sorry, that was the wrong password associated with the nickname - " + message.nickname;
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, your auth request contained too many/few parameters. Try again or type !help for useage.";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd >= IRCCommands.kick)
                                    {
                                        Invoke(ref newMessage, message);
                                    }
                                    else if (message.chatMessage.Contains("#")) // Catch chat messages to the server itself
                                    {
                                        string stringToServer = "fBot: " + message.chatMessage.Substring(message.chatMessage.IndexOf("#") + 1);
                                        Logger.Log(stringToServer, LogType.IRC);
                                        Server.SendToAll(stringToServer);
                                    }
                                    else if (message.chatMessage.Contains("Hello") || message.chatMessage.Contains("hello"))
                                    {
                                        newMessage.chatMessage = "Hi there, " + message.nickname + "!";
                                        newMessage.chatMessage = "You can access help by typing '!help'.";
                                    }
                                    else
                                    {
                                        newMessage.chatMessage = "Sorry, unreadable IRCCommand. Try typing '!help' for help.";
                                    }
                                }
                                if (message.to == "server")
                                {
                                    string stringToServer = "(IRC)" + message.nickname + ": " + message.chatMessage;
                                    Logger.Log(stringToServer, LogType.IRC);
                                    Server.SendToAll(stringToServer);
                                }
                            }
                            messageStack.Remove(message);
                        }
                        tempMsgStack.Clear();
                    
                    }
                    if (doShutdown == true)
                    {
                        return;
                    }
                }

            }
            catch (ThreadAbortException ex)
            {
                Console.WriteLine(ex.ToString());
                thread.Abort();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Thread.Sleep(10000);
                IRCHandler();
            }
        }

        // Parse IRC Message into a nice package for use 
        public static void parseMsg( ref IRCMessage newMsg, string input)
        {
            bool isPm = false;

            // This code handles ping/pong to keep the irc bot alive and connected
            if (input.Contains("PING :"))
            { 
                if (IRCComm.initConnect())
                {
                    string pongresp = input.Substring(6, input.Length - 6);
                    newMsg.type = "RAW";
                    newMsg.chatMessage = "PONG :" + pongresp;
                    hpStack.Add(newMsg);
                    return;
                }
                else
                {
                    SERVERHOST = input.Substring(6, input.Length - 6);
                    if (BOTHOST != "")
                    {
                        newMsg.type = "RAW";
                        newMsg.chatMessage = "PONG :" + BOTHOST + " " + SERVERHOST;
                        hpStack.Add(newMsg);
                        return;
                    }
                    else
                        Console.WriteLine("*** ERROR: BOTHOST was empty, this means it couldn't parse a host! ***");
                }
            }
            // Don't ever, EVER ask me how this works. It's fucking magic okay.
            // This parses private messages and channel messages
            else if(input.Contains("PRIVMSG") || input.Contains("MODE " + NICK))
            {                
                newMsg.nickname = (input.Substring(1, input.IndexOf("!") - 1)).Trim();
                input = input.Substring(input.IndexOf(newMsg.nickname) + newMsg.nickname.Length);
                if (input.IndexOf("PRIVMSG") != -1)
                {
                    newMsg.host = input.Substring(1, input.IndexOf("PRIVMSG") - 1).Trim();
                    input = input.Substring(input.IndexOf("PRIVMSG") - 1);
                }
                else if (input.IndexOf("MODE") != -1)
                {
                    string[] hostBreak = input.Substring(1, input.IndexOf("MODE") - 1).Trim().Split('@');
                    newMsg.host = hostBreak[1];
                    BOTHOST = newMsg.host;
                    return; // Parsing needs to end here or the bot will pm itself and infinite loops ensue!
                }
                
                // Parse private message
                if (input.IndexOf(NICK + " :") != -1)
                {
                    isPm = true;
                    newMsg.type = input.Substring(0, input.IndexOf(NICK)).Trim();
                    input = input.Substring(input.IndexOf(NICK) - 1).Trim(); ;
                    newMsg.to = input.Substring(0, input.IndexOf(":")).Trim();
                    input = input.Substring(input.IndexOf(":") + 1).Trim();
                }

                // Parse other messages (ie: channel messages)
                if(!isPm)
                {
                    foreach (string channel in CHANNELS)
                    {
                        if (!FORWARD_ALL)
                        {
                            if (input.IndexOf("PRIVMSG " + channel + " :") != -1 && input.IndexOf(PREFIX) != -1)
                            {
                                newMsg.type = input.Substring(0, input.IndexOf(channel));
                                input = input.Substring(input.IndexOf(channel) - 1).Trim();
                                newMsg.to = "server";
                                input = input.Substring(input.IndexOf(PREFIX) + PREFIX.Length).Trim();
                                newMsg.chatMessage = input.Substring(0);
                                return; // return here
                            }
                        }
                        else
                        {
                            if (input.IndexOf("PRIVMSG " + channel + " :") != -1)
                            {
                                string chanCatch = channel + " :";
                                newMsg.type = input.Substring(0, input.IndexOf(channel));
                                input = input.Substring(input.IndexOf(channel) - 1).Trim();
                                newMsg.to = "server";
                                input = input.Substring(input.IndexOf(chanCatch) + chanCatch.Length).Trim();
                                newMsg.chatMessage = input.Substring(0);
                                return; // return here
                            }
                        }
                    }
                }    
                else
                {
                    newMsg.chatMessage = input.Substring(0);
                    foreach (IRCCommands item in Enum.GetValues(typeof(IRCCommands)))
                    {
                        if (newMsg.chatMessage.Contains("!"+item.ToString())){
                            newMsg.cmd = item;
                            return;
                        }
                    }
                }
#if DEBUG_IRC              
                Console.WriteLine("*RECEIVED*: Message from " + newMsg.nickname + " @ " + newMsg.host + ":: " + newMsg.chatMessage); 
#endif
            }
        }

        private static bool Invoke( ref IRCMessage newMessage, IRCMessage message)
        {
            if (isAuthed(message.nickname, message.host))
            {
                string[] cmdLine = message.chatMessage.Split(' ');
                if (cmdLine.Length == 2)
                {
                    // TODO: FIX THIS SHIT 
                    // It's bananas, it only handles players that are/have been online 
                    PlayerInfo OfflineOffender;
                    Player Offender;
                    bool pIsOnline = false;
                    if ((Offender = Server.FindPlayer(cmdLine[1])) != null)
                    {
                        pIsOnline = true;
                        HandlePlayer(ref newMessage, ref message, cmdLine[1]);
                    }
                    else if(pIsOnline == false)
                    {
                        PlayerDB.FindPlayerInfo(cmdLine[1], out OfflineOffender);
                        if(OfflineOffender != null){
                            HandlePlayer(ref newMessage, ref message, cmdLine[1]);
                        }else
                        {
                            newMessage.chatMessage = "Sorry, no player by the name '" + cmdLine[1] + "' was found.";
                            lpStack.Add(newMessage);
                            return false;
                        } 
                    }   
                }
                lpStack.Add(newMessage);
                Logger.Log("(IRC)" + message.nickname + newMessage.chatMessage, LogType.IRC);
                return true;
            }
            else
            {
                newMessage.chatMessage = "Sorry, you're not authorized to do that";
                lpStack.Add(newMessage);
                return false;
            }
        }

        internal static void HandlePlayer(ref IRCMessage newMessage, ref IRCMessage message, string command){
            // Fix this with above 
            switch (message.cmd)
            {
                case (IRCCommands.kick):
                    Commands.ParseCommand(fBot, "/kick " + command, true);
                    newMessage.chatMessage = " Kicked player: " + command + "!";
                    break;
                case (IRCCommands.ban):
                    Commands.ParseCommand(fBot, "/ban " + command, true);
                    newMessage.chatMessage = " Banned(player): " + command + "!";
                    break;
                case (IRCCommands.banip):
                    Commands.ParseCommand(fBot, "/banip " + command, true);
                    newMessage.chatMessage = "Banned(ip): " + command + "!";
                    break;
                case (IRCCommands.banall):
                    Commands.ParseCommand(fBot, "/banall " + command, true);
                    newMessage.chatMessage = " Banned(all): " + command + "!";
                    break;
                case (IRCCommands.unban):
                    Commands.ParseCommand(fBot, "/unban " + command, true);
                    newMessage.chatMessage = " Unbanned: " + command + "!";
                    break;
                case (IRCCommands.unbanip):
                    Commands.ParseCommand(fBot, "/unbanip " + command, true);
                    newMessage.chatMessage = " Unbanned(ip): " + command + "!";
                    break;
                case (IRCCommands.unbanall):
                    Commands.ParseCommand(fBot, "/unbanall " + command, true);
                    newMessage.chatMessage = " Unbanned(all): " + command + "!";
                    break;
                case (IRCCommands.slock):
                    Commands.ParseCommand(fBot, "/lock " + command, true);
                    newMessage.chatMessage = " Initiated a Lockdown on the server!";
                    break;
                case (IRCCommands.unlock):
                    Commands.ParseCommand(fBot, "/unlock " + command, true);
                    newMessage.chatMessage = " Revoked a Lockdown on the server!";
                    break;
            }
            Logger.Log("(IRC)" + message.nickname + newMessage.chatMessage, LogType.IRC);
            lpStack.Add(newMessage);
        }

        public static void AddMessage( IRCMessage message)
        {
            messageStack.Add(message);
        }

        public static void rmLP(IRCMessage msg)
        {
            lpStack.Remove(msg);
        }

        public static void rmHP(IRCMessage msg)
        {
            hpStack.Remove(msg);
        }

        static bool isAuthed(string nickname,string host)
        {
            foreach (AuthPkg check in authedHosts)
            {
                if (check.nickname == nickname && check.host == host)
                    return true;
            }
            return false;
        }

        public static bool isOnline()
        {
            return IRCComm.commStatus();
        }

        public static void ShutDown()
        {
            doShutdown = true;
            IRCComm.ShutDown();
            thread.Join(1000);
            if(thread != null && thread.IsAlive ) thread.Abort();
        }

        public static List<IRCMessage> getHpStack()
        {
            return hpStack;
        }

        public static List<IRCMessage> getLpStack()
        {
            return lpStack;
        }
    }
}
