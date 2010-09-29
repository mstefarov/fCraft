using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using fCraft;


namespace ConfigTool {
    public sealed partial class DeleteRankPopup : Form {
        internal Rank substituteRank;

        public DeleteRankPopup( Rank rank ) {
            InitializeComponent();
            foreach( Rank pc in RankList.ranksByIndex ) {
                if( pc != rank ) {
                    cSubstitute.Items.Add( pc.ToComboBoxOption() );
                }
            }
            lWarning.Text = String.Format( lWarning.Text, rank.Name );
            cSubstitute.SelectedIndex = cSubstitute.Items.Count - 1;
        }


        private void cSubstitute_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cSubstitute.SelectedIndex >= 0 ) {
                foreach( Rank rank in RankList.ranksByIndex ) {
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
