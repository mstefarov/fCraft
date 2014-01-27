// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    static class Program {
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
#if DEBUG
            Application.Run( new MainForm() );
#else
            try {
                Application.Run( new MainForm() );
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error in ConfigGUI", "ConfigGUI", ex, true );
                if( !Server.HasArg( ArgKey.ExitOnCrash ) ) {
                    MessageBox.Show( ex.ToString(), "fCraft ConfigGUI has crashed" );
                }
            }
#endif
        }
    }
}