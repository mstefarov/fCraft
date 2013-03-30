namespace fCraft.GUI {
    sealed partial class AboutWindow {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutWindow));
            this.tCredits = new System.Windows.Forms.TextBox();
            this.lHeader = new System.Windows.Forms.Label();
            this.lSubheader = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tCredits
            // 
            this.tCredits.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tCredits.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tCredits.Location = new System.Drawing.Point(12, 76);
            this.tCredits.Multiline = true;
            this.tCredits.Name = "tCredits";
            this.tCredits.ReadOnly = true;
            this.tCredits.Size = new System.Drawing.Size(465, 392);
            this.tCredits.TabIndex = 0;
            this.tCredits.Text = resources.GetString("tCredits.Text");
            // 
            // lHeader
            // 
            this.lHeader.AutoSize = true;
            this.lHeader.Font = new System.Drawing.Font("Consolas", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.lHeader.Location = new System.Drawing.Point(12, 9);
            this.lHeader.Name = "lHeader";
            this.lHeader.Size = new System.Drawing.Size(180, 56);
            this.lHeader.TabIndex = 1;
            this.lHeader.Text = "fCraft";
            // 
            // lSubheader
            // 
            this.lSubheader.AutoSize = true;
            this.lSubheader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lSubheader.Location = new System.Drawing.Point(198, 13);
            this.lSubheader.Name = "lSubheader";
            this.lSubheader.Size = new System.Drawing.Size(289, 52);
            this.lSubheader.TabIndex = 2;
            this.lSubheader.Text = "Free/open-source Minecraft game server.\r\nVersion {0}\r\nDeveloped by Matvei Stefaro" +
    "v in 2009-2012\r\nFor news and documentation, visit www.fCraft.net";
            // 
            // AboutWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 480);
            this.Controls.Add(this.lSubheader);
            this.Controls.Add(this.lHeader);
            this.Controls.Add(this.tCredits);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 440);
            this.Name = "AboutWindow";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tCredits;
        private System.Windows.Forms.Label lHeader;
        private System.Windows.Forms.Label lSubheader;
    }
}