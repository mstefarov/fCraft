namespace ConfigTool {
    partial class TextEditorPopup {
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
            this.tRules = new System.Windows.Forms.TextBox();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.lWarning = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tRules
            // 
            this.tRules.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tRules.Font = new System.Drawing.Font( "Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.tRules.Location = new System.Drawing.Point( 13, 13 );
            this.tRules.Multiline = true;
            this.tRules.Name = "tRules";
            this.tRules.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tRules.Size = new System.Drawing.Size( 485, 212 );
            this.tRules.TabIndex = 0;
            this.tRules.WordWrap = false;
            this.tRules.KeyDown += new System.Windows.Forms.KeyEventHandler( this.tRules_KeyDown );
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bOK.Location = new System.Drawing.Point( 292, 231 );
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size( 100, 28 );
            this.bOK.TabIndex = 2;
            this.bOK.Text = "OK";
            this.bOK.Click += new System.EventHandler( this.bOK_Click );
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Font = new System.Drawing.Font( "Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
            this.bCancel.Location = new System.Drawing.Point( 398, 231 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 100, 28 );
            this.bCancel.TabIndex = 3;
            this.bCancel.Text = "Cancel";
            this.bCancel.Click += new System.EventHandler( this.bCancel_Click );
            // 
            // lWarning
            // 
            this.lWarning.AutoSize = true;
            this.lWarning.Location = new System.Drawing.Point( 12, 240 );
            this.lWarning.Name = "lWarning";
            this.lWarning.Size = new System.Drawing.Size( 261, 13 );
            this.lWarning.TabIndex = 4;
            this.lWarning.Text = "Warning: Lines over 64 characters long will be cut off.";
            // 
            // TextEditorPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 510, 271 );
            this.Controls.Add( this.lWarning );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bOK );
            this.Controls.Add( this.tRules );
            this.Name = "TextEditorPopup";
            this.Text = "TextEditorPopup";
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tRules;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Label lWarning;
    }
}