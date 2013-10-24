// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;

namespace fCraft.HeartbeatSaver {
    internal static class HeartbeatSaver {
        const int ProtocolVersion = 7;

        static readonly TimeSpan Delay = TimeSpan.FromSeconds( 20 ),
                                 Timeout = TimeSpan.FromSeconds( 10 ),
                                 ErrorDelay = TimeSpan.FromSeconds( 5 ),
                                 RefreshDataDelay = TimeSpan.FromSeconds( 60 );

        static readonly RequestCachePolicy CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );

        const string UrlFileName = "externalurl.txt",
                     DefaultDataFileName = "heartbeatdata.txt",
                     UserAgent = "fCraft HeartbeatSaver";

        static string heartbeatDataFileName;
        static HeartbeatData data;


        static int Main( string[] args ) {
            if( args.Length == 0 ) {
                heartbeatDataFileName = DefaultDataFileName;
            } else if( args.Length == 1 && File.Exists( args[0] ) ) {
                heartbeatDataFileName = args[0];
            } else {
                Console.WriteLine( @"Usage: HeartbeatSaver ""path/to/datafile""" );
                return (int)ReturnCode.UsageError;
            }

            if( !RefreshData() ) {
                return (int)ReturnCode.HeartbeatDataReadingError;
            }

            new Thread( BeatThreadMinecraftNet ) {IsBackground = true}.Start();

            while( true ) {
                Thread.Sleep( RefreshDataDelay );
                RefreshData();
            }
        }


        // fetches fresh data from the given file. Should not throw any exceptions. Runs in the main thread.
        static bool RefreshData() {
            try {
                string[] rawData = File.ReadAllLines( heartbeatDataFileName, Encoding.ASCII );
                HeartbeatData newData = new HeartbeatData {
                    Salt = rawData[0],
                    ServerIP = IPAddress.Parse( rawData[1] ),
                    Port = Int32.Parse( rawData[2] ),
                    PlayerCount = Int32.Parse( rawData[3] ),
                    MaxPlayers = Int32.Parse( rawData[4] ),
                    ServerName = rawData[5],
                    IsPublic = Boolean.Parse( rawData[6] ),
                    HeartbeatUri = new Uri( rawData[7] )
                };
                data = newData;
                return true;
            } catch( Exception ex ) {
                if( ex is UnauthorizedAccessException || ex is IOException ) {
                    Console.Error.WriteLine( "{0} > Error reading {1}: {2} {3}",
                                             Timestamp(),
                                             heartbeatDataFileName,
                                             ex.GetType().Name,
                                             ex.Message );
                } else if( ex is FormatException || ex is ArgumentException ) {
                    Console.Error.WriteLine( "{0} > Cannot parse one of the data fields of {1}: {2} {3}",
                                             Timestamp(),
                                             heartbeatDataFileName,
                                             ex.GetType().Name,
                                             ex.Message );
                } else {
                    Console.Error.WriteLine( "{0} > Unexpected error: {1} {2}",
                                             Timestamp(),
                                             ex.GetType().Name,
                                             ex.Message );
                }
                return false;
            }
        }


        // Sends a heartbeat to Minecraft.net, and saves response. Runs in its own background thread.
        static void BeatThreadMinecraftNet() {
            while( true ) {
                try {
                    HeartbeatData freshData = data;
                    UriBuilder ub = new UriBuilder( data.HeartbeatUri );
                    ub.Query = String.Format( "public={0}&max={1}&users={2}&port={3}&version={4}&salt={5}&name={6}",
                                              freshData.IsPublic,
                                              freshData.MaxPlayers,
                                              freshData.PlayerCount,
                                              freshData.Port,
                                              ProtocolVersion,
                                              Uri.EscapeDataString( freshData.Salt ),
                                              Uri.EscapeDataString( freshData.ServerName ) );
                    CreateRequest( ub.Uri );
                    Thread.Sleep( Delay );
                } catch( Exception ex ) {
                    if( ex is WebException ) {
                        Console.Error.WriteLine( "{0} > Minecraft.net probably down ({1})", Timestamp(), ex.Message );
                    } else {
                        Console.Error.WriteLine( "{0} > {1}", Timestamp(), ex );
                    }
                    Thread.Sleep( ErrorDelay );
                }
            }
        }

        // Creates an HTTP GET request to the given Uri. Optionally saves the response, to UrlFileName.
        // Throws all kinds of exceptions on failure
        static void CreateRequest( Uri uri ) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( uri );
            request.ServicePoint.BindIPEndPointDelegate = BindIPEndPointCallback;
            request.Method = "GET";
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.ReadWriteTimeout = (int)Timeout.TotalMilliseconds;
            request.CachePolicy = CachePolicy;
            request.UserAgent = UserAgent;

            using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() ) {
                using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                    string responseText = responseReader.ReadToEnd();
                    File.WriteAllText( UrlFileName, responseText.Trim(), Encoding.ASCII );
                    Console.WriteLine( "{0} > {1} OK: {2}", Timestamp(), uri.Host, responseText );
                }
            }
        }


        // Timestamp, used for logging
        static string Timestamp() {
            return DateTime.Now.ToLongTimeString();
        }


        // delegate used to ensure that heartbeats get sent from the correct NIC/IP
        static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            return new IPEndPoint( data.ServerIP, 0 );
        }


        // container class for all the heartbeat data
        sealed class HeartbeatData {
            public string Salt { get; set; }
            public IPAddress ServerIP { get; set; }
            public int Port { get; set; }
            public int PlayerCount { get; set; }
            public int MaxPlayers { get; set; }
            public string ServerName { get; set; }
            public bool IsPublic { get; set; }
            public Uri HeartbeatUri { get; set; }
        }
    }
}
