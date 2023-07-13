// using System.IO;

namespace EverLoader.Services
{
    public class ImportManager
    {
        // public void ReadGames()
        // {
        //     //reads games from database folder
        //     var dirGame = new DirectoryInfo(APP_GAME_FOLDER);
        //     if (!dirGame.Exists) return;
        //
        //     foreach (var jsonFile in Directory.GetFiles(APP_GAME_FOLDER, "*.json"))
        //     {
        //         var gameInfo = JsonConvert.DeserializeObject<GameInfo>(File.ReadAllText(jsonFile));
        //
        //         gameInfo.Id = Path.GetFileNameWithoutExtension(jsonFile);
        //
        //         //if no romPlatformId set, just quickly find it 
        //         if (gameInfo.romPlatformId == 0)
        //         {
        //             var romExt = Path.GetExtension(gameInfo.romFileName);
        //             if (!Program.ExtensionPlatformMapping.ContainsKey(romExt)) continue;
        //             gameInfo.romPlatformId = Program.ExtensionPlatformMapping[romExt];
        //         }
        //
        //         gameInfo.IsSelected = UserSettingsManager.UserSettings.SelectedRoms.Contains(gameInfo.Id);
        //
        //         //find box art images
        //         var pathNoExt = jsonFile.Replace(".json", "");
        //         if (File.Exists($"{pathNoExt}0.png")) gameInfo.Image = $"{pathNoExt}0.png";
        //         if (File.Exists($"{pathNoExt}0_hd.png")) gameInfo.ImageHD = $"{pathNoExt}0_hd.png";
        //         if (File.Exists($"{pathNoExt}0_1080.png")) gameInfo.Image1080 = $"{pathNoExt}0_1080.png";
        //         if (File.Exists($"{pathNoExt}_gamebanner.png")) gameInfo.ImageBanner = $"{pathNoExt}_gamebanner.png";
        //
        //         //if no MD5 or CRC32 found: calculate them!
        //         if (string.IsNullOrEmpty(gameInfo.romCRC32) || string.IsNullOrEmpty(gameInfo.romMD5))
        //         {
        //             var romPath = $"{APP_GAME_FOLDER}{gameInfo.romFileName}";
        //
        //             //if rom missing or 0-byte, skip for now (handle 0-byte marker later)
        //             if (!File.Exists(romPath) || new FileInfo(romPath).Length == 0)
        //             {
        //                 continue;
        //             }
        //
        //             (gameInfo.romCRC32, gameInfo.romMD5) = HashHelper.CalculateHashcodes(romPath);
        //
        //             //var romFound = File.Exists(romPath);
        //             //if (romFound)
        //             //{
        //             //    var romSize = new FileInfo(romPath).Length;
        //             //    if (romSize == 0)
        //             //    {
        //             //        var specialRom = dirGame.EnumerateFiles($"{gameInfo.Id}.*").FirstOrDefault(f => !f.Extension.ToLower().In("",".json"));
        //             //        romFound = specialRom != null;
        //             //        if (romFound)
        //             //        {
        //             //            romFound = true;
        //             //            romPath = pathNoExt + specialRom.Extension;
        //             //        }
        //             //    }
        //
        //             //    if (romFound)
        //             //    {
        //             //        (gameInfo.romCRC32, gameInfo.romMD5) = HashHelper.CalculateHashcodes(romPath);
        //             //    }
        //             //}
        //         }
        //
        //         //handle possible shell script in /special folder
        //         if (File.Exists($"{APP_SPECIAL_FOLDER}{gameInfo.Id}.sh"))
        //         {
        //             gameInfo.SpecialScript = File.ReadAllText($"{APP_SPECIAL_FOLDER}{gameInfo.Id}.sh");
        //         }
        //
        //         _games.Add(gameInfo.Id, gameInfo);
        //         _gameCRCs.Add(uint.Parse(gameInfo.romCRC32, NumberStyles.HexNumber));
        //     }
        // }
    }
}
