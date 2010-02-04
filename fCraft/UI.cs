using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace fCraft {
    public partial class UI : Form {
        public UI() {
            InitializeComponent();
            this.FormClosing += HandleShutDown;

            trayIcon.Visible = true;
            trayIcon.BalloonTipText = "Server is running. Click this icon to see the main window.";
            trayIcon.BalloonTipTitle = "fCraft Custom Server";

            //this.WindowState = FormWindowState.Minimized;
            //this.ShowInTaskbar = false;

            Logger.Init( "fCraft.log", this );
            Color.Init();
            Config.Init( "config.xml" );
            World.Init(this);
        }

        private void HandleShutDown( object sender, CancelEventArgs e ) {
            //MessageBox.Show( "Shutting down" );
            World.server.ShutDown();
        }

        delegate void LogDelegate( string message );
        public void Log( string message ) {
            if( this.textBox1.InvokeRequired ) {
                LogDelegate d = new LogDelegate( LogInternal );
                this.Invoke
                    ( d, new object[] { message } );
            } else {
                LogInternal( message );
            }
        }
        private void LogInternal( string message ) {
            textBox1.Text += message + Environment.NewLine;
        }

        private void trayIcon_Click( object sender, EventArgs e ) {
            if( !ShowInTaskbar ) {
                this.ShowInTaskbar = true;
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void UI_Resize( object sender, EventArgs e ) {
            if( this.WindowState == FormWindowState.Minimized ) {
                this.ShowInTaskbar = false;
            }
        }

    }
}
