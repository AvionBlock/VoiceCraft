using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class ThemeSettings : Setting<ThemeSettings>
    {
        public override event Action<ThemeSettings>? OnUpdated;

        public Guid SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                _selectedTheme = value;
                OnUpdated?.Invoke(this);
            }
        }

        public Guid SelectedBackgroundImage
        {
            get => _selectedBackgroundImage;
            set
            {
                _selectedBackgroundImage = value;
                OnUpdated?.Invoke(this);
            }
        }

        private Guid _selectedTheme = Guid.Empty;
        private Guid _selectedBackgroundImage = Guid.Empty;
        
        public override object Clone()
        {
            var clone = (ThemeSettings)MemberwiseClone();
            clone.OnUpdated = null;
            return clone;
        }
    }
}