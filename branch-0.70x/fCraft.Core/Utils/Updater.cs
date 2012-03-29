// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Checks for updates, and keeps track of current version/revision. </summary>
    public static class Updater {

        /// <summary> The current release information of this version/revision. </summary>
        public static readonly ReleaseInfo CurrentRelease = new ReleaseInfo(
            700,
            1538,
            new DateTime( 2012, 2, 25, 4, 30, 0, DateTimeKind.Utc ),
            "", "",
            ReleaseFlags.Dev
#if DEBUG
            | ReleaseFlags.Dev
#endif
 );

        /// <summary> User-agent value used for HTTP requests (heartbeat, updater, external IP check, etc). </summary>
        public static string UserAgent { get; set; }

        /// <summary> The latest stable branch/version of fCraft. </summary>
        public const string LatestStable = "0.615_r1444";

        /// <summary> Url to update fCraft from. Use "{0}" as a placeholder for CurrentRelease.Version.Revision </summary>
        public static string UpdateUrl { get; set; }

        /// <summary> Amount of time in milliseconds before the updater will consider the connection dead.
        /// Default: 4000ms </summary>
        public static int UpdateCheckTimeout { get; set; }


        static Updater() {
            UpdateCheckTimeout = 4000;
            UpdateUrl = "http://www.fcraft.net/UpdateCheck.php?r={0}";
            UserAgent = "fCraft " + CurrentRelease.VersionString;
        }


        /// <summary> Checks fCraft.net for updated versions of fCraft. </summary>
        /// <returns></returns>
        public static UpdaterResult CheckForUpdates() {
            UpdaterMode mode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if( mode == UpdaterMode.Disabled ) return UpdaterResult.NoUpdate;

            string url = String.Format( UpdateUrl, CurrentRelease.Version.Build );
            if( !RaiseCheckingForUpdatesEvent( ref url ) ) return UpdaterResult.NoUpdate;

            Logger.Log( LogType.SystemActivity, "Checking for fCraft updates..." );
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( url );

                request.Method = "GET";
                request.UserAgent = "fCraft";
                request.Timeout = UpdateCheckTimeout;
                request.ReadWriteTimeout = UpdateCheckTimeout;
                request.CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );
                request.UserAgent = UserAgent;

                using( WebResponse response = request.GetResponse() ) {
                    using( XmlTextReader reader = new XmlTextReader( response.GetResponseStream() ) ) {
                        XDocument doc = XDocument.Load( reader );
                        XElement root = doc.Root;
                        if( root.Attribute( "result" ).Value == "update" ) {
                            string downloadUrl = root.Attribute( "url" ).Value;
                            List<ReleaseInfo> releases = new List<ReleaseInfo>();
                            foreach( XElement el in root.Elements( "Release" ) ) {
                                releases.Add(
                                    new ReleaseInfo(
                                        Int32.Parse( el.Attribute( "v" ).Value ),
                                        Int32.Parse( el.Attribute( "r" ).Value ),
                                        Int64.Parse( el.Attribute( "date" ).Value ).ToDateTime(),
                                        el.Element( "Summary" ).Value,
                                        el.Element( "ChangeLog" ).Value,
                                        ReleaseInfo.StringToReleaseFlags( el.Attribute( "flags" ).Value )
                                    )
                                );
                            }
                            UpdaterResult result = new UpdaterResult( (releases.Count > 0),
                                                                      new Uri( downloadUrl ),
                                                                      releases.ToArray() );
                            RaiseCheckedForUpdatesEvent( UpdateUrl, result );
                            return result;
                        } else {
                            return UpdaterResult.NoUpdate;
                        }
                    }
                }
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "An error occurred while trying to check for updates: {0}: {1}",
                            ex.GetType(), ex.Message );
                return UpdaterResult.NoUpdate;
            }
        }

        /// <summary> Whether or not the updater should run at shutdown. </summary>
        public static bool RunAtShutdown { get; set; }


        #region Events

        /// <summary> Occurs when fCraft is about to check for updates (cancellable).
        /// The update Url may be overridden. </summary>
        public static event EventHandler<CheckingForUpdatesEventArgs> CheckingForUpdates;


        /// <summary> Occurs when fCraft has just checked for updates. </summary>
        public static event EventHandler<CheckedForUpdatesEventArgs> CheckedForUpdates;


        static bool RaiseCheckingForUpdatesEvent( ref string updateUrl ) {
            var handler = CheckingForUpdates;
            if( handler == null ) return true;
            var e = new CheckingForUpdatesEventArgs( updateUrl );
            handler( null, e );
            updateUrl = e.Url;
            return !e.Cancel;
        }


        static void RaiseCheckedForUpdatesEvent( string url, UpdaterResult result ) {
            var handler = CheckedForUpdates;
            if( handler != null ) handler( null, new CheckedForUpdatesEventArgs( url, result ) );
        }

        #endregion
    }

    /// <summary> Result of an update attempt. </summary>
    public sealed class UpdaterResult {
        public static UpdaterResult NoUpdate {
            get {
                return new UpdaterResult( false, null, new ReleaseInfo[0] );
            }
        }

        internal UpdaterResult( bool updateAvailable, Uri downloadUri, [NotNull] ReleaseInfo[] releases ) {
            if( releases == null ) throw new ArgumentNullException( "releases" );
            UpdateAvailable = updateAvailable;
            DownloadUri = downloadUri;
            History = releases.OrderByDescending( r => r.Version.Build ).ToArray();
            LatestRelease = releases.FirstOrDefault();
        }

        /// <summary> Whether or not an update for fCraft is available for download. </summary>
        public bool UpdateAvailable { get; private set; }
        /// <summary> Url to download the update from. </summary>
        public Uri DownloadUri { get; private set; }
        /// <summary> Array of previous release information. </summary>
        public ReleaseInfo[] History { get; private set; }
        /// <summary> Release information of the lastest release. </summary>
        public ReleaseInfo LatestRelease { get; private set; }
    }

    /// <summary> Used to describe a particular release version of fCraft. Includes date released, version </summary>
    public sealed class ReleaseInfo {
        internal ReleaseInfo( int version, int revision, DateTime releaseDate,
                              string summary, string changeLog, ReleaseFlags releaseType ) {
            Version = new Version( version / 1000, version%1000, revision );
            Date = releaseDate;
            Summary = summary;
            ChangeLog = changeLog.Split( new[] { '\n' } );
            Flags = releaseType;
        }

        /// <summary> Flag collection. </summary>
        public ReleaseFlags Flags { get; private set; }
        
        /// <summary> Flags in this release as a string. </summary>
        public string FlagsString { get { return ReleaseFlagsToString( Flags ); } }

        /// <summary> String array of flags for this release. </summary>
        public string[] FlagsList { get { return ReleaseFlagsToStringArray( Flags ); } }

        /// <summary> Version of the particular release of fCraft. </summary>
        public Version Version { get; private set; }

        /// <summary> Date this version was released. </summary>
        public DateTime Date { get; private set; }

        /// <summary> How long this version has been released for. </summary>
        public TimeSpan Age {
            get {
                return DateTime.UtcNow.Subtract( Date );
            }
        }

        /// <summary> This version as a string. </summary>
        public string VersionString {
            get {
                if( IsFlagged( ReleaseFlags.Unstable ) ) {
                    return Version + "_unstable";
                } else if( IsFlagged( ReleaseFlags.Dev ) ) {
                    return Version + "_dev";
                } else {
                    return Version.ToString();
                }
            }
        }

        /// <summary> Summary of what the release was about. </summary>
        public string Summary { get; private set; }

        /// <summary> List of all the changes from the previous version. </summary>
        public string[] ChangeLog { get; private set; }

        /// <summary> Converts a string to its corresponding release flag. </summary>
        /// <param name="str"> String to convert. </param>
        /// <returns> Release flag. </returns>
        public static ReleaseFlags StringToReleaseFlags( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            ReleaseFlags flags = ReleaseFlags.None;
            for( int i = 0; i < str.Length; i++ ) {
                switch( Char.ToUpper( str[i] ) ) {
                    case 'A':
                        flags |= ReleaseFlags.APIChange;
                        break;
                    case 'B':
                        flags |= ReleaseFlags.Bugfix;
                        break;
                    case 'C':
                        flags |= ReleaseFlags.ConfigFormatChange;
                        break;
                    case 'D':
                        flags |= ReleaseFlags.Dev;
                        break;
                    case 'F':
                        flags |= ReleaseFlags.Feature;
                        break;
                    case 'M':
                        flags |= ReleaseFlags.MapFormatChange;
                        break;
                    case 'P':
                        flags |= ReleaseFlags.PlayerDBFormatChange;
                        break;
                    case 'S':
                        flags |= ReleaseFlags.Security;
                        break;
                    case 'U':
                        flags |= ReleaseFlags.Unstable;
                        break;
                    case 'O':
                        flags |= ReleaseFlags.Optimized;
                        break;
                }
            }
            return flags;
        }

        /// <summary> Converts a release flag to a string. </summary>
        /// <param name="flags"> Release flag to convert. </param>
        /// <returns> Release flag as a string. </returns>
        public static string ReleaseFlagsToString( ReleaseFlags flags ) {
            StringBuilder sb = new StringBuilder();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) sb.Append( 'A' );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) sb.Append( 'B' );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) sb.Append( 'C' );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) sb.Append( 'D' );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) sb.Append( 'F' );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) sb.Append( 'M' );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) sb.Append( 'P' );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) sb.Append( 'S' );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) sb.Append( 'U' );
            if( (flags & ReleaseFlags.Optimized) == ReleaseFlags.Optimized ) sb.Append( 'O' );
            return sb.ToString();
        }

        /// <summary> Converts a release flag collection into a string array. </summary>
        /// <param name="flags"> release flag collection to convert. </param>
        /// <returns> Release flags as a string array. </returns>
        public static string[] ReleaseFlagsToStringArray( ReleaseFlags flags ) {
            List<string> list = new List<string>();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) list.Add( "API Changes" );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) list.Add( "Fixes" );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) list.Add( "Config Changes" );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) list.Add( "Developer" );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) list.Add( "New Features" );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) list.Add( "Map Format Changes" );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) list.Add( "PlayerDB Changes" );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) list.Add( "Security Patch" );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) list.Add( "Unstable" );
            if( (flags & ReleaseFlags.Optimized) == ReleaseFlags.Optimized ) list.Add( "Optimized" );
            return list.ToArray();
        }

        /// <summary> Whether or not this release is flagged with the specified flag. </summary>
        /// <param name="flag"> Flag to check for. </param>
        /// <returns> True if this release contains specified flag, otherwise false. </returns>
        public bool IsFlagged( ReleaseFlags flag ) {
            return (Flags & flag) == flag;
        }
    }


    #region Enums

    /// <summary> Updater behavior. </summary>
    public enum UpdaterMode {
        /// <summary> Does not check for updates. </summary>
        Disabled,

        /// <summary> Checks for updates and notifies of availability (in console/log). </summary>
        Notify,

        /// <summary> Checks for updates, downloads them if available, and prompts to install.
        /// Behavior is frontend-specific: in ServerGUI, a dialog is shown with the list of changes and
        /// options to update immediately or next time. In ServerCLI, asks to type in 'y' to confirm updating
        /// or press any other key to skip. '''Note: Requires user interaction
        /// (if you restart the server remotely while unattended, it may get stuck on this dialog).''' </summary>
        Prompt,

        /// <summary> Checks for updates, automatically downloads and installs the updates, and restarts the server. </summary>
        Auto,
    }


    /// <summary> A list of release flags/attributes.
    /// Use binary flag logic or Release.IsFlagged() to test for flags. </summary>
    [Flags]
    public enum ReleaseFlags {
        /// <summary> Nothing was changed. </summary>
        None = 0,

        /// <summary> The API was notably changed in this release. </summary>
        APIChange = 1,

        /// <summary> Bugs were fixed in this release. </summary>
        Bugfix = 2,

        /// <summary> Config.xml format was changed (and version was incremented) in this release. </summary>
        ConfigFormatChange = 4,

        /// <summary> This is a developer-only release, not to be used on live servers.
        /// Untested/undertested releases are often marked as such. </summary>
        Dev = 8,

        /// <summary> A notable new feature was added in this release. </summary>
        Feature = 16,

        /// <summary> The map format was changed in this release (rare). </summary>
        MapFormatChange = 32,

        /// <summary> The PlayerDB format was changed in this release. </summary>
        PlayerDBFormatChange = 64,

        /// <summary> A security issue was addressed in this release. </summary>
        Security = 128,

        /// <summary> There are known or likely stability issues in this release. </summary>
        Unstable = 256,

        /// <summary> This release contains notable optimizations. </summary>
        Optimized = 512
    }

    #endregion
}


namespace fCraft.Events {
    public sealed class CheckingForUpdatesEventArgs : EventArgs, ICancelableEvent {
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
}