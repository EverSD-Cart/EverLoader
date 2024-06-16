using EverLoader.Enums;
using EverLoader.Extensions;
using EverLoader.Forms;
using EverLoader.Helpers;
using EverLoader.Models;
using EverLoader.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using TreeView = System.Windows.Forms.TreeView;
using TextBox = System.Windows.Forms.TextBox;
using Button = System.Windows.Forms.Button;

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
        private readonly UserSettingsManager _userSettingsManager;
        private ToolTip toolTipLabel = new ToolTip();
        public string SDDrive { get; set; }

        public MainForm(GamesManager gamesManager,
            ScrapeManager scrapeManager,
            RomManager romManager,
            ImageManager imageManager,
            AppUpdateManager appUpdateManager,
            AppSettings appSettings,
            UserSettingsManager userSettingsManager)
        {
            _gamesManager = gamesManager;
            _scrapeManager = scrapeManager;
            _romManager = romManager;
            _imageManager = imageManager;
            _appUpdateManager = appUpdateManager;
            _appSettings = appSettings;
            _userSettingsManager = userSettingsManager;

            InitializeComponent();

            // some more UI settings
            this.MinimumSize = this.Size; //startup size is the minimum size
            this.CenterToScreen();

            lvGames.Groups.Add(new ListViewGroup("ROM(s)", HorizontalAlignment.Center)); //group 0
            lvGames.Groups.Add(new ListViewGroup("Added Just Now", HorizontalAlignment.Center)); //group 1

            //set TabStop for linklabels to false
            lblMissingBiosFiles.TabStop = false;
            llBannerNext.TabStop = false;
            llBannerPrev.TabStop = false;
            llBannerUp.TabStop = false;
            llBannerDown.TabStop = false;

            //allow image dropping in picture box
            pbBoxArtLarge.AllowDrop = true;
            pbBoxArtMedium.AllowDrop = true;
            pbBoxArtSmall.AllowDrop = true;
            pbBanner.AllowDrop = true;

            CheckUserSettings();
            PopulatePlatformCombobox();

            GetFolderList();
            BuildFolderTree("");
        }

        private void CheckUserSettings()
        {
            optimizeImagesToolStripMenuItem.Checked = _userSettingsManager.UserSettings.OptimizeImageSizes;
            optimizeImagesToolStripMenuItem.CheckedChanged += OptimizeImagesToolStripMenuItem_CheckedChanged;
        }

        private void OptimizeImagesToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _userSettingsManager.UserSettings.OptimizeImageSizes = optimizeImagesToolStripMenuItem.Checked;
        }

        /// <summary>
        /// Populates the platform dropdown boxes
        /// </summary>
        private void PopulatePlatformCombobox()
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
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            cbRomFilter.SelectedIndexChanged -= new EventHandler(this.cbRomFilter_SelectedIndexChanged);
        }

        private void PopulateRomFilterCombobox()
        {
            cbRomFilter.SelectedIndexChanged -= new EventHandler(this.cbRomFilter_SelectedIndexChanged);

            var currSelectedValue = cbRomFilter.SelectedValue;

            cbRomFilter.DataSource = null;
            cbRomFilter.DisplayMember = "Text";
            cbRomFilter.ValueMember = "Value";
            cbRomFilter.GroupMember = "Group";
            var dsRomFilter = Enum.GetValues(typeof(RomFeatureFilter))
                .Cast<Enum>()
                .Select(value => new
                {
                    Group = "Filter by Feature",
                    Text = (Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute).Description,
                    Value = value.ToString()
                }).ToList();
            //add platforms used by current collection of games
            foreach (var platform in _gamesManager.GetExistingGamePlatforms())
            {
                dsRomFilter.Add(new
                {
                    Group = "Filter by Platform",
                    Text = platform.GroupAndName,
                    Value = platform.Id.ToString()
                });
            }

            cbRomFilter.DataSource = dsRomFilter;

            if (currSelectedValue != null)
            {
                cbRomFilter.SelectedValue = currSelectedValue;
            }

            cbRomFilter.Enabled = true;
            cbRomFilter.SelectedIndexChanged += new EventHandler(this.cbRomFilter_SelectedIndexChanged);
        }

        private async void MainForm_Shown(object sender, System.EventArgs e)
        {
            //create GamesManager and read local database
            using (var progressForm = new ProgressForm(this))
            {
                await _gamesManager.ReadGames(progressForm.Reporter);
            }

            UpdateTotalSelectedGamesLabel();

            //after games read, select the 'all ROMs' filter, which will add all games to listview
            PopulateRomFilterCombobox();
            cbRomFilter_SelectedIndexChanged(null, null);

            lvGames.ItemChecked += new ItemCheckedEventHandler(lvGames_ItemChecked);

            if (_gamesManager.GamesDictionary.Count == 0)
            {
                toolTip1.Show("Welcome to EverLoader; the easiest way to sync your ROMs to the EverSD cart!\n\n" +
                              "It looks like your ROMs collection is still empty...\n\n" +
                              "Click the 'Add New ROM(s)' button below to import your ROMs.", lvGames);
            }

            //run stuff in the background
            await Task.Run(async () =>
            {
                try
                {
                    await _romManager.Init();
                }
                catch (Exception ex)
                {
                    /* ignore for now */
                }
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
                //show original rom filenam when hovering gameid label
                toolTip1.SetToolTip(lblGameId, _game != null ? $"'{_game.OriginalRomFileName}'" : null);

                // Emulator Settings
                var platform = _gamesManager.GetGamePlatform(_game);
                var ext = Path.GetExtension(_game?.romFileName);

                cbMultiDisc.Visible = _game?.IsMultiDisc == true;

                rbInternalCore.Enabled = !cbMultiDisc.Visible && platform?.InternalEmulator?.SupportedExtensions.Contains(ext) == true;
                rbRetroArchCore.Enabled = cbRetroArchCore.Enabled = platform?.RetroArchCores?.Any(r => r.SupportedExtensions.Contains(ext)) == true;

                cbRetroArchCore.DataSource = rbRetroArchCore.Enabled
                    ? platform.RetroArchCores.Where(r => r.SupportedExtensions.Contains(ext))
                        .Select(c => new ComboboxItem(c.DisplayName, c.CoreFileName)).ToArray()
                    : null;
                cbRetroArchCore.DataBindings.DefaultDataSourceUpdateMode = DataSourceUpdateMode.Never;
                cbRetroArchCore.DataBindings.AddSingle("SelectedValue", _game, nameof(_game.RetroArchCore));

                if (_game == null)
                {
                    rbInternalCore.Checked = rbRetroArchCore.Checked = false;
                }
                else
                {
                    rbInternalCore.Checked = !_game.IsMultiDisc && _game.RetroArchCore == null;
                    rbRetroArchCore.Checked = _game.RetroArchCore != null;
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
            var missingBiosFiles = _gamesManager.GetMissingBiosFiles(_game, includeOptionalBios: true);
            lblMissingBiosFiles.Visible = missingBiosFiles.Length > 0;
            var platform = _gamesManager.GetGamePlatform(_game);
            if (platform != null)
            {
                string missingBiosText = null;
                string missingBiosList = null;
                if (missingBiosFiles.Any(b => b.Required))
                {
                    lblMissingBiosFiles.Text = "Upload required BIOS files";
                    lblMissingBiosFiles.LinkColor = Color.Red;
                    missingBiosList = string.Join("\n - ", missingBiosFiles.Where(b => b.Required).Select(b => b.FileName));
                    missingBiosText = "requires\nthese missing";
                }
                else
                {
                    lblMissingBiosFiles.Text = "Upload optional BIOS files";
                    lblMissingBiosFiles.LinkColor = Color.Orange;
                    missingBiosList = string.Join("\n - ", missingBiosFiles.Select(b => b.FileName));
                    missingBiosText = "supports\nthese optional";
                }

                toolTip1.SetToolTip(lblMissingBiosFiles,
                    $"The {platform.Name} emulator {missingBiosText} BIOS files:\n - {missingBiosList}");
            }
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
            contextMenuStripMSD.Show(Cursor.Position);
        }

        private async Task SelectSDDrive(string driveName)
        {
            SDDrive = driveName;
            var validDrive = driveName != null;
            pbConnected2.Image = validDrive ? Properties.Resources.green : Properties.Resources.red;
            btnSyncWithSDCard.Enabled = validDrive;
            btnNewSDCardFolder.Enabled = validDrive;
            lblCartName.Enabled = validDrive;
            tbCartName2.Enabled = validDrive;

            if (validDrive)
            {
                btnSelectedSD.Text = $"MicroSD = {driveName}";
            }
            else
            {
                btnSelectedSD.Text = "Select MicroSD";
            }

            if (!validDrive) return;

            using (var progressForm = new ProgressForm(this, "Reading from MicroSD...", ProgressBarStyle.Marquee)) //TODO: real progress
            {
                //if there is a cartridge.json on the SD card, read the name
                var cartJsonPath = $"{driveName}cartridge.json";
                if (await Task.Run(() => File.Exists(cartJsonPath)))
                {
                    var cart = JsonConvert.DeserializeObject<Cartridge>(await File.ReadAllTextAsync(cartJsonPath));
                    tbCartName2.Text = cart.cartridgeName;
                }

                /* pre-select games which are found on the SD card */
                var cartGamesDir = new DirectoryInfo($"{driveName}game");

                var gameJsonPaths = cartGamesDir.GetFiles("*.json");
                if (gameJsonPaths.Length > 0)
                {
                    InitFolderList(SDDrive);
                }

                GetFolderList();

                foreach (var gameJsonPath in gameJsonPaths)
                {
                    var gameId = Path.GetFileNameWithoutExtension(gameJsonPath.Name);

                    if (_gamesManager.Games.Select(j => j.Id).Contains(gameId))
                    {
                        var romPath = $"{driveName}game\\{gameId}.json";
                        var gameJson = JsonConvert.DeserializeObject<GameInfo>(await File.ReadAllTextAsync(romPath));
                        var romTitle = gameJson.romTitle;

                        _gamesManager.GamesOnSDCard.Add(new GameInfoTreeNode { Id = gameId, Title = romTitle, Path = romPath });
                    }
                    else
                    {
                        if (!gameId.Equals("retroarch"))
                        {
                            var gameJson = JsonConvert.DeserializeObject<GameInfo>(await File.ReadAllTextAsync(gameJsonPath.FullName));
                            if (gameJson != null)
                            {
                                _gamesManager.GamesOnSDCard.Add(new GameInfoTreeNode { Id = gameId, Title = $"{gameId} ({gameJson.romTitle})", Path = $"{driveName}game\\{gameId}.json", IsMissngInCollection = true });
                            }
                        }
                    }
                }

                if (Directory.Exists($"{driveName}folders"))
                {
                    cartGamesDir = new DirectoryInfo($"{driveName}folders");

                    var gameFolders = cartGamesDir.GetDirectories();
                    foreach (var gameFolder in gameFolders)
                    {
                        var gameFolderDir = new DirectoryInfo($"{gameFolder.FullName}\\game");
                        gameJsonPaths = gameFolderDir.GetFiles("*.json");

                        foreach (var gameJsonPath in gameJsonPaths)
                        {
                            var gameId = Path.GetFileNameWithoutExtension(gameJsonPath.Name);
                            if (gameId == "_HOME")
                            {
                                continue;
                            }

                            if (_gamesManager.Games.Select(j => j.Id).Contains(gameId))
                            {
                                var romPath = $"{gameFolder}\\game\\{gameId}.json";
                                var gameJson = JsonConvert.DeserializeObject<GameInfo>(await File.ReadAllTextAsync(romPath));
                                var romTitle = gameJson.romTitle;

                                _gamesManager.GamesOnSDCard.Add(new GameInfoTreeNode { Id = gameId, Title = romTitle, Path = $"{gameFolder}\\game\\{gameId}.json" });
                            }
                            else
                            {
                                var gameJson = JsonConvert.DeserializeObject<GameInfo>(await File.ReadAllTextAsync(gameJsonPath.FullName));
                                _gamesManager.GamesOnSDCard.Add(new GameInfoTreeNode { Id = gameId, Title = $"{gameId} ({gameJson.romTitle})", Path = $"{gameFolder}\\game\\{gameId}.json", IsMissngInCollection = true });
                            }
                        }
                    }
                }

                _gamesManager.GamesOnSDCard = _gamesManager.GamesOnSDCard.OrderBy(x => x.Title).ToList();
                foreach (var game in _gamesManager.Games)
                {
                    game.IsPresentOnCartridge = _gamesManager.GamesOnSDCard.Where(g => g.Id == game.Id).Count() > 0;
                }

                UpdateTotalSelectedGamesLabel();
                UpdateTotalGamesOnSDCardLabel();

                BuildFolderTree("");
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
                if (cbRetroArchCore.SelectedIndex == -1 && cbRetroArchCore.Items.Count > 0)
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

                lvGames.Groups[0].Header = $"{lvGames.Items.Count} ROM(s)";
                lvGames.EndUpdate();

                UpdateTotalSelectedGamesLabel();

                //clear form controls
                ClearDataBindings();
            }
        }

        private void UpdateTotalSelectedGamesLabel()
        {
            lblNumberOfGamesSelected.Text = $"{_gamesManager.Games.Count(g => g.IsSelected)} / {_gamesManager.Games.Count()} ROM(s) selected for Sync";
        }

        private void UpdateTotalGamesOnSDCardLabel()
        {
            lblNumberGamesSDCard.Text = $"{_gamesManager.GamesOnSDCard.Count()} ROM(s) and {_gamesManager.GamesOnSDCard.Count(i => i.IsFolder) - 1} Folders on Cartridge";
        }

        private async void pbGameImage_Click(object sender, EventArgs e)
        {
            if (_game != null && (openGameImage.ShowDialog() == DialogResult.OK))
            {
                await ImportResizeImages(sender as Control, openGameImage.FileName);
            }
        }

        private async Task ImportResizeImages(Control targetControl, string imageFilePath, byte[] fileBytes = null)
        {
            if (_game == null) return;

            var controlName = targetControl.Name;
            List<ImageInfo> gameImages = new List<ImageInfo>();

            if (controlName == nameof(pbBoxArtSmall) || (controlName == nameof(pbBoxArtLarge) && _game.Image == null))
            {
                gameImages.Add(_gamesManager.GetGameImageInfo(_game.Id, ImageType.Small));
            }

            if (controlName == nameof(pbBoxArtMedium) || (controlName == nameof(pbBoxArtLarge) && _game.ImageHD == null))
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
                pbBanner.Tag = 0;
            }

            if (imageFilePath != null) await _imageManager.ResizeImage(imageFilePath, _game, gameImages);
            if (fileBytes != null) _imageManager.ResizeImage(fileBytes, _game, gameImages);

            LoadBoxArt();

            await _gamesManager.SerializeGame(_game);
        }

        private async Task AddGames(string[] fileNames)
        {
            toolTip1.Hide(lvGames); //hide the welcome text

            using (var progressForm = new ProgressForm(this))
            {
                var newGames = await _gamesManager.ImportGamesByRom(fileNames, progressForm.Reporter);
                if (!newGames.Any()) return; //nothing to import

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

                lvGames.Groups[0].Header = $"{lvGames.Items.Count} ROM(s)";

                lvGames.ItemChecked += new ItemCheckedEventHandler(lvGames_ItemChecked);
                MainForm_Resize(null, null); //resize form to make long game names fit
                lvGames.EndUpdate();
            }

            if (lvGames.Groups[1].Items.Count > 0)
            {
                lvGames.SelectedIndices.Clear();
                lvGames.Groups[1].Items[lvGames.Groups[1].Items.Count - 1].EnsureVisible();
                lvGames.Groups[1].Items[0].Focused = true;
                lvGames.Groups[1].Items[0].Selected = true;
                lvGames.Groups[1].Items[0].EnsureVisible();
                lvGames.Focus();
            }

            PopulateRomFilterCombobox();
        }

        private async void btnSyncToSD_Click(object sender, EventArgs e)
        {
            if (!_gamesManager.Games.Any(g => g.IsSelected) && !_gamesManager.SdContainsKnownGames(SDDrive))
            {
                MessageBox.Show(
                    "Please first select some game(s) by ticking their checkbox in the list view on the left.",
                    "No games selected to sync!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //show warning if there are any games using RetroArch, but no /sdcard/retroarch directory
            if (_gamesManager.Games.Any(g => g.IsSelected && g.RetroArchCore != null) && !Directory.Exists($"{Path.GetPathRoot(SDDrive)}retroarch"))
            {
                MessageBox.Show(
                    "You've selected some games to run with RetroArch, but no RetroArch directory was found in the root of your MicroSD card.\n" +
                    "Please download RetroArch from https://eversd.com/downloads and extract it to the root folder of your MicroSD card.",
                    "No RetroArch found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //show warning if one of the games selected for sync has missing BIOS file(s)
            foreach (var game in _gamesManager.Games.Where(g => g.IsSelected))
            {
                var missingBiosFiles = _gamesManager.GetMissingBiosFiles(game, includeOptionalBios: false).Select(b => b.FileName);
                if (missingBiosFiles.Any())
                {
                    MessageBox.Show(
                        $"The game '{game.romTitle}' is missing these required BIOS files:\n\n" +
                        " - " + string.Join("\n - ", missingBiosFiles) +
                        "\n\nPlease select the game and upload the missing BIOS files.",
                        "Required BIOS files missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            using (var progressForm = new ProgressForm(this)) //, progressStyle:ProgressBarStyle.Marquee))
            {
                try
                {
                    await _gamesManager.SyncToSd(SDDrive, tbCartName2.Text, "", progressForm.Reporter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not sync to MicroSD. Error message: \"{ex.Message}\".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            MessageBox.Show("The selected ROMs were synced to MicroSD.\nPut the MicroSD card back in your EverSD cartridge and enjoy!",
                "ROM Sync Done", MessageBoxButtons.OK, MessageBoxIcon.Information);

            foreach (var game in _gamesManager.Games)
            {
                game.IsSelected = false;
            }

            cbRomFilter_SelectedIndexChanged(sender, e);

            await SelectSDDrive(SDDrive);
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

        private void InitFolderList(string driveName)
        {
            _gamesManager.GamesOnSDCard.Clear();
            var folderItem = new GameInfoTreeNode();

            if (String.IsNullOrEmpty(driveName))
            {
                folderItem.Title = "Select MicroSD";
            }
            else
            {
                folderItem.Title = "Top Level / Root";
            }

            if (!String.IsNullOrEmpty(driveName))
            {
                folderItem.Path = driveName;
            }
            else
            {
                folderItem.Path = "";
            }

            folderItem.IsFolder = true;

            _gamesManager.GamesOnSDCard.Add(folderItem);
        }

        private void GetFolderList()
        {
            if (_gamesManager.GamesOnSDCard.Count == 0)
            {
                InitFolderList(SDDrive);
            }

            var drive = SDDrive;
            if (!String.IsNullOrEmpty(drive))
            {
                //check for subfolders of the /folders directory
                var folders = new DirectoryInfo($"{drive}folders");
                if (folders.Exists)
                {
                    foreach (var dir in folders.GetDirectories())
                    {
                        var folderSubItem = new GameInfoTreeNode();
                        var gameJsonPath = _gamesManager.GetGameJsonFromFolderPath(dir.FullName);

                        if (File.Exists(gameJsonPath))
                        {
                            var gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText(gameJsonPath));
                            folderSubItem.Title = gameInfo.romTitle;
                        }
                        else
                        {
                            folderSubItem.IsMissngOnCartridge = true;
                            folderSubItem.Title = Path.GetFileName(dir.FullName);
                        }

                        folderSubItem.Path = dir.FullName + "\\";
                        folderSubItem.IsFolder = true;

                        _gamesManager.GamesOnSDCard.Add(folderSubItem);
                    }
                }
            }
        }

        private void BuildFolderTree(string search)
        {
            tvCartridge.Nodes.Clear();
            var rootNode = new TreeNode();

            if (!string.IsNullOrWhiteSpace(SDDrive))
            {
                var rootGameInfo = _gamesManager.GetGameJsonFromSDCardByPath(SDDrive);
                rootNode.Text = rootGameInfo.Title;
                rootNode.Name = rootGameInfo.Path;
                rootNode.ImageIndex = 2;
                tvCartridge.Nodes.Add(rootNode);
            }

            foreach (var folder in _gamesManager.GamesOnSDCard)
            {
                if (folder.IsFolder)
                {
                    if (!String.IsNullOrEmpty(folder.Path) && folder.Path == SDDrive)
                    {
                        // rootNode already exists 
                    }
                    else
                    {
                        var folderNode = new TreeNode(folder.Title);
                        folderNode.Name = folder.Path;
                        folderNode.ImageIndex = 2;
                        if (folder.IsMissngOnCartridge)
                        {
                            folderNode.ImageIndex = 3;
                        }

                        tvCartridge.Nodes.Add(folderNode);
                    }
                }
            }

            foreach (var folder in _gamesManager.GamesOnSDCard)
            {
                var addItem = false;

                if (!String.IsNullOrEmpty(folder.Path) && folder.Path.StartsWith($"{SDDrive}game"))
                {
                    if (!folder.Path.StartsWith($"{SDDrive}game\\_"))
                    {
                        if (!String.IsNullOrEmpty(search))
                        {
                            if (folder.Title.ToUpper().Contains(search.ToUpper()))
                            {
                                addItem = true;
                            }
                        }
                        else
                        {
                            addItem = true;
                        }

                        if (addItem)
                        {
                            var node = new TreeNode(folder.Title);
                            node.Name = folder.Path;

                            if (folder.IsMissngInCollection)
                            {
                                node.ImageIndex = 1;

                            }

                            rootNode.Nodes.Add(node);
                        }
                    }

                }
                else
                {
                    if (!folder.IsFolder)
                    {
                        var node = new TreeNode(folder.Title);
                        node.Name = folder.Path;
                        var folderNode = findParentNodeOfGamePath(node);
                        if (folderNode != null)
                        {
                            if (!String.IsNullOrEmpty(search))
                            {
                                if (folder.Title.ToUpper().Contains(search.ToUpper()))
                                {
                                    addItem = true;
                                }
                            }
                            else
                            {
                                addItem = true;
                            }

                            if (addItem)
                            {
                                if (folder.IsMissngInCollection)
                                {
                                    node.ImageIndex = 1;

                                }

                                folderNode.Nodes.Add(node);
                            }
                        }
                    }
                }
            }

            tvCartridge.ExpandAll();
            rootNode.EnsureVisible();
        }

        private TreeNode findParentNodeOfGamePath(TreeNode node)
        {
            if (node.Name.StartsWith($"{SDDrive}folders\\"))
            {
                var i = node.Name.IndexOf("\\game\\");
                if (i >= 0)
                {
                    var key = node.Name.Substring(0, i + 1);
                    var nodeFound = tvCartridge.Nodes.Find(key, true);
                    if (nodeFound.Length > 0)
                    {
                        return nodeFound[0];
                    }
                }
                else
                {
                    return node;
                }
            }
            else if (node.Name.StartsWith($"{SDDrive}game\\"))
            {
                var root = tvCartridge.Nodes.Find(SDDrive, true);
                if (root.Length > 0)
                {
                    return root[0];
                }
            }
            else if (node.Name == SDDrive)
            {
                return node;
            }

            return null;
        }

        private async void selectSDDriveToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            selectSDDrive_Opening(true);
        }

        private void contextMenuStripMSD_Opening(object sender, CancelEventArgs e)
        {
            selectSDDrive_Opening(false);
        }

        private async void selectSDDrive_Opening(bool isMenu)
        {
            bool itemWasChecked = false;
            var drives = SDCardHelper.FindRemovableDrives();

            if (isMenu)
            {
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

                    foreach (var drive in drives)
                    {
                        var item = new ToolStripMenuItem(
                            $"{drive.Name} {string.Empty.PadLeft(30)} [Total size: {drive.TotalSize.ToSize()}]", null,
                            driveToolStripMenuItem_Click, drive.Name);

                        AddDrivePathItem(item);
                    }

                    if (!itemWasChecked)
                    {
                        await SelectSDDrive(null);
                    }
                }

                void AddDrivePathItem(ToolStripMenuItem item)
                {
                    item.Checked = SDDrive != null && item.Name == SDDrive;
                    itemWasChecked |= item.Checked;
                    selectSDDriveToolStripMenuItem.DropDownItems.Add(item);
                }
            }
            else
            {
                if (drives.Length == 0)
                {
                    await SelectSDDrive(null);

                    if (contextMenuStripMSD.Items.Count == 0
                        || contextMenuStripMSD.Items["notfound"] == null)
                    {
                        contextMenuStripMSD.Items.Clear();
                        var item = new ToolStripMenuItem("No removable drive(s) found...", null, null, "notfound");
                        item.ForeColor = Color.Red;
                        contextMenuStripMSD.Items.Add(item);
                    }
                }
                else
                {
                    contextMenuStripMSD.Items.Clear();

                    foreach (var drive in drives)
                    {
                        var item = new ToolStripMenuItem(
                            $"{drive.Name} {string.Empty.PadLeft(30)} [Total size: {drive.TotalSize.ToSize()}]", null,
                            driveToolStripMenuItem_Click, drive.Name);

                        AddDrivePathItem(item);
                    }

                    if (!itemWasChecked)
                    {
                        await SelectSDDrive(null);
                    }
                }

                void AddDrivePathItem(ToolStripMenuItem item)
                {
                    item.Checked = SDDrive != null && item.Name == SDDrive;
                    itemWasChecked |= item.Checked;
                    contextMenuStripMSD.Items.Add(item);
                }
            }
        }

        private async void btnImageClear_Click(object sender, EventArgs e)
        {
            if (_game != null)
            {
                //clear banner img
                if (Enum.TryParse<ImageType>(((Control)sender).Tag as string, ignoreCase: true, out ImageType imgType))
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
            IEnumerable<GameInfo> gameInfos = _gamesManager.Games; //default

            gbBoxArt.Visible = false;

            var romFeatureFilters = Enum.GetNames(typeof(RomFeatureFilter));
            if (romFeatureFilters.Contains(cbRomFilter.SelectedValue as string))
            {
                switch (Enum.Parse(typeof(RomFeatureFilter), cbRomFilter.SelectedValue as string))
                {
                    case RomFeatureFilter.SelectedForSync:
                        gameInfos = _gamesManager.Games.Where(g => g.IsSelected);
                        break;
                    case RomFeatureFilter.RecentlyAdded:
                        gameInfos = _gamesManager.Games.Where(g => g.IsRecentlyAdded);
                        break;
                    case RomFeatureFilter.RomsWithoutBanner:
                        gameInfos = _gamesManager.Games.Where(g => g.ImageBanner == null);
                        break;
                    case RomFeatureFilter.RomsWithoutBoxart:
                        gameInfos = _gamesManager.Games.Where(g => g.Image == null && g.Image1080 == null);
                        break;
                    case RomFeatureFilter.RomsWithoutDescription:
                        gameInfos = _gamesManager.Games.Where(g => g.romDescription == string.Empty);
                        break;
                    case RomFeatureFilter.RomsNotPresentOnCartridge:
                        gameInfos = _gamesManager.Games.Where(g => g.IsPresentOnCartridge == false);
                        break;
                        //default
                }
            }
            else if (int.TryParse(cbRomFilter.SelectedValue as string, out int platformId))
            {
                //filter on platform id
                gameInfos = _gamesManager.Games.Where(g => g.romPlatformId == platformId);
            }

            if (!String.IsNullOrEmpty(tbSearchCollection.Text))
            {
                gameInfos = gameInfos.Where(g => g.romTitle.ToUpper().Contains(tbSearchCollection.Text.ToUpper()));
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
            lvGames.Groups[0].Header = $"{lvGames.Items.Count} ROM(s)";
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
            using (var aboutBox = Program.ServiceProvider.GetRequiredService<AboutBox>())
            {
                aboutBox.ShowDialog(this);
            }
        }

        private void lvGames_MouseClick(object sender, MouseEventArgs e)
        {
            gbBoxArt.Visible = true;

            if (e.Button == MouseButtons.Right)
            {
                var focusedItem = lvGames.FocusedItem;
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                {
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private void ShowScrapingErrorMessageBox(Exception ex)
        {
            MessageBox.Show($"There were problems calling the Scraping API.\nError message: \"{ex.Message}\".\nInner exception message: \"{ex.InnerException?.Message}\"", "Scraping Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            var missingBiosFiles = _gamesManager.GetMissingBiosFiles(_game, includeOptionalBios: true);
            var missingFileList = string.Join(";", missingBiosFiles.Select(b => b.FileName));

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
                    Directory.CreateDirectory(platformRomsDir); //ensure directory exists
                    var missingBiosFile = missingBiosFiles.FirstOrDefault(b => b.FileName.ToLower() == Path.GetFileName(romFilePath).ToLower());
                    if (missingBiosFile?.MD5.Length > 0 && !missingBiosFile.MD5.Contains(HashHelper.CalculateHashcodes(romFilePath).Md5))
                    {
                        if (MessageBox.Show(
                            $"Uploaded BIOS file '{missingBiosFile.FileName}' doesn't have the expected MD5 hash {string.Join(" or ", missingBiosFile.MD5)}." +
                            "\nPlease try a different file.\n\n" +
                            "Clicking 'OK' will copy the required MD5 hash(es) to your clipboard.",
                            "BIOS file MD5 mismatch", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.OK)
                        {
                            Clipboard.SetText(string.Join(" ", missingBiosFile.MD5));
                        }

                        continue;
                    }
                    File.Copy(romFilePath, Path.Combine(platformRomsDir, Path.GetFileName(romFilePath)), overwrite: true);
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
                    && MessageBox.Show(
                        "This will overwrite the current ROM description and images with the scraped information.\nIs this OK?",
                        "Applying scraped game content",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.No)
                {
                    return;
                }

                lbScrapeResults.Tag = "WARNING_SHOWN";

                _game.GameInfoChanged -= Game_GameInfoChanged; //suppress

                _game.TgdbId = selectedResult.GameId;
                //_game.romTitle = selectedResult.Game.GameTitle;
                //lvGames.SelectedItems[0].Text = _gamesManager.GetRomListTitle(_game);
                _game.romDescription = selectedResult.Game.Overview;
                _game.romGenre = _romManager.MapToGenre(selectedResult.Game.Genres);
                _game.romPlayers = selectedResult.Game.Players.HasValue ? selectedResult.Game.Players.Value : 1; //default 1
                _game.romReleaseDate = selectedResult.Game.ReleaseDate.HasValue ? selectedResult.Game.ReleaseDate.Value.ToString("yyyy-MM-dd") : "";

                if (selectedResult.BoxArt != null)
                {
                    await _imageManager.ResizeImage($"{selectedResult.ImageBaseUrl}{selectedResult.BoxArt.FileName}", _game, _gamesManager.GetGameBoxartImageInfo(_game.Id));
                }

                if (selectedResult.Banners?.Length > 0)
                {
                    pbBanner.Tag = 0; //reset banner offset
                    await _imageManager.ResizeImage($"{selectedResult.ImageBaseUrl}{selectedResult.Banners[0].FileName}", _game, new[] { _gamesManager.GetGameImageInfo(_game.Id, ImageType.Banner) });
                }

                llBannerNext.Visible = llBannerPrev.Visible = (selectedResult.Banners?.Length > 1);

                await _gamesManager.SerializeGame(_game);
                SetDataBindings(refreshOnly: true);

                gbBoxArt.Visible = true;

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

        private void lvGames_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var allowedExtensions = _appSettings.Platforms.SelectMany(p => p.SupportedExtensions);
                if (files.Any(f => allowedExtensions.Contains(Path.GetExtension(f)?.ToLower())))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
        }

        private async void lvGames_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var allowedExtensions = _appSettings.Platforms.SelectMany(p => p.SupportedExtensions);
                await AddGames(files.Where(f => allowedExtensions.Contains(Path.GetExtension(f)?.ToLower())).ToArray());
            }
        }

        private void pbGameImage_DragEnter(object sender, DragEventArgs e)
        {
            if (_game == null) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length > 1 || !Path.GetExtension(files[0].ToLower()).In(".bmp", ".png", ".jpg", ".jpeg", ".gif")) return;
                e.Effect = DragDropEffects.Copy;
            }

            if (e.Data.GetDataPresent(DataFormats.Html))
            {
                var html = (string)e.Data.GetData(DataFormats.Html);
                if (html == null) return;
                if (Regex.IsMatch(html, "<img src=\"data:image/(bmp|jpeg|png|gif);base64,")
                    || Regex.IsMatch(html, "<img[^>]* src=\"https?://"))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }

        private async void pbGameImage_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                await ImportResizeImages(sender as Control, files[0]);
            }

            if (e.Data.GetDataPresent(DataFormats.Html))
            {
                var html = (string)e.Data.GetData(DataFormats.Html);
                if (Regex.IsMatch(html, "<img src=\"data:image/(bmp|jpeg|png|gif);base64,"))
                {
                    var ixBase64Start = html.IndexOf(";base64,") + ";base64,".Length;
                    var ixBase64End = html.IndexOf("\"", ixBase64Start);
                    var base64Img = html.Substring(ixBase64Start, ixBase64End - ixBase64Start);
                    var imgBytes = Convert.FromBase64String(base64Img);
                    await ImportResizeImages(sender as Control, null, imgBytes);
                }
                else
                {
                    var ixUrlStart = html.IndexOf(" src=\"http", html.IndexOf("<img ")) + " src=\"".Length;
                    var ixUrlEnd = html.IndexOf("\"", ixUrlStart);
                    var imgUrl = html.Substring(ixUrlStart, ixUrlEnd - ixUrlStart);
                    await ImportResizeImages(sender as Control, imgUrl);
                }
            }
        }

        private void tvCartridge_OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void tvCartridge_OnItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void tvCartridge_DragOver(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.  
            Point targetPoint = tvCartridge.PointToClient(new Point(e.X, e.Y));

            // Select the node at the mouse position.  
            tvCartridge.SelectedNode = tvCartridge.GetNodeAt(targetPoint);
        }

        private async void tvCartridge_OnDragDrop(object sender, DragEventArgs e)
        {
            TreeNode srcNode;

            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                srcNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");

                Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));

                TreeNode dstNode = ((TreeView)sender).GetNodeAt(pt);
                dstNode = findParentNodeOfGamePath(dstNode);

                if (dstNode != null)
                {
                    var success = await MoveGameOnSDCard(srcNode.Name, dstNode.Name);
                    if (success)
                    {
                        await SelectSDDrive(SDDrive);
                    }
                }
            }
        }

        #region toolstrip menu

        private void deleteSelectedGamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedGames();
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
                    //show error, but don't break the flow
                }

                _game.GameInfoChanged += Game_GameInfoChanged;
                SetDataBindings();
            }
        }

        private async void setCoreMenuItem_Click(object sender, EventArgs e)
        {
            var toolStripItem = sender as ToolStripItem;

            var firstGamePlatform = _gamesManager.GetGamePlatform((lvGames.SelectedItems[0] as ListViewItem).Name);
            List<GameInfo> selectedGames = new List<GameInfo>(lvGames.SelectedItems.Cast<ListViewItem>().Select(s => _gamesManager.GetGameById(s.Name)).Where(g => g != null));

            string retroArchCore = null; //null = internal emulator
            if (toolStripItem.Text.Contains(":"))
            {
                retroArchCore = firstGamePlatform.RetroArchCores.FirstOrDefault(c => c.DisplayName == toolStripItem.Text.Split(":")[1].Trim())?.CoreFileName;
            }

            //now set the internal/external core for all selected games
            foreach (var game in selectedGames)
            {
                if (game.RetroArchCore != retroArchCore)
                {
                    game.RetroArchCore = retroArchCore;
                    await _gamesManager.SerializeGame(game);
                }
            }

            SetDataBindings(); //update form UI
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            //clean previously dynamically added items
            for (int i = contextMenuStrip1.Items.Count; i > 3; i--) contextMenuStrip1.Items.RemoveAt(i - 1);
            if (lvGames.SelectedItems.Count == 0) return;

            //if all selectedItems share the same platform, show additional contextMenuItem to choose internal/external core
            var firstGamePlatform = _gamesManager.GetGamePlatform((lvGames.SelectedItems[0] as ListViewItem).Name);
            if (firstGamePlatform == null) return;
            HashSet<GameInfo> selectedGames = new HashSet<GameInfo>(lvGames.SelectedItems.Cast<ListViewItem>().Select(s => _gamesManager.GetGameById(s.Name)).Where(g => g != null));
            if (selectedGames.Any(g => g.romPlatformId != firstGamePlatform.Id)) return;

            if (firstGamePlatform.InternalEmulator != null)
            {
                contextMenuStrip1.Items.Add(new ToolStripSeparator());
                contextMenuStrip1.Items.Add("Selected ROM(s) -> Use Internal Emulator", null, setCoreMenuItem_Click);
            }

            if (firstGamePlatform.RetroArchCores.Length > 0)
            {
                contextMenuStrip1.Items.Add(new ToolStripSeparator());
                foreach (var core in firstGamePlatform.RetroArchCores)
                {
                    contextMenuStrip1.Items.Add("Selected ROM(s) -> Use RetroArch Core: " + core.DisplayName, null, setCoreMenuItem_Click);
                }
            }
        }

        private void contextMenuCartridge_Opening(object sender, CancelEventArgs e)
        {
            //clean previously dynamically added items
            for (int i = contextMenuCartridge.Items.Count; i > 0; i--) contextMenuCartridge.Items.RemoveAt(i - 1);
            if (_gamesManager.GamesOnSDCard.Count == 0) return;

            var currNode = tvCartridge.SelectedNode;
            if (currNode.Name.EndsWith("\\"))
            {
                if (!currNode.Name.Equals(SDDrive))
                {
                    contextMenuCartridge.Items.Add("Rename Folder", null, contextMenuCartridge_RenameFolder_Click);
                    contextMenuCartridge.Items.Add(new ToolStripSeparator());
                    contextMenuCartridge.Items.Add("Delete Folder", null, contextMenuCartridge_DeleteFolder_Click);
                }
            }
            else if (currNode.Name.EndsWith(".json"))
            {
                contextMenuCartridge.Items.Add("Delete Game from Cartridge", null, contextMenuCartridge_DeleteGame_Click);

                var gameRoot = _gamesManager.GetGameRootFromJsonPath(SDDrive, currNode.Name);
                var gameId = _gamesManager.GetGameIdFromJsonPath(gameRoot, currNode.Name);
                var exists = _gamesManager.GetGameById(gameId);
                if (exists == null)
                {
                    var extensionIsSupported = false;
                    try
                    {
                        var gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText($"{currNode.Name}"));
                        if (gameInfo != null)
                        {
                            extensionIsSupported = _appSettings.Platforms.SelectMany(p => p.SupportedExtensions).Distinct().Select(e => e).Contains(Path.GetExtension(gameInfo.romFileName));

                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    if (extensionIsSupported)
                    {
                        contextMenuCartridge.Items.Add(new ToolStripSeparator());
                        contextMenuCartridge.Items.Add("Import Game into ROM Collection", null, contextMenuCartridge_ImportGame_Click);
                    }
                }
            }
        }

        private async void contextMenuCartridge_RenameFolder_Click(object sender, EventArgs e)
        {
            var currNode = tvCartridge.SelectedNode;
            var newName = Prompt.ShowDialog("Please enter new Name:", $"Rename Folder \"{currNode.Text}\"");
            if (!String.IsNullOrEmpty(newName))
            {
                try
                {
                    var folderPath = currNode.Name;

                    var gameJsonPath = _gamesManager.GetGameJsonFromFolderPath(folderPath);
                    var gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText(gameJsonPath));
                    gameInfo.romTitle = newName;
                    File.WriteAllText(gameJsonPath, JsonConvert.SerializeObject(gameInfo, Formatting.Indented));

                    var gameCartridgeJson = $"{currNode.Name}\\cartridge.json";
                    var cartInfo = JsonConvert.DeserializeObject<Cartridge>(File.ReadAllText(gameCartridgeJson));
                    cartInfo.cartridgeName = newName;
                    File.WriteAllText(gameCartridgeJson, JsonConvert.SerializeObject(cartInfo, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    var err = ex;
                }

                await SelectSDDrive(SDDrive);
            }
        }

        private async void contextMenuCartridge_DeleteFolder_Click(object sender, EventArgs e)
        {
            var currNode = tvCartridge.SelectedNode;
            if (MessageBox.Show($"Are you sure you want to delete this Folder (\"{currNode.Text}\") from the Cartridge?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _gamesManager.DeleteGameFolderOnSD(new GameInfo { Id = currNode.Name }, SDDrive);
                await SelectSDDrive(SDDrive);
            }
        }

        private async void contextMenuCartridge_DeleteGame_Click(object sender, EventArgs e)
        {
            //tvCartridge.SelectedNode = tvCartridge.GetNodeAt(e.X, e.Y);
            var currNode = tvCartridge.SelectedNode;
            if (MessageBox.Show($"Are you sure you want to delete this ROM (\"{currNode.Text}\") from the Cartridge?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _gamesManager.DeleteGameFilesOnSD(new GameInfo { Id = currNode.Name }, _gamesManager.GetGameRootFromJsonPath(SDDrive, currNode.Name)); //  SDDrive
                await SelectSDDrive(SDDrive);
            }
        }

        private async void contextMenuCartridge_ImportGame_Click(object sender, EventArgs e)
        {
            var currNode = tvCartridge.SelectedNode;
            var gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText(currNode.Name));
            var filePath = $"{_gamesManager.GetGameRootFromJsonPath(SDDrive, currNode.Name)}game\\{gameInfo.romFileName}";
            var newGames = new List<string>();
            newGames.Add(filePath);
            await AddGames(newGames.ToArray());
        }
        #endregion toolstrip menu

        private async void btnNewSDCardFolder_Click(object sender, EventArgs e)
        {
            using (var form = new CreateNewFolder(SDDrive))
            {
                form.ShowDialog();
                if (form.JustCreatedFolderName != null && Directory.Exists(Path.Combine(Path.GetPathRoot(SDDrive), "folders", form.JustCreatedFolderName)))
                {
                    await SelectSDDrive(SDDrive);
                }
            }
        }
        public async Task<bool> MoveGameOnSDCard(string currentCartJsonFilePath, string newSDCardLocationPath)
        {
            var ret = true;

            try
            {
                var gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText(currentCartJsonFilePath));
                gameInfo.Id = _gamesManager.GetGameIdFromJsonPath(SDDrive, currentCartJsonFilePath);

                var success = await _gamesManager.CopyGameToSDPath(newSDCardLocationPath, gameInfo);
                if (!success)
                {
                    MessageBox.Show($"Could not move game to folder - perhaps the game doesn't exist in the collection?", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                _gamesManager.DeleteGameFilesOnSD(gameInfo, _gamesManager.GetGameRootFromJsonPath(SDDrive, currentCartJsonFilePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not sync to MicroSD. Error message: \"{ex.Message}\".", "Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                ret = false;
            }

            return ret;
        }

        private async void btnAddGame_Click(object sender, EventArgs e)
        {
            var supportedExtensions = String.Join("; ", _appSettings.Platforms
                .SelectMany(p => p.SupportedExtensions).Distinct().Select(e => $"*{e}"));

            OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = $"Game ROMs ({supportedExtensions})|{supportedExtensions}",
                Title = "Select Game ROM(s)"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                await AddGames(dialog.FileNames);
            }
        }

        private async void tvCartridge_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode dstNode = tvCartridge.GetNodeAt(e.X, e.Y);
                tvCartridge.SelectedNode = dstNode;

                contextMenuCartridge.Show(Cursor.Position);
                return;
            }
            else if (e.Button == MouseButtons.Left)
            {
                TreeNode dstNode = tvCartridge.GetNodeAt(e.X, e.Y);
                tvCartridge.SelectedNode = dstNode;
                var isFolder = false;
                var isRoot = false;
                var gameId = Path.GetFileNameWithoutExtension(dstNode.Name);

                if (String.IsNullOrEmpty(gameId))
                {
                    if (dstNode.Name.Equals(SDDrive))
                    {
                        isRoot = true;
                    }
                    else
                    {
                        gameId = "_" + _gamesManager.GetFolderIdFromPath(dstNode.Name);
                    }

                    isFolder = true;
                }

                var gameInfo = _gamesManager.GetGameJsonFromSDCardByGameId(gameId);
                if (!isRoot && (gameInfo == null || gameInfo.IsMissngOnCartridge))
                {
                    var msg = $"Missing: {Path.Combine(Path.GetPathRoot(dstNode.Name), "game", gameId + ".json")}";
                    lblNumberGamesSDCard.Text = $"{msg.Substring(0, 30)}...";
                    toolTipLabel.SetToolTip(lblNumberGamesSDCard, msg);

                    if (tvCartridge.SelectedNode != null)
                    {
                        tvCartridge.SelectedNode.ImageIndex = 3;
                        tvCartridge.SelectedNode.SelectedImageIndex = 3;
                    }
                }
                else if (!isFolder && gameInfo != null && gameInfo.IsMissngInCollection)
                {
                    var msg = $"Missing in Collection";
                    lblNumberGamesSDCard.Text = msg;
                    toolTipLabel.SetToolTip(lblNumberGamesSDCard, msg);
                    if (tvCartridge.SelectedNode != null)
                    {
                        tvCartridge.SelectedNode.ImageIndex = 1;
                        tvCartridge.SelectedNode.SelectedImageIndex = 1;
                    }
                }
                else
                {
                    UpdateTotalGamesOnSDCardLabel();
                    toolTipLabel.SetToolTip(lblNumberGamesSDCard, "");
                    if (isFolder)
                    {
                        if (tvCartridge.SelectedNode != null)
                        {
                            tvCartridge.SelectedNode.ImageIndex = 2;
                            tvCartridge.SelectedNode.SelectedImageIndex = 2;
                        }
                    }
                }

                var found = false;
                foreach (var groupItem in lvGames.Groups)
                {
                    var items = ((ListViewGroup)groupItem).Items;

                    foreach (var item in items)
                    {
                        var currItem = (ListViewItem)item;
                        currItem.Selected = false;
                        if (currItem.Name.ToLower().Equals(gameId.ToLower()))
                        {
                            gbBoxArt.Visible = true;

                            currItem.Selected = true;
                            currItem.EnsureVisible();

                            lvGames.Focus();

                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    cbRomFilter_SelectedIndexChanged(null, null);
                }
            }
        }

        private void tbSearchCollection_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                cbRomFilter_SelectedIndexChanged(sender, e);
            }
        }

        private void tbSearchCartridge_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BuildFolderTree(tbSearchCartridge.Text);
            }
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 120,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            //Label textLabel = new Label() { Left = 50, Top = 20, Width = 400, Text = text };
            TextBox textBox = new TextBox() { Left = 50, Top = 20, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 50, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 250, Width = 100, Top = 50, DialogResult = DialogResult.Cancel };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) =>
            {
                prompt.Close();
            };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            //prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }


}