namespace fCraft.ConfigGUI {
    partial class PropertyGridPopup {
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
            this.pgProperties = new System.Windows.Forms.PropertyGrid();
            this.bSave = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // pgProperties
            // 
            this.pgProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pgProperties.Location = new System.Drawing.Point( 12, 12 );
            this.pgProperties.Name = "pgProperties";
            this.pgProperties.Size = new System.Drawing.Size( 260, 289 );
            this.pgProperties.TabIndex = 0;
            // 
            // bSave
            // 
            this.bSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bSave.Location = new System.Drawing.Point( 116, 307 );
            this.bSave.Name = "bSave";
            this.bSave.Size = new System.Drawing.Size( 75, 23 );
            this.bSave.TabIndex = 1;
            this.bSave.Text = "Save";
            this.bSave.UseVisualStyleBackColor = true;
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point( 197, 307 );
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size( 75, 23 );
            this.bCancel.TabIndex = 2;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // PropertyGridPopup
            // 
            this.AcceptButton = this.bSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size( 284, 342 );
            this.Controls.Add( this.bCancel );
            this.Controls.Add( this.bSave );
            this.Controls.Add( this.pgProperties );
            this.Name = "PropertyGridPopup";
            this.Text = "PropertyGridPopup";
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.PropertyGrid pgProperties;
        private System.Windows.Forms.Button bSave;
        private System.Windows.Forms.Button bCancel;
    }
}