// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;
using fCraft;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace fCraftUI {
    public sealed partial class UpdateWindow : Form {
        readonly UpdaterResult update;
        const string UpdaterFile = "fCraftUpdater.exe";
        readonly string updaterFullPath;
        readonly WebClient downloader = new WebClient();
        readonly bool auto;

        public UpdateWindow( UpdaterResult _update, bool _auto ) {
            InitializeComponent();
            updaterFullPath = Path.Combine( Paths.WorkingPath, UpdaterFile );
            update = _update;
            auto = _auto;
            CreateDetailedChangeLog();
            lVersion.Text = String.Format( lVersion.Text,
                                           Updater.CurrentRelease.VersionString,
                                           update.LatestRelease.VersionString,
                                           update.LatestRelease.Age.TotalDays );
            Shown += Download;
        }


        void Download( object caller, EventArgs args ) {
            xShowDetails.Focus();
            downloader.DownloadProgressChanged += DownloadProgress;
            downloader.DownloadFileCompleted += DownloadComplete;
            downloader.DownloadFileAsync( new Uri( update.DownloadUrl ), updaterFullPath );
        }


        void DownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            Invoke( (Action)delegate {
                progress.Value = e.ProgressPercentage;
                lProgress.Text = "Downloading (" + e.ProgressPercentage + "%)";
            } );
        }


        void DownloadComplete( object sender, AsyncCompletedEventArgs e ) {
            progress.Value = 100;
            if( e.Cancelled || e.Error != null ) {
                MessageBox.Show( e.Error.ToString(), "Error occured while trying to download "+UpdaterFile );
            } else if( auto ) {
                bUpdateNow_Click( null, null );
            } else {
                bUpdateNow.Enabled = true;
            }
        }


        private void bCancel_Click( object sender, EventArgs e ) {
            Close();
        }

        private void bUpdateNow_Click( object sender, EventArgs e ) {
            List<string> argsList = new List<string>( Server.GetArgList() );
            argsList.Add( "\"--restart=fCraftUI.exe\"" );
            switch( Environment.OSVersion.Platform ) {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    argsList.Insert( 0, updaterFullPath );
                    Process.Start( "mono", String.Join( " ", argsList.ToArray() ) + " &" );
                    break;
                default:
                    Process.Start( updaterFullPath, String.Join( " ", argsList.ToArray() ) );
                    break;
            }
            Application.Exit();
        }


        void CreateDetailedChangeLog() {
            StringBuilder sb = new StringBuilder();
            foreach( ReleaseInfo release in update.History ) {
                sb.AppendFormat( "{0} - {1:0} days ago - {2}",
                                 release.VersionString,
                                 release.Age.TotalDays,
                                 String.Join( ", ", release.FlagsList ) );
                sb.AppendLine();
                if( xShowDetails.Checked ) {
                    sb.AppendFormat( "    {0}", String.Join( Environment.NewLine + "    ", release.ChangeLog ) );
                } else {
                    sb.AppendFormat( "    {0}", release.Summary );
                }
                sb.AppendLine().AppendLine();
            }
            tChangeLog.Text = sb.ToString();
        }

        private void xShowDetails_CheckedChanged( object sender, EventArgs e ) {
            CreateDetailedChangeLog();
        }
    }
}