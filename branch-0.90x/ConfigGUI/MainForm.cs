// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using fCraft.GUI;
using fCraft.MapGeneration;
using JetBrains.Annotations;


namespace fCraft.ConfigGUI {
    public sealed partial class MainForm : Form {
        static MainForm instance;
        readonly Font bold;
        Rank selectedRank;
        UpdaterSettingsPopup updaterWindow;
        internal static SortableBindingList<WorldListEntry> Worlds;

        #region Initialization

        public MainForm() {
            instance = this;
            InitializeComponent();
            dgvcBlockDB.TrueValue = YesNoAuto.Yes;
            dgvcBlockDB.FalseValue = YesNoAuto.No;
            dgvcBlockDB.IndeterminateValue = YesNoAuto.Auto;
            bold = new Font( Font, FontStyle.Bold );
            Text = "fCraft Configuration (" + Updater.CurrentRelease.VersionString + ")";
        }


        protected override void OnShown( EventArgs e ) {
            base.OnShown( e );

            if( typeof( Server ).Assembly.GetName().Version != typeof( Program ).Assembly.GetName().Version ) {
                MessageBox.Show( "fCraft.dll version does not match ConfigGUI.exe version." );
                Application.Exit();
                return;
            }

            // fills Permission and LogType lists
            FillEnumLists();

            // create hidden boxes for permission limits
            FillPermissionLimitBoxes();

            // fill out all the tool tips
            FillToolTipsGeneral();
            FillToolTipsChat();
            FillToolTipsWorlds();
            FillToolTipsRanks();
            FillToolTipsSecurity();
            FillToolTipsSavingAndBackup();
            FillToolTipsLogging();
            FillToolTipsIRC();
            FillToolTipsAdvanced();

            FillIRCNetworkList( false );

            // Initialize fCraft's args, paths, and logging backend.
            Server.InitLibrary( Environment.GetCommandLineArgs() );
            MapGenUtil.Init();

            dgvWorlds.DataError += WorldListErrorHandler;

            LoadConfig();

            // Redraw chat preview when re-entering the tab.
            // This ensured that changes to rank colors/prefixes are applied.
            tabChat.Enter += ( o, e2 ) => UpdateChatPreview();

            bReadme.Enabled = File.Exists( ReadmeFileName );
            bChangelog.Enabled = File.Exists( ChangelogFileName );
        }


        void FillEnumLists() {
            foreach( Permission permission in Enum.GetValues( typeof( Permission ) ) ) {
                ListViewItem item = new ListViewItem( permission.ToString() ) {Tag = permission};
                vPermissions.Items.Add( item );
            }

            foreach( LogType type in Enum.GetValues( typeof( LogType ) ) ) {
                if( type == LogType.Trace ) continue;
                ListViewItem item = new ListViewItem( type.ToString() ) {Tag = type};
                vLogFileOptions.Items.Add( item );
                vConsoleOptions.Items.Add( (ListViewItem)item.Clone() );
            }
        }


        void FillWorldList() {
            cMainWorld.Items.Clear();
            foreach( WorldListEntry world in Worlds ) {
                cMainWorld.Items.Add( world.Name );
            }
        }

        #endregion

        #region Input Handlers

        #region General

        void bMeasure_Click( object sender, EventArgs e ) {
            try {
                Process.Start( "http://www.speedtest.net/" );
            } catch( Exception ) {
                MessageBox.Show( "Please go to speedtest.net" );
            }
        }


        void bAnnouncements_Click( object sender, EventArgs e ) {
            TextEditorPopup popup = new TextEditorPopup( Paths.AnnouncementsFileName, "" );
            popup.ShowDialog();
        }


        void xAnnouncements_CheckedChanged( object sender, EventArgs e ) {
            nAnnouncements.Enabled = xAnnouncements.Checked;
            bAnnouncements.Enabled = xAnnouncements.Checked;
        }


        void bPortCheck_Click( object sender, EventArgs e ) {
            bPortCheck.Text = "Checking";
            Enabled = false;
            TcpListener listener = null;

            try {
                listener = new TcpListener( IPAddress.Any, (int)nPort.Value );
                listener.Start();

                HttpWebRequest request =
                    (HttpWebRequest)WebRequest.Create( "http://www.fcraft.net/portcheck.php?port=" + nPort.Value );
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if( response.StatusCode == HttpStatusCode.OK ) {
                    using( Stream stream = response.GetResponseStream() ) {
                        if( stream != null ) {
                            StreamReader reader = new StreamReader( stream );
                            string returnMessage = reader.ReadLine();
                            if( returnMessage != null && returnMessage[0] == '1' ) {
                                MessageBox.Show( "Port " + nPort.Value + " is open!", "Port check success" );
                                return;
                            }
                        }
                    }
                }
                MessageBox.Show( "Port " + nPort.Value + " is closed. You will need to set up forwarding.",
                                 "Port check failed" );
            } catch {
                MessageBox.Show(
                    "Could not start listening on port " + nPort.Value + ". Another program may be using the port.",
                    "Port check failed" );
            } finally {
                if( listener != null ) {
                    listener.Stop();
                }
                Enabled = true;
                bPortCheck.Text = "Check";
            }
        }


        void tIP_Validating( object sender, CancelEventArgs e ) {
            IPAddress IP;
            if( IPAddressUtil.IsIP( tIP.Text ) && IPAddress.TryParse( tIP.Text, out IP ) ) {
                tIP.ForeColor = SystemColors.ControlText;
            } else {
                tIP.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            }
        }


        void xIP_CheckedChanged( object sender, EventArgs e ) {
            tIP.Enabled = xIP.Checked;
        }


        void bGreeting_Click( object sender, EventArgs e ) {
            TextEditorPopup popup = new TextEditorPopup( Paths.GreetingFileName,
                                                         @"Welcome to {SERVER_NAME}
Your rank is {RANK}&S. Type &H/Help&S for help." );
            popup.ShowDialog();
        }


        void bShowAdvancedUpdaterSettings_Click( object sender, EventArgs e ) {
            if( updaterWindow == null ) {
                updaterWindow = new UpdaterSettingsPopup();
                ApplyUpdaterWindow();
            }
            updaterWindow.UpdaterMode = (UpdaterMode)cUpdaterMode.SelectedIndex;
            updaterWindow.ShowDialog();
            cUpdaterMode.SelectedIndex = (int)updaterWindow.UpdaterMode;
        }


        const string WikiUri = "http://www.fcraft.net/wiki/Main_Page";


        void bOpenWiki_Click( object sender, EventArgs e ) {
            try {
                Process.Start( WikiUri );
            } catch( Exception ) {
                Clipboard.SetText( WikiUri, TextDataFormat.Text );
                MessageBox.Show( "Link to the wiki has been copied to clipboard." );
            }
        }


        const string ReportUri = "http://forum.fcraft.net/viewforum.php?f=5";


        void bReportABug_Click( object sender, EventArgs e ) {
            try {
                Process.Start( ReportUri );
            } catch( Exception ) {
                Clipboard.SetText( ReportUri, TextDataFormat.Text );
                MessageBox.Show( "Link to the bug report forum has been copied to clipboard." );
            }
        }


        void nMaxPlayerPerWorld_Validating( object sender, CancelEventArgs e ) {
            CheckMaxPlayersPerWorldValue();
        }


        void nMaxPlayers_ValueChanged( object sender, EventArgs e ) {
            CheckMaxPlayersPerWorldValue();
        }


        void bCredits_Click( object sender, EventArgs e ) {
            new AboutWindow().Show();
        }

        #endregion

        #region Worlds

        void WorldListErrorHandler( object sender, DataGridViewDataErrorEventArgs e ) {
            if( e.Exception is FormatException ) {
                string columnName = dgvWorlds.Columns[e.ColumnIndex].HeaderText;
                MessageBox.Show( e.Exception.Message, "Error editing " + columnName );
            } else {
                MessageBox.Show( e.Exception.ToString(), "An error occurred in the world list" );
            }
        }


        void bAddWorld_Click( object sender, EventArgs e ) {
            using( AddWorldPopup popup = new AddWorldPopup( null ) ) {
                if( popup.ShowDialog() == DialogResult.OK ) {
                    Worlds.Add( popup.World );
                    popup.World.LoadedBy = WorldListEntry.WorldInfoSignature;
                    popup.World.LoadedOn = DateTime.UtcNow;
                }
                if( cMainWorld.SelectedItem == null ) {
                    FillWorldList();
                    if( cMainWorld.Items.Count > 0 ) {
                        cMainWorld.SelectedIndex = 0;
                    }
                } else {
                    string mainWorldName = cMainWorld.SelectedItem.ToString();
                    FillWorldList();
                    cMainWorld.SelectedItem = mainWorldName;
                }
            }
        }


        void bWorldEdit_Click( object sender, EventArgs e ) {
            using( AddWorldPopup popup = new AddWorldPopup( Worlds[dgvWorlds.SelectedRows[0].Index] ) ) {
                if( popup.ShowDialog() == DialogResult.OK ) {
                    string oldName = Worlds[dgvWorlds.SelectedRows[0].Index].Name;
                    Worlds[dgvWorlds.SelectedRows[0].Index] = popup.World;
                    HandleWorldRename( oldName, popup.World.Name );
                }
            }
        }


        void dgvWorlds_Click( object sender, EventArgs e ) {
            bool oneRowSelected = (dgvWorlds.SelectedRows.Count == 1);
            bWorldDelete.Enabled = oneRowSelected;
            bWorldEdit.Enabled = oneRowSelected;
        }


        void bWorldDel_Click( object sender, EventArgs e ) {
            if( dgvWorlds.SelectedRows.Count > 0 ) {
                WorldListEntry world = Worlds[dgvWorlds.SelectedRows[0].Index];

                // prompt to delete map file, if it exists
                if( File.Exists( world.FullFileName ) ) {
                    string promptMessage = String.Format( "Are you sure you want to delete world \"{0}\"?", world.Name );

                    if( MessageBox.Show( promptMessage, "Deleting a world", MessageBoxButtons.YesNo ) == DialogResult.No ) {
                        return;
                    }

                    string fileDeleteWarning = "Do you want to delete the map file (" + world.FileName + ") as well?";
                    if( MessageBox.Show( fileDeleteWarning, "Warning", MessageBoxButtons.YesNo ) == DialogResult.Yes ) {
                        try {
                            File.Delete( world.FullFileName );
                        } catch( Exception ex ) {
                            MessageBox.Show( "You have to delete the file (" + world.FileName + ") manually. " +
                                             "An error occurred while trying to delete it automatically:" +
                                             Environment.NewLine + ex,
                                             "Could not delete map file" );
                        }
                    }
                }

                Worlds.Remove( world );

                if( cMainWorld.SelectedItem == null ) {
                    // deleting non-main world
                    FillWorldList();
                    if( cMainWorld.Items.Count > 0 ) {
                        cMainWorld.SelectedIndex = 0;
                    }
                } else {
                    // deleting main world
                    string mainWorldName = cMainWorld.SelectedItem.ToString();
                    FillWorldList();
                    if( mainWorldName == world.Name ) {
                        MessageBox.Show( "Main world has been reset." );
                        if( cMainWorld.Items.Count > 0 ) {
                            cMainWorld.SelectedIndex = 0;
                        }
                    } else {
                        cMainWorld.SelectedItem = mainWorldName;
                    }
                }
            }
        }


        void bMapPath_Click( object sender, EventArgs e ) {
            FolderBrowserDialog dialog = new FolderBrowserDialog {
                SelectedPath = tMapPath.Text,
                Description = "Select a directory to save map files to"
            };
            if( dialog.ShowDialog() == DialogResult.OK ) {
                tMapPath.Text = dialog.SelectedPath;
            }
        }

        #endregion

        #region Security

        void cVerifyNames_SelectedIndexChanged( object sender, EventArgs e ) {
            xAllowUnverifiedLAN.Enabled = (cVerifyNames.SelectedIndex != 0);
            xAllowUnverifiedLAN.Checked = !xAllowUnverifiedLAN.Enabled;
        }


        void xMaxConnectionsPerIP_CheckedChanged( object sender, EventArgs e ) {
            nMaxConnectionsPerIP.Enabled = xMaxConnectionsPerIP.Checked;
        }

        #endregion

        #region Logging

        void vConsoleOptions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            if( e.Item.Checked ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vConsoleOptions.Font;
            }
        }


        void vLogFileOptions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            if( e.Item.Checked ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vLogFileOptions.Font;
            }
        }


        void xLogLimit_CheckedChanged( object sender, EventArgs e ) {
            nLogLimit.Enabled = xLogLimit.Checked;
        }

        #endregion

        #region Saving & Backup

        void xSaveAtInterval_CheckedChanged( object sender, EventArgs e ) {
            nSaveInterval.Enabled = xSaveInterval.Checked;
        }


        void xBackupAtInterval_CheckedChanged( object sender, EventArgs e ) {
            nBackupInterval.Enabled = xBackupInterval.Checked;
        }


        void xMaxBackups_CheckedChanged( object sender, EventArgs e ) {
            nMaxBackups.Enabled = xMaxBackups.Checked;
        }


        void xMaxBackupSize_CheckedChanged( object sender, EventArgs e ) {
            nMaxBackupSize.Enabled = xMaxBackupSize.Checked;
        }

        #endregion

        #region IRC

        void xIRC_CheckedChanged( object sender, EventArgs e ) {
            gIRCNetwork.Enabled = xIRCBotEnabled.Checked;
            gIRCFormatting.Enabled = xIRCBotEnabled.Checked;
            gIRCOptions.Enabled = xIRCBotEnabled.Checked;
            lIRCList.Enabled = xIRCBotEnabled.Checked;
            cIRCList.Enabled = xIRCBotEnabled.Checked;
            xIRCListShowNonEnglish.Enabled = xIRCBotEnabled.Checked;
        }


        struct IRCNetwork {
            const int DefaultIRCPort = 6667;

            public readonly string Name, Host;
            public readonly int Port;
            public readonly bool IsNonEnglish;


            public IRCNetwork( string name, string host )
                : this( name, host, DefaultIRCPort, false ) {}


            public IRCNetwork( string name, string host, int port, bool isNonEnglish ) {
                Name = name;
                Host = host;
                Port = port;
                IsNonEnglish = isNonEnglish;
            }
        }


        static readonly IRCNetwork[] IRCNetworks = new[] {
            new IRCNetwork( "FreeNode", "chat.freenode.net" ),
            new IRCNetwork( "QuakeNet", "irc.quakenet.org" ),
            new IRCNetwork( "IRCnet", "irc.belwue.de" ),
            new IRCNetwork( "Undernet", "irc.undernet.org" ),
            new IRCNetwork( "EFNet", "irc.servercentral.net" ),
            new IRCNetwork( "Ustream", "c.ustream.tv" ),
            new IRCNetwork( "WebChat", "irc.webchat.org" ),
            new IRCNetwork( "DALnet", "irc.dal.net" ),
            new IRCNetwork( "Rizon", "irc.rizon.net" ),
            new IRCNetwork( "IRC-Hispano [ES]", "irc.irc-hispano.org", 6667, true ),
            new IRCNetwork( "FCirc", "irc.friend.td.nu" ),
            new IRCNetwork( "GameSurge", "irc.gamesurge.net" ),
            new IRCNetwork( "LinkNet", "irc.link-net.org" ),
            new IRCNetwork( "OltreIrc [IT]", "irc.oltreirc.net", 6667, true ),
            new IRCNetwork( "AllNetwork", "irc.allnetwork.org" ),
            new IRCNetwork( "SwiftIRC", "irc.swiftirc.net" ),
            new IRCNetwork( "OpenJoke", "irc.openjoke.org" ),
            new IRCNetwork( "Abjects", "irc.abjects.net" ),
            new IRCNetwork( "OFTC", "irc.oftc.net" ),
            new IRCNetwork( "ChatZona [ES]", "irc.chatzona.org", 6667, true ),
            new IRCNetwork( "synIRC", "irc.synirc.net" ),
            new IRCNetwork( "OnlineGamesNet", "irc.OnlineGamesNet.net" ),
            new IRCNetwork( "DarkSin [IT]", "irc.darksin.it", 6667, true ),
            new IRCNetwork( "RusNet", "irc.run.net", 6667, true ),
            new IRCNetwork( "ExplosionIRC", "irc.explosionirc.net" ),
            new IRCNetwork( "IrCQ-Net", "irc.icq.com" ),
            new IRCNetwork( "IRCHighWay", "irc.irchighway.net" ),
            new IRCNetwork( "EsperNet", "irc.esper.net" ),
            new IRCNetwork( "euIRC", "irc.euirc.net" ),
            new IRCNetwork( "P2P-NET", "irc.p2p-irc.net" ),
            new IRCNetwork( "Mibbit", "irc.mibbit.com" ),
            new IRCNetwork( "kiss0fdeath", "irc.kiss0fdeath.net" ),
            new IRCNetwork( "P2P-NET.EU", "titan.ca.p2p-net.eu", 6667, true ),
            new IRCNetwork( "2ch [JP]", "irc.2ch.net", 6667, true ),
            new IRCNetwork( "SorceryNet", "irc.sorcery.net", 9000, false ),
            new IRCNetwork( "FurNet", "irc.furnet.org" ),
            new IRCNetwork( "GIMPnet", "irc.gimp.org" ),
            new IRCNetwork( "Coldfront", "irc.coldfront.net" ),
            new IRCNetwork( "MindForge", "irc.mindforge.org" ),
            new IRCNetwork( "Zurna.Net [TR]", "irc.zurna.net", 6667, true ),
            new IRCNetwork( "7-indonesia [ID]", "irc.7-indonesia.org", 6667, true ),
            new IRCNetwork( "EpiKnet", "irc.epiknet.org" ),
            new IRCNetwork( "EnterTheGame", "irc.enterthegame.com" ),
            new IRCNetwork( "DalNet(ru) [RU]", "irc.chatnet.ru", 6667, true ),
            new IRCNetwork( "GalaxyNet", "irc.galaxynet.org" ),
            new IRCNetwork( "Omerta", "irc.barafranca.com" ),
            new IRCNetwork( "SlashNET", "irc.slashnet.org" ),
            new IRCNetwork( "DarkMyst", "irc2.darkmyst.org" ),
            new IRCNetwork( "iZ-smart.net", "irc.iZ-smart.net" ),
            new IRCNetwork( "ItaLiaN-AmiCi [IT]", "irc.italian-amici.com", 6667, true ),
            new IRCNetwork( "Aitvaras [LT]", "irc.data.lt", 6667, true ),
            new IRCNetwork( "V-IRC [RU]", "irc.v-irc.ru", 6667, true ),
            new IRCNetwork( "ByroeNet [ID]", "irc.byroe.net", 6667, true ),
            new IRCNetwork( "Azzurra [IT]", "irc.azzurra.org", 6667, true ),
            new IRCNetwork( "Europa-IRC.DE [DE]", "irc.europa-irc.de", 6667, true ),
            new IRCNetwork( "ByNets [BY]", "irc.bynets.org", 6667, true ),
            new IRCNetwork( "GRNet [GR]", "global.irc.gr", 6667, true ),
            new IRCNetwork( "OceanIRC", "irc.oceanirc.net" ),
            new IRCNetwork( "UniBG [BG]", "irc.ITDNet.net", 6667, true ),
            new IRCNetwork( "KampungChat.Org [MY]", "irc.kampungchat.org", 6667, true ),
            new IRCNetwork( "WeNet [RU]", "ircworld.ru", 6667, true ),
            new IRCNetwork( "Stratics", "irc.stratics.com" ),
            new IRCNetwork( "Mozilla", "irc.mozilla.org" ),
            new IRCNetwork( "bondage.com", "irc.bondage.com" ),
            new IRCNetwork( "ShakeIT [BG]", "irc.index.bg", 6667, true ),
            new IRCNetwork( "NetGamers.Org", "firefly.no.eu.netgamers.org" ),
            new IRCNetwork( "FroZyn", "irc.Frozyn.us" ),
            new IRCNetwork( "PTnet", "irc.ptnet.org" ),
            new IRCNetwork( "Recycled-IRC", "yare.recycled-irc.net" ),
            new IRCNetwork( "Foonetic", "irc.foonetic.net" ),
            new IRCNetwork( "AlphaIRC", "irc.alphairc.com" ),
            new IRCNetwork( "KreyNet", "chat.be.krey.net" ),
            new IRCNetwork( "GeekShed", "irc.geekshed.net" ),
            new IRCNetwork( "VirtuaLife.com.br [BR]", "irc.virtualife.com.br", 6667, true ),
            new IRCNetwork( "IRCGate.it [IT]", "marte.ircgate.it", 6667, true ),
            new IRCNetwork( "Worldnet", "irc.worldnet.net" ),
            new IRCNetwork( "PIK [BA]", "irc.krstarica.com", 6667, true ),
            new IRCNetwork( "Friend4ever [IT]", "irc.friend4ever.it", 6667, true ),
            new IRCNetwork( "AustNet", "irc.austnet.org" ),
            new IRCNetwork( "GamesNET", "irc.GamesNET.net" )
        }.OrderBy( network => network.Name ).ToArray();


        void cIRCList_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cIRCList.SelectedIndex < 0 ) return;
            string selectedNetwork = (string)cIRCList.Items[cIRCList.SelectedIndex];
            IRCNetwork network = IRCNetworks.First( n => (n.Name == selectedNetwork) );
            tIRCBotNetwork.Text = network.Host;
            nIRCBotPort.Value = network.Port;
        }


        void xIRCListShowNonEnglish_CheckedChanged( object sender, EventArgs e ) {
            FillIRCNetworkList( xIRCListShowNonEnglish.Checked );
        }


        void FillIRCNetworkList( bool showNonEnglishNetworks ) {
            cIRCList.Items.Clear();
            foreach( IRCNetwork network in IRCNetworks ) {
                if( showNonEnglishNetworks || !network.IsNonEnglish ) {
                    cIRCList.Items.Add( network.Name );
                }
            }
        }


        void xIRCRegisteredNick_CheckedChanged( object sender, EventArgs e ) {
            tIRCNickServ.Enabled = xIRCRegisteredNick.Checked;
            tIRCNickServMessage.Enabled = xIRCRegisteredNick.Checked;
            lIRCNickServ.Enabled = xIRCRegisteredNick.Checked;
            lIRCNickServMessage.Enabled = xIRCRegisteredNick.Checked;
        }

        #endregion

        #region Advanced

        void nMaxUndo_ValueChanged( object sender, EventArgs e ) {
            if( xMaxUndo.Checked ) {
                decimal maxMemUsage = Math.Ceiling( nMaxUndoStates.Value*(nMaxUndo.Value*8)/(1024*1024) );
                lMaxUndoUnits.Text = String.Format( "blocks each (up to {0} MB of RAM per player)", maxMemUsage );
            } else {
                lMaxUndoUnits.Text = "blocks each";
            }
        }


        void xMaxUndo_CheckedChanged( object sender, EventArgs e ) {
            nMaxUndo.Enabled = xMaxUndo.Checked;
            lMaxUndoUnits.Enabled = xMaxUndo.Checked;
        }


        void xMapPath_CheckedChanged( object sender, EventArgs e ) {
            tMapPath.Enabled = xMapPath.Checked;
            bMapPath.Enabled = xMapPath.Checked;
        }

        #endregion

        void xAnnounceRankChanges_CheckedChanged( object sender, EventArgs e ) {
            xAnnounceRankChangeReasons.Enabled = xAnnounceRankChanges.Checked;
        }

        #endregion

        #region Ranks

        BindingList<string> rankNameList;


        void SelectRank( Rank rank ) {
            if( rank == null ) {
                if( selectedRank == null ) return;
                if( vRanks.SelectedIndex != -1 ) {
                    vRanks.ClearSelected();
                    return;
                }
                DisableRankOptions();
                return;
            }
            if( vRanks.SelectedIndex != rank.Index ) {
                vRanks.SelectedIndex = rank.Index;
                return;
            }
            selectedRank = rank;
            tRankName.Text = rank.Name;

            ApplyColor( bColorRank, ParseToIndex( rank.Color ) );

            tPrefix.Text = rank.Prefix;

            foreach( var box in permissionLimitBoxes.Values ) {
                box.SelectRank( rank );
            }

            xReserveSlot.Checked = rank.HasReservedSlot;
            xKickIdle.Checked = rank.IdleKickTimer > 0;
            nKickIdle.Value = rank.IdleKickTimer;
            nKickIdle.Enabled = xKickIdle.Checked;
            xAntiGrief.Checked = (rank.AntiGriefBlocks > 0 && rank.AntiGriefSeconds > 0);
            nAntiGriefBlocks.Value = rank.AntiGriefBlocks;
            nAntiGriefBlocks.Enabled = xAntiGrief.Checked;
            nAntiGriefSeconds.Value = rank.AntiGriefSeconds;
            nAntiGriefSeconds.Enabled = xAntiGrief.Checked;
            xDrawLimit.Checked = (rank.DrawLimit > 0);
            nDrawLimit.Value = rank.DrawLimit;
            nCopyPasteSlots.Value = rank.CopySlots;
            nFillLimit.Value = rank.FillLimit;
            xAllowSecurityCircumvention.Checked = rank.AllowSecurityCircumvention;

            foreach( ListViewItem item in vPermissions.Items ) {
                item.Checked = rank.Permissions[item.Index];
                if( item.Checked ) {
                    item.Font = bold;
                } else {
                    item.Font = vPermissions.Font;
                }
            }

            foreach( ListViewItem item in vPermissions.Items ) {
                CheckPermissionConsistency( (Permission)item.Tag, item.Checked );
            }

            xDrawLimit.Enabled = rank.Can( Permission.Draw ) || rank.Can( Permission.CopyAndPaste );
            nDrawLimit.Enabled = xDrawLimit.Checked;
            xAllowSecurityCircumvention.Enabled = rank.Can( Permission.ManageWorlds ) ||
                                                  rank.Can( Permission.ManageZones );

            gRankOptions.Enabled = true;
            lPermissions.Enabled = true;
            vPermissions.Enabled = true;

            bDeleteRank.Enabled = true;
            bRaiseRank.Enabled = (selectedRank != RankManager.HighestRank);
            bLowerRank.Enabled = (selectedRank != RankManager.LowestRank);
        }


        void RebuildRankList() {
            vRanks.Items.Clear();
            foreach( Rank rank in RankManager.Ranks ) {
                vRanks.Items.Add( ToComboBoxOption( rank ) );
            }

            FillRankList( cDefaultRank, "(lowest rank)" );
            cDefaultRank.SelectedIndex = RankManager.GetIndex( RankManager.DefaultRank );
            FillRankList( cDefaultBuildRank, "(default rank)" );
            cDefaultBuildRank.SelectedIndex = RankManager.GetIndex( RankManager.DefaultBuildRank );
            FillRankList( cPatrolledRank, "(default rank)" );
            cPatrolledRank.SelectedIndex = RankManager.GetIndex( RankManager.PatrolledRank );
            FillRankList( cBlockDBAutoEnableRank, "(default rank)" );
            cBlockDBAutoEnableRank.SelectedIndex = RankManager.GetIndex( RankManager.BlockDBAutoEnableRank );

            if( selectedRank != null ) {
                vRanks.SelectedIndex = selectedRank.Index;
            }
            SelectRank( selectedRank );

            foreach( var box in permissionLimitBoxes.Values ) {
                box.RebuildList();
                box.SelectRank( selectedRank );
            }
        }


        void DisableRankOptions() {
            selectedRank = null;
            bDeleteRank.Enabled = false;
            bRaiseRank.Enabled = false;
            bLowerRank.Enabled = false;
            tRankName.Text = "";
            bColorRank.Text = "";
            tPrefix.Text = "";

            foreach( var box in permissionLimitBoxes.Values ) {
                box.SelectRank( null );
            }

            xReserveSlot.Checked = false;
            xKickIdle.Checked = false;
            nKickIdle.Value = 0;
            xAntiGrief.Checked = false;
            nAntiGriefBlocks.Value = 0;
            xDrawLimit.Checked = false;
            nDrawLimit.Value = 0;
            xAllowSecurityCircumvention.Checked = false;
            nCopyPasteSlots.Value = 0;
            nFillLimit.Value = 32;
            foreach( ListViewItem item in vPermissions.Items ) {
                item.Checked = false;
                item.Font = vPermissions.Font;
            }
            gRankOptions.Enabled = false;
            lPermissions.Enabled = false;
            vPermissions.Enabled = false;
        }


        static void FillRankList( [NotNull] ComboBox box, string firstItem ) {
            if( box == null ) throw new ArgumentNullException( "box" );
            box.Items.Clear();
            box.Items.Add( firstItem );
            foreach( Rank rank in RankManager.Ranks ) {
                box.Items.Add( ToComboBoxOption( rank ) );
            }
        }

        #region Ranks Input Handlers

        void bAddRank_Click( object sender, EventArgs e ) {
            int number = 1;
            while( RankManager.RanksByName.ContainsKey( "rank" + number ) ) {
                number++;
            }

            Rank rank = new Rank( "rank" + number, RankManager.GenerateID() );

            RankManager.AddRank( rank );
            selectedRank = null;

            RebuildRankList();
            SelectRank( rank );

            rankNameList.Insert( rank.Index + 1, ToComboBoxOption( rank ) );
        }


        void bDeleteRank_Click( object sender, EventArgs e ) {
            if( vRanks.SelectedItem != null ) {
                selectedRank = null;
                int index = vRanks.SelectedIndex;
                Rank deletedRank = RankManager.FindRank( index );
                if( deletedRank == null ) return;

                string messages = "";

                // Ask for substitute rank
                DeleteRankPopup popup = new DeleteRankPopup( deletedRank );
                if( popup.ShowDialog() != DialogResult.OK ) return;

                Rank replacementRank = popup.SubstituteRank;

                // Update default rank
                if( RankManager.DefaultRank == deletedRank ) {
                    RankManager.DefaultRank = replacementRank;
                    messages += "DefaultRank has been changed to \"" + replacementRank.Name + "\"" + Environment.NewLine;
                }

                // Update defaultbuild rank
                if( RankManager.DefaultBuildRank == deletedRank ) {
                    RankManager.DefaultBuildRank = replacementRank;
                    messages += "DefaultBuildRank has been changed to \"" + replacementRank.Name + "\"" +
                                Environment.NewLine;
                }

                // Update patrolled rank
                if( RankManager.PatrolledRank == deletedRank ) {
                    RankManager.PatrolledRank = replacementRank;
                    messages += "PatrolledRank has been changed to \"" + replacementRank.Name + "\"" +
                                Environment.NewLine;
                }

                // Update patrolled rank
                if( RankManager.BlockDBAutoEnableRank == deletedRank ) {
                    RankManager.BlockDBAutoEnableRank = replacementRank;
                    messages += "BlockDBAutoEnableRank has been changed to \"" + replacementRank.Name + "\"" +
                                Environment.NewLine;
                }

                // Delete rank
                if( RankManager.DeleteRank( deletedRank, replacementRank ) ) {
                    messages += "Some of the rank limits for kick, ban, promote, and/or demote have been reset." +
                                Environment.NewLine;
                }
                vRanks.Items.RemoveAt( index );

                // Update world permissions
                string worldUpdates = "";
                foreach( WorldListEntry world in Worlds ) {
                    if( world.AccessPermission == ToComboBoxOption( deletedRank ) ) {
                        world.AccessPermission = ToComboBoxOption( replacementRank );
                        worldUpdates += " - " + world.Name + ": access permission changed to " + replacementRank.Name +
                                        Environment.NewLine;
                    }
                    if( world.BuildPermission == ToComboBoxOption( deletedRank ) ) {
                        world.BuildPermission = ToComboBoxOption( replacementRank );
                        worldUpdates += " - " + world.Name + ": build permission changed to " + replacementRank.Name +
                                        Environment.NewLine;
                    }
                }

                rankNameList.RemoveAt( index + 1 );

                if( worldUpdates.Length > 0 ) {
                    messages += "The following worlds were affected:" + Environment.NewLine + worldUpdates;
                }

                if( messages.Length > 0 ) {
                    MessageBox.Show( messages, "Warning" );
                }

                RebuildRankList();

                if( index < vRanks.Items.Count ) {
                    vRanks.SelectedIndex = index;
                }
            }
        }


        void tPrefix_Validating( object sender, CancelEventArgs e ) {
            if( selectedRank == null ) return;
            tPrefix.Text = tPrefix.Text.Trim();
            if( tPrefix.Text.Length > 0 && !Rank.IsValidPrefix( tPrefix.Text ) ) {
                MessageBox.Show( "Invalid prefix character!\n" +
                                 "Prefixes may only contain characters that are allowed in chat (except space).",
                                 "Warning" );
                tPrefix.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            } else {
                tPrefix.ForeColor = SystemColors.ControlText;
            }
            if( selectedRank.Prefix == tPrefix.Text ) return;

            string oldName = ToComboBoxOption( selectedRank );

            // To avoid DataErrors in World tab's DataGridView while renaming a rank,
            // the new name is first added to the list of options (without removing the old name)
            rankNameList.Insert( selectedRank.Index + 1, String.Format( "{0,1}{1}", tPrefix.Text, selectedRank.Name ) );

            selectedRank.Prefix = tPrefix.Text;

            // Remove the old name from the list of options
            rankNameList.Remove( oldName );

            Worlds.ResetBindings();
            RebuildRankList();
        }


        void xReserveSlot_CheckedChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            selectedRank.HasReservedSlot = xReserveSlot.Checked;
        }


        void nKickIdle_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xKickIdle.Checked ) return;
            selectedRank.IdleKickTimer = Convert.ToInt32( nKickIdle.Value );
        }


        void nAntiGriefBlocks_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xAntiGrief.Checked ) return;
            selectedRank.AntiGriefBlocks = Convert.ToInt32( nAntiGriefBlocks.Value );
        }


        void nAntiGriefSeconds_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xAntiGrief.Checked ) return;
            selectedRank.AntiGriefSeconds = Convert.ToInt32( nAntiGriefSeconds.Value );
        }


        void nDrawLimit_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xDrawLimit.Checked ) return;
            selectedRank.DrawLimit = Convert.ToInt32( nDrawLimit.Value );
            double cubed = Math.Pow( Convert.ToDouble( nDrawLimit.Value ), 1/3d );
            lDrawLimitUnits.Text = String.Format( "blocks ({0:0}\u00B3)", cubed );
        }


        void nCopyPasteSlots_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            selectedRank.CopySlots = Convert.ToInt32( nCopyPasteSlots.Value );
        }


        void xAllowSecurityCircumvention_CheckedChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            selectedRank.AllowSecurityCircumvention = xAllowSecurityCircumvention.Checked;
        }


        void xSpamChatKick_CheckedChanged( object sender, EventArgs e ) {
            nAntispamMaxWarnings.Enabled = xAntispamKicks.Checked;
            lAntispamMaxWarnings.Enabled = xAntispamKicks.Checked;
        }


        void vRanks_SelectedIndexChanged( object sender, EventArgs e ) {
            if( vRanks.SelectedIndex != -1 ) {
                SelectRank( RankManager.FindRank( vRanks.SelectedIndex ) );
            } else {
                DisableRankOptions();
            }
        }


        void xKickIdle_CheckedChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            if( xKickIdle.Checked ) {
                nKickIdle.Value = selectedRank.IdleKickTimer;
            } else {
                nKickIdle.Value = 0;
                selectedRank.IdleKickTimer = 0;
            }
            nKickIdle.Enabled = xKickIdle.Checked;
        }


        void xAntiGrief_CheckedChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            if( xAntiGrief.Checked ) {
                nAntiGriefBlocks.Value = selectedRank.AntiGriefBlocks;
                nAntiGriefSeconds.Value = selectedRank.AntiGriefSeconds;
            } else {
                nAntiGriefBlocks.Value = 0;
                selectedRank.AntiGriefBlocks = 0;
                nAntiGriefSeconds.Value = 0;
                selectedRank.AntiGriefSeconds = 0;
            }
            nAntiGriefBlocks.Enabled = xAntiGrief.Checked;
            nAntiGriefSeconds.Enabled = xAntiGrief.Checked;
        }


        void xDrawLimit_CheckedChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            if( xDrawLimit.Checked ) {
                nDrawLimit.Value = selectedRank.DrawLimit;
                double cubed = Math.Pow( Convert.ToDouble( nDrawLimit.Value ), 1/3d );
                lDrawLimitUnits.Text = String.Format( "blocks ({0:0}\u00B3)", cubed );
            } else {
                nDrawLimit.Value = 0;
                selectedRank.DrawLimit = 0;
                lDrawLimitUnits.Text = "blocks";
            }
            nDrawLimit.Enabled = xDrawLimit.Checked;
        }


        void vPermissions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            bool check = e.Item.Checked;
            if( check ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vPermissions.Font;
            }
            if( selectedRank == null ) return;

            Permission permission = (Permission)e.Item.Tag;
            CheckPermissionConsistency( permission, check );

            selectedRank.Permissions[(int)e.Item.Tag] = e.Item.Checked;
        }


        void CheckPermissionConsistency( Permission permission, bool check ) {
            switch( permission ) {
                case Permission.Chat:
                    if( !check ) {
                        vPermissions.Items[(int)Permission.Say].Checked = false;
                        vPermissions.Items[(int)Permission.Say].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.UseColorCodes].Checked = false;
                        vPermissions.Items[(int)Permission.UseColorCodes].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.UseEmotes].Checked = false;
                        vPermissions.Items[(int)Permission.UseEmotes].ForeColor = SystemColors.GrayText;
                    } else {
                        vPermissions.Items[(int)Permission.Say].ForeColor = SystemColors.ControlText;
                        vPermissions.Items[(int)Permission.UseColorCodes].ForeColor = SystemColors.ControlText;
                        vPermissions.Items[(int)Permission.UseEmotes].ForeColor = SystemColors.ControlText;
                    }
                    break;

                case Permission.Say:
                case Permission.UseEmotes:
                case Permission.UseColorCodes:
                    if( check ) vPermissions.Items[(int)Permission.Chat].Checked = true;
                    break;

                case Permission.Ban:
                    if( !check ) {
                        vPermissions.Items[(int)Permission.BanIP].Checked = false;
                        vPermissions.Items[(int)Permission.BanIP].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.BanAll].Checked = false;
                        vPermissions.Items[(int)Permission.BanAll].ForeColor = SystemColors.GrayText;
                    } else {
                        vPermissions.Items[(int)Permission.BanIP].ForeColor = SystemColors.ControlText;
                        vPermissions.Items[(int)Permission.BanAll].ForeColor = SystemColors.ControlText;
                    }
                    break;

                case Permission.BanIP:
                    if( check ) {
                        vPermissions.Items[(int)Permission.Ban].Checked = true;
                        vPermissions.Items[(int)Permission.BanAll].ForeColor = SystemColors.ControlText;
                    } else {
                        vPermissions.Items[(int)Permission.BanAll].Checked = false;
                        vPermissions.Items[(int)Permission.BanAll].ForeColor = SystemColors.GrayText;
                    }
                    break;

                case Permission.BanAll:
                    if( check ) {
                        vPermissions.Items[(int)Permission.Ban].Checked = true;
                        vPermissions.Items[(int)Permission.BanIP].Checked = true;
                    }
                    break;

                case Permission.Draw:
                    xDrawLimit.Enabled = vPermissions.Items[(int)Permission.Draw].Checked ||
                                         vPermissions.Items[(int)Permission.CopyAndPaste].Checked;
                    if( check ) {
                        vPermissions.Items[(int)Permission.DrawAdvanced].ForeColor = SystemColors.ControlText;
                        vPermissions.Items[(int)Permission.CopyAndPaste].ForeColor = SystemColors.ControlText;
                    } else {
                        vPermissions.Items[(int)Permission.DrawAdvanced].Checked = false;
                        vPermissions.Items[(int)Permission.DrawAdvanced].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.CopyAndPaste].Checked = false;
                        vPermissions.Items[(int)Permission.CopyAndPaste].ForeColor = SystemColors.GrayText;
                    }
                    break;

                case Permission.DrawAdvanced:
                    if( check ) vPermissions.Items[(int)Permission.Draw].Checked = true;
                    lFillLimit.Enabled = check;
                    lFillLimitUnits.Enabled = check;
                    nFillLimit.Enabled = check;
                    break;

                case Permission.UndoAll:
                    if( check ) vPermissions.Items[(int)Permission.UndoOthersActions].Checked = true;
                    break;

                case Permission.UndoOthersActions:
                    if( check ) {
                        vPermissions.Items[(int)Permission.UndoAll].ForeColor = SystemColors.ControlText;
                    } else {
                        vPermissions.Items[(int)Permission.UndoAll].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.UndoAll].Checked = false;
                    }
                    break;

                case Permission.CopyAndPaste:
                    xDrawLimit.Enabled = vPermissions.Items[(int)Permission.Draw].Checked ||
                                         vPermissions.Items[(int)Permission.CopyAndPaste].Checked;
                    lCopyPasteSlots.Enabled = check;
                    nCopyPasteSlots.Enabled = check;
                    break;

                case Permission.ManageWorlds:
                case Permission.ManageZones:
                    xAllowSecurityCircumvention.Enabled = vPermissions.Items[(int)Permission.ManageWorlds].Checked ||
                                                          vPermissions.Items[(int)Permission.ManageZones].Checked;
                    break;

                case Permission.Teleport:
                    if( !check ) {
                        vPermissions.Items[(int)Permission.Patrol].Checked = false;
                        vPermissions.Items[(int)Permission.Patrol].ForeColor = SystemColors.GrayText;
                    } else {
                        vPermissions.Items[(int)Permission.Patrol].ForeColor = SystemColors.ControlText;
                    }
                    break;

                case Permission.Patrol:
                    if( check ) vPermissions.Items[(int)Permission.Teleport].Checked = true;
                    break;

                case Permission.Delete:
                    if( !check ) {
                        vPermissions.Items[(int)Permission.DeleteAdmincrete].Checked = false;
                        vPermissions.Items[(int)Permission.DeleteAdmincrete].ForeColor = SystemColors.GrayText;
                    } else {
                        vPermissions.Items[(int)Permission.DeleteAdmincrete].ForeColor = SystemColors.ControlText;
                    }
                    break;

                case Permission.DeleteAdmincrete:
                    if( check ) vPermissions.Items[(int)Permission.Delete].Checked = true;
                    break;

                case Permission.Build:
                    if( !check ) {
                        vPermissions.Items[(int)Permission.PlaceAdmincrete].Checked = false;
                        vPermissions.Items[(int)Permission.PlaceAdmincrete].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.PlaceGrass].Checked = false;
                        vPermissions.Items[(int)Permission.PlaceGrass].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.PlaceLava].Checked = false;
                        vPermissions.Items[(int)Permission.PlaceLava].ForeColor = SystemColors.GrayText;
                        vPermissions.Items[(int)Permission.PlaceWater].Checked = false;
                        vPermissions.Items[(int)Permission.PlaceWater].ForeColor = SystemColors.GrayText;
                    } else {
                        vPermissions.Items[(int)Permission.PlaceAdmincrete].ForeColor = SystemColors.ControlText;
                        vPermissions.Items[(int)Permission.PlaceGrass].ForeColor = SystemColors.ControlText;
                        vPermissions.Items[(int)Permission.PlaceLava].ForeColor = SystemColors.ControlText;
                        vPermissions.Items[(int)Permission.PlaceWater].ForeColor = SystemColors.ControlText;
                    }
                    break;

                case Permission.PlaceAdmincrete:
                case Permission.PlaceGrass:
                case Permission.PlaceLava:
                case Permission.PlaceWater:
                    if( check ) vPermissions.Items[(int)Permission.Build].Checked = true;
                    break;

                case Permission.Bring:
                    if( !check ) {
                        vPermissions.Items[(int)Permission.BringAll].Checked = false;
                        vPermissions.Items[(int)Permission.BringAll].ForeColor = SystemColors.GrayText;
                    } else {
                        vPermissions.Items[(int)Permission.BringAll].ForeColor = SystemColors.ControlText;
                    }
                    break;

                case Permission.BringAll:
                    if( check ) vPermissions.Items[(int)Permission.Bring].Checked = true;
                    break;
            }

            if( permissionLimitBoxes.ContainsKey( permission ) ) {
                permissionLimitBoxes[permission].PermissionToggled( check );
            }
        }


        void tRankName_Validating( object sender, CancelEventArgs e ) {
            tRankName.ForeColor = SystemColors.ControlText;
            if( selectedRank == null ) return;

            string newName = tRankName.Text.Trim();

            if( newName == selectedRank.Name ) {
                return;
            }
            if( newName.Length == 0 ) {
                MessageBox.Show( "Rank name cannot be blank." );
                tRankName.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            } else if( !Rank.IsValidRankName( newName ) ) {
                MessageBox.Show( "Rank name can only contain letters, digits, and underscores." );
                tRankName.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            } else if( !RankManager.CanRenameRank( selectedRank, newName ) ) {
                MessageBox.Show( "There is already another rank named \"" + newName + "\".\n" +
                                 "Duplicate rank names are not allowed." );
                tRankName.ForeColor = System.Drawing.Color.Red;
                e.Cancel = true;
            } else {
                string oldName = ToComboBoxOption( selectedRank );

                // To avoid DataErrors in World tab's DataGridView while renaming a rank,
                // the new name is first added to the list of options (without removing the old name)
                rankNameList.Insert( selectedRank.Index + 1, String.Format( "{0,1}{1}", selectedRank.Prefix, newName ) );

                RankManager.RenameRank( selectedRank, newName );

                // Remove the old name from the list of options
                rankNameList.Remove( oldName );

                Worlds.ResetBindings();
                RebuildRankList();
            }
        }


        void bRaiseRank_Click( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            if( RankManager.RaiseRank( selectedRank ) ) {
                RebuildRankList();
                rankNameList.Insert( selectedRank.Index + 1, ToComboBoxOption( selectedRank ) );
                rankNameList.RemoveAt( selectedRank.Index + 3 );
            }
        }


        void bLowerRank_Click( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            if( RankManager.LowerRank( selectedRank ) ) {
                RebuildRankList();
                rankNameList.Insert( selectedRank.Index + 2, ToComboBoxOption( selectedRank ) );
                rankNameList.RemoveAt( selectedRank.Index );
            }
        }

        #endregion

        #endregion

        #region Apply / Save / Cancel Buttons

        void bApply_Click( object sender, EventArgs e ) {
            SaveEverything();
        }


        void bSave_Click( object sender, EventArgs e ) {
            SaveEverything();
            Application.Exit();
        }


        void SaveEverything() {
            using( LogRecorder applyLogger = new LogRecorder() ) {
                SaveConfig();
                if( applyLogger.HasMessages ) {
                    MessageBox.Show( applyLogger.MessageString,
                                     "Some problems were encountered with the selected values." );
                    return;
                }
            }
            using( LogRecorder saveLogger = new LogRecorder() ) {
                if( Config.Save() ) {
                    bApply.Enabled = false;
                }
                if( saveLogger.HasMessages ) {
                    MessageBox.Show( saveLogger.MessageString, "Some problems were encountered while saving." );
                }
            }
        }


        void bCancel_Click( object sender, EventArgs e ) {
            Application.Exit();
        }

        #endregion

        #region Reset

        void bResetAll_Click( object sender, EventArgs e ) {
            if( MessageBox.Show( "Are you sure you want to reset everything to defaults?",
                                 "Warning",
                                 MessageBoxButtons.OKCancel ) != DialogResult.OK ) return;
            Config.LoadDefaults();
            Config.ResetRanks();
            Config.ResetLogOptions();

            ApplyTabGeneral();
            ApplyTabChat();
            ApplyTabWorlds(); // also reloads world list
            ApplyTabRanks();
            ApplyTabSecurity();
            ApplyTabSavingAndBackup();
            ApplyTabLogging();
            ApplyTabIRC();
            ApplyTabAdvanced();
        }


        void bResetTab_Click( object sender, EventArgs e ) {
            if( MessageBox.Show( "Are you sure you want to reset this tab to defaults?",
                                 "Warning",
                                 MessageBoxButtons.OKCancel ) != DialogResult.OK ) return;
            switch( tabs.SelectedIndex ) {
                case 0: // General
                    Config.LoadDefaults( ConfigSection.General );
                    ApplyTabGeneral();
                    break;
                case 1: // Chat
                    Config.LoadDefaults( ConfigSection.Chat );
                    ApplyTabChat();
                    break;
                case 2: // Worlds
                    Config.LoadDefaults( ConfigSection.Worlds );
                    ApplyTabWorlds(); // also reloads world list
                    break;
                case 3: // Ranks
                    Config.ResetRanks();
                    ApplyTabWorlds();
                    ApplyTabRanks();
                    RebuildRankList();
                    break;
                case 4: // Security
                    Config.LoadDefaults( ConfigSection.Security );
                    ApplyTabSecurity();
                    break;
                case 5: // Saving and Backup
                    Config.LoadDefaults( ConfigSection.SavingAndBackup );
                    ApplyTabSavingAndBackup();
                    break;
                case 6: // Logging
                    Config.LoadDefaults( ConfigSection.Logging );
                    Config.ResetLogOptions();
                    ApplyTabLogging();
                    break;
                case 7: // IRC
                    Config.LoadDefaults( ConfigSection.IRC );
                    ApplyTabIRC();
                    break;
                case 8: // Advanced
                    Config.LoadDefaults( ConfigSection.Logging );
                    ApplyTabAdvanced();
                    break;
            }
        }

        #endregion

        #region Utils

        #region Change Detection

        bool pauseTrackingSomethingChanged = false;

        void SomethingChanged( object sender, EventArgs args ) {
            if( pauseTrackingSomethingChanged ) return;
            bApply.Enabled = true;
        }


        void AddChangeHandler( Control c, EventHandler handler ) {
            if( c is CheckBox ) {
                ((CheckBox)c).CheckedChanged += handler;
            } else if( c is ComboBox ) {
                ((ComboBox)c).SelectedIndexChanged += handler;
            } else if( c is ListView ) {
                ((ListView)c).ItemChecked += (( o, e ) => handler( o, e ));
            } else if( c is NumericUpDown ) {
                ((NumericUpDown)c).ValueChanged += handler;
            } else if( c is ListBox ) {
                ((ListBox)c).SelectedIndexChanged += handler;
            } else if( c is TextBoxBase ) {
                c.TextChanged += handler;
            } else if( c is ButtonBase ) {
                if( c != bPortCheck && c != bMeasure ) {
                    c.Click += handler;
                }
            }
            foreach( Control child in c.Controls ) {
                AddChangeHandler( child, handler );
            }
        }

        #endregion

        #region Colors

        int colorSys, colorSay, colorHelp, colorAnnouncement, colorPM, colorIRC, colorMe, colorWarning;


        void ApplyColor( Button button, int color ) {
            button.Text = GetName( color );
            button.BackColor = ColorPicker.ColorPairs[color].Background;
            button.ForeColor = ColorPicker.ColorPairs[color].Foreground;
            bApply.Enabled = true;
        }


        void bColorSys_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "System message color", colorSys );
            picker.ShowDialog();
            colorSys = picker.ColorIndex;
            ApplyColor( bColorSys, colorSys );
            Color.Sys = Parse( colorSys );
        }


        void bColorHelp_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Help message color", colorHelp );
            picker.ShowDialog();
            colorHelp = picker.ColorIndex;
            ApplyColor( bColorHelp, colorHelp );
            Color.Help = Parse( colorHelp );
        }


        void bColorSay_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "/Say message color", colorSay );
            picker.ShowDialog();
            colorSay = picker.ColorIndex;
            ApplyColor( bColorSay, colorSay );
            Color.Say = Parse( colorSay );
        }


        void bColorAnnouncement_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Announcement color", colorAnnouncement );
            picker.ShowDialog();
            colorAnnouncement = picker.ColorIndex;
            ApplyColor( bColorAnnouncement, colorAnnouncement );
            Color.Announcement = Parse( colorAnnouncement );
        }


        void bColorPM_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Private / rank chat color", colorPM );
            picker.ShowDialog();
            colorPM = picker.ColorIndex;
            ApplyColor( bColorPM, colorPM );
            Color.PM = Parse( colorPM );
        }


        void bColorWarning_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Warning / Error message color", colorWarning );
            picker.ShowDialog();
            colorWarning = picker.ColorIndex;
            ApplyColor( bColorWarning, colorWarning );
            Color.Warning = Parse( colorWarning );
        }


        void bColorMe_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "/Me command color", colorMe );
            picker.ShowDialog();
            colorMe = picker.ColorIndex;
            ApplyColor( bColorMe, colorMe );
            Color.Me = Parse( colorMe );
        }


        void bColorIRC_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "IRC message color", colorIRC );
            picker.ShowDialog();
            colorIRC = picker.ColorIndex;
            ApplyColor( bColorIRC, colorIRC );
            Color.IRC = Parse( colorIRC );
        }


        void bColorRank_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Rank color for \"" + selectedRank.Name + "\"",
                                                  ParseToIndex( selectedRank.Color ) );
            picker.ShowDialog();
            ApplyColor( bColorRank, picker.ColorIndex );
            selectedRank.Color = Parse( picker.ColorIndex );
        }


        void HandleTabChatChange( object sender, EventArgs args ) {
            UpdateChatPreview();
        }


        void UpdateChatPreview() {
            List<string> lines = new List<string>();
            if( xShowConnectionMessages.Checked ) {
                lines.Add( String.Format( "&SPlayer {0}{1}Notch&S connected, joined {2}{3}main",
                                          xRankColorsInChat.Checked ? RankManager.HighestRank.Color : "",
                                          xRankPrefixesInChat.Checked ? RankManager.HighestRank.Prefix : "",
                                          xRankColorsInWorldNames.Checked ? RankManager.LowestRank.Color : "",
                                          xRankPrefixesInChat.Checked ? RankManager.LowestRank.Prefix : "" ) );
            }
            lines.Add( "&R<*- This is a random announcement -*>" );
            lines.Add( "&YSomeone wrote this message with /Say" );
            lines.Add( String.Format( "{0}{1}Notch&F: This is a normal chat message",
                                      xRankColorsInChat.Checked ? RankManager.HighestRank.Color : "",
                                      xRankPrefixesInChat.Checked ? RankManager.HighestRank.Prefix : "" ) );
            lines.Add( "&Pfrom Notch: This is a private message / whisper" );
            lines.Add( "&M*Notch is using /Me to write this" );
            if( xShowJoinedWorldMessages.Checked ) {
                Rank midRank = RankManager.LowestRank;
                if( RankManager.LowestRank.NextRankUp != null ) {
                    midRank = RankManager.LowestRank.NextRankUp;
                }

                lines.Add( String.Format( "&SPlayer {0}{1}Notch&S joined {2}{3}SomeOtherMap",
                                          xRankColorsInChat.Checked ? RankManager.HighestRank.Color : "",
                                          xRankPrefixesInChat.Checked ? RankManager.HighestRank.Prefix : "",
                                          xRankColorsInWorldNames.Checked ? midRank.Color : "",
                                          xRankPrefixesInChat.Checked ? midRank.Prefix : "" ) );
            }
            lines.Add( "&SUnknown command \"kikc\", see &H/Commands" );
            if( xAnnounceKickAndBanReasons.Checked ) {
                lines.Add( String.Format( "&W{0}{1}Notch&W was kicked by {0}{1}gamer1&W: Reason goes here",
                                          xRankColorsInChat.Checked ? RankManager.HighestRank.Color : "",
                                          xRankPrefixesInChat.Checked ? RankManager.HighestRank.Prefix : "" ) );
            } else {
                lines.Add( String.Format( "&W{0}{1}Notch&W was kicked by {0}{1}gamer1",
                                          xRankColorsInChat.Checked ? RankManager.HighestRank.Color : "",
                                          xRankPrefixesInChat.Checked ? RankManager.HighestRank.Prefix : "" ) );
            }

            if( xShowConnectionMessages.Checked ) {
                lines.Add( String.Format( "&S{0}{1}Notch&S left the server.",
                                          xRankColorsInChat.Checked ? RankManager.HighestRank.Color : "",
                                          xRankPrefixesInChat.Checked ? RankManager.HighestRank.Prefix : "" ) );
            }

            chatPreview.SetText( lines.ToArray() );
        }

        #endregion

        void bRules_Click( object sender, EventArgs e ) {
            TextEditorPopup popup = new TextEditorPopup( Paths.RulesFileName, "Use common sense!" );
            popup.ShowDialog();
        }


        internal static bool IsWorldNameTaken( string name ) {
            return Worlds.Any( world => world.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        void CheckMaxPlayersPerWorldValue() {
            if( nMaxPlayersPerWorld.Value > nMaxPlayers.Value ) {
                nMaxPlayersPerWorld.Value = nMaxPlayers.Value;
            }
            nMaxPlayersPerWorld.Maximum = Math.Min( 128, nMaxPlayers.Value );
        }


        internal static void HandleWorldRename( string from, string to ) {
            if( instance.cMainWorld.Items.Count == 0 ) return;
            if( instance.cMainWorld.SelectedItem == null ) {
                instance.cMainWorld.SelectedIndex = 0;
            } else {
                string mainWorldName = instance.cMainWorld.SelectedItem.ToString();
                instance.FillWorldList();
                if( mainWorldName == @from ) {
                    instance.cMainWorld.SelectedItem = to;
                } else {
                    instance.cMainWorld.SelectedItem = mainWorldName;
                }
            }
        }

        #endregion

        void ConfigUI_FormClosing( object sender, FormClosingEventArgs e ) {
            if( !bApply.Enabled ) return;
            DialogResult dr = MessageBox.Show( "Would you like to save the changes before exiting?",
                                               "Warning",
                                               MessageBoxButtons.YesNoCancel );
            switch( dr ) {
                case DialogResult.Yes:
                    SaveEverything();
                    return;

                case DialogResult.Cancel:
                    e.Cancel = true;
                    return;
            }
        }


        readonly Dictionary<Permission, PermissionLimitBox> permissionLimitBoxes =
            new Dictionary<Permission, PermissionLimitBox>();

        const string DefaultPermissionLimitString = "(own rank)";


        void FillPermissionLimitBoxes() {
            permissionLimitBoxes[Permission.Kick] = new PermissionLimitBox( "Kick limit",
                                                                            Permission.Kick,
                                                                            DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Ban] = new PermissionLimitBox( "Ban limit",
                                                                           Permission.Ban,
                                                                           DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Promote] = new PermissionLimitBox( "Promote limit",
                                                                               Permission.Promote,
                                                                               DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Demote] = new PermissionLimitBox( "Demote limit",
                                                                              Permission.Demote,
                                                                              DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Hide] = new PermissionLimitBox( "Can hide from",
                                                                            Permission.Hide,
                                                                            DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Freeze] = new PermissionLimitBox( "Freeze limit",
                                                                              Permission.Freeze,
                                                                              DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Mute] = new PermissionLimitBox( "Mute limit",
                                                                            Permission.Mute,
                                                                            DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Bring] = new PermissionLimitBox( "Bring limit",
                                                                             Permission.Bring,
                                                                             DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.Spectate] = new PermissionLimitBox( "Spectate limit",
                                                                                Permission.Spectate,
                                                                                DefaultPermissionLimitString );
            permissionLimitBoxes[Permission.UndoOthersActions] = new PermissionLimitBox( "Undo limit",
                                                                                         Permission.UndoOthersActions,
                                                                                         DefaultPermissionLimitString );

            foreach( var box in permissionLimitBoxes.Values ) {
                permissionLimitBoxContainer.Controls.Add( box );
            }
        }


        void cDefaultRank_SelectedIndexChanged( object sender, EventArgs e ) {
            RankManager.DefaultRank = RankManager.FindRank( cDefaultRank.SelectedIndex - 1 );
        }


        void cDefaultBuildRank_SelectedIndexChanged( object sender, EventArgs e ) {
            RankManager.DefaultBuildRank = RankManager.FindRank( cDefaultBuildRank.SelectedIndex - 1 );
        }


        void cPatrolledRank_SelectedIndexChanged( object sender, EventArgs e ) {
            RankManager.PatrolledRank = RankManager.FindRank( cPatrolledRank.SelectedIndex - 1 );
        }


        void cBlockDBAutoEnableRank_SelectedIndexChanged( object sender, EventArgs e ) {
            RankManager.BlockDBAutoEnableRank = RankManager.FindRank( cBlockDBAutoEnableRank.SelectedIndex - 1 );
        }


        void xBlockDBEnabled_CheckedChanged( object sender, EventArgs e ) {
            xBlockDBAutoEnable.Enabled = xBlockDBEnabled.Checked;
            cBlockDBAutoEnableRank.Enabled = xBlockDBEnabled.Checked && xBlockDBAutoEnable.Checked;
        }


        void xBlockDBAutoEnable_CheckedChanged( object sender, EventArgs e ) {
            cBlockDBAutoEnableRank.Enabled = xBlockDBEnabled.Checked && xBlockDBAutoEnable.Checked;
        }


        void nFillLimit_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            selectedRank.FillLimit = Convert.ToInt32( nFillLimit.Value );
        }


        const string ReadmeFileName = "README.txt";


        void bReadme_Click( object sender, EventArgs e ) {
            try {
                if( File.Exists( ReadmeFileName ) ) {
                    Process.Start( ReadmeFileName );
                }
            } catch( Exception ) {}
        }


        const string ChangelogFileName = "CHANGELOG.txt";


        void bChangelog_Click( object sender, EventArgs e ) {
            try {
                if( File.Exists( ChangelogFileName ) ) {
                    Process.Start( ChangelogFileName );
                }
            } catch( Exception ) {}
        }


        static bool usePrefixes;


        public static string ToComboBoxOption( Rank rank ) {
            if( usePrefixes ) {
                return String.Format( "{0,1}{1}", rank.Prefix, rank.Name );
            } else {
                return rank.Name;
            }
        }


        void xRankPrefixesInChat_CheckedChanged( object sender, EventArgs e ) {
            usePrefixes = xRankPrefixesInChat.Checked;
            tPrefix.Enabled = usePrefixes;
            lPrefix.Enabled = usePrefixes;
            if( ranksLoaded ) RebuildRankList();
        }


        void xAntispamMessageCount_CheckedChanged( object sender, EventArgs e ) {
            bool enabled = xAntispamMessageCount.Checked;
            nAntispamMessageCount.Enabled = enabled;
            lAntispamMessageCount.Enabled = enabled;
            nAntispamInterval.Enabled = enabled;
            lAntispamIntervalUnits.Enabled = enabled;
            xAntispamMuteDuration.Enabled = enabled;
            xAntispamKicks.Enabled = enabled;
        }


        void xAntispamMuteDuration_CheckedChanged( object sender, EventArgs e ) {
            nAntispamMuteDuration.Enabled = xAntispamMuteDuration.Checked;
            lAntispamMuteDurationUnits.Enabled = xAntispamMuteDuration.Checked;
        }


        [Pure]
        public static int ParseToIndex( [NotNull] string color ) {
            if( color == null ) throw new ArgumentNullException( "color" );
            color = color.ToLower();
            if( color.Length == 2 && color[0] == '&' ) {
                if( Color.ColorNames.ContainsKey( color[1] ) ) {
                    return Color.ColorNames.IndexOfKey( color[1] );
                } else {
                    switch( color ) {
                        case "&s":
                            return Color.ColorNames.IndexOfKey( Color.Sys[1] );
                        case "&y":
                            return Color.ColorNames.IndexOfKey( Color.Say[1] );
                        case "&p":
                            return Color.ColorNames.IndexOfKey( Color.PM[1] );
                        case "&r":
                            return Color.ColorNames.IndexOfKey( Color.Announcement[1] );
                        case "&h":
                            return Color.ColorNames.IndexOfKey( Color.Help[1] );
                        case "&w":
                            return Color.ColorNames.IndexOfKey( Color.Warning[1] );
                        case "&m":
                            return Color.ColorNames.IndexOfKey( Color.Me[1] );
                        case "&i":
                            return Color.ColorNames.IndexOfKey( Color.IRC[1] );
                        default:
                            return 15;
                    }
                }
            } else if( Color.ColorNames.ContainsValue( color ) ) {
                return Color.ColorNames.IndexOfValue( color );
            } else {
                return 15; // white
            }
        }


        [CanBeNull]
        [Pure]
        public static string Parse( int index ) {
            if( index >= 0 && index <= 15 ) {
                return "&" + Color.ColorNames.Keys[index];
            } else {
                return null;
            }
        }


        // Looks up color name for the given numeric color code.
        [CanBeNull]
        [Pure]
        public static string GetName( int index ) {
            if( index >= 0 && index <= 15 ) {
                return Color.ColorNames.Values[index];
            } else {
                return null;
            }
        }


        void xAllowFreePlayers_CheckedChanged( object sender, EventArgs e ) {
            xAllowEmailAccounts.Enabled = xAllowFreePlayers.Checked;
        }
    }
}
