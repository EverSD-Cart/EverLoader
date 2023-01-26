using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using EverLoader.Models;
using System.IO;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;

namespace EverLoader.Forms
{
    public partial class CreateNewFolder : Form
    {
        private string _sdDriveRoot;

        public CreateNewFolder(string sdDrive)
        {
            _sdDriveRoot = Path.GetPathRoot(sdDrive);
            InitializeComponent();
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        public string JustCreatedFolderName { get; set; }

        private async void btnOK_Click(object sender, EventArgs e)
        {
            var newSDFolder = Path.GetInvalidFileNameChars().Aggregate(tbFolderName.Text, (f, c) => f.Replace(c, ' ')).Trim();
            if (string.IsNullOrEmpty(newSDFolder))
            {
                MessageBox.Show("Please enter a valid folder name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //disable form
            Cursor.Current = Cursors.WaitCursor;
            this.UseWaitCursor = true;
            Application.DoEvents();

            int mkdosfsExitCode = 0;
            string mkdosfsError = null;
            
            await Task.Run(() =>
            {
                //create temp dir for scripts etc
                var tempDir = $"{Path.GetTempPath()}everfolders";
                Directory.CreateDirectory(tempDir);

                //copy png to temp and rename
                var pngSourcePath = this.tbPictureFile.Text;
                var pngTargetPath = Path.Combine(tempDir, $"{newSDFolder}.png");
                string buildFoldersCmdFile = Path.Combine(tempDir, "Build Folders.cmd");
                File.Copy(pngSourcePath, pngTargetPath, overwrite:true);

                //extract scripts
                using (var memStream = new MemoryStream(Properties.Resources.Build_EverSD_Folders))
                using (var archive = new ZipArchive(memStream))
                {
                    archive.ExtractToDirectory(tempDir, overwriteFiles: true);
                }

                //remove all 'pause' statements from '"Build Folders.cmd' script, or WaitForExit will hang
                File.WriteAllLines(buildFoldersCmdFile, File.ReadAllLines(buildFoldersCmdFile).Where(l => l != "pause"));

                //execute scripts
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = buildFoldersCmdFile,
                        Arguments = $"\"{pngTargetPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true
                    }
                };
                process.Start();
                mkdosfsError = process.StandardError.ReadToEnd();
                process.WaitForExit();
                mkdosfsExitCode = process.ExitCode;
                if (mkdosfsExitCode == 0)
                {
                    //copy folders to SD drive
                    CopyFilesRecursively(Path.Combine(tempDir, "folders"), Path.Combine(_sdDriveRoot, "folders"));
                    CopyFilesRecursively(Path.Combine(tempDir, "game"), Path.Combine(_sdDriveRoot, "game"));
                    CopyFilesRecursively(Path.Combine(tempDir, "retroarch"), Path.Combine(_sdDriveRoot, "retroarch"));
                    CopyFilesRecursively(Path.Combine(tempDir, "special"), Path.Combine(_sdDriveRoot, "special"));
                    if (!File.Exists(Path.Combine(_sdDriveRoot, "cartridge.json"))) File.Copy(Path.Combine(tempDir, "cartridge.json"), Path.Combine(_sdDriveRoot, "cartridge.json"));
                }

                //clean up temp folder 
                Directory.Delete(tempDir, true);
            });

            //enable form
            this.UseWaitCursor = false;
            this.Enabled = true;

            if (mkdosfsExitCode != 0)
            {
                MessageBox.Show($"Problem found when running script to create folder.\n" +
                    $"Message: \"{mkdosfsError?.Trim()}\".",
                    "Error found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                //preselect new folder and show succes message
                JustCreatedFolderName = newSDFolder.Replace(" ", ""); //TODO: ask Eric why his script allows no spaces in the game folder name

                MessageBox.Show($"Successfully created games folder \"{JustCreatedFolderName}\" on your MicroSD card!",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CreateNewFolder_Shown(object sender, EventArgs e)
        {
            this.tbFolderName.Focus();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = this.openFileDialog1.ShowDialog();
            // if a file is selected
            if (result == DialogResult.OK)
            {
                this.tbPictureFile.Text = this.openFileDialog1.FileName;
            }
        }

        private void tb_TextChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = tbPictureFile.Text.Length > 0 && tbFolderName.Text.Length > 0;
            btnOK.BackColor = btnOK.Enabled ? SystemColors.ActiveCaption : SystemColors.Control;
            btnOK.ForeColor = btnOK.Enabled ? SystemColors.ActiveCaptionText : SystemColors.ControlText;
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourcePath, targetPath, true);
        }
    }
}
