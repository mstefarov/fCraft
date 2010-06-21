// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;


namespace ConfigTool {
    public partial class TextEditorPopup : Form {

        string oldText, fileName;

        public TextEditorPopup( string _fileName, string defaultValue ) {
            InitializeComponent();

            fileName = _fileName;
            Text = "Editing " + fileName;

            if( File.Exists( fileName ) ) {
                oldText = File.ReadAllText( fileName );
            } else {
                oldText = defaultValue;
            }

            tRules.Text = oldText;
            lWarning.Visible = CheckForLongLines();
        }

        bool CheckForLongLines() {
            foreach( string line in tRules.Lines ) {
                if( line.Length > 62 ) return true;
            }
            return false;
        }

        private void tRules_KeyDown( object sender, KeyEventArgs e ) {
            lWarning.Visible = CheckForLongLines();
        }

        private void bOK_Click( object sender, EventArgs e ) {
            if( tRules.Text.Length > 0 ) {
                File.WriteAllText( fileName, tRules.Text );
            } else {
                File.Delete( fileName );
            }
            Close();
        }

        private void bCancel_Click( object sender, EventArgs e ) {
            Close();
        }
    }
}
