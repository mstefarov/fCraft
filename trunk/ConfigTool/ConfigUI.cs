// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using fCraft;
using Color = System.Drawing.Color;


namespace ConfigTool {
    public sealed partial class ConfigUI : Form {
        static ConfigUI instance;
        Font bold;
        Rank selectedRank, defaultRank, patrolledRank;
        internal static SortableBindingList<WorldListEntry> worlds = new SortableBindingList<WorldListEntry>();

        #region Initialization

        public ConfigUI() {
            instance = this;
            InitializeComponent();

            foreach( ListViewItem item in vConsoleOptions.Items ) {
                vLogFileOptions.Items.Add( (ListViewItem)item.Clone() );
            }

            bold = new Font( Font, FontStyle.Bold );

            FillOptionList();
            FillToolTipsGeneral();
            FillToolTipsWorlds();
            FillToolTipsRanks();
            FillToolTipsSecurity();
            FillToolTipsSavingAndBackup();
            FillToolTipsLogging();
            FillToolTipsIRC();
            FillToolTipsAdvanced();

            dgvWorlds.DataError += delegate( object sender, DataGridViewDataErrorEventArgs e ) {
                MessageBox.Show( e.Exception.Message, "Data Error" );
            };

            nMaxPlayers.Maximum = Config.MaxPlayersSupported;

            Config.logToString = true;

            ZLibStream.Init();

            Load += LoadConfig;
        }

        void FillOptionList() {
            foreach( Permission permission in Enum.GetValues( typeof( Permission ) ) ) {
                ListViewItem item = new ListViewItem( permission.ToString() );
                item.Tag = permission;
                vPermissions.Items.Add( item );
            }

            foreach( LogType type in Enum.GetValues( typeof( LogType ) ) ) {
                ListViewItem item = new ListViewItem( type.ToString() );
                item.Tag = type;
                vLogFileOptions.Items.Add( item );
                vConsoleOptions.Items.Add( (ListViewItem)item.Clone() );
            }

            FillToolTipsLogging();
        }

        internal static void HandleWorldRename( string from, string to ) {
            if( instance.cMainWorld.SelectedItem == null ) {
                instance.cMainWorld.SelectedIndex = 0;
            } else {
                string mainWorldName = instance.cMainWorld.SelectedItem.ToString();
                instance.FillWorldList();
                if( mainWorldName == from ) {
                    instance.cMainWorld.SelectedItem = to;
                } else {
                    instance.cMainWorld.SelectedItem = mainWorldName;
                }
            }
        }

        void FillWorldList() {
            cMainWorld.Items.Clear();
            foreach( WorldListEntry world in worlds ) {
                cMainWorld.Items.Add( world.name );
            }
        }

        #endregion

        #region Input Handlers

        #region General

        private void bMeasure_Click( object sender, EventArgs e ) {
            Process.Start( "http://www.speedtest.net/" );
        }

        private void bAnnouncements_Click( object sender, EventArgs e ) {
            TextEditorPopup popup = new TextEditorPopup( Server.AnnouncementsFile, "" );
            popup.ShowDialog();
        }

        private void xAnnouncements_CheckedChanged( object sender, EventArgs e ) {
            nAnnouncements.Enabled = xAnnouncements.Checked;
            bAnnouncements.Enabled = xAnnouncements.Checked;
        }

        private void bPortCheck_Click( object sender, EventArgs e ) {
            bPortCheck.Text = "Checking";
            this.Enabled = false;
            TcpListener listener = null;

            try {
                listener = new TcpListener( IPAddress.Any, (int)nPort.Value );
                listener.Start();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( "http://www.utorrent.com/testport?plain=1&port=" + nPort.Value );
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if( response.StatusCode == HttpStatusCode.OK ) {
                    using( Stream stream = response.GetResponseStream() ) {
                        StreamReader reader = new StreamReader( stream );
                        if( reader.ReadLine().StartsWith( "ok" ) ) {
                            MessageBox.Show( "Port " + nPort.Value + " is open!", "Port check success" );
                            return;
                        }
                    }
                }
                MessageBox.Show( "Port " + nPort.Value + " is closed. You will need to set up forwarding.", "Port check failed" );

            } catch {
                MessageBox.Show( "Could not start listening on port " + nPort.Value + ". Another program may be using the port.", "Port check failed" );
            } finally {
                if( listener != null ) {
                    listener.Stop();
                }
                this.Enabled = true;
                bPortCheck.Text = "Check";
            }
        }

        private void tIP_Validating( object sender, CancelEventArgs e ) {
            IPAddress IP;
            if( IPAddress.TryParse( tIP.Text, out IP ) ) {
                tIP.ForeColor = SystemColors.ControlText;
            } else {
                tIP.ForeColor = Color.Red;
                e.Cancel = true;
            }
        }

        private void xIP_CheckedChanged( object sender, EventArgs e ) {
            tIP.Enabled = xIP.Checked;
            if( !xIP.Checked ) {
                tIP.Text = IPAddress.Any.ToString();
            }
        }

        #endregion

        #region Worlds

        private void bAddWorld_Click( object sender, EventArgs e ) {
            AddWorldPopup popup = new AddWorldPopup( null );
            if( popup.ShowDialog() == DialogResult.OK ) {
                worlds.Add( popup.world );
            }
            string mainWorldName;
            if( cMainWorld.SelectedItem == null ) {
                FillWorldList();
                if( cMainWorld.Items.Count > 0 ) {
                    cMainWorld.SelectedIndex = 0;
                }
            } else {
                mainWorldName = cMainWorld.SelectedItem.ToString();
                FillWorldList();
                cMainWorld.SelectedItem = mainWorldName;
            }
        }

        private void bWorldEdit_Click( object sender, EventArgs e ) {
            AddWorldPopup popup = new AddWorldPopup( worlds[dgvWorlds.SelectedRows[0].Index] );
            if( popup.ShowDialog() == DialogResult.OK ) {
                string oldName = worlds[dgvWorlds.SelectedRows[0].Index].name;
                worlds[dgvWorlds.SelectedRows[0].Index] = popup.world;
                HandleWorldRename( oldName, popup.world.name );
            }
        }

        private void dgvWorlds_SelectionChanged( object sender, EventArgs e ) {
            bool oneRowSelected = (dgvWorlds.SelectedRows.Count == 1);
            bWorldDelete.Enabled = oneRowSelected;
            bWorldEdit.Enabled = oneRowSelected;
        }

        private void bWorldDel_Click( object sender, EventArgs e ) {
            if( dgvWorlds.SelectedRows.Count > 0 ) {
                WorldListEntry world = worlds[dgvWorlds.SelectedRows[0].Index];
                string fileName = "maps/" + world.Name + ".fcm";
                if( File.Exists( fileName ) &&
                    MessageBox.Show( "Do you want to delete the map file (" + fileName + ") as well?", "Warning", MessageBoxButtons.YesNo ) == DialogResult.Yes ) {
                    try {
                        File.Delete( fileName );
                    } catch( Exception ex ) {
                        MessageBox.Show( "You have to delete the file (" + fileName + ") manually. " +
                                         "An error occured while trying to delete it automatically:" + Environment.NewLine + ex, "Error" );
                    }
                }

                worlds.Remove( world );

                // handle change of main world
                if( cMainWorld.SelectedItem == null ) {
                    FillWorldList();
                    if( cMainWorld.Items.Count > 0 ) {
                        cMainWorld.SelectedIndex = 0;
                    }
                } else {
                    string mainWorldName = cMainWorld.SelectedItem.ToString();
                    FillWorldList();
                    if( mainWorldName == world.name ) {
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
            nSaveInterval.Enabled = xSaveInterval.Checked;
        }

        private void xBackupAtInterval_CheckedChanged( object sender, EventArgs e ) {
            nBackupInterval.Enabled = xBackupInterval.Checked;
        }

        private void xMaxBackups_CheckedChanged( object sender, EventArgs e ) {
            nMaxBackups.Enabled = xMaxBackups.Checked;
        }

        private void xMaxBackupSize_CheckedChanged( object sender, EventArgs e ) {
            nMaxBackupSize.Enabled = xMaxBackupSize.Checked;
        }

        #endregion

        #region IRC

        private void xIRC_CheckedChanged( object sender, EventArgs e ) {
            gIRCNetwork.Enabled = xIRC.Checked;
            gIRCOptions.Enabled = xIRC.Checked;
        }

        private void xIRCRegisteredNick_CheckedChanged( object sender, EventArgs e ) {
            tIRCNickServ.Enabled = xIRCRegisteredNick.Checked;
            tIRCNickServMessage.Enabled = xIRCRegisteredNick.Checked;
        }

        #endregion

        #endregion

        #region Ranks

        BindingList<string> rankNameList;

        void SelectRank( Rank rank ) {
            if( rank == null ) {
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

            ApplyColor( bColorRank, fCraft.Color.ParseToIndex( rank.Color ) );

            tPrefix.Text = rank.Prefix;
            cKickLimit.SelectedIndex = rank.GetLimitIndex( Permission.Kick );
            cBanLimit.SelectedIndex = rank.GetLimitIndex( Permission.Ban );
            cPromoteLimit.SelectedIndex = rank.GetLimitIndex( Permission.Promote );
            cDemoteLimit.SelectedIndex = rank.GetLimitIndex( Permission.Demote );
            cMaxHideFrom.SelectedIndex = rank.GetLimitIndex( Permission.Hide );
            cFreezeLimit.SelectedIndex = rank.GetLimitIndex( Permission.Freeze );
            xReserveSlot.Checked = rank.ReservedSlot;
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
            nDrawLimit.Enabled = xDrawLimit.Checked;

            foreach( ListViewItem item in vPermissions.Items ) {
                item.Checked = rank.Permissions[item.Index];
                if( item.Checked ) {
                    item.Font = bold;
                } else {
                    item.Font = vPermissions.Font;
                }
            }

            cKickLimit.Enabled = rank.Can( Permission.Kick );
            cBanLimit.Enabled = rank.Can( Permission.Ban );
            cPromoteLimit.Enabled = rank.Can( Permission.Promote );
            cDemoteLimit.Enabled = rank.Can( Permission.Demote );
            cMaxHideFrom.Enabled = rank.Can( Permission.Hide );
            cFreezeLimit.Enabled = rank.Can( Permission.Freeze );

            xDrawLimit.Enabled = rank.Can( Permission.Draw );
            nDrawLimit.Enabled &= rank.Can( Permission.Draw );

            gRankOptions.Enabled = true;
            lPermissions.Enabled = true;
            vPermissions.Enabled = true;

            bDeleteRank.Enabled = true;
            bRaiseRank.Enabled = (selectedRank != RankList.HighestRank);
            bLowerRank.Enabled = (selectedRank != RankList.LowestRank);
        }

        void RebuildRankList() {
            vRanks.Items.Clear();
            foreach( Rank rank in RankList.Ranks ) {
                vRanks.Items.Add( rank.ToComboBoxOption() );
            }
            if( selectedRank != null ) {
                vRanks.SelectedIndex = selectedRank.Index;
            }
            SelectRank( selectedRank );

            FillRankList( cDefaultRank, "(lowest rank)" );
            cDefaultRank.SelectedIndex = RankList.GetIndex( defaultRank );
            FillRankList( cPatrolledRank, "(lowest rank)" );
            cPatrolledRank.SelectedIndex = RankList.GetIndex( patrolledRank );

            FillRankList( cKickLimit, "(own rank)" );
            FillRankList( cBanLimit, "(own rank)" );
            FillRankList( cPromoteLimit, "(own rank)" );
            FillRankList( cDemoteLimit, "(own rank)" );
            FillRankList( cMaxHideFrom, "(own rank)" );
            FillRankList( cFreezeLimit, "(own rank)" );

            if( selectedRank != null ) {
                cKickLimit.SelectedIndex = selectedRank.GetLimitIndex( Permission.Kick );
                cBanLimit.SelectedIndex = selectedRank.GetLimitIndex( Permission.Ban );
                cPromoteLimit.SelectedIndex = selectedRank.GetLimitIndex( Permission.Promote );
                cDemoteLimit.SelectedIndex = selectedRank.GetLimitIndex( Permission.Demote );
                cMaxHideFrom.SelectedIndex = selectedRank.GetLimitIndex( Permission.Hide );
                cFreezeLimit.SelectedIndex = selectedRank.GetLimitIndex( Permission.Freeze );
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

            FillRankList( cPromoteLimit, "(own rank)" );
            FillRankList( cDemoteLimit, "(own rank)" );
            FillRankList( cKickLimit, "(own rank)" );
            FillRankList( cBanLimit, "(own rank)" );
            FillRankList( cMaxHideFrom, "(own rank)" );
            FillRankList( cFreezeLimit, "(own rank)" );

            cPromoteLimit.SelectedIndex = 0;
            cDemoteLimit.SelectedIndex = 0;
            cKickLimit.SelectedIndex = 0;
            cBanLimit.SelectedIndex = 0;
            cMaxHideFrom.SelectedIndex = 0;
            cFreezeLimit.SelectedIndex = 0;

            xReserveSlot.Checked = false;
            xKickIdle.Checked = false;
            nKickIdle.Value = 0;
            xAntiGrief.Checked = false;
            nAntiGriefBlocks.Value = 0;
            xDrawLimit.Checked = false;
            nDrawLimit.Value = 0;
            foreach( ListViewItem item in vPermissions.Items ) {
                item.Checked = false;
                item.Font = vPermissions.Font;
            }
            gRankOptions.Enabled = false;
            lPermissions.Enabled = false;
            vPermissions.Enabled = false;
        }

        static void FillRankList( ComboBox box, string firstItem ) {
            box.Items.Clear();
            box.Items.Add( firstItem );
            foreach( Rank rank in RankList.Ranks ) {
                box.Items.Add( rank.ToComboBoxOption() );
            }
        }

        #region Permission Limits

        private void cPromoteLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            PermissionLimitChange( Permission.Promote, cPromoteLimit );
        }

        private void cDemoteLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            PermissionLimitChange( Permission.Demote, cDemoteLimit );
        }

        private void cKickLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            PermissionLimitChange( Permission.Kick, cKickLimit );
        }

        private void cBanLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            PermissionLimitChange( Permission.Ban, cBanLimit );
        }

        private void cMaxHideFrom_SelectedIndexChanged( object sender, EventArgs e ) {
            PermissionLimitChange( Permission.Hide, cMaxHideFrom );
        }

        private void cFreezeLimit_SelectedIndexChanged( object sender, EventArgs e ) {
            PermissionLimitChange( Permission.Freeze, cFreezeLimit );
        }

        void PermissionLimitChange( Permission permission, ComboBox control ) {
            if( selectedRank != null ) {
                if( control.SelectedIndex == 0 ) {
                    selectedRank.ResetLimit( permission );
                } else {
                    selectedRank.SetLimit( permission, RankList.FindRank( control.SelectedIndex - 1 ) );
                }
            }
        }

        #endregion

        #region Ranks Input Handlers

        private void bAddRank_Click( object sender, EventArgs e ) {
            int number = 1;
            while( RankList.RanksByName.ContainsKey( "rank" + number ) ) number++;

            Rank rank = new Rank();
            rank.ID = RankList.GenerateID();
            rank.Name = "rank" + number;
            rank.legacyNumericRank = 0;
            rank.Prefix = "";
            rank.ReservedSlot = false;
            rank.Color = "";

            defaultRank = RankList.FindRank( cDefaultRank.SelectedIndex - 1 );
            patrolledRank = RankList.FindRank( cPatrolledRank.SelectedIndex - 1 );

            RankList.AddRank( rank );
            selectedRank = null;

            RebuildRankList();
            SelectRank( rank );

            rankNameList.Insert( rank.Index + 1, rank.ToComboBoxOption() );
        }

        private void bDeleteRank_Click( object sender, EventArgs e ) {
            if( vRanks.SelectedItem != null ) {
                selectedRank = null;
                int index = vRanks.SelectedIndex;
                Rank deletedRank = RankList.FindRank( index );

                string messages = "";

                // Ask for substitute rank
                DeleteRankPopup popup = new DeleteRankPopup( deletedRank );
                if( popup.ShowDialog() != DialogResult.OK ) return;

                Rank replacementRank = popup.substituteRank;

                // Update default rank
                Rank defaultRank = RankList.FindRank( cDefaultRank.SelectedIndex - 1 );
                if( defaultRank == deletedRank ) {
                    defaultRank = replacementRank;
                    messages += "DefaultRank has been changed to \"" + replacementRank.Name + "\"" + Environment.NewLine;
                }

                // Delete rank
                if( RankList.DeleteRank( deletedRank, replacementRank ) ) {
                    messages += "Some of the rank limits for kick, ban, promote, and/or demote have been reset." + Environment.NewLine;
                }
                vRanks.Items.RemoveAt( index );

                // Update world permissions
                string worldUpdates = "";
                foreach( WorldListEntry world in worlds ) {
                    if( world.accessRank == deletedRank ) {
                        world.AccessPermission = replacementRank.ToComboBoxOption();
                        worldUpdates += " - " + world.name + ": access permission changed to " + replacementRank.Name + Environment.NewLine;
                    }
                    if( world.buildRank == deletedRank ) {
                        world.BuildPermission = replacementRank.ToComboBoxOption();
                        worldUpdates += " - " + world.name + ": build permission changed to " + replacementRank.Name + Environment.NewLine;
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


        private void tPrefix_Validating( object sender, CancelEventArgs e ) {
            if( selectedRank == null ) return;
            if( tPrefix.Text.Length > 0 && !Rank.IsValidPrefix( tPrefix.Text ) ) {
                MessageBox.Show( "Invalid prefix character!\n" +
                    "Prefixes may only contain characters that are allowed in chat (except space).", "Warning" );
                tPrefix.ForeColor = Color.Red;
                e.Cancel = true;
            } else {
                tPrefix.ForeColor = SystemColors.ControlText;
            }
            if( selectedRank.Prefix == tPrefix.Text ) return;

            defaultRank = RankList.FindRank( cDefaultRank.SelectedIndex - 1 );
            patrolledRank = RankList.FindRank( cPatrolledRank.SelectedIndex - 1 );

            string oldName = selectedRank.ToComboBoxOption();

            // To avoid DataErrors in World tab's DataGridView while renaming a rank,
            // the new name is first added to the list of options (without removing the old name)
            rankNameList.Insert( selectedRank.Index + 1, String.Format( "{0,1}{1}", tPrefix.Text, selectedRank.Name ) );

            selectedRank.Prefix = tPrefix.Text;

            // Remove the old name from the list of options
            rankNameList.Remove( oldName );

            worlds.ResetBindings();
            RebuildRankList();
        }

        private void xReserveSlot_CheckedChanged( object sender, EventArgs e ) {
            if( selectedRank == null ) return;
            selectedRank.ReservedSlot = xReserveSlot.Checked;
        }

        private void nKickIdle_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xKickIdle.Checked ) return;
            selectedRank.IdleKickTimer = Convert.ToInt32( nKickIdle.Value );
        }

        private void nAntiGriefBlocks_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xAntiGrief.Checked ) return;
            selectedRank.AntiGriefBlocks = Convert.ToInt32( nAntiGriefBlocks.Value );
        }

        private void nAntiGriefSeconds_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xAntiGrief.Checked ) return;
            selectedRank.AntiGriefSeconds = Convert.ToInt32( nAntiGriefSeconds.Value );
        }

        private void nDrawLimit_ValueChanged( object sender, EventArgs e ) {
            if( selectedRank == null || !xDrawLimit.Checked ) return;
            selectedRank.DrawLimit = Convert.ToInt32( nDrawLimit.Value );
            double cubed = Math.Pow( Convert.ToDouble( nDrawLimit.Value ), 1 / 3d );
            lDrawLimitUnits.Text = String.Format( "blocks ({0:0}\u00B3)", cubed ); ;
        }


        private void xSpamChatKick_CheckedChanged( object sender, EventArgs e ) {
            nSpamChatWarnings.Enabled = xSpamChatKick.Checked;
        }

        private void vRanks_SelectedIndexChanged( object sender, EventArgs e ) {
            if( vRanks.SelectedIndex != -1 ) {
                SelectRank( RankList.FindRank( vRanks.SelectedIndex ) );
            } else {
                DisableRankOptions();
            }
        }

        private void xKickIdle_CheckedChanged( object sender, EventArgs e ) {
            nKickIdle.Enabled = xKickIdle.Checked;
            if( selectedRank != null ) {
                if( xKickIdle.Checked ) {
                    nKickIdle.Value = selectedRank.IdleKickTimer;
                } else {
                    nKickIdle.Value = 0;
                    selectedRank.IdleKickTimer = 0;
                }
            }
        }

        private void xAntiGrief_CheckedChanged( object sender, EventArgs e ) {
            nAntiGriefBlocks.Enabled = xAntiGrief.Checked;
            if( selectedRank != null ) {
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
        }

        private void xDrawLimit_CheckedChanged( object sender, EventArgs e ) {
            nDrawLimit.Enabled = xDrawLimit.Checked;
            if( selectedRank != null ) {
                if( xDrawLimit.Checked ) {
                    nDrawLimit.Value = selectedRank.DrawLimit;
                    double cubed = Math.Pow( Convert.ToDouble( nDrawLimit.Value ), 1 / 3d );
                    lDrawLimitUnits.Text = String.Format( "blocks ({0:0}\u00B3)", cubed ); ;
                } else {
                    nDrawLimit.Value = 0;
                    selectedRank.DrawLimit = 0;
                    lDrawLimitUnits.Text = "blocks";
                }
                nDrawLimit.Enabled = xDrawLimit.Checked;
            }
        }

        private void vPermissions_ItemChecked( object sender, ItemCheckedEventArgs e ) {
            bool check = e.Item.Checked;
            if( check ) {
                e.Item.Font = bold;
            } else {
                e.Item.Font = vPermissions.Font;
            }
            if( selectedRank == null ) return;
            switch( (Permission)e.Item.Tag ) {
                case Permission.Ban:
                    cBanLimit.Enabled = check;
                    if( !check ) {
                        vPermissions.Items[(int)Permission.BanIP].Checked = false;
                        vPermissions.Items[(int)Permission.BanAll].Checked = false;
                    }
                    break;
                case Permission.BanIP:
                    if( check ) vPermissions.Items[(int)Permission.Ban].Checked = true;
                    break;
                case Permission.BanAll:
                    if( check ) vPermissions.Items[(int)Permission.Ban].Checked = true;
                    break;
                case Permission.Kick:
                    cKickLimit.Enabled = check; break;
                case Permission.Promote:
                    cPromoteLimit.Enabled = check; break;
                case Permission.Demote:
                    cDemoteLimit.Enabled = check; break;
                case Permission.Draw:
                    xDrawLimit.Enabled = check; break;
                case Permission.Hide:
                    cMaxHideFrom.Enabled = check; break;
                case Permission.Freeze:
                    cFreezeLimit.Enabled = check; break;
            }

            selectedRank.Permissions[(int)e.Item.Tag] = e.Item.Checked;
        }


        private void tRankName_Validating( object sender, CancelEventArgs e ) {
            if( selectedRank == null ) return;

            string newName = tRankName.Text.Trim();

            if( newName == selectedRank.Name ) {
                return;

            } else if( newName.Length == 0 ) {
                MessageBox.Show( "Rank name cannot be blank." );
                tRankName.ForeColor = Color.Red;
                e.Cancel = true;

            } else if( !Rank.IsValidRankName( newName ) ) {
                MessageBox.Show( "Rank name can only contain letters, digits, and underscores." );
                tRankName.ForeColor = Color.Red;
                e.Cancel = true;

            } else if( !RankList.CanRenameRank( selectedRank, newName ) ) {
                MessageBox.Show( "There is already another rank named \"" + newName + "\".\n" +
                                 "Duplicate rank names are now allowed." );
                tRankName.ForeColor = Color.Red;
                e.Cancel = true;

            } else {
                string oldName = selectedRank.ToComboBoxOption();

                tRankName.ForeColor = SystemColors.ControlText;
                defaultRank = RankList.FindRank( cDefaultRank.SelectedIndex - 1 );
                patrolledRank = RankList.FindRank( cPatrolledRank.SelectedIndex - 1 );

                // To avoid DataErrors in World tab's DataGridView while renaming a rank,
                // the new name is first added to the list of options (without removing the old name)
                rankNameList.Insert( selectedRank.Index + 1, String.Format( "{0,1}{1}", selectedRank.Prefix, newName ) );

                RankList.RenameRank( selectedRank, newName );

                // Remove the old name from the list of options
                rankNameList.Remove( oldName );

                worlds.ResetBindings();
                RebuildRankList();
            }
        }


        private void bRaiseRank_Click( object sender, EventArgs e ) {
            if( selectedRank != null ) {
                defaultRank = RankList.FindRank( cDefaultRank.SelectedIndex - 1 );
                patrolledRank = RankList.FindRank( cPatrolledRank.SelectedIndex - 1 );
                RankList.RaiseRank( selectedRank );
                RebuildRankList();
                rankNameList.Insert( selectedRank.Index + 1, selectedRank.ToComboBoxOption() );
                rankNameList.RemoveAt( selectedRank.Index + 3 );
            }
        }

        private void bLowerRank_Click( object sender, EventArgs e ) {
            if( selectedRank != null ) {
                defaultRank = RankList.FindRank( cDefaultRank.SelectedIndex - 1 );
                patrolledRank = RankList.FindRank( cPatrolledRank.SelectedIndex - 1 );
                RankList.LowerRank( selectedRank );
                RebuildRankList();
                rankNameList.Insert( selectedRank.Index + 2, selectedRank.ToComboBoxOption() );
                rankNameList.RemoveAt( selectedRank.Index );
            }
        }

        #endregion

        #endregion

        #region Apply / Save / Cancel Buttons

        private void bApply_Click( object sender, EventArgs e ) {
            SaveConfig();
            if( Config.errors.Length > 0 ) {
                MessageBox.Show( Config.errors, "Some errors were found in the selected values:" );
            } else if( Config.Save(false) ) {
                bApply.Enabled = false;
            } else {
                MessageBox.Show( Config.errors, "An error occured while trying to save:" );
            }
        }

        private void bSave_Click( object sender, EventArgs e ) {
            SaveConfig();
            if( Config.errors.Length > 0 ) {
                MessageBox.Show( Config.errors, "Some errors were found in the selected values:" );
            } else if( Config.Save(false) ) {
                bApply.Enabled = false;
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
                Config.ResetRanks();

                ApplyTabGeneral();
                ApplyTabWorlds(); // also reloads world list
                ApplyTabRanks();
                ApplyTabSecurity();
                ApplyTabSavingAndBackup();
                ApplyTabLogging();
                ApplyTabIRC();
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
                    case 1:// Worlds
                        ApplyTabWorlds(); // also reloads world list
                        break;
                    case 2:// Ranks
                        Config.ResetRanks();
                        ApplyTabWorlds();
                        ApplyTabRanks();
                        defaultRank = null;
                        patrolledRank = null;
                        RebuildRankList();
                        break;
                    case 3:// Security
                        Config.LoadDefaultsSecurity();
                        ApplyTabSecurity();
                        break;
                    case 4:// Saving and Backup
                        Config.LoadDefaultsSavingAndBackup();
                        ApplyTabSavingAndBackup();
                        break;
                    case 5:// Logging
                        Config.LoadDefaultsLogging();
                        ApplyTabLogging();
                        break;
                    case 6:// IRC
                        Config.LoadDefaultsIRC();
                        ApplyTabIRC();
                        break;
                    case 7:// Advanced
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
        int colorSys, colorSay, colorHelp, colorAnnouncement, colorPM, colorIRC;

        void ApplyColor( Button button, int color ) {
            button.Text = fCraft.Color.GetName( color );
            button.BackColor = ColorPicker.colors[color].background;
            button.ForeColor = ColorPicker.colors[color].foreground;
            bApply.Enabled = true;
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

        private void bColorAnnouncement_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Announcement color", colorAnnouncement );
            picker.ShowDialog();
            colorAnnouncement = picker.color;
            ApplyColor( bColorAnnouncement, colorAnnouncement );
        }

        private void bColorRank_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Rank color for \"" + selectedRank.Name + "\"", fCraft.Color.ParseToIndex( selectedRank.Color ) );
            picker.ShowDialog();
            ApplyColor( bColorRank, picker.color );
            selectedRank.Color = fCraft.Color.GetName( picker.color );
        }

        private void bColorPM_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "Private / rank chat color", colorPM );
            picker.ShowDialog();
            colorPM = picker.color;
            ApplyColor( bColorPM, colorPM );
        }


        private void bColorIRC_Click( object sender, EventArgs e ) {
            ColorPicker picker = new ColorPicker( "IRC message color", colorIRC );
            picker.ShowDialog();
            colorIRC = picker.color;
            ApplyColor( bColorIRC, colorIRC );
        }

        #endregion

        private void bRules_Click( object sender, EventArgs e ) {
            TextEditorPopup popup = new TextEditorPopup( "rules.txt", "Use common sense!" );
            popup.ShowDialog();
        }

        internal static bool IsWorldNameTaken( string name ) {
            foreach( WorldListEntry world in worlds ) {
                if( world.Name == name ) return true;
            }
            return false;
        }

        #endregion

        private void ConfigUI_FormClosing( object sender, FormClosingEventArgs e ) {
            if( bApply.Enabled ) {
                switch( MessageBox.Show( "Would you like to save the changes before exiting?", "Warning", MessageBoxButtons.YesNoCancel ) ) {
                    case DialogResult.Yes:
                        SaveConfig();
                        if( Config.errors.Length > 0 ) {
                            MessageBox.Show( Config.errors, "Some errors were found in the selected values:" );
                        } else if( Config.Save(false) ) {
                            bApply.Enabled = false;
                        } else {
                            MessageBox.Show( Config.errors, "An error occured while trying to save:" );
                        }
                        return;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }
        }

        private void nMaxUndo_ValueChanged( object sender, EventArgs e ) {
            if( nMaxUndo.Value == 0 ) {
                lMaxUndoUnits.Text = "(unlimited, 1 MB RAM = 65,536 blocks)";
            } else {
                decimal maxMemUsage = Math.Ceiling( nMaxUndo.Value * 160 / 1024 / 1024 ) / 10;
                lMaxUndoUnits.Text = String.Format( "(up to {0:0.0} MB of RAM per player)", maxMemUsage );
            }
        }

        private void xMaxUndo_CheckedChanged( object sender, EventArgs e ) {
            nMaxUndo.Enabled = xMaxUndo.Checked;
            lMaxUndoUnits.Enabled = xMaxUndo.Checked;
        }
    }
}