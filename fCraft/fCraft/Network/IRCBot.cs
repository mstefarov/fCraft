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
//#define DEBUG_IRC

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

        public static string SERVER;
        private static int PORT;
        private static string USER;
        private static string NICK;
        private static string CHANNEL;
        private static string SERVERHOST;
        private static string BOTHOST;
        private List<string> commands = new List<string>() {"!help","!status","!auth","!kick","!ban"};

        private static List<AuthPkg> authedHosts = new List<AuthPkg>();

        public static StreamWriter writer;
        public static TcpClient connection;
        public static NetworkStream stream;
        public static StreamReader reader;

        public static bool linkStatus = false;

        string inputline;

        private bool firstConnect = true;
        private bool doShutdown;


        public IRCBot(World _world)
        {
            world = _world;
            // Load credentials from config
            try
            {
                SERVER = world.config.GetString("IRCBotNetwork");
                PORT = world.config.GetInt("IRCBotPort");
                NICK = world.config.GetString("IRCBotNick");
                CHANNEL = world.config.GetString("IRCBotChannel");
                USER = "USER fCraftbot 8 * :fCraft IRC Bot";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Start()
        {
            thread = new Thread(IRCHandler);
            thread.IsBackground = true;
            thread.Start();      
        }

        void IRCHandler()
        {
            try
            {
                // Initiate connection and bring the streams to life!
                connection = new TcpClient(SERVER, PORT);
                stream = connection.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                // IRC Registration RFC demands you send your user credentials
                SendRaw(USER);
                // Then send the nickname you will use
                SendRaw("NICK " + NICK + "\r\n");
                world.log.Log("IRCBot is now connected to " + SERVER + ":" + PORT + ".", LogType.IRC);
                linkStatus = true;
                // After registration is done, listen for messages
                while (true)
                {
                    while((inputline = reader.ReadLine()) != null)
                    {
                        // Declare a new message
                        IRCMessage message = new IRCMessage() { to="none",nickname="none",type="none",host="none",chatMessage="none",cmd="none" };
                        // Pass message into parseMsg to turn it into a nice package that can be referenced
                        parseMsg(ref message, inputline);

                        #if DEBUG_IRC
                        if(message.host != "none" )
                            Console.WriteLine("\nTo: " + message.to + "\nNick: " + message.nickname + "\nHost: " + message.host + "\nMessage: " + message.chatMessage + "\nType: " + message.type + "\nCommand: " + message.cmd + "\n\n" );
                        #endif
                        if (message.chatMessage != "")
                        {
                            // If it's a private message (the message target is the bot's nickname), start handling pm commands
                            if (message.to == NICK)
                            {
                                if (message.cmd == "status")
                                {
                                    // Put together all of the status variables from world and such
                                    string serverName = world.config.GetString("ServerName");
                                    string MOTD = world.config.GetString("MOTD");
                                    string serverAddress = File.ReadAllText("externalurl.txt", ASCIIEncoding.ASCII);
                                    int playersOnline = world.GetPlayerCount();

                                    SendMsg(message.nickname, message.nickname + ", you have requested a status update.");
                                    SendMsg(message.nickname, "Server Name: ** " + serverName + " **");
                                    SendMsg(message.nickname, "MOTD: ** " + MOTD + " **");
                                    SendMsg(message.nickname, "Address: ** " + serverAddress + " **");
                                    SendMsg(message.nickname, "Players online: ** " + playersOnline.ToString() + " **");
                                    string[] playerList = world.GetPlayerListString().Split(',');
                                    // List the players online if there are any
                                    if (playersOnline > 0)
                                    {
                                        SendMsg(message.nickname, "Players:");
                                        foreach (string player in playerList)
                                            SendMsg(message.nickname, " ** " + player + " ** ");
                                    }
                                }
                                else if (message.cmd == "help")
                                {
                                    SendMsg(message.nickname, "Hello, " + message.nickname + " , you have requested help!");
                                    SendMsg(message.nickname, "Commands are:");
                                    SendMsg(message.nickname, "!status - Gives the status of the server itself.");
                                    SendMsg(message.nickname, "!auth <password> - Will authorize you with the bot, so long as you have registered your message.nickname from inside the server.");
                                    SendMsg(message.nickname, "!help - Displays this message.");
                                    SendMsg(message.nickname, "# - initiates sending a chat message to the server from this PM.");
                                    SendMsg(message.nickname, "<botname>: - initiates sending a chat message to the server from a channel.");
                                    SendMsg(message.nickname, "!kick <player> - initiates kicking a player from the server.");
                                    SendMsg(message.nickname, "!ban <player> - initiates banning a player from the server.");

                                }
                                else if (message.cmd == "auth") // Authenticate clients
                                {
                                    string[] authLine = message.chatMessage.Split(' ');
                                    if (authLine.Length == 2)
                                    {
                                        // Need an authorization workup here
                                        // registerdnicks.contains(message.nickname)
                                        // password matches registered users password
                                        if (authLine[1] == "auth0riz3m3")
                                        {
                                            string authResponse = message.nickname + " Authenticated to host " + message.host;
                                            world.log.Log(message.nickname + " Authenticated to host " + message.host, LogType.Chat);
                                            AuthPkg newAuth = new AuthPkg() { host = message.host, nickname = message.nickname };
                                            authedHosts.Add(newAuth);
                                        }
                                        else
                                        {
                                            SendMsg(message.nickname, "Sorry, that was the wrong password associated with the nickname - " + message.nickname);
                                        }
                                    }
                                    else
                                    {
                                        SendMsg(message.nickname, "Sorry, your auth request contained too many/few parameters. Try again or type !help for useage.");
                                    }
                                }
                                else if (message.cmd == "kick")
                                {
                                    string[] kickLine = message.chatMessage.Split(' ');
                                    if (kickLine.Length == 2)
                                    {
                                        Player fBot = new Player(world, "fBot");
                                        PlayerClass fClass = new PlayerClass();
                                        fClass = world.classes.FindClass("owner");
                                        fBot.info.playerClass = fClass;
                                        world.cmd.ParseCommand(fBot, "/kick " + kickLine[1], true);
                                        //if (isAuthed(message.nickname, message.host))
                                        //{
                                            // DO ME

                                        //}   
                                    }
                                }
                                else if (message.cmd == "ban")
                                {
                                    string[] kickLine = message.chatMessage.Split(' ');
                                    if (kickLine.Length == 2)
                                    {
                                        //if (isAuthed(message.nickname, message.host))
                                        //{
                                        // DO ME
                                        Player fBot = new Player(world, "fBot");
                                        PlayerClass fClass = new PlayerClass();
                                        fClass = world.classes.FindClass("owner");
                                        fBot.info.playerClass = fClass;
                                        world.cmd.ParseCommand(fBot, "/ban " + kickLine[1], true);

                                    }
                                    //}
                                }
                                else if (message.cmd == "banip")
                                {
                                    //if (isAuthed(message.nickname, message.host))
                                    //{
                                    // DO ME
                                    string[] kickLine = message.chatMessage.Split(' ');
                                    if (kickLine.Length == 2)
                                    {

                                        Player fBot = new Player(world, "fBot");
                                        PlayerClass fClass = new PlayerClass();
                                        fClass = world.classes.FindClass("owner");
                                        fBot.info.playerClass = fClass;
                                        world.cmd.ParseCommand(fBot, "/banip " + kickLine[1], true);


   
                                    }
                                    //}
                                }
                                else if (message.cmd == "banall")
                                {
                                    //if (isAuthed(message.nickname, message.host))
                                    //{
                                    // DO ME
                                    string[] kickLine = message.chatMessage.Split(' ');
                                    if (kickLine.Length == 2)
                                    {

                                        Player fBot = new Player(world, "fBot");
                                        PlayerClass fClass = new PlayerClass();
                                        fClass = world.classes.FindClass("owner");
                                        fBot.info.playerClass = fClass;
                                        world.cmd.ParseCommand(fBot, "/banall " + kickLine[1], true);



                                    }
                                    //}
                                }
                                // This is pretty broken atm
                                //else if (message.cmd == "shutdown")
                                //{
                                //     world.SendToAll("Server has been sent a shutdown command from IRC. Shutting down.", null);
                                //     new Thread(delegate(){world.ShutDown();}).Start();
                                //}
                                else if (message.chatMessage.Contains("#")) // Catch chat messages to the server itself
                                {
                                    string stringToServer = "fBot: " + inputline.Substring(inputline.IndexOf("#") + 1);
                                    world.log.Log(stringToServer, LogType.Chat);
                                    world.SendToAll(stringToServer, null);

                                }
                                else if (message.chatMessage.Contains("Hello") || inputline.Contains("hello"))
                                {
                                    SendMsg(message.nickname, "Hi there, " + message.nickname + "!");
                                    SendMsg(message.nickname, "You can access help by typing '!help'.");
                                }
                                else
                                {
                                    SendMsg(message.nickname, "Sorry, unreadable command. Try typing '!help' for help.");
                                }
                            }
                            else if (message.to == CHANNEL && message.chatMessage.Contains(NICK + ":"))
                            {
                                string stringToServer = "(IRC)" + message.nickname + ": " + inputline.Substring(inputline.IndexOf(NICK) + NICK.Length + 1).Trim();
                                world.log.Log(stringToServer, LogType.Chat);
                                world.SendToAll(stringToServer, null);

#if DEBUG_IRC
                            Console.WriteLine(stringToServer);
#endif
                            }

                        }
#if DEBUG_IRC
                            Console.WriteLine(Recieve());
#endif
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
                firstConnect = true;
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
                if (firstConnect == true)
                {
                    string pongresp = input.Substring(6, input.Length - 6);
                    SendRaw("PONG :" + pongresp);
                    Thread.Sleep(500);
                    SendRaw("JOIN " + CHANNEL + "\r\n");
                    firstConnect = false;
                }
                else
                {
                    SERVERHOST = input.Substring(6, input.Length - 6);
                    if (BOTHOST != "")
                    {
                        #if DEBUG_IRC
                        Console.WriteLine("PONG :" + BOTHOST + SERVERHOST);
                        #endif
                        SendRaw("PONG :" + BOTHOST + SERVERHOST);
                    }
                    else
                        Console.WriteLine("*** ERROR: BOTHOST was empty, this means it couldn't parse a host! ***");

                }
            }
            else if(input.Contains("PRIVMSG " + NICK) || input.Contains("PRIVMSG " + CHANNEL) || input.Contains("MODE " + NICK))
            {
#if DEBUG_IRC
                Console.WriteLine(input);
#endif
                
                // Don't ever, EVER ask me how this works. It's fucking magic okay.
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
                    input = ""; // Input needs to be null or useless here or the bot will flood itself and get kicked
                }
                if (input.IndexOf(NICK + " :") != -1)
                {
                    isPm = true;
                    newMsg.type = input.Substring(0, input.IndexOf(NICK)).Trim();
                    input = input.Substring(input.IndexOf(NICK) - 1);
                    newMsg.to = input.Substring(0, input.IndexOf(":")).Trim();
                    input = input.Substring(input.IndexOf(":") + 1);
                }
                else if(input.IndexOf(CHANNEL + " :") != -1)
                {
                    newMsg.type = input.Substring(0, input.IndexOf(CHANNEL));
                    input = input.Substring(input.IndexOf(CHANNEL) - 1);
                    newMsg.to = input.Substring(0, input.IndexOf(":")).Trim();
                    input = input.Substring(input.IndexOf(":") + 1);
                }
                newMsg.chatMessage = input.Substring(0);
                if (isPm == true)
                {
                    foreach( string cmd in commands){
                    if (newMsg.chatMessage.Contains(cmd))
                        newMsg.cmd = cmd.Substring(1);
                    }
                }

            }
        }

        bool SendRaw(string text)
        {
            try
            {
                writer.WriteLine(text);
                writer.Flush();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public bool SendMsgChannel(string text)
        {
            try
            {
                writer.WriteLine("PRIVMSG " + CHANNEL + " :" + text);
                writer.Flush();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public bool SendMsg(string who, string text)
        {
            try
            {
                writer.WriteLine("PRIVMSG " + who + " :" + text);
                writer.Flush();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

        }

        bool isAuthed(string nickname,string host)
        {
           // for (int i = 0; i < authedHosts.Count; ++i)
           // {
           //     if(nickname)
           // }
            return true;
        }

        public bool isOnline()
        {
            return linkStatus;
        }
        string Recieve()
        {
            return inputline;
        }

        public void ShutDown()
        {
            SendRaw("QUIT :I've been told to go offline now!");
            doShutdown = true;
            thread.Join(1000);
            if(thread != null && thread.IsAlive ) thread.Abort();
            if (reader != null) reader.Close();
            if (writer != null) writer.Close();
            if( connection!=null) connection.Close();
            world.log.Log("IRCBot disconnected from " + SERVER + ":" + PORT + ".", LogType.IRC);
        }
 
    }
}
