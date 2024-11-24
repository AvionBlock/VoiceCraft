using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace VoiceCraft.Client.Plugin.Assets
{
    public class Colors : ResourceDictionary
    {
        public Colors()
        {
            ThemeDictionaries.Add(ThemeVariant.Default, GetDefault());
            ThemeDictionaries.Add(ThemeVariant.Dark, GetDark());
        }

        private ResourceDictionary GetDefault()
        {
            var rd = new ResourceDictionary
            {
                { "accentColor", Color.Parse("#1751C3") },
                { "errorAccentColor", Color.Parse("#E0A030") },
                { "successAccentColor", Color.Parse("#3A823D") },
                { "backgroundColor", Color.Parse("#88FFFFFF") },
                { "notificationBackgroundColor", Color.Parse("#444") },
                { "errorBackgroundColor", Color.Parse("#800") },
                { "successBackgroundColor", Color.Parse("#0A0") }
            };

            return rd;
        }

        private ResourceDictionary GetDark()
        {
            var rd = new ResourceDictionary
            {
                { "accentColor", Color.Parse("#1751C3") },
                { "errorAccentColor", Color.Parse("#E0A030") },
                { "successAccentColor", Color.Parse("#3A823D") },
                { "backgroundColor", Color.Parse("#88000000") },
                { "notificationBackgroundColor", Color.Parse("#222") },
                { "errorBackgroundColor", Color.Parse("#800") },
                { "successBackgroundColor", Color.Parse("#060") }
            };

            return rd;
        }
    }
}
