using System;
using System.IO;
using System.Windows.Forms;

// Copyright 2009, 2010 Matvei Stefarov
// me@matvei.org

namespace fCraft {
    enum LogLevel {
        Info,
        Alert,
        Error
    }

    static class Logger {
        private static string logFile;
        private static object locker = new object();
        private static UI form;

        public static void Init( string _logFile, UI _form ) {
            form = _form;
            logFile = _logFile;
            if( !File.Exists( logFile ) ) {
                FileStream fs = File.Create( logFile );
                fs.Close();
            }
        }

        public static void Log( string message ) {
            Log( message, LogLevel.Info );
        }

        public static void LogError( string message ) {
            Log( message, LogLevel.Error );
        }

        public static void LogAlert( string message ) {
            Log( message, LogLevel.Alert );
        }

        public static void Log( string message, LogLevel level ) {
            if( level >= Config.LogThreshold ) {
                string line = DateTime.Now.ToLongTimeString() + " > " + GetPrefix( level ) + message;
                StreamWriter writer = null;
                lock( locker ) {
                    try {
                        form.Log( line );
                        writer = File.AppendText( logFile );
                        writer.WriteLine( DateTime.Now.ToLongTimeString() + " > " + message );
                        writer.Flush();
                    } catch( Exception e ) {
                        Console.WriteLine( e.Message.ToString() );
                    } finally {
                        if( writer != null )
                            writer.Close();
                    }
                }
            }
        }

        public static string GetPrefix( LogLevel level ) {
            switch( level ) {
                case LogLevel.Alert:
                    return "Alert: ";
                case LogLevel.Error:
                    return "ERROR: ";
                default:
                    return String.Empty;
            }
        }
    }
}
