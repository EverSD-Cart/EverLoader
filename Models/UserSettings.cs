using EverLoader.Enums;
using EverLoader.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EverLoader.Models
{
    public class UserSettings
    {
        #region private properties
        private ImageResizeMode _boxArtResizeMode = ImageResizeMode.Cropping;
        #endregion

        #region public properties
        public ImageResizeMode BoxArtResizeMode { get => _boxArtResizeMode; set => NotifyChange(ref _boxArtResizeMode, value); }
        #endregion

        public event EventHandler UserSettingsChanged;
        internal void NotifyChange<T>(ref T backVar, T value)
        {
            backVar = value;
            if (UserSettingsChanged != null) UserSettingsChanged(this, EventArgs.Empty);
        }
    }
}
