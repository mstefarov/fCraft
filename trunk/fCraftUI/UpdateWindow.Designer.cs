namespace fCraftUI {
    partial class UpdateWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && (components != null) ) {
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
            this.changelog = new System.Windows.Forms.TextBox();
            this.lHeader = new System.Windows.Forms.Label();
            this.bCancel = new System.Windows.Forms.Button();
            this.bUpdateNow = new System.Windows.Forms.Button();
            this.bUpdateLater = new System.Windows.Forms.Button();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.lProgress = new System.Windows.Forms.Label();
            this.lVersion = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // changelog
            // 
            this.changelog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.changelog.Font = new System.Drawing.Font( "Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.changelog.Location = new System.Drawing.Point( 12, 60 );
            this.changelog.Multiline = true;
            this.changelog.Name = "changelog";
            this.changelog.ReadOnly = true;
            this.changelog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.changelog.Size = new System.Drawing.Size( 476, 249 );
            this.changelog.TabIndex = 0;
            this.changelog.TabStop = false;
            // 
            // lHeader
            // 
            this.lHeader.AutoSize = true;
            this.lHeader.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.lHeader.Location = new System.Drawing.Point( 12, 9 );
            this.lHeader.Name = "lHeader";
            this.lHeader.Size = new System.Drawing.Size( 187, 13 );
            this.lHeader.TabIndex = 4;
            this.lHeader.Text = "An update to fCraft is available!";
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 388, 315 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 100, 23 );
            this.bCancel.TabIndex = 6;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.Click += new System.EventHandler( this.bCancel_Click );
            // 
            // bUpdateNow
            // 
            this.bUpdateNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bUpdateNow.Location = new System.Drawing.Point( 176, 315 );
            this.bUpdateNow.Name = "bUpdateNow";
            this.bUpdateNow.Size = new System.Drawing.Size( 100, 23 );
            this.bUpdateNow.TabIndex = 7;
            this.bUpdateNow.Text = "Restart Now";
            this.bUpdateNow.UseVisualStyleBackColor = true;
            this.bUpdateNow.Click += new System.EventHandler( this.bUpdateNow_Click );
            // 
            // bUpdateLater
            // 
            this.bUpdateLater.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bUpdateLater.Location = new System.Drawing.Point( 282, 315 );
            this.bUpdateLater.Name = "bUpdateLater";
            this.bUpdateLater.Size = new System.Drawing.Size( 100, 23 );
            this.bUpdateLater.TabIndex = 8;
            this.bUpdateLater.Text = "Update Later";
            this.bUpdateLater.UseVisualStyleBackColor = true;
            // 
            // progress
            // 
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.Location = new System.Drawing.Point( 388, 12 );
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size( 100, 23 );
            this.progress.TabIndex = 1;
            // 
            // lProgress
            // 
            this.lProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lProgress.AutoSize = true;
            this.lProgress.Location = new System.Drawing.Point( 385, 38 );
            this.lProgress.Name = "lProgress";
            this.lProgress.Size = new System.Drawing.Size( 100, 13 );
            this.lProgress.TabIndex = 5;
            this.lProgress.Text = "Downloading ({0}%)";
            // 
            // lVersion
            // 
            this.lVersion.AutoSize = true;
            this.lVersion.Location = new System.Drawing.Point( 12, 25 );
            this.lVersion.Name = "lVersion";
            this.lVersion.Size = new System.Drawing.Size( 266, 26 );
            this.lVersion.TabIndex = 9;
            this.lVersion.Text = "Currently installed version: {0}\r\nNewest available version: {1} (released {2:0} d" +
                "ays ago)";
            // 
            // UpdateWindow
            // 
            this.AcceptButton = this.bUpdateNow;
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size( 500, 350 );
            this.Controls.Add( this.lVersion );
            this.Controls.Add( this.bUpdateLater );
            this.Controls.Add( this.bUpdateNow );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.lProgress );
            this.Controls.Add( this.lHeader );
            this.Controls.Add( this.progress );
            this.Controls.Add( this.changelog );
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateWindow";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "fCraft Updater";
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox changelog;
        private System.Windows.Forms.Label lHeader;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bUpdateNow;
        private System.Windows.Forms.Button bUpdateLater;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Label lProgress;
        private System.Windows.Forms.Label lVersion;
    }
}