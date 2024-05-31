using EverLoader.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EverLoader.Extensions;
using System.Drawing;
using System.Text.RegularExpressions;
using EverLoader.Helpers;
using System.Threading.Tasks;
using TheGamesDBApiWrapper.Models.Enums;
using System.Globalization;
using System.Windows.Forms;
using TheGamesDBApiWrapper.Domain;
using EverLoader.Properties;
using EverLoader.Forms;
using System.Text;
using System.IO.Compression;

namespace EverLoader.Services
{
    public class GamesManager
    {
        private const int MAX_GAME_ID_LENGTH = 16;
        private readonly string APP_GAMES_FOLDER = $"{Constants.APP_ROOT_FOLDER}games\\";
        private const string SUBFOLDER_IMAGES = "images\\";
        private const string SUBFOLDER_IMAGES_SOURCE = "images\\source\\";
        private const string SUBFOLDER_ROM = "rom\\";

        private readonly Dictionary<string, GameInfo> _games = new Dictionary<string, GameInfo>();
        private readonly HashSet<uint> _gameCRCs = new HashSet<uint>(); //used for quick lookup of existing CRCs

        private readonly ITheGamesDBAPI _tgdbApi;
        private readonly RomManager _romManager;
        private readonly ImageManager _imageManager;
        private readonly DownloadManager _downloadManager;
        private readonly AppSettings _appSettings;

        public GamesManager(ITheGamesDBAPI tgdbApi,
            RomManager romManager,
            ImageManager imageManager,
            DownloadManager downloadManager,
            AppSettings appSettings)
        {
            _tgdbApi = tgdbApi;
            _romManager = romManager;
            _imageManager = imageManager;
            _downloadManager = downloadManager;
            _appSettings = appSettings;

            //create games folder directory
            Directory.CreateDirectory(APP_GAMES_FOLDER);
        }

        public IEnumerable<GameInfo> Games => _games.Values;
        public Dictionary<string, GameInfo> GamesDictionary => _games;

        public List<GameInfoTreeNode> GamesOnSDCard = new List<GameInfoTreeNode>();

        public IEnumerable<ImageInfo> GetGameBoxartImageInfo(string gameId)
        {
            return new[]
            {
                GetGameImageInfo(gameId, ImageType.Large),
                GetGameImageInfo(gameId, ImageType.Medium),
                GetGameImageInfo(gameId, ImageType.Small),
            };
        }

        public ImageInfo GetGameImageInfo(string gameId, ImageType imageType)
        {
            switch (imageType)
            {
                case ImageType.Medium:
                    return new ImageInfo()
                    {
                        //Size = new Size(210, 295),
                        Size = new Size(260, 358),
                        LocalPath = $"{APP_GAMES_FOLDER}{gameId}\\{SUBFOLDER_IMAGES}{gameId}0_hd.png",
                        ImageType = imageType
                    };
                case ImageType.Large:
                    return new ImageInfo()
                    {
                        Size = new Size(474, 666),
                        LocalPath = $"{APP_GAMES_FOLDER}{gameId}\\{SUBFOLDER_IMAGES}{gameId}0_1080.png",
                        ImageType = imageType
                    };
                case ImageType.Banner:
                    return new ImageInfo()
                    {
                        Size = new Size(1920, 551),
                        LocalPath = $"{APP_GAMES_FOLDER}{gameId}\\{SUBFOLDER_IMAGES}{gameId}_gamebanner.png",
                        ImageType = imageType
                    };
                default:
                case ImageType.Small:
                    return new ImageInfo()
                    {
                        Size = new Size(112, 157),
                        LocalPath = $"{APP_GAMES_FOLDER}{gameId}\\{SUBFOLDER_IMAGES}{gameId}0.png",
                        ImageType = imageType
                    };
            }
        }

        public GameInfo GetGameById(string id)
        {
            return _games.TryGetValue(id, out GameInfo value) ? value : null;
        }

        public Platform GetGamePlatform(GameInfo game)
        {
            return game != null ? _appSettings.Platforms.FirstOrDefault(p => p.Id == game.romPlatformId) : null;
        }

        public Platform GetGamePlatform(string gameId)
        {
            return GetGamePlatform(GetGameById(gameId));
        }

        public IEnumerable<Platform> GetExistingGamePlatforms()
        {
            var platformIds = Games.Select(g => g.romPlatformId).Distinct();
            return _appSettings.Platforms.Where(p => platformIds.Contains(p.Id));
        }

        //this returns one or multiple platforms that support a specific ROM file extension
        public IEnumerable<Platform> GetGamePlatformsByRomExtesion(string extension)
        {
            if (extension == null) return null;
            return _appSettings.Platforms.Where(p => p.SupportedExtensions.Contains(extension));
        }

        public async Task SerializeGame(GameInfo game)
        {
            if (string.IsNullOrEmpty(game.romPlatform))
            {
                game.romPlatform = GetGamePlatform(game)?.GroupAndName; //updating romPlatform won't trigger update events
                PreselectGameCore(game);
            }

            game.AppVersion = Application.ProductVersion;

            var gameJson = JsonConvert.SerializeObject(game, Formatting.Indented);
            await File.WriteAllTextAsync($"{APP_GAMES_FOLDER}{game.Id}\\{game.Id}.json", gameJson, new UTF8Encoding(false));
        }

        public string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Trim().Split(Path.GetInvalidFileNameChars()));
        }

        public BiosFile[] GetMissingBiosFiles(GameInfo game, bool includeOptionalBios)
        {
            var platform = GetGamePlatform(game);
            if (platform?.BiosFiles != null)
            {
                List<BiosFile> missingBiosFiles = new List<BiosFile>();
                foreach (var biosFile in platform.BiosFiles.Where(b => b.Required || includeOptionalBios))
                {
                    if (biosFile.SupportedExtensions.Length > 0 && !biosFile.SupportedExtensions.Contains(Path.GetExtension(game.romFileName))) continue;

                    if (!File.Exists($"{Constants.APP_ROOT_FOLDER}bios\\{platform.Alias}\\{biosFile.FileName}"))
                    {
                        missingBiosFiles.Add(biosFile);
                    }
                }
                return missingBiosFiles.ToArray();
            }
            return new BiosFile[0];
        }

        public bool SdContainsKnownGames(string sdDrive)
        {
            var sdGamesDir = new DirectoryInfo($"{sdDrive}game");
            if (!sdGamesDir.Exists) return false;

            return sdGamesDir.EnumerateFiles("*.json")
                .Select(j => Path.GetFileNameWithoutExtension(j.Name))
                .Any(i => _games.ContainsKey(i));
        } 

        public GameInfoTreeNode GetGameJsonFromSDCardByGameId(string gameId)
        {
            var ret = GamesOnSDCard.Where(g => g.Id == gameId).ToList().FirstOrDefault();

            return ret;
        }
        public async Task<bool>   CopyGameToSDPath(string sdDrive, GameInfo gameParam)
        {
            bool ret = true;
            if (gameParam.Id is null)
            {
                gameParam.Id = GenerateGameId(gameParam.romTitle,true);//  Path.GetFileName(gameParam.romFileName);
            }

            var game = GetGameById(gameParam.Id);
            var platform = _appSettings.Platforms.SingleOrDefault(p => p.Id == game.romPlatformId);
            if (platform == null) return false;

            game.FixRomLaunchType(); //fix old romLaunchType values

            // RA cores should go to the SD root path, not to subfolders
            var targetCorePath = game.RetroArchCore == null ? sdDrive : Path.GetPathRoot(sdDrive);

            // 4a. copy emulator core (only overwrite if newer) + bios files (only overwrite if newer)
            //first select the right core (note: megadrive has an 'empty' BlastRetro core, as it uses BlastEm emulator)
            var selectedCore = game.RetroArchCore == null
                ? platform.InternalEmulator
                : platform.RetroArchCores.OrderBy(c => c.CoreFileName == game.RetroArchCore ? 0 : 1).FirstOrDefault();

            if (selectedCore != null)
            {
                foreach (var file in selectedCore.Files)
                {
                    var destFilePath = $"{targetCorePath}{file.TargetPath}".Replace("[game.Id]", game.Id);
                    if (!File.Exists(destFilePath)) Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)); //ensure target dir exists
                    if (file.SourceContent != null)
                    {
                        await File.WriteAllLinesAsync(destFilePath, file.SourceContent);
                    }
                    else
                    {
                        var sourceFile = new FileInfo(await _downloadManager.GetDownloadedFilePath(new Uri(file.SourceUrl), file.SourcePath));
                        sourceFile.CopyToOverwriteIfNewer(destFilePath);
                    }
                }

                //copy over BIOS files
                foreach (var biosFile in platform.BiosFiles)
                {
                    var sourceBiosFile = new FileInfo($"{Constants.APP_ROOT_FOLDER}bios\\{platform.Alias}\\{biosFile.FileName}");
                    //bios files go into /sdcard/bios (for internal emulator) or /sdcard/retroarch/system (for RA cores)
                    var destBiosFilePath = $"{targetCorePath}{(game.RetroArchCore == null ? "bios" : "retroarch\\system")}\\{biosFile.FileName}";
                    if (sourceBiosFile.Exists)
                    {
                        if (!File.Exists(destBiosFilePath)) Directory.CreateDirectory(Path.GetDirectoryName(destBiosFilePath)); //ensure target dir exists
                        sourceBiosFile.CopyToOverwriteIfNewer(destBiosFilePath);
                    }
                }
            }

            // 4b. copy all image files (only overwrite if newer)
            var imagesDir = new DirectoryInfo($"{APP_GAMES_FOLDER}{game.Id}\\{SUBFOLDER_IMAGES}");
            if (imagesDir.Exists) imagesDir.GetFiles().ToList().ForEach(f =>
            {
                f.CopyToOverwriteIfNewer($"{sdDrive}game\\{f.Name}");
            });

            // 4c. copy over rom file (only overwrite if newer)
            var targetRomDir = game.RetroArchCore != null
                    ? $"roms\\{game.Id}"                    /* for RetroArch, put all roms under /sdcard/roms */
                    : (platform.Id == 1 ? "mame" : "game"); /* internal emulator: arcade roms to /sdcard/mame, otherwise use /sdcard/game */
            Directory.CreateDirectory($"{sdDrive}{targetRomDir}"); //ensure target directory exists on MicroSD card
            var sourceRomDir = new DirectoryInfo($"{APP_GAMES_FOLDER}{game.Id}\\{SUBFOLDER_ROM}");
            if (sourceRomDir.Exists) sourceRomDir.GetFiles().ToList().ForEach(f =>
            {
                var targetRomFileName = game.RetroArchCore != null
                    ? f.Name /* for RA, use original filename */
                    : (Path.GetExtension(f.Name.ToLower()) == Path.GetExtension(game.romFileName) ? game.romFileName : f.Name);
                var targetFile = $"{sdDrive}{targetRomDir}\\{targetRomFileName}";
                f.CopyToOverwriteIfNewer(targetFile);
            });

            // multidisc without a m3u? => create a m3u
            string multiDiscFilePath = null;
            if (game.IsMultiDisc && !game.romFileName.EndsWith(".m3u"))
            {
                multiDiscFilePath = $"{sdDrive}{targetRomDir}\\{RemoveInvalidChars(game.romTitle)}.m3u";
                var cueFileFound = sourceRomDir.GetFiles().Any(f => f.Extension?.ToLower() == ".cue");
                File.WriteAllLines(multiDiscFilePath, cueFileFound
                    ? sourceRomDir.GetFiles().Where(f => f.Extension?.ToLower() == ".cue").Select(f => f.Name)
                    : sourceRomDir.GetFiles().Select(f => f.Name));
            }

            //custom handling for cores without autolaunch
            var gameJson = JsonConvert.SerializeObject(game, Formatting.Indented);
            var evercadeGameInfo = new EvercadeGameInfo(gameJson);
            if (selectedCore?.AutoLaunch == false)
            {
                //clean up any old-style 0-byte pointer file if it exists
                if (File.Exists($"{sdDrive}game\\{game.Id}")) File.Delete($"{sdDrive}game\\{game.Id}");

                // for internal Arcade/MAME, we have special way of launching using .cue file
                if (game.RetroArchCore == null && platform.Id == 1)
                {
                    File.WriteAllText($"{sdDrive}game\\{game.Id}.cue", game.PreferedRomFileName());
                    evercadeGameInfo.romFileName = $"{game.Id}.cue";
                }
                else
                {
                    //no auto launcher, so write out special script and 0-byte pointer file
                    var pointerFileName = $"{game.Id}-";
                    evercadeGameInfo.romFileName = pointerFileName;
                    File.Create($"{sdDrive}game\\{pointerFileName}").Dispose(); //create 0-byte pointer file

                    Directory.CreateDirectory($"{sdDrive}special"); //ensure special directory exists

                    string shScript = game.RetroArchCore != null ? Resources.special_bash_ra : Resources.special_bash;
                    string romFileName = multiDiscFilePath != null ? Path.GetFileName(multiDiscFilePath) : game.PreferedRomFileName();
                    string romFileRelativePath = game.RetroArchCore == null ? romFileName : $"{game.Id}/{romFileName}";
                    shScript = shScript
                        .Replace("{CORE_FILENAME}", selectedCore.CoreFileName)
                        .Replace("{ROM_FILENAME}", romFileRelativePath)
                        .Replace("\r", ""); //remove possible windows CR

                    await File.WriteAllTextAsync($"{sdDrive}special\\{pointerFileName}.sh", shScript, new UTF8Encoding(false));
                }
            }
            //now write out evercade game json
            var evercadeGameJson = JsonConvert.SerializeObject(evercadeGameInfo, Formatting.Indented);
            await File.WriteAllTextAsync($"{sdDrive}game\\{game.Id}.json", evercadeGameJson, new UTF8Encoding(false));
            
            return ret;
        }
    
        public async Task SyncToSd(string sdDrive, string cartName, string gameId, IProgress<(string, int, int)> progress)
        {
            var selectedGameIds = _games.Values.Where(g => g.IsSelected).Select(g => g.Id).ToArray();
            
            if (  gameId.Length > 0)
            {
                selectedGameIds = new List<string>
                {
                    gameId
                }.ToArray();
            }

            // 1. write cartridge.json
            var cartJson = JsonConvert.SerializeObject(new Cartridge()
            {
                cartridgeName = cartName
            }, Formatting.Indented);
            await File.WriteAllTextAsync($"{sdDrive}cartridge.json", cartJson);

            // 2. ensure the game directory exists on MicroSD
            Directory.CreateDirectory($"{sdDrive}game");

            int syncedGameIndex = 0;
            // 4. Save assets for each selected game
            foreach (var game in _games.Values.Where(g => g.IsSelected)) 
            {
                progress.Show(("Syncing Games", ++syncedGameIndex, selectedGameIds.Length));
                var platform = _appSettings.Platforms.SingleOrDefault(p => p.Id == game.romPlatformId);
                if (platform == null) return;

                var exists = GetGameJsonFromSDCardByGameId(game.Id);
                 var gameFolder = sdDrive;
                if (exists != null)
                {
                    gameFolder = GetGameRootFromJsonPath(sdDrive, exists.Path);
                }

                await CopyGameToSDPath(gameFolder, game);

                game.IsSelected = false;
            }
        }

        private string GenerateGameId(string title, bool ignoreDupicates)
        {
            //remove text within parenthesis
            var cleanTitle = Regex.Replace(title, @"\([^)]*\)", "");

            //filter out stopwords "the", "and" and "a"
            cleanTitle = string.Join(" ", cleanTitle.Trim().ToLower()
                .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !s.In("the", "and", "a")));

            //filter out non-alphanumeric and maximum 16 chars
            cleanTitle = Regex.Replace(cleanTitle, "[^a-zA-Z0-9]", String.Empty);
            cleanTitle = cleanTitle.Substring(0, Math.Min(cleanTitle.Length, MAX_GAME_ID_LENGTH));

            int addedNumber = 2; //start with 2
            while (!ignoreDupicates && _games.ContainsKey(cleanTitle))
            {
                cleanTitle = cleanTitle.Substring(0, (cleanTitle+"_").IndexOf("_"));
                //replace last chars with a number
                cleanTitle = $"{cleanTitle}_{addedNumber++}";
                if (cleanTitle.Length > MAX_GAME_ID_LENGTH)
                {
                    cleanTitle = cleanTitle.Remove(cleanTitle.IndexOf("_")-1, 1);
                }
            }
            return cleanTitle;
        }

        private IEnumerable<string> GetFilesFromCue(string cueFilePath)
        {
            var metaFiles = new List<string>();
            foreach (var line in File.ReadAllLines(cueFilePath))
            {
                if (!line.Trim().StartsWith("FILE \"") || line.Split('"').Length != 3) continue;
                metaFiles.Add(Path.Combine(Path.GetDirectoryName(cueFilePath), line.Split('"')[1]));
            }
            return metaFiles;
        }

        private IEnumerable<string> GetFilesFromM3u(string m3uFilePath)
        {
            var metaFiles = new List<string>();
            foreach (var line in File.ReadAllLines(m3uFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                metaFiles.Add(Path.Combine(Path.GetDirectoryName(m3uFilePath), line));
            }
            return metaFiles;
        }

        /// <summary>
        /// Creates a new GameInfo and adds this to your local games database
        /// TODO: match CRC with local gamelist.xml and call out to TGB for game metadata
        /// </summary>
        /// <param name="romPath"></param>
        public async Task<IEnumerable<GameInfo>> ImportGamesByRom(string[] romPaths, IProgress<(string, int, int)> progress)
        {
            var validRomPaths = romPaths.OrderBy(p => p).ToList();

            bool IsValidCue(string cueFilePath)
            {
                bool cueSheetValid = true;
                foreach (var filePath in GetFilesFromCue(cueFilePath))
                {
                    validRomPaths.Remove(filePath);
                    cueSheetValid &= File.Exists(filePath);
                }
                return cueSheetValid;
            }

            bool IsValidM3u(string m3uFilePath)
            {
                bool m3uValid = true;
                foreach (var filePath in GetFilesFromM3u(m3uFilePath))
                {
                    validRomPaths.Remove(filePath);
                    m3uValid &= File.Exists(filePath) && (filePath.ToLower().EndsWith(".cue") || IsValidCue(filePath));
                }
                return m3uValid;
            }

            //Validation: check all the .m3u and .cue metadata files to see if all included files are available
            foreach (var m3uFilePath in romPaths.Where(p => p.ToLower().EndsWith(".m3u")))
            {
                if (!IsValidM3u(m3uFilePath)) validRomPaths.Remove(m3uFilePath);
            }
            foreach (var cueFilePath in romPaths.Where(p => p.ToLower().EndsWith(".cue")))
            {
                if (!IsValidCue(cueFilePath)) validRomPaths.Remove(cueFilePath);
            }

            var newGames = new List<GameInfo>();
            int importedGames = 0;

            string multiDiscGameId = null;
            string multiDiscBaseTitleStart = null;
            string multiDiscBaseTitleEnd = null;

            foreach (var romPathEntry in validRomPaths)
            {
                var romPath = romPathEntry;
                progress.Show(("Importing game(s)", ++importedGames, validRomPaths.Count));

                var romExtracted = false;
                var romTmpFilePath = "";

                var f = new FileInfo(romPath);

                if (!f.Exists || f.Length == 0)
                {
                    var msg = $"Trying to add this file to collection:\n\n{romPath}\n\nBut it doesn't exist!\n\n";
                    msg += "Do you want to select the correct file?";

                    var answer = MessageBox.Show(msg, "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (answer == DialogResult.Yes)
                    {
                        OpenFileDialog dialog = new OpenFileDialog()
                        {
                            Multiselect = false,
                            Filter = $"*.*|*.*",
                            Title = "Select ROM file",
                            InitialDirectory = Path.GetDirectoryName(romPath),
                            FileName = romPath
                        };

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            romPath = dialog.FileNames[0];
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                if (Path.GetExtension(romPath).ToLower() == ".zip")
                {
                    string tempFileName = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
                    string tempPath = Path.GetTempPath();

                    using (var file = new FileStream(romPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        try
                        {
                            using (var zip = new ZipArchive(file, ZipArchiveMode.Read,true))
                            {
                                var extList = new List<string>();

                                var possiblePlatforms = _appSettings.Platforms.Select(p => p.SupportedExtensions ).ToList();
                                foreach (var possiblePlatform in possiblePlatforms)
                                {
                                    foreach (var platform in possiblePlatform)
                                    {
                                        extList.Add(platform);
                                    }
                                }

                                //check if a console rom is present in archive and extract it before adding rom to collection
                                var exists2 = zip.Entries.Select(k => k.Name).Where( j =>  extList.Contains(Path.GetExtension(j))).ToList();
                                if (exists2 .Count  > 0)
                                {
                                    var zipPath = Path.Combine(tempPath, tempFileName);
                                    zip.ExtractToDirectory(zipPath);
                                    romTmpFilePath = Path.Combine(zipPath, exists2.First());
                                    romExtracted = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            /* suppress scraping errors during rom import */
                        }
                    }
                }
            
                if (romExtracted)
                {
                    romPath = romTmpFilePath;
                }
               

                //await Task.Run(() => zip..ExtractToDirectory(tempPath, true));

                //1. don't add rom if CRC is already in collection
                (var romCRC32, var romMD5) = HashHelper.CalculateHashcodes(romPath);
                var crc32 = uint.Parse(romCRC32, NumberStyles.HexNumber);
                if (_gameCRCs.Contains(crc32)) continue;

                //2. calculate unique code
                var title = Path.GetFileNameWithoutExtension(romPath).Trim();
                var ext = Path.GetExtension(romPath).ToLower();
                if (ext != ".zip") title = title.Replace("_", " "); //for non-MAME roms, get rid of underscores
                title = Regex.Replace(title, @"\s+", " "); //replace multiple whitespace chars by a single space
                var newId = GenerateGameId(title, false);
                var newRomFileName = $"{newId}{ext}";
                var originalRomFileName = $"{Path.GetFileNameWithoutExtension(romPath)}{ext}";

                //fix for Quake .pak files
                if (ext == ".pak")
                {
                    title = "Quake";
                    newId = "tyrquake";
                    originalRomFileName = originalRomFileName.ToLower();
                    if (originalRomFileName != "pak0.pak")
                    {
                        if (Directory.Exists($"{APP_GAMES_FOLDER}{newId}\\{SUBFOLDER_ROM}"))
                        {
                            File.Copy(romPath, $"{APP_GAMES_FOLDER}{newId}\\{SUBFOLDER_ROM}{originalRomFileName}", overwrite: true);
                        }
                        continue;
                    }
                }

                //handle multi-disc files:
                // ... (Disc 1)
                // ... - disk 2
                // ... - D3
                // ... (Disk 4 of 5)
                // ... (D5)
                // ... disk6
                if (Regex.IsMatch(title, @"[\(\s-]d(is[c|k]\s?)?[0-9]+\b", RegexOptions.IgnoreCase))
                {
                    var disc1Match = Regex.Match(title, @"[\(\s-]d(is[c|k]\s?)?1\b", RegexOptions.IgnoreCase);
                    if (disc1Match.Success)
                    {
                        multiDiscGameId = newId;
                        //get the title without the Disc-number part, which is used for matching consecutive disc files
                        multiDiscBaseTitleEnd = title.Substring(disc1Match.Index + disc1Match.Length);
                        multiDiscBaseTitleStart = title.Substring(0, disc1Match.Index); //update title so it won't display the "(disk 1 of ..."
                        title = multiDiscBaseTitleStart.Trim();
                    }
                    else if (multiDiscBaseTitleStart != null && title.StartsWith(multiDiscBaseTitleStart) && title.EndsWith(multiDiscBaseTitleEnd))
                    {
                        File.Copy(romPath, $"{APP_GAMES_FOLDER}{multiDiscGameId}\\{SUBFOLDER_ROM}{originalRomFileName}", overwrite: true);
                        if (ext == ".cue")
                        {
                            CopyFilesFromCue(cueFilePath: romPath, $"{APP_GAMES_FOLDER}{multiDiscGameId}\\{SUBFOLDER_ROM}");
                        }
                        continue; //after copying a consecutive disc, we are done here
                    }
                }
                else
                {
                    multiDiscGameId = multiDiscBaseTitleStart = multiDiscBaseTitleEnd = null;
                }

                //set the platformId if the extension can be mapped to a single platform
                var mappedPlatforms = _appSettings.Platforms.Where(p => p.SupportedExtensions.Contains(ext));
                var mappedPlatform = mappedPlatforms.Count() == 1 ? mappedPlatforms.First() : null;

                //if platform is MAME/Arcade, replace mame-name with real name
                if (mappedPlatform?.Id == 1 && _romManager.MameNames.ContainsKey(title))
                {
                    title = _romManager.MameNames[title];
                }

                //create minimal GameInfo information
                var newGame = new GameInfo()
                {
                    Id = newId,
                    romTitle = title,
                    romFileName = mappedPlatform?.Id == 1 ? originalRomFileName : newRomFileName, //stock emulator rom file name (for RA we use the original filename)
                    romPlatformId = mappedPlatform?.Id ?? 0,
                    romCRC32 = romCRC32,
                    romMD5 = romMD5,
                    OriginalRomFileName = originalRomFileName,
                    IsRecentlyAdded = true,
                    IsMultiDisc = (newId == multiDiscGameId) || ext == ".m3u"
                };

                //4. create required folders
                Directory.CreateDirectory($"{APP_GAMES_FOLDER}{newGame.Id}\\{SUBFOLDER_IMAGES_SOURCE}"); //this also creates the images subfolder
                Directory.CreateDirectory($"{APP_GAMES_FOLDER}{newGame.Id}\\{SUBFOLDER_ROM}");

                //5. copy over the rom file
                File.Copy(romPath, $"{APP_GAMES_FOLDER}{newGame.Id}\\{SUBFOLDER_ROM}{originalRomFileName}", overwrite: true);
                //5b. for cue or m3u files, copy over all contained FILEs
                if (ext == ".m3u")
                {
                    CopyFilesFromM3u(m3uFilePath: romPath, $"{APP_GAMES_FOLDER}{newGame.Id}\\{SUBFOLDER_ROM}");
                }
                if (ext == ".cue")
                {
                    CopyFilesFromCue(cueFilePath: romPath, $"{APP_GAMES_FOLDER}{newGame.Id}\\{SUBFOLDER_ROM}");
                }

                await SerializeGame(newGame);

                _games.Add(newId, newGame);
                _gameCRCs.Add(crc32);

                newGames.Add(newGame);

                if (romExtracted)
                {
                    Directory.Delete( Path.GetDirectoryName( romTmpFilePath), true);
                }
            }

            if (!newGames.Any()) return newGames;

            try
            {
                await EnrichGames(newGames, progress);
            }
            catch {  /* suppress scraping errors during rom import */ }

            await EnsurePlatform(newGames, progress);

            

            return newGames;
            //note: MainForm will take care of adding the new game to the UI
        }

        void CopyFilesFromCue(string cueFilePath, string targetFolder)
        {
            foreach (var filePath in GetFilesFromCue(cueFilePath))
            {
                File.Copy(filePath, Path.Combine(targetFolder, Path.GetFileName(filePath)), overwrite: true);
            }
        }

        void CopyFilesFromM3u(string m3uFilePath, string targetFolder)
        {
            foreach (var filePath in GetFilesFromM3u(m3uFilePath))
            {
                File.Copy(filePath, Path.Combine(targetFolder, Path.GetFileName(filePath)), overwrite: true);
                if (Path.GetExtension(filePath).ToLower() == ".cue")
                {
                    CopyFilesFromCue(cueFilePath: filePath, targetFolder);
                }
            }
        }

        public void DeleteObsoleteRomFilesOnSD(GameInfo game, string sdDrive)
        {
            var gameId = game.Id;
            if (game.RetroArchCore == null)
            {
                var romsGameDir = $"{sdDrive}roms\\{gameId}";
                // using internal emulator, so remove possible game files in /roms folder
                if (Directory.Exists(romsGameDir)) Directory.Delete(romsGameDir, true);
            }
            else
            {
                // using RA emulator, so remove possible game files in /game and /mame folder
                var romGameFilePath = $"{sdDrive}game\\{game.romFileName}";
                var romMameFilePath = $"{sdDrive}mame\\{game.romFileName}";
                if (File.Exists(romGameFilePath))
                {
                    if (romGameFilePath.EndsWith(".cue"))
                    {
                        GetFilesFromCue(romGameFilePath).ToList().ForEach(f => File.Delete(f));
                    }
                    File.Delete(romGameFilePath);
                }
                if (File.Exists(romMameFilePath)) File.Delete(romMameFilePath);
            }
        }

        /// <summary>
        /// returns a list of relative filepaths used by game.
        /// these start with game\.., mame\.. or special\..
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        
        public void DeleteGameFilesOnSD(GameInfo game, string sdDrive)
        {
            var gameId = GetGameIdFromJsonPath(sdDrive, game.Id);
            if (gameId == null)
            {
                gameId = game.Id;
            }

            //if there is a mame cue file, get info from it and delete the rom from /sdcard/mame dir
            if (File.Exists($"{sdDrive}game\\{gameId}.cue"))
            {
                var mameFileName = File.ReadAllTextAsync($"{sdDrive}game\\{gameId}.cue");
                var mameFilePath = $"{sdDrive}mame\\{mameFileName}";
                if (File.Exists(mameFilePath)) File.Delete(mameFilePath);
            }

            //remove game files in /roms folder
            if (Directory.Exists($"{sdDrive}roms\\{gameId}")) Directory.Delete($"{sdDrive}roms\\{gameId}", true);

            //remove special script
            var specialScriptPath = $"{sdDrive}special\\{gameId}-.sh";
            if (File.Exists(specialScriptPath)) File.Delete(specialScriptPath);

            //delete screenshots
            var screenshotDir = $"{sdDrive}game\\{gameId}";
            if (Directory.Exists(screenshotDir)) Directory.Delete(screenshotDir, true);

            var sdGameDir = new DirectoryInfo($"{sdDrive}game");
            sdGameDir.EnumerateFiles($"{gameId}.*") //json, rom, saveslot files, and possiblye cue file or (old style) 0-byte special pointer file
                .Concat(sdGameDir.EnumerateFiles($"{gameId}-.*")) //0-byte pointer file
                .Concat(sdGameDir.EnumerateFiles($"{gameId}0*.png")) //artwork gfx
                .Concat(sdGameDir.EnumerateFiles($"{gameId}1*.png")) //artwork gfx
                .Concat(sdGameDir.EnumerateFiles($"{gameId}2*.png")) //artwork gfx
                .Concat(sdGameDir.EnumerateFiles($"{gameId}_gamebanner.png")) //optional banner gfx
                .ToList().ForEach(f => {
                    File.Delete(f.FullName);
                });
        }
        public void DeleteGameFolderOnSD(GameInfo game)
        {
            var t = Path.GetDirectoryName(game.Id);
            Directory.Delete(game.Id, true);

        }
        public void DeleteGameByIds(IEnumerable<string> ids)
        {
            foreach (string id in ids)
            {
                Directory.Delete($"{APP_GAMES_FOLDER}{id}", recursive:true);

                //now delete from memory and CRC dictionary
                if (_games.Remove(id, out GameInfo game) && uint.TryParse(game.romCRC32, NumberStyles.HexNumber, null, out uint crc32))
                {
                    _gameCRCs.Remove(crc32);
                }
            }
        }

        public static string GetSourceImagePath(string imagePath)
        {
            return Path.Combine(Path.GetDirectoryName(imagePath), "source", Path.GetFileName(imagePath));
        }

        /// <summary>
        /// Reads games from app games filder
        /// </summary>
        public async Task ReadGames(IProgress<(string, int, int)> progress)
        {
            //reads games from database folder
            if (!Directory.Exists(APP_GAMES_FOLDER)) return;

            var allGameDirs = new DirectoryInfo(APP_GAMES_FOLDER).GetDirectories();
            var totalGameDirs = allGameDirs.Length;
            var loadedGamesCount = 0;

            foreach (var gameDir in allGameDirs) 
            {
                progress.Show(("Loading games", ++loadedGamesCount, totalGameDirs));

                var jsonFilePath = $"{gameDir.FullName}\\{gameDir.Name}.json";
                if (!File.Exists(jsonFilePath)) continue;

                try
                {
                    //TODO: before skipping invalid game folder, maybe clean it up first

                    var gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText(jsonFilePath));
                    if (gameInfo.Id != gameDir.Name) continue; //skip 

                    //rom MUST exist
                    var romsDir = new DirectoryInfo($"{APP_GAMES_FOLDER}{gameInfo.Id}\\{SUBFOLDER_ROM}");
                    if (!romsDir.Exists || romsDir.GetFiles().Length == 0) continue; //skip 

                    //fix changed platform mappings
                    foreach (var remap in _appSettings.PlatformRemappings)
                    {
                        if (gameInfo.romPlatformId == remap.OldPlatformId && gameInfo.romPlatform == remap.OldPlatformName)
                        {
                            gameInfo.romPlatformId = remap.NewPlatformId;
                            gameInfo.romPlatform = _appSettings.Platforms.First(p => p.Id == remap.NewPlatformId).Name;
                            await SerializeGame(gameInfo); //and update
                        }
                    }

                    //ensure image folders exist
                    Directory.CreateDirectory($"{APP_GAMES_FOLDER}{gameInfo.Id}\\{SUBFOLDER_IMAGES_SOURCE}"); //this also creates the images subfolder

                    _games.Add(gameInfo.Id, gameInfo);
                    _gameCRCs.Add(uint.Parse(gameInfo.romCRC32, NumberStyles.HexNumber));
                }
                catch
                {
                    //TODO:log
                }
            }
        }

        internal async Task EnrichGames(IEnumerable<string> gameIds, IProgress<(string, int, int)> progress)
        {
            await EnrichGames(gameIds.Select(i => GetGameById(i)).Where(g => g != null), progress);
        }

        internal string GetCleanedTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            //strip everything after brackets
            int bracketsIndex = title.IndexOfAny(new[] { '(', '[', ':', ';' });
            if (bracketsIndex > 1) title = title.Substring(0, bracketsIndex);

            //...and trim
            return title.Trim();
        }

        internal string GetCompareTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            title = GetCleanedTitle(title).ToLowerInvariant();

            //only keep words
            title = string.Join(" ", Regex.Replace(title, "[^A-Za-z0-9]", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s switch { "i" => "1", "ii" => "2", "iii" => "3", "iv" => "4", "v" => "5", _ => s })); //convert lower roman numbers

            title = title.Replace(" & ", " and ");
            title = title.Replace(" s ", " "); //probably this was a 's

            return title;
        }

        /// <summary>
        /// This method makes ensures every imported game is mapped to a platform and core (either stock or RA)
        /// If no platform is set, it will ask the user
        /// </summary>
        internal async Task EnsurePlatform(IEnumerable<GameInfo> games, IProgress<(string, int, int)> progress)
        {
            var useAsDefaultExtensions = new Dictionary<string, int>();
            var processedGames = 0;
            foreach (var game in games)
            {
                progress.Show(("Finalizing platforms", ++processedGames, games.Count()));

                var ext = Path.GetExtension(game.romFileName);
                var possiblePlatforms = _appSettings.Platforms.Where(p => p.SupportedExtensions.Contains(ext)).ToList();

                if (game.romPlatformId == 0)
                {
                    if (useAsDefaultExtensions.ContainsKey(ext))
                    {
                        game.romPlatformId = useAsDefaultExtensions[ext];
                    }
                    else
                    {
                        using (var form = new SelectPlatform(possiblePlatforms, game))
                        {
                            form.ShowDialog();
                            if (form.SelectedPlatform != null)
                            {
                                game.romPlatformId = form.SelectedPlatform.Id;
                            } else
                            {
                                game.romPlatform = "";
                            }
                            
                            if (form.UseAsDefault)
                            {
                                useAsDefaultExtensions.Add(ext, game.romPlatformId);
                            }
                        }
                    }
                }
                
                await SerializeGame(game); // this will internally call PreselectGameCore(game), as game.romPlatform is still null
            }
        }

        public void PreselectGameCore(GameInfo game)
        {
            var gamePlatform = _appSettings.Platforms.SingleOrDefault(p => p.Id == game.romPlatformId);
            if (gamePlatform == null) return;

            var ext = Path.GetExtension(game.romFileName);
            //depending on extension, select the first matching core (note: try stock core first)
            if (game.IsMultiDisc || gamePlatform.InternalEmulator == null || !gamePlatform.InternalEmulator.SupportedExtensions.Contains(ext))
            {
                game.RetroArchCore = gamePlatform.RetroArchCores.FirstOrDefault(c => c.SupportedExtensions.Contains(ext))?.CoreFileName;
            }
            else
            {
                game.RetroArchCore = null;
            }
        }

        internal async Task EnrichGames(IEnumerable<GameInfo> games, IProgress<(string, int, int)> progress)
        {
            var mappedGames = new Dictionary<int, List<GameInfo>>();
            int mappedGamesCount = 0;

            void AddGameToMapped(GameInfo game, int tgdbId)
            {
                mappedGamesCount++;
                game.TgdbId = tgdbId;
                if (mappedGames.ContainsKey(tgdbId))
                {
                    mappedGames[tgdbId].Add(game);
                }
                else
                {
                    mappedGames.Add(tgdbId, new List<GameInfo>() { game });
                }
            }
            
            //build list of mappedGames
            foreach (var game in games)
            {
                if (_romManager.RomMappings.TryGetValue(uint.Parse(game.romCRC32, NumberStyles.HexNumber), out int tgdbId))
                {
                    AddGameToMapped(game, tgdbId);
                }
            }

            int processedGames = 0;

            if (mappedGamesCount > 0)
            {
                var resp = await _tgdbApi.Games.ByGameID(
                    mappedGames.Keys.ToArray(),
                    new[] { GameFieldIncludes.BoxArt },
                    GameFields.Players, GameFields.Publishers, GameFields.Genres, GameFields.Overview, GameFields.Platform);
                do
                {
                    if (resp.Code == 200)
                    {
                        var imgBaseUrl = resp.Include?.BoxArt?.BaseUrl?.Medium;
                        foreach (var tgdbGame in resp.Data.Games)
                        {
                            foreach (var mappedGame in mappedGames[tgdbGame.Id])
                            {
                                progress.Show(("Scraping Game info by CRC code", ++processedGames, mappedGamesCount));
                                //metadata
                                mappedGame.romDescription = tgdbGame.Overview;
                                mappedGame.romPlayers = tgdbGame.Players.HasValue ? tgdbGame.Players.Value : 1; //default 1
                                mappedGame.romReleaseDate = tgdbGame.ReleaseDate.HasValue ? tgdbGame.ReleaseDate.Value.ToString("yyyy-MM-dd") : "";
                                mappedGame.romPlatformId = _romManager.TryMapToPlatform(tgdbGame.Platform, out int platform) ? platform : mappedGame.romPlatformId;
                                mappedGame.romTitle = !string.IsNullOrWhiteSpace(tgdbGame.GameTitle) ? tgdbGame.GameTitle : mappedGame.romTitle;
                                mappedGame.romGenre = _romManager.MapToGenre(tgdbGame.Genres);

                                //images
                                if (resp.Include?.BoxArt?.Data?.ContainsKey(tgdbGame.Id) == true)
                                {
                                    var selectedImg = resp.Include.BoxArt.Data[tgdbGame.Id].OrderBy(i => i.Side == "front" ? 1 : 2).FirstOrDefault();
                                    if (selectedImg != null)
                                    {
                                        await _imageManager.ResizeImage($"{imgBaseUrl}{selectedImg.FileName}", mappedGame, GetGameBoxartImageInfo(mappedGame.Id));
                                    }
                                }
                            }
                        }
                    }
                    //go to next page
                    resp = resp.Pages?.Next != null ? await resp.NextPage() : null;
                }
                while (resp != null);
            }

            //now for the unmapped games, try to find a single match (by name)
            //if found => add to mapped
            var nonMappedGames = games.Except(mappedGames.Values.SelectMany(g => g));
            var nonMappedGamesCount = nonMappedGames.Count();
            processedGames = 0;
            foreach (var nonMappedGame in nonMappedGames)
            {
                var tgdbPlatformIds = GetGamePlatformsByRomExtesion(Path.GetExtension(nonMappedGame.romFileName))
                    .SelectMany(p => p.TGDB_PlatformIds)
                    .Select(p => p.Id).ToArray();

                if (tgdbPlatformIds.Length == 0)
                {
                    tgdbPlatformIds = new[] { 1 }; //use PC as fallback plaform for Doom and Quake
                }

                //
                var resp = await _tgdbApi.Games.ByGameName(GetCleanedTitle(nonMappedGame.romTitle), 1, tgdbPlatformIds
                    , new[] { GameFieldIncludes.BoxArt },
                    GameFields.Players, GameFields.Publishers, GameFields.Genres, GameFields.Overview, GameFields.Platform);

                progress.Show(("Scraping Game info by name", ++processedGames, nonMappedGamesCount));

                if (resp.Code == 200)
                {
                    var imgBaseUrl = resp.Include?.BoxArt?.BaseUrl?.Medium;

                    var tgdbGames = resp.Data.Games.Where(g => GetCompareTitle(g.GameTitle) == GetCompareTitle(nonMappedGame.romTitle));
                    var firstMatchPlatform = tgdbGames.FirstOrDefault()?.Platform;
                    // if matches are for the same platform, then we can be pretty sure the first match is a good one
                    if (tgdbGames.Count() > 0 && tgdbGames.All(g => g.Platform == firstMatchPlatform))
                    {
                        var tgdbGame = tgdbGames.First();
                        var mappedGame = nonMappedGame;

                        AddGameToMapped(mappedGame, tgdbGame.Id);

                        //TODO: extract code below to method

                        //metadata
                        mappedGame.romDescription = tgdbGame.Overview;
                        mappedGame.romPlayers = tgdbGame.Players.HasValue ? tgdbGame.Players.Value : 1; //default 1
                        mappedGame.romReleaseDate = tgdbGame.ReleaseDate.HasValue ? tgdbGame.ReleaseDate.Value.ToString("yyyy-MM-dd") : "";
                        mappedGame.romPlatformId = _romManager.TryMapToPlatform(tgdbGame.Platform, out int platform) ? platform : mappedGame.romPlatformId;
                        //probably don't overwrite title, as we want to keep original name in case match was a false positive
                        //mappedGame.romTitle = !string.IsNullOrWhiteSpace(tgdbGame.GameTitle) ? tgdbGame.GameTitle : mappedGame.romTitle;
                        mappedGame.romGenre = _romManager.MapToGenre(tgdbGame.Genres);

                        //images
                        if (resp.Include?.BoxArt?.Data?.ContainsKey(tgdbGame.Id) == true)
                        {
                            var selectedImg = resp.Include.BoxArt.Data[tgdbGame.Id].OrderBy(i => i.Side == "front" ? 1 : 2).FirstOrDefault();
                            if (selectedImg != null)
                            {
                                await _imageManager.ResizeImage($"{imgBaseUrl}{selectedImg.FileName}", mappedGame, GetGameBoxartImageInfo(mappedGame.Id));
                            }
                        }
                    }
                }
            }

            if (mappedGamesCount > 0)
            {
                //now try to find a nice banner
                var respImg = await _tgdbApi.Games.Images(
                    mappedGames.Keys.ToArray(),
                    GameImageType.Screenshot, GameImageType.TitleScreen, GameImageType.Fanart);
                processedGames = 0;
                do
                {
                    if (respImg.Code == 200)
                    {
                        var imgBaseUrl = respImg.Data.BaseUrl.Medium;
                        foreach (var tgdbImg in respImg.Data.Images)
                        {
                            foreach (var mappedGame in mappedGames[tgdbImg.Key])
                            {
                                progress.Show(("Looking for banner images", ++processedGames, mappedGamesCount));
                                var selectedImg = tgdbImg.Value.OrderBy(i => i.Type == GameImageType.Screenshot ? 0 : 1).FirstOrDefault(); //first screenshot, then titlescreen and fanart
                                if (selectedImg != null)
                                {
                                    await _imageManager.ResizeImage($"{imgBaseUrl}{selectedImg.FileName}", mappedGame, new[] { GetGameImageInfo(mappedGame.Id, ImageType.Banner) });
                                }
                            }
                        }
                    }
                    //go to next page
                    respImg = respImg.Pages?.Next != null ? await respImg.NextPage() : null;
                }
                while (respImg != null);
            }

            //now serialize the games
            processedGames = 0;
            foreach (var gameInfo in mappedGames.Values.SelectMany(g => g))
            {
                progress.Show(("Updating game database", ++processedGames, mappedGamesCount));
                await SerializeGame(gameInfo);
            }
        }

        public async Task ClearImage(GameInfo gameInfo, ImageType imageType, bool autoSerialize = true)
        {
            var imagePath = GetGameImageInfo(gameInfo.Id, imageType).LocalPath;
            File.Delete(imagePath);

            //also remove source image
            var sourceImg = GetSourceImagePath(imagePath);
            if (File.Exists(sourceImg)) File.Delete(sourceImg);

            switch (imageType)
            {
                case ImageType.Small: gameInfo.Image = null; break;
                case ImageType.Medium: gameInfo.ImageHD = null; break;
                case ImageType.Large: gameInfo.Image1080 = null; break;
                case ImageType.Banner: gameInfo.ImageBanner = null; break;
            }

            if (autoSerialize) await SerializeGame(gameInfo);
        }

        public string GetRomListTitle(GameInfo game)
        {
            if (game == null) return "";
            return $"{game.romTitle} [{game.romPlatform}]";
        }

        public string GetGameRootFromJsonPath(string sdDrive, string currentGameJsonFilePath)
        {
            var ret = "";

            if (currentGameJsonFilePath.StartsWith($"{sdDrive}folders\\"))
            {
                var i = currentGameJsonFilePath.IndexOf("\\game\\");
                if (i >= 0)
                {
                    ret = currentGameJsonFilePath.Substring(0, i + 1);
                }
            }
            else
            {
                ret = sdDrive;
            }

            return ret;
        }

        public string GetGameIdFromJsonPath(string sdDrive, string currentGameJsonFilePath)
        {
            var ret = currentGameJsonFilePath;
            var folderPrefix = $"{sdDrive}folders\\";
            var rootPrefix = sdDrive;
 
            var i = currentGameJsonFilePath.IndexOf("\\game\\");
            if (i >= 0)
            {
                if (currentGameJsonFilePath.StartsWith(folderPrefix))
                {
                    ret = currentGameJsonFilePath.Substring(i + 6);
                }
                else
                {
                    ret = currentGameJsonFilePath.Substring(rootPrefix.Length + 5);
                }

                ret = ret.Substring(0, ret.IndexOf(".json"));
            }

            return ret;
        }
    }
}
