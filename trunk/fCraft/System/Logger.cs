// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Linq;
using fCraft.Events;

namespace fCraft {

    /// <summary>
    /// Category of a log event.
    /// </summary>
    public enum LogType {
        SystemActivity,
        Warning,
        Error,
        SeriousError,

        UserActivity,
        UserCommand,
        SuspiciousActivity,

        GlobalChat,
        PrivateChat,
        RankChat,

        ConsoleInput,
        ConsoleOutput,

        IRC,
        Debug,
        Trace
    }


    public enum LogSplittingType {
        OneFile,
        SplitBySession,
        SplitByDay
    }


    /// <summary>
    /// Central logging class. Logs to file, relays messages to the frontend, submits crash reports.
    /// </summary>
    public static class Logger {
        static readonly object LogLock = new object();
        public static readonly bool[] ConsoleOptions;
        public static readonly bool[] LogFileOptions;

        const string DefaultLogFileName = "fCraft.log",
                     CrashReportUrl = "http://fragmer.net/fcraft/crashreport.php",
                     LongDateFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss",
                     ShortDateFormat = "yyyy'-'MM'-'dd";
        public static LogSplittingType SplittingType = LogSplittingType.OneFile;

        static readonly string SessionStart = DateTime.Now.ToString( LongDateFormat );
        static readonly Queue<string> RecentMessages = new Queue<string>();
        const int MaxRecentMessages = 25;


        static Logger() {
            ConsoleOptions = new bool[Enum.GetNames( typeof( LogType ) ).Length];
            LogFileOptions = new bool[ConsoleOptions.Length];
            for( int i = 0; i < ConsoleOptions.Length; i++ ) {
                ConsoleOptions[i] = true;
                LogFileOptions[i] = true;
            }
        }


        internal static void MarkLogStart() {
            // Mark start of logging
            Log( "------ Log Starts {0} ({1}) ------", LogType.SystemActivity,
                 DateTime.Now.ToLongDateString(), DateTime.Now.ToShortDateString() );
        }


        public static void Log( string message, LogType type, params object[] values ) {
            Log( String.Format( message, values ), type );
        }


        public static void LogToConsole( string message ) {
            if( message.Contains( "&N" ) ) {
                foreach( string line in message.Split( PacketWriter.NewlineSplitter, StringSplitOptions.RemoveEmptyEntries ) ) {
                    LogToConsole( line );
                }
                return;
            }
            string processedMessage = "# ";
            for( int i = 0; i < message.Length; i++ ) {
                if( message[i] == '&' ) i++;
                else processedMessage += message[i];
            }
            Log( processedMessage, LogType.ConsoleOutput );
        }


        public static void Log( string message, LogType type ) {
            string line = DateTime.Now.ToLongTimeString() + " > " + GetPrefix( type ) + message;

            RaiseLoggedEvent( message, line, type );

            if( LogFileOptions[(int)type] ) {
                string actualLogFileName;
                switch( SplittingType ) {
                    case LogSplittingType.SplitBySession:
                        actualLogFileName = Path.Combine( Paths.LogPath, SessionStart + ".log" );
                        break;
                    case LogSplittingType.SplitByDay:
                        actualLogFileName = Path.Combine( Paths.LogPath, DateTime.Now.ToString( ShortDateFormat ) + ".log" );
                        break;
                    default:
                        actualLogFileName = Path.Combine( Paths.LogPath, DefaultLogFileName );
                        break;
                }
                try {
                    lock( LogLock ) {
                        File.AppendAllText( actualLogFileName, line + Environment.NewLine );
                        RecentMessages.Enqueue( line );
                        while( RecentMessages.Count > MaxRecentMessages ) {
                            RecentMessages.Dequeue();
                        }
                    }
                } catch( Exception ex ) {
                    string errorMessage = "Logger.Log: " + ex.Message;
                    RaiseLoggedEvent( errorMessage,
                                      DateTime.Now.ToLongTimeString() + " > " + GetPrefix( LogType.Error ) + errorMessage,
                                      LogType.Error );
                }
            }
        }


        public static string GetPrefix( LogType level ) {
            switch( level ) {
                case LogType.SeriousError:
                case LogType.Error:
                    return "ERROR: ";
                case LogType.Warning:
                    return "Warning: ";
                case LogType.IRC:
                    return "IRC: ";
                default:
                    return String.Empty;
            }
        }


        #region Crash Handling

        static readonly object CrashReportLock = new object(); // mutex to prevent simultaneous reports (messes up the timers/requests)
        static DateTime lastCrashReport = DateTime.MinValue;
        const int MinCrashReportInterval = 61; // minimum interval between submitting crash reports, in seconds


        public static void LogAndReportCrash( string message, string assembly, Exception exception, bool shutdownImminent ) {

            Log( "{0}: {1}", LogType.SeriousError, message, exception );

            bool submitCrashReport = ConfigKey.SubmitCrashReports.GetBool();
            bool isCommon = CheckForCommonErrors( exception );

            try {
                CrashEventArgs eventArgs = new CrashEventArgs( message, assembly, exception, submitCrashReport && !isCommon, isCommon, shutdownImminent );
                RaiseCrashedEvent( eventArgs );
                isCommon = eventArgs.IsCommonProblem;
            } catch { }

            if( !submitCrashReport || isCommon ) {
                return;
            }

            lock( CrashReportLock ) {
                if( DateTime.UtcNow.Subtract( lastCrashReport ).TotalSeconds < MinCrashReportInterval ) {
                    Log( "Logger.SubmitCrashReport: Could not submit crash report, reports too frequent.", LogType.Warning );
                    return;
                }
                lastCrashReport = DateTime.UtcNow;

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
                    if( exception != null ) {
                        if( exception is TargetInvocationException ) {
                            exception = (exception).InnerException;
                        } else if( exception is TypeInitializationException ) {
                            exception = (exception).InnerException;
                        }
                        sb.Append( "&exceptiontype=" ).Append( Uri.EscapeDataString( exception.GetType().ToString() ) );
                        sb.Append( "&exceptionmessage=" ).Append( Uri.EscapeDataString( exception.Message ) );
                        sb.Append( "&exceptionstacktrace=" ).Append( Uri.EscapeDataString( exception.StackTrace ) );
                    } else {
                        sb.Append( "&exceptiontype=&exceptionmessage=&exceptiontrace=" );
                    }
                    if( File.Exists( Paths.ConfigFileName ) ) {
                        sb.Append( "&config=" ).Append( Uri.EscapeDataString( File.ReadAllText( Paths.ConfigFileName ) ) );
                    } else {
                        sb.Append( "&config=" );
                    }

                    string[] lastFewLines;
                    lock( LogLock ) {
                        lastFewLines = RecentMessages.ToArray();
                    }
                    sb.Append( "&log=" ).Append( Uri.EscapeDataString( String.Join( Environment.NewLine, lastFewLines ) ) );

                    byte[] formData = Encoding.ASCII.GetBytes( sb.ToString() );

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create( CrashReportUrl );
                    ServicePointManager.Expect100Continue = false;
                    request.Method = "POST";
                    request.Timeout = 15000; // 15s timeout
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );
                    request.ContentLength = formData.Length;

                    using( Stream requestStream = request.GetRequestStream() ) {
                        requestStream.Write( formData, 0, formData.Length );
                        requestStream.Flush();
                    }

                    request.Abort();
                    Log( "Crash report submitted.", LogType.SystemActivity );

                } catch( Exception ex ) {
                    Log( "Logger.SubmitCrashReport: {0}", LogType.Warning, ex.Message );
                }
            }
        }


        // Called by the Logger in case of serious errors to print troubleshooting advice.
        // Returns true if this type of error is common, and crash report should NOT be submitted.
        public static bool CheckForCommonErrors( Exception ex ) {
            string message = null;
            try {
                if( ex is FileNotFoundException && (ex.Message.Contains( "System.Xml.Linq, Version=3.5" ) ||
                                                    ex.Message.Contains( "System.Core, Version=3.5" )) ) {
                    message = "Your crash was likely caused by using an outdated version of .NET or Mono runtime. " +
                              "Please update to Microsoft .NET Framework 3.5+ (Windows) OR Mono 2.6.4+ (Linux, Unix, Mac OS X).";
                    return true;

                } else if( ex.Message.Trim().Equals( "libMonoPosixHelper.so", StringComparison.OrdinalIgnoreCase ) ) {
                    message = "fCraft could not locate Mono's compression functionality. " +
                              "Please make sure that you have zlib (sometimes called \"libz\" or just \"z\") installed. " +
                              "Some versions of Mono may also require \"libmono-posix-2.0-cil\" package to be installed.";
                    return true;

                } else if( ex is UnauthorizedAccessException ) {
                    message = "fCraft was blocked from accessing a file or resource. " +
                              "Make sure that correct permissions are set for the fCraft files, folders, and processes.";
                    return true;

                } else if( ex is OutOfMemoryException ) {
                    message = "fCraft ran out of memory. Make sure there is enough RAM to run. " +
                              "Note that large draw commands can consume a lot of RAM.";
                    return true;

                } else if( ex is TypeLoadException && ex.Message.Contains( "ZLibStream" ) ) {
                    message = "Note that ZLibStream is obsolete since fCraft 0.498. Use GZipStream instead.";
                    return true;

                } else if( ex is SystemException && ex.Message == "Can't find current process" ) {
                    // Mono-specific bug in MonitorProcessorUsage()
                    return true;

                } else {
                    return false;
                }
            } finally {
                if( message != null ) {
                    Log( message, LogType.Warning );
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

            Log( "TraceEvent: {0}.{1}( {2} )", LogType.Trace,
                 eventInfo.DeclaringType.Name, eventInfo.Name, sb.ToString() );

        }

#endif
        #endregion


        #region Events

        public static event EventHandler<LogEventArgs> Logged;


        public static event EventHandler<CrashEventArgs> Crashed;


        static void RaiseLoggedEvent( string rawMessage, string line, LogType logType ) {
            if( ConsoleOptions[(int)logType] ) {// LEGACY
                Server.FireLogEvent( line, logType );
            }
            var h = Logged;
            if( h != null ) h( null, new LogEventArgs( rawMessage,
                                                       line,
                                                       logType,
                                                       LogFileOptions[(int)logType],
                                                       ConsoleOptions[(int)logType] ) );
        }


        static void RaiseCrashedEvent( CrashEventArgs e ) {
            var h = Crashed;
            if( h != null ) h( null, e );
        }

        #endregion
    }
}


#region EventArgs
namespace fCraft.Events {

    public sealed class LogEventArgs : EventArgs {
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


    public sealed class CrashEventArgs : EventArgs {
        internal CrashEventArgs( string message, string location, Exception exception, bool submitCrashReport, bool isCommonProblem, bool shutdownImminent ) {
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
#endregion