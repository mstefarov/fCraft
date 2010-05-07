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

        Chat,
        PrivateChat,
        ClassChat,
        
        ConsoleInput,
        ConsoleOutput,

        IRC,
        Debug
    }


    public class Logger {
        string logFile;
        object locker = new object();
        World world;
        public bool[] consoleOptions = new bool[14];
        public bool[] logFileOptions = new bool[14];


        public Logger( World _world ) {
            world = _world;
        }


        internal void Init( string _logFile ) {
            // TODO: log splitting
            logFile = _logFile;
            if( !File.Exists( logFile ) ) {
                FileStream fs = File.Create( logFile );
                fs.Close();
            }
            Log( "------ Log Starts {0} ({1}) ------", LogType.Debug,
                        DateTime.Now.ToLongDateString(), DateTime.Now.ToShortDateString() );
        }

        public void LogDebug( string message) {
            Log( message, LogType.Debug );
        }

        public void Log( string message, LogType type, params object[] values ) {
            Log( String.Format(message,values), type );
        }


        public void LogConsole( string message ) {
            // TODO: move to log
            string processedMessage = "# ";
            for( int i = 0; i < message.Length; i++ ) {
                if( message[i] == '&' ) i++;
                else processedMessage += message[i];
            }
            Log( processedMessage, LogType.ConsoleOutput );
        }



        public void Log( string message, LogType type ) {
            //TODO: check if logging is enabled
            string line = DateTime.Now.ToLongTimeString() + " > " + GetPrefix( type ) + message;
            if( logFileOptions[(int)type] ) {
                lock( locker ) {
                    try {
                        using( StreamWriter writer = File.AppendText( logFile ) ) {
                            writer.WriteLine( line );
                        }
                    } catch( Exception e ) {
                        Console.WriteLine( e.Message.ToString() );
                    }
                }
            }
            if( world != null && consoleOptions[(int)type] ) world.FireLog( line, type );
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
