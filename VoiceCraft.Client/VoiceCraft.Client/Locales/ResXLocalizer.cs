using System.Globalization;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Locales;

namespace VoiceCraft.Client.Locales
{
    //Credits https://github.com/tifish/Jeek.Avalonia.Localization/blob/main/Jeek.Avalonia.Localization.Example/ResXLocalizer.cs
    public class ResXLocalizer : BaseLocalizer
    {
        public override void Reload()
        {
            if (_languages.Count == 0)
            {
                _languages.Add("en-US");
                _languages.Add("ja-JP");
                _languages.Add("nl-nl");
            }

            ValidateLanguage();

            Resources.Culture = new CultureInfo(_language);

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

            var langString = Resources.ResourceManager.GetString(key, Resources.Culture);
            return langString != null
                ? langString.Replace("\\n", "\n")
                : $"{Language}:{key}";
        }
    }
}