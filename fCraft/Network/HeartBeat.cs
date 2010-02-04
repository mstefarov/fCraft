using System;
using System.Net;
using System.Web;
using System.Threading;
using System.Text;
using System.IO;

namespace fCraft {
    static class HeartBeat {
        static Thread thread;
        static string data;
        static bool hasReportedServerURL;

        static HttpWebRequest request;

        public static void Begin(){
            thread = new Thread( HeartBeatHandler );
            thread.IsBackground = true;

            data = "name=" + HttpUtility.UrlEncode( Config.GetString("ServerName") );
            data += "&max=" + Config.GetInt( "MaxPlayers" );
            data += "&public=" + Config.GetBool( "IsPublic" );
            data += "&port=" + Config.Port;
            data += "&salt=" + Config.Salt;
            data += "&version=" + Config.ProtocolVersion;

            thread.Start();
        }

        public static void ShutDown() {
            thread.Abort();
        }

        private static void HeartBeatHandler() {
            while( true ) {
                try {
                    request = (HttpWebRequest)WebRequest.Create( Config.HeartBeatURL );
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new System.Net.Cache.RequestCachePolicy( System.Net.Cache.RequestCacheLevel.NoCacheNoStore );
                    byte[] formData = Encoding.ASCII.GetBytes( data + "&users=" + World.GetPlayerCount() );
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
                        Logger.LogAlert( "HeartBeat: " + Config.ServerURL );
                        hasReportedServerURL = true;
                    }

                } catch( Exception ex ) {
                    Logger.LogError( "HeartBeat: " + ex.Message );
                }

                Thread.Sleep( Config.HeartBeatDelay );
            }
        }
    }
}
