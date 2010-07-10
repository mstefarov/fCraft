namespace fCraftUI {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && ( components != null ) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( MainForm ) );
            this.logBox = new System.Windows.Forms.TextBox();
            this.urlDisplay = new System.Windows.Forms.TextBox();
            this.URLLabel = new System.Windows.Forms.Label();
            this.console = new System.Windows.Forms.TextBox();
            this.playerList = new System.Windows.Forms.ListBox();
            this.playerListLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.Font = new System.Drawing.Font( "Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.logBox.Location = new System.Drawing.Point( 12, 38 );
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logBox.Size = new System.Drawing.Size( 618, 403 );
            this.logBox.TabIndex = 1;
            // 
            // urlDisplay
            // 
            this.urlDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.urlDisplay.Enabled = false;
            this.urlDisplay.Location = new System.Drawing.Point( 95, 12 );
            this.urlDisplay.Name = "urlDisplay";
            this.urlDisplay.ReadOnly = true;
            this.urlDisplay.Size = new System.Drawing.Size( 535, 20 );
            this.urlDisplay.TabIndex = 0;
            this.urlDisplay.Text = "Waiting for first heartbeat...";
            this.urlDisplay.WordWrap = false;
            // 
            // URLLabel
            // 
            this.URLLabel.AutoSize = true;
            this.URLLabel.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.URLLabel.Location = new System.Drawing.Point( 12, 15 );
            this.URLLabel.Name = "URLLabel";
            this.URLLabel.Size = new System.Drawing.Size( 77, 13 );
            this.URLLabel.TabIndex = 2;
            this.URLLabel.Text = "Server URL:";
            // 
            // console
            // 
            this.console.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.console.Enabled = false;
            this.console.Location = new System.Drawing.Point( 12, 447 );
            this.console.Multiline = true;
            this.console.Name = "console";
            this.console.Size = new System.Drawing.Size( 768, 20 );
            this.console.TabIndex = 3;
            this.console.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler( this.console_Enter );
            // 
            // playerList
            // 
            this.playerList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.playerList.FormattingEnabled = true;
            this.playerList.IntegralHeight = false;
            this.playerList.Location = new System.Drawing.Point( 636, 38 );
            this.playerList.Name = "playerList";
            this.playerList.Size = new System.Drawing.Size( 144, 403 );
            this.playerList.TabIndex = 4;
            // 
            // playerListLabel
            // 
            this.playerListLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.playerListLabel.AutoSize = true;
            this.playerListLabel.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.playerListLabel.Location = new System.Drawing.Point( 718, 15 );
            this.playerListLabel.Name = "playerListLabel";
            this.playerListLabel.Size = new System.Drawing.Size( 62, 13 );
            this.playerListLabel.TabIndex = 5;
            this.playerListLabel.Text = "Player list";
            // 
            // UI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 792, 479 );
            this.Controls.Add( this.playerListLabel );
            this.Controls.Add( this.playerList );
            this.Controls.Add( this.console );
            this.Controls.Add( this.URLLabel );
            this.Controls.Add( this.urlDisplay );
            this.Controls.Add( this.logBox );
            this.Icon = ((System.Drawing.Icon)(resources.GetObject( "$this.Icon" )));
            this.Name = "UI";
            this.Text = "fCraft";
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.TextBox urlDisplay;
        private System.Windows.Forms.Label URLLabel;
        private System.Windows.Forms.TextBox console;
        private System.Windows.Forms.ListBox playerList;
        private System.Windows.Forms.Label playerListLabel;
    }
}

