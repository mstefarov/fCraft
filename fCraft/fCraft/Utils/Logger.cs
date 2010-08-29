// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Text.RegularExpressions;


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

        const string LogFileName = "fCraft.log", LongDateFormat = "yyyy'-'MM'-'dd'_'HH'-'mm'-'ss", ShortDateFormat = "yyyy'-'MM'-'dd";
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
                    Server.FireLogEvent( "Logger.Log: "+ex, type );
                }
            }
            if( consoleOptions[(int)type] ) Server.FireLogEvent( line, type );
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
    }
}