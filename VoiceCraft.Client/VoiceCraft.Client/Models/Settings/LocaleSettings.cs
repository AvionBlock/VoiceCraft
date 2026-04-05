using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Models.Settings;

public class LocaleSettings : Setting<LocaleSettings>
{
    private string _culture = ResolvePreferredCulture();

    public string Culture
    {
        get => _culture;
        set
        {
            _culture = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<LocaleSettings>? OnUpdated;

    public override bool OnLoading()
    {
        if (!Localizer.Languages.Contains(_culture))
            _culture = ResolvePreferredCulture();
        return base.OnLoading();
    }

    public override object Clone()
    {
        var clone = (LocaleSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }

    private static string ResolvePreferredCulture()
    {
        return ResolvePreferredCulture(CultureInfo.CurrentUICulture.Name, Localizer.Languages);
    }

    private static string ResolvePreferredCulture(string currentCulture, IEnumerable<string> supportedLanguages)
    {
        var supported = supportedLanguages.ToArray();
        if (supported.Length == 0)
            return Constants.DefaultLanguage;

        var exact = supported.FirstOrDefault(x => x.Equals(currentCulture, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        var language = TryGetLanguageCode(currentCulture);
        if (!string.IsNullOrWhiteSpace(language))
        {
            var neutral = supported.FirstOrDefault(x => x.Equals(language, StringComparison.OrdinalIgnoreCase));
            if (neutral != null)
                return neutral;

            var regional = supported.FirstOrDefault(x =>
                x.StartsWith(language + "-", StringComparison.OrdinalIgnoreCase));
            if (regional != null)
                return regional;
        }

        return supported.FirstOrDefault(x => x.Equals(Constants.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
               ?? supported[0];
    }

    private static string TryGetLanguageCode(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return string.Empty;
        var separator = cultureName.IndexOf('-');
        return separator == -1 ? cultureName : cultureName[..separator];
    }
}
