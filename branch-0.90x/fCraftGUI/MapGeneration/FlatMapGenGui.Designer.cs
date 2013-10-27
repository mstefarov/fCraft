namespace fCraft.GUI {
    partial class FlatMapGenGui {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.pgDetails = new System.Windows.Forms.PropertyGrid();
            this.lInstructions = new System.Windows.Forms.Label();
            this.xCustom = new System.Windows.Forms.CheckBox();
            this.lPreset = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // pgDetails
            // 
            this.pgDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgDetails.Location = new System.Drawing.Point(3, 26);
            this.pgDetails.Name = "pgDetails";
            this.pgDetails.Size = new System.Drawing.Size(384, 266);
            this.pgDetails.TabIndex = 1;
            this.pgDetails.ToolbarVisible = false;
            this.pgDetails.Visible = false;
            this.pgDetails.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.pgDetails_PropertyValueChanged);
            // 
            // lInstructions
            // 
            this.lInstructions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lInstructions.AutoSize = true;
            this.lInstructions.Location = new System.Drawing.Point(5, 299);
            this.lInstructions.Name = "lInstructions";
            this.lInstructions.Size = new System.Drawing.Size(229, 26);
            this.lInstructions.TabIndex = 0;
            this.lInstructions.Text = "Select one of the presets from the list below,\r\nor check \"Custom settings\" for de" +
    "tailed control.";
            this.lInstructions.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // xCustom
            // 
            this.xCustom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.xCustom.AutoSize = true;
            this.xCustom.Location = new System.Drawing.Point(287, 308);
            this.xCustom.Name = "xCustom";
            this.xCustom.Size = new System.Drawing.Size(100, 17);
            this.xCustom.TabIndex = 2;
            this.xCustom.Text = "Custom settings";
            this.xCustom.UseVisualStyleBackColor = true;
            this.xCustom.CheckedChanged += new System.EventHandler(this.xCustom_CheckedChanged);
            // 
            // lPreset
            // 
            this.lPreset.AutoSize = true;
            this.lPreset.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lPreset.Location = new System.Drawing.Point(5, 6);
            this.lPreset.Name = "lPreset";
            this.lPreset.Size = new System.Drawing.Size(112, 13);
            this.lPreset.TabIndex = 3;
            this.lPreset.Text = "Current preset: {0}";
            this.lPreset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FlatMapGenGui
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lPreset);
            this.Controls.Add(this.xCustom);
            this.Controls.Add(this.lInstructions);
            this.Controls.Add(this.pgDetails);
            this.Name = "FlatMapGenGui";
            this.Size = new System.Drawing.Size(390, 332);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid pgDetails;
        private System.Windows.Forms.Label lInstructions;
        private System.Windows.Forms.CheckBox xCustom;
        private System.Windows.Forms.Label lPreset;
    }
}
