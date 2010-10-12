namespace ConfigTool {
    partial class ConfigUI {
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( ConfigUI ) );
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.gInformation = new System.Windows.Forms.GroupBox();
            this.lAnnouncementsUnits = new System.Windows.Forms.Label();
            this.nAnnouncements = new System.Windows.Forms.NumericUpDown();
            this.xAnnouncements = new System.Windows.Forms.CheckBox();
            this.bRules = new System.Windows.Forms.Button();
            this.bAnnouncements = new System.Windows.Forms.Button();
            this.gAppearence = new System.Windows.Forms.GroupBox();
            this.xShowJoinedWorldMessages = new System.Windows.Forms.CheckBox();
            this.xRankColorsInWorldNames = new System.Windows.Forms.CheckBox();
            this.bColorPM = new System.Windows.Forms.Button();
            this.lColorPM = new System.Windows.Forms.Label();
            this.bColorAnnouncement = new System.Windows.Forms.Button();
            this.lColorAnnouncement = new System.Windows.Forms.Label();
            this.bColorSay = new System.Windows.Forms.Button();
            this.bColorHelp = new System.Windows.Forms.Button();
            this.bColorSys = new System.Windows.Forms.Button();
            this.xListPrefixes = new System.Windows.Forms.CheckBox();
            this.xChatPrefixes = new System.Windows.Forms.CheckBox();
            this.xRankColors = new System.Windows.Forms.CheckBox();
            this.lColorSay = new System.Windows.Forms.Label();
            this.lColorHelp = new System.Windows.Forms.Label();
            this.lColorSys = new System.Windows.Forms.Label();
            this.gBasic = new System.Windows.Forms.GroupBox();
            this.tIP = new System.Windows.Forms.TextBox();
            this.xIP = new System.Windows.Forms.CheckBox();
            this.bPortCheck = new System.Windows.Forms.Button();
            this.lPort = new System.Windows.Forms.Label();
            this.nPort = new System.Windows.Forms.NumericUpDown();
            this.cDefaultRank = new System.Windows.Forms.ComboBox();
            this.lDefaultRank = new System.Windows.Forms.Label();
            this.lUploadBandwidth = new System.Windows.Forms.Label();
            this.bMeasure = new System.Windows.Forms.Button();
            this.tServerName = new System.Windows.Forms.TextBox();
            this.lUploadBandwidthUnits = new System.Windows.Forms.Label();
            this.lServerName = new System.Windows.Forms.Label();
            this.nUploadBandwidth = new System.Windows.Forms.NumericUpDown();
            this.tMOTD = new System.Windows.Forms.TextBox();
            this.lMOTD = new System.Windows.Forms.Label();
            this.cPublic = new System.Windows.Forms.ComboBox();
            this.nMaxPlayers = new System.Windows.Forms.NumericUpDown();
            this.lPublic = new System.Windows.Forms.Label();
            this.lMaxPlayers = new System.Windows.Forms.Label();
            this.tabWorlds = new System.Windows.Forms.TabPage();
            this.cMainWorld = new System.Windows.Forms.ComboBox();
            this.lMainWorld = new System.Windows.Forms.Label();
            this.bWorldEdit = new System.Windows.Forms.Button();
            this.bAddWorld = new System.Windows.Forms.Button();
            this.bWorldDelete = new System.Windows.Forms.Button();
            this.dgvWorlds = new System.Windows.Forms.DataGridView();
            this.dgvcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvcDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvcHidden = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dgvcAccess = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dgvcBuild = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dgvcBackup = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.tabRanks = new System.Windows.Forms.TabPage();
            this.lRankList = new System.Windows.Forms.Label();
            this.bLowerRank = new System.Windows.Forms.Button();
            this.bRaiseRank = new System.Windows.Forms.Button();
            this.gRankOptions = new System.Windows.Forms.GroupBox();
            this.cFreezeLimit = new System.Windows.Forms.ComboBox();
            this.lFreezeLimit = new System.Windows.Forms.Label();
            this.cMaxHideFrom = new System.Windows.Forms.ComboBox();
            this.lMaxHideFrom = new System.Windows.Forms.Label();
            this.lAntiGrief1 = new System.Windows.Forms.Label();
            this.lAntiGrief3 = new System.Windows.Forms.Label();
            this.nAntiGriefSeconds = new System.Windows.Forms.NumericUpDown();
            this.bColorRank = new System.Windows.Forms.Button();
            this.xDrawLimit = new System.Windows.Forms.CheckBox();
            this.lDrawLimitUnits = new System.Windows.Forms.Label();
            this.lKickIdleUnits = new System.Windows.Forms.Label();
            this.nDrawLimit = new System.Windows.Forms.NumericUpDown();
            this.nKickIdle = new System.Windows.Forms.NumericUpDown();
            this.xAntiGrief = new System.Windows.Forms.CheckBox();
            this.lAntiGrief2 = new System.Windows.Forms.Label();
            this.xKickIdle = new System.Windows.Forms.CheckBox();
            this.nAntiGriefBlocks = new System.Windows.Forms.NumericUpDown();
            this.xReserveSlot = new System.Windows.Forms.CheckBox();
            this.cBanLimit = new System.Windows.Forms.ComboBox();
            this.cKickLimit = new System.Windows.Forms.ComboBox();
            this.cDemoteLimit = new System.Windows.Forms.ComboBox();
            this.cPromoteLimit = new System.Windows.Forms.ComboBox();
            this.lBanLimit = new System.Windows.Forms.Label();
            this.lKickLimit = new System.Windows.Forms.Label();
            this.lDemoteLimit = new System.Windows.Forms.Label();
            this.lPromoteLimit = new System.Windows.Forms.Label();
            this.tPrefix = new System.Windows.Forms.TextBox();
            this.lPrefix = new System.Windows.Forms.Label();
            this.lRankColor = new System.Windows.Forms.Label();
            this.tRankName = new System.Windows.Forms.TextBox();
            this.lRankName = new System.Windows.Forms.Label();
            this.bDeleteRank = new System.Windows.Forms.Button();
            this.vPermissions = new System.Windows.Forms.ListView();
            this.chPermissions = new System.Windows.Forms.ColumnHeader();
            this.bAddRank = new System.Windows.Forms.Button();
            this.lPermissions = new System.Windows.Forms.Label();
            this.vRanks = new System.Windows.Forms.ListBox();
            this.tabSecurity = new System.Windows.Forms.TabPage();
            this.gSecurityMisc = new System.Windows.Forms.GroupBox();
            this.lPatrolledRankAndBelow = new System.Windows.Forms.Label();
            this.cPatrolledRank = new System.Windows.Forms.ComboBox();
            this.lPatrolledRank = new System.Windows.Forms.Label();
            this.xAnnounceRankChanges = new System.Windows.Forms.CheckBox();
            this.xAnnounceKickAndBanReasons = new System.Windows.Forms.CheckBox();
            this.xRequireRankChangeReason = new System.Windows.Forms.CheckBox();
            this.xRequireBanReason = new System.Windows.Forms.CheckBox();
            this.gSpamChat = new System.Windows.Forms.GroupBox();
            this.lSpamChatWarnings = new System.Windows.Forms.Label();
            this.nSpamChatWarnings = new System.Windows.Forms.NumericUpDown();
            this.xSpamChatKick = new System.Windows.Forms.CheckBox();
            this.lSpamMuteSeconds = new System.Windows.Forms.Label();
            this.lSpamChatSeconds = new System.Windows.Forms.Label();
            this.nSpamMute = new System.Windows.Forms.NumericUpDown();
            this.lSpamMute = new System.Windows.Forms.Label();
            this.nSpamChatTimer = new System.Windows.Forms.NumericUpDown();
            this.lSpamChatMessages = new System.Windows.Forms.Label();
            this.nSpamChatCount = new System.Windows.Forms.NumericUpDown();
            this.lSpamChat = new System.Windows.Forms.Label();
            this.gVerify = new System.Windows.Forms.GroupBox();
            this.xLimitOneConnectionPerIP = new System.Windows.Forms.CheckBox();
            this.lVerifyNames = new System.Windows.Forms.Label();
            this.cVerifyNames = new System.Windows.Forms.ComboBox();
            this.tabSavingAndBackup = new System.Windows.Forms.TabPage();
            this.gSaving = new System.Windows.Forms.GroupBox();
            this.xSaveOnShutdown = new System.Windows.Forms.CheckBox();
            this.nSaveInterval = new System.Windows.Forms.NumericUpDown();
            this.lSaveIntervalUnits = new System.Windows.Forms.Label();
            this.xSaveInterval = new System.Windows.Forms.CheckBox();
            this.gBackups = new System.Windows.Forms.GroupBox();
            this.xBackupOnlyWhenChanged = new System.Windows.Forms.CheckBox();
            this.lMaxBackupSize = new System.Windows.Forms.Label();
            this.xMaxBackupSize = new System.Windows.Forms.CheckBox();
            this.nMaxBackupSize = new System.Windows.Forms.NumericUpDown();
            this.xMaxBackups = new System.Windows.Forms.CheckBox();
            this.xBackupOnStartup = new System.Windows.Forms.CheckBox();
            this.lMaxBackups = new System.Windows.Forms.Label();
            this.nMaxBackups = new System.Windows.Forms.NumericUpDown();
            this.nBackupInterval = new System.Windows.Forms.NumericUpDown();
            this.lBackupIntervalUnits = new System.Windows.Forms.Label();
            this.xBackupInterval = new System.Windows.Forms.CheckBox();
            this.xBackupOnJoin = new System.Windows.Forms.CheckBox();
            this.tabLogging = new System.Windows.Forms.TabPage();
            this.gLogFile = new System.Windows.Forms.GroupBox();
            this.xLogLimit = new System.Windows.Forms.CheckBox();
            this.lLogFileOptions = new System.Windows.Forms.Label();
            this.vLogFileOptions = new System.Windows.Forms.ListView();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.lLogLimitUnits = new System.Windows.Forms.Label();
            this.nLogLimit = new System.Windows.Forms.NumericUpDown();
            this.cLogMode = new System.Windows.Forms.ComboBox();
            this.lLogMode = new System.Windows.Forms.Label();
            this.gConsole = new System.Windows.Forms.GroupBox();
            this.lConsoleOptions = new System.Windows.Forms.Label();
            this.vConsoleOptions = new System.Windows.Forms.ListView();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.tabIRC = new System.Windows.Forms.TabPage();
            this.gIRCOptions = new System.Windows.Forms.GroupBox();
            this.lIRCNoForwardingMessage = new System.Windows.Forms.Label();
            this.xIRCBotAnnounceIRCJoins = new System.Windows.Forms.CheckBox();
            this.bColorIRC = new System.Windows.Forms.Button();
            this.lColorIRC = new System.Windows.Forms.Label();
            this.xIRCBotForwardFromIRC = new System.Windows.Forms.CheckBox();
            this.xIRCBotAnnounceServerJoins = new System.Windows.Forms.CheckBox();
            this.xIRCBotForwardFromServer = new System.Windows.Forms.CheckBox();
            this.gIRCNetwork = new System.Windows.Forms.GroupBox();
            this.lIRCDelayUnits = new System.Windows.Forms.Label();
            this.xIRCRegisteredNick = new System.Windows.Forms.CheckBox();
            this.tIRCNickServMessage = new System.Windows.Forms.TextBox();
            this.lIRCNickServMessage = new System.Windows.Forms.Label();
            this.tIRCNickServ = new System.Windows.Forms.TextBox();
            this.lIRCNickServ = new System.Windows.Forms.Label();
            this.nIRCDelay = new System.Windows.Forms.NumericUpDown();
            this.lIRCDelay = new System.Windows.Forms.Label();
            this.lIRCBotChannels2 = new System.Windows.Forms.Label();
            this.lIRCBotChannels3 = new System.Windows.Forms.Label();
            this.tIRCBotChannels = new System.Windows.Forms.TextBox();
            this.lIRCBotChannels = new System.Windows.Forms.Label();
            this.nIRCBotPort = new System.Windows.Forms.NumericUpDown();
            this.lIRCBotPort = new System.Windows.Forms.Label();
            this.tIRCBotNetwork = new System.Windows.Forms.TextBox();
            this.lIRCBotNetwork = new System.Windows.Forms.Label();
            this.lIRCBotNick = new System.Windows.Forms.Label();
            this.tIRCBotNick = new System.Windows.Forms.TextBox();
            this.xIRC = new System.Windows.Forms.CheckBox();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lAdvancedWarning = new System.Windows.Forms.Label();
            this.lTickInterval = new System.Windows.Forms.Label();
            this.xLowLatencyMode = new System.Windows.Forms.CheckBox();
            this.nTickInterval = new System.Windows.Forms.NumericUpDown();
            this.cUpdater = new System.Windows.Forms.ComboBox();
            this.lTickIntervalUnits = new System.Windows.Forms.Label();
            this.lUpdater = new System.Windows.Forms.Label();
            this.xRedundantPacket = new System.Windows.Forms.CheckBox();
            this.lThrottlingUnits = new System.Windows.Forms.Label();
            this.lProcessPriority = new System.Windows.Forms.Label();
            this.nThrottling = new System.Windows.Forms.NumericUpDown();
            this.cProcessPriority = new System.Windows.Forms.ComboBox();
            this.lThrottling = new System.Windows.Forms.Label();
            this.xAbsoluteUpdates = new System.Windows.Forms.CheckBox();
            this.gCrashReport = new System.Windows.Forms.GroupBox();
            this.lCrashReportDisclaimer = new System.Windows.Forms.Label();
            this.xSubmitCrashReports = new System.Windows.Forms.CheckBox();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.bResetTab = new System.Windows.Forms.Button();
            this.bResetAll = new System.Windows.Forms.Button();
            this.bApply = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip( this.components );
            this.tabs.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.gInformation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAnnouncements)).BeginInit();
            this.gAppearence.SuspendLayout();
            this.gBasic.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUploadBandwidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayers)).BeginInit();
            this.tabWorlds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWorlds)).BeginInit();
            this.tabRanks.SuspendLayout();
            this.gRankOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefSeconds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nDrawLimit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickIdle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefBlocks)).BeginInit();
            this.tabSecurity.SuspendLayout();
            this.gSecurityMisc.SuspendLayout();
            this.gSpamChat.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatWarnings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamMute)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatTimer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatCount)).BeginInit();
            this.gVerify.SuspendLayout();
            this.tabSavingAndBackup.SuspendLayout();
            this.gSaving.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSaveInterval)).BeginInit();
            this.gBackups.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackupSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackups)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nBackupInterval)).BeginInit();
            this.tabLogging.SuspendLayout();
            this.gLogFile.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nLogLimit)).BeginInit();
            this.gConsole.SuspendLayout();
            this.tabIRC.SuspendLayout();
            this.gIRCOptions.SuspendLayout();
            this.gIRCNetwork.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCBotPort)).BeginInit();
            this.tabAdvanced.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nTickInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nThrottling)).BeginInit();
            this.gCrashReport.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabs
            // 
            this.tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabs.Controls.Add( this.tabGeneral );
            this.tabs.Controls.Add( this.tabWorlds );
            this.tabs.Controls.Add( this.tabRanks );
            this.tabs.Controls.Add( this.tabSecurity );
            this.tabs.Controls.Add( this.tabSavingAndBackup );
            this.tabs.Controls.Add( this.tabLogging );
            this.tabs.Controls.Add( this.tabIRC );
            this.tabs.Controls.Add( this.tabAdvanced );
            this.tabs.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.tabs.Location = new System.Drawing.Point( 12, 12 );
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size( 659, 453 );
            this.tabs.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add( this.gInformation );
            this.tabGeneral.Controls.Add( this.gAppearence );
            this.tabGeneral.Controls.Add( this.gBasic );
            this.tabGeneral.Location = new System.Drawing.Point( 4, 24 );
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabGeneral.Size = new System.Drawing.Size( 651, 425 );
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // gInformation
            // 
            this.gInformation.Controls.Add( this.lAnnouncementsUnits );
            this.gInformation.Controls.Add( this.nAnnouncements );
            this.gInformation.Controls.Add( this.xAnnouncements );
            this.gInformation.Controls.Add( this.bRules );
            this.gInformation.Controls.Add( this.bAnnouncements );
            this.gInformation.Location = new System.Drawing.Point( 8, 356 );
            this.gInformation.Name = "gInformation";
            this.gInformation.Size = new System.Drawing.Size( 635, 56 );
            this.gInformation.TabIndex = 16;
            this.gInformation.TabStop = false;
            this.gInformation.Text = "Information";
            // 
            // lAnnouncementsUnits
            // 
            this.lAnnouncementsUnits.AutoSize = true;
            this.lAnnouncementsUnits.Location = new System.Drawing.Point( 306, 27 );
            this.lAnnouncementsUnits.Name = "lAnnouncementsUnits";
            this.lAnnouncementsUnits.Size = new System.Drawing.Size( 28, 15 );
            this.lAnnouncementsUnits.TabIndex = 17;
            this.lAnnouncementsUnits.Text = "min";
            // 
            // nAnnouncements
            // 
            this.nAnnouncements.Location = new System.Drawing.Point( 250, 25 );
            this.nAnnouncements.Maximum = new decimal( new int[] {
            60,
            0,
            0,
            0} );
            this.nAnnouncements.Name = "nAnnouncements";
            this.nAnnouncements.Size = new System.Drawing.Size( 50, 21 );
            this.nAnnouncements.TabIndex = 16;
            this.nAnnouncements.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // xAnnouncements
            // 
            this.xAnnouncements.AutoSize = true;
            this.xAnnouncements.Location = new System.Drawing.Point( 24, 26 );
            this.xAnnouncements.Name = "xAnnouncements";
            this.xAnnouncements.Size = new System.Drawing.Size( 220, 19 );
            this.xAnnouncements.TabIndex = 6;
            this.xAnnouncements.Text = "Show random announcement every";
            this.xAnnouncements.UseVisualStyleBackColor = true;
            this.xAnnouncements.CheckedChanged += new System.EventHandler( this.xAnnouncements_CheckedChanged );
            // 
            // bRules
            // 
            this.bRules.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bRules.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bRules.Location = new System.Drawing.Point( 532, 20 );
            this.bRules.Name = "bRules";
            this.bRules.Size = new System.Drawing.Size( 98, 28 );
            this.bRules.TabIndex = 2;
            this.bRules.Text = "Edit Rules";
            this.bRules.UseVisualStyleBackColor = true;
            this.bRules.Click += new System.EventHandler( this.bRules_Click );
            // 
            // bAnnouncements
            // 
            this.bAnnouncements.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bAnnouncements.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bAnnouncements.Location = new System.Drawing.Point( 361, 20 );
            this.bAnnouncements.Name = "bAnnouncements";
            this.bAnnouncements.Size = new System.Drawing.Size( 165, 28 );
            this.bAnnouncements.TabIndex = 15;
            this.bAnnouncements.Text = "Edit Announcement List";
            this.bAnnouncements.UseVisualStyleBackColor = true;
            this.bAnnouncements.Click += new System.EventHandler( this.bAnnouncements_Click );
            // 
            // gAppearence
            // 
            this.gAppearence.Controls.Add( this.xShowJoinedWorldMessages );
            this.gAppearence.Controls.Add( this.xRankColorsInWorldNames );
            this.gAppearence.Controls.Add( this.bColorPM );
            this.gAppearence.Controls.Add( this.lColorPM );
            this.gAppearence.Controls.Add( this.bColorAnnouncement );
            this.gAppearence.Controls.Add( this.lColorAnnouncement );
            this.gAppearence.Controls.Add( this.bColorSay );
            this.gAppearence.Controls.Add( this.bColorHelp );
            this.gAppearence.Controls.Add( this.bColorSys );
            this.gAppearence.Controls.Add( this.xListPrefixes );
            this.gAppearence.Controls.Add( this.xChatPrefixes );
            this.gAppearence.Controls.Add( this.xRankColors );
            this.gAppearence.Controls.Add( this.lColorSay );
            this.gAppearence.Controls.Add( this.lColorHelp );
            this.gAppearence.Controls.Add( this.lColorSys );
            this.gAppearence.Location = new System.Drawing.Point( 8, 184 );
            this.gAppearence.Name = "gAppearence";
            this.gAppearence.Size = new System.Drawing.Size( 635, 166 );
            this.gAppearence.TabIndex = 1;
            this.gAppearence.TabStop = false;
            this.gAppearence.Text = "Appearence Tweaks";
            // 
            // xShowJoinedWorldMessages
            // 
            this.xShowJoinedWorldMessages.AutoSize = true;
            this.xShowJoinedWorldMessages.Location = new System.Drawing.Point( 24, 23 );
            this.xShowJoinedWorldMessages.Name = "xShowJoinedWorldMessages";
            this.xShowJoinedWorldMessages.Size = new System.Drawing.Size( 219, 19 );
            this.xShowJoinedWorldMessages.TabIndex = 11;
            this.xShowJoinedWorldMessages.Text = "Show \"X joined world Y\" messages.";
            this.xShowJoinedWorldMessages.UseVisualStyleBackColor = true;
            // 
            // xRankColorsInWorldNames
            // 
            this.xRankColorsInWorldNames.AutoSize = true;
            this.xRankColorsInWorldNames.Location = new System.Drawing.Point( 24, 81 );
            this.xRankColorsInWorldNames.Name = "xRankColorsInWorldNames";
            this.xRankColorsInWorldNames.Size = new System.Drawing.Size( 243, 19 );
            this.xRankColorsInWorldNames.TabIndex = 10;
            this.xRankColorsInWorldNames.Text = "Color world names based on build rank.";
            this.xRankColorsInWorldNames.UseVisualStyleBackColor = true;
            // 
            // bColorPM
            // 
            this.bColorPM.BackColor = System.Drawing.Color.White;
            this.bColorPM.Location = new System.Drawing.Point( 517, 136 );
            this.bColorPM.Name = "bColorPM";
            this.bColorPM.Size = new System.Drawing.Size( 100, 23 );
            this.bColorPM.TabIndex = 9;
            this.bColorPM.UseVisualStyleBackColor = false;
            this.bColorPM.Click += new System.EventHandler( this.bColorPM_Click );
            // 
            // lColorPM
            // 
            this.lColorPM.AutoSize = true;
            this.lColorPM.Location = new System.Drawing.Point( 350, 140 );
            this.lColorPM.Name = "lColorPM";
            this.lColorPM.Size = new System.Drawing.Size( 161, 15 );
            this.lColorPM.TabIndex = 8;
            this.lColorPM.Text = "Private / rank message color";
            // 
            // bColorAnnouncement
            // 
            this.bColorAnnouncement.BackColor = System.Drawing.Color.White;
            this.bColorAnnouncement.Location = new System.Drawing.Point( 517, 107 );
            this.bColorAnnouncement.Name = "bColorAnnouncement";
            this.bColorAnnouncement.Size = new System.Drawing.Size( 100, 23 );
            this.bColorAnnouncement.TabIndex = 7;
            this.bColorAnnouncement.UseVisualStyleBackColor = false;
            this.bColorAnnouncement.Click += new System.EventHandler( this.bColorAnnouncement_Click );
            // 
            // lColorAnnouncement
            // 
            this.lColorAnnouncement.AutoSize = true;
            this.lColorAnnouncement.Location = new System.Drawing.Point( 355, 111 );
            this.lColorAnnouncement.Name = "lColorAnnouncement";
            this.lColorAnnouncement.Size = new System.Drawing.Size( 156, 15 );
            this.lColorAnnouncement.TabIndex = 6;
            this.lColorAnnouncement.Text = "Announcement / rules color";
            // 
            // bColorSay
            // 
            this.bColorSay.BackColor = System.Drawing.Color.White;
            this.bColorSay.Location = new System.Drawing.Point( 517, 78 );
            this.bColorSay.Name = "bColorSay";
            this.bColorSay.Size = new System.Drawing.Size( 100, 23 );
            this.bColorSay.TabIndex = 5;
            this.bColorSay.UseVisualStyleBackColor = false;
            this.bColorSay.Click += new System.EventHandler( this.bColorSay_Click );
            // 
            // bColorHelp
            // 
            this.bColorHelp.BackColor = System.Drawing.Color.White;
            this.bColorHelp.Location = new System.Drawing.Point( 517, 49 );
            this.bColorHelp.Name = "bColorHelp";
            this.bColorHelp.Size = new System.Drawing.Size( 100, 23 );
            this.bColorHelp.TabIndex = 4;
            this.bColorHelp.UseVisualStyleBackColor = false;
            this.bColorHelp.Click += new System.EventHandler( this.bColorHelp_Click );
            // 
            // bColorSys
            // 
            this.bColorSys.BackColor = System.Drawing.Color.White;
            this.bColorSys.Location = new System.Drawing.Point( 517, 20 );
            this.bColorSys.Name = "bColorSys";
            this.bColorSys.Size = new System.Drawing.Size( 100, 23 );
            this.bColorSys.TabIndex = 3;
            this.bColorSys.UseVisualStyleBackColor = false;
            this.bColorSys.Click += new System.EventHandler( this.bColorSys_Click );
            // 
            // xListPrefixes
            // 
            this.xListPrefixes.AutoSize = true;
            this.xListPrefixes.Location = new System.Drawing.Point( 40, 139 );
            this.xListPrefixes.Name = "xListPrefixes";
            this.xListPrefixes.Size = new System.Drawing.Size( 219, 19 );
            this.xListPrefixes.TabIndex = 2;
            this.xListPrefixes.Text = "Prefixes in player list (breaks skins).";
            this.xListPrefixes.UseVisualStyleBackColor = true;
            // 
            // xChatPrefixes
            // 
            this.xChatPrefixes.AutoSize = true;
            this.xChatPrefixes.Location = new System.Drawing.Point( 24, 110 );
            this.xChatPrefixes.Name = "xChatPrefixes";
            this.xChatPrefixes.Size = new System.Drawing.Size( 133, 19 );
            this.xChatPrefixes.TabIndex = 1;
            this.xChatPrefixes.Text = "Show rank prefixes.";
            this.xChatPrefixes.UseVisualStyleBackColor = true;
            // 
            // xRankColors
            // 
            this.xRankColors.AutoSize = true;
            this.xRankColors.Location = new System.Drawing.Point( 24, 52 );
            this.xRankColors.Name = "xRankColors";
            this.xRankColors.Size = new System.Drawing.Size( 123, 19 );
            this.xRankColors.TabIndex = 0;
            this.xRankColors.Text = "Show rank colors.";
            this.xRankColors.UseVisualStyleBackColor = true;
            // 
            // lColorSay
            // 
            this.lColorSay.AutoSize = true;
            this.lColorSay.Location = new System.Drawing.Point( 400, 82 );
            this.lColorSay.Name = "lColorSay";
            this.lColorSay.Size = new System.Drawing.Size( 111, 15 );
            this.lColorSay.TabIndex = 2;
            this.lColorSay.Text = "Say message color";
            // 
            // lColorHelp
            // 
            this.lColorHelp.AutoSize = true;
            this.lColorHelp.Location = new System.Drawing.Point( 394, 53 );
            this.lColorHelp.Name = "lColorHelp";
            this.lColorHelp.Size = new System.Drawing.Size( 117, 15 );
            this.lColorHelp.TabIndex = 1;
            this.lColorHelp.Text = "Help message color";
            // 
            // lColorSys
            // 
            this.lColorSys.AutoSize = true;
            this.lColorSys.Location = new System.Drawing.Point( 380, 24 );
            this.lColorSys.Name = "lColorSys";
            this.lColorSys.Size = new System.Drawing.Size( 131, 15 );
            this.lColorSys.TabIndex = 0;
            this.lColorSys.Text = "System message color";
            // 
            // gBasic
            // 
            this.gBasic.Controls.Add( this.tIP );
            this.gBasic.Controls.Add( this.xIP );
            this.gBasic.Controls.Add( this.bPortCheck );
            this.gBasic.Controls.Add( this.lPort );
            this.gBasic.Controls.Add( this.nPort );
            this.gBasic.Controls.Add( this.cDefaultRank );
            this.gBasic.Controls.Add( this.lDefaultRank );
            this.gBasic.Controls.Add( this.lUploadBandwidth );
            this.gBasic.Controls.Add( this.bMeasure );
            this.gBasic.Controls.Add( this.tServerName );
            this.gBasic.Controls.Add( this.lUploadBandwidthUnits );
            this.gBasic.Controls.Add( this.lServerName );
            this.gBasic.Controls.Add( this.nUploadBandwidth );
            this.gBasic.Controls.Add( this.tMOTD );
            this.gBasic.Controls.Add( this.lMOTD );
            this.gBasic.Controls.Add( this.cPublic );
            this.gBasic.Controls.Add( this.nMaxPlayers );
            this.gBasic.Controls.Add( this.lPublic );
            this.gBasic.Controls.Add( this.lMaxPlayers );
            this.gBasic.Location = new System.Drawing.Point( 8, 13 );
            this.gBasic.Name = "gBasic";
            this.gBasic.Size = new System.Drawing.Size( 635, 165 );
            this.gBasic.TabIndex = 0;
            this.gBasic.TabStop = false;
            this.gBasic.Text = "Basic Settings";
            // 
            // tIP
            // 
            this.tIP.Location = new System.Drawing.Point( 440, 132 );
            this.tIP.MaxLength = 15;
            this.tIP.Name = "tIP";
            this.tIP.Size = new System.Drawing.Size( 97, 21 );
            this.tIP.TabIndex = 35;
            this.tIP.Validating += new System.ComponentModel.CancelEventHandler( this.tIP_Validating );
            // 
            // xIP
            // 
            this.xIP.AutoSize = true;
            this.xIP.Location = new System.Drawing.Point( 331, 134 );
            this.xIP.Name = "xIP";
            this.xIP.Size = new System.Drawing.Size( 103, 19 );
            this.xIP.TabIndex = 34;
            this.xIP.Text = "Designated IP";
            this.xIP.UseVisualStyleBackColor = true;
            this.xIP.CheckedChanged += new System.EventHandler( this.xIP_CheckedChanged );
            // 
            // bPortCheck
            // 
            this.bPortCheck.Location = new System.Drawing.Point( 517, 103 );
            this.bPortCheck.Name = "bPortCheck";
            this.bPortCheck.Size = new System.Drawing.Size( 68, 23 );
            this.bPortCheck.TabIndex = 33;
            this.bPortCheck.Text = "Check";
            this.bPortCheck.UseVisualStyleBackColor = true;
            this.bPortCheck.Click += new System.EventHandler( this.bPortCheck_Click );
            // 
            // lPort
            // 
            this.lPort.AutoSize = true;
            this.lPort.Location = new System.Drawing.Point( 359, 107 );
            this.lPort.Name = "lPort";
            this.lPort.Size = new System.Drawing.Size( 75, 15 );
            this.lPort.TabIndex = 32;
            this.lPort.Text = "Port number";
            // 
            // nPort
            // 
            this.nPort.Location = new System.Drawing.Point( 440, 105 );
            this.nPort.Maximum = new decimal( new int[] {
            65535,
            0,
            0,
            0} );
            this.nPort.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nPort.Name = "nPort";
            this.nPort.Size = new System.Drawing.Size( 71, 21 );
            this.nPort.TabIndex = 7;
            this.nPort.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // cDefaultRank
            // 
            this.cDefaultRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cDefaultRank.FormattingEnabled = true;
            this.cDefaultRank.Location = new System.Drawing.Point( 440, 74 );
            this.cDefaultRank.Name = "cDefaultRank";
            this.cDefaultRank.Size = new System.Drawing.Size( 189, 23 );
            this.cDefaultRank.TabIndex = 6;
            // 
            // lDefaultRank
            // 
            this.lDefaultRank.AutoSize = true;
            this.lDefaultRank.Location = new System.Drawing.Point( 361, 77 );
            this.lDefaultRank.Name = "lDefaultRank";
            this.lDefaultRank.Size = new System.Drawing.Size( 73, 15 );
            this.lDefaultRank.TabIndex = 12;
            this.lDefaultRank.Text = "Default rank";
            // 
            // lUploadBandwidth
            // 
            this.lUploadBandwidth.AutoSize = true;
            this.lUploadBandwidth.Location = new System.Drawing.Point( 7, 107 );
            this.lUploadBandwidth.Name = "lUploadBandwidth";
            this.lUploadBandwidth.Size = new System.Drawing.Size( 107, 15 );
            this.lUploadBandwidth.TabIndex = 8;
            this.lUploadBandwidth.Text = "Upload bandwidth";
            // 
            // bMeasure
            // 
            this.bMeasure.Location = new System.Drawing.Point( 244, 103 );
            this.bMeasure.Name = "bMeasure";
            this.bMeasure.Size = new System.Drawing.Size( 65, 23 );
            this.bMeasure.TabIndex = 5;
            this.bMeasure.Text = "Measure";
            this.bMeasure.UseVisualStyleBackColor = true;
            this.bMeasure.Click += new System.EventHandler( this.bMeasure_Click );
            // 
            // tServerName
            // 
            this.tServerName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tServerName.Location = new System.Drawing.Point( 120, 20 );
            this.tServerName.MaxLength = 64;
            this.tServerName.Name = "tServerName";
            this.tServerName.Size = new System.Drawing.Size( 509, 21 );
            this.tServerName.TabIndex = 0;
            // 
            // lUploadBandwidthUnits
            // 
            this.lUploadBandwidthUnits.AutoSize = true;
            this.lUploadBandwidthUnits.Location = new System.Drawing.Point( 206, 107 );
            this.lUploadBandwidthUnits.Name = "lUploadBandwidthUnits";
            this.lUploadBandwidthUnits.Size = new System.Drawing.Size( 32, 15 );
            this.lUploadBandwidthUnits.TabIndex = 10;
            this.lUploadBandwidthUnits.Text = "KB/s";
            // 
            // lServerName
            // 
            this.lServerName.AutoSize = true;
            this.lServerName.Location = new System.Drawing.Point( 37, 23 );
            this.lServerName.Name = "lServerName";
            this.lServerName.Size = new System.Drawing.Size( 77, 15 );
            this.lServerName.TabIndex = 1;
            this.lServerName.Text = "Server name";
            // 
            // nUploadBandwidth
            // 
            this.nUploadBandwidth.Increment = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nUploadBandwidth.Location = new System.Drawing.Point( 120, 105 );
            this.nUploadBandwidth.Maximum = new decimal( new int[] {
            10000,
            0,
            0,
            0} );
            this.nUploadBandwidth.Minimum = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nUploadBandwidth.Name = "nUploadBandwidth";
            this.nUploadBandwidth.Size = new System.Drawing.Size( 80, 21 );
            this.nUploadBandwidth.TabIndex = 4;
            this.nUploadBandwidth.Value = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            // 
            // tMOTD
            // 
            this.tMOTD.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tMOTD.Location = new System.Drawing.Point( 120, 47 );
            this.tMOTD.MaxLength = 64;
            this.tMOTD.Name = "tMOTD";
            this.tMOTD.Size = new System.Drawing.Size( 509, 21 );
            this.tMOTD.TabIndex = 1;
            // 
            // lMOTD
            // 
            this.lMOTD.AutoSize = true;
            this.lMOTD.Location = new System.Drawing.Point( 71, 50 );
            this.lMOTD.Name = "lMOTD";
            this.lMOTD.Size = new System.Drawing.Size( 43, 15 );
            this.lMOTD.TabIndex = 3;
            this.lMOTD.Text = "MOTD";
            // 
            // cPublic
            // 
            this.cPublic.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPublic.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.cPublic.FormattingEnabled = true;
            this.cPublic.Items.AddRange( new object[] {
            "Public",
            "Private"} );
            this.cPublic.Location = new System.Drawing.Point( 120, 132 );
            this.cPublic.Name = "cPublic";
            this.cPublic.Size = new System.Drawing.Size( 80, 23 );
            this.cPublic.TabIndex = 3;
            // 
            // nMaxPlayers
            // 
            this.nMaxPlayers.Location = new System.Drawing.Point( 120, 75 );
            this.nMaxPlayers.Maximum = new decimal( new int[] {
            255,
            0,
            0,
            0} );
            this.nMaxPlayers.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nMaxPlayers.Name = "nMaxPlayers";
            this.nMaxPlayers.Size = new System.Drawing.Size( 48, 21 );
            this.nMaxPlayers.TabIndex = 2;
            this.nMaxPlayers.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lPublic
            // 
            this.lPublic.AutoSize = true;
            this.lPublic.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.lPublic.Location = new System.Drawing.Point( 54, 135 );
            this.lPublic.Name = "lPublic";
            this.lPublic.Size = new System.Drawing.Size( 60, 15 );
            this.lPublic.TabIndex = 6;
            this.lPublic.Text = "Visibility";
            // 
            // lMaxPlayers
            // 
            this.lMaxPlayers.AutoSize = true;
            this.lMaxPlayers.Location = new System.Drawing.Point( 41, 77 );
            this.lMaxPlayers.Name = "lMaxPlayers";
            this.lMaxPlayers.Size = new System.Drawing.Size( 73, 15 );
            this.lMaxPlayers.TabIndex = 5;
            this.lMaxPlayers.Text = "Max players";
            // 
            // tabWorlds
            // 
            this.tabWorlds.Controls.Add( this.cMainWorld );
            this.tabWorlds.Controls.Add( this.lMainWorld );
            this.tabWorlds.Controls.Add( this.bWorldEdit );
            this.tabWorlds.Controls.Add( this.bAddWorld );
            this.tabWorlds.Controls.Add( this.bWorldDelete );
            this.tabWorlds.Controls.Add( this.dgvWorlds );
            this.tabWorlds.Location = new System.Drawing.Point( 4, 24 );
            this.tabWorlds.Name = "tabWorlds";
            this.tabWorlds.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabWorlds.Size = new System.Drawing.Size( 651, 425 );
            this.tabWorlds.TabIndex = 9;
            this.tabWorlds.Text = "Worlds";
            this.tabWorlds.UseVisualStyleBackColor = true;
            // 
            // cMainWorld
            // 
            this.cMainWorld.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cMainWorld.Location = new System.Drawing.Point( 371, 17 );
            this.cMainWorld.Name = "cMainWorld";
            this.cMainWorld.Size = new System.Drawing.Size( 102, 23 );
            this.cMainWorld.TabIndex = 5;
            // 
            // lMainWorld
            // 
            this.lMainWorld.AutoSize = true;
            this.lMainWorld.Location = new System.Drawing.Point( 294, 20 );
            this.lMainWorld.Name = "lMainWorld";
            this.lMainWorld.Size = new System.Drawing.Size( 71, 15 );
            this.lMainWorld.TabIndex = 4;
            this.lMainWorld.Text = "Main world:";
            // 
            // bWorldEdit
            // 
            this.bWorldEdit.Enabled = false;
            this.bWorldEdit.Location = new System.Drawing.Point( 135, 13 );
            this.bWorldEdit.Name = "bWorldEdit";
            this.bWorldEdit.Size = new System.Drawing.Size( 120, 28 );
            this.bWorldEdit.TabIndex = 2;
            this.bWorldEdit.Text = "Edit";
            this.bWorldEdit.UseVisualStyleBackColor = true;
            this.bWorldEdit.Click += new System.EventHandler( this.bWorldEdit_Click );
            // 
            // bAddWorld
            // 
            this.bAddWorld.Location = new System.Drawing.Point( 8, 13 );
            this.bAddWorld.Name = "bAddWorld";
            this.bAddWorld.Size = new System.Drawing.Size( 120, 28 );
            this.bAddWorld.TabIndex = 1;
            this.bAddWorld.Text = "Add World";
            this.bAddWorld.UseVisualStyleBackColor = true;
            this.bAddWorld.Click += new System.EventHandler( this.bAddWorld_Click );
            // 
            // bWorldDelete
            // 
            this.bWorldDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bWorldDelete.Enabled = false;
            this.bWorldDelete.Location = new System.Drawing.Point( 523, 13 );
            this.bWorldDelete.Name = "bWorldDelete";
            this.bWorldDelete.Size = new System.Drawing.Size( 120, 28 );
            this.bWorldDelete.TabIndex = 3;
            this.bWorldDelete.Text = "Delete World";
            this.bWorldDelete.UseVisualStyleBackColor = true;
            this.bWorldDelete.Click += new System.EventHandler( this.bWorldDel_Click );
            // 
            // dgvWorlds
            // 
            this.dgvWorlds.AllowUserToAddRows = false;
            this.dgvWorlds.AllowUserToDeleteRows = false;
            this.dgvWorlds.AllowUserToOrderColumns = true;
            this.dgvWorlds.AllowUserToResizeRows = false;
            this.dgvWorlds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvWorlds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWorlds.Columns.AddRange( new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvcName,
            this.dgvcDescription,
            this.dgvcHidden,
            this.dgvcAccess,
            this.dgvcBuild,
            this.dgvcBackup} );
            this.dgvWorlds.Location = new System.Drawing.Point( 9, 47 );
            this.dgvWorlds.MultiSelect = false;
            this.dgvWorlds.Name = "dgvWorlds";
            this.dgvWorlds.RowHeadersVisible = false;
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding( 0, 1, 0, 1 );
            this.dgvWorlds.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvWorlds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvWorlds.Size = new System.Drawing.Size( 634, 365 );
            this.dgvWorlds.TabIndex = 0;
            this.dgvWorlds.SelectionChanged += new System.EventHandler( this.dgvWorlds_SelectionChanged );
            // 
            // dgvcName
            // 
            this.dgvcName.DataPropertyName = "Name";
            this.dgvcName.HeaderText = "World Name";
            this.dgvcName.Name = "dgvcName";
            this.dgvcName.Width = 110;
            // 
            // dgvcDescription
            // 
            this.dgvcDescription.DataPropertyName = "Description";
            this.dgvcDescription.HeaderText = "";
            this.dgvcDescription.Name = "dgvcDescription";
            this.dgvcDescription.ReadOnly = true;
            this.dgvcDescription.Width = 180;
            // 
            // dgvcHidden
            // 
            this.dgvcHidden.DataPropertyName = "Hidden";
            this.dgvcHidden.HeaderText = "Hide";
            this.dgvcHidden.Name = "dgvcHidden";
            this.dgvcHidden.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dgvcHidden.Width = 40;
            // 
            // dgvcAccess
            // 
            this.dgvcAccess.DataPropertyName = "AccessPermission";
            this.dgvcAccess.HeaderText = "Access";
            this.dgvcAccess.Name = "dgvcAccess";
            this.dgvcAccess.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // dgvcBuild
            // 
            this.dgvcBuild.DataPropertyName = "BuildPermission";
            this.dgvcBuild.HeaderText = "Build";
            this.dgvcBuild.Name = "dgvcBuild";
            this.dgvcBuild.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // dgvcBackup
            // 
            this.dgvcBackup.DataPropertyName = "Backup";
            this.dgvcBackup.HeaderText = "Backup";
            this.dgvcBackup.Name = "dgvcBackup";
            this.dgvcBackup.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // tabRanks
            // 
            this.tabRanks.Controls.Add( this.lRankList );
            this.tabRanks.Controls.Add( this.bLowerRank );
            this.tabRanks.Controls.Add( this.bRaiseRank );
            this.tabRanks.Controls.Add( this.gRankOptions );
            this.tabRanks.Controls.Add( this.bDeleteRank );
            this.tabRanks.Controls.Add( this.vPermissions );
            this.tabRanks.Controls.Add( this.bAddRank );
            this.tabRanks.Controls.Add( this.lPermissions );
            this.tabRanks.Controls.Add( this.vRanks );
            this.tabRanks.Location = new System.Drawing.Point( 4, 24 );
            this.tabRanks.Name = "tabRanks";
            this.tabRanks.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabRanks.Size = new System.Drawing.Size( 651, 425 );
            this.tabRanks.TabIndex = 2;
            this.tabRanks.Text = "Ranks";
            this.tabRanks.UseVisualStyleBackColor = true;
            // 
            // lRankList
            // 
            this.lRankList.AutoSize = true;
            this.lRankList.Location = new System.Drawing.Point( 8, 10 );
            this.lRankList.Name = "lRankList";
            this.lRankList.Size = new System.Drawing.Size( 58, 15 );
            this.lRankList.TabIndex = 8;
            this.lRankList.Text = "Rank List";
            // 
            // bLowerRank
            // 
            this.bLowerRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bLowerRank.Location = new System.Drawing.Point( 84, 389 );
            this.bLowerRank.Name = "bLowerRank";
            this.bLowerRank.Size = new System.Drawing.Size( 70, 23 );
            this.bLowerRank.TabIndex = 7;
            this.bLowerRank.Text = "▼ Lower";
            this.bLowerRank.UseVisualStyleBackColor = true;
            this.bLowerRank.Click += new System.EventHandler( this.bLowerRank_Click );
            // 
            // bRaiseRank
            // 
            this.bRaiseRank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bRaiseRank.Location = new System.Drawing.Point( 8, 389 );
            this.bRaiseRank.Name = "bRaiseRank";
            this.bRaiseRank.Size = new System.Drawing.Size( 70, 23 );
            this.bRaiseRank.TabIndex = 6;
            this.bRaiseRank.Text = "▲ Raise";
            this.bRaiseRank.UseVisualStyleBackColor = true;
            this.bRaiseRank.Click += new System.EventHandler( this.bRaiseRank_Click );
            // 
            // gRankOptions
            // 
            this.gRankOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gRankOptions.Controls.Add( this.cFreezeLimit );
            this.gRankOptions.Controls.Add( this.lFreezeLimit );
            this.gRankOptions.Controls.Add( this.cMaxHideFrom );
            this.gRankOptions.Controls.Add( this.lMaxHideFrom );
            this.gRankOptions.Controls.Add( this.lAntiGrief1 );
            this.gRankOptions.Controls.Add( this.lAntiGrief3 );
            this.gRankOptions.Controls.Add( this.nAntiGriefSeconds );
            this.gRankOptions.Controls.Add( this.bColorRank );
            this.gRankOptions.Controls.Add( this.xDrawLimit );
            this.gRankOptions.Controls.Add( this.lDrawLimitUnits );
            this.gRankOptions.Controls.Add( this.lKickIdleUnits );
            this.gRankOptions.Controls.Add( this.nDrawLimit );
            this.gRankOptions.Controls.Add( this.nKickIdle );
            this.gRankOptions.Controls.Add( this.xAntiGrief );
            this.gRankOptions.Controls.Add( this.lAntiGrief2 );
            this.gRankOptions.Controls.Add( this.xKickIdle );
            this.gRankOptions.Controls.Add( this.nAntiGriefBlocks );
            this.gRankOptions.Controls.Add( this.xReserveSlot );
            this.gRankOptions.Controls.Add( this.cBanLimit );
            this.gRankOptions.Controls.Add( this.cKickLimit );
            this.gRankOptions.Controls.Add( this.cDemoteLimit );
            this.gRankOptions.Controls.Add( this.cPromoteLimit );
            this.gRankOptions.Controls.Add( this.lBanLimit );
            this.gRankOptions.Controls.Add( this.lKickLimit );
            this.gRankOptions.Controls.Add( this.lDemoteLimit );
            this.gRankOptions.Controls.Add( this.lPromoteLimit );
            this.gRankOptions.Controls.Add( this.tPrefix );
            this.gRankOptions.Controls.Add( this.lPrefix );
            this.gRankOptions.Controls.Add( this.lRankColor );
            this.gRankOptions.Controls.Add( this.tRankName );
            this.gRankOptions.Controls.Add( this.lRankName );
            this.gRankOptions.Location = new System.Drawing.Point( 160, 13 );
            this.gRankOptions.Name = "gRankOptions";
            this.gRankOptions.Size = new System.Drawing.Size( 306, 399 );
            this.gRankOptions.TabIndex = 1;
            this.gRankOptions.TabStop = false;
            this.gRankOptions.Text = "Rank Options";
            // 
            // cFreezeLimit
            // 
            this.cFreezeLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cFreezeLimit.FormattingEnabled = true;
            this.cFreezeLimit.Location = new System.Drawing.Point( 96, 222 );
            this.cFreezeLimit.Name = "cFreezeLimit";
            this.cFreezeLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cFreezeLimit.TabIndex = 25;
            this.cFreezeLimit.SelectedIndexChanged += new System.EventHandler( this.cFreezeLimit_SelectedIndexChanged );
            // 
            // lFreezeLimit
            // 
            this.lFreezeLimit.AutoSize = true;
            this.lFreezeLimit.Location = new System.Drawing.Point( 19, 225 );
            this.lFreezeLimit.Name = "lFreezeLimit";
            this.lFreezeLimit.Size = new System.Drawing.Size( 71, 15 );
            this.lFreezeLimit.TabIndex = 26;
            this.lFreezeLimit.Text = "Freeze limit";
            // 
            // cMaxHideFrom
            // 
            this.cMaxHideFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cMaxHideFrom.FormattingEnabled = true;
            this.cMaxHideFrom.Location = new System.Drawing.Point( 96, 193 );
            this.cMaxHideFrom.Name = "cMaxHideFrom";
            this.cMaxHideFrom.Size = new System.Drawing.Size( 180, 23 );
            this.cMaxHideFrom.TabIndex = 23;
            this.cMaxHideFrom.SelectedIndexChanged += new System.EventHandler( this.cMaxHideFrom_SelectedIndexChanged );
            // 
            // lMaxHideFrom
            // 
            this.lMaxHideFrom.AutoSize = true;
            this.lMaxHideFrom.Location = new System.Drawing.Point( 6, 196 );
            this.lMaxHideFrom.Name = "lMaxHideFrom";
            this.lMaxHideFrom.Size = new System.Drawing.Size( 84, 15 );
            this.lMaxHideFrom.TabIndex = 24;
            this.lMaxHideFrom.Text = "Can hide from";
            // 
            // lAntiGrief1
            // 
            this.lAntiGrief1.AutoSize = true;
            this.lAntiGrief1.Location = new System.Drawing.Point( 43, 340 );
            this.lAntiGrief1.Name = "lAntiGrief1";
            this.lAntiGrief1.Size = new System.Drawing.Size( 47, 15 );
            this.lAntiGrief1.TabIndex = 22;
            this.lAntiGrief1.Text = "Kick on";
            // 
            // lAntiGrief3
            // 
            this.lAntiGrief3.AutoSize = true;
            this.lAntiGrief3.Location = new System.Drawing.Point( 265, 340 );
            this.lAntiGrief3.Name = "lAntiGrief3";
            this.lAntiGrief3.Size = new System.Drawing.Size( 26, 15 );
            this.lAntiGrief3.TabIndex = 21;
            this.lAntiGrief3.Text = "sec";
            // 
            // nAntiGriefSeconds
            // 
            this.nAntiGriefSeconds.Location = new System.Drawing.Point( 214, 338 );
            this.nAntiGriefSeconds.Name = "nAntiGriefSeconds";
            this.nAntiGriefSeconds.Size = new System.Drawing.Size( 45, 21 );
            this.nAntiGriefSeconds.TabIndex = 20;
            this.nAntiGriefSeconds.ValueChanged += new System.EventHandler( this.nAntiGriefSeconds_ValueChanged );
            // 
            // bColorRank
            // 
            this.bColorRank.BackColor = System.Drawing.Color.White;
            this.bColorRank.Location = new System.Drawing.Point( 96, 47 );
            this.bColorRank.Name = "bColorRank";
            this.bColorRank.Size = new System.Drawing.Size( 100, 24 );
            this.bColorRank.TabIndex = 2;
            this.bColorRank.UseVisualStyleBackColor = false;
            this.bColorRank.Click += new System.EventHandler( this.bColorRank_Click );
            // 
            // xDrawLimit
            // 
            this.xDrawLimit.AutoSize = true;
            this.xDrawLimit.Location = new System.Drawing.Point( 9, 373 );
            this.xDrawLimit.Name = "xDrawLimit";
            this.xDrawLimit.Size = new System.Drawing.Size( 81, 19 );
            this.xDrawLimit.TabIndex = 13;
            this.xDrawLimit.Text = "Draw limit";
            this.xDrawLimit.UseVisualStyleBackColor = true;
            this.xDrawLimit.CheckedChanged += new System.EventHandler( this.xDrawLimit_CheckedChanged );
            // 
            // lDrawLimitUnits
            // 
            this.lDrawLimitUnits.AutoSize = true;
            this.lDrawLimitUnits.Location = new System.Drawing.Point( 169, 374 );
            this.lDrawLimitUnits.Name = "lDrawLimitUnits";
            this.lDrawLimitUnits.Size = new System.Drawing.Size( 42, 15 );
            this.lDrawLimitUnits.TabIndex = 8;
            this.lDrawLimitUnits.Text = "blocks";
            // 
            // lKickIdleUnits
            // 
            this.lKickIdleUnits.AutoSize = true;
            this.lKickIdleUnits.Location = new System.Drawing.Point( 178, 284 );
            this.lKickIdleUnits.Name = "lKickIdleUnits";
            this.lKickIdleUnits.Size = new System.Drawing.Size( 51, 15 );
            this.lKickIdleUnits.TabIndex = 19;
            this.lKickIdleUnits.Text = "minutes";
            // 
            // nDrawLimit
            // 
            this.nDrawLimit.Increment = new decimal( new int[] {
            32,
            0,
            0,
            0} );
            this.nDrawLimit.Location = new System.Drawing.Point( 96, 372 );
            this.nDrawLimit.Maximum = new decimal( new int[] {
            100000000,
            0,
            0,
            0} );
            this.nDrawLimit.Name = "nDrawLimit";
            this.nDrawLimit.Size = new System.Drawing.Size( 67, 21 );
            this.nDrawLimit.TabIndex = 14;
            this.nDrawLimit.ValueChanged += new System.EventHandler( this.nDrawLimit_ValueChanged );
            // 
            // nKickIdle
            // 
            this.nKickIdle.Location = new System.Drawing.Point( 113, 282 );
            this.nKickIdle.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nKickIdle.Name = "nKickIdle";
            this.nKickIdle.Size = new System.Drawing.Size( 59, 21 );
            this.nKickIdle.TabIndex = 10;
            this.nKickIdle.ValueChanged += new System.EventHandler( this.nKickIdle_ValueChanged );
            // 
            // xAntiGrief
            // 
            this.xAntiGrief.AutoSize = true;
            this.xAntiGrief.Location = new System.Drawing.Point( 9, 313 );
            this.xAntiGrief.Name = "xAntiGrief";
            this.xAntiGrief.Size = new System.Drawing.Size( 213, 19 );
            this.xAntiGrief.TabIndex = 11;
            this.xAntiGrief.Text = "Enable grief / autoclicker detection";
            this.xAntiGrief.UseVisualStyleBackColor = true;
            this.xAntiGrief.CheckedChanged += new System.EventHandler( this.xAntiGrief_CheckedChanged );
            // 
            // lAntiGrief2
            // 
            this.lAntiGrief2.AutoSize = true;
            this.lAntiGrief2.Location = new System.Drawing.Point( 153, 340 );
            this.lAntiGrief2.Name = "lAntiGrief2";
            this.lAntiGrief2.Size = new System.Drawing.Size( 55, 15 );
            this.lAntiGrief2.TabIndex = 5;
            this.lAntiGrief2.Text = "blocks in";
            // 
            // xKickIdle
            // 
            this.xKickIdle.AutoSize = true;
            this.xKickIdle.Location = new System.Drawing.Point( 9, 283 );
            this.xKickIdle.Name = "xKickIdle";
            this.xKickIdle.Size = new System.Drawing.Size( 98, 19 );
            this.xKickIdle.TabIndex = 9;
            this.xKickIdle.Text = "Kick if idle for";
            this.xKickIdle.UseVisualStyleBackColor = true;
            this.xKickIdle.CheckedChanged += new System.EventHandler( this.xKickIdle_CheckedChanged );
            // 
            // nAntiGriefBlocks
            // 
            this.nAntiGriefBlocks.Location = new System.Drawing.Point( 96, 338 );
            this.nAntiGriefBlocks.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nAntiGriefBlocks.Name = "nAntiGriefBlocks";
            this.nAntiGriefBlocks.Size = new System.Drawing.Size( 51, 21 );
            this.nAntiGriefBlocks.TabIndex = 12;
            this.nAntiGriefBlocks.ValueChanged += new System.EventHandler( this.nAntiGriefBlocks_ValueChanged );
            // 
            // xReserveSlot
            // 
            this.xReserveSlot.AutoSize = true;
            this.xReserveSlot.Location = new System.Drawing.Point( 9, 253 );
            this.xReserveSlot.Name = "xReserveSlot";
            this.xReserveSlot.Size = new System.Drawing.Size( 129, 19 );
            this.xReserveSlot.TabIndex = 8;
            this.xReserveSlot.Text = "Reserve player slot";
            this.xReserveSlot.UseVisualStyleBackColor = true;
            this.xReserveSlot.CheckedChanged += new System.EventHandler( this.xReserveSlot_CheckedChanged );
            // 
            // cBanLimit
            // 
            this.cBanLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBanLimit.FormattingEnabled = true;
            this.cBanLimit.Location = new System.Drawing.Point( 96, 106 );
            this.cBanLimit.Name = "cBanLimit";
            this.cBanLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cBanLimit.TabIndex = 7;
            this.cBanLimit.SelectedIndexChanged += new System.EventHandler( this.cBanLimit_SelectedIndexChanged );
            // 
            // cKickLimit
            // 
            this.cKickLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cKickLimit.FormattingEnabled = true;
            this.cKickLimit.Location = new System.Drawing.Point( 96, 77 );
            this.cKickLimit.Name = "cKickLimit";
            this.cKickLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cKickLimit.TabIndex = 6;
            this.cKickLimit.SelectedIndexChanged += new System.EventHandler( this.cKickLimit_SelectedIndexChanged );
            // 
            // cDemoteLimit
            // 
            this.cDemoteLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cDemoteLimit.FormattingEnabled = true;
            this.cDemoteLimit.Location = new System.Drawing.Point( 96, 164 );
            this.cDemoteLimit.Name = "cDemoteLimit";
            this.cDemoteLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cDemoteLimit.TabIndex = 5;
            this.cDemoteLimit.SelectedIndexChanged += new System.EventHandler( this.cDemoteLimit_SelectedIndexChanged );
            // 
            // cPromoteLimit
            // 
            this.cPromoteLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPromoteLimit.FormattingEnabled = true;
            this.cPromoteLimit.Location = new System.Drawing.Point( 96, 135 );
            this.cPromoteLimit.Name = "cPromoteLimit";
            this.cPromoteLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cPromoteLimit.TabIndex = 4;
            this.cPromoteLimit.SelectedIndexChanged += new System.EventHandler( this.cPromoteLimit_SelectedIndexChanged );
            // 
            // lBanLimit
            // 
            this.lBanLimit.AutoSize = true;
            this.lBanLimit.Location = new System.Drawing.Point( 35, 109 );
            this.lBanLimit.Name = "lBanLimit";
            this.lBanLimit.Size = new System.Drawing.Size( 55, 15 );
            this.lBanLimit.TabIndex = 11;
            this.lBanLimit.Text = "Ban limit";
            // 
            // lKickLimit
            // 
            this.lKickLimit.AutoSize = true;
            this.lKickLimit.Location = new System.Drawing.Point( 34, 80 );
            this.lKickLimit.Name = "lKickLimit";
            this.lKickLimit.Size = new System.Drawing.Size( 56, 15 );
            this.lKickLimit.TabIndex = 10;
            this.lKickLimit.Text = "Kick limit";
            // 
            // lDemoteLimit
            // 
            this.lDemoteLimit.AutoSize = true;
            this.lDemoteLimit.Location = new System.Drawing.Point( 13, 167 );
            this.lDemoteLimit.Name = "lDemoteLimit";
            this.lDemoteLimit.Size = new System.Drawing.Size( 77, 15 );
            this.lDemoteLimit.TabIndex = 9;
            this.lDemoteLimit.Text = "Demote limit";
            // 
            // lPromoteLimit
            // 
            this.lPromoteLimit.AutoSize = true;
            this.lPromoteLimit.Location = new System.Drawing.Point( 10, 138 );
            this.lPromoteLimit.Name = "lPromoteLimit";
            this.lPromoteLimit.Size = new System.Drawing.Size( 80, 15 );
            this.lPromoteLimit.TabIndex = 8;
            this.lPromoteLimit.Text = "Promote limit";
            // 
            // tPrefix
            // 
            this.tPrefix.Location = new System.Drawing.Point( 254, 48 );
            this.tPrefix.MaxLength = 1;
            this.tPrefix.Name = "tPrefix";
            this.tPrefix.Size = new System.Drawing.Size( 22, 21 );
            this.tPrefix.TabIndex = 3;
            this.tPrefix.Validating += new System.ComponentModel.CancelEventHandler( this.tPrefix_Validating );
            // 
            // lPrefix
            // 
            this.lPrefix.AutoSize = true;
            this.lPrefix.Location = new System.Drawing.Point( 210, 51 );
            this.lPrefix.Name = "lPrefix";
            this.lPrefix.Size = new System.Drawing.Size( 38, 15 );
            this.lPrefix.TabIndex = 6;
            this.lPrefix.Text = "Prefix";
            // 
            // lRankColor
            // 
            this.lRankColor.AutoSize = true;
            this.lRankColor.Location = new System.Drawing.Point( 54, 51 );
            this.lRankColor.Name = "lRankColor";
            this.lRankColor.Size = new System.Drawing.Size( 36, 15 );
            this.lRankColor.TabIndex = 2;
            this.lRankColor.Text = "Color";
            // 
            // tRankName
            // 
            this.tRankName.Location = new System.Drawing.Point( 96, 20 );
            this.tRankName.MaxLength = 16;
            this.tRankName.Name = "tRankName";
            this.tRankName.Size = new System.Drawing.Size( 143, 21 );
            this.tRankName.TabIndex = 0;
            this.tRankName.Validating += new System.ComponentModel.CancelEventHandler( this.tRankName_Validating );
            // 
            // lRankName
            // 
            this.lRankName.AutoSize = true;
            this.lRankName.Location = new System.Drawing.Point( 49, 23 );
            this.lRankName.Name = "lRankName";
            this.lRankName.Size = new System.Drawing.Size( 41, 15 );
            this.lRankName.TabIndex = 0;
            this.lRankName.Text = "Name";
            // 
            // bDeleteRank
            // 
            this.bDeleteRank.Location = new System.Drawing.Point( 84, 28 );
            this.bDeleteRank.Name = "bDeleteRank";
            this.bDeleteRank.Size = new System.Drawing.Size( 70, 23 );
            this.bDeleteRank.TabIndex = 4;
            this.bDeleteRank.Text = "Delete";
            this.bDeleteRank.UseVisualStyleBackColor = true;
            this.bDeleteRank.Click += new System.EventHandler( this.bDeleteRank_Click );
            // 
            // vPermissions
            // 
            this.vPermissions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.vPermissions.CheckBoxes = true;
            this.vPermissions.Columns.AddRange( new System.Windows.Forms.ColumnHeader[] {
            this.chPermissions} );
            this.vPermissions.GridLines = true;
            this.vPermissions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.vPermissions.Location = new System.Drawing.Point( 472, 28 );
            this.vPermissions.MultiSelect = false;
            this.vPermissions.Name = "vPermissions";
            this.vPermissions.ShowGroups = false;
            this.vPermissions.ShowItemToolTips = true;
            this.vPermissions.Size = new System.Drawing.Size( 171, 384 );
            this.vPermissions.TabIndex = 2;
            this.vPermissions.UseCompatibleStateImageBehavior = false;
            this.vPermissions.View = System.Windows.Forms.View.Details;
            this.vPermissions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler( this.vPermissions_ItemChecked );
            // 
            // chPermissions
            // 
            this.chPermissions.Width = 150;
            // 
            // bAddRank
            // 
            this.bAddRank.Location = new System.Drawing.Point( 8, 28 );
            this.bAddRank.Name = "bAddRank";
            this.bAddRank.Size = new System.Drawing.Size( 70, 23 );
            this.bAddRank.TabIndex = 3;
            this.bAddRank.Text = "Add Rank";
            this.bAddRank.UseVisualStyleBackColor = true;
            this.bAddRank.Click += new System.EventHandler( this.bAddRank_Click );
            // 
            // lPermissions
            // 
            this.lPermissions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lPermissions.AutoSize = true;
            this.lPermissions.Location = new System.Drawing.Point( 472, 10 );
            this.lPermissions.Name = "lPermissions";
            this.lPermissions.Size = new System.Drawing.Size( 107, 15 );
            this.lPermissions.TabIndex = 3;
            this.lPermissions.Text = "Rank Permissions";
            // 
            // vRanks
            // 
            this.vRanks.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.vRanks.Font = new System.Drawing.Font( "Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.vRanks.FormattingEnabled = true;
            this.vRanks.IntegralHeight = false;
            this.vRanks.ItemHeight = 15;
            this.vRanks.Location = new System.Drawing.Point( 8, 57 );
            this.vRanks.Name = "vRanks";
            this.vRanks.Size = new System.Drawing.Size( 146, 326 );
            this.vRanks.TabIndex = 0;
            this.vRanks.SelectedIndexChanged += new System.EventHandler( this.vRanks_SelectedIndexChanged );
            // 
            // tabSecurity
            // 
            this.tabSecurity.Controls.Add( this.gSecurityMisc );
            this.tabSecurity.Controls.Add( this.gSpamChat );
            this.tabSecurity.Controls.Add( this.gVerify );
            this.tabSecurity.Location = new System.Drawing.Point( 4, 24 );
            this.tabSecurity.Name = "tabSecurity";
            this.tabSecurity.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabSecurity.Size = new System.Drawing.Size( 651, 425 );
            this.tabSecurity.TabIndex = 7;
            this.tabSecurity.Text = "Security";
            this.tabSecurity.UseVisualStyleBackColor = true;
            // 
            // gSecurityMisc
            // 
            this.gSecurityMisc.Controls.Add( this.lPatrolledRankAndBelow );
            this.gSecurityMisc.Controls.Add( this.cPatrolledRank );
            this.gSecurityMisc.Controls.Add( this.lPatrolledRank );
            this.gSecurityMisc.Controls.Add( this.xAnnounceRankChanges );
            this.gSecurityMisc.Controls.Add( this.xAnnounceKickAndBanReasons );
            this.gSecurityMisc.Controls.Add( this.xRequireRankChangeReason );
            this.gSecurityMisc.Controls.Add( this.xRequireBanReason );
            this.gSecurityMisc.Location = new System.Drawing.Point( 8, 172 );
            this.gSecurityMisc.Name = "gSecurityMisc";
            this.gSecurityMisc.Size = new System.Drawing.Size( 635, 117 );
            this.gSecurityMisc.TabIndex = 3;
            this.gSecurityMisc.TabStop = false;
            this.gSecurityMisc.Text = "Misc";
            // 
            // lPatrolledRankAndBelow
            // 
            this.lPatrolledRankAndBelow.AutoSize = true;
            this.lPatrolledRankAndBelow.Location = new System.Drawing.Point( 242, 81 );
            this.lPatrolledRankAndBelow.Name = "lPatrolledRankAndBelow";
            this.lPatrolledRankAndBelow.Size = new System.Drawing.Size( 72, 15 );
            this.lPatrolledRankAndBelow.TabIndex = 8;
            this.lPatrolledRankAndBelow.Text = "(and below)";
            // 
            // cPatrolledRank
            // 
            this.cPatrolledRank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPatrolledRank.FormattingEnabled = true;
            this.cPatrolledRank.Location = new System.Drawing.Point( 113, 78 );
            this.cPatrolledRank.Name = "cPatrolledRank";
            this.cPatrolledRank.Size = new System.Drawing.Size( 123, 23 );
            this.cPatrolledRank.TabIndex = 7;
            // 
            // lPatrolledRank
            // 
            this.lPatrolledRank.AutoSize = true;
            this.lPatrolledRank.Location = new System.Drawing.Point( 24, 81 );
            this.lPatrolledRank.Name = "lPatrolledRank";
            this.lPatrolledRank.Size = new System.Drawing.Size( 83, 15 );
            this.lPatrolledRank.TabIndex = 6;
            this.lPatrolledRank.Text = "Patrolled rank";
            // 
            // xAnnounceRankChanges
            // 
            this.xAnnounceRankChanges.AutoSize = true;
            this.xAnnounceRankChanges.Location = new System.Drawing.Point( 304, 45 );
            this.xAnnounceRankChanges.Name = "xAnnounceRankChanges";
            this.xAnnounceRankChanges.Size = new System.Drawing.Size( 231, 19 );
            this.xAnnounceRankChanges.TabIndex = 5;
            this.xAnnounceRankChanges.Text = "Announce promotions and demotions";
            this.xAnnounceRankChanges.UseVisualStyleBackColor = true;
            // 
            // xAnnounceKickAndBanReasons
            // 
            this.xAnnounceKickAndBanReasons.AutoSize = true;
            this.xAnnounceKickAndBanReasons.Location = new System.Drawing.Point( 304, 20 );
            this.xAnnounceKickAndBanReasons.Name = "xAnnounceKickAndBanReasons";
            this.xAnnounceKickAndBanReasons.Size = new System.Drawing.Size( 244, 19 );
            this.xAnnounceKickAndBanReasons.TabIndex = 4;
            this.xAnnounceKickAndBanReasons.Text = "Announce kick, ban, and unban reasons";
            this.xAnnounceKickAndBanReasons.UseVisualStyleBackColor = true;
            // 
            // xRequireRankChangeReason
            // 
            this.xRequireRankChangeReason.AutoSize = true;
            this.xRequireRankChangeReason.Location = new System.Drawing.Point( 42, 45 );
            this.xRequireRankChangeReason.Name = "xRequireRankChangeReason";
            this.xRequireRankChangeReason.Size = new System.Drawing.Size( 225, 19 );
            this.xRequireRankChangeReason.TabIndex = 3;
            this.xRequireRankChangeReason.Text = "Require promotion/demotion reason";
            this.xRequireRankChangeReason.UseVisualStyleBackColor = true;
            // 
            // xRequireBanReason
            // 
            this.xRequireBanReason.AutoSize = true;
            this.xRequireBanReason.Location = new System.Drawing.Point( 42, 20 );
            this.xRequireBanReason.Name = "xRequireBanReason";
            this.xRequireBanReason.Size = new System.Drawing.Size( 197, 19 );
            this.xRequireBanReason.TabIndex = 2;
            this.xRequireBanReason.Text = "Require ban and unban reason";
            this.xRequireBanReason.UseVisualStyleBackColor = true;
            // 
            // gSpamChat
            // 
            this.gSpamChat.Controls.Add( this.lSpamChatWarnings );
            this.gSpamChat.Controls.Add( this.nSpamChatWarnings );
            this.gSpamChat.Controls.Add( this.xSpamChatKick );
            this.gSpamChat.Controls.Add( this.lSpamMuteSeconds );
            this.gSpamChat.Controls.Add( this.lSpamChatSeconds );
            this.gSpamChat.Controls.Add( this.nSpamMute );
            this.gSpamChat.Controls.Add( this.lSpamMute );
            this.gSpamChat.Controls.Add( this.nSpamChatTimer );
            this.gSpamChat.Controls.Add( this.lSpamChatMessages );
            this.gSpamChat.Controls.Add( this.nSpamChatCount );
            this.gSpamChat.Controls.Add( this.lSpamChat );
            this.gSpamChat.Location = new System.Drawing.Point( 8, 72 );
            this.gSpamChat.Name = "gSpamChat";
            this.gSpamChat.Size = new System.Drawing.Size( 635, 94 );
            this.gSpamChat.TabIndex = 2;
            this.gSpamChat.TabStop = false;
            this.gSpamChat.Text = "Chat Spam Prevention";
            // 
            // lSpamChatWarnings
            // 
            this.lSpamChatWarnings.AutoSize = true;
            this.lSpamChatWarnings.Location = new System.Drawing.Point( 454, 62 );
            this.lSpamChatWarnings.Name = "lSpamChatWarnings";
            this.lSpamChatWarnings.Size = new System.Drawing.Size( 57, 15 );
            this.lSpamChatWarnings.TabIndex = 12;
            this.lSpamChatWarnings.Text = "warnings";
            // 
            // nSpamChatWarnings
            // 
            this.nSpamChatWarnings.Location = new System.Drawing.Point( 386, 60 );
            this.nSpamChatWarnings.Name = "nSpamChatWarnings";
            this.nSpamChatWarnings.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamChatWarnings.TabIndex = 4;
            // 
            // xSpamChatKick
            // 
            this.xSpamChatKick.AutoSize = true;
            this.xSpamChatKick.Location = new System.Drawing.Point( 304, 61 );
            this.xSpamChatKick.Name = "xSpamChatKick";
            this.xSpamChatKick.Size = new System.Drawing.Size( 76, 19 );
            this.xSpamChatKick.TabIndex = 3;
            this.xSpamChatKick.Text = "Kick after";
            this.xSpamChatKick.UseVisualStyleBackColor = true;
            this.xSpamChatKick.CheckedChanged += new System.EventHandler( this.xSpamChatKick_CheckedChanged );
            // 
            // lSpamMuteSeconds
            // 
            this.lSpamMuteSeconds.AutoSize = true;
            this.lSpamMuteSeconds.Location = new System.Drawing.Point( 221, 62 );
            this.lSpamMuteSeconds.Name = "lSpamMuteSeconds";
            this.lSpamMuteSeconds.Size = new System.Drawing.Size( 53, 15 );
            this.lSpamMuteSeconds.TabIndex = 9;
            this.lSpamMuteSeconds.Text = "seconds";
            // 
            // lSpamChatSeconds
            // 
            this.lSpamChatSeconds.AutoSize = true;
            this.lSpamChatSeconds.Location = new System.Drawing.Point( 372, 27 );
            this.lSpamChatSeconds.Name = "lSpamChatSeconds";
            this.lSpamChatSeconds.Size = new System.Drawing.Size( 53, 15 );
            this.lSpamChatSeconds.TabIndex = 4;
            this.lSpamChatSeconds.Text = "seconds";
            // 
            // nSpamMute
            // 
            this.nSpamMute.Location = new System.Drawing.Point( 153, 59 );
            this.nSpamMute.Name = "nSpamMute";
            this.nSpamMute.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamMute.TabIndex = 2;
            // 
            // lSpamMute
            // 
            this.lSpamMute.AutoSize = true;
            this.lSpamMute.Location = new System.Drawing.Point( 39, 62 );
            this.lSpamMute.Name = "lSpamMute";
            this.lSpamMute.Size = new System.Drawing.Size( 108, 15 );
            this.lSpamMute.TabIndex = 7;
            this.lSpamMute.Text = "Mute spammer for";
            // 
            // nSpamChatTimer
            // 
            this.nSpamChatTimer.Location = new System.Drawing.Point( 304, 25 );
            this.nSpamChatTimer.Maximum = new decimal( new int[] {
            50,
            0,
            0,
            0} );
            this.nSpamChatTimer.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nSpamChatTimer.Name = "nSpamChatTimer";
            this.nSpamChatTimer.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamChatTimer.TabIndex = 1;
            this.nSpamChatTimer.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lSpamChatMessages
            // 
            this.lSpamChatMessages.AutoSize = true;
            this.lSpamChatMessages.Location = new System.Drawing.Point( 219, 27 );
            this.lSpamChatMessages.Name = "lSpamChatMessages";
            this.lSpamChatMessages.Size = new System.Drawing.Size( 77, 15 );
            this.lSpamChatMessages.TabIndex = 2;
            this.lSpamChatMessages.Text = "messages in";
            // 
            // nSpamChatCount
            // 
            this.nSpamChatCount.Location = new System.Drawing.Point( 153, 25 );
            this.nSpamChatCount.Maximum = new decimal( new int[] {
            50,
            0,
            0,
            0} );
            this.nSpamChatCount.Minimum = new decimal( new int[] {
            2,
            0,
            0,
            0} );
            this.nSpamChatCount.Name = "nSpamChatCount";
            this.nSpamChatCount.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamChatCount.TabIndex = 0;
            this.nSpamChatCount.Value = new decimal( new int[] {
            2,
            0,
            0,
            0} );
            // 
            // lSpamChat
            // 
            this.lSpamChat.AutoSize = true;
            this.lSpamChat.Location = new System.Drawing.Point( 63, 27 );
            this.lSpamChat.Name = "lSpamChat";
            this.lSpamChat.Size = new System.Drawing.Size( 84, 15 );
            this.lSpamChat.TabIndex = 0;
            this.lSpamChat.Text = "Limit chat rate";
            // 
            // gVerify
            // 
            this.gVerify.Controls.Add( this.xLimitOneConnectionPerIP );
            this.gVerify.Controls.Add( this.lVerifyNames );
            this.gVerify.Controls.Add( this.cVerifyNames );
            this.gVerify.Location = new System.Drawing.Point( 8, 13 );
            this.gVerify.Name = "gVerify";
            this.gVerify.Size = new System.Drawing.Size( 635, 53 );
            this.gVerify.TabIndex = 0;
            this.gVerify.TabStop = false;
            this.gVerify.Text = "Name Verification";
            // 
            // xLimitOneConnectionPerIP
            // 
            this.xLimitOneConnectionPerIP.AutoSize = true;
            this.xLimitOneConnectionPerIP.Location = new System.Drawing.Point( 304, 22 );
            this.xLimitOneConnectionPerIP.Name = "xLimitOneConnectionPerIP";
            this.xLimitOneConnectionPerIP.Size = new System.Drawing.Size( 161, 19 );
            this.xLimitOneConnectionPerIP.TabIndex = 2;
            this.xLimitOneConnectionPerIP.Text = "Limit 1 connection per IP";
            this.xLimitOneConnectionPerIP.UseVisualStyleBackColor = true;
            // 
            // lVerifyNames
            // 
            this.lVerifyNames.AutoSize = true;
            this.lVerifyNames.Location = new System.Drawing.Point( 45, 23 );
            this.lVerifyNames.Name = "lVerifyNames";
            this.lVerifyNames.Size = new System.Drawing.Size( 102, 15 );
            this.lVerifyNames.TabIndex = 16;
            this.lVerifyNames.Text = "Verification mode";
            // 
            // cVerifyNames
            // 
            this.cVerifyNames.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cVerifyNames.FormattingEnabled = true;
            this.cVerifyNames.Items.AddRange( new object[] {
            "Never",
            "Balanced",
            "Strict"} );
            this.cVerifyNames.Location = new System.Drawing.Point( 153, 20 );
            this.cVerifyNames.Name = "cVerifyNames";
            this.cVerifyNames.Size = new System.Drawing.Size( 100, 23 );
            this.cVerifyNames.TabIndex = 0;
            // 
            // tabSavingAndBackup
            // 
            this.tabSavingAndBackup.Controls.Add( this.gSaving );
            this.tabSavingAndBackup.Controls.Add( this.gBackups );
            this.tabSavingAndBackup.Location = new System.Drawing.Point( 4, 24 );
            this.tabSavingAndBackup.Name = "tabSavingAndBackup";
            this.tabSavingAndBackup.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabSavingAndBackup.Size = new System.Drawing.Size( 651, 425 );
            this.tabSavingAndBackup.TabIndex = 4;
            this.tabSavingAndBackup.Text = "Saving and Backup";
            this.tabSavingAndBackup.UseVisualStyleBackColor = true;
            // 
            // gSaving
            // 
            this.gSaving.Controls.Add( this.xSaveOnShutdown );
            this.gSaving.Controls.Add( this.nSaveInterval );
            this.gSaving.Controls.Add( this.lSaveIntervalUnits );
            this.gSaving.Controls.Add( this.xSaveInterval );
            this.gSaving.Location = new System.Drawing.Point( 8, 13 );
            this.gSaving.Name = "gSaving";
            this.gSaving.Size = new System.Drawing.Size( 635, 77 );
            this.gSaving.TabIndex = 0;
            this.gSaving.TabStop = false;
            this.gSaving.Text = "Saving";
            // 
            // xSaveOnShutdown
            // 
            this.xSaveOnShutdown.AutoSize = true;
            this.xSaveOnShutdown.Location = new System.Drawing.Point( 16, 20 );
            this.xSaveOnShutdown.Name = "xSaveOnShutdown";
            this.xSaveOnShutdown.Size = new System.Drawing.Size( 160, 19 );
            this.xSaveOnShutdown.TabIndex = 0;
            this.xSaveOnShutdown.Text = "Save maps on shutdown";
            this.xSaveOnShutdown.UseVisualStyleBackColor = true;
            // 
            // nSaveInterval
            // 
            this.nSaveInterval.Location = new System.Drawing.Point( 140, 44 );
            this.nSaveInterval.Name = "nSaveInterval";
            this.nSaveInterval.Size = new System.Drawing.Size( 48, 21 );
            this.nSaveInterval.TabIndex = 2;
            // 
            // lSaveIntervalUnits
            // 
            this.lSaveIntervalUnits.AutoSize = true;
            this.lSaveIntervalUnits.Location = new System.Drawing.Point( 194, 46 );
            this.lSaveIntervalUnits.Name = "lSaveIntervalUnits";
            this.lSaveIntervalUnits.Size = new System.Drawing.Size( 53, 15 );
            this.lSaveIntervalUnits.TabIndex = 3;
            this.lSaveIntervalUnits.Text = "seconds";
            // 
            // xSaveInterval
            // 
            this.xSaveInterval.AutoSize = true;
            this.xSaveInterval.Location = new System.Drawing.Point( 16, 45 );
            this.xSaveInterval.Name = "xSaveInterval";
            this.xSaveInterval.Size = new System.Drawing.Size( 118, 19 );
            this.xSaveInterval.TabIndex = 1;
            this.xSaveInterval.Text = "Save maps every";
            this.xSaveInterval.UseVisualStyleBackColor = true;
            this.xSaveInterval.CheckedChanged += new System.EventHandler( this.xSaveAtInterval_CheckedChanged );
            // 
            // gBackups
            // 
            this.gBackups.Controls.Add( this.xBackupOnlyWhenChanged );
            this.gBackups.Controls.Add( this.lMaxBackupSize );
            this.gBackups.Controls.Add( this.xMaxBackupSize );
            this.gBackups.Controls.Add( this.nMaxBackupSize );
            this.gBackups.Controls.Add( this.xMaxBackups );
            this.gBackups.Controls.Add( this.xBackupOnStartup );
            this.gBackups.Controls.Add( this.lMaxBackups );
            this.gBackups.Controls.Add( this.nMaxBackups );
            this.gBackups.Controls.Add( this.nBackupInterval );
            this.gBackups.Controls.Add( this.lBackupIntervalUnits );
            this.gBackups.Controls.Add( this.xBackupInterval );
            this.gBackups.Controls.Add( this.xBackupOnJoin );
            this.gBackups.Location = new System.Drawing.Point( 8, 96 );
            this.gBackups.Name = "gBackups";
            this.gBackups.Size = new System.Drawing.Size( 635, 158 );
            this.gBackups.TabIndex = 1;
            this.gBackups.TabStop = false;
            this.gBackups.Text = "Backups";
            // 
            // xBackupOnlyWhenChanged
            // 
            this.xBackupOnlyWhenChanged.AutoSize = true;
            this.xBackupOnlyWhenChanged.Location = new System.Drawing.Point( 369, 46 );
            this.xBackupOnlyWhenChanged.Name = "xBackupOnlyWhenChanged";
            this.xBackupOnlyWhenChanged.Size = new System.Drawing.Size( 260, 19 );
            this.xBackupOnlyWhenChanged.TabIndex = 3;
            this.xBackupOnlyWhenChanged.Text = "Skip timed backups if map hasn\'t changed.";
            this.xBackupOnlyWhenChanged.UseVisualStyleBackColor = true;
            // 
            // lMaxBackupSize
            // 
            this.lMaxBackupSize.AutoSize = true;
            this.lMaxBackupSize.Location = new System.Drawing.Point( 418, 124 );
            this.lMaxBackupSize.Name = "lMaxBackupSize";
            this.lMaxBackupSize.Size = new System.Drawing.Size( 103, 15 );
            this.lMaxBackupSize.TabIndex = 13;
            this.lMaxBackupSize.Text = "MB of disk space.";
            // 
            // xMaxBackupSize
            // 
            this.xMaxBackupSize.AutoSize = true;
            this.xMaxBackupSize.Location = new System.Drawing.Point( 16, 123 );
            this.xMaxBackupSize.Name = "xMaxBackupSize";
            this.xMaxBackupSize.Size = new System.Drawing.Size( 317, 19 );
            this.xMaxBackupSize.TabIndex = 7;
            this.xMaxBackupSize.Text = "Delete old backups if the directory takes up more than";
            this.xMaxBackupSize.UseVisualStyleBackColor = true;
            this.xMaxBackupSize.CheckedChanged += new System.EventHandler( this.xMaxBackupSize_CheckedChanged );
            // 
            // nMaxBackupSize
            // 
            this.nMaxBackupSize.Location = new System.Drawing.Point( 339, 122 );
            this.nMaxBackupSize.Maximum = new decimal( new int[] {
            1000000,
            0,
            0,
            0} );
            this.nMaxBackupSize.Name = "nMaxBackupSize";
            this.nMaxBackupSize.Size = new System.Drawing.Size( 73, 21 );
            this.nMaxBackupSize.TabIndex = 8;
            // 
            // xMaxBackups
            // 
            this.xMaxBackups.AutoSize = true;
            this.xMaxBackups.Location = new System.Drawing.Point( 16, 98 );
            this.xMaxBackups.Name = "xMaxBackups";
            this.xMaxBackups.Size = new System.Drawing.Size( 251, 19 );
            this.xMaxBackups.TabIndex = 5;
            this.xMaxBackups.Text = "Delete old backups if there are more than";
            this.xMaxBackups.UseVisualStyleBackColor = true;
            this.xMaxBackups.CheckedChanged += new System.EventHandler( this.xMaxBackups_CheckedChanged );
            // 
            // xBackupOnStartup
            // 
            this.xBackupOnStartup.AutoSize = true;
            this.xBackupOnStartup.Enabled = false;
            this.xBackupOnStartup.Location = new System.Drawing.Point( 16, 20 );
            this.xBackupOnStartup.Name = "xBackupOnStartup";
            this.xBackupOnStartup.Size = new System.Drawing.Size( 168, 19 );
            this.xBackupOnStartup.TabIndex = 0;
            this.xBackupOnStartup.Text = "Create backups on startup";
            this.xBackupOnStartup.UseVisualStyleBackColor = true;
            // 
            // lMaxBackups
            // 
            this.lMaxBackups.AutoSize = true;
            this.lMaxBackups.Location = new System.Drawing.Point( 336, 99 );
            this.lMaxBackups.Name = "lMaxBackups";
            this.lMaxBackups.Size = new System.Drawing.Size( 157, 15 );
            this.lMaxBackups.TabIndex = 10;
            this.lMaxBackups.Text = "files in the backup directory.";
            // 
            // nMaxBackups
            // 
            this.nMaxBackups.Location = new System.Drawing.Point( 273, 97 );
            this.nMaxBackups.Maximum = new decimal( new int[] {
            100000,
            0,
            0,
            0} );
            this.nMaxBackups.Name = "nMaxBackups";
            this.nMaxBackups.Size = new System.Drawing.Size( 57, 21 );
            this.nMaxBackups.TabIndex = 6;
            // 
            // nBackupInterval
            // 
            this.nBackupInterval.Location = new System.Drawing.Point( 164, 45 );
            this.nBackupInterval.Name = "nBackupInterval";
            this.nBackupInterval.Size = new System.Drawing.Size( 48, 21 );
            this.nBackupInterval.TabIndex = 2;
            // 
            // lBackupIntervalUnits
            // 
            this.lBackupIntervalUnits.AutoSize = true;
            this.lBackupIntervalUnits.Location = new System.Drawing.Point( 218, 47 );
            this.lBackupIntervalUnits.Name = "lBackupIntervalUnits";
            this.lBackupIntervalUnits.Size = new System.Drawing.Size( 51, 15 );
            this.lBackupIntervalUnits.TabIndex = 5;
            this.lBackupIntervalUnits.Text = "minutes";
            // 
            // xBackupInterval
            // 
            this.xBackupInterval.AutoSize = true;
            this.xBackupInterval.Location = new System.Drawing.Point( 16, 46 );
            this.xBackupInterval.Name = "xBackupInterval";
            this.xBackupInterval.Size = new System.Drawing.Size( 142, 19 );
            this.xBackupInterval.TabIndex = 1;
            this.xBackupInterval.Text = "Create backups every";
            this.xBackupInterval.UseVisualStyleBackColor = true;
            this.xBackupInterval.CheckedChanged += new System.EventHandler( this.xBackupAtInterval_CheckedChanged );
            // 
            // xBackupOnJoin
            // 
            this.xBackupOnJoin.AutoSize = true;
            this.xBackupOnJoin.Location = new System.Drawing.Point( 16, 72 );
            this.xBackupOnJoin.Name = "xBackupOnJoin";
            this.xBackupOnJoin.Size = new System.Drawing.Size( 279, 19 );
            this.xBackupOnJoin.TabIndex = 4;
            this.xBackupOnJoin.Text = "Create backup whenever a player joins a world";
            this.xBackupOnJoin.UseVisualStyleBackColor = true;
            // 
            // tabLogging
            // 
            this.tabLogging.Controls.Add( this.gLogFile );
            this.tabLogging.Controls.Add( this.gConsole );
            this.tabLogging.Location = new System.Drawing.Point( 4, 24 );
            this.tabLogging.Name = "tabLogging";
            this.tabLogging.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabLogging.Size = new System.Drawing.Size( 651, 425 );
            this.tabLogging.TabIndex = 5;
            this.tabLogging.Text = "Logging";
            this.tabLogging.UseVisualStyleBackColor = true;
            // 
            // gLogFile
            // 
            this.gLogFile.Controls.Add( this.xLogLimit );
            this.gLogFile.Controls.Add( this.lLogFileOptions );
            this.gLogFile.Controls.Add( this.vLogFileOptions );
            this.gLogFile.Controls.Add( this.lLogLimitUnits );
            this.gLogFile.Controls.Add( this.nLogLimit );
            this.gLogFile.Controls.Add( this.cLogMode );
            this.gLogFile.Controls.Add( this.lLogMode );
            this.gLogFile.Location = new System.Drawing.Point( 329, 14 );
            this.gLogFile.Name = "gLogFile";
            this.gLogFile.Size = new System.Drawing.Size( 314, 398 );
            this.gLogFile.TabIndex = 1;
            this.gLogFile.TabStop = false;
            this.gLogFile.Text = "Log File";
            // 
            // xLogLimit
            // 
            this.xLogLimit.AutoSize = true;
            this.xLogLimit.Enabled = false;
            this.xLogLimit.Location = new System.Drawing.Point( 34, 325 );
            this.xLogLimit.Name = "xLogLimit";
            this.xLogLimit.Size = new System.Drawing.Size( 102, 19 );
            this.xLogLimit.TabIndex = 2;
            this.xLogLimit.Text = "Only keep last";
            this.xLogLimit.UseVisualStyleBackColor = true;
            this.xLogLimit.CheckedChanged += new System.EventHandler( this.xLogLimit_CheckedChanged );
            // 
            // lLogFileOptions
            // 
            this.lLogFileOptions.AutoSize = true;
            this.lLogFileOptions.Location = new System.Drawing.Point( 45, 20 );
            this.lLogFileOptions.Name = "lLogFileOptions";
            this.lLogFileOptions.Size = new System.Drawing.Size( 49, 15 );
            this.lLogFileOptions.TabIndex = 6;
            this.lLogFileOptions.Text = "Options";
            // 
            // vLogFileOptions
            // 
            this.vLogFileOptions.CheckBoxes = true;
            this.vLogFileOptions.Columns.AddRange( new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2} );
            this.vLogFileOptions.GridLines = true;
            this.vLogFileOptions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.vLogFileOptions.Location = new System.Drawing.Point( 100, 20 );
            this.vLogFileOptions.Name = "vLogFileOptions";
            this.vLogFileOptions.ShowItemToolTips = true;
            this.vLogFileOptions.Size = new System.Drawing.Size( 161, 270 );
            this.vLogFileOptions.TabIndex = 0;
            this.vLogFileOptions.UseCompatibleStateImageBehavior = false;
            this.vLogFileOptions.View = System.Windows.Forms.View.Details;
            this.vLogFileOptions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler( this.vLogFileOptions_ItemChecked );
            // 
            // columnHeader2
            // 
            this.columnHeader2.Width = 157;
            // 
            // lLogLimitUnits
            // 
            this.lLogLimitUnits.AutoSize = true;
            this.lLogLimitUnits.Location = new System.Drawing.Point( 210, 326 );
            this.lLogLimitUnits.Name = "lLogLimitUnits";
            this.lLogLimitUnits.Size = new System.Drawing.Size( 29, 15 );
            this.lLogLimitUnits.TabIndex = 4;
            this.lLogLimitUnits.Text = "files";
            // 
            // nLogLimit
            // 
            this.nLogLimit.Enabled = false;
            this.nLogLimit.Location = new System.Drawing.Point( 142, 324 );
            this.nLogLimit.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nLogLimit.Name = "nLogLimit";
            this.nLogLimit.Size = new System.Drawing.Size( 62, 21 );
            this.nLogLimit.TabIndex = 3;
            // 
            // cLogMode
            // 
            this.cLogMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cLogMode.FormattingEnabled = true;
            this.cLogMode.Items.AddRange( new object[] {
            "One long file",
            "Multiple files, split by session",
            "Multiple files, split by day"} );
            this.cLogMode.Location = new System.Drawing.Point( 100, 296 );
            this.cLogMode.Name = "cLogMode";
            this.cLogMode.Size = new System.Drawing.Size( 199, 23 );
            this.cLogMode.TabIndex = 1;
            // 
            // lLogMode
            // 
            this.lLogMode.AutoSize = true;
            this.lLogMode.Location = new System.Drawing.Point( 31, 299 );
            this.lLogMode.Name = "lLogMode";
            this.lLogMode.Size = new System.Drawing.Size( 63, 15 );
            this.lLogMode.TabIndex = 0;
            this.lLogMode.Text = "Log mode";
            // 
            // gConsole
            // 
            this.gConsole.Controls.Add( this.lConsoleOptions );
            this.gConsole.Controls.Add( this.vConsoleOptions );
            this.gConsole.Location = new System.Drawing.Point( 9, 14 );
            this.gConsole.Name = "gConsole";
            this.gConsole.Size = new System.Drawing.Size( 314, 398 );
            this.gConsole.TabIndex = 0;
            this.gConsole.TabStop = false;
            this.gConsole.Text = "Console";
            // 
            // lConsoleOptions
            // 
            this.lConsoleOptions.AutoSize = true;
            this.lConsoleOptions.Location = new System.Drawing.Point( 33, 20 );
            this.lConsoleOptions.Name = "lConsoleOptions";
            this.lConsoleOptions.Size = new System.Drawing.Size( 49, 15 );
            this.lConsoleOptions.TabIndex = 7;
            this.lConsoleOptions.Text = "Options";
            // 
            // vConsoleOptions
            // 
            this.vConsoleOptions.CheckBoxes = true;
            this.vConsoleOptions.Columns.AddRange( new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3} );
            this.vConsoleOptions.GridLines = true;
            this.vConsoleOptions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.vConsoleOptions.Location = new System.Drawing.Point( 88, 20 );
            this.vConsoleOptions.Name = "vConsoleOptions";
            this.vConsoleOptions.ShowItemToolTips = true;
            this.vConsoleOptions.Size = new System.Drawing.Size( 161, 270 );
            this.vConsoleOptions.TabIndex = 0;
            this.vConsoleOptions.UseCompatibleStateImageBehavior = false;
            this.vConsoleOptions.View = System.Windows.Forms.View.Details;
            this.vConsoleOptions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler( this.vConsoleOptions_ItemChecked );
            // 
            // columnHeader3
            // 
            this.columnHeader3.Width = 157;
            // 
            // tabIRC
            // 
            this.tabIRC.Controls.Add( this.gIRCOptions );
            this.tabIRC.Controls.Add( this.gIRCNetwork );
            this.tabIRC.Controls.Add( this.xIRC );
            this.tabIRC.Location = new System.Drawing.Point( 4, 24 );
            this.tabIRC.Name = "tabIRC";
            this.tabIRC.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabIRC.Size = new System.Drawing.Size( 651, 425 );
            this.tabIRC.TabIndex = 8;
            this.tabIRC.Text = "IRC";
            this.tabIRC.UseVisualStyleBackColor = true;
            // 
            // gIRCOptions
            // 
            this.gIRCOptions.Controls.Add( this.lIRCNoForwardingMessage );
            this.gIRCOptions.Controls.Add( this.xIRCBotAnnounceIRCJoins );
            this.gIRCOptions.Controls.Add( this.bColorIRC );
            this.gIRCOptions.Controls.Add( this.lColorIRC );
            this.gIRCOptions.Controls.Add( this.xIRCBotForwardFromIRC );
            this.gIRCOptions.Controls.Add( this.xIRCBotAnnounceServerJoins );
            this.gIRCOptions.Controls.Add( this.xIRCBotForwardFromServer );
            this.gIRCOptions.Location = new System.Drawing.Point( 8, 205 );
            this.gIRCOptions.Name = "gIRCOptions";
            this.gIRCOptions.Size = new System.Drawing.Size( 635, 137 );
            this.gIRCOptions.TabIndex = 2;
            this.gIRCOptions.TabStop = false;
            this.gIRCOptions.Text = "Options";
            // 
            // lIRCNoForwardingMessage
            // 
            this.lIRCNoForwardingMessage.AutoSize = true;
            this.lIRCNoForwardingMessage.Location = new System.Drawing.Point( 33, 110 );
            this.lIRCNoForwardingMessage.Name = "lIRCNoForwardingMessage";
            this.lIRCNoForwardingMessage.Size = new System.Drawing.Size( 567, 15 );
            this.lIRCNoForwardingMessage.TabIndex = 15;
            this.lIRCNoForwardingMessage.Text = "NOTE: If forwarding all messages is not enabled, only messages starting with a ha" +
                "sh (#) will be relayed.";
            // 
            // xIRCBotAnnounceIRCJoins
            // 
            this.xIRCBotAnnounceIRCJoins.AutoSize = true;
            this.xIRCBotAnnounceIRCJoins.Location = new System.Drawing.Point( 326, 79 );
            this.xIRCBotAnnounceIRCJoins.Name = "xIRCBotAnnounceIRCJoins";
            this.xIRCBotAnnounceIRCJoins.Size = new System.Drawing.Size( 303, 19 );
            this.xIRCBotAnnounceIRCJoins.TabIndex = 14;
            this.xIRCBotAnnounceIRCJoins.Text = "Announce people joining/leaving the IRC channels.";
            this.xIRCBotAnnounceIRCJoins.UseVisualStyleBackColor = true;
            // 
            // bColorIRC
            // 
            this.bColorIRC.BackColor = System.Drawing.Color.White;
            this.bColorIRC.Location = new System.Drawing.Point( 152, 20 );
            this.bColorIRC.Name = "bColorIRC";
            this.bColorIRC.Size = new System.Drawing.Size( 100, 23 );
            this.bColorIRC.TabIndex = 13;
            this.bColorIRC.UseVisualStyleBackColor = false;
            this.bColorIRC.Click += new System.EventHandler( this.bColorIRC_Click );
            // 
            // lColorIRC
            // 
            this.lColorIRC.AutoSize = true;
            this.lColorIRC.Location = new System.Drawing.Point( 35, 24 );
            this.lColorIRC.Name = "lColorIRC";
            this.lColorIRC.Size = new System.Drawing.Size( 111, 15 );
            this.lColorIRC.TabIndex = 12;
            this.lColorIRC.Text = "IRC message color";
            // 
            // xIRCBotForwardFromIRC
            // 
            this.xIRCBotForwardFromIRC.AutoSize = true;
            this.xIRCBotForwardFromIRC.Location = new System.Drawing.Point( 38, 79 );
            this.xIRCBotForwardFromIRC.Name = "xIRCBotForwardFromIRC";
            this.xIRCBotForwardFromIRC.Size = new System.Drawing.Size( 240, 19 );
            this.xIRCBotForwardFromIRC.TabIndex = 2;
            this.xIRCBotForwardFromIRC.Text = "Forward ALL chat from IRC to SERVER.";
            this.xIRCBotForwardFromIRC.UseVisualStyleBackColor = true;
            // 
            // xIRCBotAnnounceServerJoins
            // 
            this.xIRCBotAnnounceServerJoins.AutoSize = true;
            this.xIRCBotAnnounceServerJoins.Location = new System.Drawing.Point( 326, 54 );
            this.xIRCBotAnnounceServerJoins.Name = "xIRCBotAnnounceServerJoins";
            this.xIRCBotAnnounceServerJoins.Size = new System.Drawing.Size( 279, 19 );
            this.xIRCBotAnnounceServerJoins.TabIndex = 0;
            this.xIRCBotAnnounceServerJoins.Text = "Announce people joining/leaving the SERVER.";
            this.xIRCBotAnnounceServerJoins.UseVisualStyleBackColor = true;
            // 
            // xIRCBotForwardFromServer
            // 
            this.xIRCBotForwardFromServer.AutoSize = true;
            this.xIRCBotForwardFromServer.Location = new System.Drawing.Point( 38, 54 );
            this.xIRCBotForwardFromServer.Name = "xIRCBotForwardFromServer";
            this.xIRCBotForwardFromServer.Size = new System.Drawing.Size( 240, 19 );
            this.xIRCBotForwardFromServer.TabIndex = 1;
            this.xIRCBotForwardFromServer.Text = "Forward ALL chat from SERVER to IRC.";
            this.xIRCBotForwardFromServer.UseVisualStyleBackColor = true;
            // 
            // gIRCNetwork
            // 
            this.gIRCNetwork.Controls.Add( this.lIRCDelayUnits );
            this.gIRCNetwork.Controls.Add( this.xIRCRegisteredNick );
            this.gIRCNetwork.Controls.Add( this.tIRCNickServMessage );
            this.gIRCNetwork.Controls.Add( this.lIRCNickServMessage );
            this.gIRCNetwork.Controls.Add( this.tIRCNickServ );
            this.gIRCNetwork.Controls.Add( this.lIRCNickServ );
            this.gIRCNetwork.Controls.Add( this.nIRCDelay );
            this.gIRCNetwork.Controls.Add( this.lIRCDelay );
            this.gIRCNetwork.Controls.Add( this.lIRCBotChannels2 );
            this.gIRCNetwork.Controls.Add( this.lIRCBotChannels3 );
            this.gIRCNetwork.Controls.Add( this.tIRCBotChannels );
            this.gIRCNetwork.Controls.Add( this.lIRCBotChannels );
            this.gIRCNetwork.Controls.Add( this.nIRCBotPort );
            this.gIRCNetwork.Controls.Add( this.lIRCBotPort );
            this.gIRCNetwork.Controls.Add( this.tIRCBotNetwork );
            this.gIRCNetwork.Controls.Add( this.lIRCBotNetwork );
            this.gIRCNetwork.Controls.Add( this.lIRCBotNick );
            this.gIRCNetwork.Controls.Add( this.tIRCBotNick );
            this.gIRCNetwork.Location = new System.Drawing.Point( 8, 38 );
            this.gIRCNetwork.Name = "gIRCNetwork";
            this.gIRCNetwork.Size = new System.Drawing.Size( 635, 161 );
            this.gIRCNetwork.TabIndex = 1;
            this.gIRCNetwork.TabStop = false;
            this.gIRCNetwork.Text = "Network";
            // 
            // lIRCDelayUnits
            // 
            this.lIRCDelayUnits.AutoSize = true;
            this.lIRCDelayUnits.Location = new System.Drawing.Point( 605, 23 );
            this.lIRCDelayUnits.Name = "lIRCDelayUnits";
            this.lIRCDelayUnits.Size = new System.Drawing.Size( 24, 15 );
            this.lIRCDelayUnits.TabIndex = 27;
            this.lIRCDelayUnits.Text = "ms";
            // 
            // xIRCRegisteredNick
            // 
            this.xIRCRegisteredNick.AutoSize = true;
            this.xIRCRegisteredNick.Location = new System.Drawing.Point( 272, 102 );
            this.xIRCRegisteredNick.Name = "xIRCRegisteredNick";
            this.xIRCRegisteredNick.Size = new System.Drawing.Size( 86, 19 );
            this.xIRCRegisteredNick.TabIndex = 26;
            this.xIRCRegisteredNick.Text = "Registered";
            this.xIRCRegisteredNick.UseVisualStyleBackColor = true;
            this.xIRCRegisteredNick.CheckedChanged += new System.EventHandler( this.xIRCRegisteredNick_CheckedChanged );
            // 
            // tIRCNickServMessage
            // 
            this.tIRCNickServMessage.Enabled = false;
            this.tIRCNickServMessage.Location = new System.Drawing.Point( 395, 127 );
            this.tIRCNickServMessage.Name = "tIRCNickServMessage";
            this.tIRCNickServMessage.Size = new System.Drawing.Size( 234, 21 );
            this.tIRCNickServMessage.TabIndex = 25;
            // 
            // lIRCNickServMessage
            // 
            this.lIRCNickServMessage.AutoSize = true;
            this.lIRCNickServMessage.Enabled = false;
            this.lIRCNickServMessage.Location = new System.Drawing.Point( 272, 130 );
            this.lIRCNickServMessage.Name = "lIRCNickServMessage";
            this.lIRCNickServMessage.Size = new System.Drawing.Size( 117, 15 );
            this.lIRCNickServMessage.TabIndex = 24;
            this.lIRCNickServMessage.Text = "Authentication string";
            // 
            // tIRCNickServ
            // 
            this.tIRCNickServ.Enabled = false;
            this.tIRCNickServ.Location = new System.Drawing.Point( 128, 127 );
            this.tIRCNickServ.MaxLength = 32;
            this.tIRCNickServ.Name = "tIRCNickServ";
            this.tIRCNickServ.Size = new System.Drawing.Size( 138, 21 );
            this.tIRCNickServ.TabIndex = 23;
            // 
            // lIRCNickServ
            // 
            this.lIRCNickServ.AutoSize = true;
            this.lIRCNickServ.Enabled = false;
            this.lIRCNickServ.Location = new System.Drawing.Point( 42, 130 );
            this.lIRCNickServ.Name = "lIRCNickServ";
            this.lIRCNickServ.Size = new System.Drawing.Size( 80, 15 );
            this.lIRCNickServ.TabIndex = 22;
            this.lIRCNickServ.Text = "NickServ nick";
            // 
            // nIRCDelay
            // 
            this.nIRCDelay.Increment = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nIRCDelay.Location = new System.Drawing.Point( 543, 21 );
            this.nIRCDelay.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nIRCDelay.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nIRCDelay.Name = "nIRCDelay";
            this.nIRCDelay.Size = new System.Drawing.Size( 56, 21 );
            this.nIRCDelay.TabIndex = 21;
            this.nIRCDelay.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lIRCDelay
            // 
            this.lIRCDelay.AutoSize = true;
            this.lIRCDelay.Location = new System.Drawing.Point( 423, 23 );
            this.lIRCDelay.Name = "lIRCDelay";
            this.lIRCDelay.Size = new System.Drawing.Size( 114, 15 );
            this.lIRCDelay.TabIndex = 20;
            this.lIRCDelay.Text = "Min message delay";
            // 
            // lIRCBotChannels2
            // 
            this.lIRCBotChannels2.AutoSize = true;
            this.lIRCBotChannels2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.lIRCBotChannels2.Location = new System.Drawing.Point( 22, 66 );
            this.lIRCBotChannels2.Name = "lIRCBotChannels2";
            this.lIRCBotChannels2.Size = new System.Drawing.Size( 97, 13 );
            this.lIRCBotChannels2.TabIndex = 17;
            this.lIRCBotChannels2.Text = "(comma seperated)";
            // 
            // lIRCBotChannels3
            // 
            this.lIRCBotChannels3.AutoSize = true;
            this.lIRCBotChannels3.Location = new System.Drawing.Point( 125, 72 );
            this.lIRCBotChannels3.Name = "lIRCBotChannels3";
            this.lIRCBotChannels3.Size = new System.Drawing.Size( 340, 15 );
            this.lIRCBotChannels3.TabIndex = 16;
            this.lIRCBotChannels3.Text = "NOTE: Channel names are case-sensitive on some networks!";
            // 
            // tIRCBotChannels
            // 
            this.tIRCBotChannels.Location = new System.Drawing.Point( 128, 48 );
            this.tIRCBotChannels.MaxLength = 1000;
            this.tIRCBotChannels.Name = "tIRCBotChannels";
            this.tIRCBotChannels.Size = new System.Drawing.Size( 499, 21 );
            this.tIRCBotChannels.TabIndex = 2;
            // 
            // lIRCBotChannels
            // 
            this.lIRCBotChannels.AutoSize = true;
            this.lIRCBotChannels.Location = new System.Drawing.Point( 27, 51 );
            this.lIRCBotChannels.Name = "lIRCBotChannels";
            this.lIRCBotChannels.Size = new System.Drawing.Size( 95, 15 );
            this.lIRCBotChannels.TabIndex = 14;
            this.lIRCBotChannels.Text = "Channels to join";
            // 
            // nIRCBotPort
            // 
            this.nIRCBotPort.Location = new System.Drawing.Point( 307, 21 );
            this.nIRCBotPort.Maximum = new decimal( new int[] {
            65535,
            0,
            0,
            0} );
            this.nIRCBotPort.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nIRCBotPort.Name = "nIRCBotPort";
            this.nIRCBotPort.Size = new System.Drawing.Size( 64, 21 );
            this.nIRCBotPort.TabIndex = 1;
            this.nIRCBotPort.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lIRCBotPort
            // 
            this.lIRCBotPort.AutoSize = true;
            this.lIRCBotPort.Location = new System.Drawing.Point( 272, 23 );
            this.lIRCBotPort.Name = "lIRCBotPort";
            this.lIRCBotPort.Size = new System.Drawing.Size( 29, 15 );
            this.lIRCBotPort.TabIndex = 12;
            this.lIRCBotPort.Text = "Port";
            // 
            // tIRCBotNetwork
            // 
            this.tIRCBotNetwork.Location = new System.Drawing.Point( 128, 20 );
            this.tIRCBotNetwork.MaxLength = 512;
            this.tIRCBotNetwork.Name = "tIRCBotNetwork";
            this.tIRCBotNetwork.Size = new System.Drawing.Size( 138, 21 );
            this.tIRCBotNetwork.TabIndex = 0;
            // 
            // lIRCBotNetwork
            // 
            this.lIRCBotNetwork.AutoSize = true;
            this.lIRCBotNetwork.Location = new System.Drawing.Point( 33, 23 );
            this.lIRCBotNetwork.Name = "lIRCBotNetwork";
            this.lIRCBotNetwork.Size = new System.Drawing.Size( 89, 15 );
            this.lIRCBotNetwork.TabIndex = 10;
            this.lIRCBotNetwork.Text = "IRC server host";
            // 
            // lIRCBotNick
            // 
            this.lIRCBotNick.AutoSize = true;
            this.lIRCBotNick.Location = new System.Drawing.Point( 72, 103 );
            this.lIRCBotNick.Name = "lIRCBotNick";
            this.lIRCBotNick.Size = new System.Drawing.Size( 50, 15 );
            this.lIRCBotNick.TabIndex = 9;
            this.lIRCBotNick.Text = "Bot nick";
            // 
            // tIRCBotNick
            // 
            this.tIRCBotNick.Location = new System.Drawing.Point( 128, 100 );
            this.tIRCBotNick.MaxLength = 32;
            this.tIRCBotNick.Name = "tIRCBotNick";
            this.tIRCBotNick.Size = new System.Drawing.Size( 138, 21 );
            this.tIRCBotNick.TabIndex = 3;
            // 
            // xIRC
            // 
            this.xIRC.AutoSize = true;
            this.xIRC.Location = new System.Drawing.Point( 14, 13 );
            this.xIRC.Name = "xIRC";
            this.xIRC.Size = new System.Drawing.Size( 149, 19 );
            this.xIRC.TabIndex = 0;
            this.xIRC.Text = "Enable IRC integration";
            this.xIRC.UseVisualStyleBackColor = true;
            this.xIRC.CheckedChanged += new System.EventHandler( this.xIRC_CheckedChanged );
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.Controls.Add( this.groupBox1 );
            this.tabAdvanced.Controls.Add( this.gCrashReport );
            this.tabAdvanced.Location = new System.Drawing.Point( 4, 24 );
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabAdvanced.Size = new System.Drawing.Size( 651, 425 );
            this.tabAdvanced.TabIndex = 6;
            this.tabAdvanced.Text = "Advanced";
            this.tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add( this.lAdvancedWarning );
            this.groupBox1.Controls.Add( this.lTickInterval );
            this.groupBox1.Controls.Add( this.xLowLatencyMode );
            this.groupBox1.Controls.Add( this.nTickInterval );
            this.groupBox1.Controls.Add( this.cUpdater );
            this.groupBox1.Controls.Add( this.lTickIntervalUnits );
            this.groupBox1.Controls.Add( this.lUpdater );
            this.groupBox1.Controls.Add( this.xRedundantPacket );
            this.groupBox1.Controls.Add( this.lThrottlingUnits );
            this.groupBox1.Controls.Add( this.lProcessPriority );
            this.groupBox1.Controls.Add( this.nThrottling );
            this.groupBox1.Controls.Add( this.cProcessPriority );
            this.groupBox1.Controls.Add( this.lThrottling );
            this.groupBox1.Controls.Add( this.xAbsoluteUpdates );
            this.groupBox1.Location = new System.Drawing.Point( 8, 118 );
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size( 635, 294 );
            this.groupBox1.TabIndex = 42;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Miscallaneous";
            // 
            // lAdvancedWarning
            // 
            this.lAdvancedWarning.AutoSize = true;
            this.lAdvancedWarning.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.lAdvancedWarning.Location = new System.Drawing.Point( 6, 17 );
            this.lAdvancedWarning.Name = "lAdvancedWarning";
            this.lAdvancedWarning.Size = new System.Drawing.Size( 555, 30 );
            this.lAdvancedWarning.TabIndex = 0;
            this.lAdvancedWarning.Text = "Warning: Altering these settings can decrease your server\'s stability and perform" +
                "ance.\r\nIf you\'re not sure what these settings do, you probably shouldn\'t touch t" +
                "hem...";
            // 
            // lTickInterval
            // 
            this.lTickInterval.AutoSize = true;
            this.lTickInterval.Location = new System.Drawing.Point( 96, 245 );
            this.lTickInterval.Name = "lTickInterval";
            this.lTickInterval.Size = new System.Drawing.Size( 71, 15 );
            this.lTickInterval.TabIndex = 15;
            this.lTickInterval.Text = "Tick interval";
            // 
            // xLowLatencyMode
            // 
            this.xLowLatencyMode.AutoSize = true;
            this.xLowLatencyMode.Location = new System.Drawing.Point( 9, 115 );
            this.xLowLatencyMode.Name = "xLowLatencyMode";
            this.xLowLatencyMode.Size = new System.Drawing.Size( 616, 19 );
            this.xLowLatencyMode.TabIndex = 11;
            this.xLowLatencyMode.Text = "Experimental low-latency mode (disables Nagle\'s algorithm, reducing latency but i" +
                "ncreasing bandwidth use).";
            this.xLowLatencyMode.UseVisualStyleBackColor = true;
            // 
            // nTickInterval
            // 
            this.nTickInterval.Increment = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nTickInterval.Location = new System.Drawing.Point( 173, 243 );
            this.nTickInterval.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nTickInterval.Minimum = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nTickInterval.Name = "nTickInterval";
            this.nTickInterval.Size = new System.Drawing.Size( 59, 21 );
            this.nTickInterval.TabIndex = 10;
            this.nTickInterval.Value = new decimal( new int[] {
            100,
            0,
            0,
            0} );
            // 
            // cUpdater
            // 
            this.cUpdater.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cUpdater.FormattingEnabled = true;
            this.cUpdater.Items.AddRange( new object[] {
            "Disabled",
            "Notify of update availability",
            "Download and prompt to install",
            "Fully automatic"} );
            this.cUpdater.Location = new System.Drawing.Point( 173, 183 );
            this.cUpdater.Name = "cUpdater";
            this.cUpdater.Size = new System.Drawing.Size( 200, 23 );
            this.cUpdater.TabIndex = 8;
            // 
            // lTickIntervalUnits
            // 
            this.lTickIntervalUnits.AutoSize = true;
            this.lTickIntervalUnits.Location = new System.Drawing.Point( 238, 245 );
            this.lTickIntervalUnits.Name = "lTickIntervalUnits";
            this.lTickIntervalUnits.Size = new System.Drawing.Size( 24, 15 );
            this.lTickIntervalUnits.TabIndex = 17;
            this.lTickIntervalUnits.Text = "ms";
            // 
            // lUpdater
            // 
            this.lUpdater.AutoSize = true;
            this.lUpdater.Location = new System.Drawing.Point( 31, 186 );
            this.lUpdater.Name = "lUpdater";
            this.lUpdater.Size = new System.Drawing.Size( 136, 15 );
            this.lUpdater.TabIndex = 38;
            this.lUpdater.Text = "Check for fCraft updates";
            // 
            // xRedundantPacket
            // 
            this.xRedundantPacket.AutoSize = true;
            this.xRedundantPacket.Enabled = false;
            this.xRedundantPacket.Location = new System.Drawing.Point( 9, 65 );
            this.xRedundantPacket.Name = "xRedundantPacket";
            this.xRedundantPacket.Size = new System.Drawing.Size( 554, 19 );
            this.xRedundantPacket.TabIndex = 2;
            this.xRedundantPacket.Text = "When a player changes a block, send him the redundant update packet anyway (vanil" +
                "la behavior).";
            this.xRedundantPacket.UseVisualStyleBackColor = true;
            // 
            // lThrottlingUnits
            // 
            this.lThrottlingUnits.AutoSize = true;
            this.lThrottlingUnits.Location = new System.Drawing.Point( 238, 218 );
            this.lThrottlingUnits.Name = "lThrottlingUnits";
            this.lThrottlingUnits.Size = new System.Drawing.Size( 129, 15 );
            this.lThrottlingUnits.TabIndex = 37;
            this.lThrottlingUnits.Text = "blocks / second / client";
            // 
            // lProcessPriority
            // 
            this.lProcessPriority.AutoSize = true;
            this.lProcessPriority.Location = new System.Drawing.Point( 77, 157 );
            this.lProcessPriority.Name = "lProcessPriority";
            this.lProcessPriority.Size = new System.Drawing.Size( 90, 15 );
            this.lProcessPriority.TabIndex = 6;
            this.lProcessPriority.Text = "Process priority";
            // 
            // nThrottling
            // 
            this.nThrottling.Increment = new decimal( new int[] {
            100,
            0,
            0,
            0} );
            this.nThrottling.Location = new System.Drawing.Point( 173, 216 );
            this.nThrottling.Maximum = new decimal( new int[] {
            10000,
            0,
            0,
            0} );
            this.nThrottling.Minimum = new decimal( new int[] {
            100,
            0,
            0,
            0} );
            this.nThrottling.Name = "nThrottling";
            this.nThrottling.Size = new System.Drawing.Size( 59, 21 );
            this.nThrottling.TabIndex = 9;
            this.nThrottling.Value = new decimal( new int[] {
            2500,
            0,
            0,
            0} );
            // 
            // cProcessPriority
            // 
            this.cProcessPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cProcessPriority.Items.AddRange( new object[] {
            "(system default)",
            "High",
            "Above Normal",
            "Normal",
            "Below Normal",
            "Low"} );
            this.cProcessPriority.Location = new System.Drawing.Point( 173, 154 );
            this.cProcessPriority.Name = "cProcessPriority";
            this.cProcessPriority.Size = new System.Drawing.Size( 109, 23 );
            this.cProcessPriority.TabIndex = 6;
            // 
            // lThrottling
            // 
            this.lThrottling.AutoSize = true;
            this.lThrottling.Location = new System.Drawing.Point( 39, 218 );
            this.lThrottling.Name = "lThrottling";
            this.lThrottling.Size = new System.Drawing.Size( 128, 15 );
            this.lThrottling.TabIndex = 35;
            this.lThrottling.Text = "Block update throttling";
            // 
            // xAbsoluteUpdates
            // 
            this.xAbsoluteUpdates.AutoSize = true;
            this.xAbsoluteUpdates.Enabled = false;
            this.xAbsoluteUpdates.Location = new System.Drawing.Point( 9, 90 );
            this.xAbsoluteUpdates.Name = "xAbsoluteUpdates";
            this.xAbsoluteUpdates.Size = new System.Drawing.Size( 326, 19 );
            this.xAbsoluteUpdates.TabIndex = 5;
            this.xAbsoluteUpdates.Text = "Do not use partial position updates (opcodes 9, 10, 11).";
            this.xAbsoluteUpdates.UseVisualStyleBackColor = true;
            // 
            // gCrashReport
            // 
            this.gCrashReport.Controls.Add( this.lCrashReportDisclaimer );
            this.gCrashReport.Controls.Add( this.xSubmitCrashReports );
            this.gCrashReport.Location = new System.Drawing.Point( 8, 13 );
            this.gCrashReport.Name = "gCrashReport";
            this.gCrashReport.Size = new System.Drawing.Size( 635, 99 );
            this.gCrashReport.TabIndex = 41;
            this.gCrashReport.TabStop = false;
            this.gCrashReport.Text = "Crash Reporting";
            // 
            // lCrashReportDisclaimer
            // 
            this.lCrashReportDisclaimer.AutoSize = true;
            this.lCrashReportDisclaimer.Location = new System.Drawing.Point( 27, 42 );
            this.lCrashReportDisclaimer.Name = "lCrashReportDisclaimer";
            this.lCrashReportDisclaimer.Size = new System.Drawing.Size( 550, 45 );
            this.lCrashReportDisclaimer.TabIndex = 41;
            this.lCrashReportDisclaimer.Text = resources.GetString( "lCrashReportDisclaimer.Text" );
            // 
            // xSubmitCrashReports
            // 
            this.xSubmitCrashReports.AutoSize = true;
            this.xSubmitCrashReports.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.xSubmitCrashReports.Location = new System.Drawing.Point( 6, 20 );
            this.xSubmitCrashReports.Name = "xSubmitCrashReports";
            this.xSubmitCrashReports.Size = new System.Drawing.Size( 253, 19 );
            this.xSubmitCrashReports.TabIndex = 40;
            this.xSubmitCrashReports.Text = "Submit crash reports to fragmer.net";
            this.xSubmitCrashReports.UseVisualStyleBackColor = true;
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bOK.Location = new System.Drawing.Point( 355, 471 );
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size( 100, 28 );
            this.bOK.TabIndex = 1;
            this.bOK.Text = "OK";
            this.bOK.Click += new System.EventHandler( this.bSave_Click );
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bCancel.Location = new System.Drawing.Point( 461, 471 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 100, 28 );
            this.bCancel.TabIndex = 2;
            this.bCancel.Text = "Cancel";
            this.bCancel.Click += new System.EventHandler( this.bCancel_Click );
            // 
            // bResetTab
            // 
            this.bResetTab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bResetTab.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bResetTab.Location = new System.Drawing.Point( 132, 471 );
            this.bResetTab.Name = "bResetTab";
            this.bResetTab.Size = new System.Drawing.Size( 100, 28 );
            this.bResetTab.TabIndex = 5;
            this.bResetTab.Text = "Reset Tab";
            this.bResetTab.UseVisualStyleBackColor = true;
            this.bResetTab.Click += new System.EventHandler( this.bResetTab_Click );
            // 
            // bResetAll
            // 
            this.bResetAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bResetAll.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bResetAll.Location = new System.Drawing.Point( 12, 471 );
            this.bResetAll.Name = "bResetAll";
            this.bResetAll.Size = new System.Drawing.Size( 114, 28 );
            this.bResetAll.TabIndex = 4;
            this.bResetAll.Text = "Reset All Defaults";
            this.bResetAll.UseVisualStyleBackColor = true;
            this.bResetAll.Click += new System.EventHandler( this.bResetAll_Click );
            // 
            // bApply
            // 
            this.bApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bApply.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bApply.Location = new System.Drawing.Point( 567, 471 );
            this.bApply.Name = "bApply";
            this.bApply.Size = new System.Drawing.Size( 100, 28 );
            this.bApply.TabIndex = 3;
            this.bApply.Text = "Apply";
            this.bApply.Click += new System.EventHandler( this.bApply_Click );
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 10000;
            this.toolTip.InitialDelay = 500;
            this.toolTip.ReshowDelay = 100;
            // 
            // ConfigUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 684, 511 );
            this.Controls.Add( this.bApply );
            this.Controls.Add( this.bResetAll );
            this.Controls.Add( this.bResetTab );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.tabs );
            this.Icon = ((System.Drawing.Icon)(resources.GetObject( "$this.Icon" )));
            this.MinimumSize = new System.Drawing.Size( 700, 547 );
            this.Name = "ConfigUI";
            this.Text = "fCraft Config Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.ConfigUI_FormClosing );
            this.tabs.ResumeLayout( false );
            this.tabGeneral.ResumeLayout( false );
            this.gInformation.ResumeLayout( false );
            this.gInformation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAnnouncements)).EndInit();
            this.gAppearence.ResumeLayout( false );
            this.gAppearence.PerformLayout();
            this.gBasic.ResumeLayout( false );
            this.gBasic.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUploadBandwidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayers)).EndInit();
            this.tabWorlds.ResumeLayout( false );
            this.tabWorlds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWorlds)).EndInit();
            this.tabRanks.ResumeLayout( false );
            this.tabRanks.PerformLayout();
            this.gRankOptions.ResumeLayout( false );
            this.gRankOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefSeconds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nDrawLimit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickIdle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nAntiGriefBlocks)).EndInit();
            this.tabSecurity.ResumeLayout( false );
            this.gSecurityMisc.ResumeLayout( false );
            this.gSecurityMisc.PerformLayout();
            this.gSpamChat.ResumeLayout( false );
            this.gSpamChat.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatWarnings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamMute)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatTimer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatCount)).EndInit();
            this.gVerify.ResumeLayout( false );
            this.gVerify.PerformLayout();
            this.tabSavingAndBackup.ResumeLayout( false );
            this.gSaving.ResumeLayout( false );
            this.gSaving.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSaveInterval)).EndInit();
            this.gBackups.ResumeLayout( false );
            this.gBackups.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackupSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxBackups)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nBackupInterval)).EndInit();
            this.tabLogging.ResumeLayout( false );
            this.gLogFile.ResumeLayout( false );
            this.gLogFile.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nLogLimit)).EndInit();
            this.gConsole.ResumeLayout( false );
            this.gConsole.PerformLayout();
            this.tabIRC.ResumeLayout( false );
            this.tabIRC.PerformLayout();
            this.gIRCOptions.ResumeLayout( false );
            this.gIRCOptions.PerformLayout();
            this.gIRCNetwork.ResumeLayout( false );
            this.gIRCNetwork.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nIRCBotPort)).EndInit();
            this.tabAdvanced.ResumeLayout( false );
            this.groupBox1.ResumeLayout( false );
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nTickInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nThrottling)).EndInit();
            this.gCrashReport.ResumeLayout( false );
            this.gCrashReport.PerformLayout();
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bResetTab;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabRanks;
        private System.Windows.Forms.Label lServerName;
        private System.Windows.Forms.TextBox tServerName;
        private System.Windows.Forms.Label lMOTD;
        private System.Windows.Forms.TextBox tMOTD;
        private System.Windows.Forms.Label lMaxPlayers;
        private System.Windows.Forms.NumericUpDown nMaxPlayers;
        private System.Windows.Forms.TabPage tabSavingAndBackup;
        private System.Windows.Forms.ComboBox cPublic;
        private System.Windows.Forms.Label lPublic;
        private System.Windows.Forms.Button bMeasure;
        private System.Windows.Forms.Label lUploadBandwidthUnits;
        private System.Windows.Forms.NumericUpDown nUploadBandwidth;
        private System.Windows.Forms.Label lUploadBandwidth;
        private System.Windows.Forms.TabPage tabLogging;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.Label lTickIntervalUnits;
        private System.Windows.Forms.NumericUpDown nTickInterval;
        private System.Windows.Forms.Label lTickInterval;
        private System.Windows.Forms.Label lAdvancedWarning;
        private System.Windows.Forms.ListBox vRanks;
        private System.Windows.Forms.Button bAddRank;
        private System.Windows.Forms.Label lPermissions;
        private System.Windows.Forms.ListView vPermissions;
        private System.Windows.Forms.ColumnHeader chPermissions;
        private System.Windows.Forms.GroupBox gRankOptions;
        private System.Windows.Forms.Button bDeleteRank;
        private System.Windows.Forms.Label lRankColor;
        private System.Windows.Forms.TextBox tRankName;
        private System.Windows.Forms.Label lRankName;
        private System.Windows.Forms.TextBox tPrefix;
        private System.Windows.Forms.Label lPrefix;
        private System.Windows.Forms.Label lAntiGrief2;
        private System.Windows.Forms.NumericUpDown nAntiGriefBlocks;
        private System.Windows.Forms.CheckBox xDrawLimit;
        private System.Windows.Forms.Label lDrawLimitUnits;
        private System.Windows.Forms.Label lBanLimit;
        private System.Windows.Forms.Label lKickLimit;
        private System.Windows.Forms.Label lDemoteLimit;
        private System.Windows.Forms.Label lPromoteLimit;
        private System.Windows.Forms.ComboBox cPromoteLimit;
        private System.Windows.Forms.ComboBox cBanLimit;
        private System.Windows.Forms.ComboBox cKickLimit;
        private System.Windows.Forms.ComboBox cDemoteLimit;
        private System.Windows.Forms.GroupBox gAppearence;
        private System.Windows.Forms.Label lColorHelp;
        private System.Windows.Forms.Label lColorSys;
        private System.Windows.Forms.GroupBox gBasic;
        private System.Windows.Forms.CheckBox xListPrefixes;
        private System.Windows.Forms.CheckBox xChatPrefixes;
        private System.Windows.Forms.CheckBox xRankColors;
        private System.Windows.Forms.Label lColorSay;
        private System.Windows.Forms.ComboBox cDefaultRank;
        private System.Windows.Forms.Label lDefaultRank;
        private System.Windows.Forms.GroupBox gSaving;
        private System.Windows.Forms.CheckBox xSaveOnShutdown;
        private System.Windows.Forms.NumericUpDown nSaveInterval;
        private System.Windows.Forms.Label lSaveIntervalUnits;
        private System.Windows.Forms.CheckBox xSaveInterval;
        private System.Windows.Forms.GroupBox gBackups;
        private System.Windows.Forms.CheckBox xBackupOnStartup;
        private System.Windows.Forms.NumericUpDown nBackupInterval;
        private System.Windows.Forms.Label lBackupIntervalUnits;
        private System.Windows.Forms.CheckBox xBackupInterval;
        private System.Windows.Forms.CheckBox xBackupOnJoin;
        private System.Windows.Forms.CheckBox xRedundantPacket;
        private System.Windows.Forms.ComboBox cProcessPriority;
        private System.Windows.Forms.Label lProcessPriority;
        private System.Windows.Forms.Button bResetAll;
        private System.Windows.Forms.GroupBox gLogFile;
        private System.Windows.Forms.ComboBox cLogMode;
        private System.Windows.Forms.Label lLogMode;
        private System.Windows.Forms.GroupBox gConsole;
        private System.Windows.Forms.Label lLogFileOptions;
        private System.Windows.Forms.ListView vLogFileOptions;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Label lLogLimitUnits;
        private System.Windows.Forms.NumericUpDown nLogLimit;
        private System.Windows.Forms.Label lConsoleOptions;
        private System.Windows.Forms.ListView vConsoleOptions;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.CheckBox xLogLimit;
        private System.Windows.Forms.CheckBox xReserveSlot;
        private System.Windows.Forms.NumericUpDown nDrawLimit;
        private System.Windows.Forms.Label lKickIdleUnits;
        private System.Windows.Forms.NumericUpDown nKickIdle;
        private System.Windows.Forms.CheckBox xKickIdle;
        private System.Windows.Forms.CheckBox xAbsoluteUpdates;
        private System.Windows.Forms.Label lMaxBackups;
        private System.Windows.Forms.NumericUpDown nMaxBackups;
        private System.Windows.Forms.Label lMaxBackupSize;
        private System.Windows.Forms.NumericUpDown nMaxBackupSize;
        private System.Windows.Forms.CheckBox xMaxBackupSize;
        private System.Windows.Forms.CheckBox xMaxBackups;
        private System.Windows.Forms.Label lThrottlingUnits;
        private System.Windows.Forms.NumericUpDown nThrottling;
        private System.Windows.Forms.Label lThrottling;
        private System.Windows.Forms.Button bApply;
        private System.Windows.Forms.Button bColorSys;
        private System.Windows.Forms.Button bColorSay;
        private System.Windows.Forms.Button bColorHelp;
        private System.Windows.Forms.Button bColorRank;
        private System.Windows.Forms.ComboBox cUpdater;
        private System.Windows.Forms.Label lUpdater;
        private System.Windows.Forms.TabPage tabSecurity;
        private System.Windows.Forms.GroupBox gVerify;
        private System.Windows.Forms.Label lVerifyNames;
        private System.Windows.Forms.ComboBox cVerifyNames;
        private System.Windows.Forms.GroupBox gSpamChat;
        private System.Windows.Forms.Label lSpamChatSeconds;
        private System.Windows.Forms.NumericUpDown nSpamChatTimer;
        private System.Windows.Forms.Label lSpamChatMessages;
        private System.Windows.Forms.NumericUpDown nSpamChatCount;
        private System.Windows.Forms.Label lSpamChat;
        private System.Windows.Forms.CheckBox xLowLatencyMode;
        private System.Windows.Forms.CheckBox xSpamChatKick;
        private System.Windows.Forms.Label lSpamMuteSeconds;
        private System.Windows.Forms.NumericUpDown nSpamMute;
        private System.Windows.Forms.Label lSpamMute;
        private System.Windows.Forms.Label lSpamChatWarnings;
        private System.Windows.Forms.NumericUpDown nSpamChatWarnings;
        private System.Windows.Forms.CheckBox xBackupOnlyWhenChanged;
        private System.Windows.Forms.Label lPort;
        private System.Windows.Forms.NumericUpDown nPort;
        private System.Windows.Forms.Button bRules;
        private System.Windows.Forms.TabPage tabIRC;
        private System.Windows.Forms.GroupBox gIRCNetwork;
        private System.Windows.Forms.CheckBox xIRC;
        private System.Windows.Forms.CheckBox xIRCBotAnnounceServerJoins;
        private System.Windows.Forms.Label lIRCBotChannels;
        private System.Windows.Forms.NumericUpDown nIRCBotPort;
        private System.Windows.Forms.Label lIRCBotPort;
        private System.Windows.Forms.TextBox tIRCBotNetwork;
        private System.Windows.Forms.Label lIRCBotNetwork;
        private System.Windows.Forms.Label lIRCBotNick;
        private System.Windows.Forms.TextBox tIRCBotNick;
        private System.Windows.Forms.CheckBox xIRCBotForwardFromServer;
        private System.Windows.Forms.GroupBox gIRCOptions;
        private System.Windows.Forms.TextBox tIRCBotChannels;
        private System.Windows.Forms.Label lIRCBotChannels3;
        private System.Windows.Forms.Label lIRCBotChannels2;
        private System.Windows.Forms.CheckBox xIRCBotForwardFromIRC;
        private System.Windows.Forms.CheckBox xLimitOneConnectionPerIP;
        private System.Windows.Forms.TabPage tabWorlds;
        private System.Windows.Forms.DataGridView dgvWorlds;
        private System.Windows.Forms.Button bWorldDelete;
        private System.Windows.Forms.Button bAddWorld;
        private System.Windows.Forms.Button bWorldEdit;
        private System.Windows.Forms.ComboBox cMainWorld;
        private System.Windows.Forms.Label lMainWorld;
        private System.Windows.Forms.GroupBox gInformation;
        private System.Windows.Forms.CheckBox xAnnouncements;
        private System.Windows.Forms.Button bAnnouncements;
        private System.Windows.Forms.Label lAnnouncementsUnits;
        private System.Windows.Forms.NumericUpDown nAnnouncements;
        private System.Windows.Forms.Button bColorAnnouncement;
        private System.Windows.Forms.Label lColorAnnouncement;
        private System.Windows.Forms.Label lAntiGrief3;
        private System.Windows.Forms.NumericUpDown nAntiGriefSeconds;
        private System.Windows.Forms.CheckBox xAntiGrief;
        private System.Windows.Forms.Label lAntiGrief1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvcName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvcDescription;
        private System.Windows.Forms.DataGridViewCheckBoxColumn dgvcHidden;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvcAccess;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvcBuild;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvcBackup;
        private System.Windows.Forms.GroupBox gSecurityMisc;
        private System.Windows.Forms.CheckBox xAnnounceKickAndBanReasons;
        private System.Windows.Forms.CheckBox xRequireRankChangeReason;
        private System.Windows.Forms.CheckBox xRequireBanReason;
        private System.Windows.Forms.CheckBox xAnnounceRankChanges;
        private System.Windows.Forms.Button bColorPM;
        private System.Windows.Forms.Label lColorPM;
        private System.Windows.Forms.Button bPortCheck;
        private System.Windows.Forms.Button bColorIRC;
        private System.Windows.Forms.Label lColorIRC;
        private System.Windows.Forms.CheckBox xIRCBotAnnounceIRCJoins;
        private System.Windows.Forms.CheckBox xSubmitCrashReports;
        private System.Windows.Forms.GroupBox gCrashReport;
        private System.Windows.Forms.Label lCrashReportDisclaimer;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lIRCDelay;
        private System.Windows.Forms.NumericUpDown nIRCDelay;
        private System.Windows.Forms.CheckBox xRankColorsInWorldNames;
        private System.Windows.Forms.CheckBox xShowJoinedWorldMessages;
        private System.Windows.Forms.ComboBox cMaxHideFrom;
        private System.Windows.Forms.Label lMaxHideFrom;
        private System.Windows.Forms.CheckBox xIP;
        private System.Windows.Forms.TextBox tIP;
        private System.Windows.Forms.ComboBox cPatrolledRank;
        private System.Windows.Forms.Label lPatrolledRank;
        private System.Windows.Forms.Label lPatrolledRankAndBelow;
        private System.Windows.Forms.Button bLowerRank;
        private System.Windows.Forms.Button bRaiseRank;
        private System.Windows.Forms.Label lRankList;
        private System.Windows.Forms.CheckBox xIRCRegisteredNick;
        private System.Windows.Forms.TextBox tIRCNickServMessage;
        private System.Windows.Forms.Label lIRCNickServMessage;
        private System.Windows.Forms.TextBox tIRCNickServ;
        private System.Windows.Forms.Label lIRCNickServ;
        private System.Windows.Forms.ComboBox cFreezeLimit;
        private System.Windows.Forms.Label lFreezeLimit;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label lIRCNoForwardingMessage;
        private System.Windows.Forms.Label lIRCDelayUnits;
    }
}