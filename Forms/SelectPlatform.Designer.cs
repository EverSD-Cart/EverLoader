
namespace EverLoader.Forms
{
    partial class SelectPlatform
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
            this.label1 = new System.Windows.Forms.Label();
            this.cbPossiblePlatforms = new System.Windows.Forms.ComboBox();
            this.btnSelectPlatformOK = new System.Windows.Forms.Button();
            this.cbUseAsDefault = new System.Windows.Forms.CheckBox();
            this.lblGame = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblExtension = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(187, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Could not autodetect platform for";
            // 
            // cbPossiblePlatforms
            // 
            this.cbPossiblePlatforms.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPossiblePlatforms.FormattingEnabled = true;
            this.cbPossiblePlatforms.Location = new System.Drawing.Point(206, 35);
            this.cbPossiblePlatforms.Name = "cbPossiblePlatforms";
            this.cbPossiblePlatforms.Size = new System.Drawing.Size(211, 23);
            this.cbPossiblePlatforms.TabIndex = 1;
            // 
            // btnSelectPlatformOK
            // 
            this.btnSelectPlatformOK.Location = new System.Drawing.Point(342, 64);
            this.btnSelectPlatformOK.Name = "btnSelectPlatformOK";
            this.btnSelectPlatformOK.Size = new System.Drawing.Size(75, 23);
            this.btnSelectPlatformOK.TabIndex = 2;
            this.btnSelectPlatformOK.Text = "OK";
            this.btnSelectPlatformOK.UseVisualStyleBackColor = true;
            this.btnSelectPlatformOK.Click += new System.EventHandler(this.btnSelectPlatformOK_Click);
            // 
            // cbUseAsDefault
            // 
            this.cbUseAsDefault.AutoSize = true;
            this.cbUseAsDefault.Location = new System.Drawing.Point(13, 64);
            this.cbUseAsDefault.Name = "cbUseAsDefault";
            this.cbUseAsDefault.Size = new System.Drawing.Size(215, 19);
            this.cbUseAsDefault.TabIndex = 3;
            this.cbUseAsDefault.Text = "Do this for all ROMs with extionsion";
            this.cbUseAsDefault.UseVisualStyleBackColor = true;
            // 
            // lblGame
            // 
            this.lblGame.AutoSize = true;
            this.lblGame.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblGame.Location = new System.Drawing.Point(206, 13);
            this.lblGame.Name = "lblGame";
            this.lblGame.Size = new System.Drawing.Size(53, 15);
            this.lblGame.TabIndex = 4;
            this.lblGame.Text = "lblGame";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "Please select platform:";
            // 
            // lblExtension
            // 
            this.lblExtension.AutoSize = true;
            this.lblExtension.Location = new System.Drawing.Point(224, 65);
            this.lblExtension.Name = "lblExtension";
            this.lblExtension.Size = new System.Drawing.Size(71, 15);
            this.lblExtension.TabIndex = 6;
            this.lblExtension.Text = "lblExtension";
            // 
            // SelectPlatform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 93);
            this.ControlBox = false;
            this.Controls.Add(this.lblExtension);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblGame);
            this.Controls.Add(this.cbUseAsDefault);
            this.Controls.Add(this.btnSelectPlatformOK);
            this.Controls.Add(this.cbPossiblePlatforms);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectPlatform";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Game Platform";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbPossiblePlatforms;
        private System.Windows.Forms.Button btnSelectPlatformOK;
        private System.Windows.Forms.CheckBox cbUseAsDefault;
        private System.Windows.Forms.Label lblGame;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblExtension;
    }
}