﻿// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
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
            bool hasReportedServerURL = false;

            while( true ) {
                try {
                    request = (HttpWebRequest)WebRequest.Create( URL );
                    request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( BindIPEndPointCallback );
                    request.Method = "POST";
                    request.Timeout = HeartbeatTimeout;
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

                    string dataString = String.Format( "name={0}&motd={1}&max={2}&public={3}&port={4}&salt={5}&version={6}&users={7}",
                                                       Server.UrlEncode( Config.GetString( ConfigKey.ServerName ) ),
                                                       Server.UrlEncode( Config.GetString( ConfigKey.MOTD ) ),
                                                       Config.GetInt( ConfigKey.MaxPlayers ),
                                                       Config.GetBool( ConfigKey.IsPublic ),
                                                       Server.Port,
                                                       Server.Salt,
                                                       Config.ProtocolVersion,
                                                       Server.GetPlayerCount() );

                    byte[] formData = Encoding.ASCII.GetBytes( dataString );
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