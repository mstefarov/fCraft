using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using fCraft;

namespace fCraftUI {
    class ConsoleBox : TextBox {
        const int WM_KEYDOWN = 0x100;
        const int WM_SYSKEYDOWN = 0x104;
        public SimpleEventHandler OnCommand;
        List<string> log = new List<string>();
        int logPointer;

        protected override bool ProcessCmdKey( ref Message msg, Keys keyData ) {
            if( keyData == Keys.Up ) {
                if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                    if( log.Count == 0 ) return true;
                    if( logPointer > 0 ) {
                        logPointer--;
                    }
                    Text = log[logPointer];
                    SelectAll();
                }
                return true;

            } else if( keyData == Keys.Down ) {
                if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                    if( log.Count == 0 ) return true;
                    if( logPointer < log.Count - 1 ) {
                        logPointer++;
                    }
                    Text = log[logPointer];
                    SelectAll();
                }
                return true;

            } else if( keyData == Keys.Enter ) {
                if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                    if( Text.Length > 0 ) {
                        log.Add( Text );
                        if( log.Count > 100 ) log.RemoveAt( 0 );
                        logPointer = log.Count;
                    }
                }
                if( OnCommand != null ) OnCommand();
                return true;

            } else if( keyData == Keys.Escape ) {
                if( msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_KEYDOWN ) {
                    logPointer = log.Count;
                    Text = "";
                }
                return base.ProcessCmdKey( ref msg, keyData );

            } else {
                return base.ProcessCmdKey( ref msg, keyData );
            }
        }
    }
}
