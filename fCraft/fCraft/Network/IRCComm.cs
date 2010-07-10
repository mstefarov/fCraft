// WARNING: This is a lot of text.
//#define DEBUG_IRC // This line will give you messages sent back/forth between the bot and server
//#define DEBUG_IRC_RAW // This line will show all raw server messages
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

namespace fCraft {
    public static class IRCComm {
        #region Variables

        // Bot and Server Variables
        private static Thread thread;
        public static string IRCSERVER;
        public static int PORT;
        public static string USER = "USER fCraftbot 8 * :fCraft IRC Bot";
        public static string BOTNICK;
        public static List<string> CHANNELS = new List<string>();
        public static string QUITMSG;
        public static bool FORWARD_SERVER;
        public static bool FORWARD_IRC;
        public static int NICKVALUE = 1;

        // Status variables
        private static bool online; // Signifies a *complete* registration with the network (ability to send messages)
        private static bool firstConnect;
        private static bool doShutdown; // Signifies shutdown

        // Message handling variables
        private static List<IRCMessage> outMessages = new List<IRCMessage>();
        private static IRCMessage serverMsg = new IRCMessage();

        // Message event
        public delegate void MessageAddedHandler();
        public static event IRCComm.MessageAddedHandler MessageAdded;

        internal static void MessageAddedEvent() {
            if( MessageAdded != null ) MessageAdded();
        }

        // Socket and Stream variables
        private static StreamWriter writer;
        private static TcpClient connection;
        private static NetworkStream stream;
        private static StreamReader reader;

        #endregion

        public static void Start() {
            try {
                // Load up server/bot values from config
                IRCSERVER = Config.GetString( ConfigKey.IRCBotNetwork );
                PORT = Config.GetInt( ConfigKey.IRCBotPort );
                BOTNICK = Config.GetString( ConfigKey.IRCBotNick );
                FORWARD_IRC = Config.GetBool( ConfigKey.IRCBotForwardFromIRC );
                FORWARD_SERVER = Config.GetBool( ConfigKey.IRCBotForwardFromServer );
                QUITMSG = Config.GetString( ConfigKey.IRCBotQuitMsg );

                // Parse channels from the config by comma seperation
                string[] tmpChans = Config.GetString( ConfigKey.IRCBotChannels ).Split( ',' );
                for( int i = 0; i < tmpChans.Length; ++i )
                    CHANNELS.Add( tmpChans[i] );

                // Subscribe message added event
                //MessageAdded += new MessageAddedHandler();

                // Start the thread and initialize a connection
                thread = new Thread( CommHandler );
                thread.IsBackground = true;
                thread.Start();
            } catch( Exception ex ) {
                Console.WriteLine( ex.ToString() );
            }
        }

        static void CommHandler() {
            try {
                #region Initialize connection and register
                // Initiate connection and bring the streams to life!
                connection = new TcpClient( IRCSERVER, PORT );
                stream = connection.GetStream();
                reader = new StreamReader( stream );
                writer = new StreamWriter( stream );

                // IRC Registration RFC demands you send your user credentials
                serverMsg.chatMessage = USER;
                SendRaw( ref serverMsg );
                // Then send the nickname you will use
                serverMsg.chatMessage = "NICK " + BOTNICK + "\r\n";
                SendRaw( ref serverMsg );

                // After registration is done
                firstConnect = true;
                #endregion
                while( true ) {
                    // Prevent exceptions being thrown on thread.abort()
                    if( connection.Connected != true ) {
                        online = false;
                        throw new Exception( "Connection was severed somehow." );
                    }
                    // If the server is going offline, return, else process messages
                    if( doShutdown ) return;
                    else {
                        Process();
                        Thread.Sleep( 500 );
                    }

                    #region Read stream and parse messages

                    // get ready to read input
                    String serverInput;
                    byte[] myReadBuffer = new byte[2048];
                    String myCompleteMessage = "";
                    int numberOfBytesRead = 0;

                    do // Read input while data is available
                    {
                        // Read from the stream and place the read buffer into serverInput
                        numberOfBytesRead = stream.Read( myReadBuffer, 0, myReadBuffer.Length );
                        serverInput = String.Concat( myCompleteMessage, Encoding.ASCII.GetString( myReadBuffer, 0, numberOfBytesRead ) );
#if DEBUG_IRC_RAW
Console.WriteLine("*BYTES* :" + numberOfBytesRead);
#endif
                        // Split each line in the buffer by \r\n
                        String[] splitParam = new String[1];
                        splitParam[0] = "\r\n";
                        String[] tmpServerMsgs = serverInput.Split( splitParam, System.StringSplitOptions.RemoveEmptyEntries );

                        // Loop through each line we have and parse it
                        foreach( String msg in tmpServerMsgs ) {

#if DEBUG_IRC_RAW
Console.WriteLine("*SERVERMSG* :" + msg);
#endif
                            // check each message for errors
                            if( firstConnect ) Init( msg ); // If the server has just registered, join channels etc.
                            else if( msg.StartsWith( "ERROR :Closing Link:" ) )
                                throw new Exception( "Connection was terminated by the server." );
                            else // If no errors are found, parse the message
                            {
                                IRCMessage message = new IRCMessage();
                                IRCBot.ParseMsg( ref message, msg );
                                // Parse the message for something useful (commands, etc)
                                if( message.chatMessage != null && message.chatMessage != "" ) {
                                    IRCBot.AddMessage( message );
                                    // TODO: Instead of only adding messages to a stack, throw an event too
                                }
                            }
                        }
                        Thread.Sleep( 100 );
                    } while( stream.DataAvailable );
                    #endregion
                }
            } catch( ThreadAbortException ex ) {
                Console.WriteLine( ex.ToString() );
                thread.Abort();
            } catch( Exception ex ) {
                if( doShutdown ) return;

                if( ex.Message.Contains( "(433)" ) ) {
                    BOTNICK = BOTNICK + NICKVALUE;
                    NICKVALUE++;
                }

                Logger.Log( "IRC Bot has been disconnected, trying to restart: " + ex.Message, LogType.Error );
#if DEBUG_IRC_RAW
Console.WriteLine(ex.ToString());
#endif
                Thread.Sleep( 10000 );
                firstConnect = true;

                CommHandler();
            }
        }

        // This method is a 'first run' method which runs 
        // only when the bot is A) First connecting and B) has registered with the IRC server
        private static void Init( string msg ) {
            IRCBot.ParseMsg( ref serverMsg, msg );
            if( msg.Contains( "376" ) ) // 376 is the end of MOTD command from the server, a great time to finally join channels
            {
                foreach( string channel in CHANNELS ) {
                    serverMsg.chatMessage = "JOIN " + channel + "\r\n";
                    SendRaw( ref serverMsg );
                }
                Logger.Log( "IRCBot is now connected to " + IRCSERVER + ":" + PORT + ".", LogType.IRC );
                string ircConnected = "The bot, '" + BOTNICK + "', is in channel(s):";
                foreach( string channel in CHANNELS )
                    ircConnected += " | " + channel;
                Logger.Log( ircConnected, LogType.IRC );
                //Logger.Log("** Remember ** IRC Channel names are case sensitive,\n double check your config if things aren't working proper!",LogType.IRC);

                firstConnect = false;
                online = true;
            }
            serverMsg.chatMessage = "";
            if( msg.Contains( "433" ) ) { // 433 is username already in use
                throw new Exception( "Username " + BOTNICK + " already in use (433)." );
            }
        }

        // Process messages that need to be sent to the IRC server
        public static void Process() {
            outMessages.AddRange( IRCBot.GetOutgoingMessages() );

            // Process messages on the stack
            if( outMessages.Count > 0 ) {
                int count = 0;
                foreach( IRCMessage msg in outMessages ) {
                    // Prevent spamming the entire stack at once and getting kicked for flooding
                    if( count == 4 ) Thread.Sleep( 5000 );
                    if( msg.destination == Destination.PM )
                        SendPM( msg );
                    else if( msg.destination == Destination.Channels )
                        SendMsgChannels( msg );
                    else if( msg.destination == Destination.NOTICE )
                        SendNotice( msg );
                    else if( msg.destination == Destination.RAW ) {
                        IRCMessage rawMsg = new IRCMessage();
                        rawMsg = msg;
                        SendRaw( ref rawMsg );
                    }

                    IRCBot.RemoveOutgoingMessage( msg );
                }
                outMessages.Clear();
            }
        }

        public static bool SendMsgChannels( IRCMessage message ) {
            try {
                foreach( string channel in CHANNELS ) {
#if DEBUG_IRC
Console.WriteLine("*SENT-MESSAGE* :" + message.chatMessage + " | to: " + message.to);
#endif
                    if( message.colour != null && message.colour != "" )
                        writer.WriteLine( "PRIVMSG " + channel + " :" + message.colour + message.chatMessage + "\r\n" );
                    else
                        writer.WriteLine( "PRIVMSG " + channel + " :" + message.chatMessage + "\r\n" );

                    writer.Flush();
                }
                return true;
            } catch( Exception e ) {
                Console.WriteLine( e.ToString() );
                return false;
            }
        }

        public static bool SendPM( IRCMessage message ) {
            try {
#if DEBUG_IRC
Console.WriteLine("*SENT-PM* :" + message.chatMessage + " | to: " + message.to);
#endif
                if( message.colour != null && message.colour != "" )
                    writer.WriteLine( "PRIVMSG " + message.to + " :" + message.colour + message.chatMessage + "\r\n" );
                else
                    writer.WriteLine( "PRIVMSG " + message.to + " :" + message.chatMessage + "\r\n" );
                writer.Flush();
                return true;
            } catch( Exception e ) {
                Console.WriteLine( e.ToString() );
                return false;
            }
        }

        public static bool SendNotice( IRCMessage message ) {
            try {
#if DEBUG_IRC
Console.WriteLine("*SENT-PM* :" + message.chatMessage + " | to: " + message.to);
#endif
                if( message.colour != null && message.colour != "" )
                    writer.WriteLine( "NOTICE " + message.to + " :" + message.colour + message.chatMessage + "\r\n" );
                else
                    writer.WriteLine( "NOTICE " + message.to + " :" + message.chatMessage + "\r\n" );
                writer.Flush();
                return true;
            } catch( Exception e ) {
                Console.WriteLine( e.ToString() );
                return false;
            }
        }

        internal static bool SendRaw( ref IRCMessage message ) {
            try {
#if DEBUG_IRC
Console.WriteLine("*SENT-RAW* :" + message.chatMessage );
#endif
                writer.WriteLine( message.chatMessage );
                writer.Flush();
                return true;
            } catch( Exception e ) {
                Console.WriteLine( e.ToString() );
                return false;
            }
        }

        #region Utilities
        public static void ShutDown() {
            serverMsg.chatMessage = "QUIT :" + QUITMSG;
            SendRaw( ref serverMsg );
            doShutdown = true;
            thread.Join( 1000 );
            if( thread != null && thread.IsAlive ) thread.Abort();
            if( reader != null ) reader.Close();
            if( writer != null ) writer.Close();
            if( connection != null ) connection.Close();
            Logger.Log( "IRCBot disconnected from " + IRCSERVER + ":" + PORT + ".", LogType.IRC );
        }
        public static bool CommStatus() {
            return online;
        }
        public static bool InitConnect() {
            return firstConnect;
        }
        public static string GetServer() {
            return IRCSERVER;
        }
        public static string GetBotNick() {
            return BOTNICK;
        }
        public static int GetPort() {
            return PORT;
        }
        public static List<string> GetChannels() {
            return CHANNELS;
        }
        public static string GetUser() {
            return USER;
        }
        public static bool GetSendIRC() {
            return FORWARD_IRC;
        }
        public static bool GetSendServer() {
            return FORWARD_SERVER;
        }
        #endregion
    }
}