using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VoiceCraft.Client.Services
{
    public class ThemesService
    {
        public IEnumerable<string> ThemeNames { get => _themes.Keys; }

        private ConcurrentDictionary<string, Theme> _themes = new ConcurrentDictionary<string, Theme>();
        private Theme? _currentTheme;

        public void RegisterTheme(string themeName, IStyle[] themeStyles, PlatformThemeVariant themeVariant = PlatformThemeVariant.Light)
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

        /// <summary>
        /// Get Brush from resource <paramref name="key"/>. Returns <paramref name="fallback"/> if key has not been found OR returns a default color if <paramref name="fallback"/> has not been defined.
        /// </summary>
        /// <param name="key">Key for TryGetResource</param>
        /// <param name="fallback">Fallback for when the resource cannot be found. Can be null</param>
        /// <returns>An IBrush with the value of <paramref name="key"/> or <paramref name="fallback"/> or the default color.</returns>
        public static IBrush GetBrushResource(string key, IBrush? fallback = null)
        {
            return Application.Current is not null && Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var val) && val is not null ? (IBrush)val : (fallback is not null ? fallback : new SolidColorBrush(new Color()));
        }

        private class Theme
        {
            public readonly PlatformThemeVariant Variant;
            public readonly IStyle[] ThemeStyles;

            public Theme(PlatformThemeVariant variant, IStyle[] themeStyles)
            {
                Variant = variant;
                ThemeStyles = themeStyles;
            }
        }
    }
}
