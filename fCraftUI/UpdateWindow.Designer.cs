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
            this.progress = new System.Windows.Forms.ProgressBar();
            this.bApply = new System.Windows.Forms.Button();
            this.title = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // changelog
            // 
            this.changelog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.changelog.Font = new System.Drawing.Font( "Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.changelog.Location = new System.Drawing.Point( 12, 34 );
            this.changelog.Multiline = true;
            this.changelog.Name = "changelog";
            this.changelog.ReadOnly = true;
            this.changelog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.changelog.Size = new System.Drawing.Size( 559, 287 );
            this.changelog.TabIndex = 0;
            this.changelog.TabStop = false;
            // 
            // progress
            // 
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.Location = new System.Drawing.Point( 12, 327 );
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size( 443, 23 );
            this.progress.TabIndex = 1;
            // 
            // bApply
            // 
            this.bApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bApply.Enabled = false;
            this.bApply.Location = new System.Drawing.Point( 461, 327 );
            this.bApply.Name = "bApply";
            this.bApply.Size = new System.Drawing.Size( 111, 23 );
            this.bApply.TabIndex = 2;
            this.bApply.Text = "Downloading";
            this.bApply.UseVisualStyleBackColor = true;
            this.bApply.Click += new System.EventHandler( this.bApply_Click );
            // 
            // title
            // 
            this.title.AutoSize = true;
            this.title.Font = new System.Drawing.Font( "Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.title.Location = new System.Drawing.Point( 12, 9 );
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size( 159, 13 );
            this.title.TabIndex = 4;
            this.title.Text = "A new version is available.";
            // 
            // UpdateWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 584, 362 );
            this.Controls.Add( this.title );
            this.Controls.Add( this.bApply );
            this.Controls.Add( this.progress );
            this.Controls.Add( this.changelog );
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateWindow";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "fCraft Updater";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.UpdateWindow_FormClosed );
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox changelog;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Button bApply;
        private System.Windows.Forms.Label title;
    }
}