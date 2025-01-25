using System.Globalization;
using System.Threading;
using Jeek.Avalonia.Localization;

namespace VoiceCraft.Client.Locales
{
    //Credits https://github.com/tifish/Jeek.Avalonia.Localization/blob/main/Jeek.Avalonia.Localization.Example/ResXLocalizer.cs
    public class ResXLocalizer : BaseLocalizer
    {
        public override void Reload()
        {
            if (_languages.Count == 0)
            {
                _languages.Add("en-us");
                _languages.Add("ja-jp");
                _languages.Add("nl-nl");
            }

            ValidateLanguage();

            Resources.Culture = new CultureInfo(_language);
            Thread.CurrentThread.CurrentCulture = Resources.Culture;
            Thread.CurrentThread.CurrentUICulture = Resources.Culture;

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