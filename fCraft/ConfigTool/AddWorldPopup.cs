using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;
using fCraft;


namespace ConfigTool {
    public partial class AddWorldPopup : Form {
        BackgroundWorker bwLoader = new BackgroundWorker(),
                         bwGenerator = new BackgroundWorker(),
                         bwRenderer = new BackgroundWorker();
        object loadLock = new object();
        Map map;
        internal WorldListEntry world;
        MapGenType genType;
        Stopwatch stopwatch;
        int previewRotation = 0;
        Bitmap previewImage;


        public AddWorldPopup() {
            InitializeComponent();
            cAccess.SelectedIndex = 0;
            cBuild.SelectedIndex = 0;
            cBackup.SelectedIndex = 0;
            cWorld.SelectedIndex = 0;
            cTerrain.SelectedIndex = 0;
            cTheme.SelectedIndex = 0;

            // this forces calling all the *_CheckedChanged methods, disabling everything unnecessary
            rCopy.Checked = true;
            rEmpty.Checked = true;
            rFlatgrass.Checked = true;
            rTerrain.Checked = true;
            rLoad.Checked = true;

            bwLoader.DoWork += AsyncLoad;
            bwLoader.RunWorkerCompleted += AsyncLoadComplete;

            bwGenerator.DoWork += AsyncGen;
            bwGenerator.RunWorkerCompleted += AsyncGenComplete;

            bwRenderer.DoWork += AsyncDraw;
            bwRenderer.RunWorkerCompleted += AsyncDrawComplete;

            tStatus1.Text = "";
            tStatus2.Text = "";
        }


        #region Input Handlers
        void ToggleDimensions() {
            bool isMapGenerated = (rEmpty.Checked || rFlatgrass.Checked || rTerrain.Checked);
            if( !isMapGenerated ) xFloodBarrier.Checked = false;
            xFloodBarrier.Enabled = isMapGenerated;
            nWidthX.Enabled = isMapGenerated;
            nWidthY.Enabled = isMapGenerated;
            nHeight.Enabled = isMapGenerated;
        }

        private void rLoad_CheckedChanged( object sender, EventArgs e ) {
            tFile.Enabled = rLoad.Checked;
            bBrowse.Enabled = rLoad.Checked;
            ToggleDimensions();
        }

        private void rCopy_CheckedChanged( object sender, EventArgs e ) {
            cWorld.Enabled = rCopy.Checked;
            ToggleDimensions();
        }

        private void rTerrain_CheckedChanged( object sender, EventArgs e ) {
            lTerrain.Enabled = rTerrain.Checked;
            cTerrain.Enabled = rTerrain.Checked;
            lTheme.Enabled = rTerrain.Checked;
            cTheme.Enabled = rTerrain.Checked;
            bGenerate.Enabled = rTerrain.Checked;
        }

        private void bBrowse_Click( object sender, EventArgs e ) {
            gMap.Enabled = false;

            fileBrowser.FileName = tFile.Text;
            fileBrowser.ShowDialog();
            tFile.Text = fileBrowser.FileName;
            tFile.SelectAll();

            tStatus1.Text = "Loading " + new FileInfo( tFile.Text ).Name;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            Refresh();
            bwLoader.RunWorkerAsync();
        }
        #endregion

        void AsyncLoad( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            map = Map.Load( null, tFile.Text );
            stopwatch.Stop();
        }

        void AsyncLoadComplete( object sender, RunWorkerCompletedEventArgs e ) {
            if( map == null ) {
                tStatus1.Text = "Load failed!";
            } else {
                tStatus1.Text = "Load succesful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                nWidthX.Value = map.widthX;
                nWidthY.Value = map.widthY;
                nHeight.Value = map.height;
                tStatus2.Text = ", drawing...";
                bwRenderer.RunWorkerAsync();
            }
            gMap.Enabled = true;
        }

        private void bGenerate_Click( object sender, EventArgs e ) {
            gMap.Enabled = false;

            tStatus1.Text = "Generating...";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            Refresh();
            genType = (MapGenType)cTerrain.SelectedIndex;
            bwGenerator.RunWorkerAsync();
        }

        MapGenerator generator;

        void AsyncGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            map = new Map( null, Convert.ToInt32( nWidthX.Value ), Convert.ToInt32( nWidthY.Value ), Convert.ToInt32( nHeight.Value ) );
            generator = new MapGenerator( map, null, null, genType );
            generator.Generate();
            stopwatch.Stop();
        }

        void AsyncGenComplete( object sender, RunWorkerCompletedEventArgs e ) {
            if( map == null ) {
                tStatus1.Text = "Generation failed!";
            } else {
                tStatus1.Text = "Generation succesful ("+stopwatch.Elapsed.TotalSeconds.ToString("0.000")+"s)";
                tStatus2.Text = ", drawing...";
                bwRenderer.RunWorkerAsync();
            }
            gMap.Enabled = true;
        }

        IsoCat renderer;

        void AsyncDraw( object sender, DoWorkEventArgs e ) {
            renderer = new IsoCat( map, IsoCatMode.Normal, previewRotation );
            Rectangle cropRectangle = new Rectangle();
            Bitmap rawImage = renderer.Draw( ref cropRectangle );
            previewImage = rawImage.Clone( cropRectangle, rawImage.PixelFormat );
        }

        void AsyncDrawComplete( object sender, RunWorkerCompletedEventArgs e ) {
            tStatus2.Text = "";
            preview.Image = previewImage;
            progressBar.Visible = false;
        }

        private void bPreviewPrev_Click( object sender, EventArgs e ) {
            if( previewRotation == 0 ) previewRotation = 3;
            else previewRotation--;
            tStatus2.Text = ", redrawing...";
            progressBar.Visible = true;
            bwRenderer.RunWorkerAsync();
        }

        private void bPreviewNext_Click( object sender, EventArgs e ) {
            if( previewRotation == 3 ) previewRotation = 0;
            else previewRotation++;
            tStatus2.Text = ", redrawing...";
            progressBar.Visible = true;
            bwRenderer.RunWorkerAsync();
        }
    }
}
