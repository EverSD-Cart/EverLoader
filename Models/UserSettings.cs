using System;

namespace EverLoader.Models
{
    public class UserSettings
    {
        #region private properties
        private bool _optimizeImageSizes = true;
        #endregion

        #region public properties
        public bool OptimizeImageSizes { get => _optimizeImageSizes; set => NotifyChange(ref _optimizeImageSizes, value); }
        #endregion

        public event EventHandler UserSettingsChanged;
        internal void NotifyChange<T>(ref T backVar, T value)
        {
            backVar = value;
            if (UserSettingsChanged != null) UserSettingsChanged(this, EventArgs.Empty);
        }
    }
}
