// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;


namespace fCraft.HeartbeatSaver {
    static class HeartbeatSaver {
        static readonly Uri MinecraftNetUri = new Uri( "http://minecraft.net/heartbeat.jsp" ),
                            WoMDirectUri = new Uri( "http://direct.worldofminecraft.com/hb.php" );

        static readonly TimeSpan Delay = TimeSpan.FromSeconds( 20 ),
                                 Timeout = TimeSpan.FromSeconds( 10 ),
                                 ErrorDelay = TimeSpan.FromSeconds( 5 ),
                                 RefreshDataDelay = TimeSpan.FromSeconds( 60 );

        const string UrlFileName = "externalurl.txt",
                     DefaultDataFileName = "heartbeatdata.txt";

        static string heartbeatDataFileName;
        static HeartbeatData data;
        static volatile bool beatToWoM;
        static readonly HttpRequestCachePolicy CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );


        static int Main( string[] args ) {
            if( args.Length == 0 ) {
                heartbeatDataFileName = DefaultDataFileName;
            } else if( args.Length == 1 && File.Exists( args[0] ) ) {
                heartbeatDataFileName = args[0];
            } else {
                Console.WriteLine( "Usage: fHeartbeat \"path/to/datafile\"" );
                return (int)ReturnCode.UsageError;
            }

            if( !RefreshData() ) {
                return (int)ReturnCode.HeartbeatDataReadingError;
            }

            new Thread( BeatThreadMinecraftNet ) { IsBackground = true }.Start();
            new Thread( BeatThreadWoM ) { IsBackground = true }.Start();

            while( true ) {
                Thread.Sleep( RefreshDataDelay );
                RefreshData();
            }
        }


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
                    WoMDescription = rawData[7],
                    WoMFlags = rawData[8]
                };
                beatToWoM = Boolean.Parse( rawData[9] );
                data = newData;
                return true;

            } catch( Exception ex ) {
                if( ex is UnauthorizedAccessException || ex is IOException ) {
                    Console.Error.WriteLine( "{0} > Error reading {1}: {2} {3}",
                                             Timestamp(),
                                             heartbeatDataFileName, ex.GetType().Name, ex.Message );
                } else if( ex is FormatException || ex is ArgumentException ) {
                    Console.Error.WriteLine( "{0} > Cannot parse one of the data fields of {1}: {2} {3}",
                                             Timestamp(),
                                             heartbeatDataFileName, ex.GetType().Name, ex.Message );
                } else {
                    Console.Error.WriteLine( "{0} > Unexpected error: {1} {2}",
                                             Timestamp(),
                                             ex.GetType().Name, ex.Message );
                }
                return false;
            }
        }


        static void BeatThreadMinecraftNet() {
            while( true ) {
                try {
                    CreateRequest( data.CreateUri( MinecraftNetUri, false ), true );
                    Thread.Sleep( Delay );

                } catch( Exception ex ) {
                    if( ex is WebException ) {
                        Console.Error.WriteLine( "{0} > Minecraft.net probably down ({1})", Timestamp(), ex.Message );
                    } else {
                        Console.Error.WriteLine( ex );
                    }
                    Thread.Sleep( ErrorDelay );
                }
            }
        }


        static void BeatThreadWoM() {
            while( true ) {
                try {
                    if( beatToWoM ) {
                        CreateRequest( data.CreateUri( WoMDirectUri, true ), false );
                    }
                    Thread.Sleep( Delay );

                } catch( Exception ex ) {
                    if( ex is WebException ) {
                        Console.Error.WriteLine( "{0} > WoM is probably down ({1})", Timestamp(), ex.Message );
                    } else {
                        Console.Error.WriteLine( ex );
                    }
                    Thread.Sleep( ErrorDelay );
                }
            }
        }


        static void CreateRequest( Uri uri, bool getUri ) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( uri );
            request.ServicePoint.BindIPEndPointDelegate = BindIPEndPointCallback;
            request.Method = "GET";
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.ReadWriteTimeout = (int)Timeout.TotalMilliseconds;
            request.CachePolicy = CachePolicy;
            request.UserAgent = "fCraft HeartbeatSaver";

            using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() ) {
                if( getUri ) {
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        string responseText = responseReader.ReadToEnd();
                        File.WriteAllText( UrlFileName, responseText.Trim(), Encoding.ASCII );
                        Console.WriteLine( "{0} > {1} OK: {2}", Timestamp(), uri.Host, responseText );
                    }
                } else {
                    Console.WriteLine( "{0} > {1} OK", Timestamp(), uri.Host );
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


        sealed class HeartbeatData {
            const int ProtocolVersion = 7;
            public string Salt { get; set; }
            public IPAddress ServerIP { get; set; }
            public int Port { get; set; }
            public int PlayerCount { get; set; }
            public int MaxPlayers { get; set; }
            public string ServerName { get; set; }
            public bool IsPublic { get; set; }
            public string WoMFlags { get; set; }
            public string WoMDescription { get; set; }


            public Uri CreateUri( Uri heartbeatUri, bool includeWoM ) {
                UriBuilder ub = new UriBuilder( heartbeatUri );
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat( "public={0}&max={1}&users={2}&port={3}&version={4}&salt={5}&name={6}",
                                 IsPublic,
                                 MaxPlayers,
                                 PlayerCount,
                                 Port,
                                 ProtocolVersion,
                                 Uri.EscapeDataString( Salt ),
                                 Uri.EscapeDataString( ServerName ) );
                if( includeWoM ) {
                    sb.AppendFormat( "&noforward=1&desc={0}&flags={1}",
                                     Uri.EscapeDataString( WoMDescription ),
                                     Uri.EscapeDataString( WoMFlags ) );
                }
                ub.Query = sb.ToString();
                return ub.Uri;
            }
        }
    }
}