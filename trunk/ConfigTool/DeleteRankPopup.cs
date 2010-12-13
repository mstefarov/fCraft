using System;
using System.Windows.Forms;
using fCraft;


namespace ConfigTool {
    public sealed partial class DeleteRankPopup : Form {
        internal Rank substituteRank;

        public DeleteRankPopup( Rank deletedRank ) {
            InitializeComponent();
            foreach( Rank rank in RankList.Ranks ) {
                if( rank != deletedRank ) {
                    cSubstitute.Items.Add( rank.ToComboBoxOption() );
                }
            }
            lWarning.Text = String.Format( lWarning.Text, deletedRank.Name );
            cSubstitute.SelectedIndex = cSubstitute.Items.Count - 1;
        }


        private void cSubstitute_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cSubstitute.SelectedIndex >= 0 ) {
                foreach( Rank rank in RankList.Ranks ) {
                    if( cSubstitute.SelectedItem.ToString() == rank.ToComboBoxOption() ) {
                        substituteRank = rank;
                        bDelete.Enabled = true;
                        break;
                    }
                }
            }
        }
    }
}
