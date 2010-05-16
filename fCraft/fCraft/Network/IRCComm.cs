
// Uncomment this define to get IRC debugging data
// WARNING: This is a lot of text.
#define DEBUG_IRC // This line will give you messages sent back/forth between the bot and server
//#define DEBU_IRC_RAW // This line will show all raw server messages
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;


namespace fCraft
{

    public class IRCComm
    {
        Thread thread;
        World world;
        IRCBot bot;
        public static string SERVER;
        public static int PORT;
        public static string USER;
        public static string NICK;
        public static string[] CHANNELS;
        public static string QUITMSG = "I've been told to go offline now!";
        private static bool online;
        private string serverInput;
        private bool firstConnect;
        private List<IRCMessage> hpStack = new List<IRCMessage>(); // High priority message stack
        private List<IRCMessage> lpStack = new List<IRCMessage>(); // Low priority message stack

        private IRCMessage serverMsg = new IRCMessage();

        private static StreamWriter writer;
        private static TcpClient connection;
        private static NetworkStream stream;
        private static StreamReader reader;

        private bool doShutdown;

        public IRCComm(World _world, IRCBot _bot)
        {
            world = _world;
            bot = _bot;
            try
            {
                SERVER = world.config.GetString("IRCBotNetwork");
                PORT = world.config.GetInt("IRCBotPort");
                NICK = world.config.GetString("IRCBotNick");
                CHANNELS = world.config.GetString("IRCBotChannels").Split(',');
                USER = "USER fCraftbot 8 * :fCraft IRC Bot";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Start()
        {
            thread = new Thread(CommHandler);
            thread.IsBackground = true;
            thread.Start();
        }

        void CommHandler()
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
                world.log.Log("IRCBot is now connected to " + SERVER + ":" + PORT + ".", LogType.IRC);
                // After registration is done, listen for messages
                firstConnect = true;



                // New idea is to seperate the I/O into two threads.
                // Useing AsyncCallback() in the BeginWrite will give the ability to process messages.
                // Also using 


                while (true)
                {
                    // Prevent exceptions being thrown on thread.abort()
                    if (connection.Connected != true)
                        throw new Exception("Connection was terminated.");
                    if (doShutdown)
                    {
                        return;
                    }
                    else
                    {
                        Process();
                        Thread.Sleep(500);
                    }
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
                            if (firstConnect == true)
                            {
                                bot.parseMsg(ref serverMsg, msg);
                                if (msg.Contains("376"))
                                {
                                    foreach (string channel in CHANNELS)
                                    {
                                        serverMsg.chatMessage = "JOIN " + channel + "\r\n";
                                        SendRaw(ref serverMsg);
                                    }
                                    firstConnect = false;
                                }
                                serverMsg.chatMessage = "";
                            }
                            else
                            {
                                IRCMessage message = new IRCMessage();
                                bot.parseMsg(ref message, msg);
                                // Parse the message for something useful (commands, etc)
                                if (message.chatMessage != null)
                                    bot.AddMessage(message);
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
                Console.WriteLine(e.ToString());
                Thread.Sleep(10000);
                //firstConnect = true;
                CommHandler();
            }
        }

        public void Process()
        {
            hpStack.AddRange( bot.getHpStack() );
            lpStack.AddRange( bot.getLpStack() );

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
                    bot.rmHP(msg);
                }
                hpStack.Clear();
            }
            if (lpStack.Count > 0)
            {
                int count = 0;
                foreach (IRCMessage msg in lpStack)
                {
                    if (count == 4) Thread.Sleep(5000);
                    SendPM(msg);
                    bot.rmLP(msg);
                }
                lpStack.Clear();
            }
        }

        public bool SendMsgChannels(ref IRCMessage message)
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

        public bool SendPM(IRCMessage message)
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

        bool SendRaw(ref IRCMessage message)
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

        public bool commStatus()
        {
            return online;
        }

        public void ShutDown()
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
            world.log.Log("IRCBot disconnected from " + SERVER + ":" + PORT + ".", LogType.IRC);
        }

        public bool initConnect()
        {
            return firstConnect;
        }
        public string getServer()
        {
            return SERVER;
        }
        public string getNick()
        {
            return NICK;
        }
        public int getPort()
        {
            return PORT;
        }
        public string[] getChannels()
        {
            return CHANNELS;
        }
        public string getUser()
        {
            return USER;
        }
    }
}
