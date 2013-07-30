// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using fCraft.GUI;
using fCraft.MapConversion;
using JetBrains.Annotations;


namespace fCraft.ConfigGUI {
    sealed partial class AddWorldPopup : Form {
        readonly BackgroundWorker bwLoader = new BackgroundWorker(),
                                  bwGenerator = new BackgroundWorker(),
                                  bwRenderer = new BackgroundWorker();

        Stopwatch stopwatch;
        int previewRotation;
        Bitmap previewImage;
        string originalWorldName;
        readonly List<WorldListEntry> copyOptionsList = new List<WorldListEntry>();
        Tabs tab;
        MapGeneratorGui genGui;

        const string MapLoadFilter = "Minecraft Maps|*.fcm;*.lvl;*.dat;*.mclevel;*.gz;*.map;*.meta;*.mine;*.save";

        readonly object redrawLock = new object();


        Map map;

        Map Map {
            get { return map; }
            set {
                try {
                    bOK.Invoke( (MethodInvoker)delegate {
                        try {
                            bOK.Enabled = (value != null);
                            lCreateMap.Visible = !bOK.Enabled;
                        } catch( ObjectDisposedException ) {
                        } catch( InvalidOperationException ) {}
                    } );
                } catch( ObjectDisposedException ) {
                } catch( InvalidOperationException ) {}
                map = value;
            }
        }


        internal WorldListEntry World { get; private set; }


        public AddWorldPopup( WorldListEntry world ) {
            InitializeComponent();

            fileBrowser.Filter = MapLoadFilter;

            cBackup.Items.AddRange( WorldListEntry.BackupEnumNames );

            bwLoader.DoWork += AsyncLoad;
            bwLoader.RunWorkerCompleted += AsyncLoadCompleted;

            bwGenerator.WorkerReportsProgress = true;
            bwGenerator.DoWork += AsyncGen;
            bwGenerator.ProgressChanged += AsyncGenProgress;
            bwGenerator.RunWorkerCompleted += AsyncGenCompleted;

            bwRenderer.WorkerReportsProgress = true;
            bwRenderer.DoWork += AsyncDraw;
            bwRenderer.ProgressChanged += AsyncDrawProgress;
            bwRenderer.RunWorkerCompleted += AsyncDrawCompleted;

            renderer = new IsoCat();
            // event routed through BackgroundWorker to avoid cross-thread invocation issues
            renderer.ProgressChanged +=
                ( progressSender, progressArgs ) =>
                bwRenderer.ReportProgress( progressArgs.ProgressPercentage, progressArgs.UserState );

            nMapWidth.Validating += MapDimensionValidating;
            nMapHeight.Validating += MapDimensionValidating;
            nMapLength.Validating += MapDimensionValidating;

            cAccess.Items.Add( "(everyone)" );
            cBuild.Items.Add( "(everyone)" );
            foreach( Rank rank in RankManager.Ranks ) {
                cAccess.Items.Add( MainForm.ToComboBoxOption( rank ) );
                cBuild.Items.Add( MainForm.ToComboBoxOption( rank ) );
            }

            progressBar.Visible = false;
            tStatus1.Text = "";
            tStatus2.Text = "";

            World = world;
            cPreviewMode.SelectedIndex = 0;

            savePreviewDialog.Filter =
                "PNG Image|*.png|TIFF Image|*.tif;*.tiff|Bitmap Image|*.bmp|JPEG Image|*.jpg;*.jpeg";
            savePreviewDialog.Title = "Saving preview image...";

            cGenerator.Items.AddRange( MapGenUtil.GeneratorList.Select( gen => gen.Name ).ToArray() );
            cGenerator.SelectedIndex = 0;

            tsbLoadPreset.DropDownItemClicked += tsbLoadPreset_DropDownItemClicked;
            tsbCopyGenSettings.DropDownItemClicked += tsbImportSettings_DropDownItemClicked;
            Shown += LoadMap;
        }
        

        WorldListEntry[] otherWorlds;

        void LoadMap( object sender, EventArgs args ) {
            // get the list of existing worlds
            otherWorlds = MainForm.Worlds.Where( w => w != World ).ToArray();

            // Fill in the "Copy existing world" combobox
            foreach( WorldListEntry otherWorld in otherWorlds ) {
                cWorld.Items.Add( otherWorld.Name + " (" + otherWorld.Description + ")" );
                copyOptionsList.Add( otherWorld );
                var item = new ToolStripMenuItem( otherWorld.Name ) {Tag = otherWorld};
                tsbCopyGenSettings.DropDownItems.Insert( 0, item );
            }

            if( World == null ) {
                // initialize defaults for a new world (Adding)
                Text = "Adding a New World";

                // keep trying "NewWorld#" until we find an unused number
                int worldNameCounter = 1;
                while( MainForm.IsWorldNameTaken( "NewWorld" + worldNameCounter ) ) {
                    worldNameCounter++;
                }

                World = new WorldListEntry( "NewWorld" + worldNameCounter );

                tName.Text = World.Name;
                cAccess.SelectedIndex = 0;
                cBuild.SelectedIndex = 0;
                cBackup.SelectedIndex = 0;
                cBlockDB.SelectedIndex = 0; // Auto
                cVisibility.SelectedIndex = 0;
                Map = null;

            } else {
                // Fill in information from an existing world (Editing)
                World = new WorldListEntry( World );
                Text = "Editing World \"" + World.Name + "\"";
                originalWorldName = World.Name;
                tName.Text = World.Name;
                cAccess.SelectedItem = World.AccessPermission;
                cBuild.SelectedItem = World.BuildPermission;
                cBackup.SelectedItem = World.Backup;
                cVisibility.SelectedIndex = (World.Hidden ? 1 : 0);

                switch( World.BlockDBEnabled ) {
                    case YesNoAuto.Auto:
                        cBlockDB.SelectedIndex = 0;
                        break;
                    case YesNoAuto.Yes:
                        cBlockDB.SelectedIndex = 1;
                        break;
                    case YesNoAuto.No:
                        cBlockDB.SelectedIndex = 2;
                        break;
                }
            }

            // Disable "copy" tab if there are no other worlds
            if( cWorld.Items.Count > 0 ) {
                cWorld.SelectedIndex = 0;
            } else {
                tabs.TabPages.Remove( tabCopy );
            }

            // Disable "existing map" tab if map file does not exist
            fileToLoad = World.FullFileName;
            if( File.Exists( fileToLoad ) ) {
                ShowMapDetails( tExistingMapInfo, fileToLoad );
                StartLoadingMap();
            } else {
                tabs.TabPages.Remove( tabExisting );
                tabs.SelectTab( tabLoad );
            }

            savePreviewDialog.FileName = World.Name;
        }


        #region Loading/Saving Map

        string fileToLoad;

        void StartLoadingMap() {
            Map = null;
            tStatus1.Text = "Loading " + new FileInfo( fileToLoad ).Name;
            tStatus2.Text = "";
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            bwLoader.RunWorkerAsync();
        }


        void bBrowseFile_Click( object sender, EventArgs e ) {
            fileBrowser.FileName = tFile.Text;
            if( fileBrowser.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( fileBrowser.FileName ) ) {
                tFolder.Text = "";
                tFile.Text = fileBrowser.FileName;
                tFile.Select( tFile.Text.Length, 0 );

                fileToLoad = fileBrowser.FileName;
                ShowMapDetails( tLoadFileInfo, fileToLoad );
                StartLoadingMap();
                World.MapChangedBy = WorldListEntry.WorldInfoSignature;
                World.MapChangedOn = DateTime.UtcNow;
            }
        }


        void bBrowseFolder_Click( object sender, EventArgs e ) {
            if( folderBrowser.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty( folderBrowser.SelectedPath ) ) {
                tFile.Text = "";
                tFolder.Text = folderBrowser.SelectedPath;
                tFolder.Select( tFolder.Text.Length, 0 );

                fileToLoad = folderBrowser.SelectedPath;
                ShowMapDetails( tLoadFileInfo, fileToLoad );
                StartLoadingMap();
                World.MapChangedBy = WorldListEntry.WorldInfoSignature;
                World.MapChangedOn = DateTime.UtcNow;
            }
        }


        void AsyncLoad( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            try {
                Map = MapUtility.Load( fileToLoad, true );
            } catch( Exception ex ) {
                MessageBox.Show( String.Format( "Could not load specified map: {0}: {1}",
                                                ex.GetType().Name,
                                                ex.Message ) );
            }
        }


        void AsyncLoadCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( Map == null ) {
                tStatus1.Text = "Load failed!";
                ClearPreview();
            } else {
                tStatus1.Text = "Load successful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw( true );
            }
        }

        #endregion Loading


        #region Map Preview

        readonly IsoCat renderer;

        void ClearPreview() {
            string stack = Environment.StackTrace;
            Debug.WriteLine( "ClearPreview() @ " + stack.Substring( 0, stack.IndexOf( "at System.Windows.Forms.Control.WndProc" ) ) );
            renderer.CancelAsync();
            lock( redrawLock ) {
                previewImage = null;
                preview.Image = null;
            }
        }

        void Redraw( bool drawAgain ) {
            lock( redrawLock ) {
                string stack = Environment.StackTrace;
                Debug.WriteLine( "Redraw(" + drawAgain + ") @ " + stack.Substring(0,stack.IndexOf("at System.Windows.Forms.Control.WndProc")) );
                if( map == null ) {
                    ClearPreview();
                    return;
                }
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                if( bwRenderer.IsBusy ) {
                    renderer.CancelAsync();
                    bwRenderer.CancelAsync();
                    while( bwRenderer.IsBusy ) {
                        Thread.Sleep( 1 );
                    }
                }
                if( drawAgain ) {
                    renderer.Rotation = previewRotation;
                    renderer.SeeThroughWater = false;
                    renderer.SeeThroughLava = false;
                    renderer.Mode = IsoCatMode.Normal;
                    switch( cPreviewMode.SelectedIndex ) {
                        case 1:
                            renderer.Mode = IsoCatMode.Cut;
                            break;
                        case 2:
                            renderer.Mode = IsoCatMode.Peeled;
                            break;
                        case 3:
                            renderer.SeeThroughWater = true;
                            break;
                        case 4:
                            renderer.SeeThroughLava = true;
                            break;
                    }
                    bwRenderer.RunWorkerAsync();
                }
            }
        }


        void AsyncDraw( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            renderer.Rotation = previewRotation;

            if( bwRenderer.CancellationPending ) return;

            IsoCatResult result = renderer.Draw( map );
            if( result.Cancelled || bwRenderer.CancellationPending ) return;

            Bitmap rawImage = result.Bitmap;
            if( rawImage != null ) {
                previewImage = rawImage.Clone( result.CropRectangle, rawImage.PixelFormat );
            }
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


        void bPreviewPrev_Click( object sender, EventArgs e ) {
            if( Map == null ) return;
            if( previewRotation == 0 ) previewRotation = 3;
            else previewRotation--;
            tStatus2.Text = ", redrawing...";
            Redraw( true );
        }


        void bPreviewNext_Click( object sender, EventArgs e ) {
            if( Map == null ) return;
            if( previewRotation == 3 ) previewRotation = 0;
            else previewRotation++;
            tStatus2.Text = ", redrawing...";
            Redraw( true );
        }


        void cPreviewMode_SelectedIndexChanged( object sender, EventArgs e ) {
            if( Map == null ) return;
            tStatus2.Text = ", redrawing...";
            Redraw( true );
        }


        readonly SaveFileDialog savePreviewDialog = new SaveFileDialog();

        void bSavePreview_Click( object sender, EventArgs e ) {
            try {
                using( Image img = (Image)preview.Image.Clone() ) {
                    if( savePreviewDialog.ShowDialog() == DialogResult.OK &&
                        !String.IsNullOrEmpty( savePreviewDialog.FileName ) ) {
                        switch( savePreviewDialog.FilterIndex ) {
                            case 1:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Png );
                                break;
                            case 2:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Tiff );
                                break;
                            case 3:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Bmp );
                                break;
                            case 4:
                                img.Save( savePreviewDialog.FileName, ImageFormat.Jpeg );
                                break;
                        }
                    }
                }
            } catch( Exception ex ) {
                MessageBox.Show( "Could not prepare image for saving: " + ex );
            }
        }

        #endregion


        #region Map Generation

        MapGeneratorState genState;

        void bGenerate_Click( object sender, EventArgs e ) {
            if( genState != null ) {
                genState.CancelAsync();
                tStatus1.Text = "Canceling...";
                bGenerate.Enabled = false;
                return;
            }
            Map = null;

            MapGeneratorParameters genParams = genGui.GetParameters();
            genState = genParams.CreateGenerator();

            tStatus1.Text = "Generating...";
            tStatus2.Text = "";
            if( genState.ReportsProgress ) {
                progressBar.Style = ProgressBarStyle.Continuous;
                genState.ProgressChanged +=
                    ( progressSender, progressArgs ) =>
                    bwGenerator.ReportProgress( progressArgs.ProgressPercentage, progressArgs.UserState );
            } else {
                progressBar.Style = ProgressBarStyle.Marquee;
            }
            if( genState.SupportsCancellation ) {
                bGenerate.Text = "Cancel";
            } else {
                bGenerate.Enabled = false;
            }
            progressBar.Value = 0;
            progressBar.Visible = true;
            Refresh();
            bwGenerator.RunWorkerAsync();
            World.MapChangedBy = WorldListEntry.WorldInfoSignature;
            World.MapChangedOn = DateTime.UtcNow;
        }


        void AsyncGen( object sender, DoWorkEventArgs e ) {
            stopwatch = Stopwatch.StartNew();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            Map = genState.Generate();
            if( Map != null ) {
                genState.Parameters.SaveToMap( Map );
            }
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }


        void AsyncGenProgress( object sender, ProgressChangedEventArgs e ) {
            progressBar.Value = e.ProgressPercentage;
            tStatus1.Text = (string)e.UserState;
        }


        void AsyncGenCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( genState.Canceled ) {
                tStatus1.Text = "Generation cancelled!";
                progressBar.Visible = false;
            } else if( Map == null ) {
                tStatus1.Text = "Generation failed!";
                Logger.LogAndReportCrash( "Exception while generating map", "ConfigGUI", e.Error, false );
                progressBar.Visible = false;
            } else {
                tStatus1.Text = "Generation successful (" + stopwatch.Elapsed.TotalSeconds.ToString( "0.000" ) + "s)";
                tStatus2.Text = ", drawing...";
                Redraw( true );
            }
            bGenerate.Enabled = true;
            bGenerate.Text = "Generate";
            genState = null;
        }


        MapGenerator generator;

        void cGenerator_SelectedIndexChanged( object sender, EventArgs e ) {
            string genName = cGenerator.SelectedItem.ToString();
            SelectGenerator( MapGenUtil.GetGeneratorByName( genName ) );
            bGenerate.PerformClick();
        }


        void SelectGenerator( MapGenerator newGen ) {
            int genIndex = cGenerator.Items.IndexOf( newGen.Name );
            if( cGenerator.SelectedIndex != genIndex ) {
                cGenerator.SelectedIndex = genIndex;
                return;
            }

            generatorParamsPanel.SuspendLayout();
            if( genGui != null ) {
                generatorParamsPanel.Controls.Clear();
                genGui.Dispose();
                genGui = null;
            }

            generator = newGen;
            genGui = MapGenGuiUtil.GetGuiForGenerator( newGen ).CreateGui();

            genGui.Width = generatorParamsPanel.Width;
            generatorParamsPanel.Controls.Add( genGui );
            SetGenParams( generator.CreateDefaultParameters() );
            generatorParamsPanel.ResumeLayout();
            generatorParamsPanel.PerformLayout();

            // clear existing presets
            for( int i = tsbLoadPreset.DropDownItems.Count; i > 4; i-- ) {
                var item = tsbLoadPreset.DropDownItems[0];
                tsbLoadPreset.DropDownItems.RemoveAt( 0 );
                item.Dispose();
            }

            // add new presets
            tsbDefaultPreset.Text = generator.Presets[0];
            foreach( string presetName in generator.Presets.Skip( 1 ) ) {
                tsbLoadPreset.DropDownItems.Insert( 0, new ToolStripMenuItem( presetName ) );
            }
        }


        void SetGenParams( [NotNull] MapGeneratorParameters genParams ) {
            if( genParams == null ) {
                throw new ArgumentNullException( "genParams" );
            }
            genGui.SetParameters( genParams );
            genGui.OnMapDimensionChange( (int)nMapWidth.Value, (int)nMapLength.Value, (int)nMapHeight.Value );
        }

        #endregion


        #region Input Handlers

        void MapDimensionValidating( object sender, CancelEventArgs e ) {
            ((NumericUpDown)sender).Value = Convert.ToInt32( ((NumericUpDown)sender).Value/16 )*16;
            genGui.OnMapDimensionChange( (int)nMapWidth.Value, (int)nMapLength.Value, (int)nMapHeight.Value );
        }


        void tName_Validating( object sender, CancelEventArgs e ) {
            if( fCraft.World.IsValidName( tName.Text ) &&
                (!MainForm.IsWorldNameTaken( tName.Text ) ||
                 (originalWorldName != null && tName.Text.ToLower() == originalWorldName.ToLower())) ) {
                tName.ForeColor = SystemColors.ControlText;
            } else {
                tName.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            }
        }


        void tName_Validated( object sender, EventArgs e ) {
            World.Name = tName.Text;
        }


        void cAccess_SelectedIndexChanged( object sender, EventArgs e ) {
            World.AccessPermission = cAccess.SelectedItem.ToString();
        }


        void cBuild_SelectedIndexChanged( object sender, EventArgs e ) {
            World.BuildPermission = cBuild.SelectedItem.ToString();
        }


        void cBackup_SelectedIndexChanged( object sender, EventArgs e ) {
            World.Backup = cBackup.SelectedItem.ToString();
        }


        void cWorld_SelectedIndexChanged( object sender, EventArgs e ) {
            if( tabs.SelectedTab != tabCopy ) return;
            if( cWorld.SelectedIndex != -1 ) {
                string fileName = copyOptionsList[cWorld.SelectedIndex].FullFileName;
                if( File.Exists( fileName ) ) {
                    fileToLoad = fileName;
                    ShowMapDetails( tCopyInfo, fileToLoad );
                    StartLoadingMap();
                } else {
                    Map = null;
                    tCopyInfo.Text = "Map file not found: " + fileName;
                    ClearPreview();
                }
            } else {
                Map = null;
                tCopyInfo.Text = "There are no worlds to copy maps from.";
                ClearPreview();
            }
        }


        void cBlockDB_SelectedIndexChanged( object sender, EventArgs e ) {
            switch( cBlockDB.SelectedIndex ) {
                case 0:
                    World.BlockDBEnabled = YesNoAuto.Auto;
                    break;
                case 1:
                    World.BlockDBEnabled = YesNoAuto.Yes;
                    break;
                case 2:
                    World.BlockDBEnabled = YesNoAuto.No;
                    break;
            }
        }


        void cVisibility_SelectedIndexChanged( object sender, EventArgs e ) {
            switch( cVisibility.SelectedIndex ) {
                case 0:
                    World.Hidden = false;
                    break;
                case 1:
                    World.Hidden = true;
                    break;
            }
        }

        #endregion


        #region Generator Presets

        SaveFileDialog savePresetDialog;

        void tsbSavePreset_Click( object sender, EventArgs e ) {
            var genParams = genGui.GetParameters();
            if( savePresetDialog == null ) {
                savePresetDialog = new SaveFileDialog {
                    Filter = "fCraft MapGen Preset|*.fmgp|" +
                             "All files|*.*",
                    InitialDirectory = Paths.MapPath
                };
            }
            savePresetDialog.FileName = genParams.Generator.Name + "_preset.fmgp";
            if( savePresetDialog.ShowDialog() == DialogResult.OK ) {
                XElement root = new XElement( "fCraftMapGenPreset" );
                root.Add( new XElement( "Generator", genParams.Generator.Name ) );
                root.Add( new XElement( "Version", genParams.Generator.Version ) );
                XElement genParamsEl = new XElement( "Parameters" );
                genParams.Save( genParamsEl );
                root.Add( genParamsEl );
                root.Save( savePresetDialog.FileName );
            }
        }


        OpenFileDialog importSettingsDialog;

        void ImportSettingsFromFile() {
            if( importSettingsDialog == null ) {
                importSettingsDialog = new OpenFileDialog {
                    Filter = "All supported formats|*.fcm;*.ftpl|" +
                             "fCraft Map|*.fcm|" +
                             "fCraft Map Template (Legacy)|*.ftpl|" +
                             "All files|*.*",
                    InitialDirectory = Paths.MapPath
                };
            }
            if( importSettingsDialog.ShowDialog() == DialogResult.OK ) {
                string fullFileName = importSettingsDialog.FileName;
                string fileName = Path.GetFileName( fullFileName );
                if( fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                    Map ourMap;
                    if( MapUtility.TryLoadHeader( fullFileName, false, out ourMap ) ) {
                        MapGenParamsFromMap( fileName, ourMap );
                    } else {
                        MessageBox.Show( "Could not load map file!" );
                    }

                } else if( fileName.EndsWith( ".ftpl", StringComparison.OrdinalIgnoreCase ) ) {
                    XDocument doc = XDocument.Load( fullFileName );
                    XElement root = doc.Root;
                    // TODO: legacy templates
                    MessageBox.Show( LegacyTemplateMessage );

                } else {
                    MessageBox.Show( "Unrecognized file: \"" + fileName + "\"" );
                }
            }
        }


        void tsbImportSettings_DropDownItemClicked( object sender, ToolStripItemClickedEventArgs e ) {
            WorldListEntry entry = e.ClickedItem.Tag as WorldListEntry;
            if( entry == null ) {
                BeginInvoke( (Action)ImportSettingsFromFile ); // allow menu to close
                return;
            }

            Map ourMap;
            if( MapUtility.TryLoadHeader( entry.FullFileName, false, out ourMap ) ) {
                MapGenParamsFromMap( entry.FileName, ourMap );
            } else {
                MessageBox.Show( "Could not load map file!" );
            }
        }


        const string LegacyTemplateMessage = "This version of fCraft does not [yet] support loading legacy .ftpl files.";

        void MapGenParamsFromMap( string fileName, Map ourMap ) {
            tStatus2.Text = "";

            string oldData;
            if( ourMap.Metadata.TryGetValue( "_Origin", "GeneratorParams", out oldData ) ) {
                // load legacy (pre-0.640) embedded generation parameters
                // TODO: legacy templates
                MessageBox.Show( LegacyTemplateMessage );

            } else {
                // load modern (0.640+) embedded generation parameters
                try {
                    MapGeneratorParameters genParams = MapGenUtil.LoadParamsFromMap( ourMap );
                    if( genParams == null ) {
                        tStatus1.Text = "No generation parameters found in " + fileName;
                        MessageBox.Show(
                            "No embedded map generation parameters found in " + fileName,
                            "No generation parameters found",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning );
                        return;
                    }
                    SelectGenerator( genParams.Generator );
                    SetGenParams( genParams );
                    tStatus1.Text = "Imported map generation from " + fileName;

                } catch( MapGenUtil.UnknownMapGeneratorException ex ) {
                    tStatus1.Text = "No matching generator found for " + fileName;
                    MessageBox.Show( "Could not find a matching map generator for \"" + ex.GeneratorName + "\"",
                                     "Missing map generator",
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Warning );

                } catch( Exception ex ) {
                    tStatus1.Text = "Error loading parameters from " + fileName;
                    MessageBox.Show( ex.GetType().Name + Environment.NewLine + ex.Message,
                                     "Error loading parameters from " + fileName,
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Warning );
                }
            }
        }


        void tsbLoadPreset_DropDownItemClicked( object sender, ToolStripItemClickedEventArgs e ) {
            if( e.ClickedItem == tsbLoadPresetFromFile ) {
                BeginInvoke( (Action)LoadPresetFromFile ); // allow menu to close

            } else if( e.ClickedItem == tsbDefaultPreset ) {
                SetGenParams( generator.CreateDefaultParameters() );
                SetStatus( "Default preset applied." );
                bGenerate.PerformClick();

            } else if( e.ClickedItem is ToolStripSeparator ) {
                BeginInvoke( (Action)delegate { tsbLoadPreset.DropDown.AutoClose = true; } );
                tsbLoadPreset.DropDown.AutoClose = false;

            } else {
                try {
                    string presetName = e.ClickedItem.Text;
                    MapGeneratorParameters genParams = generator.CreateParameters( presetName );
                    if( genParams == null ) {
                        ShowPresetLoadError( "Preset \"{0}\" was not recognized by {1} map generator.", presetName, generator.Name );
                    } else {
                        SetGenParams( genParams );
                        SetStatus( "Preset \"{0}\" applied.", presetName );
                        bGenerate.PerformClick();
                    }

                } catch( Exception ex ) {
                    ShowPresetLoadError( ex.GetType().Name + Environment.NewLine + ex );
                }
            }
        }

        [StringFormatMethod( "message" )]
        void ShowPresetLoadError( string message, params object[] formatParams ) {
            MessageBox.Show( String.Format( message, formatParams ),
                             "Error loading preset",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Error );
        }

        [StringFormatMethod( "message" )]
        void SetStatus( string message, params object[] formatParams ) {
            tStatus1.Text = "";
            tStatus2.Text = String.Format( message, formatParams );
            tStatus2.Visible = true;
            progressBar.Visible = false;
        }


        OpenFileDialog loadPresetDialog;

        void LoadPresetFromFile() {
            if( loadPresetDialog == null ) {
                loadPresetDialog = new OpenFileDialog {
                    Filter = "fCraft MapGen Preset|*.fmgp|" +
                             "All files|*.*",
                    InitialDirectory = Paths.MapPath
                };
            }
            if( loadPresetDialog.ShowDialog() != DialogResult.OK ) {
                return;
            }
            string fullFileName = loadPresetDialog.FileName;
            XDocument doc = XDocument.Load( fullFileName );
            XElement root = doc.Root;
            string genName = root.Element( "Generator" ).Value;
            MapGenerator gen = MapGenUtil.GetGeneratorByName( genName );
            string versionMismatchMsg =
                String.Format( "This preset was made for a different version of {0} map generator. Continue?",
                               gen.Name );
            if( gen.Version != new Version( root.Element( "Version" ).Value ) &&
                MessageBox.Show( versionMismatchMsg, "Version mismatch", MessageBoxButtons.YesNo ) !=
                DialogResult.Yes ) {
                return;
            }
            SelectGenerator( gen );
            MapGeneratorParameters genParams = gen.CreateParameters( root.Element( "Parameters" ) );
            SetGenParams( genParams );
            SetStatus( "Generation parameters loaded." );
            bGenerate.PerformClick();
        }

        #endregion


        #region Tabs

        void tabs_SelectedIndexChanged( object sender, EventArgs e ) {
            if( tabs.SelectedTab == tabExisting ) {
                tab = Tabs.ExistingMap;
            } else if( tabs.SelectedTab == tabLoad ) {
                tab = Tabs.LoadFile;
            } else if( tabs.SelectedTab == tabCopy ) {
                tab = Tabs.CopyWorld;
            } else {
                tab = Tabs.Generator;
            }

            switch( tab ) {
                case Tabs.ExistingMap:
                    fileToLoad = World.FullFileName;
                    ShowMapDetails( tExistingMapInfo, fileToLoad );
                    StartLoadingMap();
                    return;

                case Tabs.LoadFile:
                    if( !String.IsNullOrEmpty( tFile.Text ) ) {
                        tFile.SelectAll();
                        fileToLoad = tFile.Text;
                        ShowMapDetails( tLoadFileInfo, fileToLoad );
                        StartLoadingMap();
                    } else {
                        Map = null;
                        ClearPreview();
                    }
                    return;

                case Tabs.CopyWorld:
                    Map = null;
                    ClearPreview();
                    cWorld_SelectedIndexChanged( cWorld, EventArgs.Empty );
                    return;

                case Tabs.Generator:
                    Map = null;
                    ClearPreview();
                    bGenerate.PerformClick();
                    return;
            }
        }

        enum Tabs {
            ExistingMap,
            LoadFile,
            CopyWorld,
            Generator
        }

        #endregion


        static void ShowMapDetails( TextBox textBox, string fileName ) {
            DateTime creationTime, modificationTime;
            long fileSize;

            if( File.Exists( fileName ) ) {
                FileInfo existingMapFileInfo = new FileInfo( fileName );
                creationTime = existingMapFileInfo.CreationTime;
                modificationTime = existingMapFileInfo.LastWriteTime;
                fileSize = existingMapFileInfo.Length;

            } else if( Directory.Exists( fileName ) ) {
                DirectoryInfo dirInfo = new DirectoryInfo( fileName );
                creationTime = dirInfo.CreationTime;
                modificationTime = dirInfo.LastWriteTime;
                fileSize = dirInfo.GetFiles().Sum( fileInfo => fileInfo.Length );

            } else {
                textBox.Text = "File or directory \"" + fileName + "\" does not exist.";
                return;
            }

            MapFormat format = MapUtility.Identify( fileName, true );
            try {
                Map loadedMap = MapUtility.LoadHeader( fileName, true );
                const string msgFormat =
                    @"  Location: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}
Dimensions: {5}×{6}×{7}
    Blocks: {8}";
                textBox.Text = String.Format( msgFormat,
                                              fileName,
                                              format,
                                              (fileSize/1024),
                                              creationTime.ToLongDateString(),
                                              modificationTime.ToLongDateString(),
                                              loadedMap.Width,
                                              loadedMap.Length,
                                              loadedMap.Height,
                                              loadedMap.Volume );

            } catch( Exception ex ) {
                const string msgFormat =
                    @"  Location: {0}
    Format: {1}
  Filesize: {2} KB
   Created: {3}
  Modified: {4}

Could not load more information:
{5}: {6}";
                textBox.Text = String.Format( msgFormat,
                                              fileName,
                                              format,
                                              (fileSize/1024),
                                              creationTime.ToLongDateString(),
                                              modificationTime.ToLongDateString(),
                                              ex.GetType().Name,
                                              ex.Message );
            }
        }


        void AddWorldPopup_FormClosing( object sender, FormClosingEventArgs e ) {
            if( DialogResult == DialogResult.OK ) {
                if( Map == null ) {
                    e.Cancel = true;
                } else {
                    ClearPreview();
                    Enabled = false;
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                    tStatus1.Text = "Saving map...";
                    tStatus2.Text = "";
                    Refresh();

                    string newFileName = World.FullFileName;
                    Map.Save( newFileName );
                    string oldFileName = Path.Combine( Paths.MapPath, originalWorldName + ".fcm" );

                    if( originalWorldName != null && originalWorldName != World.Name && File.Exists( oldFileName ) ) {
                        try {
                            File.Delete( oldFileName );
                        } catch( Exception ex ) {
                            string errorMessage =
                                String.Format(
                                    "Renaming the map file failed. Please delete the old file ({0}.fcm) manually.{1}{2}",
                                    originalWorldName,
                                    Environment.NewLine,
                                    ex );
                            MessageBox.Show( errorMessage, "Error renaming the map file" );
                        }
                    }
                }
            }
        }
    }
}