// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
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


namespace fCraft.ConfigGUI {
    sealed partial class AddWorldPopup : Form {
        static readonly Dictionary<IMapGenerator, IMapGeneratorGuiProvider> Generators =
            new Dictionary<IMapGenerator, IMapGeneratorGuiProvider>();

        static AddWorldPopup() {
            Generators.Add( FlatMapGen.Instance, DefaultMapGenGuiProvider.Instance );
            Generators.Add( RealisticMapGen.Instance, RealisticMapGenGuiProvider.Instance );
            Generators.Add( VanillaMapGen.Instance, DefaultMapGenGuiProvider.Instance );
            Generators.Add( FloatingIslandMapGen.Instance, DefaultMapGenGuiProvider.Instance );
        }


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
            renderer = new IsoCat();
            fileBrowser.Filter = MapLoadFilter;

            cBackup.Items.AddRange( WorldListEntry.BackupEnumNames );

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

            cGenerator.Items.AddRange( Generators.Keys.Select( gen => gen.Name ).ToArray() );
            cGenerator.SelectedIndex = 0;

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
                var importSettingsItem = tsbImportSettings.DropDownItems.Add( otherWorld.Name );
                importSettingsItem.Tag = otherWorld;
            }
            tsbImportSettings.DropDownItemClicked += tsbImportSettings_DropDownItemClicked;

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
                /*xHidden.Checked = World.Hidden;*/

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

            // Disable "existing map" tab if mapfile does not exist
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


        #region Map Preview

        readonly IsoCat renderer;

        void Redraw( bool drawAgain ) {
            lock( redrawLock ) {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                if( bwRenderer.IsBusy ) {
                    renderer.CancelAsync();
                    bwRenderer.CancelAsync();
                    while( bwRenderer.IsBusy ) {
                        Thread.Sleep( 1 );
                        Application.DoEvents();
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

            renderer.ProgressChanged +=
                ( progressSender, progressArgs ) =>
                bwRenderer.ReportProgress( progressArgs.ProgressPercentage, progressArgs.UserState );
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

        #endregion


        #region Map Generation

        IMapGeneratorState genState;

        void bGenerate_Click( object sender, EventArgs e ) {
            if( genState != null ) {
                genState.CancelAsync();
                tStatus1.Text = "Canceling...";
                bGenerate.Enabled = false;
                return;
            }
            Map = null;

            IMapGeneratorParameters genParams = genGui.GetParameters();
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
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }


        void AsyncGenProgress( object sender, ProgressChangedEventArgs e ) {
            progressBar.Value = e.ProgressPercentage;
            tStatus1.Text = (string)e.UserState;
        }


        void AsyncGenCompleted( object sender, RunWorkerCompletedEventArgs e ) {
            stopwatch.Stop();
            if( genState.Canceled ) {
                tStatus1.Text = "Generation canceled!";
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


        void bShow_Click( object sender, EventArgs e ) {
            if( cWorld.SelectedIndex != -1 && File.Exists( copyOptionsList[cWorld.SelectedIndex].FullFileName ) ) {
                bShow.Enabled = false;
                fileToLoad = copyOptionsList[cWorld.SelectedIndex].FullFileName;
                ShowMapDetails( tCopyInfo, fileToLoad );
                StartLoadingMap();
            }
        }


        void cWorld_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cWorld.SelectedIndex != -1 ) {
                string fileName = copyOptionsList[cWorld.SelectedIndex].FullFileName;
                bShow.Enabled = File.Exists( fileName );
                ShowMapDetails( tCopyInfo, fileName );
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
                    }
                    return;
                case Tabs.CopyWorld:
                    if( cWorld.SelectedIndex != -1 ) {
                        bShow.Enabled = File.Exists( copyOptionsList[cWorld.SelectedIndex].FullFileName );
                    }
                    return;
                case Tabs.Generator:
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
                fileSize = dirInfo.GetFiles().Sum( finfo => finfo.Length );

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
            Redraw( false );
            if( DialogResult == DialogResult.OK ) {
                if( Map == null ) {
                    e.Cancel = true;
                } else {
                    bwRenderer.CancelAsync();
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


        IMapGenerator generator;

        void cGenerator_SelectedIndexChanged( object sender, EventArgs e ) {
            string genName = cGenerator.SelectedItem.ToString();
            SelectGenerator( GetGeneratorByName( genName ) );
        }


        IMapGenerator GetGeneratorByName( string genName ) {
            return Generators.First( kvp => kvp.Key.Name == genName ).Key;
        }

        void SelectGenerator( IMapGenerator newGen ) {
            generatorParamsPanel.SuspendLayout();
            if( genGui != null ) {
                generatorParamsPanel.Controls.Clear();
                genGui.Dispose();
                genGui = null;
            }

            generator = newGen;
            genGui = Generators[newGen].CreateGui();

            genGui.Width = generatorParamsPanel.Width;
            generatorParamsPanel.Controls.Add( genGui );
            genGui.SetParameters( generator.GetDefaultParameters() );
            SignalMapDimensionChange();
            generatorParamsPanel.ResumeLayout();
            generatorParamsPanel.PerformLayout();
        }


        void SignalMapDimensionChange() {
            genGui.OnMapDimensionChange( (int)nMapWidth.Value, (int)nMapLength.Value, (int)nMapHeight.Value );
        }


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

        void tsbImportSettings_ButtonClick( object sender, EventArgs e ) {
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
                        MapGenParamsFromMap( ourMap );
                    } else {
                        MessageBox.Show( "Could not load map file!" );
                    }

                } else if( fileName.EndsWith( ".ftpl", StringComparison.OrdinalIgnoreCase ) ) {
                    XDocument doc = XDocument.Load( fullFileName );
                    XElement root = doc.Root;
                    MessageBox.Show( "TODO" ); // TODO

                } else {
                    MessageBox.Show( "Unrecognized file: \"" + fileName + "\"" );
                }
            }
        }


        void tsbImportSettings_DropDownItemClicked( object sender, ToolStripItemClickedEventArgs e ) {
            WorldListEntry entry = e.ClickedItem.Tag as WorldListEntry;
            if( entry == null ) return;

            Map ourMap;
            if( MapUtility.TryLoadHeader( entry.FullFileName, false, out ourMap ) ) {
                MapGenParamsFromMap( ourMap );
            } else {
                MessageBox.Show( "Could not load map file!" );
            }
        }


        void MapGenParamsFromMap( Map ourMap ) {
            string oldData;
            if( ourMap.Metadata.TryGetValue( "_Origin", "GeneratorParams", out oldData ) ) {
                MessageBox.Show( "TODO" ); // TODO
            } else {
                MessageBox.Show( "No embedded generation settings found!" );
            }
        }


        OpenFileDialog loadPresetDialog;

        void tsbLoadPreset_ButtonClick( object sender, EventArgs e ) {
            if( loadPresetDialog == null ) {
                loadPresetDialog = new OpenFileDialog {
                    Filter = "fCraft MapGen Preset|*.fmgp|" +
                             "All files|*.*",
                    InitialDirectory = Paths.MapPath
                };
            }
            if( loadPresetDialog.ShowDialog() == DialogResult.OK ) {
                string fullFileName = loadPresetDialog.FileName;
                XDocument doc = XDocument.Load( fullFileName );
                XElement root = doc.Root;
                string genName = root.Element( "Generator" ).Value;
                IMapGenerator gen = GetGeneratorByName( genName );
                if( gen.Version != new Version( root.Element( "Version" ).Value ) &&
                    MessageBox.Show(
                        "This preset was made for a different version of " + gen.Name +
                        " map generator. Continue?",
                        "Version mismatch",
                        MessageBoxButtons.YesNo ) != DialogResult.Yes ) {
                    return;
                }
                SelectGenerator( gen );
                IMapGeneratorParameters genParams = gen.CreateParameters( root.Element( "Parameters" ) );
                genGui.SetParameters( genParams );
                SignalMapDimensionChange();
            }
        }
    }
}