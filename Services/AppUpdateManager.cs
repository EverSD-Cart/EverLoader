using EverLoader.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EverLoader.Services
{
    public class AppUpdateManager
    {
        private readonly AppSettings _appSettings;

        public AppUpdateManager(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public async Task CheckForUpdate(Form parent)
        {
            GitHubRelease[] releases = null;
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "EverLoader");
                    var respString = wc.DownloadString(new Uri(_appSettings.ReleasesEndpoint));
                    releases = JsonConvert.DeserializeObject<GitHubRelease[]>(respString);
                }
            }
            catch
            {
                /* either call to github or deserializing response failed */
            }

            //check if valid release exists (non-draft and has asset ending on "-portable.zip")
            var latestRelease = releases?
                .FirstOrDefault(p => !p.draft && p.assets
                    .Any(a => a.name.ToLower().StartsWith("everloader") && a.name.ToLower().EndsWith("-portable.zip")));
            if (latestRelease == null)
            {
                MessageBox.Show("No valid EverLoader releases found.", "No updates found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentVersion = Version.Parse(Application.ProductVersion);
            if (!Version.TryParse(latestRelease.tag_name, out Version latestVersion) || currentVersion >= latestVersion)
            {
                MessageBox.Show($"You are already running the latest EverLoader version {currentVersion} !", "Update check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"There is a newer {latestVersion} version of EverLoader available.\n" +
                $"Do you want to download and upgrade now?\n" +
                $"\nRelease notes:\n{latestRelease.body}"
                , "New version available", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string tempFolder = $"{Path.GetTempPath()}everloader";
                var downloadUrl = latestRelease.assets.First(a => a.name.ToLower().StartsWith("everloader") && a.name.ToLower().EndsWith("-portable.zip")).browser_download_url;

                try
                {
                    using (var progressForm = new ProgressForm(parent, "Download, install and restart...", ProgressBarStyle.Marquee))
                    {
                        //download and extract
                        using (var client = new HttpClient())
                        using (var response = await client.GetAsync(downloadUrl))
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var archive = new ZipArchive(responseStream))
                        {
                            Directory.CreateDirectory(tempFolder);
                            archive.ExtractToDirectory(tempFolder, overwriteFiles: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error found during download/extract:\n{ex.Message}", "Error found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //copy & start new EverLoader.exe and exit this app
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/C timeout 3 > NUL & copy \"{tempFolder}\\EverLoader.exe\" EverLoader.exe /y /b & EverLoader.exe");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process.Start(psi);
                //Environment.Exit(0); //hard close
                Application.Exit();
            }
        }
    }
}