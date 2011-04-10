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
                if( Config.Errors.Length > 0 ) {
                    MessageBox.Show( Config.Errors, "Config loading warnings" );
                }
            } else {
                MessageBox.Show( Config.Errors, "Error occured while trying to load config" );
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
            tServerName.Text = ConfigKey.ServerName.GetString();
            tMOTD.Text = ConfigKey.MOTD.GetString();

            nMaxPlayers.Value = ConfigKey.MaxPlayers.GetInt();
            CheckMaxPlayersPerWorldValue();
            nMaxPlayersPerWorld.Value = ConfigKey.MaxPlayersPerWorld.GetInt();

            FillRankList( cDefaultRank, "(lowest rank)" );
            cDefaultRank.SelectedIndex = RankList.GetIndex( RankList.ParseRank( ConfigKey.DefaultRank.GetString() ) );
            cPublic.SelectedIndex = ConfigKey.IsPublic.GetBool() ? 0 : 1;
            nPort.Value = ConfigKey.Port.GetInt();
            nUploadBandwidth.Value = ConfigKey.UploadBandwidth.GetInt();

            xAnnouncements.Checked = (ConfigKey.AnnouncementInterval.GetInt() > 0);
            nAnnouncements.Value = ConfigKey.AnnouncementInterval.GetInt();

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

            colorSys = Color.ParseToIndex( ConfigKey.SystemMessageColor.GetString() );
            ApplyColor( bColorSys, colorSys );
            Color.Sys = Color.Parse( colorSys );

            colorHelp = Color.ParseToIndex( ConfigKey.HelpColor.GetString() );
            ApplyColor( bColorHelp, colorHelp );
            Color.Help = Color.Parse( colorHelp );

            colorSay = Color.ParseToIndex( ConfigKey.SayColor.GetString() );
            ApplyColor( bColorSay, colorSay );
            Color.Say = Color.Parse( colorSay );

            colorAnnouncement = Color.ParseToIndex( ConfigKey.AnnouncementColor.GetString() );
            ApplyColor( bColorAnnouncement, colorAnnouncement );
            Color.Announcement = Color.Parse( colorAnnouncement );

            colorPM = Color.ParseToIndex( ConfigKey.PrivateMessageColor.GetString() );
            ApplyColor( bColorPM, colorPM );
            Color.PM = Color.Parse( colorPM );

            colorWarning = Color.ParseToIndex( ConfigKey.WarningColor.GetString() );
            ApplyColor( bColorWarning, colorWarning );
            Color.Warning = Color.Parse( colorWarning );

            colorMe = Color.ParseToIndex( ConfigKey.MeColor.GetString() );
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
            cDefaultBuildRank.SelectedIndex = RankList.GetIndex( RankList.ParseRank( ConfigKey.DefaultBuildRank.GetString() ) );

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

            nMaxConnectionsPerIP.Value = ConfigKey.MaxConnectionsPerIP.GetInt();
            xMaxConnectionsPerIP.Checked = (nMaxConnectionsPerIP.Value > 0);
            xAllowUnverifiedLAN.Checked = ConfigKey.AllowUnverifiedLAN.GetBool();

            nSpamChatCount.Value = ConfigKey.AntispamMessageCount.GetInt();
            nSpamChatTimer.Value = ConfigKey.AntispamInterval.GetInt();
            nSpamMute.Value = ConfigKey.AntispamMuteDuration.GetInt();

            xSpamChatKick.Checked = (ConfigKey.AntispamMaxWarnings.GetInt() > 0);
            nSpamChatWarnings.Value = ConfigKey.AntispamMaxWarnings.GetInt();
            if( !xSpamChatKick.Checked ) nSpamChatWarnings.Enabled = false;

            xRequireKickReason.Checked = ConfigKey.RequireKickReason.GetBool();
            xRequireBanReason.Checked = ConfigKey.RequireBanReason.GetBool();
            xRequireRankChangeReason.Checked = ConfigKey.RequireRankChangeReason.GetBool();
            xAnnounceKickAndBanReasons.Checked = ConfigKey.AnnounceKickAndBanReasons.GetBool();
            xAnnounceRankChanges.Checked = ConfigKey.AnnounceRankChanges.GetBool();
            xAnnounceRankChangeReasons.Checked = ConfigKey.RequireRankChangeReason.GetBool();
            xAnnounceRankChangeReasons.Enabled = xAnnounceRankChanges.Checked;

            FillRankList( cPatrolledRank, "(lowest rank)" );
            cPatrolledRank.SelectedIndex = RankList.GetIndex( RankList.ParseRank( ConfigKey.PatrolledRank.GetString() ) );

            xPaidPlayersOnly.Checked = ConfigKey.PaidPlayersOnly.GetBool();
        }


        void ApplyTabSavingAndBackup() {
            xSaveOnShutdown.Checked = ConfigKey.SaveOnShutdown.GetBool();

            xSaveInterval.Checked = (ConfigKey.SaveInterval.GetInt() > 0);
            nSaveInterval.Value = ConfigKey.SaveInterval.GetInt();
            if( !xSaveInterval.Checked ) nSaveInterval.Enabled = false;

            xBackupOnStartup.Checked = ConfigKey.BackupOnStartup.GetBool();
            xBackupOnJoin.Checked = ConfigKey.BackupOnJoin.GetBool();
            xBackupOnlyWhenChanged.Checked = ConfigKey.BackupOnlyWhenChanged.GetBool();

            xBackupInterval.Checked = (ConfigKey.BackupInterval.GetInt() > 0);
            nBackupInterval.Value = ConfigKey.BackupInterval.GetInt();
            if( !xBackupInterval.Checked ) nBackupInterval.Enabled = false;

            xMaxBackups.Checked = (ConfigKey.MaxBackups.GetInt() > 0);
            nMaxBackups.Value = ConfigKey.MaxBackups.GetInt();
            if( !xMaxBackups.Checked ) nMaxBackups.Enabled = false;

            xMaxBackupSize.Checked = (ConfigKey.MaxBackupSize.GetInt() > 0);
            nMaxBackupSize.Value = ConfigKey.MaxBackupSize.GetInt();
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

            xLogLimit.Checked = (ConfigKey.MaxLogs.GetInt() > 0);
            nLogLimit.Value = ConfigKey.MaxLogs.GetInt();
            if( !xLogLimit.Checked ) nLogLimit.Enabled = false;
        }


        void ApplyTabIRC() {
            xIRC.Checked = ConfigKey.IRCBotEnabled.GetBool();
            gIRCNetwork.Enabled = xIRC.Checked;
            gIRCOptions.Enabled = xIRC.Checked;

            tIRCBotNetwork.Text = ConfigKey.IRCBotNetwork.GetString();
            nIRCBotPort.Value = ConfigKey.IRCBotPort.GetInt();
            nIRCDelay.Value = ConfigKey.IRCDelay.GetInt();

            tIRCBotChannels.Text = ConfigKey.IRCBotChannels.GetString();

            tIRCBotNick.Text = ConfigKey.IRCBotNick.GetString();
            xIRCRegisteredNick.Checked = ConfigKey.IRCRegisteredNick.GetBool();

            tIRCNickServ.Text = ConfigKey.IRCNickServ.GetString();
            tIRCNickServMessage.Text = ConfigKey.IRCNickServMessage.GetString();

            xIRCBotAnnounceIRCJoins.Checked = ConfigKey.IRCBotAnnounceIRCJoins.GetBool();
            xIRCBotAnnounceServerJoins.Checked = ConfigKey.IRCBotAnnounceServerJoins.GetBool();
            xIRCBotForwardFromIRC.Checked = ConfigKey.IRCBotForwardFromIRC.GetBool();
            xIRCBotForwardFromServer.Checked = ConfigKey.IRCBotForwardFromServer.GetBool();


            colorIRC = Color.ParseToIndex( ConfigKey.IRCMessageColor.GetString() );
            ApplyColor( bColorIRC, colorIRC );
            Color.IRC = Color.Parse( colorIRC );

            xIRCUseColor.Checked = ConfigKey.IRCUseColor.GetBool();
            xIRCBotAnnounceServerEvents.Checked = ConfigKey.IRCBotAnnounceServerEvents.GetBool();
        }


        void ApplyTabAdvanced() {
            xRelayAllBlockUpdates.Checked = ConfigKey.RelayAllBlockUpdates.GetBool();
            xNoPartialPositionUpdates.Checked = ConfigKey.NoPartialPositionUpdates.GetBool();
            nTickInterval.Value = ConfigKey.TickInterval.GetInt();

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

            nThrottling.Value = ConfigKey.BlockUpdateThrottling.GetInt();
            xLowLatencyMode.Checked = ConfigKey.LowLatencyMode.GetBool();
            xSubmitCrashReports.Checked = ConfigKey.SubmitCrashReports.GetBool();

            xMaxUndo.Checked = (ConfigKey.MaxUndo.GetInt() > 0);
            nMaxUndo.Value = ConfigKey.MaxUndo.GetInt();

            tConsoleName.Text = ConfigKey.ConsoleName.GetString();

            tIP.Text = ConfigKey.IP.GetString();
            if( ConfigKey.IP.IsBlank() || ConfigKey.IP.IsDefault() ) {
                tIP.Enabled = false;
                xIP.Checked = false;
            } else {
                tIP.Enabled = true;
                xIP.Checked = true;
            }
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
            Config.Errors = "";

            // General
            Config.TrySetValue( ConfigKey.ServerName, tServerName.Text );
            Config.TrySetValue( ConfigKey.MOTD, tMOTD.Text );
            Config.TrySetValue( ConfigKey.MaxPlayers, nMaxPlayers.Value );
            Config.TrySetValue( ConfigKey.MaxPlayersPerWorld, nMaxPlayersPerWorld.Value );
            if( cDefaultRank.SelectedIndex == 0 ) {
                Config.TrySetValue( ConfigKey.DefaultRank, "" );
            } else {
                Config.TrySetValue( ConfigKey.DefaultRank, RankList.FindRank( cDefaultRank.SelectedIndex - 1 ) );
            }
            Config.TrySetValue( ConfigKey.IsPublic, cPublic.SelectedIndex == 0 );
            Config.TrySetValue( ConfigKey.Port, nPort.Value );
            if( xIP.Checked ) {
                ConfigKey.IP.TrySetValue( tIP.Text );
            } else {
                ConfigKey.IP.ResetValue();
            }

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

            if( xMaxConnectionsPerIP.Checked ) {
                Config.TrySetValue( ConfigKey.MaxConnectionsPerIP, nMaxConnectionsPerIP.Value );
            }
            Config.TrySetValue( ConfigKey.AllowUnverifiedLAN, xAllowUnverifiedLAN.Checked );

            Config.TrySetValue( ConfigKey.AntispamMessageCount, nSpamChatCount.Value );
            Config.TrySetValue( ConfigKey.AntispamInterval, nSpamChatTimer.Value );
            Config.TrySetValue( ConfigKey.AntispamMuteDuration, nSpamMute.Value );

            if( xSpamChatKick.Checked ) Config.TrySetValue( ConfigKey.AntispamMaxWarnings, nSpamChatWarnings.Value );
            else Config.TrySetValue( ConfigKey.AntispamMaxWarnings, 0 );

            Config.TrySetValue( ConfigKey.RequireKickReason, xRequireKickReason.Checked );
            Config.TrySetValue( ConfigKey.RequireBanReason, xRequireBanReason.Checked );
            Config.TrySetValue( ConfigKey.RequireRankChangeReason, xRequireRankChangeReason.Checked );
            Config.TrySetValue( ConfigKey.AnnounceKickAndBanReasons, xAnnounceKickAndBanReasons.Checked );
            Config.TrySetValue( ConfigKey.AnnounceRankChanges, xAnnounceRankChanges.Checked );
            Config.TrySetValue( ConfigKey.AnnounceRankChangeReasons, xAnnounceRankChangeReasons.Checked );

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
                Paths.MoveOrReplace( WorldListTempFileName, Server.WorldListFileName );
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