using System;
using System.Net;
using System.Web;
using System.Threading;
using System.Text;
using System.IO;

namespace fCraft {
    sealed class HeartBeat {
        private Thread thread;
        private string data;

        public HeartBeat() {
            thread = new Thread( HeartBeatHandler );
            thread.IsBackground = true;

            data = "name=" + HttpUtility.UrlEncode( Config.ServerName );
            data += "&max=" + Config.MaxPlayers;
            data += "&public=" + Config.IsPublic;
            data += "&port=" + Config.Port;
            data += "&salt=" + Config.Salt;
            data += "&version=" + Config.ProtocolVersion;

            thread.Start();
        }


        private void HeartBeatHandler() {
            HttpWebRequest request;
            Stream requestStream = null;
            WebResponse response = null;
            StreamReader responseReader = null;

            while( true ) {
                try {
                    request = (HttpWebRequest)WebRequest.Create( Config.HeartBeatURL );
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new System.Net.Cache.RequestCachePolicy( System.Net.Cache.RequestCacheLevel.NoCacheNoStore );
                    byte[] formData = Encoding.ASCII.GetBytes( data + "&users=" + World.GetPlayerCount() );
                    request.ContentLength = formData.Length;

                    requestStream = request.GetRequestStream();
                    requestStream.Write( formData, 0, formData.Length );
                    requestStream.Flush();

                    response = request.GetResponse();
                    responseReader = new StreamReader( response.GetResponseStream() );

                    Config.ServerURL = responseReader.ReadLine();
                    Logger.Log( "HeartBeat: " + Config.ServerURL );

                } catch( Exception ex ) {
                    Logger.LogError( "HeartBeat: " + ex.Message );

                } finally {
                    // free up system resources
                    if( requestStream != null ) {
                        requestStream.Close();
                        requestStream = null;
                    }
                    if( responseReader != null ) {
                        responseReader.Close();
                        responseReader = null;
                    }
                    if( response != null ) {
                        response.Close();
                        response = null;
                    }
                }

                Thread.Sleep( Config.HeartBeatDelay );
            }
        }

        ~HeartBeat() {
            if( thread != null && thread.IsAlive )
                thread.Abort();
        }
    }
}
