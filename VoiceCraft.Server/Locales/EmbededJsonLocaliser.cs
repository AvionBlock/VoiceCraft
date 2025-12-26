using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Locales;

//Credits https://github.com/tifish/Jeek.Avalonia.Localization/blob/main/Jeek.Avalonia.Localization/JsonLocalizer.cs
public class EmbeddedJsonLocalizer(string languageJsonDirectory = "") : IBaseLocalizer
{
    private readonly string _languageJsonDirectory =
        string.IsNullOrWhiteSpace(languageJsonDirectory) ? "Languages.json" : languageJsonDirectory;

    private JsonNode? _languageStrings;

    public string FallbackLanguage => Constants.DefaultLanguage;
    public ObservableCollection<string> Languages { get; } = [];

    public string Reload(string language)
    {
        _languageStrings = null;
        Languages.Clear();

        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames();

        if (!resources.Any(x => x.Contains(_languageJsonDirectory)))
            throw new FileNotFoundException(_languageJsonDirectory);

        var files = resources.Where(x => x.Contains(_languageJsonDirectory) && x.EndsWith(".json"));
        foreach (var file in files)
        {
            var lang = Path.GetFileNameWithoutExtension(file).Replace($"{_languageJsonDirectory}.", "");
            Languages.Add(lang);
        }

        if (!Languages.Contains(language))
            language = FallbackLanguage;

        var languageFile = $"{_languageJsonDirectory}.{language}.json";
        using var stream = assembly.GetManifestResourceStream(languageFile);
        if (stream == null) throw new FileNotFoundException($"Could not find resource {languageFile}");

        using var reader = new StreamReader(stream);
        var jsonContent = reader.ReadToEnd();
        _languageStrings = JsonNode.Parse(jsonContent);
        return language;
    }

    public string Get(string key)
    {
        var splitString = key.Split(':', 2);
        if (splitString.Length <= 0) return key;
        var translation = GetTranslation(splitString[0]);
        try
        {
            if (splitString.Length != 2) return translation;
            var variables = splitString[1].Split(',').Select(GetTranslation).ToArray<object?>();
            translation = string.Format(translation, variables);
            return translation;
        }
        catch(Exception ex)
        {
            LogService.Log(ex);
            return translation;
        }
    }

    public string GetTranslation(string key)
    {
        if (_languageStrings is null)
            return key;

        var dict = _languageStrings;

        int start = 0, end;
        while ((end = key.IndexOf('.', start)) != -1)
        {
            dict = dict?[key[start..end]];
            start = end + 1;
        }

        var node = dict?[key[start..]];
        return node?.GetValueKind() != JsonValueKind.String ? key : node.GetValue<string>().Replace("\\n", "\n");
    }
}