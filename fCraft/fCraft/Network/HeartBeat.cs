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
        const string fListURL = "http://list.fragmer.net/announce.php",
                     URL = "http://www.minecraft.net/heartbeat.jsp";
        World world;


        public Heartbeat( World _world ) {
            world = _world;
        }


        public void Start(){
            thread = new Thread( HeartBeatHandler );
            thread.IsBackground = true;

            staticData = String.Format( "name={0}&max={1}&public={2}&port={3}&salt={4}&version={5}",
                                        Server.UrlEncode( world.config.GetString( "ServerName" ) ),
                                        world.config.GetInt( "MaxPlayers" ),
                                        world.config.GetBool( "IsPublic" ),
                                        world.config.GetInt("Port"),
                                        world.config.Salt,
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
                                world.config.ServerURL = responseReader.ReadLine();
                            }
                        }
                        hash = world.config.ServerURL.Substring( world.config.ServerURL.LastIndexOf( '=' ) + 1 );
                        world.FireURLChange( world.config.ServerURL );
                        hasReportedServerURL = true;
                    }
                    request.Abort();

                } catch( Exception ex ) {
                    world.log.Log( "HeartBeat: {0}", LogType.Error, ex.Message );
                }

                try {
                    request = (HttpWebRequest)WebRequest.Create( fListURL );
                    request.Method = "POST";
                    request.Timeout = 15000; // 15s timeout
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new System.Net.Cache.RequestCachePolicy( System.Net.Cache.RequestCacheLevel.NoCacheNoStore );
                    string requestString = staticData +
                                            "&users=" + world.GetPlayerCount() +
                                            "&hash=" + hash +
                                            "&motd=" + Server.UrlEncode( world.config.GetString( "MOTD" ) ) +
                                            "&server=fcraft" +
                                            "&players=" + world.GetPlayerListString();
                    byte[] formData = Encoding.ASCII.GetBytes( requestString );
                    request.ContentLength = formData.Length;

                    using( Stream requestStream = request.GetRequestStream() ) {
                        requestStream.Write( formData, 0, formData.Length );
                        requestStream.Flush();
                    }
                    request.Abort();
                } catch( Exception ex ) {
                    world.log.Log( "HeartBeat: Error reporting to fList: {0}", LogType.Error, ex.Message );
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
