
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
            label1 = new System.Windows.Forms.Label();
            cbPossiblePlatforms = new System.Windows.Forms.ComboBox();
            btnSelectPlatformOK = new System.Windows.Forms.Button();
            lblGame = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            btnSelectPlatformCancel = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(13, 13);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(187, 15);
            label1.TabIndex = 0;
            label1.Text = "Could not autodetect platform for";
            // 
            // cbPossiblePlatforms
            // 
            cbPossiblePlatforms.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbPossiblePlatforms.FormattingEnabled = true;
            cbPossiblePlatforms.Location = new System.Drawing.Point(206, 35);
            cbPossiblePlatforms.Name = "cbPossiblePlatforms";
            cbPossiblePlatforms.Size = new System.Drawing.Size(218, 23);
            cbPossiblePlatforms.TabIndex = 1;
            // 
            // btnSelectPlatformOK
            // 
            btnSelectPlatformOK.Location = new System.Drawing.Point(430, 35);
            btnSelectPlatformOK.Name = "btnSelectPlatformOK";
            btnSelectPlatformOK.Size = new System.Drawing.Size(75, 23);
            btnSelectPlatformOK.TabIndex = 2;
            btnSelectPlatformOK.Text = "OK";
            btnSelectPlatformOK.UseVisualStyleBackColor = true;
            btnSelectPlatformOK.Click += btnSelectPlatformOK_Click;
            // 
            // lblGame
            // 
            lblGame.AutoSize = true;
            lblGame.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            lblGame.Location = new System.Drawing.Point(206, 13);
            lblGame.Name = "lblGame";
            lblGame.Size = new System.Drawing.Size(53, 15);
            lblGame.TabIndex = 4;
            lblGame.Text = "lblGame";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(13, 38);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(125, 15);
            label2.TabIndex = 5;
            label2.Text = "Please select platform:";
            // 
            // btnSelectPlatformCancel
            // 
            btnSelectPlatformCancel.Location = new System.Drawing.Point(430, 64);
            btnSelectPlatformCancel.Name = "btnSelectPlatformCancel";
            btnSelectPlatformCancel.Size = new System.Drawing.Size(75, 23);
            btnSelectPlatformCancel.TabIndex = 6;
            btnSelectPlatformCancel.Text = "Cancel";
            btnSelectPlatformCancel.UseVisualStyleBackColor = true;
            btnSelectPlatformCancel.Click += btnSelectPlatformCancel_Click;
            // 
            // SelectPlatform
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(512, 96);
            ControlBox = false;
            Controls.Add(btnSelectPlatformCancel);
            Controls.Add(label2);
            Controls.Add(lblGame);
            Controls.Add(btnSelectPlatformOK);
            Controls.Add(cbPossiblePlatforms);
            Controls.Add(label1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SelectPlatform";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Select Game Platform";
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbPossiblePlatforms;
        private System.Windows.Forms.Button btnSelectPlatformOK;
        private System.Windows.Forms.Label lblGame;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnSelectPlatformCancel;
    }
}