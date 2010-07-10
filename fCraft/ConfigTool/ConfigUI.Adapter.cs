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
    // This section handles transfer of settings from Config to the specific UI controls, and vice versa
    // Effectively, it's an adapter between Config and ConfigUI representations of the settings
    public partial class ConfigUI : Form {
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
            ApplyTabWorlds();
            ApplyTabClasses();
            ApplyTabSecurity();
            ApplyTabSavingAndBackup();
            ApplyTabLogging();
            ApplyTabIRC();
            ApplyTabAdvanced();

            AddChangeHandler( tabs );
            bApply.Enabled = false;
        }


        void ApplyTabGeneral() {
            tServerName.Text = Config.GetString( ConfigKey.ServerName );
            tMOTD.Text = Config.GetString( ConfigKey.MOTD );
            nMaxPlayers.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxPlayers ) );
            FillClassList( cDefaultClass, "(lowest class)" );
            cDefaultClass.SelectedIndex = ClassList.GetIndex( ClassList.ParseClass( Config.GetString( ConfigKey.DefaultClass ) ) );
            cPublic.SelectedIndex = Config.GetBool( ConfigKey.IsPublic ) ? 0 : 1;
            nPort.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.Port ) );
            nUploadBandwidth.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.UploadBandwidth ) );

            xClassColors.Checked = Config.GetBool( ConfigKey.ClassColorsInChat );
            xChatPrefixes.Checked = Config.GetBool( ConfigKey.ClassPrefixesInChat );
            xListPrefixes.Checked = Config.GetBool( ConfigKey.ClassPrefixesInList );

            colorSys = fCraft.Color.ParseToIndex( Config.GetString( ConfigKey.SystemMessageColor ) );
            ApplyColor( bColorSys, colorSys );
            colorHelp = fCraft.Color.ParseToIndex( Config.GetString( ConfigKey.HelpColor ) );
            ApplyColor( bColorHelp, colorHelp );
            colorSay = fCraft.Color.ParseToIndex( Config.GetString( ConfigKey.SayColor ) );
            ApplyColor( bColorSay, colorSay );
        }


        void ApplyTabWorlds() {
            dgWorlds.Rows.Clear();
            List<string> classes = new List<string>();
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                classes.Add( String.Format( "{0,3} {1,1}{2}", pc.rank, pc.prefix, pc.name ) );
            }
            wAccess.DataSource = classes;
            wBuild.DataSource = classes;
        }


        void ApplyTabClasses() {
            vClasses.Items.Clear();
            foreach( PlayerClass playerClass in ClassList.classesByIndex ) {
                string line = String.Format( "{0,3} {1,1}{2}", playerClass.rank, playerClass.prefix, playerClass.name );
                vClasses.Items.Add( line );
            }
            DisableClassOptions();
        }


        void ApplyTabSecurity() {
            ApplyEnum( cVerifyNames, ConfigKey.VerifyNames, 1, "Never", "Balanced", "Always" );
            xAnnounceUnverified.Checked = Config.GetBool( ConfigKey.AnnounceUnverifiedNames );
            xLimitOneConnectionPerIP.Checked = Config.GetBool( ConfigKey.LimitOneConnectionPerIP );

            nSpamChatCount.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamMessageCount ) );
            nSpamChatTimer.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamInterval ) );
            nSpamMute.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamMuteDuration ) );

            xSpamChatKick.Checked = Config.GetInt( ConfigKey.AntispamMaxWarnings ) > 0;
            nSpamChatWarnings.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamMaxWarnings ) );
            if( !xSpamChatKick.Checked ) nSpamChatWarnings.Enabled = false;

            nSpamBlockCount.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntigriefBlockCount ) );
            nSpamBlockTimer.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntigriefInterval ) );
        }


        void ApplyTabSavingAndBackup() {
            xSaveOnShutdown.Checked = Config.GetBool( ConfigKey.SaveOnShutdown );
            xSaveAtInterval.Checked = Config.GetInt( ConfigKey.SaveInterval ) > 0;
            nSaveInterval.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.SaveInterval ) );
            if( !xSaveAtInterval.Checked ) nSaveInterval.Enabled = false;

            xBackupOnStartup.Checked = Config.GetBool( ConfigKey.BackupOnStartup );
            xBackupOnJoin.Checked = Config.GetBool( ConfigKey.BackupOnJoin );
            xBackupOnlyWhenChanged.Checked = Config.GetBool( ConfigKey.BackupOnlyWhenChanged );
            xBackupAtInterval.Checked = Config.GetInt( ConfigKey.BackupInterval ) > 0;
            nBackupInterval.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.BackupInterval ) );
            if( !xBackupAtInterval.Checked ) nBackupInterval.Enabled = false;
            xMaxBackups.Checked = Config.GetInt( ConfigKey.MaxBackups ) > 0;
            nMaxBackups.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxBackups ) );
            if( !xMaxBackups.Checked ) nMaxBackups.Enabled = false;
            xMaxBackupSize.Checked = Config.GetInt( ConfigKey.MaxBackupSize ) > 0;
            nMaxBackupSize.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxBackupSize ) );
            if( !xMaxBackupSize.Checked ) nMaxBackupSize.Enabled = false;
        }


        void ApplyTabLogging() {
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                item.Checked = Logger.consoleOptions[item.Index];
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                item.Checked = Logger.logFileOptions[item.Index];
            }

            ApplyEnum( cLogMode, ConfigKey.LogMode, 1, "None", "OneFile", "SplitBySession", "SplitByDay" );

            xLogLimit.Checked = Config.GetInt( ConfigKey.MaxLogs ) > 0;
            nLogLimit.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxLogs ) );
            if( !xLogLimit.Checked ) nLogLimit.Enabled = false;
        }


        void ApplyTabIRC() {
            xIRC.Checked = Config.GetBool( ConfigKey.IRCBot );
            tIRCBotNetwork.Text = Config.GetString( ConfigKey.IRCBotNetwork );
            nIRCBotPort.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.IRCBotPort ) );
            tIRCBotChannels.Text = Config.GetString( ConfigKey.IRCBotChannels );

            tIRCBotNick.Text = Config.GetString( ConfigKey.IRCBotNick );
            tIRCBotQuitMsg.Text = Config.GetString( ConfigKey.IRCBotQuitMsg );

            xIRCMsgs.Checked = Config.GetBool( ConfigKey.IRCMsgs );
            xIRCBotForwardFromServer.Checked = Config.GetBool( ConfigKey.IRCBotForwardFromServer );
            xIRCBotForwardFromIRC.Checked = Config.GetBool( ConfigKey.IRCBotForwardFromIRC );

            gIRCNetwork.Enabled = xIRC.Checked;
            gIRCOptions.Enabled = xIRC.Checked;
        }


        void ApplyTabAdvanced() {
            ApplyEnum( cPolicyColor, ConfigKey.PolicyColorCodesInChat, 1, "Disallow", "ConsoleOnly", "Allow" );
            ApplyEnum( cPolicyIllegal, ConfigKey.PolicyIllegalCharacters, 0, "Disallow", "ConsoleOnly", "Allow" );

            xRedundantPacket.Checked = Config.GetBool( ConfigKey.SendRedundantBlockUpdates );
            xPing.Checked = Config.GetInt( ConfigKey.PingInterval ) > 0;
            nPing.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.PingInterval ) );
            if( !xPing.Checked ) nPing.Enabled = false;
            xAbsoluteUpdates.Checked = Config.GetBool( ConfigKey.NoPartialPositionUpdates );
            nTickInterval.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.TickInterval ) );

            ApplyEnum( cProcessPriority, ConfigKey.ProcessPriority, 0, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
            ApplyEnum( cStartup, ConfigKey.RunOnStartup, 1, "Always", "OnUnexpectedShutdown", "Never" );
            ApplyEnum( cUpdater, ConfigKey.AutomaticUpdates, 2, "Disabled", "Notify", "Prompt", "Auto" );

            nThrottling.Value = Config.GetInt( ConfigKey.BlockUpdateThrottling );
            xLowLatencyMode.Checked = Config.GetBool( ConfigKey.LowLatencyMode );
        }


        void ApplyEnum( ComboBox box, ConfigKey key, int def, params string[] options ) {
            int index = Array.IndexOf<string>( options, Config.GetString( key ) );
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
            Config.SetValue( ConfigKey.ServerName, tServerName.Text );
            Config.SetValue( ConfigKey.MOTD, tMOTD.Text );
            Config.SetValue( ConfigKey.MaxPlayers, nMaxPlayers.Value.ToString() );
            if( cDefaultClass.SelectedIndex == 0 ) {
                Config.SetValue( ConfigKey.DefaultClass, "" );
            } else {
                Config.SetValue( ConfigKey.DefaultClass, ClassList.ParseIndex( cDefaultClass.SelectedIndex - 1 ).name );
            }
            Config.SetValue( ConfigKey.IsPublic, cPublic.SelectedIndex == 0 ? "True" : "False" );
            Config.SetValue( ConfigKey.Port, nPort.Value.ToString() );
            Config.SetValue( ConfigKey.UploadBandwidth, nUploadBandwidth.Value.ToString() );
            Config.SetValue( ConfigKey.ClassColorsInChat, xClassColors.Checked.ToString() );
            Config.SetValue( ConfigKey.ClassPrefixesInChat, xChatPrefixes.Checked.ToString() );
            Config.SetValue( ConfigKey.ClassPrefixesInList, xListPrefixes.Checked.ToString() );
            Config.SetValue( ConfigKey.SystemMessageColor, fCraft.Color.GetName( colorSys ) );
            Config.SetValue( ConfigKey.HelpColor, fCraft.Color.GetName( colorHelp ) );
            Config.SetValue( ConfigKey.SayColor, fCraft.Color.GetName( colorSay ) );


            WriteEnum( cVerifyNames, ConfigKey.VerifyNames, "Never", "Balanced", "Always" );
            Config.SetValue( ConfigKey.AnnounceUnverifiedNames, xAnnounceUnverified.Checked.ToString() );
            Config.SetValue( ConfigKey.LimitOneConnectionPerIP, xLimitOneConnectionPerIP.Checked.ToString() );

            Config.SetValue( ConfigKey.AntispamMessageCount, nSpamChatCount.Value.ToString() );
            Config.SetValue( ConfigKey.AntispamInterval, nSpamChatTimer.Value.ToString() );
            Config.SetValue( ConfigKey.AntispamMuteDuration, nSpamMute.Value.ToString() );

            if( xSpamChatKick.Checked ) Config.SetValue( ConfigKey.AntispamMaxWarnings, nSpamChatWarnings.Value.ToString() );
            else Config.SetValue( ConfigKey.AntispamMaxWarnings, "0" );

            Config.SetValue( ConfigKey.AntigriefBlockCount, nSpamBlockCount.Value.ToString() );
            Config.SetValue( ConfigKey.AntigriefInterval, nSpamBlockTimer.Value.ToString() );



            Config.SetValue( ConfigKey.SaveOnShutdown, xSaveOnShutdown.Checked.ToString() );
            if( xSaveAtInterval.Checked ) Config.SetValue( ConfigKey.SaveInterval, nSaveInterval.Value.ToString() );
            else Config.SetValue( ConfigKey.SaveInterval, "0" );
            Config.SetValue( ConfigKey.BackupOnStartup, xBackupOnStartup.Checked.ToString() );
            Config.SetValue( ConfigKey.BackupOnJoin, xBackupOnJoin.Checked.ToString() );
            Config.SetValue( ConfigKey.BackupOnlyWhenChanged, xBackupOnlyWhenChanged.Checked.ToString() );

            if( xBackupAtInterval.Checked ) Config.SetValue( ConfigKey.BackupInterval, nBackupInterval.Value.ToString() );
            else Config.SetValue( ConfigKey.BackupInterval, "0" );
            if( xMaxBackups.Checked ) Config.SetValue( ConfigKey.MaxBackups, nMaxBackups.Value.ToString() );
            else Config.SetValue( ConfigKey.MaxBackups, "0" );
            if( xMaxBackupSize.Checked ) Config.SetValue( ConfigKey.MaxBackupSize, nMaxBackupSize.Value.ToString() );
            else Config.SetValue( ConfigKey.MaxBackupSize, "0" );


            WriteEnum( cLogMode, ConfigKey.LogMode, "None", "OneFile", "SplitBySession", "SplitByDay" );
            if( xLogLimit.Checked ) Config.SetValue( ConfigKey.MaxLogs, nLogLimit.Value.ToString() );
            else Config.SetValue( ConfigKey.MaxLogs, "0" );
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                Logger.consoleOptions[item.Index] = item.Checked;
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                Logger.logFileOptions[item.Index] = item.Checked;
            }


            Config.SetValue( ConfigKey.IRCBot, xIRC.Checked.ToString() );

            Config.SetValue( ConfigKey.IRCBotNetwork, tIRCBotNetwork.Text );
            Config.SetValue( ConfigKey.IRCBotPort, nIRCBotPort.Value.ToString() );
            Config.SetValue( ConfigKey.IRCBotChannels, tIRCBotChannels.Text );

            Config.SetValue( ConfigKey.IRCBotNick, tIRCBotNick.Text );
            Config.SetValue( ConfigKey.IRCBotQuitMsg, tIRCBotQuitMsg.Text );

            Config.SetValue( ConfigKey.IRCMsgs, xIRCMsgs.Checked.ToString() );
            Config.SetValue( ConfigKey.IRCBotForwardFromServer, xIRCBotForwardFromServer.Checked.ToString() );
            Config.SetValue( ConfigKey.IRCBotForwardFromIRC, xIRCBotForwardFromIRC.Checked.ToString() );


            WriteEnum( cPolicyColor, ConfigKey.PolicyColorCodesInChat, "Disallow", "ConsoleOnly", "Allow" );
            WriteEnum( cPolicyIllegal, ConfigKey.PolicyIllegalCharacters, "Disallow", "ConsoleOnly", "Allow" );

            Config.SetValue( ConfigKey.SendRedundantBlockUpdates, xRedundantPacket.Checked.ToString() );
            if( xPing.Checked ) Config.SetValue( ConfigKey.PingInterval, nPing.Value.ToString() );
            else Config.SetValue( ConfigKey.PingInterval, "0" );
            Config.SetValue( ConfigKey.NoPartialPositionUpdates, xAbsoluteUpdates.Checked.ToString() );
            Config.SetValue( ConfigKey.TickInterval, Convert.ToInt32( nTickInterval.Value ).ToString() );

            WriteEnum( cProcessPriority, ConfigKey.ProcessPriority, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
            WriteEnum( cStartup, ConfigKey.RunOnStartup, "Always", "OnUnexpectedShutdown", "Never" );
            WriteEnum( cUpdater, ConfigKey.AutomaticUpdates, "Disabled", "Notify", "Prompt", "Auto" );

            Config.SetValue( ConfigKey.BlockUpdateThrottling, Convert.ToInt32( nThrottling.Value ).ToString() );

            Config.SetValue( ConfigKey.LowLatencyMode, xLowLatencyMode.Checked.ToString() );
        }


        void WriteEnum( ComboBox box, ConfigKey value, params string[] options ) {
            Config.SetValue( value, options[box.SelectedIndex] );
        }

        #endregion
    }
}