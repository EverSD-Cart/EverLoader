using EverLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGamesDBApiWrapper.Domain;
using TheGamesDBApiWrapper.Models.Enums;

namespace EverLoader.Services
{
    public class ScrapeManager
    {
        private readonly ITheGamesDBAPI _tgdbApi;

        public ScrapeManager(ITheGamesDBAPI tgdbApi)
        {
            _tgdbApi = tgdbApi;
        }

        public async Task<IEnumerable<TgdbScrapeResult>> ScrapeByGameTitle(string title, IdName[] tgdbPlatformIds)
        {
            var scrapeResult = new Dictionary<int, TgdbScrapeResult>();

            if (string.IsNullOrWhiteSpace(title)) return scrapeResult.Values;

            var resp = await _tgdbApi.Games.ByGameName(title.Trim(), 1, tgdbPlatformIds.Select(p => p.Id).ToArray(), new[] { GameFieldIncludes.BoxArt },
                GameFields.Players, GameFields.Publishers, GameFields.Genres, GameFields.Overview, GameFields.Platform);

            if (resp.Code == 200)
            {
                foreach (var tgdbGame in resp.Data.Games)
                {
                    var tgdbPlatform = tgdbPlatformIds.FirstOrDefault(p => p.Id == tgdbGame.Platform)?.Name;
                    var tgdbYear = tgdbGame.ReleaseDate.HasValue ? $"{tgdbGame.ReleaseDate.Value.Year}{(tgdbPlatform != null ? " - " : null)}" : null;
                    scrapeResult.Add(tgdbGame.Id, new TgdbScrapeResult()
                    {
                        GameId = tgdbGame.Id,
                        GameName = $"{tgdbGame.GameTitle} ({tgdbYear}{tgdbPlatform})",
                        ImageBaseUrl = resp.Include?.BoxArt?.BaseUrl?.Medium,
                        Game = tgdbGame,
                        BoxArt = resp.Include?.BoxArt?.Data?.ContainsKey(tgdbGame.Id) == true
                            ? resp.Include.BoxArt.Data[tgdbGame.Id].OrderBy(i => i.Side == "front" ? 1 : 2).FirstOrDefault()
                            : null
                    });
                }
            }

            if (scrapeResult.Count == 0) return scrapeResult.Values;
 
            //now try to finds some nice banners
            var respImg = await _tgdbApi.Games.Images(scrapeResult.Keys.ToArray(),
                GameImageType.Screenshot, GameImageType.TitleScreen, GameImageType.Fanart);

            if (respImg.Code == 200)
            {
                foreach (var tgdbImg in respImg.Data.Images)
                {
                    if (scrapeResult.TryGetValue(tgdbImg.Key, out TgdbScrapeResult item))
                    {
                        item.Banners = tgdbImg.Value.OrderBy(i => i.Type == GameImageType.Screenshot ? 0 : 1).ToArray(); //screenshots first
                    }
                }
                ////add all banners to all scraped games
                //foreach (var scrapedGame in scrapeResult.Values)
                //{
                //    scrapedGame.Banners = respImg.Data.Images.Values.SelectMany(v => v).OrderBy(i => i.Type == GameImageType.Screenshot ? 0 : 1).ToArray();
                //}
            }

            return scrapeResult.Values;
        }
    }
}
