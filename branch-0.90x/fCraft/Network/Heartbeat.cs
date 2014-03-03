// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Static class responsible for sending heartbeats. </summary>
    public static class Heartbeat {
        /// <summary> Server's public URL, as returned by the heartbeat server.
        /// This is the URL that players should be able to connect by.
        /// May be null (if heartbeat is disabled, or first heartbeat has not been sent yet). </summary>
        [CanBeNull]
        public static Uri Url { get; internal set; }

        internal static Uri HeartbeatServerUrl;
        static readonly TimeSpan DelayDefault = TimeSpan.FromSeconds( 20 );
        static readonly TimeSpan TimeoutDefault = TimeSpan.FromSeconds( 10 );

        /// <summary> Delay between sending heartbeats. Default: 20s </summary>
        public static TimeSpan Delay { get; set; }

        /// <summary> Request timeout for heartbeats. Default: 10s </summary>
        public static TimeSpan Timeout { get; set; }

        /// <summary> Secret string used to verify players' names.
        /// Randomly generated at startup, and can be randomized by "/reload salt"
        /// Known only to this server and to heartbeat server(s). </summary>
        [NotNull]
        public static string Salt { get; internal set; }


        // Dns lookup, to make sure that IPv4 is preferred for heartbeats
        static readonly Dictionary<string, IPAddress> TargetAddresses = new Dictionary<string, IPAddress>(); 
        static DateTime nextDnsLookup = DateTime.MinValue;
        static readonly TimeSpan DnsRefreshInterval = TimeSpan.FromMinutes(30);


        static IPAddress RefreshTargetAddress([NotNull] Uri requestUri) {
            if( requestUri == null ) throw new ArgumentNullException("requestUri");

            string hostName = requestUri.Host.ToLowerInvariant();
            IPAddress targetAddress;
            if( !TargetAddresses.TryGetValue(hostName, out targetAddress) || DateTime.UtcNow >= nextDnsLookup ) {
                try {
                    // Perform a DNS lookup on given host. Throws SocketException if no host found.
                    IPAddress[] allAddresses = Dns.GetHostAddresses(requestUri.Host);
                    // Find a suitable IPv4 address. Throws InvalidOperationException if none found.
                    targetAddress = allAddresses.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                } catch( SocketException ex ) {
                    Logger.Log(LogType.Error,
                               "Heartbeat.RefreshTargetAddress: Error looking up heartbeat server URLs: {0}",
                               ex);
                } catch( InvalidOperationException ) {
                    Logger.Log(LogType.Warning,
                               "Heartbeat.RefreshTargetAddress: {0} does not have an IPv4 address!", requestUri.Host);
                }
                TargetAddresses[hostName] = targetAddress;
                nextDnsLookup = DateTime.UtcNow + DnsRefreshInterval;
            }
            return targetAddress;
        }


        static Heartbeat() {
            Delay = DelayDefault;
            Timeout = TimeoutDefault;
            Salt = Server.GetRandomString( 32 );
            Server.ShutdownBegan += OnServerShutdown;
        }


        static void OnServerShutdown( [CanBeNull] object sender, [NotNull] ShutdownEventArgs e ) {
            if( minecraftNetRequest != null ) {
                minecraftNetRequest.Abort();
            }
        }


        internal static void Start() {
            Scheduler.NewBackgroundTask( Beat ).RunForever( Delay );
        }


        static void Beat( [NotNull] SchedulerTask scheduledTask ) {
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
                    ConfigKey.IsPublic.GetString(),
                    ConfigKey.HeartbeatUrl.GetString()
                };
                const string tempFile = Paths.HeartbeatDataFileName + ".tmp";
                File.WriteAllLines( tempFile, data, Encoding.ASCII );
                Paths.MoveOrReplaceFile( tempFile, Paths.HeartbeatDataFileName );
            }
        }


        static HttpWebRequest minecraftNetRequest;

        static void SendMinecraftNetBeat() {
            HeartbeatData data = new HeartbeatData( HeartbeatServerUrl );
            if( !RaiseHeartbeatSendingEvent(data, HeartbeatServerUrl) ) {
                return;
            }
            minecraftNetRequest = CreateRequest( data.CreateUri() );
            var state = new HeartbeatRequestState( minecraftNetRequest, data );
            minecraftNetRequest.BeginGetResponse( ResponseCallback, state );
        }


        // Creates an asynchronous HTTP request to the given URL
        [NotNull]
        static HttpWebRequest CreateRequest([NotNull] Uri uri) {
            if( uri == null ) throw new ArgumentNullException("uri");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.CachePolicy = Server.CachePolicy;
            request.Method = "GET";
            request.ReadWriteTimeout = (int)Timeout.TotalMilliseconds;
            request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.UserAgent = Updater.UserAgent;
            if( uri.Scheme == "http" ) {
                request.Proxy = new WebProxy("http://" + RefreshTargetAddress(uri) + ":" + uri.Port);
            }
            return request;
        }


        // Called when the heartbeat server responds.
        static void ResponseCallback( [NotNull] IAsyncResult result ) {
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

                // try parse response as server Url, if needed
                string replyString = responseText.Trim();
                if( replyString.StartsWith( "bad heartbeat", StringComparison.OrdinalIgnoreCase ) ) {
                    Logger.Log( LogType.Error, "Heartbeat: {0}", replyString );
                } else {
                    try {
                        Uri newUri = new Uri( replyString );
                        Uri oldUri = Url;
                        if( newUri != oldUri ) {
                            Url = newUri;
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

        /// <summary> Occurs when the server Url has been set or changed. </summary>
        public static event EventHandler<UrlChangedEventArgs> UriChanged;


        static bool RaiseHeartbeatSendingEvent( [NotNull] HeartbeatData data, [NotNull] Uri uri ) {
            var h = Sending;
            if( h == null ) return true;
            var e = new HeartbeatSendingEventArgs( data, uri );
            h( null, e );
            return !e.Cancel;
        }

        static void RaiseHeartbeatSentEvent( [NotNull] HeartbeatData heartbeatData, [NotNull] HttpWebResponse response,
                                             [NotNull] string text ) {
            var h = Sent;
            if( h != null ) {
                h( null,
                   new HeartbeatSentEventArgs( heartbeatData,
                                               response.Headers,
                                               response.StatusCode,
                                               text ) );
            }
        }

        static void RaiseUriChangedEvent( [CanBeNull] Uri oldUri, [NotNull] Uri newUri ) {
            var h = UriChanged;
            if( h != null ) h( null, new UrlChangedEventArgs( oldUri, newUri ) );
        }

        #endregion

        sealed class HeartbeatRequestState {
            public HeartbeatRequestState( [NotNull] HttpWebRequest request, [NotNull] HeartbeatData data ) {
                if( request == null ) throw new ArgumentNullException( "request" );
                if( data == null ) throw new ArgumentNullException( "data" );
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
            Port = Server.Port;
            ProtocolVersion = Config.ProtocolVersion;
            Salt = Heartbeat.Salt;
            ServerName = ConfigKey.ServerName.GetString();
            CustomData = new Dictionary<string, string>();
            HeartbeatUri = heartbeatUri;
        }

        /// <summary> The heartbeat URI sent to minecraft.net in order to remain on the server list. </summary>
        [NotNull]
        public Uri HeartbeatUri { get; private set; }

        /// <summary> Server salt used in name verification (hashing). </summary>
        [NotNull]
        public string Salt { get; set; }

        /// <summary> Port that players should connect to in order to join this server. </summary>
        public int Port { get; set; }

        /// <summary> Number of players currently in the server. </summary>
        public int PlayerCount { get; set; }

        /// <summary> Maximum number of player the server can support. </summary>
        public int MaxPlayers { get; set; }

        /// <summary> Name of the server to display on minecraft.net. </summary>
        [NotNull]
        public string ServerName { get; set; }

        /// <summary> Whether or not the server should be listed on minecraft.net </summary>
        public bool IsPublic { get; set; }

        /// <summary> Version of the classic minecraft protocol that this server is using. </summary>
        public int ProtocolVersion { get; set; }

        /// <summary> Any other custom data that needs to be sent. </summary>
        [NotNull]
        public Dictionary<string, string> CustomData { get; private set; }


        [NotNull]
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
    /// HeartbeatData and Url may be modified. </summary>
    public sealed class HeartbeatSendingEventArgs : EventArgs, ICancelableEvent {
        internal HeartbeatSendingEventArgs( [NotNull] HeartbeatData data, [NotNull] Uri url ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            HeartbeatData = data;
            Url = url;
        }

        /// <summary> Data that will be sent to the heartbeat server. </summary>
        [NotNull]
        public HeartbeatData HeartbeatData { get; private set; }

        /// <summary> Url of the heartbeat server. </summary>
        [NotNull]
        public Uri Url { get; set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Heartbeat.Sent event. Immutable. </summary>
    public sealed class HeartbeatSentEventArgs : EventArgs {
        internal HeartbeatSentEventArgs( [NotNull] HeartbeatData heartbeatData, [NotNull] WebHeaderCollection headers,
                                         HttpStatusCode status, [NotNull] string text ) {
            if( heartbeatData == null ) throw new ArgumentNullException( "heartbeatData" );
            if( headers == null ) throw new ArgumentNullException( "headers" );
            if( text == null ) throw new ArgumentNullException( "text" );
            HeartbeatData = heartbeatData;
            ResponseHeaders = headers;
            ResponseStatusCode = status;
            ResponseText = text;
        }

        /// <summary> Data that was sent to the heartbeat server. </summary>
        [NotNull]
        public HeartbeatData HeartbeatData { get; private set; }

        /// <summary> Response headers received from the heartbeat servers. </summary>
        [NotNull]
        public WebHeaderCollection ResponseHeaders { get; private set; }

        /// <summary> HTTP status code returned by the heartbeat server. </summary>
        public HttpStatusCode ResponseStatusCode { get; private set; }

        /// <summary> Raw response returned by the heartbeat server. </summary>
        [NotNull]
        public string ResponseText { get; private set; }
    }


    /// <summary> Provides data for Heartbeat.UriChanged event. Immutable. </summary>
    public sealed class UrlChangedEventArgs : EventArgs {
        internal UrlChangedEventArgs( [CanBeNull] Uri oldUrl, [NotNull] Uri newUrl ) {
            if( newUrl == null ) throw new ArgumentNullException( "newUrl" );
            OldUrl = oldUrl;
            NewUrl = newUrl;
        }

        /// <summary> This server's old URL. </summary>
        [CanBeNull]
        public Uri OldUrl { get; private set; }

        /// <summary> This server's new, freshly-returned URL. </summary>
        [NotNull]
        public Uri NewUrl { get; private set; }
    }
}
