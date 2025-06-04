using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Jeek.Avalonia.Localization;

namespace VoiceCraft.Client.Locales;

//Credits https://github.com/tifish/Jeek.Avalonia.Localization/blob/main/Jeek.Avalonia.Localization/JsonLocalizer.cs
public class EmbeddedJsonLocalizer : BaseLocalizer
{
    private readonly string _languageJsonDirectory;
    private JsonNode? _languageStrings;

    public EmbeddedJsonLocalizer(string languageJsonDirectory = "")
    {
        FallbackLanguage = "en-us";
        _languageJsonDirectory = string.IsNullOrWhiteSpace(languageJsonDirectory) ? "Languages.json" : languageJsonDirectory;
    }

    public override void Reload()
    {
        _languageStrings = null;
        _languages.Clear();

        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();

        if (!resources.Any(x => x.Contains(_languageJsonDirectory)))
            throw new FileNotFoundException(_languageJsonDirectory);

        var files = resources.Where(x => x.Contains(_languageJsonDirectory) && x.EndsWith(".json"));
        foreach (var file in files)
        {
            var language = Path.GetFileNameWithoutExtension(file).Replace($"{_languageJsonDirectory}.", "");
            _languages.Add(language);
        }

        ValidateLanguage();

        var languageFile = $"{_languageJsonDirectory}.{_language}.json";
        using (var stream = assembly.GetManifestResourceStream(languageFile))
        {
            if (stream == null) throw new FileNotFoundException($"Could not find resource {languageFile}");

            using (var reader = new StreamReader(stream))
            {
                var jsonContent = reader.ReadToEnd();
                _languageStrings = JsonNode.Parse(jsonContent);
            }
        }

        _hasLoaded = true;

        UpdateDisplayLanguages();
    }

    protected override void OnLanguageChanged()
    {
        Reload();
    }

    public override string Get(string key)
    {
        if (!_hasLoaded)
            Reload();

        if (_languageStrings == null)
            return key;

        try
        {
            var keys = key.Split('.');
            var value = _languageStrings;
            for (var i = 0; i < keys.Length; i++)
            {
                if (i == keys.Length - 1)
                {
                    var langStr = value?[keys[i]]?.GetValue<string>();
                    return langStr?.Replace("\\n", "\n") ?? key;
                }

                value = value?[keys[i]];
            }
        }
        catch
        {
            // ignored
        }

        return key;
    }
}