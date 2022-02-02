using System;
using System.Collections.Generic;
using System.Text;
using TheGamesDBApiWrapper.Models.Entities;

namespace EverLoader.Models
{
    public class TgdbScrapeResult
    {
        public int GameId { get; set; }
        public string GameName { get; set; }
        public string ImageBaseUrl { get; set; }
        public GameModel Game { get; set; }
        public GameImageModel BoxArt { get; set; }
        public GameImageModel[] Banners { get; set; }
        public int UIBannerIndex { get; set; } /* used in UI for looping the banners  */
    }
}
