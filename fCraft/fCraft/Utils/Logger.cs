// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Text.RegularExpressions;


namespace fCraft {
    public enum LogType : byte {
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


    public static class Logger {
        static object locker = new object();
        public static bool[] consoleOptions;
        public static bool[] logFileOptions;

        const string LogFileName = "fCraft.log";

        static Logger() {
            // TODO: log splitting
            consoleOptions = new bool[15];
            logFileOptions = new bool[15];
            for( int i = 0; i < consoleOptions.Length; i++ ) {
                consoleOptions[i] = true;
                logFileOptions[i] = true;
            }

            // Mark start of logging
            Log( "------ Log Starts {0} ({1}) ------", LogType.SystemActivity,
DateTime.Now.ToLongDateString(), DateTime.Now.ToShortDateString() );
        }

        public static void Log( string message, LogType type, params object[] values ) {
            Log( String.Format(message,values), type );
        }


        public static void LogConsole( string message ) {
            // TODO: move to log
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
                lock( locker ) {
                    try {
                        File.AppendAllText( LogFileName, line + Environment.NewLine );
                    } catch( Exception e ) {
                        Console.WriteLine( e.Message.ToString() );
                    }
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
