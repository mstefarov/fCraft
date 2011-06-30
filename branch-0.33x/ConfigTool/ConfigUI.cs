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
        Config config;
        Font bold;
        PlayerClass selectedClass;
        int colorSys, colorSay, colorHelp;

        //===================================================== INIT ===========

        public ConfigUI() {
            InitializeComponent();
            tip.SetToolTip( nUploadBandwidth, "Maximum available upload bandwidth.\n" +
                                              "Used to determine throttling settings.\n" +
                                              "Setting this too high may cause clients to time out when using /load." );

            fCraft.Color.Init();
            ColorPicker.Init();
            config = new Config( null, new ClassList( null ), new Logger( null ) );

            bold = new Font( Font, FontStyle.Bold );

            Load += LoadConfig;
        }

        void ApplyColor( Button button, int color ) {
            button.Text = fCraft.Color.GetName( color );
            button.BackColor = ColorPicker.colors[color].background;
            button.ForeColor = ColorPicker.colors[color].foreground;
        }

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


        //===================================================== LOAD / APPLY CONFIG ===========

        void LoadConfig( object sender, EventArgs args ) {

            if( !File.Exists( "config.xml" ) ) {
                MessageBox.Show( "config.xml was not found. Using defaults." );
            }
            if( config.Load( "config.xml" ) ) {
                if( config.errors.Length > 0 ) {
                    MessageBox.Show( config.errors, "Config loading warnings" );
                }
            } else {
                MessageBox.Show( config.errors, "Error occured while trying to load config" );
            }

            ApplyTabGeneral();
            ApplyTabSecurity();
            ApplyTabClasses();
            ApplyTabSavingAndBackup();
            ApplyTabLogging();
            ApplyTabAdvanced();

            AddChangeHandler( tabs );
            bApply.Enabled = false;
        }


        void ApplyTabGeneral() {
            tServerName.Text = config.GetString( "ServerName" );
            tMOTD.Text = config.GetString( "MOTD" );
            nMaxPlayers.Value = Convert.ToDecimal( config.GetInt( "MaxPlayers" ) );
            FillClassList( cDefaultClass, "(lowest class)" );
            cDefaultClass.SelectedIndex = config.classes.GetIndex( config.classes.ParseClass( config.GetString( "DefaultClass" ) ) );
            cPublic.SelectedIndex = config.GetBool( "IsPublic" ) ? 0 : 1;
            nPort.Value = Convert.ToDecimal( config.GetInt( "Port" ) );
            nUploadBandwidth.Value = Convert.ToDecimal( config.GetInt( "UploadBandwidth" ) );

            ApplyEnum( cReservedSlotBehavior, "ReservedSlotBehavior", 2, "KickIdle", "KickRandom", "IncreaseMaxPlayers" );

            xClassColors.Checked = config.GetBool( "ClassColorsInChat" );
            xChatPrefixes.Checked = config.GetBool( "ClassPrefixesInChat" );
            xListPrefixes.Checked = config.GetBool( "ClassPrefixesInList" );

            colorSys = fCraft.Color.ParseToIndex( config.GetString( "SystemMessageColor" ) );
            ApplyColor(bColorSys,colorSys );
            colorHelp = fCraft.Color.ParseToIndex( config.GetString( "HelpColor" ) );
            ApplyColor( bColorHelp, colorHelp );
            colorSay = fCraft.Color.ParseToIndex( config.GetString( "SayColor" ) );
            ApplyColor( bColorSay, colorSay );
        }


        void ApplyTabSecurity() {
            ApplyEnum( cVerifyNames, "VerifyNames", 1, "Never", "Balanced", "Always" );
            xAnnounceUnverified.Checked = config.GetBool( "AnnounceUnverifiedNames" );

            nSpamChatCount.Value = Convert.ToDecimal( config.GetInt( "AntispamMessageCount" ) );
            nSpamChatTimer.Value = Convert.ToDecimal( config.GetInt( "AntispamInterval" ) );
            nSpamMute.Value = Convert.ToDecimal( config.GetInt( "AntispamMuteDuration" ) );

            xSpamChatKick.Checked = config.GetInt( "AntispamMaxWarnings" ) > 0;
            nSpamChatWarnings.Value = Convert.ToDecimal( config.GetInt( "AntispamMaxWarnings" ) );
            if( !xSpamChatKick.Checked ) nSpamChatWarnings.Enabled = false;

            nSpamBlockCount.Value = Convert.ToDecimal( config.GetInt( "AntigriefBlockCount" ) );
            nSpamBlockTimer.Value = Convert.ToDecimal( config.GetInt( "AntigriefInterval" ) );

            ApplyEnum( cSpamAction1, "AntigriefAction1", 0, "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            ApplyEnum( cSpamAction2, "AntigriefAction2", 1, "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            ApplyEnum( cSpamAction3, "AntigriefAction3", 4, "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
        }



        void ApplyTabClasses() {
            vClasses.Items.Clear();
            foreach( PlayerClass playerClass in config.classes.classesByIndex ) {
                string line = String.Format( "{0,3} {1,1}{2}", playerClass.rank, playerClass.prefix, playerClass.name );
                vClasses.Items.Add( line );
            }
            DisableClassOptions();
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


        void ApplyTabSavingAndBackup() {
            xSaveOnShutdown.Checked = config.GetBool( "SaveOnShutdown" );
            xSaveAtInterval.Checked = config.GetInt( "SaveInterval" ) > 0;
            nSaveInterval.Value = Convert.ToDecimal( config.GetInt( "SaveInterval" ) );
            if( !xSaveAtInterval.Checked ) nSaveInterval.Enabled = false;

            xBackupOnStartup.Checked = config.GetBool( "BackupOnStartup" );
            xBackupOnJoin.Checked = config.GetBool( "BackupOnJoin" );
            xBackupOnlyWhenChanged.Checked = config.GetBool( "BackupOnlyWhenChanged" );
            xBackupAtInterval.Checked = config.GetInt( "BackupInterval" ) > 0;
            nBackupInterval.Value = Convert.ToDecimal( config.GetInt( "BackupInterval" ) );
            if( !xBackupAtInterval.Checked ) nBackupInterval.Enabled = false;
            xMaxBackups.Checked = config.GetInt( "MaxBackups" ) > 0;
            nMaxBackups.Value = Convert.ToDecimal( config.GetInt( "MaxBackups" ) );
            if( !xMaxBackups.Checked ) nMaxBackups.Enabled = false;
            xMaxBackupSize.Checked = config.GetInt( "MaxBackupSize" ) > 0;
            nMaxBackupSize.Value = Convert.ToDecimal( config.GetInt( "MaxBackupSize" ) );
            if( !xMaxBackupSize.Checked ) nMaxBackupSize.Enabled = false;
        }


        void ApplyTabLogging() {
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                item.Checked = config.logger.consoleOptions[item.Index];
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                item.Checked = config.logger.logFileOptions[item.Index];
            }

            ApplyEnum( cLogMode, "LogMode", 1, "None", "OneFile", "SplitBySession", "SplitByDay" );

            xLogLimit.Checked = config.GetInt( "MaxLogs" ) > 0;
            nLogLimit.Value = Convert.ToDecimal( config.GetInt( "MaxLogs" ) );
            if( !xLogLimit.Checked ) nLogLimit.Enabled = false;
        }


        void ApplyTabAdvanced() {
            ApplyEnum( cPolicyColor, "PolicyColorCodesInChat", 1, "Disallow", "ConsoleOnly", "Allow" );
            ApplyEnum( cPolicyIllegal, "PolicyIllegalCharacters", 0, "Disallow", "ConsoleOnly", "Allow" );

            xRedundantPacket.Checked = config.GetBool( "SendRedundantBlockUpdates" );
            xPing.Checked = config.GetInt( "PingInterval" ) > 0;
            nPing.Value = Convert.ToDecimal( config.GetInt( "PingInterval" ) );
            if( !xPing.Checked ) nPing.Enabled = false;
            xAbsoluteUpdates.Checked = config.GetBool( "NoPartialPositionUpdates" );
            nTickInterval.Value = Convert.ToDecimal( config.GetInt( "TickInterval" ) );

            ApplyEnum( cProcessPriority, "ProcessPriority", 0, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
            ApplyEnum( cStartup, "RunOnStartup", 1, "Always", "OnUnexpectedShutdown", "Never" );
            ApplyEnum( cUpdater, "AutomaticUpdates", 2, "Disabled", "Notify", "Prompt", "Auto" );

            nThrottling.Value = config.GetInt( "BlockUpdateThrottling" );
            xLowLatencyMode.Checked = config.GetBool( "LowLatencyMode" );
        }


        void ApplyEnum( ComboBox box, string value, int def, params string[] options ) {
            int index = Array.IndexOf<string>( options, config.GetString( value ) );
            if( index != -1 ) {
                box.SelectedIndex = index;
            } else {
                box.SelectedIndex = def;
            }
        }

        void FillClassList( ComboBox box, string firstItem ) {
            box.Items.Clear();
            box.Items.Add( firstItem );
            foreach( PlayerClass pc in config.classes.classesByIndex ) {
                box.Items.Add( String.Format( "{0,3} {1,1}{2}", pc.rank, pc.prefix, pc.name ) );
            }
        }


        //===================================================== WRITE CONFIG ===========

        void WriteConfig() {
            config.errors = "";
            config.SetValue( "ServerName", tServerName.Text );
            config.SetValue( "MOTD", tMOTD.Text );
            config.SetValue( "MaxPlayers", nMaxPlayers.Value.ToString() );
            if( cDefaultClass.SelectedIndex == 0 ) {
                config.SetValue( "DefaultClass", "" );
            } else {
                config.SetValue( "DefaultClass", config.classes.ParseIndex(cDefaultClass.SelectedIndex-1).name );
            }
            config.SetValue( "IsPublic", cPublic.SelectedIndex == 0 ? "True" : "False" );
            config.SetValue( "Port", nPort.Value.ToString() );
            switch( cVerifyNames.SelectedIndex ) {
                case 0: config.SetValue( "VerifyNames", "Never" ); break;
                case 1: config.SetValue( "VerifyNames", "Balanced" ); break;
                case 2: config.SetValue( "VerifyNames", "Always" ); break;
            }
            config.SetValue( "UploadBandwidth", nUploadBandwidth.Value.ToString() );
            WriteEnum( cReservedSlotBehavior, "ReservedSlotBehavior", "KickIdle", "KickRandom", "IncreaseMaxPlayers" );
            config.SetValue( "ClassColorsInChat", xClassColors.Checked.ToString() );
            config.SetValue( "ClassPrefixesInChat", xChatPrefixes.Checked.ToString() );
            config.SetValue( "ClassPrefixesInList", xListPrefixes.Checked.ToString() );
            config.SetValue( "SystemMessageColor", fCraft.Color.GetName( colorSys ) );
            config.SetValue( "HelpColor", fCraft.Color.GetName( colorHelp ) );
            config.SetValue( "SayColor", fCraft.Color.GetName( colorSay ) );


            WriteEnum( cVerifyNames, "VerifyNames", "Never", "Balanced", "Always" );

            config.SetValue( "AnnounceUnverifiedNames", xAnnounceUnverified.Checked.ToString() );

            config.SetValue( "AntispamMessageCount", nSpamChatCount.Value.ToString() );
            config.SetValue( "AntispamInterval",nSpamChatTimer.Value.ToString());
            config.SetValue( "AntispamMuteDuration",nSpamMute.Value.ToString());

            if( xSpamChatKick.Checked ) config.SetValue( "AntispamMaxWarnings", nSpamChatWarnings.Value.ToString() );
            else config.SetValue( "AntispamMaxWarnings", "0" );

            config.SetValue( "AntigriefBlockCount", nSpamBlockCount.Value.ToString() );
            config.SetValue( "AntigriefInterval", nSpamBlockTimer.Value.ToString() );

            WriteEnum( cSpamAction1, "AntigriefAction1", "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            WriteEnum( cSpamAction2, "AntigriefAction2", "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
            WriteEnum( cSpamAction3, "AntigriefAction3", "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );



            config.SetValue( "SaveOnShutdown", xSaveOnShutdown.Checked.ToString() );
            if( xSaveAtInterval.Checked ) config.SetValue( "SaveInterval", nSaveInterval.Value.ToString() );
            else config.SetValue( "SaveInterval", "0" );
            config.SetValue( "BackupOnStartup", xBackupOnStartup.Checked.ToString() );
            config.SetValue( "BackupOnJoin", xBackupOnJoin.Checked.ToString() );
            config.SetValue( "BackupOnlyWhenChanged", xBackupOnlyWhenChanged.Checked.ToString() );

            if( xBackupAtInterval.Checked ) config.SetValue( "BackupInterval", nBackupInterval.Value.ToString() );
            else config.SetValue( "BackupInterval", "0" );
            if( xMaxBackups.Checked ) config.SetValue( "MaxBackups", nMaxBackups.Value.ToString() );
            else config.SetValue( "MaxBackups", "0" );
            if( xMaxBackupSize.Checked ) config.SetValue( "MaxBackupSize", nMaxBackupSize.Value.ToString() );
            else config.SetValue( "MaxBackupSize", "0" );


            WriteEnum( cLogMode, "LogMode", "None", "OneFile", "SplitBySession", "SplitByDay" );
            if( xLogLimit.Checked ) config.SetValue( "MaxLogs", nLogLimit.Value.ToString() );
            else config.SetValue( "MaxLogs", "0" );
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                config.logger.consoleOptions[item.Index] = item.Checked;
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                config.logger.logFileOptions[item.Index] = item.Checked;
            }


            WriteEnum( cPolicyColor, "PolicyColorCodesInChat", "Disallow", "ConsoleOnly", "Allow" );
            WriteEnum( cPolicyIllegal, "PolicyIllegalCharacters", "Disallow", "ConsoleOnly", "Allow" );

            config.SetValue( "SendRedundantBlockUpdates", xRedundantPacket.Checked.ToString() );
            if( xPing.Checked ) config.SetValue( "PingInterval", nPing.Value.ToString() );
            else config.SetValue( "PingInterval", "0" );
            config.SetValue( "NoPartialPositionUpdates", xAbsoluteUpdates.Checked.ToString() );
            config.SetValue( "TickInterval", Convert.ToInt32( nTickInterval.Value ).ToString() );

            WriteEnum( cProcessPriority, "ProcessPriority", "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
            WriteEnum( cStartup, "RunOnStartup", "Always", "OnUnexpectedShutdown", "Never" );
            WriteEnum( cUpdater, "AutomaticUpdates", "Disabled", "Notify", "Prompt", "Auto" );

            config.SetValue( "BlockUpdateThrottling", Convert.ToInt32( nThrottling.Value ).ToString() );

            config.SetValue( "LowLatencyMode", xLowLatencyMode.Checked.ToString() );
        }

        void WriteEnum( ComboBox box, string value, params string[] options ) {
            config.SetValue( value, options[box.SelectedIndex] );
        }



        private void bMeasure_Click( object sender, EventArgs e ) {
            System.Diagnostics.Process.Start( "http://www.speedtest.net/" );
        }

        private void vPermissions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            if( e.Item.Checked ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vPermissions.Font;
            }
            if( selectedClass == null ) return;
            switch( (Permissions)e.Item.Index ) {
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

        private void vClasses_SelectedIndexChanged( object sender, EventArgs e ) {
                if( vClasses.SelectedIndex != -1 ) {
                    SelectClass( config.classes.ParseIndex( vClasses.SelectedIndex ) );
                    bRemoveClass.Enabled = true;
                } else {
                    DisableClassOptions();
                    bRemoveClass.Enabled = false;
                }
        }
        

        private void xIdleKick_CheckedChanged( object sender, EventArgs e ) {
            nKickIdle.Enabled = xIdleKick.Checked;
            if( selectedClass != null ){
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

        private void xLogLimit_CheckedChanged( object sender, EventArgs e ) {
            nLogLimit.Enabled = xLogLimit.Checked;
        }

        private void xPing_CheckedChanged( object sender, EventArgs e ) {
            nPing.Enabled = xPing.Checked;
        }

        private void bApply_Click( object sender, EventArgs e ) {
            WriteConfig();
            if( config.errors != "" ) {
                MessageBox.Show( config.errors, "Some errors were found in the selected values:" );
            } else if( config.Save( "config.xml" ) ) {
                bApply.Enabled = false;
            } else {
                MessageBox.Show( config.errors, "An error occured while trying to save:" );
            }
        }

        private void bSave_Click( object sender, EventArgs e ) {
            WriteConfig();
            if( config.errors != "" ) {
                MessageBox.Show( config.errors, "Some errors were found in the selected values:" );
            } else if( config.Save( "config.xml" ) ) {
                Application.Exit();
            } else {
                MessageBox.Show( config.errors, "An error occured while trying to save:" );
            }
        }

        private void bResetAll_Click( object sender, EventArgs e ) {
            if( MessageBox.Show( "Are you sure you want to reset everything to defaults?", "Warning", MessageBoxButtons.OKCancel )== DialogResult.OK ) {
                config.LoadDefaults();
                config.ResetClasses();
                ApplyTabGeneral();
                ApplyTabClasses();
                ApplyTabSavingAndBackup();
                ApplyTabLogging();
                ApplyTabAdvanced();
            }
        }

        private void bRemoveClass_Click( object sender, EventArgs e ) {
            if( vClasses.SelectedItem != null ) {
                selectedClass = null;
                int index = vClasses.SelectedIndex;

                PlayerClass defaultClass = config.classes.ParseIndex( cDefaultClass.SelectedIndex - 1 );
                if( defaultClass != null && index == defaultClass.index ) {
                    defaultClass = null;
                    MessageBox.Show( "DefaultClass has been reset to \"(lowest class)\"", "Warning" );
                }

                if( config.classes.DeleteClass( index ) ) {
                    MessageBox.Show( "Some of the rank limits for kick, ban, promote, and/or demote have been reset.", "Warning" );
                }
                vClasses.Items.RemoveAt( index );

                RebuildClassList();

                if( index < vClasses.Items.Count ) {
                    vClasses.SelectedIndex = index;
                }
            }
        }

        private void bCancel_Click( object sender, EventArgs e ) {
            Application.Exit();
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
            } else if( !config.classes.CanChangeName( selectedClass, name ) ) {
                MessageBox.Show( "There is already another class named \"" + name + "\".\n" +
                                "Duplicate class names are now allowed." );
                e.Cancel = true;
            } else {
                defaultClass = config.classes.ParseIndex( cDefaultClass.SelectedIndex - 1 );
                config.classes.ChangeName( selectedClass, name );
                RebuildClassList();
            }
        }

        private void nRank_Validating( object sender, CancelEventArgs e ) {
            byte rank = Convert.ToByte( nRank.Value );
            if( rank == selectedClass.rank ) return;
            if( !config.classes.CanChangeRank( selectedClass, rank ) ) {
                MessageBox.Show( "There is already another class with the same rank (" + nRank.Value + ").\n" +
                "Duplicate class ranks are now allowed." );
                e.Cancel = true;
            } else {
                defaultClass = config.classes.ParseIndex( cDefaultClass.SelectedIndex - 1 ); 
                config.classes.ChangeRank( selectedClass, rank );
                RebuildClassList();
            }
        }

        PlayerClass defaultClass;
        void RebuildClassList() {
            vClasses.Items.Clear();
            foreach( PlayerClass pc in config.classes.classesByIndex ) {
                vClasses.Items.Add( String.Format( "{0,3} {1,1}{2}", pc.rank, pc.prefix, pc.name ) );
            }
            if( selectedClass != null ) {
                vClasses.SelectedIndex = selectedClass.index;
            }
            SelectClass( selectedClass );

            FillClassList( cDefaultClass, "(lowest class)" );
            cDefaultClass.SelectedIndex = config.classes.GetIndex( defaultClass );

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

        private void bAddClass_Click( object sender, EventArgs e ) {
            if( vClasses.Items.Count == 255 ) {
                MessageBox.Show( "Maximum number of classes (255) reached!", "Warning" );
                return;
            }
            int number = 1;
            byte rank = 0;
            while( config.classes.classes.ContainsKey( "class" + number ) ) number++;
            while( config.classes.ContainsRank( rank ) ) rank++;
            PlayerClass pc = new PlayerClass();
            pc.name = "class" + number;
            pc.rank = rank;
            for( int i = 0; i < pc.permissions.Length; i++ ) pc.permissions[i] = false;
            pc.prefix = "";
            pc.reservedSlot = false;
            pc.color = "";

            defaultClass = config.classes.ParseIndex( cDefaultClass.SelectedIndex - 1 );

            config.classes.AddClass( pc );
            RebuildClassList();
        }

        private void tPrefix_Validating( object sender, CancelEventArgs e ) {
            if( selectedClass == null ) return;
            if( tPrefix.Text.Length > 0 && !PlayerClass.IsValidPrefix( tPrefix.Text ) ) {
                MessageBox.Show( "Invalid prefix character!\n"+
                    "Prefixes may only contain characters that are allowed in chat (except space).", "Warning" );
                e.Cancel = true;
            }
            defaultClass = config.classes.ParseIndex( cDefaultClass.SelectedIndex - 1 );
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
                selectedClass.maxPromote = config.classes.ParseIndex( cPromoteLimit.SelectedIndex - 1 );
            }
        }

        private void cDemoteLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            if( selectedClass != null ) {
                selectedClass.maxDemote = config.classes.ParseIndex( cDemoteLimit.SelectedIndex - 1 );
            }
        }

        private void cKickLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            if( selectedClass != null ) {
                selectedClass.maxKick = config.classes.ParseIndex( cKickLimit.SelectedIndex-1 );
            }
        }

        private void cBanLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            if( selectedClass != null ) {
                selectedClass.maxBan = config.classes.ParseIndex( cBanLimit.SelectedIndex - 1 );
            }
        }

        private void bResetTab_Click( object sender, EventArgs e ) {
            if( MessageBox.Show( "Are you sure you want to reset this tab to defaults?", "Warning", MessageBoxButtons.OKCancel ) == DialogResult.OK ) {
                switch( tabs.SelectedIndex ) {
                    case 0:// General
                        config.LoadDefaultsGeneral();
                        ApplyTabGeneral();
                        break;
                    case 1:// Security
                        config.LoadDefaultsSecurity();
                        ApplyTabSecurity();
                        break;
                    case 2:// Classes
                        config.ResetClasses();
                        ApplyTabClasses();
                        defaultClass = null;
                        RebuildClassList();
                        break;
                    case 3:// TODO: Physics
                        break;
                    case 4:// Saving and Backup
                        config.LoadDefaultsSavingAndBackup();
                        ApplyTabSavingAndBackup();
                        break;
                    case 5:// Logging
                        config.LoadDefaultsLogging();
                        ApplyTabLogging();
                        break;
                    case 6:// Advanced
                        config.LoadDefaultsAdvanced();
                        ApplyTabAdvanced();
                        break;
                }
            }
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

        private void xSpamChatKick_CheckedChanged( object sender, EventArgs e ) {
            nSpamChatWarnings.Enabled = xSpamChatKick.Checked;
        }
    }
}