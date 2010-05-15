// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace fCraft {
    public static class Server {

        // events
        public static event SimpleEventHandler OnStart;
        public static event ConnectionEventHandler OnPlayerConnect;
        public static event ConnectionEventHandler OnPlayerDisconnect;
        public static event MessageEventHandler OnClassChange;
        public static event MessageEventHandler OnURLChange;
        public static event MessageEventHandler OnShutdown;
        public static event LogEventHandler OnLog;

        static Dictionary<int, Player> players = new Dictionary<int, Player>( 255 );
        static Player[] playerList;

        static TcpListener listener;
        static List<Session> sessions = new List<Session>();
        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          maxSessionPacketsPerTick = 128;
        public static Dictionary<string, World> worlds = new Dictionary<string, World>();
        public static World defaultWorld;

        public static IRCBot ircbot;
        //static bool IRCBotOnline;

        const int maxPortAttempts = 20;


        public static bool Init() {
            Color.Init();
            Map.Init();

            Logger.Init( "fCraft.log" );
            if( !Config.Load() ) return false;
            Config.ApplyConfig();
            Config.Save();

            if( Config.GetBool( "IRCBot" ) == true ) {
                //ircbot = new IRCBot();
                //IRCBotOnline = true;
            }

            // allocate player list
            Tasks.Init();

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            return true;
        }


        // Opens a socket for listening for incoming connections
        public static bool Start() {
            bool portFound = false;
            int attempts = 0;
            int port = Config.GetInt( "Port" );
            do {
                try {
                    listener = new TcpListener( IPAddress.Any, port );
                    listener.Start();
                    portFound = true;
                } catch( Exception ex ) {
                    Logger.Log( "Could not start listening on port {0}, trying next port. ({1})", LogType.Error,
                                   port, ex.Message );
                    port++;
                    attempts++;
                }
            } while( !portFound && attempts < maxPortAttempts );

            if( !portFound ) {
                Logger.Log( "Could not start listening after {0} tries. Giving up!", LogType.FatalError,
                               maxPortAttempts );
                return false;
            }

            Logger.Log( "Server.Run: now accepting connections at port {0}.", LogType.Debug,
                           port );
            return true;
        }


        // checks for incoming connections and disposes old sessions
        internal static void CheckForIncomingConnections( object param ) {
            if( listener.Pending() ) {
                Logger.Log( "Server.ListenerHandler: Incoming connection", LogType.Debug );
                try {
                    sessions.Add( new Session( defaultWorld, listener.AcceptTcpClient() ) );
                } catch( Exception ex ) {
                    Logger.Log( "ERROR: Could not accept incoming connection: " + ex.Message, LogType.Error );
                }
            }
            for( int i = 0; i < sessions.Count; i++ ) {
                OnPlayerDisconnect( sessions[i] );
                if( sessions[i].canDispose ) {
                    sessions[i].Disconnect();
                    sessions.RemoveAt( i );
                    i--;
                    Logger.Log( "Session disposed. Active sessions left: {0}.", LogType.Debug, sessions.Count );
                    GC.Collect();
                }
            }
        }


        // shuts down the server and aborts threads
        // NOTE: heartbeat should stop automatically
        public static void ShutDown() {
            if( listener != null ) {
                listener.Stop();
                listener = null;
            }
        }


        public static char[] reservedChars = { ' ', '!', '*', '\'', '(', ')', ';', ':', '@', '&',
                                                 '=', '+', '$', ',', '/', '?', '%', '#', '[', ']' };
        public static string UrlEncode( string input ) {
            StringBuilder output = new StringBuilder();
            for( int i = 0; i < input.Length; i++ ) {
                if( ( input[i] >= '0' && input[i] <= '9' ) ||
                    ( input[i] >= 'a' && input[i] <= 'z' ) ||
                    ( input[i] >= 'A' && input[i] <= 'Z' ) ||
                    input[i] == '-' || input[i] == '_' || input[i] == '.' || input[i] == '~' ) {
                    output.Append(input[i]);
                } else if( Array.IndexOf<char>( reservedChars, input[i] ) != -1 ) {
                    output.Append('%').Append(((int)input[i]).ToString( "X" ));
                }
            }
            return output.ToString();
        }


        public static bool VerifyName( string name, string hash ) {
            MD5 hasher = MD5.Create();
            byte[] data = hasher.ComputeHash( Encoding.ASCII.GetBytes( Config.Salt + name ) );
            for( int i = 0; i < 16; i+=2 ) {
                if( hash[i] + "" + hash[i + 1] != data[i/2].ToString( "x2" ) ) {
                    return false;
                }
            }
            return true;
        }


        public static int CalculateMaxPacketsPerUpdate( World world ) {
            int packetsPerTick = (int)(packetsPerSecond / World.ticksPerSecond);
            int maxPacketsPerUpdate = (int)(Server.maxUploadSpeed / World.ticksPerSecond * 128);

            int playerCount = Server.GetPlayerCount();
            if( playerCount > 0 ) {
                maxPacketsPerUpdate /= playerCount;
                if( maxPacketsPerUpdate > packetsPerTick ) {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            } else {
                maxPacketsPerUpdate = Int32.MaxValue;
            }

            return maxPacketsPerUpdate;
        }

        public static int htons( int value ) {
            return IPAddress.HostToNetworkOrder( value );
        }

        public static short htons( short value ) {
            return IPAddress.HostToNetworkOrder( value );
        }


        // Return player count
        public static int GetPlayerCount() {
            return sessions.Count;
        }



        internal static void FireURLChangeEvent( string URL ) {
            if( OnURLChange != null ) OnURLChange( URL );
        }
        internal static void FireLogEvent( string message, LogType type ) {
            if( OnLog != null ) OnLog( message, type );
        }
        internal static void FirePlayerConnectEvent( Session session ) {
            OnPlayerConnect( session );
        }

    }
}