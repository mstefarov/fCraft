// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using fCraft.Drawing;
using fCraft.Events;
using fCraft.MapGeneration;
using JetBrains.Annotations;
using ThreadState = System.Threading.ThreadState;

namespace fCraft {
    /// <summary> Core of an fCraft server. Manages startup/shutdown, online player
    /// sessions, global events and scheduled tasks. </summary>
    public static partial class Server {
        /// <summary> Time when the server started (UTC). Used to check uptime. </summary>
        public static DateTime StartTime { get; private set; }

        /// <summary> Internal IP address that the server's bound to (0.0.0.0 if not explicitly specified by the user). </summary>
        [NotNull]
        public static IPAddress InternalIP { get; private set; }

        /// <summary> External IP address of this machine, as reported by checkip.dyndns.org
        /// Same as InternalIP, if dyndns check was not carried out or has failed. </summary>
        [NotNull]
        public static IPAddress ExternalIP { get; private set; }

        /// <summary> Number of the local listening port. </summary>
        public static int Port { get; private set; }


        internal static int MaxUploadSpeed, // set by Config.ApplyConfig
                            BlockUpdateThrottling; // used when there are no players in a world
        internal const int MaxSessionPacketsPerTick = 128, // used when there are no players in a world
                           MaxBlockUpdatesPerTick = 100000; // used when there are no players in a world
        internal static float TicksPerSecond;

        static TcpListener listener;


        #region Command-line args

        static readonly Dictionary<ArgKey, string> Args = new Dictionary<ArgKey, string>();


        /// <summary> Returns value of a given command-line argument (if present). Use HasArg to check flag arguments. </summary>
        /// <param name="key"> Command-line argument name (enumerated) </param>
        /// <returns> Value of the command-line argument, or null if this argument was not set or argument is a flag. </returns>
        [CanBeNull]
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


        /// <summary> Produces a string containing all recognized arguments that were set/passed to this instance of fCraft. </summary>
        /// <returns> A string containing all given arguments, or an empty string if none were set. </returns>
        [NotNull]
        public static string GetArgString() {
            return String.Join( " ", GetArgList() );
        }


        /// <summary> Produces a list of arguments that were passed to this instance of fCraft. </summary>
        /// <returns> An array of strings, formatted as --key="value" (or, for flag arguments, --key).
        /// Returns an empty string array if no arguments were set. </returns>
        [NotNull]
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


        #region Initialization and Startup

        // flags used to ensure proper initialization order
        static bool libraryInitialized,
                    serverInitialized;

        /// <summary> Whether the server is currently running (true between Started and ShutdownEnded events). </summary>
        public static bool IsRunning { get; private set; }


        static Server() {
            InternalIP = IPAddress.Any;
            ExternalIP = IPAddress.Any;
            Players = new Player[0];
        }


        /// <summary> Reads command-line switches and sets up paths and logging.
        /// This should be called before any other library function.
        /// Note to frontend devs: Subscribe to log-related events before calling this.
        /// Does not raise any events besides Logger.Logged.
        /// Throws exceptions on failure. </summary>
        /// <param name="rawArgs"> string arguments passed to the frontend (if any). </param>
        /// <exception cref="System.InvalidOperationException"> Library is already initialized. </exception>
        /// <exception cref="System.IO.IOException"> Working path, log path, or map path cannot be set. </exception>
        public static void InitLibrary( [NotNull] IEnumerable<string> rawArgs ) {
            if( rawArgs == null ) throw new ArgumentNullException( "rawArgs" );
            if( libraryInitialized ) {
                throw new InvalidOperationException( "fCraft library is already initialized" );
            }

            ServicePointManager.Expect100Continue = false;

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
                    ArgKey key;
                    if( EnumUtil.TryParse( argKeyName, out key, true ) ) {
                        Args.Add( key, argValue );
                    } else {
                        Console.Error.WriteLine( "Unknown argument: {0}", arg );
                    }
                } else {
                    Console.Error.WriteLine( "Unknown argument: {0}", arg );
                }
            }

            // before we do anything, set path to the default location
            Directory.SetCurrentDirectory( Paths.WorkingPath );

            // set custom working path (if specified)
            string path = GetArg( ArgKey.Path );
            if( path != null && Paths.TestDirectory( "WorkingPath", path, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( path );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else if( Paths.TestDirectory( "WorkingPath", Paths.WorkingPathDefault, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( Paths.WorkingPathDefault );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else {
                throw new IOException( "Could not set the working path." );
            }

            // set log path
            string logPath = GetArg( ArgKey.LogPath );
            if( logPath != null && Paths.TestDirectory( "LogPath", logPath, true ) ) {
                Paths.LogPath = Path.GetFullPath( logPath );
            } else if( Paths.TestDirectory( "LogPath", Paths.LogPathDefault, true ) ) {
                Paths.LogPath = Path.GetFullPath( Paths.LogPathDefault );
            } else {
                throw new IOException( "Could not set the log path." );
            }

            if( HasArg( ArgKey.NoLog ) ) {
                Logger.Enabled = false;
            } else {
                Logger.MarkLogStart();
            }

            // set map path
            string mapPath = GetArg( ArgKey.MapPath );
            if( mapPath != null && Paths.TestDirectory( "MapPath", mapPath, true ) ) {
                Paths.MapPath = Path.GetFullPath( mapPath );
                Paths.IgnoreMapPathConfigKey = true;
            } else if( Paths.TestDirectory( "MapPath", Paths.MapPathDefault, true ) ) {
                Paths.MapPath = Path.GetFullPath( Paths.MapPathDefault );
            } else {
                throw new IOException( "Could not set the map path." );
            }

            // set config path
            Paths.ConfigFileName = Paths.ConfigFileNameDefault;
            string configFile = GetArg( ArgKey.Config );
            if( configFile != null ) {
                if( Paths.TestFile( "config.xml", configFile, false, FileAccess.Read ) ) {
                    Paths.ConfigFileName = new FileInfo( configFile ).FullName;
                }
            }

            if( MonoCompat.IsMono ) {
                Logger.Log( LogType.Debug, "Running on Mono {0}", MonoCompat.MonoVersion );
            }

#if DEBUG_EVENTS
            Logger.PrepareEventTracing();
#endif

            Logger.Log( LogType.Debug, "Working directory: {0}", Directory.GetCurrentDirectory() );
            Logger.Log( LogType.Debug, "Log path: {0}", Path.GetFullPath( Paths.LogPath ) );
            Logger.Log( LogType.Debug, "Map path: {0}", Path.GetFullPath( Paths.MapPath ) );
            Logger.Log( LogType.Debug, "Config path: {0}", Path.GetFullPath( Paths.ConfigFileName ) );

            libraryInitialized = true;
        }


        /// <summary> Initialized various server subsystems. This should be called after InitLibrary and before StartServer.
        /// Loads config, PlayerDB, IP bans, builds a list of commands, and prepares the IRC bot.
        /// Raises Server.Initializing and Server.Initialized events, and possibly Logger.Logged events.
        /// Throws exceptions on failure. </summary>
        /// <exception cref="System.InvalidOperationException"> Library is not initialized, or server is already initialized. </exception>
        public static void InitServer() {
            if( serverInitialized ) {
                throw new InvalidOperationException( "Server is already initialized" );
            }
            if( !libraryInitialized ) {
                throw new InvalidOperationException( "Server.InitLibrary must be called before Server.InitServer" );
            }
            RaiseEvent( Initializing );

            // Instantiate DeflateStream to make sure that libMonoPosix is present.
            // This allows catching misconfigured Mono installs early, and stopping the server.
            using( var testMemStream = new MemoryStream() ) {
                using( new DeflateStream( testMemStream, CompressionMode.Compress ) ) {}
            }

            // warnings/disclaimers
            if( Updater.CurrentRelease.IsFlagged( ReleaseFlags.Dev ) ) {
                Logger.Log( LogType.Warning,
                            "You are using an unreleased developer version of fCraft. " +
                            "Do not use this version unless you are ready to deal with bugs and potential data loss. " +
                            "Consider using the latest stable version instead, available from www.fCraft.net" );
            }

            if( Updater.CurrentRelease.IsFlagged( ReleaseFlags.Unstable ) ) {
                const string unstableMessage = "This build has been marked as UNSTABLE. " +
                                               "Do not use except for debugging purposes. " +
                                               "Latest non-broken build is " + Updater.LatestStable;
#if DEBUG
                Logger.Log( LogType.Warning, unstableMessage );
#else
                throw new Exception( unstableMessage );
#endif
            }

#if DEBUG
            Config.RunSelfTest();
#else
            // delete the old updater, if exists
            File.Delete( Paths.UpdateInstallerFileName );
#endif

            // try to load the config
            Config.Load( false, false );

            if( ConfigKey.VerifyNames.GetEnum<NameVerificationMode>() == NameVerificationMode.Never ) {
                Logger.Log( LogType.Warning,
                            "Name verification is currently OFF. Your server is at risk of being hacked. " +
                            "Enable name verification as soon as possible." );
            }

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            // prepare the list of commands
            CommandManager.Init();

            // prepare the brushes
            BrushManager.Init();

            // prepare list of map generators
            MapGenUtil.Init();

            // Init IRC
            IRC.Init();

            RaiseEvent( Initialized );

            serverInitialized = true;
        }


        /// <summary> Starts the server:
        /// Creates Console pseudo-player, loads the world list, starts listening for incoming connections,
        /// sets up scheduled tasks and starts the scheduler, starts the heartbeat, and connects to IRC.
        /// Raises Server.Starting and Server.Started events.
        /// May throw an exception on hard failure. </summary>
        /// <returns> True if server started normally, false on soft failure. </returns>
        /// <exception cref="System.InvalidOperationException"> Server is already running, or server/library have not been initialized. </exception>
        public static bool StartServer() {
            if( IsRunning ) {
                throw new InvalidOperationException( "Server is already running" );
            }
            if( !libraryInitialized || !serverInitialized ) {
                throw new InvalidOperationException(
                    "Server.InitLibrary and Server.InitServer must be called before Server.StartServer" );
            }

            StartTime = DateTime.UtcNow;
            cpuUsageStartingOffset = Process.GetCurrentProcess().TotalProcessorTime;

            RaiseEvent( Starting );

            if( ConfigKey.BackupDataOnStartup.Enabled() ) {
                BackupData();
            }

            Player.Console = new Player( ConfigKey.ConsoleName.GetString() );

            // Back up server data (PlayerDB, worlds, bans, config)
            if( ConfigKey.BlockDBEnabled.Enabled() ) {
                BlockDB.Init();
            }

            // Load the world list
            if( !WorldManager.LoadWorldList() ) return false;
            WorldManager.SaveWorldList();

            // Back up all worlds (if needed)
            if( ConfigKey.BackupOnStartup.Enabled() ) {
                foreach( World world in WorldManager.Worlds ) {
                    string backupFileName = String.Format( World.TimedBackupFormat,
                                                           world.Name, DateTime.Now ); // localized
                    world.SaveBackup( Path.Combine( Paths.BackupPath, backupFileName ) );
                }
            }

            // open the port
            Port = ConfigKey.Port.GetInt();
            InternalIP = IPAddress.Parse( ConfigKey.IP.GetString() );

            try {
                listener = new TcpListener( InternalIP, Port );
                listener.Start();

            } catch( Exception ex ) {
                // if the port is unavailable
                Logger.Log( LogType.Error,
                            "Could not start listening on port {0}, stopping. ({1})",
                            Port, ex.Message );
                if( !ConfigKey.IP.IsDefault() ) {
                    Logger.Log( LogType.Warning,
                                "Do not use the \"Designated IP\" setting unless you have multiple NICs or IPs." );
                }
                return false;
            }

            // Resolve internal and external IP addresses
            InternalIP = ( (IPEndPoint)listener.LocalEndpoint ).Address;
            IPAddress foundExternalIP = null;
            for( int i = 0; i < 3 && foundExternalIP == null; i++ ) {
                Logger.Log( LogType.SystemActivity, "Resolving external IP address... (try {0})", i + 1 );
                foundExternalIP = CheckExternalIP();
            }

            if( foundExternalIP != null ) {
                ExternalIP = foundExternalIP;
                Logger.Log( LogType.SystemActivity,
                            "Server.StartServer: now accepting connections at {0}:{1}",
                            ExternalIP, Port );
            } else {
                Logger.Log( LogType.SystemActivity,
                            "Server.StartServer: External IP could not be looked up. Now accepting connections on port {0}",
                            Port );
            }

            // list loaded worlds
            WorldManager.UpdateWorldList();
            Logger.Log( LogType.SystemActivity,
                        "All available worlds: {0}",
                        WorldManager.Worlds.JoinToString( ", ", w => w.Name ) );

            Logger.Log( LogType.SystemActivity,
                        "Main world: {0}; default rank: {1}",
                        WorldManager.MainWorld.Name, RankManager.DefaultRank.Name );

            // Check for incoming connections (every 250ms)
            Scheduler.NewTask( CheckConnections ).RunForever( CheckConnectionsInterval );

            // Check for idles (every 30s)
            Scheduler.NewTask( CheckIdles ).RunForever( CheckIdlesInterval );

            // Monitor CPU usage (every 30s)
            try {
                MonitorProcessorUsage( null );
                Scheduler.NewTask( MonitorProcessorUsage ).RunForever( MonitorProcessorUsageInterval,
                                                                       MonitorProcessorUsageInterval );
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "Server.StartServer: Could not start monitoring CPU use: {0}", ex );
            }

            // PlayerDB saving (every 90s)
            PlayerDB.StartSaveTask();

            // Announcements
            if( ConfigKey.AnnouncementInterval.GetInt() > 0 ) {
                TimeSpan announcementInterval = TimeSpan.FromMinutes( ConfigKey.AnnouncementInterval.GetInt() );
                Scheduler.NewTask( ShowRandomAnnouncement )
                         .RunForever( announcementInterval );
            }

            // garbage collection (every 60s)
            Scheduler.NewTask( DoGC ).RunForever( GCInterval, TimeSpan.FromSeconds( 45 ) );

            Heartbeat.Start();

            if( ConfigKey.IRCBotEnabled.Enabled() ) {
                IRC.Start();
            }

            if( ConfigKey.RestartInterval.GetInt() > 0 ) {
                // schedule automatic restart
                TimeSpan restartIn = TimeSpan.FromSeconds( ConfigKey.RestartInterval.GetInt() );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.RestartTimer, restartIn, true,
                                                        "Automatic Server Restart",
                                                        Player.Console );
                Shutdown( sp, false );
            }

            // start the main loop - server is now connectible
            Scheduler.Start();
            IsRunning = true;

            RaiseEvent( Started );
            return true;
        }

        #endregion


        #region Shutdown

        /// <summary> Whether the server is currently being shut down. </summary>
        public static volatile bool IsShuttingDown;

        static readonly object ShutdownLock = new object();
        static readonly AutoResetEvent ShutdownWaiter = new AutoResetEvent( false );
        static Thread shutdownThread;
        static ChatTimer shutdownTimer;


        static void ShutdownNow( [NotNull] ShutdownParams shutdownParams ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            if( IsShuttingDown ) return; // to avoid starting shutdown twice
            IsShuttingDown = true;
#if !DEBUG
            try {
#endif
                RaiseShutdownBeganEvent( shutdownParams );

                Scheduler.BeginShutdown();

                Logger.Log( LogType.SystemActivity,
                            "Server shutting down ({0})",
                            shutdownParams.ReasonString );

                // stop accepting new players
                if( listener != null ) {
                    listener.Stop();
                    listener = null;
                }

                // kill IRC bot
                string quitMsg = ConfigKey.ServerName.GetString() + " - " + shutdownParams.ReasonString;
                IRC.Disconnect( Color.MinecraftToIrcColors( quitMsg ) );

                // kick all players
                Player[] kickedPlayers = null;
                lock( PlayerListLock ) {
                    if( PlayerIndex.Count > 0 ) {
                        Logger.Log( LogType.SystemActivity, "Shutdown: Kicking players..." );
                        foreach( Player p in PlayerIndex ) {
                            // NOTE: kick packet delivery here is not currently guaranteed
                            p.Kick( "Server shutting down (" + shutdownParams.ReasonString + Color.White + ")",
                                    LeaveReason.ServerShutdown );
                        }
                        kickedPlayers = PlayerIndex.ToArray();
                    }
                }

                if( kickedPlayers != null ) {
                    Logger.Log( LogType.SystemActivity, "Shutdown: Waiting for players to disconnect..." );
                    foreach( Player p in kickedPlayers ) {
                        p.WaitForDisconnect();
                    }
                }

                if( WorldManager.Worlds.Length > 0 ) {
                    Logger.Log( LogType.SystemActivity, "Shutdown: Saving worlds..." );
                    lock( WorldManager.SyncRoot ) {
                        // unload all worlds (includes saving)
                        foreach( World world in WorldManager.Worlds ) {
                            if( BlockDB.IsEnabledGlobally && world.BlockDB.IsEnabled ) {
                                world.BlockDB.Flush( false );
                            }
                            world.SaveMap();
                        }
                    }
                }

                if( Scheduler.CriticalTaskCount > 0 ) {
                    Logger.Log( LogType.SystemActivity,
                                "Shutdown: Waiting for {0} background tasks to finish...",
                                Scheduler.CriticalTaskCount );
                }
                Scheduler.EndShutdown();

                if( IsRunning ) {
                    Logger.Log( LogType.SystemActivity, "Shutdown: Saving databases..." );
                    if( PlayerDB.IsLoaded ) PlayerDB.Save();
                    if( IPBanList.IsLoaded ) IPBanList.Save();
                }
                IsRunning = false;

                Environment.ExitCode = (int)shutdownParams.Reason;

                Logger.Log( LogType.SystemActivity, "Shutdown: Complete" );
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error in Server.Shutdown", "fCraft", ex, true );
            }
#endif
        }


        /// <summary> Initiates the server shutdown with given parameters. </summary>
        /// <param name="shutdownParams"> Shutdown parameters </param>
        /// <param name="waitForShutdown"> If true, blocks the calling thread until shutdown is complete or cancelled. </param>
        public static void Shutdown( [NotNull] ShutdownParams shutdownParams, bool waitForShutdown ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            lock( ShutdownLock ) {
                if( !CancelShutdown() ) return;
                shutdownThread = new Thread( ShutdownThread ) {
                    Name = "fCraft.Shutdown",
                    CurrentCulture = new CultureInfo( "en-US" )
                };
                if( shutdownParams.Delay >= ChatTimer.MinDuration ) {
                    string timerMsg = String.Format( "Server {0} ({1})",
                                                     shutdownParams.Restart ? "restart" : "shutdown",
                                                     shutdownParams.ReasonString );
                    string nameOnTimer;
                    if( shutdownParams.InitiatedBy == null ) {
                        nameOnTimer = Player.Console.Name;
                    } else {
                        nameOnTimer = shutdownParams.InitiatedBy.Name;
                    }
                    shutdownTimer = ChatTimer.Start( shutdownParams.Delay, timerMsg, nameOnTimer );
                }
                shutdownThread.Start( shutdownParams );
            }
            if( waitForShutdown ) {
                ShutdownWaiter.WaitOne();
            }
        }


        /// <summary> Attempts to cancel the shutdown timer. </summary>
        /// <returns> True if a shutdown timer was cancelled, false if no shutdown is in progress.
        /// Also returns false if it's too late to cancel (shutdown has begun). </returns>
        public static bool CancelShutdown() {
            lock( ShutdownLock ) {
                if( shutdownThread != null ) {
                    if( IsShuttingDown || shutdownThread.ThreadState != ThreadState.WaitSleepJoin ) {
                        return false;
                    }
                    if( shutdownTimer != null ) {
                        shutdownTimer.Abort();
                        shutdownTimer = null;
                    }
                    ShutdownWaiter.Set();
                    shutdownThread.Abort();
                    shutdownThread = null;
                }
            }
            return true;
        }


        static void ShutdownThread( [NotNull] object obj ) {
            if( obj == null ) throw new ArgumentNullException( "obj" );
            ShutdownParams param = (ShutdownParams)obj;
            Thread.Sleep( param.Delay );
            ShutdownNow( param );
            ShutdownWaiter.Set();

            if( !HasArg( ArgKey.NoUpdater ) ) {
                bool doRestart = ( param.Restart && !HasArg( ArgKey.NoRestart ) );
                string assemblyExecutable = Assembly.GetEntryAssembly().Location;

                if( Updater.RunAtShutdown && doRestart ) {
                    string args = String.Format( "--restart=\"{0}\" {1}",
                                                 MonoCompat.PrependMono( assemblyExecutable ),
                                                 GetArgString() );

                    MonoCompat.StartDotNetProcess( Paths.UpdateInstallerFileName, args, true );

                } else if( Updater.RunAtShutdown ) {
                    MonoCompat.StartDotNetProcess( Paths.UpdateInstallerFileName, GetArgString(), true );

                } else if( doRestart ) {
                    MonoCompat.StartDotNetProcess( assemblyExecutable, GetArgString(), true );
                }
            }

            RaiseShutdownEndedEvent( param );
        }

        #endregion


        #region Messaging / Packet Sending

        /// <summary> Broadcasts a message to all online players.
        /// Shorthand for Server.Players.Message </summary>
        [StringFormatMethod( "message" )]
        public static void Message( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            Players.Message( message, formatArgs );
        }


        /// <summary> Broadcasts a message to all online players except one.
        /// Shorthand for Server.Players.Except(except).Message </summary>
        [StringFormatMethod( "message" )]
        public static void Message( [CanBeNull] Player except, [NotNull] string message,
                                    [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            Players.Except( except ).Message( message, formatArgs );
        }

        #endregion


        #region Scheduled Tasks

        // checks for incoming connections
        static readonly TimeSpan CheckConnectionsInterval = TimeSpan.FromMilliseconds( 250 );


        static void CheckConnections( [NotNull] SchedulerTask param ) {
            TcpListener listenerCache = listener;
            if( listenerCache != null && listenerCache.Pending() ) {
                try {
                    Player.StartSession( listenerCache.AcceptTcpClient() );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "Server.CheckConnections: Could not accept incoming connection: {0}", ex );
                }
            }
        }


        // checks for idle players
        static readonly TimeSpan CheckIdlesInterval = TimeSpan.FromSeconds( 30 );


        static void CheckIdles( [NotNull] SchedulerTask task ) {
            Player[] tempPlayerList = Players;
            for( int i = 0; i < tempPlayerList.Length; i++ ) {
                Player player = tempPlayerList[i];
                if( player.Info.Rank.IdleKickTimer <= 0 ) continue;

                if( player.IdleTime.TotalMinutes >= player.Info.Rank.IdleKickTimer ) {
                    Message( "{0}&S was kicked for being idle for {1} min",
                             player.ClassyName,
                             player.Info.Rank.IdleKickTimer );
                    string kickReason = "Idle for " + player.Info.Rank.IdleKickTimer + " minutes";
                    player.Kick( Player.Console, kickReason, LeaveReason.IdleKick, false, true, false );
                    player.ResetIdleTimer(); // to prevent kick from firing more than once
                }
            }
        }


        static readonly TimeSpan GCInterval = TimeSpan.FromSeconds( 60 );


        static void DoGC( [NotNull] SchedulerTask task ) {
            if( !gcRequested ) return;
            gcRequested = false;

            Process thisProcess = Process.GetCurrentProcess();
            thisProcess.Refresh();
            long usageBefore = thisProcess.PrivateMemorySize64/( 1024*1024 );

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );

            thisProcess.Refresh();
            long usageAfter = thisProcess.PrivateMemorySize64/( 1024*1024 );

            Logger.Log( LogType.Debug,
                        "Server.DoGC: Collected on schedule ({0}->{1} MB).",
                        usageBefore,
                        usageAfter );
        }


        // shows announcements
        static void ShowRandomAnnouncement( [NotNull] SchedulerTask task ) {
            if( !File.Exists( Paths.AnnouncementsFileName ) ) return;
            string[] lines = File.ReadAllLines( Paths.AnnouncementsFileName );
            if( lines.Length == 0 ) return;
            string line = lines[new Random().Next( 0, lines.Length )].Trim();
            if( line.Length == 0 ) return;
            foreach( Player player in Players.Where( player => player.World != null ) ) {
                string announcementLine = Chat.ReplaceTextKeywords( player, line );
                announcementLine = Chat.ReplaceEmoteKeywords( announcementLine );
                announcementLine = Chat.ReplaceUnicodeWithEmotes( announcementLine );
                player.Message( "&R" + announcementLine );
            }
        }


        // measures CPU usage
        public static bool IsMonitoringCpuUsage { get; private set; }
        static TimeSpan cpuUsageStartingOffset;
        public static double CpuUsageTotal { get; private set; }
        public static double CpuUsageLastMinute { get; private set; }

        static TimeSpan oldCpuTime = new TimeSpan( 0 );
        static readonly TimeSpan MonitorProcessorUsageInterval = TimeSpan.FromSeconds( 30 );
        static DateTime lastMonitorTime = DateTime.UtcNow;


        static void MonitorProcessorUsage( [NotNull] SchedulerTask task ) {
            TimeSpan newCpuTime = Process.GetCurrentProcess().TotalProcessorTime - cpuUsageStartingOffset;
            CpuUsageLastMinute = ( newCpuTime - oldCpuTime ).TotalSeconds /
                                 ( Environment.ProcessorCount * DateTime.UtcNow.Subtract( lastMonitorTime ).TotalSeconds );
            lastMonitorTime = DateTime.UtcNow;
            CpuUsageTotal = newCpuTime.TotalSeconds /
                            ( Environment.ProcessorCount * DateTime.UtcNow.Subtract( StartTime ).TotalSeconds );
            oldCpuTime = newCpuTime;
            IsMonitoringCpuUsage = true;
        }

        #endregion


        #region Utilities

        static bool gcRequested;


        /// <summary> Informs the server that garbage collection should be performed.
        /// Actual collection is done on the background task thread, asynchronously, as needed. </summary>
        public static void RequestGC() {
            gcRequested = true;
        }


        internal static bool VerifyName( [NotNull] string name, [NotNull] string hash, [NotNull] string salt ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( hash == null ) throw new ArgumentNullException( "hash" );
            if( salt == null ) throw new ArgumentNullException( "salt" );
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


        internal static int CalculateMaxPacketsPerUpdate( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            int packetsPerTick = (int)( BlockUpdateThrottling / TicksPerSecond );
            int maxPacketsPerUpdate = (int)( MaxUploadSpeed / TicksPerSecond * 128 );

            int playerCount = world.Players.Length;
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


        public static void BackupData() {
            if( !Paths.TestDirectory( "DataBackup", Paths.DataBackupDirectory, true ) ) {
                Logger.Log( LogType.Error, "Unable to create a data backup." );
                return;
            }
            string backupFileName = String.Format( Paths.DataBackupFileNameFormat, DateTime.Now ); // localized
            backupFileName = Path.Combine( Paths.DataBackupDirectory, backupFileName );
            using( FileStream fs = File.Create( backupFileName ) ) {
                string fileComment = String.Format( "Backup of fCraft data for server \"{0}\", saved on {1}",
                                                    ConfigKey.ServerName.GetString(),
                                                    DateTime.Now );
                using( ZipStorer backupZip = ZipStorer.Create( fs, fileComment ) ) {
                    foreach( string dataFileName in Paths.DataFilesToBackup ) {
                        if( File.Exists( dataFileName ) ) {
                            backupZip.AddFile( ZipStorer.Compression.Deflate,
                                               dataFileName,
                                               dataFileName,
                                               "" );
                        }
                    }
                }
            }
            Logger.Log( LogType.SystemActivity,
                        "Backed up server data to \"{0}\"",
                        backupFileName );
        }


        /// <summary> Returns a cryptographically secure random string of given length. </summary>
        [NotNull]
        public static string GetRandomString( int chars ) {
            RandomNumberGenerator prng = RandomNumberGenerator.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while( sb.Length < chars ) {
                prng.GetBytes( oneChar );
                if( oneChar[0] >= 48 && oneChar[0] <= 57 ||
                    oneChar[0] >= 65 && oneChar[0] <= 90 ||
                    oneChar[0] >= 97 && oneChar[0] <= 122 ) {
                    sb.Append( (char)oneChar[0] );
                }
            }
            return sb.ToString();
        }


        static readonly Uri IPCheckUri = new Uri( "http://checkip.dyndns.org/" );
        const int IPCheckTimeout = 30000;


        /// <summary> Checks server's external IP, as reported by checkip.dyndns.org. </summary>
        [CanBeNull]
        static IPAddress CheckExternalIP() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( IPCheckUri );
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.BypassCache );
            request.ReadWriteTimeout = IPCheckTimeout;
            request.ServicePoint.BindIPEndPointDelegate = BindIPEndPointCallback;
            request.Timeout = IPCheckTimeout;
            request.UserAgent = Updater.UserAgent;

            try {
                using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() ) {
                    if( response.StatusCode != HttpStatusCode.OK ) {
                        Logger.Log( LogType.Warning,
                                    "Could not check external IP: {0}", response.StatusDescription );
                        return null;
                    }
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        string responseString = responseReader.ReadToEnd();
                        int startIndex = responseString.IndexOf( ':' ) + 2;
                        int endIndex = responseString.IndexOf( '<', startIndex ) - startIndex;
                        IPAddress result;
                        if( IPAddress.TryParse( responseString.Substring( startIndex, endIndex ), out result ) ) {
                            return result;
                        } else {
                            return null;
                        }
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( LogType.Warning,
                            "Could not check external IP: {0}", ex );
                return null;
            }
        }


        // Callback for setting the local IP binding. Implements System.Net.BindIPEndPoint delegate.
        public static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint,
                                                         int retryCount ) {
            return new IPEndPoint( InternalIP, 0 );
        }


        internal static readonly RequestCachePolicy CachePolicy = new RequestCachePolicy( RequestCacheLevel.BypassCache );

        #endregion


        #region Player and Session Management

        /// <summary> List of online players.
        /// This property is volatile and can change when players connect/disconnect,
        /// so cache a reference if you need to refer to the same snapshot of the
        /// player list more than once. </summary>
        [NotNull]
        public static Player[] Players { get; private set; }

        // list of all player sessions currently registered with the server
        static readonly List<Player> PlayerIndex = new List<Player>();

        // lock shared by RegisterPlayer/UnregisterPlayer/UpdatePlayerList
        static readonly object PlayerListLock = new object();


        // Registers a player and checks if the server is full.
        // Also kicks any existing connections for this player account.
        // Returns true if player was registered successfully.
        // Returns false if the server was full.
        internal static bool RegisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( PlayerListLock ) {
                // Kick other sessions with same player name
                Player ghost = PlayerIndex.FirstOrDefault( p => p.Name.Equals( player.Name,
                                                                               StringComparison.OrdinalIgnoreCase ) );
                if( ghost != null ) {
                    // Wait for other session to exit/unregister
                    Logger.Log( LogType.SuspiciousActivity,
                                "Server.RegisterPlayer: Player {0} logged in twice. Ghost from {1} was kicked.",
                                ghost.Name, ghost.IP );
                    ghost.KickSynchronously( "Connected from elsewhere!", LeaveReason.ClientReconnect );
                }

                int maxSessions = ConfigKey.MaxConnectionsPerIP.GetInt();
                // check the number of connections from this IP.
                if( !player.IP.Equals( IPAddress.Loopback ) && maxSessions > 0 ) {
                    int connections = PlayerIndex.Count( p => p.IP.Equals( player.IP ) );
                    if( connections >= maxSessions ) {
                        Logger.Log( LogType.SuspiciousActivity,
                                    "Player.LoginSequence: Denied player {0}: maximum number of connections was reached for {1}",
                                    player.Name, player.IP );
                        player.Kick( "Max connections reached for " + player.IP, LeaveReason.LoginFailed );
                        return false;
                    }
                }

                // check if server is full
                if( PlayerIndex.Count >= ConfigKey.MaxPlayers.GetInt() && !player.Info.Rank.HasReservedSlot ) {
                    player.Kick( "Server is full!", LeaveReason.ServerFull );
                    return false;
                }
                PlayerIndex.Add( player );
                player.HasRegistered = true;
                return true;
            }
        }


        public static string MakePlayerConnectedMessage( [NotNull] Player player, bool firstTime, [NotNull] World world ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( world == null ) throw new ArgumentNullException( "world" );
            if( firstTime ) {
                return String.Format( "&SPlayer {0}&S connected, joined {1}",
                                      player.ClassyName,
                                      world.ClassyName );
            } else {
                return String.Format( "&SPlayer {0}&S connected again, joined {1}",
                                      player.ClassyName,
                                      world.ClassyName );
            }
        }


        public static string MakePlayerDisconnectedMessage( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            return String.Format( "&SPlayer {0}&S left the server.",
                                  player.ClassyName );
        }


        // Removes player from the list, and announced them leaving
        internal static void UnregisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( PlayerListLock ) {
                if( !player.HasRegistered ) {
                    return;
                }
                player.Info.ProcessLogout( player );

                Logger.Log( LogType.UserActivity,
                            "{0} left the server ({1}).", player.Name, player.LeaveReason );
                if( player.HasFullyConnected && ConfigKey.ShowConnectionMessages.Enabled() ) {
                    Players.CanSee( player )
                           .Message( MakePlayerDisconnectedMessage( player ) );
                }

                if( player.World != null ) {
                    player.World.ReleasePlayer( player );
                }
                PlayerIndex.Remove( player );
                UpdatePlayerList();
            }
        }


        internal static void UpdatePlayerList() {
            lock( PlayerListLock ) {
                Players = PlayerIndex.Where( p => p.IsOnline )
                                     .OrderBy( player => player.Name )
                                     .ToArray();
                RaiseEvent( PlayerListChanged );
            }
        }


        /// <summary> Finds a player by name, using autocompletion.
        /// Returns ALL matching players, including hidden ones. </summary>
        /// <param name="namePart"> Full or partial player name. </param>
        /// <param name="options"> Search options (recognizes SuppressEvent). </param>
        /// <returns> An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches. </returns>
        /// <exception cref="ArgumentNullException"></exception>
        [NotNull]
        public static Player[] FindPlayers( [NotNull] string namePart, SearchOptions options) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            bool suppressEvent = ( options & SearchOptions.SuppressEvent ) != 0;
            Player[] tempList = Players;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] == null ) continue;
                if( tempList[i].Name.Equals( namePart, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Clear();
                    results.Add( tempList[i] );
                    break;
                } else if( tempList[i].Name.StartsWith( namePart, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Add( tempList[i] );
                }
            }
            if( !suppressEvent ) {
                var h = SearchingForPlayer;
                if( h != null ) {
                    var e = new SearchingForPlayerEventArgs( null, namePart, results, options );
                    h( null, e );
                }
            }
            return results.ToArray();
        }


        /// <summary> Finds a player by name, using autocompletion. Does not include hidden players. 
        /// Raises Player.SearchingForPlayer event, which may modify search results, unless SuppressEvent option is set.
        /// Does not include self unless IncludeSelf search option is set. </summary>
        /// <param name="player"> Player who initiated the search.
        /// Used to determine which hidden players to show in results. </param>
        /// <param name="name"> Full or partial name of the search target. </param>
        /// <param name="options"> Search options.
        /// All flags (IncludeHidden, IncludeSelf, SuppressEvent, and ReturnSelfIfOnlyMatch) are applicable. </param>
        /// <returns> An array of matches. Array length of 0 means "no matches";
        /// 1 means an exact match or a single partial match; over 1 means multiple matches. </returns>
        [NotNull]
        public static Player[] FindPlayers( [NotNull] Player player, [NotNull] string name, SearchOptions options ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( name == null ) throw new ArgumentNullException( "name" );

            bool includeHidden = (options & SearchOptions.IncludeHidden) != 0;
            bool includeSelf = (options & SearchOptions.IncludeSelf) != 0;
            bool suppressEvent = (options & SearchOptions.SuppressEvent) != 0;
            bool returnSelf = (options & SearchOptions.ReturnSelfIfOnlyMatch) != 0;

            // Repeat last-used player name
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    return new Player[0];
                }
            }

            // in case someone tries to use the "!" prefix in an online-only search
            if( name.Length > 0 && name[0] == '!' ) {
                name = name.Substring( 1 );
            }

            bool foundSelf = false;
            List<Player> results = new List<Player>();
            Player[] tempList = Players;
            foreach( Player otherPlayer in tempList ) {
                if( otherPlayer == null ||
                    !includeHidden && !player.CanSee( otherPlayer ) ) {
                    continue;
                }
                if( otherPlayer.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if( !includeSelf && otherPlayer == player ) {
                        foundSelf = true;
                    } else {
                        results.Clear();
                        results.Add( otherPlayer );
                        break;
                    }
                } else if( otherPlayer.Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if( !includeSelf && otherPlayer == player ) {
                        foundSelf = true;
                    } else {
                        results.Add( otherPlayer );
                    }
                }
            }

            // set LastUsedPlayerName if we found one result
            if( results.Count == 1 ) {
                player.LastUsedPlayerName = results[0].Name;
            }

            // raise the SearchingForPlayer event
            if( !suppressEvent ) {
                var h = SearchingForPlayer;
                if( h != null ) {
                    var e = new SearchingForPlayerEventArgs( player, name, results, options );
                    h( null, e );
                }
            }

            // special behavior for ReturnSelfIfOnlyMatch flag
            if( results.Count == 0 && !includeSelf && foundSelf && returnSelf ) {
                results.Add( player );
            }
            return results.ToArray();
        }


        /// <summary> Finds player by name without autocompletion.
        /// Returns null if no player with the given name is online. </summary>
        /// <param name="player"> Player from whose perspective search is performed. Used to determine whether others are hidden. </param>
        /// <param name="name"> Full player name. Case-insensitive. </param>
        /// <param name="options"> Search options (IncludeHidden and IncludeSelf are applicable, other flags are ignored). </param>
        /// <returns> Player object if player was found online; otherwise null. </returns>
        [CanBeNull]
        public static Player FindPlayerExact( [NotNull] Player player, [NotNull] string name, SearchOptions options ) {
            Player target = Players.FirstOrDefault( p => p.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
            bool includeHidden = (options & SearchOptions.IncludeHidden) != 0;
            bool includeSelf = (options & SearchOptions.IncludeSelf) != 0;
            if( target != null && !includeHidden && !player.CanSee( target ) || // hide players whom player cant see
                target == player && !includeSelf ) { // hide self, if applicable
                target = null;
            }
            return target;
        }


        /// <summary> Find player by name using autocompletion.
        /// Returns null and prints message if none or multiple players matched.
        /// Raises Player.SearchingForPlayer event, which may modify search results, unless SuppressEvent option is set. </summary>
        /// <param name="player"> Player who initiated the search. This is where messages are sent. </param>
        /// <param name="namePart"> Full or partial name of the search target. </param>
        /// <param name="options"> Search options.
        /// All flags (IncludeHidden, IncludeSelf, SuppressEvent, and ReturnSelfIfOnlyMatch) are applicable. </param>
        /// <returns> Player object, or null if no player was found. </returns>
        [CanBeNull]
        public static Player FindPlayerOrPrintMatches( [NotNull] Player player,
                                                       [NotNull] string namePart,
                                                       SearchOptions options ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( namePart == null ) throw new ArgumentNullException( "namePart" );

            // Repeat last-used player name
            if( namePart == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    namePart = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return null;
                }
            }

            // in case someone tries to use the "!" prefix in an online-only search
            if( namePart.Length > 0 && namePart[0] == '!' ) {
                namePart = namePart.Substring( 1 );
            }

            // Make sure player name is valid
            if( !Player.ContainsValidCharacters( namePart ) ) {
                player.MessageInvalidPlayerName( namePart );
                return null;
            }

            Player[] matches = FindPlayers( player, namePart, options );

            if( matches.Length == 0 ) {
                player.MessageNoPlayer( namePart );
                return null;

            } else if( matches.Length > 1 ) {
                player.MessageManyMatches( "player", matches );
                return null;

            } else {
                player.LastUsedPlayerName = matches[0].Name;
                return matches[0];
            }
        }


        /// <summary> Counts online players, optionally including hidden ones. </summary>
        public static int CountPlayers( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return Players.Length;
            } else {
                return Players.Count( player => !player.Info.IsHidden );
            }
        }


        /// <summary> Counts online players whom the given observer can see. </summary>
        public static int CountVisiblePlayers( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return Players.Count( observer.CanSee );
        }

        #endregion
    }
}