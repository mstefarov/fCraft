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
        public static int Version = 500;
        public static int Revision = 385;
        public static bool IsDev = true,
                           IsBroken = false;
        public static string LatestNonBroken = "0.500_r385_dev";

        const string UpdateURL = "http://fcraft.fragmer.net/version.log";

        public static UpdaterResult CheckForUpdates() {
            UpdaterResult result = new UpdaterResult( Version );
            if( Config.GetString( ConfigKey.AutomaticUpdates ) == "Disabled" ) return result;
            Logger.Log( "Checking for fCraft updates...", LogType.SystemActivity );
            try {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create( UpdateURL );

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
                            result.ChangeLog += logVersion.ToString() + ":" + Environment.NewLine;
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
            return result;
        }

        public static string GetVersionString() {
            return String.Format( "{0:0.000}_r{1}{2}{3}",
                                  Decimal.Divide( Version, 1000 ).ToString( "0.000", CultureInfo.InvariantCulture ),
                                  Revision,
                                  (IsDev ? "_dev" : ""),
                                  (IsBroken ? "_broken" : "") );
        }
    }
}