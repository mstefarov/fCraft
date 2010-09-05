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
    public sealed partial class DeleteClassPopup : Form {
        internal PlayerClass substituteClass;

        public DeleteClassPopup( PlayerClass _pc ) {
            InitializeComponent();
            foreach( PlayerClass pc in ClassList.classesByIndex ) {
                if( pc != _pc ) {
                    cSubstitute.Items.Add( pc.ToComboBoxOption() );
                }
            }
            lWarning.Text = String.Format( lWarning.Text, _pc.name );
            cSubstitute.SelectedIndex = cSubstitute.Items.Count - 1;
        }


        private void cSubstitute_SelectedIndexChanged( object sender, EventArgs e ) {
            if( cSubstitute.SelectedIndex >= 0 ) {
                foreach( PlayerClass pc in ClassList.classesByIndex ) {
                    if( cSubstitute.SelectedItem.ToString() == pc.ToComboBoxOption() ) {
                        substituteClass = pc;
                        bDelete.Enabled = true;
                        break;
                    }
                }
            }
        }
    }
}
