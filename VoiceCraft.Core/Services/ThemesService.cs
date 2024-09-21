using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VoiceCraft.Core.Services
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