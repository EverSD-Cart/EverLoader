using Newtonsoft.Json;

namespace EverLoader.Models
{
    /// <summary>
    /// GameInfo holds all information about a game. This class will be serialized as /game/[game-name].json
    /// </summary>
    public class GameInfo : EvercadeGameInfo
    {
        public GameInfo () : base() { }

        #region private vars
        private int _romPlatformId = 0; //0 = unknown
        private string _retroArchCore;
        #endregion

        public string Id { get; set; }

        public string AppVersion { get; set; }

        // additional json fields
        public int romPlatformId
        {
            get => _romPlatformId;
            set
            {
                if (_romPlatformId != 0 && _romPlatformId != value)
                {
                    romPlatform = null;

                    ////changing platform: clear the selected core
                    //_retroArchCore = null;
                }
                base.NotifyChange(ref _romPlatformId, value);
            }
        }

        public string OriginalRomFileName { get; set; }

        public string PreferedRomFileName()
        {
            return (RetroArchCore != null ? OriginalRomFileName : null) ?? romFileName;
        }

        public bool IsMultiDisc { get; set; }
        
        public string romMD5 { get; set; }
        public string romCRC32 { get; set; }
        public int TgdbId { get; set; }

        public string Image { get; set; }
        public string ImageHD { get; set; }
        public string Image1080 { get; set; }
        public string ImageBanner { get; set; }

        public string RetroArchCore {
            get => _retroArchCore;
            set
            {
                base.NotifyChange(ref _retroArchCore, value);
                FixRomLaunchType();
            }
        }

        public void FixRomLaunchType()
        {
            romLaunchType = _retroArchCore != null ? "NATIVE" : "NULL";
        }

        public int ImageBannerVerticalOffset { get; set; }

        [JsonIgnore]
        public bool IsSelected { get; set; } //indicates if game is selected in UI
        [JsonIgnore]
        public bool IsRecentlyAdded { get; set; } //indicates if game was recently added in the app
        [JsonIgnore]
        public bool IsPresentOnCartridge { get; set; } //indicates if game exists on the inserted cartridge/microsd
    }
}
