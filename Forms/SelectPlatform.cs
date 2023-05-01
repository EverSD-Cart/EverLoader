using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EverLoader.Models;

namespace EverLoader.Forms
{
    public partial class SelectPlatform : Form
    {
        public SelectPlatform(IList<Platform> possiblePlatforms, GameInfo game)
        {
            InitializeComponent();

            cbPossiblePlatforms.DisplayMember = nameof(Platform.GroupAndName);
            cbPossiblePlatforms.ValueMember = nameof(Platform.Id);
            cbPossiblePlatforms.DataSource = possiblePlatforms;

            lblGame.Text = game.OriginalRomFileName;
            //lblExtension.Text = $"\"{ Path.GetExtension(game.romFileName) }\"";
        }

        public Platform SelectedPlatform { get; private set; }
        public bool UseAsDefault { get; private set; }

        private void btnSelectPlatformOK_Click(object sender, EventArgs e)
        {
            SelectedPlatform = cbPossiblePlatforms.SelectedItem as Platform;
            //UseAsDefault = cbUseAsDefault.Checked;
            this.Close();
        }
    }
}
