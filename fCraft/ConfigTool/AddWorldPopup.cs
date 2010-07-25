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
    partial class AddWorldPopup : Form {
        BackgroundWorker bwLoader = new BackgroundWorker(),
                         bwGenerator = new BackgroundWorker(),
                         bwRenderer = new BackgroundWorker(),
                         bwFlatgrassGen = new BackgroundWorker();
        object redrawLock = new object();
        Map map;
        MapGenType genType;
        MapGenTheme genTheme;
        Stopwatch stopwatch;
        int previewRotation = 0;
        Bitmap previewImage;
        bool floodBarrier = false;

        internal WorldListEntry world;

        public AddWorldPopup( WorldListEntry _world )
            : this() {
            
            if( _world == null ) {
                world = new WorldListEntry();
                int worldNameCounter = 1;
                for( ; ConfigUI.IsWorldNameTaken( "NewWorld" + worldNameCounter ); worldNameCounter++ ) ;
                world.Name = "NewWorld" + worldNameCounter;
                tName.Text = world.Name;
                cAccess.SelectedIndex = 0;
                cBuild.SelectedIndex = 0;
                cBackup.SelectedIndex = 5;
            } else {
                world = new WorldListEntry( _world );
                tName.Text = world.Name;
                cAccess.SelectedItem = world.AccessPermission;
                cBuild.SelectedItem = world.BuildPermission;
                cBackup.SelectedItem = world.Backup;
                xHidden.Checked = world.Hidden;
            }
            cWorld.SelectedIndex = 0;
            cTerrain.SelectedIndex = (int)MapGenType.River;
            cTheme.SelectedIndex = (int)MapGenTheme.Forest;
        }

        public AddWorldPopup() {
            InitializeComponent();

            cBackup.Items.AddRange( ConfigUI.BackupEnum );
            cTerrain.Items.AddRange( Enum.GetNames( typeof( MapGenType ) ) );
            cTheme.Items.AddRange( Enum.GetNames( typeof( MapGenTheme ) ) );

            bwLoader.DoWork += AsyncLoad;
            bwLoader.RunWorkerCompleted += AsyncLoadCompleted;

            bwGenerator.DoWork += AsyncGen;
            bwGenerator.RunWorkerCompleted += AsyncGenCompleted;

            bwRenderer.WorkerReportsProgress = true;
            bwRenderer.WorkerSupportsCancellation = true;
            bwRenderer.DoWork += AsyncDraw;
            bwRenderer.ProgressChanged += AsyncDrawProgress;
            bwRenderer.RunWorkerCompleted += AsyncDrawCompleted;

            bwFlatgrassGen.DoWork += AsyncFlatgrassGen;
            bwFlatgrassGen.RunWorkerCompleted += AsyncFlatgrassGenCompleted;

            nWidthX.Validating += MapDimensionValidating;
            nWidthY.Validating += MapDimensionValidating;
            nHeight.Validating += MapDimensionValidating;

            cAccess.Items.Add( "(everyone)" );
            cBuild.Items.Add( "(everyone)" );
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                cAccess.Items.Add( pc.ToComboBoxOption() );
                cBuild.Items.Add( pc.ToComboBoxOption() );
            }

            tStatus1.Text = "";
            tStatus2.Text = "";
        }


        #region Loading/Saving

        private void rLoad_CheckedChanged( object sender, EventArgs e ) {
            tFile.Enabled = rLoad.Checked;
            bBrowse.Enabled = rLoad.Checked;
            ToggleDimensions();
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

        void AsyncLoad( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            map = Map.Load( null, tFile.Text );
            map.CalculateShadows();
        }

        void AsyncLoadCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( map == null ) {
                tStatus1.Text = "Load failed!";
            } else {
                tStatus1.Text = "Load succesful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                nWidthX.Value = map.widthX;
                nWidthY.Value = map.widthY;
                nHeight.Value = map.height;
                tStatus2.Text = ", drawing...";
                Redraw();
            }
            gMap.Enabled = true;
        }
        #endregion Loading

        #region Preview

        IsoCat renderer;

        void Redraw() {
            lock( redrawLock ) {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                if( bwRenderer.IsBusy ) {
                    bwRenderer.CancelAsync();
                    while( bwRenderer.IsBusy ) {
                        Thread.Sleep( 1 );
                        Application.DoEvents();
                    }
                }
                bwRenderer.RunWorkerAsync();
            }
        }

        void AsyncDraw( object sender, DoWorkEventArgs e ) {
            renderer = new IsoCat( map, IsoCatMode.Normal, previewRotation );
            Rectangle cropRectangle = new Rectangle();
            if( bwRenderer.CancellationPending ) return;
            Bitmap rawImage = renderer.Draw( ref cropRectangle, bwRenderer );
            if( bwRenderer.CancellationPending ) return;
            if( rawImage != null ) {
                previewImage = rawImage.Clone( cropRectangle, rawImage.PixelFormat );
            }
            renderer = null;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }

        void AsyncDrawProgress( object sender, ProgressChangedEventArgs e ) {
            progressBar.Value = e.ProgressPercentage;
        }

        void AsyncDrawCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            tStatus2.Text = "";
            preview.Image = previewImage;
            progressBar.Visible = false;
        }

        private void bPreviewPrev_Click( object sender, EventArgs e ) {
            if( previewRotation == 0 ) previewRotation = 3;
            else previewRotation--;
            tStatus2.Text = ", redrawing...";
            Redraw();
        }

        private void bPreviewNext_Click( object sender, EventArgs e ) {
            if( previewRotation == 3 ) previewRotation = 0;
            else previewRotation++;
            tStatus2.Text = ", redrawing...";
            Redraw();
        }

        #endregion

        #region Generation

        private void rTerrain_CheckedChanged( object sender, EventArgs e ) {
            lTerrain.Enabled = rTerrain.Checked;
            cTerrain.Enabled = rTerrain.Checked;
            lTheme.Enabled = rTerrain.Checked;
            cTheme.Enabled = rTerrain.Checked;
            bGenerate.Enabled = rTerrain.Checked;
        }

        private void bGenerate_Click( object sender, EventArgs e ) {
            gMap.Enabled = false;

            tStatus1.Text = "Generating...";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            Refresh();
            genType = (MapGenType)cTerrain.SelectedIndex;
            genTheme = (MapGenTheme)cTheme.SelectedIndex;
            bwGenerator.RunWorkerAsync();
        }

        MapGenerator generator;

        // terrain
        void AsyncGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            map = null;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            map = new Map( null, Convert.ToInt32( nWidthX.Value ), Convert.ToInt32( nWidthY.Value ), Convert.ToInt32( nHeight.Value ) );
            generator = new MapGenerator( map, null, null, genType, genTheme );
            generator.Generate();
            if( floodBarrier ) map.MakeFloodBarrier();
            map.CalculateShadows();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }

        void AsyncGenCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( map == null ) {
                tStatus1.Text = "Generation failed!";
            } else {
                tStatus1.Text = "Generation succesful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw();
            }
            gMap.Enabled = true;
        }


        // empty (done without BackgroundWorker, it's fast enough)
        private void rEmpty_CheckedChanged( object sender, EventArgs e ) {
            if( rEmpty.Checked ) {
                tStatus1.Text = "Generating...";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                Refresh();
                map = null;
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                map = new Map( null, Convert.ToInt32( nWidthX.Value ), Convert.ToInt32( nWidthY.Value ), Convert.ToInt32( nHeight.Value ) );
                map.MakeFloodBarrier();
                map.shadows = new short[map.widthX, map.widthY]; // skip shadow calculations, there is no overlap
                Redraw();
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                tStatus1.Text = "";
            }
        }


        // flatgrass
        private void rFlatgrass_CheckedChanged( object sender, EventArgs e ) {
            if( rFlatgrass.Checked ) {
                gMap.Enabled = false;

                tStatus1.Text = "Generating...";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;

                Refresh();
                bwFlatgrassGen.RunWorkerAsync();
            }
        }

        void AsyncFlatgrassGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            map = null;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            map = new Map( null, Convert.ToInt32( nWidthX.Value ), Convert.ToInt32( nWidthY.Value ), Convert.ToInt32( nHeight.Value ) );
            MapGenerator.GenerateFlatgrass( map );
            if( floodBarrier ) map.MakeFloodBarrier();
            map.CalculateShadows();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }

        void AsyncFlatgrassGenCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( map == null ) {
                tStatus1.Text = "Generation failed!";
            } else {
                tStatus1.Text = "Generation succesful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw();
            }
            gMap.Enabled = true;
        }

        #endregion

        #region Input Handlers
        void ToggleDimensions() {
            bool isMapGenerated = (rEmpty.Checked || rFlatgrass.Checked || rTerrain.Checked);
            if( !isMapGenerated ) xFloodBarrier.Checked = false;
            xFloodBarrier.Enabled = isMapGenerated;
            nWidthX.Enabled = isMapGenerated;
            nWidthY.Enabled = isMapGenerated;
            nHeight.Enabled = isMapGenerated;
        }

        private void rCopy_CheckedChanged( object sender, EventArgs e ) {
            cWorld.Enabled = rCopy.Checked;
            ToggleDimensions();
        }

        private void xFloodBarrier_CheckedChanged( object sender, EventArgs e ) {
            floodBarrier = xFloodBarrier.Checked;
        }

        private void MapDimensionValidating( object sender, CancelEventArgs e ) {
            ((NumericUpDown)sender).Value = Convert.ToInt32( ((NumericUpDown)sender).Value / 16 ) * 16;
        }

        private void tName_TextChanged( object sender, EventArgs e ) {
            world.Name = tName.Text;
        }

        private void tName_Validating( object sender, CancelEventArgs e ) {
            e.Cancel = !Player.IsValidName( tName.Text );
        }

        private void cAccess_SelectedIndexChanged( object sender, EventArgs e ) {
            world.AccessPermission = cAccess.SelectedItem.ToString();
        }

        private void cBuild_SelectedIndexChanged( object sender, EventArgs e ) {
            world.BuildPermission = cBuild.SelectedItem.ToString();
        }

        private void cBackup_SelectedIndexChanged( object sender, EventArgs e ) {
            world.Backup = cBackup.SelectedItem.ToString();
        }

        private void xHidden_CheckedChanged( object sender, EventArgs e ) {
            world.Hidden = xHidden.Checked;
        }
        #endregion

        private void AddWorldPopup_FormClosing( object sender, FormClosingEventArgs e ) {
            if( DialogResult == DialogResult.OK ) {
                if( map == null ) {
                    e.Cancel = true;
                } else {
                    bwRenderer.CancelAsync();
                    Enabled = false;
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                    tStatus1.Text = "Saving map...";
                    tStatus2.Text = "";
                    Refresh();
                    map.Save( world.Name + ".fcm" );
                }
            }
        }
    }
}