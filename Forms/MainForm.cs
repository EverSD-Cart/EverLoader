using EverLoader.Enums;
using EverLoader.Extensions;
using EverLoader.Helpers;
using EverLoader.Models;
using EverLoader.Services;
using Force.Crc32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheGamesDBApiWrapper.Models.Enums;
using TheGamesDBApiWrapper.Models.Responses.Games;

namespace EverLoader
{
    public partial class MainForm : Form
    {
        private readonly GamesManager _gamesManager;
        private readonly ScrapeManager _scrapeManager;
        private readonly RomManager _romManager;
        private readonly ImageManager _imageManager;
        private readonly AppUpdateManager _appUpdateManager;
        private readonly AppSettings _appSettings;

        public string SDDrive { get; set; }
        private Keys[] _konamiCode = new [] { Keys.Up, Keys.Up, Keys.Down, Keys.Down, Keys.Left, Keys.Right, Keys.Left, Keys.Right, Keys.B, Keys.A };
        private int _konamiIndex = 0;

        public MainForm(GamesManager gamesManager, 
            ScrapeManager scrapeManager,
            RomManager romManager,
            ImageManager imageManager,
            AppUpdateManager appUpdateManager,
            AppSettings appSettings)
        {
            _gamesManager = gamesManager;
            _scrapeManager = scrapeManager;
            _romManager = romManager;
            _imageManager = imageManager;
            _appUpdateManager = appUpdateManager;
            _appSettings = appSettings;

            InitializeComponent();

            // some more UI settings
            this.MinimumSize = this.Size; //startup size is the minimum size
            this.CenterToScreen();

            lvGames.Groups.Add(new ListViewGroup("ROMs", HorizontalAlignment.Center)); //group 0
            lvGames.Groups.Add(new ListViewGroup("Added Just Now", HorizontalAlignment.Center)); //group 1

            //Visual Studio Forms Editor keeps setting these TabStops to true, so manually set to false again
            rbInternalCore.TabStop = false;
            lblMissingBiosFiles.TabStop = false;
            llBannerNext.TabStop = false;
            llBannerPrev.TabStop = false;
            llBannerUp.TabStop = false;
            llBannerDown.TabStop = false;

            PopulateComboboxes();
        }

        /// <summary>
        /// Populates the platform dropdown boxes
        /// </summary>
        private void PopulateComboboxes()
        {
            //platform combobox
            cbPlatform.DisplayMember = "Text";
            cbPlatform.ValueMember = "Value";
            cbPlatform.GroupMember = "Group";
            var items = _appSettings.Platforms.OrderBy(p => p.Name)
                .Select(p => new { Text = p.Name, Value = p.Id, Group = p.Group ?? "Other" }).ToArray();
            cbPlatform.SortComparer = Comparer<string>.Create((x, y) =>
            {
                var xPlatform = _appSettings.Platforms.FirstOrDefault(p => p.Name == x);
                var yPlatform = _appSettings.Platforms.FirstOrDefault(p => p.Name == y);
                if (xPlatform != null && yPlatform != null 
                    && xPlatform.Group == yPlatform.Group 
                    && xPlatform.GroupItemSortOrder * yPlatform.GroupItemSortOrder != 0)
                {
                    return Comparer<int>.Default.Compare(xPlatform.GroupItemSortOrder, yPlatform.GroupItemSortOrder);
                }
                return Comparer<string>.Default.Compare(x == "Other" ? "ZZZ" : x, y == "Other" ? "ZZZ" : y);
            });
            cbPlatform.DataSource = items;
            cbPlatform.SelectedItem = null;

            cbRomFilter.DisplayMember = "Description";
            cbRomFilter.ValueMember = "Value";
            cbRomFilter.DataSource = Enum.GetValues(typeof(RomListFilter))
                .Cast<Enum>()
                .Select(value => new
                {
                    (Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute).Description,
                    value
                })
                .OrderBy(item => item.value)
                .ToList();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private async void MainForm_Shown(object sender, System.EventArgs e)
        {
            //create GamesManager and read local database
            using (var progressForm = new ProgressForm(this))
            {
                _gamesManager.ReadGames(progressForm.Reporter);
            }

            UpdateTotalSelectedGamesLabel();

            //after games read, select the 'all ROMs' filter, which will add all games to listview
            cbRomFilter_SelectedIndexChanged(null, null);
            cbRomFilter.Enabled = true;
            cbRomFilter.SelectedIndexChanged += new EventHandler(this.cbRomFilter_SelectedIndexChanged);

            lvGames.ItemChecked += new ItemCheckedEventHandler(lvGames_ItemChecked);

            if (_gamesManager.GamesDictionary.Count == 0)
            {
                toolTip1.Show("Welcome to EverLoader; the easiest way to sync your ROMs to the EverSD cart!\n\n" +
                    "It looks like your ROMs collection is still empty...\n\n" +
                    "Click the 'Add New ROMs' button below to import your ROMs.", lvGames);
            }

            //run stuff in the background
            await Task.Run(async () =>
            {
                try
                {
                    await _romManager.Init();
                }
                catch (Exception) { /* ignore for now */ }
            });
        }

        private GameInfo _game; //currently displayed game
        //private Platform _platform; //current game's platform

        private void lvGames_SelectionChanged(object sender, EventArgs e)
        {
            // this will only fire once only
            IdleHandlerSet = false;
            Application.Idle -= lvGames_SelectionChanged;

            if (lvGames.SelectedItems.Count > 0)
            {
                if (lvGames.SelectedItems[0].Name == _game?.Id) return;

                if (_game != null)
                {
                    _game.GameInfoChanged -= Game_GameInfoChanged;
                }

                //show game data
                _game = _gamesManager.GetGameById(lvGames.SelectedItems[0].Name);

                if (_game != null)
                {
                    SetDataBindings(); //first set bindings, then add change event
                    _game.GameInfoChanged += Game_GameInfoChanged;
                    return;
                }
            }

            //when unselected, or game not found, clear bindings
            ClearDataBindings();
        }

        private void ClearDataBindings()
        {
            if (_game != null)
            {
                _game.GameInfoChanged -= Game_GameInfoChanged;
            }
            //bind with empty game object to clear the form fields
            _game = null;
            //_platform = null;
            SetDataBindings(); //this clears form and disables controls
        }

        private void SetDataBindings(bool refreshOnly = false)
        {
            // Game Info
            lblGameId.Text = _game?.Id;
            lblGameCRC.Text = _game?.romCRC32;
            tbTitle.DataBindings.AddSingle("Text", _game, nameof(_game.romTitle));
            cbPlatform.DataBindings.AddSingle("SelectedValue", _game, nameof(_game.romPlatformId));
            cbGenre.DataBindings.AddSingle("Text", _game, nameof(_game.romGenre));
            tbReleaseDate.DataBindings.AddSingle("Text", _game, nameof(_game.romReleaseDate));
            cbMaxPlayers.DataBindings.AddSingle("Text", _game, nameof(_game.romPlayers));
            tbDescription.DataBindings.AddSingle("Text", _game, nameof(_game.romDescription));

            // Box art
            LoadBoxArt();

            // Button Mapping
            tbMappingA.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.a));
            tbMappingB.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.b));
            tbMappingX.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.x));
            tbMappingY.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.y));
            tbMappingSELECT.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.select));
            tbMappingSTART.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.start));
            tbMappingDPAD.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.dpad));
            tbMappingL1.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.l1));
            tbMappingL2.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.l2));
            tbMappingR1.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.r1));
            tbMappingR2.DataBindings.AddSingle("Text", _game?.romMapping, nameof(_game.romMapping.r2));

            if (!refreshOnly) //refresh is done after clicking item in scrape results, or when shifting banner up/down
            {
                // Emulator Settings
                var platform = _gamesManager.GetGamePlatform(_game);
#if RA_SUPPORTED
                rbInternalCore.Enabled = platform?.BlastRetroCore != null;
                rbRetroArchCore.Enabled = cbRetroArchCore.Enabled = platform?.RetroArchCores.Length > 0;

                cbRetroArchCore.DataSource = platform?.RetroArchCores.Length > 0
                    ? platform.RetroArchCores.Select(c => new ComboboxItem(c.DisplayName, c.CoreFileName)).ToArray()
                    : null;
                cbRetroArchCore.DataBindings.DefaultDataSourceUpdateMode = DataSourceUpdateMode.Never;
                cbRetroArchCore.DataBindings.AddSingle("SelectedValue", _game, nameof(_game.RetroArchCore));
#else
                rbInternalCore.Enabled = _game != null;
#endif

                if (_game == null)
                {
                    rbInternalCore.Checked = rbRetroArchCore.Checked = false;
                }
                else
                {
#if RA_SUPPORTED
                    rbInternalCore.Checked = _game?.RetroArchCore == null;
                    rbRetroArchCore.Checked = _game?.RetroArchCore != null;
#else
                    rbInternalCore.Checked = true;
#endif
                }

                UpdateMissingBiosFilesLabel();

                // Scraping
                tbScrapeName.Text = _gamesManager.GetCleanedTitle(_game?.romTitle);
                tbScrapeName.Enabled = btnScrape.Enabled = _game != null;

                UpdateScrapeGroupText();

                lbScrapeResults.Items.Clear();
                lbScrapeResults.Tag = null;
                lbScrapeResults.Enabled = false;

                llBannerNext.Visible = llBannerPrev.Visible = false;

                pbBanner.Tag = _game?.ImageBannerVerticalOffset ?? 0;

                llBannerDown.Enabled = llBannerUp.Enabled = _game != null;
            }
        }

        private void UpdateScrapeGroupText()
        {
            gbScrapeByName.Text = "Scrape by game title" + (_game != null ? $" (filter: {_game.romPlatform})" : null);
        }

        private void ComboBox_SelectionChangeCommitted(object sender, System.EventArgs e)
        {
            ((ComboBox)sender).DataBindings["SelectedValue"].WriteValue();

            //if platform changed, then update all bindings, as core-selection has changed, also header of 'Scrape by game title' group 
            if (Equals(sender, cbPlatform))
            {
                SetDataBindings();
            }
        }

        /// <summary>
        /// handle display of "missing BIOS files" linklabel
        /// </summary>
        private void UpdateMissingBiosFilesLabel()
        {
            var missingBiosFiles = GetMissingRomFiles();
            lblMissingBiosFiles.Visible = missingBiosFiles.Length > 0;
            var platform = _gamesManager.GetGamePlatform(_game);
            if (platform != null)
            {
                toolTip1.SetToolTip(lblMissingBiosFiles, $"The '{platform.Name}' emulator\nrequires these missing BIOS files:\n - {string.Join("\n - ", missingBiosFiles) }");
            }
        }

        private string[] GetMissingRomFiles()
        {
            var platform = _gamesManager.GetGamePlatform(_game);
            if (platform?.BiosFiles != null)
            {
                List<string> missingBiosFiles = new List<string>();
                foreach (var biosFile in platform.BiosFiles)
                {
                    if (!File.Exists($"{Constants.APP_ROOT_FOLDER}bios\\{platform.Alias}\\{biosFile}"))
                    {
                        missingBiosFiles.Add(biosFile);
                    }
                }
                return missingBiosFiles.ToArray();
            }
            return new string[0];
        }

        private void LoadBoxArt()
        {
            if (_game?.Image != null) pbBoxArtSmall.ImageLocation = _game.Image; else pbBoxArtSmall.Image = Properties.Resources.NoBoxArtMedium;
            if (_game?.ImageHD != null) pbBoxArtMedium.ImageLocation = _game.ImageHD; else pbBoxArtMedium.Image = Properties.Resources.NoBoxArtMedium;
            if (_game?.Image1080 != null) pbBoxArtLarge.ImageLocation = _game.Image1080; else pbBoxArtLarge.Image = Properties.Resources.NoBoxArtLarge;
            if (_game?.ImageBanner != null) pbBanner.ImageLocation = _game.ImageBanner; else pbBanner.Image = Properties.Resources.NoBannerArt;

            btnClearSmall.Enabled = _game?.Image != null;
            btnClearMedium.Enabled = _game?.ImageHD != null;
            btnClearLarge.Enabled = _game?.Image1080 != null;
            btnClearBanner.Enabled = _game?.ImageBanner != null;
        }

        private async void Game_GameInfoChanged(object sender, EventArgs e)
        {
            if (_game != null)
            {
                //save the updated game info (this could also change romPlatform)
                await _gamesManager.SerializeGame(_game);

                if (e is TitleChangedEventArgs)
                {
                    //game name could have been changed
                    lvGames.SelectedItems[0].Text = _gamesManager.GetRomListTitle(_game);
                }
                
            }
        }

        private void btnSelectSD_Click(object sender, EventArgs e)
        {
            fileToolStripMenuItem.ShowDropDown();
            selectSDDriveToolStripMenuItem.ShowDropDown();
        }

        private async Task SelectSDDrive(string driveName)
        {
            SDDrive = driveName;
            var validDrive = driveName != null;
            pbConnected.Image = validDrive ? Properties.Resources.green : Properties.Resources.red;
            btnSyncToSD.Enabled = validDrive;
            lblCartName.Enabled = validDrive;
            tbCartName.Enabled = validDrive;

            if (validDrive)
            {
                btnSelectSD.Text = $"Selected MicroSD drive = {driveName}";
            }
            else
            {
                btnSelectSD.Text = "Select MicroSD drive";
            }

            if (!validDrive) return;

            using (var progressForm = new ProgressForm(this, "Reading from MicroSD...", ProgressBarStyle.Marquee)) //TODO: real progress
            {
                //if there is a cartridge.json on the SD card, read the name
                var cartJsonPath = $"{driveName}cartridge.json";
                if (await Task.Run(() => File.Exists(cartJsonPath)))
                {
                    var cart = JsonConvert.DeserializeObject<Cartridge>(await File.ReadAllTextAsync(cartJsonPath));
                    tbCartName.Text = cart.cartridgeName;
                }

                //if any of the existing games on the SD card are known, pre-select those games and de-select the others
                var cartGamesDir = new DirectoryInfo($"{driveName}game");
                if (cartGamesDir.Exists)
                {
                    HashSet<string> cartGameIds = new HashSet<string>(cartGamesDir.GetFiles("*.json")
                        .Select(j => Path.GetFileNameWithoutExtension(j.Name))
                        .Where(g => _gamesManager.GamesDictionary.ContainsKey(g)));

                    if (cartGameIds.Count > 0)
                    {
                        foreach (var game in _gamesManager.Games)
                        {
                            game.IsSelected = cartGameIds.Contains(game.Id);
                        }

                        //TODO: in case 'selected games' filter is active, code below doesn't work
                        lvGames.ItemChecked -= new ItemCheckedEventHandler(lvGames_ItemChecked);
                        foreach (ListViewItem lvi in lvGames.Items)
                        {
                            lvi.Checked = cartGameIds.Contains(lvi.Name);
                        }
                        lvGames.ItemChecked += new ItemCheckedEventHandler(lvGames_ItemChecked);

                        UpdateTotalSelectedGamesLabel();
                    }
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            //resize listview to full width
            lvGames.Columns[0].Width = lvGames.Width - 4 - SystemInformation.VerticalScrollBarWidth;
        }

        private void rbRetroArchCore_CheckedChanged(object sender, EventArgs e)
        {
            cbRetroArchCore.Enabled = rbRetroArchCore.Checked;
            if (rbRetroArchCore.Checked)
            {
                if (cbRetroArchCore.SelectedIndex == -1)
                {
                    cbRetroArchCore.SelectedIndex = 0;
                    cbRetroArchCore.DataBindings["SelectedValue"].WriteValue();
                }
            }
            else
            {
                cbRetroArchCore.SelectedIndex = -1;
                if (_game != null) _game.RetroArchCore = null;
            }
        }

        private void lvGames_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && lvGames.SelectedItems.Count > 0)
            {
                DeleteSelectedGames();
            }
            //select all
            if (e.KeyCode == Keys.A && e.Control)
            {
                foreach (ListViewItem item in lvGames.Items)
                {
                    item.Selected = true;
                }
            }
            if (e.KeyCode == _konamiCode[_konamiIndex])
            {
                if (++_konamiIndex == _konamiCode.Length)
                {
                    _konamiIndex = 0;
                    Process.Start(new ProcessStartInfo(Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovL2xvZGVydW5uZXJ3ZWJnYW1lLmNvbS9nYW1l"))) { UseShellExecute = true });
                    MessageBox.Show("Kazuhisa Hashimoto and Douglas Smith R.I.P.", "Code was cracked");
                }
            }
            else
            {
                _konamiIndex = 0;
            }
        }

        private void DeleteSelectedGames()
        {
            //ask user if sure
            if (MessageBox.Show("Are you sure you want to delete these ROMs?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _gamesManager.DeleteGameByIds(lvGames.SelectedItems.Cast<ListViewItem>().Select(i => i.Name));

                //now also remove games from listview
                lvGames.BeginUpdate();
                foreach (ListViewItem listViewItem in lvGames.SelectedItems)
                {
                    listViewItem.Remove();
                }
                lvGames.Groups[0].Header = $"{lvGames.Items.Count} ROMs";
                lvGames.EndUpdate();

                UpdateTotalSelectedGamesLabel();

                //clear form controls
                ClearDataBindings();
            }
        }

        private void UpdateTotalSelectedGamesLabel()
        {
            lblNumberOfGamesSelected.Text = $"{_gamesManager.Games.Count(g => g.IsSelected)} ROM(s) selected for Sync";
        }

        private async void pbGameImage_Click(object sender, EventArgs e)
        {
            if (_game != null && (openGameImage.ShowDialog() == DialogResult.OK))
            {
                var controlName = ((Control)sender).Name;
                List<ImageInfo> gameImages = new List<ImageInfo>();

                if (controlName == nameof(pbBoxArtSmall) || controlName == nameof(pbBoxArtLarge))
                {
                    gameImages.Add(_gamesManager.GetGameImageInfo(_game.Id, ImageType.Small));
                }
                if (controlName == nameof(pbBoxArtMedium) || controlName == nameof(pbBoxArtLarge))
                {
                    gameImages.Add(_gamesManager.GetGameImageInfo(_game.Id, ImageType.Medium));
                }
                if (controlName == nameof(pbBoxArtLarge))
                {
                    gameImages.Add(_gamesManager.GetGameImageInfo(_game.Id, ImageType.Large));
                }
                if (controlName == nameof(pbBanner))
                {
                    gameImages.Add(_gamesManager.GetGameImageInfo(_game.Id, ImageType.Banner));
                }

                await _imageManager.ResizeImage(openGameImage.FileName, _game, gameImages);

                LoadBoxArt();

                await _gamesManager.SerializeGame(_game);
            }
        }

        private async void btnAddGames_Click(object sender, EventArgs e)
        {
            toolTip1.Hide(lvGames); //hide the welcome text

            var supportedExtensions = String.Join(";", _appSettings.Platforms
                .SelectMany(p => p.RomFileExtensions).Select(e => $"*{e}"));

            OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = $"Game ROMs ({supportedExtensions})|{supportedExtensions}",
                Title = "Select Game ROMs"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (var progressForm = new ProgressForm(this))
                {
                    var newGames = await _gamesManager.ImportGamesByRom(dialog.FileNames, progressForm.Reporter);
                    if (!newGames.Any()) return; //nothing to import

                    try
                    {
                        await _gamesManager.EnrichGames(newGames, progressForm.Reporter);
                    }
                    catch {  /* suppress scraping errors during rom import */ }

                    lvGames.BeginUpdate();
                    lvGames.ItemChecked -= new ItemCheckedEventHandler(lvGames_ItemChecked);

                    //add to current view as "Just added"
                    lvGames.Groups[0].Items.AddRange(lvGames.Groups[1].Items);
                    lvGames.Groups[1].Items.Clear();

                    foreach (var newGame in newGames)
                    {
                        var lvi = new ListViewItem(_gamesManager.GetRomListTitle(newGame))
                        {
                            Name = newGame.Id
                        };
                        lvGames.Groups[1].Items.Add(lvi);
                        lvGames.Items.Add(lvi);
                    }

                    lvGames.Groups[0].Header = $"{lvGames.Items.Count} ROMs";

                    lvGames.ItemChecked += new ItemCheckedEventHandler(lvGames_ItemChecked);
                    MainForm_Resize(null, null); //resize form to make long game names fit
                    lvGames.EndUpdate();
                }

                if (lvGames.Groups[1].Items.Count > 0)
                {
                    lvGames.SelectedIndices.Clear();
                    lvGames.Groups[1].Items[lvGames.Groups[1].Items.Count -1].EnsureVisible();
                    lvGames.Groups[1].Items[0].Focused = true;
                    lvGames.Groups[1].Items[0].Selected = true;
                    lvGames.Groups[1].Items[0].EnsureVisible();
                    lvGames.Focus();
                }
            }
        }

        private async void btnSyncToSD_Click(object sender, EventArgs e)
        {
            if (lvGames.CheckedItems.Count == 0)
            {
                MessageBox.Show(
                    "Please first select some game(s) by ticking their checkbox in the list view on the left.",
                    "No games selected to sync!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //show warning if there are any games using RetroArch, but no /sdcard/retroarch directory
            if (_gamesManager.Games.Any(g => g.IsSelected && g.RetroArchCore != null) && !Directory.Exists($"{SDDrive}retroarch"))
            {
                MessageBox.Show(
                    "You've selected some games to run with RetroArch, but no RetroArch directory was found on your MicroSD card.\n" +
                    "Please download RetroArch from https://eversd.com/downloads and extract it to the root folder of your MicroSD card.",
                    "No RetroArch found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var progressForm = new ProgressForm(this))
            {
                try
                {
                    await _gamesManager.SyncToSd(SDDrive, tbCartName.Text, progressForm.Reporter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not sync to MicroSD. Error message: \"{ex.Message}\".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            MessageBox.Show("The selected ROMs were synced to MicroSD.\nPut the MicroSD card back in your EverSD cartridge and enjoy!", 
                "ROM Sync Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void driveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem menuItem in selectSDDriveToolStripMenuItem.DropDownItems)
            {
                menuItem.Checked = false;
            }
            var clickedItem = (ToolStripMenuItem)sender;
            clickedItem.Checked = true;
            await SelectSDDrive(clickedItem.Name);
        }


        private async void selectSDDriveToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var drives = SDCardHelper.FindRemovableDrives();
            if (drives.Length == 0)
            {
                await SelectSDDrive(null);
                if (selectSDDriveToolStripMenuItem.DropDownItems.Count == 0
                    || selectSDDriveToolStripMenuItem.DropDownItems["notfound"] == null)
                {
                    selectSDDriveToolStripMenuItem.DropDownItems.Clear();
                    var item = new ToolStripMenuItem("No removable drive(s) found...", null, null, "notfound");
                    item.ForeColor = Color.Red;
                    selectSDDriveToolStripMenuItem.DropDownItems.Add(item);
                }
            }
            else
            {
                selectSDDriveToolStripMenuItem.DropDownItems.Clear();
                bool itemWasChecked = false;
                foreach (var drive in drives)
                {
                    var item = new ToolStripMenuItem($"{drive.Name} - Total size: {drive.TotalSize.ToSize()}", null, driveToolStripMenuItem_Click, drive.Name);
                    item.Checked = SDDrive != null && item.Name.StartsWith(SDDrive);
                    itemWasChecked |= item.Checked;
                    selectSDDriveToolStripMenuItem.DropDownItems.Add(item);
                }
                if (!itemWasChecked)
                {
                    await SelectSDDrive (null);
                }
            }
        }

        private async void btnImageClear_Click(object sender, EventArgs e)
        {
            if (_game != null)
            {
                //clear banner img
                if (Enum.TryParse<ImageType>(((Control)sender).Tag as string, ignoreCase:true, out ImageType imgType))
                {
                    await _gamesManager.ClearImage(_game, imgType);
                    if (imgType == ImageType.Banner)
                    {
                        pbBanner.Tag = 0;
                    }
                    LoadBoxArt();
                }
            }
        }

        private void cbRomFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            IEnumerable<GameInfo> gameInfos;
            switch (cbRomFilter.SelectedIndex)
            {
                case (int)RomListFilter.SelectedForSync: //selected
                    gameInfos = _gamesManager.Games.Where(g => g.IsSelected); break;
                case (int)RomListFilter.RecentlyAdded: //recent
                    gameInfos = _gamesManager.Games.Where(g => g.IsRecentlyAdded); break;
                case (int)RomListFilter.RomsWithoutBanner: //selected
                    gameInfos = _gamesManager.Games.Where(g => g.ImageBanner == null); break;
                case (int)RomListFilter.RomsWithoutBoxart: //selected
                    gameInfos = _gamesManager.Games.Where(g => g.Image == null && g.Image1080 == null); break;
                case (int)RomListFilter.RomsWithoutDescription: //selected
                    gameInfos = _gamesManager.Games.Where(g => g.romDescription == string.Empty); break;
                default: //all
                    gameInfos = _gamesManager.Games; break;
            }

            lvGames.BeginUpdate();
            lvGames.SelectedIndices.Clear();
            lvGames.Items.Clear();
            lvGames.Items.AddRange(gameInfos.Select(g => new ListViewItem(_gamesManager.GetRomListTitle(g))
            {
                Checked = g.IsSelected,
                Name = g.Id,
                Group = lvGames.Groups[0]
            }).ToArray());
            lvGames.Groups[0].Header = $"{lvGames.Items.Count} ROMs";
            lvGames.EndUpdate();

            MainForm_Resize(null, null); //resize form to make long game names fit
        }

        private void lvGames_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (lvGames.FocusedItem == null) return;

            _gamesManager.GetGameById(e.Item.Name).IsSelected = e.Item.Checked;
            UpdateTotalSelectedGamesLabel();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"You are running EverLoader version {Application.ProductVersion}.\n" +
                $"\nThis tool allows you to easily add games to your EverSD cartridge.\n" +
                $"\nExternal components used:\n" +
                $"- TheGamesDBApiWrapper by Malte Peters\n" +
                $"- Crc32.NET by force\n" +
                $"- DropDownControls by Bradley Smith", 
                "About EverLoader", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void deleteSelectedGamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedGames();
        }

        private void lvGames_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var focusedItem = lvGames.FocusedItem;
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                {
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private async void scrapeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var progressForm = new ProgressForm(this))
            {
                _game.GameInfoChanged -= Game_GameInfoChanged;
                try
                {
                    await _gamesManager.EnrichGames(lvGames.SelectedItems.Cast<ListViewItem>().Select(s => s.Name), progressForm.Reporter);
                }
                catch (Exception ex)
                {
                    ShowScrapingErrorMessageBox(ex);
                    //continue
                }
                _game.GameInfoChanged += Game_GameInfoChanged;
                SetDataBindings();
            }
        }

        private void ShowScrapingErrorMessageBox(Exception ex)
        {
            MessageBox.Show($"There were problems calling the Scraping API. Error message: \"{ex.Message}\".", "Scraping Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool IdleHandlerSet { get; set; }
        private void lvGames_SelectedIndexChanged(object sender, EventArgs e)
        {
            // this will fire every time items are selected or deselected.
            if (!IdleHandlerSet)
            {
                IdleHandlerSet = true;
                Application.Idle += lvGames_SelectionChanged;
            }
        }

        private void lblMissingBiosFiles_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var missingFileList = string.Join(";", GetMissingRomFiles());

            //upload BIOS files for this platform
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = $"{missingFileList}|{missingFileList}",
                Title = "Select BIOS files"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var platformRomsDir = $"{Constants.APP_ROOT_FOLDER}bios\\{_gamesManager.GetGamePlatform(_game).Alias}";
                foreach (var romFilePath in dialog.FileNames)
                {
                    Directory.CreateDirectory(platformRomsDir);
                    File.Copy(romFilePath, Path.Combine(platformRomsDir,Path.GetFileName(romFilePath)));
                }

                UpdateMissingBiosFilesLabel();
            }
        }

        private async void btnScrape_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var platform = _gamesManager.GetGamePlatform(_game);
                lbScrapeResults.Items.Clear();
                lbScrapeResults.Enabled = true;
                lbScrapeResults.DisplayMember = nameof(TgdbScrapeResult.GameName);
                lbScrapeResults.Items.AddRange((await _scrapeManager.ScrapeByGameTitle(tbScrapeName.Text, platform.TGDB_PlatformIds)).ToArray());
            }
            catch (Exception ex)
            {
                ShowScrapingErrorMessageBox(ex);
                //continue
            }
            Cursor.Current = Cursors.Default;
            if (lbScrapeResults.Items.Count == 0)
            {
                lbScrapeResults.Items.Add("[no results - try again using less words]");
            }
        }
        private void tbScrapeName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                btnScrape_Click(null, null);
            }  
        }

        private async void lbScrapeResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedResult = lbScrapeResults.SelectedItem as TgdbScrapeResult;
            if (selectedResult != null && _game != null)
            {
                if ((_game.romDescription != "" || _game.Image != null || _game.Image1080 != null) 
                    && (lbScrapeResults.Tag as string) != "WARNING_SHOWN"
                    && MessageBox.Show("This will overwrite the current ROM description and images with the scraped information.\nIs this OK?", 
                    "Applying scraped game content",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.No)
                {
                    return;
                }

                lbScrapeResults.Tag = "WARNING_SHOWN";

                _game.GameInfoChanged -= Game_GameInfoChanged; //suppress

                _game.TgdbId = selectedResult.GameId;
                _game.romTitle = selectedResult.Game.GameTitle;
                lvGames.SelectedItems[0].Text = _gamesManager.GetRomListTitle(_game);
                _game.romDescription = selectedResult.Game.Overview;
                _game.romGenre = _romManager.MapToGenre(selectedResult.Game.Genres);
                _game.romPlayers = selectedResult.Game.Players.HasValue ? selectedResult.Game.Players.Value : 1; //default 1
                _game.romReleaseDate = selectedResult.Game.ReleaseDate.HasValue ? selectedResult.Game.ReleaseDate.Value.ToString("yyyy-MM-dd") : "";

                pbBanner.Tag = 0; //reset banner offset

                if (selectedResult.BoxArt != null)
                {
                    await _imageManager.ResizeImage($"{selectedResult.ImageBaseUrl}{selectedResult.BoxArt.FileName}", _game, _gamesManager.GetGameBoxartImageInfo(_game.Id));
                }

                if (selectedResult.Banners?.Length > 0)
                {
                    await _imageManager.ResizeImage($"{selectedResult.ImageBaseUrl}{selectedResult.Banners[0].FileName}", _game, new[] { _gamesManager.GetGameImageInfo(_game.Id, ImageType.Banner) });
                }
                else
                {
                    _game.ImageBanner = null;
                }
                llBannerNext.Visible = llBannerPrev.Visible = (selectedResult.Banners?.Length > 1);

                await _gamesManager.SerializeGame(_game);
                SetDataBindings(refreshOnly:true);

                _game.GameInfoChanged += Game_GameInfoChanged;
            }
        }

        private async void llBannerNextPrev_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var selectedResult = lbScrapeResults.SelectedItem as TgdbScrapeResult;
            if (selectedResult?.Banners != null && _game != null)
            {
                pbBanner.Tag = 0; //reset offset

                if (((Control)sender).Name.Contains("Next"))
                {
                    if (++selectedResult.UIBannerIndex >= selectedResult.Banners.Length) selectedResult.UIBannerIndex = 0;
                }
                else
                {
                    if (--selectedResult.UIBannerIndex < 0) selectedResult.UIBannerIndex = selectedResult.Banners.Length - 1;
                }

                await _imageManager.ResizeImage($"{selectedResult.ImageBaseUrl}{selectedResult.Banners[selectedResult.UIBannerIndex].FileName}", _game, new[] { _gamesManager.GetGameImageInfo(_game.Id, ImageType.Banner) });

                await _gamesManager.SerializeGame(_game);
                SetDataBindings(refreshOnly: true);
            }
        }

        private async void llBannerUpDown_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_game != null && !string.IsNullOrEmpty(_game.ImageBanner))
            {
                var sourceImg = GamesManager.GetSourceImagePath(_game.ImageBanner);
                if (File.Exists(sourceImg))
                {
                    if (((Control)sender).Name == nameof(llBannerUp))
                    {
                        //decrement pbBanner offset (stored in Tag property)
                        pbBanner.Tag = Math.Max(-ImageInfo.MaxVerticalOffset, (int)pbBanner.Tag - 1);
                    }
                    else
                    {
                        //increment pbBanner offset
                        pbBanner.Tag = Math.Min(ImageInfo.MaxVerticalOffset, (int)pbBanner.Tag + 1);
                    }

                    var bannerInfo = _gamesManager.GetGameImageInfo(_game.Id, ImageType.Banner);
                    bannerInfo.VerticalOffset = (int)pbBanner.Tag;

                    await _imageManager.ResizeImage(sourceImg, _game, new[] { bannerInfo }, saveOriginal: false); //no need to save original, as we already have it

                    await _gamesManager.SerializeGame(_game);
                    SetDataBindings(refreshOnly: true);
                }
            }
        }

        private async void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await _appUpdateManager.CheckForUpdate(this);
        }

        private void pbEverSD_Click(object sender, EventArgs e)
        {
            //open EverSD website
            Process.Start(new ProcessStartInfo("https://eversd.com") { UseShellExecute = true });
        }

        private void pbGameImage_SizeChanged(object sender, EventArgs e)
        {
            switch (((Control)sender).Name)
            {
                case nameof(pbBoxArtSmall):
                    btnClearSmall.Location = new Point(pbBoxArtSmall.Location.X + pbBoxArtSmall.Width - btnClearSmall.Width, btnClearSmall.Location.Y);
                    break;
                case nameof(pbBoxArtMedium):
                    btnClearMedium.Location = new Point(pbBoxArtMedium.Location.X + pbBoxArtMedium.Width - btnClearMedium.Width, btnClearMedium.Location.Y);
                    break;
                case nameof(pbBoxArtLarge):
                    btnClearLarge.Location = new Point(pbBoxArtLarge.Location.X + pbBoxArtLarge.Width - btnClearLarge.Width, btnClearLarge.Location.Y);
                    break;
                case nameof(pbBanner):
                    // reposition clear image
                    btnClearBanner.Location = new Point(pbBanner.Location.X + pbBanner.Width - btnClearBanner.Width, btnClearBanner.Location.Y);
                    // reposition up/down buttons
                    llBannerUp.Location = new Point(pbBanner.Location.X + pbBanner.Width - 75, llBannerUp.Location.Y);
                    llBannerDown.Location = new Point(pbBanner.Location.X + pbBanner.Width - 57, llBannerDown.Location.Y);
                    //left/right buttons
                    llBannerPrev.Location = new Point(pbBanner.Location.X, pbBanner.Location.Y + (pbBanner.Height - llBannerPrev.Height) / 2);
                    llBannerNext.Location = new Point(pbBanner.Location.X + pbBanner.Width - llBannerNext.Width, pbBanner.Location.Y + (pbBanner.Height - llBannerNext.Height) / 2);
                    break;
            }
        }
    }
}
