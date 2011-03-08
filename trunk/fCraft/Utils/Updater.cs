// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;

namespace fCraft {
    public struct UpdaterResult {
        public UpdaterResult( int version ) {
            UpdateAvailable = false;
            DownloadLink = "";
            ChangeLog = "";
            NewVersionNumber = version;
            ReleaseDate = DateTime.MinValue;
        }
        public bool UpdateAvailable;
        public string DownloadLink;
        public string ChangeLog;
        public DateTime ReleaseDate;
        public int NewVersionNumber;

        public string GetVersionString() {
            return Decimal.Divide( NewVersionNumber, 1000 ).ToString( "0.000" );
        }
    }


    /// <summary>
    /// Checks for updates, and keeps track of current version/revision.
    /// </summary>
    public static class Updater {
        public static int Version = 510;
        public static int Revision = 451;
        public static bool IsDev = true,
                           IsBroken = true;
        public static string LatestStable = "0.506_r427";

        public static string UpdateURL { get; set; }

        static Updater(){
            UpdateURL = "http://fcraft.fragmer.net/version.log";
        }


        public static UpdaterResult CheckForUpdates() {
            UpdaterResult result = new UpdaterResult( Version );
            return result;
            // TODO: fix the rest
            AutoUpdaterMode mode = Config.GetEnum<AutoUpdaterMode>( ConfigKey.UpdateMode );
            if( mode == AutoUpdaterMode.Disabled ) return result;

            string Url = UpdateURL;
            if( FireCheckingForUpdatesEvent( ref Url ) ) return result;

            Logger.Log( "Checking for fCraft updates...", LogType.SystemActivity );
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( Url );

                request.Method = "GET";
                request.UserAgent = "fCraft";
                request.Timeout = 6000;
                request.ReadWriteTimeout = 6000;
                request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

                using( WebResponse response = request.GetResponse() ) {
                    using( StreamReader reader = new StreamReader( response.GetResponseStream() ) ) {
                        result.DownloadLink = reader.ReadLine();
                        result.ReleaseDate = DateTime.Parse( reader.ReadLine() );

                        string line = reader.ReadLine();
                        while( !reader.EndOfStream ) {
                            int logVersion = Int32.Parse( line );
                            if( logVersion <= Version ) break;
                            else if( result.NewVersionNumber < logVersion ) result.NewVersionNumber = logVersion;
                            result.ChangeLog += logVersion + ":" + Environment.NewLine;
                            line = reader.ReadLine();
                            while( line.StartsWith( " " ) ) {
                                result.ChangeLog += line + Environment.NewLine;
                                if( reader.EndOfStream ) break;
                                line = reader.ReadLine();
                            }
                            result.ChangeLog += Environment.NewLine;
                        }

                        if( result.NewVersionNumber > Version ) {
                            result.UpdateAvailable = true;
                        }
                    }
                }
                request.Abort();
            } catch( Exception ex ) {
                Logger.Log( "An error occured while trying to check for updates: {0}: {1}", LogType.Error,
                               ex.GetType().ToString(), ex.Message );
            }
            FireCheckedForUpdatesEvent( UpdateURL, result );
            return result;
        }

        public static string GetVersionString() {
            return String.Format( "{0}_r{1}{2}{3}",
                                  Decimal.Divide( Version, 1000 ).ToString( "0.000", CultureInfo.InvariantCulture ),
                                  Revision,
                                  (IsDev ? "_dev" : ""),
                                  (IsBroken ? "_broken" : "") );
        }


        #region Events

        public static event EventHandler<CheckingForUpdatesEventArgs> CheckingForUpdates;


        public static event EventHandler<CheckedForUpdatesEventArgs> CheckedForUpdates;


        public static event EventHandler<BeforeUpdateRestartEventArgs> BeforeUpdateRestart;


        public static event EventHandler AfterUpdateRestart;


        static bool FireCheckingForUpdatesEvent(ref string updateUrl) {
            var h = CheckingForUpdates;
            if( h == null ) return false;
            var e = new CheckingForUpdatesEventArgs( updateUrl );
            h( null,e );
            updateUrl = e.Url;
            return e.Cancel;
        }


        static void FireCheckedForUpdatesEvent( string _url, UpdaterResult _result ) {
            var h = CheckedForUpdates;
            if( h != null ) h( null, new CheckedForUpdatesEventArgs( _url, _result ) );
        }


        static bool FireBeforeUpdateRestartEvent() {
            var h = BeforeUpdateRestart;
            if( h == null ) return false;
            var e = new BeforeUpdateRestartEventArgs();
            h( null, e );
            return e.Cancel;
        }


        static void FireAfterUpdateRestartEvent() {
            var h = AfterUpdateRestart;
            if( h != null ) h( null, EventArgs.Empty );
        }

        #endregion
    }


    public enum AutoUpdaterMode {
        Disabled,
        Notify,
        Prompt,
        Auto,
    }


    #region EventArgs

    public class CheckingForUpdatesEventArgs : EventArgs {
        internal CheckingForUpdatesEventArgs( string _url ) {
            Url = _url;
        }
        public string Url { get; set; }
        public bool Cancel { get; set; }
    }


    public class CheckedForUpdatesEventArgs : EventArgs {
        internal CheckedForUpdatesEventArgs( string _url, UpdaterResult _result ) {
            Url = _url;
            Result = _result;
        }
        public string Url { get; private set; }
        public UpdaterResult Result { get; private set; }
    }


    public class BeforeUpdateRestartEventArgs : EventArgs {
        internal BeforeUpdateRestartEventArgs() {
            Cancel = false;
        }
        public bool Cancel { get; set; }
    }

    #endregion
}