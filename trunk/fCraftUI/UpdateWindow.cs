// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;
using fCraft;

namespace fCraftUI {
    public sealed partial class UpdateWindow : Form {
        UpdaterResult update;
        const string UpdaterFile = "Updater.exe";
        WebClient downloader = new WebClient();
        MainForm parent;
        bool auto;

        public UpdateWindow( UpdaterResult _update, MainForm _parent, bool _auto ) {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            parent = _parent;
            update = _update;
            auto = _auto;
            changelog.Text = update.ChangeLog;
            title.Text = String.Format( "A new version is available: v{0}, released {1:0} day(s) ago.",
                                        update.GetVersionString(),
                                        DateTime.Now.Subtract( update.ReleaseDate ).TotalDays );
            Shown += Download;
        }


        void Download( object caller, EventArgs args ) {
            downloader.DownloadProgressChanged += DownloadProgress;
            downloader.DownloadFileCompleted += DownloadComplete;
            downloader.DownloadFileAsync( new Uri( update.DownloadLink ), UpdaterFile );
        }


        void DownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            progress.Value = e.ProgressPercentage;
            bApply.Text = "Downloading (" + e.ProgressPercentage + "%)";
        }


        void DownloadComplete( object sender, AsyncCompletedEventArgs e ) {
            progress.Value = 100;
            if( e.Cancelled || e.Error != null ) {
                MessageBox.Show( e.Error.ToString(), "Error occured while trying to download" );
            } else if( auto ) {
                bApply_Click( null, null );
            }else{
                bApply.Text = "Apply Update";
                bApply.Enabled = true;
            }
        }


        private void bApply_Click( object sender, EventArgs e ) {
            Process.Start( UpdaterFile, Process.GetCurrentProcess().Id.ToString() );
            Application.Exit();
        }

        private void UpdateWindow_FormClosed( object sender, FormClosedEventArgs e ) {
            if( e.CloseReason != CloseReason.ApplicationExitCall ) {
                parent.StartServer();
            }
        }
    }
}