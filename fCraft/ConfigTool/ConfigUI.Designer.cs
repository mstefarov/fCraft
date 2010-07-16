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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem( "System Activity" );
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem( "Warnings" );
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem( "Errors" );
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem( "Critical Errors" );
            System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem( "User Activity" );
            System.Windows.Forms.ListViewItem listViewItem6 = new System.Windows.Forms.ListViewItem( "User Commands" );
            System.Windows.Forms.ListViewItem listViewItem7 = new System.Windows.Forms.ListViewItem( "Suspicious Activity" );
            System.Windows.Forms.ListViewItem listViewItem8 = new System.Windows.Forms.ListViewItem( "Chat" );
            System.Windows.Forms.ListViewItem listViewItem9 = new System.Windows.Forms.ListViewItem( "Private Chat" );
            System.Windows.Forms.ListViewItem listViewItem10 = new System.Windows.Forms.ListViewItem( "Class Chat" );
            System.Windows.Forms.ListViewItem listViewItem11 = new System.Windows.Forms.ListViewItem( "Console Input" );
            System.Windows.Forms.ListViewItem listViewItem12 = new System.Windows.Forms.ListViewItem( "Console Output" );
            System.Windows.Forms.ListViewItem listViewItem13 = new System.Windows.Forms.ListViewItem( "Debug Information" );
            System.Windows.Forms.ListViewItem listViewItem14 = new System.Windows.Forms.ListViewItem( "System Activity" );
            System.Windows.Forms.ListViewItem listViewItem15 = new System.Windows.Forms.ListViewItem( "Warnings" );
            System.Windows.Forms.ListViewItem listViewItem16 = new System.Windows.Forms.ListViewItem( "Errors" );
            System.Windows.Forms.ListViewItem listViewItem17 = new System.Windows.Forms.ListViewItem( "Critical Errors" );
            System.Windows.Forms.ListViewItem listViewItem18 = new System.Windows.Forms.ListViewItem( "User Activity" );
            System.Windows.Forms.ListViewItem listViewItem19 = new System.Windows.Forms.ListViewItem( "User Commands" );
            System.Windows.Forms.ListViewItem listViewItem20 = new System.Windows.Forms.ListViewItem( "Suspicious Activity" );
            System.Windows.Forms.ListViewItem listViewItem21 = new System.Windows.Forms.ListViewItem( "Chat" );
            System.Windows.Forms.ListViewItem listViewItem22 = new System.Windows.Forms.ListViewItem( "Private Chat" );
            System.Windows.Forms.ListViewItem listViewItem23 = new System.Windows.Forms.ListViewItem( "Class Chat" );
            System.Windows.Forms.ListViewItem listViewItem24 = new System.Windows.Forms.ListViewItem( "Console Input" );
            System.Windows.Forms.ListViewItem listViewItem25 = new System.Windows.Forms.ListViewItem( "Console Output" );
            System.Windows.Forms.ListViewItem listViewItem26 = new System.Windows.Forms.ListViewItem( "Debug Information" );
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( ConfigUI ) );
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.bRules = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.gAppearence = new System.Windows.Forms.GroupBox();
            this.bColorSay = new System.Windows.Forms.Button();
            this.bColorHelp = new System.Windows.Forms.Button();
            this.bColorSys = new System.Windows.Forms.Button();
            this.xListPrefixes = new System.Windows.Forms.CheckBox();
            this.xChatPrefixes = new System.Windows.Forms.CheckBox();
            this.xClassColors = new System.Windows.Forms.CheckBox();
            this.lSayColor = new System.Windows.Forms.Label();
            this.lHelpColor = new System.Windows.Forms.Label();
            this.lMessageColor = new System.Windows.Forms.Label();
            this.gBasic = new System.Windows.Forms.GroupBox();
            this.lPort = new System.Windows.Forms.Label();
            this.nPort = new System.Windows.Forms.NumericUpDown();
            this.cDefaultClass = new System.Windows.Forms.ComboBox();
            this.lDefaultClass = new System.Windows.Forms.Label();
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
            this.bWorldDel = new System.Windows.Forms.Button();
            this.bWorldDup = new System.Windows.Forms.Button();
            this.bWorldGen = new System.Windows.Forms.Button();
            this.bWorldLoad = new System.Windows.Forms.Button();
            this.dgWorlds = new System.Windows.Forms.DataGridView();
            this.wName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Map = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.wHidden = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.wAccess = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.wBuild = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.wBackup = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.tabClasses = new System.Windows.Forms.TabPage();
            this.gClassOptions = new System.Windows.Forms.GroupBox();
            this.bColorClass = new System.Windows.Forms.Button();
            this.xBanOn = new System.Windows.Forms.CheckBox();
            this.lBanOnUnits = new System.Windows.Forms.Label();
            this.lKickIdleUnits = new System.Windows.Forms.Label();
            this.nBanOn = new System.Windows.Forms.NumericUpDown();
            this.nKickIdle = new System.Windows.Forms.NumericUpDown();
            this.xKickOn = new System.Windows.Forms.CheckBox();
            this.lKickOnUnits = new System.Windows.Forms.Label();
            this.xIdleKick = new System.Windows.Forms.CheckBox();
            this.nKickOn = new System.Windows.Forms.NumericUpDown();
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
            this.nRank = new System.Windows.Forms.NumericUpDown();
            this.lRank = new System.Windows.Forms.Label();
            this.lClassColor = new System.Windows.Forms.Label();
            this.tClassName = new System.Windows.Forms.TextBox();
            this.lClassName = new System.Windows.Forms.Label();
            this.bRemoveClass = new System.Windows.Forms.Button();
            this.vPermissions = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.bAddClass = new System.Windows.Forms.Button();
            this.lPermissions = new System.Windows.Forms.Label();
            this.lClasses = new System.Windows.Forms.Label();
            this.vClasses = new System.Windows.Forms.ListBox();
            this.tabSecurity = new System.Windows.Forms.TabPage();
            this.gAntigrief = new System.Windows.Forms.GroupBox();
            this.lSpamBlockRate = new System.Windows.Forms.Label();
            this.nSpamBlockCount = new System.Windows.Forms.NumericUpDown();
            this.lSpamBlocks = new System.Windows.Forms.Label();
            this.nSpamBlockTimer = new System.Windows.Forms.NumericUpDown();
            this.lSpamBlockSeconds = new System.Windows.Forms.Label();
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
            this.gHackingDetection = new System.Windows.Forms.GroupBox();
            this.label11 = new System.Windows.Forms.Label();
            this.numericUpDown7 = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.gVerify = new System.Windows.Forms.GroupBox();
            this.xLimitOneConnectionPerIP = new System.Windows.Forms.CheckBox();
            this.xAnnounceUnverified = new System.Windows.Forms.CheckBox();
            this.lVerifyNames = new System.Windows.Forms.Label();
            this.cVerifyNames = new System.Windows.Forms.ComboBox();
            this.tabSavingAndBackup = new System.Windows.Forms.TabPage();
            this.gSaving = new System.Windows.Forms.GroupBox();
            this.xSaveOnShutdown = new System.Windows.Forms.CheckBox();
            this.nSaveInterval = new System.Windows.Forms.NumericUpDown();
            this.nSaveIntervalUnits = new System.Windows.Forms.Label();
            this.xSaveAtInterval = new System.Windows.Forms.CheckBox();
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
            this.xBackupAtInterval = new System.Windows.Forms.CheckBox();
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
            this.xIRCBotForwardFromIRC = new System.Windows.Forms.CheckBox();
            this.xIRCMsgs = new System.Windows.Forms.CheckBox();
            this.xIRCBotForwardFromServer = new System.Windows.Forms.CheckBox();
            this.gIRCNetwork = new System.Windows.Forms.GroupBox();
            this.lIRCBotQuitMsg = new System.Windows.Forms.Label();
            this.tIRCBotQuitMsg = new System.Windows.Forms.TextBox();
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
            this.xLowLatencyMode = new System.Windows.Forms.CheckBox();
            this.cUpdater = new System.Windows.Forms.ComboBox();
            this.bUpdater = new System.Windows.Forms.Label();
            this.lThrottlingUnits = new System.Windows.Forms.Label();
            this.nThrottling = new System.Windows.Forms.NumericUpDown();
            this.lThrottling = new System.Windows.Forms.Label();
            this.lPing = new System.Windows.Forms.Label();
            this.nPing = new System.Windows.Forms.NumericUpDown();
            this.xAbsoluteUpdates = new System.Windows.Forms.CheckBox();
            this.xPing = new System.Windows.Forms.CheckBox();
            this.cStartup = new System.Windows.Forms.ComboBox();
            this.lStartup = new System.Windows.Forms.Label();
            this.cProcessPriority = new System.Windows.Forms.ComboBox();
            this.lProcessPriority = new System.Windows.Forms.Label();
            this.cPolicyIllegal = new System.Windows.Forms.ComboBox();
            this.xRedundantPacket = new System.Windows.Forms.CheckBox();
            this.lPolicyColor = new System.Windows.Forms.Label();
            this.lPolicyIllegal = new System.Windows.Forms.Label();
            this.cPolicyColor = new System.Windows.Forms.ComboBox();
            this.lTickIntervalUnits = new System.Windows.Forms.Label();
            this.nTickInterval = new System.Windows.Forms.NumericUpDown();
            this.lTickInterval = new System.Windows.Forms.Label();
            this.lAdvancedWarning = new System.Windows.Forms.Label();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.bResetTab = new System.Windows.Forms.Button();
            this.tip = new System.Windows.Forms.ToolTip( this.components );
            this.bResetAll = new System.Windows.Forms.Button();
            this.bApply = new System.Windows.Forms.Button();
            this.tabs.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.gAppearence.SuspendLayout();
            this.gBasic.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUploadBandwidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayers)).BeginInit();
            this.tabWorlds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgWorlds)).BeginInit();
            this.tabClasses.SuspendLayout();
            this.gClassOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nBanOn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickIdle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickOn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nRank)).BeginInit();
            this.tabSecurity.SuspendLayout();
            this.gAntigrief.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamBlockCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamBlockTimer)).BeginInit();
            this.gSpamChat.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatWarnings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamMute)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatTimer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatCount)).BeginInit();
            this.gHackingDetection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown7)).BeginInit();
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
            ((System.ComponentModel.ISupportInitialize)(this.nIRCBotPort)).BeginInit();
            this.tabAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nThrottling)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nPing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nTickInterval)).BeginInit();
            this.SuspendLayout();
            // 
            // tabs
            // 
            this.tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabs.Controls.Add( this.tabGeneral );
            this.tabs.Controls.Add( this.tabWorlds );
            this.tabs.Controls.Add( this.tabClasses );
            this.tabs.Controls.Add( this.tabSecurity );
            this.tabs.Controls.Add( this.tabSavingAndBackup );
            this.tabs.Controls.Add( this.tabLogging );
            this.tabs.Controls.Add( this.tabIRC );
            this.tabs.Controls.Add( this.tabAdvanced );
            this.tabs.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.tabs.Location = new System.Drawing.Point( 12, 12 );
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size( 659, 436 );
            this.tabs.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add( this.bRules );
            this.tabGeneral.Controls.Add( this.label1 );
            this.tabGeneral.Controls.Add( this.gAppearence );
            this.tabGeneral.Controls.Add( this.gBasic );
            this.tabGeneral.Location = new System.Drawing.Point( 4, 24 );
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabGeneral.Size = new System.Drawing.Size( 651, 408 );
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // bRules
            // 
            this.bRules.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bRules.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bRules.Location = new System.Drawing.Point( 8, 300 );
            this.bRules.Name = "bRules";
            this.bRules.Size = new System.Drawing.Size( 100, 28 );
            this.bRules.TabIndex = 14;
            this.bRules.Text = "Edit rules.txt";
            this.bRules.UseVisualStyleBackColor = true;
            this.bRules.Click += new System.EventHandler( this.bRules_Click );
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.label1.Location = new System.Drawing.Point( 97, 362 );
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size( 457, 15 );
            this.label1.TabIndex = 14;
            this.label1.Text = "NOTE: Grayed-out and disabled settings are NOT IMPLEMENTED YET.";
            // 
            // gAppearence
            // 
            this.gAppearence.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gAppearence.Controls.Add( this.bColorSay );
            this.gAppearence.Controls.Add( this.bColorHelp );
            this.gAppearence.Controls.Add( this.bColorSys );
            this.gAppearence.Controls.Add( this.xListPrefixes );
            this.gAppearence.Controls.Add( this.xChatPrefixes );
            this.gAppearence.Controls.Add( this.xClassColors );
            this.gAppearence.Controls.Add( this.lSayColor );
            this.gAppearence.Controls.Add( this.lHelpColor );
            this.gAppearence.Controls.Add( this.lMessageColor );
            this.gAppearence.Location = new System.Drawing.Point( 8, 181 );
            this.gAppearence.Name = "gAppearence";
            this.gAppearence.Size = new System.Drawing.Size( 635, 113 );
            this.gAppearence.TabIndex = 13;
            this.gAppearence.TabStop = false;
            this.gAppearence.Text = "Appearence Tweaks";
            // 
            // bColorSay
            // 
            this.bColorSay.Location = new System.Drawing.Point( 440, 77 );
            this.bColorSay.Name = "bColorSay";
            this.bColorSay.Size = new System.Drawing.Size( 100, 23 );
            this.bColorSay.TabIndex = 21;
            this.bColorSay.UseVisualStyleBackColor = true;
            this.bColorSay.Click += new System.EventHandler( this.bColorSay_Click );
            // 
            // bColorHelp
            // 
            this.bColorHelp.Location = new System.Drawing.Point( 440, 48 );
            this.bColorHelp.Name = "bColorHelp";
            this.bColorHelp.Size = new System.Drawing.Size( 100, 23 );
            this.bColorHelp.TabIndex = 20;
            this.bColorHelp.UseVisualStyleBackColor = true;
            this.bColorHelp.Click += new System.EventHandler( this.bColorHelp_Click );
            // 
            // bColorSys
            // 
            this.bColorSys.Location = new System.Drawing.Point( 440, 19 );
            this.bColorSys.Name = "bColorSys";
            this.bColorSys.Size = new System.Drawing.Size( 100, 23 );
            this.bColorSys.TabIndex = 19;
            this.bColorSys.UseVisualStyleBackColor = true;
            this.bColorSys.Click += new System.EventHandler( this.bColorSys_Click );
            // 
            // xListPrefixes
            // 
            this.xListPrefixes.AutoSize = true;
            this.xListPrefixes.Location = new System.Drawing.Point( 39, 81 );
            this.xListPrefixes.Name = "xListPrefixes";
            this.xListPrefixes.Size = new System.Drawing.Size( 201, 19 );
            this.xListPrefixes.TabIndex = 5;
            this.xListPrefixes.Text = "Show class prefixes in player list";
            this.xListPrefixes.UseVisualStyleBackColor = true;
            // 
            // xChatPrefixes
            // 
            this.xChatPrefixes.AutoSize = true;
            this.xChatPrefixes.Location = new System.Drawing.Point( 39, 52 );
            this.xChatPrefixes.Name = "xChatPrefixes";
            this.xChatPrefixes.Size = new System.Drawing.Size( 173, 19 );
            this.xChatPrefixes.TabIndex = 4;
            this.xChatPrefixes.Text = "Show class prefixes in chat";
            this.xChatPrefixes.UseVisualStyleBackColor = true;
            // 
            // xClassColors
            // 
            this.xClassColors.AutoSize = true;
            this.xClassColors.Location = new System.Drawing.Point( 39, 23 );
            this.xClassColors.Name = "xClassColors";
            this.xClassColors.Size = new System.Drawing.Size( 163, 19 );
            this.xClassColors.TabIndex = 3;
            this.xClassColors.Text = "Show class colors in chat";
            this.xClassColors.UseVisualStyleBackColor = true;
            // 
            // lSayColor
            // 
            this.lSayColor.AutoSize = true;
            this.lSayColor.Location = new System.Drawing.Point( 375, 81 );
            this.lSayColor.Name = "lSayColor";
            this.lSayColor.Size = new System.Drawing.Size( 59, 15 );
            this.lSayColor.TabIndex = 2;
            this.lSayColor.Text = "Say Color";
            // 
            // lHelpColor
            // 
            this.lHelpColor.AutoSize = true;
            this.lHelpColor.Location = new System.Drawing.Point( 369, 52 );
            this.lHelpColor.Name = "lHelpColor";
            this.lHelpColor.Size = new System.Drawing.Size( 65, 15 );
            this.lHelpColor.TabIndex = 1;
            this.lHelpColor.Text = "Help Color";
            // 
            // lMessageColor
            // 
            this.lMessageColor.AutoSize = true;
            this.lMessageColor.Location = new System.Drawing.Point( 301, 23 );
            this.lMessageColor.Name = "lMessageColor";
            this.lMessageColor.Size = new System.Drawing.Size( 133, 15 );
            this.lMessageColor.TabIndex = 0;
            this.lMessageColor.Text = "System Message Color";
            // 
            // gBasic
            // 
            this.gBasic.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gBasic.Controls.Add( this.lPort );
            this.gBasic.Controls.Add( this.nPort );
            this.gBasic.Controls.Add( this.cDefaultClass );
            this.gBasic.Controls.Add( this.lDefaultClass );
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
            this.gBasic.Size = new System.Drawing.Size( 635, 162 );
            this.gBasic.TabIndex = 12;
            this.gBasic.TabStop = false;
            this.gBasic.Text = "Basic Settings";
            // 
            // lPort
            // 
            this.lPort.AutoSize = true;
            this.lPort.Location = new System.Drawing.Point( 359, 105 );
            this.lPort.Name = "lPort";
            this.lPort.Size = new System.Drawing.Size( 75, 15 );
            this.lPort.TabIndex = 32;
            this.lPort.Text = "Port number";
            // 
            // nPort
            // 
            this.nPort.Location = new System.Drawing.Point( 440, 103 );
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
            this.nPort.TabIndex = 31;
            this.nPort.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // cDefaultClass
            // 
            this.cDefaultClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cDefaultClass.FormattingEnabled = true;
            this.cDefaultClass.Location = new System.Drawing.Point( 440, 74 );
            this.cDefaultClass.Name = "cDefaultClass";
            this.cDefaultClass.Size = new System.Drawing.Size( 189, 23 );
            this.cDefaultClass.TabIndex = 13;
            // 
            // lDefaultClass
            // 
            this.lDefaultClass.AutoSize = true;
            this.lDefaultClass.Location = new System.Drawing.Point( 357, 77 );
            this.lDefaultClass.Name = "lDefaultClass";
            this.lDefaultClass.Size = new System.Drawing.Size( 77, 15 );
            this.lDefaultClass.TabIndex = 12;
            this.lDefaultClass.Text = "Default class";
            // 
            // lUploadBandwidth
            // 
            this.lUploadBandwidth.AutoSize = true;
            this.lUploadBandwidth.Location = new System.Drawing.Point( 7, 134 );
            this.lUploadBandwidth.Name = "lUploadBandwidth";
            this.lUploadBandwidth.Size = new System.Drawing.Size( 107, 15 );
            this.lUploadBandwidth.TabIndex = 8;
            this.lUploadBandwidth.Text = "Upload bandwidth";
            // 
            // bMeasure
            // 
            this.bMeasure.Location = new System.Drawing.Point( 231, 130 );
            this.bMeasure.Name = "bMeasure";
            this.bMeasure.Size = new System.Drawing.Size( 75, 23 );
            this.bMeasure.TabIndex = 11;
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
            this.lUploadBandwidthUnits.Location = new System.Drawing.Point( 193, 134 );
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
            this.nUploadBandwidth.Location = new System.Drawing.Point( 120, 132 );
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
            this.nUploadBandwidth.Size = new System.Drawing.Size( 67, 21 );
            this.nUploadBandwidth.TabIndex = 9;
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
            this.tMOTD.TabIndex = 2;
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
            this.cPublic.FormattingEnabled = true;
            this.cPublic.Items.AddRange( new object[] {
            "Public",
            "Private"} );
            this.cPublic.Location = new System.Drawing.Point( 120, 103 );
            this.cPublic.Name = "cPublic";
            this.cPublic.Size = new System.Drawing.Size( 67, 23 );
            this.cPublic.TabIndex = 7;
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
            this.nMaxPlayers.TabIndex = 4;
            this.nMaxPlayers.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lPublic
            // 
            this.lPublic.AutoSize = true;
            this.lPublic.Location = new System.Drawing.Point( 64, 106 );
            this.lPublic.Name = "lPublic";
            this.lPublic.Size = new System.Drawing.Size( 50, 15 );
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
            this.tabWorlds.Controls.Add( this.bWorldDel );
            this.tabWorlds.Controls.Add( this.bWorldDup );
            this.tabWorlds.Controls.Add( this.bWorldGen );
            this.tabWorlds.Controls.Add( this.bWorldLoad );
            this.tabWorlds.Controls.Add( this.dgWorlds );
            this.tabWorlds.Location = new System.Drawing.Point( 4, 24 );
            this.tabWorlds.Name = "tabWorlds";
            this.tabWorlds.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabWorlds.Size = new System.Drawing.Size( 651, 408 );
            this.tabWorlds.TabIndex = 9;
            this.tabWorlds.Text = "Worlds";
            this.tabWorlds.UseVisualStyleBackColor = true;
            // 
            // bWorldDel
            // 
            this.bWorldDel.Enabled = false;
            this.bWorldDel.Location = new System.Drawing.Point( 523, 13 );
            this.bWorldDel.Name = "bWorldDel";
            this.bWorldDel.Size = new System.Drawing.Size( 120, 28 );
            this.bWorldDel.TabIndex = 4;
            this.bWorldDel.Text = "Delete World";
            this.bWorldDel.UseVisualStyleBackColor = true;
            // 
            // bWorldDup
            // 
            this.bWorldDup.Enabled = false;
            this.bWorldDup.Location = new System.Drawing.Point( 261, 13 );
            this.bWorldDup.Name = "bWorldDup";
            this.bWorldDup.Size = new System.Drawing.Size( 120, 28 );
            this.bWorldDup.TabIndex = 3;
            this.bWorldDup.Text = "Duplicate World";
            this.bWorldDup.UseVisualStyleBackColor = true;
            // 
            // bWorldGen
            // 
            this.bWorldGen.Enabled = false;
            this.bWorldGen.Location = new System.Drawing.Point( 135, 13 );
            this.bWorldGen.Name = "bWorldGen";
            this.bWorldGen.Size = new System.Drawing.Size( 120, 28 );
            this.bWorldGen.TabIndex = 2;
            this.bWorldGen.Text = "Generate New";
            this.bWorldGen.UseVisualStyleBackColor = true;
            // 
            // bWorldLoad
            // 
            this.bWorldLoad.Enabled = false;
            this.bWorldLoad.Location = new System.Drawing.Point( 9, 13 );
            this.bWorldLoad.Name = "bWorldLoad";
            this.bWorldLoad.Size = new System.Drawing.Size( 120, 28 );
            this.bWorldLoad.TabIndex = 1;
            this.bWorldLoad.Text = "Load World";
            this.bWorldLoad.UseVisualStyleBackColor = true;
            // 
            // dgWorlds
            // 
            this.dgWorlds.AllowUserToResizeRows = false;
            this.dgWorlds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgWorlds.Columns.AddRange( new System.Windows.Forms.DataGridViewColumn[] {
            this.wName,
            this.Map,
            this.wHidden,
            this.wAccess,
            this.wBuild,
            this.wBackup} );
            this.dgWorlds.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgWorlds.Enabled = false;
            this.dgWorlds.Location = new System.Drawing.Point( 9, 47 );
            this.dgWorlds.Name = "dgWorlds";
            this.dgWorlds.RowHeadersVisible = false;
            this.dgWorlds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgWorlds.Size = new System.Drawing.Size( 634, 348 );
            this.dgWorlds.TabIndex = 0;
            // 
            // wName
            // 
            this.wName.HeaderText = "World Name";
            this.wName.Name = "wName";
            this.wName.Width = 110;
            // 
            // Map
            // 
            this.Map.HeaderText = "";
            this.Map.Name = "Map";
            this.Map.Width = 180;
            // 
            // wHidden
            // 
            this.wHidden.HeaderText = "Hide";
            this.wHidden.Name = "wHidden";
            this.wHidden.Width = 40;
            // 
            // wAccess
            // 
            this.wAccess.HeaderText = "Access";
            this.wAccess.Name = "wAccess";
            this.wAccess.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // wBuild
            // 
            this.wBuild.HeaderText = "Build";
            this.wBuild.Name = "wBuild";
            this.wBuild.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // wBackup
            // 
            this.wBackup.HeaderText = "Backup";
            this.wBackup.Name = "wBackup";
            this.wBackup.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // tabClasses
            // 
            this.tabClasses.Controls.Add( this.gClassOptions );
            this.tabClasses.Controls.Add( this.bRemoveClass );
            this.tabClasses.Controls.Add( this.vPermissions );
            this.tabClasses.Controls.Add( this.bAddClass );
            this.tabClasses.Controls.Add( this.lPermissions );
            this.tabClasses.Controls.Add( this.lClasses );
            this.tabClasses.Controls.Add( this.vClasses );
            this.tabClasses.Location = new System.Drawing.Point( 4, 24 );
            this.tabClasses.Name = "tabClasses";
            this.tabClasses.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabClasses.Size = new System.Drawing.Size( 651, 408 );
            this.tabClasses.TabIndex = 2;
            this.tabClasses.Text = "Classes";
            this.tabClasses.UseVisualStyleBackColor = true;
            // 
            // gClassOptions
            // 
            this.gClassOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gClassOptions.Controls.Add( this.bColorClass );
            this.gClassOptions.Controls.Add( this.xBanOn );
            this.gClassOptions.Controls.Add( this.lBanOnUnits );
            this.gClassOptions.Controls.Add( this.lKickIdleUnits );
            this.gClassOptions.Controls.Add( this.nBanOn );
            this.gClassOptions.Controls.Add( this.nKickIdle );
            this.gClassOptions.Controls.Add( this.xKickOn );
            this.gClassOptions.Controls.Add( this.lKickOnUnits );
            this.gClassOptions.Controls.Add( this.xIdleKick );
            this.gClassOptions.Controls.Add( this.nKickOn );
            this.gClassOptions.Controls.Add( this.xReserveSlot );
            this.gClassOptions.Controls.Add( this.cBanLimit );
            this.gClassOptions.Controls.Add( this.cKickLimit );
            this.gClassOptions.Controls.Add( this.cDemoteLimit );
            this.gClassOptions.Controls.Add( this.cPromoteLimit );
            this.gClassOptions.Controls.Add( this.lBanLimit );
            this.gClassOptions.Controls.Add( this.lKickLimit );
            this.gClassOptions.Controls.Add( this.lDemoteLimit );
            this.gClassOptions.Controls.Add( this.lPromoteLimit );
            this.gClassOptions.Controls.Add( this.tPrefix );
            this.gClassOptions.Controls.Add( this.lPrefix );
            this.gClassOptions.Controls.Add( this.nRank );
            this.gClassOptions.Controls.Add( this.lRank );
            this.gClassOptions.Controls.Add( this.lClassColor );
            this.gClassOptions.Controls.Add( this.tClassName );
            this.gClassOptions.Controls.Add( this.lClassName );
            this.gClassOptions.Location = new System.Drawing.Point( 155, 13 );
            this.gClassOptions.Name = "gClassOptions";
            this.gClassOptions.Size = new System.Drawing.Size( 303, 382 );
            this.gClassOptions.TabIndex = 9;
            this.gClassOptions.TabStop = false;
            this.gClassOptions.Text = "Class Options";
            // 
            // bColorClass
            // 
            this.bColorClass.Location = new System.Drawing.Point( 96, 73 );
            this.bColorClass.Name = "bColorClass";
            this.bColorClass.Size = new System.Drawing.Size( 100, 24 );
            this.bColorClass.TabIndex = 20;
            this.bColorClass.UseVisualStyleBackColor = true;
            this.bColorClass.Click += new System.EventHandler( this.bColorClass_Click );
            // 
            // xBanOn
            // 
            this.xBanOn.AutoSize = true;
            this.xBanOn.Location = new System.Drawing.Point( 12, 309 );
            this.xBanOn.Name = "xBanOn";
            this.xBanOn.Size = new System.Drawing.Size( 141, 19 );
            this.xBanOn.TabIndex = 9;
            this.xBanOn.Text = "Ban for blockspam at";
            this.xBanOn.UseVisualStyleBackColor = true;
            this.xBanOn.CheckedChanged += new System.EventHandler( this.xBanOn_CheckedChanged );
            // 
            // lBanOnUnits
            // 
            this.lBanOnUnits.AutoSize = true;
            this.lBanOnUnits.Location = new System.Drawing.Point( 224, 310 );
            this.lBanOnUnits.Name = "lBanOnUnits";
            this.lBanOnUnits.Size = new System.Drawing.Size( 64, 15 );
            this.lBanOnUnits.TabIndex = 8;
            this.lBanOnUnits.Text = "blocks/sec";
            // 
            // lKickIdleUnits
            // 
            this.lKickIdleUnits.AutoSize = true;
            this.lKickIdleUnits.Location = new System.Drawing.Point( 181, 256 );
            this.lKickIdleUnits.Name = "lKickIdleUnits";
            this.lKickIdleUnits.Size = new System.Drawing.Size( 51, 15 );
            this.lKickIdleUnits.TabIndex = 19;
            this.lKickIdleUnits.Text = "minutes";
            // 
            // nBanOn
            // 
            this.nBanOn.Location = new System.Drawing.Point( 160, 308 );
            this.nBanOn.Name = "nBanOn";
            this.nBanOn.Size = new System.Drawing.Size( 58, 21 );
            this.nBanOn.TabIndex = 7;
            this.nBanOn.ValueChanged += new System.EventHandler( this.nBanOn_ValueChanged );
            // 
            // nKickIdle
            // 
            this.nKickIdle.Location = new System.Drawing.Point( 116, 254 );
            this.nKickIdle.Maximum = new decimal( new int[] {
            1000,
            0,
            0,
            0} );
            this.nKickIdle.Name = "nKickIdle";
            this.nKickIdle.Size = new System.Drawing.Size( 59, 21 );
            this.nKickIdle.TabIndex = 18;
            this.nKickIdle.ValueChanged += new System.EventHandler( this.nKickIdle_ValueChanged );
            // 
            // xKickOn
            // 
            this.xKickOn.AutoSize = true;
            this.xKickOn.Location = new System.Drawing.Point( 12, 282 );
            this.xKickOn.Name = "xKickOn";
            this.xKickOn.Size = new System.Drawing.Size( 142, 19 );
            this.xKickOn.TabIndex = 6;
            this.xKickOn.Text = "Kick for blockspam at";
            this.xKickOn.UseVisualStyleBackColor = true;
            this.xKickOn.CheckedChanged += new System.EventHandler( this.xKickOn_CheckedChanged );
            // 
            // lKickOnUnits
            // 
            this.lKickOnUnits.AutoSize = true;
            this.lKickOnUnits.Location = new System.Drawing.Point( 224, 283 );
            this.lKickOnUnits.Name = "lKickOnUnits";
            this.lKickOnUnits.Size = new System.Drawing.Size( 64, 15 );
            this.lKickOnUnits.TabIndex = 5;
            this.lKickOnUnits.Text = "blocks/sec";
            // 
            // xIdleKick
            // 
            this.xIdleKick.AutoSize = true;
            this.xIdleKick.Location = new System.Drawing.Point( 12, 255 );
            this.xIdleKick.Name = "xIdleKick";
            this.xIdleKick.Size = new System.Drawing.Size( 98, 19 );
            this.xIdleKick.TabIndex = 17;
            this.xIdleKick.Text = "Kick if idle for";
            this.xIdleKick.UseVisualStyleBackColor = true;
            this.xIdleKick.CheckedChanged += new System.EventHandler( this.xIdleKick_CheckedChanged );
            // 
            // nKickOn
            // 
            this.nKickOn.Location = new System.Drawing.Point( 160, 281 );
            this.nKickOn.Name = "nKickOn";
            this.nKickOn.Size = new System.Drawing.Size( 58, 21 );
            this.nKickOn.TabIndex = 4;
            this.nKickOn.ValueChanged += new System.EventHandler( this.nKickOn_ValueChanged );
            // 
            // xReserveSlot
            // 
            this.xReserveSlot.AutoSize = true;
            this.xReserveSlot.Enabled = false;
            this.xReserveSlot.Location = new System.Drawing.Point( 12, 230 );
            this.xReserveSlot.Name = "xReserveSlot";
            this.xReserveSlot.Size = new System.Drawing.Size( 129, 19 );
            this.xReserveSlot.TabIndex = 16;
            this.xReserveSlot.Text = "Reserve player slot";
            this.xReserveSlot.UseVisualStyleBackColor = true;
            this.xReserveSlot.CheckedChanged += new System.EventHandler( this.xReserveSlot_CheckedChanged );
            // 
            // cBanLimit
            // 
            this.cBanLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBanLimit.FormattingEnabled = true;
            this.cBanLimit.Location = new System.Drawing.Point( 96, 190 );
            this.cBanLimit.Name = "cBanLimit";
            this.cBanLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cBanLimit.TabIndex = 15;
            this.cBanLimit.SelectedIndexChanged += new System.EventHandler( this.cBanLimit_SelectedIndexChanged );
            // 
            // cKickLimit
            // 
            this.cKickLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cKickLimit.FormattingEnabled = true;
            this.cKickLimit.Location = new System.Drawing.Point( 96, 161 );
            this.cKickLimit.Name = "cKickLimit";
            this.cKickLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cKickLimit.TabIndex = 14;
            this.cKickLimit.SelectedIndexChanged += new System.EventHandler( this.cKickLimit_SelectedIndexChanged );
            // 
            // cDemoteLimit
            // 
            this.cDemoteLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cDemoteLimit.FormattingEnabled = true;
            this.cDemoteLimit.Location = new System.Drawing.Point( 96, 132 );
            this.cDemoteLimit.Name = "cDemoteLimit";
            this.cDemoteLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cDemoteLimit.TabIndex = 13;
            this.cDemoteLimit.SelectedIndexChanged += new System.EventHandler( this.cDemoteLimit_SelectedIndexChanged );
            // 
            // cPromoteLimit
            // 
            this.cPromoteLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPromoteLimit.FormattingEnabled = true;
            this.cPromoteLimit.Location = new System.Drawing.Point( 96, 103 );
            this.cPromoteLimit.Name = "cPromoteLimit";
            this.cPromoteLimit.Size = new System.Drawing.Size( 180, 23 );
            this.cPromoteLimit.TabIndex = 12;
            this.cPromoteLimit.SelectedIndexChanged += new System.EventHandler( this.cPromoteLimit_SelectedIndexChanged );
            // 
            // lBanLimit
            // 
            this.lBanLimit.AutoSize = true;
            this.lBanLimit.Location = new System.Drawing.Point( 31, 193 );
            this.lBanLimit.Name = "lBanLimit";
            this.lBanLimit.Size = new System.Drawing.Size( 59, 15 );
            this.lBanLimit.TabIndex = 11;
            this.lBanLimit.Text = "Ban Limit";
            // 
            // lKickLimit
            // 
            this.lKickLimit.AutoSize = true;
            this.lKickLimit.Location = new System.Drawing.Point( 30, 164 );
            this.lKickLimit.Name = "lKickLimit";
            this.lKickLimit.Size = new System.Drawing.Size( 60, 15 );
            this.lKickLimit.TabIndex = 10;
            this.lKickLimit.Text = "Kick Limit";
            // 
            // lDemoteLimit
            // 
            this.lDemoteLimit.AutoSize = true;
            this.lDemoteLimit.Location = new System.Drawing.Point( 9, 135 );
            this.lDemoteLimit.Name = "lDemoteLimit";
            this.lDemoteLimit.Size = new System.Drawing.Size( 81, 15 );
            this.lDemoteLimit.TabIndex = 9;
            this.lDemoteLimit.Text = "Demote Limit";
            // 
            // lPromoteLimit
            // 
            this.lPromoteLimit.AutoSize = true;
            this.lPromoteLimit.Location = new System.Drawing.Point( 6, 106 );
            this.lPromoteLimit.Name = "lPromoteLimit";
            this.lPromoteLimit.Size = new System.Drawing.Size( 84, 15 );
            this.lPromoteLimit.TabIndex = 8;
            this.lPromoteLimit.Text = "Promote Limit";
            // 
            // tPrefix
            // 
            this.tPrefix.Location = new System.Drawing.Point( 254, 74 );
            this.tPrefix.MaxLength = 1;
            this.tPrefix.Name = "tPrefix";
            this.tPrefix.Size = new System.Drawing.Size( 22, 21 );
            this.tPrefix.TabIndex = 7;
            this.tPrefix.Validating += new System.ComponentModel.CancelEventHandler( this.tPrefix_Validating );
            // 
            // lPrefix
            // 
            this.lPrefix.AutoSize = true;
            this.lPrefix.Location = new System.Drawing.Point( 210, 77 );
            this.lPrefix.Name = "lPrefix";
            this.lPrefix.Size = new System.Drawing.Size( 38, 15 );
            this.lPrefix.TabIndex = 6;
            this.lPrefix.Text = "Prefix";
            // 
            // nRank
            // 
            this.nRank.Location = new System.Drawing.Point( 96, 47 );
            this.nRank.Maximum = new decimal( new int[] {
            255,
            0,
            0,
            0} );
            this.nRank.Name = "nRank";
            this.nRank.Size = new System.Drawing.Size( 54, 21 );
            this.nRank.TabIndex = 5;
            this.nRank.Validating += new System.ComponentModel.CancelEventHandler( this.nRank_Validating );
            // 
            // lRank
            // 
            this.lRank.AutoSize = true;
            this.lRank.Location = new System.Drawing.Point( 54, 49 );
            this.lRank.Name = "lRank";
            this.lRank.Size = new System.Drawing.Size( 36, 15 );
            this.lRank.TabIndex = 4;
            this.lRank.Text = "Rank";
            // 
            // lClassColor
            // 
            this.lClassColor.AutoSize = true;
            this.lClassColor.Location = new System.Drawing.Point( 54, 77 );
            this.lClassColor.Name = "lClassColor";
            this.lClassColor.Size = new System.Drawing.Size( 36, 15 );
            this.lClassColor.TabIndex = 2;
            this.lClassColor.Text = "Color";
            // 
            // tClassName
            // 
            this.tClassName.Location = new System.Drawing.Point( 96, 20 );
            this.tClassName.MaxLength = 16;
            this.tClassName.Name = "tClassName";
            this.tClassName.Size = new System.Drawing.Size( 143, 21 );
            this.tClassName.TabIndex = 1;
            this.tClassName.Validating += new System.ComponentModel.CancelEventHandler( this.tClassName_Validating );
            // 
            // lClassName
            // 
            this.lClassName.AutoSize = true;
            this.lClassName.Location = new System.Drawing.Point( 49, 23 );
            this.lClassName.Name = "lClassName";
            this.lClassName.Size = new System.Drawing.Size( 41, 15 );
            this.lClassName.TabIndex = 0;
            this.lClassName.Text = "Name";
            // 
            // bRemoveClass
            // 
            this.bRemoveClass.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bRemoveClass.Location = new System.Drawing.Point( 85, 372 );
            this.bRemoveClass.Name = "bRemoveClass";
            this.bRemoveClass.Size = new System.Drawing.Size( 64, 23 );
            this.bRemoveClass.TabIndex = 8;
            this.bRemoveClass.Text = "Remove";
            this.bRemoveClass.UseVisualStyleBackColor = true;
            this.bRemoveClass.Click += new System.EventHandler( this.bRemoveClass_Click );
            // 
            // vPermissions
            // 
            this.vPermissions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.vPermissions.CheckBoxes = true;
            this.vPermissions.Columns.AddRange( new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1} );
            this.vPermissions.GridLines = true;
            this.vPermissions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.vPermissions.Location = new System.Drawing.Point( 467, 28 );
            this.vPermissions.MultiSelect = false;
            this.vPermissions.Name = "vPermissions";
            this.vPermissions.ShowGroups = false;
            this.vPermissions.Size = new System.Drawing.Size( 176, 367 );
            this.vPermissions.TabIndex = 7;
            this.vPermissions.UseCompatibleStateImageBehavior = false;
            this.vPermissions.View = System.Windows.Forms.View.Details;
            this.vPermissions.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler( this.vPermissions_ItemChecked );
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 155;
            // 
            // bAddClass
            // 
            this.bAddClass.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bAddClass.Location = new System.Drawing.Point( 8, 372 );
            this.bAddClass.Name = "bAddClass";
            this.bAddClass.Size = new System.Drawing.Size( 57, 23 );
            this.bAddClass.TabIndex = 4;
            this.bAddClass.Text = "Add";
            this.bAddClass.UseVisualStyleBackColor = true;
            this.bAddClass.Click += new System.EventHandler( this.bAddClass_Click );
            // 
            // lPermissions
            // 
            this.lPermissions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lPermissions.AutoSize = true;
            this.lPermissions.Location = new System.Drawing.Point( 464, 10 );
            this.lPermissions.Name = "lPermissions";
            this.lPermissions.Size = new System.Drawing.Size( 108, 15 );
            this.lPermissions.TabIndex = 3;
            this.lPermissions.Text = "Class Permissions";
            // 
            // lClasses
            // 
            this.lClasses.AutoSize = true;
            this.lClasses.Location = new System.Drawing.Point( 8, 10 );
            this.lClasses.Name = "lClasses";
            this.lClasses.Size = new System.Drawing.Size( 50, 15 );
            this.lClasses.TabIndex = 1;
            this.lClasses.Text = "Classes";
            // 
            // vClasses
            // 
            this.vClasses.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.vClasses.Font = new System.Drawing.Font( "Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.vClasses.FormattingEnabled = true;
            this.vClasses.IntegralHeight = false;
            this.vClasses.Location = new System.Drawing.Point( 8, 28 );
            this.vClasses.Name = "vClasses";
            this.vClasses.Size = new System.Drawing.Size( 141, 338 );
            this.vClasses.TabIndex = 0;
            this.vClasses.SelectedIndexChanged += new System.EventHandler( this.vClasses_SelectedIndexChanged );
            // 
            // tabSecurity
            // 
            this.tabSecurity.Controls.Add( this.gAntigrief );
            this.tabSecurity.Controls.Add( this.gSpamChat );
            this.tabSecurity.Controls.Add( this.gHackingDetection );
            this.tabSecurity.Controls.Add( this.gVerify );
            this.tabSecurity.Location = new System.Drawing.Point( 4, 24 );
            this.tabSecurity.Name = "tabSecurity";
            this.tabSecurity.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabSecurity.Size = new System.Drawing.Size( 651, 408 );
            this.tabSecurity.TabIndex = 7;
            this.tabSecurity.Text = "Security";
            this.tabSecurity.UseVisualStyleBackColor = true;
            // 
            // gAntigrief
            // 
            this.gAntigrief.Controls.Add( this.lSpamBlockRate );
            this.gAntigrief.Controls.Add( this.nSpamBlockCount );
            this.gAntigrief.Controls.Add( this.lSpamBlocks );
            this.gAntigrief.Controls.Add( this.nSpamBlockTimer );
            this.gAntigrief.Controls.Add( this.lSpamBlockSeconds );
            this.gAntigrief.Location = new System.Drawing.Point( 8, 321 );
            this.gAntigrief.Name = "gAntigrief";
            this.gAntigrief.Size = new System.Drawing.Size( 635, 63 );
            this.gAntigrief.TabIndex = 21;
            this.gAntigrief.TabStop = false;
            this.gAntigrief.Text = "Grief Bot / Autoclicker Prevension";
            // 
            // lSpamBlockRate
            // 
            this.lSpamBlockRate.AutoSize = true;
            this.lSpamBlockRate.Location = new System.Drawing.Point( 6, 31 );
            this.lSpamBlockRate.Name = "lSpamBlockRate";
            this.lSpamBlockRate.Size = new System.Drawing.Size( 131, 15 );
            this.lSpamBlockRate.TabIndex = 5;
            this.lSpamBlockRate.Text = "Limit build / delete rate";
            // 
            // nSpamBlockCount
            // 
            this.nSpamBlockCount.Location = new System.Drawing.Point( 143, 29 );
            this.nSpamBlockCount.Maximum = new decimal( new int[] {
            500,
            0,
            0,
            0} );
            this.nSpamBlockCount.Minimum = new decimal( new int[] {
            2,
            0,
            0,
            0} );
            this.nSpamBlockCount.Name = "nSpamBlockCount";
            this.nSpamBlockCount.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamBlockCount.TabIndex = 6;
            this.nSpamBlockCount.Value = new decimal( new int[] {
            2,
            0,
            0,
            0} );
            // 
            // lSpamBlocks
            // 
            this.lSpamBlocks.AutoSize = true;
            this.lSpamBlocks.Location = new System.Drawing.Point( 209, 31 );
            this.lSpamBlocks.Name = "lSpamBlocks";
            this.lSpamBlocks.Size = new System.Drawing.Size( 55, 15 );
            this.lSpamBlocks.TabIndex = 7;
            this.lSpamBlocks.Text = "blocks in";
            // 
            // nSpamBlockTimer
            // 
            this.nSpamBlockTimer.Location = new System.Drawing.Point( 294, 29 );
            this.nSpamBlockTimer.Maximum = new decimal( new int[] {
            50,
            0,
            0,
            0} );
            this.nSpamBlockTimer.Minimum = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            this.nSpamBlockTimer.Name = "nSpamBlockTimer";
            this.nSpamBlockTimer.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamBlockTimer.TabIndex = 8;
            this.nSpamBlockTimer.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lSpamBlockSeconds
            // 
            this.lSpamBlockSeconds.AutoSize = true;
            this.lSpamBlockSeconds.Location = new System.Drawing.Point( 362, 31 );
            this.lSpamBlockSeconds.Name = "lSpamBlockSeconds";
            this.lSpamBlockSeconds.Size = new System.Drawing.Size( 53, 15 );
            this.lSpamBlockSeconds.TabIndex = 9;
            this.lSpamBlockSeconds.Text = "seconds";
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
            this.gSpamChat.Location = new System.Drawing.Point( 8, 221 );
            this.gSpamChat.Name = "gSpamChat";
            this.gSpamChat.Size = new System.Drawing.Size( 635, 94 );
            this.gSpamChat.TabIndex = 20;
            this.gSpamChat.TabStop = false;
            this.gSpamChat.Text = "Chat Spam Prevention";
            // 
            // lSpamChatWarnings
            // 
            this.lSpamChatWarnings.AutoSize = true;
            this.lSpamChatWarnings.Location = new System.Drawing.Point( 444, 60 );
            this.lSpamChatWarnings.Name = "lSpamChatWarnings";
            this.lSpamChatWarnings.Size = new System.Drawing.Size( 57, 15 );
            this.lSpamChatWarnings.TabIndex = 12;
            this.lSpamChatWarnings.Text = "warnings";
            // 
            // nSpamChatWarnings
            // 
            this.nSpamChatWarnings.Location = new System.Drawing.Point( 376, 58 );
            this.nSpamChatWarnings.Name = "nSpamChatWarnings";
            this.nSpamChatWarnings.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamChatWarnings.TabIndex = 11;
            // 
            // xSpamChatKick
            // 
            this.xSpamChatKick.AutoSize = true;
            this.xSpamChatKick.Location = new System.Drawing.Point( 294, 59 );
            this.xSpamChatKick.Name = "xSpamChatKick";
            this.xSpamChatKick.Size = new System.Drawing.Size( 76, 19 );
            this.xSpamChatKick.TabIndex = 10;
            this.xSpamChatKick.Text = "Kick after";
            this.xSpamChatKick.UseVisualStyleBackColor = true;
            this.xSpamChatKick.CheckedChanged += new System.EventHandler( this.xSpamChatKick_CheckedChanged );
            // 
            // lSpamMuteSeconds
            // 
            this.lSpamMuteSeconds.AutoSize = true;
            this.lSpamMuteSeconds.Location = new System.Drawing.Point( 211, 60 );
            this.lSpamMuteSeconds.Name = "lSpamMuteSeconds";
            this.lSpamMuteSeconds.Size = new System.Drawing.Size( 53, 15 );
            this.lSpamMuteSeconds.TabIndex = 9;
            this.lSpamMuteSeconds.Text = "seconds";
            // 
            // lSpamChatSeconds
            // 
            this.lSpamChatSeconds.AutoSize = true;
            this.lSpamChatSeconds.Location = new System.Drawing.Point( 362, 25 );
            this.lSpamChatSeconds.Name = "lSpamChatSeconds";
            this.lSpamChatSeconds.Size = new System.Drawing.Size( 53, 15 );
            this.lSpamChatSeconds.TabIndex = 4;
            this.lSpamChatSeconds.Text = "seconds";
            // 
            // nSpamMute
            // 
            this.nSpamMute.Location = new System.Drawing.Point( 143, 57 );
            this.nSpamMute.Name = "nSpamMute";
            this.nSpamMute.Size = new System.Drawing.Size( 62, 21 );
            this.nSpamMute.TabIndex = 8;
            // 
            // lSpamMute
            // 
            this.lSpamMute.AutoSize = true;
            this.lSpamMute.Location = new System.Drawing.Point( 29, 60 );
            this.lSpamMute.Name = "lSpamMute";
            this.lSpamMute.Size = new System.Drawing.Size( 108, 15 );
            this.lSpamMute.TabIndex = 7;
            this.lSpamMute.Text = "Mute spammer for";
            // 
            // nSpamChatTimer
            // 
            this.nSpamChatTimer.Location = new System.Drawing.Point( 294, 23 );
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
            this.nSpamChatTimer.TabIndex = 3;
            this.nSpamChatTimer.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lSpamChatMessages
            // 
            this.lSpamChatMessages.AutoSize = true;
            this.lSpamChatMessages.Location = new System.Drawing.Point( 209, 25 );
            this.lSpamChatMessages.Name = "lSpamChatMessages";
            this.lSpamChatMessages.Size = new System.Drawing.Size( 77, 15 );
            this.lSpamChatMessages.TabIndex = 2;
            this.lSpamChatMessages.Text = "messages in";
            // 
            // nSpamChatCount
            // 
            this.nSpamChatCount.Location = new System.Drawing.Point( 143, 23 );
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
            this.nSpamChatCount.TabIndex = 1;
            this.nSpamChatCount.Value = new decimal( new int[] {
            2,
            0,
            0,
            0} );
            // 
            // lSpamChat
            // 
            this.lSpamChat.AutoSize = true;
            this.lSpamChat.Location = new System.Drawing.Point( 53, 25 );
            this.lSpamChat.Name = "lSpamChat";
            this.lSpamChat.Size = new System.Drawing.Size( 84, 15 );
            this.lSpamChat.TabIndex = 0;
            this.lSpamChat.Text = "Limit chat rate";
            // 
            // gHackingDetection
            // 
            this.gHackingDetection.Controls.Add( this.label11 );
            this.gHackingDetection.Controls.Add( this.numericUpDown7 );
            this.gHackingDetection.Controls.Add( this.label10 );
            this.gHackingDetection.Controls.Add( this.checkBox4 );
            this.gHackingDetection.Controls.Add( this.checkBox3 );
            this.gHackingDetection.Controls.Add( this.checkBox2 );
            this.gHackingDetection.Controls.Add( this.checkBox1 );
            this.gHackingDetection.Enabled = false;
            this.gHackingDetection.Location = new System.Drawing.Point( 8, 98 );
            this.gHackingDetection.Name = "gHackingDetection";
            this.gHackingDetection.Size = new System.Drawing.Size( 635, 117 );
            this.gHackingDetection.TabIndex = 19;
            this.gHackingDetection.TabStop = false;
            this.gHackingDetection.Text = "Hacking Detection";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point( 209, 84 );
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size( 57, 15 );
            this.label11.TabIndex = 6;
            this.label11.Text = "warnings";
            // 
            // numericUpDown7
            // 
            this.numericUpDown7.Location = new System.Drawing.Point( 143, 82 );
            this.numericUpDown7.Name = "numericUpDown7";
            this.numericUpDown7.Size = new System.Drawing.Size( 62, 21 );
            this.numericUpDown7.TabIndex = 5;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point( 80, 84 );
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size( 57, 15 );
            this.label10.TabIndex = 4;
            this.label10.Text = "Kick after";
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point( 294, 32 );
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size( 229, 19 );
            this.checkBox4.TabIndex = 3;
            this.checkBox4.Text = "Allow players to affect far-away blocks";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point( 294, 57 );
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size( 235, 19 );
            this.checkBox3.TabIndex = 2;
            this.checkBox3.Text = "Allow players to leave map boundaries";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point( 143, 57 );
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size( 91, 19 );
            this.checkBox2.TabIndex = 1;
            this.checkBox2.Text = "Allow noclip";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point( 143, 32 );
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size( 109, 19 );
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "Allow speeding";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // gVerify
            // 
            this.gVerify.Controls.Add( this.xLimitOneConnectionPerIP );
            this.gVerify.Controls.Add( this.xAnnounceUnverified );
            this.gVerify.Controls.Add( this.lVerifyNames );
            this.gVerify.Controls.Add( this.cVerifyNames );
            this.gVerify.Location = new System.Drawing.Point( 8, 13 );
            this.gVerify.Name = "gVerify";
            this.gVerify.Size = new System.Drawing.Size( 635, 79 );
            this.gVerify.TabIndex = 18;
            this.gVerify.TabStop = false;
            this.gVerify.Text = "Name Verification";
            // 
            // xLimitOneConnectionPerIP
            // 
            this.xLimitOneConnectionPerIP.AutoSize = true;
            this.xLimitOneConnectionPerIP.Location = new System.Drawing.Point( 294, 47 );
            this.xLimitOneConnectionPerIP.Name = "xLimitOneConnectionPerIP";
            this.xLimitOneConnectionPerIP.Size = new System.Drawing.Size( 161, 19 );
            this.xLimitOneConnectionPerIP.TabIndex = 20;
            this.xLimitOneConnectionPerIP.Text = "Limit 1 connection per IP";
            this.xLimitOneConnectionPerIP.UseVisualStyleBackColor = true;
            // 
            // xAnnounceUnverified
            // 
            this.xAnnounceUnverified.AutoSize = true;
            this.xAnnounceUnverified.Location = new System.Drawing.Point( 294, 22 );
            this.xAnnounceUnverified.Name = "xAnnounceUnverified";
            this.xAnnounceUnverified.Size = new System.Drawing.Size( 264, 19 );
            this.xAnnounceUnverified.TabIndex = 19;
            this.xAnnounceUnverified.Text = "Announce unverified name warnings in chat";
            this.xAnnounceUnverified.UseVisualStyleBackColor = true;
            // 
            // lVerifyNames
            // 
            this.lVerifyNames.AutoSize = true;
            this.lVerifyNames.Location = new System.Drawing.Point( 35, 23 );
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
            this.cVerifyNames.Location = new System.Drawing.Point( 143, 20 );
            this.cVerifyNames.Name = "cVerifyNames";
            this.cVerifyNames.Size = new System.Drawing.Size( 100, 23 );
            this.cVerifyNames.TabIndex = 17;
            // 
            // tabSavingAndBackup
            // 
            this.tabSavingAndBackup.Controls.Add( this.gSaving );
            this.tabSavingAndBackup.Controls.Add( this.gBackups );
            this.tabSavingAndBackup.Location = new System.Drawing.Point( 4, 24 );
            this.tabSavingAndBackup.Name = "tabSavingAndBackup";
            this.tabSavingAndBackup.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabSavingAndBackup.Size = new System.Drawing.Size( 651, 408 );
            this.tabSavingAndBackup.TabIndex = 4;
            this.tabSavingAndBackup.Text = "Saving and Backup";
            this.tabSavingAndBackup.UseVisualStyleBackColor = true;
            // 
            // gSaving
            // 
            this.gSaving.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gSaving.Controls.Add( this.xSaveOnShutdown );
            this.gSaving.Controls.Add( this.nSaveInterval );
            this.gSaving.Controls.Add( this.nSaveIntervalUnits );
            this.gSaving.Controls.Add( this.xSaveAtInterval );
            this.gSaving.Location = new System.Drawing.Point( 8, 13 );
            this.gSaving.Name = "gSaving";
            this.gSaving.Size = new System.Drawing.Size( 635, 77 );
            this.gSaving.TabIndex = 5;
            this.gSaving.TabStop = false;
            this.gSaving.Text = "Saving";
            // 
            // xSaveOnShutdown
            // 
            this.xSaveOnShutdown.AutoSize = true;
            this.xSaveOnShutdown.Location = new System.Drawing.Point( 16, 20 );
            this.xSaveOnShutdown.Name = "xSaveOnShutdown";
            this.xSaveOnShutdown.Size = new System.Drawing.Size( 154, 19 );
            this.xSaveOnShutdown.TabIndex = 0;
            this.xSaveOnShutdown.Text = "Save map on shutdown";
            this.xSaveOnShutdown.UseVisualStyleBackColor = true;
            // 
            // nSaveInterval
            // 
            this.nSaveInterval.Location = new System.Drawing.Point( 134, 44 );
            this.nSaveInterval.Name = "nSaveInterval";
            this.nSaveInterval.Size = new System.Drawing.Size( 48, 21 );
            this.nSaveInterval.TabIndex = 2;
            // 
            // nSaveIntervalUnits
            // 
            this.nSaveIntervalUnits.AutoSize = true;
            this.nSaveIntervalUnits.Location = new System.Drawing.Point( 188, 46 );
            this.nSaveIntervalUnits.Name = "nSaveIntervalUnits";
            this.nSaveIntervalUnits.Size = new System.Drawing.Size( 53, 15 );
            this.nSaveIntervalUnits.TabIndex = 3;
            this.nSaveIntervalUnits.Text = "seconds";
            // 
            // xSaveAtInterval
            // 
            this.xSaveAtInterval.AutoSize = true;
            this.xSaveAtInterval.Location = new System.Drawing.Point( 16, 45 );
            this.xSaveAtInterval.Name = "xSaveAtInterval";
            this.xSaveAtInterval.Size = new System.Drawing.Size( 112, 19 );
            this.xSaveAtInterval.TabIndex = 1;
            this.xSaveAtInterval.Text = "Save map every";
            this.xSaveAtInterval.UseVisualStyleBackColor = true;
            this.xSaveAtInterval.CheckedChanged += new System.EventHandler( this.xSaveAtInterval_CheckedChanged );
            // 
            // gBackups
            // 
            this.gBackups.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
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
            this.gBackups.Controls.Add( this.xBackupAtInterval );
            this.gBackups.Controls.Add( this.xBackupOnJoin );
            this.gBackups.Location = new System.Drawing.Point( 8, 96 );
            this.gBackups.Name = "gBackups";
            this.gBackups.Size = new System.Drawing.Size( 635, 158 );
            this.gBackups.TabIndex = 4;
            this.gBackups.TabStop = false;
            this.gBackups.Text = "Backups";
            // 
            // xBackupOnlyWhenChanged
            // 
            this.xBackupOnlyWhenChanged.AutoSize = true;
            this.xBackupOnlyWhenChanged.Location = new System.Drawing.Point( 366, 47 );
            this.xBackupOnlyWhenChanged.Name = "xBackupOnlyWhenChanged";
            this.xBackupOnlyWhenChanged.Size = new System.Drawing.Size( 260, 19 );
            this.xBackupOnlyWhenChanged.TabIndex = 14;
            this.xBackupOnlyWhenChanged.Text = "Skip timed backups if map hasn\'t changed.";
            this.xBackupOnlyWhenChanged.UseVisualStyleBackColor = true;
            // 
            // lMaxBackupSize
            // 
            this.lMaxBackupSize.AutoSize = true;
            this.lMaxBackupSize.Location = new System.Drawing.Point( 381, 127 );
            this.lMaxBackupSize.Name = "lMaxBackupSize";
            this.lMaxBackupSize.Size = new System.Drawing.Size( 103, 15 );
            this.lMaxBackupSize.TabIndex = 13;
            this.lMaxBackupSize.Text = "MB of disk space.";
            // 
            // xMaxBackupSize
            // 
            this.xMaxBackupSize.AutoSize = true;
            this.xMaxBackupSize.Location = new System.Drawing.Point( 16, 126 );
            this.xMaxBackupSize.Name = "xMaxBackupSize";
            this.xMaxBackupSize.Size = new System.Drawing.Size( 302, 19 );
            this.xMaxBackupSize.TabIndex = 11;
            this.xMaxBackupSize.Text = "Delete old backups if the folder takes up more than";
            this.xMaxBackupSize.UseVisualStyleBackColor = true;
            this.xMaxBackupSize.CheckedChanged += new System.EventHandler( this.xMaxBackupSize_CheckedChanged );
            // 
            // nMaxBackupSize
            // 
            this.nMaxBackupSize.Location = new System.Drawing.Point( 324, 125 );
            this.nMaxBackupSize.Name = "nMaxBackupSize";
            this.nMaxBackupSize.Size = new System.Drawing.Size( 51, 21 );
            this.nMaxBackupSize.TabIndex = 12;
            // 
            // xMaxBackups
            // 
            this.xMaxBackups.AutoSize = true;
            this.xMaxBackups.Location = new System.Drawing.Point( 16, 98 );
            this.xMaxBackups.Name = "xMaxBackups";
            this.xMaxBackups.Size = new System.Drawing.Size( 251, 19 );
            this.xMaxBackups.TabIndex = 7;
            this.xMaxBackups.Text = "Delete old backups if there are more than";
            this.xMaxBackups.UseVisualStyleBackColor = true;
            this.xMaxBackups.CheckedChanged += new System.EventHandler( this.xMaxBackups_CheckedChanged );
            // 
            // xBackupOnStartup
            // 
            this.xBackupOnStartup.AutoSize = true;
            this.xBackupOnStartup.Location = new System.Drawing.Point( 16, 20 );
            this.xBackupOnStartup.Name = "xBackupOnStartup";
            this.xBackupOnStartup.Size = new System.Drawing.Size( 162, 19 );
            this.xBackupOnStartup.TabIndex = 6;
            this.xBackupOnStartup.Text = "Create backup on startup";
            this.xBackupOnStartup.UseVisualStyleBackColor = true;
            // 
            // lMaxBackups
            // 
            this.lMaxBackups.AutoSize = true;
            this.lMaxBackups.Location = new System.Drawing.Point( 330, 99 );
            this.lMaxBackups.Name = "lMaxBackups";
            this.lMaxBackups.Size = new System.Drawing.Size( 142, 15 );
            this.lMaxBackups.TabIndex = 10;
            this.lMaxBackups.Text = "files in the backup folder.";
            // 
            // nMaxBackups
            // 
            this.nMaxBackups.Location = new System.Drawing.Point( 273, 97 );
            this.nMaxBackups.Name = "nMaxBackups";
            this.nMaxBackups.Size = new System.Drawing.Size( 51, 21 );
            this.nMaxBackups.TabIndex = 9;
            // 
            // nBackupInterval
            // 
            this.nBackupInterval.Location = new System.Drawing.Point( 158, 45 );
            this.nBackupInterval.Name = "nBackupInterval";
            this.nBackupInterval.Size = new System.Drawing.Size( 48, 21 );
            this.nBackupInterval.TabIndex = 4;
            // 
            // lBackupIntervalUnits
            // 
            this.lBackupIntervalUnits.AutoSize = true;
            this.lBackupIntervalUnits.Location = new System.Drawing.Point( 212, 47 );
            this.lBackupIntervalUnits.Name = "lBackupIntervalUnits";
            this.lBackupIntervalUnits.Size = new System.Drawing.Size( 51, 15 );
            this.lBackupIntervalUnits.TabIndex = 5;
            this.lBackupIntervalUnits.Text = "minutes";
            // 
            // xBackupAtInterval
            // 
            this.xBackupAtInterval.AutoSize = true;
            this.xBackupAtInterval.Location = new System.Drawing.Point( 16, 46 );
            this.xBackupAtInterval.Name = "xBackupAtInterval";
            this.xBackupAtInterval.Size = new System.Drawing.Size( 136, 19 );
            this.xBackupAtInterval.TabIndex = 1;
            this.xBackupAtInterval.Text = "Create backup every";
            this.xBackupAtInterval.UseVisualStyleBackColor = true;
            this.xBackupAtInterval.CheckedChanged += new System.EventHandler( this.xBackupAtInterval_CheckedChanged );
            // 
            // xBackupOnJoin
            // 
            this.xBackupOnJoin.AutoSize = true;
            this.xBackupOnJoin.Location = new System.Drawing.Point( 16, 72 );
            this.xBackupOnJoin.Name = "xBackupOnJoin";
            this.xBackupOnJoin.Size = new System.Drawing.Size( 236, 19 );
            this.xBackupOnJoin.TabIndex = 0;
            this.xBackupOnJoin.Text = "Create backup whenever a player joins";
            this.xBackupOnJoin.UseVisualStyleBackColor = true;
            // 
            // tabLogging
            // 
            this.tabLogging.Controls.Add( this.gLogFile );
            this.tabLogging.Controls.Add( this.gConsole );
            this.tabLogging.Location = new System.Drawing.Point( 4, 24 );
            this.tabLogging.Name = "tabLogging";
            this.tabLogging.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabLogging.Size = new System.Drawing.Size( 651, 408 );
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
            this.gLogFile.Size = new System.Drawing.Size( 314, 340 );
            this.gLogFile.TabIndex = 1;
            this.gLogFile.TabStop = false;
            this.gLogFile.Text = "Log File";
            // 
            // xLogLimit
            // 
            this.xLogLimit.AutoSize = true;
            this.xLogLimit.Enabled = false;
            this.xLogLimit.Location = new System.Drawing.Point( 34, 306 );
            this.xLogLimit.Name = "xLogLimit";
            this.xLogLimit.Size = new System.Drawing.Size( 102, 19 );
            this.xLogLimit.TabIndex = 7;
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
            listViewItem1.StateImageIndex = 0;
            listViewItem2.StateImageIndex = 0;
            listViewItem3.StateImageIndex = 0;
            listViewItem4.StateImageIndex = 0;
            listViewItem5.StateImageIndex = 0;
            listViewItem6.StateImageIndex = 0;
            listViewItem7.StateImageIndex = 0;
            listViewItem8.StateImageIndex = 0;
            listViewItem9.StateImageIndex = 0;
            listViewItem10.StateImageIndex = 0;
            listViewItem11.StateImageIndex = 0;
            listViewItem12.StateImageIndex = 0;
            listViewItem13.StateImageIndex = 0;
            this.vLogFileOptions.Items.AddRange( new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5,
            listViewItem6,
            listViewItem7,
            listViewItem8,
            listViewItem9,
            listViewItem10,
            listViewItem11,
            listViewItem12,
            listViewItem13} );
            this.vLogFileOptions.Location = new System.Drawing.Point( 100, 20 );
            this.vLogFileOptions.Name = "vLogFileOptions";
            this.vLogFileOptions.Size = new System.Drawing.Size( 161, 251 );
            this.vLogFileOptions.TabIndex = 5;
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
            this.lLogLimitUnits.Location = new System.Drawing.Point( 210, 307 );
            this.lLogLimitUnits.Name = "lLogLimitUnits";
            this.lLogLimitUnits.Size = new System.Drawing.Size( 29, 15 );
            this.lLogLimitUnits.TabIndex = 4;
            this.lLogLimitUnits.Text = "files";
            // 
            // nLogLimit
            // 
            this.nLogLimit.Enabled = false;
            this.nLogLimit.Location = new System.Drawing.Point( 142, 305 );
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
            this.cLogMode.Enabled = false;
            this.cLogMode.FormattingEnabled = true;
            this.cLogMode.Items.AddRange( new object[] {
            "None",
            "One long file",
            "Multiple files, split by session",
            "Multiple files, split by day"} );
            this.cLogMode.Location = new System.Drawing.Point( 100, 277 );
            this.cLogMode.Name = "cLogMode";
            this.cLogMode.Size = new System.Drawing.Size( 199, 23 );
            this.cLogMode.TabIndex = 1;
            // 
            // lLogMode
            // 
            this.lLogMode.AutoSize = true;
            this.lLogMode.Enabled = false;
            this.lLogMode.Location = new System.Drawing.Point( 31, 280 );
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
            this.gConsole.Size = new System.Drawing.Size( 314, 340 );
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
            listViewItem14.StateImageIndex = 0;
            listViewItem15.StateImageIndex = 0;
            listViewItem16.StateImageIndex = 0;
            listViewItem17.StateImageIndex = 0;
            listViewItem18.StateImageIndex = 0;
            listViewItem19.StateImageIndex = 0;
            listViewItem20.StateImageIndex = 0;
            listViewItem21.StateImageIndex = 0;
            listViewItem22.StateImageIndex = 0;
            listViewItem23.StateImageIndex = 0;
            listViewItem24.StateImageIndex = 0;
            listViewItem25.StateImageIndex = 0;
            listViewItem26.StateImageIndex = 0;
            this.vConsoleOptions.Items.AddRange( new System.Windows.Forms.ListViewItem[] {
            listViewItem14,
            listViewItem15,
            listViewItem16,
            listViewItem17,
            listViewItem18,
            listViewItem19,
            listViewItem20,
            listViewItem21,
            listViewItem22,
            listViewItem23,
            listViewItem24,
            listViewItem25,
            listViewItem26} );
            this.vConsoleOptions.Location = new System.Drawing.Point( 88, 20 );
            this.vConsoleOptions.Name = "vConsoleOptions";
            this.vConsoleOptions.Size = new System.Drawing.Size( 161, 251 );
            this.vConsoleOptions.TabIndex = 7;
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
            this.tabIRC.Size = new System.Drawing.Size( 651, 408 );
            this.tabIRC.TabIndex = 8;
            this.tabIRC.Text = "IRC";
            this.tabIRC.UseVisualStyleBackColor = true;
            // 
            // gIRCOptions
            // 
            this.gIRCOptions.Controls.Add( this.xIRCBotForwardFromIRC );
            this.gIRCOptions.Controls.Add( this.xIRCMsgs );
            this.gIRCOptions.Controls.Add( this.xIRCBotForwardFromServer );
            this.gIRCOptions.Location = new System.Drawing.Point( 8, 179 );
            this.gIRCOptions.Name = "gIRCOptions";
            this.gIRCOptions.Size = new System.Drawing.Size( 635, 96 );
            this.gIRCOptions.TabIndex = 7;
            this.gIRCOptions.TabStop = false;
            this.gIRCOptions.Text = "Options";
            // 
            // xIRCBotForwardFromIRC
            // 
            this.xIRCBotForwardFromIRC.AutoSize = true;
            this.xIRCBotForwardFromIRC.Location = new System.Drawing.Point( 21, 70 );
            this.xIRCBotForwardFromIRC.Name = "xIRCBotForwardFromIRC";
            this.xIRCBotForwardFromIRC.Size = new System.Drawing.Size( 240, 19 );
            this.xIRCBotForwardFromIRC.TabIndex = 8;
            this.xIRCBotForwardFromIRC.Text = "Forward ALL chat from IRC to SERVER.";
            this.xIRCBotForwardFromIRC.UseVisualStyleBackColor = true;
            // 
            // xIRCMsgs
            // 
            this.xIRCMsgs.AutoSize = true;
            this.xIRCMsgs.Location = new System.Drawing.Point( 21, 20 );
            this.xIRCMsgs.Name = "xIRCMsgs";
            this.xIRCMsgs.Size = new System.Drawing.Size( 350, 19 );
            this.xIRCMsgs.TabIndex = 1;
            this.xIRCMsgs.Text = "Announce in-game when people join/part the IRC channels.";
            this.xIRCMsgs.UseVisualStyleBackColor = true;
            // 
            // xIRCBotForwardFromServer
            // 
            this.xIRCBotForwardFromServer.AutoSize = true;
            this.xIRCBotForwardFromServer.Location = new System.Drawing.Point( 21, 45 );
            this.xIRCBotForwardFromServer.Name = "xIRCBotForwardFromServer";
            this.xIRCBotForwardFromServer.Size = new System.Drawing.Size( 240, 19 );
            this.xIRCBotForwardFromServer.TabIndex = 7;
            this.xIRCBotForwardFromServer.Text = "Forward ALL chat from SERVER to IRC.";
            this.xIRCBotForwardFromServer.UseVisualStyleBackColor = true;
            // 
            // gIRCNetwork
            // 
            this.gIRCNetwork.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gIRCNetwork.Controls.Add( this.lIRCBotQuitMsg );
            this.gIRCNetwork.Controls.Add( this.tIRCBotQuitMsg );
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
            this.gIRCNetwork.Size = new System.Drawing.Size( 635, 135 );
            this.gIRCNetwork.TabIndex = 5;
            this.gIRCNetwork.TabStop = false;
            this.gIRCNetwork.Text = "Network";
            // 
            // lIRCBotQuitMsg
            // 
            this.lIRCBotQuitMsg.AutoSize = true;
            this.lIRCBotQuitMsg.Location = new System.Drawing.Point( 296, 102 );
            this.lIRCBotQuitMsg.Name = "lIRCBotQuitMsg";
            this.lIRCBotQuitMsg.Size = new System.Drawing.Size( 126, 15 );
            this.lIRCBotQuitMsg.TabIndex = 19;
            this.lIRCBotQuitMsg.Text = "Custom quit message";
            // 
            // tIRCBotQuitMsg
            // 
            this.tIRCBotQuitMsg.Location = new System.Drawing.Point( 428, 99 );
            this.tIRCBotQuitMsg.MaxLength = 32;
            this.tIRCBotQuitMsg.Name = "tIRCBotQuitMsg";
            this.tIRCBotQuitMsg.Size = new System.Drawing.Size( 193, 21 );
            this.tIRCBotQuitMsg.TabIndex = 18;
            // 
            // lIRCBotChannels2
            // 
            this.lIRCBotChannels2.AutoSize = true;
            this.lIRCBotChannels2.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.lIRCBotChannels2.Location = new System.Drawing.Point( 27, 65 );
            this.lIRCBotChannels2.Name = "lIRCBotChannels2";
            this.lIRCBotChannels2.Size = new System.Drawing.Size( 97, 13 );
            this.lIRCBotChannels2.TabIndex = 17;
            this.lIRCBotChannels2.Text = "(comma seperated)";
            // 
            // lIRCBotChannels3
            // 
            this.lIRCBotChannels3.AutoSize = true;
            this.lIRCBotChannels3.Location = new System.Drawing.Point( 130, 71 );
            this.lIRCBotChannels3.Name = "lIRCBotChannels3";
            this.lIRCBotChannels3.Size = new System.Drawing.Size( 237, 15 );
            this.lIRCBotChannels3.TabIndex = 16;
            this.lIRCBotChannels3.Text = "NOTE: Channel names are case-sensitive!";
            // 
            // tIRCBotChannels
            // 
            this.tIRCBotChannels.Location = new System.Drawing.Point( 130, 47 );
            this.tIRCBotChannels.MaxLength = 1000;
            this.tIRCBotChannels.Name = "tIRCBotChannels";
            this.tIRCBotChannels.Size = new System.Drawing.Size( 491, 21 );
            this.tIRCBotChannels.TabIndex = 15;
            // 
            // lIRCBotChannels
            // 
            this.lIRCBotChannels.AutoSize = true;
            this.lIRCBotChannels.Location = new System.Drawing.Point( 29, 50 );
            this.lIRCBotChannels.Name = "lIRCBotChannels";
            this.lIRCBotChannels.Size = new System.Drawing.Size( 95, 15 );
            this.lIRCBotChannels.TabIndex = 14;
            this.lIRCBotChannels.Text = "Channels to join";
            // 
            // nIRCBotPort
            // 
            this.nIRCBotPort.Location = new System.Drawing.Point( 331, 20 );
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
            this.nIRCBotPort.Size = new System.Drawing.Size( 70, 21 );
            this.nIRCBotPort.TabIndex = 13;
            this.nIRCBotPort.Value = new decimal( new int[] {
            1,
            0,
            0,
            0} );
            // 
            // lIRCBotPort
            // 
            this.lIRCBotPort.AutoSize = true;
            this.lIRCBotPort.Location = new System.Drawing.Point( 296, 23 );
            this.lIRCBotPort.Name = "lIRCBotPort";
            this.lIRCBotPort.Size = new System.Drawing.Size( 29, 15 );
            this.lIRCBotPort.TabIndex = 12;
            this.lIRCBotPort.Text = "Port";
            // 
            // tIRCBotNetwork
            // 
            this.tIRCBotNetwork.Location = new System.Drawing.Point( 130, 20 );
            this.tIRCBotNetwork.MaxLength = 512;
            this.tIRCBotNetwork.Name = "tIRCBotNetwork";
            this.tIRCBotNetwork.Size = new System.Drawing.Size( 160, 21 );
            this.tIRCBotNetwork.TabIndex = 11;
            // 
            // lIRCBotNetwork
            // 
            this.lIRCBotNetwork.AutoSize = true;
            this.lIRCBotNetwork.Location = new System.Drawing.Point( 35, 22 );
            this.lIRCBotNetwork.Name = "lIRCBotNetwork";
            this.lIRCBotNetwork.Size = new System.Drawing.Size( 89, 15 );
            this.lIRCBotNetwork.TabIndex = 10;
            this.lIRCBotNetwork.Text = "IRC server host";
            // 
            // lIRCBotNick
            // 
            this.lIRCBotNick.AutoSize = true;
            this.lIRCBotNick.Location = new System.Drawing.Point( 74, 102 );
            this.lIRCBotNick.Name = "lIRCBotNick";
            this.lIRCBotNick.Size = new System.Drawing.Size( 50, 15 );
            this.lIRCBotNick.TabIndex = 9;
            this.lIRCBotNick.Text = "Bot nick";
            // 
            // tIRCBotNick
            // 
            this.tIRCBotNick.Location = new System.Drawing.Point( 130, 99 );
            this.tIRCBotNick.MaxLength = 32;
            this.tIRCBotNick.Name = "tIRCBotNick";
            this.tIRCBotNick.Size = new System.Drawing.Size( 160, 21 );
            this.tIRCBotNick.TabIndex = 8;
            // 
            // xIRC
            // 
            this.xIRC.AutoSize = true;
            this.xIRC.Location = new System.Drawing.Point( 14, 13 );
            this.xIRC.Name = "xIRC";
            this.xIRC.Size = new System.Drawing.Size( 149, 19 );
            this.xIRC.TabIndex = 6;
            this.xIRC.Text = "Enable IRC integration";
            this.xIRC.UseVisualStyleBackColor = true;
            this.xIRC.CheckedChanged += new System.EventHandler( this.xIRC_CheckedChanged );
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.Controls.Add( this.xLowLatencyMode );
            this.tabAdvanced.Controls.Add( this.cUpdater );
            this.tabAdvanced.Controls.Add( this.bUpdater );
            this.tabAdvanced.Controls.Add( this.lThrottlingUnits );
            this.tabAdvanced.Controls.Add( this.nThrottling );
            this.tabAdvanced.Controls.Add( this.lThrottling );
            this.tabAdvanced.Controls.Add( this.lPing );
            this.tabAdvanced.Controls.Add( this.nPing );
            this.tabAdvanced.Controls.Add( this.xAbsoluteUpdates );
            this.tabAdvanced.Controls.Add( this.xPing );
            this.tabAdvanced.Controls.Add( this.cStartup );
            this.tabAdvanced.Controls.Add( this.lStartup );
            this.tabAdvanced.Controls.Add( this.cProcessPriority );
            this.tabAdvanced.Controls.Add( this.lProcessPriority );
            this.tabAdvanced.Controls.Add( this.cPolicyIllegal );
            this.tabAdvanced.Controls.Add( this.xRedundantPacket );
            this.tabAdvanced.Controls.Add( this.lPolicyColor );
            this.tabAdvanced.Controls.Add( this.lPolicyIllegal );
            this.tabAdvanced.Controls.Add( this.cPolicyColor );
            this.tabAdvanced.Controls.Add( this.lTickIntervalUnits );
            this.tabAdvanced.Controls.Add( this.nTickInterval );
            this.tabAdvanced.Controls.Add( this.lTickInterval );
            this.tabAdvanced.Controls.Add( this.lAdvancedWarning );
            this.tabAdvanced.Location = new System.Drawing.Point( 4, 24 );
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Padding = new System.Windows.Forms.Padding( 5, 10, 5, 10 );
            this.tabAdvanced.Size = new System.Drawing.Size( 651, 408 );
            this.tabAdvanced.TabIndex = 6;
            this.tabAdvanced.Text = "Advanced";
            this.tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // xLowLatencyMode
            // 
            this.xLowLatencyMode.AutoSize = true;
            this.xLowLatencyMode.Location = new System.Drawing.Point( 11, 376 );
            this.xLowLatencyMode.Name = "xLowLatencyMode";
            this.xLowLatencyMode.Size = new System.Drawing.Size( 613, 19 );
            this.xLowLatencyMode.TabIndex = 40;
            this.xLowLatencyMode.Text = "Experimental low-latency mode (disables Nagle\'s alrorithm, reducing latency but i" +
                "ncreasing bandwidth use).";
            this.xLowLatencyMode.UseVisualStyleBackColor = true;
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
            this.cUpdater.Location = new System.Drawing.Point( 197, 276 );
            this.cUpdater.Name = "cUpdater";
            this.cUpdater.Size = new System.Drawing.Size( 200, 23 );
            this.cUpdater.TabIndex = 39;
            // 
            // bUpdater
            // 
            this.bUpdater.AutoSize = true;
            this.bUpdater.Location = new System.Drawing.Point( 55, 279 );
            this.bUpdater.Name = "bUpdater";
            this.bUpdater.Size = new System.Drawing.Size( 136, 15 );
            this.bUpdater.TabIndex = 38;
            this.bUpdater.Text = "Check for fCraft updates";
            // 
            // lThrottlingUnits
            // 
            this.lThrottlingUnits.AutoSize = true;
            this.lThrottlingUnits.Location = new System.Drawing.Point( 262, 311 );
            this.lThrottlingUnits.Name = "lThrottlingUnits";
            this.lThrottlingUnits.Size = new System.Drawing.Size( 129, 15 );
            this.lThrottlingUnits.TabIndex = 37;
            this.lThrottlingUnits.Text = "blocks / second / client";
            // 
            // nThrottling
            // 
            this.nThrottling.Increment = new decimal( new int[] {
            100,
            0,
            0,
            0} );
            this.nThrottling.Location = new System.Drawing.Point( 197, 309 );
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
            this.nThrottling.TabIndex = 36;
            this.nThrottling.Value = new decimal( new int[] {
            2500,
            0,
            0,
            0} );
            // 
            // lThrottling
            // 
            this.lThrottling.AutoSize = true;
            this.lThrottling.Location = new System.Drawing.Point( 63, 311 );
            this.lThrottling.Name = "lThrottling";
            this.lThrottling.Size = new System.Drawing.Size( 128, 15 );
            this.lThrottling.TabIndex = 35;
            this.lThrottling.Text = "Block update throttling";
            // 
            // lPing
            // 
            this.lPing.AutoSize = true;
            this.lPing.Enabled = false;
            this.lPing.Location = new System.Drawing.Point( 273, 159 );
            this.lPing.Name = "lPing";
            this.lPing.Size = new System.Drawing.Size( 123, 15 );
            this.lPing.TabIndex = 34;
            this.lPing.Text = "ms (vanilla behavior).";
            // 
            // nPing
            // 
            this.nPing.Enabled = false;
            this.nPing.Location = new System.Drawing.Point( 220, 157 );
            this.nPing.Name = "nPing";
            this.nPing.Size = new System.Drawing.Size( 47, 21 );
            this.nPing.TabIndex = 33;
            // 
            // xAbsoluteUpdates
            // 
            this.xAbsoluteUpdates.AutoSize = true;
            this.xAbsoluteUpdates.Enabled = false;
            this.xAbsoluteUpdates.Location = new System.Drawing.Point( 11, 184 );
            this.xAbsoluteUpdates.Name = "xAbsoluteUpdates";
            this.xAbsoluteUpdates.Size = new System.Drawing.Size( 326, 19 );
            this.xAbsoluteUpdates.TabIndex = 32;
            this.xAbsoluteUpdates.Text = "Do not use partial position updates (opcodes 9, 10, 11).";
            this.xAbsoluteUpdates.UseVisualStyleBackColor = true;
            // 
            // xPing
            // 
            this.xPing.AutoSize = true;
            this.xPing.Enabled = false;
            this.xPing.Location = new System.Drawing.Point( 11, 158 );
            this.xPing.Name = "xPing";
            this.xPing.Size = new System.Drawing.Size( 203, 19 );
            this.xPing.TabIndex = 31;
            this.xPing.Text = "Send useless ping packets every";
            this.xPing.UseVisualStyleBackColor = true;
            this.xPing.CheckedChanged += new System.EventHandler( this.xPing_CheckedChanged );
            // 
            // cStartup
            // 
            this.cStartup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cStartup.Enabled = false;
            this.cStartup.Items.AddRange( new object[] {
            "Always",
            "Only if computer didn\'t shut down properly",
            "Never"} );
            this.cStartup.Location = new System.Drawing.Point( 197, 247 );
            this.cStartup.Name = "cStartup";
            this.cStartup.Size = new System.Drawing.Size( 252, 23 );
            this.cStartup.TabIndex = 30;
            // 
            // lStartup
            // 
            this.lStartup.AutoSize = true;
            this.lStartup.Enabled = false;
            this.lStartup.Location = new System.Drawing.Point( 32, 250 );
            this.lStartup.Name = "lStartup";
            this.lStartup.Size = new System.Drawing.Size( 159, 15 );
            this.lStartup.TabIndex = 29;
            this.lStartup.Text = "Run fCraft on system startup";
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
            this.cProcessPriority.Location = new System.Drawing.Point( 197, 218 );
            this.cProcessPriority.Name = "cProcessPriority";
            this.cProcessPriority.Size = new System.Drawing.Size( 109, 23 );
            this.cProcessPriority.TabIndex = 24;
            // 
            // lProcessPriority
            // 
            this.lProcessPriority.AutoSize = true;
            this.lProcessPriority.Location = new System.Drawing.Point( 101, 221 );
            this.lProcessPriority.Name = "lProcessPriority";
            this.lProcessPriority.Size = new System.Drawing.Size( 90, 15 );
            this.lProcessPriority.TabIndex = 23;
            this.lProcessPriority.Text = "Process priority";
            // 
            // cPolicyIllegal
            // 
            this.cPolicyIllegal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPolicyIllegal.Enabled = false;
            this.cPolicyIllegal.FormattingEnabled = true;
            this.cPolicyIllegal.Items.AddRange( new object[] {
            "Disallow",
            "Allow from console only",
            "Allow"} );
            this.cPolicyIllegal.Location = new System.Drawing.Point( 197, 87 );
            this.cPolicyIllegal.Name = "cPolicyIllegal";
            this.cPolicyIllegal.Size = new System.Drawing.Size( 160, 23 );
            this.cPolicyIllegal.TabIndex = 21;
            // 
            // xRedundantPacket
            // 
            this.xRedundantPacket.AutoSize = true;
            this.xRedundantPacket.Enabled = false;
            this.xRedundantPacket.Location = new System.Drawing.Point( 11, 132 );
            this.xRedundantPacket.Name = "xRedundantPacket";
            this.xRedundantPacket.Size = new System.Drawing.Size( 554, 19 );
            this.xRedundantPacket.TabIndex = 22;
            this.xRedundantPacket.Text = "When a player changes a block, send him the redundant update packet anyway (vanil" +
                "la behavior).";
            this.xRedundantPacket.UseVisualStyleBackColor = true;
            // 
            // lPolicyColor
            // 
            this.lPolicyColor.AutoSize = true;
            this.lPolicyColor.Enabled = false;
            this.lPolicyColor.Location = new System.Drawing.Point( 30, 61 );
            this.lPolicyColor.Name = "lPolicyColor";
            this.lPolicyColor.Size = new System.Drawing.Size( 161, 15 );
            this.lPolicyColor.TabIndex = 18;
            this.lPolicyColor.Text = "Policy on color codes in chat";
            // 
            // lPolicyIllegal
            // 
            this.lPolicyIllegal.AutoSize = true;
            this.lPolicyIllegal.Enabled = false;
            this.lPolicyIllegal.Location = new System.Drawing.Point( 8, 90 );
            this.lPolicyIllegal.Name = "lPolicyIllegal";
            this.lPolicyIllegal.Size = new System.Drawing.Size( 183, 15 );
            this.lPolicyIllegal.TabIndex = 20;
            this.lPolicyIllegal.Text = "Policy on other illegal characters";
            // 
            // cPolicyColor
            // 
            this.cPolicyColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cPolicyColor.Enabled = false;
            this.cPolicyColor.FormattingEnabled = true;
            this.cPolicyColor.Items.AddRange( new object[] {
            "Disallow",
            "Allow from console only",
            "Allow"} );
            this.cPolicyColor.Location = new System.Drawing.Point( 197, 58 );
            this.cPolicyColor.Name = "cPolicyColor";
            this.cPolicyColor.Size = new System.Drawing.Size( 160, 23 );
            this.cPolicyColor.TabIndex = 19;
            // 
            // lTickIntervalUnits
            // 
            this.lTickIntervalUnits.AutoSize = true;
            this.lTickIntervalUnits.Location = new System.Drawing.Point( 262, 338 );
            this.lTickIntervalUnits.Name = "lTickIntervalUnits";
            this.lTickIntervalUnits.Size = new System.Drawing.Size( 24, 15 );
            this.lTickIntervalUnits.TabIndex = 17;
            this.lTickIntervalUnits.Text = "ms";
            // 
            // nTickInterval
            // 
            this.nTickInterval.Increment = new decimal( new int[] {
            10,
            0,
            0,
            0} );
            this.nTickInterval.Location = new System.Drawing.Point( 197, 336 );
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
            this.nTickInterval.TabIndex = 16;
            this.nTickInterval.Value = new decimal( new int[] {
            100,
            0,
            0,
            0} );
            // 
            // lTickInterval
            // 
            this.lTickInterval.AutoSize = true;
            this.lTickInterval.Location = new System.Drawing.Point( 120, 338 );
            this.lTickInterval.Name = "lTickInterval";
            this.lTickInterval.Size = new System.Drawing.Size( 71, 15 );
            this.lTickInterval.TabIndex = 15;
            this.lTickInterval.Text = "Tick interval";
            // 
            // lAdvancedWarning
            // 
            this.lAdvancedWarning.AutoSize = true;
            this.lAdvancedWarning.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.lAdvancedWarning.Location = new System.Drawing.Point( 8, 10 );
            this.lAdvancedWarning.Name = "lAdvancedWarning";
            this.lAdvancedWarning.Size = new System.Drawing.Size( 555, 30 );
            this.lAdvancedWarning.TabIndex = 0;
            this.lAdvancedWarning.Text = "Warning: Altering these settings can decrease your server\'s stability and perform" +
                "ance.\r\nIf you\'re not sure what these settings do, you probably shouldn\'t touch t" +
                "hem...";
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bOK.Location = new System.Drawing.Point( 355, 454 );
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
            this.bCancel.Location = new System.Drawing.Point( 461, 454 );
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
            this.bResetTab.Location = new System.Drawing.Point( 132, 454 );
            this.bResetTab.Name = "bResetTab";
            this.bResetTab.Size = new System.Drawing.Size( 100, 28 );
            this.bResetTab.TabIndex = 3;
            this.bResetTab.Text = "Reset Tab";
            this.bResetTab.UseVisualStyleBackColor = true;
            this.bResetTab.Click += new System.EventHandler( this.bResetTab_Click );
            // 
            // tip
            // 
            this.tip.AutoPopDelay = 10000;
            this.tip.InitialDelay = 500;
            this.tip.ReshowDelay = 100;
            // 
            // bResetAll
            // 
            this.bResetAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bResetAll.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bResetAll.Location = new System.Drawing.Point( 12, 454 );
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
            this.bApply.Location = new System.Drawing.Point( 567, 454 );
            this.bApply.Name = "bApply";
            this.bApply.Size = new System.Drawing.Size( 100, 28 );
            this.bApply.TabIndex = 6;
            this.bApply.Text = "Apply";
            this.bApply.Click += new System.EventHandler( this.bApply_Click );
            // 
            // ConfigUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 684, 494 );
            this.Controls.Add( this.bApply );
            this.Controls.Add( this.bResetAll );
            this.Controls.Add( this.bResetTab );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.tabs );
            this.Icon = ((System.Drawing.Icon)(resources.GetObject( "$this.Icon" )));
            this.MinimumSize = new System.Drawing.Size( 700, 500 );
            this.Name = "ConfigUI";
            this.Text = "fCraft Config Tool";
            this.tabs.ResumeLayout( false );
            this.tabGeneral.ResumeLayout( false );
            this.tabGeneral.PerformLayout();
            this.gAppearence.ResumeLayout( false );
            this.gAppearence.PerformLayout();
            this.gBasic.ResumeLayout( false );
            this.gBasic.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nUploadBandwidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nMaxPlayers)).EndInit();
            this.tabWorlds.ResumeLayout( false );
            ((System.ComponentModel.ISupportInitialize)(this.dgWorlds)).EndInit();
            this.tabClasses.ResumeLayout( false );
            this.tabClasses.PerformLayout();
            this.gClassOptions.ResumeLayout( false );
            this.gClassOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nBanOn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickIdle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nKickOn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nRank)).EndInit();
            this.tabSecurity.ResumeLayout( false );
            this.gAntigrief.ResumeLayout( false );
            this.gAntigrief.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamBlockCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamBlockTimer)).EndInit();
            this.gSpamChat.ResumeLayout( false );
            this.gSpamChat.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatWarnings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamMute)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatTimer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nSpamChatCount)).EndInit();
            this.gHackingDetection.ResumeLayout( false );
            this.gHackingDetection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown7)).EndInit();
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
            ((System.ComponentModel.ISupportInitialize)(this.nIRCBotPort)).EndInit();
            this.tabAdvanced.ResumeLayout( false );
            this.tabAdvanced.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nThrottling)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nPing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nTickInterval)).EndInit();
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bResetTab;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabClasses;
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
        private System.Windows.Forms.ToolTip tip;
        private System.Windows.Forms.ListBox vClasses;
        private System.Windows.Forms.Label lClasses;
        private System.Windows.Forms.Button bAddClass;
        private System.Windows.Forms.Label lPermissions;
        private System.Windows.Forms.ListView vPermissions;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.GroupBox gClassOptions;
        private System.Windows.Forms.Button bRemoveClass;
        private System.Windows.Forms.Label lClassColor;
        private System.Windows.Forms.TextBox tClassName;
        private System.Windows.Forms.Label lClassName;
        private System.Windows.Forms.TextBox tPrefix;
        private System.Windows.Forms.Label lPrefix;
        private System.Windows.Forms.NumericUpDown nRank;
        private System.Windows.Forms.Label lRank;
        private System.Windows.Forms.CheckBox xKickOn;
        private System.Windows.Forms.Label lKickOnUnits;
        private System.Windows.Forms.NumericUpDown nKickOn;
        private System.Windows.Forms.CheckBox xBanOn;
        private System.Windows.Forms.Label lBanOnUnits;
        private System.Windows.Forms.Label lBanLimit;
        private System.Windows.Forms.Label lKickLimit;
        private System.Windows.Forms.Label lDemoteLimit;
        private System.Windows.Forms.Label lPromoteLimit;
        private System.Windows.Forms.ComboBox cPromoteLimit;
        private System.Windows.Forms.ComboBox cBanLimit;
        private System.Windows.Forms.ComboBox cKickLimit;
        private System.Windows.Forms.ComboBox cDemoteLimit;
        private System.Windows.Forms.GroupBox gAppearence;
        private System.Windows.Forms.Label lHelpColor;
        private System.Windows.Forms.Label lMessageColor;
        private System.Windows.Forms.GroupBox gBasic;
        private System.Windows.Forms.CheckBox xListPrefixes;
        private System.Windows.Forms.CheckBox xChatPrefixes;
        private System.Windows.Forms.CheckBox xClassColors;
        private System.Windows.Forms.Label lSayColor;
        private System.Windows.Forms.ComboBox cDefaultClass;
        private System.Windows.Forms.Label lDefaultClass;
        private System.Windows.Forms.GroupBox gSaving;
        private System.Windows.Forms.CheckBox xSaveOnShutdown;
        private System.Windows.Forms.NumericUpDown nSaveInterval;
        private System.Windows.Forms.Label nSaveIntervalUnits;
        private System.Windows.Forms.CheckBox xSaveAtInterval;
        private System.Windows.Forms.GroupBox gBackups;
        private System.Windows.Forms.CheckBox xBackupOnStartup;
        private System.Windows.Forms.NumericUpDown nBackupInterval;
        private System.Windows.Forms.Label lBackupIntervalUnits;
        private System.Windows.Forms.CheckBox xBackupAtInterval;
        private System.Windows.Forms.CheckBox xBackupOnJoin;
        private System.Windows.Forms.ComboBox cPolicyColor;
        private System.Windows.Forms.Label lPolicyColor;
        private System.Windows.Forms.CheckBox xRedundantPacket;
        private System.Windows.Forms.ComboBox cPolicyIllegal;
        private System.Windows.Forms.Label lPolicyIllegal;
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
        private System.Windows.Forms.ComboBox cStartup;
        private System.Windows.Forms.Label lStartup;
        private System.Windows.Forms.NumericUpDown nBanOn;
        private System.Windows.Forms.Label lKickIdleUnits;
        private System.Windows.Forms.NumericUpDown nKickIdle;
        private System.Windows.Forms.CheckBox xIdleKick;
        private System.Windows.Forms.CheckBox xPing;
        private System.Windows.Forms.CheckBox xAbsoluteUpdates;
        private System.Windows.Forms.Label lMaxBackups;
        private System.Windows.Forms.NumericUpDown nMaxBackups;
        private System.Windows.Forms.Label lMaxBackupSize;
        private System.Windows.Forms.NumericUpDown nMaxBackupSize;
        private System.Windows.Forms.CheckBox xMaxBackupSize;
        private System.Windows.Forms.CheckBox xMaxBackups;
        private System.Windows.Forms.Label lPing;
        private System.Windows.Forms.NumericUpDown nPing;
        private System.Windows.Forms.Label lThrottlingUnits;
        private System.Windows.Forms.NumericUpDown nThrottling;
        private System.Windows.Forms.Label lThrottling;
        private System.Windows.Forms.Button bApply;
        private System.Windows.Forms.Button bColorSys;
        private System.Windows.Forms.Button bColorSay;
        private System.Windows.Forms.Button bColorHelp;
        private System.Windows.Forms.Button bColorClass;
        private System.Windows.Forms.ComboBox cUpdater;
        private System.Windows.Forms.Label bUpdater;
        private System.Windows.Forms.TabPage tabSecurity;
        private System.Windows.Forms.GroupBox gVerify;
        private System.Windows.Forms.Label lVerifyNames;
        private System.Windows.Forms.ComboBox cVerifyNames;
        private System.Windows.Forms.CheckBox xAnnounceUnverified;
        private System.Windows.Forms.GroupBox gHackingDetection;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.GroupBox gSpamChat;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.Label lSpamBlockSeconds;
        private System.Windows.Forms.NumericUpDown nSpamBlockTimer;
        private System.Windows.Forms.Label lSpamBlocks;
        private System.Windows.Forms.NumericUpDown nSpamBlockCount;
        private System.Windows.Forms.Label lSpamBlockRate;
        private System.Windows.Forms.Label lSpamChatSeconds;
        private System.Windows.Forms.NumericUpDown nSpamChatTimer;
        private System.Windows.Forms.Label lSpamChatMessages;
        private System.Windows.Forms.NumericUpDown nSpamChatCount;
        private System.Windows.Forms.Label lSpamChat;
        private System.Windows.Forms.NumericUpDown numericUpDown7;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox xLowLatencyMode;
        private System.Windows.Forms.GroupBox gAntigrief;
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
        private System.Windows.Forms.CheckBox xIRCMsgs;
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
        private System.Windows.Forms.Label lIRCBotQuitMsg;
        private System.Windows.Forms.TextBox tIRCBotQuitMsg;
        private System.Windows.Forms.CheckBox xLimitOneConnectionPerIP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabWorlds;
        private System.Windows.Forms.DataGridView dgWorlds;
        private System.Windows.Forms.DataGridViewTextBoxColumn wName;
        private System.Windows.Forms.DataGridViewTextBoxColumn Map;
        private System.Windows.Forms.DataGridViewCheckBoxColumn wHidden;
        private System.Windows.Forms.DataGridViewComboBoxColumn wAccess;
        private System.Windows.Forms.DataGridViewComboBoxColumn wBuild;
        private System.Windows.Forms.DataGridViewComboBoxColumn wBackup;
        private System.Windows.Forms.Button bWorldDel;
        private System.Windows.Forms.Button bWorldDup;
        private System.Windows.Forms.Button bWorldGen;
        private System.Windows.Forms.Button bWorldLoad;
    }
}