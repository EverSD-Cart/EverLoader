﻿using System;
using System.Windows.Forms;

namespace EverLoader
{
    public partial class ProgressForm : Form
    {
        private readonly Form _parent;

        public int ProgressBarValue
        {
            get => progressBar1.Value;
            set
            {
                if (progressBar1.Value == value) 
                    return;
                
                progressBar1.Value = value;
            }
        }

        public ProgressBarStyle ProgressBarStyle
        {
            get => progressBar1.Style;
            set => progressBar1.Style = value;
        }

        public ProgressForm()
        {
            InitializeComponent();

            MaximumSize = MinimumSize = Size; //no resizing

            progressBar1.Maximum = 100;
            progressBar1.Step = 1;
            progressBar1.Value = 0;

            Reporter = new Progress<(string action, int item, int total)>(p
                => UpdateProgress(p.action, p.item, p.total));
        }

        public ProgressForm(Form parent, string actionText = "Preparing...", ProgressBarStyle progressStyle = ProgressBarStyle.Blocks) : this()
        {
            lblAction.Text = actionText;
            progressBar1.Style = progressStyle;
            _parent = parent;
            _parent.Enabled = false;
            Show(_parent);
            CenterToParent();
            Application.DoEvents();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (_parent is not null)
            {
                _parent.Enabled = true;
                _parent.BringToFront();
            }

            if (disposing && components is not null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        public void Report(string description, int item, int total)
        {
            (Reporter as IProgress<(string, int, int)>).Report((description, item, total));
        }

        public Progress<(string description, int item, int total)> Reporter { get; private set; }

        public void UpdateProgress(string action, int itemNumber, int totalNumberOfItems)
        {
            lblAction.Text = action;
            if (progressBar1.Style == ProgressBarStyle.Blocks)
            {
                progressBar1.Value = 100 * itemNumber / totalNumberOfItems;
                progressBar1.Update();
            }
            Application.DoEvents();
        }
    }
}
