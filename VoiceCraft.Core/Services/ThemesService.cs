using Avalonia.Styling;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VoiceCraft.Core.Services
{
    public class ThemesService
    {
        public delegate void ThemeChanged(Theme? from, Theme to);
        public delegate void VariantChanged(ThemeVariant? from, ThemeVariant to);

        public event ThemeChanged? OnThemeChanged;
        public event VariantChanged? OnVariantChanged;

        public IEnumerable<KeyValuePair<string, Theme>> Themes { get => _themes; }

        private ConcurrentDictionary<string, Theme> _themes = new ConcurrentDictionary<string, Theme>();
        private Theme? _currentTheme;
        private ThemeVariant? _currentVariant;

        public void RegisterTheme(string name, Theme theme)
        {
            _themes.AddOrUpdate(name, theme, (key, old) => old = theme);
        }

        public void ChangeTheme(string name)
        {
            if (_themes.TryGetValue(name, out var theme))
            {
                var currTheme = _currentTheme;
                _currentTheme = theme;

                OnThemeChanged?.Invoke(currTheme, _currentTheme);
            }
        }

        public void ChangeVariant(string name)
        {
            if (_currentTheme == null) return;

            foreach(var variant in _currentTheme.ThemeVariants)
            {
                if (variant.Key.ToString() == name)
                {
                    var currVariant = _currentVariant;
                    _currentVariant = variant;

                    OnVariantChanged?.Invoke(currVariant, _currentVariant);
                }
            }
        }
    }

    public class Theme
    {
        public readonly IStyle ThemeStyle;
        public readonly ThemeVariant[] ThemeVariants;

        public Theme(IStyle themeStyle, params ThemeVariant[] variants)
        {
            ThemeStyle = themeStyle;
            ThemeVariants = variants;
        }
    }
}