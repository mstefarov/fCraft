// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace fCraft {

    /// <summary>
    /// Category of a log event.
    /// </summary>
    public enum LogType {
        SystemActivity,
        Warning,
        Error,
        FatalError,

        UserActivity,
        UserCommand,
        SuspiciousActivity,

        GlobalChat,
        PrivateChat,
        RankChat,

        ConsoleInput,
        ConsoleOutput,

        IRC,
        Debug
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
        static object locker = new object();
        public static bool[] consoleOptions;
        public static bool[] logFileOptions;

        const string DefaultLogFileName = "fCraft.log",
                     CrashLogFileName = "fCraftCRASH.log",
                     CrashReportURL = "http://fragmer.net/fcraft/crashreport.php",
                     LongDateFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss",
                     ShortDateFormat = "yyyy'-'MM'-'dd";
        public static LogSplittingType split = LogSplittingType.OneFile;

        static string sessionStart = DateTime.Now.ToString( LongDateFormat );
        static Queue<string> recentMessages = new Queue<string>();
        const int MaxRecentMessages = 25;


        static Logger() {
            consoleOptions = new bool[Enum.GetNames( typeof( LogType ) ).Length];
            logFileOptions = new bool[consoleOptions.Length];
            for( int i = 0; i < consoleOptions.Length; i++ ) {
                consoleOptions[i] = true;
                logFileOptions[i] = true;
            }

            MarkLogStart();
        }


        public static void MarkLogStart() {
            // Mark start of logging
            Log( "------ Log Starts {0} ({1}) ------", LogType.SystemActivity,
                 DateTime.Now.ToLongDateString(), DateTime.Now.ToShortDateString() );
        }


        public static void Log( string message, LogType type, params object[] values ) {
            Log( String.Format( message, values ), type );
        }


        public static void LogConsole( string message ) {
            if( message.Contains( "&N" ) ) {
                foreach( string line in message.Split( PacketWriter.splitter, StringSplitOptions.RemoveEmptyEntries ) ) {
                    LogConsole( line );
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
            if( type == LogType.FatalError ) {
                LogFatalError( message );
            }

            string line = DateTime.Now.ToLongTimeString() + " > " + GetPrefix( type ) + message;
            if( logFileOptions[(int)type] ) {
                string actualLogFileName;
                switch( split ) {
                    case LogSplittingType.SplitBySession:
                        actualLogFileName = Path.Combine( Paths.LogPath, sessionStart + ".log" );
                        break;
                    case LogSplittingType.SplitByDay:
                        actualLogFileName = Path.Combine( Paths.LogPath, DateTime.Now.ToString( ShortDateFormat ) + ".log" );
                        break;
                    default:
                        actualLogFileName = Path.Combine( Paths.LogPath, DefaultLogFileName );
                        break;
                }
                try {
                    lock( locker ) {
                        File.AppendAllText( actualLogFileName, line + Environment.NewLine );
                        recentMessages.Enqueue( line );
                        while( recentMessages.Count > MaxRecentMessages ) {
                            recentMessages.Dequeue();
                        }
                    }
                } catch( Exception ex ) {
                    Server.FireLogEvent( "Logger.Log: " + ex, type );
                }
            }

            if( consoleOptions[(int)type] ) Server.FireLogEvent( line, type );
        }


        public static string GetPrefix( LogType level ) {
            switch( level ) {
                case LogType.FatalError:
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

        static readonly object crashReportLock = new object(); // mutex to prevent simultaneous reports (messes up the timers/requests)
        static DateTime lastCrashReport = DateTime.MinValue;
        const int MinCrashReportInterval = 61; // minimum interval between submitting crash reports, in seconds


        public static void LogAndReportCrash( string message, string assembly, Exception exception ) {

            if( !CheckForCommonErrors( exception ) ) return;

            Log( "{0}: {1}", LogType.FatalError, message, exception );

            lock( crashReportLock ) {
                if( DateTime.UtcNow.Subtract( lastCrashReport ).TotalSeconds < MinCrashReportInterval ) {
                    Log( "Logger.SubmitCrashReport: Could not submit crash report, reports too frequent.", LogType.Warning );
                    return;
                }
                lastCrashReport = DateTime.UtcNow;

                try {
                    StringBuilder sb = new StringBuilder();
                    sb.Append( "version=" ).Append( Uri.EscapeDataString( Updater.GetVersionString() ) );
                    sb.Append( "&message=" ).Append( Uri.EscapeDataString( message ) );
                    sb.Append( "&assembly=" ).Append( Uri.EscapeDataString( assembly ) );
                    sb.Append( "&runtime=" ).Append( Uri.EscapeDataString( Environment.Version + " / " + RuntimeEnvironment.GetSystemVersion() ) );
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
                    lock( locker ) {
                        lastFewLines = recentMessages.ToArray();
                    }
                    sb.Append( "&log=" ).Append( Uri.EscapeDataString( String.Join( Environment.NewLine, lastFewLines ) ) );

                    byte[] formData = Encoding.ASCII.GetBytes( sb.ToString() );

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create( CrashReportURL );
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


        public static void LogFatalError( string message ) {
            string crashMessage = String.Format( "{0}{1}{2}{1}{1}",
                                                 DateTime.Now,
                                                 Environment.NewLine,
                                                 message );
            try {
                File.AppendAllText( CrashLogFileName, crashMessage );
            } catch( Exception ex ) {
                Log( "Cannot save crash report to \"{0}\": {1}", LogType.Error,
                            CrashLogFileName, ex );
            }
        }

        // Called by the Logger in case of serious errors to print troubleshooting advice.
        // Returns true if a crash report should be submitted for this type of errors.
        public static bool CheckForCommonErrors( Exception ex ) {
            if( ex.Message.Contains( "System.Xml.Linq" ) ) {
                Log( "Your crash was likely caused by using an outdated version of .NET or Mono runtime. " +
                            "Please update to Microsoft .NET Framework 3.5+ (Windows) OR Mono 2.6.4+ (Linux, Unix, Mac OS X).", LogType.Warning );
                return false;

            } else if( ex.Message.Equals( "libMonoPosixHelper.so", StringComparison.OrdinalIgnoreCase ) ) {
                Log( "fCraft could not locate Mono's compression functionality. " +
                            "Please make sure that you have zlib and libmono-posix-2.0-cil or equivalent package installed.", LogType.Warning );
                return false;

            } else if( ex is UnauthorizedAccessException ) {
                Log( "fCraft was blocked from accessing a file or resource. " +
                            "Make sure that correct permissions are set for the fCraft files, folders, and processes.", LogType.Warning );
                return false;

            } else if( ex is OutOfMemoryException ) {
                Log( "fCraft ran out of memory. Make sure there is enough RAM to run. " +
                            "Note that large draw commands can consume a lot of RAM.", LogType.Warning );
                return false;

            } else if( ex is TypeLoadException && ex.Message.Contains( "ZLibStream" ) ) {
                Log( "Note that ZLibStream is obsolete since fCraft 0.498. Use GZipStream instead.", LogType.Warning );
                return false;

            } else {
                return true;
            }
        }

        #endregion


        #region Event Tracing
#if DEBUG_EVENTS

        // list of events in this assembly
        static private readonly Dictionary<int, EventInfo> eventsMap = new Dictionary<int, EventInfo>();


        // adds hooks to all compliant events in current assembly
        internal static void FindEvents() {

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
                    if( eventInfo.EventHandlerType.FullName.StartsWith( typeof( EventHandler<> ).FullName ) ) {
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
            var eventInfo = eventsMap[eventIndex];

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( var prop in e.GetType().GetProperties() ) {
                if( !first ) sb.Append( ", " );
                if( prop.Name != prop.PropertyType.Name ) {
                    sb.Append( prop.Name ).Append( '=' );
                }
                sb.Append( prop.GetValue( e, null ) );
                first = false;
            }

            Log( "TraceEvent: {0}.{1}( {2} )", LogType.Debug,
                 eventInfo.DeclaringType.Name, eventInfo.Name, sb.ToString() );

        }

#endif
        #endregion
    }
}