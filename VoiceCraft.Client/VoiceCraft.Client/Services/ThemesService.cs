using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace VoiceCraft.Client.Services
{
    public class ThemesService
    {
        public IEnumerable<RegisteredTheme> RegisteredThemes => _registeredThemes.Values.ToArray();
        
        private readonly ConcurrentDictionary<Guid, RegisteredTheme> _registeredThemes = new();
        private RegisteredTheme? _currentTheme;

        public bool RegisterTheme(Guid id, string name, IStyle[] themeStyles, IResourceDictionary[] resourceDictionaries, PlatformThemeVariant themeVariant = PlatformThemeVariant.Light)
        {
            return _registeredThemes.TryAdd(id, new RegisteredTheme(id, name, themeVariant, themeStyles, resourceDictionaries));
        }

        public bool UnregisterTheme(Guid id)
        {
            return _registeredThemes.TryRemove(id, out _);
        }

        public void SwitchTheme(Guid id)
        {
            if (!_registeredThemes.TryGetValue(id, out var theme) || Application.Current == null) return;
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.RequestedThemeVariant = theme.Variant == PlatformThemeVariant.Light ? ThemeVariant.Light : ThemeVariant.Dark;

            if (_currentTheme != null)
            {
                foreach (var themeStyle in _currentTheme.ThemeStyles)
                {
                    Application.Current.Styles.Remove(themeStyle);
                }
                foreach (var resource in _currentTheme.Resources)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(resource);
                }
            }

            _currentTheme = theme;
            foreach (var themeStyle in theme.ThemeStyles)
            {
                Application.Current.Styles.Add(themeStyle);
            }
            foreach (var resource in _currentTheme.Resources)
            {
                Application.Current.Resources.MergedDictionaries.Add(resource);
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
            return Application.Current is not null && Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var val) && val is not null ? (IBrush)val : fallback ?? new SolidColorBrush(new Color());
        }
    }
    
    public class RegisteredTheme(Guid id, string name, PlatformThemeVariant variant, IStyle[] themeStyles, IResourceDictionary[] resourceDictionaries)
    {
        public readonly string Name = name;
        public readonly Guid Id = id;
        public readonly PlatformThemeVariant Variant = variant;
        public readonly IStyle[] ThemeStyles = themeStyles;
        public readonly IResourceDictionary[] Resources = resourceDictionaries;
    }
}