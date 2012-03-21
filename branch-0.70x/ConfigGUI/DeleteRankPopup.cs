// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    sealed partial class DeleteRankPopup : Form {
        internal Rank SubstituteRank { get; private set; }

        public DeleteRankPopup( Rank deletedRank ) {
            InitializeComponent();
            foreach( Rank rank in RankManager.Ranks ) {
                if( rank != deletedRank ) {
                    cSubstitute.Items.Add( MainForm.ToComboBoxOption( rank ) );
                }
            }
            lWarning.Text = String.Format( lWarning.Text, deletedRank.Name );
            cSubstitute.SelectedIndex = cSubstitute.Items.Count - 1;
        }


        private void cSubstitute_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cSubstitute.SelectedIndex < 0 ) return;
            foreach( Rank rank in RankManager.Ranks ) {
                if( cSubstitute.SelectedItem.ToString() != MainForm.ToComboBoxOption( rank ) ) continue;
                SubstituteRank = rank;
                bDelete.Enabled = true;
                break;
            }
        }
    }
}
