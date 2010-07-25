namespace ConfigTool {
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
            this.lX2 = new System.Windows.Forms.Label();
            this.lX1 = new System.Windows.Forms.Label();
            this.lDim = new System.Windows.Forms.Label();
            this.rEmpty = new System.Windows.Forms.RadioButton();
            this.rFlatgrass = new System.Windows.Forms.RadioButton();
            this.rTerrain = new System.Windows.Forms.RadioButton();
            this.nWidthX = new System.Windows.Forms.NumericUpDown();
            this.nWidthY = new System.Windows.Forms.NumericUpDown();
            this.nHeight = new System.Windows.Forms.NumericUpDown();
            this.gMap = new System.Windows.Forms.GroupBox();
            this.xFloodBarrier = new System.Windows.Forms.CheckBox();
            this.cTheme = new System.Windows.Forms.ComboBox();
            this.lTheme = new System.Windows.Forms.Label();
            this.cTerrain = new System.Windows.Forms.ComboBox();
            this.lTerrain = new System.Windows.Forms.Label();
            this.bGenerate = new System.Windows.Forms.Button();
            this.cWorld = new System.Windows.Forms.ComboBox();
            this.tFile = new System.Windows.Forms.TextBox();
            this.bBrowse = new System.Windows.Forms.Button();
            this.rLoad = new System.Windows.Forms.RadioButton();
            this.rCopy = new System.Windows.Forms.RadioButton();
            this.lPreview = new System.Windows.Forms.Label();
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
            this.xHidden = new System.Windows.Forms.CheckBox();
            this.fileBrowser = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tStatus1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tStatus2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.preview = new System.Windows.Forms.PictureBox();
            this.previewLayout = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.nWidthX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nWidthY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nHeight)).BeginInit();
            this.gMap.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.preview)).BeginInit();
            this.previewLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // lX2
            // 
            this.lX2.AutoSize = true;
            this.lX2.Location = new System.Drawing.Point( 266, 21 );
            this.lX2.Name = "lX2";
            this.lX2.Size = new System.Drawing.Size( 13, 13 );
            this.lX2.TabIndex = 6;
            this.lX2.Text = "×";
            // 
            // lX1
            // 
            this.lX1.AutoSize = true;
            this.lX1.Location = new System.Drawing.Point( 187, 21 );
            this.lX1.Name = "lX1";
            this.lX1.Size = new System.Drawing.Size( 13, 13 );
            this.lX1.TabIndex = 5;
            this.lX1.Text = "×";
            // 
            // lDim
            // 
            this.lDim.AutoSize = true;
            this.lDim.Location = new System.Drawing.Point( 60, 21 );
            this.lDim.Name = "lDim";
            this.lDim.Size = new System.Drawing.Size( 61, 13 );
            this.lDim.TabIndex = 3;
            this.lDim.Text = "Dimensions";
            // 
            // rEmpty
            // 
            this.rEmpty.AutoSize = true;
            this.rEmpty.Location = new System.Drawing.Point( 6, 124 );
            this.rEmpty.Name = "rEmpty";
            this.rEmpty.Size = new System.Drawing.Size( 77, 17 );
            this.rEmpty.TabIndex = 8;
            this.rEmpty.Text = "Empty map";
            this.rEmpty.UseVisualStyleBackColor = true;
            this.rEmpty.CheckedChanged += new System.EventHandler( this.rEmpty_CheckedChanged );
            // 
            // rFlatgrass
            // 
            this.rFlatgrass.AutoSize = true;
            this.rFlatgrass.Location = new System.Drawing.Point( 6, 152 );
            this.rFlatgrass.Name = "rFlatgrass";
            this.rFlatgrass.Size = new System.Drawing.Size( 111, 17 );
            this.rFlatgrass.TabIndex = 9;
            this.rFlatgrass.Text = "Generate flatgrass";
            this.rFlatgrass.UseVisualStyleBackColor = true;
            this.rFlatgrass.CheckedChanged += new System.EventHandler( this.rFlatgrass_CheckedChanged );
            // 
            // rTerrain
            // 
            this.rTerrain.AutoSize = true;
            this.rTerrain.Location = new System.Drawing.Point( 6, 180 );
            this.rTerrain.Name = "rTerrain";
            this.rTerrain.Size = new System.Drawing.Size( 139, 17 );
            this.rTerrain.TabIndex = 10;
            this.rTerrain.Text = "Generate realistic terrain";
            this.rTerrain.UseVisualStyleBackColor = true;
            this.rTerrain.CheckedChanged += new System.EventHandler( this.rTerrain_CheckedChanged );
            // 
            // nWidthX
            // 
            this.nWidthX.Increment = new decimal( new int[] {
            16,
            0,
            0,
            0} );
            this.nWidthX.Location = new System.Drawing.Point( 127, 19 );
            this.nWidthX.Maximum = new decimal( new int[] {
            2032,
            0,
            0,
            0} );
            this.nWidthX.Minimum = new decimal( new int[] {
            16,
            0,
            0,
            0} );
            this.nWidthX.Name = "nWidthX";
            this.nWidthX.Size = new System.Drawing.Size( 54, 20 );
            this.nWidthX.TabIndex = 0;
            this.nWidthX.Value = new decimal( new int[] {
            64,
            0,
            0,
            0} );
            // 
            // nWidthY
            // 
            this.nWidthY.Increment = new decimal( new int[] {
            16,
            0,
            0,
            0} );
            this.nWidthY.Location = new System.Drawing.Point( 206, 19 );
            this.nWidthY.Maximum = new decimal( new int[] {
            2032,
            0,
            0,
            0} );
            this.nWidthY.Minimum = new decimal( new int[] {
            16,
            0,
            0,
            0} );
            this.nWidthY.Name = "nWidthY";
            this.nWidthY.Size = new System.Drawing.Size( 54, 20 );
            this.nWidthY.TabIndex = 1;
            this.nWidthY.Value = new decimal( new int[] {
            64,
            0,
            0,
            0} );
            // 
            // nHeight
            // 
            this.nHeight.Increment = new decimal( new int[] {
            16,
            0,
            0,
            0} );
            this.nHeight.Location = new System.Drawing.Point( 285, 19 );
            this.nHeight.Maximum = new decimal( new int[] {
            2032,
            0,
            0,
            0} );
            this.nHeight.Minimum = new decimal( new int[] {
            16,
            0,
            0,
            0} );
            this.nHeight.Name = "nHeight";
            this.nHeight.Size = new System.Drawing.Size( 54, 20 );
            this.nHeight.TabIndex = 2;
            this.nHeight.Value = new decimal( new int[] {
            64,
            0,
            0,
            0} );
            // 
            // gMap
            // 
            this.gMap.Controls.Add( this.xFloodBarrier );
            this.gMap.Controls.Add( this.cTheme );
            this.gMap.Controls.Add( this.lTheme );
            this.gMap.Controls.Add( this.cTerrain );
            this.gMap.Controls.Add( this.lTerrain );
            this.gMap.Controls.Add( this.bGenerate );
            this.gMap.Controls.Add( this.cWorld );
            this.gMap.Controls.Add( this.tFile );
            this.gMap.Controls.Add( this.bBrowse );
            this.gMap.Controls.Add( this.rLoad );
            this.gMap.Controls.Add( this.nHeight );
            this.gMap.Controls.Add( this.rCopy );
            this.gMap.Controls.Add( this.nWidthY );
            this.gMap.Controls.Add( this.rEmpty );
            this.gMap.Controls.Add( this.nWidthX );
            this.gMap.Controls.Add( this.rFlatgrass );
            this.gMap.Controls.Add( this.rTerrain );
            this.gMap.Controls.Add( this.lX2 );
            this.gMap.Controls.Add( this.lDim );
            this.gMap.Controls.Add( this.lX1 );
            this.gMap.Location = new System.Drawing.Point( 13, 142 );
            this.gMap.Name = "gMap";
            this.gMap.Size = new System.Drawing.Size( 365, 264 );
            this.gMap.TabIndex = 5;
            this.gMap.TabStop = false;
            this.gMap.Text = "Map";
            // 
            // xFloodBarrier
            // 
            this.xFloodBarrier.AutoSize = true;
            this.xFloodBarrier.Location = new System.Drawing.Point( 127, 45 );
            this.xFloodBarrier.Name = "xFloodBarrier";
            this.xFloodBarrier.Size = new System.Drawing.Size( 112, 17 );
            this.xFloodBarrier.TabIndex = 3;
            this.xFloodBarrier.Text = "Add a flood barrier";
            this.xFloodBarrier.UseVisualStyleBackColor = true;
            this.xFloodBarrier.CheckedChanged += new System.EventHandler( this.xFloodBarrier_CheckedChanged );
            // 
            // cTheme
            // 
            this.cTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cTheme.Enabled = false;
            this.cTheme.Location = new System.Drawing.Point( 273, 206 );
            this.cTheme.Name = "cTheme";
            this.cTheme.Size = new System.Drawing.Size( 86, 21 );
            this.cTheme.TabIndex = 12;
            // 
            // lTheme
            // 
            this.lTheme.AutoSize = true;
            this.lTheme.Enabled = false;
            this.lTheme.Location = new System.Drawing.Point( 227, 209 );
            this.lTheme.Name = "lTheme";
            this.lTheme.Size = new System.Drawing.Size( 40, 13 );
            this.lTheme.TabIndex = 19;
            this.lTheme.Text = "Theme";
            // 
            // cTerrain
            // 
            this.cTerrain.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cTerrain.Enabled = false;
            this.cTerrain.Location = new System.Drawing.Point( 127, 206 );
            this.cTerrain.Name = "cTerrain";
            this.cTerrain.Size = new System.Drawing.Size( 86, 21 );
            this.cTerrain.TabIndex = 11;
            // 
            // lTerrain
            // 
            this.lTerrain.AutoSize = true;
            this.lTerrain.Enabled = false;
            this.lTerrain.Location = new System.Drawing.Point( 81, 209 );
            this.lTerrain.Name = "lTerrain";
            this.lTerrain.Size = new System.Drawing.Size( 40, 13 );
            this.lTerrain.TabIndex = 17;
            this.lTerrain.Text = "Terrain";
            // 
            // bGenerate
            // 
            this.bGenerate.Enabled = false;
            this.bGenerate.Location = new System.Drawing.Point( 84, 233 );
            this.bGenerate.Name = "bGenerate";
            this.bGenerate.Size = new System.Drawing.Size( 75, 23 );
            this.bGenerate.TabIndex = 13;
            this.bGenerate.Text = "Generate";
            this.bGenerate.UseVisualStyleBackColor = true;
            this.bGenerate.Click += new System.EventHandler( this.bGenerate_Click );
            // 
            // cWorld
            // 
            this.cWorld.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cWorld.Enabled = false;
            this.cWorld.FormattingEnabled = true;
            this.cWorld.Items.AddRange( new object[] {
            "main (64x64x64)"} );
            this.cWorld.Location = new System.Drawing.Point( 127, 95 );
            this.cWorld.Name = "cWorld";
            this.cWorld.Size = new System.Drawing.Size( 132, 21 );
            this.cWorld.TabIndex = 7;
            // 
            // tFile
            // 
            this.tFile.Location = new System.Drawing.Point( 100, 69 );
            this.tFile.Name = "tFile";
            this.tFile.ReadOnly = true;
            this.tFile.Size = new System.Drawing.Size( 178, 20 );
            this.tFile.TabIndex = 4;
            // 
            // bBrowse
            // 
            this.bBrowse.Location = new System.Drawing.Point( 284, 67 );
            this.bBrowse.Name = "bBrowse";
            this.bBrowse.Size = new System.Drawing.Size( 75, 23 );
            this.bBrowse.TabIndex = 5;
            this.bBrowse.Text = "Browse";
            this.bBrowse.UseVisualStyleBackColor = true;
            this.bBrowse.Click += new System.EventHandler( this.bBrowse_Click );
            // 
            // rLoad
            // 
            this.rLoad.AutoSize = true;
            this.rLoad.Checked = true;
            this.rLoad.Location = new System.Drawing.Point( 6, 68 );
            this.rLoad.Name = "rLoad";
            this.rLoad.Size = new System.Drawing.Size( 88, 17 );
            this.rLoad.TabIndex = 3;
            this.rLoad.TabStop = true;
            this.rLoad.Text = "Load from file";
            this.rLoad.UseVisualStyleBackColor = true;
            this.rLoad.CheckedChanged += new System.EventHandler( this.rLoad_CheckedChanged );
            // 
            // rCopy
            // 
            this.rCopy.AutoSize = true;
            this.rCopy.Location = new System.Drawing.Point( 6, 96 );
            this.rCopy.Name = "rCopy";
            this.rCopy.Size = new System.Drawing.Size( 115, 17 );
            this.rCopy.TabIndex = 6;
            this.rCopy.Text = "Copy existing world";
            this.rCopy.UseVisualStyleBackColor = true;
            this.rCopy.CheckedChanged += new System.EventHandler( this.rCopy_CheckedChanged );
            // 
            // lPreview
            // 
            this.lPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lPreview.AutoSize = true;
            this.lPreview.Location = new System.Drawing.Point( 158, 336 );
            this.lPreview.Name = "lPreview";
            this.lPreview.Size = new System.Drawing.Size( 54, 28 );
            this.lPreview.TabIndex = 16;
            this.lPreview.Text = "Preview";
            this.lPreview.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bOK.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bOK.Location = new System.Drawing.Point( 549, 382 );
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size( 100, 25 );
            this.bOK.TabIndex = 8;
            this.bOK.Text = "OK";
            this.bOK.UseVisualStyleBackColor = true;
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 655, 382 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 100, 25 );
            this.bCancel.TabIndex = 7;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // cBackup
            // 
            this.cBackup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBackup.FormattingEnabled = true;
            this.cBackup.Items.AddRange( new object[] {
            "Never"} );
            this.cBackup.Location = new System.Drawing.Point( 140, 92 );
            this.cBackup.Name = "cBackup";
            this.cBackup.Size = new System.Drawing.Size( 110, 21 );
            this.cBackup.TabIndex = 3;
            this.cBackup.SelectedIndexChanged += new System.EventHandler( this.cBackup_SelectedIndexChanged );
            // 
            // cAccess
            // 
            this.cAccess.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cAccess.FormattingEnabled = true;
            this.cAccess.Location = new System.Drawing.Point( 140, 38 );
            this.cAccess.Name = "cAccess";
            this.cAccess.Size = new System.Drawing.Size( 132, 21 );
            this.cAccess.TabIndex = 1;
            this.cAccess.SelectedIndexChanged += new System.EventHandler( this.cAccess_SelectedIndexChanged );
            // 
            // cBuild
            // 
            this.cBuild.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBuild.FormattingEnabled = true;
            this.cBuild.Location = new System.Drawing.Point( 140, 65 );
            this.cBuild.Name = "cBuild";
            this.cBuild.Size = new System.Drawing.Size( 132, 21 );
            this.cBuild.TabIndex = 2;
            this.cBuild.SelectedIndexChanged += new System.EventHandler( this.cBuild_SelectedIndexChanged );
            // 
            // lName
            // 
            this.lName.AutoSize = true;
            this.lName.Location = new System.Drawing.Point( 99, 15 );
            this.lName.Name = "lName";
            this.lName.Size = new System.Drawing.Size( 35, 13 );
            this.lName.TabIndex = 21;
            this.lName.Text = "Name";
            // 
            // lAccess
            // 
            this.lAccess.AutoSize = true;
            this.lAccess.Location = new System.Drawing.Point( 39, 41 );
            this.lAccess.Name = "lAccess";
            this.lAccess.Size = new System.Drawing.Size( 95, 13 );
            this.lAccess.TabIndex = 22;
            this.lAccess.Text = "Access Permission";
            // 
            // lBuild
            // 
            this.lBuild.AutoSize = true;
            this.lBuild.Location = new System.Drawing.Point( 51, 68 );
            this.lBuild.Name = "lBuild";
            this.lBuild.Size = new System.Drawing.Size( 83, 13 );
            this.lBuild.TabIndex = 23;
            this.lBuild.Text = "Build Permission";
            // 
            // lBackup
            // 
            this.lBackup.AutoSize = true;
            this.lBackup.Location = new System.Drawing.Point( 49, 95 );
            this.lBackup.Name = "lBackup";
            this.lBackup.Size = new System.Drawing.Size( 85, 13 );
            this.lBackup.TabIndex = 24;
            this.lBackup.Text = "Backup Settings";
            // 
            // tName
            // 
            this.tName.Location = new System.Drawing.Point( 140, 12 );
            this.tName.Name = "tName";
            this.tName.Size = new System.Drawing.Size( 132, 20 );
            this.tName.TabIndex = 0;
            this.tName.TextChanged += new System.EventHandler( this.tName_TextChanged );
            this.tName.Validating += new System.ComponentModel.CancelEventHandler( this.tName_Validating );
            // 
            // bPreviewPrev
            // 
            this.bPreviewPrev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bPreviewPrev.Location = new System.Drawing.Point( 130, 339 );
            this.bPreviewPrev.Name = "bPreviewPrev";
            this.bPreviewPrev.Size = new System.Drawing.Size( 22, 22 );
            this.bPreviewPrev.TabIndex = 0;
            this.bPreviewPrev.Text = "<";
            this.bPreviewPrev.UseVisualStyleBackColor = true;
            this.bPreviewPrev.Click += new System.EventHandler( this.bPreviewPrev_Click );
            // 
            // bPreviewNext
            // 
            this.bPreviewNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bPreviewNext.Location = new System.Drawing.Point( 218, 339 );
            this.bPreviewNext.Name = "bPreviewNext";
            this.bPreviewNext.Size = new System.Drawing.Size( 22, 22 );
            this.bPreviewNext.TabIndex = 1;
            this.bPreviewNext.Text = ">";
            this.bPreviewNext.UseVisualStyleBackColor = true;
            this.bPreviewNext.Click += new System.EventHandler( this.bPreviewNext_Click );
            // 
            // xHidden
            // 
            this.xHidden.AutoSize = true;
            this.xHidden.Location = new System.Drawing.Point( 140, 119 );
            this.xHidden.Name = "xHidden";
            this.xHidden.Size = new System.Drawing.Size( 132, 17 );
            this.xHidden.TabIndex = 4;
            this.xHidden.Text = "Hide from the world list";
            this.xHidden.UseVisualStyleBackColor = true;
            // 
            // fileBrowser
            // 
            this.fileBrowser.Filter = "Minecraft Maps|*.fcm;*.lvl;*.dat;*.mclevel";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.tStatus1,
            this.tStatus2,
            this.progressBar} );
            this.statusStrip1.Location = new System.Drawing.Point( 0, 417 );
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size( 767, 22 );
            this.statusStrip1.TabIndex = 29;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tStatus1
            // 
            this.tStatus1.Name = "tStatus1";
            this.tStatus1.Size = new System.Drawing.Size( 44, 17 );
            this.tStatus1.Text = "status1";
            // 
            // tStatus2
            // 
            this.tStatus2.Name = "tStatus2";
            this.tStatus2.Size = new System.Drawing.Size( 44, 17 );
            this.tStatus2.Text = "status2";
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size( 100, 16 );
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;
            // 
            // preview
            // 
            this.preview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.preview.BackColor = System.Drawing.Color.Black;
            this.previewLayout.SetColumnSpan( this.preview, 3 );
            this.preview.Location = new System.Drawing.Point( 3, 3 );
            this.preview.Name = "preview";
            this.preview.Padding = new System.Windows.Forms.Padding( 5 );
            this.preview.Size = new System.Drawing.Size( 365, 330 );
            this.preview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.preview.TabIndex = 30;
            this.preview.TabStop = false;
            // 
            // previewLayout
            // 
            this.previewLayout.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.previewLayout.ColumnCount = 3;
            this.previewLayout.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle( System.Windows.Forms.SizeType.Percent, 50F ) );
            this.previewLayout.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle( System.Windows.Forms.SizeType.Absolute, 60F ) );
            this.previewLayout.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle( System.Windows.Forms.SizeType.Percent, 50F ) );
            this.previewLayout.Controls.Add( this.preview, 0, 0 );
            this.previewLayout.Controls.Add( this.bPreviewPrev, 0, 1 );
            this.previewLayout.Controls.Add( this.bPreviewNext, 2, 1 );
            this.previewLayout.Controls.Add( this.lPreview, 1, 1 );
            this.previewLayout.Location = new System.Drawing.Point( 384, 12 );
            this.previewLayout.Name = "previewLayout";
            this.previewLayout.RowCount = 2;
            this.previewLayout.RowStyles.Add( new System.Windows.Forms.RowStyle( System.Windows.Forms.SizeType.Percent, 100F ) );
            this.previewLayout.RowStyles.Add( new System.Windows.Forms.RowStyle( System.Windows.Forms.SizeType.Absolute, 28F ) );
            this.previewLayout.Size = new System.Drawing.Size( 371, 364 );
            this.previewLayout.TabIndex = 6;
            // 
            // AddWorldPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 767, 439 );
            this.Controls.Add( this.previewLayout );
            this.Controls.Add( this.statusStrip1 );
            this.Controls.Add( this.xHidden );
            this.Controls.Add( this.tName );
            this.Controls.Add( this.lBackup );
            this.Controls.Add( this.lBuild );
            this.Controls.Add( this.lAccess );
            this.Controls.Add( this.lName );
            this.Controls.Add( this.cBuild );
            this.Controls.Add( this.cAccess );
            this.Controls.Add( this.cBackup );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.gMap );
            this.Name = "AddWorldPopup";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Add World";
            ((System.ComponentModel.ISupportInitialize)(this.nWidthX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nWidthY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nHeight)).EndInit();
            this.gMap.ResumeLayout( false );
            this.gMap.PerformLayout();
            this.statusStrip1.ResumeLayout( false );
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.preview)).EndInit();
            this.previewLayout.ResumeLayout( false );
            this.previewLayout.PerformLayout();
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lDim;
        private System.Windows.Forms.Label lX2;
        private System.Windows.Forms.Label lX1;
        private System.Windows.Forms.RadioButton rEmpty;
        private System.Windows.Forms.RadioButton rFlatgrass;
        private System.Windows.Forms.RadioButton rTerrain;
        private System.Windows.Forms.NumericUpDown nWidthX;
        private System.Windows.Forms.NumericUpDown nWidthY;
        private System.Windows.Forms.NumericUpDown nHeight;
        private System.Windows.Forms.GroupBox gMap;
        private System.Windows.Forms.Label lPreview;
        private System.Windows.Forms.Button bGenerate;
        private System.Windows.Forms.ComboBox cWorld;
        private System.Windows.Forms.TextBox tFile;
        private System.Windows.Forms.Button bBrowse;
        private System.Windows.Forms.RadioButton rLoad;
        private System.Windows.Forms.RadioButton rCopy;
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
        private System.Windows.Forms.ComboBox cTheme;
        private System.Windows.Forms.Label lTheme;
        private System.Windows.Forms.ComboBox cTerrain;
        private System.Windows.Forms.Label lTerrain;
        private System.Windows.Forms.Button bPreviewPrev;
        private System.Windows.Forms.Button bPreviewNext;
        private System.Windows.Forms.CheckBox xFloodBarrier;
        private System.Windows.Forms.CheckBox xHidden;
        private System.Windows.Forms.OpenFileDialog fileBrowser;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.ToolStripStatusLabel tStatus1;
        private System.Windows.Forms.PictureBox preview;
        private System.Windows.Forms.ToolStripStatusLabel tStatus2;
        private System.Windows.Forms.TableLayoutPanel previewLayout;
    }
}