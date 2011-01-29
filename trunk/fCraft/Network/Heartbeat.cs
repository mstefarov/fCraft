// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;


namespace fCraft {
    /// <summary>
    /// Static class responsible for sending heartbeats.
    /// </summary>
    public static class Heartbeat {
        const int HeartbeatDelay = 20000,
                  HeartbeatTimeout = 10000;
        const string URL = "http://www.minecraft.net/heartbeat.jsp";
        const string HeartbeatDataFileName = "heartbeatdata.txt";


        /// <summary>
        /// Callback for setting the local IP binding. Implements System.Net.BindIPEndPoint delegate
        /// </summary>
        internal static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            return new IPEndPoint( Server.IP, 0 );
        }


        /// <summary>
        /// Starts the heartbeat thread. The thread will be shut down automatically when the process exits.
        /// </summary>
        public static void Start() {
            task = Scheduler.AddTask( Beat ).RunManual();
        }

        static HttpWebRequest request;
        static Scheduler.Task task;

        static void Beat( Scheduler.Task task ) {
            if( Config.GetBool( ConfigKey.HeartbeatEnabled ) ) {
                request = (HttpWebRequest)WebRequest.Create( URL );
                request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( BindIPEndPointCallback );
                request.Method = "POST";
                request.Timeout = HeartbeatTimeout;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );
                string dataString = String.Format( "name={0}&max={1}&public={2}&port={3}&salt={4}&version={5}&users={6}",
                                                   Uri.EscapeDataString( Config.GetString( ConfigKey.ServerName ) ),
                                                   Config.GetInt( ConfigKey.MaxPlayers ),
                                                   Config.GetBool( ConfigKey.IsPublic ),
                                                   Server.Port,
                                                   Server.Salt,
                                                   Config.ProtocolVersion,
                                                   Server.GetPlayerCount( false ) );

                byte[] formData = Encoding.ASCII.GetBytes( dataString );
                request.ContentLength = formData.Length;

                request.BeginGetRequestStream( RequestCallback, formData );
            } else {
                // If heartbeats are disabled, the data is written to a text file (heartbeatdata.txt)
                string tempFile = HeartbeatDataFileName + ".tmp";
                File.WriteAllLines( tempFile, new string[]{
                                        Server.Salt,
                                        Server.IP.ToString(),
                                        Server.Port.ToString(),
                                        Server.GetPlayerCount(false).ToString(),
                                        Config.GetString(ConfigKey.MaxPlayers),
                                        Config.GetString(ConfigKey.ServerName),
                                        Config.GetString(ConfigKey.IsPublic)
                    } );
                if( File.Exists( HeartbeatDataFileName ) ) {
                    File.Replace( tempFile, HeartbeatDataFileName, null, true );
                } else {
                    File.Move( tempFile, HeartbeatDataFileName );
                }
                task.RunManual( TimeSpan.FromMilliseconds( HeartbeatDelay * 2 ) );
            }
        }

        static void RequestCallback( IAsyncResult result ) {
            try {
                byte[] formData = result.AsyncState as byte[];
                using( Stream requestStream = request.EndGetRequestStream( result ) ) {
                    requestStream.Write( formData, 0, formData.Length );
                }
                request.BeginGetResponse( ResponseCallback, null );
            } catch( Exception ex ) {
                if( ex is WebException || ex is IOException ) {
                    Logger.Log( "Heartbeat: Minecraft.net is probably down ({0})", LogType.Warning, ex.Message );
                } else {
                    Logger.Log( "Heartbeat: {0}", LogType.Error, ex );
                }
                task.RunManual( TimeSpan.FromMilliseconds( HeartbeatDelay ) );
            }
        }

        static void ResponseCallback( IAsyncResult result ) {
            string newURL = null;
            try {
                using( WebResponse response = request.EndGetResponse( result ) ) {
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        newURL = responseReader.ReadLine();
                    }
                }
                if( newURL.Trim().Length > 32 && newURL != Server.URL ) {
                    Server.URL = newURL;
                    Server.FireURLChangeEvent( Server.URL );
                }
            } catch( Exception ex ) {
                if( ex is WebException || ex is IOException ) {
                    Logger.Log( "Heartbeat: Minecraft.net is probably down ({0})", LogType.Warning, ex.Message );
                } else {
                    Logger.Log( "Heartbeat: {0}", LogType.Error, ex );
                }
            } finally {
                task.RunManual( TimeSpan.FromMilliseconds( HeartbeatDelay ) );
            }
        }
    }
}