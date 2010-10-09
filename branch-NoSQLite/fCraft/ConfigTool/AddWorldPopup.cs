using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;
using fCraft;


namespace ConfigTool {
    sealed partial class AddWorldPopup : Form {
        BackgroundWorker bwLoader = new BackgroundWorker(),
                         bwGenerator = new BackgroundWorker(),
                         bwRenderer = new BackgroundWorker();
        object redrawLock = new object();

        Map _map;
        Map map {
            get {
                return _map;
            }
            set {
                bOK.Invoke( (MethodInvoker)delegate() {
                    bOK.Enabled = (value != null);
                    lCreateMap.Visible = !bOK.Enabled;
                } );
                _map = value;
            }
        }

        Stopwatch stopwatch;
        int previewRotation;
        Bitmap previewImage;
        bool floodBarrier;
        string originalWorldName;
        internal WorldListEntry world;
        List<WorldListEntry> copyOptionsList = new List<WorldListEntry>();

        Tabs tab;

        public AddWorldPopup( WorldListEntry _world ) {
            InitializeComponent();

            cBackup.Items.AddRange( World.BackupEnum );
            cTemplates.Items.AddRange( Enum.GetNames( typeof( MapGenTemplate ) ) );
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

            nWidthX.Validating += MapDimensionValidating;
            nWidthY.Validating += MapDimensionValidating;
            nHeight.Validating += MapDimensionValidating;

            cAccess.Items.Add( "(everyone)" );
            cBuild.Items.Add( "(everyone)" );
            foreach( Rank rank in RankList.Ranks ) {
                cAccess.Items.Add( rank.ToComboBoxOption() );
                cBuild.Items.Add( rank.ToComboBoxOption() );
            }

            tStatus1.Text = "";
            tStatus2.Text = "";

            world = _world;

            savePreviewDialog.Filter = "PNG Image|*.png|TIFF Image|*.tif;*.tiff|Bitmap Image|*.bmp|JPEG Image|*.jpg;*.jpeg";
            savePreviewDialog.Title = "Saving preview image...";

            browseTemplateDialog.Filter = "MapGenerator Template|*.ftpl";
            browseTemplateDialog.Title = "Opening a MapGenerator template...";

            saveTemplateDialog.Filter = browseTemplateDialog.Filter;
            saveTemplateDialog.Title = "Saving a MapGenerator template...";

            Load += LoadMap;
        }

        void LoadMap( object sender, EventArgs args ) {
            if( world == null ) {
                Text = "Adding a New World";
                world = new WorldListEntry();
                int worldNameCounter = 1;
                for( ; ConfigUI.IsWorldNameTaken( "NewWorld" + worldNameCounter ); worldNameCounter++ ) ;
                world.name = "NewWorld" + worldNameCounter;
                tName.Text = world.Name;
                cAccess.SelectedIndex = 0;
                cBuild.SelectedIndex = 0;
                cBackup.SelectedIndex = 5;
                map = null;
            } else {
                Text = "Editing World \"" + world.Name + "\"";
                world = new WorldListEntry( world );
                originalWorldName = world.Name;
                tName.Text = world.Name;
                cAccess.SelectedItem = world.AccessPermission;
                cBuild.SelectedItem = world.BuildPermission;
                cBackup.SelectedItem = world.Backup;
                xHidden.Checked = world.Hidden;
            }

            // Fill in the "Copy existing world" combobox
            foreach( WorldListEntry otherWorld in ConfigUI.worlds ) {
                if( otherWorld != world ) {
                    cWorld.Items.Add( otherWorld.name + " (" + otherWorld.Description + ")" );
                    copyOptionsList.Add( otherWorld );
                }
            }

            // Disable "copy" tab if there are no other worlds
            if( cWorld.Items.Count > 0 ) {
                cWorld.SelectedIndex = 0;
            } else {
                tabs.TabPages.Remove( tabCopy );
            }

            // Disable "existing map" tab if there are no other worlds
            fileToLoad = "maps/" + world.Name + ".fcm";
            if( File.Exists( fileToLoad ) ) {
                ShowMapDetails( tExistingMapInfo, fileToLoad );
                StartLoadingMap();
            } else {
                tabs.TabPages.Remove( tabExisting );
                tabs.SelectTab( tabLoad );
            }

            // Set Generator comboboxes to defaults
            cTemplates.SelectedIndex = (int)MapGenTemplate.River;

            savePreviewDialog.FileName = world.name;
        }


        #region Loading/Saving

        void StartLoadingMap() {
            map = null;
            tStatus1.Text = "Loading " + new FileInfo( fileToLoad ).Name;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            bwLoader.RunWorkerAsync();
        }

        private void bBrowseFile_Click( object sender, EventArgs e ) {
            fileBrowser.FileName = tFile.Text;
            if( fileBrowser.ShowDialog() == DialogResult.OK && fileBrowser.FileName != "" ) {
                tFolder.Text = "";
                tFile.Text = fileBrowser.FileName;
                tFile.SelectAll();

                fileToLoad = fileBrowser.FileName;
                ShowMapDetails( tLoadFileInfo, fileToLoad );
                StartLoadingMap();
            }
        }

        private void bBrowseFolder_Click( object sender, EventArgs e ) {
            if( folderBrowser.ShowDialog() == DialogResult.OK && folderBrowser.SelectedPath != "" ) {
                tFile.Text = "";
                tFolder.Text = folderBrowser.SelectedPath;
                tFolder.SelectAll();

                fileToLoad = folderBrowser.SelectedPath;
                ShowMapDetails( tLoadFileInfo, fileToLoad );
                StartLoadingMap();
            }
        }

        string fileToLoad;
        void AsyncLoad( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            Map loadedMap = Map.Load( null, fileToLoad );
            if( loadedMap != null ) {
                loadedMap.CalculateShadows();
            }
            map = loadedMap;
        }

        void AsyncLoadCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( map == null ) {
                tStatus1.Text = "Load failed!";
            } else {
                tStatus1.Text = "Load successful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw( true );
            }
            if( tab == Tabs.CopyWorld ) {
                bShow.Enabled = true;
            }
        }
        #endregion Loading

        #region Preview

        IsoCat renderer;

        void Redraw( bool drawAgain ) {
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
                if( drawAgain ) bwRenderer.RunWorkerAsync();
            }
        }

        void AsyncDraw( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
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
            stopwatch.Stop();
            tStatus2.Text = String.Format("drawn ({0:0.000}s)",stopwatch.Elapsed.TotalSeconds);
            if( previewImage != null && previewImage != preview.Image ) {
                Image oldImage = preview.Image;
                if( oldImage != null ) oldImage.Dispose();
                preview.Image = previewImage;
                bSavePreview.Enabled = true;
            }
            progressBar.Visible = false;
        }

        private void bPreviewPrev_Click( object sender, EventArgs e ) {
            if( map == null ) return;
            if( previewRotation == 0 ) previewRotation = 3;
            else previewRotation--;
            tStatus2.Text = ", redrawing...";
            Redraw( true );
        }

        private void bPreviewNext_Click( object sender, EventArgs e ) {
            if( map == null ) return;
            if( previewRotation == 3 ) previewRotation = 0;
            else previewRotation++;
            tStatus2.Text = ", redrawing...";
            Redraw( true );
        }

        #endregion

        #region Generation

        MapGeneratorArgs generatorArgs = new MapGeneratorArgs();

        private void bGenerate_Click( object sender, EventArgs e ) {
            map = null;
            bGenerate.Enabled = false;
            bFlatgrassGenerate.Enabled = false;

            if( tab == Tabs.Generator ) {
                if( !xSeed.Checked ) {
                    nSeed.Value = GetRandomSeed();
                }

                SaveGeneratorArgs();
            }

            tStatus1.Text = "Generating...";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            Refresh();
            bwGenerator.RunWorkerAsync();
        }

        void AsyncGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            Map generatedMap;
            if( tab == Tabs.Generator ) {
                generatedMap = new MapGenerator( generatorArgs ).Generate();
            } else {
                generatedMap = new Map( null, Convert.ToInt32( nFlatgrassDimX.Value ), Convert.ToInt32( nFlatgrassDimY.Value ), Convert.ToInt32( nFlatgrassDimH.Value ) );
                MapGenerator.GenerateFlatgrass( generatedMap );
            }

            if( floodBarrier ) generatedMap.MakeFloodBarrier();
            generatedMap.CalculateShadows();
            map = generatedMap;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }

        void AsyncGenCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( map == null ) {
                tStatus1.Text = "Generation failed!";
            } else {
                tStatus1.Text = "Generation successful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw( true );
            }
            bGenerate.Enabled = true;
            bFlatgrassGenerate.Enabled = true;
        }

        Random rand = new Random();
        int GetRandomSeed() {
            return rand.Next() - rand.Next();
        }

        #endregion

        #region Input Handlers
        private void xFloodBarrier_CheckedChanged( object sender, EventArgs e ) {
            floodBarrier = xFloodBarrier.Checked;
        }

        private void MapDimensionValidating( object sender, CancelEventArgs e ) {
            ((NumericUpDown)sender).Value = Convert.ToInt32( ((NumericUpDown)sender).Value / 16 ) * 16;
        }

        private void tName_Validating( object sender, CancelEventArgs e ) {
            if( Player.IsValidName( tName.Text ) &&
                (!ConfigUI.IsWorldNameTaken( tName.Text ) ||
                (originalWorldName != null && tName.Text.ToLower() == originalWorldName.ToLower())) ) {
                tName.ForeColor = SystemColors.ControlText;
            } else {
                tName.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            }
        }

        private void tName_Validated( object sender, EventArgs e ) {
            world.name = tName.Text;
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

        private void bShow_Click( object sender, EventArgs e ) {
            if( cWorld.SelectedIndex != -1 && File.Exists( "maps/" + copyOptionsList[cWorld.SelectedIndex].name + ".fcm" ) ) {
                bShow.Enabled = false;
                fileToLoad = "maps/" + copyOptionsList[cWorld.SelectedIndex].name + ".fcm";
                ShowMapDetails( tCopyInfo, fileToLoad );
                StartLoadingMap();
            }
        }

        private void cWorld_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cWorld.SelectedIndex != -1 ) {
                string fileName = "maps/" + copyOptionsList[cWorld.SelectedIndex].name + ".fcm";
                bShow.Enabled = File.Exists( fileName );
                ShowMapDetails( tCopyInfo, fileName );
            }
        }

        private void xAdvanced_CheckedChanged( object sender, EventArgs e ) {
            gTerrainFeatures.Visible = xAdvanced.Checked;
            gHeightmapCreation.Visible = xAdvanced.Checked;
            gTrees.Visible = xAdvanced.Checked;
            gCaves.Visible = xCaves.Checked && xAdvanced.Checked;
        }

        private void nWidthX_ValueChanged( object sender, EventArgs e ) {
            sFeatureScale.Maximum = (int)Math.Log( (double)Math.Max( nWidthX.Value, nWidthY.Value ), 2 );
            int value = sDetailScale.Maximum - sDetailScale.Value;
            sDetailScale.Maximum = sFeatureScale.Maximum;
            sDetailScale.Value = sDetailScale.Maximum - value;

            int resolution = 1 << (sDetailScale.Maximum - sDetailScale.Value);
            lDetailSizeDisplay.Text = resolution + "×" + resolution;
            resolution = 1 << (sFeatureScale.Maximum - sFeatureScale.Value);
            lFeatureSizeDisplay.Text = resolution + "×" + resolution;
        }

        private void sFeatureSize_ValueChanged( object sender, EventArgs e ) {
            int resolution = 1 << (sFeatureScale.Maximum - sFeatureScale.Value);
            lFeatureSizeDisplay.Text = resolution + "×" + resolution;
            if( sDetailScale.Value < sFeatureScale.Value ) {
                sDetailScale.Value = sFeatureScale.Value;
            }
        }

        private void sDetailSize_ValueChanged( object sender, EventArgs e ) {
            int resolution = 1 << (sDetailScale.Maximum - sDetailScale.Value);
            lDetailSizeDisplay.Text = resolution + "×" + resolution;
            if( sFeatureScale.Value > sDetailScale.Value ) {
                sFeatureScale.Value = sDetailScale.Value;
            }
        }

        private void xMatchWaterCoverage_CheckedChanged( object sender, EventArgs e ) {
            sWaterCoverage.Enabled = xMatchWaterCoverage.Checked;
        }

        private void sWaterCoverage_ValueChanged( object sender, EventArgs e ) {
            lMatchWaterCoverageDisplay.Text = sWaterCoverage.Value + "%";
        }

        private void sBias_ValueChanged( object sender, EventArgs e ) {
            lBiasDisplay.Text = sBias.Value + "%";
            bool useBias = (sBias.Value != 0);

            nRaisedCorners.Enabled = useBias;
            nLoweredCorners.Enabled = useBias;
            cMidpoint.Enabled = useBias;
        }

        private void sRoughness_ValueChanged( object sender, EventArgs e ) {
            lRoughnessDisplay.Text = sRoughness.Value + "%";
        }

        private void xSeed_CheckedChanged( object sender, EventArgs e ) {
            nSeed.Enabled = xSeed.Checked;
        }

        private void nRaisedCorners_ValueChanged( object sender, EventArgs e ) {
            nLoweredCorners.Value = Math.Min( 4 - nRaisedCorners.Value, nLoweredCorners.Value );
        }

        private void nLoweredCorners_ValueChanged( object sender, EventArgs e ) {
            nRaisedCorners.Value = Math.Min( 4 - nLoweredCorners.Value, nRaisedCorners.Value );
        }
        #endregion

        #region Tabs
        private void tabs_SelectedIndexChanged( object sender, EventArgs e ) {
            if( tabs.SelectedTab == tabExisting ) {
                tab = Tabs.ExistingMap;
            } else if( tabs.SelectedTab == tabLoad ) {
                tab = Tabs.LoadFile;
            } else if( tabs.SelectedTab == tabCopy ) {
                tab = Tabs.CopyWorld;
            } else if( tabs.SelectedTab == tabFlatgrass ) {
                tab = Tabs.Flatgrass;
            } else if( tabs.SelectedTab == tabHeightmap ) {
                tab = Tabs.Heightmap;
            } else {
                tab = Tabs.Generator;
            }

            switch( tab ) {
                case Tabs.ExistingMap:
                    fileToLoad = "maps/" + world.Name + ".fcm";
                    ShowMapDetails( tExistingMapInfo, fileToLoad );
                    StartLoadingMap();
                    return;
                case Tabs.LoadFile:
                    if( tFile.Text != "" ) {
                        tFile.SelectAll();
                        fileToLoad = tFile.Text;
                        ShowMapDetails( tLoadFileInfo, fileToLoad );
                        StartLoadingMap();
                    }
                    return;
                case Tabs.CopyWorld:
                    if( cWorld.SelectedIndex != -1 ) {
                        bShow.Enabled = File.Exists( "maps/" + copyOptionsList[cWorld.SelectedIndex].name + ".fcm" );
                    }
                    return;
                case Tabs.Flatgrass:
                    return;
                case Tabs.Heightmap:
                    return;
                case Tabs.Generator:
                    return;
            }
        }

        enum Tabs {
            ExistingMap,
            LoadFile,
            CopyWorld,
            Flatgrass,
            Heightmap,
            Generator
        }
        #endregion


        void ShowMapDetails( TextBox textBox, string fileName ) {

            Mcc.MapFormat format = Mcc.MapFormat.Unknown;
            try {
                if( File.Exists( fileName ) ) {
                    using( Stream stream = File.OpenRead( fileName ) ) {
                        format = Mcc.MapUtility.Identify( stream, fileName );
                    }
                } else {
                    format = Mcc.MapUtility.Identify( null, fileName );
                }
            } catch( Exception ) { }

            if( File.Exists( fileName ) ) {
                Map loadedMap = Map.LoadHeaderOnly( fileName );
                FileInfo existingMapFileInfo = new FileInfo( fileName );


                if( loadedMap != null ) {
                    textBox.Text = String.Format(
@"      File: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}
Dimensions: {5}×{6}×{7}
    Blocks: {8}",
                    fileName,
                    format,
                    (existingMapFileInfo.Length / 1024),
                    existingMapFileInfo.CreationTime.ToLongDateString(),
                    existingMapFileInfo.LastWriteTime.ToLongDateString(),
                    loadedMap.widthX,
                    loadedMap.widthY,
                    loadedMap.height,
                    loadedMap.widthX * loadedMap.widthY * loadedMap.height );
                } else {
                    textBox.Text = String.Format(
@"      File: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}",
                    fileName,
                    format,
                    (existingMapFileInfo.Length / 1024),
                    existingMapFileInfo.CreationTime.ToLongDateString(),
                    existingMapFileInfo.LastWriteTime.ToLongDateString() );
                }
            } else if( Directory.Exists( fileName ) ) {
                DirectoryInfo dirInfo = new DirectoryInfo( fileName );
                long totalSize = 0;
                foreach( FileInfo finfo in dirInfo.GetFiles() ) {
                    totalSize += finfo.Length;
                }
                textBox.Text = String.Format(
@"    Folder: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}",
                    dirInfo.Name,
                    format,
                    (totalSize / 1024),
                    dirInfo.CreationTime.ToLongDateString(),
                    dirInfo.LastWriteTime.ToLongDateString() );
            } else {
                textBox.Text = "File \"" + fileName + "\" does not exist.";
            }
        }


        private void AddWorldPopup_FormClosing( object sender, FormClosingEventArgs e ) {
            Redraw( false );
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
                    map.Save( "maps/" + world.Name + ".fcm" );
                    string oldFile = "maps/" + originalWorldName + ".fcm";
                    if( originalWorldName != null && originalWorldName != world.Name && File.Exists( oldFile ) ) {
                        try {
                            File.Delete( oldFile );
                        } catch( Exception ex ) {
                            MessageBox.Show( "You can delete the old file (" + oldFile + ") manually. " +
                                "An error occured while trying to delete it automatically: " + Environment.NewLine + ex, "Error" );
                        }
                    }
                }
            }
        }

        SaveFileDialog savePreviewDialog = new SaveFileDialog();
        private void bSavePreview_Click( object sender, EventArgs e ) {
            try{
                using(Image img = (Image)preview.Image.Clone()){
                    if( savePreviewDialog.ShowDialog() == DialogResult.OK && savePreviewDialog.FileName != ""){
                        switch( savePreviewDialog.FilterIndex ) {
                            case 1:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Png ); break;
                            case 2:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Tiff ); break;
                            case 3:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Bmp ); break;
                            case 4:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Jpeg ); break;
                        }
                    }
                }
            }catch(Exception ex){
                MessageBox.Show("Could not prepare image for saving: "+ex);
            }
        }


        OpenFileDialog browseTemplateDialog = new OpenFileDialog();
        private void bBrowseTemplate_Click( object sender, EventArgs e ) {
            if( browseTemplateDialog.ShowDialog() == DialogResult.OK && browseTemplateDialog.FileName != "" ) {
                try {
                    generatorArgs = new MapGeneratorArgs( browseTemplateDialog.FileName );
                    LoadGeneratorArgs();
                    bGenerate.PerformClick();
                } catch( Exception ex ) {
                    MessageBox.Show( "Could not open template file: " + ex );
                }
            }
        }

        void LoadGeneratorArgs() {
            nHeight.Value = generatorArgs.dimH;
            nWidthX.Value = generatorArgs.dimX;
            nWidthY.Value = generatorArgs.dimY;

            sDetailScale.Value = generatorArgs.detailScale;
            sFeatureScale.Value = generatorArgs.featureScale;

            xLayeredHeightmap.Checked=generatorArgs.layeredHeightmap;
            xMarbledMode.Checked = generatorArgs.marbledHeightmap;
            xMatchWaterCoverage.Checked = generatorArgs.matchWaterCoverage;
            xInvert.Checked = generatorArgs.invertHeightmap;

            nMaxDepth.Value = generatorArgs.maxDepth;
            nMaxHeight.Value = generatorArgs.maxHeight;
            xTrees.Checked = generatorArgs.addTrees;
            sRoughness.Value = (int)(generatorArgs.roughness * 100);
            nSeed.Value = generatorArgs.seed;
            xWater.Checked = generatorArgs.addWater;

            if( generatorArgs.useBias ) sBias.Value = (int)(generatorArgs.bias * 100);
            else sBias.Value = 0;

            sWaterCoverage.Value = (int)(100*generatorArgs.waterCoverage);
            cMidpoint.SelectedIndex = generatorArgs.midPoint + 1;
            nRaisedCorners.Value = generatorArgs.raisedCorners;
            nLoweredCorners.Value = generatorArgs.loweredCorners;

            cTheme.SelectedIndex = (int)generatorArgs.theme;
            nTreeHeight.Value = (generatorArgs.treeHeightMax + generatorArgs.treeHeightMin) / 2;
            nTreeHeightVariation.Value = (generatorArgs.treeHeightMax - generatorArgs.treeHeightMin) / 2;
            nTreeSpacing.Value = (generatorArgs.treeSpacingMax + generatorArgs.treeSpacingMin) / 2;
            nTreeSpacingVariation.Value = (generatorArgs.treeSpacingMax - generatorArgs.treeSpacingMin) / 2;

            xCaves.Checked = generatorArgs.addCaves;
            xCaveLava.Checked = generatorArgs.addCaveLava;
            xCaveWater.Checked = generatorArgs.addCaveWater;
            xOre.Checked = generatorArgs.addOre;
            sCaveDensity.Value = (int)(generatorArgs.caveDensity * 100);
            sCaveSize.Value = (int)(generatorArgs.caveSize * 100);
        }

        void SaveGeneratorArgs() {
            generatorArgs = new MapGeneratorArgs {
                detailScale = sDetailScale.Value,
                featureScale = sFeatureScale.Value,
                dimH = (int)nHeight.Value,
                dimX = (int)nWidthX.Value,
                dimY = (int)nWidthY.Value,
                layeredHeightmap = xLayeredHeightmap.Checked,
                marbledHeightmap = xMarbledMode.Checked,
                matchWaterCoverage = xMatchWaterCoverage.Checked,
                maxDepth = (int)nMaxDepth.Value,
                maxHeight = (int)nMaxHeight.Value,
                addTrees = xTrees.Checked,
                roughness = sRoughness.Value / 100f,
                seed = (int)nSeed.Value,
                theme = (MapGenTheme)cTheme.SelectedIndex,
                treeHeightMax = (int)(nTreeHeight.Value + nTreeHeightVariation.Value),
                treeHeightMin = (int)(nTreeHeight.Value - nTreeHeightVariation.Value),
                treeSpacingMax = (int)(nTreeSpacing.Value + nTreeSpacingVariation.Value),
                treeSpacingMin = (int)(nTreeSpacing.Value - nTreeSpacingVariation.Value),
                useBias = (sBias.Value != 0),
                waterCoverage = sWaterCoverage.Value / 100f,
                bias = sBias.Value / 100f,
                midPoint = cMidpoint.SelectedIndex - 1,
                raisedCorners = (int)nRaisedCorners.Value,
                loweredCorners = (int)nLoweredCorners.Value,
                invertHeightmap = xInvert.Checked,
                addWater = xWater.Checked,
                addCaves = xCaves.Checked,
                addOre = xOre.Checked,
                addCaveLava = xCaveLava.Checked,
                addCaveWater = xCaveWater.Checked,
                caveDensity = sCaveDensity.Value / 100f,
                caveSize = sCaveSize.Value / 100f
            };
        }

        SaveFileDialog saveTemplateDialog = new SaveFileDialog();
        private void bSaveTemplate_Click( object sender, EventArgs e ) {
            if( saveTemplateDialog.ShowDialog() == DialogResult.OK && saveTemplateDialog.FileName != "" ) {
                try {
                    SaveGeneratorArgs();
                    generatorArgs.Save( saveTemplateDialog.FileName );
                } catch( Exception ex ) {
                    MessageBox.Show( "Could not open template file: " + ex );
                }
            }
        }

        private void cTemplates_SelectedIndexChanged( object sender, EventArgs e ) {
            generatorArgs = MapGenerator.MakeTemplate( (MapGenTemplate)cTemplates.SelectedIndex );
            LoadGeneratorArgs();
            bGenerate.PerformClick();
        }

        private void xCaves_CheckedChanged( object sender, EventArgs e ) {
            gCaves.Visible = xCaves.Checked && xAdvanced.Checked;
        }

        private void sCaveDensity_ValueChanged( object sender, EventArgs e ) {
            lCaveDensityDisplay.Text = sCaveDensity.Value + "%";
        }

        private void sCaveSize_ValueChanged( object sender, EventArgs e ) {
            lCaveSizeDisplay.Text = sCaveSize.Value + "%";
        }
    }
}