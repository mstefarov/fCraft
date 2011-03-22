// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;
using fCraft;
using System.Text;

namespace fCraftUI {
    public sealed partial class UpdateWindow : Form {
        UpdaterResult update;
        const string UpdaterFile = "Updater.exe";
        readonly WebClient downloader = new WebClient();
        MainForm parent;
        bool auto;

        public UpdateWindow( UpdaterResult _update, MainForm _parent, bool _auto ) {
            InitializeComponent();
            parent = _parent;
            update = _update;
            auto = _auto;
            CreateDetailedChangeLog();
            lVersion.Text = String.Format( lVersion.Text,
                                           Updater.CurrentRelease.VersionString,
                                           update.LatestRelease.VersionString,
                                           update.LatestRelease.Age.TotalDays );
            //Shown += Download;
        }


        void Download( object caller, EventArgs args ) {
            downloader.DownloadProgressChanged += DownloadProgress;
            downloader.DownloadFileCompleted += DownloadComplete;
            downloader.DownloadFileAsync( new Uri( update.DownloadUrl ), UpdaterFile );
        }


        void DownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            progress.Value = e.ProgressPercentage;
            lProgress.Text = "Downloading (" + e.ProgressPercentage + "%)";
        }


        void DownloadComplete( object sender, AsyncCompletedEventArgs e ) {
            progress.Value = 100;
            if( e.Cancelled || e.Error != null ) {
                MessageBox.Show( e.Error.ToString(), "Error occured while trying to download" );
            } else if( auto ) {
                bApply_Click( null, null );
            } else {
                bUpdateNow.Enabled = true;
            }
        }


        private void bApply_Click( object sender, EventArgs e ) {
            Process.Start( UpdaterFile, Process.GetCurrentProcess().Id.ToString() );
            Application.Exit();
        }

        private void bCancel_Click( object sender, EventArgs e ) {
            Close();
        }

        private void bUpdateNow_Click( object sender, EventArgs e ) {
            Application.Restart();
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