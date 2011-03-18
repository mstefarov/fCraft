// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using fCraft;
using fCraft.MapConversion;
using Color = System.Drawing.Color;


namespace ConfigTool {
    sealed partial class AddWorldPopup : Form {
        BackgroundWorker bwLoader = new BackgroundWorker(),
                         bwGenerator = new BackgroundWorker(),
                         bwRenderer = new BackgroundWorker();

        readonly object redrawLock = new object();

        Map _map;
        Map map {
            get {
                return _map;
            }
            set {
                try {
                    bOK.Invoke( (MethodInvoker)delegate {
                        bOK.Enabled = (value != null);
                        lCreateMap.Visible = !bOK.Enabled;
                    } );
                } catch( ObjectDisposedException ) { }
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
            bwGenerator.WorkerReportsProgress = true;
            bwGenerator.ProgressChanged += AsyncGenProgress;
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

            Shown += LoadMap;
        }


        void LoadMap( object sender, EventArgs args ) {

            // Fill in the "Copy existing world" combobox
            foreach( WorldListEntry otherWorld in ConfigUI.worlds ) {
                if( otherWorld != world ) {
                    cWorld.Items.Add( otherWorld.name + " (" + otherWorld.Description + ")" );
                    copyOptionsList.Add( otherWorld );
                }
            }

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
                world = new WorldListEntry( world );
                Text = "Editing World \"" + world.Name + "\"";
                originalWorldName = world.Name;
                tName.Text = world.Name;
                cAccess.SelectedItem = world.AccessPermission;
                cBuild.SelectedItem = world.BuildPermission;
                cBackup.SelectedItem = world.Backup;
                xHidden.Checked = world.Hidden;
            }

            // Disable "copy" tab if there are no other worlds
            if( cWorld.Items.Count > 0 ) {
                cWorld.SelectedIndex = 0;
            } else {
                tabs.TabPages.Remove( tabCopy );
            }

            // Disable "existing map" tab if there are no other worlds
            fileToLoad = Path.Combine( Paths.MapPath, world.Name + ".fcm" );
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
            tStatus2.Text = "";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            bwLoader.RunWorkerAsync();
        }

        private void bBrowseFile_Click( object sender, EventArgs e ) {
            fileBrowser.FileName = tFile.Text;
            if( fileBrowser.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( fileBrowser.FileName ) ) {
                tFolder.Text = "";
                tFile.Text = fileBrowser.FileName;
                tFile.SelectAll();

                fileToLoad = fileBrowser.FileName;
                ShowMapDetails( tLoadFileInfo, fileToLoad );
                StartLoadingMap();
            }
        }

        private void bBrowseFolder_Click( object sender, EventArgs e ) {
            if( folderBrowser.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( folderBrowser.SelectedPath ) ) {
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
            tStatus2.Text = String.Format( "drawn ({0:0.000}s)", stopwatch.Elapsed.TotalSeconds );
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
            tStatus2.Text = "";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;

            Refresh();
            bwGenerator.RunWorkerAsync();
        }

        void AsyncGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            Map generatedMap;
            if( tab == Tabs.Generator ) {
                MapGenerator gen = new MapGenerator( generatorArgs );
                gen.ProgressCallback = delegate( object _sender, ProgressChangedEventArgs args ) {
                    bwGenerator.ReportProgress( args.ProgressPercentage, args.UserState );
                };
                generatedMap = gen.Generate();
            } else {
                generatedMap = new Map( null, Convert.ToInt32( nFlatgrassDimX.Value ), Convert.ToInt32( nFlatgrassDimY.Value ), Convert.ToInt32( nFlatgrassDimH.Value ) );
                MapGenerator.GenerateFlatgrass( generatedMap );
                generatedMap.ResetSpawn();
            }

            if( floodBarrier ) generatedMap.MakeFloodBarrier();
            generatedMap.CalculateShadows();
            map = generatedMap;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }

        void AsyncGenProgress( object sender, ProgressChangedEventArgs e ) {
            progressBar.Value = e.ProgressPercentage;
            tStatus1.Text = (string)e.UserState;
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
                tName.ForeColor = Color.Red;
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
            if( cWorld.SelectedIndex != -1 && File.Exists( Path.Combine( Paths.MapPath, copyOptionsList[cWorld.SelectedIndex].name + ".fcm" ) ) ) {
                bShow.Enabled = false;
                fileToLoad = Path.Combine( Paths.MapPath, copyOptionsList[cWorld.SelectedIndex].name + ".fcm" );
                ShowMapDetails( tCopyInfo, fileToLoad );
                StartLoadingMap();
            }
        }

        private void cWorld_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cWorld.SelectedIndex != -1 ) {
                string fileName = Path.Combine( Paths.MapPath, copyOptionsList[cWorld.SelectedIndex].name + ".fcm" );
                bShow.Enabled = File.Exists( fileName );
                ShowMapDetails( tCopyInfo, fileName );
            }
        }

        private void xAdvanced_CheckedChanged( object sender, EventArgs e ) {
            gTerrainFeatures.Visible = xAdvanced.Checked;
            gHeightmapCreation.Visible = xAdvanced.Checked;
            gTrees.Visible = xAdvanced.Checked && xAddTrees.Checked;
            gCaves.Visible = xCaves.Checked && xAdvanced.Checked;
            gSnow.Visible = xAdvanced.Checked && xAddSnow.Checked;
            gCliffs.Visible = xAdvanced.Checked && xAddCliffs.Checked;
            gBeaches.Visible = xAdvanced.Checked && xAddBeaches.Checked;
        }

        private void MapDimensionChanged( object sender, EventArgs e ) {
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
            xDelayBias.Enabled = useBias;
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
                    fileToLoad = Path.Combine( Paths.MapPath, world.Name + ".fcm" );
                    ShowMapDetails( tExistingMapInfo, fileToLoad );
                    StartLoadingMap();
                    return;
                case Tabs.LoadFile:
                    if( !String.IsNullOrEmpty( tFile.Text ) ) {
                        tFile.SelectAll();
                        fileToLoad = tFile.Text;
                        ShowMapDetails( tLoadFileInfo, fileToLoad );
                        StartLoadingMap();
                    }
                    return;
                case Tabs.CopyWorld:
                    if( cWorld.SelectedIndex != -1 ) {
                        bShow.Enabled = File.Exists( Path.Combine( Paths.MapPath, copyOptionsList[cWorld.SelectedIndex].name + ".fcm" ) );
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


        static void ShowMapDetails( TextBox textBox, string fileName ) {

            DateTime creationTime, modificationTime;
            long fileSize = 0;

            if( File.Exists( fileName ) ) {
                FileInfo existingMapFileInfo = new FileInfo( fileName );
                creationTime = existingMapFileInfo.CreationTime;
                modificationTime = existingMapFileInfo.LastWriteTime;
                fileSize = existingMapFileInfo.Length;
            } else if( Directory.Exists( fileName ) ) {
                DirectoryInfo dirInfo = new DirectoryInfo( fileName );
                creationTime = dirInfo.CreationTime;
                modificationTime = dirInfo.LastWriteTime;
                foreach( FileInfo finfo in dirInfo.GetFiles() ) {
                    fileSize += finfo.Length;
                }
            } else {
                textBox.Text = "File or directory \"" + fileName + "\" does not exist.";
                return;
            }

            MapFormat format = MapUtility.Identify( fileName );
            Map loadedMap = Map.LoadHeaderOnly( fileName );

            if( loadedMap != null ) {
                textBox.Text = String.Format(
@"  Location: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}
Dimensions: {5}×{6}×{7}
    Blocks: {8}",
                fileName,
                format,
                (fileSize / 1024),
                creationTime.ToLongDateString(),
                modificationTime.ToLongDateString(),
                loadedMap.WidthX,
                loadedMap.WidthY,
                loadedMap.Height,
                loadedMap.WidthX * loadedMap.WidthY * loadedMap.Height );
            } else {
                textBox.Text = String.Format(
@"  Location: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}",
                fileName,
                format,
                (fileSize / 1024),
                creationTime.ToLongDateString(),
                modificationTime.ToLongDateString() );
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

                    string newFileName = Path.Combine( Paths.MapPath, world.Name + ".fcm" );
                    map.Save( newFileName );
                    string oldFileName = Path.Combine( Paths.MapPath, originalWorldName + ".fcm" );

                    if( originalWorldName != null && originalWorldName != world.Name && File.Exists( oldFileName ) ) {
                        try {
                            File.Delete( oldFileName );
                        } catch( Exception ex ) {
                            string errorMessage = String.Format( "Renaming the map file failed. Please delete the old file ({0}.fcm) manually.{1}{2}",
                                                                 originalWorldName, Environment.NewLine, ex );
                            MessageBox.Show( errorMessage, "Error renaming the map file" );
                        }
                    }
                }
            }
        }

        SaveFileDialog savePreviewDialog = new SaveFileDialog();
        private void bSavePreview_Click( object sender, EventArgs e ) {
            try {
                using( Image img = (Image)preview.Image.Clone() ) {
                    if( savePreviewDialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( savePreviewDialog.FileName ) ) {
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
            } catch( Exception ex ) {
                MessageBox.Show( "Could not prepare image for saving: " + ex );
            }
        }


        OpenFileDialog browseTemplateDialog = new OpenFileDialog();
        private void bBrowseTemplate_Click( object sender, EventArgs e ) {
            if( browseTemplateDialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( browseTemplateDialog.FileName ) ) {
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

            xLayeredHeightmap.Checked = generatorArgs.layeredHeightmap;
            xMarbledMode.Checked = generatorArgs.marbledHeightmap;
            xMatchWaterCoverage.Checked = generatorArgs.matchWaterCoverage;
            xInvert.Checked = generatorArgs.invertHeightmap;

            nMaxDepth.Value = generatorArgs.maxDepth;
            nMaxHeight.Value = generatorArgs.maxHeight;
            xAddTrees.Checked = generatorArgs.addTrees;
            sRoughness.Value = (int)(generatorArgs.roughness * 100);
            nSeed.Value = generatorArgs.seed;
            xWater.Checked = generatorArgs.addWater;

            if( generatorArgs.useBias ) sBias.Value = (int)(generatorArgs.bias * 100);
            else sBias.Value = 0;
            xDelayBias.Checked = generatorArgs.delayBias;

            sWaterCoverage.Value = (int)(100 * generatorArgs.waterCoverage);
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

            xWaterLevel.Checked = generatorArgs.customWaterLevel;
            nWaterLevel.Maximum = generatorArgs.dimH;
            nWaterLevel.Value = Math.Min( generatorArgs.waterLevel, generatorArgs.dimH );

            xAddSnow.Checked = generatorArgs.addSnow;

            nSnowAltitude.Value = generatorArgs.snowAltitude - (generatorArgs.customWaterLevel ? generatorArgs.waterLevel : generatorArgs.dimH / 2);
            nSnowTransition.Value = generatorArgs.snowTransition;

            xAddCliffs.Checked = generatorArgs.addCliffs;
            sCliffThreshold.Value = (int)(generatorArgs.cliffThreshold * 100);
            xCliffSmoothing.Checked = generatorArgs.cliffSmoothing;

            xAddBeaches.Checked = generatorArgs.addBeaches;
            nBeachExtent.Value = generatorArgs.beachExtent;
            nBeachHeight.Value = generatorArgs.beachHeight;

            sAboveFunc.Value = ExponentToTrackBar( sAboveFunc, generatorArgs.aboveFuncExponent );
            sBelowFunc.Value = ExponentToTrackBar( sBelowFunc, generatorArgs.belowFuncExponent );

            nMaxHeightVariation.Value = generatorArgs.maxHeightVariation;
            nMaxDepthVariation.Value = generatorArgs.maxDepthVariation;
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
                addTrees = xAddTrees.Checked,
                roughness = sRoughness.Value / 100f,
                seed = (int)nSeed.Value,
                theme = (MapGenTheme)cTheme.SelectedIndex,
                treeHeightMax = (int)(nTreeHeight.Value + nTreeHeightVariation.Value),
                treeHeightMin = (int)(nTreeHeight.Value - nTreeHeightVariation.Value),
                treeSpacingMax = (int)(nTreeSpacing.Value + nTreeSpacingVariation.Value),
                treeSpacingMin = (int)(nTreeSpacing.Value - nTreeSpacingVariation.Value),
                useBias = (sBias.Value != 0),
                delayBias = xDelayBias.Checked,
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
                caveSize = sCaveSize.Value / 100f,
                customWaterLevel = xWaterLevel.Checked,
                waterLevel = (int)(xWaterLevel.Checked ? nWaterLevel.Value : nHeight.Value / 2),
                addSnow = xAddSnow.Checked,
                snowTransition = (int)nSnowTransition.Value,
                snowAltitude = (int)(nSnowAltitude.Value + (xWaterLevel.Checked ? nWaterLevel.Value : nHeight.Value / 2)),
                addCliffs = xAddCliffs.Checked,
                cliffThreshold = sCliffThreshold.Value / 100f,
                cliffSmoothing = xCliffSmoothing.Checked,
                addBeaches = xAddBeaches.Checked,
                beachExtent = (int)nBeachExtent.Value,
                beachHeight = (int)nBeachHeight.Value,
                aboveFuncExponent = TrackBarToExponent( sAboveFunc ),
                belowFuncExponent = TrackBarToExponent( sBelowFunc ),
                maxHeightVariation = (int)nMaxHeightVariation.Value,
                maxDepthVariation = (int)nMaxDepthVariation.Value
            };
        }

        SaveFileDialog saveTemplateDialog = new SaveFileDialog();
        private void bSaveTemplate_Click( object sender, EventArgs e ) {
            if( saveTemplateDialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( saveTemplateDialog.FileName ) ) {
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

        private void xWaterLevel_CheckedChanged( object sender, EventArgs e ) {
            nWaterLevel.Enabled = xWaterLevel.Checked;
        }

        private void nHeight_ValueChanged( object sender, EventArgs e ) {
            nWaterLevel.Value = Math.Min( nWaterLevel.Value, nHeight.Value );
            nWaterLevel.Maximum = nHeight.Value;
        }

        private void xAddTrees_CheckedChanged( object sender, EventArgs e ) {
            gTrees.Visible = xAddTrees.Checked;
        }

        private void xWater_CheckedChanged( object sender, EventArgs e ) {
            xAddBeaches.Enabled = xWater.Checked;
        }

        private void sAboveFunc_ValueChanged( object sender, EventArgs e ) {
            lAboveFuncUnits.Text = (1 / TrackBarToExponent( sAboveFunc )).ToString( "0.0%" );
        }

        private void sBelowFunc_ValueChanged( object sender, EventArgs e ) {
            lBelowFuncUnits.Text = (1 / TrackBarToExponent( sBelowFunc )).ToString( "0.0%" );
        }

        static float TrackBarToExponent( TrackBar bar ) {
            if( bar.Value >= bar.Maximum / 2 ) {
                float normalized = (bar.Value - bar.Maximum / 2f) / (bar.Maximum / 2f);
                return 1 + normalized * normalized * 3;
            } else {
                float normalized = (bar.Value / (bar.Maximum / 2f));
                return normalized * .75f + .25f;
            }
        }

        static int ExponentToTrackBar( TrackBar bar, float val ) {
            if( val >= 1 ) {
                float normalized = (float)Math.Sqrt( (val - 1) / 3f );
                return (int)(bar.Maximum / 2 + normalized * (bar.Maximum / 2));
            } else {
                float normalized = (val - .25f) / .75f;
                return (int)(normalized * bar.Maximum / 2f);
            }
        }

        private void sCliffThreshold_ValueChanged( object sender, EventArgs e ) {
            lCliffThresholdUnits.Text = sCliffThreshold.Value + "%";
        }

        private void xAddSnow_CheckedChanged( object sender, EventArgs e ) {
            gSnow.Visible = xAdvanced.Checked && xAddSnow.Checked;
        }

        private void xAddCliffs_CheckedChanged( object sender, EventArgs e ) {
            gCliffs.Visible = xAdvanced.Checked && xAddCliffs.Checked;
        }

        private void xAddBeaches_CheckedChanged( object sender, EventArgs e ) {
            gBeaches.Visible = xAdvanced.Checked && xAddBeaches.Checked;
        }
    }
}