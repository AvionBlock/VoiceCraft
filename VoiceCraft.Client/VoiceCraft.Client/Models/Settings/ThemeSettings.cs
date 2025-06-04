using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Models.Settings;

public class ThemeSettings : Setting<ThemeSettings>
{
    private Guid _selectedBackgroundImage = Constants.DockNightGuid;
    private Guid _selectedTheme = Constants.DarkThemeGuid;

    public Guid SelectedBackgroundImage
    {
        get => _selectedBackgroundImage;
        set
        {
            _selectedBackgroundImage = value;
            OnUpdated?.Invoke(this);
        }
    }
    
    public Guid SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            _selectedTheme = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<ThemeSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (ThemeSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}