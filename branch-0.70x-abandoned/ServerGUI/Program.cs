// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.ServerGUI {
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
                Logger.LogAndReportCrash( "Unhandled exception in ServerGUI", "ServerGUI", ex, true );
            }
#endif
        }
    }
}