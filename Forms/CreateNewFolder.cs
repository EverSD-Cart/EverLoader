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
using VB = Microsoft.VisualBasic.FileIO.FileSystem;

namespace EverLoader.Forms
{
    public partial class CreateNewFolder : Form
    {
        private string _sdDriveRoot;

        public CreateNewFolder(string sdDrive)
        {
            _sdDriveRoot = Path.GetPathRoot(sdDrive);
            InitializeComponent();
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

            int scriptExitCode = 0;
            string scriptError = null;
            await Task.Run(() =>
            {
                //create temp dir for scripts etc
                var tempDir = $"{Path.GetTempPath()}everfolders";
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); //clean up temp folder from previous time
                Directory.CreateDirectory(tempDir);

                //copy png to temp and rename
                var pngSourcePath = this.tbPictureFile.Text;
                var pngTargetPath = Path.Combine(tempDir, $"{newSDFolder}.png");
                File.Copy(pngSourcePath, pngTargetPath, overwrite:true);

                //extract scripts
                using (var memStream = new MemoryStream(Properties.Resources.Build_EverSD_Folders))
                using (var archive = new ZipArchive(memStream))
                {
                    archive.ExtractToDirectory(tempDir, overwriteFiles: true);
                }

                string buildFoldersCmdFile = Path.Combine(tempDir, "Build Folders.cmd");
                string makeWallpapersCmdFile = Path.Combine(tempDir, "Make Wallpapers.cmd");

                //remove all 'pause' statements from the command scripts, or else process.WaitForExit() will wait forever
                File.WriteAllLines(buildFoldersCmdFile, File.ReadAllLines(buildFoldersCmdFile).Where(l => l != "pause"));
                File.WriteAllLines(makeWallpapersCmdFile, File.ReadAllLines(makeWallpapersCmdFile).Where(l => l != "pause"));

                //execute 'Build Folders.cmd' script
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
                scriptError = process.StandardError.ReadToEnd();
                process.WaitForExit();
                scriptExitCode = process.ExitCode;
                process.Close();
                if (scriptExitCode != 0) return;

                if (!string.IsNullOrEmpty(this.tbWallpaperFile.Text))
                {
                    var wallpaperTempDir = Path.Combine(tempDir, "wallpaper");
                    Directory.CreateDirectory(wallpaperTempDir);
                    pngTargetPath = Path.Combine(wallpaperTempDir, $"{newSDFolder}.png");
                    File.Copy(this.tbWallpaperFile.Text, pngTargetPath, overwrite: true);

                    //execute 'Make Wallpapers.cmd' script
                    process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = makeWallpapersCmdFile,
                            Arguments = $"\"{pngTargetPath}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardError = true
                        }
                    };
                    process.Start();
                    scriptError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    scriptExitCode = process.ExitCode;
                    process.Close();
                    if (scriptExitCode != 0) return;
                }

                //copy folders to SD drive
                VB.CopyDirectory(Path.Combine(tempDir, "folders"), Path.Combine(_sdDriveRoot, "folders"), overwrite:true);
                VB.CopyDirectory(Path.Combine(tempDir, "game"), Path.Combine(_sdDriveRoot, "game"), overwrite: true);
                VB.CopyDirectory(Path.Combine(tempDir, "retroarch"), Path.Combine(_sdDriveRoot, "retroarch"), overwrite: true);
                VB.CopyDirectory(Path.Combine(tempDir, "special"), Path.Combine(_sdDriveRoot, "special"), overwrite: true);
                if (!File.Exists(Path.Combine(_sdDriveRoot, "cartridge.json"))) File.Copy(Path.Combine(tempDir, "cartridge.json"), Path.Combine(_sdDriveRoot, "cartridge.json"));
            });

            //enable form
            this.UseWaitCursor = false;
            this.Enabled = true;

            if (scriptExitCode != 0)
            {
                MessageBox.Show($"Problem found when running script to create folder.\n" +
                    $"Message: \"{scriptError?.Trim()}\".",
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
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
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

        private void btnBrowseWallpaper_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.tbWallpaperFile.Text = this.openFileDialog1.FileName;
            }
        }
    }
}
