using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace EverLoader.Forms
{
    partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();

            lblInfo.Text = lblInfo.Text
                .Replace("{ProductVersion}", Application.ProductVersion);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //open EverSD website
            Process.Start(new ProcessStartInfo("https://eversd.com") { UseShellExecute = true });
        }
    }
}
