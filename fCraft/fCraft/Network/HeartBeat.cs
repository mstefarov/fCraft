// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Threading;
using System.Text;
using System.IO;


namespace fCraft {
    public static class Heartbeat {

        static Thread thread;
        static string staticData;
        const string URL = "http://www.minecraft.net/heartbeat.jsp";


        public static void Start() {
            thread = new Thread( HeartBeatHandler );
            thread.IsBackground = true;

            staticData = String.Format( "name={0}&max={1}&public={2}&port={3}&salt={4}&version={5}",
                                        Server.UrlEncode( Config.GetString( "ServerName" ) ),
                                        Config.GetInt( "MaxPlayers" ),
                                        Config.GetBool( "IsPublic" ),
                                        Server.port,
                                        Config.Salt,
                                        Config.ProtocolVersion );

            thread.Start();
        }


        static void HeartBeatHandler() {
            HttpWebRequest request;
            bool hasReportedServerURL = false;

            while( true ) {
                try {
                    request = (HttpWebRequest)WebRequest.Create( URL );
                    request.Method = "POST";
                    request.Timeout = 15000; // 15s timeout
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new System.Net.Cache.RequestCachePolicy( System.Net.Cache.RequestCacheLevel.NoCacheNoStore );
                    byte[] formData = Encoding.ASCII.GetBytes( staticData + "&users=" + Server.GetPlayerCount() );
                    request.ContentLength = formData.Length;

                    using( Stream requestStream = request.GetRequestStream() ) {
                        requestStream.Write( formData, 0, formData.Length );
                        requestStream.Flush();
                    }

                    if( !hasReportedServerURL ) {
                        using( WebResponse response = request.GetResponse() ) {
                            using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                                Config.ServerURL = responseReader.ReadLine();
                            }
                        }
                        Server.FireURLChangeEvent( Config.ServerURL );
                        hasReportedServerURL = true;
                    }
                    request.Abort();

                } catch( Exception ex ) {
                    Logger.Log( "HeartBeat: {0}", LogType.Error, ex.Message );
                }

                Thread.Sleep( Config.HeartBeatDelay );
            }
        }


        public static void ShutDown() {
            if( thread != null && thread.IsAlive ) {
                thread.Abort();
            }
        }
    }
}
