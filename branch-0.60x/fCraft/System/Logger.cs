// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using fCraft.Events;
using JetBrains.Annotations;
#if DEBUG_EVENTS
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace fCraft {
    /// <summary> Central logging class. Logs to file, relays messages to the frontend, submits crash reports. </summary>
    public static class Logger {
        /// <summary> Gets or sets whether logging is globally enabled/disabled.
        /// If "--nolog" command-line argument is given, logging is disabled. </summary>
        public static bool Enabled { get; set; }

        public static readonly bool[] ConsoleOptions;
        public static readonly bool[] LogFileOptions;

        static readonly object LogLock = new object();
        const string DefaultLogFileName = "fCraft.log",
                     LongDateFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss",
                     ShortDateFormat = "yyyy'-'MM'-'dd",
                     TimeFormat = "HH':'mm':'ss";
        static readonly Uri CrashReportUri = new Uri( "http://www.fcraft.net/crashreport.php" );

        static readonly string SessionStart = DateTime.Now.ToString( LongDateFormat ); // localized
        static readonly Queue<string> RecentMessages = new Queue<string>();
        const int MaxRecentMessages = 25;

        /// <summary> Name of the file that log messages are currently being written to.
        /// Does not include path to the log folder (see Paths.LogPath for that). </summary>
        public static string CurrentLogFileName {
            get {
                switch( SplittingType ) {
                    case LogSplittingType.SplitBySession:
                        return SessionStart + ".log";
                    case LogSplittingType.SplitByDay:
                        return DateTime.Now.ToString( ShortDateFormat ) + ".log"; // localized
                    default:
                        return DefaultLogFileName;
                }
            }
        }


        public static LogSplittingType SplittingType { get; set; }


        static Logger() {
            // initialize defaults
            SplittingType = LogSplittingType.OneFile;
            Enabled = true;
            int typeCount = Enum.GetNames( typeof( LogType ) ).Length;
            ConsoleOptions = new bool[typeCount];
            LogFileOptions = new bool[typeCount];
            for( int i = 0; i < typeCount; i++ ) {
                ConsoleOptions[i] = true;
                LogFileOptions[i] = true;
            }
        }


        internal static void MarkLogStart() {
            // Mark start of logging
            Log( LogType.SystemActivity, "------ Log Starts {0} ({1}) ------",
                 DateTime.Now.ToLongDateString(), DateTime.Now.ToShortDateString() ); // localized
        }


        /// <summary> Logs a message of type ConsoleOutput, strips colors,
        /// and splits into multiple messages at newlines.
        /// Use this method for all messages of LogType.ConsoleOutput </summary>
        public static void LogToConsole( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( message.Contains( '\n' ) ) {
                foreach( string line in message.Split( '\n' ) ) {
                    LogToConsole( line );
                }
                return;
            }

            message = "# " + message;
            Log( LogType.ConsoleOutput, message );
        }


        /// <summary> Adds a message to the server log.
        /// Depending on server configuration and log category, message can be shown in console, logged to file, both, or neither. </summary>
        /// <param name="type"> Type of message. </param>
        /// <param name="message"> Format string for the message. Uses same syntax as String.Format. </param>
        /// <param name="args"> An System.Object array containing zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> Message or args is null. </exception>
        /// <exception cref="FormatException"> String.Format rejected formatting. </exception>
        [DebuggerStepThrough]
        [StringFormatMethod( "message" )]
        public static void Log( LogType type, [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( !Enabled ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            message = message.Replace( "&n", "\n" );
            message = message.Replace( "&N", "\n" );
            message = Chat.ReplaceEmotesWithUncode( message );
            message = Color.StripColors( message );
            string line = DateTime.Now.ToString( TimeFormat ) + " > " + GetPrefix( type ) + message; // localized

            lock( LogLock ) {
                RaiseLoggedEvent( message, line, type );

                RecentMessages.Enqueue( line );
                while( RecentMessages.Count > MaxRecentMessages ) {
                    RecentMessages.Dequeue();
                }

                if( LogFileOptions[(int)type] ) {
                    try {
                        File.AppendAllText( Path.Combine( Paths.LogPath, CurrentLogFileName ),
                                            line + Environment.NewLine );
                    } catch( Exception ex ) {
                        string errorMessage = "Logger.Log: " + ex;
                        line = String.Format( "{0} > {1}{2}",
                                              DateTime.Now.ToString( TimeFormat ),// localized
                                              GetPrefix( LogType.Error ),
                                              errorMessage );
                        RaiseLoggedEvent( errorMessage,
                                          line, 
                                          LogType.Error );
                    }
                }
            }
        }


        [DebuggerStepThrough]
        static string GetPrefix( LogType level ) {
            switch( level ) {
                case LogType.SeriousError:
                case LogType.Error:
                    return "ERROR: ";
                case LogType.Warning:
                    return "Warning: ";
                case LogType.IRCStatus:
                    return "IRC: ";
                default:
                    return String.Empty;
            }
        }


        /// <summary> Disables all file logging (sets all LogFileOptions to false). </summary>
        public static void DisableFileLogging() {
            for( int i = 0; i < LogFileOptions.Length; i++ ) {
                LogFileOptions[i] = false;
            }
        }


        #region Crash Handling

        static readonly object CrashReportLock = new object(); // mutex to prevent simultaneous reports (messes up the timers/requests)
        static DateTime lastCrashReport = DateTime.MinValue;
        static readonly TimeSpan MinCrashReportInterval = TimeSpan.FromSeconds( 61 ); // minimum interval between submitting crash reports, in seconds
        static readonly TimeSpan CrashReporterTimeout = TimeSpan.FromSeconds( 15 );


        /// <summary> Logs and reports a crash or an unhandled exception.
        /// Details are logged, and a crash report may be submitted to fCraft.net.
        /// Note that this method may take several seconds to finish,
        /// since it gathers system information and possibly communicates to fCraft.net. </summary>
        /// <param name="message"> Description/context of the crash. May be null if unknown. </param>
        /// <param name="assembly"> Assembly or component where the crash/exception was caught. May be null if unknown. </param>
        /// <param name="exception"> Exception. May be null. </param>
        /// <param name="shutdownImminent"> Whether this crash will likely report in a server shutdown.
        /// Used for Logger.Crashed event. </param>
        public static void LogAndReportCrash( [CanBeNull] string message, [CanBeNull] string assembly,
                                              [CanBeNull] Exception exception, bool shutdownImminent ) {
            if( message == null ) message = "(none)";
            if( assembly == null ) assembly = "(none)";
            if( exception == null ) exception = new Exception( "(none)" );

            Log( LogType.SeriousError, "{0}: {1}", message, exception );

            // see if crash report should be submitted or skipped, based on CheckForCommonErrors and Crashed event
            try {
                bool submitCrashReport = ConfigKey.SubmitCrashReports.Enabled();
                bool isCommon = CheckForCommonErrors( exception );

                try {
                    var eventArgs = new CrashedEventArgs( message,
                                                          assembly,
                                                          exception,
                                                          submitCrashReport && !isCommon,
                                                          isCommon,
                                                          shutdownImminent );
                    RaiseCrashedEvent( eventArgs );
                    isCommon = eventArgs.IsCommonProblem;
                } catch( Exception ex ) {
                    Log( LogType.Error, "Crash reporter callback failure: {0}", ex );
                }

                if( !submitCrashReport || isCommon ) {
                    return;
                }
            } catch( Exception ex ) {
                Log( LogType.Error, "Crash reporter failure: {0}", ex );
            }

            lock( CrashReportLock ) {
                // For compatibility with lighttpd server (that received crash reports)
                ServicePointManager.Expect100Continue = false;

                // Make sure tight errors-in-loops don't spam the reporter
                if( DateTime.UtcNow.Subtract( lastCrashReport ) < MinCrashReportInterval ) {
                    Log( LogType.Warning, "Logger.SubmitCrashReport: Could not submit crash report, reports too frequent." );
                    return;
                }

                lastCrashReport = DateTime.UtcNow;
                LogAndReportCrashInner( message, assembly, exception );
            }
        }


        static void LogAndReportCrashInner( string message, string assembly, Exception exception ) {
            if( exception.InnerException != null ) {
                LogAndReportCrashInner( "(inner)" + message, assembly, exception.InnerException );
            }

            try {
                StringBuilder sb = new StringBuilder();
                sb.Append( "version=" ).Append( Uri.EscapeDataString( Updater.CurrentRelease.VersionString ) );
                sb.Append( "&message=" ).Append( Uri.EscapeDataString( message ) );
                sb.Append( "&assembly=" ).Append( Uri.EscapeDataString( assembly ) );
                sb.Append( "&runtime=" );
                if( MonoCompat.IsMono ) {
                    sb.Append( Uri.EscapeDataString( "Mono " + MonoCompat.MonoVersionString ) );
                } else {
                    sb.Append( Uri.EscapeDataString( "CLR " + Environment.Version ) );
                }
                sb.Append( "&os=" ).Append( Environment.OSVersion.Platform + " / " + Environment.OSVersion.VersionString );

                sb.Append( "&exceptiontype=" ).Append( Uri.EscapeDataString( exception.GetType().ToString() ) );
                sb.Append( "&exceptionmessage=" ).Append( Uri.EscapeDataString( exception.Message ) );
                sb.Append( "&exceptionstacktrace=" );
                if( exception.StackTrace != null ) {
                    sb.Append( Uri.EscapeDataString( exception.StackTrace ) );
                } else {
                    sb.Append( "(none)" );
                }

                sb.Append( "&config=" );
                if( File.Exists( Paths.ConfigFileName ) ) {
                    sb.Append( Uri.EscapeDataString( File.ReadAllText( Paths.ConfigFileName ) ) );
                }

                string assemblies = AppDomain.CurrentDomain
                                             .GetAssemblies()
                                             .JoinToString( Environment.NewLine, asm => asm.FullName );
                sb.Append( "&asm=" ).Append( Uri.EscapeDataString( assemblies ) );

                string[] lastFewLines;
                lock( LogLock ) {
                    lastFewLines = RecentMessages.ToArray();
                }
                sb.Append( "&log=" ).Append( Uri.EscapeDataString( String.Join( Environment.NewLine, lastFewLines ) ) );

                byte[] formData = Encoding.UTF8.GetBytes( sb.ToString() );

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( CrashReportUri );
                request.CachePolicy = Server.CachePolicy;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                request.ReadWriteTimeout = (int)CrashReporterTimeout.TotalMilliseconds;
                request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
                request.Timeout = (int)CrashReporterTimeout.TotalMilliseconds;
                request.UserAgent = Updater.UserAgent;

                request.ContentLength = formData.Length;

                using( Stream requestStream = request.GetRequestStream() ) {
                    requestStream.Write( formData, 0, formData.Length );
                    requestStream.Flush();
                }

                string responseString;
                using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() ) {
                    using( Stream responseStream = response.GetResponseStream() ) {
                        // ReSharper disable AssignNullToNotNullAttribute
                        using( StreamReader reader = new StreamReader( responseStream ) ) {
                            // ReSharper restore AssignNullToNotNullAttribute
                            responseString = reader.ReadLine();
                        }
                    }
                }
                request.Abort();

                if( responseString != null && responseString.StartsWith( "ERROR" ) ) {
                    Log( LogType.Error, "Crash report could not be processed by fCraft.net." );
                } else {
                    int referenceNumber;
                    if( responseString != null && Int32.TryParse( responseString, out referenceNumber ) ) {
                        Log( LogType.SystemActivity, "Crash report submitted (Reference #{0})", referenceNumber );
                    } else {
                        Log( LogType.SystemActivity, "Crash report submitted." );
                    }
                }


            } catch( Exception ex ) {
                Log( LogType.Warning, "Logger.SubmitCrashReport: {0}", ex );
            }
        }


        // Called by the Logger in case of serious errors to print troubleshooting advice.
        // Returns true if this type of error is common, and crash report should NOT be submitted.
        static bool CheckForCommonErrors( [CanBeNull] Exception ex ) {
            if( ex == null ) throw new ArgumentNullException( "ex" );
            string message = null;
            try {
                if( ex is FileNotFoundException && ex.Message.Contains( "Version=3.5" ) ) {
                    message = "Your crash was likely caused by using a wrong version of .NET or Mono runtime. " +
                              "Please update to Microsoft .NET Framework 3.5 (Windows) OR Mono 2.6.4+ (Linux, Unix, Mac OS X).";
                    return true;

                } else if( ex.Message.Contains( "libMonoPosixHelper" ) ||
                           ex is EntryPointNotFoundException && ex.Message.Contains( "CreateZStream" ) ) {
                    message = "fCraft could not locate Mono's compression functionality. " +
                              "Please make sure that you have zlib (sometimes called \"libz\" or just \"z\") installed. " +
                              "Some versions of Mono may also require \"libmono-posix-2.0-cil\" package to be installed.";
                    return true;

                } else if( ex is MissingMemberException || ex is TypeLoadException ) {
                    message = "Something is incompatible with the current revision of fCraft. " +
                              "If you installed third-party modifications, " +
                              "make sure to use the correct revision (as specified by mod developers). " +
                              "If your own modifications stopped working, your may need to make some updates.";
                    return true;

                } else if( ex is UnauthorizedAccessException ) {
                    message = "fCraft was blocked from accessing a file or resource. " +
                              "Make sure that correct permissions are set for the fCraft files, folders, and processes.";
                    return true;

                } else if( ex is OutOfMemoryException ) {
                    message = "fCraft ran out of memory. Make sure there is enough RAM to run.";
                    return true;

                } else if( ex is SystemException && ex.Message == "Can't find current process" ) {
                    // Ignore Mono-specific bug in MonitorProcessorUsage()
                    return true;

                } else if( ex is InvalidOperationException && ex.StackTrace.Contains( "MD5CryptoServiceProvider" ) ) {
                    message = "Some Windows settings are preventing fCraft from doing player name verification. " +
                              "See http://support.microsoft.com/kb/811833";
                    return true;

                } else if( ex.StackTrace.Contains( "__Error.WinIOError" ) ) {
                    message = "A filesystem-related error has occurred. Make sure that only one instance of fCraft is running, " +
                              "and that no other processes are using server's files or directories.";
                    return true;

                } else if( ex.Message.Contains( "UNSTABLE" ) ) {
                    return true;

                } else {
                    return false;
                }
            } finally {
                if( message != null ) {
                    Log( LogType.Warning, message );
                }
            }
        }

        #endregion


        #region Event Tracing
#if DEBUG_EVENTS

        // list of events in this assembly
        static readonly Dictionary<int, EventInfo> eventsMap = new Dictionary<int, EventInfo>();


        static List<string> eventWhitelist = new List<string>();
        static List<string> eventBlacklist = new List<string>();
        const string TraceWhitelistFile = "traceonly.txt",
                     TraceBlacklistFile = "notrace.txt";
        static bool useEventWhitelist, useEventBlacklist;

        static void LoadTracingSettings() {
            if( File.Exists( TraceWhitelistFile ) ) {
                useEventWhitelist = true;
                eventWhitelist.AddRange( File.ReadAllLines( TraceWhitelistFile ) );
            } else if( File.Exists( TraceBlacklistFile ) ) {
                useEventBlacklist = true;
                eventBlacklist.AddRange( File.ReadAllLines( TraceBlacklistFile ) );
            }
        }


        // adds hooks to all compliant events in current assembly
        internal static void PrepareEventTracing() {

            LoadTracingSettings();

            // create a dynamic type to hold our handler methods
            AppDomain myDomain = AppDomain.CurrentDomain;
            var asmName = new AssemblyName( "fCraftEventTracing" );
            AssemblyBuilder myAsmBuilder = myDomain.DefineDynamicAssembly( asmName, AssemblyBuilderAccess.RunAndSave );
            ModuleBuilder myModule = myAsmBuilder.DefineDynamicModule( "DynamicHandlersModule" );
            TypeBuilder typeBuilder = myModule.DefineType( "EventHandlersContainer", TypeAttributes.Public );

            int eventIndex = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            List<EventInfo> eventList = new List<EventInfo>();

            // find all events in current assembly, and create a handler for each one
            foreach( Type type in asm.GetTypes() ) {
                foreach( EventInfo eventInfo in type.GetEvents() ) {
                    // Skip non-static events
                    if( (eventInfo.GetAddMethod().Attributes & MethodAttributes.Static) != MethodAttributes.Static ) {
                        continue;
                    }
                    if( eventInfo.EventHandlerType.FullName.StartsWith( typeof( EventHandler<> ).FullName ) ||
                        eventInfo.EventHandlerType.FullName.StartsWith( typeof( EventHandler ).FullName ) ) {

                        if( useEventWhitelist && !eventWhitelist.Contains( type.Name + "." + eventInfo.Name, StringComparer.OrdinalIgnoreCase ) ||
                            useEventBlacklist && eventBlacklist.Contains( type.Name + "." + eventInfo.Name, StringComparer.OrdinalIgnoreCase ) ) continue;

                        MethodInfo method = eventInfo.EventHandlerType.GetMethod( "Invoke" );
                        var parameterTypes = method.GetParameters().Select( info => info.ParameterType ).ToArray();
                        AddEventHook( typeBuilder, parameterTypes, method.ReturnType, eventIndex );
                        eventList.Add( eventInfo );
                        eventsMap.Add( eventIndex, eventInfo );
                        eventIndex++;
                    }
                }
            }

            // hook up the handlers
            Type handlerType = typeBuilder.CreateType();
            for( int i = 0; i < eventList.Count; i++ ) {
                MethodInfo notifier = handlerType.GetMethod( "EventHook" + i );
                var handlerDelegate = Delegate.CreateDelegate( eventList[i].EventHandlerType, notifier );
                try {
                    eventList[i].AddEventHandler( null, handlerDelegate );
                } catch( TargetException ) {
                    // There's no way to tell if an event is static until you
                    // try adding a handler with target=null.
                    // If it wasn't static, TargetException is thrown
                }
            }
        }


        // create a static handler method that matches the given signature, and calls EventTraceNotifier
        static void AddEventHook( TypeBuilder typeBuilder, Type[] methodParams, Type returnType, int eventIndex ) {
            string methodName = "EventHook" + eventIndex;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod( methodName,
                                                                    MethodAttributes.Public | MethodAttributes.Static,
                                                                    returnType,
                                                                    methodParams );

            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit( OpCodes.Ldc_I4, eventIndex );
            generator.Emit( OpCodes.Ldarg_1 );
            MethodInfo min = typeof( Logger ).GetMethod( "EventTraceNotifier" );
            generator.EmitCall( OpCodes.Call, min, null );
            generator.Emit( OpCodes.Ret );
        }


        // Invoked when events fire
        public static void EventTraceNotifier( int eventIndex, EventArgs e ) {
            if( (e is LogEventArgs) && ((LogEventArgs)e).MessageType == LogType.Trace ) return;
            var eventInfo = eventsMap[eventIndex];

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( var prop in e.GetType().GetProperties() ) {
                if( !first ) sb.Append( ", " );
                if( prop.Name != prop.PropertyType.Name ) {
                    sb.Append( prop.Name ).Append( '=' );
                }
                object val = prop.GetValue( e, null );
                if( val == null ) {
                    sb.Append( "null" );
                } else if( val is string ) {
                    sb.AppendFormat( "\"{0}\"", val );
                } else {
                    sb.Append( val );
                }
                first = false;
            }

            Log( LogType.Trace,
                 "TraceEvent: {0}.{1}( {2} )",
                 eventInfo.DeclaringType.Name, eventInfo.Name, sb.ToString() );

        }

#endif
        #endregion


        #region Events

        /// <summary> Occurs after a message has been logged. </summary>
        public static event EventHandler<LogEventArgs> Logged;


        /// <summary> Occurs when the server "crashes" (has an unhandled exception).
        /// Note that such occurences will not always cause shutdowns - check ShutdownImminent property.
        /// Reporting of the crash may be suppressed. </summary>
        public static event EventHandler<CrashedEventArgs> Crashed;


        [DebuggerStepThrough]
        static void RaiseLoggedEvent( [NotNull] string rawMessage, [NotNull] string line, LogType logType ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            if( line == null ) throw new ArgumentNullException( "line" );
            var h = Logged;
            if( h != null ) h( null, new LogEventArgs( rawMessage,
                                                       line,
                                                       logType,
                                                       LogFileOptions[(int)logType],
                                                       ConsoleOptions[(int)logType] ) );
        }


        static void RaiseCrashedEvent( CrashedEventArgs e ) {
            var h = Crashed;
            if( h != null ) h( null, e );
        }

        #endregion
    }


    #region Enums

    /// <summary> Category of a log event. </summary>
    public enum LogType {
        /// <summary> System activity (loading/saving of data, shutdown and startup events, etc). </summary>
        SystemActivity,

        /// <summary> Warnings (missing files, config discrepancies, minor recoverable errors, etc). </summary>
        Warning,

        /// <summary> Recoverable errors (loading/saving problems, connection problems, etc). </summary>
        Error,

        /// <summary> Critical non-recoverable errors and crashes. </summary>
        SeriousError,

        /// <summary> Routine user activity (command results, kicks, bans, etc). </summary>
        UserActivity,

        /// <summary> Raw commands entered by the player. </summary>
        UserCommand,

        /// <summary> Permission and hack related activity (name verification failures, banned players logging in, detected hacks, etc). </summary>
        SuspiciousActivity,

        /// <summary> Normal (white) chat written by the players. </summary>
        GlobalChat,

        /// <summary> Private messages exchanged by players. </summary>
        PrivateChat,

        /// <summary> Rank chat messages. </summary>
        RankChat,

        /// <summary> Messages and commands entered from console. </summary>
        ConsoleInput,

        /// <summary> Messages printed to the console (typically as the result of commands called from console). </summary>
        ConsoleOutput,

        /// <summary> Information related to IRC activity. </summary>
        IRCStatus,

        /// <summary> IRC chatter and join/part messages. </summary>
        IRCChat,

        /// <summary> Information useful for debugging (error details, routine events, system information). </summary>
        Debug,

        /// <summary> Special-purpose messages related to event tracing (never logged). </summary>
        Trace
    }


    /// <summary> Log splitting type. </summary>
    public enum LogSplittingType {
        /// <summary> All logs are written to one file. </summary>
        OneFile,

        /// <summary> A new timestamped logfile is made every time the server is started. </summary>
        SplitBySession,

        /// <summary> A new timestamped logfile is created every 24 hours. </summary>
        SplitByDay
    }

    #endregion
}


namespace fCraft.Events {
    /// <summary> Provides data for Logger.Logged event. Immutable. </summary>
    public sealed class LogEventArgs : EventArgs {
        [DebuggerStepThrough]
        internal LogEventArgs( string rawMessage, string message, LogType messageType, bool writeToFile, bool writeToConsole ) {
            RawMessage = rawMessage;
            Message = message;
            MessageType = messageType;
            WriteToFile = writeToFile;
            WriteToConsole = writeToConsole;
        }
        public string RawMessage { get; private set; }
        public string Message { get; private set; }
        public LogType MessageType { get; private set; }
        public bool WriteToFile { get; private set; }
        public bool WriteToConsole { get; private set; }
    }


    /// <summary> Provides for Logger.Crashed event. Crash reporting can be canceled. </summary>
    public sealed class CrashedEventArgs : EventArgs {
        internal CrashedEventArgs( string message, string location, Exception exception, bool submitCrashReport, bool isCommonProblem, bool shutdownImminent ) {
            Message = message;
            Location = location;
            Exception = exception;
            SubmitCrashReport = submitCrashReport;
            IsCommonProblem = isCommonProblem;
            ShutdownImminent = shutdownImminent;
        }
        public string Message { get; private set; }
        public string Location { get; private set; }
        public Exception Exception { get; private set; }
        public bool SubmitCrashReport { get; set; }
        public bool IsCommonProblem { get; private set; }
        public bool ShutdownImminent { get; private set; }
    }
}