using System.Collections.ObjectModel;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Locales;

public class CombinedLocaliser(IBaseLocalizer defaultLocalizer, params IBaseLocalizer[] localizers) : IBaseLocalizer
{
    public string FallbackLanguage => defaultLocalizer.FallbackLanguage;

    public ObservableCollection<string> Languages { get; } = [];

    public string Reload(string language)
    {
        Languages.Clear();

        language = defaultLocalizer.Reload(language);
        foreach (var lang in defaultLocalizer.Languages)
        {
            Languages.Add(lang);
        }

        foreach (var localizer in localizers)
        {
            localizer.Reload(language);
        }

        return language;
    }

    public string Get(string key)
    {
        var result = defaultLocalizer.Get(key);
        if (result != key) return result;
        foreach (var localizer in localizers)
        {
            result = localizer.Get(key);
        }

        return result;
    }
}