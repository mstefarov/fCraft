// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Windows.Forms;


namespace ConfigTool {
    public sealed partial class TextEditorPopup : Form {

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
            File.WriteAllText( fileName, tRules.Text );
            Close();
        }
    }
}
