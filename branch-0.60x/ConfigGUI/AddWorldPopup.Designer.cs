namespace fCraft.ConfigGUI {
    partial class AddWorldPopup {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && (components != null) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.bShow = new System.Windows.Forms.Button();
            this.bGenerate = new System.Windows.Forms.Button();
            this.cWorld = new System.Windows.Forms.ComboBox();
            this.tFile = new System.Windows.Forms.TextBox();
            this.bBrowseFile = new System.Windows.Forms.Button();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.cBackup = new System.Windows.Forms.ComboBox();
            this.cAccess = new System.Windows.Forms.ComboBox();
            this.cBuild = new System.Windows.Forms.ComboBox();
            this.lName = new System.Windows.Forms.Label();
            this.lAccess = new System.Windows.Forms.Label();
            this.lBuild = new System.Windows.Forms.Label();
            this.lBackup = new System.Windows.Forms.Label();
            this.tName = new System.Windows.Forms.TextBox();
            this.bPreviewPrev = new System.Windows.Forms.Button();
            this.bPreviewNext = new System.Windows.Forms.Button();
            this.fileBrowser = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.tStatus1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tStatus2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.previewLayout = new System.Windows.Forms.TableLayoutPanel();
            this.preview = new fCraft.ConfigGUI.CustomPictureBox();
            this.bSavePreview = new System.Windows.Forms.Button();
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabExisting = new System.Windows.Forms.TabPage();
            this.tExistingMapInfo = new System.Windows.Forms.TextBox();
            this.tabLoad = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lFolder = new System.Windows.Forms.Label();
            this.tFolder = new System.Windows.Forms.TextBox();
            this.bBrowseFolder = new System.Windows.Forms.Button();
            this.lFormatList = new System.Windows.Forms.Label();
            this.lFile = new System.Windows.Forms.Label();
            this.tLoadFileInfo = new System.Windows.Forms.TextBox();
            this.tabCopy = new System.Windows.Forms.TabPage();
            this.tCopyInfo = new System.Windows.Forms.TextBox();
            this.lWorldToCopy = new System.Windows.Forms.Label();
            this.tabFlatgrass = new System.Windows.Forms.TabPage();
            this.bFlatgrassGenerate = new System.Windows.Forms.Button();
            this.nFlatgrassDimX = new System.Windows.Forms.NumericUpDown();
            this.lFlatgrassX1 = new System.Windows.Forms.Label();
            this.lFlatgrassDimensions = new System.Windows.Forms.Label();
            this.lFlatgrassX2 = new System.Windows.Forms.Label();
            this.nFlatgrassDimZ = new System.Windows.Forms.NumericUpDown();
            this.nFlatgrassDimY = new System.Windows.Forms.NumericUpDown();
            this.tabTerrain = new System.Windows.Forms.TabPage();
            this.lMapFileOptions = new System.Windows.Forms.Label();
            this.lCreateMap = new System.Windows.Forms.Label();
            this.folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            this.lVisibility = new System.Windows.Forms.Label();
            this.cVisibility = new System.Windows.Forms.ComboBox();
            this.lBlockDB = new System.Windows.Forms.Label();
            this.cBlockDB = new System.Windows.Forms.ComboBox();
            this.cGenerator = new System.Windows.Forms.ComboBox();
            this.lGenerator = new System.Windows.Forms.Label();
            this.generatorParamsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.cPreviewMode = new System.Windows.Forms.ComboBox();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown3 = new System.Windows.Forms.NumericUpDown();
            this.lDimensions = new System.Windows.Forms.Label();
            this.statusStrip.SuspendLayout();
            this.previewLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.preview)).BeginInit();
            this.tabs.SuspendLayout();
            this.tabExisting.SuspendLayout();
            this.tabLoad.SuspendLayout();
            this.tabCopy.SuspendLayout();
            this.tabFlatgrass.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nFlatgrassDimX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nFlatgrassDimZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nFlatgrassDimY)).BeginInit();
            this.tabTerrain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit();
            this.SuspendLayout();
            // 
            // bShow
            // 
            this.bShow.Location = new System.Drawing.Point(305, 6);
            this.bShow.Name = "bShow";
            this.bShow.Size = new System.Drawing.Size(74, 23);
            this.bShow.TabIndex = 2;
            this.bShow.Text = "Show";
            this.bShow.UseVisualStyleBackColor = true;
            this.bShow.Click += new System.EventHandler(this.bShow_Click);
            // 
            // bGenerate
            // 
            this.bGenerate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bGenerate.Location = new System.Drawing.Point(6, 6);
            this.bGenerate.Name = "bGenerate";
            this.bGenerate.Size = new System.Drawing.Size(115, 47);
            this.bGenerate.TabIndex = 0;
            this.bGenerate.Text = "Generate";
            this.bGenerate.UseVisualStyleBackColor = true;
            this.bGenerate.Click += new System.EventHandler(this.bGenerate_Click);
            // 
            // cWorld
            // 
            this.cWorld.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cWorld.FormattingEnabled = true;
            this.cWorld.Location = new System.Drawing.Point(85, 7);
            this.cWorld.Name = "cWorld";
            this.cWorld.Size = new System.Drawing.Size(214, 21);
            this.cWorld.TabIndex = 1;
            this.cWorld.SelectedIndexChanged += new System.EventHandler(this.cWorld_SelectedIndexChanged);
            // 
            // tFile
            // 
            this.tFile.Location = new System.Drawing.Point(72, 87);
            this.tFile.Name = "tFile";
            this.tFile.ReadOnly = true;
            this.tFile.Size = new System.Drawing.Size(233, 20);
            this.tFile.TabIndex = 3;
            // 
            // bBrowseFile
            // 
            this.bBrowseFile.Location = new System.Drawing.Point(311, 85);
            this.bBrowseFile.Name = "bBrowseFile";
            this.bBrowseFile.Size = new System.Drawing.Size(74, 23);
            this.bBrowseFile.TabIndex = 4;
            this.bBrowseFile.Text = "Browse";
            this.bBrowseFile.UseVisualStyleBackColor = true;
            this.bBrowseFile.Click += new System.EventHandler(this.bBrowseFile_Click);
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bOK.Location = new System.Drawing.Point(766, 533);
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size(100, 25);
            this.bOK.TabIndex = 16;
            this.bOK.Text = "OK";
            this.bOK.UseVisualStyleBackColor = true;
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point(872, 533);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(100, 25);
            this.bCancel.TabIndex = 17;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // cBackup
            // 
            this.cBackup.FormattingEnabled = true;
            this.cBackup.Location = new System.Drawing.Point(342, 66);
            this.cBackup.Name = "cBackup";
            this.cBackup.Size = new System.Drawing.Size(78, 21);
            this.cBackup.TabIndex = 7;
            this.cBackup.SelectedIndexChanged += new System.EventHandler(this.cBackup_SelectedIndexChanged);
            // 
            // cAccess
            // 
            this.cAccess.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cAccess.FormattingEnabled = true;
            this.cAccess.Location = new System.Drawing.Point(112, 39);
            this.cAccess.Name = "cAccess";
            this.cAccess.Size = new System.Drawing.Size(113, 21);
            this.cAccess.TabIndex = 3;
            this.cAccess.SelectedIndexChanged += new System.EventHandler(this.cAccess_SelectedIndexChanged);
            // 
            // cBuild
            // 
            this.cBuild.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBuild.FormattingEnabled = true;
            this.cBuild.Location = new System.Drawing.Point(112, 66);
            this.cBuild.Name = "cBuild";
            this.cBuild.Size = new System.Drawing.Size(113, 21);
            this.cBuild.TabIndex = 5;
            this.cBuild.SelectedIndexChanged += new System.EventHandler(this.cBuild_SelectedIndexChanged);
            // 
            // lName
            // 
            this.lName.AutoSize = true;
            this.lName.Location = new System.Drawing.Point(42, 15);
            this.lName.Name = "lName";
            this.lName.Size = new System.Drawing.Size(64, 13);
            this.lName.TabIndex = 0;
            this.lName.Text = "World name";
            // 
            // lAccess
            // 
            this.lAccess.AutoSize = true;
            this.lAccess.Location = new System.Drawing.Point(12, 42);
            this.lAccess.Name = "lAccess";
            this.lAccess.Size = new System.Drawing.Size(94, 13);
            this.lAccess.TabIndex = 2;
            this.lAccess.Text = "Access permission";
            // 
            // lBuild
            // 
            this.lBuild.AutoSize = true;
            this.lBuild.Location = new System.Drawing.Point(24, 69);
            this.lBuild.Name = "lBuild";
            this.lBuild.Size = new System.Drawing.Size(82, 13);
            this.lBuild.TabIndex = 4;
            this.lBuild.Text = "Build permission";
            // 
            // lBackup
            // 
            this.lBackup.AutoSize = true;
            this.lBackup.Location = new System.Drawing.Point(242, 69);
            this.lBackup.Name = "lBackup";
            this.lBackup.Size = new System.Drawing.Size(94, 13);
            this.lBackup.TabIndex = 6;
            this.lBackup.Text = "Backup frequency";
            this.lBackup.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tName
            // 
            this.tName.Location = new System.Drawing.Point(112, 12);
            this.tName.Name = "tName";
            this.tName.Size = new System.Drawing.Size(113, 20);
            this.tName.TabIndex = 1;
            this.tName.Validating += new System.ComponentModel.CancelEventHandler(this.tName_Validating);
            this.tName.Validated += new System.EventHandler(this.tName_Validated);
            // 
            // bPreviewPrev
            // 
            this.bPreviewPrev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bPreviewPrev.Location = new System.Drawing.Point(173, 490);
            this.bPreviewPrev.Name = "bPreviewPrev";
            this.bPreviewPrev.Size = new System.Drawing.Size(22, 22);
            this.bPreviewPrev.TabIndex = 0;
            this.bPreviewPrev.Text = "<";
            this.bPreviewPrev.UseVisualStyleBackColor = true;
            this.bPreviewPrev.Click += new System.EventHandler(this.bPreviewPrev_Click);
            // 
            // bPreviewNext
            // 
            this.bPreviewNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bPreviewNext.Location = new System.Drawing.Point(351, 490);
            this.bPreviewNext.Name = "bPreviewNext";
            this.bPreviewNext.Size = new System.Drawing.Size(22, 22);
            this.bPreviewNext.TabIndex = 2;
            this.bPreviewNext.Text = ">";
            this.bPreviewNext.UseVisualStyleBackColor = true;
            this.bPreviewNext.Click += new System.EventHandler(this.bPreviewNext_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.progressBar,
            this.tStatus1,
            this.tStatus2});
            this.statusStrip.Location = new System.Drawing.Point(0, 561);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(984, 22);
            this.statusStrip.TabIndex = 13;
            this.statusStrip.Text = "statusStrip1";
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(100, 16);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;
            // 
            // tStatus1
            // 
            this.tStatus1.Name = "tStatus1";
            this.tStatus1.Size = new System.Drawing.Size(44, 17);
            this.tStatus1.Text = "status1";
            // 
            // tStatus2
            // 
            this.tStatus2.Name = "tStatus2";
            this.tStatus2.Size = new System.Drawing.Size(44, 17);
            this.tStatus2.Text = "status2";
            // 
            // previewLayout
            // 
            this.previewLayout.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.previewLayout.ColumnCount = 3;
            this.previewLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.previewLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.previewLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.previewLayout.Controls.Add(this.bPreviewPrev, 0, 1);
            this.previewLayout.Controls.Add(this.bPreviewNext, 2, 1);
            this.previewLayout.Controls.Add(this.preview, 0, 0);
            this.previewLayout.Controls.Add(this.cPreviewMode, 1, 1);
            this.previewLayout.Location = new System.Drawing.Point(426, 12);
            this.previewLayout.Name = "previewLayout";
            this.previewLayout.RowCount = 2;
            this.previewLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.previewLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.previewLayout.Size = new System.Drawing.Size(546, 515);
            this.previewLayout.TabIndex = 12;
            // 
            // preview
            // 
            this.preview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.preview.BackColor = System.Drawing.Color.Black;
            this.previewLayout.SetColumnSpan(this.preview, 3);
            this.preview.Location = new System.Drawing.Point(3, 3);
            this.preview.Name = "preview";
            this.preview.Size = new System.Drawing.Size(540, 481);
            this.preview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.preview.TabIndex = 17;
            this.preview.TabStop = false;
            // 
            // bSavePreview
            // 
            this.bSavePreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bSavePreview.Enabled = false;
            this.bSavePreview.Location = new System.Drawing.Point(422, 533);
            this.bSavePreview.Name = "bSavePreview";
            this.bSavePreview.Size = new System.Drawing.Size(125, 25);
            this.bSavePreview.TabIndex = 14;
            this.bSavePreview.Text = "Save Preview Image...";
            this.bSavePreview.UseVisualStyleBackColor = true;
            this.bSavePreview.Click += new System.EventHandler(this.bSavePreview_Click);
            // 
            // tabs
            // 
            this.tabs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tabs.Controls.Add(this.tabExisting);
            this.tabs.Controls.Add(this.tabLoad);
            this.tabs.Controls.Add(this.tabCopy);
            this.tabs.Controls.Add(this.tabFlatgrass);
            this.tabs.Controls.Add(this.tabTerrain);
            this.tabs.Location = new System.Drawing.Point(12, 110);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(408, 448);
            this.tabs.TabIndex = 11;
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanged);
            // 
            // tabExisting
            // 
            this.tabExisting.Controls.Add(this.tExistingMapInfo);
            this.tabExisting.Location = new System.Drawing.Point(4, 22);
            this.tabExisting.Name = "tabExisting";
            this.tabExisting.Padding = new System.Windows.Forms.Padding(3);
            this.tabExisting.Size = new System.Drawing.Size(391, 422);
            this.tabExisting.TabIndex = 0;
            this.tabExisting.Text = "Existing Map";
            this.tabExisting.UseVisualStyleBackColor = true;
            // 
            // tExistingMapInfo
            // 
            this.tExistingMapInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tExistingMapInfo.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tExistingMapInfo.Location = new System.Drawing.Point(6, 6);
            this.tExistingMapInfo.Multiline = true;
            this.tExistingMapInfo.Name = "tExistingMapInfo";
            this.tExistingMapInfo.ReadOnly = true;
            this.tExistingMapInfo.Size = new System.Drawing.Size(373, 410);
            this.tExistingMapInfo.TabIndex = 0;
            this.tExistingMapInfo.TabStop = false;
            // 
            // tabLoad
            // 
            this.tabLoad.Controls.Add(this.label3);
            this.tabLoad.Controls.Add(this.label2);
            this.tabLoad.Controls.Add(this.lFolder);
            this.tabLoad.Controls.Add(this.tFolder);
            this.tabLoad.Controls.Add(this.bBrowseFolder);
            this.tabLoad.Controls.Add(this.lFormatList);
            this.tabLoad.Controls.Add(this.lFile);
            this.tabLoad.Controls.Add(this.tLoadFileInfo);
            this.tabLoad.Controls.Add(this.tFile);
            this.tabLoad.Controls.Add(this.bBrowseFile);
            this.tabLoad.Location = new System.Drawing.Point(4, 22);
            this.tabLoad.Name = "tabLoad";
            this.tabLoad.Padding = new System.Windows.Forms.Padding(3);
            this.tabLoad.Size = new System.Drawing.Size(391, 422);
            this.tabLoad.TabIndex = 1;
            this.tabLoad.Text = "Load File";
            this.tabLoad.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(211, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(151, 65);
            this.label3.TabIndex = 1;
            this.label3.Text = "\r\n- MinerCPP and LuaCraft (.dat)\r\n- D3 (.map)\r\n- JTE\'s (.gz)\r\n- OptiCraft (.save)" +
    "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 126);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(173, 26);
            this.label2.TabIndex = 5;
            this.label2.Text = "Supported folder formats:\r\n- Myne, MyneCraft, Hyvebuilt, iCraft";
            // 
            // lFolder
            // 
            this.lFolder.AutoSize = true;
            this.lFolder.Location = new System.Drawing.Point(6, 162);
            this.lFolder.Name = "lFolder";
            this.lFolder.Size = new System.Drawing.Size(60, 13);
            this.lFolder.TabIndex = 6;
            this.lFolder.Text = "Load folder";
            // 
            // tFolder
            // 
            this.tFolder.Location = new System.Drawing.Point(72, 159);
            this.tFolder.Name = "tFolder";
            this.tFolder.ReadOnly = true;
            this.tFolder.Size = new System.Drawing.Size(233, 20);
            this.tFolder.TabIndex = 7;
            // 
            // bBrowseFolder
            // 
            this.bBrowseFolder.Location = new System.Drawing.Point(311, 156);
            this.bBrowseFolder.Name = "bBrowseFolder";
            this.bBrowseFolder.Size = new System.Drawing.Size(74, 23);
            this.bBrowseFolder.TabIndex = 8;
            this.bBrowseFolder.Text = "Browse";
            this.bBrowseFolder.UseVisualStyleBackColor = true;
            this.bBrowseFolder.Click += new System.EventHandler(this.bBrowseFolder_Click);
            // 
            // lFormatList
            // 
            this.lFormatList.AutoSize = true;
            this.lFormatList.Location = new System.Drawing.Point(6, 3);
            this.lFormatList.Name = "lFormatList";
            this.lFormatList.Size = new System.Drawing.Size(144, 78);
            this.lFormatList.TabIndex = 0;
            this.lFormatList.Text = "Supported file formats:\r\n- fCraft and SpaceCraft (.fcm)\r\n- MCSharp and MCZall (.l" +
    "vl)\r\n- Creative (original .dat)\r\n- Survival Test (.mine)\r\n- Survival Indev (.mcl" +
    "evel)";
            // 
            // lFile
            // 
            this.lFile.AutoSize = true;
            this.lFile.Location = new System.Drawing.Point(6, 90);
            this.lFile.Name = "lFile";
            this.lFile.Size = new System.Drawing.Size(47, 13);
            this.lFile.TabIndex = 2;
            this.lFile.Text = "Load file";
            // 
            // tLoadFileInfo
            // 
            this.tLoadFileInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tLoadFileInfo.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tLoadFileInfo.Location = new System.Drawing.Point(3, 185);
            this.tLoadFileInfo.Multiline = true;
            this.tLoadFileInfo.Name = "tLoadFileInfo";
            this.tLoadFileInfo.ReadOnly = true;
            this.tLoadFileInfo.Size = new System.Drawing.Size(379, 233);
            this.tLoadFileInfo.TabIndex = 9;
            this.tLoadFileInfo.TabStop = false;
            // 
            // tabCopy
            // 
            this.tabCopy.Controls.Add(this.tCopyInfo);
            this.tabCopy.Controls.Add(this.lWorldToCopy);
            this.tabCopy.Controls.Add(this.bShow);
            this.tabCopy.Controls.Add(this.cWorld);
            this.tabCopy.Location = new System.Drawing.Point(4, 22);
            this.tabCopy.Name = "tabCopy";
            this.tabCopy.Padding = new System.Windows.Forms.Padding(3);
            this.tabCopy.Size = new System.Drawing.Size(391, 422);
            this.tabCopy.TabIndex = 2;
            this.tabCopy.Text = "Copy World";
            this.tabCopy.UseVisualStyleBackColor = true;
            // 
            // tCopyInfo
            // 
            this.tCopyInfo.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tCopyInfo.Location = new System.Drawing.Point(6, 34);
            this.tCopyInfo.Multiline = true;
            this.tCopyInfo.Name = "tCopyInfo";
            this.tCopyInfo.ReadOnly = true;
            this.tCopyInfo.Size = new System.Drawing.Size(373, 100);
            this.tCopyInfo.TabIndex = 3;
            // 
            // lWorldToCopy
            // 
            this.lWorldToCopy.AutoSize = true;
            this.lWorldToCopy.Location = new System.Drawing.Point(6, 11);
            this.lWorldToCopy.Name = "lWorldToCopy";
            this.lWorldToCopy.Size = new System.Drawing.Size(73, 13);
            this.lWorldToCopy.TabIndex = 0;
            this.lWorldToCopy.Text = "World to copy";
            // 
            // tabFlatgrass
            // 
            this.tabFlatgrass.Controls.Add(this.bFlatgrassGenerate);
            this.tabFlatgrass.Controls.Add(this.nFlatgrassDimX);
            this.tabFlatgrass.Controls.Add(this.lFlatgrassX1);
            this.tabFlatgrass.Controls.Add(this.lFlatgrassDimensions);
            this.tabFlatgrass.Controls.Add(this.lFlatgrassX2);
            this.tabFlatgrass.Controls.Add(this.nFlatgrassDimZ);
            this.tabFlatgrass.Controls.Add(this.nFlatgrassDimY);
            this.tabFlatgrass.Location = new System.Drawing.Point(4, 22);
            this.tabFlatgrass.Name = "tabFlatgrass";
            this.tabFlatgrass.Padding = new System.Windows.Forms.Padding(3);
            this.tabFlatgrass.Size = new System.Drawing.Size(400, 422);
            this.tabFlatgrass.TabIndex = 3;
            this.tabFlatgrass.Text = "Flatgrass";
            this.tabFlatgrass.UseVisualStyleBackColor = true;
            // 
            // bFlatgrassGenerate
            // 
            this.bFlatgrassGenerate.Location = new System.Drawing.Point(6, 6);
            this.bFlatgrassGenerate.Name = "bFlatgrassGenerate";
            this.bFlatgrassGenerate.Size = new System.Drawing.Size(74, 50);
            this.bFlatgrassGenerate.TabIndex = 0;
            this.bFlatgrassGenerate.Text = "Generate";
            this.bFlatgrassGenerate.UseVisualStyleBackColor = true;
            this.bFlatgrassGenerate.Click += new System.EventHandler(this.bGenerate_Click);
            // 
            // nFlatgrassDimX
            // 
            this.nFlatgrassDimX.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.nFlatgrassDimX.Location = new System.Drawing.Point(153, 23);
            this.nFlatgrassDimX.Maximum = new decimal(new int[] {
            2032,
            0,
            0,
            0});
            this.nFlatgrassDimX.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.nFlatgrassDimX.Name = "nFlatgrassDimX";
            this.nFlatgrassDimX.Size = new System.Drawing.Size(54, 20);
            this.nFlatgrassDimX.TabIndex = 2;
            this.nFlatgrassDimX.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // lFlatgrassX1
            // 
            this.lFlatgrassX1.AutoSize = true;
            this.lFlatgrassX1.Location = new System.Drawing.Point(213, 25);
            this.lFlatgrassX1.Name = "lFlatgrassX1";
            this.lFlatgrassX1.Size = new System.Drawing.Size(13, 13);
            this.lFlatgrassX1.TabIndex = 3;
            this.lFlatgrassX1.Text = "×";
            // 
            // lFlatgrassDimensions
            // 
            this.lFlatgrassDimensions.AutoSize = true;
            this.lFlatgrassDimensions.Location = new System.Drawing.Point(86, 25);
            this.lFlatgrassDimensions.Name = "lFlatgrassDimensions";
            this.lFlatgrassDimensions.Size = new System.Drawing.Size(61, 13);
            this.lFlatgrassDimensions.TabIndex = 1;
            this.lFlatgrassDimensions.Text = "Dimensions";
            // 
            // lFlatgrassX2
            // 
            this.lFlatgrassX2.AutoSize = true;
            this.lFlatgrassX2.Location = new System.Drawing.Point(292, 25);
            this.lFlatgrassX2.Name = "lFlatgrassX2";
            this.lFlatgrassX2.Size = new System.Drawing.Size(13, 13);
            this.lFlatgrassX2.TabIndex = 5;
            this.lFlatgrassX2.Text = "×";
            // 
            // nFlatgrassDimZ
            // 
            this.nFlatgrassDimZ.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.nFlatgrassDimZ.Location = new System.Drawing.Point(311, 23);
            this.nFlatgrassDimZ.Maximum = new decimal(new int[] {
            2032,
            0,
            0,
            0});
            this.nFlatgrassDimZ.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.nFlatgrassDimZ.Name = "nFlatgrassDimZ";
            this.nFlatgrassDimZ.Size = new System.Drawing.Size(54, 20);
            this.nFlatgrassDimZ.TabIndex = 6;
            this.nFlatgrassDimZ.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // nFlatgrassDimY
            // 
            this.nFlatgrassDimY.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.nFlatgrassDimY.Location = new System.Drawing.Point(232, 23);
            this.nFlatgrassDimY.Maximum = new decimal(new int[] {
            2032,
            0,
            0,
            0});
            this.nFlatgrassDimY.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.nFlatgrassDimY.Name = "nFlatgrassDimY";
            this.nFlatgrassDimY.Size = new System.Drawing.Size(54, 20);
            this.nFlatgrassDimY.TabIndex = 4;
            this.nFlatgrassDimY.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // tabTerrain
            // 
            this.tabTerrain.BackColor = System.Drawing.SystemColors.Window;
            this.tabTerrain.Controls.Add(this.lDimensions);
            this.tabTerrain.Controls.Add(this.numericUpDown1);
            this.tabTerrain.Controls.Add(this.label1);
            this.tabTerrain.Controls.Add(this.label4);
            this.tabTerrain.Controls.Add(this.numericUpDown2);
            this.tabTerrain.Controls.Add(this.numericUpDown3);
            this.tabTerrain.Controls.Add(this.generatorParamsPanel);
            this.tabTerrain.Controls.Add(this.lGenerator);
            this.tabTerrain.Controls.Add(this.cGenerator);
            this.tabTerrain.Controls.Add(this.bGenerate);
            this.tabTerrain.Location = new System.Drawing.Point(4, 22);
            this.tabTerrain.Name = "tabTerrain";
            this.tabTerrain.Padding = new System.Windows.Forms.Padding(3);
            this.tabTerrain.Size = new System.Drawing.Size(400, 422);
            this.tabTerrain.TabIndex = 5;
            this.tabTerrain.Text = "Generator";
            this.tabTerrain.UseVisualStyleBackColor = true;
            // 
            // lMapFileOptions
            // 
            this.lMapFileOptions.AutoSize = true;
            this.lMapFileOptions.Location = new System.Drawing.Point(12, 94);
            this.lMapFileOptions.Name = "lMapFileOptions";
            this.lMapFileOptions.Size = new System.Drawing.Size(47, 13);
            this.lMapFileOptions.TabIndex = 10;
            this.lMapFileOptions.Text = "Map file:";
            // 
            // lCreateMap
            // 
            this.lCreateMap.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lCreateMap.AutoSize = true;
            this.lCreateMap.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lCreateMap.ForeColor = System.Drawing.Color.Red;
            this.lCreateMap.Location = new System.Drawing.Point(610, 539);
            this.lCreateMap.Name = "lCreateMap";
            this.lCreateMap.Size = new System.Drawing.Size(150, 13);
            this.lCreateMap.TabIndex = 15;
            this.lCreateMap.Text = "Create a map to continue";
            // 
            // folderBrowser
            // 
            this.folderBrowser.Description = "Find the folder where your Myne / MyneCraft / Hydebuild / iCraft map is located.";
            // 
            // lVisibility
            // 
            this.lVisibility.AutoSize = true;
            this.lVisibility.Location = new System.Drawing.Point(248, 14);
            this.lVisibility.Name = "lVisibility";
            this.lVisibility.Size = new System.Drawing.Size(88, 13);
            this.lVisibility.TabIndex = 18;
            this.lVisibility.Text = "World list visibility";
            this.lVisibility.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cVisibility
            // 
            this.cVisibility.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cVisibility.FormattingEnabled = true;
            this.cVisibility.Items.AddRange(new object[] {
            "Everyone",
            "All who can access",
            "All who can build",
            "No one"});
            this.cVisibility.Location = new System.Drawing.Point(342, 12);
            this.cVisibility.Name = "cVisibility";
            this.cVisibility.Size = new System.Drawing.Size(78, 21);
            this.cVisibility.TabIndex = 19;
            // 
            // lBlockDB
            // 
            this.lBlockDB.AutoSize = true;
            this.lBlockDB.Location = new System.Drawing.Point(287, 42);
            this.lBlockDB.Name = "lBlockDB";
            this.lBlockDB.Size = new System.Drawing.Size(49, 13);
            this.lBlockDB.TabIndex = 20;
            this.lBlockDB.Text = "BlockDB";
            this.lBlockDB.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cBlockDB
            // 
            this.cBlockDB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBlockDB.FormattingEnabled = true;
            this.cBlockDB.Items.AddRange(new object[] {
            "Auto",
            "Always On",
            "Always Off"});
            this.cBlockDB.Location = new System.Drawing.Point(342, 39);
            this.cBlockDB.Name = "cBlockDB";
            this.cBlockDB.Size = new System.Drawing.Size(78, 21);
            this.cBlockDB.TabIndex = 21;
            // 
            // cGenerator
            // 
            this.cGenerator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cGenerator.FormattingEnabled = true;
            this.cGenerator.Location = new System.Drawing.Point(194, 6);
            this.cGenerator.Name = "cGenerator";
            this.cGenerator.Size = new System.Drawing.Size(200, 21);
            this.cGenerator.TabIndex = 1;
            // 
            // lGenerator
            // 
            this.lGenerator.AutoSize = true;
            this.lGenerator.Location = new System.Drawing.Point(134, 9);
            this.lGenerator.Name = "lGenerator";
            this.lGenerator.Size = new System.Drawing.Size(54, 13);
            this.lGenerator.TabIndex = 21;
            this.lGenerator.Text = "Generator";
            this.lGenerator.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // generatorParamsPanel
            // 
            this.generatorParamsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.generatorParamsPanel.Location = new System.Drawing.Point(0, 59);
            this.generatorParamsPanel.Name = "generatorParamsPanel";
            this.generatorParamsPanel.Size = new System.Drawing.Size(400, 363);
            this.generatorParamsPanel.TabIndex = 22;
            // 
            // cPreviewMode
            // 
            this.cPreviewMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cPreviewMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPreviewMode.FormattingEnabled = true;
            this.cPreviewMode.Items.AddRange(new object[] {
            "Normal Preview",
            "Cross-section (\"Cut\")",
            "No front walls (\"Peeled\")",
            "See-through water",
            "See-through lava"});
            this.cPreviewMode.Location = new System.Drawing.Point(201, 490);
            this.cPreviewMode.Name = "cPreviewMode";
            this.cPreviewMode.Size = new System.Drawing.Size(144, 21);
            this.cPreviewMode.TabIndex = 18;
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown1.Location = new System.Drawing.Point(194, 33);
            this.numericUpDown1.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            2032,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(54, 20);
            this.numericUpDown1.TabIndex = 23;
            this.numericUpDown1.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(251, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(13, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "×";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(324, 35);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(13, 13);
            this.label4.TabIndex = 26;
            this.label4.Text = "×";
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown2.Location = new System.Drawing.Point(340, 33);
            this.numericUpDown2.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.numericUpDown2.Maximum = new decimal(new int[] {
            2032,
            0,
            0,
            0});
            this.numericUpDown2.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(54, 20);
            this.numericUpDown2.TabIndex = 27;
            this.numericUpDown2.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // numericUpDown3
            // 
            this.numericUpDown3.Increment = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown3.Location = new System.Drawing.Point(267, 33);
            this.numericUpDown3.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.numericUpDown3.Maximum = new decimal(new int[] {
            2032,
            0,
            0,
            0});
            this.numericUpDown3.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.numericUpDown3.Name = "numericUpDown3";
            this.numericUpDown3.Size = new System.Drawing.Size(54, 20);
            this.numericUpDown3.TabIndex = 25;
            this.numericUpDown3.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // lDimensions
            // 
            this.lDimensions.AutoSize = true;
            this.lDimensions.Location = new System.Drawing.Point(127, 35);
            this.lDimensions.Name = "lDimensions";
            this.lDimensions.Size = new System.Drawing.Size(61, 13);
            this.lDimensions.TabIndex = 28;
            this.lDimensions.Text = "Dimensions";
            this.lDimensions.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // AddWorldPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 583);
            this.Controls.Add(this.lBlockDB);
            this.Controls.Add(this.cBlockDB);
            this.Controls.Add(this.lVisibility);
            this.Controls.Add(this.cVisibility);
            this.Controls.Add(this.lCreateMap);
            this.Controls.Add(this.lMapFileOptions);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.bSavePreview);
            this.Controls.Add(this.previewLayout);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.tName);
            this.Controls.Add(this.lBackup);
            this.Controls.Add(this.lBuild);
            this.Controls.Add(this.lAccess);
            this.Controls.Add(this.lName);
            this.Controls.Add(this.cBuild);
            this.Controls.Add(this.cAccess);
            this.Controls.Add(this.cBackup);
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.bOK);
            this.Name = "AddWorldPopup";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Add World";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AddWorldPopup_FormClosing);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.previewLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.preview)).EndInit();
            this.tabs.ResumeLayout(false);
            this.tabExisting.ResumeLayout(false);
            this.tabExisting.PerformLayout();
            this.tabLoad.ResumeLayout(false);
            this.tabLoad.PerformLayout();
            this.tabCopy.ResumeLayout(false);
            this.tabCopy.PerformLayout();
            this.tabFlatgrass.ResumeLayout(false);
            this.tabFlatgrass.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nFlatgrassDimX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nFlatgrassDimZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nFlatgrassDimY)).EndInit();
            this.tabTerrain.ResumeLayout(false);
            this.tabTerrain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bGenerate;
        private System.Windows.Forms.ComboBox cWorld;
        private System.Windows.Forms.TextBox tFile;
        private System.Windows.Forms.Button bBrowseFile;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.ComboBox cBackup;
        private System.Windows.Forms.ComboBox cAccess;
        private System.Windows.Forms.ComboBox cBuild;
        private System.Windows.Forms.Label lName;
        private System.Windows.Forms.Label lAccess;
        private System.Windows.Forms.Label lBuild;
        private System.Windows.Forms.Label lBackup;
        private System.Windows.Forms.TextBox tName;
        private System.Windows.Forms.Button bPreviewPrev;
        private System.Windows.Forms.Button bPreviewNext;
        private System.Windows.Forms.OpenFileDialog fileBrowser;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.ToolStripStatusLabel tStatus1;
        private System.Windows.Forms.ToolStripStatusLabel tStatus2;
        private System.Windows.Forms.TableLayoutPanel previewLayout;
        private System.Windows.Forms.Button bShow;
        private System.Windows.Forms.Button bSavePreview;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabExisting;
        private System.Windows.Forms.TabPage tabLoad;
        private System.Windows.Forms.TabPage tabCopy;
        private System.Windows.Forms.TabPage tabFlatgrass;
        private System.Windows.Forms.TabPage tabTerrain;
        private CustomPictureBox preview;
        private System.Windows.Forms.Label lMapFileOptions;
        private System.Windows.Forms.TextBox tExistingMapInfo;
        private System.Windows.Forms.TextBox tLoadFileInfo;
        private System.Windows.Forms.Label lFile;
        private System.Windows.Forms.Label lFormatList;
        private System.Windows.Forms.TextBox tCopyInfo;
        private System.Windows.Forms.Label lWorldToCopy;
        private System.Windows.Forms.Button bFlatgrassGenerate;
        private System.Windows.Forms.NumericUpDown nFlatgrassDimX;
        private System.Windows.Forms.Label lFlatgrassX1;
        private System.Windows.Forms.Label lFlatgrassDimensions;
        private System.Windows.Forms.Label lFlatgrassX2;
        private System.Windows.Forms.NumericUpDown nFlatgrassDimZ;
        private System.Windows.Forms.NumericUpDown nFlatgrassDimY;
        private System.Windows.Forms.Label lCreateMap;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lFolder;
        private System.Windows.Forms.TextBox tFolder;
        private System.Windows.Forms.Button bBrowseFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowser;
        private System.Windows.Forms.Label lVisibility;
        private System.Windows.Forms.ComboBox cVisibility;
        private System.Windows.Forms.Label lBlockDB;
        private System.Windows.Forms.ComboBox cBlockDB;
        private System.Windows.Forms.FlowLayoutPanel generatorParamsPanel;
        private System.Windows.Forms.Label lGenerator;
        private System.Windows.Forms.ComboBox cGenerator;
        private System.Windows.Forms.ComboBox cPreviewMode;
        private System.Windows.Forms.Label lDimensions;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.NumericUpDown numericUpDown3;
    }
}