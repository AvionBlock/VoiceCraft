using Avalonia;
using Avalonia.Markup.Xaml.Styling;
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

        public void RegisterTheme(string themeName, PlatformThemeVariant themeVariant = PlatformThemeVariant.Light, params ResourceInclude[] themeResources)
        {
            var theme = new Theme(themeVariant, themeResources);
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

                foreach (var themeResource in theme.ThemeResources)
                {
                    Application.Current.Resources.MergedDictionaries.Add(themeResource);
                }
            }
        }

        private class Theme
        {
            public readonly PlatformThemeVariant Variant;
            public readonly ResourceInclude[] ThemeResources;

            public Theme(PlatformThemeVariant variant, params ResourceInclude[] themeResources)
            {
                Variant = variant;
                ThemeResources = themeResources;
            }
        }
    }
}