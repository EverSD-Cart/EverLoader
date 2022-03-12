using EverLoader.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TheGamesDBApiWrapper.Models.Entities;
using TheGamesDBApiWrapper.Models.Responses.Platforms;

namespace EverLoader.Services
{
    public class RomManager
    {
        private readonly DownloadManager _downloadManager;
        private readonly AppSettings _appSettings;

        public Dictionary<uint, int> RomMappings { get; internal set; } //note: the int values (=tgdb Ids) are not unique
        public Dictionary<string, string> MameNames { get; internal set; }
        private Dictionary<int, int> _platformMapping = null;
        private Dictionary<int, string> _genreMapping = null;

        public RomManager(DownloadManager downloadManager,
            AppSettings appSettings)
        {
            _downloadManager = downloadManager;
            _appSettings = appSettings;
        }

        public string MapToGenre(int[] tgdbGenreIds)
        {
            if (tgdbGenreIds == null || tgdbGenreIds.Length == 0) return null;
            if (_genreMapping == null)
            {
                _genreMapping = new Dictionary<int, string>();
                foreach (var genre in _appSettings.Genres)
                {
                    foreach (var tgdbGenreId in genre.TGDB_GenreIds)
                    {
                        _genreMapping.Add(tgdbGenreId, genre.Name);
                    }
                }
            }
            foreach (var tgdbGenreId in tgdbGenreIds)
            {
                if (_genreMapping.TryGetValue(tgdbGenreId, out string genreText)) return genreText;
            }
            return null; //no mapping possible
        }

        public bool TryMapToPlatform(int? tgdbPlatformId, out int platformId)
        {
            platformId = 0;
            if (!tgdbPlatformId.HasValue) return false;
            if (_platformMapping == null)
            {
                _platformMapping = new Dictionary<int, int>();
                foreach (var platform in _appSettings.Platforms)
                {
                    foreach (var tgdbpid in platform.TGDB_PlatformIds)
                    {
                        _platformMapping.Add(tgdbpid.Id, platform.Id);
                    }
                }
            }
            return _platformMapping.TryGetValue(tgdbPlatformId.Value, out platformId);
        }

        public async Task Init()
        {
            using (var ms = new MemoryStream(Properties.Resources.crc2rom_mappings))
            using (var archive = new ZipArchive(ms))
            using (var entryStream = archive.Entries[0].Open())
            using (var reader = new StreamReader(entryStream, Encoding.UTF8))
            {
                var romFilesJson = await reader.ReadToEndAsync();
                RomMappings = JsonConvert.DeserializeObject<Dictionary<uint, int>>(romFilesJson);
            }

            using (var ms = new MemoryStream(Properties.Resources.mamenames))
            using (var archive = new ZipArchive(ms))
            using (var entryStream = archive.Entries[0].Open())
            using (var reader = new StreamReader(entryStream, Encoding.UTF8))
            {
                string line;
                MameNames = new Dictionary<string, string>();
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var lineSplit = line.Split('\t');
                    MameNames.Add(lineSplit[0], lineSplit[1]);
                }
            }
        }

        /// <summary>
        /// https://github.com/TeamShinkansen/hash-db/releases/download/data/romfiles.xml.gz
        /// </summary>
        /// <param name="romfilesXmlGzUrl"></param>
        /// <returns></returns>
        public async Task InitFromExternalRomfilesXml(string romfilesXmlGzUrl)
        {
            //load roms
            var romFileXml = await _downloadManager.GetDownloadedFilePath(new Uri(romfilesXmlGzUrl), "romfiles.xml");

            var romMappings = new Dictionary<uint, int>();
            using (var reader = new XmlTextReader(romFileXml))
            {
                uint crc32 = 0;
                List<int> tgdb = new List<int>();
                while (reader.Read())
                {
                    if ((reader.Name == "file" && reader.NodeType == XmlNodeType.Element)
                        || (reader.Name == "romfiles" && reader.NodeType == XmlNodeType.EndElement))
                    {
                        //save previous entry
                        if (crc32 != 0)
                        {
                            romMappings.Add(crc32, tgdb[0]);
                        }
                        tgdb.Clear();
                        crc32 = uint.TryParse(reader.GetAttribute("crc32"), NumberStyles.HexNumber, null, out uint result) ? result : 0;
                    }
                    if (reader.Name == "tgdb" && reader.NodeType == XmlNodeType.Element)
                    {
                        if (int.TryParse(reader.ReadElementContentAsString(), out int tgdbId))
                        {
                            tgdb.Add(tgdbId);
                        }
                    }
                }
            }

            //now set
            RomMappings = romMappings;
        }
    }
}
