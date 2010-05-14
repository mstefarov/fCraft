// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Threading;
using System.Text;
using System.IO;


namespace fCraft {
    public class Heartbeat {

        Thread thread;
        string staticData;
        bool hasReportedServerURL;
        HttpWebRequest request;
        string hash;
        const string URL = "http://www.minecraft.net/heartbeat.jsp";
        World world;


        public Heartbeat( World _world ) {
            world = _world;
        }


        public void Start(){
            thread = new Thread( HeartBeatHandler );
            thread.IsBackground = true;

            staticData = String.Format( "name={0}&max={1}&public={2}&port={3}&salt={4}&version={5}",
                                        Server.UrlEncode( Config.GetString( "ServerName" ) ),
                                        Config.GetInt( "MaxPlayers" ),
                                        Config.GetBool( "IsPublic" ),
                                        Config.GetInt("Port"),
                                        Config.Salt,
                                        Config.ProtocolVersion );

            thread.Start();
        }


        void HeartBeatHandler() {
            while( true ) {
                try {
                    request = (HttpWebRequest)WebRequest.Create( URL );
                    request.Method = "POST";
                    request.Timeout = 15000; // 15s timeout
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new System.Net.Cache.RequestCachePolicy( System.Net.Cache.RequestCacheLevel.NoCacheNoStore );
                    byte[] formData = Encoding.ASCII.GetBytes( staticData + "&users=" + world.GetPlayerCount() );
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
                        hash = Config.ServerURL.Substring( Config.ServerURL.LastIndexOf( '=' ) + 1 );
                        world.FireURLChange( Config.ServerURL );
                        hasReportedServerURL = true;
                    }
                    request.Abort();

                } catch( Exception ex ) {
                    Logger.Log( "HeartBeat: {0}", LogType.Error, ex.Message );
                }

                Thread.Sleep( Config.HeartBeatDelay );
            }
        }


        public void ShutDown() {
            if( thread != null && thread.IsAlive ) {
                thread.Abort();
            }
        }
    }
}
