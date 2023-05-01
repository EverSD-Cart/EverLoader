using EverLoader.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace EverLoader.Services
{
    public class UserSettingsManager
    {
        private UserSettings _userSettings;
        internal readonly string USERSETTINGS_PATH = $"{Constants.APP_ROOT_FOLDER}usersettings.json";

        public UserSettings UserSettings 
        {
            get
            {
                if (_userSettings == null)
                {
                    try
                    {
                        //if not exists, will throw exception
                        _userSettings = JsonConvert.DeserializeObject<UserSettings>(File.ReadAllText(USERSETTINGS_PATH));
                    }
                    catch (Exception)
                    {
                        _userSettings = new UserSettings(); 
                    }
                    UserSettings.UserSettingsChanged += UserSettings_UserSettingsChanged;
                }
                return _userSettings;
            }
        }

        private void UserSettings_UserSettingsChanged(object sender, EventArgs e)
        {
            File.WriteAllText(USERSETTINGS_PATH, JsonConvert.SerializeObject(UserSettings, Formatting.Indented));
        }
    }
}
