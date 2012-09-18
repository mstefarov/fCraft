using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace fCraft.ConfigGUI {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && (components != null) ) {
                components.Dispose();
            }
            bold.Dispose();
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            ComponentResourceManager resources = new ComponentResourceManager( typeof( MainForm ) );
            this.tabs = new TabControl();
            this.tabGeneral = new TabPage();
            this.gWoMDirect = new GroupBox();
            this.lWoMDirectFlags = new Label();
            this.tWoMDirectFlags = new TextBox();
            this.lWoMDirectDescription = new Label();
            this.tWoMDirectDescription = new TextBox();
            this.xHeartbeatToWoMDirect = new CheckBox();
            this.gUpdaterSettings = new GroupBox();
            this.bShowAdvancedUpdaterSettings = new Button();
            this.cUpdaterMode = new ComboBox();
            this.lUpdater = new Label();
            this.groupBox2 = new GroupBox();
            this.bChangelog = new Button();
            this.bCredits = new Button();
            this.bReadme = new Button();
            this.gHelpAndSupport = new GroupBox();
            this.bOpenWiki = new Button();
            this.bReportABug = new Button();
            this.gInformation = new GroupBox();
            this.bGreeting = new Button();
            this.lAnnouncementsUnits = new Label();
            this.nAnnouncements = new NumericUpDown();
            this.xAnnouncements = new CheckBox();
            this.bRules = new Button();
            this.bAnnouncements = new Button();
            this.gBasic = new GroupBox();
            this.nMaxPlayersPerWorld = new NumericUpDown();
            this.lMaxPlayersPerWorld = new Label();
            this.bPortCheck = new Button();
            this.lPort = new Label();
            this.nPort = new NumericUpDown();
            this.cDefaultRank = new ComboBox();
            this.lDefaultRank = new Label();
            this.lUploadBandwidth = new Label();
            this.bMeasure = new Button();
            this.tServerName = new TextBox();
            this.lUploadBandwidthUnits = new Label();
            this.lServerName = new Label();
            this.nUploadBandwidth = new NumericUpDown();
            this.tMOTD = new TextBox();
            this.lMOTD = new Label();
            this.cPublic = new ComboBox();
            this.nMaxPlayers = new NumericUpDown();
            this.lPublic = new Label();
            this.lMaxPlayers = new Label();
            this.tabChat = new TabPage();
            this.gChatColors = new GroupBox();
            this.lColorMe = new Label();
            this.bColorMe = new Button();
            this.lColorWarning = new Label();
            this.bColorWarning = new Button();
            this.bColorSys = new Button();
            this.lColorSys = new Label();
            this.bColorPM = new Button();
            this.lColorHelp = new Label();
            this.lColorPM = new Label();
            this.lColorSay = new Label();
            this.bColorAnnouncement = new Button();
            this.lColorAnnouncement = new Label();
            this.bColorHelp = new Button();
            this.bColorSay = new Button();
            this.gAppearence = new GroupBox();
            this.xShowConnectionMessages = new CheckBox();
            this.xShowJoinedWorldMessages = new CheckBox();
            this.xRankColorsInWorldNames = new CheckBox();
            this.xRankPrefixesInList = new CheckBox();
            this.xRankPrefixesInChat = new CheckBox();
            this.xRankColorsInChat = new CheckBox();
            this.chatPreview = new ChatPreview();
            this.tabWorlds = new TabPage();
            this.xWoMEnableEnvExtensions = new CheckBox();
            this.bMapPath = new Button();
            this.xMapPath = new CheckBox();
            this.tMapPath = new TextBox();
            this.lDefaultBuildRank = new Label();
            this.cDefaultBuildRank = new ComboBox();
            this.cMainWorld = new ComboBox();
            this.lMainWorld = new Label();
            this.bWorldEdit = new Button();
            this.bAddWorld = new Button();
            this.bWorldDelete = new Button();
            this.dgvWorlds = new DataGridView();
            this.dgvcName = new DataGridViewTextBoxColumn();
            this.dgvcDescription = new DataGridViewTextBoxColumn();
            this.dgvcAccess = new DataGridViewComboBoxColumn();
            this.dgvcBuild = new DataGridViewComboBoxColumn();
            this.dgvcBackup = new DataGridViewComboBoxColumn();
            this.dgvcHidden = new DataGridViewCheckBoxColumn();
            this.dgvcBlockDB = new DataGridViewCheckBoxColumn();
            this.tabRanks = new TabPage();
            this.gPermissionLimits = new GroupBox();
            this.permissionLimitBoxContainer = new FlowLayoutPanel();
            this.lRankList = new Label();
            this.bLowerRank = new Button();
            this.bRaiseRank = new Button();
            this.gRankOptions = new GroupBox();
            this.lFillLimitUnits = new Label();
            this.nFillLimit = new NumericUpDown();
            this.lFillLimit = new Label();
            this.nCopyPasteSlots = new NumericUpDown();
            this.lCopyPasteSlots = new Label();
            this.xAllowSecurityCircumvention = new CheckBox();
            this.lAntiGrief1 = new Label();
            this.lAntiGrief3 = new Label();
            this.nAntiGriefSeconds = new NumericUpDown();
            this.bColorRank = new Button();
            this.xDrawLimit = new CheckBox();
            this.lDrawLimitUnits = new Label();
            this.lKickIdleUnits = new Label();
            this.nDrawLimit = new NumericUpDown();
            this.nKickIdle = new NumericUpDown();
            this.xAntiGrief = new CheckBox();
            this.lAntiGrief2 = new Label();
            this.xKickIdle = new CheckBox();
            this.nAntiGriefBlocks = new NumericUpDown();
            this.xReserveSlot = new CheckBox();
            this.tPrefix = new TextBox();
            this.lPrefix = new Label();
            this.lRankColor = new Label();
            this.tRankName = new TextBox();
            this.lRankName = new Label();
            this.bDeleteRank = new Button();
            this.vPermissions = new ListView();
            this.chPermissions = new ColumnHeader();
            this.bAddRank = new Button();
            this.lPermissions = new Label();
            this.vRanks = new ListBox();
            this.tabSecurity = new TabPage();
            this.gBlockDB = new GroupBox();
            this.cBlockDBAutoEnableRank = new ComboBox();
            this.xBlockDBAutoEnable = new CheckBox();
            this.xBlockDBEnabled = new CheckBox();
            this.gSecurityMisc = new GroupBox();
            this.xAnnounceRankChangeReasons = new CheckBox();
            this.xRequireKickReason = new CheckBox();
            this.xPaidPlayersOnly = new CheckBox();
            this.lPatrolledRankAndBelow = new Label();
            this.cPatrolledRank = new ComboBox();
            this.lPatrolledRank = new Label();
            this.xAnnounceRankChanges = new CheckBox();
            this.xAnnounceKickAndBanReasons = new CheckBox();
            this.xRequireRankChangeReason = new CheckBox();
            this.xRequireBanReason = new CheckBox();
            this.gSpamChat = new GroupBox();
            this.xAntispamMuteDuration = new CheckBox();
            this.xAntispamMessageCount = new CheckBox();
            this.lAntispamMaxWarnings = new Label();
            this.nAntispamMaxWarnings = new NumericUpDown();
            this.xAntispamKicks = new CheckBox();
            this.lAntispamMuteDurationUnits = new Label();
            this.lAntispamIntervalUnits = new Label();
            this.nAntispamMuteDuration = new NumericUpDown();
            this.nAntispamInterval = new NumericUpDown();
            this.lAntispamMessageCount = new Label();
            this.nAntispamMessageCount = new NumericUpDown();
            this.gVerify = new GroupBox();
            this.nMaxConnectionsPerIP = new NumericUpDown();
            this.xAllowUnverifiedLAN = new CheckBox();
            this.xMaxConnectionsPerIP = new CheckBox();
            this.lVerifyNames = new Label();
            this.cVerifyNames = new ComboBox();
            this.tabSavingAndBackup = new TabPage();
            this.gDataBackup = new GroupBox();
            this.xBackupDataOnStartup = new CheckBox();
            this.gSaving = new GroupBox();
            this.nSaveInterval = new NumericUpDown();
            this.lSaveIntervalUnits = new Label();
            this.xSaveInterval = new CheckBox();
            this.gBackups = new GroupBox();
            this.xBackupOnlyWhenChanged = new CheckBox();
            this.lMaxBackupSize = new Label();
            this.xMaxBackupSize = new CheckBox();
            this.nMaxBackupSize = new NumericUpDown();
            this.xMaxBackups = new CheckBox();
            this.xBackupOnStartup = new CheckBox();
            this.lMaxBackups = new Label();
            this.nMaxBackups = new NumericUpDown();
            this.nBackupInterval = new NumericUpDown();
            this.lBackupIntervalUnits = new Label();
            this.xBackupInterval = new CheckBox();
            this.xBackupOnJoin = new CheckBox();
            this.tabLogging = new TabPage();
            this.gLogFile = new GroupBox();
            this.lLogFileOptionsDescription = new Label();
            this.xLogLimit = new CheckBox();
            this.vLogFileOptions = new ListView();
            this.columnHeader2 = new ColumnHeader();
            this.lLogLimitUnits = new Label();
            this.nLogLimit = new NumericUpDown();
            this.cLogMode = new ComboBox();
            this.lLogMode = new Label();
            this.gConsole = new GroupBox();
            this.lLogConsoleOptionsDescription = new Label();
            this.vConsoleOptions = new ListView();
            this.columnHeader3 = new ColumnHeader();
            this.tabIRC = new TabPage();
            this.gIRCColors = new GroupBox();
            this.xIRCStripMinecraftColors = new CheckBox();
            this.bColorIRC = new Button();
            this.xIRCUseColor = new CheckBox();
            this.lColorIRC = new Label();
            this.xIRCListShowNonEnglish = new CheckBox();
            this.gIRCOptions = new GroupBox();
            this.xIRCBotAnnounceServerEvents = new CheckBox();
            this.lIRCNoForwardingMessage = new Label();
            this.xIRCBotAnnounceIRCJoins = new CheckBox();
            this.xIRCBotForwardFromIRC = new CheckBox();
            this.xIRCBotAnnounceServerJoins = new CheckBox();
            this.xIRCBotForwardFromServer = new CheckBox();
            this.gIRCNetwork = new GroupBox();
            this.lIRCDelayUnits = new Label();
            this.xIRCRegisteredNick = new CheckBox();
            this.tIRCNickServMessage = new TextBox();
            this.lIRCNickServMessage = new Label();
            this.tIRCNickServ = new TextBox();
            this.lIRCNickServ = new Label();
            this.nIRCDelay = new NumericUpDown();
            this.lIRCDelay = new Label();
            this.lIRCBotChannels2 = new Label();
            this.lIRCBotChannels3 = new Label();
            this.tIRCBotChannels = new TextBox();
            this.lIRCBotChannels = new Label();
            this.nIRCBotPort = new NumericUpDown();
            this.lIRCBotPort = new Label();
            this.tIRCBotNetwork = new TextBox();
            this.lIRCBotNetwork = new Label();
            this.lIRCBotNick = new Label();
            this.tIRCBotNick = new TextBox();
            this.lIRCList = new Label();
            this.xIRCBotEnabled = new CheckBox();
            this.cIRCList = new ComboBox();
            this.tabAdvanced = new TabPage();
            this.gCrashReport = new GroupBox();
            this.lCrashReportDisclaimer = new Label();
            this.xSubmitCrashReports = new CheckBox();
            this.gPerformance = new GroupBox();
            this.lAdvancedWarning = new Label();
            this.xLowLatencyMode = new CheckBox();
            this.lProcessPriority = new Label();
            this.cProcessPriority = new ComboBox();
            this.nTickInterval = new NumericUpDown();
            this.lTickIntervalUnits = new Label();
            this.lTickInterval = new Label();
            this.nThrottling = new NumericUpDown();
            this.lThrottling = new Label();
            this.lThrottlingUnits = new Label();
            this.gAdvancedMisc = new GroupBox();
            this.nMaxUndoStates = new NumericUpDown();
            this.lMaxUndoStates = new Label();
            this.lIPWarning = new Label();
            this.tIP = new TextBox();
            this.xIP = new CheckBox();
            this.lConsoleName = new Label();
            this.tConsoleName = new TextBox();
            this.nMaxUndo = new NumericUpDown();
            this.lMaxUndoUnits = new Label();
            this.xMaxUndo = new CheckBox();
            this.xRelayAllBlockUpdates = new CheckBox();
            this.xNoPartialPositionUpdates = new CheckBox();
            this.bOK = new Button();
            this.bCancel = new Button();
            this.bResetTab = new Button();
            this.bResetAll = new Button();
            this.bApply = new Button();
            this.toolTip = new ToolTip( this.components );
            this.tabs.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.gWoMDirect.SuspendLayout();
            this.gUpdaterSettings.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.gHelpAndSupport.SuspendLayout();
            this.gInformation.SuspendLayout();
            ( (ISupportInitialize)( this.nAnnouncements ) ).BeginInit();
            this.gBasic.SuspendLayout();
            ( (ISupportInitialize)( this.nMaxPlayersPerWorld ) ).BeginInit();
            ( (ISupportInitialize)( this.nPort ) ).BeginInit();
            ( (ISupportInitialize)( this.nUploadBandwidth ) ).BeginInit();
            ( (ISupportInitialize)( this.nMaxPlayers ) ).BeginInit();
            this.tabChat.SuspendLayout();
            this.gChatColors.SuspendLayout();
            this.gAppearence.SuspendLayout();
            this.tabWorlds.SuspendLayout();
            ( (ISupportInitialize)( this.dgvWorlds ) ).BeginInit();
            this.tabRanks.SuspendLayout();
            this.gPermissionLimits.SuspendLayout();
            this.gRankOptions.SuspendLayout();
            ( (ISupportInitialize)( this.nFillLimit ) ).BeginInit();
            ( (ISupportInitialize)( this.nCopyPasteSlots ) ).BeginInit();
            ( (ISupportInitialize)( this.nAntiGriefSeconds ) ).BeginInit();
            ( (ISupportInitialize)( this.nDrawLimit ) ).BeginInit();
            ( (ISupportInitialize)( this.nKickIdle ) ).BeginInit();
            ( (ISupportInitialize)( this.nAntiGriefBlocks ) ).BeginInit();
            this.tabSecurity.SuspendLayout();
            this.gBlockDB.SuspendLayout();
            this.gSecurityMisc.SuspendLayout();
            this.gSpamChat.SuspendLayout();
            ( (ISupportInitialize)( this.nAntispamMaxWarnings ) ).BeginInit();
            ( (ISupportInitialize)( this.nAntispamMuteDuration ) ).BeginInit();
            ( (ISupportInitialize)( this.nAntispamInterval ) ).BeginInit();
            ( (ISupportInitialize)( this.nAntispamMessageCount ) ).BeginInit();
            this.gVerify.SuspendLayout();
            ( (ISupportInitialize)( this.nMaxConnectionsPerIP ) ).BeginInit();
            this.tabSavingAndBackup.SuspendLayout();
            this.gDataBackup.SuspendLayout();
            this.gSaving.SuspendLayout();
            ( (ISupportInitialize)( this.nSaveInterval ) ).BeginInit();
            this.gBackups.SuspendLayout();
            ( (ISupportInitialize)( this.nMaxBackupSize ) ).BeginInit();
            ( (ISupportInitialize)( this.nMaxBackups ) ).BeginInit();
            ( (ISupportInitialize)( this.nBackupInterval ) ).BeginInit();
            this.tabLogging.SuspendLayout();
            this.gLogFile.SuspendLayout();
            ( (ISupportInitialize)( this.nLogLimit ) ).BeginInit();
            this.gConsole.SuspendLayout();
            this.tabIRC.SuspendLayout();
            this.gIRCColors.SuspendLayout();
            this.gIRCOptions.SuspendLayout();
            this.gIRCNetwork.SuspendLayout();
            ( (ISupportInitialize)( this.nIRCDelay ) ).BeginInit();
            ( (ISupportInitialize)( this.nIRCBotPort ) ).BeginInit();
            this.tabAdvanced.SuspendLayout();
            this.gCrashReport.SuspendLayout();
            this.gPerformance.SuspendLayout();
            ( (ISupportInitialize)( this.nTickInterval ) ).BeginInit();
            ( (ISupportInitialize)( this.nThrottling ) ).BeginInit();
            this.gAdvancedMisc.SuspendLayout();
            ( (ISupportInitialize)( this.nMaxUndoStates ) ).BeginInit();
            ( (ISupportInitialize)( this.nMaxUndo ) ).BeginInit();
            this.SuspendLayout();
            // 
            // tabs
            // 
            this.tabs.Anchor = ( (AnchorStyles)( ( ( ( AnchorStyles.Top | AnchorStyles.Bottom )
                        | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.tabs.Controls.Add( this.tabGeneral );
            this.tabs.Controls.Add( this.tabChat );
            this.tabs.Controls.Add( this.tabWorlds );
            this.tabs.Controls.Add( this.tabRanks );
            this.tabs.Controls.Add( this.tabSecurity );
            this.tabs.Controls.Add( this.tabSavingAndBackup );
            this.tabs.Controls.Add( this.tabLogging );
            this.tabs.Controls.Add( this.tabIRC );
            this.tabs.Controls.Add( this.tabAdvanced );
            this.tabs.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.tabs.Location = new Point( 12, 12 );
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new Size( 660, 510 );
            this.tabs.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add( this.gWoMDirect );
            this.tabGeneral.Controls.Add( this.gUpdaterSettings );
            this.tabGeneral.Controls.Add( this.groupBox2 );
            this.tabGeneral.Controls.Add( this.gHelpAndSupport );
            this.tabGeneral.Controls.Add( this.gInformation );
            this.tabGeneral.Controls.Add( this.gBasic );
            this.tabGeneral.Location = new Point( 4, 24 );
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new Padding( 5, 10, 5, 10 );
            this.tabGeneral.Size = new Size( 652, 482 );
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // gWoMDirect
            // 
            this.gWoMDirect.Controls.Add( this.lWoMDirectFlags );
            this.gWoMDirect.Controls.Add( this.tWoMDirectFlags );
            this.gWoMDirect.Controls.Add( this.lWoMDirectDescription );
            this.gWoMDirect.Controls.Add( this.tWoMDirectDescription );
            this.gWoMDirect.Controls.Add( this.xHeartbeatToWoMDirect );
            this.gWoMDirect.Location = new Point( 8, 307 );
            this.gWoMDirect.Name = "gWoMDirect";
            this.gWoMDirect.Size = new Size( 636, 80 );
            this.gWoMDirect.TabIndex = 6;
            this.gWoMDirect.TabStop = false;
            this.gWoMDirect.Text = "WoM Direct";
            // 
            // lWoMDirectFlags
            // 
            this.lWoMDirectFlags.AutoSize = true;
            this.lWoMDirectFlags.Location = new Point( 386, 51 );
            this.lWoMDirectFlags.Name = "lWoMDirectFlags";
            this.lWoMDirectFlags.Size = new Size( 37, 15 );
            this.lWoMDirectFlags.TabIndex = 24;
            this.lWoMDirectFlags.Text = "Flags";
            // 
            // tWoMDirectFlags
            // 
            this.tWoMDirectFlags.Location = new Point( 429, 48 );
            this.tWoMDirectFlags.MaxLength = 16;
            this.tWoMDirectFlags.Name = "tWoMDirectFlags";
            this.tWoMDirectFlags.Size = new Size( 144, 21 );
            this.tWoMDirectFlags.TabIndex = 25;
            // 
            // lWoMDirectDescription
            // 
            this.lWoMDirectDescription.AutoSize = true;
            this.lWoMDirectDescription.Location = new Point( 40, 51 );
            this.lWoMDirectDescription.Name = "lWoMDirectDescription";
            this.lWoMDirectDescription.Size = new Size( 69, 15 );
            this.lWoMDirectDescription.TabIndex = 22;
            this.lWoMDirectDescription.Text = "Description";
            // 
            // tWoMDirectDescription
            // 
            this.tWoMDirectDescription.Location = new Point( 115, 48 );
            this.tWoMDirectDescription.MaxLength = 64;
            this.tWoMDirectDescription.Name = "tWoMDirectDescription";
            this.tWoMDirectDescription.Size = new Size( 228, 21 );
            this.tWoMDirectDescription.TabIndex = 23;
            // 
            // xHeartbeatToWoMDirect
            // 
            this.xHeartbeatToWoMDirect.AutoSize = true;
            this.xHeartbeatToWoMDirect.Location = new Point( 16, 20 );
            this.xHeartbeatToWoMDirect.Name = "xHeartbeatToWoMDirect";
            this.xHeartbeatToWoMDirect.Size = new Size( 354, 19 );
            this.xHeartbeatToWoMDirect.TabIndex = 21;
            this.xHeartbeatToWoMDirect.Text = "Send heartbeats to WoM Direct (direct.worldofminecraft.net).";
            this.xHeartbeatToWoMDirect.UseVisualStyleBackColor = true;
            this.xHeartbeatToWoMDirect.CheckedChanged += new EventHandler( this.xHeartbeatToWoMDirect_CheckedChanged );
            // 
            // gUpdaterSettings
            // 
            this.gUpdaterSettings.Controls.Add( this.bShowAdvancedUpdaterSettings );
            this.gUpdaterSettings.Controls.Add( this.cUpdaterMode );
            this.gUpdaterSettings.Controls.Add( this.lUpdater );
            this.gUpdaterSettings.Location = new Point( 8, 247 );
            this.gUpdaterSettings.Name = "gUpdaterSettings";
            this.gUpdaterSettings.Size = new Size( 636, 54 );
            this.gUpdaterSettings.TabIndex = 2;
            this.gUpdaterSettings.TabStop = false;
            this.gUpdaterSettings.Text = "Updater Settings";
            // 
            // bShowAdvancedUpdaterSettings
            // 
            this.bShowAdvancedUpdaterSettings.Location = new Point( 318, 22 );
            this.bShowAdvancedUpdaterSettings.Name = "bShowAdvancedUpdaterSettings";
            this.bShowAdvancedUpdaterSettings.Size = new Size( 75, 23 );
            this.bShowAdvancedUpdaterSettings.TabIndex = 2;
            this.bShowAdvancedUpdaterSettings.Text = "Advanced";
            this.bShowAdvancedUpdaterSettings.UseVisualStyleBackColor = true;
            this.bShowAdvancedUpdaterSettings.Click += new EventHandler( this.bShowAdvancedUpdaterSettings_Click );
            // 
            // cUpdaterMode
            // 
            this.cUpdaterMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cUpdaterMode.FormattingEnabled = true;
            this.cUpdaterMode.Items.AddRange( new object[] {
            "Disabled",
            "Notify about availability",
            "Prompt to install",
            "Fully automatic"} );
            this.cUpdaterMode.Location = new Point( 123, 22 );
            this.cUpdaterMode.Name = "cUpdaterMode";
            this.cUpdaterMode.Size = new Size( 189, 23 );
            this.cUpdaterMode.TabIndex = 1;
            this.cUpdaterMode.SelectedIndexChanged += new EventHandler( this.cUpdaterMode_SelectedIndexChanged );
            // 
            // lUpdater
            // 
            this.lUpdater.AutoSize = true;
            this.lUpdater.Location = new Point( 6, 25 );
            this.lUpdater.Name = "lUpdater";
            this.lUpdater.Size = new Size( 111, 15 );
            this.lUpdater.TabIndex = 0;
            this.lUpdater.Text = "fCraft update check";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.groupBox2.Controls.Add( this.bChangelog );
            this.groupBox2.Controls.Add( this.bCredits );
            this.groupBox2.Controls.Add( this.bReadme );
            this.groupBox2.Location = new Point( 329, 412 );
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new Size( 315, 56 );
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "About fCraft";
            // 
            // bChangelog
            // 
            this.bChangelog.Location = new Point( 107, 20 );
            this.bChangelog.Name = "bChangelog";
            this.bChangelog.Size = new Size( 95, 23 );
            this.bChangelog.TabIndex = 2;
            this.bChangelog.Text = "Changelog";
            this.bChangelog.UseVisualStyleBackColor = true;
            this.bChangelog.Click += new EventHandler( this.bChangelog_Click );
            // 
            // bCredits
            // 
            this.bCredits.Location = new Point( 208, 20 );
            this.bCredits.Name = "bCredits";
            this.bCredits.Size = new Size( 95, 23 );
            this.bCredits.TabIndex = 1;
            this.bCredits.Text = "Credits";
            this.bCredits.UseVisualStyleBackColor = true;
            this.bCredits.Click += new EventHandler( this.bCredits_Click );
            // 
            // bReadme
            // 
            this.bReadme.Enabled = false;
            this.bReadme.Location = new Point( 6, 20 );
            this.bReadme.Name = "bReadme";
            this.bReadme.Size = new Size( 95, 23 );
            this.bReadme.TabIndex = 0;
            this.bReadme.Text = "Readme";
            this.bReadme.UseVisualStyleBackColor = true;
            this.bReadme.Click += new EventHandler( this.bReadme_Click );
            // 
            // gHelpAndSupport
            // 
            this.gHelpAndSupport.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.gHelpAndSupport.Controls.Add( this.bOpenWiki );
            this.gHelpAndSupport.Controls.Add( this.bReportABug );
            this.gHelpAndSupport.Location = new Point( 8, 412 );
            this.gHelpAndSupport.Name = "gHelpAndSupport";
            this.gHelpAndSupport.Size = new Size( 315, 56 );
            this.gHelpAndSupport.TabIndex = 3;
            this.gHelpAndSupport.TabStop = false;
            this.gHelpAndSupport.Text = "Help and Support";
            // 
            // bOpenWiki
            // 
            this.bOpenWiki.Location = new Point( 9, 20 );
            this.bOpenWiki.Name = "bOpenWiki";
            this.bOpenWiki.Size = new Size( 140, 23 );
            this.bOpenWiki.TabIndex = 0;
            this.bOpenWiki.Text = "Open fCraft Wiki";
            this.bOpenWiki.UseVisualStyleBackColor = true;
            this.bOpenWiki.Click += new EventHandler( this.bOpenWiki_Click );
            // 
            // bReportABug
            // 
            this.bReportABug.Location = new Point( 155, 20 );
            this.bReportABug.Name = "bReportABug";
            this.bReportABug.Size = new Size( 140, 23 );
            this.bReportABug.TabIndex = 1;
            this.bReportABug.Text = "Report a Bug";
            this.bReportABug.UseVisualStyleBackColor = true;
            this.bReportABug.Click += new EventHandler( this.bReportABug_Click );
            // 
            // gInformation
            // 
            this.gInformation.Controls.Add( this.bGreeting );
            this.gInformation.Controls.Add( this.lAnnouncementsUnits );
            this.gInformation.Controls.Add( this.nAnnouncements );
            this.gInformation.Controls.Add( this.xAnnouncements );
            this.gInformation.Controls.Add( this.bRules );
            this.gInformation.Controls.Add( this.bAnnouncements );
            this.gInformation.Location = new Point( 8, 184 );
            this.gInformation.Name = "gInformation";
            this.gInformation.Size = new Size( 636, 57 );
            this.gInformation.TabIndex = 1;
            this.gInformation.TabStop = false;
            this.gInformation.Text = "Information";
            // 
            // bGreeting
            // 
            this.bGreeting.Anchor = ( (AnchorStyles)( ( AnchorStyles.Top | AnchorStyles.Right ) ) );
            this.bGreeting.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bGreeting.Location = new Point( 538, 20 );
            this.bGreeting.Name = "bGreeting";
            this.bGreeting.Size = new Size( 92, 28 );
            this.bGreeting.TabIndex = 5;
            this.bGreeting.Text = "Edit Greeting";
            this.bGreeting.UseVisualStyleBackColor = true;
            this.bGreeting.Click += new EventHandler( this.bGreeting_Click );
            // 
            // lAnnouncementsUnits
            // 
            this.lAnnouncementsUnits.AutoSize = true;
            this.lAnnouncementsUnits.Location = new Point( 266, 27 );
            this.lAnnouncementsUnits.Name = "lAnnouncementsUnits";
            this.lAnnouncementsUnits.Size = new Size( 28, 15 );
            this.lAnnouncementsUnits.TabIndex = 2;
            this.lAnnouncementsUnits.Text = "min";
            // 
            // nAnnouncements
            // 
            this.nAnnouncements.Enabled = false;
            this.nAnnouncements.Location = new Point( 210, 25 );
            this.nAnnouncements.Maximum = new decimal( new int[] {
            60,
            0,
            0,
            0} );
            this.nAnnouncements.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nAnnouncements.Name = "nAnnouncements";
            this.nAnnouncements.Size = new Size( 50, 21 );
            this.nAnnouncements.TabIndex = 1;
            this.nAnnouncements.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // xAnnouncements
            // 
            this.xAnnouncements.AutoSize = true;
            this.xAnnouncements.Location = new Point( 24, 26 );
            this.xAnnouncements.Name = "xAnnouncements";
            this.xAnnouncements.Size = new Size( 180, 19 );
            this.xAnnouncements.TabIndex = 0;
            this.xAnnouncements.Text = "Show announcements every";
            this.xAnnouncements.UseVisualStyleBackColor = true;
            this.xAnnouncements.CheckedChanged += new EventHandler( this.xAnnouncements_CheckedChanged );
            // 
            // bRules
            // 
            this.bRules.Anchor = ( (AnchorStyles)( ( AnchorStyles.Top | AnchorStyles.Right ) ) );
            this.bRules.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bRules.Location = new Point( 445, 20 );
            this.bRules.Name = "bRules";
            this.bRules.Size = new Size( 87, 28 );
            this.bRules.TabIndex = 4;
            this.bRules.Text = "Edit Rules";
            this.bRules.UseVisualStyleBackColor = true;
            this.bRules.Click += new EventHandler( this.bRules_Click );
            // 
            // bAnnouncements
            // 
            this.bAnnouncements.Anchor = ( (AnchorStyles)( ( AnchorStyles.Top | AnchorStyles.Right ) ) );
            this.bAnnouncements.Enabled = false;
            this.bAnnouncements.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bAnnouncements.Location = new Point( 301, 20 );
            this.bAnnouncements.Name = "bAnnouncements";
            this.bAnnouncements.Size = new Size( 138, 28 );
            this.bAnnouncements.TabIndex = 3;
            this.bAnnouncements.Text = "Edit Announcements";
            this.bAnnouncements.UseVisualStyleBackColor = true;
            this.bAnnouncements.Click += new EventHandler( this.bAnnouncements_Click );
            // 
            // gBasic
            // 
            this.gBasic.Controls.Add( this.nMaxPlayersPerWorld );
            this.gBasic.Controls.Add( this.lMaxPlayersPerWorld );
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
            this.gBasic.Location = new Point( 8, 13 );
            this.gBasic.Name = "gBasic";
            this.gBasic.Size = new Size( 636, 165 );
            this.gBasic.TabIndex = 0;
            this.gBasic.TabStop = false;
            this.gBasic.Text = "Basic Settings";
            // 
            // nMaxPlayersPerWorld
            // 
            this.nMaxPlayersPerWorld.Location = new Point( 440, 74 );
            this.nMaxPlayersPerWorld.Maximum = new decimal( new int[] {
            127,
            0,
            0,
            0} );
            this.nMaxPlayersPerWorld.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nMaxPlayersPerWorld.Name = "nMaxPlayersPerWorld";
            this.nMaxPlayersPerWorld.Size = new Size( 75, 21 );
            this.nMaxPlayersPerWorld.TabIndex = 12;
            this.nMaxPlayersPerWorld.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nMaxPlayersPerWorld.Validating += new CancelEventHandler( this.nMaxPlayerPerWorld_Validating );
            // 
            // lMaxPlayersPerWorld
            // 
            this.lMaxPlayersPerWorld.AutoSize = true;
            this.lMaxPlayersPerWorld.Location = new Point( 299, 76 );
            this.lMaxPlayersPerWorld.Name = "lMaxPlayersPerWorld";
            this.lMaxPlayersPerWorld.Size = new Size( 135, 15 );
            this.lMaxPlayersPerWorld.TabIndex = 11;
            this.lMaxPlayersPerWorld.Text = "Max players (per world)";
            // 
            // bPortCheck
            // 
            this.bPortCheck.Location = new Point( 204, 99 );
            this.bPortCheck.Name = "bPortCheck";
            this.bPortCheck.Size = new Size( 68, 23 );
            this.bPortCheck.TabIndex = 8;
            this.bPortCheck.Text = "Check";
            this.bPortCheck.UseVisualStyleBackColor = true;
            this.bPortCheck.Click += new EventHandler( this.bPortCheck_Click );
            // 
            // lPort
            // 
            this.lPort.AutoSize = true;
            this.lPort.Location = new Point( 42, 103 );
            this.lPort.Name = "lPort";
            this.lPort.Size = new Size( 75, 15 );
            this.lPort.TabIndex = 6;
            this.lPort.Text = "Port number";
            // 
            // nPort
            // 
            this.nPort.Location = new Point( 123, 101 );
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
            this.nPort.Size = new Size( 75, 21 );
            this.nPort.TabIndex = 7;
            this.nPort.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // cDefaultRank
            // 
            this.cDefaultRank.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cDefaultRank.FormattingEnabled = true;
            this.cDefaultRank.Location = new Point( 440, 128 );
            this.cDefaultRank.Name = "cDefaultRank";
            this.cDefaultRank.Size = new Size( 170, 23 );
            this.cDefaultRank.TabIndex = 18;
            this.cDefaultRank.SelectedIndexChanged += new EventHandler( this.cDefaultRank_SelectedIndexChanged );
            // 
            // lDefaultRank
            // 
            this.lDefaultRank.AutoSize = true;
            this.lDefaultRank.Location = new Point( 361, 131 );
            this.lDefaultRank.Name = "lDefaultRank";
            this.lDefaultRank.Size = new Size( 73, 15 );
            this.lDefaultRank.TabIndex = 17;
            this.lDefaultRank.Text = "Default rank";
            // 
            // lUploadBandwidth
            // 
            this.lUploadBandwidth.AutoSize = true;
            this.lUploadBandwidth.Location = new Point( 327, 103 );
            this.lUploadBandwidth.Name = "lUploadBandwidth";
            this.lUploadBandwidth.Size = new Size( 107, 15 );
            this.lUploadBandwidth.TabIndex = 13;
            this.lUploadBandwidth.Text = "Upload bandwidth";
            // 
            // bMeasure
            // 
            this.bMeasure.Location = new Point( 559, 99 );
            this.bMeasure.Name = "bMeasure";
            this.bMeasure.Size = new Size( 71, 23 );
            this.bMeasure.TabIndex = 16;
            this.bMeasure.Text = "Measure";
            this.bMeasure.UseVisualStyleBackColor = true;
            this.bMeasure.Click += new EventHandler( this.bMeasure_Click );
            // 
            // tServerName
            // 
            this.tServerName.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Top | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.tServerName.HideSelection = false;
            this.tServerName.Location = new Point( 123, 20 );
            this.tServerName.MaxLength = 64;
            this.tServerName.Name = "tServerName";
            this.tServerName.Size = new Size( 507, 21 );
            this.tServerName.TabIndex = 1;
            // 
            // lUploadBandwidthUnits
            // 
            this.lUploadBandwidthUnits.AutoSize = true;
            this.lUploadBandwidthUnits.Location = new Point( 521, 103 );
            this.lUploadBandwidthUnits.Name = "lUploadBandwidthUnits";
            this.lUploadBandwidthUnits.Size = new Size( 32, 15 );
            this.lUploadBandwidthUnits.TabIndex = 15;
            this.lUploadBandwidthUnits.Text = "KB/s";
            // 
            // lServerName
            // 
            this.lServerName.AutoSize = true;
            this.lServerName.Location = new Point( 40, 23 );
            this.lServerName.Name = "lServerName";
            this.lServerName.Size = new Size( 77, 15 );
            this.lServerName.TabIndex = 0;
            this.lServerName.Text = "Server name";
            // 
            // nUploadBandwidth
            // 
            this.nUploadBandwidth.Increment = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nUploadBandwidth.Location = new Point( 440, 101 );
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
            this.nUploadBandwidth.Size = new Size( 75, 21 );
            this.nUploadBandwidth.TabIndex = 14;
            this.nUploadBandwidth.Value = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            // 
            // tMOTD
            // 
            this.tMOTD.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Top | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.tMOTD.Location = new Point( 123, 47 );
            this.tMOTD.MaxLength = 64;
            this.tMOTD.Name = "tMOTD";
            this.tMOTD.Size = new Size( 507, 21 );
            this.tMOTD.TabIndex = 3;
            // 
            // lMOTD
            // 
            this.lMOTD.AutoSize = true;
            this.lMOTD.Location = new Point( 74, 50 );
            this.lMOTD.Name = "lMOTD";
            this.lMOTD.Size = new Size( 43, 15 );
            this.lMOTD.TabIndex = 2;
            this.lMOTD.Text = "MOTD";
            // 
            // cPublic
            // 
            this.cPublic.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cPublic.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.cPublic.FormattingEnabled = true;
            this.cPublic.Items.AddRange( new object[] {
            "Public",
            "Private"} );
            this.cPublic.Location = new Point( 123, 128 );
            this.cPublic.Name = "cPublic";
            this.cPublic.Size = new Size( 75, 23 );
            this.cPublic.TabIndex = 10;
            // 
            // nMaxPlayers
            // 
            this.nMaxPlayers.Location = new Point( 123, 74 );
            this.nMaxPlayers.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nMaxPlayers.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nMaxPlayers.Name = "nMaxPlayers";
            this.nMaxPlayers.Size = new Size( 75, 21 );
            this.nMaxPlayers.TabIndex = 5;
            this.nMaxPlayers.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nMaxPlayers.ValueChanged += new EventHandler( this.nMaxPlayers_ValueChanged );
            // 
            // lPublic
            // 
            this.lPublic.AutoSize = true;
            this.lPublic.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.lPublic.Location = new Point( 14, 131 );
            this.lPublic.Name = "lPublic";
            this.lPublic.Size = new Size( 103, 15 );
            this.lPublic.TabIndex = 9;
            this.lPublic.Text = "Server visibility";
            // 
            // lMaxPlayers
            // 
            this.lMaxPlayers.AutoSize = true;
            this.lMaxPlayers.Location = new Point( 10, 76 );
            this.lMaxPlayers.Name = "lMaxPlayers";
            this.lMaxPlayers.Size = new Size( 107, 15 );
            this.lMaxPlayers.TabIndex = 4;
            this.lMaxPlayers.Text = "Max players (total)";
            // 
            // tabChat
            // 
            this.tabChat.Controls.Add( this.gChatColors );
            this.tabChat.Controls.Add( this.gAppearence );
            this.tabChat.Controls.Add( this.chatPreview );
            this.tabChat.Location = new Point( 4, 24 );
            this.tabChat.Name = "tabChat";
            this.tabChat.Padding = new Padding( 5, 5, 5, 10 );
            this.tabChat.Size = new Size( 652, 482 );
            this.tabChat.TabIndex = 10;
            this.tabChat.Text = "Chat";
            this.tabChat.UseVisualStyleBackColor = true;
            // 
            // gChatColors
            // 
            this.gChatColors.Controls.Add( this.lColorMe );
            this.gChatColors.Controls.Add( this.bColorMe );
            this.gChatColors.Controls.Add( this.lColorWarning );
            this.gChatColors.Controls.Add( this.bColorWarning );
            this.gChatColors.Controls.Add( this.bColorSys );
            this.gChatColors.Controls.Add( this.lColorSys );
            this.gChatColors.Controls.Add( this.bColorPM );
            this.gChatColors.Controls.Add( this.lColorHelp );
            this.gChatColors.Controls.Add( this.lColorPM );
            this.gChatColors.Controls.Add( this.lColorSay );
            this.gChatColors.Controls.Add( this.bColorAnnouncement );
            this.gChatColors.Controls.Add( this.lColorAnnouncement );
            this.gChatColors.Controls.Add( this.bColorHelp );
            this.gChatColors.Controls.Add( this.bColorSay );
            this.gChatColors.Location = new Point( 8, 8 );
            this.gChatColors.Name = "gChatColors";
            this.gChatColors.Size = new Size( 636, 139 );
            this.gChatColors.TabIndex = 0;
            this.gChatColors.TabStop = false;
            this.gChatColors.Text = "Colors";
            // 
            // lColorMe
            // 
            this.lColorMe.AutoSize = true;
            this.lColorMe.Location = new Point( 402, 82 );
            this.lColorMe.Name = "lColorMe";
            this.lColorMe.Size = new Size( 117, 15 );
            this.lColorMe.TabIndex = 12;
            this.lColorMe.Text = "/Me command color";
            // 
            // bColorMe
            // 
            this.bColorMe.BackColor = System.Drawing.Color.White;
            this.bColorMe.Location = new Point( 525, 78 );
            this.bColorMe.Name = "bColorMe";
            this.bColorMe.Size = new Size( 100, 23 );
            this.bColorMe.TabIndex = 13;
            this.bColorMe.UseVisualStyleBackColor = false;
            this.bColorMe.Click += new EventHandler( this.bColorMe_Click );
            // 
            // lColorWarning
            // 
            this.lColorWarning.AutoSize = true;
            this.lColorWarning.Location = new Point( 69, 53 );
            this.lColorWarning.Name = "lColorWarning";
            this.lColorWarning.Size = new Size( 118, 15 );
            this.lColorWarning.TabIndex = 2;
            this.lColorWarning.Text = "Warning / error color";
            // 
            // bColorWarning
            // 
            this.bColorWarning.BackColor = System.Drawing.Color.White;
            this.bColorWarning.Location = new Point( 193, 49 );
            this.bColorWarning.Name = "bColorWarning";
            this.bColorWarning.Size = new Size( 100, 23 );
            this.bColorWarning.TabIndex = 3;
            this.bColorWarning.UseVisualStyleBackColor = false;
            this.bColorWarning.Click += new EventHandler( this.bColorWarning_Click );
            // 
            // bColorSys
            // 
            this.bColorSys.BackColor = System.Drawing.Color.White;
            this.bColorSys.Location = new Point( 193, 20 );
            this.bColorSys.Name = "bColorSys";
            this.bColorSys.Size = new Size( 100, 23 );
            this.bColorSys.TabIndex = 1;
            this.bColorSys.UseVisualStyleBackColor = false;
            this.bColorSys.Click += new EventHandler( this.bColorSys_Click );
            // 
            // lColorSys
            // 
            this.lColorSys.AutoSize = true;
            this.lColorSys.Location = new Point( 56, 24 );
            this.lColorSys.Name = "lColorSys";
            this.lColorSys.Size = new Size( 131, 15 );
            this.lColorSys.TabIndex = 0;
            this.lColorSys.Text = "System message color";
            // 
            // bColorPM
            // 
            this.bColorPM.BackColor = System.Drawing.Color.White;
            this.bColorPM.Location = new Point( 193, 78 );
            this.bColorPM.Name = "bColorPM";
            this.bColorPM.Size = new Size( 100, 23 );
            this.bColorPM.TabIndex = 5;
            this.bColorPM.UseVisualStyleBackColor = false;
            this.bColorPM.Click += new EventHandler( this.bColorPM_Click );
            // 
            // lColorHelp
            // 
            this.lColorHelp.AutoSize = true;
            this.lColorHelp.Location = new Point( 70, 111 );
            this.lColorHelp.Name = "lColorHelp";
            this.lColorHelp.Size = new Size( 117, 15 );
            this.lColorHelp.TabIndex = 6;
            this.lColorHelp.Text = "Help message color";
            // 
            // lColorPM
            // 
            this.lColorPM.AutoSize = true;
            this.lColorPM.Location = new Point( 26, 82 );
            this.lColorPM.Name = "lColorPM";
            this.lColorPM.Size = new Size( 161, 15 );
            this.lColorPM.TabIndex = 4;
            this.lColorPM.Text = "Private / rank message color";
            // 
            // lColorSay
            // 
            this.lColorSay.AutoSize = true;
            this.lColorSay.Location = new Point( 407, 53 );
            this.lColorSay.Name = "lColorSay";
            this.lColorSay.Size = new Size( 114, 15 );
            this.lColorSay.TabIndex = 10;
            this.lColorSay.Text = "/Say message color";
            // 
            // bColorAnnouncement
            // 
            this.bColorAnnouncement.BackColor = System.Drawing.Color.White;
            this.bColorAnnouncement.Location = new Point( 525, 20 );
            this.bColorAnnouncement.Name = "bColorAnnouncement";
            this.bColorAnnouncement.Size = new Size( 100, 23 );
            this.bColorAnnouncement.TabIndex = 9;
            this.bColorAnnouncement.UseVisualStyleBackColor = false;
            this.bColorAnnouncement.Click += new EventHandler( this.bColorAnnouncement_Click );
            // 
            // lColorAnnouncement
            // 
            this.lColorAnnouncement.AutoSize = true;
            this.lColorAnnouncement.Location = new Point( 342, 24 );
            this.lColorAnnouncement.Name = "lColorAnnouncement";
            this.lColorAnnouncement.Size = new Size( 182, 15 );
            this.lColorAnnouncement.TabIndex = 8;
            this.lColorAnnouncement.Text = "Announcement and /Rules color";
            // 
            // bColorHelp
            // 
            this.bColorHelp.BackColor = System.Drawing.Color.White;
            this.bColorHelp.Location = new Point( 193, 107 );
            this.bColorHelp.Name = "bColorHelp";
            this.bColorHelp.Size = new Size( 100, 23 );
            this.bColorHelp.TabIndex = 7;
            this.bColorHelp.UseVisualStyleBackColor = false;
            this.bColorHelp.Click += new EventHandler( this.bColorHelp_Click );
            // 
            // bColorSay
            // 
            this.bColorSay.BackColor = System.Drawing.Color.White;
            this.bColorSay.Location = new Point( 525, 49 );
            this.bColorSay.Name = "bColorSay";
            this.bColorSay.Size = new Size( 100, 23 );
            this.bColorSay.TabIndex = 11;
            this.bColorSay.UseVisualStyleBackColor = false;
            this.bColorSay.Click += new EventHandler( this.bColorSay_Click );
            // 
            // gAppearence
            // 
            this.gAppearence.Controls.Add( this.xShowConnectionMessages );
            this.gAppearence.Controls.Add( this.xShowJoinedWorldMessages );
            this.gAppearence.Controls.Add( this.xRankColorsInWorldNames );
            this.gAppearence.Controls.Add( this.xRankPrefixesInList );
            this.gAppearence.Controls.Add( this.xRankPrefixesInChat );
            this.gAppearence.Controls.Add( this.xRankColorsInChat );
            this.gAppearence.Location = new Point( 7, 153 );
            this.gAppearence.Name = "gAppearence";
            this.gAppearence.Size = new Size( 637, 97 );
            this.gAppearence.TabIndex = 1;
            this.gAppearence.TabStop = false;
            this.gAppearence.Text = "Appearence Tweaks";
            // 
            // xShowConnectionMessages
            // 
            this.xShowConnectionMessages.AutoSize = true;
            this.xShowConnectionMessages.Location = new Point( 325, 45 );
            this.xShowConnectionMessages.Name = "xShowConnectionMessages";
            this.xShowConnectionMessages.Size = new Size( 306, 19 );
            this.xShowConnectionMessages.TabIndex = 4;
            this.xShowConnectionMessages.Text = "Show a message when players join/leave SERVER.";
            this.xShowConnectionMessages.UseVisualStyleBackColor = true;
            // 
            // xShowJoinedWorldMessages
            // 
            this.xShowJoinedWorldMessages.AutoSize = true;
            this.xShowJoinedWorldMessages.Location = new Point( 325, 20 );
            this.xShowJoinedWorldMessages.Name = "xShowJoinedWorldMessages";
            this.xShowJoinedWorldMessages.Size = new Size( 261, 19 );
            this.xShowJoinedWorldMessages.TabIndex = 3;
            this.xShowJoinedWorldMessages.Text = "Show a message when players join worlds.";
            this.xShowJoinedWorldMessages.UseVisualStyleBackColor = true;
            // 
            // xRankColorsInWorldNames
            // 
            this.xRankColorsInWorldNames.AutoSize = true;
            this.xRankColorsInWorldNames.Location = new Point( 325, 70 );
            this.xRankColorsInWorldNames.Name = "xRankColorsInWorldNames";
            this.xRankColorsInWorldNames.Size = new Size( 243, 19 );
            this.xRankColorsInWorldNames.TabIndex = 5;
            this.xRankColorsInWorldNames.Text = "Color world names based on build rank.";
            this.xRankColorsInWorldNames.UseVisualStyleBackColor = true;
            // 
            // xRankPrefixesInList
            // 
            this.xRankPrefixesInList.AutoSize = true;
            this.xRankPrefixesInList.Location = new Point( 44, 70 );
            this.xRankPrefixesInList.Name = "xRankPrefixesInList";
            this.xRankPrefixesInList.Size = new Size( 219, 19 );
            this.xRankPrefixesInList.TabIndex = 2;
            this.xRankPrefixesInList.Text = "Prefixes in player list (breaks skins).";
            this.xRankPrefixesInList.UseVisualStyleBackColor = true;
            // 
            // xRankPrefixesInChat
            // 
            this.xRankPrefixesInChat.AutoSize = true;
            this.xRankPrefixesInChat.Location = new Point( 25, 45 );
            this.xRankPrefixesInChat.Name = "xRankPrefixesInChat";
            this.xRankPrefixesInChat.Size = new Size( 133, 19 );
            this.xRankPrefixesInChat.TabIndex = 1;
            this.xRankPrefixesInChat.Text = "Show rank prefixes.";
            this.xRankPrefixesInChat.UseVisualStyleBackColor = true;
            this.xRankPrefixesInChat.CheckedChanged += new EventHandler( this.xRankPrefixesInChat_CheckedChanged );
            // 
            // xRankColorsInChat
            // 
            this.xRankColorsInChat.AutoSize = true;
            this.xRankColorsInChat.Location = new Point( 25, 20 );
            this.xRankColorsInChat.Name = "xRankColorsInChat";
            this.xRankColorsInChat.Size = new Size( 123, 19 );
            this.xRankColorsInChat.TabIndex = 0;
            this.xRankColorsInChat.Text = "Show rank colors.";
            this.xRankColorsInChat.UseVisualStyleBackColor = true;
            // 
            // chatPreview
            // 
            this.chatPreview.Location = new Point( 7, 256 );
            this.chatPreview.Name = "chatPreview";
            this.chatPreview.Size = new Size( 637, 241 );
            this.chatPreview.TabIndex = 2;
            // 
            // tabWorlds
            // 
            this.tabWorlds.Controls.Add( this.xWoMEnableEnvExtensions );
            this.tabWorlds.Controls.Add( this.bMapPath );
            this.tabWorlds.Controls.Add( this.xMapPath );
            this.tabWorlds.Controls.Add( this.tMapPath );
            this.tabWorlds.Controls.Add( this.lDefaultBuildRank );
            this.tabWorlds.Controls.Add( this.cDefaultBuildRank );
            this.tabWorlds.Controls.Add( this.cMainWorld );
            this.tabWorlds.Controls.Add( this.lMainWorld );
            this.tabWorlds.Controls.Add( this.bWorldEdit );
            this.tabWorlds.Controls.Add( this.bAddWorld );
            this.tabWorlds.Controls.Add( this.bWorldDelete );
            this.tabWorlds.Controls.Add( this.dgvWorlds );
            this.tabWorlds.Location = new Point( 4, 24 );
            this.tabWorlds.Name = "tabWorlds";
            this.tabWorlds.Padding = new Padding( 5, 10, 5, 10 );
            this.tabWorlds.Size = new Size( 652, 482 );
            this.tabWorlds.TabIndex = 9;
            this.tabWorlds.Text = "Worlds";
            this.tabWorlds.UseVisualStyleBackColor = true;
            // 
            // xWoMEnableEnvExtensions
            // 
            this.xWoMEnableEnvExtensions.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.xWoMEnableEnvExtensions.AutoSize = true;
            this.xWoMEnableEnvExtensions.Location = new Point( 8, 436 );
            this.xWoMEnableEnvExtensions.Name = "xWoMEnableEnvExtensions";
            this.xWoMEnableEnvExtensions.Size = new Size( 267, 19 );
            this.xWoMEnableEnvExtensions.TabIndex = 22;
            this.xWoMEnableEnvExtensions.Text = "Enable WoM environment extensions (/Env).";
            this.xWoMEnableEnvExtensions.UseVisualStyleBackColor = true;
            // 
            // bMapPath
            // 
            this.bMapPath.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Right ) ) );
            this.bMapPath.Enabled = false;
            this.bMapPath.Location = new Point( 587, 409 );
            this.bMapPath.Name = "bMapPath";
            this.bMapPath.Size = new Size( 57, 23 );
            this.bMapPath.TabIndex = 10;
            this.bMapPath.Text = "Browse";
            this.bMapPath.UseVisualStyleBackColor = true;
            this.bMapPath.Click += new EventHandler( this.bMapPath_Click );
            // 
            // xMapPath
            // 
            this.xMapPath.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.xMapPath.AutoSize = true;
            this.xMapPath.Location = new Point( 8, 409 );
            this.xMapPath.Name = "xMapPath";
            this.xMapPath.Size = new Size( 189, 19 );
            this.xMapPath.TabIndex = 8;
            this.xMapPath.Text = "Custom path for storing maps:";
            this.xMapPath.UseVisualStyleBackColor = true;
            this.xMapPath.CheckedChanged += new EventHandler( this.xMapPath_CheckedChanged );
            // 
            // tMapPath
            // 
            this.tMapPath.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Bottom | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.tMapPath.Enabled = false;
            this.tMapPath.Font = new Font( "Lucida Console", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.tMapPath.Location = new Point( 203, 411 );
            this.tMapPath.Name = "tMapPath";
            this.tMapPath.Size = new Size( 378, 19 );
            this.tMapPath.TabIndex = 9;
            // 
            // lDefaultBuildRank
            // 
            this.lDefaultBuildRank.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.lDefaultBuildRank.AutoSize = true;
            this.lDefaultBuildRank.Location = new Point( 24, 381 );
            this.lDefaultBuildRank.Name = "lDefaultBuildRank";
            this.lDefaultBuildRank.Size = new Size( 342, 15 );
            this.lDefaultBuildRank.TabIndex = 6;
            this.lDefaultBuildRank.Text = "Default rank requirement for building on newly-loaded worlds:";
            // 
            // cDefaultBuildRank
            // 
            this.cDefaultBuildRank.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.cDefaultBuildRank.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cDefaultBuildRank.FormattingEnabled = true;
            this.cDefaultBuildRank.Location = new Point( 372, 378 );
            this.cDefaultBuildRank.Name = "cDefaultBuildRank";
            this.cDefaultBuildRank.Size = new Size( 121, 23 );
            this.cDefaultBuildRank.TabIndex = 7;
            this.cDefaultBuildRank.SelectedIndexChanged += new EventHandler( this.cDefaultBuildRank_SelectedIndexChanged );
            // 
            // cMainWorld
            // 
            this.cMainWorld.Anchor = ( (AnchorStyles)( ( AnchorStyles.Top | AnchorStyles.Right ) ) );
            this.cMainWorld.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cMainWorld.Location = new Point( 542, 17 );
            this.cMainWorld.Name = "cMainWorld";
            this.cMainWorld.Size = new Size( 102, 23 );
            this.cMainWorld.TabIndex = 5;
            // 
            // lMainWorld
            // 
            this.lMainWorld.Anchor = ( (AnchorStyles)( ( AnchorStyles.Top | AnchorStyles.Right ) ) );
            this.lMainWorld.AutoSize = true;
            this.lMainWorld.Location = new Point( 465, 20 );
            this.lMainWorld.Name = "lMainWorld";
            this.lMainWorld.Size = new Size( 71, 15 );
            this.lMainWorld.TabIndex = 4;
            this.lMainWorld.Text = "Main world:";
            // 
            // bWorldEdit
            // 
            this.bWorldEdit.Enabled = false;
            this.bWorldEdit.Location = new Point( 114, 13 );
            this.bWorldEdit.Name = "bWorldEdit";
            this.bWorldEdit.Size = new Size( 100, 28 );
            this.bWorldEdit.TabIndex = 2;
            this.bWorldEdit.Text = "Edit";
            this.bWorldEdit.UseVisualStyleBackColor = true;
            this.bWorldEdit.Click += new EventHandler( this.bWorldEdit_Click );
            // 
            // bAddWorld
            // 
            this.bAddWorld.Location = new Point( 8, 13 );
            this.bAddWorld.Name = "bAddWorld";
            this.bAddWorld.Size = new Size( 100, 28 );
            this.bAddWorld.TabIndex = 1;
            this.bAddWorld.Text = "Add World";
            this.bAddWorld.UseVisualStyleBackColor = true;
            this.bAddWorld.Click += new EventHandler( this.bAddWorld_Click );
            // 
            // bWorldDelete
            // 
            this.bWorldDelete.Enabled = false;
            this.bWorldDelete.Location = new Point( 220, 13 );
            this.bWorldDelete.Name = "bWorldDelete";
            this.bWorldDelete.Size = new Size( 100, 28 );
            this.bWorldDelete.TabIndex = 3;
            this.bWorldDelete.Text = "Delete World";
            this.bWorldDelete.UseVisualStyleBackColor = true;
            this.bWorldDelete.Click += new EventHandler( this.bWorldDel_Click );
            // 
            // dgvWorlds
            // 
            this.dgvWorlds.AllowUserToAddRows = false;
            this.dgvWorlds.AllowUserToDeleteRows = false;
            this.dgvWorlds.AllowUserToOrderColumns = true;
            this.dgvWorlds.AllowUserToResizeRows = false;
            this.dgvWorlds.Anchor = ( (AnchorStyles)( ( ( ( AnchorStyles.Top | AnchorStyles.Bottom )
                        | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.dgvWorlds.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWorlds.Columns.AddRange( new DataGridViewColumn[] {
            this.dgvcName,
            this.dgvcDescription,
            this.dgvcAccess,
            this.dgvcBuild,
            this.dgvcBackup,
            this.dgvcHidden,
            this.dgvcBlockDB} );
            this.dgvWorlds.Location = new Point( 8, 47 );
            this.dgvWorlds.MultiSelect = false;
            this.dgvWorlds.Name = "dgvWorlds";
            this.dgvWorlds.RowHeadersVisible = false;
            dataGridViewCellStyle4.Padding = new Padding( 0, 1, 0, 1 );
            this.dgvWorlds.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvWorlds.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvWorlds.Size = new Size( 636, 325 );
            this.dgvWorlds.TabIndex = 0;
            this.dgvWorlds.SelectionChanged += new EventHandler( this.dgvWorlds_Click );
            this.dgvWorlds.Click += new EventHandler( this.dgvWorlds_Click );
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
            this.dgvcDescription.Width = 130;
            // 
            // dgvcAccess
            // 
            this.dgvcAccess.DataPropertyName = "AccessPermission";
            this.dgvcAccess.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
            this.dgvcAccess.HeaderText = "Access";
            this.dgvcAccess.Name = "dgvcAccess";
            this.dgvcAccess.SortMode = DataGridViewColumnSortMode.Automatic;
            // 
            // dgvcBuild
            // 
            this.dgvcBuild.DataPropertyName = "BuildPermission";
            this.dgvcBuild.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
            this.dgvcBuild.HeaderText = "Build";
            this.dgvcBuild.Name = "dgvcBuild";
            this.dgvcBuild.SortMode = DataGridViewColumnSortMode.Automatic;
            // 
            // dgvcBackup
            // 
            this.dgvcBackup.DataPropertyName = "Backup";
            this.dgvcBackup.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
            this.dgvcBackup.HeaderText = "Backup";
            this.dgvcBackup.Name = "dgvcBackup";
            this.dgvcBackup.SortMode = DataGridViewColumnSortMode.Automatic;
            this.dgvcBackup.Width = 90;
            // 
            // dgvcHidden
            // 
            this.dgvcHidden.DataPropertyName = "Hidden";
            this.dgvcHidden.HeaderText = "Hide";
            this.dgvcHidden.Name = "dgvcHidden";
            this.dgvcHidden.SortMode = DataGridViewColumnSortMode.Automatic;
            this.dgvcHidden.Width = 40;
            // 
            // dgvcBlockDB
            // 
            this.dgvcBlockDB.DataPropertyName = "BlockDBEnabled";
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.dgvcBlockDB.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvcBlockDB.HeaderText = "BlockDB";
            this.dgvcBlockDB.Name = "dgvcBlockDB";
            this.dgvcBlockDB.SortMode = DataGridViewColumnSortMode.Automatic;
            this.dgvcBlockDB.ThreeState = true;
            this.dgvcBlockDB.Width = 60;
            // 
            // tabRanks
            // 
            this.tabRanks.Controls.Add( this.gPermissionLimits );
            this.tabRanks.Controls.Add( this.lRankList );
            this.tabRanks.Controls.Add( this.bLowerRank );
            this.tabRanks.Controls.Add( this.bRaiseRank );
            this.tabRanks.Controls.Add( this.gRankOptions );
            this.tabRanks.Controls.Add( this.bDeleteRank );
            this.tabRanks.Controls.Add( this.vPermissions );
            this.tabRanks.Controls.Add( this.bAddRank );
            this.tabRanks.Controls.Add( this.lPermissions );
            this.tabRanks.Controls.Add( this.vRanks );
            this.tabRanks.Location = new Point( 4, 24 );
            this.tabRanks.Name = "tabRanks";
            this.tabRanks.Padding = new Padding( 5, 10, 5, 10 );
            this.tabRanks.Size = new Size( 652, 482 );
            this.tabRanks.TabIndex = 2;
            this.tabRanks.Text = "Ranks";
            this.tabRanks.UseVisualStyleBackColor = true;
            // 
            // gPermissionLimits
            // 
            this.gPermissionLimits.Anchor = ( (AnchorStyles)( ( ( ( AnchorStyles.Top | AnchorStyles.Bottom )
                        | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.gPermissionLimits.Controls.Add( this.permissionLimitBoxContainer );
            this.gPermissionLimits.Location = new Point( 160, 292 );
            this.gPermissionLimits.Name = "gPermissionLimits";
            this.gPermissionLimits.Size = new Size( 307, 186 );
            this.gPermissionLimits.TabIndex = 7;
            this.gPermissionLimits.TabStop = false;
            this.gPermissionLimits.Text = "Permission Limits";
            // 
            // permissionLimitBoxContainer
            // 
            this.permissionLimitBoxContainer.AutoScroll = true;
            this.permissionLimitBoxContainer.Dock = DockStyle.Fill;
            this.permissionLimitBoxContainer.FlowDirection = FlowDirection.TopDown;
            this.permissionLimitBoxContainer.Location = new Point( 3, 17 );
            this.permissionLimitBoxContainer.Margin = new Padding( 0 );
            this.permissionLimitBoxContainer.Name = "permissionLimitBoxContainer";
            this.permissionLimitBoxContainer.Size = new Size( 301, 166 );
            this.permissionLimitBoxContainer.TabIndex = 0;
            this.permissionLimitBoxContainer.WrapContents = false;
            // 
            // lRankList
            // 
            this.lRankList.AutoSize = true;
            this.lRankList.Location = new Point( 8, 10 );
            this.lRankList.Name = "lRankList";
            this.lRankList.Size = new Size( 58, 15 );
            this.lRankList.TabIndex = 0;
            this.lRankList.Text = "Rank List";
            // 
            // bLowerRank
            // 
            this.bLowerRank.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.bLowerRank.Location = new Point( 84, 455 );
            this.bLowerRank.Name = "bLowerRank";
            this.bLowerRank.Size = new Size( 70, 23 );
            this.bLowerRank.TabIndex = 5;
            this.bLowerRank.Text = "▼ Lower";
            this.bLowerRank.UseVisualStyleBackColor = true;
            this.bLowerRank.Click += new EventHandler( this.bLowerRank_Click );
            // 
            // bRaiseRank
            // 
            this.bRaiseRank.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.bRaiseRank.Location = new Point( 8, 455 );
            this.bRaiseRank.Name = "bRaiseRank";
            this.bRaiseRank.Size = new Size( 70, 23 );
            this.bRaiseRank.TabIndex = 4;
            this.bRaiseRank.Text = "▲ Raise";
            this.bRaiseRank.UseVisualStyleBackColor = true;
            this.bRaiseRank.Click += new EventHandler( this.bRaiseRank_Click );
            // 
            // gRankOptions
            // 
            this.gRankOptions.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Top | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.gRankOptions.Controls.Add( this.lFillLimitUnits );
            this.gRankOptions.Controls.Add( this.nFillLimit );
            this.gRankOptions.Controls.Add( this.lFillLimit );
            this.gRankOptions.Controls.Add( this.nCopyPasteSlots );
            this.gRankOptions.Controls.Add( this.lCopyPasteSlots );
            this.gRankOptions.Controls.Add( this.xAllowSecurityCircumvention );
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
            this.gRankOptions.Controls.Add( this.tPrefix );
            this.gRankOptions.Controls.Add( this.lPrefix );
            this.gRankOptions.Controls.Add( this.lRankColor );
            this.gRankOptions.Controls.Add( this.tRankName );
            this.gRankOptions.Controls.Add( this.lRankName );
            this.gRankOptions.Location = new Point( 160, 13 );
            this.gRankOptions.Name = "gRankOptions";
            this.gRankOptions.Size = new Size( 307, 273 );
            this.gRankOptions.TabIndex = 6;
            this.gRankOptions.TabStop = false;
            this.gRankOptions.Text = "Rank Options";
            // 
            // lFillLimitUnits
            // 
            this.lFillLimitUnits.AutoSize = true;
            this.lFillLimitUnits.Location = new Point( 239, 245 );
            this.lFillLimitUnits.Name = "lFillLimitUnits";
            this.lFillLimitUnits.Size = new Size( 42, 15 );
            this.lFillLimitUnits.TabIndex = 24;
            this.lFillLimitUnits.Text = "blocks";
            // 
            // nFillLimit
            // 
            this.nFillLimit.Location = new Point( 174, 243 );
            this.nFillLimit.Maximum = new decimal( new int[] {
            2048,
            0,
            0,
            0} );
            this.nFillLimit.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nFillLimit.Name = "nFillLimit";
            this.nFillLimit.Size = new Size( 59, 21 );
            this.nFillLimit.TabIndex = 23;
            this.nFillLimit.Value = new decimal( new int[] {
            16,
            0,
            0,
            0} );
            this.nFillLimit.ValueChanged += new EventHandler( this.nFillLimit_ValueChanged );
            // 
            // lFillLimit
            // 
            this.lFillLimit.AutoSize = true;
            this.lFillLimit.Location = new Point( 85, 245 );
            this.lFillLimit.Name = "lFillLimit";
            this.lFillLimit.Size = new Size( 83, 15 );
            this.lFillLimit.TabIndex = 22;
            this.lFillLimit.Text = "Flood-fill limit:";
            // 
            // nCopyPasteSlots
            // 
            this.nCopyPasteSlots.Location = new Point( 174, 216 );
            this.nCopyPasteSlots.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nCopyPasteSlots.Name = "nCopyPasteSlots";
            this.nCopyPasteSlots.Size = new Size( 59, 21 );
            this.nCopyPasteSlots.TabIndex = 21;
            this.nCopyPasteSlots.ValueChanged += new EventHandler( this.nCopyPasteSlots_ValueChanged );
            // 
            // lCopyPasteSlots
            // 
            this.lCopyPasteSlots.AutoSize = true;
            this.lCopyPasteSlots.Location = new Point( 50, 218 );
            this.lCopyPasteSlots.Name = "lCopyPasteSlots";
            this.lCopyPasteSlots.Size = new Size( 118, 15 );
            this.lCopyPasteSlots.TabIndex = 20;
            this.lCopyPasteSlots.Text = "Copy/paste slot limit:";
            // 
            // xAllowSecurityCircumvention
            // 
            this.xAllowSecurityCircumvention.AutoSize = true;
            this.xAllowSecurityCircumvention.Location = new Point( 12, 165 );
            this.xAllowSecurityCircumvention.Name = "xAllowSecurityCircumvention";
            this.xAllowSecurityCircumvention.Size = new Size( 271, 19 );
            this.xAllowSecurityCircumvention.TabIndex = 16;
            this.xAllowSecurityCircumvention.Text = "Allow removing own access/build restrictions.";
            this.xAllowSecurityCircumvention.UseVisualStyleBackColor = true;
            this.xAllowSecurityCircumvention.CheckedChanged += new EventHandler( this.xAllowSecurityCircumvention_CheckedChanged );
            // 
            // lAntiGrief1
            // 
            this.lAntiGrief1.AutoSize = true;
            this.lAntiGrief1.Location = new Point( 50, 135 );
            this.lAntiGrief1.Name = "lAntiGrief1";
            this.lAntiGrief1.Size = new Size( 47, 15 );
            this.lAntiGrief1.TabIndex = 11;
            this.lAntiGrief1.Text = "Kick on";
            // 
            // lAntiGrief3
            // 
            this.lAntiGrief3.AutoSize = true;
            this.lAntiGrief3.Location = new Point( 275, 135 );
            this.lAntiGrief3.Name = "lAntiGrief3";
            this.lAntiGrief3.Size = new Size( 26, 15 );
            this.lAntiGrief3.TabIndex = 15;
            this.lAntiGrief3.Text = "sec";
            // 
            // nAntiGriefSeconds
            // 
            this.nAntiGriefSeconds.Location = new Point( 229, 133 );
            this.nAntiGriefSeconds.Name = "nAntiGriefSeconds";
            this.nAntiGriefSeconds.Size = new Size( 40, 21 );
            this.nAntiGriefSeconds.TabIndex = 14;
            this.nAntiGriefSeconds.ValueChanged += new EventHandler( this.nAntiGriefSeconds_ValueChanged );
            // 
            // bColorRank
            // 
            this.bColorRank.BackColor = System.Drawing.Color.White;
            this.bColorRank.Location = new Point( 201, 47 );
            this.bColorRank.Name = "bColorRank";
            this.bColorRank.Size = new Size( 100, 24 );
            this.bColorRank.TabIndex = 6;
            this.bColorRank.UseVisualStyleBackColor = false;
            this.bColorRank.Click += new EventHandler( this.bColorRank_Click );
            // 
            // xDrawLimit
            // 
            this.xDrawLimit.AutoSize = true;
            this.xDrawLimit.Location = new Point( 12, 190 );
            this.xDrawLimit.Name = "xDrawLimit";
            this.xDrawLimit.Size = new Size( 81, 19 );
            this.xDrawLimit.TabIndex = 17;
            this.xDrawLimit.Text = "Draw limit";
            this.xDrawLimit.UseVisualStyleBackColor = true;
            this.xDrawLimit.CheckedChanged += new EventHandler( this.xDrawLimit_CheckedChanged );
            // 
            // lDrawLimitUnits
            // 
            this.lDrawLimitUnits.AutoSize = true;
            this.lDrawLimitUnits.Location = new Point( 172, 191 );
            this.lDrawLimitUnits.Name = "lDrawLimitUnits";
            this.lDrawLimitUnits.Size = new Size( 42, 15 );
            this.lDrawLimitUnits.TabIndex = 19;
            this.lDrawLimitUnits.Text = "blocks";
            // 
            // lKickIdleUnits
            // 
            this.lKickIdleUnits.AutoSize = true;
            this.lKickIdleUnits.Location = new Point( 181, 79 );
            this.lKickIdleUnits.Name = "lKickIdleUnits";
            this.lKickIdleUnits.Size = new Size( 51, 15 );
            this.lKickIdleUnits.TabIndex = 9;
            this.lKickIdleUnits.Text = "minutes";
            // 
            // nDrawLimit
            // 
            this.nDrawLimit.Increment = new decimal( new int[] {
            32,
            0,
            0,
            0} );
            this.nDrawLimit.Location = new Point( 99, 189 );
            this.nDrawLimit.Maximum = new decimal( new int[] {
            100000000,
            0,
            0,
            0} );
            this.nDrawLimit.Name = "nDrawLimit";
            this.nDrawLimit.Size = new Size( 67, 21 );
            this.nDrawLimit.TabIndex = 18;
            this.nDrawLimit.ValueChanged += new EventHandler( this.nDrawLimit_ValueChanged );
            // 
            // nKickIdle
            // 
            this.nKickIdle.Location = new Point( 116, 77 );
            this.nKickIdle.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nKickIdle.Name = "nKickIdle";
            this.nKickIdle.Size = new Size( 59, 21 );
            this.nKickIdle.TabIndex = 8;
            this.nKickIdle.ValueChanged += new EventHandler( this.nKickIdle_ValueChanged );
            // 
            // xAntiGrief
            // 
            this.xAntiGrief.AutoSize = true;
            this.xAntiGrief.Location = new Point( 12, 108 );
            this.xAntiGrief.Name = "xAntiGrief";
            this.xAntiGrief.Size = new Size( 213, 19 );
            this.xAntiGrief.TabIndex = 10;
            this.xAntiGrief.Text = "Enable grief / autoclicker detection";
            this.xAntiGrief.UseVisualStyleBackColor = true;
            this.xAntiGrief.CheckedChanged += new EventHandler( this.xAntiGrief_CheckedChanged );
            // 
            // lAntiGrief2
            // 
            this.lAntiGrief2.AutoSize = true;
            this.lAntiGrief2.Location = new Point( 168, 135 );
            this.lAntiGrief2.Name = "lAntiGrief2";
            this.lAntiGrief2.Size = new Size( 55, 15 );
            this.lAntiGrief2.TabIndex = 13;
            this.lAntiGrief2.Text = "blocks in";
            // 
            // xKickIdle
            // 
            this.xKickIdle.AutoSize = true;
            this.xKickIdle.Location = new Point( 12, 78 );
            this.xKickIdle.Name = "xKickIdle";
            this.xKickIdle.Size = new Size( 98, 19 );
            this.xKickIdle.TabIndex = 7;
            this.xKickIdle.Text = "Kick if idle for";
            this.xKickIdle.UseVisualStyleBackColor = true;
            this.xKickIdle.CheckedChanged += new EventHandler( this.xKickIdle_CheckedChanged );
            // 
            // nAntiGriefBlocks
            // 
            this.nAntiGriefBlocks.Location = new Point( 103, 133 );
            this.nAntiGriefBlocks.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nAntiGriefBlocks.Name = "nAntiGriefBlocks";
            this.nAntiGriefBlocks.Size = new Size( 59, 21 );
            this.nAntiGriefBlocks.TabIndex = 12;
            this.nAntiGriefBlocks.ValueChanged += new EventHandler( this.nAntiGriefBlocks_ValueChanged );
            // 
            // xReserveSlot
            // 
            this.xReserveSlot.AutoSize = true;
            this.xReserveSlot.Location = new Point( 12, 51 );
            this.xReserveSlot.Name = "xReserveSlot";
            this.xReserveSlot.Size = new Size( 129, 19 );
            this.xReserveSlot.TabIndex = 4;
            this.xReserveSlot.Text = "Reserve player slot";
            this.xReserveSlot.UseVisualStyleBackColor = true;
            this.xReserveSlot.CheckedChanged += new EventHandler( this.xReserveSlot_CheckedChanged );
            // 
            // tPrefix
            // 
            this.tPrefix.Enabled = false;
            this.tPrefix.Location = new Point( 279, 20 );
            this.tPrefix.MaxLength = 1;
            this.tPrefix.Name = "tPrefix";
            this.tPrefix.Size = new Size( 22, 21 );
            this.tPrefix.TabIndex = 3;
            this.tPrefix.Validating += new CancelEventHandler( this.tPrefix_Validating );
            // 
            // lPrefix
            // 
            this.lPrefix.AutoSize = true;
            this.lPrefix.Enabled = false;
            this.lPrefix.Location = new Point( 235, 23 );
            this.lPrefix.Name = "lPrefix";
            this.lPrefix.Size = new Size( 38, 15 );
            this.lPrefix.TabIndex = 2;
            this.lPrefix.Text = "Prefix";
            // 
            // lRankColor
            // 
            this.lRankColor.AutoSize = true;
            this.lRankColor.Location = new Point( 159, 52 );
            this.lRankColor.Name = "lRankColor";
            this.lRankColor.Size = new Size( 36, 15 );
            this.lRankColor.TabIndex = 5;
            this.lRankColor.Text = "Color";
            // 
            // tRankName
            // 
            this.tRankName.Location = new Point( 62, 20 );
            this.tRankName.MaxLength = 16;
            this.tRankName.Name = "tRankName";
            this.tRankName.Size = new Size( 143, 21 );
            this.tRankName.TabIndex = 1;
            this.tRankName.Validating += new CancelEventHandler( this.tRankName_Validating );
            // 
            // lRankName
            // 
            this.lRankName.AutoSize = true;
            this.lRankName.Location = new Point( 15, 23 );
            this.lRankName.Name = "lRankName";
            this.lRankName.Size = new Size( 41, 15 );
            this.lRankName.TabIndex = 0;
            this.lRankName.Text = "Name";
            // 
            // bDeleteRank
            // 
            this.bDeleteRank.Location = new Point( 84, 28 );
            this.bDeleteRank.Name = "bDeleteRank";
            this.bDeleteRank.Size = new Size( 70, 23 );
            this.bDeleteRank.TabIndex = 3;
            this.bDeleteRank.Text = "Delete";
            this.bDeleteRank.UseVisualStyleBackColor = true;
            this.bDeleteRank.Click += new EventHandler( this.bDeleteRank_Click );
            // 
            // vPermissions
            // 
            this.vPermissions.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Top | AnchorStyles.Bottom )
                        | AnchorStyles.Right ) ) );
            this.vPermissions.CheckBoxes = true;
            this.vPermissions.Columns.AddRange( new ColumnHeader[] {
            this.chPermissions} );
            this.vPermissions.GridLines = true;
            this.vPermissions.HeaderStyle = ColumnHeaderStyle.None;
            this.vPermissions.Location = new Point( 473, 28 );
            this.vPermissions.MultiSelect = false;
            this.vPermissions.Name = "vPermissions";
            this.vPermissions.ShowGroups = false;
            this.vPermissions.ShowItemToolTips = true;
            this.vPermissions.Size = new Size( 171, 450 );
            this.vPermissions.TabIndex = 9;
            this.vPermissions.UseCompatibleStateImageBehavior = false;
            this.vPermissions.View = View.Details;
            this.vPermissions.ItemChecked += new ItemCheckedEventHandler( this.vPermissions_ItemChecked );
            // 
            // chPermissions
            // 
            this.chPermissions.Width = 150;
            // 
            // bAddRank
            // 
            this.bAddRank.Location = new Point( 8, 28 );
            this.bAddRank.Name = "bAddRank";
            this.bAddRank.Size = new Size( 70, 23 );
            this.bAddRank.TabIndex = 2;
            this.bAddRank.Text = "Add Rank";
            this.bAddRank.UseVisualStyleBackColor = true;
            this.bAddRank.Click += new EventHandler( this.bAddRank_Click );
            // 
            // lPermissions
            // 
            this.lPermissions.Anchor = ( (AnchorStyles)( ( AnchorStyles.Top | AnchorStyles.Right ) ) );
            this.lPermissions.AutoSize = true;
            this.lPermissions.Location = new Point( 473, 10 );
            this.lPermissions.Name = "lPermissions";
            this.lPermissions.Size = new Size( 107, 15 );
            this.lPermissions.TabIndex = 8;
            this.lPermissions.Text = "Rank Permissions";
            // 
            // vRanks
            // 
            this.vRanks.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Top | AnchorStyles.Bottom )
                        | AnchorStyles.Left ) ) );
            this.vRanks.Font = new Font( "Lucida Console", 11.25F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.vRanks.FormattingEnabled = true;
            this.vRanks.IntegralHeight = false;
            this.vRanks.ItemHeight = 15;
            this.vRanks.Location = new Point( 8, 57 );
            this.vRanks.Name = "vRanks";
            this.vRanks.Size = new Size( 146, 392 );
            this.vRanks.TabIndex = 1;
            this.vRanks.SelectedIndexChanged += new EventHandler( this.vRanks_SelectedIndexChanged );
            // 
            // tabSecurity
            // 
            this.tabSecurity.Controls.Add( this.gBlockDB );
            this.tabSecurity.Controls.Add( this.gSecurityMisc );
            this.tabSecurity.Controls.Add( this.gSpamChat );
            this.tabSecurity.Controls.Add( this.gVerify );
            this.tabSecurity.Location = new Point( 4, 24 );
            this.tabSecurity.Name = "tabSecurity";
            this.tabSecurity.Padding = new Padding( 5, 10, 5, 10 );
            this.tabSecurity.Size = new Size( 652, 482 );
            this.tabSecurity.TabIndex = 7;
            this.tabSecurity.Text = "Security";
            this.tabSecurity.UseVisualStyleBackColor = true;
            // 
            // gBlockDB
            // 
            this.gBlockDB.Controls.Add( this.cBlockDBAutoEnableRank );
            this.gBlockDB.Controls.Add( this.xBlockDBAutoEnable );
            this.gBlockDB.Controls.Add( this.xBlockDBEnabled );
            this.gBlockDB.Location = new Point( 8, 100 );
            this.gBlockDB.Name = "gBlockDB";
            this.gBlockDB.Size = new Size( 636, 88 );
            this.gBlockDB.TabIndex = 1;
            this.gBlockDB.TabStop = false;
            this.gBlockDB.Text = "BlockDB";
            // 
            // cBlockDBAutoEnableRank
            // 
            this.cBlockDBAutoEnableRank.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cBlockDBAutoEnableRank.FormattingEnabled = true;
            this.cBlockDBAutoEnableRank.Location = new Point( 442, 53 );
            this.cBlockDBAutoEnableRank.Name = "cBlockDBAutoEnableRank";
            this.cBlockDBAutoEnableRank.Size = new Size( 121, 23 );
            this.cBlockDBAutoEnableRank.TabIndex = 2;
            this.cBlockDBAutoEnableRank.TabStop = false;
            this.cBlockDBAutoEnableRank.SelectedIndexChanged += new EventHandler( this.cBlockDBAutoEnableRank_SelectedIndexChanged );
            // 
            // xBlockDBAutoEnable
            // 
            this.xBlockDBAutoEnable.AutoSize = true;
            this.xBlockDBAutoEnable.Enabled = false;
            this.xBlockDBAutoEnable.Location = new Point( 76, 55 );
            this.xBlockDBAutoEnable.Name = "xBlockDBAutoEnable";
            this.xBlockDBAutoEnable.Size = new Size( 360, 19 );
            this.xBlockDBAutoEnable.TabIndex = 1;
            this.xBlockDBAutoEnable.TabStop = false;
            this.xBlockDBAutoEnable.Text = "Automatically enable BlockDB on worlds that can be edited by";
            this.xBlockDBAutoEnable.UseVisualStyleBackColor = true;
            this.xBlockDBAutoEnable.CheckedChanged += new EventHandler( this.xBlockDBAutoEnable_CheckedChanged );
            // 
            // xBlockDBEnabled
            // 
            this.xBlockDBEnabled.AutoSize = true;
            this.xBlockDBEnabled.Location = new Point( 42, 30 );
            this.xBlockDBEnabled.Name = "xBlockDBEnabled";
            this.xBlockDBEnabled.Size = new Size( 249, 19 );
            this.xBlockDBEnabled.TabIndex = 0;
            this.xBlockDBEnabled.Text = "Enable BlockDB (per-block edit tracking).";
            this.xBlockDBEnabled.UseVisualStyleBackColor = true;
            this.xBlockDBEnabled.CheckedChanged += new EventHandler( this.xBlockDBEnabled_CheckedChanged );
            // 
            // gSecurityMisc
            // 
            this.gSecurityMisc.Controls.Add( this.xAnnounceRankChangeReasons );
            this.gSecurityMisc.Controls.Add( this.xRequireKickReason );
            this.gSecurityMisc.Controls.Add( this.xPaidPlayersOnly );
            this.gSecurityMisc.Controls.Add( this.lPatrolledRankAndBelow );
            this.gSecurityMisc.Controls.Add( this.cPatrolledRank );
            this.gSecurityMisc.Controls.Add( this.lPatrolledRank );
            this.gSecurityMisc.Controls.Add( this.xAnnounceRankChanges );
            this.gSecurityMisc.Controls.Add( this.xAnnounceKickAndBanReasons );
            this.gSecurityMisc.Controls.Add( this.xRequireRankChangeReason );
            this.gSecurityMisc.Controls.Add( this.xRequireBanReason );
            this.gSecurityMisc.Location = new Point( 8, 294 );
            this.gSecurityMisc.Name = "gSecurityMisc";
            this.gSecurityMisc.Size = new Size( 636, 178 );
            this.gSecurityMisc.TabIndex = 3;
            this.gSecurityMisc.TabStop = false;
            this.gSecurityMisc.Text = "Misc";
            // 
            // xAnnounceRankChangeReasons
            // 
            this.xAnnounceRankChangeReasons.AutoSize = true;
            this.xAnnounceRankChangeReasons.Location = new Point( 336, 109 );
            this.xAnnounceRankChangeReasons.Name = "xAnnounceRankChangeReasons";
            this.xAnnounceRankChangeReasons.Size = new Size( 253, 19 );
            this.xAnnounceRankChangeReasons.TabIndex = 6;
            this.xAnnounceRankChangeReasons.Text = "Announce promotion && demotion reasons";
            this.xAnnounceRankChangeReasons.UseVisualStyleBackColor = true;
            // 
            // xRequireKickReason
            // 
            this.xRequireKickReason.AutoSize = true;
            this.xRequireKickReason.Location = new Point( 42, 59 );
            this.xRequireKickReason.Name = "xRequireKickReason";
            this.xRequireKickReason.Size = new Size( 135, 19 );
            this.xRequireKickReason.TabIndex = 1;
            this.xRequireKickReason.Text = "Require kick reason";
            this.xRequireKickReason.UseVisualStyleBackColor = true;
            // 
            // xPaidPlayersOnly
            // 
            this.xPaidPlayersOnly.AutoSize = true;
            this.xPaidPlayersOnly.Location = new Point( 42, 20 );
            this.xPaidPlayersOnly.Name = "xPaidPlayersOnly";
            this.xPaidPlayersOnly.Size = new Size( 489, 19 );
            this.xPaidPlayersOnly.TabIndex = 0;
            this.xPaidPlayersOnly.Text = "Only allow players with paid Minecraft accounts to join the server (not recommend" +
                "ed).";
            this.xPaidPlayersOnly.UseVisualStyleBackColor = true;
            // 
            // lPatrolledRankAndBelow
            // 
            this.lPatrolledRankAndBelow.AutoSize = true;
            this.lPatrolledRankAndBelow.Location = new Point( 282, 145 );
            this.lPatrolledRankAndBelow.Name = "lPatrolledRankAndBelow";
            this.lPatrolledRankAndBelow.Size = new Size( 72, 15 );
            this.lPatrolledRankAndBelow.TabIndex = 9;
            this.lPatrolledRankAndBelow.Text = "(and below)";
            // 
            // cPatrolledRank
            // 
            this.cPatrolledRank.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cPatrolledRank.FormattingEnabled = true;
            this.cPatrolledRank.Location = new Point( 153, 142 );
            this.cPatrolledRank.Name = "cPatrolledRank";
            this.cPatrolledRank.Size = new Size( 123, 23 );
            this.cPatrolledRank.TabIndex = 8;
            this.cPatrolledRank.SelectedIndexChanged += new EventHandler( this.cPatrolledRank_SelectedIndexChanged );
            // 
            // lPatrolledRank
            // 
            this.lPatrolledRank.AutoSize = true;
            this.lPatrolledRank.Location = new Point( 64, 145 );
            this.lPatrolledRank.Name = "lPatrolledRank";
            this.lPatrolledRank.Size = new Size( 83, 15 );
            this.lPatrolledRank.TabIndex = 7;
            this.lPatrolledRank.Text = "Patrolled rank";
            // 
            // xAnnounceRankChanges
            // 
            this.xAnnounceRankChanges.AutoSize = true;
            this.xAnnounceRankChanges.Location = new Point( 304, 84 );
            this.xAnnounceRankChanges.Name = "xAnnounceRankChanges";
            this.xAnnounceRankChanges.Size = new Size( 231, 19 );
            this.xAnnounceRankChanges.TabIndex = 5;
            this.xAnnounceRankChanges.Text = "Announce promotions and demotions";
            this.xAnnounceRankChanges.UseVisualStyleBackColor = true;
            this.xAnnounceRankChanges.CheckedChanged += new EventHandler( this.xAnnounceRankChanges_CheckedChanged );
            // 
            // xAnnounceKickAndBanReasons
            // 
            this.xAnnounceKickAndBanReasons.AutoSize = true;
            this.xAnnounceKickAndBanReasons.Location = new Point( 304, 59 );
            this.xAnnounceKickAndBanReasons.Name = "xAnnounceKickAndBanReasons";
            this.xAnnounceKickAndBanReasons.Size = new Size( 244, 19 );
            this.xAnnounceKickAndBanReasons.TabIndex = 4;
            this.xAnnounceKickAndBanReasons.Text = "Announce kick, ban, and unban reasons";
            this.xAnnounceKickAndBanReasons.UseVisualStyleBackColor = true;
            // 
            // xRequireRankChangeReason
            // 
            this.xRequireRankChangeReason.AutoSize = true;
            this.xRequireRankChangeReason.Location = new Point( 42, 109 );
            this.xRequireRankChangeReason.Name = "xRequireRankChangeReason";
            this.xRequireRankChangeReason.Size = new Size( 236, 19 );
            this.xRequireRankChangeReason.TabIndex = 3;
            this.xRequireRankChangeReason.Text = "Require promotion && demotion reason";
            this.xRequireRankChangeReason.UseVisualStyleBackColor = true;
            // 
            // xRequireBanReason
            // 
            this.xRequireBanReason.AutoSize = true;
            this.xRequireBanReason.Location = new Point( 42, 84 );
            this.xRequireBanReason.Name = "xRequireBanReason";
            this.xRequireBanReason.Size = new Size( 184, 19 );
            this.xRequireBanReason.TabIndex = 2;
            this.xRequireBanReason.Text = "Require ban && unban reason";
            this.xRequireBanReason.UseVisualStyleBackColor = true;
            // 
            // gSpamChat
            // 
            this.gSpamChat.Controls.Add( this.xAntispamMuteDuration );
            this.gSpamChat.Controls.Add( this.xAntispamMessageCount );
            this.gSpamChat.Controls.Add( this.lAntispamMaxWarnings );
            this.gSpamChat.Controls.Add( this.nAntispamMaxWarnings );
            this.gSpamChat.Controls.Add( this.xAntispamKicks );
            this.gSpamChat.Controls.Add( this.lAntispamMuteDurationUnits );
            this.gSpamChat.Controls.Add( this.lAntispamIntervalUnits );
            this.gSpamChat.Controls.Add( this.nAntispamMuteDuration );
            this.gSpamChat.Controls.Add( this.nAntispamInterval );
            this.gSpamChat.Controls.Add( this.lAntispamMessageCount );
            this.gSpamChat.Controls.Add( this.nAntispamMessageCount );
            this.gSpamChat.Location = new Point( 8, 194 );
            this.gSpamChat.Name = "gSpamChat";
            this.gSpamChat.Size = new Size( 636, 94 );
            this.gSpamChat.TabIndex = 2;
            this.gSpamChat.TabStop = false;
            this.gSpamChat.Text = "Chat Spam Prevention";
            // 
            // xAntispamMuteDuration
            // 
            this.xAntispamMuteDuration.AutoSize = true;
            this.xAntispamMuteDuration.Enabled = false;
            this.xAntispamMuteDuration.Location = new Point( 42, 60 );
            this.xAntispamMuteDuration.Name = "xAntispamMuteDuration";
            this.xAntispamMuteDuration.Size = new Size( 127, 19 );
            this.xAntispamMuteDuration.TabIndex = 12;
            this.xAntispamMuteDuration.Text = "Mute spammer for";
            this.xAntispamMuteDuration.UseVisualStyleBackColor = true;
            this.xAntispamMuteDuration.CheckedChanged += new EventHandler( this.xAntispamMuteDuration_CheckedChanged );
            // 
            // xAntispamMessageCount
            // 
            this.xAntispamMessageCount.AutoSize = true;
            this.xAntispamMessageCount.Location = new Point( 42, 26 );
            this.xAntispamMessageCount.Name = "xAntispamMessageCount";
            this.xAntispamMessageCount.Size = new Size( 116, 19 );
            this.xAntispamMessageCount.TabIndex = 11;
            this.xAntispamMessageCount.Text = "Limit chat rate to";
            this.xAntispamMessageCount.UseVisualStyleBackColor = true;
            this.xAntispamMessageCount.CheckedChanged += new EventHandler( this.xAntispamMessageCount_CheckedChanged );
            // 
            // lAntispamMaxWarnings
            // 
            this.lAntispamMaxWarnings.AutoSize = true;
            this.lAntispamMaxWarnings.Enabled = false;
            this.lAntispamMaxWarnings.Location = new Point( 569, 61 );
            this.lAntispamMaxWarnings.Name = "lAntispamMaxWarnings";
            this.lAntispamMaxWarnings.Size = new Size( 57, 15 );
            this.lAntispamMaxWarnings.TabIndex = 10;
            this.lAntispamMaxWarnings.Text = "warnings";
            // 
            // nAntispamMaxWarnings
            // 
            this.nAntispamMaxWarnings.Enabled = false;
            this.nAntispamMaxWarnings.Location = new Point( 501, 59 );
            this.nAntispamMaxWarnings.Name = "nAntispamMaxWarnings";
            this.nAntispamMaxWarnings.Size = new Size( 62, 21 );
            this.nAntispamMaxWarnings.TabIndex = 9;
            // 
            // xAntispamKicks
            // 
            this.xAntispamKicks.AutoSize = true;
            this.xAntispamKicks.Enabled = false;
            this.xAntispamKicks.Location = new Point( 419, 60 );
            this.xAntispamKicks.Name = "xAntispamKicks";
            this.xAntispamKicks.Size = new Size( 76, 19 );
            this.xAntispamKicks.TabIndex = 8;
            this.xAntispamKicks.Text = "Kick after";
            this.xAntispamKicks.UseVisualStyleBackColor = true;
            this.xAntispamKicks.CheckedChanged += new EventHandler( this.xSpamChatKick_CheckedChanged );
            // 
            // lAntispamMuteDurationUnits
            // 
            this.lAntispamMuteDurationUnits.AutoSize = true;
            this.lAntispamMuteDurationUnits.Enabled = false;
            this.lAntispamMuteDurationUnits.Location = new Point( 243, 61 );
            this.lAntispamMuteDurationUnits.Name = "lAntispamMuteDurationUnits";
            this.lAntispamMuteDurationUnits.Size = new Size( 53, 15 );
            this.lAntispamMuteDurationUnits.TabIndex = 7;
            this.lAntispamMuteDurationUnits.Text = "seconds";
            // 
            // lAntispamIntervalUnits
            // 
            this.lAntispamIntervalUnits.AutoSize = true;
            this.lAntispamIntervalUnits.Enabled = false;
            this.lAntispamIntervalUnits.Location = new Point( 383, 27 );
            this.lAntispamIntervalUnits.Name = "lAntispamIntervalUnits";
            this.lAntispamIntervalUnits.Size = new Size( 53, 15 );
            this.lAntispamIntervalUnits.TabIndex = 4;
            this.lAntispamIntervalUnits.Text = "seconds";
            // 
            // nAntispamMuteDuration
            // 
            this.nAntispamMuteDuration.Enabled = false;
            this.nAntispamMuteDuration.Location = new Point( 175, 59 );
            this.nAntispamMuteDuration.Name = "nAntispamMuteDuration";
            this.nAntispamMuteDuration.Size = new Size( 62, 21 );
            this.nAntispamMuteDuration.TabIndex = 6;
            this.nAntispamMuteDuration.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // nAntispamInterval
            // 
            this.nAntispamInterval.Enabled = false;
            this.nAntispamInterval.Location = new Point( 315, 25 );
            this.nAntispamInterval.Maximum = new decimal( new int[] {
            50,
            0,
            0,
            0} );
            this.nAntispamInterval.Name = "nAntispamInterval";
            this.nAntispamInterval.Size = new Size( 62, 21 );
            this.nAntispamInterval.TabIndex = 3;
            this.nAntispamInterval.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lAntispamMessageCount
            // 
            this.lAntispamMessageCount.AutoSize = true;
            this.lAntispamMessageCount.Enabled = false;
            this.lAntispamMessageCount.Location = new Point( 232, 27 );
            this.lAntispamMessageCount.Name = "lAntispamMessageCount";
            this.lAntispamMessageCount.Size = new Size( 77, 15 );
            this.lAntispamMessageCount.TabIndex = 2;
            this.lAntispamMessageCount.Text = "messages in";
            // 
            // nAntispamMessageCount
            // 
            this.nAntispamMessageCount.Enabled = false;
            this.nAntispamMessageCount.Location = new Point( 164, 25 );
            this.nAntispamMessageCount.Maximum = new decimal( new int[] {
            50,
            0,
            0,
            0} );
            this.nAntispamMessageCount.Name = "nAntispamMessageCount";
            this.nAntispamMessageCount.Size = new Size( 62, 21 );
            this.nAntispamMessageCount.TabIndex = 1;
            this.nAntispamMessageCount.Value = new decimal( new int[] {
            2,
            0,
            0,
            0} );
            // 
            // gVerify
            // 
            this.gVerify.Controls.Add( this.nMaxConnectionsPerIP );
            this.gVerify.Controls.Add( this.xAllowUnverifiedLAN );
            this.gVerify.Controls.Add( this.xMaxConnectionsPerIP );
            this.gVerify.Controls.Add( this.lVerifyNames );
            this.gVerify.Controls.Add( this.cVerifyNames );
            this.gVerify.Location = new Point( 8, 13 );
            this.gVerify.Name = "gVerify";
            this.gVerify.Size = new Size( 636, 81 );
            this.gVerify.TabIndex = 0;
            this.gVerify.TabStop = false;
            this.gVerify.Text = "Connection";
            // 
            // nMaxConnectionsPerIP
            // 
            this.nMaxConnectionsPerIP.Location = new Point( 539, 21 );
            this.nMaxConnectionsPerIP.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nMaxConnectionsPerIP.Name = "nMaxConnectionsPerIP";
            this.nMaxConnectionsPerIP.Size = new Size( 47, 21 );
            this.nMaxConnectionsPerIP.TabIndex = 4;
            this.nMaxConnectionsPerIP.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // xAllowUnverifiedLAN
            // 
            this.xAllowUnverifiedLAN.AutoSize = true;
            this.xAllowUnverifiedLAN.Location = new Point( 42, 49 );
            this.xAllowUnverifiedLAN.Name = "xAllowUnverifiedLAN";
            this.xAllowUnverifiedLAN.Size = new Size( 490, 19 );
            this.xAllowUnverifiedLAN.TabIndex = 2;
            this.xAllowUnverifiedLAN.Text = "Allow connections from LAN without name verification (192.168.0.0/16 and 10.0.0.0" +
                "/8)";
            this.xAllowUnverifiedLAN.UseVisualStyleBackColor = true;
            // 
            // xMaxConnectionsPerIP
            // 
            this.xMaxConnectionsPerIP.AutoSize = true;
            this.xMaxConnectionsPerIP.Location = new Point( 304, 22 );
            this.xMaxConnectionsPerIP.Name = "xMaxConnectionsPerIP";
            this.xMaxConnectionsPerIP.Size = new Size( 229, 19 );
            this.xMaxConnectionsPerIP.TabIndex = 3;
            this.xMaxConnectionsPerIP.Text = "Limit number of connections per IP to";
            this.xMaxConnectionsPerIP.UseVisualStyleBackColor = true;
            this.xMaxConnectionsPerIP.CheckedChanged += new EventHandler( this.xMaxConnectionsPerIP_CheckedChanged );
            // 
            // lVerifyNames
            // 
            this.lVerifyNames.AutoSize = true;
            this.lVerifyNames.Location = new Point( 45, 23 );
            this.lVerifyNames.Name = "lVerifyNames";
            this.lVerifyNames.Size = new Size( 102, 15 );
            this.lVerifyNames.TabIndex = 0;
            this.lVerifyNames.Text = "Name verification";
            // 
            // cVerifyNames
            // 
            this.cVerifyNames.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cVerifyNames.FormattingEnabled = true;
            this.cVerifyNames.Items.AddRange( new object[] {
            "None (Unsafe)",
            "Normal",
            "Strict"} );
            this.cVerifyNames.Location = new Point( 153, 20 );
            this.cVerifyNames.Name = "cVerifyNames";
            this.cVerifyNames.Size = new Size( 120, 23 );
            this.cVerifyNames.TabIndex = 1;
            this.cVerifyNames.SelectedIndexChanged += new EventHandler( this.cVerifyNames_SelectedIndexChanged );
            // 
            // tabSavingAndBackup
            // 
            this.tabSavingAndBackup.Controls.Add( this.gDataBackup );
            this.tabSavingAndBackup.Controls.Add( this.gSaving );
            this.tabSavingAndBackup.Controls.Add( this.gBackups );
            this.tabSavingAndBackup.Location = new Point( 4, 24 );
            this.tabSavingAndBackup.Name = "tabSavingAndBackup";
            this.tabSavingAndBackup.Padding = new Padding( 5, 10, 5, 10 );
            this.tabSavingAndBackup.Size = new Size( 652, 482 );
            this.tabSavingAndBackup.TabIndex = 4;
            this.tabSavingAndBackup.Text = "Saving and Backup";
            this.tabSavingAndBackup.UseVisualStyleBackColor = true;
            // 
            // gDataBackup
            // 
            this.gDataBackup.Controls.Add( this.xBackupDataOnStartup );
            this.gDataBackup.Location = new Point( 8, 235 );
            this.gDataBackup.Name = "gDataBackup";
            this.gDataBackup.Size = new Size( 636, 52 );
            this.gDataBackup.TabIndex = 2;
            this.gDataBackup.TabStop = false;
            this.gDataBackup.Text = "Data Backup";
            // 
            // xBackupDataOnStartup
            // 
            this.xBackupDataOnStartup.AutoSize = true;
            this.xBackupDataOnStartup.Location = new Point( 16, 20 );
            this.xBackupDataOnStartup.Name = "xBackupDataOnStartup";
            this.xBackupDataOnStartup.Size = new Size( 261, 19 );
            this.xBackupDataOnStartup.TabIndex = 0;
            this.xBackupDataOnStartup.Text = "Backup PlayerDB and IP ban list on startup.";
            this.xBackupDataOnStartup.UseVisualStyleBackColor = true;
            // 
            // gSaving
            // 
            this.gSaving.Controls.Add( this.nSaveInterval );
            this.gSaving.Controls.Add( this.lSaveIntervalUnits );
            this.gSaving.Controls.Add( this.xSaveInterval );
            this.gSaving.Location = new Point( 8, 13 );
            this.gSaving.Name = "gSaving";
            this.gSaving.Size = new Size( 636, 52 );
            this.gSaving.TabIndex = 0;
            this.gSaving.TabStop = false;
            this.gSaving.Text = "Map Saving";
            // 
            // nSaveInterval
            // 
            this.nSaveInterval.Location = new Point( 136, 20 );
            this.nSaveInterval.Maximum = new decimal( new int[] {
            86400,
            0,
            0,
            0} );
            this.nSaveInterval.Name = "nSaveInterval";
            this.nSaveInterval.Size = new Size( 48, 21 );
            this.nSaveInterval.TabIndex = 1;
            // 
            // lSaveIntervalUnits
            // 
            this.lSaveIntervalUnits.AutoSize = true;
            this.lSaveIntervalUnits.Location = new Point( 190, 22 );
            this.lSaveIntervalUnits.Name = "lSaveIntervalUnits";
            this.lSaveIntervalUnits.Size = new Size( 53, 15 );
            this.lSaveIntervalUnits.TabIndex = 2;
            this.lSaveIntervalUnits.Text = "seconds";
            // 
            // xSaveInterval
            // 
            this.xSaveInterval.AutoSize = true;
            this.xSaveInterval.Location = new Point( 12, 21 );
            this.xSaveInterval.Name = "xSaveInterval";
            this.xSaveInterval.Size = new Size( 118, 19 );
            this.xSaveInterval.TabIndex = 0;
            this.xSaveInterval.Text = "Save maps every";
            this.xSaveInterval.UseVisualStyleBackColor = true;
            this.xSaveInterval.CheckedChanged += new EventHandler( this.xSaveAtInterval_CheckedChanged );
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
            this.gBackups.Location = new Point( 8, 71 );
            this.gBackups.Name = "gBackups";
            this.gBackups.Size = new Size( 636, 158 );
            this.gBackups.TabIndex = 1;
            this.gBackups.TabStop = false;
            this.gBackups.Text = "Map Backups";
            // 
            // xBackupOnlyWhenChanged
            // 
            this.xBackupOnlyWhenChanged.AutoSize = true;
            this.xBackupOnlyWhenChanged.Location = new Point( 369, 46 );
            this.xBackupOnlyWhenChanged.Name = "xBackupOnlyWhenChanged";
            this.xBackupOnlyWhenChanged.Size = new Size( 260, 19 );
            this.xBackupOnlyWhenChanged.TabIndex = 4;
            this.xBackupOnlyWhenChanged.Text = "Skip timed backups if map hasn\'t changed.";
            this.xBackupOnlyWhenChanged.UseVisualStyleBackColor = true;
            // 
            // lMaxBackupSize
            // 
            this.lMaxBackupSize.AutoSize = true;
            this.lMaxBackupSize.Location = new Point( 418, 124 );
            this.lMaxBackupSize.Name = "lMaxBackupSize";
            this.lMaxBackupSize.Size = new Size( 103, 15 );
            this.lMaxBackupSize.TabIndex = 11;
            this.lMaxBackupSize.Text = "MB of disk space.";
            // 
            // xMaxBackupSize
            // 
            this.xMaxBackupSize.AutoSize = true;
            this.xMaxBackupSize.Location = new Point( 16, 123 );
            this.xMaxBackupSize.Name = "xMaxBackupSize";
            this.xMaxBackupSize.Size = new Size( 317, 19 );
            this.xMaxBackupSize.TabIndex = 9;
            this.xMaxBackupSize.Text = "Delete old backups if the directory takes up more than";
            this.xMaxBackupSize.UseVisualStyleBackColor = true;
            this.xMaxBackupSize.CheckedChanged += new EventHandler( this.xMaxBackupSize_CheckedChanged );
            // 
            // nMaxBackupSize
            // 
            this.nMaxBackupSize.Location = new Point( 339, 122 );
            this.nMaxBackupSize.Maximum = new decimal( new int[] {
            1000000,
            0,
            0,
            0} );
            this.nMaxBackupSize.Name = "nMaxBackupSize";
            this.nMaxBackupSize.Size = new Size( 73, 21 );
            this.nMaxBackupSize.TabIndex = 10;
            // 
            // xMaxBackups
            // 
            this.xMaxBackups.AutoSize = true;
            this.xMaxBackups.Location = new Point( 16, 98 );
            this.xMaxBackups.Name = "xMaxBackups";
            this.xMaxBackups.Size = new Size( 251, 19 );
            this.xMaxBackups.TabIndex = 6;
            this.xMaxBackups.Text = "Delete old backups if there are more than";
            this.xMaxBackups.UseVisualStyleBackColor = true;
            this.xMaxBackups.CheckedChanged += new EventHandler( this.xMaxBackups_CheckedChanged );
            // 
            // xBackupOnStartup
            // 
            this.xBackupOnStartup.AutoSize = true;
            this.xBackupOnStartup.Enabled = false;
            this.xBackupOnStartup.Location = new Point( 16, 20 );
            this.xBackupOnStartup.Name = "xBackupOnStartup";
            this.xBackupOnStartup.Size = new Size( 168, 19 );
            this.xBackupOnStartup.TabIndex = 0;
            this.xBackupOnStartup.Text = "Create backups on startup";
            this.xBackupOnStartup.UseVisualStyleBackColor = true;
            // 
            // lMaxBackups
            // 
            this.lMaxBackups.AutoSize = true;
            this.lMaxBackups.Location = new Point( 336, 99 );
            this.lMaxBackups.Name = "lMaxBackups";
            this.lMaxBackups.Size = new Size( 157, 15 );
            this.lMaxBackups.TabIndex = 8;
            this.lMaxBackups.Text = "files in the backup directory.";
            // 
            // nMaxBackups
            // 
            this.nMaxBackups.Location = new Point( 273, 97 );
            this.nMaxBackups.Maximum = new decimal( new int[] {
            100000,
            0,
            0,
            0} );
            this.nMaxBackups.Name = "nMaxBackups";
            this.nMaxBackups.Size = new Size( 57, 21 );
            this.nMaxBackups.TabIndex = 7;
            // 
            // nBackupInterval
            // 
            this.nBackupInterval.Location = new Point( 164, 45 );
            this.nBackupInterval.Maximum = new decimal( new int[] {
            100000,
            0,
            0,
            0} );
            this.nBackupInterval.Name = "nBackupInterval";
            this.nBackupInterval.Size = new Size( 48, 21 );
            this.nBackupInterval.TabIndex = 2;
            // 
            // lBackupIntervalUnits
            // 
            this.lBackupIntervalUnits.AutoSize = true;
            this.lBackupIntervalUnits.Location = new Point( 218, 47 );
            this.lBackupIntervalUnits.Name = "lBackupIntervalUnits";
            this.lBackupIntervalUnits.Size = new Size( 51, 15 );
            this.lBackupIntervalUnits.TabIndex = 3;
            this.lBackupIntervalUnits.Text = "minutes";
            // 
            // xBackupInterval
            // 
            this.xBackupInterval.AutoSize = true;
            this.xBackupInterval.Location = new Point( 16, 46 );
            this.xBackupInterval.Name = "xBackupInterval";
            this.xBackupInterval.Size = new Size( 142, 19 );
            this.xBackupInterval.TabIndex = 1;
            this.xBackupInterval.Text = "Create backups every";
            this.xBackupInterval.UseVisualStyleBackColor = true;
            this.xBackupInterval.CheckedChanged += new EventHandler( this.xBackupAtInterval_CheckedChanged );
            // 
            // xBackupOnJoin
            // 
            this.xBackupOnJoin.AutoSize = true;
            this.xBackupOnJoin.Location = new Point( 16, 72 );
            this.xBackupOnJoin.Name = "xBackupOnJoin";
            this.xBackupOnJoin.Size = new Size( 279, 19 );
            this.xBackupOnJoin.TabIndex = 5;
            this.xBackupOnJoin.Text = "Create backup whenever a player joins a world";
            this.xBackupOnJoin.UseVisualStyleBackColor = true;
            // 
            // tabLogging
            // 
            this.tabLogging.Controls.Add( this.gLogFile );
            this.tabLogging.Controls.Add( this.gConsole );
            this.tabLogging.Location = new Point( 4, 24 );
            this.tabLogging.Name = "tabLogging";
            this.tabLogging.Padding = new Padding( 5, 10, 5, 10 );
            this.tabLogging.Size = new Size( 652, 482 );
            this.tabLogging.TabIndex = 5;
            this.tabLogging.Text = "Logging";
            this.tabLogging.UseVisualStyleBackColor = true;
            // 
            // gLogFile
            // 
            this.gLogFile.Controls.Add( this.lLogFileOptionsDescription );
            this.gLogFile.Controls.Add( this.xLogLimit );
            this.gLogFile.Controls.Add( this.vLogFileOptions );
            this.gLogFile.Controls.Add( this.lLogLimitUnits );
            this.gLogFile.Controls.Add( this.nLogLimit );
            this.gLogFile.Controls.Add( this.cLogMode );
            this.gLogFile.Controls.Add( this.lLogMode );
            this.gLogFile.Location = new Point( 329, 13 );
            this.gLogFile.Name = "gLogFile";
            this.gLogFile.Size = new Size( 315, 423 );
            this.gLogFile.TabIndex = 1;
            this.gLogFile.TabStop = false;
            this.gLogFile.Text = "Log File";
            // 
            // lLogFileOptionsDescription
            // 
            this.lLogFileOptionsDescription.AutoSize = true;
            this.lLogFileOptionsDescription.Location = new Point( 27, 22 );
            this.lLogFileOptionsDescription.Name = "lLogFileOptionsDescription";
            this.lLogFileOptionsDescription.Size = new Size( 212, 30 );
            this.lLogFileOptionsDescription.TabIndex = 0;
            this.lLogFileOptionsDescription.Text = "Types of messages that will be written\r\nto the log file on disk.";
            // 
            // xLogLimit
            // 
            this.xLogLimit.AutoSize = true;
            this.xLogLimit.Enabled = false;
            this.xLogLimit.Location = new Point( 18, 390 );
            this.xLogLimit.Name = "xLogLimit";
            this.xLogLimit.Size = new Size( 80, 19 );
            this.xLogLimit.TabIndex = 4;
            this.xLogLimit.Text = "Only keep";
            this.xLogLimit.UseVisualStyleBackColor = true;
            this.xLogLimit.CheckedChanged += new EventHandler( this.xLogLimit_CheckedChanged );
            // 
            // vLogFileOptions
            // 
            this.vLogFileOptions.CheckBoxes = true;
            this.vLogFileOptions.Columns.AddRange( new ColumnHeader[] {
            this.columnHeader2} );
            this.vLogFileOptions.GridLines = true;
            this.vLogFileOptions.HeaderStyle = ColumnHeaderStyle.None;
            this.vLogFileOptions.Location = new Point( 78, 59 );
            this.vLogFileOptions.Name = "vLogFileOptions";
            this.vLogFileOptions.ShowItemToolTips = true;
            this.vLogFileOptions.Size = new Size( 161, 294 );
            this.vLogFileOptions.TabIndex = 1;
            this.vLogFileOptions.UseCompatibleStateImageBehavior = false;
            this.vLogFileOptions.View = View.Details;
            this.vLogFileOptions.ItemChecked += new ItemCheckedEventHandler( this.vLogFileOptions_ItemChecked );
            // 
            // columnHeader2
            // 
            this.columnHeader2.Width = 157;
            // 
            // lLogLimitUnits
            // 
            this.lLogLimitUnits.AutoSize = true;
            this.lLogLimitUnits.Location = new Point( 166, 391 );
            this.lLogLimitUnits.Name = "lLogLimitUnits";
            this.lLogLimitUnits.Size = new Size( 129, 15 );
            this.lLogLimitUnits.TabIndex = 6;
            this.lLogLimitUnits.Text = "of most recent log files";
            // 
            // nLogLimit
            // 
            this.nLogLimit.Enabled = false;
            this.nLogLimit.Location = new Point( 104, 389 );
            this.nLogLimit.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nLogLimit.Name = "nLogLimit";
            this.nLogLimit.Size = new Size( 56, 21 );
            this.nLogLimit.TabIndex = 5;
            // 
            // cLogMode
            // 
            this.cLogMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cLogMode.FormattingEnabled = true;
            this.cLogMode.Items.AddRange( new object[] {
            "One long file",
            "Multiple files, split by session",
            "Multiple files, split by day"} );
            this.cLogMode.Location = new Point( 104, 360 );
            this.cLogMode.Name = "cLogMode";
            this.cLogMode.Size = new Size( 185, 23 );
            this.cLogMode.TabIndex = 3;
            // 
            // lLogMode
            // 
            this.lLogMode.AutoSize = true;
            this.lLogMode.Location = new Point( 35, 363 );
            this.lLogMode.Name = "lLogMode";
            this.lLogMode.Size = new Size( 63, 15 );
            this.lLogMode.TabIndex = 2;
            this.lLogMode.Text = "Log mode";
            // 
            // gConsole
            // 
            this.gConsole.Controls.Add( this.lLogConsoleOptionsDescription );
            this.gConsole.Controls.Add( this.vConsoleOptions );
            this.gConsole.Location = new Point( 8, 13 );
            this.gConsole.Name = "gConsole";
            this.gConsole.Size = new Size( 315, 423 );
            this.gConsole.TabIndex = 0;
            this.gConsole.TabStop = false;
            this.gConsole.Text = "Console";
            // 
            // lLogConsoleOptionsDescription
            // 
            this.lLogConsoleOptionsDescription.AutoSize = true;
            this.lLogConsoleOptionsDescription.Location = new Point( 9, 21 );
            this.lLogConsoleOptionsDescription.Name = "lLogConsoleOptionsDescription";
            this.lLogConsoleOptionsDescription.Size = new Size( 212, 30 );
            this.lLogConsoleOptionsDescription.TabIndex = 0;
            this.lLogConsoleOptionsDescription.Text = "Types of messages that will be written\r\ndirectly to console.";
            // 
            // vConsoleOptions
            // 
            this.vConsoleOptions.CheckBoxes = true;
            this.vConsoleOptions.Columns.AddRange( new ColumnHeader[] {
            this.columnHeader3} );
            this.vConsoleOptions.GridLines = true;
            this.vConsoleOptions.HeaderStyle = ColumnHeaderStyle.None;
            this.vConsoleOptions.Location = new Point( 76, 59 );
            this.vConsoleOptions.Name = "vConsoleOptions";
            this.vConsoleOptions.ShowItemToolTips = true;
            this.vConsoleOptions.Size = new Size( 161, 294 );
            this.vConsoleOptions.TabIndex = 1;
            this.vConsoleOptions.UseCompatibleStateImageBehavior = false;
            this.vConsoleOptions.View = View.Details;
            this.vConsoleOptions.ItemChecked += new ItemCheckedEventHandler( this.vConsoleOptions_ItemChecked );
            // 
            // columnHeader3
            // 
            this.columnHeader3.Width = 157;
            // 
            // tabIRC
            // 
            this.tabIRC.Controls.Add( this.gIRCColors );
            this.tabIRC.Controls.Add( this.xIRCListShowNonEnglish );
            this.tabIRC.Controls.Add( this.gIRCOptions );
            this.tabIRC.Controls.Add( this.gIRCNetwork );
            this.tabIRC.Controls.Add( this.lIRCList );
            this.tabIRC.Controls.Add( this.xIRCBotEnabled );
            this.tabIRC.Controls.Add( this.cIRCList );
            this.tabIRC.Location = new Point( 4, 24 );
            this.tabIRC.Name = "tabIRC";
            this.tabIRC.Padding = new Padding( 5, 10, 5, 10 );
            this.tabIRC.Size = new Size( 652, 482 );
            this.tabIRC.TabIndex = 8;
            this.tabIRC.Text = "IRC";
            this.tabIRC.UseVisualStyleBackColor = true;
            // 
            // gIRCColors
            // 
            this.gIRCColors.Controls.Add( this.xIRCStripMinecraftColors );
            this.gIRCColors.Controls.Add( this.bColorIRC );
            this.gIRCColors.Controls.Add( this.xIRCUseColor );
            this.gIRCColors.Controls.Add( this.lColorIRC );
            this.gIRCColors.Location = new Point( 8, 206 );
            this.gIRCColors.Name = "gIRCColors";
            this.gIRCColors.Size = new Size( 635, 77 );
            this.gIRCColors.TabIndex = 6;
            this.gIRCColors.TabStop = false;
            this.gIRCColors.Text = "Colors";
            // 
            // xIRCStripMinecraftColors
            // 
            this.xIRCStripMinecraftColors.AutoSize = true;
            this.xIRCStripMinecraftColors.Location = new Point( 38, 49 );
            this.xIRCStripMinecraftColors.Name = "xIRCStripMinecraftColors";
            this.xIRCStripMinecraftColors.Size = new Size( 395, 19 );
            this.xIRCStripMinecraftColors.TabIndex = 3;
            this.xIRCStripMinecraftColors.Text = "Strip Minecraft color codes (&&-codes) from incoming IRC messages.";
            this.xIRCStripMinecraftColors.UseVisualStyleBackColor = true;
            // 
            // bColorIRC
            // 
            this.bColorIRC.BackColor = System.Drawing.Color.White;
            this.bColorIRC.Location = new Point( 159, 20 );
            this.bColorIRC.Name = "bColorIRC";
            this.bColorIRC.Size = new Size( 100, 23 );
            this.bColorIRC.TabIndex = 1;
            this.bColorIRC.UseVisualStyleBackColor = false;
            this.bColorIRC.Click += new EventHandler( this.bColorIRC_Click );
            // 
            // xIRCUseColor
            // 
            this.xIRCUseColor.AutoSize = true;
            this.xIRCUseColor.Location = new Point( 325, 20 );
            this.xIRCUseColor.Name = "xIRCUseColor";
            this.xIRCUseColor.Size = new Size( 304, 19 );
            this.xIRCUseColor.TabIndex = 2;
            this.xIRCUseColor.Text = "Use colors in bot\'s IRC messages and notifications.";
            this.xIRCUseColor.UseVisualStyleBackColor = true;
            // 
            // lColorIRC
            // 
            this.lColorIRC.AutoSize = true;
            this.lColorIRC.Location = new Point( 42, 24 );
            this.lColorIRC.Name = "lColorIRC";
            this.lColorIRC.Size = new Size( 111, 15 );
            this.lColorIRC.TabIndex = 0;
            this.lColorIRC.Text = "IRC message color";
            // 
            // xIRCListShowNonEnglish
            // 
            this.xIRCListShowNonEnglish.AutoSize = true;
            this.xIRCListShowNonEnglish.Enabled = false;
            this.xIRCListShowNonEnglish.Location = new Point( 465, 13 );
            this.xIRCListShowNonEnglish.Name = "xIRCListShowNonEnglish";
            this.xIRCListShowNonEnglish.Size = new Size( 178, 19 );
            this.xIRCListShowNonEnglish.TabIndex = 3;
            this.xIRCListShowNonEnglish.Text = "Show non-English networks";
            this.xIRCListShowNonEnglish.UseVisualStyleBackColor = true;
            this.xIRCListShowNonEnglish.CheckedChanged += new EventHandler( this.xIRCListShowNonEnglish_CheckedChanged );
            // 
            // gIRCOptions
            // 
            this.gIRCOptions.Controls.Add( this.xIRCBotAnnounceServerEvents );
            this.gIRCOptions.Controls.Add( this.lIRCNoForwardingMessage );
            this.gIRCOptions.Controls.Add( this.xIRCBotAnnounceIRCJoins );
            this.gIRCOptions.Controls.Add( this.xIRCBotForwardFromIRC );
            this.gIRCOptions.Controls.Add( this.xIRCBotAnnounceServerJoins );
            this.gIRCOptions.Controls.Add( this.xIRCBotForwardFromServer );
            this.gIRCOptions.Location = new Point( 8, 289 );
            this.gIRCOptions.Name = "gIRCOptions";
            this.gIRCOptions.Size = new Size( 636, 136 );
            this.gIRCOptions.TabIndex = 5;
            this.gIRCOptions.TabStop = false;
            this.gIRCOptions.Text = "Options";
            // 
            // xIRCBotAnnounceServerEvents
            // 
            this.xIRCBotAnnounceServerEvents.AutoSize = true;
            this.xIRCBotAnnounceServerEvents.Location = new Point( 38, 70 );
            this.xIRCBotAnnounceServerEvents.Name = "xIRCBotAnnounceServerEvents";
            this.xIRCBotAnnounceServerEvents.Size = new Size( 417, 19 );
            this.xIRCBotAnnounceServerEvents.TabIndex = 7;
            this.xIRCBotAnnounceServerEvents.Text = "Announce SERVER events (kicks, bans, promotions, demotions) on IRC.";
            this.xIRCBotAnnounceServerEvents.UseVisualStyleBackColor = true;
            // 
            // lIRCNoForwardingMessage
            // 
            this.lIRCNoForwardingMessage.AutoSize = true;
            this.lIRCNoForwardingMessage.Location = new Point( 35, 108 );
            this.lIRCNoForwardingMessage.Name = "lIRCNoForwardingMessage";
            this.lIRCNoForwardingMessage.Size = new Size( 567, 15 );
            this.lIRCNoForwardingMessage.TabIndex = 8;
            this.lIRCNoForwardingMessage.Text = "NOTE: If forwarding all messages is not enabled, only messages starting with a ha" +
                "sh (#) will be relayed.";
            // 
            // xIRCBotAnnounceIRCJoins
            // 
            this.xIRCBotAnnounceIRCJoins.AutoSize = true;
            this.xIRCBotAnnounceIRCJoins.Location = new Point( 325, 45 );
            this.xIRCBotAnnounceIRCJoins.Name = "xIRCBotAnnounceIRCJoins";
            this.xIRCBotAnnounceIRCJoins.Size = new Size( 303, 19 );
            this.xIRCBotAnnounceIRCJoins.TabIndex = 6;
            this.xIRCBotAnnounceIRCJoins.Text = "Announce people joining/leaving the IRC channels.";
            this.xIRCBotAnnounceIRCJoins.UseVisualStyleBackColor = true;
            // 
            // xIRCBotForwardFromIRC
            // 
            this.xIRCBotForwardFromIRC.AutoSize = true;
            this.xIRCBotForwardFromIRC.Location = new Point( 38, 45 );
            this.xIRCBotForwardFromIRC.Name = "xIRCBotForwardFromIRC";
            this.xIRCBotForwardFromIRC.Size = new Size( 240, 19 );
            this.xIRCBotForwardFromIRC.TabIndex = 4;
            this.xIRCBotForwardFromIRC.Text = "Forward ALL chat from IRC to SERVER.";
            this.xIRCBotForwardFromIRC.UseVisualStyleBackColor = true;
            // 
            // xIRCBotAnnounceServerJoins
            // 
            this.xIRCBotAnnounceServerJoins.AutoSize = true;
            this.xIRCBotAnnounceServerJoins.Location = new Point( 325, 20 );
            this.xIRCBotAnnounceServerJoins.Name = "xIRCBotAnnounceServerJoins";
            this.xIRCBotAnnounceServerJoins.Size = new Size( 279, 19 );
            this.xIRCBotAnnounceServerJoins.TabIndex = 5;
            this.xIRCBotAnnounceServerJoins.Text = "Announce people joining/leaving the SERVER.";
            this.xIRCBotAnnounceServerJoins.UseVisualStyleBackColor = true;
            // 
            // xIRCBotForwardFromServer
            // 
            this.xIRCBotForwardFromServer.AutoSize = true;
            this.xIRCBotForwardFromServer.Location = new Point( 38, 20 );
            this.xIRCBotForwardFromServer.Name = "xIRCBotForwardFromServer";
            this.xIRCBotForwardFromServer.Size = new Size( 240, 19 );
            this.xIRCBotForwardFromServer.TabIndex = 3;
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
            this.gIRCNetwork.Location = new Point( 8, 40 );
            this.gIRCNetwork.Name = "gIRCNetwork";
            this.gIRCNetwork.Size = new Size( 636, 160 );
            this.gIRCNetwork.TabIndex = 4;
            this.gIRCNetwork.TabStop = false;
            this.gIRCNetwork.Text = "Network";
            // 
            // lIRCDelayUnits
            // 
            this.lIRCDelayUnits.AutoSize = true;
            this.lIRCDelayUnits.Location = new Point( 598, 22 );
            this.lIRCDelayUnits.Name = "lIRCDelayUnits";
            this.lIRCDelayUnits.Size = new Size( 24, 15 );
            this.lIRCDelayUnits.TabIndex = 6;
            this.lIRCDelayUnits.Text = "ms";
            // 
            // xIRCRegisteredNick
            // 
            this.xIRCRegisteredNick.AutoSize = true;
            this.xIRCRegisteredNick.Location = new Point( 265, 101 );
            this.xIRCRegisteredNick.Name = "xIRCRegisteredNick";
            this.xIRCRegisteredNick.Size = new Size( 86, 19 );
            this.xIRCRegisteredNick.TabIndex = 13;
            this.xIRCRegisteredNick.Text = "Registered";
            this.xIRCRegisteredNick.UseVisualStyleBackColor = true;
            this.xIRCRegisteredNick.CheckedChanged += new EventHandler( this.xIRCRegisteredNick_CheckedChanged );
            // 
            // tIRCNickServMessage
            // 
            this.tIRCNickServMessage.Enabled = false;
            this.tIRCNickServMessage.Location = new Point( 388, 126 );
            this.tIRCNickServMessage.Name = "tIRCNickServMessage";
            this.tIRCNickServMessage.Size = new Size( 234, 21 );
            this.tIRCNickServMessage.TabIndex = 17;
            // 
            // lIRCNickServMessage
            // 
            this.lIRCNickServMessage.AutoSize = true;
            this.lIRCNickServMessage.Enabled = false;
            this.lIRCNickServMessage.Location = new Point( 265, 129 );
            this.lIRCNickServMessage.Name = "lIRCNickServMessage";
            this.lIRCNickServMessage.Size = new Size( 117, 15 );
            this.lIRCNickServMessage.TabIndex = 16;
            this.lIRCNickServMessage.Text = "Authentication string";
            // 
            // tIRCNickServ
            // 
            this.tIRCNickServ.Enabled = false;
            this.tIRCNickServ.Location = new Point( 121, 126 );
            this.tIRCNickServ.MaxLength = 32;
            this.tIRCNickServ.Name = "tIRCNickServ";
            this.tIRCNickServ.Size = new Size( 138, 21 );
            this.tIRCNickServ.TabIndex = 15;
            // 
            // lIRCNickServ
            // 
            this.lIRCNickServ.AutoSize = true;
            this.lIRCNickServ.Enabled = false;
            this.lIRCNickServ.Location = new Point( 35, 129 );
            this.lIRCNickServ.Name = "lIRCNickServ";
            this.lIRCNickServ.Size = new Size( 80, 15 );
            this.lIRCNickServ.TabIndex = 14;
            this.lIRCNickServ.Text = "NickServ nick";
            // 
            // nIRCDelay
            // 
            this.nIRCDelay.Increment = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nIRCDelay.Location = new Point( 536, 20 );
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
            this.nIRCDelay.Size = new Size( 56, 21 );
            this.nIRCDelay.TabIndex = 5;
            this.nIRCDelay.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lIRCDelay
            // 
            this.lIRCDelay.AutoSize = true;
            this.lIRCDelay.Location = new Point( 416, 22 );
            this.lIRCDelay.Name = "lIRCDelay";
            this.lIRCDelay.Size = new Size( 114, 15 );
            this.lIRCDelay.TabIndex = 4;
            this.lIRCDelay.Text = "Min message delay";
            // 
            // lIRCBotChannels2
            // 
            this.lIRCBotChannels2.AutoSize = true;
            this.lIRCBotChannels2.Font = new Font( "Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.lIRCBotChannels2.Location = new Point( 15, 65 );
            this.lIRCBotChannels2.Name = "lIRCBotChannels2";
            this.lIRCBotChannels2.Size = new Size( 97, 13 );
            this.lIRCBotChannels2.TabIndex = 9;
            this.lIRCBotChannels2.Text = "(comma seperated)";
            // 
            // lIRCBotChannels3
            // 
            this.lIRCBotChannels3.AutoSize = true;
            this.lIRCBotChannels3.Location = new Point( 118, 71 );
            this.lIRCBotChannels3.Name = "lIRCBotChannels3";
            this.lIRCBotChannels3.Size = new Size( 340, 15 );
            this.lIRCBotChannels3.TabIndex = 10;
            this.lIRCBotChannels3.Text = "NOTE: Channel names are case-sensitive on some networks!";
            // 
            // tIRCBotChannels
            // 
            this.tIRCBotChannels.Location = new Point( 121, 47 );
            this.tIRCBotChannels.MaxLength = 1000;
            this.tIRCBotChannels.Name = "tIRCBotChannels";
            this.tIRCBotChannels.Size = new Size( 501, 21 );
            this.tIRCBotChannels.TabIndex = 8;
            // 
            // lIRCBotChannels
            // 
            this.lIRCBotChannels.AutoSize = true;
            this.lIRCBotChannels.Location = new Point( 20, 50 );
            this.lIRCBotChannels.Name = "lIRCBotChannels";
            this.lIRCBotChannels.Size = new Size( 95, 15 );
            this.lIRCBotChannels.TabIndex = 7;
            this.lIRCBotChannels.Text = "Channels to join";
            // 
            // nIRCBotPort
            // 
            this.nIRCBotPort.Location = new Point( 300, 20 );
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
            this.nIRCBotPort.Size = new Size( 64, 21 );
            this.nIRCBotPort.TabIndex = 3;
            this.nIRCBotPort.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lIRCBotPort
            // 
            this.lIRCBotPort.AutoSize = true;
            this.lIRCBotPort.Location = new Point( 265, 22 );
            this.lIRCBotPort.Name = "lIRCBotPort";
            this.lIRCBotPort.Size = new Size( 29, 15 );
            this.lIRCBotPort.TabIndex = 2;
            this.lIRCBotPort.Text = "Port";
            // 
            // tIRCBotNetwork
            // 
            this.tIRCBotNetwork.Location = new Point( 121, 19 );
            this.tIRCBotNetwork.MaxLength = 512;
            this.tIRCBotNetwork.Name = "tIRCBotNetwork";
            this.tIRCBotNetwork.Size = new Size( 138, 21 );
            this.tIRCBotNetwork.TabIndex = 1;
            // 
            // lIRCBotNetwork
            // 
            this.lIRCBotNetwork.AutoSize = true;
            this.lIRCBotNetwork.Location = new Point( 26, 22 );
            this.lIRCBotNetwork.Name = "lIRCBotNetwork";
            this.lIRCBotNetwork.Size = new Size( 89, 15 );
            this.lIRCBotNetwork.TabIndex = 0;
            this.lIRCBotNetwork.Text = "IRC server host";
            // 
            // lIRCBotNick
            // 
            this.lIRCBotNick.AutoSize = true;
            this.lIRCBotNick.Location = new Point( 65, 102 );
            this.lIRCBotNick.Name = "lIRCBotNick";
            this.lIRCBotNick.Size = new Size( 50, 15 );
            this.lIRCBotNick.TabIndex = 11;
            this.lIRCBotNick.Text = "Bot nick";
            // 
            // tIRCBotNick
            // 
            this.tIRCBotNick.Location = new Point( 121, 99 );
            this.tIRCBotNick.MaxLength = 32;
            this.tIRCBotNick.Name = "tIRCBotNick";
            this.tIRCBotNick.Size = new Size( 138, 21 );
            this.tIRCBotNick.TabIndex = 12;
            // 
            // lIRCList
            // 
            this.lIRCList.AutoSize = true;
            this.lIRCList.Enabled = false;
            this.lIRCList.Location = new Point( 213, 14 );
            this.lIRCList.Name = "lIRCList";
            this.lIRCList.Size = new Size( 105, 15 );
            this.lIRCList.TabIndex = 1;
            this.lIRCList.Text = "Popular networks:";
            // 
            // xIRCBotEnabled
            // 
            this.xIRCBotEnabled.AutoSize = true;
            this.xIRCBotEnabled.Location = new Point( 14, 13 );
            this.xIRCBotEnabled.Name = "xIRCBotEnabled";
            this.xIRCBotEnabled.Size = new Size( 149, 19 );
            this.xIRCBotEnabled.TabIndex = 0;
            this.xIRCBotEnabled.Text = "Enable IRC integration";
            this.xIRCBotEnabled.UseVisualStyleBackColor = true;
            this.xIRCBotEnabled.CheckedChanged += new EventHandler( this.xIRC_CheckedChanged );
            // 
            // cIRCList
            // 
            this.cIRCList.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cIRCList.Enabled = false;
            this.cIRCList.FormattingEnabled = true;
            this.cIRCList.Location = new Point( 321, 11 );
            this.cIRCList.Name = "cIRCList";
            this.cIRCList.Size = new Size( 138, 23 );
            this.cIRCList.TabIndex = 2;
            this.cIRCList.SelectedIndexChanged += new EventHandler( this.cIRCList_SelectedIndexChanged );
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.Controls.Add( this.gCrashReport );
            this.tabAdvanced.Controls.Add( this.gPerformance );
            this.tabAdvanced.Controls.Add( this.gAdvancedMisc );
            this.tabAdvanced.Location = new Point( 4, 24 );
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Padding = new Padding( 5, 10, 5, 10 );
            this.tabAdvanced.Size = new Size( 652, 482 );
            this.tabAdvanced.TabIndex = 6;
            this.tabAdvanced.Text = "Advanced";
            this.tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // gCrashReport
            // 
            this.gCrashReport.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Top | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.gCrashReport.Controls.Add( this.lCrashReportDisclaimer );
            this.gCrashReport.Controls.Add( this.xSubmitCrashReports );
            this.gCrashReport.Location = new Point( 8, 13 );
            this.gCrashReport.Name = "gCrashReport";
            this.gCrashReport.Size = new Size( 636, 99 );
            this.gCrashReport.TabIndex = 6;
            this.gCrashReport.TabStop = false;
            this.gCrashReport.Text = "Crash Reporting";
            // 
            // lCrashReportDisclaimer
            // 
            this.lCrashReportDisclaimer.AutoSize = true;
            this.lCrashReportDisclaimer.Font = new Font( "Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.lCrashReportDisclaimer.Location = new Point( 42, 42 );
            this.lCrashReportDisclaimer.Name = "lCrashReportDisclaimer";
            this.lCrashReportDisclaimer.Size = new Size( 521, 39 );
            this.lCrashReportDisclaimer.TabIndex = 1;
            this.lCrashReportDisclaimer.Text = resources.GetString( "lCrashReportDisclaimer.Text" );
            // 
            // xSubmitCrashReports
            // 
            this.xSubmitCrashReports.AutoSize = true;
            this.xSubmitCrashReports.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.xSubmitCrashReports.Location = new Point( 6, 20 );
            this.xSubmitCrashReports.Name = "xSubmitCrashReports";
            this.xSubmitCrashReports.Size = new Size( 446, 19 );
            this.xSubmitCrashReports.TabIndex = 0;
            this.xSubmitCrashReports.Text = "Automatically submit crash reports to fCraft developers (fCraft.net)";
            this.xSubmitCrashReports.UseVisualStyleBackColor = true;
            // 
            // gPerformance
            // 
            this.gPerformance.Anchor = ( (AnchorStyles)( ( ( ( AnchorStyles.Top | AnchorStyles.Bottom )
                        | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.gPerformance.Controls.Add( this.lAdvancedWarning );
            this.gPerformance.Controls.Add( this.xLowLatencyMode );
            this.gPerformance.Controls.Add( this.lProcessPriority );
            this.gPerformance.Controls.Add( this.cProcessPriority );
            this.gPerformance.Controls.Add( this.nTickInterval );
            this.gPerformance.Controls.Add( this.lTickIntervalUnits );
            this.gPerformance.Controls.Add( this.lTickInterval );
            this.gPerformance.Controls.Add( this.nThrottling );
            this.gPerformance.Controls.Add( this.lThrottling );
            this.gPerformance.Controls.Add( this.lThrottlingUnits );
            this.gPerformance.Location = new Point( 8, 304 );
            this.gPerformance.Name = "gPerformance";
            this.gPerformance.Size = new Size( 636, 151 );
            this.gPerformance.TabIndex = 2;
            this.gPerformance.TabStop = false;
            this.gPerformance.Text = "Performance";
            // 
            // lAdvancedWarning
            // 
            this.lAdvancedWarning.AutoSize = true;
            this.lAdvancedWarning.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.lAdvancedWarning.Location = new Point( 15, 21 );
            this.lAdvancedWarning.Name = "lAdvancedWarning";
            this.lAdvancedWarning.Size = new Size( 558, 30 );
            this.lAdvancedWarning.TabIndex = 0;
            this.lAdvancedWarning.Text = "Warning: Altering these settings may decrease your server\'s stability and perform" +
                "ance.\r\nIf you\'re not sure what these settings do, you probably shouldn\'t change " +
                "them...";
            // 
            // xLowLatencyMode
            // 
            this.xLowLatencyMode.AutoSize = true;
            this.xLowLatencyMode.Location = new Point( 6, 64 );
            this.xLowLatencyMode.Name = "xLowLatencyMode";
            this.xLowLatencyMode.Size = new Size( 544, 19 );
            this.xLowLatencyMode.TabIndex = 3;
            this.xLowLatencyMode.Text = "Low-latency mode (disables Nagle\'s algorithm, reducing latency but increasing ban" +
                "dwidth use).";
            this.xLowLatencyMode.UseVisualStyleBackColor = true;
            // 
            // lProcessPriority
            // 
            this.lProcessPriority.AutoSize = true;
            this.lProcessPriority.Location = new Point( 19, 94 );
            this.lProcessPriority.Name = "lProcessPriority";
            this.lProcessPriority.Size = new Size( 90, 15 );
            this.lProcessPriority.TabIndex = 10;
            this.lProcessPriority.Text = "Process priority";
            // 
            // cProcessPriority
            // 
            this.cProcessPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cProcessPriority.Items.AddRange( new object[] {
            "(system default)",
            "High",
            "Above Normal",
            "Normal",
            "Below Normal",
            "Low"} );
            this.cProcessPriority.Location = new Point( 115, 91 );
            this.cProcessPriority.Name = "cProcessPriority";
            this.cProcessPriority.Size = new Size( 109, 23 );
            this.cProcessPriority.TabIndex = 11;
            // 
            // nTickInterval
            // 
            this.nTickInterval.Increment = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nTickInterval.Location = new Point( 429, 92 );
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
            this.nTickInterval.Size = new Size( 70, 21 );
            this.nTickInterval.TabIndex = 13;
            this.nTickInterval.Value = new decimal( new int[] {
            100,
            0,
            0,
            0} );
            // 
            // lTickIntervalUnits
            // 
            this.lTickIntervalUnits.AutoSize = true;
            this.lTickIntervalUnits.Location = new Point( 505, 94 );
            this.lTickIntervalUnits.Name = "lTickIntervalUnits";
            this.lTickIntervalUnits.Size = new Size( 24, 15 );
            this.lTickIntervalUnits.TabIndex = 14;
            this.lTickIntervalUnits.Text = "ms";
            // 
            // lTickInterval
            // 
            this.lTickInterval.AutoSize = true;
            this.lTickInterval.Location = new Point( 352, 94 );
            this.lTickInterval.Name = "lTickInterval";
            this.lTickInterval.Size = new Size( 71, 15 );
            this.lTickInterval.TabIndex = 12;
            this.lTickInterval.Text = "Tick interval";
            // 
            // nThrottling
            // 
            this.nThrottling.Increment = new decimal( new int[] {
            100,
            0,
            0,
            0} );
            this.nThrottling.Location = new Point( 115, 120 );
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
            this.nThrottling.Size = new Size( 70, 21 );
            this.nThrottling.TabIndex = 16;
            this.nThrottling.Value = new decimal( new int[] {
            2048,
            0,
            0,
            0} );
            // 
            // lThrottling
            // 
            this.lThrottling.AutoSize = true;
            this.lThrottling.Location = new Point( 22, 122 );
            this.lThrottling.Name = "lThrottling";
            this.lThrottling.Size = new Size( 87, 15 );
            this.lThrottling.TabIndex = 15;
            this.lThrottling.Text = "Block throttling";
            // 
            // lThrottlingUnits
            // 
            this.lThrottlingUnits.AutoSize = true;
            this.lThrottlingUnits.Location = new Point( 191, 122 );
            this.lThrottlingUnits.Name = "lThrottlingUnits";
            this.lThrottlingUnits.Size = new Size( 129, 15 );
            this.lThrottlingUnits.TabIndex = 17;
            this.lThrottlingUnits.Text = "blocks / second / client";
            // 
            // gAdvancedMisc
            // 
            this.gAdvancedMisc.Anchor = ( (AnchorStyles)( ( ( AnchorStyles.Top | AnchorStyles.Left )
                        | AnchorStyles.Right ) ) );
            this.gAdvancedMisc.Controls.Add( this.nMaxUndoStates );
            this.gAdvancedMisc.Controls.Add( this.lMaxUndoStates );
            this.gAdvancedMisc.Controls.Add( this.lIPWarning );
            this.gAdvancedMisc.Controls.Add( this.tIP );
            this.gAdvancedMisc.Controls.Add( this.xIP );
            this.gAdvancedMisc.Controls.Add( this.lConsoleName );
            this.gAdvancedMisc.Controls.Add( this.tConsoleName );
            this.gAdvancedMisc.Controls.Add( this.nMaxUndo );
            this.gAdvancedMisc.Controls.Add( this.lMaxUndoUnits );
            this.gAdvancedMisc.Controls.Add( this.xMaxUndo );
            this.gAdvancedMisc.Controls.Add( this.xRelayAllBlockUpdates );
            this.gAdvancedMisc.Controls.Add( this.xNoPartialPositionUpdates );
            this.gAdvancedMisc.Location = new Point( 8, 118 );
            this.gAdvancedMisc.Name = "gAdvancedMisc";
            this.gAdvancedMisc.Size = new Size( 636, 180 );
            this.gAdvancedMisc.TabIndex = 1;
            this.gAdvancedMisc.TabStop = false;
            this.gAdvancedMisc.Text = "Miscellaneous";
            // 
            // nMaxUndoStates
            // 
            this.nMaxUndoStates.Location = new Point( 115, 71 );
            this.nMaxUndoStates.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nMaxUndoStates.Name = "nMaxUndoStates";
            this.nMaxUndoStates.Size = new Size( 58, 21 );
            this.nMaxUndoStates.TabIndex = 23;
            this.nMaxUndoStates.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nMaxUndoStates.ValueChanged += new EventHandler( this.nMaxUndo_ValueChanged );
            // 
            // lMaxUndoStates
            // 
            this.lMaxUndoStates.AutoSize = true;
            this.lMaxUndoStates.Location = new Point( 179, 73 );
            this.lMaxUndoStates.Name = "lMaxUndoStates";
            this.lMaxUndoStates.Size = new Size( 72, 15 );
            this.lMaxUndoStates.TabIndex = 22;
            this.lMaxUndoStates.Text = "states, up to";
            // 
            // lIPWarning
            // 
            this.lIPWarning.AutoSize = true;
            this.lIPWarning.Font = new Font( "Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.lIPWarning.Location = new Point( 112, 151 );
            this.lIPWarning.Name = "lIPWarning";
            this.lIPWarning.Size = new Size( 408, 13 );
            this.lIPWarning.TabIndex = 20;
            this.lIPWarning.Text = "Note: You do not need to specify an IP address unless you have multiple NICs or I" +
                "Ps.";
            // 
            // tIP
            // 
            this.tIP.Location = new Point( 115, 127 );
            this.tIP.MaxLength = 15;
            this.tIP.Name = "tIP";
            this.tIP.Size = new Size( 97, 21 );
            this.tIP.TabIndex = 19;
            this.tIP.Validating += new CancelEventHandler( this.tIP_Validating );
            // 
            // xIP
            // 
            this.xIP.AutoSize = true;
            this.xIP.Location = new Point( 6, 129 );
            this.xIP.Name = "xIP";
            this.xIP.Size = new Size( 103, 19 );
            this.xIP.TabIndex = 18;
            this.xIP.Text = "Designated IP";
            this.xIP.UseVisualStyleBackColor = true;
            this.xIP.CheckedChanged += new EventHandler( this.xIP_CheckedChanged );
            // 
            // lConsoleName
            // 
            this.lConsoleName.AutoSize = true;
            this.lConsoleName.Location = new Point( 22, 103 );
            this.lConsoleName.Name = "lConsoleName";
            this.lConsoleName.Size = new Size( 87, 15 );
            this.lConsoleName.TabIndex = 7;
            this.lConsoleName.Text = "Console name";
            // 
            // tConsoleName
            // 
            this.tConsoleName.Location = new Point( 115, 100 );
            this.tConsoleName.Name = "tConsoleName";
            this.tConsoleName.Size = new Size( 167, 21 );
            this.tConsoleName.TabIndex = 8;
            // 
            // nMaxUndo
            // 
            this.nMaxUndo.Increment = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nMaxUndo.Location = new Point( 257, 71 );
            this.nMaxUndo.Maximum = new decimal( new int[] {
            2147483647,
            0,
            0,
            0} );
            this.nMaxUndo.Name = "nMaxUndo";
            this.nMaxUndo.Size = new Size( 86, 21 );
            this.nMaxUndo.TabIndex = 5;
            this.nMaxUndo.Value = new decimal( new int[] {
            2000000,
            0,
            0,
            0} );
            this.nMaxUndo.ValueChanged += new EventHandler( this.nMaxUndo_ValueChanged );
            // 
            // lMaxUndoUnits
            // 
            this.lMaxUndoUnits.AutoSize = true;
            this.lMaxUndoUnits.Location = new Point( 349, 73 );
            this.lMaxUndoUnits.Name = "lMaxUndoUnits";
            this.lMaxUndoUnits.Size = new Size( 259, 15 );
            this.lMaxUndoUnits.TabIndex = 6;
            this.lMaxUndoUnits.Text = "blocks each (up to 16.0 MB of RAM per player)";
            // 
            // xMaxUndo
            // 
            this.xMaxUndo.AutoSize = true;
            this.xMaxUndo.Checked = true;
            this.xMaxUndo.CheckState = CheckState.Checked;
            this.xMaxUndo.Location = new Point( 6, 72 );
            this.xMaxUndo.Name = "xMaxUndo";
            this.xMaxUndo.Size = new Size( 100, 19 );
            this.xMaxUndo.TabIndex = 4;
            this.xMaxUndo.Text = "Limit /undo to";
            this.xMaxUndo.UseVisualStyleBackColor = true;
            this.xMaxUndo.CheckedChanged += new EventHandler( this.xMaxUndo_CheckedChanged );
            // 
            // xRelayAllBlockUpdates
            // 
            this.xRelayAllBlockUpdates.AutoSize = true;
            this.xRelayAllBlockUpdates.Location = new Point( 6, 21 );
            this.xRelayAllBlockUpdates.Name = "xRelayAllBlockUpdates";
            this.xRelayAllBlockUpdates.Size = new Size( 560, 19 );
            this.xRelayAllBlockUpdates.TabIndex = 1;
            this.xRelayAllBlockUpdates.Text = "When a player changes a block, send him the redundant update packet anyway (origi" +
                "nal behavior).";
            this.xRelayAllBlockUpdates.UseVisualStyleBackColor = true;
            // 
            // xNoPartialPositionUpdates
            // 
            this.xNoPartialPositionUpdates.AutoSize = true;
            this.xNoPartialPositionUpdates.Location = new Point( 6, 46 );
            this.xNoPartialPositionUpdates.Name = "xNoPartialPositionUpdates";
            this.xNoPartialPositionUpdates.Size = new Size( 326, 19 );
            this.xNoPartialPositionUpdates.TabIndex = 2;
            this.xNoPartialPositionUpdates.Text = "Do not use partial position updates (opcodes 9, 10, 11).";
            this.xNoPartialPositionUpdates.UseVisualStyleBackColor = true;
            // 
            // bOK
            // 
            this.bOK.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Right ) ) );
            this.bOK.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bOK.Location = new Point( 360, 528 );
            this.bOK.Name = "bOK";
            this.bOK.Size = new Size( 100, 28 );
            this.bOK.TabIndex = 1;
            this.bOK.Text = "OK";
            this.bOK.Click += new EventHandler( this.bSave_Click );
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Right ) ) );
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bCancel.Location = new Point( 466, 528 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new Size( 100, 28 );
            this.bCancel.TabIndex = 2;
            this.bCancel.Text = "Cancel";
            this.bCancel.Click += new EventHandler( this.bCancel_Click );
            // 
            // bResetTab
            // 
            this.bResetTab.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.bResetTab.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bResetTab.Location = new Point( 132, 528 );
            this.bResetTab.Name = "bResetTab";
            this.bResetTab.Size = new Size( 100, 28 );
            this.bResetTab.TabIndex = 5;
            this.bResetTab.Text = "Reset Tab";
            this.bResetTab.UseVisualStyleBackColor = true;
            this.bResetTab.Click += new EventHandler( this.bResetTab_Click );
            // 
            // bResetAll
            // 
            this.bResetAll.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Left ) ) );
            this.bResetAll.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bResetAll.Location = new Point( 12, 528 );
            this.bResetAll.Name = "bResetAll";
            this.bResetAll.Size = new Size( 114, 28 );
            this.bResetAll.TabIndex = 4;
            this.bResetAll.Text = "Reset All Defaults";
            this.bResetAll.UseVisualStyleBackColor = true;
            this.bResetAll.Click += new EventHandler( this.bResetAll_Click );
            // 
            // bApply
            // 
            this.bApply.Anchor = ( (AnchorStyles)( ( AnchorStyles.Bottom | AnchorStyles.Right ) ) );
            this.bApply.Font = new Font( "Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ( (byte)( 0 ) ) );
            this.bApply.Location = new Point( 572, 528 );
            this.bApply.Name = "bApply";
            this.bApply.Size = new Size( 100, 28 );
            this.bApply.TabIndex = 3;
            this.bApply.Text = "Apply";
            this.bApply.Click += new EventHandler( this.bApply_Click );
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 11111;
            this.toolTip.InitialDelay = 500;
            this.toolTip.IsBalloon = true;
            this.toolTip.ReshowDelay = 100;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size( 684, 568 );
            this.Controls.Add( this.bApply );
            this.Controls.Add( this.bResetAll );
            this.Controls.Add( this.bResetTab );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.tabs );
            this.Icon = ( (Icon)( resources.GetObject( "$this.Icon" ) ) );
            this.MinimumSize = new Size( 700, 547 );
            this.Name = "MainForm";
            this.Text = "fCraft Config Tool";
            this.FormClosing += new FormClosingEventHandler( this.ConfigUI_FormClosing );
            this.tabs.ResumeLayout( false );
            this.tabGeneral.ResumeLayout( false );
            this.gWoMDirect.ResumeLayout( false );
            this.gWoMDirect.PerformLayout();
            this.gUpdaterSettings.ResumeLayout( false );
            this.gUpdaterSettings.PerformLayout();
            this.groupBox2.ResumeLayout( false );
            this.gHelpAndSupport.ResumeLayout( false );
            this.gInformation.ResumeLayout( false );
            this.gInformation.PerformLayout();
            ( (ISupportInitialize)( this.nAnnouncements ) ).EndInit();
            this.gBasic.ResumeLayout( false );
            this.gBasic.PerformLayout();
            ( (ISupportInitialize)( this.nMaxPlayersPerWorld ) ).EndInit();
            ( (ISupportInitialize)( this.nPort ) ).EndInit();
            ( (ISupportInitialize)( this.nUploadBandwidth ) ).EndInit();
            ( (ISupportInitialize)( this.nMaxPlayers ) ).EndInit();
            this.tabChat.ResumeLayout( false );
            this.gChatColors.ResumeLayout( false );
            this.gChatColors.PerformLayout();
            this.gAppearence.ResumeLayout( false );
            this.gAppearence.PerformLayout();
            this.tabWorlds.ResumeLayout( false );
            this.tabWorlds.PerformLayout();
            ( (ISupportInitialize)( this.dgvWorlds ) ).EndInit();
            this.tabRanks.ResumeLayout( false );
            this.tabRanks.PerformLayout();
            this.gPermissionLimits.ResumeLayout( false );
            this.gRankOptions.ResumeLayout( false );
            this.gRankOptions.PerformLayout();
            ( (ISupportInitialize)( this.nFillLimit ) ).EndInit();
            ( (ISupportInitialize)( this.nCopyPasteSlots ) ).EndInit();
            ( (ISupportInitialize)( this.nAntiGriefSeconds ) ).EndInit();
            ( (ISupportInitialize)( this.nDrawLimit ) ).EndInit();
            ( (ISupportInitialize)( this.nKickIdle ) ).EndInit();
            ( (ISupportInitialize)( this.nAntiGriefBlocks ) ).EndInit();
            this.tabSecurity.ResumeLayout( false );
            this.gBlockDB.ResumeLayout( false );
            this.gBlockDB.PerformLayout();
            this.gSecurityMisc.ResumeLayout( false );
            this.gSecurityMisc.PerformLayout();
            this.gSpamChat.ResumeLayout( false );
            this.gSpamChat.PerformLayout();
            ( (ISupportInitialize)( this.nAntispamMaxWarnings ) ).EndInit();
            ( (ISupportInitialize)( this.nAntispamMuteDuration ) ).EndInit();
            ( (ISupportInitialize)( this.nAntispamInterval ) ).EndInit();
            ( (ISupportInitialize)( this.nAntispamMessageCount ) ).EndInit();
            this.gVerify.ResumeLayout( false );
            this.gVerify.PerformLayout();
            ( (ISupportInitialize)( this.nMaxConnectionsPerIP ) ).EndInit();
            this.tabSavingAndBackup.ResumeLayout( false );
            this.gDataBackup.ResumeLayout( false );
            this.gDataBackup.PerformLayout();
            this.gSaving.ResumeLayout( false );
            this.gSaving.PerformLayout();
            ( (ISupportInitialize)( this.nSaveInterval ) ).EndInit();
            this.gBackups.ResumeLayout( false );
            this.gBackups.PerformLayout();
            ( (ISupportInitialize)( this.nMaxBackupSize ) ).EndInit();
            ( (ISupportInitialize)( this.nMaxBackups ) ).EndInit();
            ( (ISupportInitialize)( this.nBackupInterval ) ).EndInit();
            this.tabLogging.ResumeLayout( false );
            this.gLogFile.ResumeLayout( false );
            this.gLogFile.PerformLayout();
            ( (ISupportInitialize)( this.nLogLimit ) ).EndInit();
            this.gConsole.ResumeLayout( false );
            this.gConsole.PerformLayout();
            this.tabIRC.ResumeLayout( false );
            this.tabIRC.PerformLayout();
            this.gIRCColors.ResumeLayout( false );
            this.gIRCColors.PerformLayout();
            this.gIRCOptions.ResumeLayout( false );
            this.gIRCOptions.PerformLayout();
            this.gIRCNetwork.ResumeLayout( false );
            this.gIRCNetwork.PerformLayout();
            ( (ISupportInitialize)( this.nIRCDelay ) ).EndInit();
            ( (ISupportInitialize)( this.nIRCBotPort ) ).EndInit();
            this.tabAdvanced.ResumeLayout( false );
            this.gCrashReport.ResumeLayout( false );
            this.gCrashReport.PerformLayout();
            this.gPerformance.ResumeLayout( false );
            this.gPerformance.PerformLayout();
            ( (ISupportInitialize)( this.nTickInterval ) ).EndInit();
            ( (ISupportInitialize)( this.nThrottling ) ).EndInit();
            this.gAdvancedMisc.ResumeLayout( false );
            this.gAdvancedMisc.PerformLayout();
            ( (ISupportInitialize)( this.nMaxUndoStates ) ).EndInit();
            ( (ISupportInitialize)( this.nMaxUndo ) ).EndInit();
            this.ResumeLayout( false );

        }

        #endregion

        private TabControl tabs;
        private Button bOK;
        private Button bCancel;
        private Button bResetTab;
        private TabPage tabGeneral;
        private TabPage tabRanks;
        private Label lServerName;
        private TextBox tServerName;
        private Label lMOTD;
        private TextBox tMOTD;
        private Label lMaxPlayers;
        private NumericUpDown nMaxPlayers;
        private TabPage tabSavingAndBackup;
        private ComboBox cPublic;
        private Label lPublic;
        private Button bMeasure;
        private Label lUploadBandwidthUnits;
        private NumericUpDown nUploadBandwidth;
        private Label lUploadBandwidth;
        private TabPage tabLogging;
        private TabPage tabAdvanced;
        private Label lTickIntervalUnits;
        private NumericUpDown nTickInterval;
        private Label lTickInterval;
        private Label lAdvancedWarning;
        private ListBox vRanks;
        private Button bAddRank;
        private Label lPermissions;
        private ListView vPermissions;
        private ColumnHeader chPermissions;
        private GroupBox gRankOptions;
        private Button bDeleteRank;
        private Label lRankColor;
        private TextBox tRankName;
        private Label lRankName;
        private TextBox tPrefix;
        private Label lPrefix;
        private Label lAntiGrief2;
        private NumericUpDown nAntiGriefBlocks;
        private CheckBox xDrawLimit;
        private Label lDrawLimitUnits;
        private GroupBox gBasic;
        private ComboBox cDefaultRank;
        private Label lDefaultRank;
        private GroupBox gSaving;
        private NumericUpDown nSaveInterval;
        private Label lSaveIntervalUnits;
        private CheckBox xSaveInterval;
        private GroupBox gBackups;
        private CheckBox xBackupOnStartup;
        private NumericUpDown nBackupInterval;
        private Label lBackupIntervalUnits;
        private CheckBox xBackupInterval;
        private CheckBox xBackupOnJoin;
        private CheckBox xRelayAllBlockUpdates;
        private ComboBox cProcessPriority;
        private Label lProcessPriority;
        private Button bResetAll;
        private GroupBox gLogFile;
        private ComboBox cLogMode;
        private Label lLogMode;
        private GroupBox gConsole;
        private ListView vLogFileOptions;
        private ColumnHeader columnHeader2;
        private Label lLogLimitUnits;
        private NumericUpDown nLogLimit;
        private ListView vConsoleOptions;
        private ColumnHeader columnHeader3;
        private CheckBox xLogLimit;
        private CheckBox xReserveSlot;
        private NumericUpDown nDrawLimit;
        private Label lKickIdleUnits;
        private NumericUpDown nKickIdle;
        private CheckBox xKickIdle;
        private CheckBox xNoPartialPositionUpdates;
        private Label lMaxBackups;
        private NumericUpDown nMaxBackups;
        private Label lMaxBackupSize;
        private NumericUpDown nMaxBackupSize;
        private CheckBox xMaxBackupSize;
        private CheckBox xMaxBackups;
        private Label lThrottlingUnits;
        private NumericUpDown nThrottling;
        private Label lThrottling;
        private Button bApply;
        private Button bColorRank;
        private TabPage tabSecurity;
        private GroupBox gVerify;
        private Label lVerifyNames;
        private ComboBox cVerifyNames;
        private GroupBox gSpamChat;
        private Label lAntispamIntervalUnits;
        private NumericUpDown nAntispamInterval;
        private Label lAntispamMessageCount;
        private NumericUpDown nAntispamMessageCount;
        private CheckBox xLowLatencyMode;
        private CheckBox xAntispamKicks;
        private Label lAntispamMuteDurationUnits;
        private NumericUpDown nAntispamMuteDuration;
        private Label lAntispamMaxWarnings;
        private NumericUpDown nAntispamMaxWarnings;
        private CheckBox xBackupOnlyWhenChanged;
        private Label lPort;
        private NumericUpDown nPort;
        private Button bRules;
        private TabPage tabIRC;
        private GroupBox gIRCNetwork;
        private CheckBox xIRCBotEnabled;
        private CheckBox xIRCBotAnnounceServerJoins;
        private Label lIRCBotChannels;
        private NumericUpDown nIRCBotPort;
        private Label lIRCBotPort;
        private TextBox tIRCBotNetwork;
        private Label lIRCBotNetwork;
        private Label lIRCBotNick;
        private TextBox tIRCBotNick;
        private CheckBox xIRCBotForwardFromServer;
        private GroupBox gIRCOptions;
        private TextBox tIRCBotChannels;
        private Label lIRCBotChannels3;
        private Label lIRCBotChannels2;
        private CheckBox xIRCBotForwardFromIRC;
        private CheckBox xMaxConnectionsPerIP;
        private TabPage tabWorlds;
        private DataGridView dgvWorlds;
        private Button bWorldDelete;
        private Button bAddWorld;
        private Button bWorldEdit;
        private ComboBox cMainWorld;
        private Label lMainWorld;
        private GroupBox gInformation;
        private CheckBox xAnnouncements;
        private Button bAnnouncements;
        private Label lAnnouncementsUnits;
        private NumericUpDown nAnnouncements;
        private Label lAntiGrief3;
        private NumericUpDown nAntiGriefSeconds;
        private CheckBox xAntiGrief;
        private Label lAntiGrief1;
        private GroupBox gSecurityMisc;
        private CheckBox xAnnounceKickAndBanReasons;
        private CheckBox xRequireRankChangeReason;
        private CheckBox xRequireBanReason;
        private CheckBox xAnnounceRankChanges;
        private Button bPortCheck;
        private Button bColorIRC;
        private Label lColorIRC;
        private CheckBox xIRCBotAnnounceIRCJoins;
        private GroupBox gAdvancedMisc;
        private Label lIRCDelay;
        private NumericUpDown nIRCDelay;
        private ComboBox cPatrolledRank;
        private Label lPatrolledRank;
        private Label lPatrolledRankAndBelow;
        private Button bLowerRank;
        private Button bRaiseRank;
        private Label lRankList;
        private CheckBox xIRCRegisteredNick;
        private TextBox tIRCNickServMessage;
        private Label lIRCNickServMessage;
        private TextBox tIRCNickServ;
        private Label lIRCNickServ;
        private ToolTip toolTip;
        private Label lIRCNoForwardingMessage;
        private Label lIRCDelayUnits;
        private NumericUpDown nMaxUndo;
        private Label lMaxUndoUnits;
        private CheckBox xMaxUndo;
        private Label lDefaultBuildRank;
        private ComboBox cDefaultBuildRank;
        private Button bGreeting;
        private ComboBox cIRCList;
        private Label lIRCList;
        private CheckBox xIRCListShowNonEnglish;
        private CheckBox xAllowUnverifiedLAN;
        private TabPage tabChat;
        private GroupBox gAppearence;
        private CheckBox xShowJoinedWorldMessages;
        private CheckBox xRankColorsInWorldNames;
        private Button bColorPM;
        private Label lColorPM;
        private Button bColorAnnouncement;
        private Label lColorAnnouncement;
        private Button bColorSay;
        private Button bColorHelp;
        private Button bColorSys;
        private CheckBox xRankPrefixesInList;
        private CheckBox xRankPrefixesInChat;
        private CheckBox xRankColorsInChat;
        private Label lColorSay;
        private Label lColorHelp;
        private Label lColorSys;
        private ChatPreview chatPreview;
        private GroupBox gChatColors;
        private Label lColorWarning;
        private Button bColorWarning;
        private Label lColorMe;
        private Button bColorMe;
        private Label lLogFileOptionsDescription;
        private Label lLogConsoleOptionsDescription;
        private CheckBox xIRCBotAnnounceServerEvents;
        private CheckBox xIRCUseColor;
        private CheckBox xPaidPlayersOnly;
        private Button bMapPath;
        private CheckBox xMapPath;
        private TextBox tMapPath;
        private CheckBox xAllowSecurityCircumvention;
        private Label lConsoleName;
        private TextBox tConsoleName;
        private CheckBox xShowConnectionMessages;
        private GroupBox gHelpAndSupport;
        private Button bReadme;
        private Button bReportABug;
        private Button bCredits;
        private GroupBox groupBox2;
        private Button bOpenWiki;
        private ComboBox cUpdaterMode;
        private Label lUpdater;
        private GroupBox gUpdaterSettings;
        private Button bShowAdvancedUpdaterSettings;
        private Label lIPWarning;
        private TextBox tIP;
        private CheckBox xIP;
        private NumericUpDown nMaxConnectionsPerIP;
        private NumericUpDown nMaxPlayersPerWorld;
        private Label lMaxPlayersPerWorld;
        private CheckBox xAnnounceRankChangeReasons;
        private CheckBox xRequireKickReason;
        private GroupBox gPermissionLimits;
        private FlowLayoutPanel permissionLimitBoxContainer;
        private GroupBox gDataBackup;
        private CheckBox xBackupDataOnStartup;
        private GroupBox gBlockDB;
        private ComboBox cBlockDBAutoEnableRank;
        private CheckBox xBlockDBAutoEnable;
        private CheckBox xBlockDBEnabled;
        private CheckBox xWoMEnableEnvExtensions;
        private NumericUpDown nCopyPasteSlots;
        private Label lCopyPasteSlots;
        private DataGridViewTextBoxColumn dgvcName;
        private DataGridViewTextBoxColumn dgvcDescription;
        private DataGridViewComboBoxColumn dgvcAccess;
        private DataGridViewComboBoxColumn dgvcBuild;
        private DataGridViewComboBoxColumn dgvcBackup;
        private DataGridViewCheckBoxColumn dgvcHidden;
        private DataGridViewCheckBoxColumn dgvcBlockDB;
        private Label lFillLimitUnits;
        private NumericUpDown nFillLimit;
        private Label lFillLimit;
        private NumericUpDown nMaxUndoStates;
        private Label lMaxUndoStates;
        private GroupBox gPerformance;
        private Button bChangelog;
        private GroupBox gIRCColors;
        private CheckBox xIRCStripMinecraftColors;
        private GroupBox gWoMDirect;
        private Label lWoMDirectFlags;
        private TextBox tWoMDirectFlags;
        private Label lWoMDirectDescription;
        private TextBox tWoMDirectDescription;
        private CheckBox xHeartbeatToWoMDirect;
        private GroupBox gCrashReport;
        private Label lCrashReportDisclaimer;
        private CheckBox xSubmitCrashReports;
        private CheckBox xAntispamMessageCount;
        private CheckBox xAntispamMuteDuration;
    }
}