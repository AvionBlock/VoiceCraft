using Avalonia.Styling;
using System.Collections.Generic;

namespace VoiceCraft.Core.Services
{
    public class ThemesService
    {
        public IEnumerable<IStyle> Themes { get => _themes; }
        public IEnumerable<string> ThemeKeys { get => _themeKeys; }

        private List<IStyle> _themes = new List<IStyle>();
        private List<string> _themeKeys = new List<string>();

        public void RegisterTheme(IStyle theme, params string[] themeKeys)
        {
            _themes.Add(theme);
            foreach(var themeKey in themeKeys)
            {
                if (_themeKeys.Contains(themeKey)) continue;
                _themeKeys.Add(themeKey);
            }
        }
    }
}