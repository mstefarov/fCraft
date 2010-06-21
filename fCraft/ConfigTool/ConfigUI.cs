// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using fCraft;
using Color = System.Drawing.Color;


namespace ConfigTool {
    public partial class ConfigUI : Form {
        Font bold;
        PlayerClass selectedClass, defaultClass;

        #region Initialization
        public ConfigUI() {
            InitializeComponent();
            tip.SetToolTip( nUploadBandwidth, "Maximum available upload bandwidth.\n" +
                                              "Used to determine throttling settings.\n" +
                                              "Setting this too high may cause clients to time out when using /load." );

            fCraft.Color.Init();
            ColorPicker.Init();

            bold = new Font( Font, FontStyle.Bold );

            FillPermissionList();

            Load += LoadConfig;
        }

        void FillPermissionList() {
            ListViewItem item;
            foreach( Permissions permission in Enum.GetValues( typeof( Permissions ) ) ) {
#if DEBUG
#else
                if( permission == Permissions.AddLandmarks || permission == Permissions.PlaceHardenedBlocks ) continue;
#endif
                item = new ListViewItem( permission.ToString() );
                item.Tag = permission;
                vPermissions.Items.Add( item );
            }
        }
        #endregion

        #region Loading & Applying Config
        void LoadConfig( object sender, EventArgs args ) {

            if( !File.Exists( Config.ConfigFile ) ) {
                MessageBox.Show( "config.xml was not found. Using defaults." );
            }
            if( Config.Load() ) {
                if( Config.errors.Length > 0 ) {
                    MessageBox.Show( Config.errors, "Config loading warnings" );
                }
            } else {
                MessageBox.Show( Config.errors, "Error occured while trying to load config" );
            }

            ApplyTabGeneral();
            ApplyTabSecurity();
            ApplyTabClasses();
            ApplyTabSavingAndBackup();
            ApplyTabLogging();
            ApplyTabIRC();
            ApplyTabAdvanced();

            AddChangeHandler( tabs );
            bApply.Enabled = false;
        }


        void ApplyTabGeneral() {
            tServerName.Text = Config.GetString( "ServerName" );
            tMOTD.Text = Config.GetString( "MOTD" );
            nMaxPlayers.Value = Convert.ToDecimal( Config.GetInt( "MaxPlayers" ) );
            FillClassList( cDefaultClass, "(lowest class)" );
            cDefaultClass.SelectedIndex = ClassList.GetIndex( ClassList.ParseClass( Config.GetString( "DefaultClass" ) ) );
            cPublic.SelectedIndex = Config.GetBool( "IsPublic" ) ? 0 : 1;
            nPort.Value = Convert.ToDecimal( Config.GetInt( "Port" ) );
            nUploadBandwidth.Value = Convert.ToDecimal( Config.GetInt( "UploadBandwidth" ) );

            ApplyEnum( cReservedSlotBehavior, "ReservedSlotBehavior", 2, "KickIdle", "KickRandom", "IncreaseMaxPlayers" );

            xClassColors.Checked = Config.GetBool( "ClassColorsInChat" );
            xChatPrefixes.Checked = Config.GetBool( "ClassPrefixesInChat" );
            xListPrefixes.Checked = Config.GetBool( "ClassPrefixesInList" );

            colorSys = fCraft.Color.ParseToIndex( Config.GetString( "SystemMessageColor" ) );
            ApplyColor(bColorSys,colorSys );
            colorHelp = fCraft.Color.ParseToIndex( Config.GetString( "HelpColor" ) );
            ApplyColor( bColorHelp, colorHelp );
            colorSay = fCraft.Color.ParseToIndex( Config.GetString( "SayColor" ) );
            ApplyColor( bColorSay, colorSay );
        }


        void ApplyTabSecurity() {
            ApplyEnum( cVerifyNames, "VerifyNames", 1, "Never", "Balanced", "Always" );
            xAnnounceUnverified.Checked = Config.GetBool( "AnnounceUnverifiedNames" );

            nSpamChatCount.Value = Convert.ToDecimal( Config.GetInt( "AntispamMessageCount" ) );
            nSpamChatTimer.Value = Convert.ToDecimal( Config.GetInt( "AntispamInterval" ) );
            nSpamMute.Value = Convert.ToDecimal( Config.GetInt( "AntispamMuteDuration" ) );

            xSpamChatKick.Checked = Config.GetInt( "AntispamMaxWarnings" ) > 0;
            nSpamChatWarnings.Value = Convert.ToDecimal( Config.GetInt( "AntispamMaxWarnings" ) );
            if( !xSpamChatKick.Checked ) nSpamChatWarnings.Enabled = false;

            nSpamBlockCount.Value = Convert.ToDecimal( Config.GetInt( "AntigriefBlockCount" ) );
            nSpamBlockTimer.Value = Convert.ToDecimal( Config.GetInt( "AntigriefInterval" ) );

            ApplyEnum( cSpamAction1, "AntigriefAction1", 0, "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            ApplyEnum( cSpamAction2, "AntigriefAction2", 1, "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            ApplyEnum( cSpamAction3, "AntigriefAction3", 4, "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
        }


        void ApplyTabClasses() {
            vClasses.Items.Clear();
            foreach( PlayerClass playerClass in ClassList.classesByIndex ) {
                string line = String.Format( "{0,3} {1,1}{2}", playerClass.rank, playerClass.prefix, playerClass.name );
                vClasses.Items.Add( line );
            }
            DisableClassOptions();
        }


        void ApplyTabSavingAndBackup() {
            xSaveOnShutdown.Checked = Config.GetBool( "SaveOnShutdown" );
            xSaveAtInterval.Checked = Config.GetInt( "SaveInterval" ) > 0;
            nSaveInterval.Value = Convert.ToDecimal( Config.GetInt( "SaveInterval" ) );
            if( !xSaveAtInterval.Checked ) nSaveInterval.Enabled = false;

            xBackupOnStartup.Checked = Config.GetBool( "BackupOnStartup" );
            xBackupOnJoin.Checked = Config.GetBool( "BackupOnJoin" );
            xBackupOnlyWhenChanged.Checked = Config.GetBool( "BackupOnlyWhenChanged" );
            xBackupAtInterval.Checked = Config.GetInt( "BackupInterval" ) > 0;
            nBackupInterval.Value = Convert.ToDecimal( Config.GetInt( "BackupInterval" ) );
            if( !xBackupAtInterval.Checked ) nBackupInterval.Enabled = false;
            xMaxBackups.Checked = Config.GetInt( "MaxBackups" ) > 0;
            nMaxBackups.Value = Convert.ToDecimal( Config.GetInt( "MaxBackups" ) );
            if( !xMaxBackups.Checked ) nMaxBackups.Enabled = false;
            xMaxBackupSize.Checked = Config.GetInt( "MaxBackupSize" ) > 0;
            nMaxBackupSize.Value = Convert.ToDecimal( Config.GetInt( "MaxBackupSize" ) );
            if( !xMaxBackupSize.Checked ) nMaxBackupSize.Enabled = false;
        }


        void ApplyTabLogging() {
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                item.Checked = Logger.consoleOptions[item.Index];
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                item.Checked = Logger.logFileOptions[item.Index];
            }

            ApplyEnum( cLogMode, "LogMode", 1, "None", "OneFile", "SplitBySession", "SplitByDay" );

            xLogLimit.Checked = Config.GetInt( "MaxLogs" ) > 0;
            nLogLimit.Value = Convert.ToDecimal( Config.GetInt( "MaxLogs" ) );
            if( !xLogLimit.Checked ) nLogLimit.Enabled = false;
        }


        void ApplyTabIRC() {
            xIRC.Checked = Config.GetBool( "IRCBot" );
            tIRCBotNick.Text = Config.GetString( "IRCBotNick" );
            tIRCBotNetwork.Text = Config.GetString( "IRCBotNetwork" );
            nIRCBotPort.Value = Convert.ToDecimal( Config.GetInt( "IRCBotPort" ) );
            tIRCBotChannels.Text = Config.GetString( "IRCBotChannels" );

            xIRCMsgs.Checked = Config.GetBool( "IRCMsgs" );
            xIRCBotForwardFromServer.Checked = Config.GetBool( "IRCBotForwardFromServer" );
            xIRCBotForwardFromIRC.Checked = Config.GetBool( "IRCBotForwardFromIRC" );

            gIRCNetwork.Enabled = xIRC.Checked;
            gIRCOptions.Enabled = xIRC.Checked;
        }


        void ApplyTabAdvanced() {
            ApplyEnum( cPolicyColor, "PolicyColorCodesInChat", 1, "Disallow", "ConsoleOnly", "Allow" );
            ApplyEnum( cPolicyIllegal, "PolicyIllegalCharacters", 0, "Disallow", "ConsoleOnly", "Allow" );

            xRedundantPacket.Checked = Config.GetBool( "SendRedundantBlockUpdates" );
            xPing.Checked = Config.GetInt( "PingInterval" ) > 0;
            nPing.Value = Convert.ToDecimal( Config.GetInt( "PingInterval" ) );
            if( !xPing.Checked ) nPing.Enabled = false;
            xAbsoluteUpdates.Checked = Config.GetBool( "NoPartialPositionUpdates" );
            nTickInterval.Value = Convert.ToDecimal( Config.GetInt( "TickInterval" ) );

            ApplyEnum( cProcessPriority, "ProcessPriority", 0, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
            ApplyEnum( cStartup, "RunOnStartup", 1, "Always", "OnUnexpectedShutdown", "Never" );
            ApplyEnum( cUpdater, "AutomaticUpdates", 2, "Disabled", "Notify", "Prompt", "Auto" );

            nThrottling.Value = Config.GetInt( "BlockUpdateThrottling" );
            xLowLatencyMode.Checked = Config.GetBool( "LowLatencyMode" );
        }


        void ApplyEnum( ComboBox box, string value, int def, params string[] options ) {
            int index = Array.IndexOf<string>( options, Config.GetString( value ) );
            if( index != -1 ) {
                box.SelectedIndex = index;
            } else {
                box.SelectedIndex = def;
            }
        }
        #endregion

        #region Saving Config

        void WriteConfig() {
            Config.errors = "";
            Config.SetValue( "ServerName", tServerName.Text );
            Config.SetValue( "MOTD", tMOTD.Text );
            Config.SetValue( "MaxPlayers", nMaxPlayers.Value.ToString() );
            if( cDefaultClass.SelectedIndex == 0 ) {
                Config.SetValue( "DefaultClass", "" );
            } else {
                Config.SetValue( "DefaultClass", ClassList.ParseIndex(cDefaultClass.SelectedIndex-1).name );
            }
            Config.SetValue( "IsPublic", cPublic.SelectedIndex == 0 ? "True" : "False" );
            Config.SetValue( "Port", nPort.Value.ToString() );
            switch( cVerifyNames.SelectedIndex ) {
                case 0: Config.SetValue( "VerifyNames", "Never" ); break;
                case 1: Config.SetValue( "VerifyNames", "Balanced" ); break;
                case 2: Config.SetValue( "VerifyNames", "Always" ); break;
            }
            Config.SetValue( "UploadBandwidth", nUploadBandwidth.Value.ToString() );
            WriteEnum( cReservedSlotBehavior, "ReservedSlotBehavior", "KickIdle", "KickRandom", "IncreaseMaxPlayers" );
            Config.SetValue( "ClassColorsInChat", xClassColors.Checked.ToString() );
            Config.SetValue( "ClassPrefixesInChat", xChatPrefixes.Checked.ToString() );
            Config.SetValue( "ClassPrefixesInList", xListPrefixes.Checked.ToString() );
            Config.SetValue( "SystemMessageColor", fCraft.Color.GetName( colorSys ) );
            Config.SetValue( "HelpColor", fCraft.Color.GetName( colorHelp ) );
            Config.SetValue( "SayColor", fCraft.Color.GetName( colorSay ) );


            WriteEnum( cVerifyNames, "VerifyNames", "Never", "Balanced", "Always" );

            Config.SetValue( "AnnounceUnverifiedNames", xAnnounceUnverified.Checked.ToString() );

            Config.SetValue( "AntispamMessageCount", nSpamChatCount.Value.ToString() );
            Config.SetValue( "AntispamInterval",nSpamChatTimer.Value.ToString());
            Config.SetValue( "AntispamMuteDuration",nSpamMute.Value.ToString());

            if( xSpamChatKick.Checked ) Config.SetValue( "AntispamMaxWarnings", nSpamChatWarnings.Value.ToString() );
            else Config.SetValue( "AntispamMaxWarnings", "0" );

            Config.SetValue( "AntigriefBlockCount", nSpamBlockCount.Value.ToString() );
            Config.SetValue( "AntigriefInterval", nSpamBlockTimer.Value.ToString() );

            WriteEnum( cSpamAction1, "AntigriefAction1", "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            WriteEnum( cSpamAction2, "AntigriefAction2", "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            WriteEnum( cSpamAction3, "AntigriefAction3", "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );



            Config.SetValue( "SaveOnShutdown", xSaveOnShutdown.Checked.ToString() );
            if( xSaveAtInterval.Checked ) Config.SetValue( "SaveInterval", nSaveInterval.Value.ToString() );
            else Config.SetValue( "SaveInterval", "0" );
            Config.SetValue( "BackupOnStartup", xBackupOnStartup.Checked.ToString() );
            Config.SetValue( "BackupOnJoin", xBackupOnJoin.Checked.ToString() );
            Config.SetValue( "BackupOnlyWhenChanged", xBackupOnlyWhenChanged.Checked.ToString() );

            if( xBackupAtInterval.Checked ) Config.SetValue( "BackupInterval", nBackupInterval.Value.ToString() );
            else Config.SetValue( "BackupInterval", "0" );
            if( xMaxBackups.Checked ) Config.SetValue( "MaxBackups", nMaxBackups.Value.ToString() );
            else Config.SetValue( "MaxBackups", "0" );
            if( xMaxBackupSize.Checked ) Config.SetValue( "MaxBackupSize", nMaxBackupSize.Value.ToString() );
            else Config.SetValue( "MaxBackupSize", "0" );


            WriteEnum( cLogMode, "LogMode", "None", "OneFile", "SplitBySession", "SplitByDay" );
            if( xLogLimit.Checked ) Config.SetValue( "MaxLogs", nLogLimit.Value.ToString() );
            else Config.SetValue( "MaxLogs", "0" );
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                Logger.consoleOptions[item.Index] = item.Checked;
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                Logger.logFileOptions[item.Index] = item.Checked;
            }


            Config.SetValue( "IRCBot", xIRC.Checked.ToString() );

            Config.SetValue( "IRCBotNick", tIRCBotNick.Text );
            Config.SetValue( "IRCBotNetwork", tIRCBotNetwork.Text );
            Config.SetValue( "IRCBotPort", nIRCBotPort.Value.ToString() );
            Config.SetValue( "IRCBotChannels", tIRCBotChannels.Text );

            Config.SetValue( "IRCMsgs", xIRCMsgs.Checked.ToString() );
            Config.SetValue( "IRCBotForwardFromServer", xIRCBotForwardFromServer.Checked.ToString() );
            Config.SetValue( "IRCBotForwardFromIRC", xIRCBotForwardFromIRC.Checked.ToString() );


            WriteEnum( cPolicyColor, "PolicyColorCodesInChat", "Disallow", "ConsoleOnly", "Allow" );
            WriteEnum( cPolicyIllegal, "PolicyIllegalCharacters", "Disallow", "ConsoleOnly", "Allow" );

            Config.SetValue( "SendRedundantBlockUpdates", xRedundantPacket.Checked.ToString() );
            if( xPing.Checked ) Config.SetValue( "PingInterval", nPing.Value.ToString() );
            else Config.SetValue( "PingInterval", "0" );
            Config.SetValue( "NoPartialPositionUpdates", xAbsoluteUpdates.Checked.ToString() );
            Config.SetValue( "TickInterval", Convert.ToInt32( nTickInterval.Value ).ToString() );

            WriteEnum( cProcessPriority, "ProcessPriority", "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
            WriteEnum( cStartup, "RunOnStartup", "Always", "OnUnexpectedShutdown", "Never" );
            WriteEnum( cUpdater, "AutomaticUpdates", "Disabled", "Notify", "Prompt", "Auto" );

            Config.SetValue( "BlockUpdateThrottling", Convert.ToInt32( nThrottling.Value ).ToString() );

            Config.SetValue( "LowLatencyMode", xLowLatencyMode.Checked.ToString() );
        }

        void WriteEnum( ComboBox box, string value, params string[] options ) {
            Config.SetValue( value, options[box.SelectedIndex] );
        }

        #endregion

        #region Input Handlers

        #region General

        private void bMeasure_Click( object sender, EventArgs e ) {
            System.Diagnostics.Process.Start( "http://www.speedtest.net/" );
        }

        #endregion

        #region Logging

        private void vConsoleOptions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            if( e.Item.Checked ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vConsoleOptions.Font;
            }
        }

        private void vLogFileOptions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            if( e.Item.Checked ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vLogFileOptions.Font;
            }
        }

        private void xLogLimit_CheckedChanged( object sender, EventArgs e ) {
            nLogLimit.Enabled = xLogLimit.Checked;
        }

        #endregion

        #region Saving & Backup

        private void xSaveAtInterval_CheckedChanged( object sender, EventArgs e ) {
            nSaveInterval.Enabled = xSaveAtInterval.Checked;
        }

        private void xBackupAtInterval_CheckedChanged( object sender, EventArgs e ) {
            nBackupInterval.Enabled = xBackupAtInterval.Checked;
        }

        private void xMaxBackups_CheckedChanged( object sender, EventArgs e ) {
            nMaxBackups.Enabled = xMaxBackups.Checked;
        }

        private void xMaxBackupSize_CheckedChanged( object sender, EventArgs e ) {
            nMaxBackupSize.Enabled = xMaxBackupSize.Checked;
        }

        #endregion

        #region Advanced

        private void xPing_CheckedChanged( object sender, EventArgs e ) {
            nPing.Enabled = xPing.Checked;
        }

        #endregion

        #endregion

        #region Classes

        void SelectClass( PlayerClass pc ) {
            if( pc == null ) {
                DisableClassOptions();
                return;
            }
            selectedClass = pc;
            tClassName.Text = pc.name;
            nRank.Value = pc.rank;

            ApplyColor( bColorClass, fCraft.Color.ParseToIndex( pc.color ) );

            tPrefix.Text = pc.prefix;
            cKickLimit.SelectedIndex = pc.GetMaxKickIndex();
            cBanLimit.SelectedIndex = pc.GetMaxBanIndex();
            cPromoteLimit.SelectedIndex = pc.GetMaxPromoteIndex();
            cDemoteLimit.SelectedIndex = pc.GetMaxDemoteIndex();
            xReserveSlot.Checked = pc.reservedSlot;
            xIdleKick.Checked = pc.idleKickTimer > 0;
            nKickIdle.Value = pc.idleKickTimer;
            nKickIdle.Enabled = xIdleKick.Checked;
            xKickOn.Checked = pc.spamKickThreshold > 0;
            nKickOn.Value = pc.spamKickThreshold;
            nKickOn.Enabled = xKickOn.Checked;
            xBanOn.Checked = pc.spamBanThreshold > 0;
            nBanOn.Value = pc.spamBanThreshold;
            nBanOn.Enabled = xBanOn.Checked;

            foreach( ListViewItem item in vPermissions.Items ) {
                item.Checked = pc.permissions[item.Index];
                if( item.Checked ) {
                    item.Font = bold;
                } else {
                    item.Font = vPermissions.Font;
                }
            }

            cKickLimit.Enabled = pc.Can( Permissions.Kick );
            cBanLimit.Enabled = pc.Can( Permissions.Ban );
            cPromoteLimit.Enabled = pc.Can( Permissions.Promote );
            cDemoteLimit.Enabled = pc.Can( Permissions.Demote );

            gClassOptions.Enabled = true;
            lPermissions.Enabled = true;
            vPermissions.Enabled = true;
        }

        void RebuildClassList() {
            vClasses.Items.Clear();
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                vClasses.Items.Add( String.Format( "{0,3} {1,1}{2}", pc.rank, pc.prefix, pc.name ) );
            }
            if( selectedClass != null ) {
                vClasses.SelectedIndex = selectedClass.index;
            }
            SelectClass( selectedClass );

            FillClassList( cDefaultClass, "(lowest class)" );
            cDefaultClass.SelectedIndex = ClassList.GetIndex( defaultClass );

            FillClassList( cKickLimit, "(own class)" );
            FillClassList( cBanLimit, "(own class)" );
            FillClassList( cPromoteLimit, "(own class)" );
            FillClassList( cDemoteLimit, "(own class)" );
            if( selectedClass != null ) {
                cKickLimit.SelectedIndex = selectedClass.GetMaxKickIndex();
                cBanLimit.SelectedIndex = selectedClass.GetMaxBanIndex();
                cPromoteLimit.SelectedIndex = selectedClass.GetMaxPromoteIndex();
                cDemoteLimit.SelectedIndex = selectedClass.GetMaxDemoteIndex();
            }
        }

        void DisableClassOptions() {
            selectedClass = null;
            bRemoveClass.Enabled = false;
            tClassName.Text = "";
            nRank.Value = 0;
            bColorClass.Text = "";
            tPrefix.Text = "";
            FillClassList( cPromoteLimit, "(own class)" );
            FillClassList( cDemoteLimit, "(own class)" );
            FillClassList( cKickLimit, "(own class)" );
            FillClassList( cBanLimit, "(own class)" );
            cPromoteLimit.SelectedIndex = 0;
            cDemoteLimit.SelectedIndex = 0;
            cKickLimit.SelectedIndex = 0;
            cBanLimit.SelectedIndex = 0;
            xReserveSlot.Checked = false;
            xIdleKick.Checked = false;
            nKickIdle.Value = 0;
            xKickOn.Checked = false;
            nKickOn.Value = 0;
            xBanOn.Checked = false;
            nBanOn.Value = 0;
            foreach( ListViewItem item in vPermissions.Items ) {
                item.Checked = false;
                item.Font = vPermissions.Font;
            }
            gClassOptions.Enabled = false;
            lPermissions.Enabled = false;
            vPermissions.Enabled = false;
        }

        void FillClassList( ComboBox box, string firstItem ) {
            box.Items.Clear();
            box.Items.Add( firstItem );
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                box.Items.Add( String.Format( "{0,3} {1,1}{2}", pc.rank, pc.prefix, pc.name ) );
            }
        }

        #region Classes Input Handlers

        private void bAddClass_Click( object sender, EventArgs e ) {
            if( vClasses.Items.Count == 255 ) {
                MessageBox.Show( "Maximum number of classes (255) reached!", "Warning" );
                return;
            }
            int number = 1;
            byte rank = 0;
            while( ClassList.classes.ContainsKey( "class" + number ) ) number++;
            while( ClassList.ContainsRank( rank ) ) rank++;
            PlayerClass pc = new PlayerClass();
            pc.name = "class" + number;
            pc.rank = rank;
            for( int i = 0; i < pc.permissions.Length; i++ ) pc.permissions[i] = false;
            pc.prefix = "";
            pc.reservedSlot = false;
            pc.color = "";

            defaultClass = ClassList.ParseIndex( cDefaultClass.SelectedIndex - 1 );

            ClassList.AddClass( pc );
            RebuildClassList();
        }


        private void tPrefix_Validating( object sender, CancelEventArgs e ) {
            if( selectedClass == null ) return;
            if( tPrefix.Text.Length > 0 && !PlayerClass.IsValidPrefix( tPrefix.Text ) ) {
                MessageBox.Show( "Invalid prefix character!\n" +
                    "Prefixes may only contain characters that are allowed in chat (except space).", "Warning" );
                e.Cancel = true;
            }
            defaultClass = ClassList.ParseIndex( cDefaultClass.SelectedIndex - 1 );
            selectedClass.prefix = tPrefix.Text;
            RebuildClassList();
        }


        private void xReserveSlot_CheckedChanged( object sender, EventArgs e ) {
            if( selectedClass == null ) return;
            selectedClass.reservedSlot = xReserveSlot.Checked;
        }


        private void nKickIdle_ValueChanged( object sender, EventArgs e ) {
            if( selectedClass == null || !xIdleKick.Checked ) return;
            selectedClass.idleKickTimer = Convert.ToInt32( nKickIdle.Value );
        }


        private void nKickOn_ValueChanged( object sender, EventArgs e ) {
            if( selectedClass == null || !xKickOn.Checked ) return;
            selectedClass.spamKickThreshold = Convert.ToInt32( nKickOn.Value );
        }


        private void nBanOn_ValueChanged( object sender, EventArgs e ) {
            if( selectedClass == null || !xBanOn.Checked ) return;
            selectedClass.spamBanThreshold = Convert.ToInt32( nBanOn.Value );
        }


        private void cPromoteLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            if( selectedClass != null ) {
                selectedClass.maxPromote = ClassList.ParseIndex( cPromoteLimit.SelectedIndex - 1 );
            }
        }


        private void cDemoteLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            if( selectedClass != null ) {
                selectedClass.maxDemote = ClassList.ParseIndex( cDemoteLimit.SelectedIndex - 1 );
            }
        }


        private void cKickLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            if( selectedClass != null ) {
                selectedClass.maxKick = ClassList.ParseIndex( cKickLimit.SelectedIndex - 1 );
            }
        }


        private void cBanLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            if( selectedClass != null ) {
                selectedClass.maxBan = ClassList.ParseIndex( cBanLimit.SelectedIndex - 1 );
            }
        }

        private void xSpamChatKick_CheckedChanged( object sender, EventArgs e ) {
            nSpamChatWarnings.Enabled = xSpamChatKick.Checked;
        }

        private void vClasses_SelectedIndexChanged( object sender, EventArgs e ) {
            if( vClasses.SelectedIndex != -1 ) {
                SelectClass( ClassList.ParseIndex( vClasses.SelectedIndex ) );
                bRemoveClass.Enabled = true;
            } else {
                DisableClassOptions();
                bRemoveClass.Enabled = false;
            }
        }


        private void xIdleKick_CheckedChanged( object sender, EventArgs e ) {
            nKickIdle.Enabled = xIdleKick.Checked;
            if( selectedClass != null ) {
                if( xIdleKick.Checked ) {
                    nKickIdle.Value = selectedClass.idleKickTimer;
                } else {
                    selectedClass.idleKickTimer = 0;
                    nKickIdle.Value = 0;
                }
            }
        }

        private void xKickOn_CheckedChanged( object sender, EventArgs e ) {
            nKickOn.Enabled = xKickOn.Checked;
            if( selectedClass != null ) {
                if( xKickOn.Checked ) {
                    nKickOn.Value = selectedClass.spamKickThreshold;
                } else {
                    selectedClass.spamKickThreshold = 0;
                    nKickOn.Value = 0;
                }
            }
        }

        private void xBanOn_CheckedChanged( object sender, EventArgs e ) {
            nBanOn.Enabled = xBanOn.Checked;
            if( selectedClass != null ) {
                if( xBanOn.Checked ) {
                    nBanOn.Value = selectedClass.spamBanThreshold;
                } else {
                    selectedClass.spamBanThreshold = 0;
                    nBanOn.Value = 0;
                }
            }
        }

        private void vPermissions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            if( e.Item.Checked ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vPermissions.Font;
            }
            if( selectedClass == null ) return;
            switch( (Permissions)e.Item.Tag ) {
                case Permissions.Ban:
                    cBanLimit.Enabled = e.Item.Checked;
                    if( !e.Item.Checked ) vPermissions.Items[(int)Permissions.BanIP].Checked = false;
                    if( !e.Item.Checked ) vPermissions.Items[(int)Permissions.BanAll].Checked = false;
                    break;
                case Permissions.BanIP:
                    if( e.Item.Checked ) vPermissions.Items[(int)Permissions.Ban].Checked = true;
                    break;
                case Permissions.BanAll:
                    if( e.Item.Checked ) vPermissions.Items[(int)Permissions.Ban].Checked = true;
                    break;
                case Permissions.Kick:
                    cKickLimit.Enabled = e.Item.Checked; break;
                case Permissions.Promote:
                    cPromoteLimit.Enabled = e.Item.Checked; break;
                case Permissions.Demote:
                    cDemoteLimit.Enabled = e.Item.Checked; break;
            }

            selectedClass.permissions[e.Item.Index] = e.Item.Checked;
        }

        private void bRemoveClass_Click( object sender, EventArgs e ) {
            if( vClasses.SelectedItem != null ) {
                selectedClass = null;
                int index = vClasses.SelectedIndex;

                PlayerClass defaultClass = ClassList.ParseIndex( cDefaultClass.SelectedIndex - 1 );
                if( defaultClass != null && index == defaultClass.index ) {
                    defaultClass = null;
                    MessageBox.Show( "DefaultClass has been reset to \"(lowest class)\"", "Warning" );
                }

                if( ClassList.DeleteClass( index ) ) {
                    MessageBox.Show( "Some of the rank limits for kick, ban, promote, and/or demote have been reset.", "Warning" );
                }
                vClasses.Items.RemoveAt( index );

                RebuildClassList();

                if( index < vClasses.Items.Count ) {
                    vClasses.SelectedIndex = index;
                }
            }
        }


        private void tClassName_Validating( object sender, CancelEventArgs e ) {
            if( selectedClass == null ) return;
            string name = tClassName.Text.Trim();
            if( name == selectedClass.name ) return;
            if( name.Length == 0 ) {
                MessageBox.Show( "Class name cannot be blank." );
                e.Cancel = true;
            } else if( !PlayerClass.IsValidClassName( name ) ) {
                MessageBox.Show( "Class name can only contain letters, digits, and underscores." );
                e.Cancel = true;
            } else if( !ClassList.CanChangeName( selectedClass, name ) ) {
                MessageBox.Show( "There is already another class named \"" + name + "\".\n" +
                                "Duplicate class names are now allowed." );
                e.Cancel = true;
            } else {
                defaultClass = ClassList.ParseIndex( cDefaultClass.SelectedIndex - 1 );
                ClassList.ChangeName( selectedClass, name );
                RebuildClassList();
            }
        }


        private void nRank_Validating( object sender, CancelEventArgs e ) {
            byte rank = Convert.ToByte( nRank.Value );
            if( rank == selectedClass.rank ) return;
            if( !ClassList.CanChangeRank( selectedClass, rank ) ) {
                MessageBox.Show( "There is already another class with the same rank (" + nRank.Value + ").\n" +
                "Duplicate class ranks are now allowed." );
                e.Cancel = true;
            } else {
                defaultClass = ClassList.ParseIndex( cDefaultClass.SelectedIndex - 1 );
                ClassList.ChangeRank( selectedClass, rank );
                RebuildClassList();
            }
        }

        #endregion

        #endregion

        #region Apply / Save / Cancel

        private void bApply_Click( object sender, EventArgs e ) {
            WriteConfig();
            if( Config.errors != "" ) {
                MessageBox.Show( Config.errors, "Some errors were found in the selected values:" );
            } else if( Config.Save() ) {
                bApply.Enabled = false;
            } else {
                MessageBox.Show( Config.errors, "An error occured while trying to save:" );
            }
        }

        private void bSave_Click( object sender, EventArgs e ) {
            WriteConfig();
            if( Config.errors != "" ) {
                MessageBox.Show( Config.errors, "Some errors were found in the selected values:" );
            } else if( Config.Save() ) {
                Application.Exit();
            } else {
                MessageBox.Show( Config.errors, "An error occured while trying to save:" );
            }
        }

        private void bCancel_Click( object sender, EventArgs e ) {
            Application.Exit();
        }

        #endregion

        #region Reset

        private void bResetAll_Click( object sender, EventArgs e ) {
            if( MessageBox.Show( "Are you sure you want to reset everything to defaults?", "Warning", MessageBoxButtons.OKCancel ) == DialogResult.OK ) {
                Config.LoadDefaults();
                Config.ResetClasses();
                ApplyTabGeneral();
                ApplyTabClasses();
                ApplyTabSavingAndBackup();
                ApplyTabLogging();
                ApplyTabAdvanced();
            }
        }

        private void bResetTab_Click( object sender, EventArgs e ) {
            if( MessageBox.Show( "Are you sure you want to reset this tab to defaults?", "Warning", MessageBoxButtons.OKCancel ) == DialogResult.OK ) {
                switch( tabs.SelectedIndex ) {
                    case 0:// General
                        Config.LoadDefaultsGeneral();
                        ApplyTabGeneral();
                        break;
                    case 1:// Security
                        Config.LoadDefaultsSecurity();
                        ApplyTabSecurity();
                        break;
                    case 2:// Classes
                        Config.ResetClasses();
                        ApplyTabClasses();
                        defaultClass = null;
                        RebuildClassList();
                        break;
                    case 3:// Saving and Backup
                        Config.LoadDefaultsSavingAndBackup();
                        ApplyTabSavingAndBackup();
                        break;
                    case 4:// Logging
                        Config.LoadDefaultsLogging();
                        ApplyTabLogging();
                        break;
                    case 5:// IRC
                        Config.LoadDefaultsIRC();
                        ApplyTabIRC();
                        break;
                    case 6:// Advanced
                        Config.LoadDefaultsAdvanced();
                        ApplyTabAdvanced();
                        break;
                }
            }
        }

        #endregion

        #region Utils

        #region Change Detection

        void SomethingChanged( object sender, EventArgs args ) {
            bApply.Enabled = true;
        }

        void AddChangeHandler( Control c ) {
            if( c is CheckBox ) {
                ((CheckBox)c).CheckedChanged += SomethingChanged;
            } else if( c is ComboBox ) {
                ((ComboBox)c).SelectedIndexChanged += SomethingChanged;
            } else if( c is ListView ) {
                ((ListView)c).ItemChecked += SomethingChanged;
            } else if( c is NumericUpDown ) {
                ((NumericUpDown)c).ValueChanged += SomethingChanged;
            } else if( c is ListBox ) {
                ((ListBox)c).SelectedIndexChanged += SomethingChanged;
            } else if( c is TextBoxBase ) {
                c.TextChanged += SomethingChanged;
            }
            foreach( Control child in c.Controls ) {
                AddChangeHandler( child );
            }
        }

        #endregion

        #region Colors
        int colorSys, colorSay, colorHelp;

        void ApplyColor( Button button, int color ) {
            button.Text = fCraft.Color.GetName( color );
            button.BackColor = ColorPicker.colors[color].background;
            button.ForeColor = ColorPicker.colors[color].foreground;
        }

        private void bColorSys_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "System message color", colorSys );
            picker.ShowDialog();
            colorSys = picker.color;
            ApplyColor( bColorSys, colorSys );
        }

        private void bColorHelp_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Help message color", colorHelp );
            picker.ShowDialog();
            colorHelp = picker.color;
            ApplyColor( bColorHelp, colorHelp );
        }

        private void bColorSay_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "/say message color", colorSay );
            picker.ShowDialog();
            colorSay = picker.color;
            ApplyColor( bColorSay, colorSay );
        }

        private void bColorClass_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Class color for \""+selectedClass.name+"\"", fCraft.Color.ParseToIndex(selectedClass.color) );
            picker.ShowDialog();
            ApplyColor( bColorClass, picker.color );
            selectedClass.color = fCraft.Color.GetName( picker.color );
        }

        #endregion

        private void bRules_Click( object sender, EventArgs e ) {
            TextEditorPopup popup = new TextEditorPopup( "rules.txt", "Use common sense!" );
            popup.ShowDialog();
        }

        #endregion

        private void xIRC_CheckedChanged( object sender, EventArgs e ) {
            gIRCNetwork.Enabled = xIRC.Checked;
            gIRCOptions.Enabled = xIRC.Checked;
        }

    }
}