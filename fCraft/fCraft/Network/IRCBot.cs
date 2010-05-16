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

// Uncomment this define to get IRC debugging data
// WARNING: This is a lot of text.
#define DEBUG_IRC

using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace fCraft
{
    // A neat&tidy package for an irc message contents
    public struct IRCMessage{
        public string host;
        public string to;
        public string nickname;
        public string type;
        public string chatMessage;
        public string cmd;
        public bool priority; // true = high
    }

    // A package for authorized host/nick association
    public struct AuthPkg
    {
        public string host;
        public string nickname;
    }

    public class IRCBot
    {
        Thread thread;
        World world;
        public IRCComm comm;
        public static string SERVER;
        private static int PORT;
        private static string USER;
        private static string NICK;
        private static string[] CHANNELS;
        private static string SERVERHOST;
        private static string BOTHOST;
        private List<string> commands = new List<string>() {"!help","!status","!auth","!kick","!ban","!banip","!banall","!unban","!unbanip","!unbanall","!lock","!unlock"};

        private static List<AuthPkg> authedHosts = new List<AuthPkg>();
        private static List<IRCMessage> messageStack = new List<IRCMessage>();
        private List<IRCMessage> hpStack = new List<IRCMessage>(); // High priority message stack
        private List<IRCMessage> lpStack = new List<IRCMessage>(); // Low priority message stack


        public static StreamWriter writer;
        public static TcpClient connection;
        public static NetworkStream stream;
        public static StreamReader reader;

        private bool doShutdown;
        
        public IRCBot(World _world)
        {
            world = _world;

        }

        public void Start()
        {
            thread = new Thread(IRCHandler);
            thread.IsBackground = true;
            thread.Start();
            try
            {
                // Start communications
                comm = new IRCComm(world, this);
                comm.Start();
                SERVER = comm.getServer();
                PORT = comm.getPort();
                NICK = comm.getNick();
                CHANNELS = comm.getChannels();
                USER = comm.getUser();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        void IRCHandler()
        {
            try
            {
                // After the communications are online, start processing messages
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
//#if DEBUG_IRC
//                                if (message.host != "none")
//                                    Console.WriteLine("\nTo: " + message.to + "\nNick: " + message.nickname + "\nHost: " + message.host + "\nMessage: " + message.chatMessage + "\nType: " + message.type + "\nCommand: " + message.cmd + "\n\n");
//#endif
                                IRCMessage newMessage = new IRCMessage();
                                bool isPm = false;
                                // If it's a private message (the message target is the bot's nickname), start handling pm commands
                                if (message.to == NICK)
                                {
                                    newMessage.to = message.nickname;
                                    isPm = true;
                                    if (message.cmd == "status")
                                    {
                                        // Put together all of the status variables from world and such
                                        string serverName = world.config.GetString("ServerName");
                                        string MOTD = world.config.GetString("MOTD");
                                        string serverAddress = File.ReadAllText("externalurl.txt", ASCIIEncoding.ASCII);
                                        int playersOnline = world.GetPlayerCount();
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
                                        if (playersOnline != 0)
                                            newMessage.chatMessage = "Players list will appear in 5 seconds.";
                                        lpStack.Add(newMessage);
                                        string[] playerList = world.GetPlayerListString().Split(',');
                                        // List the players online if there are any
                                        if (playersOnline > 0)
                                        {
                                            int count = 0;
                                            newMessage.chatMessage = "Players:";
                                            lpStack.Add(newMessage);
                                            foreach (string player in playerList)
                                            {
                                                newMessage.chatMessage = " ** " + player + " ** ";
                                                lpStack.Add(newMessage);
                                                ++count;
                                            }
                                        }
                                    }
                                    else if (message.cmd == "help")
                                    {
                                        newMessage.chatMessage = "Hello, " + message.nickname + " , you have requested help!";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "** Be patient the help line is long **";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "***********************************************************";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "Public Commands:";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     !status - Gives the status of the server itself.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     !auth <password> - Authorize with the bot with the password you registered from inside the server.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     !help - Displays this message.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = " Chat Commands:";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     # - initiates sending a chat message to the server from this PM.";
                                        lpStack.Add(newMessage);
                                        newMessage.chatMessage = "     <botname>: - initiates sending a chat message to the server from a channel.";
                                        lpStack.Add(newMessage);
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            newMessage.chatMessage = "***********************************************************";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "Authorized User Commands:";
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
                                            newMessage.chatMessage = "     !lock - initiates Lockdown mode for the server.";
                                            lpStack.Add(newMessage);
                                            newMessage.chatMessage = "     !unlock - revokes Lockdown mode for the server.";
                                            lpStack.Add(newMessage);
                                        }

                                    }
                                    else if (message.cmd == "auth") // Authenticate clients
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
                                                world.log.Log(message.nickname + " Authenticated to host " + message.host, LogType.Chat);
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
                                    else if (message.cmd == "kick")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 2)
                                            {
                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/kick " + kickLine[1], true);
                                                newMessage.chatMessage = "Kicked " + kickLine[1];
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "ban")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 2)
                                            {
                                                // TODO: check for player existing first
                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/ban " + kickLine[1], true);
                                                newMessage.chatMessage = "Banned(name) " + kickLine[1];
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "banip")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 2)
                                            {

                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/banip " + kickLine[1], true);
                                                newMessage.chatMessage = "Banned(ip) " + kickLine[1];
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "banall")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 2)
                                            {

                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/banall " + kickLine[1], true);
                                                newMessage.chatMessage = "Banned(all) " + kickLine[1];
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "unban")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 2)
                                            {

                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/unban " + kickLine[1], true);
                                                newMessage.chatMessage = "Unbanned(name) " + kickLine[1];
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "unbanip")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 2)
                                            {

                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/unbanip " + kickLine[1], true);
                                                newMessage.chatMessage = "Unbanned(ip) " + kickLine[1];
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "unbanall")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 2)
                                            {

                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/unbanall " + kickLine[1], true);
                                                newMessage.chatMessage = "Unbanned(all) " + kickLine[1];
                                                lpStack.Add(newMessage);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "lock")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 1)
                                            {

                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/lock", true);
                                                newMessage.chatMessage = "Initiated a Lockdown on the server.";
                                                lpStack.Add(newMessage);
                                                world.log.Log(message.nickname + " initated a Lockdown on the server.", LogType.Chat);
                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }
                                    else if (message.cmd == "unlock")
                                    {
                                        if (isAuthed(message.nickname, message.host))
                                        {
                                            string[] kickLine = message.chatMessage.Split(' ');
                                            if (kickLine.Length == 1)
                                            {

                                                Player fBot = new Player(world, "fBot");
                                                PlayerClass fClass = new PlayerClass();
                                                fClass = world.classes.FindClass("owner");
                                                fBot.info.playerClass = fClass;
                                                world.cmd.ParseCommand(fBot, "/unlock", true);
                                                newMessage.chatMessage = "Revoked a Lockdown on the server.";
                                                lpStack.Add(newMessage);
                                                world.log.Log(message.nickname + " revoked a Lockdown on the server.", LogType.Chat);

                                            }
                                        }
                                        else
                                        {
                                            newMessage.chatMessage = "Sorry, you're not authorized to do that";
                                            lpStack.Add(newMessage);
                                        }
                                    }

                                    // This is pretty broken atm
                                    //else if (message.cmd == "shutdown")
                                    //{
                                    //     world.SendToAll("Server has been sent a shutdown command from IRC. Shutting down.", null);
                                    //     new Thread(delegate(){world.ShutDown();}).Start();
                                    //}
                                    else if (message.chatMessage.Contains("#")) // Catch chat messages to the server itself
                                    {
                                        string stringToServer = "fBot: " + message.chatMessage.Substring(message.chatMessage.IndexOf("#") + 1);
                                        world.log.Log(stringToServer, LogType.Chat);
                                        world.SendToAll(stringToServer, null);

                                    }
                                    else if (message.chatMessage.Contains("Hello") || message.chatMessage.Contains("hello"))
                                    {
                                        newMessage.chatMessage = "Hi there, " + message.nickname + "!";
                                        newMessage.chatMessage = "You can access help by typing '!help'.";
                                    }
                                    else
                                    {
                                        newMessage.chatMessage = "Sorry, unreadable command. Try typing '!help' for help.";
                                    }
                                }
                                if (!isPm)
                                {
                                    foreach (string channel in CHANNELS)
                                    {
                                        if (message.to == channel && message.chatMessage.Contains(NICK + ":"))
                                        {
                                            string stringToServer = message.nickname + message.chatMessage;
                                            world.log.Log(stringToServer, LogType.Chat);
                                            world.SendToAll(stringToServer, null);
                                        }
                                    }
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
        public void parseMsg( ref IRCMessage newMsg, string input)
        {
            bool isPm = false;

            // This code handles ping/pong to keep the irc bot alive and connected
            if (input.Contains("PING :"))
            { 
                if (comm.initConnect())
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
                    input = input.Substring(input.IndexOf(NICK) - 1);
                    newMsg.to = input.Substring(0, input.IndexOf(":")).Trim();
                    input = input.Substring(input.IndexOf(":") + 1);
                }

                // Parse other messages (ie: channel messages)
                if(!isPm)
                {
                    foreach (string channel in CHANNELS)
                    {
                        if (input.IndexOf(channel + " :") != -1)
                        {
                            newMsg.type = input.Substring(0, input.IndexOf(channel));
                            input = input.Substring(input.IndexOf(channel) - 1);
                            newMsg.to = input.Substring(0, input.IndexOf(":")).Trim();
                            input = input.Substring(input.IndexOf(":") + 1);
                        }
                    }
                }
                // Add the rest of input to the 
                newMsg.chatMessage = input.Substring(0);

                if (isPm == true)
                {
                    foreach( string cmd in commands){
                    if (newMsg.chatMessage.Contains(cmd))
                        newMsg.cmd = cmd.Substring(1);
                    }
                }
#if DEBUG_IRC              
                Console.WriteLine("*RECEIVED*: Message from " + newMsg.nickname + " @ " + newMsg.host + ":: " + newMsg.chatMessage); 
#endif
            }
        }

        public void AddMessage( IRCMessage message)
        {
            messageStack.Add(message);
        }

        public void rmLP(IRCMessage msg)
        {
            lpStack.Remove(msg);
        }
        public void rmHP(IRCMessage msg)
        {
            hpStack.Remove(msg);
        }

        bool isAuthed(string nickname,string host)
        {
            foreach (AuthPkg check in authedHosts)
            {
                if (check.nickname == nickname && check.host == host)
                    return true;
            }
            return false;
        }

        public bool isOnline()
        {
            return comm.commStatus();
        }


        public void ShutDown()
        {
            doShutdown = true;
            comm.ShutDown();
            thread.Join(1000);
            if(thread != null && thread.IsAlive ) thread.Abort();
        }

        public List<IRCMessage> getHpStack()
        {
            return hpStack;
        }
        public List<IRCMessage> getLpStack()
        {
            return lpStack;
        }

    }
}
