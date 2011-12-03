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
            Logger.Init( "fCraft.log", this );
            Config.Init( "config.xml" );
            World.server = new Server();
            this.FormClosing += HandleShutDown;
            World.server.Run();
        }

        private void HandleShutDown( object sender, CancelEventArgs e ) {
            MessageBox.Show( "Shutting down" );
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

    }
}
