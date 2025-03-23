using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Jeek.Avalonia.Localization;

namespace VoiceCraft.Client.Locales
{
    //Credits https://github.com/tifish/Jeek.Avalonia.Localization/blob/main/Jeek.Avalonia.Localization/JsonLocalizer.cs
    public class EmbeddedJsonLocalizer(string languageJsonDirectory = "") : BaseLocalizer
    {
        private readonly string _languageJsonDirectory = string.IsNullOrWhiteSpace(languageJsonDirectory) ? "Languages.json" : languageJsonDirectory;
        private Dictionary<string, string>? _languageStrings;
        
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
                if (stream == null)
                {
                    throw new FileNotFoundException($"Could not find resource {languageFile}");
                }

                using (var reader = new StreamReader(stream))
                {
                    var jsonContent = reader.ReadToEnd();
                    _languageStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, LocalesSourceGenerationContext.Default.DictionaryStringString);
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

            return _languageStrings.TryGetValue(key, out var langStr) ? langStr.Replace("\\n", "\n") : $"{Language}:{key}";
        }
    }
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    public partial class LocalesSourceGenerationContext : JsonSerializerContext
    {
    }
}