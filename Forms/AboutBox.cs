using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace EverLoader.Forms
{
    partial class AboutBox : Form
    {
        private Keys[] _sss = new[] { Keys.Up, Keys.Up, Keys.Down, Keys.Down, Keys.Left, Keys.Right, Keys.Left, Keys.Right, Keys.B, Keys.A };
        private int _sssI = 0;
        public AboutBox()
        {
            InitializeComponent();

            lblInfo.Text = lblInfo.Text.Replace("{ProductVersion}", Application.ProductVersion);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //open EverSD website
            Process.Start(new ProcessStartInfo("https://eversd.com") { UseShellExecute = true });
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == _sss[_sssI])
            {
                if (++_sssI == _sss.Length)
                {
                    _sssI = 0;
                    Process.Start(new ProcessStartInfo(Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovL2xvZGVydW5uZXJ3ZWJnYW1lLmNvbS9nYW1l"))) { UseShellExecute = true });
                }
            }
            else
            {
                _sssI = 0;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
