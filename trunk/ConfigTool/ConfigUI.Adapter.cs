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
                    MessageBox.Show(
                        "Some errors occured while loading the world list:" + Environment.NewLine + errorLog, "Warning" );
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
                item.Checked = Logger.consoleOptions[item.Index];
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                item.Checked = Logger.logFileOptions[item.Index];
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

            if( ConfigKey.ProcessPriority.IsEmpty() ) {
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

            ApplyEnum( cUpdater, ConfigKey.UpdateMode, UpdaterMode.Prompt );

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
                if( key.IsEmpty() ) {
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
            Config.SetValue( ConfigKey.ServerName, tServerName.Text );
            Config.SetValue( ConfigKey.MOTD, tMOTD.Text );
            Config.SetValue( ConfigKey.MaxPlayers, nMaxPlayers.Value );
            if( cDefaultRank.SelectedIndex == 0 ) {
                Config.SetValue( ConfigKey.DefaultRank, "" );
            } else {
                Config.SetValue( ConfigKey.DefaultRank, RankList.FindRank( cDefaultRank.SelectedIndex - 1 ) );
            }
            Config.SetValue( ConfigKey.IsPublic, cPublic.SelectedIndex == 0 );
            Config.SetValue( ConfigKey.Port, nPort.Value );
            Config.SetValue( ConfigKey.IP, tIP.Text );

            Config.SetValue( ConfigKey.UploadBandwidth, nUploadBandwidth.Value );

            if( xAnnouncements.Checked ) Config.SetValue( ConfigKey.AnnouncementInterval, nAnnouncements.Value );
            else Config.SetValue( ConfigKey.AnnouncementInterval, 0 );


            // Chat
            Config.SetValue( ConfigKey.SystemMessageColor, Color.GetName( colorSys ) );
            Config.SetValue( ConfigKey.HelpColor, Color.GetName( colorHelp ) );
            Config.SetValue( ConfigKey.SayColor, Color.GetName( colorSay ) );
            Config.SetValue( ConfigKey.AnnouncementColor, Color.GetName( colorAnnouncement ) );
            Config.SetValue( ConfigKey.PrivateMessageColor, Color.GetName( colorPM ) );
            Config.SetValue( ConfigKey.WarningColor, Color.GetName( colorWarning ) );
            Config.SetValue( ConfigKey.MeColor, Color.GetName( colorMe ) );
            Config.SetValue( ConfigKey.ShowJoinedWorldMessages, xShowJoinedWorldMessages.Checked );
            Config.SetValue( ConfigKey.RankColorsInWorldNames, xRankColorsInWorldNames.Checked );
            Config.SetValue( ConfigKey.RankColorsInChat, xRankColors.Checked );
            Config.SetValue( ConfigKey.RankPrefixesInChat, xChatPrefixes.Checked );
            Config.SetValue( ConfigKey.RankPrefixesInList, xListPrefixes.Checked );


            // Worlds
            if( cDefaultBuildRank.SelectedIndex == 0 ) {
                Config.SetValue( ConfigKey.DefaultBuildRank, "" );
            } else {
                Config.SetValue( ConfigKey.DefaultBuildRank, RankList.FindRank( cDefaultBuildRank.SelectedIndex - 1 ) );
            }

            if( xMapPath.Checked ) Config.SetValue( ConfigKey.MapPath, tMapPath.Text );
            else Config.SetValue( ConfigKey.MapPath, "" );


            // Security
            WriteEnum<NameVerificationMode>( cVerifyNames, ConfigKey.VerifyNames );
            Config.SetValue( ConfigKey.LimitOneConnectionPerIP, xLimitOneConnectionPerIP.Checked );
            Config.SetValue( ConfigKey.AllowUnverifiedLAN, xAllowUnverifiedLAN.Checked );

            Config.SetValue( ConfigKey.AntispamMessageCount, nSpamChatCount.Value );
            Config.SetValue( ConfigKey.AntispamInterval, nSpamChatTimer.Value );
            Config.SetValue( ConfigKey.AntispamMuteDuration, nSpamMute.Value );

            if( xSpamChatKick.Checked ) Config.SetValue( ConfigKey.AntispamMaxWarnings, nSpamChatWarnings.Value );
            else Config.SetValue( ConfigKey.AntispamMaxWarnings, 0 );

            Config.SetValue( ConfigKey.RequireBanReason, xRequireBanReason.Checked );
            Config.SetValue( ConfigKey.RequireRankChangeReason, xRequireRankChangeReason.Checked );
            Config.SetValue( ConfigKey.AnnounceKickAndBanReasons, xAnnounceKickAndBanReasons.Checked );
            Config.SetValue( ConfigKey.AnnounceRankChanges, xAnnounceRankChanges.Checked );

            if( cPatrolledRank.SelectedIndex == 0 ) {
                Config.SetValue( ConfigKey.PatrolledRank, "" );
            } else {
                Config.SetValue( ConfigKey.PatrolledRank, RankList.FindRank( cPatrolledRank.SelectedIndex - 1 ) );
            }
            Config.SetValue( ConfigKey.PaidPlayersOnly, xPaidPlayersOnly.Checked );


            // Saving & Backups
            Config.SetValue( ConfigKey.SaveOnShutdown, xSaveOnShutdown.Checked );
            if( xSaveInterval.Checked ) Config.SetValue( ConfigKey.SaveInterval, nSaveInterval.Value );
            else Config.SetValue( ConfigKey.SaveInterval, 0 );
            Config.SetValue( ConfigKey.BackupOnStartup, xBackupOnStartup.Checked );
            Config.SetValue( ConfigKey.BackupOnJoin, xBackupOnJoin.Checked );
            Config.SetValue( ConfigKey.BackupOnlyWhenChanged, xBackupOnlyWhenChanged.Checked );

            if( xBackupInterval.Checked ) Config.SetValue( ConfigKey.BackupInterval, nBackupInterval.Value );
            else Config.SetValue( ConfigKey.BackupInterval, 0 );
            if( xMaxBackups.Checked ) Config.SetValue( ConfigKey.MaxBackups, nMaxBackups.Value );
            else Config.SetValue( ConfigKey.MaxBackups, 0 );
            if( xMaxBackupSize.Checked ) Config.SetValue( ConfigKey.MaxBackupSize, nMaxBackupSize.Value );
            else Config.SetValue( ConfigKey.MaxBackupSize, 0 );


            // Logging
            WriteEnum<LogSplittingType>( cLogMode, ConfigKey.LogMode );
            if( xLogLimit.Checked ) Config.SetValue( ConfigKey.MaxLogs, nLogLimit.Value );
            else Config.SetValue( ConfigKey.MaxLogs, "0" );
            foreach( ListViewItem item in vConsoleOptions.Items ) {
                Logger.consoleOptions[item.Index] = item.Checked;
            }
            foreach( ListViewItem item in vLogFileOptions.Items ) {
                Logger.logFileOptions[item.Index] = item.Checked;
            }


            // IRC
            Config.SetValue( ConfigKey.IRCBotEnabled, xIRC.Checked );

            Config.SetValue( ConfigKey.IRCBotNetwork, tIRCBotNetwork.Text );
            Config.SetValue( ConfigKey.IRCBotPort, nIRCBotPort.Value );
            Config.SetValue( ConfigKey.IRCDelay, nIRCDelay.Value );

            Config.SetValue( ConfigKey.IRCBotChannels, tIRCBotChannels.Text );

            Config.SetValue( ConfigKey.IRCBotNick, tIRCBotNick.Text );
            Config.SetValue( ConfigKey.IRCRegisteredNick, xIRCRegisteredNick.Checked );
            Config.SetValue( ConfigKey.IRCNickServ, tIRCNickServ.Text );
            Config.SetValue( ConfigKey.IRCNickServMessage, tIRCNickServMessage.Text );

            Config.SetValue( ConfigKey.IRCBotAnnounceIRCJoins, xIRCBotAnnounceIRCJoins.Checked );
            Config.SetValue( ConfigKey.IRCBotAnnounceServerJoins, xIRCBotAnnounceServerJoins.Checked );
            Config.SetValue( ConfigKey.IRCBotAnnounceServerEvents, xIRCBotAnnounceServerEvents.Checked );
            Config.SetValue( ConfigKey.IRCBotForwardFromIRC, xIRCBotForwardFromIRC.Checked );
            Config.SetValue( ConfigKey.IRCBotForwardFromServer, xIRCBotForwardFromServer.Checked );

            Config.SetValue( ConfigKey.IRCMessageColor, Color.GetName( colorIRC ) );
            Config.SetValue( ConfigKey.IRCUseColor, xIRCUseColor.Checked );


            // advanced
            Config.SetValue( ConfigKey.SubmitCrashReports, xSubmitCrashReports.Checked );
            WriteEnum<UpdaterMode>( cUpdater, ConfigKey.UpdateMode );

            Config.SetValue( ConfigKey.RelayAllBlockUpdates, xRelayAllBlockUpdates.Checked );
            Config.SetValue( ConfigKey.NoPartialPositionUpdates, xNoPartialPositionUpdates.Checked );
            Config.SetValue( ConfigKey.TickInterval, Convert.ToInt32( nTickInterval.Value ) );

            switch( cProcessPriority.SelectedIndex ) {
                case 0:
                    ConfigKey.ProcessPriority.ResetValue(); break;
                case 1:
                    ConfigKey.ProcessPriority.SetValue( ProcessPriorityClass.High ); break;
                case 2:
                    ConfigKey.ProcessPriority.SetValue( ProcessPriorityClass.AboveNormal ); break;
                case 3:
                    ConfigKey.ProcessPriority.SetValue( ProcessPriorityClass.Normal ); break;
                case 4:
                    ConfigKey.ProcessPriority.SetValue( ProcessPriorityClass.BelowNormal ); break;
                case 5:
                    ConfigKey.ProcessPriority.SetValue( ProcessPriorityClass.Idle ); break;
            }

            Config.SetValue( ConfigKey.BlockUpdateThrottling, Convert.ToInt32( nThrottling.Value ) );

            Config.SetValue( ConfigKey.LowLatencyMode, xLowLatencyMode.Checked );

            if( xMaxUndo.Checked ) Config.SetValue( ConfigKey.MaxUndo, Convert.ToInt32( nMaxUndo.Value ) );
            else Config.SetValue( ConfigKey.MaxUndo, 0 );

            Config.SetValue( ConfigKey.ConsoleName, tConsoleName.Text );

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
                Config.SetValue( key, val );
            } catch( ArgumentException ) {
                Logger.Log( "ConfigUI.WriteEnum<{0}>: Could not parse value for {1}. Using default ({2}).", LogType.Error,
                            typeof( TEnum ).Name, key, key.GetString() );
            }
        }

        #endregion

    }
}