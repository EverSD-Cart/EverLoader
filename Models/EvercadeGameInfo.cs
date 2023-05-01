using EverLoader.JsonConverters;
using Newtonsoft.Json;
using System;

namespace EverLoader.Models
{
    /// <summary>
    /// GameInfo holds all information about a game. This class will be serialized as /game/[game-name].json
    /// </summary>
    public class EvercadeGameInfo
    {
        public EvercadeGameInfo()
        {
            romMapping = new ButtonMappings(this);
        }

        public EvercadeGameInfo(string json) : this()
        {
            JsonConvert.PopulateObject(json, this);
        }

        [JsonIgnore]
        public bool SuppressChangeNotifactions { get; set; } = false;

        #region private vars
        private string _romTitle;
        private string _romGenre;
        private string _romPlatform;
        private string _romReleaseDate;
        private int _romPlayers = 1;
        private string _romDescription;
        #endregion

        // game json fields
        // all properties that can be changed in UI use NotifyChange
        public string romFileName { get; set; }
        public string romTitle { get => _romTitle; set => NotifyChange(ref _romTitle, value, isTitle: true); }
        public string romCore { get => "NULL"; } //required field for fw 2.1.1
        public string romLaunchType { get; set; } //required field for fw 2.1.1
        public string romPlatform { get => _romPlatform ?? ""; set => _romPlatform = value; } //don't notify change, as this value is derived
        public string romGenre { get => _romGenre ?? ""; set => NotifyChange(ref _romGenre, value); }
        public string romReleaseDate { get => _romReleaseDate ?? ""; set => NotifyChange(ref _romReleaseDate, value); }
        public int romPlayers { get => _romPlayers; set => NotifyChange(ref _romPlayers, value); }
        [JsonConverter(typeof(NewLineFixConverter))]
        public string romDescription { get => _romDescription ?? ""; set => NotifyChange(ref _romDescription, value); }
        public ButtonMappings romMapping { get; set; }

        //event handlers
        public event EventHandler GameInfoChanged;
        internal void NotifyChange<T>(ref T backVar, T value, bool isTitle = false)
        {
            //when trying to clear title, don't allow this
            if (isTitle && string.IsNullOrWhiteSpace(value as string)) return; 

            //when values equal, no need to do anything
            if (Equals(backVar, value)) return;

            backVar = value;
            if (!SuppressChangeNotifactions && GameInfoChanged != null)
            {
                GameInfoChanged(this, isTitle ? new TitleChangedEventArgs() : EventArgs.Empty);
            }
        }
    }

    public class TitleChangedEventArgs : EventArgs { }

    public class ButtonMappings
    {
        #region private vars
        private readonly EvercadeGameInfo _parent;
        private string _a, _b, _x, _y, _dpad, _select, _start, _l1, _l2, _r1, _r2;
        #endregion

        public ButtonMappings(EvercadeGameInfo parent) { _parent = parent; }

        [JsonConverter(typeof(ButtonMappingConverter))]
        public string a { get => _a ?? ""; set => NotifyChange(ref _a, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string b { get => _b ?? ""; set => NotifyChange(ref _b, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string x { get => _x ?? ""; set => NotifyChange(ref _x, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string y { get => _y ?? ""; set => NotifyChange(ref _y, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string dpad { get => _dpad ?? ""; set => NotifyChange(ref _dpad, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string select { get => _select ?? ""; set => NotifyChange(ref _select, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string start { get => _start ?? ""; set => NotifyChange(ref _start, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string l1 { get => _l1 ?? ""; set => NotifyChange(ref _l1, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string l2 { get => _l2 ?? ""; set => NotifyChange(ref _l2, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string r1 { get => _r1 ?? ""; set => NotifyChange(ref _r1, value); }
        [JsonConverter(typeof(ButtonMappingConverter))]
        public string r2 { get => _r2 ?? ""; set => NotifyChange(ref _r2, value); }

        private void NotifyChange(ref string backVar, string value)
        {
            if (_parent != null) _parent.NotifyChange(ref backVar, value); 
            else backVar = value;
        }
    }
}
