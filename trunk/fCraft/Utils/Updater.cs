// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Cache;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using fCraft.Events;
using System.Text;

namespace fCraft {

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

        public string FlagsString { get { return ReleaseFlagsToString( Flags ); } }

        public string[] FlagsList { get { return ReleaseFlagsToStringArray( Flags ); } }

        public int Version { get; private set; }

        public int Revision { get; private set; }

        public DateTime Date { get; private set; }

        public TimeSpan Age {
            get {
                return DateTime.UtcNow.Subtract( Date );
            }
        }

        public string VersionString {
            get {
                return String.Format( "{0:0.000}_r{1}", Decimal.Divide( Version, 1000 ), Revision );
            }
        }

        public string Summary { get; private set; }

        public string[] ChangeLog { get; private set; }

        public static ReleaseFlags StringToReleaseFlags( string str ) {
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
                }
            }
            return flags;
        }

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
            return sb.ToString();
        }

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
            return list.ToArray();
        }

        public bool IsFlagged( ReleaseFlags flag ) {
            return (Flags & flag) == flag;
        }
    }


    public sealed class UpdaterResult {
        public static UpdaterResult NoUpdate {
            get {
                return new UpdaterResult( false, null, new ReleaseInfo[0] );
            }
        }
        internal UpdaterResult( bool updateAvailable, string downloadUrl, ReleaseInfo[] releases ) {
            UpdateAvailable = updateAvailable;
            DownloadUrl = downloadUrl;
            History = releases.OrderByDescending( r => r.Revision ).ToArray();
            LatestRelease = releases.FirstOrDefault();
        }
        public bool UpdateAvailable { get; private set; }
        public string DownloadUrl { get; private set; }
        public ReleaseInfo[] History { get; private set; }
        public ReleaseInfo LatestRelease { get; private set; }
    }

    [Flags]
    public enum ReleaseFlags {
        None = 0,

        APIChange = 1,
        Bugfix = 2,
        ConfigFormatChange = 4,
        Dev = 8,
        Feature = 16,
        MapFormatChange = 32,
        PlayerDBFormatChange = 64,
        Security = 128,
        Unstable = 256
    }

    /// <summary>
    /// Checks for updates, and keeps track of current version/revision.
    /// </summary>
    public static class Updater {

        public static readonly ReleaseInfo CurrentRelease = new ReleaseInfo(
            510,
            489,
            new DateTime( 2011, 3, 29, 19, 00, 0, DateTimeKind.Utc ),
            "Rewrote the updater, improved support for non-Windows servers, fixed lots of bugs, and optimized.",
@"Feature: The fCraft updater has been rewritten and expanded. It now works under all OSes, and provides more features.
 Feature: fCraft now handles differences between case-sensitive and case-insensitive filesystems much better. /wrename and /wload are also aware of the filesystem.
 Feature: fCraft now checks presence, filename capitalization, and integrity of all map files on the world list at startup.
 Feature: New ConfigTool settings for updater, checkbox for showing ""player connected/left"" messages, and ""/mute"" permission settings.
 Feature: Added ""/credits"" command to ConfigUI.
 Feature: Implemented ""--norestart"" and ""--exitoncrash"" command-line switches for fCraft.
 Feature: fCraft event system is being rewritten with modding support in mind.
 Feature: Improved MCSharp/MCZall/MCLawl map support.
 Fix: Software errors that happen while parsing player and console commands will no longer cause disconnects/crashes.
 Fix: IPBanList will attempt to recover records lost due to a recent regression in 0.505.
 Fix: Fixed /wrename errors in cases where names differ only in capitalization.
 Fix: Fixed ""Copy Existing World"" showing the current world on the list in AddWorldPopup.
 Fix: Fixed ""Chat"" tab preview not updating when settings are reset.
 Fix: Fixed per-world whitelist/blacklist not saving when worlds are edited with AddWorldPopup.
 Fix: Fixed non-verified connections that get kicked from affecting that name's PlayerInfo.
 Fix: Server.GenerateSalt() now uses a cryptographically secure PRNG.
 Fix: Fixed verification problems on servers with HeartbeatEnabled=false.
 Fix: Fixed a wrong message being shown by /rank if the target is already same or higher/lower rank.
 Fix: Fixed /winfo showing incorrect information for last lock/unlock event.
 Fix: Fixed tabbing order on ConfigUI.
 Fix: Improved the way Server.InitLibrary and Logger.Log errors are handled.
 Fix: Added missing validation checks to LoadHeader in several map converters.
 Fix: Fixed players being able to complete a draw command or /zadd, if they were demoted before clicking for the second time.
 Fix: Added missing /paste and /pastenot volume checks.
 Fix: Fixed a bug in /zinfo that showed incorrect creation/editing times.
 Fix: Better error messages for unrecognized block names in command parameters.
 Fix: /restart now works from ConfigUI under Mono (but still broken in fCraftConsole).
 Fix: Fixed handling of command-line arguments surrounded with quotes.
 Fix: Fixed a bug at ConfigTool.ConfigUI.xAllowSecurityCircumvention_CheckedChanged (#3181907, thanks to clhdrums87).
 Fix: Fixed /winfo counting hidden players (#3237877, thanks to Hellenion).
 Fix: Fixed MCSharp map exporter mixing up map dimensions (#3159501, thanks to dogwatch).
 Change: Improved the way fCraft project is built. Now all binaries are copied to /trunk/bin/Release or /trunk/bin/Debug.
 Change: Improved error tolerance of Scheduler.EndShutdown().
 Change: Expanded Logger.CheckForCommonErrors().
 Change: Renamed config keys (old ones can still be loaded): IRCBot to IRCBotEnabled, AutomaticUpdates to UpdaterMode.
 Change: Added /wadd as an alias for /wload.
 Change: Renamed Mcc namespace to fCraft.MapConversion.
 Change: Decreased timeout for fCraft update check.
 Change: Added a confirmation prompt to /rank when adding an unrecognized player name to the database.
 Optimization: Sped up heartbeat sending and world-related operations, increased world saving interval to 90 seconds, reduced per-player idle CPU usage, halved CPU usage of draw commands.",
            ReleaseFlags.APIChange | ReleaseFlags.Bugfix | ReleaseFlags.ConfigFormatChange | ReleaseFlags.Feature
        );

        public const string LatestStable = "0.510_r489";

        public static string UpdateUrl { get; set; }

        static Updater() {
            UpdateCheckTimeout = 3000;
            UpdateUrl = "http://www.fcraft.net/UpdateCheck.php?r={0}";
        }


        public static int UpdateCheckTimeout { get; set; }
        public const string UpdaterFile = "fCraftUpdater.exe";

        public static UpdaterResult CheckForUpdates() {
            // TODO: fix the rest
            UpdaterMode mode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if( mode == UpdaterMode.Disabled ) return UpdaterResult.NoUpdate;

            string url = String.Format( UpdateUrl, CurrentRelease.Revision );
            if( FireCheckingForUpdatesEvent( ref url ) ) return UpdaterResult.NoUpdate;

            Logger.Log( "Checking for fCraft updates...", LogType.SystemActivity );
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( url );

                request.Method = "GET";
                request.UserAgent = "fCraft";
                request.Timeout = UpdateCheckTimeout;
                request.ReadWriteTimeout = UpdateCheckTimeout;
                request.CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );

                using( WebResponse response = request.GetResponse() ) {
                    using( XmlTextReader reader = new XmlTextReader( response.GetResponseStream() ) ) {
                        XDocument doc = XDocument.Load( reader );
                        XElement root = doc.Root;
                        if( root.Attribute( "result" ).Value == "update" ) {
                            string downloadUrl = root.Attribute( "url" ).Value;
                            var releases = new List<ReleaseInfo>();
                            foreach( XElement el in root.Elements( "Release" ) ) {
                                releases.Add( new ReleaseInfo(
                                    Int32.Parse( el.Attribute( "v" ).Value ),
                                    Int32.Parse( el.Attribute( "r" ).Value ),
                                    Server.TimestampToDateTime( Int64.Parse( el.Attribute( "date" ).Value ) ),
                                    el.Element( "Summary" ).Value,
                                    el.Element( "ChangeLog" ).Value,
                                    ReleaseInfo.StringToReleaseFlags( el.Attribute( "flags" ).Value )
                                ) );
                            }
                            UpdaterResult result = new UpdaterResult( (releases.Count > 0), downloadUrl, releases.ToArray() );
                            FireCheckedForUpdatesEvent( UpdateUrl, result );
                            return result;
                        } else {
                            return UpdaterResult.NoUpdate;
                        }
                    }
                }
            } catch( Exception ex ) {
                Logger.Log( "An error occured while trying to check for updates: {0}: {1}", LogType.Error,
                            ex.GetType().ToString(), ex.Message );
                return UpdaterResult.NoUpdate;
            }
        }


        public static bool RunAtShutdown { get; set; }

        #region Events

        public static event EventHandler<CheckingForUpdatesEventArgs> CheckingForUpdates;


        public static event EventHandler<CheckedForUpdatesEventArgs> CheckedForUpdates;


        static bool FireCheckingForUpdatesEvent( ref string updateUrl ) {
            var h = CheckingForUpdates;
            if( h == null ) return false;
            var e = new CheckingForUpdatesEventArgs( updateUrl );
            h( null, e );
            updateUrl = e.Url;
            return e.Cancel;
        }


        static void FireCheckedForUpdatesEvent( string url, UpdaterResult result ) {
            var h = CheckedForUpdates;
            if( h != null ) h( null, new CheckedForUpdatesEventArgs( url, result ) );
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

}
#endregion