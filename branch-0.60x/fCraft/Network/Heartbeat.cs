// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Static class responsible for sending heartbeats. </summary>
    public static class Heartbeat {
        static readonly Uri MinecraftNetUri;

        /// <summary> Delay between sending heartbeats. Default: 20s </summary>
        public static TimeSpan Delay { get; set; }

        /// <summary> Request timeout for heartbeats. Default: 10s </summary>
        public static TimeSpan Timeout { get; set; }

        /// <summary> Secret string used to verify players' names.
        /// Randomly generated at startup.
        /// Known only to this server and to heartbeat server(s). </summary>
        public static string Salt { get; internal set; }


        static Heartbeat() {
            MinecraftNetUri = new Uri( "https://minecraft.net/heartbeat.jsp" );
            Delay = TimeSpan.FromSeconds( 20 );
            Timeout = TimeSpan.FromSeconds( 10 );
            Salt = Server.GetRandomString( 32 );
            Server.ShutdownBegan += OnServerShutdown;
        }


        static void OnServerShutdown( object sender, ShutdownEventArgs e ) {
            if( minecraftNetRequest != null ) {
                minecraftNetRequest.Abort();
            }
        }


        internal static void Start() {
            Scheduler.NewBackgroundTask( Beat ).RunForever( Delay );
        }


        static void Beat( SchedulerTask scheduledTask ) {
            if( Server.IsShuttingDown ) return;

            if( ConfigKey.HeartbeatEnabled.Enabled() ) {
                SendMinecraftNetBeat();

            } else {
                // If heartbeats are disabled, the server data is written
                // to a text file instead (heartbeatdata.txt)
                string[] data = {
                    Salt,
                    Server.InternalIP.ToString(),
                    Server.Port.ToStringInvariant(),
                    Server.CountPlayers( false ).ToStringInvariant(),
                    ConfigKey.MaxPlayers.GetString(),
                    ConfigKey.ServerName.GetString(),
                    ConfigKey.IsPublic.GetString()
                };
                const string tempFile = Paths.HeartbeatDataFileName + ".tmp";
                File.WriteAllLines( tempFile, data, Encoding.ASCII );
                Paths.MoveOrReplaceFile( tempFile, Paths.HeartbeatDataFileName );
            }
        }


        static HttpWebRequest minecraftNetRequest;

        static void SendMinecraftNetBeat() {
            HeartbeatData data = new HeartbeatData( MinecraftNetUri );
            if( !RaiseHeartbeatSendingEvent( data, MinecraftNetUri, true ) ) {
                return;
            }
            minecraftNetRequest = CreateRequest( data.CreateUri() );
            var state = new HeartbeatRequestState( minecraftNetRequest, data );
            minecraftNetRequest.BeginGetResponse( ResponseCallback, state );
        }


        // Creates an asynchronous HTTP request to the given URL
        static HttpWebRequest CreateRequest( Uri uri ) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( uri );
            request.CachePolicy = Server.CachePolicy;
            request.Method = "GET";
            request.ReadWriteTimeout = (int)Timeout.TotalMilliseconds;
            request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.UserAgent = Updater.UserAgent;
            return request;
        }


        // Called when the heartbeat server responds.
        static void ResponseCallback( IAsyncResult result ) {
            if( Server.IsShuttingDown ) return;
            HeartbeatRequestState state = (HeartbeatRequestState)result.AsyncState;
            try {
                string responseText;
                using( HttpWebResponse response = (HttpWebResponse)state.Request.EndGetResponse( result ) ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        responseText = responseReader.ReadToEnd();
                    }
                    RaiseHeartbeatSentEvent( state.Data, response, responseText );
                }

                // try parse response as server Uri, if needed
                string replyString = responseText.Trim();
                if( replyString.StartsWith( "bad heartbeat", StringComparison.OrdinalIgnoreCase ) ) {
                    Logger.Log( LogType.Error, "Heartbeat: {0}", replyString );
                } else {
                    try {
                        Uri newUri = new Uri( replyString );
                        Uri oldUri = Server.Uri;
                        if( newUri != oldUri ) {
                            Server.Uri = newUri;
                            RaiseUriChangedEvent( oldUri, newUri );
                        }
                    } catch( UriFormatException ) {
                        Logger.Log( LogType.Error,
                                    "Heartbeat: Server replied with: {0}",
                                    replyString );
                    }
                }

            } catch( Exception ex ) {
                if( ex is WebException || ex is IOException ) {
                    Logger.Log( LogType.Warning,
                                "Heartbeat: {0} is probably down ({1})",
                                state.Request.RequestUri.Host,
                                ex.Message );
                } else {
                    Logger.Log( LogType.Error, "Heartbeat: {0}", ex );
                }
            }
        }


        #region Events

        /// <summary> Occurs when a heartbeat is about to be sent (cancelable). </summary>
        public static event EventHandler<HeartbeatSendingEventArgs> Sending;

        /// <summary> Occurs when a heartbeat has been sent. </summary>
        public static event EventHandler<HeartbeatSentEventArgs> Sent;

        /// <summary> Occurs when the server Uri has been set or changed. </summary>
        public static event EventHandler<UriChangedEventArgs> UriChanged;


        static bool RaiseHeartbeatSendingEvent( HeartbeatData data, Uri uri, bool getServerUri ) {
            var h = Sending;
            if( h == null ) return true;
            var e = new HeartbeatSendingEventArgs( data, uri, getServerUri );
            h( null, e );
            return !e.Cancel;
        }

        static void RaiseHeartbeatSentEvent( HeartbeatData heartbeatData,
                                             HttpWebResponse response,
                                             string text ) {
            var h = Sent;
            if( h != null ) {
                h( null, new HeartbeatSentEventArgs( heartbeatData,
                                                     response.Headers,
                                                     response.StatusCode,
                                                     text ) );
            }
        }

        static void RaiseUriChangedEvent( Uri oldUri, Uri newUri ) {
            var h = UriChanged;
            if( h != null ) h( null, new UriChangedEventArgs( oldUri, newUri ) );
        }

        #endregion


        sealed class HeartbeatRequestState {
            public HeartbeatRequestState( HttpWebRequest request, HeartbeatData data ) {
                Request = request;
                Data = data;
            }
            public readonly HttpWebRequest Request;
            public readonly HeartbeatData Data;
        }
    }


    /// <summary> Contains data that's sent to heartbeat servers. </summary>
    public sealed class HeartbeatData {
        internal HeartbeatData( [NotNull] Uri heartbeatUri ) {
            if( heartbeatUri == null ) throw new ArgumentNullException( "heartbeatUri" );
            IsPublic = ConfigKey.IsPublic.Enabled();
            MaxPlayers = ConfigKey.MaxPlayers.GetInt();
            PlayerCount = Server.CountPlayers( false );
            ServerIP = Server.InternalIP;
            Port = Server.Port;
            ProtocolVersion = Config.ProtocolVersion;
            Salt = Heartbeat.Salt;
            ServerName = ConfigKey.ServerName.GetString();
            CustomData = new Dictionary<string, string>();
            HeartbeatUri = heartbeatUri;
        }

        /// <summary> The heartbeat Uri sent to minecraft.net in order to remain on the server list. </summary>
        [NotNull]
        public Uri HeartbeatUri { get; private set; }

        /// <summary> Server salt used in name verification (hashing). </summary>
        public string Salt { get; set; }

        /// <summary> IP address of this server. </summary>
        public IPAddress ServerIP { get; set; }

        /// <summary> Port that players should connect to in order to join this server. </summary>
        public int Port { get; set; }

        /// <summary> Number of players currently in the server. </summary>
        public int PlayerCount { get; set; }

        /// <summary> Maximum number of player the server can support. </summary>
        public int MaxPlayers { get; set; }

        /// <summary> Name of the server to display on minecraft.net. </summary>
        public string ServerName { get; set; }

        /// <summary> Wether or not the server should be listed on minecraft.net </summary>
        public bool IsPublic { get; set; }

        /// <summary> Version of the classice minecraft protocol that this server is using. </summary>
        public int ProtocolVersion { get; set; }

        /// <summary> Any other custom data that needs to be sent. </summary>
        public Dictionary<string, string> CustomData { get; private set; }


        internal Uri CreateUri() {
            UriBuilder ub = new UriBuilder( HeartbeatUri );
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( "public={0}&max={1}&users={2}&port={3}&version={4}&salt={5}&name={6}",
                             IsPublic,
                             MaxPlayers,
                             PlayerCount,
                             Port,
                             ProtocolVersion,
                             Uri.EscapeDataString( Salt ),
                             Uri.EscapeDataString( ServerName ) );
            foreach( var pair in CustomData ) {
                sb.AppendFormat( "&{0}={1}",
                                 Uri.EscapeDataString( pair.Key ),
                                 Uri.EscapeDataString( pair.Value ) );
            }
            ub.Query = sb.ToString();
            return ub.Uri;
        }
    }
}


namespace fCraft.Events {
    /// <summary> Provides data for Heartbeat.Sending event. Cancelable. 
    /// HeartbeatData may be modified, Uri and GetSercerUri may be changed. </summary>
    public sealed class HeartbeatSendingEventArgs : EventArgs, ICancelableEvent {
        internal HeartbeatSendingEventArgs( HeartbeatData data, Uri uri, bool getServerUri ) {
            HeartbeatData = data;
            Uri = uri;
            GetServerUri = getServerUri;
        }

        public HeartbeatData HeartbeatData { get; private set; }
        public Uri Uri { get; set; }
        public bool GetServerUri { get; set; }
        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Heartbeat.Sent event. Immutable. </summary>
    public sealed class HeartbeatSentEventArgs : EventArgs {
        internal HeartbeatSentEventArgs( HeartbeatData heartbeatData,
                                         WebHeaderCollection headers,
                                         HttpStatusCode status,
                                         string text ) {
            HeartbeatData = heartbeatData;
            ResponseHeaders = headers;
            ResponseStatusCode = status;
            ResponseText = text;
        }

        public HeartbeatData HeartbeatData { get; private set; }
        public WebHeaderCollection ResponseHeaders { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; private set; }
        public string ResponseText { get; private set; }
    }


    /// <summary> Provides data for Heartbeat.UriChanged event. Immutable. </summary>
    public sealed class UriChangedEventArgs : EventArgs {
        internal UriChangedEventArgs( Uri oldUri, Uri newUri ) {
            OldUri = oldUri;
            NewUri = newUri;
        }

        public Uri OldUri { get; private set; }
        public Uri NewUri { get; private set; }
    }
}