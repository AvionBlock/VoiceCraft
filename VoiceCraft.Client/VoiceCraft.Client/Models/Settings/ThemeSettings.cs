using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Models.Settings;

public class ThemeSettings : Setting<ThemeSettings>
{
    public Guid SelectedBackgroundImage
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = Constants.DockNightGuid;

    public Guid SelectedTheme
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = Constants.DarkThemeGuid;

    public override event Action<ThemeSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (ThemeSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}