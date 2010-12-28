// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;


namespace fCraft {
    public static class Heartbeat {
        const int HeartbeatDelay = 30000,
                  HeartbeatTimeout = 15000;
        static Thread thread;
        const string URL = "http://minecraft.net/heartbeat.jsp";
        const string HeartbeatDataFileName = "heartbeatdata.txt";


        internal static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            return new IPEndPoint( Server.IP, 0 );
        }


        public static void Start() {
            thread = new Thread( HeartbeatHandler );
            thread.IsBackground = true;

            thread.Start();
        }


        static void HeartbeatHandler() {
            HttpWebRequest request;

            while( true ) {
                if( Config.GetBool( ConfigKey.HeartbeatEnabled ) ) {
                    try {
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
                        if( ex is WebException || ex is IOException ) {
                            Logger.Log( "Heartbeat: Minecraft.net is probably down ({0})", LogType.Warning, ex.Message );
                        } else {
                            Logger.Log( "Heartbeat: {0}", LogType.Error, ex );
                        }
                    }

                } else {
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