// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using fCraft.Events;

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
        public const int Version = 510,
                         Revision = 465;
        public const bool IsDev = true,
                          IsBroken = true;

        public const string LatestStable = "0.506_r427";

        public static string UpdateUrl { get; set; }

        static Updater(){
            UpdateUrl = "http://fcraft.fragmer.net/version.log";
        }


        public static UpdaterResult CheckForUpdates() {
            UpdaterResult result = new UpdaterResult( Version );
            return result;
            // TODO: fix the rest
            UpdaterMode mode = ConfigKey.UpdateMode.GetEnum<UpdaterMode>();
            if( mode == UpdaterMode.Disabled ) return result;

            string url = UpdateUrl;
            if( FireCheckingForUpdatesEvent( ref url ) ) return result;

            Logger.Log( "Checking for fCraft updates...", LogType.SystemActivity );
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( url );

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
            FireCheckedForUpdatesEvent( UpdateUrl, result );
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


        static void FireCheckedForUpdatesEvent( string url, UpdaterResult result ) {
            var h = CheckedForUpdates;
            if( h != null ) h( null, new CheckedForUpdatesEventArgs( url, result ) );
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


    public enum UpdaterMode {
        Disabled,
        Notify,
        Prompt,
        Auto,
    }

}

#region EventArgs
namespace fCraft.Events {

    public sealed class CheckingForUpdatesEventArgs : EventArgs {
        internal CheckingForUpdatesEventArgs( string url ) {
            Url = url;
        }
        public string Url { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class CheckedForUpdatesEventArgs : EventArgs {
        internal CheckedForUpdatesEventArgs( string url, UpdaterResult result ) {
            Url = url;
            Result = result;
        }
        public string Url { get; private set; }
        public UpdaterResult Result { get; private set; }
    }


    public sealed class BeforeUpdateRestartEventArgs : EventArgs {
        internal BeforeUpdateRestartEventArgs() {
            Cancel = false;
        }
        public bool Cancel { get; set; }
    }

}
#endregion