// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Collections.Generic;


namespace fCraft {
    public enum LogType {
        SystemActivity,
        Warning,
        Error,
        FatalError,

        UserActivity,
        UserCommand,
        SuspiciousActivity,

        WorldChat,
        GlobalChat,
        PrivateChat,
        ClassChat,

        ConsoleInput,
        ConsoleOutput,

        IRC,
        Debug
    }


    public enum WarningLogSubtype {
        CommandWarning,
        MissingFileWarning,
        PlayerDBWarning,
        WorldListError,
        ConfigWarning,
        ClassListWarning,
        MapLoadWarning
    }

    public enum ErrorLogSubtype {
        MapSaveFailed,
        MapLoadingFailed,
        WorldError,
        GenerationFailed,
        HeartbeatError,
        UpdateError,
        IPBanListError,
        SessionError,
        LoginSequenceError,
        JoinWorldError,
        ClassListError,
        PlayerDBError,
        NetworkError,
        WorldListError,
        ConfigError,
        BackupError,
        ImportError
    }

    public enum FatalErrorLogSubtype {
        PlayerDBSchemaTooNew,
        PlayerDBVersionNotFound,
        PlayerDBVersionError,
        CouldNotStartListening,
        CouldNotParseWorldList,
        WorldCreationFailed,
        CouldNotLoadConfig,
        CouldNotSaveConfig
    }

    public enum IRCLogSubtype {
        IRCLoggingIn,
        IRCBotNickTaken,
        IRCBotKicked,
        IRCBotKilled,
        IRCBotDisconnected,
        IRCError
    }

    public enum SuspicousActivityLogSubtype {
        InvalidSetTilePacket,
        LeavingMapBoundaries,
        InvalidPlayerName,
        NameNotVerified,
        BannedPlayerTriedToLog,
        PlayerLoggingInFromBannedIP,
        LoginFromSameName,
        LoginFromSameIP,
        BlockSpam,
        ChatSpam,
        PacketSpam
    }


    public enum LogSplittingType {
        OneFile,
        SplitBySession,
        SplitByDay
    }


    public static class Logger {
        static object locker = new object();
        public static bool[] consoleOptions;
        public static bool[] logFileOptions;

        const string LogFileName = "fCraft.log",
                     CrashFileName = "fCraftCRASH.log",
                     CrashReportURL = "http://fragmer.net/fcraft/crashreport.php",
                     LongDateFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss",
                     ShortDateFormat = "yyyy'-'MM'-'dd";
        public static LogSplittingType split = LogSplittingType.OneFile;

        static string sessionStart = DateTime.Now.ToString( LongDateFormat );

        static Logger() {
            // TODO: log splitting
            consoleOptions = new bool[15];
            logFileOptions = new bool[15];
            for( int i = 0; i < consoleOptions.Length; i++ ) {
                consoleOptions[i] = true;
                logFileOptions[i] = true;
            }

            if( !Directory.Exists( "logs" ) ) {
                Directory.CreateDirectory( "logs" );
                if( File.Exists( LogFileName ) ) {
                    File.Move( LogFileName, "logs/_OldLog.log" );
                }
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
            // TODO: move to log
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
            //TODO: check if logging is enabled
            if( type == LogType.FatalError ) {
                LogCrash( message );
            }
            string line = DateTime.Now.ToLongTimeString() + " > " + GetPrefix( type ) + message;
            if( logFileOptions[(int)type] ) {
                try {
                    if( split == LogSplittingType.SplitBySession ) {
                        lock( locker ) {
                            File.AppendAllText( "logs/" + sessionStart + ".log", line + Environment.NewLine );
                        }
                    } else if( split == LogSplittingType.SplitByDay ) {
                        lock( locker ) {
                            File.AppendAllText( "logs/" + DateTime.Now.ToString( ShortDateFormat ) + ".log", line + Environment.NewLine );
                        }
                    } else {
                        lock( locker ) {
                            File.AppendAllText( "logs/" + LogFileName, line + Environment.NewLine );
                        }
                    }
                } catch( Exception ex ) {
                    Server.FireLogEvent( "Logger.Log: " + ex, type );
                }
            }
            if( consoleOptions[(int)type] ) Server.FireLogEvent( line, type );
        }


        public static void LogCrash( string message ) {
            File.AppendAllText( CrashFileName, DateTime.Now.ToString() + Environment.NewLine + message + Environment.NewLine + Environment.NewLine );
        }

        public static string GetPrefix( LogType level ) {
            switch( level ) {
                case LogType.Error:
                    return "ERROR: ";
                case LogType.Warning:
                    return "Warning: ";
                default:
                    return String.Empty;
            }
        }

        static DateTime lastCrashReport;
        static object crashReportLock = new object();
        const int MinCrashReportInterval = 30;
        public static void UploadCrashReport( string message, string assembly, Exception exception ) {
            lock( crashReportLock ) {
                if( DateTime.UtcNow.Subtract( lastCrashReport ).TotalSeconds < MinCrashReportInterval ) {
                    Logger.Log( "Logger.SubmitCrashReport: Could not submit crash report, reports too frequent.", LogType.Warning );
                    return;
                }
                lastCrashReport = DateTime.UtcNow;

                try {
                    StringBuilder sb = new StringBuilder();
                    sb.Append( "version=" ).Append( Server.UrlEncode( Updater.GetVersionString() ) );
                    sb.Append( "&message=" ).Append( Server.UrlEncode( message ) );
                    sb.Append( "&assembly=" ).Append( Server.UrlEncode( assembly ) );
                    sb.Append( "&runtime=" ).Append( Server.UrlEncode( System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion() ) );
                    sb.Append( "&os=" ).Append( Environment.OSVersion.VersionString );
                    if( exception != null ) {
                        sb.Append( "&exceptiontype=" ).Append( Server.UrlEncode( exception.GetType().ToString() ) );
                        sb.Append( "&exceptionmessage=" ).Append( Server.UrlEncode( exception.Message ) );
                        sb.Append( "&exceptionstacktrace=" ).Append( Server.UrlEncode( exception.StackTrace ) );
                    } else {
                        sb.Append( "&exceptiontype=&exceptionmessage=&exceptiontrace=" );
                    }
                    if( File.Exists( Config.ConfigFile ) ) {
                        sb.Append( "&config=" ).Append( Server.UrlEncode( File.ReadAllText( Config.ConfigFile ) ) );
                    } else {
                        sb.Append( "&config=" );
                    }

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
                    Logger.Log( "Crash report submitted.", LogType.SystemActivity );

                } catch( Exception ex ) {
                    Logger.Log( "Logger.SubmitCrashReport: {0}", LogType.Warning, ex.Message );
                }
            }
        }
    }
}