using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        Map map;
        MapGenType genType;
        MapGenTheme genTheme;
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
            cPreset.Items.AddRange( Enum.GetNames( typeof( MapGenType ) ) );
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
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                cAccess.Items.Add( pc.ToComboBoxOption() );
                cBuild.Items.Add( pc.ToComboBoxOption() );
            }

            tStatus1.Text = "";
            tStatus2.Text = "";

            if( _world == null ) {
                Text = "Adding a New World";
                world = new WorldListEntry();
                int worldNameCounter = 1;
                for( ; ConfigUI.IsWorldNameTaken( "NewWorld" + worldNameCounter ); worldNameCounter++ ) ;
                world.name = "NewWorld" + worldNameCounter;
                tName.Text = world.Name;
                cAccess.SelectedIndex = 0;
                cBuild.SelectedIndex = 0;
                cBackup.SelectedIndex = 5;
            } else {
                Text = "Editing World \"" + _world.Name + "\"";
                world = new WorldListEntry( _world );
                originalWorldName = world.Name;
                tName.Text = world.Name;
                cAccess.SelectedItem = world.AccessPermission;
                cBuild.SelectedItem = world.BuildPermission;
                cBackup.SelectedItem = world.Backup;
                xHidden.Checked = world.Hidden;
            }

            // Fill in the "Copy existing world" combobox
            foreach( WorldListEntry otherWorld in ConfigUI.worlds ) {
                if( otherWorld != _world ) {
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
            }else{
                tabs.TabPages.Remove( tabExisting );
                tabs.SelectTab( tabLoad );
            }

            // Set Generator comboboxes to defaults
            cPreset.SelectedIndex = (int)MapGenType.River;
            cTheme.SelectedIndex = (int)MapGenTheme.Forest;
        }


        #region Loading/Saving

        void StartLoadingMap() {
            bOK.Enabled = false;
            tStatus1.Text = "Loading " + new FileInfo( fileToLoad ).Name;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            bwLoader.RunWorkerAsync();
        }

        private void bBrowse_Click( object sender, EventArgs e ) {
            fileBrowser.FileName = tFile.Text;
            fileBrowser.ShowDialog();

            tFile.Text = fileBrowser.FileName;
            tFile.SelectAll();

            fileToLoad = fileBrowser.FileName;
            ShowMapDetails( tLoadFileInfo, fileToLoad );
            StartLoadingMap();
        }

        string fileToLoad;
        void AsyncLoad( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            map = Map.Load( null, fileToLoad );
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
                Redraw( true );
            }
            if( tab == Tabs.CopyWorld ) {
                bShow.Enabled = true;
            }
            bOK.Enabled = true;
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
            if( previewImage != null && previewImage != preview.Image ) {
                Image oldImage = preview.Image;
                if( oldImage != null ) oldImage.Dispose();
                preview.Image = previewImage;
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

        MapGenerator generator;
        MapGeneratorArgs generatorArgs;

        private void bGenerate_Click( object sender, EventArgs e ) {
            bOK.Enabled = false;
            bGenerate.Enabled = false;
            bFlatgrassGenerate.Enabled = false;

            if( tab == Tabs.Generator ) {
                generatorArgs = new MapGeneratorArgs {
                    cornerBiasMax = sCornerBias.Value + sCornerBiasVariation.Value,
                    cornerBiasMin = sCornerBias.Value - sCornerBiasVariation.Value,
                    detailSize = sDetailSize.Value,
                    layeredHeightmap = xLayeredHeightmap.Checked,
                    marbled = xMarbledMode.Checked,
                    matchWaterCoverage = xMatchWaterCoverage.Checked,
                    maxDepth = (int)nMaxDepth.Value,
                    maxHeight = (int)nMaxHeight.Value,
                    midpointBias = sMidpointBias.Value,
                    placeTrees = xTrees.Checked,
                    roughness = sRoughness.Value,
                    seed = (int)nSeed.Value,
                    theme = (MapGenTheme)cTheme.SelectedIndex,
                    treeHeightMax = (int)(nTreeHeight.Value + nTreeHeightVariation.Value),
                    treeHeightMin = (int)(nTreeHeight.Value - nTreeHeightVariation.Value),
                    treeSpacingMax = (int)(nTreeSpacing.Value + nTreeSpacingVariation.Value),
                    treeSpacingMin = (int)(nTreeSpacing.Value - nTreeSpacingVariation.Value),
                    useBias = !xFullRandom.Checked,
                    waterCoverage = sWaterCoverage.Value
                };
            }

            tStatus1.Text = "Generating...";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            Refresh();
            genType = (MapGenType)cPreset.SelectedIndex;
            genTheme = (MapGenTheme)cTheme.SelectedIndex;
            bwGenerator.RunWorkerAsync();
        }

        void AsyncGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            map = null;
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            if( tab == Tabs.Generator ) {
                map = new Map( null, Convert.ToInt32( nWidthX.Value ), Convert.ToInt32( nWidthY.Value ), Convert.ToInt32( nHeight.Value ) );
            } else if( tab== Tabs.Flatgrass) {
                map = new Map( null, Convert.ToInt32( nFlatgrassDimX.Value ), Convert.ToInt32( nFlatgrassDimY.Value ), Convert.ToInt32( nFlatgrassDimH.Value ) );
            }

            if( tab == Tabs.Generator ) {
                generator = new MapGenerator( map, null, null, genType, genTheme );
                generator.Generate();
                generator = null;
            } else if( tab == Tabs.Flatgrass ) {
                MapGenerator.GenerateFlatgrass( map );
            }

            if( floodBarrier ) map.MakeFloodBarrier();
            map.CalculateShadows();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }

        void AsyncGenCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( map == null ) {
                tStatus1.Text = "Generation failed!";
            } else {
                bOK.Enabled = true;
                tStatus1.Text = "Generation succesful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw( true );
            }
            bGenerate.Enabled = true;
            bFlatgrassGenerate.Enabled = true;
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
            e.Cancel = !Player.IsValidName( tName.Text );
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
        #endregion

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
        }

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
            if( File.Exists( fileName ) ) {
                map = Map.LoadHeaderOnly( fileName );
                if( map != null ) {
                    FileInfo existingMapFileInfo = new FileInfo( fileName );
                    textBox.Text = String.Format(
@"      File: {0}
  Filesize: {1} KB
   Created: {2}
  Modified: {3}
Dimensions: {4}×{5}×{6}
    Blocks: {7}",
                    fileName,
                    (existingMapFileInfo.Length / 1024),
                    existingMapFileInfo.CreationTime.ToLongDateString(),
                    existingMapFileInfo.LastWriteTime.ToLongDateString(),
                    map.widthX,
                    map.widthY,
                    map.height,
                    map.widthX * map.widthY * map.height );
                } else {
                    textBox.Text = "An error occured while trying to load \"" + fileName + "\".";
                }
            } else {
                textBox.Text = "File \"" + fileName + "\" does not exist.";
            }
        }

        private void xFullRandom_CheckedChanged( object sender, EventArgs e ) {
            sMidpointBias.Enabled = !xFullRandom.Checked;
            sCornerBias.Enabled = !xFullRandom.Checked;
            sCornerBiasVariation.Enabled = !xFullRandom.Checked;
            lMidpointBiasDisplay.Enabled = !xFullRandom.Checked;
            lCornerBiasDisplay.Enabled = !xFullRandom.Checked;
            lCornerBiasVariationDisplay.Enabled = !xFullRandom.Checked;
        }

        private void xMatchWaterCoverage_CheckedChanged( object sender, EventArgs e ) {
            sWaterCoverage.Enabled = xMatchWaterCoverage.Checked;
        }
    }
}