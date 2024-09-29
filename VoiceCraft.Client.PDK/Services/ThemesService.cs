using Avalonia;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using System.Collections.Concurrent;

namespace VoiceCraft.Client.PDK.Services
{
    public class ThemesService
    {
        public IEnumerable<string> ThemeNames { get => _themes.Keys; }

        private ConcurrentDictionary<string, Theme> _themes = new ConcurrentDictionary<string, Theme>();
        private Theme? _currentTheme;

        public void RegisterTheme(string themeName, PlatformThemeVariant themeVariant = PlatformThemeVariant.Light, params IStyle[] themeStyles)
        {
            var theme = new Theme(themeVariant, themeStyles);
            _themes.AddOrUpdate(themeName, theme, (key, old) => old = theme);
        }

        public void UnregisterTheme(string themeName)
        {
            _themes.TryRemove(themeName, out _);
        }

        public void SwitchTheme(string themeName)
        {
            if (_themes.TryGetValue(themeName, out var theme) && Application.Current != null)
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.RequestedThemeVariant = theme.Variant == PlatformThemeVariant.Light ? ThemeVariant.Light : ThemeVariant.Dark;

                if (_currentTheme != null)
                {
                    foreach (var themeStyle in _currentTheme.ThemeStyles)
                    {
                        Application.Current.Styles.Remove(themeStyle);
                    }
                }

                _currentTheme = theme;
                foreach (var themeStyle in theme.ThemeStyles)
                {
                    Application.Current.Styles.Add(themeStyle);
                }
            }
        }

        public static IBrush GetBrushFromKey(string key)
        {
            return Application.Current is not null && Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var val) && val is not null ? (IBrush)val : new SolidColorBrush(new Color());
        }

        private class Theme
        {
            public readonly PlatformThemeVariant Variant;
            public readonly IStyle[] ThemeStyles;

            public Theme(PlatformThemeVariant variant, params IStyle[] themeStyles)
            {
                Variant = variant;
                ThemeStyles = themeStyles;
            }
        }
    }
}