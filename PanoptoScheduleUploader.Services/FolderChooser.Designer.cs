namespace PanoptoScheduleUploader.Services
{
    partial class FolderChooser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.selectButton = new System.Windows.Forms.Button();
            this.topLabel = new System.Windows.Forms.Label();
            this.bottomLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // selectButton
            // 
            this.selectButton.Location = new System.Drawing.Point(263, 202);
            this.selectButton.Name = "selectButton";
            this.selectButton.Size = new System.Drawing.Size(75, 23);
            this.selectButton.TabIndex = 0;
            this.selectButton.Text = "Select";
            this.selectButton.UseVisualStyleBackColor = true;
            this.selectButton.Click += new System.EventHandler(this.selectButton_Click);
            // 
            // topLabel
            // 
            this.topLabel.AutoSize = true;
            this.topLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.topLabel.Location = new System.Drawing.Point(42, 10);
            this.topLabel.Name = "topLabel";
            this.topLabel.Size = new System.Drawing.Size(500, 17);
            this.topLabel.TabIndex = 1;
            this.topLabel.Text = "There are multiple folders of this name. Please choose which one to record to:";
            // 
            // bottomLabel
            // 
            this.bottomLabel.AutoSize = true;
            this.bottomLabel.Location = new System.Drawing.Point(95, 240);
            this.bottomLabel.Name = "bottomLabel";
            this.bottomLabel.Size = new System.Drawing.Size(406, 13);
            this.bottomLabel.TabIndex = 2;
            this.bottomLabel.Text = "To avoid this message in the future, consider using the folder ID instead of the " +
    "name.";
            // 
            // FolderChooser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 262);
            this.Controls.Add(this.bottomLabel);
            this.Controls.Add(this.topLabel);
            this.Controls.Add(this.selectButton);
            this.Name = "FolderChooser";
            this.Text = "Choose a Folder";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button selectButton;
        private System.Windows.Forms.Label topLabel;
        private System.Windows.Forms.Label bottomLabel;
    }
}