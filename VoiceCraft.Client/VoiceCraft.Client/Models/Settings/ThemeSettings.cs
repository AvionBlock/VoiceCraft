using System;
using Avalonia.Styling;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class ThemeSettings : Setting<ThemeSettings>
    {
        public override event Action<ThemeSettings>? OnUpdated;

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                _selectedTheme = value;
                OnUpdated?.Invoke(this);
            }
        }

        private string _selectedTheme = ThemeVariant.Dark.ToString();
        
        public override object Clone()
        {
            var clone = (ThemeSettings)MemberwiseClone();
            clone.OnUpdated = null;
            return clone;
        }
    }
}