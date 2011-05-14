// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ThreadState = System.Threading.ThreadState;
using fCraft.AutoRank;

namespace fCraft {
    public static partial class Server {

        public static DateTime ServerStart { get; private set; }

        internal static int MaxUploadSpeed,   // set by Config.ApplyConfig
                            PacketsPerSecond; // used when there are no players in a world

        internal const int MaxSessionPacketsPerTick = 128, // used when there are no players in a world
                           MaxBlockUpdatesPerTick = 100000; // used when there are no players in a world
        internal static float TicksPerSecond;


        // networking
        static TcpListener listener;
        public static IPAddress IP { get; private set; }

        const int MaxPortAttempts = 20;
        public static int Port { get; private set; }

        public static string Url { get; internal set; }


        #region Command-line args

        static readonly Dictionary<ArgKey, string> Args = new Dictionary<ArgKey, string>();

        /// <summary> Returns value of a given command-line argument (if present). Use HasArg to check flag arguments. </summary>
        /// <param name="key"> Command-line argument name (enumerated) </param>
        /// <returns> Value of the command-line argument, or null if this argument was not set or argument is a flag. </returns>
        public static string GetArg( ArgKey key ) {
            if( Args.ContainsKey( key ) ) {
                return Args[key];
            } else {
                return null;
            }
        }

        /// <summary> Checks whether a command-line argument was set. </summary>
        /// <param name="key"> Command-line argument name (enumerated) </param>
        /// <returns> True if given argument was given. Otherwise false. </returns>
        public static bool HasArg( ArgKey key ) {
            return Args.ContainsKey( key );
        }


        /// <summary> Produces a string containing all recognized arguments that wereset/passed to this instance of fCraft. </summary>
        /// <returns> A string containing all given arguments, or an empty string if none were set. </returns>
        public static string GetArgString() {
            return String.Join( " ", GetArgList() );
        }


        /// <summary> Produces a list of arguments that were passed to this instance of fCraft. </summary>
        /// <returns> An array of strings, formatted as --key="value" (or, for flag arguments, --key).
        /// Returns an empty string array if no arguments were set. </returns>
        public static string[] GetArgList() {
            List<string> argList = new List<string>();
            foreach( var pair in Args ) {
                if( pair.Value != null ) {
                    argList.Add( String.Format( "--{0}=\"{1}\"", pair.Key.ToString().ToLower(), pair.Value ) );
                } else {
                    argList.Add( String.Format( "--{0}", pair.Key.ToString().ToLower() ) );
                }
            }
            return argList.ToArray();
        }

        #endregion


        #region Initialization

        // flags used to ensure proper initialization order
        static bool libraryInitialized,
                    serverInitialized;

        /// <summary> Reads command-line switches and sets up paths and logging.
        /// This should be called before any other library function.
        /// Note to frontend devs: Subscribe to log-related events before calling this.
        /// Does not raise any events besides Logger.Logged.
        /// Throws exceptions on failure. </summary>
        /// <param name="rawArgs"> string arguments passed to the frontend (if any). </param>
        public static void InitLibrary( IEnumerable<string> rawArgs ) {
            if( rawArgs == null ) throw new ArgumentNullException( "rawArgs" );

            // try to parse arguments
            foreach( string arg in rawArgs ) {
                if( arg.StartsWith( "--" ) ) {
                    string argKeyName, argValue;
                    if( arg.Contains( '=' ) ) {
                        argKeyName = arg.Substring( 2, arg.IndexOf( '=' ) - 2 ).ToLower().Trim();
                        argValue = arg.Substring( arg.IndexOf( '=' ) + 1 ).Trim();
                        if( argValue.StartsWith( "\"" ) && argValue.EndsWith( "\"" ) ) {
                            argValue = argValue.Substring( 1, argValue.Length - 2 );
                        }

                    } else {
                        argKeyName = arg.Substring( 2 );
                        argValue = null;
                    }
                    try {
                        ArgKey tryKey = (ArgKey)Enum.Parse( typeof( ArgKey ), argKeyName, true );
                        Args.Add( tryKey, argValue );
                    } catch( ArgumentException ) {
                        Console.Error.WriteLine( "Unknown argument: {0}", arg );
                    }
                } else {
                    Console.Error.WriteLine( "Unknown argument: {0}", arg );
                }
            }


            // before we do anything, set path to the default location
            Directory.SetCurrentDirectory( Paths.WorkingPath );

            // set custom working path (if specified)
            if( HasArg( ArgKey.Path ) && Paths.TestDirectory( "WorkingPath", GetArg( ArgKey.Path ), true ) ) {
                Paths.WorkingPath = Path.GetFullPath( GetArg( ArgKey.Path ) );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else if( Paths.TestDirectory( "WorkingPath", Paths.WorkingPathDefault, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( Paths.WorkingPathDefault );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else {
                throw new Exception( "Could not set the working path." );
            }


            // set log path
            if( HasArg( ArgKey.LogPath ) && Paths.TestDirectory( "LogPath", GetArg( ArgKey.LogPath ), true ) ) {
                Paths.LogPath = Path.GetFullPath( GetArg( ArgKey.LogPath ) );
            } else if( Paths.TestDirectory( "LogPath", Paths.LogPathDefault, true ) ) {
                Paths.LogPath = Path.GetFullPath( Paths.LogPathDefault );
            } else {
                throw new Exception( "Could not set the log path." );
            }


            // set map path
            if( HasArg( ArgKey.MapPath ) && Paths.TestDirectory( "MapPath", GetArg( ArgKey.MapPath ), true ) ) {
                Paths.MapPath = Path.GetFullPath( GetArg( ArgKey.MapPath ) );
                Paths.IgnoreMapPathConfigKey = true;
            } else if( Paths.TestDirectory( "MapPath", Paths.MapPathDefault, true ) ) {
                Paths.MapPath = Path.GetFullPath( Paths.MapPathDefault );
            } else {
                throw new Exception( "Could not set the map path." );
            }


            // set config path
            Paths.ConfigFileName = Paths.ConfigFileNameDefault;
            if( HasArg( ArgKey.Config ) ) {
                string fileName = GetArg( ArgKey.Config );
                if( Paths.TestFile( "config.xml", fileName, false, true, false ) ) {
                    Paths.ConfigFileName = new FileInfo( fileName ).FullName;
                }
            }

            if( MonoCompat.IsMono ) {
                Logger.Log( "Running on Mono {0}", LogType.Debug, MonoCompat.MonoVersion );
            }

#if DEBUG_EVENTS
            Logger.PrepareEventTracing();
#endif

            Logger.Log( "Working directory: {0}", LogType.Debug, Directory.GetCurrentDirectory() );
            Logger.Log( "Log path: {0}", LogType.Debug, Path.GetFullPath( Paths.LogPath ) );
            Logger.Log( "Map path: {0}", LogType.Debug, Path.GetFullPath( Paths.MapPath ) );
            Logger.Log( "Config path: {0}", LogType.Debug, Path.GetFullPath( Paths.ConfigFileName ) );

            libraryInitialized = true;
        }


        /// <summary> Initialized various server subsystems. This should be called after InitLibrary and before StartServer.
        /// Loads config, PlayerDB, IP bans, AutoRank settings, builds a list of commands, and prepares the IRC bot.
        /// Raises Server.Initializing and Server.Initialized events, and possibly Logger.Logged events.
        /// Throws exceptions on failure. </summary>
        public static void InitServer() {
            if( !libraryInitialized ) {
                throw new Exception( "Server.InitializeLibrary must be called before Server.InitServer" );
            }
            RaiseInitializingEvent( Args );

            // warnings/disclaimers
            if( Updater.CurrentRelease.IsFlagged( ReleaseFlags.Dev ) ) {
                Logger.Log( "You are using an unreleased developer version of fCraft. " +
                            "Do not use this version unless you are ready to deal with bugs and potential data loss. " +
                            "Consider using the lastest stable version instead, available from www.fcraft.net",
                            LogType.Warning );
            }

            if( Updater.CurrentRelease.IsFlagged( ReleaseFlags.Unstable ) ) {
                const string unstableMessage = "This build has been marked as UNSTABLE. " +
                                               "Do not use except for debugging purposes. " +
                                               "Latest non-broken build is " + Updater.LatestStable;
#if DEBUG
                Logger.Log( unstableMessage, LogType.Warning, Updater.LatestStable );
#else
                throw new Exception( unstableMessage );
#endif
            }

            if( MonoCompat.IsMono && !MonoCompat.IsSGen ) {
                Logger.Log( "You are using a relatively old version of the Mono runtime ({0}). " +
                            "It is recommended that you upgrade to at least 2.8+", LogType.Warning,
                            MonoCompat.MonoVersion );
            }

#if DEBUG
            Config.RunSelfTest();
#else
            // delete the old updater, if exists
            try {
                if( File.Exists( Paths.UpdaterFile ) ) {
                    File.Delete( Paths.UpdaterFile );
                }
            } catch { }
#endif

            // try to load the config
            if( !Config.Load( false, false ) ) {
                throw new Exception( "fCraft failed to initialize" );
            }
            Config.ApplyConfig();
            Salt = GenerateSalt();

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            // prepare the list of commands
            CommandManager.Init();

            // Init IRC
            IRC.Init();

            if( ConfigKey.AutoRankEnabled.GetBool() ) {
                AutoRankManager.Init();
            }

            RaiseEvent( Initialized );

            serverInitialized = true;
        }


        /// <summary> Starts the server:
        /// Creates Console pseudoplayer, loads the world list, starts listening for incoming connections,
        /// sets up scheduled tasks and starts the scheduler, starts the heartbeat, and connects to IRC.
        /// Raises Server.Starting and Server.Started events.
        /// May throw an exception on hard failure. </summary>
        /// <returns> True if server started normally, false on soft failure. </returns>
        public static bool StartServer() {
            if( !serverInitialized ) {
                throw new Exception( "Server.InitServer() must be called before Server.StartServer()" );
            }
            ServerStart = DateTime.UtcNow;

            RaiseEvent( Starting );

            if( CheckForFCraftProcesses() ) {
                Logger.Log( "Please close all other fCraft processes (fCraftUI, fCraftConsole, or ConfigTool) " +
                            "that are started from the same directory.", LogType.Warning );
            }

            Player.Console = new Player( null, ConfigKey.ConsoleName.GetString() );


            // try to load the world list
            if( !WorldManager.LoadWorldList() ) return false;
            WorldManager.SaveWorldList();

            // open the port
            bool portFound = false;
            int attempts = 0;
            Port = ConfigKey.Port.GetInt();
            IP = IPAddress.Parse( ConfigKey.IP.GetString() );

            do {
                try {
                    listener = new TcpListener( IP, Port );
                    listener.Start();
                    portFound = true;

                } catch( Exception ex ) {
                    // if the port is unavailable, try next one
                    Logger.Log( "Could not start listening on port {0}, trying next port. ({1})", LogType.Error,
                                Port, ex.Message );
                    Port++;
                    attempts++;
                }
            } while( !portFound && attempts < MaxPortAttempts );

            // if the port still cannot be opened after [maxPortAttempts] attemps, die.
            if( !portFound ) {
                Logger.Log( "Could not start listening on any IP/port. Giving up after {0} tries.", LogType.SeriousError,
                            MaxPortAttempts );
                if( !ConfigKey.IP.IsBlank() ) {
                    Logger.Log( "Do not use the \"Designated IP\" setting unless you have multiple NICs or IPs.", LogType.Warning,
                                MaxPortAttempts );
                }
                return false;
            }

            IP = ((IPEndPoint)listener.LocalEndpoint).Address;

            if( IP.Equals( IPAddress.Any ) ) {
                Logger.Log( "Server.Run: now accepting connections at port {0}.", LogType.SystemActivity,
                            Port );
            } else {
                Logger.Log( "Server.Run: now accepting connections at {0}:{1}.", LogType.SystemActivity,
                            IP, Port );
            }

            // list loaded worlds
            WorldManager.UpdateWorldList();
            Logger.Log( "All available worlds: {0}", LogType.SystemActivity,
                        WorldManager.WorldList.JoinToString( ", ", w => w.Name ) );

            Logger.Log( "Main world: {0}; default rank: {1}", LogType.SystemActivity,
                        WorldManager.MainWorld.Name, RankManager.DefaultRank.Name );

            // Check for incoming connections (every 250ms)
            checkConnectionsTask = Scheduler.NewTask( CheckConnections ).RunForever( CheckConnectionsInterval );

            // Check for idles (every 30s)
            checkIdlesTask = Scheduler.NewTask( CheckIdles ).RunForever( CheckIdlesInterval );

            // Monitor CPU usage (every 30s)
            try {
                MonitorProcessorUsage( null );
                Scheduler.NewTask( MonitorProcessorUsage ).RunForever( MonitorProcessorUsageInterval,
                                                                       MonitorProcessorUsageInterval );
            } catch( Exception ex ) {
                Logger.Log( "Server.StartServer: Could not start monitoring CPU use: {0}", LogType.Error, ex );
            }


            PlayerDB.StartSaveTask();

            // Announcements
            if( ConfigKey.AnnouncementInterval.GetInt() > 0 ) {
                TimeSpan announcementInterval = TimeSpan.FromMinutes( ConfigKey.AnnouncementInterval.GetInt() );
                Scheduler.NewTask( ShowRandomAnnouncement ).RunForever( announcementInterval );
            }

            // garbage collection
            gcTask = Scheduler.NewTask( DoGC ).RunForever( GCInterval, TimeSpan.FromSeconds( 45 ) );

            // Write out initial (empty) playerlist cache
            UpdatePlayerList();

            // start the main loop - server is now connectible
            Scheduler.Start();

            Heartbeat.Start();

            if( ConfigKey.IRCBotEnabled.GetBool() ) IRC.Start();

            RaiseEvent( Started );
            return true;
        }

        #endregion


        #region Shutdown

        public static bool IsShuttingDown;
        static readonly AutoResetEvent ShutdownWaiter = new AutoResetEvent( false );
        static Thread shutdownThread;


        internal static void ShutdownNow( ShutdownParams shutdownParams ) {
            if( IsShuttingDown ) return; // to avoid starting shutdown twice
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            IsShuttingDown = true;
#if DEBUG
#else
            try {
#endif
                RaiseShutdownBeganEvent( shutdownParams );

                Scheduler.BeginShutdown();

                Logger.Log( "Server shutting down ({0})", LogType.SystemActivity,
                            shutdownParams.ReasonString );

                // stop accepting new players
                if( listener != null ) {
                    listener.Stop();
                    listener = null;
                }

                // kick all players
                lock( SessionLock ) {
                    foreach( Session s in Sessions ) {
                        // NOTE: kick packet delivery here is not currently guaranteed
                        s.Kick( "Server shutting down (" + shutdownParams.ReasonString + Color.White + ")", LeaveReason.ServerShutdown );
                    }
                    if( Sessions.Count > 0 ) {
                        // increase the chances of kick packets being delivered
                        Thread.Sleep( 1000 );
                    }
                }

                // kill IRC bot
                IRC.Disconnect();

                if( WorldManager.WorldList != null ) {
                    lock( WorldManager.WorldListLock ) {
                        // unload all worlds (includes saving)
                        foreach( World world in WorldManager.WorldList ) {
                            world.SaveMap();
                        }
                    }
                }

                Scheduler.EndShutdown();

                if( PlayerDB.IsLoaded ) PlayerDB.Save();
                if( IPBanList.IsLoaded ) IPBanList.Save();

                RaiseShutdownEndedEvent( shutdownParams );
#if DEBUG
#else
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error in Server.Shutdown", "fCraft", ex, true );
            }
#endif
        }


        /// <summary> Initiates the server shutdown with given parameters. </summary>
        /// <param name="shutdownParams"> Shutdown parameters </param>
        /// <param name="waitForShutdown"> If true, blocks the calling thread until shutdown is complete or cancelled. </param>
        public static void Shutdown( ShutdownParams shutdownParams, bool waitForShutdown ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            if( !CancelShutdown() ) return;
            shutdownThread = new Thread( ShutdownThread ) {
                Name = "fCraft.Shutdown"
            };
            shutdownThread.Start( shutdownParams );
            if( waitForShutdown ) {
                ShutdownWaiter.WaitOne();
            }
        }


        /// <summary> Attempts to cancel the shutdown timer. </summary>
        /// <returns> True if a shutdown timer was cancelled, false if no shutdown is in progress.
        /// Also returns false if it's too late to cancel (shutdown has begun). </returns>
        public static bool CancelShutdown() {
            if( shutdownThread != null ) {
                if( IsShuttingDown || shutdownThread.ThreadState != ThreadState.WaitSleepJoin ) {
                    return false;
                }
                ShutdownWaiter.Set();
                shutdownThread.Abort();
                shutdownThread = null;
            }
            return true;
        }


        static void ShutdownThread( object obj ) {
            ShutdownParams param = (ShutdownParams)obj;
            Thread.Sleep( param.Delay * 1000 );
            ShutdownNow( param );
            ShutdownWaiter.Set();

            bool doRestart = (param.Restart && !HasArg( ArgKey.NoRestart ));
            string assemblyExecutable = Assembly.GetEntryAssembly().Location;

            if( Updater.RunAtShutdown && doRestart ) {
                string args = String.Format( "--restart=\"{0}\" {1}",
                                             MonoCompat.PrependMono( assemblyExecutable ),
                                             GetArgString() );

                MonoCompat.StartDotNetProcess( Paths.UpdaterFile, args, true );

            } else if( Updater.RunAtShutdown ) {
                MonoCompat.StartDotNetProcess( Paths.UpdaterFile, GetArgString(), true );

            } else if( doRestart ) {
                MonoCompat.StartDotNetProcess( assemblyExecutable, GetArgString(), true );
            }

            if( param.KillProcess ) {
                Process.GetCurrentProcess().Kill();
            }
        }

        #endregion


        #region Messaging / Packet Sending

        // Send a low-priority packet to everyone
        // If 'except' is not null, excludes specified player
        public static void SendToAllLowPriority( Packet packet, Player except ) {
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].SendDelayed( packet );
                }
            }
        }


        // Send a normal priority packet to everyone
        public static void SendToAll( Packet packet ) {
            SendToAll( packet, null );
        }


        // Send a normal priority packet to everyone
        // If 'except' is not null, excludes specified player
        public static void SendToAll( Packet packet, Player except ) {
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != except ) {
                    tempList[i].Send( packet );
                }
            }
        }


        // Send a message to everyone
        // Wraps String.Format() for easy formatting
        public static void SendToAll( string message, params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            SendToAllExcept( message, null, formatArgs );
        }


        // Send a message to everyone (except a specified player)
        // Wraps String.Format() for easy formatting
        public static void SendToAllExcept( string message, Player except, params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs.Length > 0 ) message = String.Format( message, formatArgs );
            //if( except != Player.Console ) Logger.LogConsole( message );
            foreach( Packet p in PacketWriter.MakeWrappedMessage( "> ", message, false ) ) {
                SendToAll( p, except );
            }
        }


        public static void SendToAllWhoCan( string message, Player except, Permission permission, params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
            }
            foreach( Packet p in PacketWriter.MakeWrappedMessage( "> ", message, false ) ) {
                foreach( Player player in PlayerList.Where( pl => pl.Can( permission ) ) ) {
                    if( player != except ) {
                        player.Send( p );
                    }
                }
            }
        }


        public static void SendToAllWhoCant( string message, Player except, Permission permission, params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs.Length > 0 ) {
                message = String.Format( message, formatArgs );
            }
            foreach( Packet p in PacketWriter.MakeWrappedMessage( "> ", message, false ) ) {
                foreach( Player player in PlayerList.Where( pl => !pl.Can( permission ) ) ) {
                    if( player != except ) {
                        player.Send( p );
                    }
                }
            }
        }


        public static void SendToAllExceptIgnored( Player origin, string message, Player except, params object[] formatArgs ) {
            if( origin == null ) throw new ArgumentNullException( "origin" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs.Length > 0 ) message = String.Format( message, formatArgs );
            foreach( Packet p in PacketWriter.MakeWrappedMessage( "> ", message, false ) ) {
                Player[] tempList = PlayerList;
                for( int i = 0; i < tempList.Length; i++ ) {
                    if( tempList[i] != except && !tempList[i].IsIgnoring( origin.Info ) ) {
                        tempList[i].Send( p );
                    }
                }
            }
        }


        // Sends a packet to everyone who CAN see 'source' player
        public static void SendToSeeing( Packet packet, Player source ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            Player[] playerListCopy = PlayerList;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }


        // Sends a string to everyone who CAN see 'source' player
        public static void SendToSeeing( string message, Player source ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( source == null ) throw new ArgumentNullException( "source" );
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                SendToSeeing( packet, source );
            }
        }


        // Sends a packet to everyone who CAN'T see 'source' player
        public static void SendToBlind( Packet packet, Player source ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            Player[] playerListCopy = PlayerList;
            for( int i = 0; i < playerListCopy.Length; i++ ) {
                if( playerListCopy[i] != source && !playerListCopy[i].CanSee( source ) ) {
                    playerListCopy[i].Send( packet );
                }
            }
        }


        // Sends a string to everyone who CAN'T see 'source' player
        public static void SendToBlind( string message, Player source ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( source == null ) throw new ArgumentNullException( "source" );
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                SendToBlind( packet, source );
            }
        }


        // Sends a packet to all players of a specific rank
        public static void SendToRank( Packet packet, Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i].Info.Rank == rank ) {
                    tempList[i].Send( packet );
                }
            }
        }


        // Sends a string to all players of a specific rank
        public static void SendToRank( Player origin, string message, Rank rank ) {
            if( origin == null ) throw new ArgumentNullException( "origin" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                Player[] tempList = PlayerList;
                for( int i = 0; i < tempList.Length; i++ ) {
                    if( tempList[i].Info.Rank == rank && !tempList[i].IsIgnoring( origin.Info ) ) {
                        tempList[i].Send( packet );
                    }
                }
            }
        }

        #endregion


        #region Obsolete Events

        [Obsolete]
        public static event PlayerChangedWorldEventHandler OnPlayerChangedWorld;

        [Obsolete]
        public static event PlayerSentMessageEventHandler OnPlayerSentMessage;

        [Obsolete]
        public static event PlayerBanStatusChangedEventHandler OnPlayerBanned;

        [Obsolete]
        public static event PlayerBanStatusChangedEventHandler OnPlayerUnbanned;


        internal static void FireWorldChangedEvent( Player player, World oldWorld, World newWorld ) {
            if( OnPlayerChangedWorld != null ) OnPlayerChangedWorld( player, oldWorld, newWorld );
        }

        internal static bool FireSentMessageEvent( Player player, ref string message ) {
            bool cancel = false;
            if( OnPlayerSentMessage != null ) {
                OnPlayerSentMessage( player, player.World, ref message, ref cancel );
            }
            return !cancel;
        }

        internal static void FirePlayerBannedEvent( PlayerInfo player, Player banner, string reason ) {
            if( OnPlayerBanned != null ) {
                OnPlayerBanned( player, banner, reason );
            }
        }

        internal static void FirePlayerUnbannedEvent( PlayerInfo player, Player unbanner, string reason ) {
            if( OnPlayerUnbanned != null ) {
                OnPlayerUnbanned( player, unbanner, reason );
            }
        }

        #endregion


        #region Scheduled Tasks

        // checks for incoming connections
        static SchedulerTask checkConnectionsTask;
        static TimeSpan checkConnectionsInterval = TimeSpan.FromMilliseconds( 250 );
        public static TimeSpan CheckConnectionsInterval {
            get { return checkConnectionsInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException();
                checkConnectionsInterval = value;
                if( checkConnectionsTask != null ) checkConnectionsTask.Interval = value;
            }
        }

        static void CheckConnections( SchedulerTask param ) {
            TcpListener listenerCache = listener;
            if( listenerCache != null && listenerCache.Pending() ) {
                try {
                    Session newSession = new Session( listenerCache.AcceptTcpClient() );
                    newSession.Start();
                } catch( Exception ex ) {
                    Logger.Log( "Server.CheckConnections: Could not accept incoming connection: " + ex, LogType.Error );
                }
            }
        }


        // checks for idle players
        static SchedulerTask checkIdlesTask;
        static TimeSpan checkIdlesInterval = TimeSpan.FromSeconds( 30 );
        public static TimeSpan CheckIdlesInterval {
            get { return checkIdlesInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException();
                checkIdlesInterval = value;
                if( checkIdlesTask != null ) checkIdlesTask.Interval = checkIdlesInterval;
            }
        }

        static void CheckIdles( object param ) {
            Player[] tempPlayerList = PlayerList;
            foreach( Player player in tempPlayerList ) {
                if( player.Info.Rank.IdleKickTimer <= 0 ) continue;
                if( DateTime.UtcNow.Subtract( player.IdleTimer ).TotalMinutes >= player.Info.Rank.IdleKickTimer ) {
                    SendToAllExcept( "{0}&S was kicked for being idle for {1} min", player,
                                     player.GetClassyName(),
                                     player.Info.Rank.IdleKickTimer.ToString() );
                    ModerationCommands.DoKick( Player.Console,
                                               player,
                                               "Idle for " + player.Info.Rank.IdleKickTimer + " minutes",
                                               true,
                                               false,
                                               LeaveReason.IdleKick );
                    player.ResetIdleTimer(); // to prevent kick from firing more than once
                }
            }
        }


        // collects garbage (forced collection is necessary under Mono)
        static SchedulerTask gcTask;
        static TimeSpan gcInterval = TimeSpan.FromSeconds( 60 );
        public static TimeSpan GCInterval {
            get { return gcInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException();
                gcInterval = value;
                if( gcTask != null ) gcTask.Interval = gcInterval;
            }
        }

        static void DoGC( SchedulerTask task ) {
            if( !gcRequested ) return;
            gcRequested = false;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            Logger.Log( "Server.DoGC: Collected on schedule.", LogType.Debug );
        }


        // shows announcements
        static void ShowRandomAnnouncement( object param ) {
            if( !File.Exists( Paths.AnnouncementsFileName ) ) return;
            string[] lines = File.ReadAllLines( Paths.AnnouncementsFileName );
            if( lines.Length == 0 ) return;
            string line = lines[new Random().Next( 0, lines.Length )].Trim();
            if( line.Length > 0 ) {
                if( line.StartsWith( "&" ) ) {
                    SendToAll( "{0}", line );
                } else {
                    SendToAll( "{0}{1}", Color.Announcement, line );
                }
            }
        }


        // measures CPU usage

        public static bool IsMonitoringCPUUsage { get; private set; }
        public static double CPUUsageTotal { get; private set; }
        public static double CPUUsageLastMinute { get; private set; }

        static TimeSpan oldCPUTime = new TimeSpan( 0 );
        static readonly TimeSpan MonitorProcessorUsageInterval = TimeSpan.FromSeconds( 30 );
        static DateTime lastMonitorTime = DateTime.UtcNow;

        static void MonitorProcessorUsage( SchedulerTask task ) {
            TimeSpan newCPUTime = Process.GetCurrentProcess().TotalProcessorTime;
            CPUUsageLastMinute = (newCPUTime - oldCPUTime).TotalSeconds /
                                 (Environment.ProcessorCount * DateTime.UtcNow.Subtract( lastMonitorTime ).TotalSeconds);
            lastMonitorTime = DateTime.UtcNow;
            CPUUsageTotal = newCPUTime.TotalSeconds /
                            (Environment.ProcessorCount * DateTime.UtcNow.Subtract( ServerStart ).TotalSeconds);
            oldCPUTime = newCPUTime;
            IsMonitoringCPUUsage = true;
        }

        #endregion


        #region Utilities

        static bool gcRequested;

        public static void RequestGC() {
            gcRequested = true;
        }


        public static string Salt { get; private set; }

        static string GenerateSalt() {
            RandomNumberGenerator prng = RandomNumberGenerator.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while( sb.Length < 32 ) {
                prng.GetBytes( oneChar );
                if( oneChar[0] >= 33 && oneChar[0] <= 126 ) {
                    sb.Append( (char)oneChar[0] );
                }
            }
            return sb.ToString();
        }


        public static bool VerifyName( string name, string hash, string salt ) {
            while( hash.Length < 32 ) {
                hash = "0" + hash;
            }
            MD5 hasher = MD5.Create();
            StringBuilder sb = new StringBuilder( 32 );
            foreach( byte b in hasher.ComputeHash( Encoding.ASCII.GetBytes( salt + name ) ) ) {
                sb.AppendFormat( "{0:x2}", b );
            }
            return sb.ToString().Equals( hash, StringComparison.OrdinalIgnoreCase );
        }


        public static int CalculateMaxPacketsPerUpdate( World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            int packetsPerTick = (int)(PacketsPerSecond / TicksPerSecond);
            int maxPacketsPerUpdate = (int)(MaxUploadSpeed / TicksPerSecond * 128);

            int playerCount = world.PlayerList.Length;
            if( playerCount > 0 && !world.IsFlushing ) {
                maxPacketsPerUpdate /= playerCount;
                if( maxPacketsPerUpdate > packetsPerTick ) {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            } else {
                maxPacketsPerUpdate = MaxBlockUpdatesPerTick;
            }

            return maxPacketsPerUpdate;
        }


        public static bool CheckForFCraftProcesses() {
            try {
                Process[] processList = Process.GetProcesses();

                foreach( Process process in processList ) {
                    if( process.ProcessName.StartsWith( "fcraftui", StringComparison.OrdinalIgnoreCase ) ||
                        process.ProcessName.StartsWith( "configtool", StringComparison.OrdinalIgnoreCase ) ||
                        process.ProcessName.StartsWith( "fcraftconsole", StringComparison.OrdinalIgnoreCase ) ) {
                        if( process.Id != Process.GetCurrentProcess().Id ) {
                            Logger.Log( "Another fCraft process detected running: {0}", LogType.Warning, process.ProcessName );
                            return true;
                        }
                    }
                }
                return false;

            } catch( Exception ex ) {
                Logger.Log( "Server.CheckForFCraftProcesses: {0}", LogType.Debug, ex );
                return false;
            }
        }


        static readonly Regex RegexIP = new Regex( @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                                                   RegexOptions.Compiled );

        public static bool IsIP( string ipString ) {
            if( ipString == null ) throw new ArgumentNullException( "ipString" );
            return RegexIP.IsMatch( ipString );
        }

        #endregion


        #region Player and Session Management

        // player list
        static readonly SortedDictionary<string, Player> Players = new SortedDictionary<string, Player>();
        public static Player[] PlayerList { get; private set; }
        static readonly object PlayerListLock = new object();

        // session list
        static readonly List<Session> Sessions = new List<Session>();
        static readonly object SessionLock = new object();


        internal static bool RegisterSessionAndCheckConnectionCount( Session session ) {
            int maxSessions = ConfigKey.MaxConnectionsPerIP.GetInt();
            lock( SessionLock ) {
                if( maxSessions > 0 ) {
                    int sessionCount = 0;
                    foreach( Session s in Sessions ) {
                        if( s.IP.Equals( session.IP) ) {
                            sessionCount++;
                            if( sessionCount >= maxSessions ) {
                                return false;
                            }
                        }
                    }
                }
                Sessions.Add( session );
            }
            return true;
        }


        public static bool RegisterPlayerAndCheckIfFull( Session session ) {
            if( session == null ) throw new ArgumentNullException( "session" );

            Player player = session.Player;

            // Kick other sessions with same player name
            List<Session> sessionsToKick = new List<Session>();
            lock( SessionLock ) {
                foreach( Session s in Sessions ) {
                    if( s == session ) continue;
                    if( s.Player.Name.Equals( player.Name, StringComparison.OrdinalIgnoreCase ) ) {
                        sessionsToKick.Add( s );
                        s.Kick( "Connected from elsewhere!", LeaveReason.ClientReconnect );
                        Logger.Log( "Session.LoginSequence: Player {0} logged in. Ghost was kicked.", LogType.SuspiciousActivity,
                                    s.Player.Name );
                    }
                }
            }

            // Wait for other sessions to exit/unregister (if any)
            foreach( Session ses in sessionsToKick ) {
                ses.WaitForDisconnect();
            }

            // Add player to the list
            lock( PlayerListLock ) {
                if( Players.Count >= ConfigKey.MaxPlayers.GetInt() && !player.Info.Rank.ReservedSlot ) {
                    return false;
                }
                Players.Add( player.Name, player );
                UpdatePlayerList();
                RaiseEvent( PlayerListChanged );
                session.IsRegistered = true;
            }
            return true;
        }


        public static string MakePlayerConnectedMessage( Player player, bool firstTime, World world ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( world == null ) throw new ArgumentNullException( "world" );
            if( firstTime ) {
                return String.Format( "&S{0} ({1}&S) connected, joined {2}",
                                      player.Name,
                                      player.Info.Rank.GetClassyName(),
                                      world.GetClassyName() );
            } else {
                return String.Format( "&S{0} ({1}&S) connected again, joined {2}",
                                      player.Name,
                                      player.Info.Rank.GetClassyName(),
                                      world.GetClassyName() );
            }
        }

        // Remove player from the list, and notify remaining players
        public static void UnregisterPlayer( Session session ) {
            if( session == null ) throw new ArgumentNullException( "session" );

            Player player = session.Player;
            lock( PlayerListLock ) {
                if( !session.IsRegistered ) return;
                player.Info.ProcessLogout( session );

                Logger.Log( "{0} left the server.", LogType.UserActivity,
                            player.Name );
                if( session.IsReady && ConfigKey.ShowConnectionMessages.GetBool() ) {
                    SendToSeeing( String.Format( "&SPlayer {0}&S left the server.", player.GetClassyName() ), player );
                }

                if( player.World != null ) {
                    player.World.ReleasePlayer( player );
                }
                Players.Remove( player.Name );
                UpdatePlayerList();
                RaiseEvent( PlayerListChanged );
            }
        }


        internal static void UnregisterSession( Session session ) {
            if( session == null ) throw new ArgumentNullException( "session" );
            lock( SessionLock ) {
                if( Sessions.Contains( session ) ) {
                    Sessions.Remove( session );
                }
            }
        }


        public static void UpdatePlayerList() {
            lock( PlayerListLock ) {
                Player[] newPlayerList = new Player[Players.Count];
                int i = 0;
                foreach( Player player in Players.Values ) {
                    newPlayerList[i++] = player;
                }
                PlayerList = newPlayerList.OrderBy( player => player.Name ).ToArray();
            }
        }


        /// <summary>Finds a player by name, using autocompletion.
        /// Count ALL players, including hidden ones.</summary>
        /// <returns>An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches.</returns>
        public static Player[] FindPlayers( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Player[] tempList = PlayerList;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] == null ) continue;
                if( tempList[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Clear();
                    results.Add( tempList[i] );
                    break;
                } else if( tempList[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Add( tempList[i] );
                }
            }
            return results.ToArray();
        }


        /// <summary>Finds a player by name, using autocompletion.
        /// Does not count hidden players.</summary>
        /// <param name="player">Player who initiated the search.
        /// Used to determine whether others are hidden or not.</param>
        /// <param name="name">Full or partial name of the search target.</param>
        /// <returns>An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches.</returns>
        public static Player[] FindPlayers( Player player, string name ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( name == null ) throw new ArgumentNullException( "name" );
            List<Player> results = new List<Player>();
            Player[] tempList = PlayerList;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] != null && player.CanSee( tempList[i] ) ) {
                    if( tempList[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( tempList[i] );
                        break;
                    } else if( tempList[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Add( tempList[i] );
                    }
                }
            }
            return results.ToArray();
        }


        /// <summary>Find player by name using autocompletion (returns only whose whom player can see)
        /// Returns null and prints message if none or multiple players matched.</summary>
        /// <param name="player">Player who initiated the search. This is where messages are sent.</param>
        /// <param name="name">Full or partial name of the search target.</param>
        /// <param name="includeHidden">Whether to include hidden players in the search.</param>
        /// <returns>Player object, or null if no player was found.</returns>
        public static Player FindPlayerOrPrintMatches( Player player, string name, bool includeHidden ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( name == null ) throw new ArgumentNullException( "name" );
            Player[] matches;
            if( includeHidden ) {
                matches = FindPlayers( name );
            } else {
                matches = FindPlayers( player, name );
            }

            if( matches.Length == 0 ) {
                player.NoPlayerMessage( name );
                return null;

            } else if( matches.Length > 1 ) {
                player.ManyMatchesMessage( "player", matches );
                return null;

            } else {
                return matches[0];
            }
        }


        /// <summary>Finds any player(s) online from given IP address.</summary>
        /// <returns>An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches.</returns>
        public static Player[] FindPlayers( IPAddress ip ) {
            if( ip == null ) throw new ArgumentNullException( "ip" );
            return PlayerList.Where( t => t != null &&
                                          t.Session.IP.Equals( ip ) ).ToArray();
        }


        /// <summary>Finds a player by name, without any kind of autocompletion.</summary>
        /// <param name="name">Name of the player (case-insensitive).</param>
        /// <returns>Player object, or null if player was not found.</returns>
        public static Player FindPlayerExact( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            return PlayerList.FirstOrDefault( t => t != null &&
                                                   t.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        /// <summary> Finds Player object associated with the given PlayerInfo object.</summary>
        /// <returns>Player object, or null if player is offline.</returns>
        public static Player FindPlayerExact( PlayerInfo info ) {
            if( info == null || !info.Online ) {
                return null;
            } else {
                return FindPlayerExact( info.Name );
            }
        }


        public static int CountPlayers( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return PlayerList.Length;
            } else {
                return PlayerList.Count( player => !player.IsHidden );
            }
        }

        public static int CountVisiblePlayers( Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return PlayerList.Count( observer.CanSee );
        }

        #endregion
    }


    /// <summary> Describes the circumstances of server shutdown. </summary>
    public sealed class ShutdownParams {
        public ShutdownParams( ShutdownReason reason, int delay, bool killProcess, bool restart ) {
            Reason = reason;
            Delay = delay;
            KillProcess = killProcess;
            Restart = restart;
        }

        public ShutdownParams( ShutdownReason reason, int delay, bool killProcess,
                               bool restart, string customReason, Player initiatedBy ) :
            this( reason, delay, killProcess, restart ) {
            customReasonString = customReason;
            InitiatedBy = initiatedBy;
        }

        public ShutdownReason Reason { get; private set; }

        readonly string customReasonString;
        public string ReasonString {
            get {
                return customReasonString ?? Reason.ToString();
            }
        }
        public int Delay { get; private set; }

        public bool KillProcess { get; private set; }

        public bool Restart { get; private set; }

        public Player InitiatedBy { get; private set; }
    }


    /// <summary> Categorizes conditions that lead to server shutdowns.
    /// Use "Other" for plugin-triggered shutdowns. </summary>
    public enum ShutdownReason {
        Unknown,
        Other,

        FailedToInitialize,
        FailedToStart,
        Restarting,
        Crashed,
        ShuttingDown,
        ProcessClosing
    }


    /// <summary> Enumerates the recognized command-line switches/arguments.
    /// Args are parsed in Server.InitLibrary </summary>
    public enum ArgKey {
        /// <summary> Working path (directory) that fCraft should use. </summary>
        Path,

        /// <summary> Path (directory) where the log files should be placed. </summary>
        LogPath,

        /// <summary> Path (directory) where the map files should be loaded from/saved to. </summary>
        MapPath,

        /// <summary> Path (file) of the configuration file. </summary>
        Config,

        /// <summary> If NoRestart flag is present, fCraft will shutdown instead of restarting.
        /// This flag is used by AutoLauncher. </summary>
        NoRestart,

        /// <summary> If ExitOnCrash flag is present, fCraft will exit
        /// at once in the event of an unrecoverable crash, instead of showing a message. </summary>
        ExitOnCrash,


        /// <summary> Disables all logging. </summary>
        NoLog
    };
}