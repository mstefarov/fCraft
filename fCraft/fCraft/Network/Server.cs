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
    public sealed class Server {
        TcpListener listener;
        List<Session> sessions = new List<Session>();
        public static int maxUploadSpeed,   // set by Config.ApplyConfig
                          packetsPerSecond, // set by Config.ApplyConfig
                          maxSessionPacketsPerTick = 128;
        World world;

        internal Server( World _world ) {
            world = _world;
        }

        // Opens a socket for listening for incoming connections
        public bool Start() {
            bool worked = false;
            int attempts = 0;
            int attemptsMax = 20;
            int port = Config.GetInt( "Port" );
            do {
                try {
                    listener = new TcpListener( IPAddress.Any, port );
                    listener.Start();
                    worked = true;
                } catch( Exception ex ) {
                    Logger.Log( "Could not start listening on port {0}, trying next port. ({1})", LogType.Error,
                                   port, ex.Message );
                    port++;
                    attempts++;
                }
            } while( !worked && attempts < attemptsMax );
            if( !worked ) {
                Logger.Log( "Could not start listening after {0} tries. Giving up!", LogType.FatalError,
                               attemptsMax );
                return false;
            }

            Logger.Log( "Server.Run: now accepting connections at port {0}.", LogType.Debug,
                           port );
            return true;
        }


        // loops forever, waiting for incoming connections
        internal void CheckForIncomingConnections( object param ) {
            if( listener.Pending() ) {
                Logger.Log( "Server.ListenerHandler: Incoming connection", LogType.Debug );
                try {
                    sessions.Add( new Session( world, listener.AcceptTcpClient() ) );
                } catch( Exception ex ) {
                    Logger.Log( "ERROR: Could not accept incoming connection: " + ex.Message, LogType.Error );
                }
            }
            for( int i = 0; i < sessions.Count; i++ ) {
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
        public void ShutDown() {
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


        public bool VerifyName( string name, string hash ) {
            MD5 hasher = MD5.Create();
            byte[] data = hasher.ComputeHash( Encoding.ASCII.GetBytes( Config.Salt + name ) );
            for( int i = 0; i < 16; i+=2 ) {
                if( hash[i] + "" + hash[i + 1] != data[i/2].ToString( "x2" ) ) {
                    return false;
                }
            }
            return true;
        }


        public int CalculateMaxPacketsPerUpdate() {
            int packetsPerTick = (int)(packetsPerSecond / World.ticksPerSecond);
            int maxPacketsPerUpdate = (int)(Server.maxUploadSpeed / World.ticksPerSecond * 128);

            int playerCount = world.GetPlayerCount();
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
    }
}