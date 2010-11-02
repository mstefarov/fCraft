// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Text;
using System.IO;


namespace fCraft {
    public static class Heartbeat {
        const int HeartbeatDelay = 30000,
                  HeartbeatTimeout = 15000;
        static Thread thread;
        const string URL = "http://minecraft.net/heartbeat.jsp";

        public static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            IPAddress IP = IPAddress.Parse( Config.GetString( ConfigKey.IP ) );
            return new IPEndPoint( IP, 0 );
        }

        public static void Start() {
            thread = new Thread( HeartbeatHandler );
            thread.IsBackground = true;

            thread.Start();
        }


        static void HeartbeatHandler() {
            HttpWebRequest request;

            while( true ) {
                try {
                    request = (HttpWebRequest)WebRequest.Create( URL );
                    request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( BindIPEndPointCallback );
                    request.Method = "POST";
                    request.Timeout = HeartbeatTimeout;
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

                    string dataString = String.Format( "name={0}&max={1}&public={2}&port={3}&salt={4}&version={5}&users={6}",
                                                       Server.UrlEncode( Config.GetString( ConfigKey.ServerName ) ),
                                                       Config.GetInt( ConfigKey.MaxPlayers ),
                                                       Config.GetBool( ConfigKey.IsPublic ),
                                                       Server.Port,
                                                       Server.Salt,
                                                       Config.ProtocolVersion,
                                                       Server.GetPlayerCount( false ) );

                    byte[] formData = Encoding.ASCII.GetBytes( dataString );
                    request.ContentLength = formData.Length;

                    using( Stream requestStream = request.GetRequestStream() ) {
                        requestStream.Write( formData, 0, formData.Length );
                        requestStream.Flush();
                    }

                    string newURL;
                    using( WebResponse response = request.GetResponse() ) {
                        using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                            newURL = responseReader.ReadLine();
                        }
                    }
                    if( newURL != Server.URL ) {
                        Server.URL = newURL;
                        Server.FireURLChangeEvent( Server.URL );
                    }
                    request.Abort();

                } catch( Exception ex ) {
                    Logger.LogWarning( "Heartbeat: {0}", WarningLogSubtype.HeartbeatWarning, ex.Message );
                }

                Thread.Sleep( HeartbeatDelay );
            }
        }


        public static void Shutdown() {
            if( thread != null && thread.IsAlive ) {
                thread.Abort();
            }
        }
    }
}