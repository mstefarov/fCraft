using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;


namespace ConfigTool {
    public partial class AddWorldPopup : Form {
        BackgroundWorker worker = new BackgroundWorker();

        public AddWorldPopup() {
            InitializeComponent();
            cAccess.SelectedIndex = 0;
            cBuild.SelectedIndex = 0;
            cBackup.SelectedIndex = 0;
            cWorld.SelectedIndex = 0;
            cTerrain.SelectedIndex = 0;
            cTheme.SelectedIndex = 0;

            // this forces calling all the *_CheckedChanged methods, disabling everything unnecessary
            rCopy.Checked = true;
            rEmpty.Checked = true;
            rFlatgrass.Checked = true;
            rTerrain.Checked = true;
            rLoad.Checked = true;
        }


        void ToggleDimensions() {
            bool isMapGenerated = (rEmpty.Checked || rFlatgrass.Checked || rTerrain.Checked);
            if( !isMapGenerated ) xFloodBarrier.Checked = false;
            xFloodBarrier.Enabled = isMapGenerated;
            nWidthX.Enabled = isMapGenerated;
            nWidthY.Enabled = isMapGenerated;
            nHeight.Enabled = isMapGenerated;
        }


        private void rLoad_CheckedChanged( object sender, EventArgs e ) {
            tFile.Enabled = rLoad.Checked;
            bBrowse.Enabled = rLoad.Checked;
            ToggleDimensions();
        }

        private void rCopy_CheckedChanged( object sender, EventArgs e ) {
            cWorld.Enabled = rCopy.Checked;
            ToggleDimensions();
        }

        private void rTerrain_CheckedChanged( object sender, EventArgs e ) {
            lTerrain.Enabled = rTerrain.Checked;
            cTerrain.Enabled = rTerrain.Checked;
            lTheme.Enabled = rTerrain.Checked;
            cTheme.Enabled = rTerrain.Checked;
            bGenerate.Enabled = rTerrain.Checked;
        }

        private void bBrowse_Click( object sender, EventArgs e ) {
            fileBrowser.FileName = tFile.Text;
            fileBrowser.ShowDialog();
            tFile.Text = fileBrowser.FileName;
            tFile.SelectAll();
        }
    }
}
