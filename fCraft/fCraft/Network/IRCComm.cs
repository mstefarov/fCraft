
// Uncomment this define to get IRC debugging data
// WARNING: This is a lot of text.
//#define DEBUG_IRC // This line will give you messages sent back/forth between the bot and server
//#define DEBUG_IRC_RAW // This line will show all raw server messages
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace fCraft
{

    public static class IRCComm
    {
        private static Thread thread;
        public static string SERVER = Config.GetString("IRCBotNetwork");
        public static int PORT = Config.GetInt("IRCBotPort");
        public static string USER = "USER fCraftbot 8 * :fCraft IRC Bot";
        public static string NICK = Config.GetString("IRCBotNick");
        public static string[] CHANNELS = Config.GetString("IRCBotChannels").Split(',');
        public static string QUITMSG = "I've been told to go offline now!";
        public static bool FORWARD_ALL = Config.GetBool("IRCBotForwardAll");

        private static bool online; // Signifies a *complete* registration with the network (ability to send messages)
        private static bool firstConnect;
        private static bool doShutdown; // Signifies shutdown


        private static List<IRCMessage> hpStack = new List<IRCMessage>(); // High priority message stack
        private static List<IRCMessage> lpStack = new List<IRCMessage>(); // Low priority message stack

        private static IRCMessage serverMsg = new IRCMessage();

        private static StreamWriter writer;
        private static TcpClient connection;
        private static NetworkStream stream;
        private static StreamReader reader;

        public static void Start()
        {
            thread = new Thread(CommHandler);
            thread.IsBackground = true;
            thread.Start();
        }

        static void CommHandler()
        {
            try
            {
                // Initiate connection and bring the streams to life!
                connection = new TcpClient(SERVER, PORT);
                stream = connection.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                // IRC Registration RFC demands you send your user credentials
                serverMsg.chatMessage = USER;
                serverMsg.priority = true;
                SendRaw(ref serverMsg);
                // Then send the nickname you will use
                serverMsg.chatMessage = "NICK " + NICK + "\r\n";
                SendRaw(ref serverMsg);
                // After registration is done, listen for messages
                firstConnect = true;
                while (true)
                {
                    // Prevent exceptions being thrown on thread.abort()
                    if (connection.Connected != true)
                    {
                        online = false;
                        throw new Exception("Connection was severed somehow.");
                    }
                    if (doShutdown)
                    {
                        return;
                    }
                    else
                    {
                        Process();
                        Thread.Sleep(500);
                    }
                    String serverInput;
                    byte[] myReadBuffer = new byte[2048];
                    String myCompleteMessage = "";
                    int numberOfBytesRead = 0;

                    do
                    {
                        numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length); 
                        serverInput = 
                                            String.Concat(myCompleteMessage, Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));  
#if DEBUG_IRC_RAW
                        Console.WriteLine("*BYTES* :" + numberOfBytesRead);
#endif
                        // RFC's dictate that each line should be terminated by \r\n!
                        // THIS MIGHT BE CHANGED IF SOME NETWORKS USE \n INSTEAD!

                        // Split each line out of the buffer by \r\n
                        String[] splitParam = new String[1];
                        splitParam[0] = "\r\n";
                        String[] tmpServerMsgs = serverInput.Split(splitParam, System.StringSplitOptions.RemoveEmptyEntries);
                        // Loop through each line we have and parse it
                        foreach (String msg in tmpServerMsgs)
                        {
// Turn this on for all raw server messages
#if DEBUG_IRC_RAW
                            Console.WriteLine("*SERVERMSG* :" + msg);
#endif
                            if(msg.StartsWith("ERROR :Closing Link:"))
                                throw new Exception("Connection was terminated by the server.");

                            if (firstConnect)
                            {
                                IRCBot.parseMsg(ref serverMsg, msg);
                                if (msg.Contains("376"))
                                {
                                    foreach (string channel in CHANNELS)
                                    {
                                        serverMsg.chatMessage = "JOIN " + channel + "\r\n";
                                        SendRaw(ref serverMsg);
                                    }
                                    Logger.Log("IRCBot is now connected to " + SERVER + ":" + PORT + ".", LogType.IRC);
                                    string ircConnected = "The bot, '" + NICK + "', is in channel(s):";
                                    foreach(string channel in CHANNELS)
                                        ircConnected += " | " + channel;
                                    Logger.Log(ircConnected, LogType.IRC);
                                    //Logger.Log("** Remember ** IRC Channel names are case sensitive,\n double check your config if things aren't working proper!",LogType.IRC);
                                    
                                    firstConnect = false;
                                    online = true;
                                }
                                serverMsg.chatMessage = "";
                            }
                            else
                            {
                                IRCMessage message = new IRCMessage();
                                IRCBot.parseMsg(ref message, msg);
                                // Parse the message for something useful (commands, etc)
                                if (message.chatMessage != null && message.chatMessage != "")
                                    IRCBot.AddMessage(message);
                            }
                        }
                        Thread.Sleep(100);
                    } while (stream.DataAvailable);
                }
            }
            catch (ThreadAbortException ex)
            {
                Console.WriteLine(ex.ToString());
                thread.Abort();
            }
            catch (Exception e)
            {
                if (doShutdown)
                {
                    return;
                }
                Logger.Log("IRC Bot has been disconnected, it should recover now.", LogType.Error);

#if DEBUG_IRC_RAW
                Console.WriteLine(e.ToString());
#endif
                Thread.Sleep(10000);
                firstConnect = true;

                CommHandler();
            }
        }

        public static void Process()
        {
            hpStack.AddRange( IRCBot.getHpStack() );
            lpStack.AddRange( IRCBot.getLpStack() );

            // Process messages on the stack and send them out by priority
            if (hpStack.Count > 0)
            {
                int count = 0;
                foreach (IRCMessage msg in hpStack)
                {
                    if (count == 4) Thread.Sleep(5000);

                    if (msg.type == "RAW")
                    {
                        serverMsg.chatMessage = msg.chatMessage;
                        SendRaw(ref serverMsg);
                        serverMsg.chatMessage = "";
                    }
                    else
                        SendPM(msg);
                    IRCBot.rmHP(msg);
                }
                hpStack.Clear();
            }
            if (lpStack.Count > 0)
            {
                int count = 0;
                foreach (IRCMessage msg in lpStack)
                {
                    if (count == 4) Thread.Sleep(5000);
                    if (msg.destination == destination.PM)
                        SendPM(msg);
                    else
                        SendMsgChannels(msg);
                    
                    IRCBot.rmLP(msg);
                }
                lpStack.Clear();
            }
        }

        public static bool SendMsgChannels(IRCMessage message)
        {
            try
            {
                foreach (string channel in CHANNELS)
                {
#if DEBUG_IRC
                    Console.WriteLine("*SENT-MESSAGE* :" + message.chatMessage + " | to: " + message.to);
#endif
                    writer.WriteLine("PRIVMSG " + channel + " :" + message.chatMessage + "\r\n");
                    writer.Flush();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public static bool SendPM(IRCMessage message)
        {
            try
            {
#if DEBUG_IRC
                Console.WriteLine("*SENT-PM* :" + message.chatMessage + " | to: " + message.to);
#endif
                writer.WriteLine("PRIVMSG " + message.to + " :" + message.chatMessage + "\r\n");
                writer.Flush();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        internal static bool SendRaw(ref IRCMessage message)
        {
            try
            {
#if DEBUG_IRC
                Console.WriteLine("*SENT-RAW* :" + message.chatMessage );
#endif
                writer.WriteLine(message.chatMessage);
                writer.Flush();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        public static bool commStatus()
        {
            return online;
        }

        public static void ShutDown()
        {
            serverMsg.chatMessage = "QUIT :" + QUITMSG;
            serverMsg.priority = true;
            SendRaw(ref serverMsg);
            doShutdown = true;
            thread.Join(1000);
            if (thread != null && thread.IsAlive) thread.Abort();
            if (reader != null) reader.Close();
            if (writer != null) writer.Close();
            if (connection != null) connection.Close();
            Logger.Log("IRCBot disconnected from " + SERVER + ":" + PORT + ".", LogType.IRC);
        }

        public static bool initConnect()
        {
            return firstConnect;
        }
        public static string getServer()
        {
            return SERVER;
        }
        public static string getNick()
        {
            return NICK;
        }
        public static int getPort()
        {
            return PORT;
        }
        public static string[] getChannels()
        {
            return CHANNELS;
        }
        public static string getUser()
        {
            return USER;
        }

        public static bool getForward()
        {
            return FORWARD_ALL;
        }
    }
}
