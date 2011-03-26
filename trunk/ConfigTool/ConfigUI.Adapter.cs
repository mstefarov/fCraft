// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Xml.Linq;
using fCraft;


namespace ConfigTool {
    // This section handles transfer of settings from Config to the specific UI controls, and vice versa.
    // Effectively, it's an adapter between Config's and ConfigUI's representations of the settings
    partial class ConfigUI {

        #region Loading & Applying Config

        void LoadConfig( object sender, EventArgs args ) {
            if( !File.Exists( "worlds.xml" ) && !File.Exists( Paths.ConfigFileName ) ) {
                MessageBox.Show( "Configuration (config.xml) and world list (worlds.xml) were not found. Using defaults." );
            } else if( !File.Exists( Paths.ConfigFileName ) ) {
                MessageBox.Show( "Configuration (config.xml) was not found. Using defaults." );
            } else if( !File.Exists( "worlds.xml" ) ) {
                MessageBox.Show( "World list (worlds.xml) was not found. Assuming 0 worlds." );
            }

            if( Config.Load( false, false ) ) {
                if( Config.errors.Length > 0 ) {
                    MessageBox.Show( Config.errors, "Config loading warnings" );
                }
            } else {
                MessageBox.Show( Config.errors, "Error occured while trying to load config" );
            }

            ApplyTabGeneral();
            ApplyTabChat();
            ApplyTabWorlds(); // also reloads world list
            ApplyTabRanks();
            ApplyTabSecurity();
            ApplyTabSavingAndBackup();
            ApplyTabLogging();
            ApplyTabIRC();
            ApplyTabAdvanced();

            AddChangeHandler( tabs, SomethingChanged );
            AddChangeHandler( bResetTab, SomethingChanged );
            AddChangeHandler( bResetAll, SomethingChanged );

            AddChangeHandler( tabChat, HandleTabChatChange );
            bApply.Enabled = false;
        }


        void LoadWorldList() {
            worlds.Clear();
            if( !File.Exists( "worlds.xml" ) ) return;

            try {
                XDocument doc = XDocument.Load( "worlds.xml" );
                XElement root = doc.Root;

                string errorLog = "";
                foreach( XElement el in root.Elements( "World" ) ) {
                    try {
                        worlds.Add( new WorldListEntry( el ) );
                    } catch( Exception ex ) {
                        errorLog += ex + Environment.NewLine;
                    }
                }
                if( errorLog.Length > 0 ) {
                    MessageBox.Show( "Some errors occured while loading the world list:" + Environment.NewLine + errorLog, "Warning" );
                }

                FillWorldList();
                XAttribute mainWorldAttr = root.Attribute( "main" );
                if( mainWorldAttr != null ) {
                    foreach( WorldListEntry world in worlds ) {
                        if( world.name.ToLower() == mainWorldAttr.Value.ToLower() ) {
                            cMainWorld.SelectedItem = world.name;
                            break;
                        }
                    }
                }

            } catch( Exception ex ) {
                MessageBox.Show( "Error occured while loading the world list: " + Environment.NewLine + ex, "Warning" );
            }
        }


        void ApplyTabGeneral() {
            tServerName.Text = Config.GetString( ConfigKey.ServerName );
            tMOTD.Text = Config.GetString( ConfigKey.MOTD );
            nMaxPlayers.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxPlayers ) );
            FillRankList( cDefaultRank, "(lowest rank)" );
            cDefaultRank.SelectedIndex = RankList.GetIndex( RankList.ParseRank( Config.GetString( ConfigKey.DefaultRank ) ) );
            cPublic.SelectedIndex = Config.GetBool( ConfigKey.IsPublic ) ? 0 : 1;
            nPort.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.Port ) );
            nUploadBandwidth.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.UploadBandwidth ) );

            if( Config.GetString( ConfigKey.IP ) == IPAddress.Any.ToString() ) {
                tIP.Enabled = false;
            } else {
                xIP.Checked = true;
            }
            tIP.Text = Config.GetString( ConfigKey.IP );

            xAnnouncements.Checked = (Config.GetInt( ConfigKey.AnnouncementInterval ) > 0);
            nAnnouncements.Value = Config.GetInt( ConfigKey.AnnouncementInterval );

            // UpdaterSettingsWindow
            updaterWindow.BackupBeforeUpdate = ConfigKey.BackupBeforeUpdate.GetBool();
            updaterWindow.RunBeforeUpdate = ConfigKey.RunBeforeUpdate.GetString();
            updaterWindow.RunAfterUpdate = ConfigKey.RunAfterUpdate.GetString();
            updaterWindow.UpdaterMode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
        }


        void ApplyTabChat() {
            xRankColors.Checked = ConfigKey.RankColorsInChat.GetBool();
            xChatPrefixes.Checked = ConfigKey.RankPrefixesInChat.GetBool();
            xListPrefixes.Checked = ConfigKey.RankPrefixesInList.GetBool();
            xRankColorsInWorldNames.Checked = ConfigKey.RankColorsInWorldNames.GetBool();
            xShowJoinedWorldMessages.Checked = ConfigKey.ShowJoinedWorldMessages.GetBool();
            xShowConnectionMessages.Checked = ConfigKey.ShowConnectionMessages.GetBool();

            colorSys = Color.ParseToIndex( Config.GetString( ConfigKey.SystemMessageColor ) );
            ApplyColor( bColorSys, colorSys );
            Color.Sys = Color.Parse( colorSys );

            colorHelp = Color.ParseToIndex( Config.GetString( ConfigKey.HelpColor ) );
            ApplyColor( bColorHelp, colorHelp );
            Color.Help = Color.Parse( colorHelp );

            colorSay = Color.ParseToIndex( Config.GetString( ConfigKey.SayColor ) );
            ApplyColor( bColorSay, colorSay );
            Color.Say = Color.Parse( colorSay );

            colorAnnouncement = Color.ParseToIndex( Config.GetString( ConfigKey.AnnouncementColor ) );
            ApplyColor( bColorAnnouncement, colorAnnouncement );
            Color.Announcement = Color.Parse( colorAnnouncement );

            colorPM = Color.ParseToIndex( Config.GetString( ConfigKey.PrivateMessageColor ) );
            ApplyColor( bColorPM, colorPM );
            Color.PM = Color.Parse( colorPM );

            colorWarning = Color.ParseToIndex( Config.GetString( ConfigKey.WarningColor ) );
            ApplyColor( bColorWarning, colorWarning );
            Color.Warning = Color.Parse( colorWarning );

            colorMe = Color.ParseToIndex( Config.GetString( ConfigKey.MeColor ) );
            ApplyColor( bColorMe, colorMe );
            Color.Me = Color.Parse( colorMe );

            UpdateChatPreview();
        }


        void ApplyTabWorlds() {
            if( rankNameList == null ) {
                rankNameList = new BindingList<string>();
                rankNameList.Add( WorldListEntry.DefaultRankOption );
                foreach( Rank rank in RankList.Ranks ) {
                    rankNameList.Add( rank.ToComboBoxOption() );
                }
                dgvcAccess.DataSource = rankNameList;
                dgvcBuild.DataSource = rankNameList;
                dgvcBackup.DataSource = World.BackupEnum;

                LoadWorldList();
                dgvWorlds.DataSource = worlds;

            } else {
                //dgvWorlds.DataSource = null;
                rankNameList.Clear();
                rankNameList.Add( WorldListEntry.DefaultRankOption );
                foreach( Rank rank in RankList.Ranks ) {
                    rankNameList.Add( rank.ToComboBoxOption() );
                }
                foreach( WorldListEntry world in worlds ) {
                    world.ReparseRanks();
                }
                worlds.ResetBindings();
                //dgvWorlds.DataSource = worlds;
            }

            FillRankList( cDefaultBuildRank, "(lowest rank)" );
            cDefaultBuildRank.SelectedIndex = RankList.GetIndex( RankList.ParseRank( Config.GetString( ConfigKey.DefaultBuildRank ) ) );

            if( Paths.IsDefaultMapPath( Config.GetString( ConfigKey.MapPath ) ) ) {
                tMapPath.Text = Paths.MapPathDefault;
                xMapPath.Checked = false;
            } else {
                tMapPath.Text = Config.GetString( ConfigKey.MapPath );
                xMapPath.Checked = true;
            }
        }


        void ApplyTabRanks() {
            vRanks.Items.Clear();
            foreach( Rank rank in RankList.Ranks ) {
                vRanks.Items.Add( rank.ToComboBoxOption() );
            }
            DisableRankOptions();
        }


        void ApplyTabSecurity() {

            ApplyEnum( cVerifyNames, ConfigKey.VerifyNames, NameVerificationMode.Balanced );

            xLimitOneConnectionPerIP.Checked = Config.GetBool( ConfigKey.LimitOneConnectionPerIP );
            xAllowUnverifiedLAN.Checked = Config.GetBool( ConfigKey.AllowUnverifiedLAN );

            nSpamChatCount.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamMessageCount ) );
            nSpamChatTimer.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamInterval ) );
            nSpamMute.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamMuteDuration ) );

            xSpamChatKick.Checked = Config.GetInt( ConfigKey.AntispamMaxWarnings ) > 0;
            nSpamChatWarnings.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.AntispamMaxWarnings ) );
            if( !xSpamChatKick.Checked ) nSpamChatWarnings.Enabled = false;

            xRequireBanReason.Checked = Config.GetBool( ConfigKey.RequireBanReason );
            xRequireRankChangeReason.Checked = Config.GetBool( ConfigKey.RequireRankChangeReason );
            xAnnounceKickAndBanReasons.Checked = Config.GetBool( ConfigKey.AnnounceKickAndBanReasons );
            xAnnounceRankChanges.Checked = Config.GetBool( ConfigKey.AnnounceRankChanges );

            FillRankList( cPatrolledRank, "(lowest rank)" );
            cPatrolledRank.SelectedIndex = RankList.GetIndex( RankList.ParseRank( Config.GetString( ConfigKey.PatrolledRank ) ) );

            xPaidPlayersOnly.Checked = Config.GetBool( ConfigKey.PaidPlayersOnly );
        }


        void ApplyTabSavingAndBackup() {
            xSaveOnShutdown.Checked = Config.GetBool( ConfigKey.SaveOnShutdown );
            xSaveInterval.Checked = Config.GetInt( ConfigKey.SaveInterval ) > 0;
            nSaveInterval.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.SaveInterval ) );
            if( !xSaveInterval.Checked ) nSaveInterval.Enabled = false;

            xBackupOnStartup.Checked = Config.GetBool( ConfigKey.BackupOnStartup );
            xBackupOnJoin.Checked = Config.GetBool( ConfigKey.BackupOnJoin );
            xBackupOnlyWhenChanged.Checked = Config.GetBool( ConfigKey.BackupOnlyWhenChanged );
            xBackupInterval.Checked = Config.GetInt( ConfigKey.BackupInterval ) > 0;
            nBackupInterval.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.BackupInterval ) );
            if( !xBackupInterval.Checked ) nBackupInterval.Enabled = false;
            xMaxBackups.Checked = Config.GetInt( ConfigKey.MaxBackups ) > 0;
            nMaxBackups.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxBackups ) );
            if( !xMaxBackups.Checked ) nMaxBackups.Enabled = false;
            xMaxBackupSize.Checked = Config.GetInt( ConfigKey.MaxBackupSize ) > 0;
            nMaxBackupSize.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxBackupSize ) );
            if( !xMaxBackupSize.Checked ) nMaxBackupSize.Enabled = false;
        }


        void ApplyTabLogging() {
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                item.Checked = Logger.ConsoleOptions[item.Index];
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                item.Checked = Logger.LogFileOptions[item.Index];
            }

            ApplyEnum( cLogMode, ConfigKey.LogMode, LogSplittingType.OneFile );

            xLogLimit.Checked = Config.GetInt( ConfigKey.MaxLogs ) > 0;
            nLogLimit.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.MaxLogs ) );
            if( !xLogLimit.Checked ) nLogLimit.Enabled = false;
        }


        void ApplyTabIRC() {
            xIRC.Checked = Config.GetBool( ConfigKey.IRCBotEnabled );
            gIRCNetwork.Enabled = xIRC.Checked;
            gIRCOptions.Enabled = xIRC.Checked;

            tIRCBotNetwork.Text = Config.GetString( ConfigKey.IRCBotNetwork );
            nIRCBotPort.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.IRCBotPort ) );
            nIRCDelay.Value = Config.GetInt( ConfigKey.IRCDelay );

            tIRCBotChannels.Text = Config.GetString( ConfigKey.IRCBotChannels );

            tIRCBotNick.Text = Config.GetString( ConfigKey.IRCBotNick );
            xIRCRegisteredNick.Checked = Config.GetBool( ConfigKey.IRCRegisteredNick );

            tIRCNickServ.Text = Config.GetString( ConfigKey.IRCNickServ );
            tIRCNickServMessage.Text = Config.GetString( ConfigKey.IRCNickServMessage );

            xIRCBotAnnounceIRCJoins.Checked = Config.GetBool( ConfigKey.IRCBotAnnounceIRCJoins );
            xIRCBotAnnounceServerJoins.Checked = Config.GetBool( ConfigKey.IRCBotAnnounceServerJoins );
            xIRCBotForwardFromIRC.Checked = Config.GetBool( ConfigKey.IRCBotForwardFromIRC );
            xIRCBotForwardFromServer.Checked = Config.GetBool( ConfigKey.IRCBotForwardFromServer );


            colorIRC = Color.ParseToIndex( Config.GetString( ConfigKey.IRCMessageColor ) );
            ApplyColor( bColorIRC, colorIRC );
            Color.IRC = Color.Parse( colorIRC );

            xIRCUseColor.Checked = Config.GetBool( ConfigKey.IRCUseColor );
            xIRCBotAnnounceServerEvents.Checked = Config.GetBool( ConfigKey.IRCBotAnnounceServerEvents );
        }


        void ApplyTabAdvanced() {
            xRelayAllBlockUpdates.Checked = Config.GetBool( ConfigKey.RelayAllBlockUpdates );
            xNoPartialPositionUpdates.Checked = Config.GetBool( ConfigKey.NoPartialPositionUpdates );
            nTickInterval.Value = Convert.ToDecimal( Config.GetInt( ConfigKey.TickInterval ) );

            if( ConfigKey.ProcessPriority.IsBlank() ) {
                cProcessPriority.SelectedIndex = 0; // Default
            } else {
                switch( ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>() ) {
                    case ProcessPriorityClass.High:
                        cProcessPriority.SelectedIndex = 1; break;
                    case ProcessPriorityClass.AboveNormal:
                        cProcessPriority.SelectedIndex = 2; break;
                    case ProcessPriorityClass.Normal:
                        cProcessPriority.SelectedIndex = 3; break;
                    case ProcessPriorityClass.BelowNormal:
                        cProcessPriority.SelectedIndex = 4; break;
                    case ProcessPriorityClass.Idle:
                        cProcessPriority.SelectedIndex = 5; break;
                }
            }

            ApplyEnum( cUpdaterMode, ConfigKey.UpdaterMode, UpdaterMode.Prompt );

            nThrottling.Value = Config.GetInt( ConfigKey.BlockUpdateThrottling );
            xLowLatencyMode.Checked = Config.GetBool( ConfigKey.LowLatencyMode );
            xSubmitCrashReports.Checked = Config.GetBool( ConfigKey.SubmitCrashReports );

            xMaxUndo.Checked = Config.GetInt( ConfigKey.MaxUndo ) > 0;
            nMaxUndo.Value = Config.GetInt( ConfigKey.MaxUndo );

            tConsoleName.Text = Config.GetString( ConfigKey.ConsoleName );
        }


        static void ApplyEnum<TEnum>( ComboBox box, ConfigKey key, TEnum def ) where TEnum : struct {
#if DEBUG
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required", "TEnum" );
#endif
            try {
                if( key.IsBlank() ) {
                    box.SelectedIndex = (int)(object)def;
                } else {
                    box.SelectedIndex = (int)Enum.Parse( typeof( TEnum ), Config.GetString( key ), true );
                }
            } catch( ArgumentException ) {
                box.SelectedIndex = (int)(object)def;
            }
        }

        #endregion


        #region Saving Config

        void SaveConfig() {
            Config.errors = "";

            // General
            Config.TrySetValue( ConfigKey.ServerName, tServerName.Text );
            Config.TrySetValue( ConfigKey.MOTD, tMOTD.Text );
            Config.TrySetValue( ConfigKey.MaxPlayers, nMaxPlayers.Value );
            if( cDefaultRank.SelectedIndex == 0 ) {
                Config.TrySetValue( ConfigKey.DefaultRank, "" );
            } else {
                Config.TrySetValue( ConfigKey.DefaultRank, RankList.FindRank( cDefaultRank.SelectedIndex - 1 ) );
            }
            Config.TrySetValue( ConfigKey.IsPublic, cPublic.SelectedIndex == 0 );
            Config.TrySetValue( ConfigKey.Port, nPort.Value );
            Config.TrySetValue( ConfigKey.IP, tIP.Text );

            Config.TrySetValue( ConfigKey.UploadBandwidth, nUploadBandwidth.Value );

            if( xAnnouncements.Checked ) Config.TrySetValue( ConfigKey.AnnouncementInterval, nAnnouncements.Value );
            else Config.TrySetValue( ConfigKey.AnnouncementInterval, 0 );

            // UpdaterSettingsWindow
            ConfigKey.UpdaterMode.TrySetValue( updaterWindow.UpdaterMode );
            ConfigKey.BackupBeforeUpdate.TrySetValue( updaterWindow.BackupBeforeUpdate );
            ConfigKey.RunBeforeUpdate.TrySetValue( updaterWindow.RunBeforeUpdate );
            ConfigKey.RunAfterUpdate.TrySetValue( updaterWindow.RunAfterUpdate );


            // Chat
            Config.TrySetValue( ConfigKey.SystemMessageColor, Color.GetName( colorSys ) );
            Config.TrySetValue( ConfigKey.HelpColor, Color.GetName( colorHelp ) );
            Config.TrySetValue( ConfigKey.SayColor, Color.GetName( colorSay ) );
            Config.TrySetValue( ConfigKey.AnnouncementColor, Color.GetName( colorAnnouncement ) );
            Config.TrySetValue( ConfigKey.PrivateMessageColor, Color.GetName( colorPM ) );
            Config.TrySetValue( ConfigKey.WarningColor, Color.GetName( colorWarning ) );
            Config.TrySetValue( ConfigKey.MeColor, Color.GetName( colorMe ) );
            Config.TrySetValue( ConfigKey.ShowJoinedWorldMessages, xShowJoinedWorldMessages.Checked );
            Config.TrySetValue( ConfigKey.RankColorsInWorldNames, xRankColorsInWorldNames.Checked );
            Config.TrySetValue( ConfigKey.RankColorsInChat, xRankColors.Checked );
            Config.TrySetValue( ConfigKey.RankPrefixesInChat, xChatPrefixes.Checked );
            Config.TrySetValue( ConfigKey.RankPrefixesInList, xListPrefixes.Checked );


            // Worlds
            if( cDefaultBuildRank.SelectedIndex == 0 ) {
                Config.TrySetValue( ConfigKey.DefaultBuildRank, "" );
            } else {
                Config.TrySetValue( ConfigKey.DefaultBuildRank, RankList.FindRank( cDefaultBuildRank.SelectedIndex - 1 ) );
            }

            if( xMapPath.Checked ) Config.TrySetValue( ConfigKey.MapPath, tMapPath.Text );
            else Config.TrySetValue( ConfigKey.MapPath, "" );


            // Security
            WriteEnum<NameVerificationMode>( cVerifyNames, ConfigKey.VerifyNames );
            Config.TrySetValue( ConfigKey.LimitOneConnectionPerIP, xLimitOneConnectionPerIP.Checked );
            Config.TrySetValue( ConfigKey.AllowUnverifiedLAN, xAllowUnverifiedLAN.Checked );

            Config.TrySetValue( ConfigKey.AntispamMessageCount, nSpamChatCount.Value );
            Config.TrySetValue( ConfigKey.AntispamInterval, nSpamChatTimer.Value );
            Config.TrySetValue( ConfigKey.AntispamMuteDuration, nSpamMute.Value );

            if( xSpamChatKick.Checked ) Config.TrySetValue( ConfigKey.AntispamMaxWarnings, nSpamChatWarnings.Value );
            else Config.TrySetValue( ConfigKey.AntispamMaxWarnings, 0 );

            Config.TrySetValue( ConfigKey.RequireBanReason, xRequireBanReason.Checked );
            Config.TrySetValue( ConfigKey.RequireRankChangeReason, xRequireRankChangeReason.Checked );
            Config.TrySetValue( ConfigKey.AnnounceKickAndBanReasons, xAnnounceKickAndBanReasons.Checked );
            Config.TrySetValue( ConfigKey.AnnounceRankChanges, xAnnounceRankChanges.Checked );

            if( cPatrolledRank.SelectedIndex == 0 ) {
                Config.TrySetValue( ConfigKey.PatrolledRank, "" );
            } else {
                Config.TrySetValue( ConfigKey.PatrolledRank, RankList.FindRank( cPatrolledRank.SelectedIndex - 1 ) );
            }
            Config.TrySetValue( ConfigKey.PaidPlayersOnly, xPaidPlayersOnly.Checked );


            // Saving & Backups
            Config.TrySetValue( ConfigKey.SaveOnShutdown, xSaveOnShutdown.Checked );
            if( xSaveInterval.Checked ) Config.TrySetValue( ConfigKey.SaveInterval, nSaveInterval.Value );
            else Config.TrySetValue( ConfigKey.SaveInterval, 0 );
            Config.TrySetValue( ConfigKey.BackupOnStartup, xBackupOnStartup.Checked );
            Config.TrySetValue( ConfigKey.BackupOnJoin, xBackupOnJoin.Checked );
            Config.TrySetValue( ConfigKey.BackupOnlyWhenChanged, xBackupOnlyWhenChanged.Checked );

            if( xBackupInterval.Checked ) Config.TrySetValue( ConfigKey.BackupInterval, nBackupInterval.Value );
            else Config.TrySetValue( ConfigKey.BackupInterval, 0 );
            if( xMaxBackups.Checked ) Config.TrySetValue( ConfigKey.MaxBackups, nMaxBackups.Value );
            else Config.TrySetValue( ConfigKey.MaxBackups, 0 );
            if( xMaxBackupSize.Checked ) Config.TrySetValue( ConfigKey.MaxBackupSize, nMaxBackupSize.Value );
            else Config.TrySetValue( ConfigKey.MaxBackupSize, 0 );


            // Logging
            WriteEnum<LogSplittingType>( cLogMode, ConfigKey.LogMode );
            if( xLogLimit.Checked ) Config.TrySetValue( ConfigKey.MaxLogs, nLogLimit.Value );
            else Config.TrySetValue( ConfigKey.MaxLogs, "0" );
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                Logger.ConsoleOptions[item.Index] = item.Checked;
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                Logger.LogFileOptions[item.Index] = item.Checked;
            }


            // IRC
            Config.TrySetValue( ConfigKey.IRCBotEnabled, xIRC.Checked );

            Config.TrySetValue( ConfigKey.IRCBotNetwork, tIRCBotNetwork.Text );
            Config.TrySetValue( ConfigKey.IRCBotPort, nIRCBotPort.Value );
            Config.TrySetValue( ConfigKey.IRCDelay, nIRCDelay.Value );

            Config.TrySetValue( ConfigKey.IRCBotChannels, tIRCBotChannels.Text );

            Config.TrySetValue( ConfigKey.IRCBotNick, tIRCBotNick.Text );
            Config.TrySetValue( ConfigKey.IRCRegisteredNick, xIRCRegisteredNick.Checked );
            Config.TrySetValue( ConfigKey.IRCNickServ, tIRCNickServ.Text );
            Config.TrySetValue( ConfigKey.IRCNickServMessage, tIRCNickServMessage.Text );

            Config.TrySetValue( ConfigKey.IRCBotAnnounceIRCJoins, xIRCBotAnnounceIRCJoins.Checked );
            Config.TrySetValue( ConfigKey.IRCBotAnnounceServerJoins, xIRCBotAnnounceServerJoins.Checked );
            Config.TrySetValue( ConfigKey.IRCBotAnnounceServerEvents, xIRCBotAnnounceServerEvents.Checked );
            Config.TrySetValue( ConfigKey.IRCBotForwardFromIRC, xIRCBotForwardFromIRC.Checked );
            Config.TrySetValue( ConfigKey.IRCBotForwardFromServer, xIRCBotForwardFromServer.Checked );

            Config.TrySetValue( ConfigKey.IRCMessageColor, Color.GetName( colorIRC ) );
            Config.TrySetValue( ConfigKey.IRCUseColor, xIRCUseColor.Checked );


            // advanced
            Config.TrySetValue( ConfigKey.SubmitCrashReports, xSubmitCrashReports.Checked );

            Config.TrySetValue( ConfigKey.RelayAllBlockUpdates, xRelayAllBlockUpdates.Checked );
            Config.TrySetValue( ConfigKey.NoPartialPositionUpdates, xNoPartialPositionUpdates.Checked );
            Config.TrySetValue( ConfigKey.TickInterval, Convert.ToInt32( nTickInterval.Value ) );

            switch( cProcessPriority.SelectedIndex ) {
                case 0:
                    ConfigKey.ProcessPriority.ResetValue(); break;
                case 1:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.High ); break;
                case 2:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.AboveNormal ); break;
                case 3:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.Normal ); break;
                case 4:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.BelowNormal ); break;
                case 5:
                    ConfigKey.ProcessPriority.TrySetValue( ProcessPriorityClass.Idle ); break;
            }

            Config.TrySetValue( ConfigKey.BlockUpdateThrottling, Convert.ToInt32( nThrottling.Value ) );

            Config.TrySetValue( ConfigKey.LowLatencyMode, xLowLatencyMode.Checked );

            if( xMaxUndo.Checked ) Config.TrySetValue( ConfigKey.MaxUndo, Convert.ToInt32( nMaxUndo.Value ) );
            else Config.TrySetValue( ConfigKey.MaxUndo, 0 );

            Config.TrySetValue( ConfigKey.ConsoleName, tConsoleName.Text );

            SaveWorldList();
        }


        const string WorldListTempFileName = Server.WorldListFileName + ".tmp";
        void SaveWorldList() {
            try {
                XDocument doc = new XDocument();
                XElement root = new XElement( "fCraftWorldList" );
                foreach( WorldListEntry world in worlds ) {
                    root.Add( world.Serialize() );
                }
                if( cMainWorld.SelectedItem != null ) {
                    root.Add( new XAttribute( "main", cMainWorld.SelectedItem ) );
                }
                doc.Add( root );
                doc.Save( WorldListTempFileName );
                if( File.Exists( Server.WorldListFileName ) ) {
                    File.Replace( WorldListTempFileName, Server.WorldListFileName, null, true );
                } else {
                    File.Move( WorldListTempFileName, Server.WorldListFileName );
                }
            } catch( Exception ex ) {
                MessageBox.Show( "An error occured while trying to save world list (worlds.xml):" + Environment.NewLine + ex );
            }
        }


        static void WriteEnum<TEnum>( ComboBox box, ConfigKey key ) where TEnum : struct {
#if DEBUG
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required", "TEnum" );
#endif
            try {
                TEnum val = (TEnum)Enum.Parse( typeof( TEnum ), box.SelectedIndex.ToString(), true );
                Config.TrySetValue( key, val );
            } catch( ArgumentException ) {
                Logger.Log( "ConfigUI.WriteEnum<{0}>: Could not parse value for {1}. Using default ({2}).", LogType.Error,
                            typeof( TEnum ).Name, key, key.GetString() );
            }
        }

        #endregion

    }
}