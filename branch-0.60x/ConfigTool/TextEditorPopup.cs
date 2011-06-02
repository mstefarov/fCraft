// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ConfigTool {
    public sealed partial class TextEditorPopup : Form {
        public string OriginalText { get; private set; }
        public string FileName { get; private set; }


        public TextEditorPopup( string fileName, string defaultValue ) {
            InitializeComponent();

            FileName = fileName;
            Text = "Editing " + FileName;

            if( File.Exists( fileName ) ) {
                OriginalText = File.ReadAllText( fileName );
            } else {
                OriginalText = defaultValue;
            }

            tRules.Text = OriginalText;
            lWarning.Visible = ContainsLongLines();
        }

        bool ContainsLongLines() {
            return tRules.Lines.Any( line => (line.Length > 62) );
        }


        private void tRules_KeyDown( object sender, KeyEventArgs e ) {
            lWarning.Visible = ContainsLongLines();
        }

        private void bOK_Click( object sender, EventArgs e ) {
            File.WriteAllText( FileName, tRules.Text );
            Close();
        }
    }
}
