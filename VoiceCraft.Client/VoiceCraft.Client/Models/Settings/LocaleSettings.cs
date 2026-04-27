using System;
using System.Globalization;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Models.Settings;

public class LocaleSettings : Setting<LocaleSettings>
{
    public string Culture
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = Localizer.Languages.Contains(CultureInfo.CurrentCulture.Name)
        ? CultureInfo.CurrentCulture.Name
        : Constants.DefaultLanguage;

    public override event Action<LocaleSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (LocaleSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}