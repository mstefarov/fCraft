// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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
            643,
            2212,
            new DateTime( 2013, 9, 20, 8, 0, 0, DateTimeKind.Utc ),
            "",
            "",
            ReleaseFlags.Bugfix | ReleaseFlags.Dev
#if DEBUG
            | ReleaseFlags.Dev
#endif
            );

        /// <summary> User-agent value used for HTTP requests (heartbeat, updater, external IP check, etc). 
        /// Defaults to "fCraft" + VersionString of the current release. </summary>
        public static string UserAgent { get; set; }

        /// <summary> The latest stable branch/version of fCraft. </summary>
        public const string LatestStable = "0.642_r2180";

        /// <summary> Url to update fCraft from. Use "{0}" as a placeholder for CurrentRelease.Version.Revision </summary>
        public static string UpdateUri { get; set; }


        static Updater() {
            UserAgent = "fCraft " + CurrentRelease.VersionString;
            UpdateCheckTimeout = TimeSpan.FromMilliseconds( 4000 );
            UpdateUri = "http://www.fCraft.net/UpdateCheck.php?r={0}";
        }


        /// <summary> Amount of time in milliseconds before the updater will consider the connection dead.
        /// Default: 4000ms </summary>
        public static TimeSpan UpdateCheckTimeout { get; set; }


        /// <summary> Checks fCraft.net for updated versions of fCraft. </summary>
        public static UpdaterResult CheckForUpdates() {
            UpdaterMode mode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if( mode == UpdaterMode.Disabled ) return UpdaterResult.NoUpdate;

            string url = String.Format( UpdateUri, CurrentRelease.Revision );
            if( RaiseCheckingForUpdatesEvent( ref url ) ) return UpdaterResult.NoUpdate;

            Logger.Log( LogType.SystemActivity, "Checking for fCraft updates..." );
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( url );

                request.CachePolicy = Server.CachePolicy;
                request.Method = "GET";
                request.ReadWriteTimeout = (int)UpdateCheckTimeout.TotalMilliseconds;
                request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
                request.Timeout = (int)UpdateCheckTimeout.TotalMilliseconds;
                request.UserAgent = UserAgent;

                using( WebResponse response = request.GetResponse() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( XmlTextReader reader = new XmlTextReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        XDocument doc = XDocument.Load( reader );
                        XElement root = doc.Root;
                        // ReSharper disable PossibleNullReferenceException
                        if( root.Attribute( "result" ).Value == "update" ) {
                            string downloadUrl = root.Attribute( "url" ).Value;
                            var releases = new List<ReleaseInfo>();
                            foreach( XElement el in root.Elements( "Release" ) ) {
                                releases.Add(
                                    new ReleaseInfo(
                                        Int32.Parse( el.Attribute( "v" ).Value ),
                                        Int32.Parse( el.Attribute( "r" ).Value ),
                                        DateTimeUtil.ToDateTime( Int64.Parse( el.Attribute( "date" ).Value ) ),
                                        el.Element( "Summary" ).Value,
                                        el.Element( "ChangeLog" ).Value,
                                        ReleaseInfo.StringToReleaseFlags( el.Attribute( "flags" ).Value )
                                    )
                                );
                            }
                            // ReSharper restore PossibleNullReferenceException
                            UpdaterResult result = new UpdaterResult( ( releases.Count > 0 ), new Uri( downloadUrl ),
                                                                      releases.ToArray() );
                            RaiseCheckedForUpdatesEvent( UpdateUri, result );
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


        /// <summary> Whether or not the update installer (UpdateInstaller.exe) should run at shutdown. </summary>
        public static bool RunAtShutdown { get; set; }


        #region Events

        /// <summary> Occurs when fCraft is about to check for updates (cancelable).
        /// The update Url may be overridden. </summary>
        public static event EventHandler<CheckingForUpdatesEventArgs> CheckingForUpdates;


        /// <summary> Occurs when fCraft has just checked for updates. </summary>
        public static event EventHandler<CheckedForUpdatesEventArgs> CheckedForUpdates;


        static bool RaiseCheckingForUpdatesEvent( ref string updateUrl ) {
            var h = CheckingForUpdates;
            if( h == null ) return false;
            var e = new CheckingForUpdatesEventArgs( updateUrl );
            h( null, e );
            updateUrl = e.Url;
            return e.Cancel;
        }


        static void RaiseCheckedForUpdatesEvent( string url, UpdaterResult result ) {
            var h = CheckedForUpdates;
            if( h != null ) h( null, new CheckedForUpdatesEventArgs( url, result ) );
        }

        #endregion
    }


    /// <summary> Result of an update check. </summary>
    public sealed class UpdaterResult {
        public static UpdaterResult NoUpdate {
            get { return new UpdaterResult( false, null, new ReleaseInfo[0] ); }
        }


        internal UpdaterResult( bool updateAvailable, Uri downloadUri, ReleaseInfo[] releases ) {
            UpdateAvailable = updateAvailable;
            DownloadUri = downloadUri;
            History = releases.OrderByDescending( r => r.Revision ).ToArray();
            LatestRelease = releases.FirstOrDefault();
        }


        public bool UpdateAvailable { get; private set; }
        public Uri DownloadUri { get; private set; }
        public ReleaseInfo[] History { get; private set; }
        public ReleaseInfo LatestRelease { get; private set; }
    }


    /// <summary> Used to describe a particular release version of fCraft. Includes date released, version </summary>
    public sealed class ReleaseInfo {
        internal ReleaseInfo( int version, int revision, DateTime releaseDate,
                              string summary, string changeLog, ReleaseFlags releaseType ) {
            Version = version;
            Revision = revision;
            Date = releaseDate;
            Summary = summary;
            ChangeLog = changeLog.Split( new[] { '\n' } );
            Flags = releaseType;
        }


        public ReleaseFlags Flags { get; private set; }

        public string FlagsString {
            get { return ReleaseFlagsToString( Flags ); }
        }

        public string[] FlagsList {
            get { return ReleaseFlagsToStringArray( Flags ); }
        }

        public int Version { get; private set; }

        public int Revision { get; private set; }

        public DateTime Date { get; private set; }

        public TimeSpan Age {
            get { return DateTime.UtcNow.Subtract( Date ); }
        }

        public string VersionString {
            get {
                string formatString = "{0:0.000}_r{1}";
                if( IsFlagged( ReleaseFlags.Dev ) ) {
                    formatString += "_dev";
                }
                if( IsFlagged( ReleaseFlags.Unstable ) ) {
                    formatString += "_u";
                }
                return String.Format( CultureInfo.InvariantCulture, formatString,
                                      Decimal.Divide( Version, 1000 ),
                                      Revision );
            }
        }

        public string Summary { get; private set; }

        public string[] ChangeLog { get; private set; }


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


        public static string ReleaseFlagsToString( ReleaseFlags flags ) {
            StringBuilder sb = new StringBuilder();
            if( ( flags & ReleaseFlags.APIChange ) == ReleaseFlags.APIChange ) sb.Append( 'A' );
            if( ( flags & ReleaseFlags.Bugfix ) == ReleaseFlags.Bugfix ) sb.Append( 'B' );
            if( ( flags & ReleaseFlags.ConfigFormatChange ) == ReleaseFlags.ConfigFormatChange ) sb.Append( 'C' );
            if( ( flags & ReleaseFlags.Dev ) == ReleaseFlags.Dev ) sb.Append( 'D' );
            if( ( flags & ReleaseFlags.Feature ) == ReleaseFlags.Feature ) sb.Append( 'F' );
            if( ( flags & ReleaseFlags.MapFormatChange ) == ReleaseFlags.MapFormatChange ) sb.Append( 'M' );
            if( ( flags & ReleaseFlags.PlayerDBFormatChange ) == ReleaseFlags.PlayerDBFormatChange ) sb.Append( 'P' );
            if( ( flags & ReleaseFlags.Security ) == ReleaseFlags.Security ) sb.Append( 'S' );
            if( ( flags & ReleaseFlags.Unstable ) == ReleaseFlags.Unstable ) sb.Append( 'U' );
            if( ( flags & ReleaseFlags.Optimized ) == ReleaseFlags.Optimized ) sb.Append( 'O' );
            return sb.ToString();
        }


        public static string[] ReleaseFlagsToStringArray( ReleaseFlags flags ) {
            List<string> list = new List<string>();
            if( ( flags & ReleaseFlags.APIChange ) == ReleaseFlags.APIChange ) list.Add( "API Changes" );
            if( ( flags & ReleaseFlags.Bugfix ) == ReleaseFlags.Bugfix ) list.Add( "Fixes" );
            if( ( flags & ReleaseFlags.ConfigFormatChange ) == ReleaseFlags.ConfigFormatChange )
                list.Add( "Config Changes" );
            if( ( flags & ReleaseFlags.Dev ) == ReleaseFlags.Dev ) list.Add( "Developer" );
            if( ( flags & ReleaseFlags.Feature ) == ReleaseFlags.Feature ) list.Add( "New Features" );
            if( ( flags & ReleaseFlags.MapFormatChange ) == ReleaseFlags.MapFormatChange )
                list.Add( "Map Format Changes" );
            if( ( flags & ReleaseFlags.PlayerDBFormatChange ) == ReleaseFlags.PlayerDBFormatChange )
                list.Add( "PlayerDB Changes" );
            if( ( flags & ReleaseFlags.Security ) == ReleaseFlags.Security ) list.Add( "Security Patch" );
            if( ( flags & ReleaseFlags.Unstable ) == ReleaseFlags.Unstable ) list.Add( "Unstable" );
            if( ( flags & ReleaseFlags.Optimized ) == ReleaseFlags.Optimized ) list.Add( "Optimized" );
            return list.ToArray();
        }


        public bool IsFlagged( ReleaseFlags flag ) {
            return ( Flags & flag ) == flag;
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
    /// Use binary flag logic (value & flag == flag) or Release.IsFlagged() to test for flags. </summary>
    [Flags]
    public enum ReleaseFlags {
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
    /// <summary> Provides data for Updater.CheckingForUpdates event. Allows changing the URL. Cancelable. </summary>
    public sealed class CheckingForUpdatesEventArgs : EventArgs, ICancelableEvent {
        internal CheckingForUpdatesEventArgs( string url ) {
            Url = url;
        }


        public string Url { get; set; }
        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Updater.CheckedForUpdates event. Immutable. </summary>
    public sealed class CheckedForUpdatesEventArgs : EventArgs {
        internal CheckedForUpdatesEventArgs( string url, UpdaterResult result ) {
            Url = url;
            Result = result;
        }


        public string Url { get; private set; }
        public UpdaterResult Result { get; private set; }
    }
}