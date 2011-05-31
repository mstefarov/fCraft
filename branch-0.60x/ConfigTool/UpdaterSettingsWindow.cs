using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ConfigTool {
    public partial class UpdaterSettingsWindow : Form {

        public string RunBeforeUpdate {
            get {
                if( xRunBeforeUpdate.Checked) return tRunBeforeUpdate.Text;
                else return "";
            }
            set { tRunBeforeUpdate.Text = value; }
        }

        public string RunAfterUpdate {
            get {
                if( xRunAfterUpdate.Checked ) return tRunAfterUpdate.Text;
                else return "";
            }
            set { tRunAfterUpdate.Text = value; }
        }

        public fCraft.UpdaterMode UpdaterMode {
            get {
                if( rDisabled.Checked ) return fCraft.UpdaterMode.Disabled;
                if( rNotify.Checked ) return fCraft.UpdaterMode.Notify;
                if( rPrompt.Checked ) return fCraft.UpdaterMode.Prompt;
                return fCraft.UpdaterMode.Auto;
            }
            set {
                switch( value ) {
                    case fCraft.UpdaterMode.Disabled:
                        rDisabled.Checked = true; break;
                    case fCraft.UpdaterMode.Notify:
                        rNotify.Checked = true; break;
                    case fCraft.UpdaterMode.Prompt:
                        rPrompt.Checked = true; break;
                    case fCraft.UpdaterMode.Auto:
                        rAutomatic.Checked = true; break;
                }
            }
        }

        public bool BackupBeforeUpdate {
            get { return xBackupBeforeUpdating.Checked; }
            set { xBackupBeforeUpdating.Checked = value; }
        }

        string oldRunBeforeUpdate, oldRunAfterUpdate;
        fCraft.UpdaterMode oldUpdaterMode;
        bool oldBackupBeforeUpdate;

        public UpdaterSettingsWindow() {
            InitializeComponent();
            Shown += delegate {
                oldRunBeforeUpdate = RunBeforeUpdate;
                oldRunAfterUpdate = RunAfterUpdate;
                oldUpdaterMode = UpdaterMode;
                oldBackupBeforeUpdate = BackupBeforeUpdate;
            };
            FormClosed += delegate( object sender, FormClosedEventArgs e ) {
                if( DialogResult != DialogResult.OK ) {
                    RunBeforeUpdate = oldRunBeforeUpdate;
                    RunAfterUpdate = oldRunAfterUpdate;
                    UpdaterMode = oldUpdaterMode;
                    BackupBeforeUpdate = oldBackupBeforeUpdate;
                }
            };
        }

        private void xRunBeforeUpdate_CheckedChanged( object sender, EventArgs e ) {
            tRunBeforeUpdate.Enabled = xRunBeforeUpdate.Checked;
        }

        private void xRunAfterUpdate_CheckedChanged( object sender, EventArgs e ) {
            tRunAfterUpdate.Enabled = xRunAfterUpdate.Checked;
        }

        private void rDisabled_CheckedChanged( object sender, EventArgs e ) {
            gOptions.Enabled = !rDisabled.Checked;
        }


        private void tRunBeforeUpdate_TextChanged( object sender, EventArgs e ) {
            if( tRunBeforeUpdate.Text.Length > 0 ) {
                xRunBeforeUpdate.Checked = true;
            }
        }

        private void tRunAfterUpdate_TextChanged( object sender, EventArgs e ) {
            if( tRunAfterUpdate.Text.Length > 0 ) {
                xRunAfterUpdate.Checked = true;
            }
        }
    }
}